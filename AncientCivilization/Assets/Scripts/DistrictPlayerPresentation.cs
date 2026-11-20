using UnityEngine;
using Plugin.AncientCivilization;
namespace Plugin.PlayerCore
{
    // 기존 원화의 발 피벗을 캡슐 아래에 맞춘다. 판정 크기는 애니메이션과 분리한다.
    public sealed class DistrictPlayerPresentation : MonoBehaviour
    {
        PlayerStateMachine player;PlayerInputReader input;PlayerModeController mode;
        SpriteRenderer body;
        public int BodyFrame { get; private set; }
        readonly AncientLocomotionMotion gait = new AncientLocomotionMotion();
        float naturalSize, lean, stretchX = 1, stretchY = 1;
        void Awake()
        {
            player=GetComponent<PlayerStateMachine>();input=GetComponent<PlayerInputReader>();mode=GetComponent<PlayerModeController>();
            body=AncientArt.Display("백 경위",AncientArt.Hero(0),Vector2.zero,12,transform);
            body.transform.localPosition=new Vector3(0,-.9f,0);
            float height=body.sprite.bounds.size.y;naturalSize=1.95f/Mathf.Max(.1f,height);body.transform.localScale=Vector3.one*naturalSize;
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
            BodyFrame = frame;
            body.sprite = AncientArt.Hero(frame);
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
