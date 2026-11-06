using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.AncientCivilization
{
    public sealed class AncientGame : MonoBehaviour
    {
        public static AncientGame Instance;
        public AncientHero hero;
        public AncientStory Story {get;private set;}
        public readonly List<Vector2> anchors = new List<Vector2>(512);
        public readonly List<AncientEnemy> enemies = new List<AncientEnemy>();
        public readonly List<AncientProjectile> shots = new List<AncientProjectile>(64);
        public readonly List<AncientPickup> pickups = new List<AncientPickup>();
        public readonly List<AncientParkedBike> parkedBikes=new List<AncientParkedBike>();
        public AncientEnemy boss;
        public AncientTraversal Traversal {get;private set;}
        public AncientCityCampaign City {get;private set;}
        public AncientWorldBatch WorldBatch {get;private set;}
        public AncientPerformance Performance {get;private set;}
        public AncientGameFeel Feel {get;private set;}
        public AncientExploration Exploration {get;private set;}
        public AncientJournal Journal {get;private set;}
        public AncientFieldCases Cases {get;private set;}
        public AncientFinale Finale {get;private set;}
        public AncientCampaign Campaign {get;private set;}
        public AncientSniperEncounter Sniper {get;internal set;}
        public AncientProjectilePool Projectiles {get;private set;}
        public AncientProductionMap ProductionMap {get;private set;}
        public AncientProductionPursuit Production {get;private set;}
        public AncientProductionResidents ProductionResidents {get;private set;}
        public bool skills, won, bossDefeated, showHelp;
        public bool MainMenu {get;private set;}
        public bool HasSession {get;private set;}
        public bool HasSavedProgress {get{return Saved("questStage")!=0||Saved("started")!=0||Saved("checkpoint")!=0||Saved("cores")!=0||Saved("credits")!=0||Saved("kills")!=0||Saved("score")!=0||Saved("bike")!=0||Saved("jump")!=0||Saved("capacity")!=0||Saved("armor")!=0||Saved("boss")!=0;}}
        public bool Paused {get {return MainMenu||skills||HasSession&&!Application.isFocused||(Story&&(Story.DialogueOpen||Story.LiftBusy))||(Journal&&Journal.Open)||(Finale&&Finale.Cinematic)||(Production&&Production.Cinematic);}}
        public bool GameplayInputReady {get{return Time.frameCount>inputReadyFrame;}}
        static bool startAfterReload;int inputReadyFrame;
        public int coreMask, credits, kills, score;
        public int Cores {get {return ((coreMask&1)!=0?1:0)+((coreMask&2)!=0?1:0)+((coreMask&4)!=0?1:0);}}
        public bool ArenaActive {get {return boss&&!boss.IsProductionBoss && (boss.IsDistrictBoss||!bossDefeated);}}
        public Vector2 checkpoint = new Vector2(2, 1.4f);
        public static readonly Color Cyan = new Color(.18f,.92f,1);
        public static readonly Color Orange = new Color(1,.43f,.14f);
        const string SaveRoot="PLUGIN.Ancient.v2.";
        static string SaveKey=SaveRoot;
        public static readonly string[] ProgressSaveKeys=BuildProgressSaveKeys();
        static string[] BuildProgressSaveKeys()
        {
            string[] standard={"started","cores","credits","checkpoint","boss","kills","score","bike","jump","capacity","armor","storyCores","storyCheckpoint","storyBoss","questStage","questFlags","residencePoints","upperCity","courseBestMs","factoryBestMs","cityFloor","cityCleared","secretaryCleared","retryActive","retryFloor","retryX","retryY","routeCaches","caseStarted","caseComplete","caseRelay","caseRecord","finalePhase","finaleZone","finaleCountdown","firstRunCleared","campaignVersion","campaignLegacy","campaignComplete","campaignSeen","campaignBeforeSeen","campaignAfterSeen","campaignActive","campaignTaskMission","campaignTasks","campaignCheckpoint","campaignDefeated","campaignChapterStarted","productionRound","productionVersion"};
            var all=new string[standard.Length+AncientCampaign.ProgressKeys.Length];standard.CopyTo(all,0);AncientCampaign.ProgressKeys.CopyTo(all,standard.Length);return all;
        }
        public static bool IsQAProfile {get{return SaveKey!=SaveRoot;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitializeSaveProfile()
        {
            SaveKey=SaveRoot;
#if UNITY_EDITOR
            string isolated=UnityEditor.SessionState.GetString("PLUGIN.QA.ActiveProfile","");
            if(isolated.StartsWith("PLUGIN.Ancient.QA.",System.StringComparison.Ordinal))SaveKey=isolated;
#endif
        }
        Camera view; Sprite square;GameObject productionCordon;
        readonly SpriteRenderer[] patrolArenaSeals=new SpriteRenderer[2];readonly BoxCollider2D[] patrolArenaSolids=new BoxCollider2D[2];bool patrolArenaClosed;
        GUIStyle small, title, centered;
        readonly AncientHudText hud=new AncientHudText();
        readonly GUIContent objectiveMeasure=new GUIContent();float objectiveHeight=55;string measuredObjective;
        string message; float messageUntil, shake, runTime;
        int checkpointStage;
        Material lineMaterial; AncientAudio audioCues;
        readonly List<AncientFragment> fragmentPool=new List<AncientFragment>();
        readonly List<AncientAfterimage> ghostPool=new List<AncientAfterimage>();
        readonly AncientImpactRing[] impactPool=new AncientImpactRing[24];
        readonly AncientSlashArc[] slashPool=new AncientSlashArc[12];
        int impactCursor,slashCursor;
        int fragmentCursor,ghostCursor;float hitPauseUntil,damageFlash,healthEcho=100;bool healthEchoBike;
        Vector3 cameraFollow;float smoothFrameTime=1f/60;
        public float FramesPerSecond {get{return 1f/Mathf.Max(.001f,smoothFrameTime);}}
        public float DamageFlash {get{return damageFlash;}}
        public Camera View {get{return view;}}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(!Object.FindAnyObjectByType<AncientGame>())new GameObject("Ancient Civilization").AddComponent<AncientGame>();
        }
        public static int Saved(string key,int fallback=0){return PlayerPrefs.GetInt(SaveKey+key,fallback);}
        public static void SetSaved(string key,int value){PlayerPrefs.SetInt(SaveKey+key,value);}
        void Awake()
        {
            QualitySettings.vSyncCount=0;Application.targetFrameRate=144;
            Time.fixedDeltaTime=1f/60;
            Instance=this;bool quickStart=startAfterReload||IsQAProfile;MainMenu=!quickStart;HasSession=quickStart;startAfterReload=false; audioCues=gameObject.AddComponent<AncientAudio>(); Time.timeScale=1; Physics2D.gravity=new Vector2(0,-32);
            coreMask=Saved("storyCores"); credits=Saved("credits"); checkpointStage=Saved("storyCheckpoint");
            bossDefeated=Saved("storyBoss")!=0; kills=Saved("kills");score=Saved("score");
            if(checkpointStage==1)checkpoint=new Vector2(46,2.3f);
            if(checkpointStage==2)checkpoint=new Vector2(86,1.3f);
            if(checkpointStage==3)checkpoint=new Vector2(132,1.3f);
            if(checkpointStage==4)checkpoint=new Vector2(115,1.3f);
            int cityFloor=Mathf.Clamp(Saved("cityFloor",Saved("upperCity")!=0?1:0),0,4);
            bool migrateRoute=Saved("campaignVersion")<AncientCampaignRules.RouteVersion;
            if(cityFloor>0||checkpointStage>=3)checkpoint=new Vector2(AncientCityLayout.Entry(cityFloor),1.3f);
            if(migrateRoute&&Saved("questStage")>=8)
            {
                SetSaved("retryActive",0);int count=AncientCampaignRules.FloorCount(Saved("campaignComplete"),cityFloor);
                if(cityFloor==4&&count>=3)checkpoint=new Vector2(count==3?AncientChapterLayout.SniperLeft-2:AncientChapterLayout.SecretaryLeft-2,1.3f);
                else if(count==4)checkpoint=new Vector2(AncientChapterLayout.ExitLift(cityFloor),1.3f);
            }
            if(Saved("retryActive")!=0&&Saved("retryFloor",-1)==cityFloor)
            {
                float retryX=Saved("retryX")/100f,retryY=Saved("retryY")/100f;
                if(AncientCityLayout.SafeResume(cityFloor,retryX,retryY))checkpoint=new Vector2(retryX,retryY);
            }
            int opening=Saved("questStage");
            if(opening<5)checkpoint=new Vector2(AncientProductionLayout.TutorialCheckpointX(opening),AncientProductionLayout.TutorialCheckpointY(opening));
            else if(opening==5||opening==6&&!bossDefeated)checkpoint=new Vector2(AncientProductionLayout.ChaseStart+Mathf.Clamp(Saved("productionRound"),0,1)*420+2,.84f);
            square=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            lineMaterial=new Material(Shader.Find("Sprites/Default"));
            view=new GameObject("Pixel Camera").AddComponent<Camera>();view.tag="MainCamera";view.gameObject.AddComponent<AudioListener>();
            view.orthographic=true;view.orthographicSize=5.8f;view.gameObject.AddComponent<AncientCameraFX>();view.backgroundColor=new Color(.025f,.04f,.07f);
            view.transform.position=new Vector3(checkpoint.x+4,4.6f,-10);cameraFollow=view.transform.position;
            for(int i=0;i<96;i++)
            {
                var f=Box("Pooled impact pixel",Vector2.zero,Vector2.one,Color.white,20).AddComponent<AncientFragment>();
                f.transform.SetParent(transform);f.gameObject.SetActive(false);fragmentPool.Add(f);
            }
            for(int i=0;i<8;i++)
            {
                var ghost=Box("Pooled afterimage",Vector2.zero,Vector2.one,Color.white,7).AddComponent<AncientAfterimage>();
                ghost.transform.SetParent(transform);ghost.gameObject.SetActive(false);ghostPool.Add(ghost);
            }
            for(int i=0;i<impactPool.Length;i++)
            {
                var line=Line("Pooled impact ring",Cyan,.035f,19);line.loop=true;line.positionCount=20;
                impactPool[i]=line.gameObject.AddComponent<AncientImpactRing>();line.gameObject.SetActive(false);
            }
            for(int i=0;i<slashPool.Length;i++)
            {
                var line=Line("Pooled enemy attack arc",Orange,.065f,18);line.positionCount=9;
                slashPool[i]=line.gameObject.AddComponent<AncientSlashArc>();line.gameObject.SetActive(false);
            }
            WorldBatch=gameObject.AddComponent<AncientWorldBatch>();WorldBatch.Initialize(this,square);
            BuildWorld();WorldBatch.Complete();
            Projectiles=gameObject.AddComponent<AncientProjectilePool>();Projectiles.Initialize(this);
            hero=new GameObject("백 경위 / 하층 치안경찰").AddComponent<AncientHero>();hero.transform.position=checkpoint;
            Story=gameObject.AddComponent<AncientStory>();Story.Initialize(this);
            Performance=gameObject.AddComponent<AncientPerformance>();Performance.Initialize(this);
            Feel=gameObject.AddComponent<AncientGameFeel>();Feel.Initialize(this);
            Production=gameObject.AddComponent<AncientProductionPursuit>();Production.Initialize(this);
            ProductionResidents=gameObject.AddComponent<AncientProductionResidents>();ProductionResidents.Initialize(this);
            Exploration=gameObject.AddComponent<AncientExploration>();Exploration.Initialize(this);
            Journal=gameObject.AddComponent<AncientJournal>();Journal.Initialize(this);
            Cases=gameObject.AddComponent<AncientFieldCases>();Cases.Initialize(this);
            gameObject.AddComponent<AncientVisuals>();gameObject.AddComponent<AncientPointer>();gameObject.AddComponent<AncientBootIntro>();gameObject.AddComponent<AncientMainMenu>();
            Time.timeScale=MainMenu?0:1;
            if(HasSession){SetSaved("started",1);PlayerPrefs.Save();inputReadyFrame=Time.frameCount+1;}
            Notice(opening<5?"하층 생산구 / 코어 재고 확인\n생산장으로 이동하며 기계팔 조작을 익히세요.":"저장한 순찰 지점에서 이어갑니다.",7);
        }
        void BuildWorld()
        {
            ProductionMap=gameObject.AddComponent<AncientProductionMap>();ProductionMap.Initialize(this);
            productionCordon=Box("Secured production service cordon",new Vector2(-5,4.8f),new Vector2(.25f,10),new Color(.1f,.65f,.8f,.6f),5);
            productionCordon.layer=8;productionCordon.AddComponent<BoxCollider2D>();productionCordon.SetActive(Saved("questStage")>=8);
            Platform(118,AncientChapterLayout.CaseLeft(0),0);
            Platform(AncientChapterLayout.CaseEnd(0),AncientChapterLayout.FloorEnd(0),0);
            Ceiling(AncientChapterLayout.CaseEnd(0),AncientChapterLayout.FloorEnd(0)+2,10);
            Traversal=gameObject.AddComponent<AncientTraversal>();Traversal.Initialize(this,false);
            City=gameObject.AddComponent<AncientCityCampaign>();City.Initialize(this);
            Finale=gameObject.AddComponent<AncientFinale>();Finale.Build(this);
            Campaign=gameObject.AddComponent<AncientCampaign>();Campaign.Initialize(this);
            AncientArt.Display("Eastern gate",AncientArt.World(3),new Vector2(115,0),-1);
            BuildLiftTerminal(AncientCityLayout.Entry(0));BuildLiftTerminal(AncientChapterLayout.ExitLift(0));
            var courseEnd=Box("Production route eastern bulkhead",new Vector2(AncientChapterLayout.FloorEnd(0)+1,5),new Vector2(.4f,10),new Color(.06f,.13f,.18f),1);
            courseEnd.layer=8;courseEnd.AddComponent<BoxCollider2D>();
        }
        void RoadRamp(float left,float right,float low,float high)
        {
            var ramp=new GameObject("Walkable street ramp");ramp.layer=8;ramp.transform.position=new Vector2(left,0);
            var solid=ramp.AddComponent<PolygonCollider2D>();solid.points=new[]{new Vector2(0,-.5f),new Vector2(right-left,-.5f),new Vector2(right-left,high),new Vector2(0,low)};
        }
        public void BuildLiftTerminal(float x)
        {
            StaticBox("Lift doorway",new Vector2(x,1.7f),new Vector2(2.6f,3.4f),new Color(.025f,.06f,.09f),0);
            foreach(float side in new[]{-1f,1f})
                StaticBox("Lift frame",new Vector2(x+side*1.4f,1.9f),new Vector2(.18f,3.8f),Cyan,1);
            StaticBox("Lift floor",new Vector2(x,.08f),new Vector2(3,.12f),Cyan,2);
            StaticBox("Lift header",new Vector2(x,3.8f),new Vector2(3,.18f),Cyan,2);
            StaticBox("Lift control panel",new Vector2(x+1.7f,1.3f),new Vector2(.3f,.5f),Cyan,2);
        }
        void Beacon(Vector2 p)
        {
            var a=AncientArt.Display("Checkpoint beacon",AncientArt.World(2),p+Vector2.up*.65f,2);
            a.transform.localScale=Vector3.one*.55f;
        }
        public void Platform(float left,float right,float top)
        {
            bool tall=top<=1;float depth=tall?3.9375f:1;
            var floor=new GameObject("Solid detailed rooftop");floor.layer=8;
            floor.transform.position=new Vector2((left+right)/2,top-depth*.5f);
            var collider=floor.AddComponent<BoxCollider2D>();collider.size=new Vector2(right-left,depth);
            for(float x=left;x<right;x+=6)
            {
                float width=Mathf.Min(6,right-x);
                WorldBatch.Sprite(AncientArt.Rooftop(width,tall),new Vector2(x+width/2,top),Vector2.one,Color.white,0);
                if(width>2)
                {
                    StaticBox("Wet cyan glint",new Vector2(x+1.3f,top-.015f),new Vector2(.6f,.025f),new Color(.3f,.75f,.85f,.5f),3);
                    StaticBox("Wet amber glint",new Vector2(x+width-.6f,top-.02f),new Vector2(.35f,.02f),new Color(1,.5f,.2f,.35f),3);
                }
            }
        }
        public void Ceiling(float left,float right,float underside)
        {
            const float thickness=32;
            var deck=Box("Solid wire ceiling",new Vector2((left+right)*.5f,underside+thickness*.5f),
                new Vector2(right-left,thickness),new Color(.075f,.13f,.18f),1);
            deck.layer=8;deck.AddComponent<BoxCollider2D>();
            StaticBox("Ceiling cyan underside",new Vector2((left+right)*.5f,underside+.025f),
                new Vector2(right-left,.05f),new Color(.12f,.62f,.7f),3);
            for(float x=left+.5f;x<right;x+=2)
            {
                StaticBox("Ceiling structural rib",new Vector2(x,underside+.325f),
                    new Vector2(.12f,.65f),new Color(.15f,.24f,.29f),2);
            }
            for(float x=left+1.5f;x<right-1;x+=4)
            {
                Vector2 point=new Vector2(x,underside-.025f);anchors.Add(point);
                WorldBatch.Sprite(AncientArt.World(2),point,Vector2.one*.32f,Color.white,4);
            }
            // Layer 8 makes the whole underside pickable by the existing surface raycast,
            // and blocks both the hero and projectiles rather than being a decorative roof.
        }
        public GameObject Box(string name,Vector2 pos,Vector2 size,Color color,int order)
        {
            var o=new GameObject(name);o.transform.position=pos;o.transform.localScale=new Vector3(size.x,size.y,1);
            var s=o.AddComponent<SpriteRenderer>();s.sprite=square;s.color=color;s.sortingOrder=order;return o;
        }
        public LineRenderer Line(string name,Color color,float width,int order=15)
        {
            var line=new GameObject(name).AddComponent<LineRenderer>();line.sharedMaterial=lineMaterial;
            line.startColor=line.endColor=color;line.startWidth=line.endWidth=width;line.positionCount=2;line.sortingOrder=order;
            return line;
        }
        public void StaticBox(string name,Vector2 pos,Vector2 size,Color color,int order)
        {WorldBatch.Box(pos,size,color,order);}
        public void Slash(Vector2 p,float facing,bool heavy)
        {slashPool[slashCursor++%slashPool.Length].Play(p,facing,heavy);}
        public void SpawnEnemy(float x,float feet,int kind)
        {
            var e=new GameObject(kind==3?"Sanctum Warden":"Sentinel "+kind).AddComponent<AncientEnemy>();
            e.Initialize(kind);e.transform.position=new Vector2(x,feet);enemies.Add(e);
            if(kind==3)boss=e;
        }
        public void SpawnPickup(Vector2 p,int core=-1,bool transient=false)
        {
            if(core>=0&&(coreMask&(1<<core))!=0)return;
            var item=new GameObject(core>=0?"Ancient core":"Particle cache").AddComponent<AncientPickup>();
            item.transform.position=p;item.core=core;item.Transient=transient;pickups.Add(item);
        }
        public void Burst(Vector2 pos,Color color,int count=16)
        {
            int cap=AncientVisuals.HighQuality?96:48;count=Mathf.Min(count,AncientVisuals.HighQuality?48:10);
            for(int i=0;i<count;i++)
            {
                var f=fragmentPool[fragmentCursor++%cap];
                f.Play(pos,color,Random.insideUnitCircle*8,Random.Range(.035f,.10f));
            }
        }
        public void Afterimage(SpriteRenderer source)
        {if(AncientVisuals.HighQuality)ghostPool[ghostCursor++%ghostPool.Count].Play(source);}
        public void HitEmphasis(bool heavy)
        {hitPauseUntil=Mathf.Max(hitPauseUntil,Time.unscaledTime+(heavy?.045f:.022f));}
        public void DamageEmphasis()
        {damageFlash=.65f;HitEmphasis(true);}
        public void Impact(Vector2 pos,Color color,bool heavy)
        {impactPool[impactCursor++%impactPool.Length].Play(pos,color,heavy);}
        public void Notice(string text,float seconds=3){message=text;messageUntil=Time.unscaledTime+seconds;}
        public void Shake(float amount){shake=Mathf.Max(shake,amount);}
        public void Sound(int cue){if(audioCues)audioCues.Play(cue);}
        public void Save()
        {
            SetSaved("storyCores",coreMask);SetSaved("credits",credits);SetSaved("storyCheckpoint",checkpointStage);
            SetSaved("storyBoss",bossDefeated?1:0);SetSaved("kills",kills);SetSaved("score",score);
            if(hero)hero.SaveUpgrades();if(Finale)Finale.WriteSave();if(Campaign)Campaign.WriteSave();if(Production)Production.WriteSave();PlayerPrefs.Save();
        }
        public void Checkpoint(int stage)
        {
            if(stage<=checkpointStage)return;
            checkpointStage=stage;checkpoint=stage==1?new Vector2(46,2.3f):stage==2?new Vector2(86,1.3f):new Vector2(132,1.3f);
            hero.hp=hero.MaxHP;hero.particles=Mathf.Max(hero.particles,60);Save();
            Burst(checkpoint,Cyan,28);Notice("체크포인트 / 체력을 회복하고 진행을 저장했습니다.",4);
        }
        public void SetLiftCheckpoint(int floor,Vector2? arrival=null)
        {
            checkpointStage=floor==0?4:3;checkpoint=arrival??new Vector2(AncientCityLayout.Entry(floor),1.3f);SetSaved("cityFloor",floor);
            SetSaved("retryActive",1);SetSaved("retryFloor",floor);SetSaved("retryX",Mathf.RoundToInt(checkpoint.x*100));SetSaved("retryY",Mathf.RoundToInt(checkpoint.y*100));Save();
        }
        public void CombatCheckpoint(int floor,Vector2 point)
        {
            if(!AncientCityLayout.SafeResume(floor,point.x,point.y))return;
            checkpoint=point;SetSaved("retryActive",1);SetSaved("retryFloor",floor);
            SetSaved("retryX",Mathf.RoundToInt(point.x*100));SetSaved("retryY",Mathf.RoundToInt(point.y*100));Save();
        }
        public void ClearBattleProjectiles()
        {
            // Remove returns pooled shots and mutates this list; victory does not cancel the player's attack.
            for(int i=shots.Count-1;i>=0;i--){var shot=shots[i];if(shot)shot.Remove();else shots.RemoveAt(i);}
        }
        public void HeroDied()
        {
            if(Production&&Production.Retry())return;
            if(Campaign&&Campaign.Retry())return;
            if(Finale&&Finale.Retry())return;
            if(boss){enemies.Remove(boss);Destroy(boss.gameObject);boss=null;}
            if(City)City.OnEnterFloor();
            foreach(var shot in shots.ToArray())if(shot)shot.Remove();
            hero.TravelTo(checkpoint);
            foreach(var vehicle in parkedBikes)if(vehicle)vehicle.ResetAtSpawn();
            if(Story)Story.ResetChaseBikeForRetry();
            SnapCameraToHero();Save();Notice("다시 도전 / 체력 회복 · 코어와 수사 진행은 유지됩니다.",2);
        }
        public void BeginSession()
        {
            MainMenu=false;skills=false;HasSession=true;Time.timeScale=1;inputReadyFrame=Time.frameCount+1;
            SetSaved("started",1);PlayerPrefs.Save();
            Story.Begin();
        }
        public void SnapCameraToHero()
        {
            cameraFollow=new Vector3(hero.transform.position.x+4,Mathf.Max(4.6f,hero.transform.position.y+1),-10);
            Vector2 productionFocus;float productionZoom;
            if(Production&&Production.TryCamera(out productionFocus,out productionZoom))
            {cameraFollow=new Vector3(productionFocus.x,productionFocus.y,-10);view.orthographicSize=productionZoom;}
            view.transform.position=cameraFollow;
        }
        public void BlockGameplayInput(){inputReadyFrame=Time.frameCount+1;}
        public void ReturnToMainMenu()
        {
            if(HasSession)Save();skills=false;MainMenu=true;Time.timeScale=0;hitPauseUntil=0;shake=0;
        }
        void ToggleSkills(){if(MainMenu)return;skills=!skills;Time.timeScale=Paused?0:1;}
        void Update()
        {
            smoothFrameTime=Mathf.Lerp(smoothFrameTime,Mathf.Max(.001f,Time.unscaledDeltaTime),.05f);
            bool guardian=ArenaActive&&!boss.IsDistrictBoss;
            if(guardian!=patrolArenaClosed)
            {patrolArenaClosed=guardian;for(int side=0;side<2;side++)if(patrolArenaSeals[side])patrolArenaSeals[side].enabled=patrolArenaSolids[side].enabled=guardian;}
            if(productionCordon&&Story&&productionCordon.activeSelf!=(Story.Stage>=8))productionCordon.SetActive(Story.Stage>=8);
            if(MainMenu){Time.timeScale=0;return;}
            if(Story&&Story.Stage<5)checkpoint=new Vector2(AncientProductionLayout.TutorialCheckpointX(Story.Stage),AncientProductionLayout.TutorialCheckpointY(Story.Stage));
            if(Production&&Production.Cinematic&&GameplayInputReady&&!(Story&&Story.DialogueOpen)&&(Input.GetKeyDown(KeyCode.K)||Input.GetKeyDown(KeyCode.Escape))){ToggleSkills();BlockGameplayInput();}
            if(Story&&(Story.DialogueOpen||Story.LiftBusy)||(Journal&&Journal.Open)||(Finale&&Finale.Cinematic)||(Production&&(Production.Cinematic||Production.Active&&!Application.isFocused))){Time.timeScale=0;return;}
            if(GameplayInputReady&&(Input.GetKeyDown(KeyCode.K)||Input.GetKeyDown(KeyCode.Escape)))ToggleSkills();
            if(Input.GetKeyDown(KeyCode.H))showHelp=!showHelp;
            Time.timeScale=Paused?0:Time.unscaledTime<hitPauseUntil?.12f:1;
            if(Paused)return;
            damageFlash=Mathf.MoveTowards(damageFlash,0,Time.unscaledDeltaTime*2.8f);
            if(hero)
            {
                float health=hero.bike?hero.bikeHP:hero.hp;
                if(healthEchoBike!=hero.bike){healthEchoBike=hero.bike;healthEcho=health;}
                healthEcho=health>=healthEcho?health:Mathf.MoveTowards(healthEcho,health,Time.unscaledDeltaTime*34);
            }
            if(!skills && !won)runTime+=Time.deltaTime;

        }
        void LateUpdate()
        {
            if(!hero)return;
            float speed=Mathf.Abs(hero.HorizontalSpeed);
            float zoom=hero.Wired?7.5f:hero.bike?6.8f:5.8f;
            float arenaLeft=0,arenaRight=0;bool arena=ArenaActive&&!(Finale&&Finale.Active);
            if(arena)
            {
                arenaLeft=boss.IsDistrictBoss?AncientCityLayout.BossArenaLeft(boss.DistrictFloor,boss.IsSecretary):98;
                arenaRight=boss.IsDistrictBoss?AncientCityLayout.BossArenaRight(boss.DistrictFloor,boss.IsSecretary):117;
                zoom=Mathf.Max(5.8f,(arenaRight-arenaLeft+3)/(2*Mathf.Max(.5f,view.aspect)));
            }
            Vector2 dialogueFocus=Vector2.zero;float dialogueZoom=5.8f;bool dialogue=Story&&Story.TryDialogueFrame(out dialogueFocus,out dialogueZoom);
            if(dialogue)zoom=dialogueZoom;
            float lead=hero.Facing*Mathf.Lerp(2.5f,4,Mathf.Clamp01(speed/25));
            bool chapter=Story&&Story.Stage>=8;
            Vector3 target=new Vector3(Mathf.Clamp(hero.transform.position.x+lead,chapter?AncientCityLayout.Left(Story.CurrentFloor)+5:AncientProductionLayout.TutorialLeft+5,chapter?AncientCityLayout.Right(Story.CurrentFloor)-6:113),Mathf.Max(4.6f,hero.transform.position.y+1),-10);
            if(arena)target=new Vector3((arenaLeft+arenaRight)*.5f,4.6f,-10);
            if(Campaign&&Campaign.Active)target=new Vector3(Mathf.Clamp(hero.transform.position.x+lead,Campaign.CameraLeft,Campaign.CameraRight),Mathf.Max(4.6f,hero.transform.position.y+1),-10);
            if(Finale&&Finale.Active)target=new Vector3(Mathf.Clamp(hero.transform.position.x+lead,AncientFinaleRules.Start+5,AncientFinaleRules.Finish+12),Mathf.Max(1.5f,hero.transform.position.y+2),-10);
            Vector2 productionFocus;float productionZoom;
            if(Production&&Production.TryCamera(out productionFocus,out productionZoom))
            {target=new Vector3(productionFocus.x,productionFocus.y,-10);if(!dialogue)zoom=productionZoom;}
            if(dialogue)target=new Vector3(dialogueFocus.x,dialogueFocus.y,-10);
            // A fast ceiling zip can separate the officer from the moving deck faster
            // than a zoom-out lerp. Expand immediately; ease back into the close fight.
            view.orthographicSize=Production&&Production.DeckCombat&&zoom>view.orthographicSize?zoom:
                Mathf.Lerp(view.orthographicSize,zoom,1-Mathf.Exp(-3*Time.unscaledDeltaTime));
            cameraFollow=Vector3.Lerp(cameraFollow,target,1-Mathf.Exp(-7*Time.unscaledDeltaTime));
            var p=cameraFollow;
            if(shake>0&&!skills)
            {
                float phase=Time.unscaledTime*22;
                p+=new Vector3((Mathf.PerlinNoise(phase,17.1f)-.5f)*2,(Mathf.PerlinNoise(43.7f,phase)-.5f)*1.5f,0)*shake;
                shake=Mathf.MoveTowards(shake,0,Time.unscaledDeltaTime);
            }
            float displayPPU=Mathf.Max(1,Screen.height/(view.orthographicSize*2));
            p.x=Mathf.Round(p.x*displayPPU)/displayPPU;p.y=Mathf.Round(p.y*displayPPU)/displayPPU;view.transform.position=p;
        }
        void OnApplicationQuit(){if(HasSession)Save();}
        void OnApplicationFocus(bool focused)
        {
            if(!HasSession)return;
            if(!focused)Time.timeScale=0;
            else BlockGameplayInput();
        }
        void OnDestroy(){Time.timeScale=1;AncientHUD.Dispose();if(lineMaterial)Destroy(lineMaterial);}
        void Panel(Rect r,Color c){AncientHUD.Panel(r,c);}
        void Bar(Rect r,float value,Color c)
        {
            float pad=r.height<=6?1:2;
            Panel(r,new Color(.04f,.07f,.10f,.95f));
            Panel(new Rect(r.x+pad,r.y+pad,(r.width-pad*2)*Mathf.Clamp01(value),r.height-pad*2),c);
        }
        void CaptureBar(Rect r)
        {
            Bar(r,hero.CaptureEnergy/100f,hero.CaptureEnergy>=100?new Color(.65f,.95f,1):Cyan);
            for(int stage=1;stage<=2;stage++)Panel(new Rect(r.x+2+(r.width-4)*stage*.4f,r.y+1,1,r.height-2),new Color(.8f,.96f,1,.8f));
        }
        public void NewRun()
        {
            foreach(string key in ProgressSaveKeys)PlayerPrefs.DeleteKey(SaveKey+key);
            PlayerPrefs.Save();startAfterReload=true;Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        void Upgrade(Rect rect,string text,int price,int required,bool owned,System.Action unlock)
        {
            GUI.enabled=!owned&&credits>=price&&Cores>=required;
            if(GUI.Button(rect,owned?text+" / 보유":text+" / "+price+" 재화 / "+required+" 코어"))
            {credits-=price;unlock();Save();Notice("장비를 강화했습니다.");}
            GUI.enabled=true;
        }
        void OnGUI()
        {
            if(!hero||MainMenu||(Story&&(Story.DialogueOpen||Story.LiftBusy))||(Journal&&Journal.Open)||(Finale&&Finale.Cinematic)||(Production&&Production.Cinematic&&!skills))return;
            if(!skills&&!won&&Event.current.type!=EventType.Repaint)return;
            GUIStyle oldButton=GUI.skin.button;Matrix4x4 oldMatrix=GUI.matrix;Color oldColor=GUI.color;int oldDepth=GUI.depth;bool oldEnabled=GUI.enabled;
            try
            {
                GUI.skin.button=AncientHUD.Button;
                if(small==null)
                {
                    small=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=14,wordWrap=true};title=new GUIStyle(small){fontSize=22,fontStyle=FontStyle.Bold};
                    centered=new GUIStyle(title){alignment=TextAnchor.MiddleCenter};
                }
                hud.Refresh(this);
                float scale=Mathf.Min(2,Mathf.Min(Screen.width/1280f,Screen.height/720f));GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
                float w=Screen.width/scale,h=Screen.height/scale;
                Panel(new Rect(16,16,302,106),new Color(.025f,.045f,.065f,.88f));
                GUI.Label(new Rect(28,21,290,24),"PLUGIN / ANCIENT CIVILIZATION",small);
                float healthMaximum=hero.bike?80:hero.MaxHP,healthCurrent=hero.bike?hero.bikeHP:hero.hp;
                Bar(new Rect(28,49,272,18),healthEcho/healthMaximum,new Color(.48f,.23f,.12f));
                Panel(new Rect(30,51,268*Mathf.Clamp01(healthCurrent/healthMaximum),14),Orange);CaptureBar(new Rect(28,73,272,13));
                GUI.Label(new Rect(28,91,275,25),hud.Resources,small);
                if(measuredObjective!=hud.Objective)
                {measuredObjective=hud.Objective;objectiveMeasure.text=hud.Objective;objectiveHeight=Mathf.Clamp(small.CalcHeight(objectiveMeasure,260),55,112);}
                Panel(new Rect(w-300,16,284,objectiveHeight+19),new Color(.025f,.045f,.065f,.88f));
                GUI.Label(new Rect(w-286,24,260,objectiveHeight),hud.Objective,small);
                GUI.Label(new Rect(28,128,300,30),hud.Movement,small);
                float statusY=37+objectiveHeight;
                GUI.Label(new Rect(w-300,statusY,284,23),hud.Performance,small);
                GUI.Label(new Rect(w-300,statusY+25,284,23),hud.Mode,small);
                GUI.Label(new Rect(w-300,statusY+49,284,24),hud.Capture,small);
                CaptureBar(new Rect(w-300,statusY+76,284,7));
                if(hero.AttackMode&&!hero.bike&&hero.Combat&&!hero.Combat.Charging)
                {GUI.color=new Color(1,.88f,.3f);GUI.Label(new Rect(w-300,statusY+87,284,24),hud.Blade,small);GUI.color=Color.white;}
                foreach(var enemy in enemies)
                {
                    if(!enemy)continue;
                    if(enemy.IsTelegraphing)
                    {
                        Vector3 tell=view.WorldToScreenPoint(enemy.AimPoint+Vector2.up*1.8f);
                        if(tell.z>0&&tell.x>0&&tell.x<Screen.width)
                        {
                            Color tellColor=enemy.AttackIsPhysical?Orange:Cyan;
                            Bar(new Rect(tell.x/scale-28,(Screen.height-tell.y)/scale,56,5),enemy.WindupProgress,tellColor);
                            GUI.color=tellColor;GUI.Label(new Rect(tell.x/scale-30,(Screen.height-tell.y)/scale-21,90,22),enemy.AttackIsPhysical?"회피":"파괴 / 회피",small);GUI.color=Color.white;
                        }
                    }
                    if(enemy.Recovering)
                    {
                        Vector3 opening=view.WorldToScreenPoint(enemy.AimPoint+Vector2.up*1.8f);
                        if(opening.z>0&&opening.x>0&&opening.x<Screen.width)
                        {
                            GUI.color=new Color(1,.85f,.2f);GUI.Label(new Rect(opening.x/scale-35,(Screen.height-opening.y)/scale-22,110,24),"빈틈 · 공격",small);GUI.color=Color.white;
                            Bar(new Rect(opening.x/scale-28,(Screen.height-opening.y)/scale,56,5),enemy.RecoveryRemaining,new Color(1,.85f,.2f));
                        }
                    }
                    if(enemy.Staggered)
                    {
                        Vector3 stagger=view.WorldToScreenPoint(enemy.AimPoint+Vector2.up*1.8f);
                        if(stagger.z>0&&stagger.x>0&&stagger.x<Screen.width)
                        {GUI.color=Cyan;GUI.Label(new Rect(stagger.x/scale-45,(Screen.height-stagger.y)/scale-20,120,24),"경직 · 공격",small);GUI.color=Color.white;}
                    }
                    if(enemy==boss || enemy.HealthRatio>=1)continue;
                    Vector3 screen=view.WorldToScreenPoint(enemy.AimPoint+Vector2.up*1.1f);
                    if(screen.z>0 && screen.x>=0 && screen.x<Screen.width)
                        Bar(new Rect(screen.x/scale-22,(Screen.height-screen.y)/scale,44,5),enemy.HealthRatio,Orange);
                }
                if(boss)
                {
                    GUI.Label(new Rect(w/2-220,h-104,440,25),hud.Boss,centered);
                    Bar(new Rect(w/2-240,h-72,480,16),boss.HealthRatio,Orange);
                }
                if(Time.unscaledTime<messageUntil&&!skills)
                {
                    Panel(new Rect(w/2-270,100,540,68),new Color(.025f,.045f,.065f,.84f));
                    GUI.Label(new Rect(w/2-256,106,512,60),message,small);
                }
                if(showHelp&&!skills&&!boss&&!won)
                {
                    Panel(new Rect(16,h-93,w-32,77),new Color(.025f,.045f,.065f,.88f));
                    GUI.Label(new Rect(28,h-87,w-56,70),"A/D 이동 · Ctrl 달리기 · Space 점프 · E 상호작용 · Tab 순찰 기록 · H 안내 닫기\n휠 위 공격 / 아래 와이어 · 좌클릭 베기/견인 · 길게 조준 후 놓아 레이저 · 우클릭 입자 수집 · 가운데 클릭 스윙",small);
                }
                if(skills)
                {
                    Panel(new Rect(0,0,w,h),new Color(0,.015f,.03f,.75f));
                    float x=w/2-260,y=Mathf.Max(20,h/2-240);
                    Panel(new Rect(x,y,520,515),new Color(.035f,.065f,.09f,.98f));
                    GUI.Label(new Rect(x+25,y+20,470,35),"입자 장비 정비",title);
                    GUI.Label(new Rect(x+25,y+57,470,52),"기술 재화: "+credits+"   /   에너지 코어: "+Cores+"\n코어를 회수하고 기술 재화를 모아 장비를 강화합니다.",small);
                    if(GUI.Button(new Rect(x+25,y+122,470,35),"F8 / 프레임 목표 "+Performance.TargetLabel+" · F9 성능 표시"))Performance.CycleTarget();
                    Upgrade(new Rect(x+25,y+174,470,42),"공중 스텝 / 2단 점프",10,1,hero.doubleJump,()=>hero.doubleJump=true);
                    Upgrade(new Rect(x+25,y+226,470,42),"입자 도관 / 수집량 +25%",12,2,hero.reservoir,()=>hero.reservoir=true);
                    Upgrade(new Rect(x+25,y+278,470,42),"강화 장갑 / 최대 체력 125",12,2,hero.armor,()=>{hero.armor=true;hero.hp=hero.MaxHP;});
                    if(GUI.Button(new Rect(x+25,y+340,470,35),"돌아가기 / K 또는 Esc"))ToggleSkills();
                    if(GUI.Button(new Rect(x+25,y+383,470,30),"체크포인트부터 재도전")){skills=false;hero.RetryFromCheckpoint();Time.timeScale=1;BlockGameplayInput();}
                    if(GUI.Button(new Rect(x+25,y+423,470,30),"저장 후 메인 화면"))ReturnToMainMenu();
                    if(GUI.Button(new Rect(x+25,y+463,470,28),"N을 누른 채 클릭하면 진행을 지우고 새 게임")&&Input.GetKey(KeyCode.N))NewRun();
                }
                if(!skills&&!won)
                {
                    Bar(new Rect(28,156,74,4),(hero.Combat?hero.Combat.ChargeProgress:0),Cyan);
                    GUI.Label(new Rect(28,162,90,22),"좌클릭 / 조준",small);
                    Bar(new Rect(120,156,74,4),hero.CaptureEnergy/100f,Orange);
                    GUI.Label(new Rect(120,162,100,22),"입자",small);
                    if(!hero.bike&&!boss)GUI.Label(new Rect(220,162,90,22),showHelp?"H 닫기":"H 조작키",small);
                    if(hero.bike)
                    {
                        Bar(new Rect(220,156,84,4),hero.bikeHP/80,Cyan);
                        GUI.Label(new Rect(220,162,120,22),hud.Speed,small);
                    }
                    AncientPickup nearest=null;float distance=float.MaxValue;
                    foreach(var item in pickups)if(item&&item.core>=0)
                    {float d=Vector2.Distance(item.transform.position,hero.transform.position);if(d<distance){distance=d;nearest=item;}}
                    if(nearest)
                    {
                        Vector3 point=view.WorldToScreenPoint(nearest.transform.position+Vector3.up*.65f);
                        float x=Mathf.Clamp(point.x/scale,60,w-100),y=Mathf.Clamp((Screen.height-point.y)/scale,185,h-125);
                        hud.Distance(nearest,distance);GUI.color=Orange;GUI.Label(new Rect(x-30,y,145,24),hud.CoreDistance,small);GUI.color=Color.white;
                    }
                }
                if(won&&!skills)
                {
                    Panel(new Rect(w/2-280,h/2-75,560,220),new Color(.02f,.05f,.075f,.96f));
                    GUI.Label(new Rect(w/2-260,h/2-57,520,42),"THE SKY REMEMBERS",centered);
                    GUI.Label(new Rect(w/2-245,h/2-8,490,90),"Ancient Civilization / Chapter 0 complete\nThe fracture was not the end. It was the beginning.\nScore "+score+"    Sentinels defeated "+kills+"    Time "+Mathf.FloorToInt(runTime)+"s",small);
                    if(GUI.Button(new Rect(w/2-180,h/2+98,360,30),"New run (erase this prototype's save)"))NewRun();
                }
            }
            finally
            {GUI.skin.button=oldButton;GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.depth=oldDepth;GUI.enabled=oldEnabled;}
        }
    }

    public sealed class AncientHero : MonoBehaviour
    {
        public float hp=100,particles=70,bikeHP;
        public bool running,bike,doubleJump,reservoir,armor;
        public float MaxHP {get{return armor?125:100;}}
        public float MaxParticles {get{return reservoir?150:100;}}
        public bool Wired {get{return wired;}}
        public int WireChain {get{return wireChain;}}
        public float Facing {get{return direction;}}
        public Vector2 WireOrigin {get{if(swingMotion!=null&&swingMotion.Presenting&&wired&&!wireZip&&!bike)return swingMotion.EmitterWorld;return (Vector2)transform.position+new Vector2(direction*(bike?.4f:.14f),bike?.45f:.78f);}}
        public float HorizontalSpeed {get{return body?body.linearVelocity.x:0;}}
        public float VerticalSpeed {get{return body?body.linearVelocity.y:0;}}
        public bool BikeGrounded {get;private set;}
        public float WireCooldown {get{return Mathf.Clamp01(1-(wireReady-Time.time)/.18f);}}
        public bool HasWireTarget {get;private set;}
        public Vector2 WireTarget {get;private set;}
        public bool ZipMode {get;private set;}
        public string WireModeName {get{return AttackMode?"ATTACK":"WIRE";}}
        public bool AttackMode {get;private set;}
        public float CaptureEnergy {get;private set;}
        public int CaptureStage {get{return CaptureEnergy<=0?0:CaptureEnergy<40?1:CaptureEnergy<80?2:3;}}
        public bool Capturing {get{return Input.GetMouseButton(1);}}
        public bool MeleeInterrupted {get{return Time.time<hurtUntil;}}
        public float BikeSkid {get;private set;}
        public AncientCombat Combat {get;private set;}
        LineRenderer chargeRing;
                Rigidbody2D body;SpriteRenderer art;AncientMotorcycle motorcycle;AncientSwingMotion swingMotion;
        CapsuleCollider2D heroCollider;BoxCollider2D bikeCollider;LineRenderer wire,wireGlow,wireTarget,absorb;TrailRenderer trail;
        DistanceJoint2D wireJoint;SpriteRenderer hook;AncientWireParticleBatch wireParticles;
        LineRenderer wireCuff,wireLatch;
        readonly LineRenderer[] wireHelices=new LineRenderer[2];
        MaterialPropertyBlock armProperties;
        static readonly int ArmCharge=Shader.PropertyToID("_ArmCharge"),ArmPulse=Shader.PropertyToID("_ArmPulse");
        Camera aimCamera;
        readonly Vector3[] cablePoints=new Vector3[2];
        readonly Vector2[] reservoirShape=UnitCircle(32),cuffShape=UnitCircle(12),latchShape=UnitCircle(6),zipShape=UnitCircle(4),swingShape=UnitCircle(8);
        readonly Vector3[] reservoirPoints=new Vector3[32],cuffPoints=new Vector3[12],latchPoints=new Vector3[6],zipPoints=new Vector3[4],swingPoints=new Vector3[8];
        bool cableVisible,reservoirVisible,targetVisible,enemyReticle,lastArmBike;int lastHelixCount=192,lastReticleCount=8;
        float lastArmCharge=-1,lastArmPulse=-1,lastReservoirEnergy=-1;
        static Vector2[] UnitCircle(int count)
        {
            var result=new Vector2[count];
            for(int i=0;i<count;i++){float angle=i*Mathf.PI*2/count;result[i]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));}return result;
        }
        static void SetCircle(LineRenderer line,Vector2[] shape,Vector3[] points,Vector2 centre,float radius,float angle)
        {
            float cs=Mathf.Cos(angle),sn=Mathf.Sin(angle);
            for(int i=0;i<shape.Length;i++)
            {Vector2 p=shape[i];points[i]=centre+new Vector2(p.x*cs-p.y*sn,p.x*sn+p.y*cs)*radius;}
            line.SetPositions(points);
        }
        void ShowCable(bool visible)
        {
            if(cableVisible==visible)return;cableVisible=visible;
            wire.enabled=wireGlow.enabled=hook.enabled=wireCuff.enabled=wireLatch.enabled=visible;
            foreach(var helix in wireHelices)helix.enabled=visible;
            if(!visible)wireParticles.Hide();
        }
        readonly Vector3[][] helixSmall={new Vector3[24],new Vector3[48],new Vector3[96],new Vector3[192]};
        readonly float[] helixNormal=new float[192],helixDepth=new float[192];
        AncientParkedBike mountedBike;
        float direction=1,invulnerableUntil,wireReady,lastGrounded=-100;
        float absorbAt=-10,jumpBufferedUntil=-10,conveyorUntil,conveyorDirection,airborneSurface;
        float wireAttachedAt,wireLength,wireMinLength,swingDirection,arcTravel,momentumUntil,lastWireRelease=-10;
        Vector2 previousRadial,wireSurfaceNormal;int wireChain;bool wireZip;
        readonly RaycastHit2D[] zipCastHits=new RaycastHit2D[16];ContactFilter2D zipCastFilter;
        AncientEnemy wireEnemy,wireContactTarget;Transform wireMovingAnchor;Rigidbody2D wireMovingBody;Vector2 wireMovingLocal,wireCarrierVelocity,wireFinishGoal;float wireContactUntil;
        bool wired,airJumpUsed,wireFinishPending;float axis;int combo;
        Sprite[] frames,bladeFrames;
        float actionStarted,actionUntil,hurtUntil,ghostAt,landingUntil,fallSpeed,dustAt;
        bool wasGrounded,cutJump,jumpHeld;float strikeFacing;
        void Awake()
        {
            // Story.Start may save immediately on a new-run reload; upgrades must already be loaded.
            doubleJump=AncientGame.Saved("jump")!=0;
            reservoir=AncientGame.Saved("capacity")!=0;armor=AncientGame.Saved("armor")!=0;hp=MaxHP;
        }
        void Start()
        {
            var g=AncientGame.Instance;aimCamera=g.View;
            ZipMode=true;
            body=gameObject.AddComponent<Rigidbody2D>();body.freezeRotation=true;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;body.interpolation=RigidbodyInterpolation2D.Interpolate;
            wireJoint=gameObject.AddComponent<DistanceJoint2D>();wireJoint.autoConfigureDistance=false;
            wireJoint.autoConfigureConnectedAnchor=false;wireJoint.maxDistanceOnly=true;wireJoint.enabled=false;
            heroCollider=gameObject.AddComponent<CapsuleCollider2D>();heroCollider.size=new Vector2(.72f,2.1f);
            var frictionless=new PhysicsMaterial2D("Free movement"){friction=0,bounciness=0};heroCollider.sharedMaterial=frictionless;
            bikeCollider=gameObject.AddComponent<BoxCollider2D>();bikeCollider.size=new Vector2(2.85f,1.8f);bikeCollider.offset=new Vector2(0,.085f);bikeCollider.edgeRadius=.01f;bikeCollider.sharedMaterial=frictionless;bikeCollider.enabled=false;
            zipCastFilter=new ContactFilter2D{useLayerMask=true,layerMask=1<<8,useTriggers=false};
            frames=new Sprite[8];for(int i=0;i<frames.Length;i++)frames[i]=AncientArt.Hero(i);
            bladeFrames=new Sprite[AncientBladeRules.StepCount*AncientBladeRules.FramesPerStep];
            for(int step=1;step<=AncientBladeRules.StepCount;step++)for(int frame=0;frame<AncientBladeRules.FramesPerStep;frame++)
                bladeFrames[(step-1)*AncientBladeRules.FramesPerStep+frame]=AncientArt.Blade(step,frame);
            art=AncientArt.Display("Animated hero",frames[0],new Vector2(0,-1.05f),10,transform);
            swingMotion=new AncientSwingMotion(art);
            if(AncientVisuals.Instance)AncientVisuals.Instance.Rim(art);
            motorcycle=gameObject.AddComponent<AncientMotorcycle>();motorcycle.Initialize(this);
            armProperties=new MaterialPropertyBlock();
            wire=g.Line("Particle wire core",new Color(.8f,1,1,.95f),.022f,17);
            wireGlow=g.Line("Particle wire glow",new Color(.15f,.6f,1,.17f),.085f,16);
            wireTarget=g.Line("Wire targeting reticle",AncientGame.Cyan,.035f,18);wireTarget.loop=true;wireTarget.positionCount=8;
            wire.positionCount=wireGlow.positionCount=2;
            for(int strand=0;strand<2;strand++)
            {
                var helix=g.Line("Particle wire / double helix "+strand,new Color(.15f,.8f,1,.9f),.028f,18);
                helix.positionCount=192;helix.numCapVertices=0;helix.enabled=false;wireHelices[strand]=helix;
            }
            foreach(var cable in new[]{wire,wireGlow})
            {
                cable.numCapVertices=3;cable.numCornerVertices=2;
                cable.widthCurve=new AnimationCurve(new Keyframe(0,.35f),new Keyframe(.08f,1),new Keyframe(.9f,1),new Keyframe(1,.3f));
            }
            wireCuff=g.Line("Wire emitter cuff",AncientGame.Cyan,.035f,19);wireCuff.loop=true;wireCuff.positionCount=12;
            wireLatch=g.Line("Wire hexagonal latch",new Color(.7f,1,1),.035f,19);wireLatch.loop=true;wireLatch.positionCount=6;
            wireCuff.enabled=wireLatch.enabled=false;
            chargeRing=g.Line("Captured particle reservoir",AncientGame.Cyan,.04f,18);
            chargeRing.loop=true;chargeRing.positionCount=32;chargeRing.enabled=false;
            absorb=g.Line("Absorption stream",AncientGame.Cyan,.04f);
            wire.enabled=wireGlow.enabled=wireTarget.enabled=absorb.enabled=false;
            hook=AncientArt.Display("Particle wire latch",AncientArt.WireParticle(),Vector2.zero,18);hook.transform.localScale=Vector3.one*.18f;hook.enabled=false;
            var dots=new GameObject("Batched flowing wire particles");dots.transform.SetParent(transform,false);
            wireParticles=dots.AddComponent<AncientWireParticleBatch>();wireParticles.Initialize(AncientArt.WireParticle());
            Combat=gameObject.AddComponent<AncientCombat>();Combat.Initialize(this);
            trail=gameObject.AddComponent<TrailRenderer>();trail.material=wire.sharedMaterial;
            trail.startColor=new Color(.2f,.9f,1,.55f);trail.endColor=new Color(.2f,.9f,1,0);
            trail.startWidth=.22f;trail.endWidth=0;trail.time=.12f;trail.sortingOrder=8;trail.emitting=false;
        }
        public void SaveUpgrades()
        {
            AncientGame.SetSaved("jump",doubleJump?1:0);
            AncientGame.SetSaved("capacity",reservoir?1:0);AncientGame.SetSaved("armor",armor?1:0);
        }
        public bool IsOnGround {get{return Grounded();}}
        public bool StandingOn(Rigidbody2D surface){return surface&&Grounded()&&groundBody==surface;}
        float groundSampleTick=-1;Vector2 groundSamplePosition,groundNormal=Vector2.up;bool groundSample;Rigidbody2D groundBody;
        bool Grounded()
        {
            Vector2 point=body?body.position:(Vector2)transform.position;
            if(groundSampleTick==Time.fixedTime&&point==groundSamplePosition)return groundSample;
            groundSampleTick=Time.fixedTime;groundSamplePosition=point;
            // Begin above the feet so an initial overlap does not replace a ramp's normal with Vector2.up.
            // The lower end of this cast matches the original foot probe's reach.
            var hit=Physics2D.BoxCast(point+Vector2.down*(bike?.63f:.85f),new Vector2(bike?2.5f:.58f,.12f),0,Vector2.down,.27f,1<<8);
            groundSample=hit.collider!=null;groundBody=hit.collider?hit.collider.attachedRigidbody:null;groundNormal=hit.collider?hit.normal:Vector2.up;return groundSample;
        }
        bool StableGrounded(Vector2 velocity)
        {
            bool contact=Grounded();Vector2 relative=velocity-(groundBody?groundBody.linearVelocity:Vector2.zero);
            return AncientMovementRules.StableGround(contact,relative.x,relative.y,groundNormal.x,groundNormal.y);
        }
        void ClearJumpInput(){jumpBufferedUntil=-10;cutJump=false;jumpHeld=false;}
        void Update()
        {
            var g=AncientGame.Instance;if(!body)return;
            if(g.won){StopWire(false);body.linearVelocity=Vector2.zero;body.gravityScale=0;absorb.enabled=false;trail.emitting=false;return;}
            if(g.Paused||!g.GameplayInputReady||(g.Sniper&&g.Sniper.InputLocked)){ClearJumpInput();absorb.enabled=false;if(Combat)Combat.CancelInput();return;}
            if(transform.position.y<(g.Finale&&g.Finale.Active?AncientFinaleRules.Road(transform.position.x)-7:-9)){Respawn();return;}
            bool onGround=StableGrounded(body.linearVelocity);BikeGrounded=onGround;
            if(onGround)
            {
                if(!wasGrounded&&fallSpeed<-7){g.Burst((Vector2)transform.position+Vector2.down*.8f,new Color(.4f,.57f,.65f),10);g.Sound(7);landingUntil=Time.time+.10f;g.Shake(.025f);}
                fallSpeed=0;lastGrounded=Time.time;airJumpUsed=false;if(!wired&&Time.time>momentumUntil)wireChain=0;
            }
            else fallSpeed=Mathf.Min(fallSpeed,body.linearVelocity.y);
            wasGrounded=onGround;
            bool productionDriving=bike&&g.Production&&g.Production.AutoDrive;
            axis=productionDriving?1:Input.GetAxisRaw("Horizontal");if(axis!=0)direction=Mathf.Sign(axis);
            jumpHeld=Input.GetKey(KeyCode.Space);
            if(Input.GetKeyDown(KeyCode.Space))jumpBufferedUntil=Time.time+AncientMovementRules.BufferSeconds;
            if(Input.GetKeyUp(KeyCode.Space))cutJump=true;
            if(Input.GetKeyDown(KeyCode.F))absorbAt=Time.time;
            if(Input.GetKeyDown(KeyCode.LeftControl)||Input.GetKeyDown(KeyCode.RightControl))running=!running;
            if(Input.GetKeyDown(KeyCode.E)&&!(g.Production&&g.Production.Active))
            {
                bool used=g.Story.TryProductionStock()||(g.ProductionResidents&&g.ProductionResidents.TryInteract())||(g.Sniper&&g.Sniper.TryInteract())||(g.Campaign&&g.Campaign.TryInteract())||g.Story.TryUseLift()||(g.Finale&&g.Finale.TryInteract())||(g.Cases&&g.Cases.TryInteract())||
                    (g.City&&g.City.TryInteract())||(g.Traversal&&g.Traversal.TryInteract());
                if(!used)
                {
                    if(g.Story.Stage<5)g.Notice("생산장 재고 단말 앞에서 E로 수량을 확인하세요.",2);
                    else if(bike||g.Finale&&g.Finale.Active)InteractBike();
                    else g.Notice("주민·단말·엘리베이터 앞에서 E / 순찰 기록은 Tab",1.5f);
                }
                if(g.Paused||!g.GameplayInputReady){ClearJumpInput();return;}
            }
            if(Input.GetKeyDown(KeyCode.X)&&bike&&!(g.Production&&g.Production.Active))ExplodeBike();
            if(!wired)ZipMode=true;
            bool productionCombatLocked=g.Production&&g.Production.Active&&!g.Production.DeckCombat;
            if(productionCombatLocked){AttackMode=false;if(Combat)Combat.CancelAttacks();}
            // Directional wheel selection never toggles back repeatedly during a long scroll gesture.
            float wheel=Input.mouseScrollDelta.y;
            if(!productionCombatLocked&&Mathf.Abs(wheel)>.001f&&(wheel>0)!=AttackMode)
            {StopWire(false);AttackMode=wheel>0;}
            if(Input.GetMouseButtonDown(1))absorbAt=Time.time;
            if(!AttackMode&&(Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(2)))
            {ZipMode=Input.GetMouseButtonDown(0);FireWire();}
            if(!productionCombatLocked)Combat.HandleInput();
            absorb.enabled=false;
            if(!productionCombatLocked&&Input.GetKey(KeyCode.F))
            {
                // Absorption never lowers speed and never prevents physical melee damage.
                float nearest=7;AncientProjectile target=null;
                foreach(var shot in g.shots)if(shot)
                {float d=Vector2.Distance(transform.position,shot.transform.position);if(d<nearest&&!Physics2D.Linecast(transform.position,shot.transform.position,1<<8)){nearest=d;target=shot;}}
                absorb.enabled=true;absorb.SetPosition(0,transform.position+Vector3.right*direction*.5f);
                if(target){absorb.SetPosition(1,target.transform.position);AbsorbShot(target);}
                else absorb.SetPosition(1,transform.position+new Vector3(direction,Mathf.Sin(Time.time*16)*.07f,0));
            }
            if(Capturing)
            {
                Vector2 point;
                if(Combat.TryCollectionTarget(out point))
                {absorb.enabled=true;absorb.SetPosition(0,transform.position+new Vector3(direction*.5f,.35f,0));absorb.SetPosition(1,point);}
            }
            art.flipX=(Time.time<actionUntil&&!wired?strikeFacing:direction)<0;
            int frame=bike?7:wired?5:(Capturing||Input.GetKey(KeyCode.F)||(Combat&&(Combat.Charging||Combat.Firing)))?6:Mathf.Abs(axis)>.1f?1+(int)(Time.time*(running?13:9))%4:0;
            art.sprite=frames[frame];
            if(!bike&&!wired)
            {
                if(Time.time<hurtUntil)art.sprite=frames[0];
                else if(Time.time<actionUntil)art.sprite=bladeFrames[(combo-1)*AncientBladeRules.FramesPerStep+AncientBladeRules.AnimationFrame(combo,Time.time-actionStarted)];
                else if(Combat&&(Combat.Charging||Combat.Firing))art.sprite=frames[6];
                else if(!onGround)art.sprite=frames[3];
            }
            float squash=Time.time<landingUntil?.08f:0;
            art.transform.localScale=new Vector3(1+squash,1-squash,1);
            if(onGround&&Mathf.Abs(body.linearVelocity.x)>7&&Time.time>=dustAt){g.Burst((Vector2)transform.position+Vector2.down*.8f,new Color(.35f,.48f,.55f),2);dustAt=Time.time+.18f;}
            art.transform.localPosition=new Vector3(0,bike?-.25f:-1.05f,0);
            bool recoverSwing=!bike&&!wired&&!onGround&&Time.time>=hurtUntil&&Time.time>=actionUntil&&!Capturing&&!Input.GetKey(KeyCode.F)&&!(Combat&&(Combat.Charging||Combat.Firing));
            if(swingMotion!=null)swingMotion.Apply(wired&&!wireZip&&!bike,recoverSwing,anchor-(Vector2)transform.position,body.linearVelocity,
                Time.time-wireAttachedAt,arcTravel,swingDirection,Time.time,Time.deltaTime);
            if(wired&&Time.time>=ghostAt){g.Afterimage(art);ghostAt=Time.time+.065f;}
            art.color=Time.time<invulnerableUntil&&(int)(Time.time*14)%2==0?new Color(1,1,1,.45f):Color.white;
            art.enabled=!bike;
            trail.emitting=wired||bike;
        }
        public void HoldForEncounter(bool held)
        {
            if(!body)return;if(held){ResetSwingPresentation();StopWire(false);airborneSurface=0;body.linearVelocity=Vector2.zero;if(Combat)Combat.CancelAttacks();}
            body.simulated=!held;
        }
        void ResetSwingPresentation()
        {if(swingMotion!=null)swingMotion.Clear();if(art){art.transform.localRotation=Quaternion.identity;art.transform.localPosition=new Vector3(0,bike?-.25f:-1.05f,0);art.transform.localScale=Vector3.one;}}
        public void CinematicPose(bool low,float facing)
        {
            if(!art||frames==null)return;ResetSwingPresentation();art.sprite=frames[low?4:0];art.flipX=facing<0;art.enabled=true;art.color=Color.white;
            art.transform.localPosition=new Vector3(0,-1.05f,0);
        }
        public void CinematicLaserPose(float facing)
        {
            if(!art||frames==null)return;ResetSwingPresentation();art.sprite=frames[6];art.flipX=facing<0;art.enabled=true;art.color=Color.white;
            art.transform.localPosition=new Vector3(0,-1.05f,0);
        }
        public void ProductionCinematicRun(float elapsed,float facing)
        {
            if(!art||frames==null)return;ResetSwingPresentation();
            art.sprite=frames[1+Mathf.FloorToInt(Mathf.Max(0,elapsed)*10)%4];art.flipX=facing<0;art.enabled=true;art.color=Color.white;
        }
        public void ProductionCinematicBlade(int step,float progress,float facing)
        {
            if(!art||bladeFrames==null)return;ResetSwingPresentation();step=AncientBladeRules.NormalizeStep(step);
            int frame=Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(progress)*(AncientBladeRules.FramesPerStep-1)),0,AncientBladeRules.FramesPerStep-1);
            art.sprite=bladeFrames[(step-1)*AncientBladeRules.FramesPerStep+frame];art.flipX=facing<0;art.enabled=true;art.color=Color.white;
        }
        public void ProductionCinematicRoll(float degrees)
        {
            if(!art)return;
            Quaternion rotation=Quaternion.Euler(0,0,degrees);art.transform.localRotation=rotation;
            // The authored sprites pivot at the feet; offset them so a backflip turns around the body's centre.
            art.transform.localPosition=rotation*(Vector3.down*1.05f);art.enabled=true;art.color=Color.white;
        }
        public void SetProductionAttackMode(bool attack)
        {StopWire(false);AttackMode=attack;ZipMode=true;if(Combat)Combat.CancelAttacks();}
        public void CinematicMove(Vector2 point)
        {
            body.position=point;transform.position=point;body.linearVelocity=Vector2.zero;
            if(art&&frames!=null)art.sprite=frames[3];
        }
        public void AimWeaponAt(Vector2 target)
        {if(Mathf.Abs(target.x-transform.position.x)>.1f)direction=target.x<transform.position.x?-1:1;}
        public void VentLift()
        {
            if(!body||bike)return;
            Vector2 velocity=body.linearVelocity;velocity.y=AncientCityLayout.VentLift(velocity.y,Time.deltaTime);
            body.linearVelocity=velocity;momentumUntil=Time.time+.25f;cutJump=false;
        }
        public void ConveyorDrift(float direction)
        {
            if(!body||bike||wired)return;
            conveyorDirection=direction;conveyorUntil=Time.time+.08f;momentumUntil=Time.time+.3f;
        }
        public void BladeRebound()
        {
            if(!body||bike||Grounded())return;
            StopWire(false);Vector2 velocity=body.linearVelocity;
            velocity.y=AncientRules.BladeReboundSpeed(velocity.y);body.linearVelocity=velocity;
            // Preserve horizontal momentum so the rebound can feed into the next wire.
            momentumUntil=Time.time+.65f;cutJump=false;jumpBufferedUntil=-10;lastGrounded=-100;
            AncientGame.Instance.Burst((Vector2)transform.position+Vector2.down*.8f,new Color(1,.9f,.2f),7);
        }
        public void BladePose(float facing,int index)
        {combo=AncientBladeRules.NormalizeStep(index);strikeFacing=facing;actionStarted=Time.time;actionUntil=actionStarted+AncientBladeRules.Duration(combo);}
        public Vector2 BladeTipWorld(float animationFrame)
        {
            float frame=Mathf.Clamp(animationFrame,0,AncientBladeRules.FramesPerStep-1);int first=Mathf.FloorToInt(frame),next=Mathf.Min(first+1,AncientBladeRules.FramesPerStep-1);
            Vector2 tip=Vector2.Lerp(AncientBladeAtlas.TipLocal(combo,first),AncientBladeAtlas.TipLocal(combo,next),frame-first);
            if(strikeFacing<0)tip.x=-tip.x;return art.transform.TransformPoint(tip);
        }
        public void CancelBladePose(){actionUntil=0;}
        public void AddCapturedEnergy(float amount){CaptureEnergy=Mathf.Min(100,CaptureEnergy+amount*(reservoir?1.25f:1));var g=AncientGame.Instance;if(g.Sniper)g.Sniper.EnergyChanged();}
        public float SpendCapturedEnergy(){float value=CaptureEnergy;CaptureEnergy=0;return value;}
        public void TravelTo(Vector2 point)
        {
            HoldForEncounter(false);ResetSwingPresentation();
            if(bike)
            {
                if(mountedBike)mountedBike.Park(body.position,direction,bikeHP);
                mountedBike=null;motorcycle.Park();SetBike(false);BikeSkid=0;
            }
            StopWire(false);body.position=point;transform.position=point;body.linearVelocity=Vector2.zero;
            momentumUntil=0;conveyorUntil=0;airborneSurface=0;ClearJumpInput();lastGrounded=-100;groundSampleTick=-1;
            fallSpeed=0;wasGrounded=false;airJumpUsed=false;hurtUntil=actionUntil=landingUntil=0;
            if(trail){trail.emitting=false;trail.Clear();}if(Combat)Combat.ResetCombat();
        }
        Vector2 anchor;
        void FixedUpdate()
        {
            var g=AncientGame.Instance;if(g.won||g.Paused||(g.Sniper&&g.Sniper.InputLocked))return;
            Vector2 velocity=body.linearVelocity;
            bool grounded=StableGrounded(velocity);
            // Physics landings consume a queued press immediately, even between render frames.
            if(grounded)
            {
                lastGrounded=Time.time;airJumpUsed=false;
                airborneSurface=groundBody&&groundBody.bodyType==RigidbodyType2D.Kinematic?groundBody.linearVelocity.x:0;
            }
            if(!bike||wired)BikeSkid=0;
            if(wired)airborneSurface=0;
            if(wired&&wireMovingAnchor)
            {
                Vector2 next=wireMovingAnchor.TransformPoint(wireMovingLocal);
                wireCarrierVelocity=wireMovingBody?wireMovingBody.linearVelocity:Vector2.ClampMagnitude((next-anchor)/Time.fixedDeltaTime,AncientZipRules.Speed);
                anchor=next;if(wireJoint)wireJoint.connectedAnchor=anchor;
            }
            // A destroyed Unity object compares equal to null but still marks an enemy attachment.
            if(wired&&!object.ReferenceEquals(wireEnemy,null))
            {
                if(!wireEnemy||!wireEnemy.IsAlive){StopWire(false);velocity=body.linearVelocity;}
                else anchor=wireEnemy.AimPoint;
            }
            bool zipCompleted=false;
            if(wired&&wireZip&&wireFinishPending)
            {
                wireFinishPending=false;
                // Check the destination of the completed physics step, not the moving
                // latch's newer position. A carrier otherwise stays one step ahead forever.
                Vector2 arrived=wireFinishGoal-body.position;
                if(AncientZipRules.Arrived(arrived.x,arrived.y))
                {
                    float carrier=wireMovingAnchor?wireCarrierVelocity.x:0;
                    StopWire(false);airborneSurface=carrier;velocity=new Vector2(carrier,0);zipCompleted=true;
                }
            }
            if(wired)
            {
                Vector2 delta=anchor-body.position;Vector2 radial=delta.normalized;
                float age=Time.time-wireAttachedAt;
                if(wireZip)
                {
                    Vector2 goal=ZipGoal();Vector2 remaining=goal-body.position;
                    AncientZipRules.Velocity(remaining.x,remaining.y,Time.fixedDeltaTime,out velocity.x,out velocity.y);
                    bool blocked;velocity=ClipZipStep(velocity,out blocked);
                    Vector2 travel=velocity*Time.fixedDeltaTime;
                    bool enemyImpact=wireEnemy&&AncientRules.WireImpactOnStep(delta.x,delta.y,travel.x,travel.y,blocked);
                    if(enemyImpact)
                    {
                        var enemy=wireEnemy;wireContactTarget=enemy;wireContactUntil=Time.time+.3f;
                        enemy.WireImpact(radial.x<0?-1:1);StopWire(false);
                        velocity=new Vector2(-radial.x*2,Mathf.Max(3,velocity.y*.15f));body.linearVelocity=velocity;
                    }
                    else if(blocked||AncientZipRules.Arrived(remaining.x,remaining.y)||age>AncientZipRules.MaximumSeconds||delta.sqrMagnitude>36*36)
                    {StopWire(false);if(!blocked)velocity=Vector2.zero;}
                    else if(!wireEnemy&&AncientZipRules.ReachesThisStep(remaining.x,remaining.y,travel.x,travel.y))
                    {wireFinishPending=true;wireFinishGoal=goal;}
                }
                else
                {
                    // A one-sided distance constraint produces an arc, rather than teleporting to the anchor.
                    wireLength=Mathf.MoveTowards(wireLength,wireMinLength,8*Time.fixedDeltaTime);
                    wireJoint.distance=wireLength;
                    Vector2 tangent=new Vector2(radial.y,-radial.x)*swingDirection;
                    velocity+=(radial*10+tangent*19)*Time.fixedDeltaTime;
                    velocity=Vector2.ClampMagnitude(velocity,30);
                    Vector2 outward=-radial;
                    arcTravel=Mathf.Max(0,arcTravel+Vector2.SignedAngle(previousRadial,outward)*swingDirection);previousRadial=outward;
                    bool occluded=!VisibleAnchor(anchor);
                    bool risingExit=age>.32f && arcTravel>66 && velocity.x*swingDirection>4 && velocity.y>1;
                    if(occluded || age>=1.35f || delta.magnitude<2 || risingExit)
                    {
                        body.linearVelocity=velocity;StopWire(!occluded);velocity=body.linearVelocity;
                    }
                }
            }
            else if(!zipCompleted)
            {
                // Air control remains relative to the support at takeoff. A jump on
                // a moving roof must not silently brake the officer behind the truck.
                float surface=airborneSurface;
                bool reversing=bike&&grounded&&Mathf.Abs(axis)>.1f&&axis*velocity.x<0&&Mathf.Abs(velocity.x)>3;
                BikeSkid=Mathf.MoveTowards(BikeSkid,reversing?Mathf.Clamp01(Mathf.Abs(velocity.x)/10):0,Time.fixedDeltaTime*7);
                if(bike)
                {
                    bool automatic=g.Production&&g.Production.AutoDrive;
                    bool brake=!automatic&&(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow));
                    float target=automatic?g.Production.DriveSpeed:(brake?0:axis*17)+surface;
                    float rate=brake?60:Mathf.Abs(axis)<.01f?11:reversing?23:28;
                    if(!grounded)rate=brake?12:8;
                    bool carry=!automatic&&!grounded&&!brake&&Time.time<momentumUntil&&Mathf.Abs(velocity.x)>17&&(axis==0||Mathf.Sign(axis)==Mathf.Sign(velocity.x));
                    if(automatic)velocity.x=target;
                    else if(!carry&&(axis!=0||grounded||brake))velocity.x=Mathf.MoveTowards(velocity.x,target,rate*Time.fixedDeltaTime);
                }
                else
                {
                    float speed=running?9:5;
                    bool carry=!grounded&&Time.time<momentumUntil&&Mathf.Abs(velocity.x)>speed&&(axis==0||Mathf.Sign(axis)==Mathf.Sign(velocity.x));
                    float belt=grounded&&Time.time<conveyorUntil?conveyorDirection*3.5f:0;
                    if(!carry&&(axis!=0||grounded))velocity.x=Mathf.MoveTowards(velocity.x,axis*speed+belt+surface,(grounded?70:22)*Time.fixedDeltaTime);
                }
            }
            AncientJumpKind jump=AncientMovementRules.ChooseJump(Time.time,jumpBufferedUntil,lastGrounded,doubleJump,airJumpUsed);
            if(jump!=AncientJumpKind.None)
            {
                StopWire(false);velocity.y=AncientMovementRules.LaunchSpeed(jump,jumpHeld||bike);
                lastGrounded=-100;jumpBufferedUntil=-10;cutJump=false;
                if(jump==AncientJumpKind.Air){airJumpUsed=true;g.Burst(transform.position,AncientGame.Cyan,10);}
            }
            if(cutJump&&!wired&&!bike)velocity.y=AncientMovementRules.ReleaseSpeed(velocity.y);
            cutJump=false;body.gravityScale=wired?(wireZip?0:1):(velocity.y<0?1.2f:1);
            if(!wired||!wireZip)velocity.y=Mathf.Max(velocity.y,-25);body.linearVelocity=velocity;
            if(g.ArenaActive)
            {
                Vector2 p=body.position;p.x=Mathf.Clamp(p.x,g.boss.IsDistrictBoss?AncientCityLayout.BossArenaLeft(g.boss.DistrictFloor,g.boss.IsSecretary):98,g.boss.IsDistrictBoss?AncientCityLayout.BossArenaRight(g.boss.DistrictFloor,g.boss.IsSecretary):117);body.position=p;
            }
        }
        Vector2 ZipGoal()
        {
            if(wireEnemy)return anchor;
            Vector2 normal=wireSurfaceNormal;
            if(normal.sqrMagnitude<.1f)normal=(body.position-anchor).normalized;
            return anchor+normal*AncientZipRules.Support(normal.x,normal.y,bike);
        }
        Vector2 ClipZipStep(Vector2 velocity,out bool blocked)
        {
            blocked=false;float length=velocity.magnitude*Time.fixedDeltaTime;if(length<.00001f)return velocity;
            Vector2 radial=velocity.normalized;float safe=length;
            int count=body.Cast(radial,zipCastFilter,zipCastHits,length+AncientZipRules.Skin);
            for(int i=0;i<count;i++)
            {
                var hit=zipCastHits[i];if(!hit.collider)continue;
                if(hit.distance<.005f)
                {
                    Vector2 closest=hit.collider.ClosestPoint(body.position)-body.position;
                    if(!AncientZipRules.BlocksInitialContact(closest.x,closest.y,radial.x,radial.y))continue;
                }
                else if(Vector2.Dot(hit.normal,radial)>=-.001f)continue;
                safe=Mathf.Min(safe,AncientZipRules.SafeDistance(length,hit.distance));
            }
            blocked=safe<length-.0001f;
            return radial*(safe/Time.fixedDeltaTime);
        }
        bool VisibleAnchor(Vector2 point)
        {
            Vector2 hand=(Vector2)transform.position+Vector2.up*.35f;
            RaycastHit2D hit=Physics2D.Linecast(hand,point,1<<8);
            return !hit || Vector2.Distance(hit.point,point)<.15f;
        }
        bool PickAnchor(out Vector2 selected)
        {
            selected=Vector2.zero;var camera=aimCamera;if(!camera)return false;
            Vector2 pixel=Input.mousePosition;
            if(pixel.x<0||pixel.y<0||pixel.x>=Screen.width||pixel.y>=Screen.height)return false;
            Vector2 hand=(Vector2)transform.position+Vector2.up*.35f;
            Vector2 mouse=camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 aim=(mouse-hand).normalized;
            float snapPixels=58*Mathf.Clamp(Screen.height/900f,.8f,1.4f);
            float best=snapPixels;bool found=false;
            foreach(var point in AncientGame.Instance.anchors)
            {
                float distance=(hand-point).sqrMagnitude;
                if(distance<2.2f*2.2f||distance>22*22||(!ZipMode&&point.y<transform.position.y+1))continue;
                Vector3 screen=camera.WorldToScreenPoint(point);if(screen.z<=0)continue;
                float score=Vector2.Distance(pixel,new Vector2(screen.x,screen.y));if(score>=best||!VisibleAnchor(point))continue;
                best=score;selected=point;found=true;
            }
            // Surface picking is limited to the cursor neighbourhood, rather than a remote point on its ray.
            float tolerance=snapPixels/(Screen.height/(camera.orthographicSize*2));
            if(aim.sqrMagnitude>.1f)
            {
                float reach=Mathf.Min(22,Vector2.Distance(hand,mouse)+tolerance);
                RaycastHit2D hit=Physics2D.Raycast(hand,aim,reach,1<<8);
                if(hit && (ZipMode||hit.point.y>transform.position.y+2.3f) && Vector2.Distance(hit.point,hand)>2.5f)
                {
                    Vector3 screen=camera.WorldToScreenPoint(hit.point);
                    float score=Vector2.Distance(pixel,new Vector2(screen.x,screen.y));
                    if(score<best){selected=hit.point;found=true;}
                }
            }
            return found;
        }
        bool PickZipTarget(out Vector2 selected,out AncientEnemy enemy,out Vector2 normal)
        {
            selected=normal=Vector2.zero;enemy=null;var camera=aimCamera;if(!camera)return false;
            Vector2 pixel=Input.mousePosition;
            if(pixel.x<0||pixel.y<0||pixel.x>=Screen.width||pixel.y>=Screen.height)return false;
            Vector2 hand=(Vector2)transform.position+Vector2.up*.35f,mouse=camera.ScreenToWorldPoint(Input.mousePosition),aim=(mouse-hand).normalized;
            if(aim.sqrMagnitude<.1f)return false;
            bool found=false;float score=float.MaxValue;
            // The clicked ray chooses the actual first solid surface. A nearby decorative hook
            // no longer steals a ceiling/wall click via the old wide 58-pixel snap radius.
            var hit=Physics2D.Raycast(hand,aim,AncientZipRules.Range,1<<8);
            if(hit&&Vector2.Distance(hand,hit.point)>.8f)
            {
                selected=hit.point;normal=hit.normal;found=true;
                Vector3 screen=camera.WorldToScreenPoint(selected);score=Vector2.Distance(pixel,new Vector2(screen.x,screen.y));
            }
            float snap=24*Mathf.Clamp(Screen.height/900f,.8f,1.4f);
            foreach(var point in AncientGame.Instance.anchors)
            {
                float distance=(point-hand).sqrMagnitude;if(distance<1||distance>AncientZipRules.Range*AncientZipRules.Range)continue;
                Vector3 screen=camera.WorldToScreenPoint(point);if(screen.z<=0)continue;
                float candidate=Vector2.Distance(pixel,new Vector2(screen.x,screen.y));
                if(candidate>=snap||candidate>=score||!VisibleAnchor(point))continue;
                score=candidate;selected=point;normal=Vector2.zero;found=true;
            }
            foreach(var candidateEnemy in AncientGame.Instance.enemies)
            {
                if(!candidateEnemy||!candidateEnemy.IsAlive)continue;Vector2 point=candidateEnemy.AimPoint;
                if((point-hand).sqrMagnitude>AncientZipRules.Range*AncientZipRules.Range)continue;
                Vector3 screen=camera.WorldToScreenPoint(point);if(screen.z<=0)continue;
                float candidate=Vector2.Distance(pixel,new Vector2(screen.x,screen.y));
                if(candidate>=snap||candidate>=score||!VisibleAnchor(point))continue;
                score=candidate;selected=point;enemy=candidateEnemy;normal=Vector2.zero;found=true;
            }
            return found;
        }
        void FireWire()
        {
            if(Time.time<wireReady)return;
            Vector2 selected,normal=Vector2.zero;var g=AncientGame.Instance;AncientEnemy target=null;
            bool productionBoarding=g.Production&&g.Production.Active&&!g.Production.DeckCombat;
            if(productionBoarding&&(!bike||!ZipMode))return;
            bool picked=ZipMode?PickZipTarget(out selected,out target,out normal):PickAnchor(out selected);
            if(!picked)
            {g.Notice(productionBoarding?"Space로 장애물을 피하세요. 트럭이 가까워지면 위쪽 연결점을 좌클릭하세요.":"적 또는 지형을 겨냥해 좌클릭으로 견인 / 가운데 클릭으로 지형 스윙 / 우클릭은 입자 수집");return;}
            if(productionBoarding)
            {
                if(!TryProductionBoard(selected))g.Notice("트럭이 공격을 멈추고 가까워지면 상단 연결점을 겨냥하세요.",1.5f);
                return;
            }
            AttachWire(selected,target,normal);
        }
        public bool TryProductionBoard(Vector2 selected)
        {
            var g=AncientGame.Instance;Vector2 boardingHook;
            if(!body||!bike||!g||g.Paused||!g.GameplayInputReady||Time.time<wireReady||!g.Production||!g.Production.TryBoardWire(selected,out boardingHook))return false;
            DismountProductionBike();ZipMode=true;AttachWire(boardingHook,null,Vector2.down);return wired;
        }
        void AttachWire(Vector2 selected,AncientEnemy target,Vector2 normal)
        {
            var g=AncientGame.Instance;
            bool different=Vector2.Distance(selected,anchor)>.5f;
            if(wired&&!different&&wireZip==ZipMode)return;
            bool chain=(wired||Time.time-lastWireRelease<1.2f)&&different;
            if(wired)StopWire(false);
            wireFinishPending=false;wireMovingBody=null;wireCarrierVelocity=Vector2.zero;
            wireChain=chain?Mathf.Min(wireChain+1,5):1;
            if(g.Traversal)g.Traversal.TryMovingAnchor(selected,out wireMovingAnchor);
            if(!wireMovingAnchor&&g.Campaign)g.Campaign.TryMovingAnchor(selected,out wireMovingAnchor);
            if(!wireMovingAnchor&&g.Production)g.Production.TryMovingAnchor(selected,out wireMovingAnchor);
            if(!wireMovingAnchor)
            {
                var moving=Physics2D.OverlapCircle(selected,.06f,1<<8);
                if(moving&&moving.attachedRigidbody&&moving.attachedRigidbody.bodyType==RigidbodyType2D.Kinematic)
                    wireMovingAnchor=moving.attachedRigidbody.transform;
            }
            wireMovingLocal=wireMovingAnchor?(Vector2)wireMovingAnchor.InverseTransformPoint(selected):Vector2.zero;
            if(wireMovingAnchor)wireMovingBody=wireMovingAnchor.GetComponent<Rigidbody2D>();
            anchor=selected;wireSurfaceNormal=normal;wireEnemy=target;wired=true;wireZip=ZipMode;airborneSurface=0;wireAttachedAt=Time.time;wireReady=Time.time+(target?.45f:.18f);
            if(wireEnemy)
            {
                wireContactTarget=wireEnemy;wireContactUntil=Time.time+.3f;
                wireEnemy.Hit(AncientRules.WireHitDamage(wireEnemy.IsBoss));
                if(!wireEnemy||!wireEnemy.IsAlive){StopWire(false);return;}
            }
            Vector2 delta=anchor-body.position;Vector2 radial=delta.normalized;
            swingDirection=Mathf.Abs(body.linearVelocity.x)>2?Mathf.Sign(body.linearVelocity.x):direction;
            previousRadial=-radial;arcTravel=0;
            wireLength=delta.magnitude;wireMinLength=Mathf.Min(wireLength,Mathf.Clamp(wireLength*.68f,3.2f,8));
            wireJoint.connectedAnchor=anchor;wireJoint.distance=wireLength;wireJoint.enabled=!wireZip;
            Vector2 velocity;
            if(wireZip)
            {
                Vector2 remaining=ZipGoal()-body.position;
                AncientZipRules.Velocity(remaining.x,remaining.y,Time.fixedDeltaTime,out velocity.x,out velocity.y);
                bool blocked;velocity=ClipZipStep(velocity,out blocked);body.gravityScale=0;
                jumpBufferedUntil=-10;cutJump=false;
            }
            else
            {
                velocity=body.linearVelocity+radial*11+new Vector2(radial.y,-radial.x)*swingDirection*5;
                velocity.x=swingDirection*Mathf.Max(12+Mathf.Min(4,wireChain),velocity.x*swingDirection);
                velocity.y=Mathf.Max(8,velocity.y);
            }
            body.linearVelocity=Vector2.ClampMagnitude(velocity,AncientZipRules.Speed);
            airJumpUsed=false;g.Sound(3);g.Burst(anchor,AncientGame.Cyan,14);g.Burst(transform.position,AncientGame.Cyan,8);g.Shake(.045f);
        }
        void StopWire(bool launch)
        {
            bool wasWired=wired;
            if(wireEnemy){wireContactTarget=wireEnemy;wireContactUntil=Time.time+.3f;}
            wireEnemy=null;wireMovingAnchor=null;wireMovingBody=null;wireFinishPending=false;wireCarrierVelocity=Vector2.zero;wired=false;if(wireJoint)wireJoint.enabled=false;
            if(!wasWired)return;
            lastWireRelease=Time.time;momentumUntil=Time.time+.7f;
            if(launch&&body)
            {
                Vector2 velocity=body.linearVelocity;
                if(!wireZip)
                {
                    velocity.x=swingDirection*Mathf.Max(13,velocity.x*swingDirection);
                    velocity.y=Mathf.Max(5,velocity.y);
                }
                body.linearVelocity=Vector2.ClampMagnitude(velocity,30);
                AncientGame.Instance.Burst(transform.position,AncientGame.Cyan,9);
            }
        }
        void LateUpdate()
        {
            if(!body||!wire)return;var g=AncientGame.Instance;
            float armCharge=CaptureEnergy/100f;
            float armPulse=g.Sniper&&g.Sniper.Clashing?1:Capturing?1:wired?.65f:Combat?Combat.ArmActivity:0;
            if(lastArmCharge!=armCharge||lastArmPulse!=armPulse||lastArmBike!=bike)
            {
                lastArmCharge=armCharge;lastArmPulse=armPulse;lastArmBike=bike;
                armProperties.SetFloat(ArmCharge,armCharge);armProperties.SetFloat(ArmPulse,armPulse);
                art.SetPropertyBlock(armProperties);motorcycle.SetArmPower(armCharge,armPulse);
            }
            bool stored=CaptureEnergy>0&&!g.won;
            if(stored!=reservoirVisible){reservoirVisible=stored;chargeRing.enabled=stored;}
            if(stored)
            {
                float radius=.45f+CaptureEnergy/100f*.85f;
                if(lastReservoirEnergy!=CaptureEnergy)
                {lastReservoirEnergy=CaptureEnergy;chargeRing.startWidth=chargeRing.endWidth=.025f+CaptureEnergy*.0007f;}
                SetCircle(chargeRing,reservoirShape,reservoirPoints,transform.position,radius,Time.time*2);
            }
            bool visible=wired&&!g.won;ShowCable(visible);
            if(visible)
            {
                Vector2 hand=WireOrigin;
                float age=Time.time-wireAttachedAt;float shot=Mathf.Clamp01(age/.055f);
                Vector2 end=Vector2.Lerp(hand,anchor,shot);
                float length=Vector2.Distance(hand,end);
                cablePoints[0]=hand;cablePoints[1]=end;
                wire.SetPositions(cablePoints);wireGlow.SetPositions(cablePoints);
                int samples=AncientWireMath.Samples(length,AncientVisuals.HighQuality);
                int buffer=samples==24?0:samples==48?1:samples==96?2:3;
                Vector2 normal=Vector2.Perpendicular(end-hand).normalized;
                if(lastHelixCount!=samples)
                {lastHelixCount=samples;foreach(var helix in wireHelices)helix.positionCount=samples;}
                for(int strand=0;strand<2;strand++)
                {
                    var points=helixSmall[buffer];
                    AncientWireMath.Fill(length,age,strand,samples,helixNormal,helixDepth);
                    for(int i=0;i<samples;i++)
                    {
                        Vector2 point=Vector2.Lerp(hand,end,i/(float)(samples-1))+normal*helixNormal[i];
                        points[i]=new Vector3(point.x,point.y,helixDepth[i]);
                    }
                    wireHelices[strand].SetPositions(points);
                }
                hook.transform.position=end;hook.transform.rotation=Quaternion.Euler(0,0,age*90);
                hook.transform.localScale=Vector3.one*(.18f+Mathf.Exp(-age*12)*.08f);
                wireParticles.Draw(hand,end,age,AncientVisuals.HighQuality);
                SetCircle(wireCuff,cuffShape,cuffPoints,hand,.16f,-age*3);
                SetCircle(wireLatch,latchShape,latchPoints,end,.22f+Mathf.Exp(-age*9)*.2f,age);
            }
            Vector2 target=Vector2.zero;AncientEnemy enemyTarget=null;
            bool hasTarget=false;
            if(!AttackMode&&!g.Paused&&!g.won)
            {
                Vector2 normal;
                if(ZipMode)hasTarget=PickZipTarget(out target,out enemyTarget,out normal);
                else hasTarget=PickAnchor(out target);
                if(g.Production&&g.Production.Active&&!g.Production.DeckCombat)
                {
                    hasTarget=hasTarget&&bike&&g.Production.CanBoardWire(target);
                    if(hasTarget){target=g.Production.BoardingHook;enemyTarget=null;}
                }
            }
            if(hasTarget&&enemyReticle!=(enemyTarget!=null))
            {enemyReticle=enemyTarget!=null;wireTarget.startColor=wireTarget.endColor=enemyReticle?AncientGame.Orange:AncientGame.Cyan;}
            HasWireTarget=hasTarget;WireTarget=target;
            if(targetVisible!=hasTarget){targetVisible=hasTarget;wireTarget.enabled=hasTarget;}
            if(hasTarget)
            {
                int count=ZipMode?4:8;
                if(lastReticleCount!=count){lastReticleCount=count;wireTarget.positionCount=count;}
                float radius=.43f+Mathf.Sin(Time.time*7)*.045f;
                SetCircle(wireTarget,ZipMode?zipShape:swingShape,ZipMode?zipPoints:swingPoints,target,radius,0);
            }
        }
        public void AbsorbShot(AncientProjectile shot)
        {
            var g=AncientGame.Instance;bool perfect=Time.time-absorbAt<.18f;
            particles=Mathf.Min(MaxParticles,particles+(shot.scrap?18:12)*(perfect?1.5f:1));
            Combat.EmitParticles(shot.transform.position,Mathf.Clamp(shot.damage,8,25)*(perfect?1.5f:1));
            g.Sound(2);g.score+=perfect?30:10;g.Burst(shot.transform.position,AncientGame.Cyan,10);
            if(perfect)g.Notice("정밀 분해 / 추가 입자",.75f);shot.Remove();
        }
        public void HurtContact(AncientEnemy source,float damage)
        {
            if(AncientRules.IgnoreWireContact(source&&source==wireContactTarget,wired&&wireZip&&wireEnemy==source,wireContactUntil,Time.time))return;
            Hurt(damage);
        }
        public void Hurt(float damage)
        {
            var g=AncientGame.Instance;if(Time.time<invulnerableUntil||g.won||(g.Sniper&&g.Sniper.Clashing)||(g.Production&&g.Production.Cinematic))return;
            invulnerableUntil=Time.time+.65f;g.Shake(.14f);
            if(bike)
            {
                bikeHP=Mathf.Max(0,bikeHP-damage);motorcycle.Damage();g.Sound(1);g.DamageEmphasis();g.Impact(transform.position,AncientGame.Orange,false);
                if(bikeHP<=0){if(g.Production&&g.Production.Active)Respawn();else ExplodeBike();}return;
            }
            if(Combat){Combat.CancelInput();Combat.CancelBlade();}actionUntil=0;hurtUntil=Time.time+.18f;
            hp-=damage;g.DamageEmphasis();g.Sound(1);g.Impact(transform.position,AncientGame.Orange,false);g.Burst(transform.position,AncientGame.Orange,9);
            if(hp<=0)Respawn();
        }
        public bool BikeProjectileHit(Vector2 start,Vector2 end)
        {
            if(!bike)return false;
            var bounds=bikeCollider.bounds;
            if(bounds.size.x<.01f)bounds=new Bounds(transform.TransformPoint(bikeCollider.offset),new Vector3(2.87f,1.82f,1));
            bounds.Expand(new Vector3(.14f,.14f,1));Vector2 delta=end-start;
            if(delta.sqrMagnitude<.000001f)return bounds.Contains(start);
            float distance;return bounds.IntersectRay(new Ray(start,delta.normalized),out distance)&&distance<=delta.magnitude;
        }
        void InteractBike()
        {
            var g=AncientGame.Instance;
            if(!Grounded()){g.Notice("착지한 뒤 오토바이에 타거나 내리세요.");return;}
            if(groundBody&&groundBody.bodyType==RigidbodyType2D.Kinematic){g.Notice("움직이는 화물차 위에서는 오토바이를 주차할 수 없습니다.");return;}
            if(bike)
            {
                if(!mountedBike)return;
                Vector2 vehiclePosition=body.position;
                Vector2 exit=vehiclePosition+Vector2.up*.25f;
                foreach(float side in new[]{direction,-direction})
                {
                    Vector2 candidate=vehiclePosition+new Vector2(side*1.85f,.3f);
                    bool ground=Physics2D.Raycast(candidate,Vector2.down,1.5f,1<<8);
                    bool blocked=Physics2D.OverlapCapsule(candidate,heroCollider.size,CapsuleDirection2D.Vertical,0,1<<8);
                    if(ground&&!blocked){exit=candidate;break;}
                }
                StopWire(false);mountedBike.Park(vehiclePosition,direction,bikeHP);mountedBike=null;
                motorcycle.Park();SetBike(false);BikeSkid=0;momentumUntil=0;body.position=exit;body.linearVelocity=Vector2.zero;
                g.Notice("오토바이 주차 / E로 다시 탑승",1.5f);return;
            }
            AncientParkedBike nearest=null;float best=2.8f*2.8f;
            foreach(var vehicle in g.parkedBikes)
            {
                if(!vehicle||!vehicle.Available)continue;
                float distance=((Vector2)vehicle.transform.position-body.position).sqrMagnitude;
                if(distance<best&&!Physics2D.Linecast(body.position,vehicle.transform.position,1<<8)){nearest=vehicle;best=distance;}
            }
            if(!nearest){g.Notice("주차된 오토바이 가까이에서 E를 누르세요.",1.5f);return;}
            Vector2 seat=nearest.transform.position;
            if(Physics2D.OverlapBox(seat+Vector2.up*.085f,new Vector2(2.87f,1.77f),0,1<<8))
            {g.Notice("주변 공간이 확보된 곳에서 탑승하세요.");return;}
            StopWire(false);mountedBike=nearest;bikeHP=nearest.Health;direction=nearest.Facing;
            nearest.Take();SetBike(true);body.position=seat;body.linearVelocity=Vector2.zero;BikeSkid=0;
            momentumUntil=0;motorcycle.Mount();g.Sound(3);
        }
        public void MountProductionBike(AncientParkedBike vehicle,Vector2 point,float speed)
        {
            if(!body||!vehicle)return;
            TravelTo(point);SetProductionAttackMode(false);mountedBike=vehicle;vehicle.Take();SetBike(true);
            body.simulated=true;body.gravityScale=1;body.position=point;transform.position=point;
            body.linearVelocity=new Vector2(speed,0);direction=1;bikeHP=80;BikeSkid=0;
            motorcycle.Mount();
            ClearJumpInput();groundSampleTick=-1;invulnerableUntil=Mathf.Max(invulnerableUntil,Time.time+.4f);
        }
        public void DismountProductionBike()
        {
            if(!body||!bike)return;Vector2 position=body.position,velocity=body.linearVelocity;
            StopWire(false);if(mountedBike)mountedBike.Park(position,1,bikeHP);mountedBike=null;
            motorcycle.Park();SetBike(false);BikeSkid=0;ResetSwingPresentation();ClearJumpInput();
            body.position=position;transform.position=position;body.linearVelocity=velocity;body.gravityScale=1;
            momentumUntil=Time.time+.7f;groundSampleTick=-1;if(Combat)Combat.CancelAttacks();
        }
        void SetBike(bool active)
        {
            groundSampleTick=-1;
            bike=active;heroCollider.enabled=!active;bikeCollider.enabled=active;body.mass=active?1.4f:1;
            if(art)art.enabled=!active;
        }
        public void ExplodeBike()
        {
            if(!bike)return;if(mountedBike)mountedBike.Wreck();mountedBike=null;motorcycle.Detonate();SetBike(false);StopWire(false);momentumUntil=Time.time+.8f;
            var g=AncientGame.Instance;g.Sound(4);g.Burst(transform.position,AncientGame.Orange,45);g.Shake(.3f);
            foreach(var e in g.enemies.ToArray())if(e&&Vector2.Distance(e.AimPoint,transform.position)<5)e.Hit(85,true);
            float launch=Mathf.Abs(body.linearVelocity.x)>1.5f?Mathf.Sign(body.linearVelocity.x):direction;
            body.linearVelocity=new Vector2(launch*Mathf.Clamp(Mathf.Abs(body.linearVelocity.x)*.85f,17,23),19);invulnerableUntil=Time.time+.7f;
            g.Notice("폭발 도약 / 다음 와이어 지점을 연결하세요.",2);
        }
        public void RetryFromCheckpoint(){Respawn();}
        void Respawn()
        {
            mountedBike=null;motorcycle.Park();if(Combat)Combat.ResetCombat();
            StopWire(false);CaptureEnergy=0;AttackMode=false;wireChain=0;momentumUntil=0;actionUntil=0;fallSpeed=0;hp=MaxHP;particles=Mathf.Max(40,particles);SetBike(false);BikeSkid=0;body.linearVelocity=Vector2.zero;
            invulnerableUntil=Time.time+1.5f;AncientGame.Instance.HeroDied();
        }
        void OnDestroy()
        {
            if(wire)Destroy(wire.gameObject);if(wireGlow)Destroy(wireGlow.gameObject);
            foreach(var helix in wireHelices)if(helix)Destroy(helix.gameObject);
            if(wireCuff)Destroy(wireCuff.gameObject);if(wireLatch)Destroy(wireLatch.gameObject);
            if(wireTarget)Destroy(wireTarget.gameObject);if(hook)Destroy(hook.gameObject);
            if(absorb)Destroy(absorb.gameObject);if(chargeRing)Destroy(chargeRing.gameObject);
        }
    }

    public sealed class AncientEnemy : MonoBehaviour
    {
        public int kind;
        public int DistrictFloor {get;private set;}
        public bool IsDistrictBoss {get;private set;}
        AncientSniperEncounter sniperEncounter;AncientProductionPursuit productionEncounter;int cloneSlot=-1;
        public bool IsProductionBoss {get{return productionEncounter;}}
        public void ConfigureProductionCommander(AncientProductionPursuit encounter)
        {productionEncounter=encounter;kind=3;DistrictFloor=0;IsDistrictBoss=false;maxHP=hp=420;dead=false;}
        public void ResetProductionHealth(float ratio)
        {if(!productionEncounter)return;hp=maxHP*Mathf.Clamp(ratio,.001f,1);dead=false;wirePushRemaining=0;resolveAt=-1;volleyRemaining=0;ClearProductionHitFlash();}
        public void ClearProductionHitFlash()
        {if(!productionEncounter)return;flashUntil=Time.time-1;if(art)art.color=Color.white;}
        public bool IsSniperClone {get{return cloneSlot>=1;}}
        public void BindSniperEncounter(AncientSniperEncounter encounter){sniperEncounter=encounter;}
        public void ConfigureSniperClone(AncientSniperEncounter encounter,int slot){sniperEncounter=encounter;cloneSlot=slot;maxHP=hp=60;}
        public bool IsSecretary {get;private set;}
        public void ConfigureSecretary(){IsSecretary=true;maxHP=hp=480;}
        public bool IsBoss {get{return kind==3||IsDistrictBoss;}}
        public void ConfigureDistrict(int floor,bool boss)
        {DistrictFloor=floor;IsDistrictBoss=boss;if(boss)maxHP=hp=floor==4?520:220+floor*50;}
        float hp=60,maxHP=60,nextAttack,resolveAt=-1,flashUntil,walkPhase,faceLockUntil,guardBrokenUntil,staggerUntil;
        int pattern,attackIndex,volleyRemaining;bool dead;
        float volleyAt,volleyInterval=.14f,volleySpeed=12,windupStartedAt,attackPoseUntil,recoveryStarts,recoveryUntil,recoveryDuration;bool enragedAnnounced;
        AncientBossController bossBrain;
        public bool IsTelegraphing {get{return productionEncounter?productionEncounter.CommanderWindup>0:bossBrain?bossBrain.Telegraphing:resolveAt>=0;}}
        public bool Recovering {get{return productionEncounter?productionEncounter.CommanderRecovering:bossBrain?bossBrain.Recovering:IsBoss&&Time.time>=recoveryStarts&&Time.time<recoveryUntil;}}
        public bool Staggered {get{return !IsBoss&&Time.time<staggerUntil;}}
        public float RecoveryRemaining {get{return productionEncounter?productionEncounter.CommanderRecoveryRemaining:bossBrain?bossBrain.RecoveryRemaining:Recovering?Mathf.Clamp01((recoveryUntil-Time.time)/Mathf.Max(.1f,recoveryDuration)):0;}}
        public bool AttackIsPhysical {get{return productionEncounter?productionEncounter.CommanderAttackIsPhysical:bossBrain?bossBrain.Physical:pattern==0;}}
        public float WindupProgress {get{return productionEncounter?productionEncounter.CommanderWindup:bossBrain?bossBrain.WindupProgress:resolveAt<0?0:Mathf.Clamp01((Time.time-windupStartedAt)/Mathf.Max(.01f,resolveAt-windupStartedAt));}}
        SpriteRenderer art;LineRenderer warning;AncientFinalTruck carrier;
        Vector2 home,lockedAim;float facing=1;
        public Vector2 AimPoint {get{return (Vector2)transform.position+Vector2.up*(productionEncounter?1.3f:IsSniperClone||IsDistrictBoss&&DistrictFloor==4&&!IsSecretary?1.5f:kind==3?1.65f:kind==4?.65f:.95f);}}
        public float DialogueHeadHeight {get{return art?Mathf.Clamp(art.bounds.max.y-transform.position.y,.9f,2.5f):2.2f;}}
        public Vector2 SniperMuzzle {get{return AimPoint+Vector2.right*facing*1.42f;}}
        public bool IsAlive {get{return !dead;}}
        float wirePushRemaining;
        public float HealthRatio {get{return maxHP>0?hp/maxHP:1;}}
        public bool Enraged {get{return IsBoss&&!IsSecretary&&!productionEncounter&&hp<maxHP*.5f;}}
        public string Role {get{if(productionEncounter)return "운송대장";if(IsSniperClone)return "에너지 분신";if(IsSecretary)return "중앙행정정보처리기관 비서실장";if(IsDistrictBoss)return DistrictFloor==1?"밀매 조직 경비장":DistrictFloor==2?"기록 집행관":DistrictFloor==3?"코어 보안 병기":"저격수";return kind==0?"LANCER":kind==1?"GUNNER":kind==2?"BOMBER":kind==3?"CARGO ENFORCER":kind==4?"DRONE":"SHIELD WARDEN";}}
        public void FaceToward(float x){facing=x<transform.position.x?-1:1;if(art)art.flipX=facing<0;}
        public void PrepareMiniBoss(){maxHP=hp=240;}
        public void Initialize(int type)
        {kind=type;maxHP=hp=kind==3?560:kind==5?115:kind==4?42:kind==0?70:55;}
        void Start()
        {
            var g=AncientGame.Instance;home=transform.position;carrier=GetComponentInParent<AncientFinalTruck>();
            art=AncientArt.Display(Role,productionEncounter?AncientArt.ProductionCaptain(0):IsSniperClone?AncientArt.Boss(4,cloneSlot>=2?1:0):AncientArt.Enemy(kind),Vector2.zero,8,transform);art.flipX=facing<0;
            if(IsSniperClone){art.sharedMaterial=sniperEncounter.CloneMaterial;art.color=AncientGame.Cyan;}
            if(AncientVisuals.Instance)
            {
                if(!IsSniperClone)AncientVisuals.Instance.Rim(art);
                AncientVisuals.Instance.Glow(new Vector2(0,kind==4?.6f:1.1f),kind==3||kind==5?AncientGame.Cyan:AncientGame.Orange,kind==3?2.5f:1.1f,9,transform);
            }
            warning=g.Line("Attack telegraph",AncientGame.Orange,.045f,6);warning.enabled=false;
            nextAttack=Time.time+(IsDistrictBoss&&DistrictFloor==4?1.6f:kind==3?1.3f:1+kind*.2f);
            if(IsBoss&&!productionEncounter){bossBrain=gameObject.AddComponent<AncientBossController>();bossBrain.Initialize(this,g,warning);BossVisual(0,facing,0,false,false);}
        }
        bool Step(float distance)
        {
            if(productionEncounter)
            {transform.position+=Vector3.right*distance;return true;}
            // A wire impact on a moving guard stays on that truck's deck, never its old world-space home.
            if(IsSecretary)
            {transform.position=new Vector2(Mathf.Clamp(transform.position.x+distance,AncientCityLayout.BossArenaLeft(4,true)+1,AncientCityLayout.BossArenaRight(4,true)-1),transform.position.y);return true;}
            if(carrier)
            {
                Vector3 local=transform.localPosition;local.x=Mathf.Clamp(local.x+distance,-1.7f,1.7f);
                Vector2 next=transform.parent.TransformPoint(local);
                if(Physics2D.Linecast(AimPoint,next+Vector2.up*.95f,1<<8))return false;
                transform.localPosition=local;return true;
            }
            float x=Mathf.Clamp(transform.position.x+distance,IsDistrictBoss?AncientCityLayout.BossArenaLeft(DistrictFloor,IsSecretary)+1:kind==3?100:home.x-2,IsDistrictBoss?AncientCityLayout.BossArenaRight(DistrictFloor,IsSecretary)-1:kind==3?114:home.x+2);
            if(Physics2D.Linecast(AimPoint,new Vector2(x,AimPoint.y),1<<8))return false;
            if(kind!=3&&kind!=4&&!Physics2D.Raycast(new Vector2(x,home.y+.2f),Vector2.down,.5f,1<<8))return false;
            if(kind==4)home.x=x;else transform.position=new Vector2(x,home.y);return true;
        }
        void Update()
        {
            var g=AncientGame.Instance;if(dead||!g.hero||g.won||g.Paused)return;
            if(DistrictFloor>0&&g.Story.CurrentFloor!=DistrictFloor)return;
            if(Mathf.Abs(wirePushRemaining)>.001f)
            {
                float step=Mathf.MoveTowards(0,wirePushRemaining,4*Time.deltaTime);
                if(Step(step))wirePushRemaining-=step;else wirePushRemaining=0;
            }
            if(productionEncounter){productionEncounter.TickCommander(this);return;}
            if(IsSniperClone)return;
            if(bossBrain){bossBrain.Tick();return;}
            // Body contact is separate from telegraphed attacks and projectiles.
            Vector2 contact=(Vector2)g.hero.transform.position-AimPoint;
            if(AncientThreatRules.BodyContactAllowed(IsBoss,Recovering,Time.time<staggerUntil)&&Mathf.Abs(contact.x)<(kind==3?1.1f:.65f)&&Mathf.Abs(contact.y)<1)
                g.hero.HurtContact(this,kind==3?14:8);
            if(Enraged&&!enragedAnnounced)
            {enragedAnnounced=true;g.Sound(8);g.Notice("화물 수호 장치 / 과부하",2);g.Impact(AimPoint,AncientGame.Orange,true);g.Shake(.12f);}
            float anticipation=IsTelegraphing?WindupProgress:0;
            float recoil=Mathf.Clamp01((attackPoseUntil-Time.time)/.18f);
            art.transform.localScale=new Vector3(1-anticipation*.035f,1+anticipation*.035f,1);
            art.transform.localRotation=Quaternion.Euler(0,0,IsTelegraphing?-facing*anticipation*4:facing*recoil*4);
            if(kind==4)transform.position=home+new Vector2(Mathf.Sin(Time.time*.75f+home.x)*2.2f,Mathf.Sin(Time.time*1.8f)*.55f);
            if(volleyRemaining>0&&Time.time>=volleyAt)
            {FireProjectile(lockedAim,volleySpeed,false,0);volleyRemaining--;volleyAt=Time.time+volleyInterval;}
            Vector2 delta=(Vector2)g.hero.transform.position-AimPoint;
            if(resolveAt<0&&Time.time>faceLockUntil){facing=delta.x<0?-1:1;art.flipX=facing<0;}
            if(Time.time<staggerUntil)
            {warning.enabled=false;art.color=AncientGame.Cyan;return;}
            if(resolveAt>=0)
            {
                float pulse=.5f+Mathf.Sin(Time.time*25)*.5f;
                art.color=Time.time<flashUntil?Color.white:Color.Lerp(Color.white,pattern==0?AncientGame.Orange:AncientGame.Cyan,pulse*.7f);
                DrawTelegraph();
                if(Time.time>=resolveAt)
                {
                    resolveAt=-1;warning.enabled=false;ResolveAttack();
                    nextAttack=Time.time+(IsDistrictBoss&&DistrictFloor==4?1.6f:kind==3?(Enraged?.65f:1.15f):kind==5?1.45f:kind==0?1.05f:kind==2?1.8f:kind==4?1.9f:1.6f);
                    if(IsBoss)
                    {
                        recoveryDuration=AncientCombatRules.Recovery(DistrictFloor,pattern,Enraged);
                        recoveryStarts=Time.time+volleyRemaining*volleyInterval;
                        recoveryUntil=recoveryStarts+recoveryDuration;nextAttack=Mathf.Max(nextAttack,recoveryUntil+.12f);
                    }
                }
                return;
            }
            if(Recovering)
            {warning.enabled=false;art.color=Time.time<flashUntil?Color.white:new Color(1,.87f,.5f);return;}
            art.color=Time.time<flashUntil?AncientGame.Cyan:kind==5&&Time.time<guardBrokenUntil?new Color(.65f,.8f,1):Color.white;
            if(kind==0||kind==3||kind==5)
            {
                float stop=kind==3?3.2f:kind==5?2.1f:1.8f;
                if(Mathf.Abs(delta.x)>stop&&Mathf.Abs(delta.x)<18)
                {
                    float speed=kind==3?(Enraged?3.6f:2.7f):kind==5?1.7f:2.4f;
                    Step(Mathf.Sign(delta.x)*speed*Time.deltaTime);walkPhase+=Time.deltaTime;
                    art.transform.localPosition=new Vector2(0,Mathf.Sin(walkPhase*14)*.025f);
                }
            }
            if(Time.time<nextAttack)return;
            // Finish an existing attack even when the camera moves, but never begin an
            // ordinary ranged tell from the unseen half of a long investigation room.
            var threatCamera=g.View;
            Vector3 viewport=threatCamera?threatCamera.WorldToViewportPoint(AimPoint):Vector3.zero;
            if(!AncientThreatRules.CanStartAttack(IsBoss,threatCamera,viewport.x,viewport.y,viewport.z))return;
            if(IsDistrictBoss&&DistrictFloor==4)
            {
                // Long visible aim, then a short burst; reload is the punish window.
                if(delta.magnitude<24&&!Physics2D.Linecast(AimPoint,g.hero.transform.position,1<<8))Windup(1,1.15f);
                return;
            }
            if((kind==0||kind==5)&&delta.magnitude<3.5f)Windup(0,kind==5?.65f:.55f);
            else if(kind==3)
            {
                pattern=attackIndex++%3==0&&Mathf.Abs(delta.x)<6?0:1;
                Windup(pattern,pattern==0?(Enraged?.48f:.7f):.65f);
            }
            else if(kind!=0&&kind!=5&&delta.magnitude<20&&!Physics2D.Linecast(AimPoint,g.hero.transform.position,1<<8))
                Windup(1,kind==2?.85f:kind==4?.7f:.65f);
        }
        void Windup(int type,float delay)
        {
            pattern=type;windupStartedAt=Time.time;resolveAt=Time.time+delay;lockedAim=AncientGame.Instance.hero.transform.position;
            faceLockUntil=resolveAt+(kind==5?1.15f:.25f);
        }
        void DrawTelegraph()
        {
            warning.enabled=true;warning.startColor=warning.endColor=pattern==0?AncientGame.Orange:AncientGame.Cyan;
            Vector2 begin=AimPoint;
            if(kind==2)
            {
                warning.positionCount=14;float flight=.95f;Vector2 velocity=(lockedAim-begin)/flight+Vector2.up*(14*flight*.5f);
                for(int i=0;i<14;i++){float t=i/13f*flight;warning.SetPosition(i,begin+velocity*t+Vector2.down*7*t*t);}
            }
            else
            {
                warning.positionCount=2;warning.SetPosition(0,begin);
                warning.SetPosition(1,pattern==0?begin+new Vector2(facing*(kind==3?4.8f:3.1f),-.5f):lockedAim);
            }
        }
        void FireProjectile(Vector2 target,float speed,bool scrap,float gravity)
        {
            var g=AncientGame.Instance;Vector2 aim=(target-AimPoint).normalized;if(aim.sqrMagnitude<.1f)aim=Vector2.right*facing;
            if(g.shots.Count>=48)return;
            Vector2 origin=AimPoint+aim*.65f;
            Vector2 velocity=gravity>0?(target-origin)/.95f+Vector2.up*(gravity*.95f*.5f):aim*speed;
            g.Projectiles.Spawn(origin,velocity,kind==3?20:scrap?25:kind==4?12:15,scrap,kind==4?1:kind==3?2:0,gravity);
        }
        void ResolveAttack()
        {
            attackPoseUntil=Time.time+.18f;
            var g=AncientGame.Instance;
            if(pattern==0)
            {
                if(kind==0)Step(facing*.9f);
                Vector2 delta=(Vector2)g.hero.transform.position-AimPoint;
                if(Mathf.Abs(delta.x)<(kind==3?4.8f:2.8f)&&Mathf.Abs(delta.y)<1.7f&&delta.x*facing>-.6f&&!Physics2D.Linecast(AimPoint,g.hero.transform.position,1<<8))g.hero.Hurt(kind==3?32:kind==5?26:22);
                g.Slash(AimPoint,facing,kind==3);g.Burst(AimPoint+Vector2.right*facing*2,AncientGame.Orange,kind==3?25:8);
                if(kind==3)g.Shake(.2f);
            }
            else if(kind==2)FireProjectile(lockedAim,9,true,14);
            else if(kind==1)
            {volleySpeed=IsDistrictBoss?18:12;volleyInterval=IsDistrictBoss?.24f:.14f;
                FireProjectile(lockedAim,volleySpeed,false,0);volleyRemaining=2;volleyAt=Time.time+volleyInterval;}
            else if(IsDistrictBoss&&DistrictFloor==2)
            {volleySpeed=14;volleyInterval=.2f;FireProjectile(lockedAim,volleySpeed,false,0);volleyRemaining=2;volleyAt=Time.time+volleyInterval;}
            else if(IsDistrictBoss&&DistrictFloor==3)
            {FireProjectile(lockedAim,9,true,14);}
            else
            {
                int count=kind==3?(Enraged?7:5):3;
                Vector2 baseAim=(lockedAim-AimPoint).normalized;
                for(int i=0;i<count;i++)
                {
                    float angle=(i-(count-1)/2f)*(Enraged?10:12)*Mathf.Deg2Rad;
                    Vector2 direction=new Vector2(baseAim.x*Mathf.Cos(angle)-baseAim.y*Mathf.Sin(angle),baseAim.x*Mathf.Sin(angle)+baseAim.y*Mathf.Cos(angle));
                    FireProjectile(AimPoint+direction*10,kind==3?11:9,false,0);
                }
            }
        }
        internal bool BossStep(float distance){return Step(distance);}
        internal void BossContact()
        {
            Vector2 contact=(Vector2)AncientGame.Instance.hero.transform.position-AimPoint;
            if(AncientThreatRules.BodyContactAllowed(IsBoss,Recovering,Time.time<staggerUntil)&&Mathf.Abs(contact.x)<(kind==3?1.1f:.65f)&&Mathf.Abs(contact.y)<1)
                AncientGame.Instance.hero.HurtContact(this,kind==3?14:8);
        }
        internal void BossVisual(int pose,float direction,float anticipation,bool telegraph,bool recovery)
        {
            if(!art)return;
            facing=direction;art.flipX=facing<0;art.sprite=productionEncounter?AncientArt.ProductionCaptain(pose):AncientArt.Boss(IsSecretary?5:IsSniperClone?4:IsDistrictBoss?DistrictFloor:0,pose);
            art.transform.localScale=new Vector3(1-anticipation*.025f,1+anticipation*.025f,1);
            art.transform.localRotation=Quaternion.identity;
            art.color=Time.time<flashUntil?AncientGame.Cyan:recovery?new Color(1,.87f,.5f):telegraph?Color.Lerp(Color.white,AttackIsPhysical?AncientGame.Orange:AncientGame.Cyan,(.5f+Mathf.Sin(Time.time*20)*.5f)*.45f):Color.white;
        }
        public void WireImpact(float direction)
        {
            if(dead)return;
            wirePushRemaining=direction*AncientRules.WirePushDistance(IsBoss);
            // Boss windups continue; a wire cannot repeatedly stun-lock a boss.
            Hit(AncientRules.WireImpactDamage(IsBoss),!IsBoss&&kind!=5);
        }
        public void Hit(float damage,bool breakGuard=false)
        {
            if(dead)return;var g=AncientGame.Instance;
            if(kind==5&&Time.time>=guardBrokenUntil&&((Vector2)g.hero.transform.position-AimPoint).x*facing>0)
            {
                if(!breakGuard){damage*=.15f;g.Burst(AimPoint+Vector2.right*facing*.6f,AncientGame.Cyan,8);g.Notice("방패 / 뒤로 돌아가거나 강한 공격으로 방어를 무너뜨리세요.",1.2f);}
                else{guardBrokenUntil=Time.time+2;staggerUntil=Time.time+.8f;resolveAt=-1;g.Notice("방어 붕괴",.8f);}
            }
            if(breakGuard&&!IsBoss){resolveAt=-1;staggerUntil=Mathf.Max(staggerUntil,Time.time+.4f);nextAttack=Mathf.Max(nextAttack,Time.time+.65f);volleyRemaining=0;}
            bool punish=Recovering&&IsBoss;damage=AncientCombatRules.Damage(damage,IsBoss,punish);
            if(sniperEncounter&&!IsSniperClone)damage=sniperEncounter.FilterDamage(damage,hp,maxHP);
            if(productionEncounter)damage=productionEncounter.FilterCommanderDamage(damage,hp,maxHP);
            if(damage<=0)return;
            hp-=damage;flashUntil=Time.time+.1f;if(g.Feel)g.Feel.Damage(AimPoint,damage,punish);
            g.Sound(6);g.HitEmphasis(breakGuard);g.Impact(AimPoint,breakGuard?AncientGame.Orange:AncientGame.Cyan,breakGuard);
            g.Burst(AimPoint,AncientGame.Cyan,7);g.Shake(.045f);g.score+=Mathf.RoundToInt(damage);
            if(productionEncounter){productionEncounter.CommanderDamaged(this,damage);return;}
            if(hp>0)return;
            if(IsSniperClone)
            {dead=true;g.enemies.Remove(this);if(sniperEncounter)sniperEncounter.CloneDown(cloneSlot,AimPoint);Destroy(gameObject);return;}
            if(IsDistrictBoss&&DistrictFloor==4)
            {
                // Combat ends without killing either top-floor opponent.
                dead=true;g.enemies.Remove(this);
                if(IsSecretary)
                {
                    hp=1;var clock=GetComponent<AncientClockBoss>();if(clock)clock.StopBattle();
                    warning.enabled=false;BossVisual(5,facing,0,false,true);g.City.SecretaryRetreated(this);return;
                }
                hp=1;warning.enabled=false;BossVisual(5,facing,0,false,true);g.City.BossDefeated(DistrictFloor);return;
            }
            dead=true;g.kills++;g.credits+=kind==3?20:kind==5?6:kind==4?5:4;g.score+=kind==3?1000:100;
            Vector2 drop=AimPoint;
            if(kind==4)
            {
                var ground=Physics2D.Raycast(drop,Vector2.down,20,1<<8);
                if(ground)drop=ground.point+Vector2.up*.7f;
            }
            g.SpawnPickup(drop,-1,true);g.Burst(AimPoint,AncientGame.Orange,kind==3?55:18);g.enemies.Remove(this);
            if(IsDistrictBoss){g.City.BossDefeated(DistrictFloor);}
            else if(kind==3){g.bossDefeated=true;g.boss=null;g.ClearBattleProjectiles();g.Save();g.Notice("화물 수호 장치 제압\n동쪽 통로가 열렸습니다.",7);}
            Destroy(gameObject);
        }
        public void CompleteSniperDuel()
        {
            if(dead||IsSecretary||!IsDistrictBoss||DistrictFloor!=4)return;
            dead=true;hp=1;var g=AncientGame.Instance;g.enemies.Remove(this);
            if(warning)warning.enabled=false;BossVisual(5,facing,0,false,true);g.City.BossDefeated(4);
        }
        public void DepartAlive()
        {
            // Both top-floor opponents leave alive after their dialogue, without death rewards.
            if(!IsDistrictBoss||DistrictFloor!=4)return;AncientGame.Instance.Burst(AimPoint,AncientGame.Cyan,16);Destroy(gameObject);
        }
        void OnDestroy(){if(warning)Destroy(warning.gameObject);}
    }
    public sealed class AncientProjectile : MonoBehaviour
    {
        public bool scrap,TrainingObject;public int metalKind;public Vector2 velocity;public float damage=15,gravity;
        float age;int ricochets;bool removed=true;SpriteRenderer art;AncientGame game;
        public bool Available {get{return removed;}}
        public void Initialize(AncientProjectilePool pool)
        {game=pool.Game;art=AncientArt.Display("Reusable metal object",AncientArt.MetalProjectile(0),Vector2.zero,12,transform);}
        public void Play(Vector2 position,Vector2 speed,float amount,bool isScrap,int kind,float acceleration,int bounces=0)
        {
            scrap=isScrap;TrainingObject=false;metalKind=Mathf.Clamp(kind,0,2);velocity=speed;damage=amount;gravity=acceleration;ricochets=Mathf.Clamp(bounces,0,1);age=0;removed=false;
            transform.position=position;transform.rotation=Quaternion.identity;transform.localScale=Vector3.one;
            art.sprite=scrap?AncientArt.Scrap():AncientArt.MetalProjectile(metalKind);
            art.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(speed.y,speed.x)*Mathf.Rad2Deg);
            art.transform.localScale=Vector3.one*(scrap||metalKind==2?.65f:metalKind==1?.48f:.5f);art.color=Color.white;
            game.shots.Add(this);gameObject.SetActive(true);
        }
        void Update()
        {
            if(removed)return;if(game.won){Remove();return;}if(game.Paused)return;
            age+=Time.deltaTime;velocity+=Vector2.down*gravity*Time.deltaTime;Vector2 previous=transform.position,next=previous+velocity*Time.deltaTime;
            Vector2 segment=next-previous,h=game.hero?(Vector2)game.hero.transform.position:previous;
            float length=segment.magnitude;Vector2 travel=length>0?segment/length:(h-previous).normalized;
            bool canHit=game.hero&&age>.05f;Bounds bikeBounds=new Bounds();float reach=canHit?.75f:0;
            if(canHit&&game.hero.bike)
            {
                // Keep the existing motorcycle hitbox, including its fallback before the first physics sync.
                var collider=game.hero.GetComponent<BoxCollider2D>();bikeBounds=collider.bounds;
                if(bikeBounds.size.x<.01f)bikeBounds=new Bounds(game.hero.transform.TransformPoint(collider.offset),new Vector3(2.87f,1.82f,1));
                bikeBounds.Expand(new Vector3(.14f,.14f,1));
                Vector2 offset=(Vector2)bikeBounds.center-h;
                reach=AncientProjectileRules.BoxOcclusionReach(travel.x,travel.y,bikeBounds.extents.x,bikeBounds.extents.y,offset.x,offset.y);
            }
            // The extra reach is occlusion only: a radius overlapping a nearby wall cannot hit through it.
            var terrain=Physics2D.Linecast(previous,next+travel*reach,1<<8);
            float surfaceFraction=terrain?(length>0?terrain.distance/length:terrain.distance>0?float.MaxValue:0):0;
            if(canHit)
            {
                Vector2 target=h-previous;float entry;
                bool hit;
                if(game.hero.bike)
                {
                    hit=AncientProjectileRules.BoxEntry(segment.x,segment.y,bikeBounds.min.x-previous.x,bikeBounds.min.y-previous.y,bikeBounds.max.x-previous.x,bikeBounds.max.y-previous.y,out entry);
                }
                else hit=AncientProjectileRules.CircleEntry(segment.x,segment.y,target.x,target.y,.75f,out entry);
                Vector2 fromSurface=terrain?h-terrain.point:Vector2.zero;
                if(hit&&(!terrain||AncientProjectileRules.BeforeSurface(entry,surfaceFraction,fromSurface.x,fromSurface.y,terrain.normal.x,terrain.normal.y)))
                {
                    // Practice objects still use real flight/cutting, but a missed lesson cannot kill the officer.
                    if(TrainingObject){game.Burst(transform.position,AncientGame.Cyan,4);Remove();return;}
                    if(Input.GetKey(KeyCode.F))game.hero.AbsorbShot(this);else{game.hero.Hurt(damage);Remove();}return;
                }
            }
            if(terrain&&terrain.distance<=length)
            {
                if(ricochets>0&&Mathf.Abs(terrain.normal.x)>.5f)
                {ricochets--;velocity=Vector2.Reflect(velocity,terrain.normal);transform.position=terrain.point+terrain.normal*.06f;game.Burst(terrain.point,AncientGame.Cyan,5);return;}
                game.Burst(previous,new Color(.65f,.48f,.3f),8);Remove();return;
            }
            transform.position=next;
            if(scrap||metalKind==1)art.transform.Rotate(0,0,(scrap?260:480)*Time.deltaTime);
            else art.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg);
            if(age>5)Remove();
        }
        public void Remove()
        {if(removed)return;removed=true;game.shots.Remove(this);gameObject.SetActive(false);}
    }
    public sealed class AncientPickup : MonoBehaviour
    {
        public int core=-1;public bool Transient;Vector2 origin;float phase;GameObject visual;
        void Start()
        {
            var g=AncientGame.Instance;origin=transform.position;
            visual=g.Box("Core crystal",Vector2.zero,Vector2.one*(core>=0?.48f:.22f),core>=0?AncientGame.Orange:AncientGame.Cyan,7);
            visual.transform.SetParent(transform,false);visual.transform.localRotation=Quaternion.Euler(0,0,45);
            var inner=g.Box("Crystal centre",Vector2.zero,Vector2.one*(core>=0?.20f:.09f),Color.white,8);inner.transform.SetParent(transform,false);
        }
        void Update()
        {
            var g=AncientGame.Instance;if(g.Paused||g.won||!g.hero)return;
            phase+=Time.deltaTime;transform.position=origin+Vector2.up*Mathf.Sin(phase*3)*.16f;
            if(visual)visual.transform.Rotate(0,0,30*Time.deltaTime);
            if(Vector2.Distance(g.hero.transform.position,transform.position)>1.2f)return;
            if(core>=0)
            {
                g.Sound(5);g.coreMask|=1<<core;g.credits+=8;g.score+=250;g.hero.hp=Mathf.Min(g.hero.MaxHP,g.hero.hp+25);
                g.Notice("에너지 코어 "+g.Cores+" / 3\n새 강화 단계 해금 / K로 정비",5);
            }
            else{g.credits+=2;g.hero.hp=Mathf.Min(g.hero.MaxHP,g.hero.hp+4);}
            g.hero.particles=Mathf.Min(g.hero.MaxParticles,g.hero.particles+(core>=0?30:20));
            g.Burst(transform.position,AncientGame.Cyan,12);g.pickups.Remove(this);
            if(core>=0)g.Save();Destroy(gameObject);
        }
    }
    public sealed class AncientFragment : MonoBehaviour
    {
        Vector2 velocity;float age,life;SpriteRenderer art;Color tint;
        public void Play(Vector2 pos,Color color,Vector2 speed,float size)
        {
            if(!art)art=GetComponent<SpriteRenderer>();tint=color;velocity=speed;age=0;life=.3f+size*2;
            transform.position=pos;transform.localScale=Vector3.one*size;art.color=color;gameObject.SetActive(true);
        }
        void Update()
        {
            age+=Time.deltaTime;transform.position+=(Vector3)(velocity*Time.deltaTime);velocity+=Vector2.down*12*Time.deltaTime;
            art.color=new Color(tint.r,tint.g,tint.b,tint.a*Mathf.Clamp01(1-age/life));
            if(age>=life)gameObject.SetActive(false);
        }
    }
    public sealed class AncientAfterimage : MonoBehaviour
    {
        SpriteRenderer art;float age;
        public void Play(SpriteRenderer source)
        {
            if(!art)art=GetComponent<SpriteRenderer>();art.sprite=source.sprite;art.flipX=source.flipX;
            transform.position=source.transform.position;transform.rotation=source.transform.rotation;transform.localScale=source.transform.lossyScale;
            age=0;art.color=new Color(.4f,.85f,1,.3f);gameObject.SetActive(true);
        }
        void Update()
        {age+=Time.deltaTime;art.color=new Color(.4f,.85f,1,.3f*Mathf.Clamp01(1-age/.16f));if(age>=.16f)gameObject.SetActive(false);}
    }
    public sealed class AncientImpactRing : MonoBehaviour
    {
        LineRenderer line;Vector2 centre;Color tint;float radius,age,baseWidth;
        readonly Vector3[] points=new Vector3[20];
        static readonly Vector2[] circle=MakeCircle();
        static Vector2[] MakeCircle()
        {var p=new Vector2[20];for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/p.Length;p[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a));}return p;}
        public void Play(Vector2 p,Color color,bool heavy)
        {
            if(!line)line=GetComponent<LineRenderer>();centre=p;tint=color;radius=heavy?1.4f:.65f;baseWidth=heavy?.06f:.035f;age=0;
            line.startWidth=line.endWidth=baseWidth;gameObject.SetActive(true);Draw(0);
        }
        void Draw(float t)
        {
            line.startColor=line.endColor=new Color(tint.r,tint.g,tint.b,(1-t)*tint.a);
            float size=Mathf.Lerp(.1f,radius,t);
            for(int i=0;i<points.Length;i++)points[i]=centre+circle[i]*size;
            line.SetPositions(points);
        }
        void Update(){age+=Time.deltaTime;float t=Mathf.Clamp01(age/.16f);Draw(t);if(t>=1)gameObject.SetActive(false);}
    }
    public sealed class AncientSlashArc : MonoBehaviour
    {
        LineRenderer line;float age;
        readonly Vector3[] points=new Vector3[9];
        public void Play(Vector2 p,float facing,bool heavy)
        {
            if(!line)line=GetComponent<LineRenderer>();age=0;Color color=heavy?AncientGame.Orange:AncientGame.Cyan;
            line.startColor=line.endColor=color;line.startWidth=line.endWidth=heavy?.11f:.065f;
            float radius=heavy?2.5f:1.8f;
            for(int i=0;i<points.Length;i++){float a=Mathf.Lerp(-1.1f,1.1f,i/8f);points[i]=p+new Vector2(Mathf.Cos(a)*facing,Mathf.Sin(a))*radius;}
            line.SetPositions(points);gameObject.SetActive(true);
        }
        void Update(){age+=Time.deltaTime;if(age>=.12f)gameObject.SetActive(false);}
    }
}
