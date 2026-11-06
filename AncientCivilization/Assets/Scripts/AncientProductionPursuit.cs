using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Inventory breach, two playable bike/deck rounds, then a directed recovery finisher.
    public sealed class AncientProductionPursuit : MonoBehaviour
    {
        sealed class Hazard {public GameObject root;public SpriteRenderer art;public LineRenderer footprint,fuseRing;public bool live,bomb,landed;public Vector2 velocity;public float fuse;}
        readonly AncientProductionSequence sequence=new AncientProductionSequence();
        readonly Hazard[] hazards=new Hazard[12];
        AncientGame game;AncientEnemy commander;AncientParkedBike escortBike;Rigidbody2D truck;BoxCollider2D deck;
        Transform cargo;SpriteRenderer truckArt,truckRoofArt,boardingMast,boardingMastLight,boardingMastCap;SpriteRenderer[] coreArt=new SpriteRenderer[3];
        LineRenderer hook,tell,strikeZone,cut,filament,filamentCore,beam,glow,core;AncientBeamVisuals beamProfile;
        AncientProductionPhase applied=AncientProductionPhase.Dormant;
        bool lane,cargoSecured,truckDestroyed,pendingArrest,ready,boardingReady,handoff,handedOff,captainEjecting;float truckX,nextHazard,deckTime,captainTimer,captainRecoverySeconds=.7f,kinematicSpeed,handoffElapsed;
        int hazardCursor,volley,captainPattern,captainLastAttack,hookSlot=-1;bool captainWindup;Vector2 captainAim,throwOrigin;float throwBikeX,captainFacing=1;
        string objective="";GUIStyle caption,hint;Vector2 roadHeroPrevious,cargoReleaseOffset,captainEjectOrigin;float cargoReleasedAt,captainEjectedAt,captainActionUntil;
        public AncientProductionSequence Sequence {get{return sequence;}}
        public AncientEnemy Commander {get{return commander;}}
        public bool Active {get{return sequence.Phase!=AncientProductionPhase.Dormant&&sequence.Phase!=AncientProductionPhase.Secured;}}
        public bool InChaseLane {get{return lane;}}
        public bool Cinematic {get{return handoff||sequence.Cinematic;}}
        public bool AutoDrive {get{return sequence.AutoDrive;}}
        public float CinematicBikeSpeed {get{return sequence.Phase==AncientProductionPhase.Finisher?AncientProductionMotion.BikeSpeed(sequence.Elapsed):0;}}
        public bool CinematicBikeBladeActive {get{return sequence.Phase==AncientProductionPhase.Finisher&&sequence.Elapsed>=AncientProductionMotion.AlongsideSeconds&&sequence.Elapsed<AncientProductionMotion.TakeoffSeconds;}}
        public bool DeckCombat {get{return sequence.Phase==AncientProductionPhase.Deck;}}
        public bool CargoSecured {get{return cargoSecured;}}
        public bool TruckDestroyed {get{return truckDestroyed;}}
        public bool CommanderRecovering {get{return DeckCombat&&!captainWindup;}}
        public float CommanderRecoveryRemaining {get{return CommanderRecovering?Mathf.Clamp01(captainTimer/Mathf.Max(.01f,captainRecoverySeconds)):0;}}
        public bool CommanderAttackIsPhysical {get{return (captainWindup?captainPattern%3:captainLastAttack)==0;}}
        float CaptainWindupSeconds {get{return captainPattern%3==0?.85f:captainPattern%3==1?1:1.1f;}}
        public float CommanderWindup {get{return captainWindup?Mathf.Clamp01(1-captainTimer/CaptainWindupSeconds):0;}}
        public Vector2 BoardingHook {get{return truck?(Vector2)truck.position+new Vector2(-1.5f,6):Vector2.zero;}}
        public string Objective {get{return objective;}}
        public float DriveSpeed {get{return lane?Mathf.Min(17,Mathf.Max(0,(AncientProductionLayout.ChaseEnd-46-game.hero.transform.position.x)*.8f)):17;}}
        public void Initialize(AncientGame owner){game=owner;AncientArt.ProductionTruck();AncientArt.ProductionCaptain(0);}
        bool ExternalPaused {get{return !game||game.MainMenu||game.skills||(game.Journal&&game.Journal.Open)||(game.Story&&(game.Story.DialogueOpen||game.Story.LiftBusy))||!Application.isFocused;}}
        bool EnsureActors()
        {
            if(ready)return true;if(!game.hero||!game.hero.Combat)return false;
            var body=new GameObject("Core theft pursuit truck");body.layer=8;truck=body.AddComponent<Rigidbody2D>();truck.bodyType=RigidbodyType2D.Kinematic;truck.interpolation=RigidbodyInterpolation2D.Interpolate;
            deck=body.AddComponent<BoxCollider2D>();deck.size=new Vector2(6.6f,2.7f);deck.offset=new Vector2(0,1.35f);
            truckArt=AncientArt.Display("Pursuit empty cargo chassis",AncientArt.ProductionTruck(),Vector2.zero,7,body.transform);
            var plate=game.Box("Walkable cargo roof",Vector2.zero,new Vector2(6.6f,.14f),new Color(.35f,.53f,.58f),10);plate.transform.SetParent(body.transform,false);plate.transform.localPosition=new Vector2(0,2.63f);
            truckRoofArt=plate.GetComponent<SpriteRenderer>();
            boardingMast=TruckPart("Telescopic boarding mast",Vector2.one,new Color(.18f,.29f,.34f),6);
            boardingMastLight=TruckPart("Boarding mast inset guide",Vector2.one,new Color(.12f,.55f,.62f),7);
            boardingMastCap=TruckPart("Boarding mast latch housing",new Vector2(.62f,.18f),new Color(.6f,.75f,.77f),7);
            cargo=new GameObject("Detachable secured core cradle").transform;cargo.SetParent(body.transform,false);cargo.localPosition=new Vector2(-.5f,1.45f);
            var container=game.Box("Core cargo cradle",Vector2.zero,new Vector2(3.8f,1.4f),new Color(.07f,.22f,.3f),9);container.transform.SetParent(cargo,false);
            for(int i=0;i<3;i++)
            {
                float x=-1.2f+i*1.2f;
                CargoPart("Stolen core steel housing "+i,new Vector2(x,0),new Vector2(.84f,1.2f),new Color(.18f,.27f,.30f),10);
                coreArt[i]=CargoPart("Stolen energy core "+i,new Vector2(x,0),new Vector2(.48f,.86f),AncientGame.Cyan,11);
                CargoPart("Core glass highlight "+i,new Vector2(x-.12f,.02f),new Vector2(.07f,.7f),new Color(.8f,1,1),12);
                foreach(float side in new[]{-1f,1f})CargoPart("Core retaining contact "+i,new Vector2(x,side*.51f),new Vector2(.66f,.14f),new Color(.65f,.72f,.70f),12);
            }
            commander=new GameObject("Core theft transport captain").AddComponent<AncientEnemy>();commander.Initialize(3);commander.ConfigureProductionCommander(this);commander.transform.SetParent(body.transform,false);commander.transform.localPosition=new Vector2(1.4f,2.7f);game.enemies.Add(commander);
            escortBike=new GameObject("Autonomous pursuit motorcycle").AddComponent<AncientParkedBike>();escortBike.Initialize(new Vector2(AncientProductionLayout.StockX-2,.84f));game.parkedBikes.Add(escortBike);
            hook=game.Line("Truck boarding latch",AncientGame.Cyan,.07f,14);hook.loop=true;hook.positionCount=4;
            hookSlot=game.anchors.Count;game.anchors.Add(BoardingHook);
            tell=game.Line("Transport captain attack tell",AncientGame.Orange,.065f,14);tell.positionCount=2;tell.enabled=false;
            strikeZone=game.Line("Transport captain locked impact footprint",AncientGame.Orange,.055f,14);strikeZone.loop=true;strikeZone.positionCount=4;strikeZone.enabled=false;
            cut=game.Line("Cargo separation blade",new Color(1,.88f,.12f),.2f,23);cut.positionCount=9;cut.enabled=false;
            filament=game.Line("Mounted recovery blade",new Color(1,.85f,.08f),.18f,24);filament.endWidth=.09f;filament.enabled=false;
            filamentCore=game.Line("Mounted recovery blade hot core",new Color(1,1,.75f),.065f,25);filamentCore.endWidth=.03f;filamentCore.enabled=false;
            beam=game.Line("Recovery finisher continuous laser",new Color(.4f,.88f,1),2.8f,23);core=game.Line("Recovery finisher white core",Color.white,.95f,24);glow=game.Line("Recovery finisher glow",new Color(.1f,.55f,1,.65f),4.2f,22);beamProfile=new AncientBeamVisuals(glow,true);
            ShowBeam(false);
            for(int i=0;i<hazards.Length;i++)
            {
                var h=new Hazard();h.root=new GameObject("Pursuit debris or fused bomb "+i);h.art=AncientArt.Display("Physical road hazard",AncientArt.MetalProjectile(i%3),Vector2.zero,11,h.root.transform);
                h.footprint=game.Line("Pooled road danger footprint "+i,AncientGame.Orange,.055f,10);h.footprint.transform.SetParent(h.root.transform,false);h.footprint.positionCount=4;h.footprint.loop=true;
                h.fuseRing=game.Line("Pooled bomb fuse progress "+i,AncientGame.Orange,.045f,12);h.fuseRing.transform.SetParent(h.root.transform,false);h.fuseRing.positionCount=17;
                h.root.SetActive(false);hazards[i]=h;
            }
            ready=true;return true;
        }
        SpriteRenderer CargoPart(string name,Vector2 point,Vector2 size,Color color,int order)
        {
            var part=game.Box(name,Vector2.zero,size,color,order);part.transform.SetParent(cargo,false);part.transform.localPosition=point;
            return part.GetComponent<SpriteRenderer>();
        }
        SpriteRenderer TruckPart(string name,Vector2 size,Color color,int order)
        {
            var part=game.Box(name,Vector2.zero,size,color,order);part.transform.SetParent(truck.transform,false);
            var view=part.GetComponent<SpriteRenderer>();view.enabled=false;return view;
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
            if(!game||handoff||!lane)return;
            handoff=true;handedOff=false;handoffElapsed=0;
            game.CombatCheckpoint(0,new Vector2(115,1.3f));game.hero.HoldForEncounter(true);game.BlockGameplayInput();
            ClearHazards();ShowBeam(false);ShowFilament(false);if(hook)hook.enabled=false;if(tell)tell.enabled=false;if(strikeZone)strikeZone.enabled=false;
        }
        void CompleteArrestHandoff()
        {
            if(handedOff)return;handedOff=true;lane=false;sequence.Reset();applied=sequence.Phase;game.boss=null;
            if(strikeZone)strikeZone.enabled=false;ShowFilament(false);
            game.hero.TravelTo(new Vector2(115,1.3f));game.hero.HoldForEncounter(true);game.SnapCameraToHero();
            if(commander){game.enemies.Remove(commander);Destroy(commander.gameObject);}if(truck)Destroy(truck.gameObject);if(cargo)Destroy(cargo.gameObject);
            if(escortBike){game.parkedBikes.Remove(escortBike);Destroy(escortBike.gameObject);}if(hookSlot>=0)game.anchors[hookSlot]=new Vector2(-100000,-100000);ready=false;
        }
        void TickHandoff(float dt)
        {
            if(!handoff||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            float next=handoffElapsed+Mathf.Min(dt,.25f);
            if(!handedOff&&next>=.325f){handoffElapsed=.325f;CompleteArrestHandoff();return;}
            handoffElapsed=next;
            if(handoffElapsed>=.65f)FinishHandoff();
        }
        void FinishHandoff()
        {
            CompleteArrestHandoff();handoff=false;game.hero.HoldForEncounter(false);game.BlockGameplayInput();game.SnapCameraToHero();
            if(game.Campaign)game.Campaign.TryContinueProductionRoute();
            game.Notice("현장 인계 완료 / 동부 운송로로 계속 이동",3);
        }
        public void WriteSave()
        {if(!game||!game.Story||game.Story.Stage>=8)return;AncientGame.SetSaved("productionVersion",1);if(Active||lane)AncientGame.SetSaved("productionRound",sequence.Round);}
        public bool Retry()
        {
            if(handoff){FinishHandoff();game.Save();return true;}
            if(!Active)return false;int round=sequence.Round;game.ClearBattleProjectiles();ClearHazards();ShowBeam(false);ShowFilament(false);cut.enabled=false;tell.enabled=strikeZone.enabled=false;cargoSecured=truckDestroyed=captainEjecting=false;
            cargo.SetParent(truck.transform,false);cargo.localPosition=new Vector2(-.5f,1.45f);cargo.gameObject.SetActive(true);deck.enabled=truckArt.enabled=truckRoofArt.enabled=true;
            sequence.RestoreRound(round);applied=AncientProductionPhase.Dormant;lane=true;truckX=AncientProductionLayout.ChaseStart+24+round*420;truck.position=new Vector2(truckX,0);
            commander.transform.SetParent(truck.transform,false);commander.transform.localPosition=new Vector2(1.4f,2.7f);commander.ResetProductionHealth(sequence.HealthRatio);game.boss=commander;
            game.hero.HoldForEncounter(false);ApplyPhase();game.SnapCameraToHero();game.BlockGameplayInput();game.Notice("추격 재도전 / 현재 승선 구간부터",2);game.Save();return true;
        }
        public bool CanBoardWire(Vector2 selected)
        {
            if(!ready||!boardingReady||!game.hero.bike||sequence.Phase!=AncientProductionPhase.Approach||truck.position.x-game.hero.transform.position.x>14)return false;
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
            bool followThrough=DeckCombat&&Time.time<captainActionUntil;
            float face=captainWindup||followThrough?captainFacing:game.hero.transform.position.x<enemy.transform.position.x?-1:1;
            int pose=captainWindup?(captainPattern%3==0?1:3):DeckCombat?(followThrough?(captainLastAttack==0?2:4):5):0;
            enemy.BossVisual(pose,face,CommanderWindup,captainWindup,CommanderRecovering);
            if(!DeckCombat||ExternalPaused){tell.enabled=strikeZone.enabled=false;return;}
            captainTimer-=Time.deltaTime;
            if(captainWindup)
            {
                Vector2 target=(Vector2)truck.position+captainAim;DrawCaptainTell(enemy.AimPoint,target);
                if(captainTimer>0)return;captainWindup=false;tell.enabled=strikeZone.enabled=false;
                if(captainPattern%3==0)
                {Vector2 origin=enemy.AimPoint,d=(Vector2)game.hero.transform.position-origin;game.Slash(origin,captainFacing,true);if(Mathf.Abs(d.x)<3.3f&&Mathf.Abs(d.y)<1.7f&&d.x*captainFacing>=-.5f)game.hero.Hurt(18);}
                else
                {
                    Vector2 startPoint=captainPattern%3==2?CaptainDropOrigin(target):CaptainThrowOrigin(enemy.AimPoint);
                    Vector2 velocity=captainPattern%3==2?new Vector2(kinematicSpeed,-4):(target-startPoint)/.6f+new Vector2(kinematicSpeed,2.4f);
                    game.Projectiles.Spawn(startPoint,velocity,16,false,1,8);
                }
                if(!DeckCombat)return;
                captainLastAttack=captainPattern%3;captainTimer=captainRecoverySeconds=captainLastAttack==2?1.45f:1.25f;captainPattern++;captainActionUntil=Time.time+.2f;
                enemy.BossVisual(captainLastAttack==0?2:4,captainFacing,0,false,true);game.Sound(6);
            }
            else if(captainTimer<=0)
            {captainWindup=true;captainFacing=face;captainTimer=CaptainWindupSeconds;captainAim=(Vector2)game.hero.transform.position-(Vector2)truck.position;game.Sound(8);}
        }
        void DrawCaptainTell(Vector2 origin,Vector2 target)
        {
            int attack=captainPattern%3;float progress=CommanderWindup;Color color=AncientGame.Orange;color.a=Mathf.Lerp(.3f,.95f,progress);
            tell.enabled=strikeZone.enabled=true;tell.startColor=tell.endColor=strikeZone.startColor=strikeZone.endColor=color;
            if(attack==0)
            {
                tell.positionCount=2;tell.SetPosition(0,origin);tell.SetPosition(1,origin+Vector2.right*captainFacing*3.3f);
                SetFootprint(strikeZone,origin.x+captainFacing*1.4f,truck.position.y+2.79f,1.9f,.1f);
            }
            else if(attack==1)
            {
                // The aim is locked in truck-local space; draw the real ballistic arc in
                // that same frame, without the carrier's world-space travel component.
                Vector2 start=CaptainThrowOrigin(origin),velocity=(target-start)/.6f+Vector2.up*2.4f;
                tell.positionCount=15;for(int i=0;i<15;i++){float t=i/14f*.6f;tell.SetPosition(i,start+velocity*t+Vector2.down*4*t*t);}
                SetFootprint(strikeZone,target.x,truck.position.y+2.79f,.75f,.1f);
            }
            else
            {
                tell.positionCount=2;tell.SetPosition(0,CaptainDropOrigin(target));tell.SetPosition(1,target);
                SetFootprint(strikeZone,target.x,truck.position.y+2.79f,.75f,.1f);
            }
        }
        static void SetFootprint(LineRenderer line,float x,float y,float halfWidth,float height)
        {
            line.SetPosition(0,new Vector2(x-halfWidth,y));line.SetPosition(1,new Vector2(x+halfWidth,y));
            line.SetPosition(2,new Vector2(x+halfWidth,y+height));line.SetPosition(3,new Vector2(x-halfWidth,y+height));
        }
        Vector2 CaptainThrowOrigin(Vector2 chest){return chest+new Vector2(captainFacing*.9f,.35f);}
        static Vector2 CaptainDropOrigin(Vector2 target)
        {return new Vector2(target.x,Mathf.Min(target.y+4,AncientProductionLayout.TunnelCeiling-.6f));}
        void Update()
        {
            if(handoff){if(!ExternalPaused)TickHandoff(Time.unscaledDeltaTime);return;}
            if(pendingArrest&&game.hero&&game.hero.Combat)ResumeArrestScene();
            if(!ready||!Active||ExternalPaused)return;Step(Cinematic?Time.unscaledDeltaTime:Time.deltaTime);
        }
        void OnApplicationFocus(bool focused){if(!focused&&(Active||handoff))Time.timeScale=0;}
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
            if(phase==AncientProductionPhase.Approach&&!boardingReady&&sequence.Elapsed>=.7f&&truck.position.x-game.hero.transform.position.x<=14)
            {boardingReady=true;objective="공격 중지 / 점등된 트럭 고리를 좌클릭해 와이어 승선";game.Notice("승선 가능 / 청록 고리를 좌클릭",2);game.Sound(2);}
            if(phase==AncientProductionPhase.Boarding)
            {
                Vector2 h=game.hero.transform.position;
                // A truck at the tunnel stop has zero carrier speed. A fallback17
                // would walk the officer off its stationary roof during the landing.
                if(!game.hero.Wired){var body=game.hero.GetComponent<Rigidbody2D>();body.linearVelocity=new Vector2(kinematicSpeed,body.linearVelocity.y);}
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
            if(applied!=AncientProductionPhase.Deck)tell.enabled=strikeZone.enabled=false;
            if(applied!=AncientProductionPhase.Finisher)ShowFilament(false);
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
            {game.hero.SetProductionAttackMode(true);deckTime=0;captainPattern=captainLastAttack=0;captainTimer=captainRecoverySeconds=.7f;captainActionUntil=-1;captainWindup=false;game.Notice("트럭 지붕 전투 / 전조를 피하고 빈틈에 블레이드·입자 레이저",4);game.BlockGameplayInput();}
            if(applied==AncientProductionPhase.Knockback)
            {
                commander.ClearProductionHitFlash();game.ClearBattleProjectiles();ClearHazards();tell.enabled=strikeZone.enabled=false;captainWindup=false;game.hero.Combat.CancelAttacks();throwOrigin=game.hero.transform.position;throwBikeX=truck.position.x-8;game.hero.HoldForEncounter(true);game.Sound(8);game.Notice("트럭 급가속 / 자동주행 오토바이로 복귀",2);
            }
            if(applied==AncientProductionPhase.Finisher)
            {truckX=truck.position.x;ReturnToBike(true);game.hero.HoldForEncounter(true);game.hero.Combat.CancelAttacks();ClearHazards();game.ClearBattleProjectiles();game.Notice("세 번째 탑승 / 코어 회수",2);}
            if(applied==AncientProductionPhase.Secured)
            {
                ShowBeam(false);cut.enabled=false;
                truckX=truck.position.x;if(cargo)cargo.position=new Vector2(truckX-11,1);
                CargoSafe();DestroyTruck();game.hero.HoldForEncounter(false);game.hero.TravelTo(new Vector2(truckX-8,1.3f));
                captainEjecting=false;commander.transform.SetParent(null,true);commander.transform.position=new Vector2(truckX-5,0);commander.ResetProductionHealth(.3f);game.boss=null;game.Story.ProductionCleared();
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
            if(t>1.5f)
            {game.hero.CinematicMove(new Vector2(AncientProductionLayout.StockX+Mathf.Clamp01((t-1.5f)/1.5f)*5,1.3f));game.hero.ProductionCinematicRun(t-1.5f,1);}
            else game.hero.CinematicPose(false,1);
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
            float t=sequence.Elapsed;PlaceTruck(new Vector2(truckX+AncientProductionMotion.TruckTravel(t),0));Vector2 truckPoint=truck.position;
            Vector2 heroPoint=truckPoint+new Vector2(AncientProductionMotion.HeroOffsetX(t),AncientProductionMotion.HeroY(t));
            if(cargoSecured)
            {
                float slide=t-cargoReleasedAt;
                cargo.position=truckPoint+new Vector2(AncientProductionMotion.CargoOffsetX(cargoReleaseOffset.x,slide),AncientProductionMotion.CargoY(cargoReleaseOffset.y,slide));
            }
            if(t<AncientProductionMotion.AlongsideSeconds){game.hero.CinematicMove(heroPoint);}
            else if(t<AncientProductionMotion.TakeoffSeconds)
            {
                game.hero.CinematicMove(heroPoint);cut.enabled=true;ShowFilament(true);
                Vector2 emitter=game.hero.WireOrigin;float extension=Mathf.Clamp01((t-1.5f)/.22f);
                float head=Mathf.Lerp(.65f,-.4f,Mathf.SmoothStep(0,1,(t-1.5f)/1.4f));Vector2 tip=emitter+new Vector2(Mathf.Cos(head),Mathf.Sin(head))*3*extension;
                filament.SetPosition(0,emitter);filament.SetPosition(1,tip);filamentCore.SetPosition(0,emitter);filamentCore.SetPosition(1,tip);
                for(int i=0;i<9;i++){float a=head+.6f-i*.6f/8;cut.SetPosition(i,emitter+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*3*extension);}
                if(t>AncientProductionMotion.CargoReleaseSeconds)CargoSafe();
            }
            else if(t<AncientProductionMotion.HoverSeconds)
            {
                if(game.hero.bike){game.hero.DismountProductionBike();game.hero.HoldForEncounter(true);}
                cut.enabled=false;ShowFilament(false);float u=(t-AncientProductionMotion.TakeoffSeconds)/(AncientProductionMotion.HoverSeconds-AncientProductionMotion.TakeoffSeconds);
                game.hero.CinematicMove(heroPoint);game.hero.CinematicPose(true,1);game.hero.ProductionCinematicRoll(-360*Mathf.Clamp01(u/.8f));
            }
            else if(t<AncientProductionMotion.LandingSeconds)
            {
                game.hero.CinematicMove(heroPoint);game.hero.CinematicLaserPose(1);
                if(t>=AncientProductionMotion.LaserSeconds){ShowBeam(true);Vector2 start=heroPoint+new Vector2(.3f,.55f),end=truckPoint+new Vector2(.7f,1.7f);SetBeam(start,end);if(!truckDestroyed&&t>=AncientProductionMotion.TruckStopSeconds)DestroyTruck();}
            }
            else
            {
                ShowBeam(false);float u=Mathf.Clamp01((t-AncientProductionMotion.LandingSeconds)/(AncientProductionMotion.EndSeconds-AncientProductionMotion.LandingSeconds));game.hero.CinematicMove(heroPoint);game.hero.CinematicPose(u<.8f,1);
            }
            TickCaptainEjection(t);
        }
        void CargoSafe()
        {
            if(cargoSecured||!cargo)return;cargoSecured=true;cargoReleaseOffset=(Vector2)cargo.position-truck.position;cargoReleasedAt=sequence.Elapsed;cargo.SetParent(null,true);
            if(sequence.Phase!=AncientProductionPhase.Finisher)cargo.position=new Vector2(truck.position.x-11,1);
            game.Impact(cargo.position,AncientGame.Cyan,true);game.Sound(5);
        }
        void ShowFilament(bool visible){if(filament)filament.enabled=visible;if(filamentCore)filamentCore.enabled=visible;}
        void PlaceTruck(Vector2 point){truck.position=point;truck.transform.position=point;}
        void DestroyTruck()
        {
            if(truckDestroyed||!cargoSecured)return;truckDestroyed=true;deck.enabled=false;truckArt.enabled=false;
            if(commander)
            {
                captainEjectOrigin=commander.transform.position;commander.transform.SetParent(null,true);
                captainEjectedAt=sequence.Elapsed;captainEjecting=sequence.Phase==AncientProductionPhase.Finisher;
                if(!captainEjecting)commander.transform.position=new Vector2(truck.position.x-5,0);
                commander.BossVisual(5,-1,0,false,true);
            }
            foreach(var view in truck.GetComponentsInChildren<SpriteRenderer>())view.enabled=false;
            game.Burst((Vector2)truck.position+Vector2.up*1.8f,AncientGame.Orange,65);game.Impact((Vector2)truck.position+Vector2.up*1.8f,AncientGame.Orange,true);game.Shake(.4f);game.Sound(4);
        }
        void TickCaptainEjection(float elapsed)
        {
            if(!captainEjecting||!commander)return;
            float u=Mathf.Clamp01((elapsed-captainEjectedAt)/.8f),smooth=Mathf.SmoothStep(0,1,u);
            // Preserve the roof position at detach. The blast lifts the living captain
            // briefly; its diminishing arc reaches the road with zero vertical speed.
            Vector2 target=new Vector2(truck.position.x-5,0);
            commander.transform.position=Vector2.Lerp(captainEjectOrigin,target,smooth)+Vector2.up*(Mathf.Sin(u*Mathf.PI)*(1-u)*1.3f);
            if(u>=1)captainEjecting=false;
        }
        void ShowBeam(bool visible)
        {if(beam)beam.enabled=visible;if(core)core.enabled=visible;if(glow)glow.enabled=visible;if(!visible&&beamProfile!=null)beamProfile.HideEndpoints();}
        void SetBeam(Vector2 start,Vector2 end)
        {
            beam.SetPosition(0,start);beam.SetPosition(1,end);core.SetPosition(0,start);core.SetPosition(1,end);glow.SetPosition(0,start);glow.SetPosition(1,end);
            beamProfile.ShowEndpoints(start,end,2.8f,1,true);
        }
        void SpawnHazard()
        {
            var h=hazards[hazardCursor++%hazards.Length];h.live=true;h.bomb=volley++%3==2;h.landed=false;h.fuse=AncientProductionHazardRules.BombFuse;
            h.root.transform.position=new Vector2(game.hero.transform.position.x+AncientProductionHazardRules.SpawnLead,h.bomb?AncientProductionHazardRules.BombStartHeight:AncientProductionHazardRules.BombFloorHeight);h.velocity=h.bomb?new Vector2(0,AncientProductionHazardRules.BombStartVelocityY):Vector2.zero;
            h.art.sprite=AncientArt.MetalProjectile(h.bomb?2:1);h.art.color=h.bomb?new Color(1,.55f,.2f):new Color(.6f,.78f,.85f);h.art.transform.localScale=Vector3.one*(h.bomb?.6f:.8f);h.root.SetActive(true);DrawHazardWarning(h,h.root.transform.position);game.Sound(h.bomb?8:0);
        }
        static void DrawHazardWarning(Hazard hazard,Vector2 point)
        {
            float width=hazard.bomb?AncientProductionHazardRules.BombHalfWidth:AncientProductionHazardRules.DebrisHalfWidth;
            SetFootprint(hazard.footprint,point.x,.09f,width,.11f);
            float progress=hazard.bomb&&hazard.landed?1-Mathf.Clamp01(hazard.fuse/AncientProductionHazardRules.BombFuse):0;
            Color tint=hazard.bomb?new Color(1,.5f,.1f,Mathf.Lerp(.4f,.9f,progress)):new Color(1,.8f,.25f,.55f);
            hazard.footprint.startColor=hazard.footprint.endColor=hazard.fuseRing.startColor=hazard.fuseRing.endColor=tint;
            hazard.fuseRing.enabled=hazard.bomb&&hazard.landed;
            if(hazard.fuseRing.enabled)for(int i=0;i<17;i++)
            {
                float angle=Mathf.PI*.5f-i/16f*Mathf.PI*2*progress;
                hazard.fuseRing.SetPosition(i,point+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*.5f);
            }
        }
        void TickHazards(float dt)
        {
            Vector2 current=game.hero.transform.position;
            foreach(var h in hazards)
            {
                if(!h.live)continue;Vector2 p=h.root.transform.position;
                float after=0,vy=h.velocity.y;bool blast=h.bomb&&AncientProductionHazardRules.TickBomb(ref p.y,ref vy,ref h.fuse,ref h.landed,dt,out after);h.velocity.y=vy;
                if(h.bomb&&h.landed)h.art.color=h.fuse% .2f<.1f?Color.white:new Color(1,.45f,.1f);
                h.root.transform.position=p;DrawHazardWarning(h,p);
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
                case AncientProductionPhase.Deck:objective="운송대장 전투 / 전조 회피 ▶ 블레이드·입자 레이저";break;
                case AncientProductionPhase.Knockback:objective="급가속 / 자동주행 오토바이로 복귀";break;
                case AncientProductionPhase.Finisher:objective="세 번째 탑승 / 짐칸 분리 ▶ 백덤블링 ▶ 입자 레이저";break;
                case AncientProductionPhase.Secured:objective="코어 세 개 확보 / 운송대장 검거";break;
                default:objective="코어 재고 확인 / 생산장 이동";break;
            }
        }
        public bool TryCamera(out Vector2 focus,out float zoom)
        {
            focus=Vector2.zero;zoom=7.5f;if(!ready||(!Active&&!lane))return false;
            if(!lane){focus=new Vector2(AncientProductionLayout.StockX+6,4.8f);zoom=7;return true;}
            if(DeckCombat)
            {
                float offset=game.hero.transform.position.x-truck.position.x;
                focus=new Vector2(truck.position.x+offset*.5f,4.8f);
                zoom=AncientProductionLayout.DeckCameraZoom(offset,game.View.aspect);
            }
            else if(sequence.Phase==AncientProductionPhase.Finisher)
            {
                float close=Mathf.SmoothStep(0,1,sequence.Elapsed/AncientProductionMotion.AlongsideSeconds);
                focus=new Vector2(truck.position.x+Mathf.Lerp(-9.5f,-4,close),5.2f);
                float halfSpan=Mathf.Max(Mathf.Abs(game.hero.transform.position.x-focus.x)+2,truck.position.x+4-focus.x);
                zoom=Mathf.Max(9.5f,halfSpan/Mathf.Max(.5f,game.View.aspect));
            }
            else if(sequence.Phase==AncientProductionPhase.Secured){focus=new Vector2(truck.position.x-7,4.6f);zoom=6.2f;}
            else if(sequence.Phase==AncientProductionPhase.Chase||sequence.Phase==AncientProductionPhase.Approach)
            {
                float gap=truck.position.x-game.hero.transform.position.x;
                focus=new Vector2(game.hero.transform.position.x+AncientProductionLayout.RoadCameraLead(gap),4.6f);
                zoom=AncientProductionLayout.RoadCameraZoom(gap,game.View.aspect);
            }
            else{focus=new Vector2(game.hero.transform.position.x+7,4.6f);zoom=7.8f;}return true;
        }
        public bool TryDialogueActor(string speaker,out Transform actor,out float headOffset)
        {actor=null;headOffset=2.2f;if(!ready||!commander||(speaker!="운송대장"&&speaker!="용의자"))return false;actor=commander.transform;headOffset=commander.DialogueHeadHeight;return true;}
        void UpdateBoardingMast()
        {
            float extension=sequence.Phase==AncientProductionPhase.Approach?Mathf.SmoothStep(0,1,sequence.Elapsed/.7f):
                sequence.Phase==AncientProductionPhase.Boarding?1:sequence.Phase==AncientProductionPhase.Deck?1-Mathf.SmoothStep(0,1,sequence.Elapsed/.5f):0;
            bool shown=!truckDestroyed&&extension>.001f;
            boardingMast.enabled=boardingMastLight.enabled=boardingMastCap.enabled=shown;if(!shown)return;
            float length=3.3f*extension;Vector2 middle=new Vector2(-1.5f,2.7f+length*.5f);
            boardingMast.transform.localPosition=middle;boardingMast.transform.localScale=new Vector3(.14f,length,1);
            boardingMastLight.transform.localPosition=middle;boardingMastLight.transform.localScale=new Vector3(.035f,length,1);
            boardingMastCap.transform.localPosition=new Vector2(-1.5f,2.7f+length);
        }
        void LateUpdate()
        {
            if(!ready)return;UpdateBoardingMast();bool shown=sequence.Phase==AncientProductionPhase.Approach&&boardingReady||sequence.Phase==AncientProductionPhase.Boarding;hook.enabled=shown;
            if(hookSlot>=0)game.anchors[hookSlot]=shown?BoardingHook:new Vector2(-100000,-100000);
            if(shown){Vector2 p=BoardingHook;for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;hook.SetPosition(i,p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.38f);}}
        }
        void OnGUI()
        {
            if(!game||ExternalPaused||Event.current.type!=EventType.Repaint||!handoff&&(!ready||!Active))return;
            if(caption==null){caption=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=19,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true};hint=new GUIStyle(caption){fontSize=14,fontStyle=FontStyle.Normal};}
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            float scale=Mathf.Min(2,Mathf.Min(Screen.width/1280f,Screen.height/720f));if(scale<=0)return;
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-16;float width=Screen.width/scale,height=Screen.height/scale;
            if(handoff)
            {
                float fade=Mathf.Clamp01(handoffElapsed<=.325f?handoffElapsed/.325f:(.65f-handoffElapsed)/.325f);
                GUI.color=new Color(0,0,0,fade);GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture);
                GUI.color=new Color(1,1,1,fade);GUI.Label(new Rect(width*.15f,height*.5f-35,width*.7f,70),"현장 인계 완료\n동부 운송로",caption);
            }
            else if(Cinematic)
            {
                GUI.color=new Color(0,0,0,.8f);GUI.DrawTexture(new Rect(0,0,width,height*.07f),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,height*.93f,width,height*.07f),Texture2D.whiteTexture);
                GUI.color=Color.white;GUI.Label(new Rect(width*.08f,height*.935f,width*.84f,height*.06f),objective,caption);
            }
            else
            {
                // Keep the road, landing point and normal boss health bar unobscured.
                // The complete objective already appears in the regular top-right HUD.
                float left=330,span=width-646;GUI.color=Color.white;
                string title=DeckCombat?captainWindup?"운송대장 / 공격 전조":"운송대장 / 공격 기회":sequence.Phase==AncientProductionPhase.Approach?"공격 중지 / 와이어 승선":sequence.Phase==AncientProductionPhase.Boarding?"트럭 승선 / 와이어 견인":"자동주행 / Space 점프";
                string cue=DeckCombat?captainWindup?(captainPattern%3==0?"근접 쓸기 / 뒤로 넘거나 거리를 벌리세요":captainPattern%3==1?"물체 투척 / 고정된 궤적을 피하거나 블레이드로 파괴":"낙하 압축판 / 바닥 표식에서 벗어나세요"):"블레이드 공격 · 우클릭 입자 수집 · 좌클릭 유지 후 놓아 레이저":sequence.Phase==AncientProductionPhase.Approach?(boardingReady?"점등된 청록 고리를 좌클릭해 트럭에 올라타세요":"트럭 접근 중 / 승선 고리가 켜질 때까지 기다리세요"):sequence.Phase==AncientProductionPhase.Boarding?"움직이는 트럭 지붕으로 견인 중":"노란 표식은 잔재 / 주황 원이 차오르면 폭탄 폭발";
                GUI.Label(new Rect(left,18,span,28),title,caption);GUI.Label(new Rect(left,47,span,44),cue,hint);
            }
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
        void OnDestroy()
        {
            if(game&&game.boss==commander)game.boss=null;if(commander){if(game)game.enemies.Remove(commander);Destroy(commander.gameObject);}if(truck)Destroy(truck.gameObject);if(cargo)Destroy(cargo.gameObject);
            if(escortBike){if(game)game.parkedBikes.Remove(escortBike);Destroy(escortBike.gameObject);}foreach(var h in hazards)if(h!=null&&h.root)Destroy(h.root);
            foreach(var line in new[]{hook,tell,strikeZone,cut,filament,filamentCore,beam,core,glow})if(line)Destroy(line.gameObject);if(beamProfile!=null)beamProfile.Dispose();
        }
    }
}
