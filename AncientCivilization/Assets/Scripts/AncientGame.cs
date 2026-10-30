using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.AncientCivilization
{
    public sealed class AncientGame : MonoBehaviour
    {
        public static AncientGame Instance;
        public AncientHero hero;
        public readonly List<Vector2> anchors = new List<Vector2>();
        public readonly List<AncientEnemy> enemies = new List<AncientEnemy>();
        public readonly List<AncientProjectile> shots = new List<AncientProjectile>();
        public readonly List<AncientPickup> pickups = new List<AncientPickup>();
        public AncientEnemy boss;
        public bool skills, won, bossDefeated, showHelp = true;
        public bool Paused {get {return skills;}}
        public int coreMask, credits, kills, score;
        public int Cores {get {return ((coreMask&1)!=0?1:0)+((coreMask&2)!=0?1:0)+((coreMask&4)!=0?1:0);}}
        public bool ArenaActive {get {return boss && !bossDefeated;}}
        public Vector2 checkpoint = new Vector2(2, 1.4f);
        public static readonly Color Cyan = new Color(.18f,.92f,1);
        public static readonly Color Orange = new Color(1,.43f,.14f);
        const string SaveKey = "PLUGIN.Ancient.v2.";
        Camera view; Sprite square; Transform backdrop;
        GUIStyle small, title, centered;
        string message; float messageUntil, shake, runTime;
        int checkpointStage; bool endingShown;
        Material lineMaterial; AncientAudio audioCues;
        readonly List<AncientFragment> fragmentPool=new List<AncientFragment>();
        readonly List<AncientAfterimage> ghostPool=new List<AncientAfterimage>();
        int fragmentCursor,ghostCursor;float hitPauseUntil,damageFlash,healthEcho=100;
        Vector3 cameraFollow;
        public float DamageFlash {get{return damageFlash;}}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(!Object.FindFirstObjectByType<AncientGame>())new GameObject("Ancient Civilization").AddComponent<AncientGame>();
        }
        public static int Saved(string key,int fallback=0){return PlayerPrefs.GetInt(SaveKey+key,fallback);}
        public static void SetSaved(string key,int value){PlayerPrefs.SetInt(SaveKey+key,value);}
        void Awake()
        {
            Instance=this; audioCues=gameObject.AddComponent<AncientAudio>(); Time.timeScale=1; Physics2D.gravity=new Vector2(0,-32);
            coreMask=Saved("cores"); credits=Saved("credits"); checkpointStage=Saved("checkpoint");
            bossDefeated=Saved("boss")!=0; kills=Saved("kills");score=Saved("score");
            if(checkpointStage==1)checkpoint=new Vector2(46,2.3f);
            if(checkpointStage==2)checkpoint=new Vector2(86,1.3f);
            square=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            lineMaterial=new Material(Shader.Find("Sprites/Default"));
            view=new GameObject("Pixel Camera").AddComponent<Camera>();view.tag="MainCamera";view.gameObject.AddComponent<AudioListener>();
            view.orthographic=true;view.orthographicSize=5.8f;view.gameObject.AddComponent<AncientCameraFX>();view.backgroundColor=new Color(.025f,.04f,.07f);
            view.transform.position=new Vector3(checkpoint.x+4,4,-10);cameraFollow=view.transform.position;
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
            BuildWorld();
            hero=new GameObject("Mechanical Arm Hero").AddComponent<AncientHero>();hero.transform.position=checkpoint;
            gameObject.AddComponent<AncientVisuals>();
            Notice(checkpointStage==0?"SECTOR 01 / THE BURIED CITY\nRecover three cores. Reach the eastern sanctum.":"Checkpoint restored. The city remembers.",7);
        }
        void BuildWorld()
        {
            backdrop=new GameObject("City parallax").transform;
            for(int i=-1;i<=1;i++)
            {
                var image=AncientArt.Display("Ancient city panorama",AncientArt.City(),new Vector2(i*48,0),-30,backdrop);
                image.flipX=i!=0;
            }
            Platform(-5,20,0);Platform(22,38,0);Platform(43,58,1);
            Platform(63,80,0);Platform(85,118,0);
            Platform(10,16,3);Platform(28,33,4);Platform(48,55,5);Platform(69,75,4);Platform(87,93,5);
            foreach(Vector2 p in new [] {new Vector2(18,10),new Vector2(30,12),new Vector2(41,11),new Vector2(58,12),new Vector2(69,11),new Vector2(81,12),new Vector2(94,12),new Vector2(104,10),new Vector2(113,10)})
            {
                anchors.Add(p);
                var support=AncientArt.Display("Hanging industrial support",AncientArt.World(1),p+Vector2.down*.2f,-2);
                support.transform.localScale=Vector3.one*.75f;
                var node=AncientArt.Display("Cyan wire anchor",AncientArt.World(2),p,3);
                node.transform.localScale=Vector3.one*.48f;
            }
            AncientArt.Display("Eastern gate",AncientArt.World(3),new Vector2(115,0),-1);
            SpawnEnemy(14,3,0);SpawnEnemy(27,0,1);SpawnEnemy(35,0,2);
            SpawnEnemy(49,1,5);SpawnEnemy(51,5,1);SpawnEnemy(60,6,4);SpawnEnemy(66,0,0);SpawnEnemy(74,4,2);
            SpawnEnemy(90,5,1);SpawnEnemy(93,0,5);SpawnEnemy(100,7,4);
            SpawnPickup(new Vector2(31,5),0);SpawnPickup(new Vector2(72,5),1);SpawnPickup(new Vector2(90,6),2);
            for(int i=0;i<8;i++)SpawnPickup(new Vector2(7+i*12,1.6f),-1);
            Beacon(new Vector2(46,1));Beacon(new Vector2(86,0));
        }
        void Beacon(Vector2 p)
        {
            var a=AncientArt.Display("Checkpoint beacon",AncientArt.World(2),p+Vector2.up*.65f,2);
            a.transform.localScale=Vector3.one*.55f;
        }
        void Platform(float left,float right,float top)
        {
            bool tall=top<=1;float depth=tall?3.9375f:1;
            var floor=new GameObject("Solid detailed rooftop");floor.layer=8;
            floor.transform.position=new Vector2((left+right)/2,top-depth*.5f);
            var collider=floor.AddComponent<BoxCollider2D>();collider.size=new Vector2(right-left,depth);
            for(float x=left;x<right;x+=6)
            {
                float width=Mathf.Min(6,right-x);
                AncientArt.Display("High detail industrial facade",AncientArt.Rooftop(width,tall),new Vector2(x+width/2,top),0);
                if(width>2)
                {
                    Box("Wet cyan glint",new Vector2(x+1.3f,top-.015f),new Vector2(.6f,.025f),new Color(.3f,.75f,.85f,.5f),3);
                    Box("Wet amber glint",new Vector2(x+width-.6f,top-.02f),new Vector2(.35f,.02f),new Color(1,.5f,.2f,.35f),3);
                }
            }
        }
        public GameObject Box(string name,Vector2 pos,Vector2 size,Color color,int order)
        {
            var o=new GameObject(name);o.transform.position=pos;o.transform.localScale=new Vector3(size.x,size.y,1);
            var s=o.AddComponent<SpriteRenderer>();s.sprite=square;s.color=color;s.sortingOrder=order;return o;
        }
        public LineRenderer Line(string name,Color color,float width,int order=15)
        {
            var line=new GameObject(name).AddComponent<LineRenderer>();line.material=lineMaterial;
            line.startColor=line.endColor=color;line.startWidth=line.endWidth=width;line.positionCount=2;line.sortingOrder=order;
            return line;
        }
        public void Slash(Vector2 p,float facing,bool heavy)
        {
            var line=Line("Arm strike arc",heavy?Orange:Cyan,heavy?.11f:.065f,18);line.positionCount=9;
            float radius=heavy?2.5f:1.8f;
            for(int i=0;i<9;i++){float a=Mathf.Lerp(-1.1f,1.1f,i/8f);line.SetPosition(i,p+new Vector2(Mathf.Cos(a)*facing,Mathf.Sin(a))*radius);}
            Destroy(line.gameObject,.12f);
        }
        public void SpawnEnemy(float x,float feet,int kind)
        {
            var e=new GameObject(kind==3?"Sanctum Warden":"Sentinel "+kind).AddComponent<AncientEnemy>();
            e.Initialize(kind);e.transform.position=new Vector2(x,feet);enemies.Add(e);
            if(kind==3)boss=e;
        }
        public void SpawnPickup(Vector2 p,int core=-1)
        {
            if(core>=0&&(coreMask&(1<<core))!=0)return;
            var item=new GameObject(core>=0?"Ancient core":"Particle cache").AddComponent<AncientPickup>();
            item.transform.position=p;item.core=core;pickups.Add(item);
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
        {
            var ring=Line("Impact ring",color,heavy?.06f:.035f,19);ring.loop=true;ring.positionCount=20;
            var effect=ring.gameObject.AddComponent<AncientImpactRing>();effect.Begin(pos,color,heavy?1.4f:.65f);
        }
        public void Notice(string text,float seconds=3){message=text;messageUntil=Time.unscaledTime+seconds;}
        public void Shake(float amount){shake=Mathf.Max(shake,amount);}
        public void Sound(int cue){if(audioCues)audioCues.Play(cue);}
        public void Save()
        {
            SetSaved("cores",coreMask);SetSaved("credits",credits);SetSaved("checkpoint",checkpointStage);
            SetSaved("boss",bossDefeated?1:0);SetSaved("kills",kills);SetSaved("score",score);
            if(hero)hero.SaveUpgrades();PlayerPrefs.Save();
        }
        public void Checkpoint(int stage)
        {
            if(stage<=checkpointStage)return;
            checkpointStage=stage;checkpoint=stage==1?new Vector2(46,2.3f):new Vector2(86,1.3f);
            hero.hp=hero.MaxHP;hero.particles=Mathf.Max(hero.particles,60);Save();
            Burst(checkpoint,Cyan,28);Notice("CHECKPOINT / Health restored. Progress saved.",4);
        }
        public void HeroDied()
        {
            if(boss){enemies.Remove(boss);Destroy(boss.gameObject);boss=null;}
            foreach(var shot in shots.ToArray())if(shot)shot.Remove();
            hero.transform.position=checkpoint;Save();Notice("Reconstructed at checkpoint. Your recovered cores are safe.",4);
        }
        void ToggleSkills(){skills=!skills;Time.timeScale=skills?0:1;}
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.K)||Input.GetKeyDown(KeyCode.Escape))ToggleSkills();
            if(Input.GetKeyDown(KeyCode.H))showHelp=!showHelp;
            Time.timeScale=skills?0:Time.unscaledTime<hitPauseUntil?.12f:1;
            damageFlash=Mathf.MoveTowards(damageFlash,0,Time.unscaledDeltaTime*2.8f);
            if(hero)healthEcho=hero.hp>=healthEcho?hero.hp:Mathf.MoveTowards(healthEcho,hero.hp,Time.unscaledDeltaTime*34);
            if(!skills && !won)runTime+=Time.deltaTime;
            if(hero&&hero.transform.position.x>44&&hero.transform.position.x<55&&hero.transform.position.y<3)Checkpoint(1);
            if(hero&&hero.transform.position.x>85&&hero.transform.position.x<94&&hero.transform.position.y<2)Checkpoint(2);
            if(hero && !bossDefeated && !boss && Cores==3 && hero.transform.position.x>98)
            {SpawnEnemy(109,0,3);Notice("SANCTUM WARDEN\nOrange strike: evade. Cyan bolts: absorb and counter.",6);Shake(.18f);}
            if(hero && hero.transform.position.x>113 && !won)
            {
                if(Cores<3)Notice("Recover all three ancient cores to awaken the gate.");
                else if(!bossDefeated)Notice("The warden still guards the sanctum.");
                else {won=true;Save();Notice("THE SKY REMEMBERS\nThe fracture was not the end. It was the beginning.",9999);}
            }
            if(won && !endingShown){endingShown=true;Burst(hero.transform.position,Cyan,60);}
        }
        void LateUpdate()
        {
            if(!hero)return;
            float speed=Mathf.Abs(hero.HorizontalSpeed);
            float zoom=hero.Wired?7.5f:hero.bike?6.8f:5.8f;
            view.orthographicSize=Mathf.Lerp(view.orthographicSize,zoom,1-Mathf.Exp(-3*Time.unscaledDeltaTime));
            float lead=hero.Facing*Mathf.Lerp(2.5f,4,Mathf.Clamp01(speed/25));
            Vector3 target=new Vector3(Mathf.Clamp(hero.transform.position.x+lead,5,113),Mathf.Max(4,hero.transform.position.y+1),-10);
            cameraFollow=Vector3.Lerp(cameraFollow,target,1-Mathf.Exp(-7*Time.unscaledDeltaTime));
            var p=cameraFollow;if(shake>0&&!skills){p+=(Vector3)(Random.insideUnitCircle*shake);shake=Mathf.MoveTowards(shake,0,Time.deltaTime);}
            float displayPPU=Mathf.Max(1,Screen.height/(view.orthographicSize*2));
            p.x=Mathf.Round(p.x*displayPPU)/displayPPU;p.y=Mathf.Round(p.y*displayPPU)/displayPPU;view.transform.position=p;
            backdrop.position=new Vector3(8+(p.x-8)*.82f,6+(p.y-4)*.72f,0);
        }
        void OnApplicationQuit(){Save();}
        void OnDestroy(){Time.timeScale=1;AncientHUD.Dispose();if(lineMaterial)Destroy(lineMaterial);}
        void Panel(Rect r,Color c){AncientHUD.Panel(r,c);}
        void Bar(Rect r,float value,Color c)
        {
            float pad=r.height<=6?1:2;
            Panel(r,new Color(.04f,.07f,.10f,.95f));
            Panel(new Rect(r.x+pad,r.y+pad,(r.width-pad*2)*Mathf.Clamp01(value),r.height-pad*2),c);
        }
        void NewRun()
        {
            foreach(string key in new[]{"cores","credits","checkpoint","boss","kills","score","bike","jump","capacity","armor"})PlayerPrefs.DeleteKey(SaveKey+key);
            PlayerPrefs.Save();Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        void Upgrade(Rect rect,string text,int price,int required,bool owned,System.Action unlock)
        {
            GUI.enabled=!owned&&credits>=price&&Cores>=required;
            if(GUI.Button(rect,owned?text+" / OWNED":text+" / "+price+" tech / "+required+" core"))
            {credits-=price;unlock();Save();Notice("Upgrade installed.");}
            GUI.enabled=true;
        }
        void OnGUI()
        {
            if(!hero)return;
            GUIStyle oldButton=GUI.skin.button;GUI.skin.button=AncientHUD.Button;
            if(small==null)
            {
                small=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};title=new GUIStyle(small){fontSize=22,fontStyle=FontStyle.Bold};
                centered=new GUIStyle(title){alignment=TextAnchor.MiddleCenter};
            }
            float scale=Mathf.Clamp(Screen.width/1280f,.65f,2);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float w=Screen.width/scale,h=Screen.height/scale;
            Panel(new Rect(16,16,302,106),new Color(.025f,.045f,.065f,.88f));
            GUI.Label(new Rect(28,21,290,24),"PLUGIN / ANCIENT CIVILIZATION",small);
            Bar(new Rect(28,49,272,18),healthEcho/hero.MaxHP,new Color(.48f,.23f,.12f));
            Panel(new Rect(30,51,268*Mathf.Clamp01(hero.hp/hero.MaxHP),14),Orange);Bar(new Rect(28,73,272,13),hero.particles/hero.MaxParticles,Cyan);
            GUI.Label(new Rect(28,91,275,25),"HP "+Mathf.CeilToInt(hero.hp)+"   PARTICLES "+Mathf.FloorToInt(hero.particles)+"   TECH "+credits,small);
            Panel(new Rect(w-300,16,284,74),new Color(.025f,.045f,.065f,.88f));
            GUI.Label(new Rect(w-286,24,260,55),"ANCIENT CORES  "+Cores+" / 3\n"+(bossDefeated?"Warden defeated / Reach the gate":Cores==3?"Reach the eastern sanctum":"Explore the upper rooftops"),small);
            GUI.Label(new Rect(28,128,300,30),(hero.bike?"BIKE "+Mathf.CeilToInt(hero.bikeHP)+" / 80":hero.running?"RUN":"WALK")+"   "+(hero.Wired?"WIRE CHAIN "+hero.WireChain:hero.WireChain>1?"CHAIN "+hero.WireChain:"")+"   K / UPGRADES",small);
            GUI.Label(new Rect(w-300,92,284,23),AncientVisuals.HighQuality?"F6 / VISUALS: ENHANCED":"F6 / VISUALS: LIGHT",small);
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
                        GUI.color=tellColor;GUI.Label(new Rect(tell.x/scale-30,(Screen.height-tell.y)/scale-21,90,22),enemy.AttackIsPhysical?"EVADE":"ABSORB",small);GUI.color=Color.white;
                    }
                }
                if(enemy==boss || enemy.HealthRatio>=1)continue;
                Vector3 screen=view.WorldToScreenPoint(enemy.AimPoint+Vector2.up*1.1f);
                if(screen.z>0 && screen.x>=0 && screen.x<Screen.width)
                    Bar(new Rect(screen.x/scale-22,(Screen.height-screen.y)/scale,44,5),enemy.HealthRatio,Orange);
            }
            if(boss)
            {
                GUI.Label(new Rect(w/2-220,h-104,440,25),"SANCTUM WARDEN  "+(boss.Enraged?"/ OVERDRIVE":""),centered);
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
                GUI.Label(new Rect(28,h-87,w-56,70),"A/D MOVE   SPACE JUMP   CTRL TAP RUN / HOLD BIKE   SHIFT DASH   F HOLD ABSORB\nJ / LMB COMBO   Q PARTICLE BURST   E / RMB AUTO WIRE   X BIKE EXPLOSION   K UPGRADES   H HIDE HELP",small);
            }
            if(skills)
            {
                Panel(new Rect(0,0,w,h),new Color(0,.015f,.03f,.75f));
                float x=w/2-260,y=Mathf.Max(20,h/2-240);
                Panel(new Rect(x,y,520,475),new Color(.035f,.065f,.09f,.98f));
                GUI.Label(new Rect(x+25,y+20,470,35),"PARTICLE ENGINEERING",title);
                GUI.Label(new Rect(x+25,y+57,470,52),"Tech fragments: "+credits+"   /   Ancient cores: "+Cores+"\nCollect cores and defeat sentinels to unlock upgrades.",small);
                Upgrade(new Rect(x+25,y+122,470,42),"Motorcycle fabrication",8,1,hero.bikeUnlocked,()=>hero.bikeUnlocked=true);
                Upgrade(new Rect(x+25,y+174,470,42),"Aerial step / double jump",10,1,hero.doubleJump,()=>hero.doubleJump=true);
                Upgrade(new Rect(x+25,y+226,470,42),"Particle reservoir / capacity 150",12,2,hero.reservoir,()=>hero.reservoir=true);
                Upgrade(new Rect(x+25,y+278,470,42),"Reinforced body / health 125",12,2,hero.armor,()=>{hero.armor=true;hero.hp=hero.MaxHP;});
                if(GUI.Button(new Rect(x+25,y+340,470,35),"Resume / K or Esc"))ToggleSkills();
                GUI.Label(new Rect(x+25,y+385,470,25),"Autosaves at checkpoints, core pickups and upgrades.",small);
                if(GUI.Button(new Rect(x+25,y+423,470,28),"Hold N + click to erase progress and start a new run")&&Input.GetKey(KeyCode.N))NewRun();
            }
            if(!skills&&!won)
            {
                Bar(new Rect(28,156,74,4),hero.DashReady,Cyan);
                GUI.Label(new Rect(28,162,90,22),"SHIFT / DASH",small);
                Bar(new Rect(120,156,74,4),hero.BurstReady,Orange);
                GUI.Label(new Rect(120,162,100,22),"Q / BURST",small);
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
        public bool running,bike,bikeUnlocked,doubleJump,reservoir,armor;
        public float MaxHP {get{return armor?125:100;}}
        public float MaxParticles {get{return reservoir?150:100;}}
        public bool Wired {get{return wired;}}
        public int WireChain {get{return wireChain;}}
        public float Facing {get{return direction;}}
        public float HorizontalSpeed {get{return body?body.linearVelocity.x:0;}}
        public float DashReady {get{return Mathf.Clamp01(1-(dashReady-Time.time)/.8f);}}
        public float BurstReady {get{return Mathf.Clamp01(1-(burstReady-Time.time)/.65f);}}
        Rigidbody2D body;SpriteRenderer art,bikeArt;LineRenderer wire,wireGlow,wireTarget,absorb;TrailRenderer trail;
        DistanceJoint2D wireJoint;SpriteRenderer hook;SpriteRenderer[] wirePulses;
        float direction=1,controlAt,attackAt,invulnerableUntil,wireReady,lastGrounded=-100;
        float dashUntil,dashReady,comboUntil,absorbAt=-10,burstReady,jumpBufferedUntil=-10;
        float wireAttachedAt,wireLength,wireMinLength,swingDirection,arcTravel,momentumUntil,lastWireRelease=-10;
        Vector2 previousRadial;int wireChain;
        bool controlConsumed,wired,airJumpUsed;float axis;int combo;
        Sprite[] frames,actions;
        float attackBufferedUntil=-10,strikeAt,actionUntil,hurtUntil,ghostAt,landingUntil,fallSpeed,dustAt;
        bool strikePending,wasGrounded,cutJump;float strikeFacing;
        void Start()
        {
            var g=AncientGame.Instance;
            bikeUnlocked=AncientGame.Saved("bike")!=0;doubleJump=AncientGame.Saved("jump")!=0;
            reservoir=AncientGame.Saved("capacity")!=0;armor=AncientGame.Saved("armor")!=0;hp=MaxHP;
            body=gameObject.AddComponent<Rigidbody2D>();body.freezeRotation=true;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;body.interpolation=RigidbodyInterpolation2D.Interpolate;
            wireJoint=gameObject.AddComponent<DistanceJoint2D>();wireJoint.autoConfigureDistance=false;
            wireJoint.autoConfigureConnectedAnchor=false;wireJoint.maxDistanceOnly=true;wireJoint.enabled=false;
            var collider=gameObject.AddComponent<CapsuleCollider2D>();collider.size=new Vector2(.7f,1.65f);
            var frictionless=new PhysicsMaterial2D("Free movement"){friction=0,bounciness=0};collider.sharedMaterial=frictionless;
            frames=new Sprite[8];actions=new Sprite[8];for(int i=0;i<frames.Length;i++){frames[i]=AncientArt.Hero(i);actions[i]=AncientArt.Action(i);}
            art=AncientArt.Display("Animated hero",frames[0],new Vector2(0,-.825f),10,transform);
            if(AncientVisuals.Instance)AncientVisuals.Instance.Rim(art);
            bikeArt=AncientArt.Display("Particle motorcycle",AncientArt.World(7),new Vector2(0,-.825f),9,transform);bikeArt.gameObject.SetActive(false);
            wire=g.Line("Particle cable core",new Color(.8f,1,1),.018f,17);
            wireGlow=g.Line("Particle cable sheath",new Color(.12f,.9f,1,.28f),.11f,16);
            wireTarget=g.Line("Wire targeting reticle",AncientGame.Cyan,.035f,18);wireTarget.loop=true;wireTarget.positionCount=8;
            wire.positionCount=wireGlow.positionCount=20;
            absorb=g.Line("Absorption stream",AncientGame.Cyan,.04f);
            wire.enabled=wireGlow.enabled=wireTarget.enabled=absorb.enabled=false;
            hook=AncientArt.Display("Wire latch",AncientArt.World(2),Vector2.zero,18);hook.transform.localScale=Vector3.one*.18f;hook.enabled=false;
            wirePulses=new SpriteRenderer[4];
            for(int i=0;i<wirePulses.Length;i++)
            {
                var pulse=g.Box("Cable energy pulse",Vector2.zero,new Vector2(.065f,.065f),AncientGame.Cyan,19);
                wirePulses[i]=pulse.GetComponent<SpriteRenderer>();wirePulses[i].enabled=false;
            }
            trail=gameObject.AddComponent<TrailRenderer>();trail.material=wire.sharedMaterial;
            trail.startColor=new Color(.2f,.9f,1,.55f);trail.endColor=new Color(.2f,.9f,1,0);
            trail.startWidth=.22f;trail.endWidth=0;trail.time=.12f;trail.sortingOrder=8;trail.emitting=false;
        }
        public void SaveUpgrades()
        {
            AncientGame.SetSaved("bike",bikeUnlocked?1:0);AncientGame.SetSaved("jump",doubleJump?1:0);
            AncientGame.SetSaved("capacity",reservoir?1:0);AncientGame.SetSaved("armor",armor?1:0);
        }
        bool Grounded()
        {return Physics2D.BoxCast((Vector2)transform.position+Vector2.down*.78f,new Vector2(.58f,.12f),0,Vector2.down,.12f,1<<8).collider!=null;}
        void Update()
        {
            var g=AncientGame.Instance;if(!body)return;
            if(g.won){StopWire(false);body.linearVelocity=Vector2.zero;body.gravityScale=0;absorb.enabled=false;trail.emitting=false;return;}
            if(g.Paused){absorb.enabled=false;return;}
            if(transform.position.y<-9){Respawn();return;}
            bool onGround=Grounded();
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
            if(Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift))
            {
                if(Time.time>=dashReady&&!bike)
                {StopWire(false);dashUntil=Time.time+.16f;dashReady=Time.time+.8f;invulnerableUntil=Mathf.Max(invulnerableUntil,dashUntil);g.Burst(transform.position,AncientGame.Cyan,8);}
            }
            bool control=Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl);
            if(Input.GetKeyDown(KeyCode.LeftControl)||Input.GetKeyDown(KeyCode.RightControl)){controlAt=Time.time;controlConsumed=false;}
            if(control&&!controlConsumed&&Time.time-controlAt>=.45f)
            {
                controlConsumed=true;
                if(bike)g.Notice("Press X to detonate and launch.");
                else if(!bikeUnlocked)g.Notice("Recover a core, then unlock motorcycle fabrication in K.");
                else if(particles<30)g.Notice("Construction needs 30 particles. Absorb enemy fire or find a cache.");
                else{particles-=30;bike=true;bikeHP=80;g.Burst(transform.position,AncientGame.Cyan,24);g.Shake(.08f);}
            }
            if((Input.GetKeyUp(KeyCode.LeftControl)||Input.GetKeyUp(KeyCode.RightControl))&&!controlConsumed)running=!running;
            if(Input.GetKeyDown(KeyCode.X)&&bike)ExplodeBike();
            if(Input.GetKeyDown(KeyCode.E)||Input.GetMouseButtonDown(1))FireWire();
            if(Input.GetKeyDown(KeyCode.J)||Input.GetMouseButtonDown(0))attackBufferedUntil=Time.time+.16f;
            if(Time.time>=attackAt&&Time.time<=attackBufferedUntil&&Time.time>=hurtUntil)BeginAttack();
            if(strikePending&&Time.time>=strikeAt)
            {
                strikePending=false;g.Sound(0);g.Slash(transform.position,strikeFacing,combo==3);
                foreach(var e in g.enemies.ToArray())if(e&&Vector2.Distance(e.AimPoint,transform.position)<(combo==3?3.2f:2.6f)&&(e.transform.position.x-transform.position.x)*strikeFacing>-.3f&&!Physics2D.Linecast(transform.position,e.AimPoint,1<<8))
                    e.Hit(combo==3?38:combo==2?23:18,combo==3);
            }
            if(Input.GetKeyDown(KeyCode.Q)&&Time.time>=burstReady)
            {
                if(particles<25)g.Notice("Particle burst needs 25 particles.");
                else
                {
                    particles-=25;burstReady=Time.time+.65f;g.Sound(4);g.Shake(.12f);g.Slash(transform.position,direction,true);
                    foreach(var e in g.enemies.ToArray())if(e&&Vector2.Distance(e.AimPoint,transform.position)<6&&(e.transform.position.x-transform.position.x)*direction>-.5f&&!Physics2D.Linecast(transform.position,e.AimPoint,1<<8))e.Hit(62,true);
                    g.Burst((Vector2)transform.position+Vector2.right*direction*2,AncientGame.Cyan,26);
                }
            }
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
            art.flipX=(Time.time<actionUntil&&!wired?strikeFacing:direction)<0;
            int frame=bike?7:wired?5:Input.GetKey(KeyCode.F)?6:Mathf.Abs(axis)>.1f?1+(int)(Time.time*(running?13:9))%4:0;
            art.sprite=frames[frame];
            if(!bike&&!wired)
            {
                if(Time.time<hurtUntil)art.sprite=actions[7];
                else if(Time.time<actionUntil)art.sprite=actions[(combo-1)*2+(Time.time<strikeAt?0:1)];
                else if(!onGround)art.sprite=actions[6];
            }
            float squash=Time.time<landingUntil?.08f:0;
            art.transform.localScale=new Vector3(1+squash,1-squash,1);
            if((Time.time<dashUntil||wired)&&Time.time>=ghostAt){g.Afterimage(art);ghostAt=Time.time+.065f;}
            if(onGround&&Mathf.Abs(body.linearVelocity.x)>7&&Time.time>=dustAt){g.Burst((Vector2)transform.position+Vector2.down*.8f,new Color(.35f,.48f,.55f),2);dustAt=Time.time+.18f;}
            art.transform.localPosition=new Vector3(0,bike?-.25f:-.825f,0);
            art.color=Time.time<invulnerableUntil&&(int)(Time.time*14)%2==0?new Color(1,1,1,.45f):Color.white;
            bikeArt.gameObject.SetActive(bike);bikeArt.flipX=direction<0;
            trail.emitting=Time.time<dashUntil||wired||bike;
        }
        void BeginAttack()
        {
            if(Time.time>comboUntil)combo=0;combo=combo%3+1;
            float duration=combo==3?.46f:combo==2?.3f:.28f;
            strikeAt=Time.time+(combo==3?.18f:.09f);strikeFacing=direction;strikePending=true;
            attackAt=actionUntil=Time.time+duration;comboUntil=attackAt+.32f;attackBufferedUntil=-10;
        }
        Vector2 anchor;
        void FixedUpdate()
        {
            var g=AncientGame.Instance;if(g.won||g.Paused)return;
            Vector2 velocity=body.linearVelocity;
            if(Time.time<dashUntil)velocity=new Vector2(direction*23,1);
            else if(wired)
            {
                Vector2 delta=anchor-body.position;Vector2 radial=delta.normalized;
                float age=Time.time-wireAttachedAt;
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
            else
            {
                float speed=bike?15:running?9:5;
                bool grounded=Grounded();
                bool carry=!grounded&&Time.time<momentumUntil&&Mathf.Abs(velocity.x)>speed&&(axis==0||Mathf.Sign(axis)==Mathf.Sign(velocity.x));
                if(!carry&&(axis!=0||grounded))velocity.x=Mathf.MoveTowards(velocity.x,axis*speed,(grounded?70:22)*Time.fixedDeltaTime);
            }
            if(Time.time<jumpBufferedUntil)
            {
                if(Time.time-lastGrounded<.12f)
                {StopWire(false);velocity.y=14;lastGrounded=-100;jumpBufferedUntil=-10;}
                else if(doubleJump&&!airJumpUsed)
                {StopWire(false);velocity.y=12;airJumpUsed=true;jumpBufferedUntil=-10;g.Burst(transform.position,AncientGame.Cyan,10);}
            }
            if(cutJump&&velocity.y>4&&!wired&&!bike)velocity.y*=.55f;
            cutJump=false;body.gravityScale=!wired&&velocity.y<0?1.2f:1;
            velocity.y=Mathf.Max(velocity.y,-25);body.linearVelocity=velocity;
            if(g.ArenaActive)
            {
                Vector2 p=body.position;p.x=Mathf.Clamp(p.x,98,117);body.position=p;
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
            selected=Vector2.zero;var camera=Camera.main;if(!camera)return false;
            Vector2 hand=(Vector2)transform.position+Vector2.up*.35f;
            Vector2 mouse=camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 aim=(mouse-hand).normalized;
            float best=float.MaxValue;bool found=false;
            foreach(var p in AncientGame.Instance.anchors)
            {
                float distance=Vector2.Distance(hand,p);
                if(distance<2.2f||distance>22||p.y<transform.position.y+1||!VisibleAnchor(p))continue;
                // A cursor-centred score keeps selection predictable, and prevents accidental side swaps.
                float score=Vector2.Distance(mouse,p);
                if(wired&&Vector2.Distance(p,anchor)<.25f)score+=8;
                if(score<best){best=score;selected=p;found=true;}
            }
            // Elevated solid platform edges and undersides can also be latched, not only preset nodes.
            if(aim.sqrMagnitude>.1f)
            {
                RaycastHit2D hit=Physics2D.Raycast(hand,aim,22,1<<8);
                if(hit && hit.point.y>transform.position.y+2.3f && Vector2.Distance(hit.point,hand)>2.5f)
                {
                    float score=Vector2.Distance(mouse,hit.point);
                    if(score<best){selected=hit.point;found=true;}
                }
            }
            return found;
        }
        void FireWire()
        {
            if(Time.time<wireReady)return;
            Vector2 selected;var g=AncientGame.Instance;
            if(!PickAnchor(out selected)){g.Notice("Aim at a cyan node or elevated rooftop edge within 22m.");return;}
            bool different=Vector2.Distance(selected,anchor)>.5f;
            if(wired&&!different)return;
            bool chain=(wired||Time.time-lastWireRelease<1.2f)&&different;
            if(wired)StopWire(false);
            wireChain=chain?Mathf.Min(wireChain+1,5):1;
            anchor=selected;wired=true;wireAttachedAt=Time.time;wireReady=Time.time+.18f;dashUntil=0;
            Vector2 delta=anchor-body.position;Vector2 radial=delta.normalized;
            swingDirection=Mathf.Abs(body.linearVelocity.x)>2?Mathf.Sign(body.linearVelocity.x):direction;
            previousRadial=-radial;arcTravel=0;
            wireLength=delta.magnitude;wireMinLength=Mathf.Min(wireLength,Mathf.Clamp(wireLength*.68f,3.2f,8));
            wireJoint.connectedAnchor=anchor;wireJoint.distance=wireLength;wireJoint.enabled=true;
            Vector2 velocity=body.linearVelocity+radial*11+new Vector2(radial.y,-radial.x)*swingDirection*5;
            velocity.x=swingDirection*Mathf.Max(12+Mathf.Min(4,wireChain),velocity.x*swingDirection);
            velocity.y=Mathf.Max(8,velocity.y);body.linearVelocity=Vector2.ClampMagnitude(velocity,30);
            airJumpUsed=false;g.Sound(3);g.Burst(anchor,AncientGame.Cyan,14);g.Burst(transform.position,AncientGame.Cyan,8);g.Shake(.045f);
        }
        void StopWire(bool launch)
        {
            bool wasWired=wired;wired=false;if(wireJoint)wireJoint.enabled=false;
            if(!wasWired)return;
            lastWireRelease=Time.time;momentumUntil=Time.time+.7f;
            if(launch&&body)
            {
                Vector2 velocity=body.linearVelocity;
                velocity.x=swingDirection*Mathf.Max(13,velocity.x*swingDirection);
                velocity.y=Mathf.Max(5,velocity.y);
                body.linearVelocity=Vector2.ClampMagnitude(velocity,30);
                AncientGame.Instance.Burst(transform.position,AncientGame.Cyan,9);
            }
        }
        void LateUpdate()
        {
            if(!body||!wire)return;var g=AncientGame.Instance;
            bool visible=wired&&!g.won;
            wire.enabled=wireGlow.enabled=hook.enabled=visible;
            foreach(var pulse in wirePulses)pulse.enabled=visible;
            if(visible)
            {
                Vector2 hand=(Vector2)transform.position+new Vector2(direction*.4f,.45f);
                float age=Time.time-wireAttachedAt;float shot=Mathf.Clamp01(age/.055f);
                Vector2 end=Vector2.Lerp(hand,anchor,shot);
                Vector2 normal=Vector2.Perpendicular(end-hand).normalized;
                for(int i=0;i<wire.positionCount;i++)
                {
                    float t=i/(float)(wire.positionCount-1);
                    float wave=Mathf.Sin(t*Mathf.PI*5-age*28)*Mathf.Sin(t*Mathf.PI)*.13f*Mathf.Exp(-age*9);
                    Vector2 point=Vector2.Lerp(hand,end,t)+normal*wave;
                    wire.SetPosition(i,point);wireGlow.SetPosition(i,point);
                }
                hook.transform.position=end;hook.transform.Rotate(0,0,220*Time.deltaTime);
                for(int i=0;i<wirePulses.Length;i++)
                    wirePulses[i].transform.position=Vector2.Lerp(hand,end,Mathf.Repeat(age*2.6f+i/(float)wirePulses.Length,1));
            }
            Vector2 target=Vector2.zero;bool hasTarget=!g.Paused&&!g.won&&PickAnchor(out target);
            wireTarget.enabled=hasTarget;
            if(hasTarget)
            {
                float radius=.43f+Mathf.Sin(Time.time*7)*.045f;
                for(int i=0;i<wireTarget.positionCount;i++)
                {
                    float a=Mathf.PI*2*i/wireTarget.positionCount;
                    wireTarget.SetPosition(i,target+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);
                }
            }
        }
        public void AbsorbShot(AncientProjectile shot)
        {
            var g=AncientGame.Instance;bool perfect=Time.time-absorbAt<.18f;
            particles=Mathf.Min(MaxParticles,particles+(shot.scrap?18:12)*(perfect?1.5f:1));
            g.Sound(2);g.score+=perfect?30:10;g.Burst(shot.transform.position,AncientGame.Cyan,10);
            if(perfect)g.Notice("PERFECT ABSORB / bonus particles",.75f);shot.Remove();
        }
        public void Hurt(float damage)
        {
            var g=AncientGame.Instance;if(Time.time<invulnerableUntil||g.won)return;
            invulnerableUntil=Time.time+.65f;g.Shake(.14f);
            if(bike){bikeHP-=damage;if(bikeHP<=0)ExplodeBike();return;}
            strikePending=false;attackBufferedUntil=-10;actionUntil=0;hurtUntil=Time.time+.18f;
            hp-=damage;g.DamageEmphasis();g.Sound(1);g.Impact(transform.position,AncientGame.Orange,false);g.Burst(transform.position,AncientGame.Orange,9);
            if(hp<=0)Respawn();
        }
        public void ExplodeBike()
        {
            if(!bike)return;bike=false;StopWire(false);momentumUntil=Time.time+.8f;
            var g=AncientGame.Instance;g.Sound(4);g.Burst(transform.position,AncientGame.Orange,45);g.Shake(.3f);
            foreach(var e in g.enemies.ToArray())if(e&&Vector2.Distance(e.AimPoint,transform.position)<5)e.Hit(85,true);
            body.linearVelocity=new Vector2(direction*17,19);invulnerableUntil=Time.time+.7f;
            g.Notice("EXPLOSION LAUNCH / connect the next anchor.",2);
        }
        void Respawn()
        {
            StopWire(false);wireChain=0;momentumUntil=0;strikePending=false;actionUntil=0;attackBufferedUntil=-10;fallSpeed=0;hp=MaxHP;particles=Mathf.Max(40,particles);bike=false;dashUntil=0;body.linearVelocity=Vector2.zero;
            invulnerableUntil=Time.time+1.5f;AncientGame.Instance.HeroDied();
        }
        void OnDestroy()
        {
            if(wire)Destroy(wire.gameObject);if(wireGlow)Destroy(wireGlow.gameObject);
            if(wireTarget)Destroy(wireTarget.gameObject);if(hook)Destroy(hook.gameObject);
            if(wirePulses!=null)foreach(var pulse in wirePulses)if(pulse)Destroy(pulse.gameObject);
            if(absorb)Destroy(absorb.gameObject);
        }
    }

    public sealed class AncientEnemy : MonoBehaviour
    {
        public int kind;
        float hp=60,maxHP=60,nextAttack,resolveAt=-1,flashUntil,walkPhase,faceLockUntil,guardBrokenUntil,staggerUntil;
        int pattern,attackIndex,volleyRemaining;bool dead;
        float volleyAt,windupStartedAt,attackPoseUntil;bool enragedAnnounced;
        public bool IsTelegraphing {get{return resolveAt>=0;}}
        public bool AttackIsPhysical {get{return pattern==0;}}
        public float WindupProgress {get{return resolveAt<0?0:Mathf.Clamp01((Time.time-windupStartedAt)/Mathf.Max(.01f,resolveAt-windupStartedAt));}}
        SpriteRenderer art;LineRenderer warning;
        Vector2 home,lockedAim;float facing=1;
        public Vector2 AimPoint {get{return (Vector2)transform.position+Vector2.up*(kind==3?1.65f:kind==4?.65f:.95f);}}
        public float HealthRatio {get{return maxHP>0?hp/maxHP:1;}}
        public bool Enraged {get{return kind==3&&hp<maxHP*.5f;}}
        public string Role {get{return kind==0?"LANCER":kind==1?"GUNNER":kind==2?"BOMBER":kind==3?"SANCTUM WARDEN":kind==4?"DRONE":"SHIELD WARDEN";}}
        public void Initialize(int type)
        {kind=type;maxHP=hp=kind==3?560:kind==5?115:kind==4?42:kind==0?70:55;}
        void Start()
        {
            var g=AncientGame.Instance;home=transform.position;
            art=AncientArt.Display(Role,AncientArt.Enemy(kind),Vector2.zero,8,transform);
            if(AncientVisuals.Instance)
            {
                AncientVisuals.Instance.Rim(art);
                AncientVisuals.Instance.Glow(new Vector2(0,kind==4?.6f:1.1f),kind==3||kind==5?AncientGame.Cyan:AncientGame.Orange,kind==3?2.5f:1.1f,9,transform);
            }
            warning=g.Line("Attack telegraph",AncientGame.Orange,.045f,6);warning.enabled=false;
            nextAttack=Time.time+(kind==3?1.3f:1+kind*.2f);
        }
        bool Step(float distance)
        {
            float x=Mathf.Clamp(transform.position.x+distance,kind==3?100:home.x-2,kind==3?114:home.x+2);
            if(kind!=3&&!Physics2D.Raycast(new Vector2(x,home.y+.2f),Vector2.down,.5f,1<<8))return false;
            transform.position=new Vector2(x,home.y);return true;
        }
        void Update()
        {
            var g=AncientGame.Instance;if(dead||!g.hero||g.won||g.Paused)return;
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
                    nextAttack=Time.time+(kind==3?(Enraged?.65f:1.15f):kind==5?1.45f:kind==0?1.05f:kind==2?1.8f:kind==4?1.9f:1.6f);
                }
                return;
            }
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
            var shot=new GameObject(scrap?"Thrown scrap":"Energy bolt").AddComponent<AncientProjectile>();
            shot.scrap=scrap;shot.gravity=gravity;shot.damage=kind==3?20:scrap?25:kind==4?12:15;
            Vector2 origin=AimPoint+aim*.65f;shot.transform.position=origin;
            shot.velocity=gravity>0?(target-origin)/.95f+Vector2.up*(gravity*.95f*.5f):aim*speed;
            g.shots.Add(shot);
        }
        void ResolveAttack()
        {
            attackPoseUntil=Time.time+.18f;
            var g=AncientGame.Instance;
            if(pattern==0)
            {
                if(kind==0)Step(facing*.9f);
                Vector2 delta=(Vector2)g.hero.transform.position-AimPoint;
                if(Mathf.Abs(delta.x)<(kind==3?4.8f:2.8f)&&Mathf.Abs(delta.y)<1.7f&&delta.x*facing>-.6f)g.hero.Hurt(kind==3?32:kind==5?26:22);
                g.Slash(AimPoint,facing,kind==3);g.Burst(AimPoint+Vector2.right*facing*2,AncientGame.Orange,kind==3?25:8);
                if(kind==3)g.Shake(.2f);
            }
            else if(kind==2)FireProjectile(lockedAim,9,true,14);
            else if(kind==1)
            {FireProjectile(lockedAim,12,false,0);volleyRemaining=2;volleyAt=Time.time+.14f;}
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
        public void Hit(float damage,bool breakGuard=false)
        {
            if(dead)return;var g=AncientGame.Instance;
            if(kind==5&&Time.time>=guardBrokenUntil&&((Vector2)g.hero.transform.position-AimPoint).x*facing>0)
            {
                if(!breakGuard){damage*=.15f;g.Burst(AimPoint+Vector2.right*facing*.6f,AncientGame.Cyan,8);g.Notice("SHIELD / Flank, finish a combo, or use Q to break guard.",1.2f);}
                else{guardBrokenUntil=Time.time+2;staggerUntil=Time.time+.8f;resolveAt=-1;g.Notice("GUARD BROKEN",.8f);}
            }
            if(breakGuard&&kind!=3){resolveAt=-1;staggerUntil=Mathf.Max(staggerUntil,Time.time+.4f);nextAttack=Mathf.Max(nextAttack,Time.time+.65f);volleyRemaining=0;}
            hp-=damage;flashUntil=Time.time+.1f;
            g.Sound(6);g.HitEmphasis(breakGuard);g.Impact(AimPoint,breakGuard?AncientGame.Orange:AncientGame.Cyan,breakGuard);
            g.Burst(AimPoint,AncientGame.Cyan,7);g.Shake(.045f);g.score+=Mathf.RoundToInt(damage);
            if(hp>0)return;
            dead=true;g.kills++;g.credits+=kind==3?20:kind==5?6:kind==4?5:4;g.score+=kind==3?1000:100;
            Vector2 drop=AimPoint;
            if(kind==4)
            {
                var ground=Physics2D.Raycast(drop,Vector2.down,20,1<<8);
                if(ground)drop=ground.point+Vector2.up*.7f;
            }
            g.SpawnPickup(drop);g.Burst(AimPoint,AncientGame.Orange,kind==3?55:18);g.enemies.Remove(this);
            if(kind==3){g.bossDefeated=true;g.boss=null;g.Save();g.Notice("WARDEN DEFEATED\nThe eastern gate is open.",7);}
            Destroy(gameObject);
        }
        void OnDestroy(){if(warning)Destroy(warning.gameObject);}
    }
    public sealed class AncientProjectile : MonoBehaviour
    {
        public bool scrap;public Vector2 velocity;public float damage=15,gravity;
        float age;bool removed;LineRenderer glow;
        void Start()
        {
            var g=AncientGame.Instance;
            if(scrap)
            {
                AncientArt.Display("Thrown metal chunk",AncientArt.Scrap(),Vector2.zero,12,transform);
            }
            else
            {
                glow=g.Line("Energy streak",AncientGame.Cyan,.075f,12);
            }
        }
        void Update()
        {
            if(removed)return;var g=AncientGame.Instance;if(g.won){Remove();return;}if(g.Paused)return;
            age+=Time.deltaTime;velocity+=Vector2.down*gravity*Time.deltaTime;Vector2 previous=transform.position;Vector2 next=previous+velocity*Time.deltaTime;
            if(Physics2D.Linecast(previous,next,1<<8)){g.Burst(previous,AncientGame.Cyan,4);Remove();return;}
            if(g.hero&&age>.05f)
            {
                Vector2 h=g.hero.transform.position;Vector2 segment=next-previous;
                float t=segment.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(h-previous,segment)/segment.sqrMagnitude):0;
                if(Vector2.Distance(h,previous+segment*t)<.75f)
                {
                    if(Input.GetKey(KeyCode.F))g.hero.AbsorbShot(this);
                    else{g.hero.Hurt(damage);Remove();}return;
                }
            }
            transform.position=next;
            if(glow){glow.SetPosition(0,next);glow.SetPosition(1,next-velocity.normalized*.7f);}
            if(scrap)transform.Rotate(0,0,300*Time.deltaTime);if(age>5)Remove();
        }
        public void Remove()
        {if(removed)return;removed=true;AncientGame.Instance.shots.Remove(this);Destroy(gameObject);}
        void OnDestroy(){if(glow)Destroy(glow.gameObject);}
    }
    public sealed class AncientPickup : MonoBehaviour
    {
        public int core=-1;Vector2 origin;float phase;GameObject visual;
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
                g.Notice("ANCIENT CORE "+g.Cores+" / 3\nNew engineering tiers available. K to upgrade.",5);
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
        LineRenderer line;Vector2 centre;Color tint;float radius,age;
        public void Begin(Vector2 p,Color color,float size)
        {line=GetComponent<LineRenderer>();centre=p;tint=color;radius=size;Draw(0);}
        void Draw(float t)
        {
            line.startColor=line.endColor=new Color(tint.r,tint.g,tint.b,1-t);
            for(int i=0;i<line.positionCount;i++){float a=i*Mathf.PI*2/line.positionCount;line.SetPosition(i,centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Mathf.Lerp(.1f,radius,t));}
        }
        void Update()
        {age+=Time.deltaTime;float t=Mathf.Clamp01(age/.16f);Draw(t);if(t>=1)Destroy(gameObject);}
    }
}
