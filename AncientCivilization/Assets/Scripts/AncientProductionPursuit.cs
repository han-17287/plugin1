using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Inventory breach, two playable bike/deck rounds, then a directed recovery finisher.
    public sealed class AncientProductionPursuit : MonoBehaviour
    {
        sealed class Hazard {public GameObject root;public SpriteRenderer art;public bool live,bomb,landed;public Vector2 velocity;public float fuse;}
        readonly AncientProductionSequence sequence=new AncientProductionSequence();
        readonly Hazard[] hazards=new Hazard[12];
        AncientGame game;AncientEnemy commander;AncientParkedBike escortBike;Rigidbody2D truck;BoxCollider2D deck;
        Transform cargo;SpriteRenderer truckArt;SpriteRenderer[] coreArt=new SpriteRenderer[3];
        LineRenderer hook,tell,cut,beam,glow,core;AncientBeamVisuals beamProfile;
        AncientProductionPhase applied=AncientProductionPhase.Dormant;
        bool lane,cargoSecured,truckDestroyed,pendingArrest,ready,boardingReady;float truckX,nextHazard,deckTime,captainTimer,kinematicSpeed;
        int hazardCursor,volley,captainPattern,hookSlot=-1;bool captainWindup;Vector2 captainAim,throwOrigin;float throwBikeX,captainFacing=1;
        string objective="";GUIStyle caption,hint;Vector2 roadHeroPrevious;
        public AncientProductionSequence Sequence {get{return sequence;}}
        public AncientEnemy Commander {get{return commander;}}
        public bool Active {get{return sequence.Phase!=AncientProductionPhase.Dormant&&sequence.Phase!=AncientProductionPhase.Secured;}}
        public bool InChaseLane {get{return lane;}}
        public bool Cinematic {get{return sequence.Cinematic;}}
        public bool AutoDrive {get{return sequence.AutoDrive;}}
        public float CinematicBikeSpeed {get{return sequence.Phase==AncientProductionPhase.Finisher&&sequence.Elapsed<3.2f?Mathf.Lerp(20,32,Mathf.Clamp01(sequence.Elapsed/1.5f)):0;}}
        public bool CinematicBikeBladeActive {get{return sequence.Phase==AncientProductionPhase.Finisher&&sequence.Elapsed>=1.5f&&sequence.Elapsed<3.2f;}}
        public bool DeckCombat {get{return sequence.Phase==AncientProductionPhase.Deck;}}
        public bool CargoSecured {get{return cargoSecured;}}
        public bool TruckDestroyed {get{return truckDestroyed;}}
        public bool CommanderRecovering {get{return DeckCombat&&!captainWindup;}}
        public float CommanderWindup {get{return captainWindup?Mathf.Clamp01(1-captainTimer/.85f):0;}}
        public Vector2 BoardingHook {get{return truck?(Vector2)truck.position+new Vector2(-1.5f,6):Vector2.zero;}}
        public string Objective {get{return objective;}}
        public float DriveSpeed {get{return lane?Mathf.Min(17,Mathf.Max(0,(AncientProductionLayout.ChaseEnd-46-game.hero.transform.position.x)*.8f)):17;}}
        public void Initialize(AncientGame owner){game=owner;AncientArt.Truck();}
        bool ExternalPaused {get{return !game||game.MainMenu||game.skills||(game.Journal&&game.Journal.Open)||(game.Story&&(game.Story.DialogueOpen||game.Story.LiftBusy))||!Application.isFocused;}}
        bool EnsureActors()
        {
            if(ready)return true;if(!game.hero||!game.hero.Combat)return false;
            var body=new GameObject("Core theft pursuit truck");body.layer=8;truck=body.AddComponent<Rigidbody2D>();truck.bodyType=RigidbodyType2D.Kinematic;truck.interpolation=RigidbodyInterpolation2D.Interpolate;
            deck=body.AddComponent<BoxCollider2D>();deck.size=new Vector2(6.6f,2.7f);deck.offset=new Vector2(0,1.35f);
            truckArt=AncientArt.Display("Pursuit cargo vehicle",AncientArt.Truck(),Vector2.zero,7,body.transform);
            var plate=game.Box("Walkable cargo roof",Vector2.zero,new Vector2(6.6f,.14f),new Color(.35f,.53f,.58f),10);plate.transform.SetParent(body.transform,false);plate.transform.localPosition=new Vector2(0,2.7f);
            cargo=new GameObject("Detachable secured core cradle").transform;cargo.SetParent(body.transform,false);cargo.localPosition=new Vector2(-.5f,1.45f);
            var container=game.Box("Core cargo cradle",Vector2.zero,new Vector2(3.8f,1.4f),new Color(.07f,.22f,.3f),9);container.transform.SetParent(cargo,false);
            for(int i=0;i<3;i++){coreArt[i]=AncientArt.Display("Stolen energy core "+i,AncientArt.WireParticle(),new Vector2(-1.2f+i*1.2f,0),10,cargo);coreArt[i].transform.localScale=Vector3.one*.38f;coreArt[i].color=AncientGame.Cyan;}
            commander=new GameObject("Core theft transport captain").AddComponent<AncientEnemy>();commander.Initialize(3);commander.ConfigureProductionCommander(this);commander.transform.SetParent(body.transform,false);commander.transform.localPosition=new Vector2(1.4f,2.7f);game.enemies.Add(commander);
            escortBike=new GameObject("Autonomous pursuit motorcycle").AddComponent<AncientParkedBike>();escortBike.Initialize(new Vector2(AncientProductionLayout.StockX-2,.84f));game.parkedBikes.Add(escortBike);
            hook=game.Line("Truck boarding latch",AncientGame.Cyan,.07f,14);hook.loop=true;hook.positionCount=4;
            hookSlot=game.anchors.Count;game.anchors.Add(BoardingHook);
            tell=game.Line("Transport captain attack tell",AncientGame.Orange,.065f,14);tell.positionCount=2;tell.enabled=false;
            cut=game.Line("Cargo separation blade",new Color(1,.88f,.12f),.2f,23);cut.positionCount=9;cut.enabled=false;
            beam=game.Line("Recovery finisher continuous laser",new Color(.4f,.88f,1),2.8f,23);core=game.Line("Recovery finisher white core",Color.white,.95f,24);glow=game.Line("Recovery finisher glow",new Color(.1f,.55f,1,.65f),4.2f,22);beamProfile=new AncientBeamVisuals(glow,true);
            ShowBeam(false);
            for(int i=0;i<hazards.Length;i++){var h=new Hazard();h.root=new GameObject("Pursuit debris or fused bomb "+i);h.art=AncientArt.Display("Physical road hazard",AncientArt.MetalProjectile(i%3),Vector2.zero,11,h.root.transform);h.root.SetActive(false);hazards[i]=h;}
            ready=true;return true;
        }
        public void BeginBreach()
        {
            if(Active||lane)return;if(!EnsureActors())return;
            game.ClearBattleProjectiles();game.hero.Combat.ResetCombat();sequence.Begin();applied=AncientProductionPhase.Dormant;
            truckX=AncientProductionLayout.StockX+10;truck.position=new Vector2(truckX,0);commander.ResetProductionHealth(1);game.boss=commander;
            cargoSecured=truckDestroyed=false;game.ProductionMap.OpenBreach();game.hero.HoldForEncounter(true);ApplyPhase();WriteSave();game.Save();
        }
        public void BeginOrResume()
        {
            if(Active||lane||!EnsureActors())return;
            sequence.RestoreRound(Mathf.Clamp(AncientGame.Saved("productionRound"),0,1));applied=AncientProductionPhase.Dormant;lane=true;
            truckX=AncientProductionLayout.ChaseStart+24+sequence.Round*420;truck.position=new Vector2(truckX,0);commander.ResetProductionHealth(sequence.HealthRatio);game.boss=commander;
            game.ProductionMap.OpenBreach();ApplyPhase();game.SnapCameraToHero();
        }
        public void ResumeArrestScene()
        {
            if(!EnsureActors()){pendingArrest=true;return;}pendingArrest=false;
            sequence.RestoreRound(1);while(sequence.Phase==AncientProductionPhase.Chase)sequence.Tick(.25f);
            sequence.BeginBoarding();sequence.LandOnDeck();sequence.ApplyDamage(1);
            while(sequence.Phase!=AncientProductionPhase.Secured)sequence.Tick(.25f);
            lane=true;applied=sequence.Phase;truckX=AncientProductionLayout.ChaseStart+900;truck.position=new Vector2(truckX,0);
            CargoSafe();DestroyTruck();commander.ResetProductionHealth(.3f);commander.transform.SetParent(null,true);commander.transform.position=new Vector2(truckX-5,0);
            game.boss=null;game.hero.TravelTo(new Vector2(truckX-8,1.3f));escortBike.Park(new Vector2(truckX-11,.84f),1,80);RefreshObjective();game.SnapCameraToHero();
        }
        public void CloseAfterArrest()
        {
            if(!game)return;lane=false;sequence.Reset();applied=sequence.Phase;game.boss=null;ClearHazards();ShowBeam(false);if(hook)hook.enabled=false;if(tell)tell.enabled=false;
            game.hero.HoldForEncounter(false);game.hero.TravelTo(new Vector2(115,1.3f));game.CombatCheckpoint(0,new Vector2(115,1.3f));game.SnapCameraToHero();
            if(commander){game.enemies.Remove(commander);Destroy(commander.gameObject);}if(truck)Destroy(truck.gameObject);if(cargo)Destroy(cargo.gameObject);
            if(escortBike){game.parkedBikes.Remove(escortBike);Destroy(escortBike.gameObject);}if(hookSlot>=0)game.anchors[hookSlot]=new Vector2(-100000,-100000);ready=false;
        }
        public void WriteSave()
        {if(!game||!game.Story||game.Story.Stage>=8)return;AncientGame.SetSaved("productionVersion",1);if(Active||lane)AncientGame.SetSaved("productionRound",sequence.Round);}
        public bool Retry()
        {
            if(!Active)return false;int round=sequence.Round;game.ClearBattleProjectiles();ClearHazards();ShowBeam(false);cut.enabled=false;cargoSecured=truckDestroyed=false;
            cargo.SetParent(truck.transform,false);cargo.localPosition=new Vector2(-.5f,1.45f);cargo.gameObject.SetActive(true);deck.enabled=truckArt.enabled=true;
            sequence.RestoreRound(round);applied=AncientProductionPhase.Dormant;lane=true;truckX=AncientProductionLayout.ChaseStart+24+round*420;truck.position=new Vector2(truckX,0);
            commander.transform.SetParent(truck.transform,false);commander.transform.localPosition=new Vector2(1.4f,2.7f);commander.ResetProductionHealth(sequence.HealthRatio);game.boss=commander;
            game.hero.HoldForEncounter(false);ApplyPhase();game.SnapCameraToHero();game.BlockGameplayInput();game.Notice("추격 재도전 / 현재 승선 구간부터",2);game.Save();return true;
        }
        public bool CanBoardWire(Vector2 selected)
        {
            if(!ready||!game.hero.bike||sequence.Phase!=AncientProductionPhase.Approach||truck.position.x-game.hero.transform.position.x>14)return false;
            if((selected-BoardingHook).sqrMagnitude<1)return true;
            var bounds=deck.bounds;bounds.Expand(new Vector3(1.5f,2,1));return bounds.Contains(new Vector3(selected.x,selected.y,truck.transform.position.z));
        }
        public bool TryMovingAnchor(Vector2 selected,out Transform moving)
        {moving=null;if(!ready||(selected-BoardingHook).sqrMagnitude>1)return false;moving=truck.transform;return true;}
        public bool TryBoardWire(Vector2 selected,out Vector2 point)
        {
            point=Vector2.zero;if(!CanBoardWire(selected))return false;
            if(!sequence.BeginBoarding())return false;point=BoardingHook;ApplyPhase();return true;
        }
        public float FilterCommanderDamage(float raw,float hp,float maxHP)
        {return maxHP<=0?0:sequence.FilterRatioDamage(raw/maxHP)*maxHP;}
        public void CommanderDamaged(AncientEnemy enemy,float damage)
        {if(enemy!=commander)return;sequence.ApplyDamage(damage/420f);ApplyPhase();}
        public void TickCommander(AncientEnemy enemy)
        {
            if(!ready||enemy!=commander||!game.hero)return;
            if(lane&&!truckDestroyed){var p=enemy.transform.localPosition;p.x=Mathf.Clamp(p.x,-2.25f,2.25f);p.y=2.7f;enemy.transform.localPosition=p;}
            float face=captainWindup?captainFacing:game.hero.transform.position.x<enemy.transform.position.x?-1:1;
            enemy.BossVisual(captainWindup?1:DeckCombat?5:0,face,CommanderWindup,captainWindup,CommanderRecovering);
            if(!DeckCombat||ExternalPaused){tell.enabled=false;return;}
            captainTimer-=Time.deltaTime;
            if(captainWindup)
            {
                Vector2 start=enemy.AimPoint,target=(Vector2)truck.position+captainAim;tell.enabled=true;tell.SetPosition(0,start);tell.SetPosition(1,captainPattern%3==0?start+Vector2.right*captainFacing*3.3f:target);
                if(captainTimer>0)return;captainWindup=false;tell.enabled=false;
                if(captainPattern%3==0)
                {Vector2 d=(Vector2)game.hero.transform.position-enemy.AimPoint;if(Mathf.Abs(d.x)<3.3f&&Mathf.Abs(d.y)<1.7f&&d.x*captainFacing>=-.5f)game.hero.Hurt(18);game.Slash(enemy.AimPoint,captainFacing,true);}
                else
                {
                    Vector2 startPoint=captainPattern%3==2?target+Vector2.up*4:enemy.AimPoint+Vector2.up*.35f;
                    Vector2 velocity=captainPattern%3==2?new Vector2(kinematicSpeed,-4):(target-startPoint)/.6f+new Vector2(kinematicSpeed,2.4f);
                    game.Projectiles.Spawn(startPoint,velocity,16,false,1,8);
                }
                captainPattern++;captainTimer=1.15f;game.Sound(6);
            }
            else if(captainTimer<=0)
            {captainWindup=true;captainFacing=face;captainTimer=.85f;captainAim=(Vector2)game.hero.transform.position-(Vector2)truck.position;game.Sound(8);}
        }
        void Update()
        {
            if(pendingArrest&&game.hero&&game.hero.Combat)ResumeArrestScene();
            if(!ready||!Active||ExternalPaused)return;Step(Time.unscaledDeltaTime);
        }
        void OnApplicationFocus(bool focused){if(!focused&&Active)Time.timeScale=0;}
        void Step(float dt)
        {
            if(!ready||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;float roadStep=dt;dt=Mathf.Min(dt,.25f);
            sequence.Tick(dt);ApplyPhase();if(!Active)return;
            var phase=sequence.Phase;
            if(phase==AncientProductionPhase.Breach){BreachPose();return;}
            if(phase==AncientProductionPhase.Knockback){KnockbackPose();return;}
            if(phase==AncientProductionPhase.Finisher){FinisherPose();return;}
            if(phase==AncientProductionPhase.Chase)
            {
                if(!game.hero.bike){game.hero.RetryFromCheckpoint();return;}
                TickHazards(roadStep);nextHazard-=dt;if(nextHazard<=0){SpawnHazard();nextHazard=sequence.Round==0?2.5f:2.05f;}
            }
            if(phase==AncientProductionPhase.Approach&&!boardingReady&&truck.position.x-game.hero.transform.position.x<=14)
            {boardingReady=true;objective="공격 중지 / 점등된 트럭 고리를 좌클릭해 와이어 승선";game.Notice("승선 가능 / 청록 고리를 좌클릭",2);game.Sound(2);}
            if(phase==AncientProductionPhase.Boarding)
            {
                Vector2 h=game.hero.transform.position;
                if(!game.hero.Wired){var body=game.hero.GetComponent<Rigidbody2D>();body.linearVelocity=new Vector2(kinematicSpeed>1?kinematicSpeed:17,body.linearVelocity.y);}
                if(Mathf.Abs(h.x-truck.position.x)<3.1f&&Mathf.Abs(h.y-3.8f)<.35f&&game.hero.IsOnGround&&!game.hero.Wired)
                {sequence.LandOnDeck();ApplyPhase();}
                else if(sequence.Elapsed>4){sequence.AbortBoarding();ReturnToBike(false);ApplyPhase();}
            }
            if(phase==AncientProductionPhase.Deck)
            {
                deckTime+=dt;if(deckTime>.7f&&game.hero.transform.position.y<2.15f)
                {sequence.AbortBoarding();ReturnToBike(false);ApplyPhase();}
            }
            if(!game.hero.bike)escortBike.Park(new Vector2(truck.position.x-8,.84f),1,80);
        }
        void FixedUpdate()
        {
            if(!ready||!lane||Cinematic||ExternalPaused||truckDestroyed)return;
            float old=truck.position.x;
            if(sequence.Phase==AncientProductionPhase.Boarding)truckX=Mathf.Min(AncientProductionLayout.ChaseEnd-24,old+17*Time.fixedDeltaTime);
            else if(AutoDrive)
            {
                float offset=sequence.Phase==AncientProductionPhase.Chase?22:Mathf.Lerp(22,8,Mathf.Clamp01(sequence.Elapsed/3));
                truckX=Mathf.Min(AncientProductionLayout.ChaseEnd-24,Mathf.MoveTowards(old,game.hero.transform.position.x+offset,Time.fixedDeltaTime*24));
            }
            else if(DeckCombat)truckX=Mathf.Min(AncientProductionLayout.ChaseEnd-24,old+Time.fixedDeltaTime*7);
            kinematicSpeed=(truckX-old)/Time.fixedDeltaTime;truck.MovePosition(new Vector2(truckX,0));
        }
        void ApplyPhase()
        {
            if(applied==sequence.Phase)return;var previous=applied;applied=sequence.Phase;RefreshObjective();
            truck.interpolation=Cinematic?RigidbodyInterpolation2D.None:RigidbodyInterpolation2D.Interpolate;
            if(applied==AncientProductionPhase.Breach){game.Sound(4);game.Shake(.3f);game.Burst(new Vector2(104,3),AncientGame.Orange,40);game.Notice("벽 돌파 / 코어 적재 차량 도주",2);}
            if(applied==AncientProductionPhase.Chase)
            {
                if(previous==AncientProductionPhase.Breach){truckX=AncientProductionLayout.ChaseStart+24;truck.position=new Vector2(truckX,0);}
                else truckX=truck.position.x;
                if(truckX>AncientProductionLayout.ChaseEnd-360){truckX=AncientProductionLayout.ChaseStart+444;truck.position=new Vector2(truckX,0);}
                lane=true;ClearHazards();game.ClearBattleProjectiles();game.hero.HoldForEncounter(false);ReturnToBike(true);nextHazard=2;volley=0;WriteSave();game.Save();game.Sound(3);
                roadHeroPrevious=game.hero.transform.position;
                game.SnapCameraToHero();
            }
            if(applied==AncientProductionPhase.Approach)
            {boardingReady=false;ClearHazards();game.ClearBattleProjectiles();objective="공격 중지 / 트럭 접근 중 · 승선 고리 점등 대기";game.Notice("적 공격 중지 · 트럭에 접근합니다.",2);}
            if(applied==AncientProductionPhase.Boarding){ClearHazards();game.hero.SetProductionAttackMode(false);}
            if(applied==AncientProductionPhase.Deck)
            {game.hero.SetProductionAttackMode(true);deckTime=0;captainTimer=.7f;captainWindup=false;game.Notice("트럭 지붕 전투 / 전조를 피하고 빈틈에 블레이드·입자 레이저",4);game.BlockGameplayInput();}
            if(applied==AncientProductionPhase.Knockback)
            {
                game.ClearBattleProjectiles();ClearHazards();tell.enabled=false;captainWindup=false;game.hero.Combat.CancelAttacks();throwOrigin=game.hero.transform.position;throwBikeX=truck.position.x-8;game.hero.HoldForEncounter(true);game.Sound(8);game.Notice("트럭 급가속 / 자동주행 오토바이로 복귀",2);
            }
            if(applied==AncientProductionPhase.Finisher)
            {truckX=truck.position.x;ReturnToBike(true);game.hero.HoldForEncounter(true);game.hero.Combat.CancelAttacks();ClearHazards();game.ClearBattleProjectiles();game.Notice("세 번째 탑승 / 코어 회수",2);}
            if(applied==AncientProductionPhase.Secured)
            {
                ShowBeam(false);cut.enabled=false;
                truckX=truck.position.x;if(cargo)cargo.position=new Vector2(truckX-11,1);
                CargoSafe();DestroyTruck();game.hero.HoldForEncounter(false);game.hero.TravelTo(new Vector2(truckX-8,1.3f));
                commander.transform.SetParent(null,true);commander.transform.position=new Vector2(truckX-5,0);commander.ResetProductionHealth(.3f);game.boss=null;game.Story.ProductionCleared();
            }
        }
        void ReturnToBike(bool forward)
        {
            if(forward&&sequence.Phase==AncientProductionPhase.Chase&&sequence.Elapsed<.05f)truck.position=new Vector2(truckX,0);
            float x=truck.position.x-(sequence.Phase==AncientProductionPhase.Chase||sequence.Phase==AncientProductionPhase.Finisher?22:8);
            game.hero.MountProductionBike(escortBike,new Vector2(x,.84f),DriveSpeed);
        }
        void BreachPose()
        {
            float t=sequence.Elapsed;PlaceTruck(new Vector2(AncientProductionLayout.StockX+10+Mathf.Clamp01(t/2.8f)*20,0));
            game.hero.CinematicPose(false,1);if(t>1.5f)game.hero.CinematicMove(new Vector2(AncientProductionLayout.StockX+Mathf.Clamp01((t-1.5f)/1.5f)*5,1.3f));
        }
        void KnockbackPose()
        {
            float u=Mathf.Clamp01(sequence.Elapsed/AncientProductionRules.KnockbackSeconds);
            float bikeTravel=17*sequence.Elapsed;PlaceTruck(new Vector2(truckX+u*(17*AncientProductionRules.KnockbackSeconds+14),0));
            Vector2 target=new Vector2(throwBikeX+bikeTravel,.84f);game.hero.CinematicMove(Vector2.Lerp(throwOrigin,target,u)+Vector2.up*(Mathf.Sin(u*Mathf.PI)*2.7f));game.hero.CinematicPose(true,-1);
            if(u>.7f)game.hero.ProductionCinematicRoll(Mathf.Lerp(0,-80,(u-.7f)/.3f));escortBike.Park(target,1,80);
        }
        void FinisherPose()
        {
            float t=sequence.Elapsed;PlaceTruck(new Vector2(truckX+Mathf.Min(t,7.65f)*12,0));Vector2 truckPoint=truck.position;
            if(cargoSecured)cargo.position=new Vector2(truckPoint.x-11,1);
            if(t<1.5f){game.hero.CinematicMove(new Vector2(Mathf.Lerp(truckPoint.x-22,truckPoint.x-.5f,t/1.5f),.84f));}
            else if(t<3.2f)
            {
                game.hero.CinematicMove(new Vector2(truckPoint.x-.6f,.84f));cut.enabled=true;
                float head=Mathf.Lerp(-1.1f,1.1f,Mathf.Clamp01((t-1.5f)/1.1f));
                for(int i=0;i<9;i++){float a=head-.75f+i*.75f/8;cut.SetPosition(i,truckPoint+new Vector2(-.2f+Mathf.Cos(a)*2.6f,1.5f+Mathf.Sin(a)*1.3f));}
                if(t>2.65f)CargoSafe();
            }
            else if(t<6.3f)
            {
                if(game.hero.bike){game.hero.DismountProductionBike();game.hero.HoldForEncounter(true);}
                cut.enabled=false;float u=(t-3.2f)/3.1f;Vector2 p=new Vector2(truckPoint.x-1.5f-u*4,1.4f+Mathf.Sin(u*Mathf.PI*.7f)*7.2f);
                game.hero.CinematicMove(p);game.hero.CinematicPose(true,1);game.hero.ProductionCinematicRoll(-360*Mathf.Clamp01(u/.8f));
            }
            else if(t<8.45f)
            {
                Vector2 p=new Vector2(truckPoint.x-5.5f,7.2f);game.hero.CinematicMove(p);game.hero.CinematicLaserPose(1);
                if(t>=7.1f){ShowBeam(true);Vector2 start=p+new Vector2(.3f,.55f),end=truckPoint+new Vector2(.7f,1.7f);SetBeam(start,end);if(!truckDestroyed&&t>=7.65f)DestroyTruck();}
            }
            else
            {
                ShowBeam(false);float u=Mathf.Clamp01((t-8.45f)/2.05f);game.hero.CinematicMove(Vector2.Lerp(new Vector2(truckPoint.x-5.5f,7.2f),new Vector2(truckPoint.x-8,1.3f),u));game.hero.CinematicPose(u<.8f,1);
            }
        }
        void CargoSafe()
        {
            if(cargoSecured||!cargo)return;cargoSecured=true;cargo.SetParent(null,true);cargo.position=new Vector2(truck.position.x-11,1);game.Impact(cargo.position,AncientGame.Cyan,true);game.Sound(5);
        }
        void PlaceTruck(Vector2 point){truck.position=point;truck.transform.position=point;}
        void DestroyTruck()
        {
            if(truckDestroyed||!cargoSecured)return;truckDestroyed=true;deck.enabled=false;truckArt.enabled=false;
            if(commander){commander.transform.SetParent(null,true);commander.transform.position=new Vector2(truck.position.x-5,0);}
            foreach(var view in truck.GetComponentsInChildren<SpriteRenderer>())view.enabled=false;
            game.Burst((Vector2)truck.position+Vector2.up*1.8f,AncientGame.Orange,65);game.Impact((Vector2)truck.position+Vector2.up*1.8f,AncientGame.Orange,true);game.Shake(.4f);game.Sound(4);
        }
        void ShowBeam(bool visible)
        {if(beam)beam.enabled=visible;if(core)core.enabled=visible;if(glow)glow.enabled=visible;if(!visible&&beamProfile!=null)beamProfile.HideEndpoints();}
        void SetBeam(Vector2 start,Vector2 end)
        {foreach(var line in new[]{beam,core,glow}){line.SetPosition(0,start);line.SetPosition(1,end);}beamProfile.ShowEndpoints(start,end,2.8f,1,true);}
        void SpawnHazard()
        {
            var h=hazards[hazardCursor++%hazards.Length];h.live=true;h.bomb=volley++%3==2;h.landed=false;h.fuse=AncientProductionHazardRules.BombFuse;
            h.root.transform.position=new Vector2(game.hero.transform.position.x+AncientProductionHazardRules.SpawnLead,h.bomb?AncientProductionHazardRules.BombStartHeight:AncientProductionHazardRules.BombFloorHeight);h.velocity=h.bomb?new Vector2(0,AncientProductionHazardRules.BombStartVelocityY):Vector2.zero;
            h.art.sprite=AncientArt.MetalProjectile(h.bomb?2:1);h.art.color=h.bomb?new Color(1,.55f,.2f):new Color(.6f,.78f,.85f);h.art.transform.localScale=Vector3.one*(h.bomb?.6f:.8f);h.root.SetActive(true);game.Sound(h.bomb?8:0);
        }
        void TickHazards(float dt)
        {
            Vector2 current=game.hero.transform.position;
            foreach(var h in hazards)
            {
                if(!h.live)continue;Vector2 p=h.root.transform.position;
                float after=0,vy=h.velocity.y;bool blast=h.bomb&&AncientProductionHazardRules.TickBomb(ref p.y,ref vy,ref h.fuse,ref h.landed,dt,out after);h.velocity.y=vy;
                if(h.bomb&&h.landed)h.art.color=h.fuse% .2f<.1f?Color.white:new Color(1,.45f,.1f);
                h.root.transform.position=p;
                bool debris=!h.bomb&&AncientProductionHazardRules.SweptDebris(roadHeroPrevious.x,roadHeroPrevious.y,current.x,current.y,p.x);
                if(debris||blast)
                {
                    float hx,hy;bool bombHit=blast&&AncientProductionHazardRules.BombImpactHero(roadHeroPrevious.x,roadHeroPrevious.y,current.x,current.y,after,dt,out hx,out hy)&&AncientProductionHazardRules.BombHit(hx,hy,p.x);
                    if(debris||bombHit)game.hero.Hurt(h.bomb?22:14);game.Impact(p,AncientGame.Orange,h.bomb);h.live=false;h.root.SetActive(false);
                    if(((Vector2)game.hero.transform.position-current).sqrMagnitude>25)return;
                }
                else if(p.x<game.hero.transform.position.x-5){h.live=false;h.root.SetActive(false);}
            }
            roadHeroPrevious=current;
        }
        void ClearHazards(){foreach(var h in hazards)if(h!=null){h.live=false;h.root.SetActive(false);}}
        void RefreshObjective()
        {
            string round=sequence.Round==0?"첫 추격":"두 번째 추격";
            switch(sequence.Phase)
            {
                case AncientProductionPhase.Breach:objective="생산장 벽 돌파 / 도난 코어 추격";break;
                case AncientProductionPhase.Chase:objective=round+" / 자동 전진 · Space로 잔재와 폭탄 회피";break;
                case AncientProductionPhase.Approach:objective="공격 중지 / 트럭 지붕 고리를 좌클릭해 와이어 승선";break;
                case AncientProductionPhase.Boarding:objective="트럭 지붕으로 견인 중";break;
                case AncientProductionPhase.Deck:objective="운송대장 전투 / 전조 회피 → 블레이드·입자 레이저";break;
                case AncientProductionPhase.Knockback:objective="급가속 / 자동주행 오토바이로 복귀";break;
                case AncientProductionPhase.Finisher:objective="세 번째 탑승 / 짐칸 분리 → 백덤블링 → 입자 레이저";break;
                case AncientProductionPhase.Secured:objective="코어 세 개 확보 / 운송대장 검거";break;
                default:objective="코어 재고 확인 / 생산장 이동";break;
            }
        }
        public bool TryCamera(out Vector2 focus,out float zoom)
        {
            focus=Vector2.zero;zoom=7.5f;if(!ready||(!Active&&!lane))return false;
            if(!lane){focus=new Vector2(AncientProductionLayout.StockX+6,4.8f);zoom=7;return true;}
            if(DeckCombat){focus=new Vector2(truck.position.x,4.8f);zoom=6.5f;}
            else if(sequence.Phase==AncientProductionPhase.Finisher){focus=new Vector2(truck.position.x-4,5.2f);zoom=9.5f;}
            else if(sequence.Phase==AncientProductionPhase.Secured){focus=new Vector2(truck.position.x-7,4.6f);zoom=6.2f;}
            else{focus=new Vector2(game.hero.transform.position.x+7,4.6f);zoom=7.8f;}return true;
        }
        public bool TryDialogueActor(string speaker,out Transform actor,out float headOffset)
        {actor=null;headOffset=2.2f;if(!ready||!commander||(speaker!="운송대장"&&speaker!="용의자"))return false;actor=commander.transform;return true;}
        void LateUpdate()
        {
            if(!ready)return;bool shown=sequence.Phase==AncientProductionPhase.Approach&&boardingReady||sequence.Phase==AncientProductionPhase.Boarding;hook.enabled=shown;
            if(hookSlot>=0)game.anchors[hookSlot]=shown?BoardingHook:new Vector2(-100000,-100000);
            if(shown){Vector2 p=BoardingHook+Vector2.up*.15f;for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;hook.SetPosition(i,p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.38f);}}
        }
        void OnGUI()
        {
            if(!ready||!Active||game.MainMenu||game.skills||(game.Journal&&game.Journal.Open)||(game.Story&&game.Story.DialogueOpen)||Event.current.type!=EventType.Repaint)return;
            if(caption==null){caption=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=19,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true};hint=new GUIStyle(caption){fontSize=14,fontStyle=FontStyle.Normal};}
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;GUI.matrix=Matrix4x4.identity;GUI.depth=-16;
            if(Cinematic){GUI.color=new Color(0,0,0,.8f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height*.07f),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,Screen.height*.93f,Screen.width,Screen.height*.07f),Texture2D.whiteTexture);}
            GUI.color=Color.white;GUI.Label(new Rect(Screen.width*.15f,Screen.height*.79f,Screen.width*.7f,55),objective,caption);
            if(sequence.Phase==AncientProductionPhase.Chase||sequence.Phase==AncientProductionPhase.Approach)GUI.Label(new Rect(Screen.width*.2f,Screen.height*.87f,Screen.width*.6f,30),"자동주행 / Space 점프 · 트럭 접근 때 좌클릭 와이어",hint);
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
        void OnDestroy()
        {
            if(game&&game.boss==commander)game.boss=null;if(commander){if(game)game.enemies.Remove(commander);Destroy(commander.gameObject);}if(truck)Destroy(truck.gameObject);if(cargo)Destroy(cargo.gameObject);
            if(escortBike){if(game)game.parkedBikes.Remove(escortBike);Destroy(escortBike.gameObject);}foreach(var h in hazards)if(h!=null&&h.root)Destroy(h.root);
            foreach(var line in new[]{hook,tell,cut,beam,core,glow})if(line)Destroy(line.gameObject);if(beamProfile!=null)beamProfile.Dispose();
        }
    }
}
