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
        public AncientProjectilePool Projectiles {get;private set;}
        public bool skills, won, bossDefeated, showHelp = true;
        public bool MainMenu {get;private set;}
        public bool HasSession {get;private set;}
        public bool HasSavedProgress {get{return Saved("questStage")!=0||Saved("started")!=0||Saved("checkpoint")!=0||Saved("cores")!=0||Saved("credits")!=0||Saved("kills")!=0||Saved("score")!=0||Saved("bike")!=0||Saved("jump")!=0||Saved("capacity")!=0||Saved("armor")!=0||Saved("boss")!=0;}}
        public bool Paused {get {return MainMenu||skills||(Story&&(Story.DialogueOpen||Story.LiftBusy))||(Journal&&Journal.Open)||(Finale&&Finale.Cinematic);}}
        public bool GameplayInputReady {get{return Time.frameCount>inputReadyFrame;}}
        static bool startAfterReload;int inputReadyFrame;
        public int coreMask, credits, kills, score;
        public int Cores {get {return ((coreMask&1)!=0?1:0)+((coreMask&2)!=0?1:0)+((coreMask&4)!=0?1:0);}}
        public bool ArenaActive {get {return boss && (boss.IsDistrictBoss||!bossDefeated);}}
        public Vector2 checkpoint = new Vector2(2, 1.4f);
        public static readonly Color Cyan = new Color(.18f,.92f,1);
        public static readonly Color Orange = new Color(1,.43f,.14f);
        const string SaveKey = "PLUGIN.Ancient.v2.";
        Camera view; Sprite square; Transform backdrop;
        GUIStyle small, title, centered;
        string message; float messageUntil, shake, runTime;
        int checkpointStage;
        Material lineMaterial; AncientAudio audioCues;
        readonly List<AncientFragment> fragmentPool=new List<AncientFragment>();
        readonly List<AncientAfterimage> ghostPool=new List<AncientAfterimage>();
        readonly AncientImpactRing[] impactPool=new AncientImpactRing[24];
        readonly AncientSlashArc[] slashPool=new AncientSlashArc[12];
        int impactCursor,slashCursor;
        int fragmentCursor,ghostCursor;float hitPauseUntil,damageFlash,healthEcho=100;
        Vector3 cameraFollow;float smoothFrameTime=1f/60;
        public float FramesPerSecond {get{return 1f/Mathf.Max(.001f,smoothFrameTime);}}
        public float DamageFlash {get{return damageFlash;}}
        public Camera View {get{return view;}}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(!Object.FindFirstObjectByType<AncientGame>())new GameObject("Ancient Civilization").AddComponent<AncientGame>();
        }
        public static int Saved(string key,int fallback=0){return PlayerPrefs.GetInt(SaveKey+key,fallback);}
        public static void SetSaved(string key,int value){PlayerPrefs.SetInt(SaveKey+key,value);}
        void Awake()
        {
            QualitySettings.vSyncCount=0;Application.targetFrameRate=144;
            Instance=this;MainMenu=!startAfterReload;HasSession=startAfterReload;startAfterReload=false; audioCues=gameObject.AddComponent<AncientAudio>(); Time.timeScale=1; Physics2D.gravity=new Vector2(0,-32);
            coreMask=Saved("storyCores"); credits=Saved("credits"); checkpointStage=Saved("storyCheckpoint");
            bossDefeated=Saved("storyBoss")!=0; kills=Saved("kills");score=Saved("score");
            if(checkpointStage==1)checkpoint=new Vector2(46,2.3f);
            if(checkpointStage==2)checkpoint=new Vector2(86,1.3f);
            if(checkpointStage==3)checkpoint=new Vector2(132,1.3f);
            if(checkpointStage==4)checkpoint=new Vector2(115,1.3f);
            int cityFloor=Mathf.Clamp(Saved("cityFloor",Saved("upperCity")!=0?1:0),0,4);
            if(cityFloor>1)checkpoint=new Vector2(AncientCityLayout.Entry(cityFloor),1.3f);
            if(Saved("retryActive")!=0&&Saved("retryFloor",-1)==cityFloor)
            {
                float retryX=Saved("retryX")/100f,retryY=Saved("retryY")/100f;
                if(AncientCityLayout.SafeResume(cityFloor,retryX,retryY))checkpoint=new Vector2(retryX,retryY);
            }
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
            hero=new GameObject("Lower-city patrol officer").AddComponent<AncientHero>();hero.transform.position=checkpoint;
            Story=gameObject.AddComponent<AncientStory>();Story.Initialize(this);
            Performance=gameObject.AddComponent<AncientPerformance>();Performance.Initialize(this);
            Feel=gameObject.AddComponent<AncientGameFeel>();Feel.Initialize(this);
            Exploration=gameObject.AddComponent<AncientExploration>();Exploration.Initialize(this);
            Journal=gameObject.AddComponent<AncientJournal>();Journal.Initialize(this);
            Cases=gameObject.AddComponent<AncientFieldCases>();Cases.Initialize(this);
            gameObject.AddComponent<AncientVisuals>();gameObject.AddComponent<AncientPointer>();gameObject.AddComponent<AncientMainMenu>();
            Time.timeScale=MainMenu?0:1;
            if(HasSession){SetSaved("started",1);PlayerPrefs.Save();inputReadyFrame=Time.frameCount+1;}
            Notice(checkpointStage==0?"LOWER CITY / MORNING PATROL\nComplete the movement, chain and blade training quests.":"Checkpoint restored. The city remembers.",7);
        }
        void BuildWorld()
        {
            backdrop=new GameObject("City parallax").transform;
            for(int i=-1;i<=1;i++)
            {
                var image=AncientArt.Display("Ancient city panorama",AncientArt.City(),new Vector2(i*48,0),-30,backdrop);
                image.flipX=i!=0;
            }
            Platform(-5,118,0);Platform(43,58,1);
            foreach(var ledge in AncientTraversalLayout.LowerRoute)Platform(ledge.left,ledge.right,ledge.top);
            foreach(var ledge in AncientTraversalLayout.LowerFastRoute)Platform(ledge.left,ledge.right,ledge.top);
            Platform(4,7,3.5f);Platform(12,16,3.4f);
            // One unbroken solid ceiling covers the entire level and extends past both camera edges.
            Ceiling(-40,280,10);
            foreach(var ledge in AncientTraversalLayout.MainRoute)Platform(ledge.left,ledge.right,ledge.top);
            foreach(var ledge in AncientTraversalLayout.FastRoute)Platform(ledge.left,ledge.right,ledge.top);
            Platform(128,264,-4.5f);
            Traversal=gameObject.AddComponent<AncientTraversal>();Traversal.Initialize(this);
            City=gameObject.AddComponent<AncientCityCampaign>();City.Initialize(this);
            Finale=gameObject.AddComponent<AncientFinale>();Finale.Build(this);
            AncientArt.Display("Upper city entry",AncientArt.World(3),new Vector2(132,0),-1);
            AncientArt.Display("Eastern gate",AncientArt.World(3),new Vector2(115,0),-1);
            BuildLiftTerminal(115);BuildLiftTerminal(132);
            var courseEnd=Box("Maintenance exit bulkhead",new Vector2(283,5),new Vector2(.4f,10),new Color(.06f,.13f,.18f),1);
            courseEnd.layer=8;courseEnd.AddComponent<BoxCollider2D>();
            // Separate city exits; floors are connected only by the elevator.
            foreach(float x in new[]{119f,127f})
            {var boundary=Box("Elevator shaft boundary",new Vector2(x,5),new Vector2(.4f,10),new Color(.05f,.1f,.13f),-2);boundary.layer=8;boundary.AddComponent<BoxCollider2D>();}
            // Story gates hostile encounters until patrol and equipment tutorials are complete.
            var district=gameObject.AddComponent<AncientDistrict>();district.Initialize(this);
            for(int i=0;i<8;i++)SpawnPickup(new Vector2(7+i*12,1.6f),-1);
            Beacon(new Vector2(46,1));Beacon(new Vector2(86,0));
            foreach(var p in new[]{new Vector2(8,.84f),new Vector2(46,1.84f),new Vector2(86,.84f)})
            {
                var vehicle=new GameObject("Placed patrol motorcycle").AddComponent<AncientParkedBike>();
                vehicle.Initialize(p);parkedBikes.Add(vehicle);
            }
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
            if(hero)hero.SaveUpgrades();if(Finale)Finale.WriteSave();PlayerPrefs.Save();
        }
        public void Checkpoint(int stage)
        {
            if(stage<=checkpointStage)return;
            checkpointStage=stage;checkpoint=stage==1?new Vector2(46,2.3f):stage==2?new Vector2(86,1.3f):new Vector2(132,1.3f);
            hero.hp=hero.MaxHP;hero.particles=Mathf.Max(hero.particles,60);Save();
            Burst(checkpoint,Cyan,28);Notice("CHECKPOINT / Health restored. Progress saved.",4);
        }
        public void SetLiftCheckpoint(int floor)
        {
            checkpointStage=floor==0?4:3;checkpoint=new Vector2(AncientCityLayout.Entry(floor),1.3f);SetSaved("cityFloor",floor);SetSaved("retryActive",0);Save();
        }
        public void CombatCheckpoint(int floor,Vector2 point)
        {
            if(!AncientCityLayout.SafeResume(floor,point.x,point.y))return;
            checkpoint=point;SetSaved("retryActive",1);SetSaved("retryFloor",floor);
            SetSaved("retryX",Mathf.RoundToInt(point.x*100));SetSaved("retryY",Mathf.RoundToInt(point.y*100));Save();
        }
        public void HeroDied()
        {
            if(Finale&&Finale.Retry())return;
            if(boss){enemies.Remove(boss);Destroy(boss.gameObject);boss=null;}
            if(City)City.OnEnterFloor();
            foreach(var shot in shots.ToArray())if(shot)shot.Remove();
            foreach(var vehicle in parkedBikes)if(vehicle)vehicle.ResetAtSpawn();
            hero.TravelTo(checkpoint);SnapCameraToHero();Save();Notice("다시 도전 / 체력 회복 · 코어와 수사 진행은 유지됩니다.",2);
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
            if(MainMenu){Time.timeScale=0;return;}
            if(Story&&(Story.DialogueOpen||Story.LiftBusy)||(Journal&&Journal.Open)||(Finale&&Finale.Cinematic)){Time.timeScale=0;return;}
            if(Input.GetKeyDown(KeyCode.K)||Input.GetKeyDown(KeyCode.Escape))ToggleSkills();
            if(Input.GetKeyDown(KeyCode.H))showHelp=!showHelp;
            Time.timeScale=Paused?0:Time.unscaledTime<hitPauseUntil?.12f:1;
            if(Paused)return;
            damageFlash=Mathf.MoveTowards(damageFlash,0,Time.unscaledDeltaTime*2.8f);
            if(hero)healthEcho=hero.hp>=healthEcho?hero.hp:Mathf.MoveTowards(healthEcho,hero.hp,Time.unscaledDeltaTime*34);
            if(!skills && !won)runTime+=Time.deltaTime;
            if(hero&&hero.transform.position.x>44&&hero.transform.position.x<55&&hero.transform.position.y<3)Checkpoint(1);
            if(hero&&hero.transform.position.x>85&&hero.transform.position.x<94&&hero.transform.position.y<2)Checkpoint(2);

        }
        void LateUpdate()
        {
            if(!hero)return;
            float speed=Mathf.Abs(hero.HorizontalSpeed);
            float zoom=hero.Wired?7.5f:hero.bike?6.8f:5.8f;
            view.orthographicSize=Mathf.Lerp(view.orthographicSize,zoom,1-Mathf.Exp(-3*Time.unscaledDeltaTime));
            float lead=hero.Facing*Mathf.Lerp(2.5f,4,Mathf.Clamp01(speed/25));
            Vector3 target=new Vector3(Mathf.Clamp(hero.transform.position.x+lead,Story&&Story.InUpperCity?AncientCityLayout.Left(Story.CurrentFloor)+5:5,Story&&Story.InUpperCity?AncientCityLayout.Right(Story.CurrentFloor)-6:113),Mathf.Max(4.6f,hero.transform.position.y+1),-10);
            if(Finale&&Finale.Active)target=new Vector3(Mathf.Clamp(hero.transform.position.x+lead,AncientFinaleRules.Start+5,AncientFinaleRules.Finish+12),Mathf.Max(1.5f,hero.transform.position.y+2),-10);
            cameraFollow=Vector3.Lerp(cameraFollow,target,1-Mathf.Exp(-7*Time.unscaledDeltaTime));
            var p=cameraFollow;if(shake>0&&!skills){p+=(Vector3)(Random.insideUnitCircle*shake);shake=Mathf.MoveTowards(shake,0,Time.unscaledDeltaTime);}
            float displayPPU=Mathf.Max(1,Screen.height/(view.orthographicSize*2));
            p.x=Mathf.Round(p.x*displayPPU)/displayPPU;p.y=Mathf.Round(p.y*displayPPU)/displayPPU;view.transform.position=p;
            float parallax=8+(p.x-8)*.82f;
            parallax+=Mathf.Floor((p.x-parallax+24)/48)*48;
            backdrop.position=new Vector3(parallax,Finale&&Finale.Active?p.y:6+(p.y-4)*.72f,0);
        }
        void OnApplicationQuit(){if(HasSession)Save();}
        void OnDestroy(){Time.timeScale=1;AncientHUD.Dispose();if(lineMaterial)Destroy(lineMaterial);}
        void Panel(Rect r,Color c){AncientHUD.Panel(r,c);}
        void Bar(Rect r,float value,Color c)
        {
            float pad=r.height<=6?1:2;
            Panel(r,new Color(.04f,.07f,.10f,.95f));
            Panel(new Rect(r.x+pad,r.y+pad,(r.width-pad*2)*Mathf.Clamp01(value),r.height-pad*2),c);
        }
        public void NewRun()
        {
            foreach(string key in new[]{"started","cores","credits","checkpoint","boss","kills","score","bike","jump","capacity","armor","storyCores","storyCheckpoint","storyBoss","questStage","questFlags","residencePoints","upperCity","courseBestMs","factoryBestMs","cityFloor","cityCleared","retryActive","retryFloor","retryX","retryY","routeCaches","caseStarted","caseComplete","caseRelay","caseRecord","finalePhase","finaleZone","finaleCountdown","firstRunCleared"})PlayerPrefs.DeleteKey(SaveKey+key);
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
            if(!hero||MainMenu||(Story&&(Story.DialogueOpen||Story.LiftBusy))||(Journal&&Journal.Open)||(Finale&&Finale.Cinematic))return;
            if(!skills&&!won&&Event.current.type!=EventType.Repaint)return;
            GUIStyle oldButton=GUI.skin.button;GUI.skin.button=AncientHUD.Button;
            if(small==null)
            {
                small=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=14,wordWrap=true};title=new GUIStyle(small){fontSize=22,fontStyle=FontStyle.Bold};
                centered=new GUIStyle(title){alignment=TextAnchor.MiddleCenter};
            }
            float scale=Mathf.Clamp(Screen.width/1280f,.65f,2);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float w=Screen.width/scale,h=Screen.height/scale;
            Panel(new Rect(16,16,302,106),new Color(.025f,.045f,.065f,.88f));
            GUI.Label(new Rect(28,21,290,24),"PLUGIN / ANCIENT CIVILIZATION",small);
            Bar(new Rect(28,49,272,18),healthEcho/hero.MaxHP,new Color(.48f,.23f,.12f));
            Panel(new Rect(30,51,268*Mathf.Clamp01(hero.hp/hero.MaxHP),14),Orange);Bar(new Rect(28,73,272,13),hero.CaptureEnergy/100f,Cyan);
            GUI.Label(new Rect(28,91,275,25),"HP "+Mathf.CeilToInt(hero.hp)+"   PARTICLES "+Mathf.RoundToInt(hero.CaptureEnergy)+"   TECH "+credits,small);
            Panel(new Rect(w-300,16,284,74),new Color(.025f,.045f,.065f,.88f));
            GUI.Label(new Rect(w-286,24,260,55),"ENERGY CORES  "+Cores+" / 3\n"+(Story.Stage<4?"Complete patrol training":Story.Stage<=5?"Pursue the core theft truck":Story.Stage==6?"Secure the suspect":"Upper-city transit authorized"),small);
            GUI.Label(new Rect(28,128,300,30),(hero.bike?"BIKE "+Mathf.CeilToInt(hero.bikeHP)+" / 80":hero.running?"RUN":"WALK")+"   "+(hero.Wired?"WIRE LINK "+hero.WireChain:hero.WireChain>1?"LINK "+hero.WireChain:"")+"   K / UPGRADES",small);
            GUI.Label(new Rect(w-300,92,284,23),(AncientVisuals.HighQuality?"F6 고품질":"F6 가볍게")+" · "+Performance.Summary,small);
            GUI.Label(new Rect(w-300,117,284,23),"WHEEL / "+hero.WireModeName+"   RMB PARTICLES",small);
            GUI.Label(new Rect(w-300,141,284,24),"CAPTURE "+hero.CaptureStage+" / 3   "+Mathf.RoundToInt(hero.CaptureEnergy)+"%",small);
            Bar(new Rect(w-300,168,284,7),hero.CaptureEnergy/100f,Cyan);
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
                if(enemy==boss || enemy.HealthRatio>=1)continue;
                Vector3 screen=view.WorldToScreenPoint(enemy.AimPoint+Vector2.up*1.1f);
                if(screen.z>0 && screen.x>=0 && screen.x<Screen.width)
                    Bar(new Rect(screen.x/scale-22,(Screen.height-screen.y)/scale,44,5),enemy.HealthRatio,Orange);
            }
            if(boss)
            {
                GUI.Label(new Rect(w/2-220,h-104,440,25),boss.Role+"  "+(boss.Enraged?"/ OVERDRIVE":""),centered);
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
                GUI.Label(new Rect(28,h-87,w-56,70),"A/D MOVE   CTRL RUN   SPACE JUMP   E RIDE / LIFT   F ABSORB   S BRAKE\nWHEEL UP ATTACK / DOWN WIRE   ATTACK LMB TAP: YELLOW BLADE   HOLD: AIM LASER, RELEASE WHEN READY   RMB PARTICLES   MMB SWING",small);
            }
            if(skills)
            {
                Panel(new Rect(0,0,w,h),new Color(0,.015f,.03f,.75f));
                float x=w/2-260,y=Mathf.Max(20,h/2-240);
                Panel(new Rect(x,y,520,515),new Color(.035f,.065f,.09f,.98f));
                GUI.Label(new Rect(x+25,y+20,470,35),"입자 장비 정비",title);
                GUI.Label(new Rect(x+25,y+57,470,52),"기술 재화: "+credits+"   /   Energy cores: "+Cores+"\n코어를 회수하고 기술 재화를 모아 장비를 강화합니다.",small);
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
                GUI.Label(new Rect(28,162,90,22),"LMB / AIM",small);
                Bar(new Rect(120,156,74,4),hero.CaptureEnergy/100f,Orange);
                GUI.Label(new Rect(120,162,100,22),"ENERGY",small);
                if(hero.bike)
                {
                    Bar(new Rect(220,156,84,4),hero.bikeHP/80,Cyan);
                    GUI.Label(new Rect(220,162,120,22),Mathf.RoundToInt(Mathf.Abs(hero.HorizontalSpeed)*3.6f)+" km/h",small);
                }
                AncientPickup nearest=null;float distance=float.MaxValue;
                foreach(var item in pickups)if(item&&item.core>=0)
                {float d=Vector2.Distance(item.transform.position,hero.transform.position);if(d<distance){distance=d;nearest=item;}}
                if(nearest)
                {
                    Vector3 point=view.WorldToScreenPoint(nearest.transform.position+Vector3.up*.65f);
                    float x=Mathf.Clamp(point.x/scale,60,w-100),y=Mathf.Clamp((Screen.height-point.y)/scale,185,h-125);
                    GUI.color=Orange;GUI.Label(new Rect(x-30,y,145,24),"CORE "+Mathf.CeilToInt(distance)+"m",small);GUI.color=Color.white;
                }
            }
            if(won&&!skills)
            {
                Panel(new Rect(w/2-280,h/2-75,560,220),new Color(.02f,.05f,.075f,.96f));
                GUI.Label(new Rect(w/2-260,h/2-57,520,42),"THE SKY REMEMBERS",centered);
                GUI.Label(new Rect(w/2-245,h/2-8,490,90),"Ancient Civilization / Chapter 0 complete\nThe fracture was not the end. It was the beginning.\nScore "+score+"    Sentinels defeated "+kills+"    Time "+Mathf.FloorToInt(runTime)+"s",small);
                if(GUI.Button(new Rect(w/2-180,h/2+98,360,30),"New run (erase this prototype's save)"))NewRun();
            }
            GUI.skin.button=oldButton;
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
        public Vector2 WireOrigin {get{return (Vector2)transform.position+new Vector2(direction*(bike?.4f:.14f),bike?.45f:.78f);}}
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
        public float BikeSkid {get;private set;}
        public AncientCombat Combat {get;private set;}
        LineRenderer chargeRing;
                Rigidbody2D body;SpriteRenderer art;AncientMotorcycle motorcycle;
        CapsuleCollider2D heroCollider;BoxCollider2D bikeCollider;LineRenderer wire,wireGlow,wireTarget,absorb;TrailRenderer trail;
        DistanceJoint2D wireJoint;SpriteRenderer hook;SpriteRenderer[] wirePulses;
        LineRenderer wireCuff,wireLatch;
        readonly LineRenderer[] wireHelices=new LineRenderer[2];
        MaterialPropertyBlock armProperties;
        Camera aimCamera;
        readonly Vector3[] cablePoints=new Vector3[2];
        readonly Vector3[][] helixSmall={new Vector3[24],new Vector3[48],new Vector3[96],new Vector3[192]};
        readonly float[] helixNormal=new float[192],helixDepth=new float[192];
        AncientParkedBike mountedBike;
        float direction=1,invulnerableUntil,wireReady,lastGrounded=-100;
        float absorbAt=-10,jumpBufferedUntil=-10,conveyorUntil,conveyorDirection;
        float wireAttachedAt,wireLength,wireMinLength,swingDirection,arcTravel,momentumUntil,lastWireRelease=-10;
        Vector2 previousRadial;int wireChain;bool wireZip;
        AncientEnemy wireEnemy,wireContactTarget;Transform wireMovingAnchor;Vector2 wireMovingLocal;float wireContactUntil;
        bool wired,airJumpUsed;float axis;int combo;
        Sprite[] frames,actions;
        float actionUntil,hurtUntil,ghostAt,landingUntil,fallSpeed,dustAt;
        bool wasGrounded,cutJump;float strikeFacing;
        void Start()
        {
            var g=AncientGame.Instance;aimCamera=g.View;
            ZipMode=true;
            doubleJump=AncientGame.Saved("jump")!=0;
            reservoir=AncientGame.Saved("capacity")!=0;armor=AncientGame.Saved("armor")!=0;hp=MaxHP;
            body=gameObject.AddComponent<Rigidbody2D>();body.freezeRotation=true;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;body.interpolation=RigidbodyInterpolation2D.Interpolate;
            wireJoint=gameObject.AddComponent<DistanceJoint2D>();wireJoint.autoConfigureDistance=false;
            wireJoint.autoConfigureConnectedAnchor=false;wireJoint.maxDistanceOnly=true;wireJoint.enabled=false;
            heroCollider=gameObject.AddComponent<CapsuleCollider2D>();heroCollider.size=new Vector2(.72f,2.1f);
            var frictionless=new PhysicsMaterial2D("Free movement"){friction=0,bounciness=0};heroCollider.sharedMaterial=frictionless;
            bikeCollider=gameObject.AddComponent<BoxCollider2D>();bikeCollider.size=new Vector2(2.85f,1.8f);bikeCollider.offset=new Vector2(0,.085f);bikeCollider.edgeRadius=.01f;bikeCollider.sharedMaterial=frictionless;bikeCollider.enabled=false;
            frames=new Sprite[8];actions=new Sprite[8];for(int i=0;i<frames.Length;i++){frames[i]=AncientArt.Hero(i);actions[i]=AncientArt.Action(i);}
            art=AncientArt.Display("Animated hero",frames[0],new Vector2(0,-1.05f),10,transform);
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
            wirePulses=new SpriteRenderer[48];
            for(int i=0;i<wirePulses.Length;i++)
            {
                var link=AncientArt.Display("Flowing wire particle",AncientArt.WireParticle(),Vector2.zero,19);
                wirePulses[i]=link;link.enabled=false;
            }
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
        float groundSampleTick=-1;Vector2 groundSamplePosition;bool groundSample;Rigidbody2D groundBody;
        bool Grounded()
        {
            Vector2 point=body?body.position:(Vector2)transform.position;
            if(groundSampleTick==Time.fixedTime&&point==groundSamplePosition)return groundSample;
            groundSampleTick=Time.fixedTime;groundSamplePosition=point;
            var hit=Physics2D.BoxCast(point+Vector2.down*(bike?.78f:1f),new Vector2(bike?2.5f:.58f,.12f),0,Vector2.down,.12f,1<<8);
            groundSample=hit.collider!=null;groundBody=hit.collider?hit.collider.attachedRigidbody:null;return groundSample;
        }
        void Update()
        {
            var g=AncientGame.Instance;if(!body)return;
            if(g.won){StopWire(false);body.linearVelocity=Vector2.zero;body.gravityScale=0;absorb.enabled=false;trail.emitting=false;return;}
            if(g.Paused||!g.GameplayInputReady){absorb.enabled=false;if(Combat)Combat.CancelInput();return;}
            if(transform.position.y<(g.Finale&&g.Finale.Active?AncientFinaleRules.Road(transform.position.x)-7:-9)){Respawn();return;}
            bool onGround=Grounded();BikeGrounded=onGround;
            if(onGround)
            {
                if(!wasGrounded&&fallSpeed<-7){g.Burst((Vector2)transform.position+Vector2.down*.8f,new Color(.4f,.57f,.65f),10);g.Sound(7);landingUntil=Time.time+.10f;g.Shake(.025f);}
                fallSpeed=0;lastGrounded=Time.time;airJumpUsed=false;if(!wired&&Time.time>momentumUntil)wireChain=0;
            }
            else fallSpeed=Mathf.Min(fallSpeed,body.linearVelocity.y);
            wasGrounded=onGround;
            axis=Input.GetAxisRaw("Horizontal");if(axis!=0)direction=Mathf.Sign(axis);
            if(Input.GetKeyDown(KeyCode.Space))jumpBufferedUntil=Time.time+.12f;
            if(Input.GetKeyUp(KeyCode.Space))cutJump=true;
            if(Input.GetKeyDown(KeyCode.F))absorbAt=Time.time;
            if(Input.GetKeyDown(KeyCode.LeftControl)||Input.GetKeyDown(KeyCode.RightControl))running=!running;
            if(Input.GetKeyDown(KeyCode.E))
            {
                bool used=g.Story.TryUseLift()||(g.Finale&&g.Finale.TryInteract())||(g.Cases&&g.Cases.TryInteract())||
                    (g.City&&g.City.TryInteract())||(g.Traversal&&g.Traversal.TryInteract());
                if(!used)InteractBike();
                if(g.Paused||!g.GameplayInputReady)return;
            }
            if(Input.GetKeyDown(KeyCode.X)&&bike)ExplodeBike();
            // Directional wheel selection never toggles back repeatedly during a long scroll gesture.
            float wheel=Input.mouseScrollDelta.y;
            if(Mathf.Abs(wheel)>.001f&&(wheel>0)!=AttackMode)
            {StopWire(false);AttackMode=wheel>0;}
            if(Input.GetMouseButtonDown(1))absorbAt=Time.time;
            if(!AttackMode&&(Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(2)))
            {ZipMode=Input.GetMouseButtonDown(0);FireWire();}
            Combat.HandleInput();
            absorb.enabled=false;
            if(Input.GetKey(KeyCode.F))
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
                else if(Time.time<actionUntil)art.sprite=actions[AncientRules.BladeAnimationFrame(Time.time-(actionUntil-AncientRules.BladeDuration))];
                else if(Combat&&(Combat.Charging||Combat.Firing))art.sprite=frames[6];
                else if(!onGround)art.sprite=frames[3];
            }
            float squash=Time.time<landingUntil?.08f:0;
            art.transform.localScale=new Vector3(1+squash,1-squash,1);
            if(wired&&Time.time>=ghostAt){g.Afterimage(art);ghostAt=Time.time+.065f;}
            if(onGround&&Mathf.Abs(body.linearVelocity.x)>7&&Time.time>=dustAt){g.Burst((Vector2)transform.position+Vector2.down*.8f,new Color(.35f,.48f,.55f),2);dustAt=Time.time+.18f;}
            art.transform.localPosition=new Vector3(0,bike?-.25f:-1.05f,0);
            art.color=Time.time<invulnerableUntil&&(int)(Time.time*14)%2==0?new Color(1,1,1,.45f):Color.white;
            art.enabled=!bike;
            trail.emitting=wired||bike;
        }
        public void CinematicPose(bool low,float facing)
        {
            if(!art||frames==null)return;art.sprite=frames[low?4:0];art.flipX=facing<0;art.enabled=true;art.color=Color.white;
            art.transform.localPosition=new Vector3(0,-1.05f,0);
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
        {combo=index;strikeFacing=facing;actionUntil=Time.time+AncientRules.BladeDuration;}
        public void CancelBladePose(){actionUntil=0;}
        public void AddCapturedEnergy(float amount){CaptureEnergy=Mathf.Min(100,CaptureEnergy+amount*(reservoir?1.25f:1));}
        public float SpendCapturedEnergy(){float value=CaptureEnergy;CaptureEnergy=0;return value;}
        public void TravelTo(Vector2 point)
        {
            if(bike)
            {
                if(mountedBike)mountedBike.Park(body.position,direction,bikeHP);
                mountedBike=null;motorcycle.Park();SetBike(false);BikeSkid=0;
            }
            StopWire(false);body.position=point;transform.position=point;body.linearVelocity=Vector2.zero;
            momentumUntil=0;conveyorUntil=0;jumpBufferedUntil=-10;cutJump=false;lastGrounded=-100;groundSampleTick=-1;if(Combat)Combat.ResetCombat();
        }
        Vector2 anchor;
        void FixedUpdate()
        {
            var g=AncientGame.Instance;if(g.won||g.Paused)return;
            Vector2 velocity=body.linearVelocity;
            if(!bike||wired)BikeSkid=0;
            if(wired&&wireMovingAnchor){anchor=wireMovingAnchor.TransformPoint(wireMovingLocal);if(wireJoint)wireJoint.connectedAnchor=anchor;}
            if(wired&&wireEnemy)
            {
                if(!wireEnemy.IsAlive){StopWire(false);velocity=body.linearVelocity;}
                else anchor=wireEnemy.AimPoint;
            }
            if(wired)
            {
                Vector2 delta=anchor-body.position;Vector2 radial=delta.normalized;
                float age=Time.time-wireAttachedAt;
                if(wireZip)
                {
                    float speed=Mathf.Clamp(12+delta.magnitude*1.2f,16,30);
                    velocity=Vector2.MoveTowards(velocity,radial*speed,95*Time.fixedDeltaTime);
                    bool blocked=!VisibleAnchor(anchor);
                    Vector2 travel=velocity*Time.fixedDeltaTime;
                    bool enemyImpact=wireEnemy&&AncientRules.WireImpactOnStep(delta.x,delta.y,travel.x,travel.y,blocked);
                    if(enemyImpact)
                    {
                        var enemy=wireEnemy;wireContactTarget=enemy;wireContactUntil=Time.time+.3f;
                        enemy.WireImpact(radial.x<0?-1:1);StopWire(false);
                        velocity=new Vector2(-radial.x*2,Mathf.Max(3,velocity.y*.15f));body.linearVelocity=velocity;
                    }
                    else if(blocked||delta.magnitude<(wireEnemy?1.05f:1.8f)||age>1.25f)
                    {body.linearVelocity=velocity;StopWire(!blocked);velocity=body.linearVelocity;}
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
            else
            {
                bool grounded=Grounded();
                float surface=grounded&&groundBody&&groundBody.bodyType==RigidbodyType2D.Kinematic?groundBody.linearVelocity.x:0;
                bool reversing=bike&&grounded&&Mathf.Abs(axis)>.1f&&axis*velocity.x<0&&Mathf.Abs(velocity.x)>3;
                BikeSkid=Mathf.MoveTowards(BikeSkid,reversing?Mathf.Clamp01(Mathf.Abs(velocity.x)/10):0,Time.fixedDeltaTime*7);
                if(bike)
                {
                    bool brake=Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow);
                    float target=(brake?0:axis*17)+surface;
                    float rate=brake?60:Mathf.Abs(axis)<.01f?11:reversing?23:28;
                    if(!grounded)rate=brake?12:8;
                    bool carry=!grounded&&!brake&&Time.time<momentumUntil&&Mathf.Abs(velocity.x)>17&&(axis==0||Mathf.Sign(axis)==Mathf.Sign(velocity.x));
                    if(!carry&&(axis!=0||grounded||brake))velocity.x=Mathf.MoveTowards(velocity.x,target,rate*Time.fixedDeltaTime);
                }
                else
                {
                    float speed=running?9:5;
                    bool carry=!grounded&&Time.time<momentumUntil&&Mathf.Abs(velocity.x)>speed&&(axis==0||Mathf.Sign(axis)==Mathf.Sign(velocity.x));
                    float belt=grounded&&Time.time<conveyorUntil?conveyorDirection*3.5f:0;
                    if(!carry&&(axis!=0||grounded))velocity.x=Mathf.MoveTowards(velocity.x,axis*speed+belt+surface,(grounded?70:22)*Time.fixedDeltaTime);
                }
            }
            if(Time.time<jumpBufferedUntil)
            {
                if(Time.time-lastGrounded<.12f)
                {StopWire(false);velocity.y=14;lastGrounded=-100;jumpBufferedUntil=-10;}
                else if(doubleJump&&!airJumpUsed)
                {StopWire(false);velocity.y=12;airJumpUsed=true;jumpBufferedUntil=-10;g.Burst(transform.position,AncientGame.Cyan,10);}
            }
            if(cutJump&&velocity.y>4&&!wired&&!bike)velocity.y*=.55f;
            cutJump=false;body.gravityScale=wired?(wireZip?.15f:1):(velocity.y<0?1.2f:1);
            velocity.y=Mathf.Max(velocity.y,-25);body.linearVelocity=velocity;
            if(g.Story&&g.Story.Stage<4)
            {Vector2 p=body.position;p.x=Mathf.Clamp(p.x,-3,19);body.position=p;}
            if(g.ArenaActive)
            {
                Vector2 p=body.position;p.x=Mathf.Clamp(p.x,g.boss.IsDistrictBoss?AncientCityLayout.ArenaLeft(g.boss.DistrictFloor):98,g.boss.IsDistrictBoss?AncientCityLayout.ArenaRight(g.boss.DistrictFloor):117);body.position=p;
            }
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
        bool PickWireEnemy(out AncientEnemy selected)
        {
            selected=null;if(!ZipMode||!aimCamera)return false;
            Vector2 pixel=Input.mousePosition;
            if(pixel.x<0||pixel.y<0||pixel.x>=Screen.width||pixel.y>=Screen.height)return false;
            float best=58*Mathf.Clamp(Screen.height/900f,.8f,1.4f);
            Vector2 hand=(Vector2)transform.position+Vector2.up*.35f;
            foreach(var enemy in AncientGame.Instance.enemies)
            {
                if(!enemy||!enemy.IsAlive)continue;
                Vector2 point=enemy.AimPoint;float distance=(point-hand).sqrMagnitude;
                if(distance>22*22||!VisibleAnchor(point))continue;
                Vector3 screen=aimCamera.WorldToScreenPoint(point);if(screen.z<=0)continue;
                float score=Vector2.Distance(pixel,new Vector2(screen.x,screen.y));
                if(score>=best)continue;best=score;selected=enemy;
            }
            return selected!=null;
        }
        void FireWire()
        {
            if(Time.time<wireReady)return;
            Vector2 selected;var g=AncientGame.Instance;AncientEnemy target;
            if(PickWireEnemy(out target))selected=target.AimPoint;
            else if(!PickAnchor(out selected)){g.Notice("적 또는 지형을 겨냥해 좌클릭으로 견인 / 가운데 클릭으로 지형 스윙 / 우클릭은 입자 수집");return;}
            bool different=Vector2.Distance(selected,anchor)>.5f;
            if(wired&&!different&&wireZip==ZipMode)return;
            bool chain=(wired||Time.time-lastWireRelease<1.2f)&&different;
            if(wired)StopWire(false);
            wireChain=chain?Mathf.Min(wireChain+1,5):1;
            if(g.Traversal)g.Traversal.TryMovingAnchor(selected,out wireMovingAnchor);
            wireMovingLocal=Vector2.zero;
            if(!wireMovingAnchor)
            {
                var moving=Physics2D.OverlapPoint(selected+Vector2.down*.04f,1<<8);
                if(moving&&moving.attachedRigidbody&&moving.attachedRigidbody.bodyType==RigidbodyType2D.Kinematic)
                {wireMovingAnchor=moving.attachedRigidbody.transform;wireMovingLocal=wireMovingAnchor.InverseTransformPoint(selected);}
            }
            anchor=selected;wireEnemy=target;wired=true;wireZip=ZipMode;wireAttachedAt=Time.time;wireReady=Time.time+(target?.45f:.18f);
            if(wireEnemy)
            {
                wireContactTarget=wireEnemy;wireContactUntil=Time.time+.3f;
                wireEnemy.Hit(AncientRules.WireHitDamage(wireEnemy.IsBoss));
            }
            Vector2 delta=anchor-body.position;Vector2 radial=delta.normalized;
            swingDirection=Mathf.Abs(body.linearVelocity.x)>2?Mathf.Sign(body.linearVelocity.x):direction;
            previousRadial=-radial;arcTravel=0;
            wireLength=delta.magnitude;wireMinLength=Mathf.Min(wireLength,Mathf.Clamp(wireLength*.68f,3.2f,8));
            wireJoint.connectedAnchor=anchor;wireJoint.distance=wireLength;wireJoint.enabled=!wireZip;
            Vector2 velocity=body.linearVelocity+radial*11+new Vector2(radial.y,-radial.x)*swingDirection*5;
            velocity.x=swingDirection*Mathf.Max(12+Mathf.Min(4,wireChain),velocity.x*swingDirection);
            velocity.y=Mathf.Max(8,velocity.y);
            if(wireZip)velocity=radial*Mathf.Clamp(body.linearVelocity.magnitude+8,18,28);
            body.linearVelocity=Vector2.ClampMagnitude(velocity,30);
            airJumpUsed=false;g.Sound(3);g.Burst(anchor,AncientGame.Cyan,14);g.Burst(transform.position,AncientGame.Cyan,8);g.Shake(.045f);
        }
        void StopWire(bool launch)
        {
            bool wasWired=wired;
            if(wireEnemy){wireContactTarget=wireEnemy;wireContactUntil=Time.time+.3f;}
            wireEnemy=null;wireMovingAnchor=null;wired=false;if(wireJoint)wireJoint.enabled=false;
            if(!wasWired)return;
            lastWireRelease=Time.time;momentumUntil=Time.time+.7f;
            if(launch&&body)
            {
                Vector2 velocity=body.linearVelocity;
                if(wireZip)
                {
                    float facing=Mathf.Abs(velocity.x)>.1f?Mathf.Sign(velocity.x):direction;
                    velocity.x=facing*Mathf.Max(8,Mathf.Abs(velocity.x));velocity.y=Mathf.Max(5,velocity.y);
                }
                else
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
            float armPulse=Capturing?1:wired?.65f:Combat?Combat.ArmActivity:0;
            armProperties.SetFloat("_ArmCharge",armCharge);armProperties.SetFloat("_ArmPulse",armPulse);
            art.SetPropertyBlock(armProperties);motorcycle.SetArmPower(armCharge,armPulse);
            chargeRing.enabled=CaptureEnergy>0&&!g.won;
            if(chargeRing.enabled)
            {
                float radius=.45f+CaptureEnergy/100f*.85f;
                chargeRing.startWidth=chargeRing.endWidth=.025f+CaptureEnergy*.0007f;
                for(int i=0;i<chargeRing.positionCount;i++)
                {
                    float a=i*Mathf.PI*2/chargeRing.positionCount+Time.time*2;
                    chargeRing.SetPosition(i,transform.position+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius);
                }
            }
            bool visible=wired&&!g.won;
            wire.enabled=wireGlow.enabled=hook.enabled=visible;
            wireCuff.enabled=wireLatch.enabled=visible;
            foreach(var helix in wireHelices)helix.enabled=visible;
            if(!visible)foreach(var link in wirePulses)link.enabled=false;
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
                for(int strand=0;strand<2;strand++)
                {
                    var points=helixSmall[buffer];
                    AncientWireMath.Fill(length,age,strand,samples,helixNormal,helixDepth);
                    for(int i=0;i<samples;i++)
                    {
                        Vector2 point=Vector2.Lerp(hand,end,i/(float)(samples-1))+normal*helixNormal[i];
                        points[i]=new Vector3(point.x,point.y,helixDepth[i]);
                    }
                    wireHelices[strand].positionCount=samples;wireHelices[strand].SetPositions(points);
                }
                hook.transform.position=end;hook.transform.rotation=Quaternion.Euler(0,0,age*90);
                hook.transform.localScale=Vector3.one*(.18f+Mathf.Exp(-age*12)*.08f);
                int particleCount=Mathf.Clamp(Mathf.CeilToInt(length*1.2f),6,AncientVisuals.HighQuality?24:12);
                for(int i=0;i<wirePulses.Length;i++)
                {
                    var pulse=wirePulses[i];pulse.enabled=i<particleCount;if(i>=particleCount)continue;
                    float t=Mathf.Repeat(age*1.6f+i/(float)particleCount,1);
                    int strand=i%2;
                    pulse.transform.position=HelixPoint(hand,end,age,t,strand);
                    pulse.transform.rotation=Quaternion.Euler(0,0,0);
                    float sparkle=.5f+.5f*Mathf.Sin(age*17+i*2);
                    pulse.transform.localScale=Vector3.one*(.055f+sparkle*.035f);
                    pulse.color=Color.Lerp(new Color(.2f,.65f,1),Color.white,sparkle*.7f);
                }
                for(int i=0;i<wireCuff.positionCount;i++)
                {
                    float a=i*Mathf.PI*2/wireCuff.positionCount-age*3;
                    wireCuff.SetPosition(i,hand+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.16f);
                }
                for(int i=0;i<wireLatch.positionCount;i++)
                {
                    float a=i*Mathf.PI*2/wireLatch.positionCount+age;
                    wireLatch.SetPosition(i,end+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(.22f+Mathf.Exp(-age*9)*.2f));
                }
            }
            Vector2 target=Vector2.zero;AncientEnemy enemyTarget=null;
            bool hasTarget=false;
            if(!AttackMode&&!g.Paused&&!g.won)
            {
                if(PickWireEnemy(out enemyTarget)){target=enemyTarget.AimPoint;hasTarget=true;}
                else hasTarget=PickAnchor(out target);
            }
            wireTarget.startColor=wireTarget.endColor=enemyTarget?AncientGame.Orange:AncientGame.Cyan;
            HasWireTarget=hasTarget;WireTarget=target;wireTarget.enabled=hasTarget;
            if(hasTarget)
            {
                wireTarget.positionCount=ZipMode?4:8;
                float radius=.43f+Mathf.Sin(Time.time*7)*.045f;
                for(int i=0;i<wireTarget.positionCount;i++)
                {
                    float a=Mathf.PI*2*i/wireTarget.positionCount;
                    wireTarget.SetPosition(i,target+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);
                }
            }
        }
        Vector2 CablePoint(Vector2 hand,Vector2 end,float age,float t)
        {
            return Vector2.Lerp(hand,end,t);
        }
        Vector3 HelixPoint(Vector2 hand,Vector2 end,float age,float t,int strand)
        {
            Vector2 normal=Vector2.Perpendicular(end-hand).normalized;
            float length=Vector2.Distance(hand,end);
            float offset,depth;AncientRules.WireHelixOffset(length,age,t,strand,out offset,out depth);
            Vector2 point=Vector2.Lerp(hand,end,t)+normal*offset;
            return new Vector3(point.x,point.y,depth);
        }
        public void AbsorbShot(AncientProjectile shot)
        {
            var g=AncientGame.Instance;bool perfect=Time.time-absorbAt<.18f;
            particles=Mathf.Min(MaxParticles,particles+(shot.scrap?18:12)*(perfect?1.5f:1));
            Combat.EmitParticles(shot.transform.position,Mathf.Clamp(shot.damage,8,25));
            g.Sound(2);g.score+=perfect?30:10;g.Burst(shot.transform.position,AncientGame.Cyan,10);
            if(perfect)g.Notice("PERFECT ABSORB / bonus particles",.75f);shot.Remove();
        }
        public void HurtContact(AncientEnemy source,float damage)
        {
            if(AncientRules.IgnoreWireContact(source&&source==wireContactTarget,wired&&wireZip&&wireEnemy==source,wireContactUntil,Time.time))return;
            Hurt(damage);
        }
        public void Hurt(float damage)
        {
            var g=AncientGame.Instance;if(Time.time<invulnerableUntil||g.won)return;
            invulnerableUntil=Time.time+.65f;g.Shake(.14f);
            if(bike){bikeHP=Mathf.Max(0,bikeHP-damage);motorcycle.Damage();if(bikeHP<=0)ExplodeBike();return;}
            if(Combat)Combat.CancelBlade();actionUntil=0;hurtUntil=Time.time+.18f;
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
                g.Notice("MOTORCYCLE PARKED / E to ride again.",1.5f);return;
            }
            AncientParkedBike nearest=null;float best=2.8f*2.8f;
            foreach(var vehicle in g.parkedBikes)
            {
                if(!vehicle||!vehicle.Available)continue;
                float distance=((Vector2)vehicle.transform.position-body.position).sqrMagnitude;
                if(distance<best&&!Physics2D.Linecast(body.position,vehicle.transform.position,1<<8)){nearest=vehicle;best=distance;}
            }
            if(!nearest){g.Notice("Find a parked motorcycle and press E.",1.5f);return;}
            Vector2 seat=nearest.transform.position;
            if(Physics2D.OverlapBox(seat+Vector2.up*.085f,new Vector2(2.87f,1.77f),0,1<<8))
            {g.Notice("The motorcycle needs clear space before you can ride.");return;}
            StopWire(false);mountedBike=nearest;bikeHP=nearest.Health;direction=nearest.Facing;
            nearest.Take();SetBike(true);body.position=seat;body.linearVelocity=Vector2.zero;BikeSkid=0;
            momentumUntil=0;motorcycle.Mount();g.Sound(3);
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
            g.Notice("EXPLOSION LAUNCH / connect the next anchor.",2);
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
            if(wirePulses!=null)foreach(var pulse in wirePulses)if(pulse)Destroy(pulse.gameObject);
            if(absorb)Destroy(absorb.gameObject);if(chargeRing)Destroy(chargeRing.gameObject);
        }
    }

    public sealed class AncientEnemy : MonoBehaviour
    {
        public int kind;
        public int DistrictFloor {get;private set;}
        public bool IsDistrictBoss {get;private set;}
        public bool IsBoss {get{return kind==3||IsDistrictBoss;}}
        public void ConfigureDistrict(int floor,bool boss)
        {DistrictFloor=floor;IsDistrictBoss=boss;if(boss)maxHP=hp=floor==4?260:220+floor*50;}
        float hp=60,maxHP=60,nextAttack,resolveAt=-1,flashUntil,walkPhase,faceLockUntil,guardBrokenUntil,staggerUntil;
        int pattern,attackIndex,volleyRemaining;bool dead;
        float volleyAt,windupStartedAt,attackPoseUntil,recoveryStarts,recoveryUntil,recoveryDuration;bool enragedAnnounced;
        public bool IsTelegraphing {get{return resolveAt>=0;}}
        public bool Recovering {get{return IsBoss&&Time.time>=recoveryStarts&&Time.time<recoveryUntil;}}
        public float RecoveryRemaining {get{return Recovering?Mathf.Clamp01((recoveryUntil-Time.time)/Mathf.Max(.1f,recoveryDuration)):0;}}
        public bool AttackIsPhysical {get{return pattern==0;}}
        public float WindupProgress {get{return resolveAt<0?0:Mathf.Clamp01((Time.time-windupStartedAt)/Mathf.Max(.01f,resolveAt-windupStartedAt));}}
        SpriteRenderer art;LineRenderer warning;
        Vector2 home,lockedAim;float facing=1;
        public Vector2 AimPoint {get{return (Vector2)transform.position+Vector2.up*(kind==3?1.65f:kind==4?.65f:.95f);}}
        public bool IsAlive {get{return !dead;}}
        float wirePushRemaining;
        public float HealthRatio {get{return maxHP>0?hp/maxHP:1;}}
        public bool Enraged {get{return kind==3&&hp<maxHP*.5f;}}
        public string Role {get{if(IsDistrictBoss)return DistrictFloor==1?"밀매 조직 경비장":DistrictFloor==2?"기록 집행관":DistrictFloor==3?"코어 보안 병기":"저격수";return kind==0?"LANCER":kind==1?"GUNNER":kind==2?"BOMBER":kind==3?"CARGO ENFORCER":kind==4?"DRONE":"SHIELD WARDEN";}}
        public void FaceToward(float x){facing=x<transform.position.x?-1:1;if(art)art.flipX=facing<0;}
        public void PrepareMiniBoss(){maxHP=hp=240;}
        public void Initialize(int type)
        {kind=type;maxHP=hp=kind==3?560:kind==5?115:kind==4?42:kind==0?70:55;}
        void Start()
        {
            var g=AncientGame.Instance;home=transform.position;
            art=AncientArt.Display(Role,AncientArt.Enemy(kind),Vector2.zero,8,transform);art.flipX=facing<0;
            if(AncientVisuals.Instance)
            {
                AncientVisuals.Instance.Rim(art);
                AncientVisuals.Instance.Glow(new Vector2(0,kind==4?.6f:1.1f),kind==3||kind==5?AncientGame.Cyan:AncientGame.Orange,kind==3?2.5f:1.1f,9,transform);
            }
            warning=g.Line("Attack telegraph",AncientGame.Orange,.045f,6);warning.enabled=false;
            nextAttack=Time.time+(IsDistrictBoss&&DistrictFloor==4?1.6f:kind==3?1.3f:1+kind*.2f);
        }
        bool Step(float distance)
        {
            float x=Mathf.Clamp(transform.position.x+distance,IsDistrictBoss?AncientCityLayout.ArenaLeft(DistrictFloor)+1:kind==3?100:home.x-2,IsDistrictBoss?AncientCityLayout.ArenaRight(DistrictFloor)-1:kind==3?114:home.x+2);
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
            // Body contact is separate from telegraphed attacks and projectiles.
            Vector2 contact=(Vector2)g.hero.transform.position-AimPoint;
            if(!Recovering&&Mathf.Abs(contact.x)<(kind==3?1.1f:.65f)&&Mathf.Abs(contact.y)<1)
                g.hero.HurtContact(this,kind==3?14:8);
            if(Enraged&&!enragedAnnounced)
            {enragedAnnounced=true;g.Sound(8);g.Notice("WARDEN / OVERDRIVE",2);g.Impact(AimPoint,AncientGame.Orange,true);g.Shake(.12f);}
            float anticipation=IsTelegraphing?WindupProgress:0;
            float recoil=Mathf.Clamp01((attackPoseUntil-Time.time)/.18f);
            art.transform.localScale=new Vector3(1-anticipation*.035f,1+anticipation*.035f,1);
            art.transform.localRotation=Quaternion.Euler(0,0,IsTelegraphing?-facing*anticipation*4:facing*recoil*4);
            if(kind==4)transform.position=home+new Vector2(Mathf.Sin(Time.time*.75f+home.x)*2.2f,Mathf.Sin(Time.time*1.8f)*.55f);
            if(volleyRemaining>0&&Time.time>=volleyAt)
            {FireProjectile(lockedAim,12,false,0);volleyRemaining--;volleyAt=Time.time+.14f;}
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
                        recoveryStarts=Time.time+volleyRemaining*(DistrictFloor==2?.2f:.14f);
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
            {FireProjectile(lockedAim,IsDistrictBoss?18:12,false,0);volleyRemaining=IsDistrictBoss?0:2;volleyAt=Time.time+.14f;}
            else if(IsDistrictBoss&&DistrictFloor==2)
            {FireProjectile(lockedAim,14,false,0);volleyRemaining=2;volleyAt=Time.time+.2f;}
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
                if(!breakGuard){damage*=.15f;g.Burst(AimPoint+Vector2.right*facing*.6f,AncientGame.Cyan,8);g.Notice("SHIELD / Flank, finish a combo, or charge a laser to break guard.",1.2f);}
                else{guardBrokenUntil=Time.time+2;staggerUntil=Time.time+.8f;resolveAt=-1;g.Notice("GUARD BROKEN",.8f);}
            }
            if(breakGuard&&!IsBoss){resolveAt=-1;staggerUntil=Mathf.Max(staggerUntil,Time.time+.4f);nextAttack=Mathf.Max(nextAttack,Time.time+.65f);volleyRemaining=0;}
            bool punish=Recovering&&IsBoss;damage=AncientCombatRules.Damage(damage,IsBoss,punish);
            hp-=damage;flashUntil=Time.time+.1f;if(g.Feel)g.Feel.Damage(AimPoint,damage,punish);
            g.Sound(6);g.HitEmphasis(breakGuard);g.Impact(AimPoint,breakGuard?AncientGame.Orange:AncientGame.Cyan,breakGuard);
            g.Burst(AimPoint,AncientGame.Cyan,7);g.Shake(.045f);g.score+=Mathf.RoundToInt(damage);
            if(hp>0)return;
            if(IsDistrictBoss&&DistrictFloor==4)
            {
                dead=true;g.enemies.Remove(this);g.City.BossDefeated(DistrictFloor);Destroy(gameObject);return;
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
            else if(kind==3){g.bossDefeated=true;g.boss=null;g.Save();g.Notice("WARDEN DEFEATED\nThe eastern gate is open.",7);}
            Destroy(gameObject);
        }
        void OnDestroy(){if(warning)Destroy(warning.gameObject);}
    }
    public sealed class AncientProjectile : MonoBehaviour
    {
        public bool scrap;public int metalKind;public Vector2 velocity;public float damage=15,gravity;
        float age;bool removed=true;SpriteRenderer art;AncientGame game;
        public bool Available {get{return removed;}}
        public void Initialize(AncientProjectilePool pool)
        {game=pool.Game;art=AncientArt.Display("Reusable metal object",AncientArt.MetalProjectile(0),Vector2.zero,12,transform);}
        public void Play(Vector2 position,Vector2 speed,float amount,bool isScrap,int kind,float acceleration)
        {
            scrap=isScrap;metalKind=Mathf.Clamp(kind,0,2);velocity=speed;damage=amount;gravity=acceleration;age=0;removed=false;
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
            if(Physics2D.Linecast(previous,next,1<<8)){game.Burst(previous,new Color(.65f,.48f,.3f),8);Remove();return;}
            if(game.hero&&age>.05f)
            {
                Vector2 h=game.hero.transform.position,segment=next-previous;
                float t=segment.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(h-previous,segment)/segment.sqrMagnitude):0;
                if(game.hero.bike?game.hero.BikeProjectileHit(previous,next):Vector2.Distance(h,previous+segment*t)<.75f)
                {
                    if(Input.GetKey(KeyCode.F))game.hero.AbsorbShot(this);else{game.hero.Hurt(damage);Remove();}return;
                }
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
                g.Notice("ENERGY CORE "+g.Cores+" / 3\nNew engineering tiers available. K to upgrade.",5);
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
