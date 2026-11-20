using System.Collections.Generic;
using UnityEngine;
namespace Plugin.PlayerCore
{
    public abstract class GrappleAnchor : MonoBehaviour
    {
        static readonly List<GrappleAnchor> registry=new List<GrappleAnchor>();
        public static IReadOnlyList<GrappleAnchor> Registry=>registry;
        public GrappleAnchorData data;public PlayerInputReader clockOwner;
        bool operational=true;bool leased,spent;LineRenderer ring;Material material;bool selected;
        public Vector2 Point=>transform.position;
        bool hasPatrolSample;
        public bool PatrolDriven {get;private set;}
        public Vector2 PatrolVelocity {get;private set;}

        // 순찰실에서 물리 틱마다 이동시키는 고리만 도착 예측을 사용한다.
        // 첫 배치는 저작 위치에서 실제 궤도로 옮기는 초기화일 수 있어 속도로 환산하지 않는다.
        public void MovePatrolSocket(Vector2 point,float dt)
        {
            if (!GrappleRules.Finite(point.x) || !GrappleRules.Finite(point.y)) return;
            PatrolVelocity=hasPatrolSample && dt>0 && GrappleRules.Finite(dt) ? (point-Point)/dt : Vector2.zero;
            transform.position=point;
            PatrolDriven=hasPatrolSample=true;
        }
        public bool Available=>isActiveAndEnabled&&operational&&!leased&&!spent;
        public bool Operational=>operational;
        public void SetOperational(bool value){if(leased||operational==value)return;operational=value;Refresh();}
        public bool Leased=>leased;
        public bool Spent=>spent;
        protected virtual bool Consumable=>false;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ClearRegistry(){registry.Clear();}
        protected virtual void OnEnable()
        {
            if(!registry.Contains(this))registry.Add(this);
            if(!data)data=Resources.Load<GrappleAnchorData>("Traversal/DefaultAnchor");
            if(!ring)
            {
                ring=gameObject.AddComponent<LineRenderer>();ring.useWorldSpace=false;ring.loop=true;ring.positionCount=16;ring.widthMultiplier=.07f;ring.sortingOrder=16;
                material=new Material(Shader.Find("Sprites/Default"));ring.sharedMaterial=material;
                float radius=data?data.radius:.38f;
                for(int i=0;i<16;i++){float a=i*Mathf.PI*2/16;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}
            }
            Refresh();
        }
        protected virtual void OnDisable(){registry.Remove(this);leased=false;hasPatrolSample=false;PatrolVelocity=Vector2.zero;}
        protected virtual void OnDestroy(){if(material)Destroy(material);}
        public bool TryAcquire()
        {if(!Available)return false;leased=true;Refresh();return true;}
        public void Release(bool arrived)
        {if(!leased)return;leased=false;if(arrived&&Consumable)spent=true;Refresh();}
        public virtual void ResetForRetry(){operational=true;leased=spent=selected=false;Refresh();}
        public void Highlight(bool value){if(selected==value)return;selected=value;Refresh();}
        void Refresh()
        {if(!ring)return;ring.enabled=!spent;Color c=selected?Color.white:data?data.color:Color.cyan;if(!operational)c.a=.15f;ring.startColor=ring.endColor=c;ring.widthMultiplier=selected?.12f:.07f;}
    }
}
