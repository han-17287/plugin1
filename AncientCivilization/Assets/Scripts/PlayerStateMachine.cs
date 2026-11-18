using System;
using System.Collections.Generic;
using UnityEngine;
namespace Plugin.PlayerCore
{
    public enum PlayerStateId {Idle,Run,Jump,Fall,WallSlide,WallJump,Dash,GrappleAim,GrappleDash,Attack,Throw,LaserCharge,LaserFire,Hurt,Dead}
    public interface IPlayerState {PlayerStateId Id {get;} void Enter();void Tick(float dt);void Exit();}
    [RequireComponent(typeof(Rigidbody2D),typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(PlayerInputReader),typeof(PlayerModeController))]
    public sealed class PlayerStateMachine : MonoBehaviour
    {
        public PlayerMovementData data;
        readonly Dictionary<PlayerStateId,IPlayerState> states=new Dictionary<PlayerStateId,IPlayerState>();
        readonly RaycastHit2D[] hits=new RaycastHit2D[24];readonly ContactPoint2D[] contacts=new ContactPoint2D[24];
        readonly List<Collider2D> ignoredPlatforms=new List<Collider2D>();
        Rigidbody2D body;CapsuleCollider2D shape;PlayerInputReader input;PlayerModeController mode;
        ContactFilter2D filter;PhysicsMaterial2D material;IPlayerState state;Vector2 velocity;
        float environmentalLift,liftUntil;float momentumUntil;float clock,groundAt=-100,jumpedAt=-100,lockUntil,dashUntil,dashReady,dropUntil,apexAge,facing=1;
        int dashes;bool arrivalJump;bool cutJump,wallLeft,wallRight,wasGrounded,paused,lastSimulation=true;float hitUntil;
        public PlayerStateId State=>state==null?PlayerStateId.Idle:state.Id;
        public Vector2 Velocity=>body?body.linearVelocity:Vector2.zero;
        public bool Grounded {get;private set;} public float Facing=>facing;
        public int RemainingDashes=>dashes;
        public bool ArrivalJumpAvailable=>arrivalJump;
        public PlayerMode Mode=>mode.Mode;
        // P2~P6 서비스는 성공적으로 행동을 시작했을 때만 true를 반환한다.
        public event Func<PlayerCommand,PlayerStateId,bool> ActionRequested;
        public event Action<float> ExternalStep;
        public event Action<PlayerStateId,PlayerStateId> StateChanged;
        public void Configure(PlayerMovementData value){data=value;input=GetComponent<PlayerInputReader>();input.data=value;}
        void Awake()
        {
            body=GetComponent<Rigidbody2D>();shape=GetComponent<CapsuleCollider2D>();input=GetComponent<PlayerInputReader>();mode=GetComponent<PlayerModeController>();
            if(!data)data=Resources.Load<PlayerMovementData>("PlayerCore/DefaultMovement");
            if(!data){data=ScriptableObject.CreateInstance<PlayerMovementData>();ownedData=true;}input.data=data;
            body.freezeRotation=true;body.gravityScale=0;body.interpolation=RigidbodyInterpolation2D.Interpolate;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            material=new PhysicsMaterial2D("P1 마찰 없는 몸체"){friction=0,bounciness=0};shape.sharedMaterial=material;
            filter=new ContactFilter2D();filter.SetLayerMask(data.solidLayers);filter.useTriggers=false;
            Add(new LocomotionState(this,PlayerStateId.Idle));Add(new LocomotionState(this,PlayerStateId.Run));
            Add(new AirState(this,PlayerStateId.Jump));Add(new AirState(this,PlayerStateId.Fall));Add(new WallState(this,false));Add(new WallState(this,true));Add(new DashState(this));
            foreach(var id in new[]{PlayerStateId.GrappleAim,PlayerStateId.GrappleDash,PlayerStateId.Attack,PlayerStateId.Throw,PlayerStateId.LaserCharge,PlayerStateId.LaserFire,PlayerStateId.Hurt,PlayerStateId.Dead})Add(new ExternalState(this,id));
            dashes=data.airDashes;Change(PlayerStateId.Idle);
        }
        bool ownedData;
        void Add(IPlayerState value){states.Add(value.Id,value);}
        void Change(PlayerStateId next){if(state!=null&&state.Id==next)return;var previous=State;state?.Exit();state=states[next];state.Enter();StateChanged?.Invoke(previous,next);}
        public bool BeginExternal(PlayerStateId next)
        {if(State==PlayerStateId.Dead||State==PlayerStateId.Hurt||next<PlayerStateId.GrappleAim||next>PlayerStateId.LaserFire)return false;Change(next);return true;}
        public void FinishExternal(Vector2 launch,bool resetAirActions)
        {if(State==PlayerStateId.Dead||State==PlayerStateId.Hurt)return;if(resetAirActions){ResetAirActions();arrivalJump=true;}velocity=launch;body.linearVelocity=launch;cutJump=true;groundAt=-100;momentumUntil=clock+.18f;Change(launch.y>0?PlayerStateId.Jump:PlayerStateId.Fall);}
        // 외부 상태의 속도는 물리 틱의 최종 쓰기에도 보존되어야 한다.
        public void SetExternalVelocity(Vector2 value)
        {if(State<PlayerStateId.GrappleAim||State>PlayerStateId.LaserFire)return;velocity=value;body.linearVelocity=value;}
        public bool TryExternalDash()
        {if(State!=PlayerStateId.GrappleAim&&State!=PlayerStateId.GrappleDash||dashes<=0||clock<dashReady)return false;Change(PlayerStateId.Dash);return true;}
        // 환기류도 같은 속도 소유자에 전달해 입력 해제가 상승을 반감시키지 않게 한다.
        public void ApplyEnvironmentalLift(float minimum)
        {if(State>PlayerStateId.WallJump||!GrappleRules.Finite(minimum)||minimum<=0)return;environmentalLift=Mathf.Max(environmentalLift,minimum);liftUntil=clock+Time.fixedDeltaTime*1.5f;}
        public void ResetAirActions(){arrivalJump=false;dashes=data.airDashes;cutJump=false;dashReady=clock;}
        public void ReceiveHurt(Vector2 knockback,float duration)
        {if(State==PlayerStateId.Dead)return;environmentalLift=0;arrivalJump=false;input.Buffer.Clear();velocity=knockback;body.linearVelocity=velocity;hitUntil=clock+Mathf.Max(.01f,duration);Change(PlayerStateId.Hurt);}
        public void Die(){environmentalLift=0;input.ClearTransient();velocity=Vector2.zero;body.linearVelocity=Vector2.zero;Change(PlayerStateId.Dead);}
        public void Respawn(Vector2 point)
        {environmentalLift=0;RestorePlatforms();input.ClearTransient();body.position=point;velocity=Vector2.zero;body.linearVelocity=velocity;groundAt=-100;jumpedAt=-100;lockUntil=clock;momentumUntil=clock;apexAge=0;ResetAirActions();Change(PlayerStateId.Fall);}
        void FixedUpdate()
        {
            bool freeze=input.Paused||!Application.isFocused||!input.Ready;
            if(freeze!=paused){paused=freeze;if(freeze){lastSimulation=body.simulated;body.simulated=false;}else body.simulated=lastSimulation;}
            if(freeze||!input.Ready)return;
            float dt=Time.fixedDeltaTime;clock+=dt;velocity=body.linearVelocity;
            filter.SetLayerMask(data.solidLayers);Probe();
            if(ignoredPlatforms.Count>0&&clock>=dropUntil)RestorePlatforms();
            if(Grounded&&State!=PlayerStateId.Dash){groundAt=clock;if(!wasGrounded)ResetAirActions();}
            wasGrounded=Grounded;
            if(State==PlayerStateId.Dead){body.linearVelocity=Vector2.zero;return;}
            if(State==PlayerStateId.Hurt){ApplyGravity(dt,false);if(clock>=hitUntil)Change(PlayerStateId.Fall);}
            else
            {
                bool controlled=State<=PlayerStateId.Dash;
                double now=Time.timeAsDouble;
                if(controlled&&input.Buffer.Take(PlayerCommand.Drop,now))TryDrop();
                if(controlled&&dashes>0&&clock>=dashReady&&input.Buffer.Take(PlayerCommand.Dash,now))Change(PlayerStateId.Dash);
                else if(controlled&&State!=PlayerStateId.Dash&&input.Buffer.Has(PlayerCommand.Jump,now))TryJump(now);
                if(State!=PlayerStateId.Dash&&clock>=lockUntil&&Mathf.Abs(input.Move.x)>.05f)facing=Mathf.Sign(input.Move.x);
                RouteActions(now);state.Tick(dt);
            }
            if(clock>liftUntil)environmentalLift=0;
            if(environmentalLift>0&&State<=PlayerStateId.WallJump)
            {
                velocity.y=Mathf.Max(velocity.y,environmentalLift);cutJump=true;
                if(Grounded){Grounded=false;groundAt=-100;}if(State!=PlayerStateId.WallJump)Change(PlayerStateId.Jump);
            }
            if(State==PlayerStateId.Dash)SweepDash(dt);
            body.linearVelocity=velocity;
        }
        void Probe()
        {
            Grounded=false;wallLeft=wallRight=false;
            int count=body.Cast(Vector2.down,filter,hits,data.groundProbe);
            for(int i=0;i<count;i++)if(!Ignored(hits[i].collider)&&hits[i].normal.y>=data.groundNormal&&velocity.y<=.5f&&clock-jumpedAt>.06f){Grounded=true;break;}
            count=body.Cast(Vector2.left,filter,hits,data.wallProbe);
            for(int i=0;i<count;i++)if(!Ignored(hits[i].collider)&&hits[i].normal.x>.7f)wallLeft=true;
            count=body.Cast(Vector2.right,filter,hits,data.wallProbe);
            for(int i=0;i<count;i++)if(!Ignored(hits[i].collider)&&hits[i].normal.x<-.7f)wallRight=true;
        }
        bool Ignored(Collider2D collider){return !collider||ignoredPlatforms.Contains(collider);}
        void TryJump(double now)
        {
            if(State==PlayerStateId.WallJump&&clock<lockUntil)return;
            if(!Grounded&&(wallLeft||wallRight))
            {input.Buffer.Take(PlayerCommand.Jump,now);velocity=new Vector2(wallLeft?data.wallJumpX:-data.wallJumpX,data.wallJumpY);facing=Mathf.Sign(velocity.x);lockUntil=clock+data.wallInputLock;JumpStarted();Change(PlayerStateId.WallJump);}
            else if(Grounded||clock-groundAt<=data.coyoteTime||arrivalJump)
            {input.Buffer.Take(PlayerCommand.Jump,now);velocity.y=data.jumpSpeed;JumpStarted();Change(PlayerStateId.Jump);}
        }
        void JumpStarted(){arrivalJump=false;Grounded=false;groundAt=-100;jumpedAt=clock;cutJump=false;apexAge=0;}
        void Horizontal(float dt)
        {
            if(clock<lockUntil)return;
            if(!Grounded&&clock<momentumUntil&&Mathf.Abs(velocity.x)>data.runSpeed&&(Mathf.Abs(input.Move.x)<.05f||Mathf.Sign(input.Move.x)==Mathf.Sign(velocity.x)))return;
            float target=input.Move.x*data.runSpeed,error=Mathf.Abs(target-velocity.x)/Mathf.Max(1,data.runSpeed);
            bool stop=Mathf.Abs(input.Move.x)<.01f;
            float rate=Grounded?(stop?data.deceleration:data.acceleration):data.airAcceleration;
            var curve=stop?data.decelerationCurve:data.accelerationCurve;
            velocity.x=Mathf.MoveTowards(velocity.x,target,rate*Mathf.Max(.05f,curve.Evaluate(Mathf.Clamp01(error)))*dt);
        }
        void ApplyGravity(float dt,bool variableJump=true)
        {
            if(Grounded&&velocity.y<=0){velocity.y=0;return;}
            if(variableJump&&!input.JumpHeld&&!cutJump&&velocity.y>0){velocity.y*=data.jumpReleaseMultiplier;cutJump=true;}
            float multiplier=velocity.y<0?data.fallMultiplier:1;
            if(Mathf.Abs(velocity.y)<data.apexSpeed&&input.JumpHeld&&apexAge<data.apexDuration){apexAge+=dt;multiplier=data.apexGravityMultiplier;}
            velocity.y=Mathf.Max(-data.maxFallSpeed,velocity.y-data.gravity*multiplier*dt);
        }
        void ChooseMotion()
        {
            if(Grounded){Change(Mathf.Abs(input.Move.x)>.05f?PlayerStateId.Run:PlayerStateId.Idle);return;}
            if(velocity.y<=0&&(wallLeft&&input.Move.x<0||wallRight&&input.Move.x>0)){Change(PlayerStateId.WallSlide);return;}
            Change(velocity.y>0?PlayerStateId.Jump:PlayerStateId.Fall);
        }
        void SweepDash(float dt)
        {
            float distance=velocity.magnitude*dt;if(distance<.001f)return;
            int count=body.Cast(velocity.normalized,filter,hits,distance+data.skin);float allowed=distance;
            for(int i=0;i<count;i++)if(!Ignored(hits[i].collider)&&Vector2.Dot(hits[i].normal,velocity)<-.01f)allowed=Mathf.Min(allowed,Mathf.Max(0,hits[i].distance-data.skin));
            if(allowed<distance){velocity=velocity.normalized*(allowed/dt);dashUntil=clock;}
        }
        void TryDrop()
        {
            if(!Grounded)return;
            int count=body.GetContacts(filter,contacts);
            for(int i=0;i<count;i++)
            {
                var c=contacts[i];Collider2D other=c.collider==shape?c.otherCollider:c.collider;
                if(!other||ignoredPlatforms.Contains(other)||!other.TryGetComponent<PlatformEffector2D>(out var effector)||!effector.useOneWay||!other.usedByEffector)continue;
                ignoredPlatforms.Add(other);Physics2D.IgnoreCollision(shape,other,true);
            }
            if(ignoredPlatforms.Count==0)return;
            groundAt=-100;Grounded=false;dropUntil=clock+data.dropDuration;velocity.y=-3;input.Buffer.Remove(PlayerCommand.Jump);Change(PlayerStateId.Fall);
        }
        void RestorePlatforms(){foreach(var other in ignoredPlatforms)if(other&&shape)Physics2D.IgnoreCollision(shape,other,false);ignoredPlatforms.Clear();}
        void RouteActions(double now)
        {
            if(State==PlayerStateId.Hurt||State==PlayerStateId.Dead||State==PlayerStateId.Dash)return;
            foreach(var cmd in commandOrder)
            {
                if(!input.Buffer.Has(cmd,now))continue;
                PlayerStateId requested=cmd==PlayerCommand.Throw?PlayerStateId.Throw:cmd==PlayerCommand.LaserPress?PlayerStateId.LaserCharge:cmd==PlayerCommand.LaserRelease?PlayerStateId.LaserFire:cmd==PlayerCommand.Primary?(mode.Mode==PlayerMode.Hook?PlayerStateId.GrappleAim:PlayerStateId.Attack):State;
                var handlers=ActionRequested;if(handlers==null)continue;
                foreach(Func<PlayerCommand,PlayerStateId,bool> handler in handlers.GetInvocationList())
                    if(handler(cmd,requested)){input.Buffer.Take(cmd,now);return;}
            }
        }
        static readonly PlayerCommand[] commandOrder={PlayerCommand.Explode,PlayerCommand.Throw,PlayerCommand.LaserRelease,PlayerCommand.LaserPress,PlayerCommand.Primary};
        void OnDisable(){RestorePlatforms();if(paused&&body)body.simulated=lastSimulation;paused=false;}
        void OnDestroy(){if(material)Destroy(material);if(ownedData&&data)Destroy(data);}
        abstract class StateBase:IPlayerState
        {protected readonly PlayerStateMachine p;public PlayerStateId Id{get;} protected StateBase(PlayerStateMachine p,PlayerStateId id){this.p=p;Id=id;}public virtual void Enter(){}public abstract void Tick(float dt);public virtual void Exit(){}}
        sealed class LocomotionState:StateBase
        {public LocomotionState(PlayerStateMachine p,PlayerStateId id):base(p,id){}public override void Tick(float dt){p.Horizontal(dt);p.ApplyGravity(dt);p.ChooseMotion();}}
        sealed class AirState:StateBase
        {public AirState(PlayerStateMachine p,PlayerStateId id):base(p,id){}public override void Tick(float dt){p.Horizontal(dt);p.ApplyGravity(dt);p.ChooseMotion();}}
        sealed class WallState:StateBase
        {readonly bool jumping;public WallState(PlayerStateMachine p,bool jump):base(p,jump?PlayerStateId.WallJump:PlayerStateId.WallSlide){jumping=jump;}public override void Tick(float dt){p.Horizontal(dt);p.ApplyGravity(dt);if(!jumping)p.velocity.y=Mathf.Max(p.velocity.y,-p.data.wallSlideSpeed);if(!jumping||p.clock>=p.lockUntil)p.ChooseMotion();}}
        sealed class DashState:StateBase
        {
            public DashState(PlayerStateMachine p):base(p,PlayerStateId.Dash){}
            public override void Enter(){p.dashes--;p.dashUntil=p.clock+p.data.dashDuration;p.dashReady=p.dashUntil+p.data.dashCooldown;float direction=Mathf.Abs(p.input.Move.x)>.1f?Mathf.Sign(p.input.Move.x):p.facing;p.velocity=new Vector2(direction*p.data.dashSpeed,0);}
            public override void Tick(float dt){if(p.clock>=p.dashUntil){p.velocity.x=Mathf.Clamp(p.velocity.x,-p.data.runSpeed,p.data.runSpeed);if(p.Grounded)p.dashes=p.data.airDashes;p.ChooseMotion();}}
        }
        sealed class ExternalState:StateBase
        {public ExternalState(PlayerStateMachine p,PlayerStateId id):base(p,id){}public override void Tick(float dt){p.ExternalStep?.Invoke(dt);}}
    }
}
