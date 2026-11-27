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
        PlayerModeController mode;
        public Vector2 Move {get;private set;} public Vector2 Pointer {get;private set;}
        public bool JumpHeld {get;private set;} public bool CollectHeld {get;private set;}
        public bool PrimaryHeld { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool PrimaryPressed { get; private set; }
        public bool PrimaryReleased { get; private set; }
        public bool LaserHeld {get;private set;} bool pauseRequested;
        public bool InterfaceOpen {get;private set;}
        public bool Paused=>pauseRequested||InterfaceOpen;
        public void SetInterfaceOpen(bool value){InterfaceOpen=value;ClearTransient();}
        public void Resume(){pauseRequested=false;InterfaceOpen=false;ClearTransient();}
        public bool Ready {get;private set;}
#if ENABLE_INPUT_SYSTEM
        InputActionAsset asset;InputActionMap map;
        InputAction move,jump,dash,drop,summon,primary,collect,laser,detach,overload,pause,point;
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
            summon=map.FindAction("Summon",true);primary=map.FindAction("Primary",true);collect=map.FindAction("Collect",true);laser=map.FindAction("Laser",true);
            detach=map.FindAction("Detach",true);overload=map.FindAction("Overload",true);pause=map.FindAction("Pause",true);point=map.FindAction("Point",true);
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
            PrimaryHeld = primary.IsPressed();
            PrimaryPressed = primary.WasPressedThisFrame();
            PrimaryReleased = primary.WasReleasedThisFrame();
            Put(summon,PlayerCommand.Summon,now,life);
            // S+Space가 점프보다 우선이며, 보류 중인 점프도 제거한다.
            bool dropping=drop.WasPressedThisFrame()||jump.WasPressedThisFrame()&&Move.y<-.5f;
            if(dropping){Buffer.Remove(PlayerCommand.Jump);Buffer.Put(PlayerCommand.Drop,now,life);}
            else if(jump.WasPressedThisFrame())Buffer.Put(PlayerCommand.Jump,now,life);
            Put(dash,PlayerCommand.Dash,now,life);
            if (PrimaryPressed) Buffer.Put(PlayerCommand.Primary, now, life);
            InteractPressed=detach.WasPressedThisFrame();
            Put(detach,PlayerCommand.Detach,now,life);Put(overload,PlayerCommand.Overload,now,life);Put(laser,PlayerCommand.LaserPress,now,life);
            if(laser.WasReleasedThisFrame())Buffer.Put(PlayerCommand.LaserRelease,now,life);
#endif
        }
#if ENABLE_INPUT_SYSTEM
        void Put(InputAction action,PlayerCommand command,double now,float life){if(action.WasPressedThisFrame())Buffer.Put(command,now,life);}
#endif
        void OnModeChanged(PlayerMode changed)
        {
            // 장착·분리 직전의 클릭이나 F 해제가 새 장비에서 발동되지 않게 한다.
            PrimaryPressed = PrimaryReleased = false;
            Buffer.Remove(PlayerCommand.Primary);Buffer.Remove(PlayerCommand.LaserPress);Buffer.Remove(PlayerCommand.LaserRelease);
        }
        public void ClearTransient(){Buffer.Clear();Move=Vector2.zero;JumpHeld=CollectHeld=LaserHeld=false;PrimaryHeld=PrimaryPressed=PrimaryReleased=InteractPressed=false;}
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
