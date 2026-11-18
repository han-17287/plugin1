using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace Plugin.PlayerCore
{
    [RequireComponent(typeof(PlayerModeController))]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public PlayerMovementData data;
        public readonly PlayerInputBuffer Buffer=new PlayerInputBuffer();
        readonly ScrollModeGate scroll=new ScrollModeGate();PlayerModeController mode;
        public Vector2 Move {get;private set;} public Vector2 Pointer {get;private set;}
        public bool JumpHeld {get;private set;} public bool CollectHeld {get;private set;}
        public bool LaserHeld {get;private set;} bool pauseRequested;
        public bool InterfaceOpen {get;private set;}
        public bool Paused=>pauseRequested||InterfaceOpen;
        public void SetInterfaceOpen(bool value){InterfaceOpen=value;ClearTransient();}
        public void Resume(){pauseRequested=false;InterfaceOpen=false;ClearTransient();}
        public bool Ready {get;private set;}
#if ENABLE_INPUT_SYSTEM
        InputActionAsset asset;InputActionMap map;
        InputAction move,jump,dash,drop,toggle,primary,collect,laser,throwBlade,explode,pause,point,wheel;
#endif
        void Awake(){mode=GetComponent<PlayerModeController>();}
        void OnEnable()
        {
            mode.Changed+=OnModeChanged;
#if ENABLE_INPUT_SYSTEM
            var source=Resources.Load<InputActionAsset>("PlayerCore/P1Player");
            if(!source){Debug.LogError("P1Player.inputactions를 찾을 수 없습니다.",this);enabled=false;return;}
            asset=Instantiate(source);map=asset.FindActionMap("Gameplay",true);
            move=map.FindAction("Move",true);jump=map.FindAction("Jump",true);dash=map.FindAction("Dash",true);drop=map.FindAction("Drop",true);
            toggle=map.FindAction("ModeToggle",true);primary=map.FindAction("Primary",true);collect=map.FindAction("Collect",true);laser=map.FindAction("Laser",true);
            throwBlade=map.FindAction("Throw",true);explode=map.FindAction("Explode",true);pause=map.FindAction("Pause",true);point=map.FindAction("Point",true);wheel=map.FindAction("ModeScroll",true);
            map.Enable();Ready=true;
#else
            Debug.LogError("신 Input System 설치 후 Active Input Handling을 Both 또는 Input System으로 설정하세요.",this);
#endif
        }
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if(!Ready)return;
            if(pause.WasPressedThisFrame()){pauseRequested=!pauseRequested;ClearTransient();}
            if(Paused||!Application.isFocused)return;
            double now=Time.timeAsDouble;float life=data?data.inputBuffer:.15f;
            Move=Vector2.ClampMagnitude(move.ReadValue<Vector2>(),1);Pointer=point.ReadValue<Vector2>();
            JumpHeld=jump.IsPressed();CollectHeld=collect.IsPressed();LaserHeld=laser.IsPressed();
            if(toggle.WasPressedThisFrame())ChangeMode(0);
            int direction=scroll.Read(wheel.ReadValue<float>(),Time.unscaledTimeAsDouble,data?data.scrollThreshold:.1f,data?data.scrollCooldown:.16f);
            if(direction!=0)ChangeMode(direction);
            // S+Space가 점프보다 우선이며, 보류 중인 점프도 제거한다.
            bool dropping=drop.WasPressedThisFrame()||jump.WasPressedThisFrame()&&Move.y<-.5f;
            if(dropping){Buffer.Remove(PlayerCommand.Jump);Buffer.Put(PlayerCommand.Drop,now,life);}
            else if(jump.WasPressedThisFrame())Buffer.Put(PlayerCommand.Jump,now,life);
            Put(dash,PlayerCommand.Dash,now,life);Put(primary,PlayerCommand.Primary,now,life);
            Put(throwBlade,PlayerCommand.Throw,now,life);Put(explode,PlayerCommand.Explode,now,life);Put(laser,PlayerCommand.LaserPress,now,life);
            if(laser.WasReleasedThisFrame())Buffer.Put(PlayerCommand.LaserRelease,now,life);
#endif
        }
#if ENABLE_INPUT_SYSTEM
        void Put(InputAction action,PlayerCommand command,double now,float life){if(action.WasPressedThisFrame())Buffer.Put(command,now,life);}
#endif
        void ChangeMode(int direction)
        {
            if(direction==0)mode.Toggle();else mode.Set(direction>0?PlayerMode.Attack:PlayerMode.Hook);
        }
        void OnModeChanged(PlayerMode changed)
        {
            // 이전 모드에서 누른 공격은 새 모드의 갈고리로 바뀌어 실행되지 않는다.
            Buffer.Remove(PlayerCommand.Primary);Buffer.Remove(PlayerCommand.LaserPress);Buffer.Remove(PlayerCommand.LaserRelease);
        }
        public void ClearTransient(){Buffer.Clear();Move=Vector2.zero;JumpHeld=CollectHeld=LaserHeld=false;scroll.Reset();}
        void OnApplicationFocus(bool focused){if(!focused)ClearTransient();}
        void OnDisable()
        {
            mode.Changed-=OnModeChanged;Ready=false;ClearTransient();
#if ENABLE_INPUT_SYSTEM
            map?.Disable();if(asset)Destroy(asset);asset=null;
#endif
        }
    }
}
