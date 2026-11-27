using System;
using System.Collections.Generic;
using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.PlayerCore
{
    // 방 하나만 활성화한다. 물리 장치, 입구와 배경은 방 루트와 수명을 같이한다.
    [DefaultExecutionOrder(80)]
    public sealed partial class PatrolRoom : MonoBehaviour
    {
        sealed class Interaction
        {
            public string name;
            public Vector2 point;
            public float range;
            public int destination;
            public Action use;
            public Func<bool> unlocked;
            public SpriteRenderer lamp;
            public bool available;
        }

        static readonly Color[] accents = {new Color(.96f,.67f,.31f),new Color(.96f,.54f,.54f),
            new Color(.45f,.86f,.89f),new Color(.52f,.86f,.65f),new Color(.65f,.76f,1f)};
        static Sprite pixel;
        readonly List<Action<float>> steps = new List<Action<float>>();
        readonly List<Interaction> actions = new List<Interaction>();
        readonly List<BluePlugSocket> bluePlugSockets = new List<BluePlugSocket>();
        PatrolExpedition expedition;
        Rigidbody2D officer;
        PlayerBluePlug bluePlug;
        PlayerSummonLoadout loadout;
        Vector2 beamStart, beamEnd;
        int lastShot;
        bool released;
        int room, lastArrival;
        BluePlugSocket arrival;
        Interaction selected;

        public static string NameOf(int id) => PatrolCatalog.Name(id);
        public string Title => socketRoute ? PatrolSocketCatalog.Name(room) : NameOf(room);
        public BluePlugSocket[] BluePlugSockets => bluePlugSockets.ToArray();
        public string Prompt => selected==null || (loadout && loadout.Equipped) ? "" : selected.available ? "E  " + selected.name : selected.name+" · 반대편에서 개방";
        public Color Accent => accents[(room-1)/10];
        public Vector2 Hero => (Vector2)transform.InverseTransformPoint(officer.position);
        public Vector2 Velocity => officer.linearVelocity;
        public bool Grounded => expedition.Player.Grounded;
        public bool Zipping => bluePlug.Moving;
        public bool TrialActive => expedition.TrialActive;
        public float Axis => expedition.InputReader.Move.x;
        public bool JumpHeld => expedition.InputReader.JumpHeld;
        public bool CollectHeld => expedition.InputReader.CollectHeld;
        public bool PrimaryHeld => loadout && loadout.Cutting;
        public float Elapsed {get;private set;}
        public bool Near(float x,float y,float distance) => (Hero-new Vector2(x,y)).sqrMagnitude<=distance*distance;
        public bool Flag(string key) => expedition.Flag(key);
        public void SetFlag(string key,bool value=true) => expedition.SetFlag(key,value);
        public int Charge => expedition.Charge;
        public void AddCharge(int amount) => expedition.AddCharge(amount);
        public bool SpendCharge(int amount) => expedition.SpendCharge(amount);
        public void Say(string message) => expedition.Notice(message);
        public void Step(Action<float> callback) => steps.Add(callback);
        public bool Arrived(GameObject hook) => arrival && hook && arrival.gameObject==hook;
        public void Push(float vx,float vy) {officer.linearVelocity=new Vector2(vx,vy);}
        public void Warp(float x,float y)
        {
            bluePlug.ResetLink();
            expedition.Player.Respawn(transform.TransformPoint(new Vector2(x,y)));
        }

        public void Build(PatrolExpedition manager,int id,bool socketRoute=false)
        {
            if (expedition || id<1 || id>PatrolCatalog.Count) throw new ArgumentOutOfRangeException(nameof(id));
            expedition=manager; room=id; this.socketRoute=socketRoute; bluePlug=manager.BluePlug;
            officer=manager.Player.GetComponent<Rigidbody2D>(); loadout=manager.Player.GetComponent<PlayerSummonLoadout>();
            lastShot=loadout.LaserSequence;
            lastArrival=bluePlug.ArrivalSequence;
            if (!pixel)
            {
                pixel=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1,0,SpriteMeshType.FullRect);
            }
            int floor=(id-1)/10;
            var skyline=AncientArt.Display("구역 전경",AncientArt.DistrictBackdrop(floor),new Vector2(24,8),-30,transform);
            var size=skyline.sprite.bounds.size;
            skyline.transform.localScale=new Vector3(50/size.x,19/size.y,1);
            skyline.color=new Color(.65f,.70f,.77f);
            if (socketRoute) BuildSocketPaving(id);
            else
            {
                Box("순찰로 기초",24,-.8f,52,1.6f,new Color(.13f,.18f,.23f),true,0);
                Box("기초 가장자리",24,-.10f,52,.17f,Color.Lerp(Accent,Color.black,.38f),false,1);
            }
            Box("밀폐 천장",24,16.6f,52,1.2f,new Color(.13f,.19f,.24f),true,0);
            for(int bay=0;bay<7;bay++)
            {
                float x=.9f+bay*7.7f;
                Box("천장 이음",x,15.86f,.26f,.29f,new Color(.22f,.29f,.35f),false,-1);
                Box("구역 유도등",x,15.7f,.68f,.09f,Accent,false,1);
            }
            // 소환·레이저가 필요한 길은 첫 방문이나 사망 직후에도 열 수 있다.
            float supplyX = 3.8f;
            Box("입자 보급 전구",supplyX,.7f,.32f,.53f,new Color(.82f,.71f,1f),false,4);
            Sign("보급 전구 · 우클릭",supplyX,2.22f);
            bool supplyHeld = false;
            Step(dt => {
                bool collecting = CollectHeld && Near(supplyX,1.1f,1.5f);
                if (collecting && !supplyHeld && Charge < loadout.SupplyTarget)
                    AddCharge(loadout.SupplyTarget-Charge);
                supplyHeld = collecting;
            });
            if (socketRoute)
            {
                BuildSocketStreet(id);
                InstallElectricFixtures(id);
                if (manager.StoryStreet && id == 1) BuildTowBench(5.3f);
                if (manager.StoryStreet && id == 6) BuildSwingBench(5.3f);
            }
            else
            {
                switch(floor)
                {
                    case 0: BuildProduction(id); break;
                    case 1: BuildLiving(id); break;
                    case 2: BuildAdministration(id); break;
                    case 3: BuildResidence(id); break;
                    case 4: BuildSummit(id); break;
                }
                InstallPlugWorks(id);
            }
            if (!manager.TrialActive && !manager.StoryStreet)
            {
                // 같은 구역의 두 순찰로는 서쪽 입구에서 만난다. 시험 주행 중에는 이 문이 기록을 끊지 않는다.
                Box("서쪽 갈림문 기둥", 2, .98f, .23f, 1.96f, new Color(.16f,.22f,.28f),false,-2);
                Box("갈림문 표시등", 2, 1.86f, .52f, .12f, Accent,false,-1);
                Use(socketRoute ? "갈림문 · 기존 순찰로" : "갈림문 · 벽 콘센트 길", 2, 1, manager.SwitchStreet, .94f);
            }
        }

        public GameObject Box(string label,float x,float y,float w,float h,Color tint,bool solid=false,int order=2)
        {
            var go=new GameObject(label);
            go.transform.SetParent(transform,false);
            go.transform.localPosition=new Vector2(x,y);
            var renderer=go.AddComponent<SpriteRenderer>();
            renderer.sprite=pixel; renderer.drawMode=SpriteDrawMode.Sliced; renderer.size=new Vector2(w,h);
            renderer.color=tint; renderer.sortingOrder=order;
            if (solid) {go.layer=8; go.AddComponent<BoxCollider2D>().size=renderer.size;}
            return go;
        }
        public GameObject Deck(string label,float x,float top,float width,float depth=.35f)
        {
            var go=Box(label,x,top-depth*.5f,width,depth,Color.Lerp(Accent,new Color(.10f,.15f,.21f),.72f),true,3);
            var effector=go.AddComponent<PlatformEffector2D>();
            effector.useOneWay=true; effector.surfaceArc=155;
            go.GetComponent<BoxCollider2D>().usedByEffector=true;
            var lip=Box("디딤면",x,top-.045f,width,.09f,Color.Lerp(Accent,Color.white,.18f),false,4);
            lip.transform.SetParent(go.transform,true);
            return go;
        }
        public GameObject Hook(string label,float x,float y)
        {
            var go=new GameObject(label);
            go.transform.SetParent(transform,false);go.transform.localPosition=new Vector2(x,y);
            // 기존 장치의 이동과 개폐 상태를 쓰고, 도착은 파란 콘센트를 통해 판정한다.
            var anchor=go.AddComponent<FixedGrappleAnchor>();
            anchor.clockOwner=expedition.InputReader;
            var socket=go.AddComponent<BluePlugSocket>();
            socket.BindAnchor(anchor);
            RegisterBluePlugSocket(socket);
            return go;
        }
        void RegisterBluePlugSocket(BluePlugSocket socket) => bluePlugSockets.Add(socket);
        public void Sign(string label,float x,float y)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);go.transform.localPosition=new Vector2(x,y);
            go.AddComponent<DistrictWorldSign>().text=label;
        }
        public void Use(string label,float x,float y,Action action,float radius=1.8f)
        {
            actions.Add(new Interaction{name=label,point=new Vector2(x,y),range=radius,use=action,available=true});
        }
        public void Portal(string label,float x,float y,int destination,Func<bool> open=null)
        {
            var frame=Box(label,x,y,1.08f,1.98f,new Color(.11f,.18f,.24f),false,-1);
            var lamp=Box(label+" 표지등",x,y+.85f,.86f,.12f,Accent,false,3);
            lamp.transform.SetParent(frame.transform,true);
            actions.Add(new Interaction{name=label,point=new Vector2(x,y),range=1.65f,
                destination=destination, use=()=>expedition.TravelDoor(destination), unlocked=open, lamp=lamp.GetComponent<SpriteRenderer>()});
        }
        public bool TryArrivalFrom(int source,out Vector2 position)
        {
            Physics2D.SyncTransforms();
            foreach(var entry in actions)
            {
                if(entry.destination!=source || entry.unlocked!=null && !entry.unlocked())continue;
                // 양방향 문을 나올 때 반대편 발판이나 문틀 속에서 시작하지 않게 한다.
                for(int i=0;i<6;i++)
                {
                    position=entry.point + Vector2.up*(.12f+i*.31f);
                    if(position.y<.92f)position.y=.92f;
                    Vector2 world=transform.TransformPoint(position);
                    if(!Physics2D.OverlapCapsule(world,new Vector2(.70f,1.76f),CapsuleDirection2D.Vertical,0,1<<8))return true;
                }
            }
            position=default;
            return false;
        }

        public void Place(GameObject go,float x,float y)
        {
            if (!go) return;
            var next=(Vector2)transform.TransformPoint(new Vector2(x,y));
            if (go.TryGetComponent<GrappleAnchor>(out var socket))
            {
                socket.MovePatrolSocket(next,Time.fixedDeltaTime);
                return;
            }
            if (!go.TryGetComponent<Collider2D>(out var surface))
            {go.transform.position=next;return;}
            if (!go.TryGetComponent<Rigidbody2D>(out var carrier))
            {
                carrier=go.AddComponent<Rigidbody2D>();carrier.bodyType=RigidbodyType2D.Kinematic;
                carrier.interpolation=RigidbodyInterpolation2D.Interpolate;
            }
            var delta=next-carrier.position;
            if(surface.enabled && go.activeInHierarchy && Grounded && !Zipping && delta.sqrMagnitude<1f)
            {
                var b=surface.bounds;var feet=officer.position-Vector2.up*.9f;
                if(Mathf.Abs(feet.y-b.max.y)<.12f && feet.x>b.min.x-.2f && feet.x<b.max.x+.2f)
                    officer.position+=delta;
            }
            if(go.activeInHierarchy)carrier.MovePosition(next);
            else carrier.position=next;
        }
        public bool BeamHits(GameObject target)
        {
            if(!released || !target || !target.activeInHierarchy) return false;
            var bounds=target.TryGetComponent<Collider2D>(out var collider) ? collider.bounds :
                target.TryGetComponent<SpriteRenderer>(out var sprite) ? sprite.bounds : new Bounds(target.transform.position,Vector3.one*.5f);
            Vector2 delta=beamEnd-beamStart;
            if(delta.sqrMagnitude<.001f)return false;
            bounds.Expand(new Vector3(.08f,.08f,.1f));
            var ray=new Ray(new Vector3(beamStart.x,beamStart.y,bounds.center.z),new Vector3(delta.x,delta.y,0).normalized);
            return bounds.IntersectRay(ray,out float distance) && distance<=delta.magnitude+.05f;
        }
        void Update()
        {
            if (!expedition || expedition.Paused) return;
            selected=null;float nearest=float.MaxValue;
            foreach(var entry in actions)
            {
                entry.available=entry.unlocked==null || entry.unlocked();
                if(entry.lamp)entry.lamp.color=entry.available ? Accent : new Color(.52f,.34f,.25f);
                float d=(Hero-entry.point).sqrMagnitude;
                if(d>entry.range*entry.range)continue;
                if(!entry.available)d+=100;
                if(d<nearest){nearest=d;selected=entry;}
            }
            if(selected!=null && selected.available && !loadout.BlocksInteract && expedition.InputReader.InteractPressed)selected.use();
        }
        void FixedUpdate()
        {
            if(!expedition || expedition.Paused)return;
            float dt=Time.fixedDeltaTime;Elapsed+=dt;
            arrival=lastArrival!=bluePlug.ArrivalSequence?bluePlug.LastArrival:null;
            lastArrival=bluePlug.ArrivalSequence;
            released=lastShot!=loadout.LaserSequence;
            lastShot=loadout.LaserSequence;
            if(released){beamStart=loadout.LaserStart;beamEnd=loadout.LaserEnd;}
            foreach(var update in steps)update(dt);
            StepPlugWorks(dt);
        }
    }
}
