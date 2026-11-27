using UnityEngine;
namespace Plugin.PlayerCore
{
    // 시험 방용 네 기믹. 시간은 물리 틱으로 전진하고 Pause·포커스 이탈 때 정지한다.
    public sealed class TraversalFloorMechanism : MonoBehaviour
    {
        public FloorTraversalProfile profile;public PlayerInputReader clockOwner;public bool dangerous;
        public GrappleAnchor socket;public float phase;
        PlayerStateMachine player;PlayerBluePlug plug;SpriteRenderer art;BoxCollider2D shutter;
        Vector3 origin;float age,exposure;
        public bool GateOpen {get;private set;}
        public float Exposure=>exposure;
        public void Initialize(FloorTraversalProfile data,PlayerInputReader reader,bool precision)
        {profile=data;clockOwner=reader;player=reader.GetComponent<PlayerStateMachine>();plug=reader.GetComponent<PlayerBluePlug>();art=GetComponent<SpriteRenderer>();shutter=GetComponent<BoxCollider2D>();origin=transform.localPosition;dangerous=precision;ResetForRetry();}
        public void ResetForRetry()
        {age=exposure=0;transform.localPosition=origin;if(socket)socket.SetOperational(true);if(shutter)shutter.enabled=false;GateOpen=true;}
        void FixedUpdate()
        {
            if(!profile||!clockOwner||!clockOwner.Ready||clockOwner.Paused||!Application.isFocused||!player)return;
            float dt=Time.fixedDeltaTime;
            if(profile.floor==TraversalFloor.Top&&socket&&socket.Leased)return;
            age+=dt;Vector2 p=player.GetComponent<Rigidbody2D>().position;
            switch(profile.floor)
            {
                case TraversalFloor.Living:
                    transform.localPosition=origin+Vector3.right*(Mathf.Sin(age*2*Mathf.PI/Mathf.Max(.1f,profile.scanPeriod))*2.5f);
                    bool seen=FloorTraversalLayout.ScanOverlap(p.x,p.y,transform.position.x,profile.scanWidth)&&!(plug&&plug.HazardInvulnerable);
                    exposure=seen?exposure+dt:0;
                    if(art)art.color=exposure>0?new Color(1,.45f,.15f,.7f):new Color(1,.9f,.3f,.25f);
                    if(dangerous&&exposure>=profile.scanHold){var lab=GetComponentInParent<TraversalRhythmPractice>();if(lab)lab.Retry();}
                    break;
                case TraversalFloor.Administration:
                    bool open=FloorTraversalLayout.Open(age,profile.gatePeriod,profile.gateOpen);
                    // 셔터가 플레이어 내부에서 다시 생겨 몸을 밀어내지 않도록 닫힘을 유예한다.
                    if(!open&&FloorTraversalLayout.ShutterOverlap(p.x,p.y,transform.position.x))open=true;
                    GateOpen=open;if(shutter)shutter.enabled=!open;
                    float local=age%Mathf.Max(.1f,profile.gatePeriod);
                    if(art)art.color=open?(local>=profile.gateOpen-profile.gateWarning?new Color(1,.7f,.2f,.5f):new Color(.25f,1,.65f,.15f)):new Color(1,.35f,.25f,.8f);
                    break;
                case TraversalFloor.Residence:
                    if(Mathf.Abs(p.x-transform.position.x)<1.8f&&p.y>=1.5f&&p.y<11&&player.State<=PlayerStateId.WallJump)player.ApplyEnvironmentalLift(profile.ventLift);
                    if(art)art.color=new Color(.3f,.8f,1,.22f+.08f*Mathf.Sin(age*5));
                    break;
                case TraversalFloor.Top:
                    if(socket)socket.SetOperational(FloorTraversalLayout.Open(age,profile.socketPeriod,profile.socketOpen,phase));
                    break;
            }
        }
    }
}
