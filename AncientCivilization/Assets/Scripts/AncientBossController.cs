using UnityEngine;
namespace Plugin.AncientCivilization
{
    // Each boss owns one predictable sequence; attacks lock aim during their visible tell.
    public sealed class AncientBossController : MonoBehaviour
    {
        enum State { Idle,Windup,Volley,Rush,Recovery,Reposition }
        AncientEnemy owner;AncientGame game;LineRenderer warning;
        readonly LineRenderer[] marks=new LineRenderer[3];readonly Vector2[] drops=new Vector2[3];
        readonly GameObject[] panels=new GameObject[2];
        AncientClockBoss clock;AncientSniperEncounter sniper;AncientLivingBoss living;
        State state;AncientBossMove move;int floor,turn,remaining,emitted,rushes;
        float until,began,interval,facing=1,wallX,rushLeft;bool announced,phaseTwo;
        Vector2 aim;float left,right;
        public bool Telegraphing {get{return living?living.Telegraphing:sniper?sniper.Telegraphing:clock?clock.Telegraphing:state==State.Windup;}}
        public bool Recovering {get{return living?living.Recovering:sniper?sniper.Recovering:clock?clock.Recovering:state==State.Recovery;}}
        public bool Physical {get{return living?living.Physical:sniper?false:clock?clock.Physical:AncientBossRules.Physical(move);}}
        public float WindupProgress {get{return living?living.Progress:sniper?sniper.WindupProgress:clock?clock.WindupProgress:Telegraphing?Mathf.Clamp01((Time.time-began)/Mathf.Max(.01f,until-began)):0;}}
        public float RecoveryRemaining {get{return living?living.Recovery:sniper?sniper.RecoveryRemaining:clock?clock.RecoveryRemaining:Recovering?Mathf.Clamp01((until-Time.time)/AncientBossRules.Opening(move)):0;}}
        public void Initialize(AncientEnemy enemy,AncientGame world,LineRenderer line)
        {
            owner=enemy;game=world;warning=line;
            if(enemy.IsLivingBoss){living=gameObject.AddComponent<AncientLivingBoss>();living.Initialize(enemy,world);enemy.BindLivingBoss(living);return;}
            if(enemy.IsSecretary){clock=gameObject.AddComponent<AncientClockBoss>();clock.Initialize(enemy,world);return;}
            if(enemy.IsDistrictBoss&&enemy.DistrictFloor==4){sniper=gameObject.AddComponent<AncientSniperEncounter>();sniper.Initialize(enemy,world);return;}
            floor=owner.IsDistrictBoss?owner.DistrictFloor:0;
            left=floor==0?100:AncientCityLayout.BossArenaLeft(floor,enemy.IsSecretary)+1;
            right=floor==0?114:AncientCityLayout.BossArenaRight(floor,enemy.IsSecretary)-1;
            for(int i=0;i<6;i++)AncientArt.Boss(floor,i);
            for(int i=0;i<3;i++)
            {
                marks[i]=game.Line("Debris landing marker",AncientGame.Orange,.05f,7);
                marks[i].loop=true;marks[i].positionCount=16;marks[i].enabled=false;
            }
            if(floor==4)
            {
                for(int i=0;i<2;i++)
                {
                    float x=i==0?left-.45f:right+.45f;
                    panels[i]=game.Box("Ballistic reflector",new Vector2(x,5),new Vector2(.2f,10),new Color(.2f,.5f,.65f),4);
                    panels[i].layer=8;panels[i].AddComponent<BoxCollider2D>();
                }
            }
            state=State.Idle;until=Time.time+1.3f;
        }
        void HideMarks(){for(int i=0;i<3;i++)if(marks[i])marks[i].enabled=false;}
        public void Tick()
        {
            if(!owner.IsAlive)return;
            if(living){living.Tick();return;}
            if(clock){clock.Tick();return;}
            if(sniper){sniper.Tick();return;}
            if(owner.Enraged&&!announced)
            {announced=true;game.Notice(owner.Role+" / 2단계",2);game.Sound(8);game.Impact(owner.AimPoint,AncientGame.Orange,true);}
            owner.BossContact();
            if(state==State.Windup)
            {
                DrawWarning();owner.BossVisual(Physical?1:3,facing,WindupProgress,true,false);
                if(Time.time>=until)Resolve();return;
            }
            warning.enabled=false;
            if(state==State.Rush)
            {
                float step=Mathf.Min(rushLeft,12*Time.deltaTime);rushLeft-=step;
                if(!owner.BossStep(facing*step))rushLeft=0;
                owner.BossVisual(2,facing,0,false,false);
                if(rushLeft<=.001f)
                {
                    if(--rushes>0)StartWindup(.65f);else Open();
                }
                return;
            }
            if(state==State.Volley)
            {
                owner.BossVisual(4,facing,0,false,false);
                if(Time.time>=until)
                {
                    Emit();remaining--;emitted++;until=Time.time+interval;
                    if(remaining<=0)Open();
                }
                return;
            }
            if(state==State.Recovery)
            {
                owner.BossVisual(5,facing,0,false,true);
                if(Time.time>=until)
                {
                    if(floor==4&&owner.Enraged){state=State.Reposition;until=Time.time+.65f;}
                    else{state=State.Idle;until=Time.time+.25f;}
                }
                return;
            }
            Vector2 delta=(Vector2)game.hero.transform.position-owner.AimPoint;
            facing=delta.x<0?-1:1;owner.BossVisual(0,facing,0,false,false);
            if(state==State.Reposition)
            {
                float destination=game.hero.transform.position.x<(left+right)*.5f?right-2:left+2;
                owner.BossStep(Mathf.Clamp(destination-owner.transform.position.x,-7*Time.deltaTime,7*Time.deltaTime));
                if(Time.time>=until){state=State.Idle;until=Time.time+.3f;}return;
            }
            if(floor!=4&&Mathf.Abs(delta.x)>3.8f&&Time.time<until)owner.BossStep(facing*2.2f*Time.deltaTime);
            if(Time.time<until)return;
            phaseTwo=owner.Enraged;move=AncientBossRules.Choose(floor,turn++,phaseTwo,Mathf.Abs(delta.x));
            rushes=phaseTwo?2:1;StartWindup(AncientBossRules.Windup(move));
        }
        void StartWindup(float seconds)
        {
            state=State.Windup;began=Time.time;until=began+seconds;aim=game.hero.transform.position;
            facing=aim.x<owner.AimPoint.x?-1:1;HideMarks();
            if(move==AncientBossMove.BankShot)
                wallX=aim.x<(left+right)*.5f?right+.35f:left-.35f;
            if(move==AncientBossMove.CeilingDebris)
            {
                for(int i=0;i<3;i++)
                {
                    drops[i]=new Vector2(Mathf.Clamp(aim.x+(i-1)*4,left+1,right-1),8.7f);
                    var line=marks[i];line.enabled=true;line.loop=true;line.positionCount=16;
                    line.startColor=line.endColor=AncientGame.Orange;
                    for(int j=0;j<16;j++){float a=j*Mathf.PI*2/16;line.SetPosition(j,new Vector2(drops[i].x+Mathf.Cos(a)*.7f,.12f+Mathf.Sin(a)*.08f));}
                }
            }
            if(move==AncientBossMove.FloorRoll)
            {
                game.Notice("지면 양방향 · 점프로 회피",seconds);
                DrawFloorRollWarning();
            }
        }
        void DrawFloorRollWarning()
        {
            int rows=AncientBossRules.Count(move,phaseTwo)/2;
            Color color=AncientGame.Cyan;color.a=.45f+.5f*WindupProgress;
            for(int row=0;row<rows;row++)
            {
                // The arrowheads show both travel directions at the actual slab heights.
                float y=AncientBossRules.FloorRollHeight(row*2);var line=marks[row];
                line.enabled=true;line.loop=false;line.positionCount=8;line.startColor=line.endColor=color;
                line.SetPosition(0,new Vector2(left+.35f,y+.18f));line.SetPosition(1,new Vector2(left,y));
                line.SetPosition(2,new Vector2(left+.35f,y-.18f));line.SetPosition(3,new Vector2(left,y));
                line.SetPosition(4,new Vector2(right,y));line.SetPosition(5,new Vector2(right-.35f,y+.18f));
                line.SetPosition(6,new Vector2(right,y));line.SetPosition(7,new Vector2(right-.35f,y-.18f));
            }
            float cueX=Mathf.Clamp(aim.x,left+.5f,right-.5f),cueY=AncientBossRules.FloorRollHeight((rows-1)*2)+1.8f;
            var jump=marks[2];jump.enabled=true;jump.loop=false;jump.positionCount=5;jump.startColor=jump.endColor=color;
            jump.SetPosition(0,new Vector2(cueX-.35f,cueY));jump.SetPosition(1,new Vector2(cueX,cueY+.45f));
            jump.SetPosition(2,new Vector2(cueX+.35f,cueY));jump.SetPosition(3,new Vector2(cueX,cueY+.45f));
            jump.SetPosition(4,new Vector2(cueX,cueY-.15f));
        }
        void DrawWarning()
        {
            warning.enabled=move!=AncientBossMove.CeilingDebris&&move!=AncientBossMove.FloorRoll;
            if(move==AncientBossMove.FloorRoll){DrawFloorRollWarning();return;}
            warning.startColor=warning.endColor=Physical?AncientGame.Orange:AncientGame.Cyan;
            Vector2 begin=owner.AimPoint;
            if(move==AncientBossMove.BankShot)
            {
                float y=AncientBossRules.BankHeight(begin.x,begin.y,aim.x,aim.y,wallX);
                warning.positionCount=3;warning.SetPosition(0,begin);warning.SetPosition(1,new Vector2(wallX,y));warning.SetPosition(2,aim);
            }
            else if(move==AncientBossMove.ArcCrate||move==AncientBossMove.CoreLob)
            {
                Vector2 velocity=(aim-begin)/.95f+Vector2.up*6.65f;warning.positionCount=14;
                for(int i=0;i<14;i++){float t=i*.95f/13;warning.SetPosition(i,begin+velocity*t+Vector2.down*7*t*t);}
            }
            else
            {warning.positionCount=2;warning.SetPosition(0,begin);warning.SetPosition(1,Physical?begin+Vector2.right*facing*(move==AncientBossMove.Rush?6:4.8f):aim);}
        }
        void Resolve()
        {
            warning.enabled=false;
            if(move==AncientBossMove.Rush){state=State.Rush;rushLeft=6;return;}
            if(move==AncientBossMove.Slam)
            {
                Vector2 delta=(Vector2)game.hero.transform.position-owner.AimPoint;
                if(Mathf.Abs(delta.x)<4.8f&&Mathf.Abs(delta.y)<1.7f&&delta.x*facing>-.6f&&!Physics2D.Linecast(owner.AimPoint,game.hero.transform.position,1<<8))game.hero.Hurt(30);
                game.Slash(owner.AimPoint,facing,true);game.Shake(.15f);Open();return;
            }
            remaining=AncientBossRules.Count(move,phaseTwo);emitted=0;interval=move==AncientBossMove.CoreLob?.32f:move==AncientBossMove.SniperBurst?.24f:.2f;
            if(move==AncientBossMove.SpreadThrow||move==AncientBossMove.FanVolley||move==AncientBossMove.FloorRoll||move==AncientBossMove.CeilingDebris)
            {Emit();Open();return;}
            state=State.Volley;until=Time.time;
        }
        void Shot(Vector2 origin,Vector2 velocity,int kind=0,float gravity=0,int ricochets=0)
        {if(game.shots.Count<48)game.Projectiles.Spawn(origin,velocity,floor==4?18:20,false,kind,gravity,ricochets);}
        void Emit()
        {
            Vector2 origin=owner.AimPoint;
            if(move==AncientBossMove.CeilingDebris)
            {for(int i=0;i<3;i++)Shot(drops[i],Vector2.down*6,2,10);HideMarks();return;}
            if(move==AncientBossMove.FloorRoll)
            {
                int count=AncientBossRules.Count(move,phaseTwo);
                for(int i=0;i<count;i++)Shot(new Vector2(owner.transform.position.x,AncientBossRules.FloorRollHeight(i)),Vector2.right*AncientBossRules.FloorRollVelocity(i),1);
                return;
            }
            if(move==AncientBossMove.ArcCrate||move==AncientBossMove.CoreLob)
            {
                Vector2 target=aim;if(move==AncientBossMove.CoreLob)target.x=Mathf.Clamp(target.x+(emitted-(AncientBossRules.Count(move,phaseTwo)-1)*.5f)*3.5f,left,right);
                Shot(origin,(target-origin)/.95f+Vector2.up*6.65f,2,14);return;
            }
            if(move==AncientBossMove.BankShot)
            {Vector2 virtualAim=new Vector2(2*wallX-aim.x,aim.y);Vector2 direction=(virtualAim-origin).normalized;Shot(origin+direction*.12f,direction*16,0,0,1);return;}
            Vector2 basis=(aim-origin).normalized;if(basis.sqrMagnitude<.1f)basis=Vector2.right*facing;
            if(move==AncientBossMove.SpreadThrow||move==AncientBossMove.FanVolley)
            {
                int count=AncientBossRules.Count(move,phaseTwo);
                for(int i=0;i<count;i++)
                {float a=(i-(count-1)*.5f)*12*Mathf.Deg2Rad;Vector2 d=new Vector2(basis.x*Mathf.Cos(a)-basis.y*Mathf.Sin(a),basis.x*Mathf.Sin(a)+basis.y*Mathf.Cos(a));Shot(origin+d*.3f,d*9,1);}
                return;
            }
            Shot(origin+basis*.3f,basis*(move==AncientBossMove.SniperBurst?18:14),move==AncientBossMove.StampBurst?1:0);
        }
        void Open(){HideMarks();state=State.Recovery;until=Time.time+AncientBossRules.Opening(move);}
        void OnDestroy()
        {foreach(var mark in marks)if(mark)Destroy(mark.gameObject);foreach(var panel in panels)if(panel)Destroy(panel);}
    }
}
