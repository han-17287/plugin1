using UnityEngine;
namespace Plugin.AncientCivilization
{
    // First phase only. No health threshold silently introduces an unrequested second phase.
    public sealed class AncientClockBoss : MonoBehaviour
    {
        enum State { Rest,Vector,Flash,TeleportTell,AreaWindup,AreaActive,RainTell,Rain }
        AncientEnemy owner;AncientGame game;State state;float began,until,nextRain,hour,minute;
        int vectorIndex,areaIndex,rainIndex,cursor,lastTick=-1;
        AncientClockDial face;Vector2 centre,destination;float left,right;
        LineRenderer dial,hourHand,minuteHand,landing;
        readonly AncientClockSpear[] spears=new AncientClockSpear[24];
        public bool Telegraphing {get{return state==State.Vector||state==State.AreaWindup||state==State.RainTell||state==State.TeleportTell;}}
        public bool Recovering {get{return state==State.Rest;}}
        public bool Physical {get{return state==State.AreaWindup||state==State.AreaActive;}}
        public float WindupProgress {get{return Telegraphing?Mathf.Clamp01((Time.time-began)/Mathf.Max(.01f,until-began)):0;}}
        public float RecoveryRemaining {get{return Recovering?Mathf.Clamp01((until-Time.time)/1.6f):0;}}
        public void Initialize(AncientEnemy enemy,AncientGame world)
        {
            owner=enemy;game=world;left=AncientCityLayout.ArenaLeft(4)+1;right=AncientCityLayout.ArenaRight(4)-1;
            face=new GameObject("Secretary brass clock dial").AddComponent<AncientClockDial>();face.Initialize();
            dial=game.Line("Secretary clock face",new Color(1,.8f,.3f),.04f,15);dial.loop=true;dial.positionCount=64;
            hourHand=game.Line("Hour force / no resultant arrow",new Color(1,.73f,.17f),.12f,16);hourHand.positionCount=2;
            minuteHand=game.Line("Minute force / no resultant arrow",new Color(.8f,.95f,1),.08f,16);minuteHand.positionCount=2;
            landing=game.Line("Teleport watch tell",new Color(1,.6f,.2f,.65f),.04f,14);landing.loop=true;landing.positionCount=32;
            for(int i=0;i<spears.Length;i++)
            {var item=new GameObject("Pooled clock spear");item.transform.SetParent(transform.parent,false);spears[i]=item.AddComponent<AncientClockSpear>();spears[i].Initialize(game);}
            Hide();Enter(State.Rest,1.6f);
        }
        void Enter(State value,float duration){state=value;began=Time.time;until=began+duration;}
        void Hide(){dial.enabled=hourHand.enabled=minuteHand.enabled=landing.enabled=false;if(face)face.Hide();}
        void Circle(LineRenderer line,Vector2 origin,float radius)
        {if(line==dial){face.Show(origin,radius,line.startColor);line.enabled=false;return;}line.enabled=true;for(int i=0;i<line.positionCount;i++){float a=i*Mathf.PI*2/line.positionCount;line.SetPosition(i,origin+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);}}
        void Hands(Vector2 origin,float h,float m,float hourLength,float minuteLength)
        {
            hourHand.enabled=minuteHand.enabled=true;hourHand.SetPosition(0,origin);minuteHand.SetPosition(0,origin);
            hourHand.SetPosition(1,origin+Direction(h)*hourLength);minuteHand.SetPosition(1,origin+Direction(m)*minuteLength);
        }
        static Vector2 Direction(float a){return new Vector2(Mathf.Cos(a),Mathf.Sin(a));}
        void Spawn(Vector2 p,Vector2 v,int kind)
        {
            for(int i=0;i<spears.Length;i++)
            {int at=(cursor+i)%spears.Length;if(!spears[at].Available)continue;spears[at].Play(p,v,kind);cursor=(at+1)%spears.Length;return;}
        }
        public bool TryBlade(Vector2 origin,Vector2 direction)
        {
            bool hit=false;foreach(var spear in spears)
            {
                if(!spear||spear.Available)continue;Vector2 point=spear.BreakPoint,delta=point-origin;
                if(delta.sqrMagnitude>3.1f*3.1f||Vector2.Dot(delta.normalized,direction)<-.15f||Physics2D.Linecast(origin,point,1<<8))continue;
                spear.Break();hit=true;
            }
            return hit;
        }
        public void StopBattle(){Hide();foreach(var spear in spears)if(spear)spear.Disarm();}
        void BeginVector(){Hide();lastTick=-1;Enter(State.Vector,AncientClockRules.SpinSeconds);}
        void BeginTeleport()
        {
            Hide();Vector2 hero=game.hero.transform.position;
            landing.startColor=landing.endColor=new Color(1,.6f,.2f,.65f);
            destination=areaIndex==2?new Vector2((left+right)*.5f,5.1f):new Vector2(Mathf.Clamp(hero.x+(areaIndex==0?2.3f:-2.3f),left+1,right-1),0);
            Circle(landing,destination,areaIndex==2?1.2f:.6f);Enter(State.TeleportTell,.45f);
        }
        public void Tick()
        {
            owner.BossContact();
            Vector2 target=game.hero.transform.position;float facing=target.x<owner.transform.position.x?-1:1;
            owner.BossVisual(state==State.Rest?5:state==State.Vector?1:state==State.AreaActive?2:3,facing,WindupProgress,Telegraphing,Recovering);
            if(state==State.Rest)
            {if(Time.time>=until){vectorIndex=0;BeginVector();}return;}
            if(state==State.Vector)
            {
                centre=owner.AimPoint;Vector2 delta=target-centre;
                int tick=Mathf.CeilToInt(until-Time.time);if(tick!=lastTick){lastTick=tick;game.Sound(2);}
                AncientClockRules.Angles(Mathf.Atan2(delta.y,delta.x),WindupProgress,out hour,out minute);
                Circle(dial,centre,3.55f);Hands(centre,hour,minute,AncientClockRules.HourForce,AncientClockRules.MinuteForce);
                if(Time.time>=until)
                {
                    Vector2 resultant=Direction(hour)*AncientClockRules.HourForce+Direction(minute)*AncientClockRules.MinuteForce;
                    Spawn(centre,resultant.normalized*15,0);
                    if(AncientClockRules.ThirdVectorShot(vectorIndex)){Spawn(centre,Direction(hour)*15,0);Spawn(centre,Direction(minute)*15,1);}
                    Hide();Enter(State.Flash,.6f);
                }
                return;
            }
            if(state==State.Flash)
            {if(Time.time>=until){if(++vectorIndex<3)BeginVector();else{areaIndex=0;BeginTeleport();}}return;}
            if(state==State.TeleportTell)
            {
                Circle(landing,destination,.5f+.25f*Mathf.Sin(WindupProgress*Mathf.PI));
                if(Time.time>=until)
                {game.Burst(owner.AimPoint,AncientGame.Cyan,12);owner.transform.position=destination;centre=owner.AimPoint;landing.enabled=false;Enter(State.AreaWindup,areaIndex==2?1.1f:.7f);}
                return;
            }
            if(state==State.AreaWindup||state==State.AreaActive)
            {
                centre=owner.AimPoint;
                float elapsed=Time.time-began,radius=areaIndex==2?11:3;
                if(areaIndex==2)
                {landing.startColor=landing.endColor=AncientGame.Cyan;Circle(landing,new Vector2(centre.x,.2f),AncientClockRules.UnderSafeHalfWidth);}
                Circle(dial,centre,radius);float angle=elapsed*(areaIndex==2?3:5);
                Hands(centre,angle,-angle,radius,radius);
                if(state==State.AreaWindup)
                {
                    dial.startColor=dial.endColor=new Color(1,.8f,.3f,.25f+.35f*WindupProgress);
                    if(Time.time>=until){Enter(State.AreaActive,areaIndex==2?1.3f:.3f);dial.startColor=dial.endColor=new Color(1,.6f,.2f);game.Sound(8);}
                }
                else
                {
                    Vector2 relative=target-centre;
                    bool hit=areaIndex==2?AncientClockRules.BroadHit(relative.x,relative.y,radius):relative.sqrMagnitude<radius*radius;
                    if(hit)game.hero.Hurt(areaIndex==2?28:22);
                    if(Time.time>=until)
                    {
                        Hide();if(++areaIndex<3)BeginTeleport();
                        else{owner.transform.position=new Vector2((left+right)*.5f,0);rainIndex=0;Enter(State.RainTell,.9f);}
                    }
                }
                return;
            }
            if(state==State.RainTell)
            {
                landing.startColor=landing.endColor=AncientGame.Orange;
                Circle(landing,new Vector2(Mathf.Clamp(target.x,left+1,right-1),8.4f),.45f);
                if(Time.time>=until){Hide();Enter(State.Rain,4.2f);nextRain=Time.time;}
                return;
            }
            if(state==State.Rain)
            {
                if(Time.time>=nextRain)
                {
                    // Each falling triplet has wide gaps and a fixed target snapshot; no mid-flight steering.
                    float x=Mathf.Clamp(target.x,left+1,right-1);
                    for(int i=0;i<3;i++)Spawn(new Vector2(Mathf.Clamp(x+(i-1)*3.4f,left+.3f,right-.3f),8.6f),Vector2.down*(7.5f+i),i);
                    rainIndex++;nextRain=Time.time+.65f;
                }
                if(Time.time>=until){Hide();Enter(State.Rest,1.8f);}
            }
        }
        void OnDestroy()
        {
            if(face)Destroy(face.gameObject);if(dial)Destroy(dial.gameObject);if(hourHand)Destroy(hourHand.gameObject);if(minuteHand)Destroy(minuteHand.gameObject);if(landing)Destroy(landing.gameObject);
            foreach(var spear in spears)if(spear)Destroy(spear.gameObject);
        }
    }
    public sealed class AncientClockDial : MonoBehaviour
    {
        Mesh mesh;MeshRenderer art;Material material;
        public void Initialize()
        {
            // One mesh for a brass annulus, twelve hour marks and the central hub.
            var vertices=new Vector3[308];var triangles=new int[462];int vi=0,ti=0;
            for(int i=0;i<64;i++)
            {
                float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;
                Quad(vertices,triangles,ref vi,ref ti,Radial(a,.992f),Radial(a,1.008f),Radial(b,1.008f),Radial(b,.992f));
            }
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;Vector2 radial=Radial(a,1),tangent=new Vector2(-radial.y,radial.x)*.012f;
                float inner=i%3==0?.82f:.88f;
                Quad(vertices,triangles,ref vi,ref ti,radial*inner-tangent,radial*.96f-tangent,radial*.96f+tangent,radial*inner+tangent);
            }
            Quad(vertices,triangles,ref vi,ref ti,new Vector2(-.04f,-.04f),new Vector2(.04f,-.04f),new Vector2(.04f,.04f),new Vector2(-.04f,.04f));
            var colors=new Color32[vertices.Length];for(int i=0;i<colors.Length;i++)colors[i]=new Color32(255,255,255,255);
            mesh=new Mesh();mesh.name="Clock dial / one mesh";mesh.vertices=vertices;mesh.colors32=colors;mesh.triangles=triangles;mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;art=gameObject.AddComponent<MeshRenderer>();
            material=new Material(Shader.Find("Sprites/Default"));art.sharedMaterial=material;art.sortingOrder=15;art.enabled=false;
        }
        static Vector2 Radial(float a,float r){return new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;}
        static void Quad(Vector3[] vertices,int[] triangles,ref int vi,ref int ti,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            vertices[vi]=a;vertices[vi+1]=b;vertices[vi+2]=c;vertices[vi+3]=d;
            triangles[ti++]=vi;triangles[ti++]=vi+1;triangles[ti++]=vi+2;triangles[ti++]=vi;triangles[ti++]=vi+2;triangles[ti++]=vi+3;vi+=4;
        }
        public void Show(Vector2 position,float radius,Color color)
        {transform.position=position;transform.localScale=Vector3.one*radius;material.color=color;art.enabled=true;}
        public void Hide(){if(art)art.enabled=false;}
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
    public sealed class AncientClockSpear : MonoBehaviour
    {
        AncientGame game;LineRenderer shape;Vector2 velocity;float age;bool live,stuck;int kind;
        public bool Available {get{return !live;}}
        public Vector2 BreakPoint {get{return (Vector2)transform.position+(stuck?Vector2.up*.32f:Vector2.zero);}}
        public void Initialize(AncientGame world)
        {
            game=world;shape=game.Line("Physical clock hand spear",new Color(1,.85f,.42f),.07f,17);
            shape.transform.SetParent(transform,false);shape.useWorldSpace=false;shape.loop=true;shape.positionCount=7;shape.enabled=false;
        }
        public void Play(Vector2 position,Vector2 speed,int hand)
        {
            live=true;stuck=false;age=0;velocity=speed;kind=hand;transform.position=position;
            transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(speed.y,speed.x)*Mathf.Rad2Deg);
            float length=kind==0?1.25f:kind==1?1.6f:1.85f,width=kind==2?.055f:.15f;
            shape.SetPosition(0,new Vector3(length*.5f,0));shape.SetPosition(1,new Vector3(length*.15f,width*2));
            shape.SetPosition(2,new Vector3(length*.15f,width));shape.SetPosition(3,new Vector3(-length*.5f,width));
            shape.SetPosition(4,new Vector3(-length*.5f,-width));shape.SetPosition(5,new Vector3(length*.15f,-width));shape.SetPosition(6,new Vector3(length*.15f,-width*2));
            shape.startColor=shape.endColor=kind==0?new Color(1,.77f,.25f):kind==1?new Color(.75f,.94f,1):new Color(1,.55f,.35f);shape.enabled=true;
        }
        public void Break(){if(!live)return;game.hero.Combat.EmitParticles(BreakPoint,kind==2?10:14);game.Burst(BreakPoint,AncientGame.Cyan,6);Remove();}
        public void Disarm(){Remove();}
        void Remove(){live=false;shape.enabled=false;}
        void Update()
        {
            if(!live||game.Paused)return;if(game.won){Remove();return;}age+=Time.deltaTime;
            if(age>9){Remove();return;}if(stuck)return;
            Vector2 previous=transform.position,next=previous+velocity*Time.deltaTime;
            var terrain=Physics2D.Linecast(previous,next,1<<8);
            if(terrain)
            {
                if(terrain.normal.y>.5f)
                {stuck=true;transform.position=terrain.point+Vector2.up*.5f;transform.rotation=Quaternion.Euler(0,0,-90);age=0;return;}
                Remove();return;
            }
            Vector2 segment=next-previous,target=game.hero.transform.position;
            float t=segment.sqrMagnitude>.001f?Mathf.Clamp01(Vector2.Dot(target-previous,segment)/segment.sqrMagnitude):0;
            if(Vector2.Distance(target,previous+segment*t)<.65f){game.hero.Hurt(18);Remove();return;}
            transform.position=next;
        }
        void OnDestroy(){if(shape)Destroy(shape.gameObject);}
    }
}
