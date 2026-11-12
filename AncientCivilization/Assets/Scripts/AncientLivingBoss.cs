using UnityEngine;
namespace Plugin.AncientCivilization
{
    public sealed class AncientLivingBoss : MonoBehaviour
    {
        enum State { Intro,Windup,Active,Recovery,Transition,Stunned }
        AncientGame game;AncientEnemy owner;AncientLivingRules rules=new AncientLivingRules();AncientLivingAttack move;State state;
        Transform rig;SpriteRenderer torso,core,handL,handR,armL,armR,footL,footR,hatchL,hatchR,unstable;
        LineRenderer shield,armSweep;readonly LineRenderer[] marks=new LineRenderer[8];
        readonly Vector2[] drops=new Vector2[6];Vector2 aim,from,grabStart;float age,duration,shotAt,behind,catchFlash;int emitted;bool grabbed,grabAttempted,overloaded;float collected,sparkAt;
        public bool Alive {get{return owner&&owner.IsAlive;}}
        public bool InputLocked {get{return grabbed;}}
        public bool Sucking {get{return Alive&&state==State.Active&&move==AncientLivingAttack.Suction&&!overloaded;}}
        public bool CanCatchRocket {get{return Alive&&rules.Phase==3&&state!=State.Transition&&state!=State.Stunned;}}
        public Vector2 Core {get{return (Vector2)owner.transform.position+new Vector2(0,2.7f+behind*1.8f);}}
        public int Phase {get{return rules.Phase;}}
        public int Barrier {get{return rules.Barrier;}}
        public bool Telegraphing {get{return state==State.Windup;}}
        public bool Recovering {get{return state==State.Recovery||state==State.Stunned;}}
        public float Progress {get{return Telegraphing?Mathf.Clamp01(age/duration):0;}}
        public float Recovery {get{return Recovering?1-Mathf.Clamp01(age/duration):0;}}
        public bool Physical {get{return move==AncientLivingAttack.Leap||move==AncientLivingAttack.Grab;}}
        float Left {get{return AncientCityLayout.ArenaLeft(1)+1;}}
        float Right {get{return AncientCityLayout.ArenaRight(1)-1;}}
        float Centre {get{return (Left+Right)*.5f;}}
        public void Initialize(AncientEnemy enemy,AncientGame world)
        {
            owner=enemy;game=world;game.Living.Boss=this;rig=new GameObject("Articulated giant gang mech").transform;rig.SetParent(transform,false);
            torso=Part("Armoured cockpit",0,new Vector2(0,3.3f),9);core=Part("Unstable core chamber",7,new Vector2(0,2.7f),11);core.color=new Color(1,.5f,.12f);
            hatchL=Part("Left sliding core hatch",1,new Vector2(-.4f,2.7f),12);hatchR=Part("Right sliding core hatch",1,new Vector2(.4f,2.7f),12);
            hatchL.transform.localScale=hatchR.transform.localScale=new Vector3(.28f,.8f,1);
            unstable=Part("Inserted incomplete core",7,new Vector2(0,7),14);unstable.enabled=false;
            armL=Part("Left hydraulic upper arm",1,new Vector2(-2,3),8);armR=Part("Right hydraulic upper arm",1,new Vector2(2,3),8);
            handL=Part("Left rocket fist",2,new Vector2(-2.3f,1.9f),10);handR=Part("Right rocket fist",2,new Vector2(2.3f,1.9f),10);
            footL=Part("Left heavy foot",3,new Vector2(-1.2f,.8f),10);footR=Part("Right heavy foot",3,new Vector2(1.2f,.8f),10);
            shield=Line("Barrier shell",AncientGame.Cyan,.07f,13,48);shield.loop=true;
            armSweep=Line("Extending capture arm",new Color(.65f,.73f,.78f),.28f,12,3);
            for(int i=0;i<marks.Length;i++)marks[i]=Line("Mech attack warning "+i,AncientGame.Orange,.06f,15,2);
            Enter(State.Intro,2.2f);game.Story.RadioBark("두목","그 팔 하나로 이걸 멈춰 보시지.");game.Notice("거대 로봇 두목 / 로켓주먹을 와이어로 되돌리세요.",3);
        }
        SpriteRenderer Part(string name,int id,Vector2 point,int order){return AncientArt.Display(name,AncientLivingArt.Part(id),point,order,rig);}
        LineRenderer Line(string name,Color color,float width,int order,int points)
        {var line=game.Line(name,color,width,order);line.transform.SetParent(transform,true);line.positionCount=points;line.enabled=false;return line;}
        void Enter(State next,float seconds){state=next;age=0;duration=seconds;HideMarks();}
        void HideMarks(){foreach(var line in marks)if(line)line.enabled=false;if(armSweep)armSweep.enabled=false;}
        public float FilterDamage(float damage,float hp,float max)
        {return rules.FilterDamage(damage,hp,max,state!=State.Intro&&state!=State.Transition&&behind<.25f);}
        public void ReceiveHit(AncientLivingHit kind,float damage)
        {
            if(!Alive||state==State.Intro||state==State.Transition||behind>.25f)return;
            if(rules.Barrier>0)
            {
                if(rules.HitBarrier(kind))
                {ReleasePlayer();Enter(State.Stunned,3.2f);game.Living.ClearRockets();game.Notice("배리어 파괴 / 스턴",2);game.Impact(Core,AncientGame.Cyan,true);}
                else game.Impact(Core,AncientGame.Cyan,false);
                return;
            }
            if(kind==AncientLivingHit.Laser&&Sucking)
            {overloaded=true;rules.ResolveSuction(true);Enter(State.Stunned,2.8f);game.Notice("코어 과부하 / 다음 공격은 미사일",2);game.Impact(Core,AncientGame.Cyan,true);}
            owner.Hit(damage,true);
        }
        public void ShowCatch(){catchFlash=.7f;game.Notice("로켓 재투척 / 직선 궤도를 피하세요.",1.1f);game.Sound(3);}
        public void Collect(float amount){if(Sucking)collected+=Mathf.Max(0,amount);}
        public void Tick()
        {
            if(!Alive||game.Paused)return;float dt=Time.deltaTime;age+=dt;catchFlash=Mathf.Max(0,catchFlash-dt);
            if(rules.AdvancePhase(owner.HealthRatio))
            {
                ReleasePlayer();game.ClearBattleProjectiles();game.Living.ClearRockets();behind=0;owner.transform.position=new Vector2(Mathf.Clamp(owner.transform.position.x,Left+3,Right-3),0);
                Enter(State.Transition,2.6f);game.Notice(rules.Phase==3?"3페이즈 / 불완전 코어 가동":"2페이즈 / 중화기 전개",3);
                game.Story.RadioBark("두목",rules.Phase==3?"아직 검수가 안 끝났다고? 지금 필요하다. 넣어!":"아직 끝난 게 아니야. 배리어를 켜!");
            }
            if(state==State.Windup&&(move==AncientLivingAttack.Cannon||move==AncientLivingAttack.XBlast||move==AncientLivingAttack.Suction))
                owner.transform.position=new Vector2(Mathf.Lerp(from.x,Centre,Mathf.SmoothStep(0,1,age/.8f)),0);
            Animate(dt);
            if(state==State.Intro||state==State.Transition||state==State.Recovery||state==State.Stunned)
            {if(age>=duration)StartAttack();return;}
            if(state==State.Windup)
            {Warn();if(age>=duration)BeginAttack();return;}
            Attack(dt);
        }
        void StartAttack()
        {
            ReleasePlayer();move=rules.Next();aim=game.hero.transform.position;from=owner.transform.position;emitted=0;shotAt=0;grabAttempted=false;overloaded=false;collected=0;
            float tell=move==AncientLivingAttack.Rocket||move==AncientLivingAttack.DoubleRocket?AncientLivingRules.RocketCharge:move==AncientLivingAttack.XBlast?2.5f:move==AncientLivingAttack.Suction?1.6f:1.1f;
            if(move==AncientLivingAttack.Grab&&rules.Barrier==0)rules.RaiseBarrier();
            if(move==AncientLivingAttack.Cannon)
                for(int i=0;i<6;i++)drops[i]=new Vector2(Mathf.Clamp(aim.x+(i-2.5f)*2.4f,Left+1,Right-1),.12f);
            if(move==AncientLivingAttack.Leap)aim=new Vector2(Mathf.Clamp(aim.x,Left+3,Right-3),0);
            Enter(State.Windup,tell);
            if(move==AncientLivingAttack.Suction)game.Notice("입자 흡입 / 천장 와이어로 버티기 또는 코어에 레이저",3);
            if(move==AncientLivingAttack.Grab)game.Notice("배리어 / 레이저 1 · 투척 10 · 베기 7",2.5f);
        }
        void Warn()
        {
            if(move==AncientLivingAttack.Leap){Segment(0,aim+Vector2.left*3.5f+Vector2.up*.1f,aim+Vector2.right*3.5f+Vector2.up*.1f,AncientGame.Orange,.09f);return;}
            if(move==AncientLivingAttack.Cannon)
            {for(int i=0;i<6;i++)Segment(i,drops[i]+Vector2.left*.7f,drops[i]+Vector2.right*.7f,AncientGame.Orange,.06f);return;}
            if(move==AncientLivingAttack.XBlast){DrawX(false);return;}
            if(move==AncientLivingAttack.Suction)return;
            if(move==AncientLivingAttack.Grab)
            {Vector2 end=Core+(aim-Core).normalized*7;Segment(0,Core,end,AncientGame.Orange,.06f);return;}
            if(move==AncientLivingAttack.Rocket||move==AncientLivingAttack.DoubleRocket)
            {
                // Charge locks only the arm pose. Launched fists track until caught; return throws never track.
                Segment(0,handL.transform.position,game.hero.transform.position,AncientGame.Cyan,.035f);
                if(move==AncientLivingAttack.DoubleRocket)Segment(1,handR.transform.position,game.hero.transform.position,AncientGame.Cyan,.035f);return;
            }
            Segment(0,Core,aim,AncientGame.Orange,.035f);
        }
        void BeginAttack()
        {
            Enter(State.Active,move==AncientLivingAttack.Leap?1:move==AncientLivingAttack.Suction?5:move==AncientLivingAttack.XBlast?.75f:move==AncientLivingAttack.Grab?4.5f:move==AncientLivingAttack.Cannon?4.4f:3);
            if(move==AncientLivingAttack.Rocket||move==AncientLivingAttack.DoubleRocket)
            {game.Living.LaunchRocket(this,handL.transform.position,move==AncientLivingAttack.DoubleRocket);if(move==AncientLivingAttack.DoubleRocket)game.Living.LaunchRocket(this,handR.transform.position,true);duration=4;}
            if(move==AncientLivingAttack.Suction)
            {game.hero.Combat.EmitParticles(new Vector2(Left+3,2),24);game.hero.Combat.EmitParticles(new Vector2(Right-3,2),24);}
            game.Sound(4);
        }
        void Attack(float dt)
        {
            Vector2 player=game.hero.transform.position;
            if(move==AncientLivingAttack.Bullets)
            {
                int count=AncientLivingRules.BulletCount(rules.Phase);
                if(emitted<count&&age>=shotAt)
                {if(emitted%5==0)aim=player;Vector2 dir=(aim-Core).normalized;Fire(Core+dir*2.6f,Rotate(dir,(emitted%5-2)*4)*17,false,12);emitted++;shotAt+=.12f;}
            }
            else if(move==AncientLivingAttack.Leap)
            {
                float t=Mathf.Clamp01(age/.9f);owner.transform.position=Vector2.Lerp(from,aim,t)+Vector2.up*(Mathf.Sin(t*Mathf.PI)*4);
                if(t>=1&&emitted==0)
                {emitted=1;game.Shake(.18f);game.Impact(aim,AncientGame.Orange,true);if(Mathf.Abs(player.x-aim.x)<3.5f&&player.y<2.6f){game.hero.Hurt(24);game.hero.AddEncounterImpulse(new Vector2(Mathf.Sign(player.x-aim.x)*10,8));}}
            }
            else if(move==AncientLivingAttack.Missiles||move==AncientLivingAttack.CrossMissiles)
            {
                int count=move==AncientLivingAttack.CrossMissiles?16:8;
                if(emitted<count&&age>=shotAt)
                {
                    Vector2 origin;
                    if(move==AncientLivingAttack.CrossMissiles)
                    {
                        int side=emitted%4;origin=side==0?new Vector2(Left+.2f,2+emitted%3):side==1?new Vector2(Right-.2f,3+emitted%3):new Vector2(Mathf.Lerp(Left+2,Right-2,(emitted%7)/6f),8.7f);
                    }
                    else origin=Core+new Vector2(emitted%2==0?-2.4f:2.4f,1);
                    // A visible ignition delay gives off-axis missiles a warning before their straight flight.
                    game.Living.QueueMissile(origin,player,move==AncientLivingAttack.CrossMissiles?.55f:.35f);emitted++;shotAt+=move==AncientLivingAttack.CrossMissiles?.25f:.3f;
                }
                if(emitted<count)return;
            }
            else if(move==AncientLivingAttack.Cannon)
            {
                for(int i=emitted;i<6;i++)Segment(i,drops[i]+Vector2.left*.7f,drops[i]+Vector2.right*.7f,AncientGame.Orange,.08f);
                if(emitted<6&&age>=.8f+emitted*.5f)
                {Vector2 p=drops[emitted++];game.Burst(Core,AncientGame.Orange,8);Fire(Core,Vector2.up*14,false,23);Fire(new Vector2(p.x,8.8f),Vector2.down*10,false,23,9);}
            }
            else if(move==AncientLivingAttack.Grab)
            {
                if(grabbed)
                {
                    float t=Mathf.Clamp01((age-shotAt)/.65f);game.hero.transform.position=Vector2.Lerp(grabStart,Core+Vector2.right*Mathf.Sign(grabStart.x-Core.x)*2.4f+Vector2.down*.8f,t);
                    armSweep.enabled=true;armSweep.SetPosition(0,Core);armSweep.SetPosition(1,Core+Vector2.up);armSweep.SetPosition(2,game.hero.transform.position);
                    if(age-shotAt>1.05f){ReleasePlayer();game.hero.Hurt(38);game.hero.AddEncounterImpulse(new Vector2(Mathf.Sign(grabStart.x-Core.x)*11,5));Finish();return;}
                }
                else if(!grabAttempted)
                {
                    float delta=player.x-owner.transform.position.x;
                    if(Mathf.Abs(delta)>6.5f)owner.BossStep(Mathf.Sign(delta)*3.2f*dt);
                    else
                    {
                        if(shotAt==0){shotAt=age+.9f;aim=player;}
                        Segment(0,Core,Core+(aim-Core).normalized*7,AncientGame.Orange,.09f);
                        if(age>=shotAt)
                        {
                            grabAttempted=true;Vector2 end=Core+(aim-Core).normalized*7;
                            Segment(0,Core,end,new Color(.75f,.8f,.85f),.35f);
                            if(AncientLivingRules.BeamHit(Core.x,Core.y,end.x,end.y,player.x,player.y,1)&&!Physics2D.Linecast(Core,player,1<<8))
                            {grabbed=true;grabStart=player;shotAt=age;game.hero.HoldForEncounter(true);}
                        }
                    }
                }
            }
            else if(move==AncientLivingAttack.XBlast)
            {
                DrawX(true);for(int i=0;i<2;i++)
                {Vector2 a=marks[i].GetPosition(0),b=marks[i].GetPosition(1);if(AncientLivingRules.BeamHit(a.x,a.y,b.x,b.y,player.x,player.y,2.25f))game.hero.Hurt(32);}
            }
            else if(move==AncientLivingAttack.Suction)
            {
                if(Vector2.Distance(player,Core)<1.6f)game.hero.Hurt(16);
                if(age>=duration){rules.ResolveSuction(false);Finish();return;}
            }
            if(age>=duration)Finish();
        }
        void Finish(){ReleasePlayer();Enter(State.Recovery,move==AncientLivingAttack.Grab?1.8f:1.3f);}
        void ReleasePlayer(){if(!grabbed)return;grabbed=false;if(game&&game.hero){game.hero.HoldForEncounter(false);game.BlockGameplayInput();}}
        void Fire(Vector2 p,Vector2 velocity,bool breakable,float damage,float gravity=0)
        {
            if(game.shots.Count>=60)return;var shot=game.Projectiles.Spawn(p,velocity,damage,false,0,gravity);if(shot)shot.SetLivingProjectile(breakable,gravity>0);
        }
        static Vector2 Rotate(Vector2 v,float degrees){float r=degrees*Mathf.Deg2Rad;return new Vector2(v.x*Mathf.Cos(r)-v.y*Mathf.Sin(r),v.x*Mathf.Sin(r)+v.y*Mathf.Cos(r));}
        void Segment(int i,Vector2 a,Vector2 b,Color color,float width)
        {var line=marks[i];line.enabled=true;line.startColor=line.endColor=color;line.startWidth=line.endWidth=width;line.SetPosition(0,a);line.SetPosition(1,b);}
        void DrawX(bool firing)
        {
            Vector2 a=new Vector2(Left, .4f),b=new Vector2(Right,8.8f);Color color=firing?new Color(.4f,.8f,1):new Color(.3f,.65f,1,.2f);
            Segment(0,a,b,color,3f);Segment(1,new Vector2(Left,8.8f),new Vector2(Right,.4f),color,3f);
        }
        void Animate(float dt)
        {
            bool back=(move==AncientLivingAttack.Cannon||move==AncientLivingAttack.XBlast)&&(state==State.Windup||state==State.Active);
            behind=Mathf.MoveTowards(behind,back?1:0,dt*1.5f);rig.localScale=Vector3.one*Mathf.Lerp(1,.62f,behind);rig.localPosition=Vector3.up*behind*2.8f;
            float breath=Mathf.Sin(Time.time*2)*.035f;torso.transform.localPosition=new Vector2(0,3.3f+breath);
            float punch=Telegraphing&&(move==AncientLivingAttack.Rocket||move==AncientLivingAttack.DoubleRocket)?Progress:0;
            armL.transform.localRotation=Quaternion.Euler(0,0,-punch*40-catchFlash*40);armR.transform.localRotation=Quaternion.Euler(0,0,punch*40+catchFlash*40);
            handL.transform.localPosition=new Vector2(-2.3f-punch*.35f,1.9f+punch*1.5f);handR.transform.localPosition=new Vector2(2.3f+punch*.35f,1.9f+punch*1.5f);
            float walk=state==State.Active&&move==AncientLivingAttack.Grab&&!grabbed?Mathf.Sin(age*8)*.16f:0;
            footL.transform.localPosition=new Vector2(-1.2f,.8f+Mathf.Max(0,walk));footR.transform.localPosition=new Vector2(1.2f,.8f+Mathf.Max(0,-walk));
            bool insertion=rules.Phase==3&&state==State.Transition;
            float open=rules.Phase==3?insertion?Mathf.SmoothStep(0,1,age/.8f):1:Sucking?1:0;
            hatchL.transform.localPosition=new Vector2(-.4f-open*.6f,2.7f);hatchR.transform.localPosition=new Vector2(.4f+open*.6f,2.7f);
            unstable.enabled=insertion&&age<1.8f;unstable.transform.localPosition=Vector2.Lerp(new Vector2(0,7),new Vector2(0,2.7f),Mathf.SmoothStep(0,1,(age-.4f)/1.3f));
            core.enabled=!insertion||age>=1.7f;
            core.color=rules.Phase==3?new Color(.25f,.72f,1):new Color(1,.5f,.12f);
            core.transform.localScale=Vector3.one*(Sucking?1.1f+Mathf.Clamp01(collected/48)*.4f:1);core.transform.localRotation=Quaternion.Euler(0,0,rules.Phase==3?45:0);
            if(rules.Phase==3&&core.enabled)
            {
                float pulse=.5f+.5f*Mathf.Sin(Time.time*8);torso.color=Color.Lerp(Color.white,new Color(.55f,.8f,1),pulse*.25f);
                // Two crossing luminous conduits make the X readable even with the core chamber shut.
                for(int k=2;k<4;k++)
                {float sign=k==2?1:-1;Segment(k,Core+new Vector2(-.6f,sign*.7f),Core+new Vector2(.6f,-sign*.7f),new Color(.25f,.7f,1),.12f);}
                if(Time.time>=sparkAt){sparkAt=Time.time+.18f;game.Burst(Core+new Vector2(Mathf.Sin(Time.time*6)*2.5f,1),AncientGame.Cyan,2);}
            }
            shield.enabled=rules.Barrier>0;
            if(shield.enabled)for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;shield.SetPosition(i,Core+new Vector2(Mathf.Cos(a)*3.4f,Mathf.Sin(a)*3));}
        }
        public void StopBattle(){ReleasePlayer();HideMarks();if(shield)shield.enabled=false;if(game&&game.Living){game.Living.ClearRockets();if(game.Living.Boss==this)game.Living.Boss=null;}}
        void OnDestroy(){StopBattle();}
    }
}
