using UnityEngine;
namespace Plugin.AncientCivilization
{
    // Joker is a mask/callsign here. His personal identity and past remain unrevealed.
    public sealed partial class AncientSniperEncounter : MonoBehaviour
    {
        sealed class Actor
        {
            public AncientEnemy enemy;public LineRenderer tell;public int state,shots,magazine,grenades;
            public int lastGrenadePhase;
            public float lastGrenadeRelease = float.NegativeInfinity;
            public bool grenadeUsed,duelUsed,lateLock,aimLocked,coveringPlate,ejected; public float ejectAt, coverY; public Vector2 ejectFrom,ejectTo;public float until,began,flash;public Vector2 aim;
        }
        sealed class Turret
        {
            public GameObject root;public SpriteRenderer body,barrel;public LineRenderer tell;
            public Vector2 position,aim;public bool broken,charging;public float until,started,riseAt,hp=65;public int lastLaserPulse,plateIndex;
        }
        sealed class ObjectShot
        {public SpriteRenderer art;public Vector2 position,velocity;public bool live,friendly,grenade;public float age;}
        AncientGame game;AncientEnemy owner;AncientSniperGround ground;
        readonly Actor[] actors=new Actor[4];readonly Turret[] turrets=new Turret[2];
        readonly ObjectShot[] objects=new ObjectShot[24];int cursor,phase=1,mounted=-1,droppedSpawns;
        float nextRewind,coverAt,coverHeight; int airBladeFrame; bool plateQueued;
        float left,right,refillAt,impactAt,winningAt=-1;bool clashing,finished;Vector2 heroAnchor;
        LineRenderer heroBeam,bossBeam,heroCore,bossCore,heroGlow,bossGlow;AncientBeamVisuals heroBeamProfile,bossBeamProfile;AncientSniperClash clash;
        readonly LineRenderer[] curtain=new LineRenderer[4];
        Material cloneMaterial;GUIStyle label,title;
        public AncientEnemy Owner {get{return owner;}}
        public int Phase {get{return phase;}}
        public int TurretsBroken {get{return (turrets[0]!=null&&turrets[0].broken?1:0)+(turrets[1]!=null&&turrets[1].broken?1:0);}}
        public int MountedIndex {get{return mounted;}}
        public bool Clashing {get{return clashing;}}
        public int LiveObjects {get{int count=0;foreach(var item in objects)if(item!=null&&item.live)count++;return count;}}
        public int ObjectCapacity {get{return objects.Length;}}
        public int DroppedSpawns {get{return droppedSpawns;}}
        static readonly string[] phaseHints =
        {
            "", "1 · 다섯 발의 박자", "2 · 두 개의 사선", "3 · 포탑 탈취",
            "4 · 옥상 릴레이", "5 · 조커 풀하우스", "6 · 레이저 충돌"
        };
        public int ClubDuelFrame => phase==1 && actors[0]!=null && actors[0].state==5 ? 0 :
            phase==1 && actors[0]!=null && actors[0].state==6 ? attacks.ClubFrame : -1;
        public bool InRearLane(int slot)
        {
            if(slot<=0) return inBackground && (phase!=5 || relay!=RelayBeat.Return);
            if(slot!=1) return false;
            if(phase==2) return !inBackground;
            return phase==5 && relay!=RelayBeat.Feint;
        }
        public bool InputLocked => clashing || mounted >= 0;
        public bool Telegraphing => (attacks != null && attacks.Telegraphing) ||
            (phase < 4 && actors[0] != null && actors[0].state == 1);
        public bool Recovering => (phase < 4 && actors[0] != null && actors[0].state == 2) ||
            (phase >= 4 && phase < 6 && relay == RelayBeat.Opening);
        public float WindupProgress => attacks != null && attacks.Telegraphing ? attacks.WindupProgress :
            Telegraphing ? Mathf.InverseLerp(actors[0].began, actors[0].until, Time.time) : 0;
        public float RecoveryRemaining => !Recovering ? 0 : phase < 4 ?
            Mathf.Clamp01((actors[0].until-Time.time)/AncientSniperRules.ReloadSeconds) :
            1-BeatProgress;
        public void Initialize(AncientEnemy enemy,AncientGame world)
        {
            owner=enemy;owner.BindSniperEncounter(this);game=world;game.Sniper=this;left=AncientCityLayout.ArenaLeft(4)+1;right=AncientCityLayout.ArenaRight(4)-1;
            var floor=new GameObject("Recoverable sniper arena floor");ground=floor.AddComponent<AncientSniperGround>();ground.Initialize(game,left-1,right+1);
            arena = new AncientJokerArena(game, game.transform, left, right);
            attacks = new AncientJokerAttacks(game, game.transform, left, right);
            attacks.SetArena(arena);
            attacks.TellCue += JokerTell;
            attacks.StrikeCue += JokerStrike;
            attacks.ContactCue += JokerContact;
            attacks.SweepFromRight = () => SideOf((phase == 5 ? ChainShooter : owner).transform.position.x) == 1;
            arena.SetPhase(1);
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
            heroBeam=game.Line("Automatic player particle laser",new Color(1,.9f,.6f),.80f,23);
            bossBeam=game.Line("Charged sniper laser",new Color(1,.25f,.4f),.90f,23);
            heroCore=game.Line("Player laser core",Color.white,.26f,24);bossCore=game.Line("Sniper laser core",new Color(1,.95f,.95f),.30f,24);
            heroGlow=game.Line("Player clash laser soft light",new Color(1,.55f,.19f,.5f),1.7f,22);
            bossGlow=game.Line("Sniper clash laser soft light",new Color(1,.15f,.3f,.5f),1.8f,22);
            heroBeamProfile=new AncientBeamVisuals(heroGlow);bossBeamProfile=new AncientBeamVisuals(bossGlow);
            heroBeam.positionCount=bossBeam.positionCount=heroCore.positionCount=bossCore.positionCount=heroGlow.positionCount=bossGlow.positionCount=2;ShowBeams(false);
            game.Notice("조커 / 다섯 번째 총성 뒤에 장전합니다. 도탄은 빛나는 거울에서 이어지는 선을 보세요.",4);
        }
        void JokerTell(Vector2 point, Vector2 direction, float scale)
        {
            AncientBossCue.Play(game, AncientBossCue.Style.Joker, AncientBossCue.Beat.Tell, point, direction, scale);
        }
        void JokerStrike(Vector2 point, Vector2 direction, float scale)
        {
            AncientBossCue.Play(game, AncientBossCue.Style.Joker, AncientBossCue.Beat.Strike, point, direction, scale);
        }
        void JokerContact(Vector2 point, Vector2 direction, float scale)
        {
            AncientBossCue.Play(game, AncientBossCue.Style.Joker, AncientBossCue.Beat.Contact, point, direction, scale);
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
            e.transform.position=position;a.enemy=e;ResetActor(a, .7f);game.enemies.Add(e);
        }
        public Material CloneMaterial {get{return cloneMaterial;}}
        public void CloneDown(int slot,Vector2 point)
        {if(slot<1||slot>3)return;actors[slot].enemy=null;actors[slot].tell.enabled=false;game.hero.Combat.EmitParticles(point,18);game.Burst(point,AncientGame.Cyan,12);}
        void RemoveClones()
        {
            for(int i=1;i<actors.Length;i++)
            {
                var a=actors[i];
                if(a==null) continue;
                if(a.enemy)
                {
                    if(game) game.enemies.Remove(a.enemy);
                    Destroy(a.enemy.gameObject);
                }
                a.enemy=null;
                if(a.tell) a.tell.enabled=false;
            }
        }
        public float FilterDamage(float damage,float hp,float maxHP)
        {
            if (phase == 6 || finished || inBackground) return 0;
            if (phase == 4 && relay == RelayBeat.Return)
            {
                if (!airBladeHit || airBladeFrame != Time.frameCount) return 0;
                airBladeFrame=-1;
            }
            return Mathf.Min(damage, Mathf.Max(0, hp - maxHP * AncientSniperRules.HealthFloor(phase)));
        }
        void ChangePhase(int next)
        {
            if (next == 6)
            {
                AncientBossCue.Clear(game, AncientBossCue.Style.Joker);
                if (ordinaryRigActive) ClearOrdinaryPresentation();
            }
            if (mounted >= 0) Dismount();
            attacks.Clear();
            CloseCurtain();
            ClearObjects();
            game.ClearBattleProjectiles();
            RemoveClones();
            foreach (var t in turrets) { t.charging=false; t.tell.enabled=false; }
            ResetActor(actors[0], .85f);
            inBackground=false; airCatch=false; airBladeHit=false; swapping=false;
            phase=next;
            arena.SetPhase(phase);
            owner.FaceToward(game.hero.transform.position.x);
            game.Sound(8);
            game.Impact(owner.AimPoint,AncientGame.Cyan,true);
            switch (phase)
            {
                case 2:
                    realSide=SideOf(owner.transform.position.x);
                    owner.transform.position=arena.RailPoint(realSide);
                    SpawnClone(1,arena.LandingPoint(1-realSide));
                    actors[1].until=Time.time+.25f;
                    inBackground=true; nextSwap=Time.time+5.5f;
                    game.Notice("다이아 거울 교대 / 모자 그림자가 진짜 귀환 지점입니다.", 4);
                    break;
                case 3:
                    ground.RestoreAll();
                    owner.transform.position=new Vector2(right-2,0);
                    RestoreTurrets();
                    for (int i=0;i<2;i++)
                    {
                        var t=turrets[i]; t.until=Time.time+1.2f+i*2.35f;
                        t.riseAt=Time.time; t.plateIndex=i;
                    }
                    SpawnClone(1,new Vector2(left+2,0));
                    SpawnClone(2,turrets[0].position+Vector2.up*.65f);
                    SpawnClone(3,turrets[1].position+Vector2.up*.65f);
                    nextRewind=Time.time+4.5f;
                    game.Notice("스페이드 되감는 바닥 / 조종 분신 제압 후 E 탑승 · 좌클릭 압축판 · E 하차", 4);
                    break;
                case 4:
                case 5:
                    ground.RestoreAll(); relayCount=0; previousChain=0;
                    realSide=SideOf(owner.transform.position.x);
                    if(phase==5) SpawnClone(1,arena.RailPoint(1-realSide));
                    StartRailRun();
                    game.Notice(phase==4 ? "하트 옥상 릴레이 / 열린 셔터를 선점하고 공중 고리를 노리세요." :
                        "조커 풀하우스 / 마지막 전조까지 보고 모자 그림자 쪽에서 반격하세요.", 4);
                    break;
                case 6:
                    ground.RestoreAll(); game.hero.Combat.ResetCombat();
                    game.hero.SpendCapturedEnergy();
                    owner.transform.position=new Vector2(right-1,0); RestoreTurrets(); refillAt=Time.time+7;
                    game.Notice("파이널 / 두 포탑을 부숴 입자를 완충하세요. 레이저 충돌에서 Space와 좌클릭을 모두 연타!", 5);
                    break;
            }
        }
        public void Tick()
        {
            if (finished || !game || game.boss!=owner || clashing && !Application.isFocused) return;
            int next=AncientSniperRules.Phase(owner.HealthRatio);
            if (phase<6 && next>phase) ChangePhase(phase+1);
            if (phase==6) { TickFinal(); return; }
            arena.Tick();
            attacks.Tick(Time.deltaTime, game.hero.transform.position);
            if (!StillFighting) return;
            TickObjects();
            if (!StillFighting) return;
            if (!inBackground && !Recovering && (phase!=4 || relay!=RelayBeat.Return)) owner.BossContact();
            if (!StillFighting) return;
            if (phase>=4) { TickRelay(); return; }
            if (phase==2 && TickSwap()) return;
            TickActor(0);
            if (phase>=2 && StillFighting && actors[1].enemy) TickActor(1);
            if (phase!=3 || !StillFighting) return;
            if(Time.time>=nextRewind) { arena.BeginRewind(); nextRewind=Time.time+10; }
            for(int i=0;i<2;i++) TickTurret(i);
            if(mounted>=0)
            {
                game.hero.CinematicMove(turrets[mounted].position+Vector2.up*1.7f);
                game.hero.CinematicPose(false,owner.transform.position.x<game.hero.transform.position.x?-1:1);
                if(game.GameplayInputReady&&Input.GetKeyDown(KeyCode.E)) Dismount();
            }
        }
        void TickActor(int i)
        {
            var a=actors[i];var e=a.enemy;if(!e||!e.IsAlive){a.tell.enabled=false;return;}
            Vector2 target=game.hero.transform.position;float facing=target.x<e.transform.position.x?-1:1;
            bool aiming=a.state==1 || a.state==4 && attacks.Telegraphing;
            float windup=a.state==4?attacks.WindupProgress:aiming?Mathf.InverseLerp(a.began,a.until,Time.time):0;
            int pose=a.state==2?5:aiming?3:a.state==5?1:Time.time<a.flash?4:0;
            e.BossVisual(pose,facing,windup,aiming,a.state==2);
            Vector2 origin=e.SniperMuzzle;
            if (a.state == 4)
            {
                // 반사 사격도 같은 탄창에 들어간다.
                if (!attacks.Busy) ConsumeRound(a);
                return;
            }
            if (a.state == 5)
            {
                float t=Mathf.InverseLerp(a.began,a.until,Time.time);
                owner.transform.position=Vector2.Lerp(travelStart,travelEnd,Mathf.SmoothStep(0,1,t));
                DrawCurtain(travelEnd,1-t*.65f,Mathf.Min(1,t*4));
                if(t>=1)
                {
                    e.FaceToward(target.x);
                    attacks.Club(e.AimPoint,target.x<e.transform.position.x?-1:1);
                    a.state=6;
                }
                return;
            }
            if (a.state==6)
            {
                if(!attacks.Busy){CloseCurtain();a.state=0;a.until=Time.time+.85f;}
                else DrawCurtain(travelEnd,.35f*(1-attacks.Progress),Mathf.Clamp01((1-attacks.Progress)*8));
                return;
            }
            if(a.state==2)
            {
                if(Time.time>=a.until){a.state=0;a.shots=0;a.magazine++;a.grenadeUsed=a.duelUsed=false;a.until=Time.time+.3f;}
                return;
            }
            if(a.state==1)
            {
                float lockLead=a.grenades>0?.35f:a.lateLock?.32f:1.05f;
                if(a.until-Time.time>lockLead) a.aim=ShotTarget(i,a,target);
                a.tell.enabled=true;
                if (!a.aimLocked && Time.time >= a.until - lockLead)
                {
                    a.aimLocked = true;
                    Vector2 aim = a.grenades > 0 ? GrenadeVelocity(a,origin) : a.aim-origin;
                    JokerTell(origin, aim.normalized, i == 0 ? .50f : .34f);
                }
                if(a.grenades>0)
                {
                    DrawGrenadeTell(a, origin, Time.time >= a.until - lockLead);
                }
                else
                {
                    Vector2 d=(a.aim-origin).normalized;
                    a.tell.positionCount=2; a.tell.SetPosition(0,origin); a.tell.SetPosition(1,origin+d*38);
                    bool locked=Time.time>=a.until-lockLead;
                    Color c=i==0?new Color(1,.42f,.35f):AncientGame.Cyan;
                    c.a=locked?1:.42f; a.tell.startColor=a.tell.endColor=c;
                    a.tell.startWidth=a.tell.endWidth=locked?.057f:.028f;
                }
                if(Time.time<a.until)return; a.tell.enabled=false;
                if(a.grenades>0){a.state=3;a.until=Time.time;}
                else
                {
                    Vector2 d=(a.aim-origin).normalized;if(d.sqrMagnitude<.1f)d=Vector2.right*facing;
                    Vector2 muzzle = origin + d*.25f;
                    if (game.shots.Count < 48 && game.Projectiles.Spawn(muzzle,d*25,18,false,0))
                        JokerStrike(muzzle, d, i == 0 ? .67f : .46f);
                    a.flash=Time.time+.18f;ConsumeRound(a);game.Sound(0);game.Burst(origin+d*.2f,new Color(1,.85f,.4f),3);
                }
                return;
            }
            if(a.state==3)
            {
                if(Time.time<a.until)return;
                Vector2 velocity = GrenadeVelocity(a, origin);
                if (SpawnObject(origin, velocity, true, false))
                {
                    JokerStrike(origin, velocity.normalized, .41f);
                    a.lastGrenadePhase=phase;
                    a.lastGrenadeRelease=Time.time;
                }
                a.grenades--;a.until=Time.time+.22f;
                if(a.grenades==0){a.state=0;a.until=Time.time+.5f;}return;
            }
            if(Time.time<a.until || attacks.Busy && i==0) return;
            if(phase==3 && i==0 && (HasGunner(0)||HasGunner(1)))
            {
                if(!plateQueued || Time.time<coverAt) return;
                plateQueued=false; a.coveringPlate=true; a.coverY=coverHeight;
            }
            if(phase==1 && !a.duelUsed && a.shots==4 && a.magazine%2==0)
            {
                a.duelUsed=true; travelStart=e.transform.position;
                float approach=target.x<e.transform.position.x?1:-1;
                travelEnd=new Vector2(Mathf.Clamp(target.x+approach*1.9f,left+1,right-1),0);
                a.state=5;a.began=Time.time;a.until=Time.time+.55f;
                game.Burst(e.AimPoint,new Color(.22f,.38f,.47f),12);return;
            }
            bool grenade=Vector2.Distance(origin,target)>7 && a.shots==2 && !a.grenadeUsed &&
                a.magazine%2==0 && !a.coveringPlate && (i!=0 || phase!=2 || !inBackground);
            if(!grenade && i==0 && ((phase==1 && (a.shots+a.magazine)%3==2) || (phase==2 && inBackground)))
            {
                attacks.Mirror(origin,target,1);
                a.state=4; return;
            }
            if(grenade) a.grenadeUsed=true;
            a.grenades=grenade?4:0;
            a.lateLock=(a.shots+a.magazine)%3==1;
            a.aim=ShotTarget(i,a,target);a.state=1;a.began=Time.time;a.aimLocked=false;
            a.until=Time.time+(grenade?.8f:AncientSniperRules.AimSeconds);
        }
        static Vector2 GrenadeVelocity(Actor actor, Vector2 origin)
        {
            Vector2 target = actor.aim + Vector2.right * (actor.grenades - 2) * 1.7f;
            return (target - origin) / 1.05f + Vector2.up * 7.35f;
        }

        void DrawGrenadeTell(Actor actor, Vector2 origin, bool locked)
        {
            // 남은 투척 수에 따라 첫 탄의 가로 오프셋이 달라진다. 실제 발사 함수와 같은 초속을 읽는다.
            const int samples = 12;
            const float previewTime = 1.05f;
            LineRenderer line = actor.tell;
            line.positionCount = samples;
            line.startColor = new Color(1f, .69f, .27f, locked ? .94f : .66f);
            line.endColor = new Color(1f, .84f, .52f, locked ? .87f : .34f);
            line.startWidth = locked ? .057f : .036f;
            line.endWidth = locked ? .040f : .021f;
            line.SetPosition(0, origin);
            Vector2 velocity = GrenadeVelocity(actor, origin);
            Vector2 previous = origin;
            for (int j = 1; j < samples; j++)
            {
                float t = j * previewTime / (samples - 1);
                // 실제 투척물은 매 프레임 중력을 먼저 더한 뒤 이동한다. 현재 프레임 간격으로 예측한다.
                Vector2 next = origin + velocity * t + Vector2.down * (7f * t * (t + Time.deltaTime));
                var floor = Physics2D.Linecast(previous, next, 1 << 8);
                if (floor)
                {
                    line.SetPosition(j, floor.point);
                    line.positionCount = j + 1;
                    break;
                }
                line.SetPosition(j, next);
                previous = next;
            }
        }

        // 장막은 착지점 뒤에 펼쳐진다. 플레이어와 두 번의 칼 궤적은 앞에 그대로 보인다.
        void DrawCurtain(Vector2 foot,float closed,float opacity)
        {
            for(int i=0;i<curtain.Length;i++)
            {
                var fold=curtain[i];
                if(!fold)
                {
                    fold=game.Line("Joker duel curtain fold "+i,Color.white,.6f,6);
                    fold.positionCount=3;
                    curtain[i]=fold;
                }
                float side=i<2?-1:1;
                float overlap=i%2*.38f;
                float outer=1.45f+overlap;
                float hem=Mathf.Lerp(1.75f,.18f,closed)+overlap;
                float ripple=.12f*Mathf.Sin(Time.time*7+i);
                fold.startColor=new Color(.16f,.11f,.29f,.76f*opacity);
                fold.endColor=new Color(.52f,.29f,.70f,.54f*opacity);
                fold.startWidth=.7f;
                fold.endWidth=.46f;
                fold.SetPosition(0,foot+new Vector2(side*outer,3.4f));
                fold.SetPosition(1,foot+new Vector2(side*(hem+.2f)+ripple,1.95f));
                fold.SetPosition(2,foot+new Vector2(side*hem+ripple,.22f+overlap));
                fold.enabled=true;
            }
        }
        void CloseCurtain()
        {
            foreach(var fold in curtain) if(fold) fold.enabled=false;
        }
        Vector2 ShotTarget(int i,Actor a,Vector2 target)
        {
            if(a.coveringPlate) return new Vector2(target.x,a.coverY);
            if(phase==2 && i==1 && inBackground && a.grenades==0)
                return new Vector2(target.x,.9f);
            return target;
        }
        void ConsumeRound(Actor a)
        {
            a.coveringPlate=false; a.shots++;
            a.state=a.shots>=AncientSniperRules.MagazineSize?2:0;
            a.until=Time.time+(a.state==2?AncientSniperRules.ReloadSeconds:.36f);
        }
        void TickTurret(int i)
        {
            var t=turrets[i];if(t.broken)return;
            float rise=Mathf.Clamp01((Time.time-t.riseAt)/.8f);t.root.transform.position=t.position+Vector2.up*Mathf.Lerp(-1.5f,0,rise*rise*(3-2*rise));
            var operatorActor=actors[i+2];
            if(operatorActor.enemy && operatorActor.ejected)
            {
                float p=Mathf.InverseLerp(operatorActor.ejectAt,operatorActor.ejectAt+.52f,Time.time);
                Vector2 path=Vector2.Lerp(operatorActor.ejectFrom,operatorActor.ejectTo,p);
                path.y+=Mathf.Sin(p*Mathf.PI)*1.65f;
                operatorActor.enemy.transform.position=path;
                if(p>=1)operatorActor.enemy.Hit(1000,true);
            }
            else if(operatorActor.enemy)
                operatorActor.enemy.transform.position=(Vector2)t.root.transform.position+Vector2.up*.65f;
            if(rise<1)return;
            bool manned=HasGunner(i);
            bool friendly=mounted==i;
            if(!manned&&!friendly){t.charging=false;t.tell.enabled=false;return;}
            Vector2 origin=t.position+Vector2.up*1.4f;
            if(!t.charging)
            {
                if(Time.time<t.until||friendly&&!Input.GetMouseButtonDown(0))return;
                t.charging=true;t.started=Time.time;t.until=Time.time+3.2f;
                t.aim=friendly?(Vector2)game.View.ScreenToWorldPoint(Input.mousePosition):
                    new Vector2(i==0?right:left,t.plateIndex%2==0?1.05f:3.7f);
                if (!friendly)
                {
                    Vector2 direction = (t.aim-origin).normalized;
                    JokerTell(origin + direction*2.2f, direction, .53f);
                }
            }
            if(friendly && t.until-Time.time>.4f)t.aim=game.View.ScreenToWorldPoint(Input.mousePosition);
            Vector2 d=(t.aim-origin).normalized;t.barrel.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
            t.tell.enabled=true;t.tell.startColor=t.tell.endColor=friendly?AncientGame.Cyan:AncientGame.Orange;t.tell.SetPosition(0,origin);t.tell.SetPosition(1,t.aim);
            t.body.color=Color.Lerp(new Color(.18f,.28f,.36f),friendly?AncientGame.Cyan:AncientGame.Orange,Mathf.Clamp01((Time.time-t.started)/3.2f));
            if(Time.time<t.until)return;
            Vector2 barrelEnd = origin+d*2.2f;
            if (SpawnObject(barrelEnd,d*13,false,friendly) && !friendly)
                JokerStrike(barrelEnd, d, .74f);
            game.Sound(4);game.Burst(barrelEnd,AncientGame.Cyan,5);
            t.charging=false;t.tell.enabled=false;t.until=Time.time+1.5f;
            if(!friendly)
            {
                t.plateIndex++;
                // 압축판을 넘은 직후의 통로를 본체가 별도의 조준선으로 겨눈다.
                if (!plateQueued)
                {
                    plateQueued=true;
                    coverHeight=t.plateIndex%2==1?3.5f:1.05f;
                    coverAt=Time.time+Mathf.Clamp(Mathf.Abs(game.hero.transform.position.x-origin.x)/13f,.1f,1.85f);
                }
            }
        }
        bool SpawnObject(Vector2 position,Vector2 velocity,bool grenade,bool friendly)
        {
            for(int k=0;k<objects.Length;k++)
            {
                int at=(cursor+k)%objects.Length;var s=objects[at];if(s.live)continue;cursor=(at+1)%objects.Length;
                s.live=true;s.age=0;s.position=position;s.velocity=velocity;s.grenade=grenade;s.friendly=friendly;
                s.art.sprite=AncientSniperEquipmentArt.Sprite(grenade?2:3);s.art.transform.localScale=Vector3.one;
                s.art.transform.position=position;s.art.color=friendly?AncientGame.Cyan:Color.white;s.art.enabled=true;return true;
            }
            droppedSpawns++;
            return false;
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
                    {
                        JokerContact(hit.point, hit.normal, .88f);
                        ground.Explode(hit.point);
                        if(Vector2.Distance(game.hero.transform.position,hit.point)<2.4f)game.hero.Hurt(22);
                        game.Impact(hit.point,AncientGame.Orange,true);game.Sound(4);
                    }
                    else game.Burst(hit.point,AncientGame.Orange,8);
                    RemoveObject(s);if(game.boss!=owner)return;continue;
                }
                if(s.friendly)
                {
                    for(int i=game.enemies.Count-1;i>=0;i--)
                    {
                        var e=game.enemies[i];if(!e||!e.IsAlive)continue;
                        if(DistanceToSegment(e.AimPoint,s.position,next)<.9f)
                        {
                            int seat=e==actors[2].enemy?0:e==actors[3].enemy?1:-1;
                            if(phase==3 && seat>=0 && HasGunner(seat))
                            {
                                var gunner=actors[seat+2];
                                gunner.ejected=true; gunner.ejectAt=Time.time;
                                gunner.ejectFrom=e.transform.position;
                                gunner.ejectTo=new Vector2(Mathf.Clamp(gunner.ejectFrom.x+Mathf.Sign(s.velocity.x)*2.3f,left,right),0);
                                game.Impact(e.AimPoint,AncientGame.Cyan,true);
                                RemoveObject(s);break;
                            }
                            e.Hit(75,true);RemoveObject(s);break;
                        }
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
        internal bool UnsafeBladeGround(Vector2 point)
        {
            if (!owner || !game || game.boss != owner) return false;
            foreach (var shot in objects)
                if (shot != null && shot.live && shot.grenade && (shot.position-point).sqrMagnitude < 4f) return true;
            return false;
        }

        internal int TurretAlongPlug(Vector2 start,Vector2 end,float tipRadius,out Vector2 contact)
        {
            contact=end;
            if (!owner || !game || game.boss!=owner || phase!=6 || clashing) return -1;
            int nearest=-1;
            float best=2f;
            Vector2 travel=end-start;
            for(int i=0;i<turrets.Length;i++)
            {
                var t=turrets[i];
                if(t==null || t.broken)continue;
                Vector2 point=t.position+Vector2.up,offset=point-start;
                if(Plugin.PlayerCore.ElectricPlugRules.SegmentHit(travel.x,travel.y,offset.x,offset.y,tipRadius+.72f,out float fraction) &&
                    fraction<best && !Physics2D.Linecast(start,point,1<<8))
                {nearest=i;best=fraction;contact=start+travel*fraction;}
            }
            return nearest;
        }

        internal void PlugTurret(int slot,float damage)
        {
            if(phase==6 && !clashing && (uint)slot<turrets.Length && !turrets[slot].broken)HitTurret(slot,damage);
        }

        internal void BladeBurst(Vector2 origin, float radius, float damage)
        {
            if (!owner || !game || game.boss != owner || clashing) return;
            float reach = radius * radius;
            foreach (var shot in objects)
            {
                if (shot == null || !shot.live || (shot.position-origin).sqrMagnitude > reach ||
                    Physics2D.Linecast(origin+Vector2.up*.08f,shot.position,1<<8)) continue;
                game.hero.Combat.EmitParticles(shot.position,shot.grenade?14:20);
                RemoveObject(shot);
            }
            if (phase != 6) return;
            for (int i=0;i<turrets.Length;i++)
            {
                var t=turrets[i];if(t==null||t.broken)continue;
                Vector2 point=t.position+Vector2.up;
                if ((point-origin).sqrMagnitude<=reach && !Physics2D.Linecast(origin+Vector2.up*.08f,point,1<<8))
                    HitTurret(i,damage);
            }
        }
        public bool TryBlade(Vector2 origin,Vector2 direction)
        {
            bool found=TryBreakObjects(origin,direction);
            if (phase==4 && relay==RelayBeat.Return && airCatch && !airBladeHit)
            {
                Vector2 delta=owner.AimPoint-origin;
                if(delta.sqrMagnitude<9 && Vector2.Dot(delta.normalized,direction)>-.15f &&
                    !Physics2D.Linecast(origin,owner.AimPoint,1<<8))
                {
                    airBladeHit=true;
                    // 다음 프레임에는 이 공중 베기 기회를 닫는다. 실제 피해는 통상 블레이드 판정으로 처리한다.
                    airBladeFrame=Time.frameCount;
                    found=true;
                }
            }
            if(phase==6&&!clashing)for(int i=0;i<2;i++)
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
        public void OnLaser(Vector2 start,Vector2 end,float bodyRadius=0,int pulseId=0)
        {
            Vector2 delta=end-start;
            if(phase==6&&!clashing)for(int i=0;i<2;i++)
            {
                var t=turrets[i];Vector2 point=t.position+Vector2.up,target=point-start;
                if(!t.broken&&(pulseId==0||t.lastLaserPulse!=pulseId)&&AncientBeamRules.IntersectsCircle(delta.x,delta.y,target.x,target.y,bodyRadius,1.1f)&&!Physics2D.Linecast(start,point,1<<8))
                {if(pulseId!=0)t.lastLaserPulse=pulseId;HitTurret(i,100);}
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
                if(HasGunner(i)){game.Notice("포탑을 조종하는 에너지 분신부터 제압하세요.",2);return true;}
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
        {if(phase==6&&!finished&&!clashing&&turrets[0].broken&&turrets[1].broken&&game.hero.CaptureEnergy>=99.99f)BeginClash();}
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
            else if(phase==6)GUI.Label(new Rect(Screen.width/2-300,95,600,60),"파이널 / 보스 충전 중 무적\n포탑 2개 파괴 ▶ 우클릭 입자 수집 ▶ 자동 레이저 충돌",label);
            else
            {
                GUI.Label(new Rect(Screen.width/2-300,95,600,55),ActivePhaseHint(),label);
                if(phase==3)for(int i=0;i<2;i++)
                {
                    var t=turrets[i];Vector3 p=game.View.WorldToScreenPoint(t.position+Vector2.up*3);
                    string text=mounted==i?"좌클릭: 3초 압축 발사 / E: 하차":HasGunner(i)?"조종 분신을 제압하세요":"E: 포탑 조종";
                    GUI.Label(new Rect(p.x-150,Screen.height-p.y-25,300,60),text,label);
                }
            }
        }
        void OnDestroy()
        {
            if(game && game.Sniper==this){game.Sniper=null;if(game.hero&&(mounted>=0||clashing))game.hero.HoldForEncounter(false);}
            foreach(var fold in curtain) if(fold) Destroy(fold.gameObject);
            if(attacks!=null)attacks.Dispose();
            if(arena!=null)arena.Dispose();
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
