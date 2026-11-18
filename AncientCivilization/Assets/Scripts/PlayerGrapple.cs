using UnityEngine;
namespace Plugin.PlayerCore
{
    [RequireComponent(typeof(PlayerStateMachine))]
    public sealed class PlayerGrapple : MonoBehaviour
    {
        public GrappleData data;
        PlayerStateMachine player;PlayerInputReader input;PlayerModeController mode;Rigidbody2D body;CapsuleCollider2D shape;Camera view;
        ContactFilter2D filter;readonly RaycastHit2D[] hits=new RaycastHit2D[24];
        GrappleAnchor candidate,leased;Vector2 target,direction,start;float delay,age,attackLeft,lastDistance;
        bool active,finishing,anchored;LineRenderer rangeLine,core,helixA,helixB;Material material;
        public bool HazardInvulnerable=>active&&player.State==PlayerStateId.GrappleDash;
        public bool Active=>active;
        public bool ZipStrikeReady=>attackLeft>0;
        public float ZipStrikeMultiplier=>ZipStrikeReady?GrappleRules.StrikeMultiplier(lastDistance,data.speed):1;
        public GrappleAnchor Candidate=>candidate;
        public bool TryConsumeZipStrike(out float multiplier)
        {multiplier=ZipStrikeMultiplier;if(!ZipStrikeReady)return false;attackLeft=0;return true;}
        void Awake()
        {
            player=GetComponent<PlayerStateMachine>();input=GetComponent<PlayerInputReader>();mode=GetComponent<PlayerModeController>();body=GetComponent<Rigidbody2D>();shape=GetComponent<CapsuleCollider2D>();
            if(!data)data=Resources.Load<GrappleData>("Traversal/DefaultGrapple");
            if(!data){Debug.LogError("DefaultGrapple 데이터가 없습니다.",this);enabled=false;return;}
            filter=new ContactFilter2D();filter.SetLayerMask(data.solids);filter.useTriggers=false;
            view=Camera.main;material=new Material(Shader.Find("Sprites/Default"));
            rangeLine=Line("갈고리 사거리",64,.015f,new Color(.2f,.8f,1,.16f));rangeLine.loop=true;
            core=Line("직선 와이어 중심",2,data.coreWidth,Color.white);
            helixA=Line("입자 나선 A",32,.025f,new Color(.15f,.65f,1));helixB=Line("입자 나선 B",32,.025f,new Color(.4f,.9f,1));
        }
        LineRenderer Line(string title,int count,float width,Color color)
        {var go=new GameObject(title);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=count;line.widthMultiplier=width;line.startColor=line.endColor=color;line.sortingOrder=18;line.enabled=false;return line;}
        void OnEnable(){player.ActionRequested+=Request;player.ExternalStep+=Step;player.StateChanged+=StateChanged;mode.Changed+=ModeChanged;}
        void OnDisable()
        {if(player){player.ActionRequested-=Request;player.ExternalStep-=Step;player.StateChanged-=StateChanged;}if(mode)mode.Changed-=ModeChanged;Cancel(true);SetCandidate(null);ShowWire(false);if(rangeLine)rangeLine.enabled=false;}
        void OnDestroy(){if(material)Destroy(material);}
        void Update()
        {
            if(!data||!input.Ready||input.Paused||!Application.isFocused)return;
            attackLeft=Mathf.Max(0,attackLeft-Time.deltaTime);
            if(!active)
            {
                bool aiming=mode.Mode==PlayerMode.Hook&&player.State!=PlayerStateId.Dead&&player.State!=PlayerStateId.Hurt;
                Vector2 point;GrappleAnchor anchor;Vector2 mouse=view?(Vector2)view.ScreenToWorldPoint(input.Pointer):body.position;
                if(aiming&&Pick(mouse,out point,out anchor))SetCandidate(anchor);else SetCandidate(null);
                rangeLine.enabled=aiming;
                if(aiming)for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;rangeLine.SetPosition(i,body.position+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*data.range);}
            }
            else rangeLine.enabled=false;
            if(active)DrawWire();
        }
        void SetCandidate(GrappleAnchor anchor){if(candidate==anchor)return;if(candidate)candidate.Highlight(false);candidate=anchor;if(candidate)candidate.Highlight(true);}
        bool Pick(Vector2 mouse,out Vector2 point,out GrappleAnchor anchor)
        {
            point=Vector2.zero;anchor=null;float best=data.assistRadius*data.assistRadius;
            foreach(var item in GrappleAnchor.Registry)
            {
                if(!item||!item.Available||!GrappleRules.Reachable(item.Point.x-body.position.x,item.Point.y-body.position.y,data.range))continue;
                float score=(item.Point-mouse).sqrMagnitude;if(score>best||Physics2D.Linecast(body.position,item.Point,data.solids))continue;
                best=score;anchor=item;
            }
            if(anchor){point=anchor.Point;return (point-body.position).sqrMagnitude>.25f;}
            Vector2 ray=(mouse-body.position).normalized;if(ray.sqrMagnitude<.5f)return false;
            var hit=Physics2D.Raycast(body.position,ray,data.range,data.solids);
            if(!hit||hit.distance<.6f)return false;
            // 모서리의 표면 점이 아니라 캡슐 전체가 설 수 있는 중심을 목표로 삼는다.
            Vector2 half=shape.bounds.extents;
            float extent=Mathf.Abs(hit.normal.x)*half.x+Mathf.Abs(hit.normal.y)*half.y;
            point=hit.point+hit.normal*(extent+data.skin);
            return (point-body.position).sqrMagnitude>.25f&&GrappleRules.Reachable(point.x-body.position.x,point.y-body.position.y,data.range);
        }
        bool Request(PlayerCommand command,PlayerStateId requested)
        {
            if(command!=PlayerCommand.Primary||requested!=PlayerStateId.GrappleAim||active||!data)return false;
            Vector2 endpoint;GrappleAnchor anchor;Vector2 mouse=view?(Vector2)view.ScreenToWorldPoint(input.Pointer):body.position;
            if(!Pick(mouse,out endpoint,out anchor))return false;
            return Begin(endpoint,anchor);
        }
        // 자동 시험/보스 서비스도 같은 검증 경로로 들어온다.
        public bool TryStartAt(GrappleAnchor anchor)
        {return anchor&&anchor.Available&&Begin(anchor.Point,anchor);}
        bool Begin(Vector2 endpoint,GrappleAnchor anchor)
        {
            if(active||!data||!input.Ready||input.Paused||mode.Mode!=PlayerMode.Hook||player.State>PlayerStateId.Dash||!GrappleRules.Reachable(endpoint.x-body.position.x,endpoint.y-body.position.y,data.range))return false;
            if((endpoint-body.position).sqrMagnitude<.25f||Physics2D.Linecast(body.position,endpoint,data.solids))return false;
            if(anchor&&!anchor.TryAcquire())return false;
            if(!player.BeginExternal(PlayerStateId.GrappleAim)){if(anchor)anchor.Release(false);return false;}
            leased=anchor;anchored=anchor;start=body.position;target=endpoint;direction=(target-start).normalized;lastDistance=Vector2.Distance(start,target);
            delay=data.launchDelay;age=0;attackLeft=0;active=true;player.SetExternalVelocity(Vector2.zero);ShowWire(true);DrawWire();return true;
        }
        void Step(float dt)
        {
            if(!active)return;
            if(player.State!=PlayerStateId.GrappleAim&&player.State!=PlayerStateId.GrappleDash){Cancel(false);return;}
            if(anchored&&(!leased||!leased.isActiveAndEnabled)){Cancel(true);return;}
            if(data.allowDashCancel&&input.Buffer.Has(PlayerCommand.Dash,Time.timeAsDouble)&&player.TryExternalDash())
            {input.Buffer.Take(PlayerCommand.Dash,Time.timeAsDouble);return;}
            age+=dt;if(age>data.range/Mathf.Max(1,data.speed)+1){Cancel(true);return;}
            if(delay>0){delay-=dt;player.SetExternalVelocity(Vector2.zero);return;}
            if(player.State==PlayerStateId.GrappleAim)player.BeginExternal(PlayerStateId.GrappleDash);
            float remaining=Vector2.Dot(target-body.position,direction);
            if(remaining<=data.arrivalTolerance){Arrive();return;}
            float travel=GrappleRules.Step(remaining,data.speed,dt),allowed=travel;
            filter.SetLayerMask(data.solids);
            int count=Physics2D.CircleCast(body.position,data.circleRadius,direction,filter,hits,travel+data.skin);
            if(count>=hits.Length){Cancel(true);return;}allowed=Allowed(count,allowed);
            count=body.Cast(direction,filter,hits,travel+data.skin);if(count>=hits.Length){Cancel(true);return;}allowed=Allowed(count,allowed);
            if(allowed<travel-.001f){Cancel(true);return;}
            player.SetExternalVelocity(direction*(travel/dt));
        }
        float Allowed(int count,float maximum)
        {for(int i=0;i<count;i++)if(hits[i].collider&&Vector2.Dot(hits[i].normal,direction)<-.001f)maximum=Mathf.Min(maximum,Mathf.Max(0,hits[i].distance-data.skin));return maximum;}
        public void ResetForRetry(){Cancel(false);attackLeft=0;SetCandidate(null);ShowWire(false);}
        void Arrive()
        {
            active=false;if(leased)leased.Release(true);leased=null;anchored=false;attackLeft=data.arrivalAttackWindow;ShowWire(false);
            finishing=true;player.FinishExternal(direction*data.speed*data.retainedVelocity,true);finishing=false;
        }
        void Cancel(bool finish)
        {
            if(!active)return;active=false;if(leased)leased.Release(false);leased=null;anchored=false;ShowWire(false);
            if(finish&&player&&(player.State==PlayerStateId.GrappleAim||player.State==PlayerStateId.GrappleDash))
            {finishing=true;player.FinishExternal(Vector2.zero,false);finishing=false;}
        }
        void StateChanged(PlayerStateId previous,PlayerStateId next)
        {if(finishing)return;if(active&&next!=PlayerStateId.GrappleAim&&next!=PlayerStateId.GrappleDash)Cancel(false);if(next==PlayerStateId.Hurt||next==PlayerStateId.Dead)attackLeft=0;}
        void ModeChanged(PlayerMode next){if(active&&data.allowModeCancel&&next!=PlayerMode.Hook)Cancel(true);}
        void ShowWire(bool shown){if(core)core.enabled=shown;if(helixA)helixA.enabled=shown;if(helixB)helixB.enabled=shown;}
        void DrawWire()
        {
            Vector2 hand=body.position+Vector2.up*.3f;core.SetPosition(0,hand);core.SetPosition(1,target);
            Vector2 normal=new Vector2(-(target-hand).y,(target-hand).x).normalized;
            for(int i=0;i<32;i++)
            {float t=i/31f,twist=Mathf.Sin(t*40-Time.time*12)*data.helixRadius*Mathf.Sin(Mathf.PI*t);Vector2 mid=Vector2.Lerp(hand,target,t);helixA.SetPosition(i,mid+normal*twist);helixB.SetPosition(i,mid-normal*twist);}
        }
    }
}
