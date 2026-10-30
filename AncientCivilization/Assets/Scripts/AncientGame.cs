using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.AncientCivilization
{
    // All artwork is generated from small pixel grids; no external assets are needed.
    public sealed class AncientGame : MonoBehaviour
    {
        public static AncientGame Instance;
        public AncientHero hero;
        public readonly List<Vector2> anchors = new List<Vector2>();
        public readonly List<AncientEnemy> enemies = new List<AncientEnemy>();
        public readonly List<AncientProjectile> shots = new List<AncientProjectile>();
        public readonly List<AncientPickup> pickups = new List<AncientPickup>();
        public bool skills, won;
        public int relics;
        public string message = "Find three ancient cores. Reach the east gate.";
        float messageUntil;
        Camera view;
        Sprite square;
        GUIStyle label, title;
        public static readonly Color Cyan = new Color(.18f, .92f, 1f);
        public static readonly Color Orange = new Color(1f, .43f, .14f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<AncientGame>() == null)
                new GameObject("Ancient Civilization").AddComponent<AncientGame>();
        }
        void Awake()
        {
            Instance = this;
            Physics2D.gravity = new Vector2(0, -32);
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            view = new GameObject("Pixel Camera").AddComponent<Camera>();
            view.tag = "MainCamera"; view.orthographic = true; view.orthographicSize = 7.5f;
            view.backgroundColor = new Color(.035f, .055f, .1f);
            view.transform.position = new Vector3(8, 4, -10);
            BuildWorld();
            hero = new GameObject("Mechanical Arm Hero").AddComponent<AncientHero>();
            hero.transform.position = new Vector3(2, 2, 0);
            Notice("A lost civilization lies below the neon city. Find its three cores.", 8);
        }
        void BuildWorld()
        {
            var random = new System.Random(17287);
            for (int i = -5; i < 62; i++)
            {
                float x = i * 2.4f, h = 5 + (float)random.NextDouble() * 11;
                Box("Distant city", new Vector2(x, h / 2 - 3), new Vector2(2.1f, h), new Color(.065f, .1f, .15f), -20);
                for (int w = 0; w < 4; w++)
                    Box("Window", new Vector2(x + (w % 2) * .6f - .3f, h - 4 - w * 1.4f), new Vector2(.15f, .35f), new Color(.6f, .32f, .13f), -19);
            }
            Platform(-5, 20, 0); Platform(22, 38, 0); Platform(43, 58, 1);
            Platform(63, 80, 0); Platform(85, 105, 0);
            Platform(10, 16, 3); Platform(28, 33, 4); Platform(48, 55, 5);
            Platform(69, 75, 4); Platform(87, 93, 5);
            foreach (Vector2 p in new [] {new Vector2(18,10),new Vector2(30,12),new Vector2(41,11),new Vector2(58,12),new Vector2(69,11),new Vector2(81,12),new Vector2(94,12)})
            {
                anchors.Add(p);
                Box("Anchor support", p + Vector2.up * 2, new Vector2(.18f, 4), new Color(.15f,.2f,.25f), -1);
                Box("Wire anchor", p, new Vector2(.7f,.7f), Cyan, 2);
                Box("Anchor core", p, new Vector2(.3f,.3f), new Color(.05f,.15f,.2f), 3);
            }
            SpawnEnemy(14, 4.2f, 0); SpawnEnemy(27, 1.3f, 1); SpawnEnemy(35, 1.3f, 2);
            SpawnEnemy(51, 6.2f, 1); SpawnEnemy(66, 1.3f, 0); SpawnEnemy(74, 5.2f, 2);
            SpawnEnemy(90, 6.2f, 1); SpawnEnemy(97, 1.3f, 0);
            SpawnPickup(new Vector2(31,5), true); SpawnPickup(new Vector2(72,5), true); SpawnPickup(new Vector2(90,6), true);
            for (int i = 0; i < 8; i++) SpawnPickup(new Vector2(7 + i * 12, 2.2f), false);
            Box("Ancient gate", new Vector2(101,3), new Vector2(2,6), new Color(.13f,.3f,.34f), -2);
            Box("Gate light", new Vector2(101,3), new Vector2(.3f,5), Cyan, -1);
        }
        void Platform(float left, float right, float top)
        {
            var floor = Box("Solid platform", new Vector2((left+right)/2, top-.5f), new Vector2(right-left,1), new Color(.12f,.16f,.20f), 0);
            floor.layer = 8; floor.AddComponent<BoxCollider2D>();
            Box("Platform rim", new Vector2((left+right)/2,top-.06f),new Vector2(right-left,.12f),new Color(.34f,.4f,.43f),1);
            for (float x=left+.5f;x<right;x+=2)
                Box("Rivets",new Vector2(x,top-.32f),new Vector2(.15f,.15f),Orange,1);
        }
        public GameObject Box(string name, Vector2 pos, Vector2 size, Color color, int order)
        {
            var o = new GameObject(name); o.transform.position=pos; o.transform.localScale=new Vector3(size.x,size.y,1);
            var s=o.AddComponent<SpriteRenderer>(); s.sprite=square; s.color=color; s.sortingOrder=order; return o;
        }
        public Sprite PixelSprite(string[] rows)
        {
            var t=new Texture2D(rows[0].Length,rows.Length,TextureFormat.RGBA32,false);
            t.filterMode=FilterMode.Point; t.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<rows.Length;y++) for(int x=0;x<rows[y].Length;x++)
            {
                Color c=Color.clear;
                switch(rows[y][x])
                {
                    case 'd':c=new Color(.08f,.10f,.15f);break;
                    case 's':c=new Color(.87f,.67f,.46f);break;
                    case 'c':c=Cyan;break; case 'o':c=Orange;break;
                    case 'g':c=new Color(.32f,.39f,.45f);break;
                    case 'w':c=new Color(.77f,.85f,.87f);break;
                }
                t.SetPixel(x,rows.Length-y-1,c);
            }
            t.Apply(); return Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.one*.5f,10);
        }
        void SpawnEnemy(float x,float y,int kind)
        {
            var e=new GameObject(kind==0?"Melee sentinel":kind==1?"Energy gunner":"Scrap thrower").AddComponent<AncientEnemy>();
            e.transform.position=new Vector2(x,y);e.kind=kind;enemies.Add(e);
        }
        public void SpawnPickup(Vector2 p,bool core)
        {
            var item=new GameObject(core?"Ancient core":"Particle cache").AddComponent<AncientPickup>();
            item.transform.position=p; item.core=core;pickups.Add(item);
        }
        public void Notice(string text,float seconds=3) {message=text;messageUntil=Time.time+seconds;}
        public void Burst(Vector2 pos,Color color,int count=16)
        {
            for(int i=0;i<count;i++)
            {
                var b=Box("Pixel fragment",pos,new Vector2(.12f,.12f),color,20);
                var f=b.AddComponent<AncientFragment>();f.velocity=Random.insideUnitCircle*8;
            }
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.K)) skills=!skills;
            if(Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            if(!won && hero && hero.transform.position.x>99)
            {
                if(relics>=3){won=true;Notice("THE SKY REMEMBERS. Prototype complete.",10000);}
                else Notice("The gate needs all three ancient cores.");
            }
        }
        void LateUpdate()
        {
            if(!hero)return;
            Vector3 target=new Vector3(Mathf.Clamp(hero.transform.position.x+3,5,99),Mathf.Max(4,hero.transform.position.y+1),-10);
            view.transform.position=Vector3.Lerp(view.transform.position,target,1-Mathf.Exp(-8*Time.deltaTime));
            var p=view.transform.position;p.x=Mathf.Round(p.x*32)/32;p.y=Mathf.Round(p.y*32)/32;view.transform.position=p;
        }
        void OnGUI()
        {
            if(!hero)return;
            if(label==null){label=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true};title=new GUIStyle(label){fontSize=24,fontStyle=FontStyle.Bold};}
            float scale=Mathf.Clamp(Screen.width/1200f,.65f,2f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float width=Screen.width/scale;
            GUI.Box(new Rect(12,12,360,125),"");
            GUI.Label(new Rect(24,18,340,30),"PLUGIN - ANCIENT CIVILIZATION",label);
            GUI.color=Orange;GUI.Box(new Rect(24,52,Mathf.Max(1,hero.hp)*3,16),"");
            GUI.color=Cyan;GUI.Box(new Rect(24,77,Mathf.Max(1,hero.particles)*3,14),"");GUI.color=Color.white;
            GUI.Label(new Rect(24,99,340,30),"HP "+Mathf.CeilToInt(hero.hp)+" | Particles "+Mathf.FloorToInt(hero.particles)+" | Cores "+relics+"/3",label);
            GUI.Box(new Rect(12,145,360,hero.bike?90:62),"");
            GUI.Label(new Rect(24,151,340,60),"Ctrl: "+(hero.bike?"BIKE":hero.running?"RUN":"WALK")+" | Wire: "+(hero.Wired?"CONNECTED":"READY")+"\n"+(hero.bike?"Bike HP: "+Mathf.CeilToInt(hero.bikeHP)+" / 80":"K: Skill tree / motorcycle unlock"),label);
            GUI.Label(new Rect(20,Screen.height/scale-62,width-40,60),"A/D: Move   Space: Jump   Ctrl tap: Run toggle   F: Absorb (hold)   J / LMB: Strike\nE / RMB: Wire toward cursor   Ctrl hold: Create bike   X: Explode bike   K: Skills   R: Restart",label);
            if(Time.time<messageUntil || won) GUI.Label(new Rect(390,20,Mathf.Max(200,width-410),75),message,title);
            if(skills)
            {
                var rect=new Rect(width/2-210,145,420,240);GUI.Box(rect,"");
                GUI.Label(new Rect(rect.x+20,rect.y+15,380,35),"PARTICLE ENGINEERING",title);
                GUI.Label(new Rect(rect.x+20,rect.y+55,380,85),"Motorcycle fabrication\nSpend 20 particles to unlock. Hold Ctrl to construct (30 particles). X detonates and launches you.",label);
                GUI.enabled=!hero.bikeUnlocked && hero.particles>=20;
                if(GUI.Button(new Rect(rect.x+20,rect.y+155,380,35),hero.bikeUnlocked?"UNLOCKED":"Unlock motorcycle - 20 particles"))
                {hero.particles-=20;hero.bikeUnlocked=true;Notice("Motorcycle unlocked. Hold Ctrl to create it.");}
                GUI.enabled=true;
                if(GUI.Button(new Rect(rect.x+20,rect.y+200,380,25),"Close (K)"))skills=false;
            }
        }
    }

    public sealed class AncientHero : MonoBehaviour
    {
        public float hp=100,particles=70,bikeHP;
        public bool running,bike,bikeUnlocked;
        public bool Wired {get{return wired;}}
        Rigidbody2D body; SpriteRenderer art; GameObject bikeArt; LineRenderer wire,absorb;
        float direction=1,controlAt,attackAt,invulnerableUntil,wireUntil,wireReady,absorbPhase;
        bool controlConsumed,wired,jumpQueued;float axis;
        Vector2 anchor,checkpoint=new Vector2(2,2);
        Sprite[] frames;float lastGrounded=-100;
        void Start()
        {
            var g=AncientGame.Instance;
            body=gameObject.AddComponent<Rigidbody2D>();body.freezeRotation=true;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            var collider=gameObject.AddComponent<CapsuleCollider2D>();collider.size=new Vector2(.7f,1.65f);
            art=gameObject.AddComponent<SpriteRenderer>();art.sortingOrder=10;
            frames=new [] {g.PixelSprite(new[]{"....dddd....","...dddddd...","...dssss....","....sss.....","oooodggcc...","....dggcccc.","....dggc....","....dddd....","....d..d....","...dd..dd...","...dd...dd..","..ddd...ddd."}),
                g.PixelSprite(new[]{"....dddd....","...dddddd...","...dssss....","....sss.....","oooodggcc...","....dggcccc.","....dggc....","....dddd....","...dd..dd...","..dd....dd..",".dd......dd.","ddd......ddd"})};art.sprite=frames[0];
            wire=MakeLine("Particle wire",.055f,AncientGame.Cyan);absorb=MakeLine("Absorption stream",.10f,AncientGame.Cyan);
            bikeArt=g.Box("Particle motorcycle",Vector2.zero,new Vector2(1,1),Color.white,9);bikeArt.transform.SetParent(transform,false);
            bikeArt.GetComponent<SpriteRenderer>().sprite=g.PixelSprite(new[]{"...........cc...........","..........ccg...........","....cccccggggcc.........","...cgggggggggggcc.......","..cgggggggggggggccc.....",".ddddggggggggggdddd.....","ddggddggggggggddggdd....","dgddgd........dgddgd....","ddggdd........ddggdd....",".dddd..........dddd....."});
            bikeArt.transform.localPosition=new Vector3(0,-.55f,0);bikeArt.SetActive(false);
        }
        LineRenderer MakeLine(string name,float width,Color color)
        {
            var o=new GameObject(name);o.transform.SetParent(transform,false);var line=o.AddComponent<LineRenderer>();
            line.material=new Material(Shader.Find("Sprites/Default"));line.startColor=line.endColor=color;line.startWidth=line.endWidth=width;
            line.positionCount=2;line.sortingOrder=15;line.enabled=false;return line;
        }
        bool Grounded()
        {return Physics2D.BoxCast((Vector2)transform.position+Vector2.down*.78f,new Vector2(.58f,.12f),0,Vector2.down,.12f,1<<8).collider!=null;}
        void Update()
        {
            var g=AncientGame.Instance;
            if(g.won){body.linearVelocity=Vector2.zero;body.gravityScale=0;wire.enabled=false;absorb.enabled=false;return;}
            if(transform.position.y<-9){Respawn();return;}
            if(Grounded())
            {
                lastGrounded=Time.time;
                if(transform.position.x>44 && transform.position.x<55)checkpoint=new Vector2(46,3);
                if(transform.position.x>85 && transform.position.x<95)checkpoint=new Vector2(86,2);
            }
            axis=g.skills?0:Input.GetAxisRaw("Horizontal");if(axis!=0)direction=Mathf.Sign(axis);
            if(!g.skills && Input.GetKeyDown(KeyCode.Space))jumpQueued=true;
            bool control=Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl);
            if(Input.GetKeyDown(KeyCode.LeftControl)||Input.GetKeyDown(KeyCode.RightControl)){controlAt=Time.time;controlConsumed=false;}
            if(control && !controlConsumed && !g.skills && Time.time-controlAt>=.45f)
            {
                controlConsumed=true;
                if(bike)g.Notice("Press X to detonate the motorcycle.");
                else if(!bikeUnlocked)g.Notice("Unlock motorcycle in the skill tree (K).");
                else if(particles<30)g.Notice("Need 30 particles to construct a motorcycle.");
                else {particles-=30;bike=true;bikeHP=80;g.Burst(transform.position,AncientGame.Cyan);}
            }
            if((Input.GetKeyUp(KeyCode.LeftControl)||Input.GetKeyUp(KeyCode.RightControl))&&!controlConsumed&&!g.skills)running=!running;
            if(!g.skills && Input.GetKeyDown(KeyCode.X)&&bike)ExplodeBike();
            if(!g.skills && (Input.GetKeyDown(KeyCode.E)||Input.GetMouseButtonDown(1)))FireWire();
            if(wired && Time.time>=wireUntil)wired=false;
            wire.enabled=wired;if(wired){wire.SetPosition(0,transform.position+Vector3.right*direction*.4f);wire.SetPosition(1,anchor);}
            if(!g.skills && (Input.GetKeyDown(KeyCode.J)||Input.GetMouseButtonDown(0)) && Time.time>=attackAt)
            {
                attackAt=Time.time+.3f;g.Burst((Vector2)transform.position+Vector2.right*direction,AncientGame.Cyan,6);
                foreach(var e in g.enemies.ToArray())if(e && Vector2.Distance(e.transform.position,transform.position)<2.6f && (e.transform.position.x-transform.position.x)*direction>-.3f)e.Hit(25);
            }
            absorb.enabled=false;
            if(!g.skills && Input.GetKey(KeyCode.F))
            {
                // Absorption changes no movement speed and never blocks melee damage.
                absorbPhase+=Time.deltaTime;float nearest=7;AncientProjectile target=null;
                foreach(var shot in g.shots)if(shot){float d=Vector2.Distance(transform.position,shot.transform.position);if(d<nearest&&!Physics2D.Linecast(transform.position,shot.transform.position,1<<8)){nearest=d;target=shot;}}
                if(target){absorb.enabled=true;absorb.SetPosition(0,transform.position);absorb.SetPosition(1,target.transform.position);particles=Mathf.Min(100,particles+(target.scrap?18:12));g.Burst(target.transform.position,AncientGame.Cyan,8);target.Remove();}
                else {absorb.enabled=true;Vector2 p=(Vector2)transform.position+new Vector2(direction*(.8f+Mathf.Sin(absorbPhase*20)*.1f),.15f);absorb.SetPosition(0,transform.position);absorb.SetPosition(1,p);}
            }
            art.flipX=direction<0;art.sprite=frames[Mathf.Abs(axis)>.1f?(int)(Time.time*(bike?15:10))%2:0];
            art.color=Time.time<invulnerableUntil && (int)(Time.time*14)%2==0?new Color(1,1,1,.35f):Color.white;
            bikeArt.SetActive(bike);bikeArt.GetComponent<SpriteRenderer>().flipX=direction<0;
        }
        void FixedUpdate()
        {
            if(AncientGame.Instance.won)return;
            var velocity=body.linearVelocity;
            if(wired)
            {
                Vector2 delta=anchor-(Vector2)transform.position;Vector2 radial=delta.normalized;
                Vector2 tangent=new Vector2(radial.y,-radial.x)*direction;
                // Immediate pull plus forward tangent: no hanging/waiting for a release input.
                velocity+= (radial*38+tangent*25)*Time.fixedDeltaTime;
                if(delta.magnitude<2)wired=false;
                velocity=Vector2.ClampMagnitude(velocity,26);
            }
            else
            {
                float speed=bike?15:running?9:5;
                float acceleration=Grounded()?70:22;
                // Keep aerial momentum after a swing or explosion; steer without abrupt braking.
                if(axis!=0||Grounded())velocity.x=Mathf.MoveTowards(velocity.x,axis*speed,acceleration*Time.fixedDeltaTime);
            }
            if(jumpQueued){if(Time.time-lastGrounded<.12f){velocity.y=13;lastGrounded=-100;wired=false;}jumpQueued=false;}
            velocity.y=Mathf.Max(velocity.y,-25);body.linearVelocity=velocity;
        }
        void FireWire()
        {
            if(Time.time<wireReady)return;
            var g=AncientGame.Instance;Vector2 mouse=Camera.main?Camera.main.ScreenToWorldPoint(Input.mousePosition):transform.position;
            float score=float.MaxValue;bool found=false;Vector2 selected=Vector2.zero;
            foreach(var p in g.anchors)
            {
                if(Vector2.Distance(p,transform.position)>20||p.y<transform.position.y-1)continue;
                if(Physics2D.Linecast(transform.position,p,1<<8))continue;
                float s=Vector2.Distance(mouse,p);if(s<score){score=s;selected=p;found=true;}
            }
            if(!found){g.Notice("No visible anchor in reach. Aim at a cyan overhead node.");return;}
            anchor=selected;wired=true;wireUntil=Time.time+.95f;wireReady=Time.time+.18f;
            Vector2 radial=(anchor-(Vector2)transform.position).normalized;
            Vector2 tangent=new Vector2(radial.y,-radial.x)*direction;
            body.linearVelocity=radial*12+tangent*14;g.Burst(transform.position,AncientGame.Cyan,5);
        }
        public void Hurt(float damage)
        {
            if(Time.time<invulnerableUntil||AncientGame.Instance.won)return;
            invulnerableUntil=Time.time+.5f;
            if(bike){bikeHP-=damage;if(bikeHP<=0)ExplodeBike();return;}
            hp-=damage;AncientGame.Instance.Burst(transform.position,AncientGame.Orange,5);
            if(hp<=0)Respawn();
        }
        public void ExplodeBike()
        {
            if(!bike)return;bike=false;wired=false;
            var g=AncientGame.Instance;g.Burst(transform.position,AncientGame.Orange,35);
            foreach(var e in g.enemies.ToArray())if(e&&Vector2.Distance(e.transform.position,transform.position)<5)e.Hit(80);
            body.linearVelocity=new Vector2(direction*17,19);invulnerableUntil=Time.time+.7f;
            g.Notice("Explosion launch! Aim at an anchor and press E / RMB.");
        }
        void Respawn()
        {
            hp=100;particles=Mathf.Max(40,particles);bike=false;wired=false;body.linearVelocity=Vector2.zero;
            transform.position=checkpoint;invulnerableUntil=Time.time+1.5f;
            AncientGame.Instance.Notice("Reconstructed at checkpoint. Cores and unlock retained.");
        }
    }

    public sealed class AncientEnemy : MonoBehaviour
    {
        public int kind;float hp=60,nextAttack,hitAt=-1;SpriteRenderer art;Vector2 home;
        void Start()
        {
            var g=AncientGame.Instance;home=transform.position;art=gameObject.AddComponent<SpriteRenderer>();art.sortingOrder=8;
            art.sprite=g.PixelSprite(new[]{"...dddd...","..dooodd..","..dwwdd...","...ggg....","..gggggg..",".gggggggg.","..gggggg..","...dddd...","..dd..dd..","..dd..dd..",".ddd..ddd."});
            nextAttack=Time.time+1+kind*.4f;
        }
        void Update()
        {
            var g=AncientGame.Instance;if(!g.hero||g.won)return;
            Vector2 delta=g.hero.transform.position-transform.position;art.flipX=delta.x<0;
            if(kind==0)
            {
                if(hitAt>=0)
                {
                    art.color=AncientGame.Orange;
                    if(Time.time>=hitAt){if(delta.magnitude<2.8f)g.hero.Hurt(22);g.Burst((Vector2)transform.position+Vector2.right*Mathf.Sign(delta.x),AncientGame.Orange,6);hitAt=-1;nextAttack=Time.time+1.0f;}
                }
                else
                {
                    art.color=Color.white;
                    if(delta.magnitude<2.7f&&Time.time>nextAttack)hitAt=Time.time+.55f;
                    else if(Mathf.Abs(delta.x)>1.6f&&Mathf.Abs(delta.x)<9)
                    {float x=Mathf.MoveTowards(transform.position.x,g.hero.transform.position.x,2*Time.deltaTime);x=Mathf.Clamp(x,home.x-2,home.x+2);transform.position=new Vector2(x,home.y);}
                }
            }
            else if(delta.magnitude<19&&Time.time>nextAttack)
            {
                nextAttack=Time.time+(kind==2?2.2f:1.65f);
                var shot=new GameObject(kind==2?"Thrown scrap":"Energy bolt").AddComponent<AncientProjectile>();
                shot.transform.position=(Vector2)transform.position+delta.normalized*.7f;shot.scrap=kind==2;
                shot.velocity=delta.normalized*(kind==2?9:12);g.shots.Add(shot);
            }
        }
        public void Hit(float damage)
        {
            hp-=damage;AncientGame.Instance.Burst(transform.position,AncientGame.Cyan,5);
            if(hp<=0){var g=AncientGame.Instance;g.SpawnPickup(transform.position,false);g.enemies.Remove(this);Destroy(gameObject);}
        }
    }
    public sealed class AncientProjectile : MonoBehaviour
    {
        public bool scrap;public Vector2 velocity;float age;bool removed;
        void Start()
        {
            var s=gameObject.AddComponent<SpriteRenderer>();s.sprite=AncientGame.Instance.PixelSprite(scrap?new[]{".ggg.","ggwgg","ggggg",".ggg."}:new[]{".cc.","cccc",".cc."});
            s.sortingOrder=12;
        }
        void Update()
        {
            if(removed)return;var g=AncientGame.Instance;if(g.won){Remove();return;}
            age+=Time.deltaTime;Vector2 previous=transform.position;Vector2 next=previous+velocity*Time.deltaTime;
            // Sweep before moving so projectiles cannot tunnel through platforms or the hero.
            if(Physics2D.Linecast(previous,next,1<<8)){Remove();return;}
            if(g.hero && age>.1f)
            {
                Vector2 h=g.hero.transform.position;Vector2 segment=next-previous;
                float t=segment.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(h-previous,segment)/segment.sqrMagnitude):0;
                if(Vector2.Distance(h,previous+segment*t)<.8f)
                {
                    if(Input.GetKey(KeyCode.F)&&!g.skills){g.hero.particles=Mathf.Min(100,g.hero.particles+(scrap?18:12));g.Burst(previous,AncientGame.Cyan,8);}
                    else g.hero.Hurt(scrap?25:15);
                    Remove();return;
                }
            }
            transform.position=next;if(scrap)transform.Rotate(0,0,300*Time.deltaTime);if(age>5)Remove();
        }
        public void Remove(){if(removed)return;removed=true;AncientGame.Instance.shots.Remove(this);Destroy(gameObject);}
    }
    public sealed class AncientPickup : MonoBehaviour
    {
        public bool core;Vector2 origin;float phase;
        void Start()
        {
            origin=transform.position;var s=gameObject.AddComponent<SpriteRenderer>();s.sortingOrder=6;
            s.sprite=AncientGame.Instance.PixelSprite(core?new[]{"..oo..",".occo.","occcco","occcco",".occo.","..oo.."}:new[]{".cc.","cccc","cccc",".cc."});
        }
        void Update()
        {
            phase+=Time.deltaTime;transform.position=origin+Vector2.up*Mathf.Sin(phase*3)*.2f;
            var g=AncientGame.Instance;if(!g.hero||g.won)return;
            if(Vector2.Distance(g.hero.transform.position,transform.position)<1.3f)
            {
                if(core){g.relics++;g.Notice("Ancient core "+g.relics+"/3 - something above the city is responding.",5);}
                g.hero.particles=Mathf.Min(100,g.hero.particles+(core?25:20));g.Burst(transform.position,AncientGame.Cyan,10);
                g.pickups.Remove(this);Destroy(gameObject);
            }
        }
    }
    public sealed class AncientFragment : MonoBehaviour
    {
        public Vector2 velocity;float age;
        void Update(){age+=Time.deltaTime;transform.position+=(Vector3)(velocity*Time.deltaTime);velocity+=Vector2.down*12*Time.deltaTime;if(age>.45f)Destroy(gameObject);}
    }
}
