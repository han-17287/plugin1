using UnityEngine;
namespace Plugin.AncientCivilization
{
    // Joker is a mask/callsign here. His personal identity and past remain unrevealed.
    public sealed class AncientSniperEncounter : MonoBehaviour
    {
        sealed class Actor
        {
            public AncientEnemy enemy;public LineRenderer tell;public int state,shots,magazine,grenades;
            public bool grenadeUsed;public float until,began,flash;public Vector2 aim;
        }
        sealed class Turret
        {
            public GameObject root;public SpriteRenderer body,barrel;public LineRenderer tell;
            public Vector2 position,aim;public bool broken,charging;public float until,started,riseAt,hp=65;
        }
        sealed class ObjectShot
        {public SpriteRenderer art;public Vector2 position,velocity;public bool live,friendly,grenade;public float age;}
        AncientGame game;AncientEnemy owner;AncientSniperGround ground;
        readonly Actor[] actors=new Actor[4];readonly Turret[] turrets=new Turret[2];
        readonly ObjectShot[] objects=new ObjectShot[24];int cursor,phase=1,mounted=-1,droppedSpawns;
        float left,right,refillAt,impactAt,winningAt=-1;bool clashing,finished;Vector2 heroAnchor;
        LineRenderer heroBeam,bossBeam,heroCore,bossCore,heroGlow,bossGlow;AncientBeamVisuals heroBeamProfile,bossBeamProfile;AncientSniperClash clash;
        Material cloneMaterial;GUIStyle label,title;
        public AncientEnemy Owner {get{return owner;}}
        public int Phase {get{return phase;}}
        public int TurretsBroken {get{return (turrets[0]!=null&&turrets[0].broken?1:0)+(turrets[1]!=null&&turrets[1].broken?1:0);}}
        public int MountedIndex {get{return mounted;}}
        public bool Clashing {get{return clashing;}}
        public int LiveObjects {get{int count=0;foreach(var item in objects)if(item!=null&&item.live)count++;return count;}}
        public int ObjectCapacity {get{return objects.Length;}}
        public int DroppedSpawns {get{return droppedSpawns;}}
        static readonly string[] phaseHints={"","저격수 1페이즈 / 조준 2초 · 5발 후 재장전","저격수 2페이즈 / 조준 2초 · 5발 후 재장전","저격수 3페이즈 / 조준 2초 · 5발 후 재장전"};
        public bool InputLocked {get{return clashing||mounted>=0;}}
        public bool Telegraphing {get{return phase<4&&actors[0]!=null&&actors[0].state==1;}}
        public bool Recovering {get{return phase<4&&actors[0]!=null&&actors[0].state==2;}}
        public float WindupProgress {get{var a=actors[0];return Telegraphing?Mathf.Clamp01((Time.time-a.began)/Mathf.Max(.01f,a.until-a.began)):0;}}
        public float RecoveryRemaining {get{return Recovering?Mathf.Clamp01((actors[0].until-Time.time)/AncientSniperRules.ReloadSeconds):0;}}
        public void Initialize(AncientEnemy enemy,AncientGame world)
        {
            owner=enemy;owner.BindSniperEncounter(this);game=world;game.Sniper=this;left=AncientCityLayout.ArenaLeft(4)+1;right=AncientCityLayout.ArenaRight(4)-1;
            var floor=new GameObject("Recoverable sniper arena floor");ground=floor.AddComponent<AncientSniperGround>();ground.Initialize(game,left-1,right+1);
            var shader=Resources.Load<Shader>("Shaders/AncientEnergyClone");cloneMaterial=new Material(shader?shader:Shader.Find("Sprites/Default"));
            for(int i=0;i<4;i++)
            {
                actors[i]=new Actor();actors[i].tell=game.Line("Two-second sniper aim "+i,i==0?new Color(1,.4f,.35f):AncientGame.Cyan,.025f,15);
                actors[i].tell.positionCount=2;actors[i].tell.enabled=false;
            }
            actors[0].enemy=owner;actors[0].until=Time.time+1;
            for(int i=0;i<6;i++)AncientSniperEquipmentArt.Sprite(i);
            for(int i=0;i<2;i++)BuildTurret(i);
            for(int i=0;i<objects.Length;i++)
            {
                objects[i]=new ObjectShot();objects[i].art=AncientArt.Display("Pooled grenade / compressed plate",AncientSniperEquipmentArt.Sprite(3),Vector2.zero,14);
                objects[i].art.enabled=false;
            }
            heroBeam=game.Line("Automatic player particle laser",new Color(.3f,.9f,1),.80f,23);
            bossBeam=game.Line("Charged sniper laser",new Color(1,.25f,.4f),.90f,23);
            heroCore=game.Line("Player laser core",Color.white,.26f,24);bossCore=game.Line("Sniper laser core",new Color(1,.95f,.95f),.30f,24);
            heroGlow=game.Line("Player clash laser soft light",new Color(.15f,.65f,1,.5f),1.7f,22);
            bossGlow=game.Line("Sniper clash laser soft light",new Color(1,.15f,.3f,.5f),1.8f,22);
            heroBeamProfile=new AncientBeamVisuals(heroGlow);bossBeamProfile=new AncientBeamVisuals(bossGlow);
            heroBeam.positionCount=bossBeam.positionCount=heroCore.positionCount=bossCore.positionCount=heroGlow.positionCount=bossGlow.positionCount=2;ShowBeams(false);
            game.Notice("저격수 / 조준 2초 · 5발 뒤 재장전 · 수류탄에 파괴된 지면은 복구됩니다.",4);
        }
        void BuildTurret(int i)
        {
            var t=new Turret();turrets[i]=t;t.position=new Vector2(i==0?left+4:right-4,0);
            t.root=new GameObject("Emerging compressed-matter turret "+i);t.root.transform.position=t.position;
            // Display takes local coordinates when parented; the root owns the arena position.
            AncientArt.Display("Turret ground hatch",AncientSniperEquipmentArt.Sprite(5),Vector2.zero,7,t.root.transform);
            t.body=AncientArt.Display("Deployable armored turret",AncientSniperEquipmentArt.Sprite(0),Vector2.zero,8,t.root.transform);
            AncientArt.Display("Recoverable turret power core",AncientSniperEquipmentArt.Sprite(4),Vector2.up*.8f,9,t.root.transform).transform.localScale=Vector3.one*.65f;
            t.barrel=AncientArt.Display("Compressed matter launcher",AncientSniperEquipmentArt.Sprite(1),Vector2.up*1.4f,10,t.root.transform);
            t.tell=game.Line("Slow turret compression",AncientGame.Orange,.05f,14);t.tell.positionCount=2;t.tell.enabled=false;t.root.SetActive(false);
        }
        void SpawnClone(int slot,Vector2 position)
        {
            var a=actors[slot];if(a.enemy)return;
            var e=new GameObject("Blue energy sniper clone "+slot).AddComponent<AncientEnemy>();e.Initialize(1);e.ConfigureDistrict(4,false);e.ConfigureSniperClone(this,slot);
            e.transform.position=position;a.enemy=e;a.state=0;a.shots=0;a.grenadeUsed=false;a.until=Time.time+.7f;game.enemies.Add(e);
        }
        public Material CloneMaterial {get{return cloneMaterial;}}
        public void CloneDown(int slot,Vector2 point)
        {if(slot<1||slot>3)return;actors[slot].enemy=null;actors[slot].tell.enabled=false;game.hero.Combat.EmitParticles(point,18);game.Burst(point,AncientGame.Cyan,12);}
        void RemoveClones()
        {
            for(int i=1;i<4;i++)
            {if(actors[i]==null)continue;if(actors[i].enemy){game.enemies.Remove(actors[i].enemy);Destroy(actors[i].enemy.gameObject);}actors[i].enemy=null;actors[i].tell.enabled=false;}
        }
        public float FilterDamage(float damage,float hp,float maxHP)
        {return phase==4||finished?0:Mathf.Min(damage,Mathf.Max(0,hp-maxHP*AncientSniperRules.HealthFloor(phase)));}
        void ChangePhase(int next)
        {
            phase=next;ClearObjects();foreach(var shot in game.shots.ToArray())if(shot)shot.Remove();
            owner.FaceToward(game.hero.transform.position.x);game.Sound(8);game.Impact(owner.AimPoint,AncientGame.Cyan,true);
            if(next==2)
            {SpawnClone(1,new Vector2(left+1,0));game.Notice("2페이즈 / 에너지 분신 · 두 곳의 조준을 확인하세요.",4);}
            if(next==3)
            {
                RemoveClones();for(int i=0;i<2;i++){turrets[i].root.SetActive(true);turrets[i].broken=false;turrets[i].until=Time.time+1;turrets[i].riseAt=Time.time;}
                SpawnClone(1,new Vector2(left+1,0));SpawnClone(2,turrets[0].position+Vector2.up*.65f);SpawnClone(3,turrets[1].position+Vector2.up*.65f);
                game.Notice("3페이즈 / 포탑 분신을 제압하면 가까이서 E로 조종 · 좌클릭 압축 발사 · E 하차",5);
            }
            if(next==4)
            {
                Dismount();RemoveClones();ground.RestoreAll();game.hero.Combat.ResetCombat();
                owner.transform.position=new Vector2(right-1,0);RestoreTurrets();refillAt=Time.time+7;
                game.Notice("파이널 / 저격수 충전 중 무적\n포탑 두 개를 부숴 나온 입자를 우클릭으로 모으세요. 가득 차면 레이저 충돌!",6);
            }
        }
        public void Tick()
        {
            if(finished||!game||game.boss!=owner||clashing&&!Application.isFocused)return;
            if(phase<4)
            {
                int next=AncientSniperRules.Phase(owner.HealthRatio);if(next>phase)ChangePhase(next);
            }
            if(phase==4){TickFinal();return;}
            TickObjects();if(game.boss!=owner)return;owner.BossContact();if(game.boss!=owner)return;
            for(int i=0;i<4;i++)if(actors[i].enemy&&(i<2||phase<3))TickActor(i);
            if(phase>=3)for(int i=0;i<2;i++)TickTurret(i);
            if(mounted>=0)
            {
                game.hero.CinematicMove(turrets[mounted].position+Vector2.up*1.7f);game.hero.CinematicPose(false,owner.transform.position.x<game.hero.transform.position.x?-1:1);
                if(game.GameplayInputReady&&Input.GetKeyDown(KeyCode.E))Dismount();
            }
        }
        void TickActor(int i)
        {
            var a=actors[i];var e=a.enemy;if(!e||!e.IsAlive){a.tell.enabled=false;return;}
            Vector2 target=game.hero.transform.position;float facing=target.x<e.transform.position.x?-1:1;
            e.BossVisual(a.state==2?5:a.state==1?(a.grenades>0?3:3):Time.time<a.flash?4:0,facing,a.state==1?Mathf.Clamp01((Time.time-a.began)/(a.until-a.began)):0,a.state==1,a.state==2);
            Vector2 origin=e.SniperMuzzle;
            if(a.state==2)
            {
                if(Time.time>=a.until){a.state=0;a.shots=0;a.magazine++;a.grenadeUsed=false;a.until=Time.time+.3f;}
                return;
            }
            if(a.state==1)
            {
                if(a.until-Time.time>.35f)a.aim=target;
                a.tell.enabled=true;
                if(a.grenades>0)
                {
                    a.tell.positionCount=12;Vector2 v=(a.aim-origin)/1.05f+Vector2.up*7.35f;
                    for(int j=0;j<12;j++){float t=j*1.05f/11;a.tell.SetPosition(j,origin+v*t+Vector2.down*7*t*t);}
                }
                else{a.tell.positionCount=2;a.tell.SetPosition(0,origin);a.tell.SetPosition(1,a.aim);}
                if(Time.time<a.until)return;a.tell.enabled=false;
                if(a.grenades>0){a.state=3;a.until=Time.time;}
                else
                {
                    Vector2 d=(a.aim-origin).normalized;if(d.sqrMagnitude<.1f)d=Vector2.right*facing;
                    if(game.shots.Count<48)game.Projectiles.Spawn(origin+d*.25f,d*25,18,false,0);
                    a.flash=Time.time+.18f;a.shots++;
                    a.state=a.shots>=AncientSniperRules.MagazineSize?2:0;a.until=Time.time+(a.state==2?AncientSniperRules.ReloadSeconds:.3f);game.Sound(0);game.Burst(origin+d*.2f,new Color(1,.85f,.4f),3);
                }
                return;
            }
            if(a.state==3)
            {
                if(Time.time<a.until)return;Vector2 p=a.aim+Vector2.right*(a.grenades-2)*1.7f;
                SpawnObject(origin,(p-origin)/1.05f+Vector2.up*7.35f,true,false);a.grenades--;a.until=Time.time+.22f;
                if(a.grenades==0){a.state=0;a.until=Time.time+.5f;}return;
            }
            if(Time.time<a.until)return;
            bool grenade=Vector2.Distance(origin,target)>7&&a.shots==2&&!a.grenadeUsed&&a.magazine%2==0;
            if(grenade)a.grenadeUsed=true;a.grenades=grenade?4:0;a.aim=target;a.state=1;a.began=Time.time;a.until=Time.time+(grenade?.8f:AncientSniperRules.AimSeconds);
        }
        void TickTurret(int i)
        {
            var t=turrets[i];if(t.broken)return;
            float rise=Mathf.Clamp01((Time.time-t.riseAt)/.8f);t.root.transform.position=t.position+Vector2.up*Mathf.Lerp(-1.5f,0,rise*rise*(3-2*rise));
            if(actors[i+2].enemy)actors[i+2].enemy.transform.position=(Vector2)t.root.transform.position+Vector2.up*.65f;
            if(rise<1)return;
            bool manned=actors[i+2].enemy&&actors[i+2].enemy.IsAlive;
            bool friendly=mounted==i;
            if(!manned&&!friendly){t.charging=false;t.tell.enabled=false;return;}
            Vector2 origin=t.position+Vector2.up*1.4f;
            if(!t.charging)
            {
                if(Time.time<t.until||friendly&&!Input.GetMouseButtonDown(0))return;
                t.charging=true;t.started=Time.time;t.until=Time.time+3.2f;t.aim=friendly?(Vector2)game.View.ScreenToWorldPoint(Input.mousePosition):(Vector2)game.hero.transform.position;
            }
            if(t.until-Time.time>.4f)t.aim=friendly?(Vector2)game.View.ScreenToWorldPoint(Input.mousePosition):(Vector2)game.hero.transform.position;
            Vector2 d=(t.aim-origin).normalized;t.barrel.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
            t.tell.enabled=true;t.tell.startColor=t.tell.endColor=friendly?AncientGame.Cyan:AncientGame.Orange;t.tell.SetPosition(0,origin);t.tell.SetPosition(1,t.aim);
            t.body.color=Color.Lerp(new Color(.18f,.28f,.36f),friendly?AncientGame.Cyan:AncientGame.Orange,Mathf.Clamp01((Time.time-t.started)/3.2f));
            if(Time.time<t.until)return;
            SpawnObject(origin+d*2.2f,d*13,false,friendly);game.Sound(4);game.Burst(origin+d*2.2f,AncientGame.Cyan,5);t.charging=false;t.tell.enabled=false;t.until=Time.time+1;
        }
        void SpawnObject(Vector2 position,Vector2 velocity,bool grenade,bool friendly)
        {
            for(int k=0;k<objects.Length;k++)
            {
                int at=(cursor+k)%objects.Length;var s=objects[at];if(s.live)continue;cursor=(at+1)%objects.Length;
                s.live=true;s.age=0;s.position=position;s.velocity=velocity;s.grenade=grenade;s.friendly=friendly;
                s.art.sprite=AncientSniperEquipmentArt.Sprite(grenade?2:3);s.art.transform.localScale=Vector3.one;
                s.art.transform.position=position;s.art.color=friendly?AncientGame.Cyan:Color.white;s.art.enabled=true;return;
            }
            droppedSpawns++;
        }
        void TickObjects()
        {
            foreach(var s in objects)
            {
                if(game.boss!=owner)return;if(!s.live)continue;s.age+=Time.deltaTime;if(s.age>6){RemoveObject(s);continue;}
                if(s.grenade)s.velocity+=Vector2.down*14*Time.deltaTime;
                Vector2 next=s.position+s.velocity*Time.deltaTime;var hit=Physics2D.Linecast(s.position,next,1<<8);
                if(hit)
                {
                    if(s.grenade)
                    {ground.Explode(hit.point);if(Vector2.Distance(game.hero.transform.position,hit.point)<2.4f)game.hero.Hurt(22);game.Impact(hit.point,AncientGame.Orange,true);game.Sound(4);}
                    else game.Burst(hit.point,AncientGame.Orange,8);
                    RemoveObject(s);if(game.boss!=owner)return;continue;
                }
                if(s.friendly)
                {
                    for(int i=game.enemies.Count-1;i>=0;i--)
                    {
                        var e=game.enemies[i];if(!e||!e.IsAlive)continue;
                        if(DistanceToSegment(e.AimPoint,s.position,next)<.9f){e.Hit(75,true);RemoveObject(s);break;}
                    }
                }
                else if(DistanceToSegment(game.hero.transform.position,s.position,next)<(s.grenade?.5f:.95f))
                {game.hero.Hurt(s.grenade?18:24);if(s.grenade){game.Impact(s.position,AncientGame.Orange,true);game.Sound(4);}RemoveObject(s);}
                if(game.boss!=owner)return;s.position=next;s.art.transform.position=next;s.art.transform.Rotate(0,0,(s.grenade?170:430)*Time.deltaTime);
            }
        }
        static float DistanceToSegment(Vector2 p,Vector2 start,Vector2 end)
        {Vector2 d=end-start;float t=d.sqrMagnitude>.001f?Mathf.Clamp01(Vector2.Dot(p-start,d)/d.sqrMagnitude):0;return Vector2.Distance(p,start+d*t);}
        void RemoveObject(ObjectShot s){s.live=false;s.art.enabled=false;}
        void ClearObjects(){foreach(var s in objects)if(s!=null)RemoveObject(s);foreach(var a in actors)if(a!=null)a.tell.enabled=false;}
        public bool TryBlade(Vector2 origin,Vector2 direction)
        {
            bool found=TryBreakObjects(origin,direction);
            if(phase==4&&!clashing)for(int i=0;i<2;i++)
            {
                var t=turrets[i];Vector2 p=t.position+Vector2.up,d=p-origin;
                if(!t.broken&&d.sqrMagnitude<3.1f*3.1f&&Vector2.Dot(d.normalized,direction)>-.15f&&!Physics2D.Linecast(origin,p,1<<8))HitTurret(i,35);
            }
            return found;
        }
        public bool TryBreakObjects(Vector2 origin,Vector2 direction)
        {
            bool found=false;
            foreach(var s in objects)
            {
                if(!s.live)continue;Vector2 d=s.position-origin;
                if(d.sqrMagnitude<3.1f*3.1f&&Vector2.Dot(d.normalized,direction)>-.15f&&!Physics2D.Linecast(origin,s.position,1<<8))
                {game.hero.Combat.EmitParticles(s.position,s.grenade?14:20);game.Burst(s.position,AncientGame.Cyan,8);RemoveObject(s);found=true;}
            }
            return found;
        }
        public void OnLaser(Vector2 start,Vector2 end,float bodyRadius=0)
        {
            Vector2 delta=end-start;
            if(phase==4&&!clashing)for(int i=0;i<2;i++)
            {
                var t=turrets[i];Vector2 point=t.position+Vector2.up,target=point-start;
                if(!t.broken&&AncientBeamRules.IntersectsCircle(delta.x,delta.y,target.x,target.y,bodyRadius,1.1f)&&!Physics2D.Linecast(start,point,1<<8))HitTurret(i,100);
            }
            foreach(var s in objects)if(s.live)
            {
                Vector2 target=s.position-start;
                if(!AncientBeamRules.IntersectsCircle(delta.x,delta.y,target.x,target.y,bodyRadius,.7f)||Physics2D.Linecast(start,s.position,1<<8))continue;
                game.hero.Combat.EmitParticles(s.position,18);RemoveObject(s);
            }
        }
        void HitTurret(int i,float damage)
        {
            var t=turrets[i];t.hp-=damage;game.Impact(t.position+Vector2.up,AncientGame.Cyan,true);
            if(t.hp>0)return;t.broken=true;t.root.SetActive(false);t.tell.enabled=false;
            game.hero.Combat.EmitParticles(t.position+Vector2.up,50);refillAt=Time.time+7;
        }
        public void PrepareSafeRecovery(){ground.RestoreAll();Dismount();}
        public bool TryInteract()
        {
            if(phase!=3||clashing||!game.GameplayInputReady)return false;
            if(mounted>=0){Dismount();return true;}
            for(int i=0;i<2;i++)
            {
                var t=turrets[i];if(t.broken||Vector2.Distance(game.hero.transform.position,t.position+Vector2.up)>2.6f)continue;
                if(actors[i+2].enemy){game.Notice("포탑을 조종하는 에너지 분신부터 제압하세요.",2);return true;}
                mounted=i;t.charging=false;t.until=Time.time+.25f;game.hero.HoldForEncounter(true);game.hero.CinematicMove(t.position+Vector2.up*1.7f);game.BlockGameplayInput();return true;
            }
            return false;
        }
        void Dismount()
        {
            if(mounted<0)return;var t=turrets[mounted];mounted=-1;t.charging=false;t.tell.enabled=false;
            game.hero.HoldForEncounter(false);game.hero.TravelTo(t.position+new Vector2(-2,1.3f));game.BlockGameplayInput();
        }
        void RestoreTurrets()
        {foreach(var t in turrets){t.broken=false;t.hp=65;t.charging=false;t.root.SetActive(true);t.root.transform.position=t.position;t.riseAt=Time.time-.8f;t.tell.enabled=false;t.body.color=new Color(.18f,.28f,.36f);}}
        void ShowBeams(bool shown){heroBeam.enabled=bossBeam.enabled=heroCore.enabled=bossCore.enabled=heroGlow.enabled=bossGlow.enabled=shown;}
        void TickFinal()
        {
            owner.BossVisual(winningAt>=0?5:clashing?4:3,game.hero.transform.position.x<owner.transform.position.x?-1:1,.9f,!clashing,false);
            if(!clashing)
            {
                if(Time.time>=impactAt){impactAt=Time.time+.22f;game.Impact(owner.AimPoint,AncientGame.Cyan,true);}
                if(turrets[0].broken&&turrets[1].broken&&game.hero.CaptureEnergy>=99.99f){BeginClash();return;}
                if(turrets[0].broken&&turrets[1].broken&&Time.time>=refillAt)
                {RestoreTurrets();refillAt=Time.time+7;game.Notice("흩어진 입자를 놓쳤습니다. 포탑 코어를 다시 부숴 수집하세요.",3);}return;
            }
            clash.Poll(Time.deltaTime,Input.GetKey(KeyCode.Space),Input.GetMouseButton(0));
            game.hero.CinematicMove(heroAnchor);game.hero.CinematicLaserPose(1);game.hero.AimWeaponAt(owner.AimPoint);
            Vector2 begin=heroAnchor+new Vector2(.3f,.55f),enemy=owner.SniperMuzzle;
            Vector2 meet=Vector2.Lerp(begin,enemy,Mathf.Lerp(.12f,.88f,clash.Progress));
            heroGlow.SetPosition(0,begin);heroGlow.SetPosition(1,meet);heroBeam.SetPosition(0,begin);heroBeam.SetPosition(1,meet);heroCore.SetPosition(0,begin);heroCore.SetPosition(1,meet);
            bossGlow.SetPosition(0,enemy);bossGlow.SetPosition(1,meet);bossBeam.SetPosition(0,enemy);bossBeam.SetPosition(1,meet);bossCore.SetPosition(0,enemy);bossCore.SetPosition(1,meet);
            if(Time.time>=impactAt){impactAt=Time.time+.1f;game.Impact(meet,AncientGame.Cyan,true);game.Shake(.035f);}
            if(clash.Result==AncientSniperClashResult.Won)
            {
                if(winningAt<0){winningAt=Time.time;game.Sound(4);game.Burst(owner.AimPoint,AncientGame.Cyan,32);game.Shake(.2f);}
                heroBeam.SetPosition(1,enemy);heroCore.SetPosition(1,enemy);heroGlow.SetPosition(1,enemy);bossBeam.enabled=bossCore.enabled=bossGlow.enabled=false;
                if(Time.time-winningAt<.65f)return;
                finished=true;clashing=false;ShowBeams(false);game.hero.HoldForEncounter(false);game.BlockGameplayInput();
                owner.CompleteSniperDuel();
            }
            else if(clash.Result==AncientSniperClashResult.Lost)
            {
                clashing=false;ShowBeams(false);game.hero.HoldForEncounter(false);game.BlockGameplayInput();RestoreTurrets();refillAt=Time.time+7;
                game.Notice("힘겨루기 실패 / 포탑을 다시 부수고 입자 재수집\n다음 충돌에서 Space와 좌클릭을 함께 연타하세요.",5);
            }
        }
        public void EnergyChanged()
        {if(phase==4&&!finished&&!clashing&&turrets[0].broken&&turrets[1].broken&&game.hero.CaptureEnergy>=99.99f)BeginClash();}
        void BeginClash()
        {
            clashing=true;winningAt=-1;clash.Reset();game.hero.SpendCapturedEnergy();game.hero.Combat.ResetCombat();
            game.hero.HoldForEncounter(true);heroAnchor=new Vector2(left+1,1.3f);game.hero.CinematicMove(heroAnchor);ShowBeams(true);game.Sound(8);game.BlockGameplayInput();
        }
        void OnGUI()
        {
            if(!game||game.Paused||game.MainMenu||finished)return;
            if(label==null){label=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=18,alignment=TextAnchor.MiddleCenter,wordWrap=true};title=new GUIStyle(label){fontSize=28,fontStyle=FontStyle.Bold};}
            if(clashing)
            {
                GUI.Label(new Rect(Screen.width/2-300,Screen.height*.18f,600,70),clash.Result==AncientSniperClashResult.Won?"충전 돌파!":"Space + 좌클릭 연타!\n두 레이저의 충돌을 밀어내세요",title);
                AncientHUD.Panel(new Rect(Screen.width/2-210,Screen.height*.18f+85,420,16),new Color(.3f,.08f,.13f));
                AncientHUD.Panel(new Rect(Screen.width/2-210,Screen.height*.18f+85,420*clash.Progress,16),AncientGame.Cyan);
            }
            else if(phase==4)GUI.Label(new Rect(Screen.width/2-300,95,600,60),"파이널 / 보스 충전 중 무적\n포탑 2개 파괴 → 우클릭 입자 수집 → 자동 레이저 충돌",label);
            else
            {
                GUI.Label(new Rect(Screen.width/2-300,95,600,55),phaseHints[Mathf.Clamp(phase,1,3)],label);
                if(phase==3)for(int i=0;i<2;i++)
                {
                    var t=turrets[i];Vector3 p=game.View.WorldToScreenPoint(t.position+Vector2.up*3);
                    string text=mounted==i?"좌클릭: 3초 압축 발사 / E: 하차":actors[i+2].enemy?"조종 분신을 제압하세요":"E: 포탑 조종";
                    GUI.Label(new Rect(p.x-150,Screen.height-p.y-25,300,60),text,label);
                }
            }
        }
        void OnDestroy()
        {
            if(game){if(game.Sniper==this)game.Sniper=null;if(game.hero)game.hero.HoldForEncounter(false);}
            RemoveClones();if(ground)Destroy(ground.gameObject);if(cloneMaterial)Destroy(cloneMaterial);
            foreach(var a in actors)if(a!=null&&a.tell)Destroy(a.tell.gameObject);
            foreach(var t in turrets)if(t!=null){if(t.root)Destroy(t.root);if(t.tell)Destroy(t.tell.gameObject);}
            foreach(var s in objects)if(s!=null&&s.art)Destroy(s.art.gameObject);
            if(heroBeamProfile!=null)heroBeamProfile.Dispose();if(bossBeamProfile!=null)bossBeamProfile.Dispose();
            if(heroGlow)Destroy(heroGlow.gameObject);if(bossGlow)Destroy(bossGlow.gameObject);
            if(heroBeam)Destroy(heroBeam.gameObject);if(bossBeam)Destroy(bossBeam.gameObject);if(heroCore)Destroy(heroCore.gameObject);if(bossCore)Destroy(bossCore.gameObject);
        }
    }
}
