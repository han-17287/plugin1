using UnityEngine;
using Plugin.AncientCivilization;
namespace Plugin.PlayerCore
{
    // 판정 크기는 애니메이션과 분리하고, 이 자세를 기준으로 PlayerBluePlug가 전선을 그린다.
    [DefaultExecutionOrder(30)]
    public sealed class DistrictPlayerPresentation : MonoBehaviour
    {
        PlayerStateMachine player;PlayerInputReader input;PlayerModeController mode;
        SpriteRenderer body;
        PlayerBluePlug plug;
        PlayerSummonLoadout loadout;
        FilamentStrikeFX bladeFx;
        SummonedBlade blade;
        BluePlugCharacterMotion plugMotion;
        public int BodyFrame { get; private set; }
        readonly AncientLocomotionMotion gait = new AncientLocomotionMotion();
        float naturalSize, lean, stretchX = 1, stretchY = 1;
        void Awake()
        {
            player=GetComponent<PlayerStateMachine>();input=GetComponent<PlayerInputReader>();mode=GetComponent<PlayerModeController>();
            plug = GetComponent<PlayerBluePlug>();
            body=AncientArt.Display("백 경위",plug ? BluePlugOfficerArt.Frame(0) : AncientArt.Hero(0),Vector2.zero,12,transform);
            body.transform.localPosition=new Vector3(0,-.9f,0);
            float height=body.sprite.bounds.size.y;naturalSize=1.95f/Mathf.Max(.1f,height);body.transform.localScale=Vector3.one*naturalSize;
            // 순찰 방을 열 때 원화의 48개 영역을 준비해 첫 베기 프레임에 텍스처를 올리지 않는다.
            for (int step=1; step<=AncientBladeRules.StepCount; step++)
                for (int frame=0; frame<AncientBladeRules.FramesPerStep; frame++) AncientArt.FieldBlade(step,frame);
            if (plug)
            {
                plugMotion = new BluePlugCharacterMotion(body, naturalSize);
                if (TryGetComponent<BluePlugVisuals>(out var visuals)) visuals.SetActor(body);
            }
        }

        void OnEnable()
        {
            player.Respawned += ResetPose;
            player.JumpLaunched += InterruptReleasePose;
            player.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            if (player)
            {
                player.Respawned -= ResetPose;
                player.JumpLaunched -= InterruptReleasePose;
                player.StateChanged -= OnStateChanged;
            }
            ResetPose();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) ResetPose();
        }

        void InterruptReleasePose() => plugMotion?.InterruptRecovery();

        void OnStateChanged(PlayerStateId previous, PlayerStateId next)
        {
            if (next == PlayerStateId.Hurt || next == PlayerStateId.Dead) ResetPose();
        }

        void ClearPlugPose()
        {
            plugMotion?.Clear();
            if (plug) plug.SetVisualGrips(null);
        }

        void ResetPose()
        {
            ClearPlugPose();
            gait.Clear();
            lean = 0;
            stretchX = stretchY = 1;
            if (body) ApplyBasePose(0, player ? player.Facing : 1);
        }

        void LateUpdate()
        {
            if(!player || !body || !input || input.Paused || !Application.isFocused) return;
            float dt = Time.deltaTime;
            Vector2 velocity = player.Velocity;
            float facing = player.Facing;
            gait.Step(player.Grounded, velocity.x, velocity.y, facing, dt);
            int frame = gait.Frame;
            float angle = gait.Angle, sx = gait.ScaleX, sy = gait.ScaleY;
            switch (player.State)
            {
                case PlayerStateId.Dash:
                    frame = 2; angle = -facing * 12; sx = 1.12f; sy = .89f;
                    break;
                case PlayerStateId.GrappleAim:
                case PlayerStateId.GrappleDash:
                    frame = 5; angle = -Mathf.Clamp(velocity.x / 36f, -1, 1) * 17;
                    sx = 1.06f; sy = 1.04f;
                    if (Mathf.Abs(velocity.x) > .5f) facing = Mathf.Sign(velocity.x);
                    break;
                case PlayerStateId.WallRest:
                    frame = 4; angle = facing * 3; sx = .95f; sy = .97f;
                    break;
                case PlayerStateId.WallSlide:
                    frame = 4; angle = facing * 6; sx = .96f; sy = 1.04f;
                    break;
                case PlayerStateId.WallJump:
                    frame = 1; angle = -facing * 11; sx = .97f; sy = 1.07f;
                    break;
                case PlayerStateId.Hurt:
                    frame = 4; angle = facing * 10; sx = 1.08f; sy = .91f;
                    break;
                case PlayerStateId.Dead:
                    frame = 4; angle = facing * 22; sx = 1.07f; sy = .85f;
                    break;
            }
            // 자세 보간은 시각 자식에만 적용하므로 벽/가시 판정과 실제 돌진 경로에 영향이 없다.
            float blend = 1 - Mathf.Exp(-28 * dt);
            lean = Mathf.Lerp(lean, angle, blend);
            stretchX = Mathf.Lerp(stretchX, sx, blend);
            stretchY = Mathf.Lerp(stretchY, sy, blend);
            ApplyBasePose(frame, facing);

            if (!loadout) loadout = GetComponent<PlayerSummonLoadout>();
            if (loadout && loadout.AnimatingCut)
            {
                ClearPlugPose();
                int step=loadout.CutStep, cutFrame=loadout.CutFrame;
                Vector2 aim=loadout.CutDirection;
                AncientBladeMotion.Paint(body,step,cutFrame,loadout.CutElapsed,aim,loadout.CutFacing,
                    player.Grounded,velocity,new Vector2(0,-.9f),naturalSize);
                if (!bladeFx) bladeFx=GetComponent<FilamentStrikeFX>();
                if (!blade) blade=GetComponent<SummonedBlade>();
                if (blade && blade.Equipped)
                    blade.SetEquippedPose(AncientBladeMotion.World(body,AncientBladeAtlas.ReelLocal(step,cutFrame)),
                        AncientBladeMotion.World(body,AncientBladeAtlas.HiltLocal(step,cutFrame)),aim,false);
                if (loadout.Cutting) bladeFx.BladeSweep(body,step,cutFrame,player.data.solidLayers);
                else bladeFx.HideSweep();
                return;
            }
            if (bladeFx) bladeFx.HideSweep();
            if (!plug || plugMotion == null) return;
            if (!plug.isActiveAndEnabled)
            {
                ClearPlugPose();
                return;
            }

            var state = player.State;
            var phase = plug.Phase;
            bool throwing = phase == BluePlugPhase.Winding || phase == BluePlugPhase.Flying;
            // 우클릭 수집/빈손 F를 누르고 있어도 실제 벽 고정이나 회수 자세가 끊기지 않는다.
            if (state == PlayerStateId.Dash || (state >= PlayerStateId.Attack && state != PlayerStateId.WallRest))
            {
                plugMotion.Clear();
                if (phase != BluePlugPhase.Ready) plugMotion.PresentPassive();
            }
            else if (plugMotion.Apply(phase, plug.VisualTargetDelta, velocity, facing, player.Grounded, dt, gait,
                plug.WallGripPoint, plug.VisualAimDirection))
            {
                if (throwing) BodyFrame = 6;
                else if (plug.Moving) BodyFrame = 5;
            }

            // 실행 순서 40의 플러그 렌더러가 이 프레임의 자세를 그대로 사용한다.
            plug.SetVisualGrips(phase == BluePlugPhase.Ready ? null : (Vector2?)plugMotion.LeftGripWorld,
                phase == BluePlugPhase.Winding ? plugMotion.RightGripWorld : (Vector2?)null);
        }

        void ApplyBasePose(int frame, float facing)
        {
            BodyFrame = frame;
            body.sprite = plug ? BluePlugOfficerArt.Frame(frame) : AncientArt.Hero(frame);
            body.flipX = facing < 0;
            body.transform.localRotation = Quaternion.Euler(0, 0, lean);
            body.transform.localScale = new Vector3(naturalSize * stretchX, naturalSize * stretchY, 1);
            body.transform.localPosition = new Vector3(0, -.9f + gait.OffsetY, 0);
        }
        void OnGUI()
        {
            if(!input||!input.Ready||!mode)return;
            Vector2 p=input.Pointer;p.y=Screen.height-p.y;Color old=GUI.color;GUI.color=mode.ModeColor;
            GUI.DrawTexture(new Rect(p.x-6,p.y-1,12,2),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(p.x-1,p.y-6,2,12),Texture2D.whiteTexture);GUI.color=old;
        }
    }
}
