using UnityEngine;
namespace Plugin.PlayerCore
{
    [CreateAssetMenu(menuName="PLUGIN/Player/Movement Data")]
    public sealed class PlayerMovementData : ScriptableObject
    {
        [Header("이동과 가속 곡선")]
        [Min(0)] public float runSpeed=9,acceleration=65,deceleration=85,airAcceleration=32;
        public AnimationCurve accelerationCurve=AnimationCurve.EaseInOut(0,.35f,1,1);
        public AnimationCurve decelerationCurve=AnimationCurve.Linear(0,.45f,1,1);
        [Header("점프")]
        [Min(0)] public float jumpSpeed=14,gravity=32,fallMultiplier=1.65f,maxFallSpeed=24;
        [Range(0,1)] public float jumpReleaseMultiplier=.5f;
        [Min(0)] public float coyoteTime=.12f,inputBuffer=.15f,apexSpeed=1.5f,apexDuration=.08f;
        [Range(.05f,1)] public float apexGravityMultiplier=.45f;
        [Header("벽 동작")]
        [Min(0)] public float wallSlideSpeed=3,wallJumpX=10,wallJumpY=13,wallInputLock=.16f;
        [Header("대시")]
        [Min(1)] public int airDashes=1;
        [Min(.01f)] public float dashSpeed=22,dashDuration=.17f,dashCooldown=.18f;
        [Header("판정과 하강")]
        public LayerMask solidLayers=1<<8;
        [Min(.001f)] public float skin=.025f,groundProbe=.09f,wallProbe=.12f,dropDuration=.22f;
        [Range(0,1)] public float groundNormal=.65f;
        [Header("입력")]
        [Min(.01f)] public float scrollCooldown=.16f,scrollThreshold=.1f;
    }
}
