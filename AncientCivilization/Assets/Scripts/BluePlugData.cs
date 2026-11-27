using UnityEngine;

namespace Plugin.PlayerCore
{
    [CreateAssetMenu(menuName = "PLUGIN/Player/Blue Plug Data")]
    public sealed class BluePlugData : ScriptableObject
    {
        [Header("투척")]
        [Min(.1f)] public float range = 16;
        [Min(.1f)] public float throwSpeed = 36;
        [Min(.05f)] public float aimRadius = 1.05f;
        [Min(.01f)] public float tipRadius = .10f;
        [Min(.1f)] public float retractSpeed = 52;
        [Min(0)] public float recastDelay = .12f;
        public float handHeight = .34f;
        public LayerMask solids = 1 << 8;

        [Header("전기 공격")]
        [Min(0)] public float spinDamage = 10;
        [Min(.02f)] public float spinHitInterval = .40f;
        [Min(0)] public float spinPush = .24f;
        [Min(.01f)] public float spinContactRadius = .32f;
        [Min(0)] public float throwDamage = 30;
        [Min(0)] public float throwStagger = .30f;
        [Min(0)] public float shockSeconds = 2;
        [Min(0)] public float shockDamagePerSecond = 8;
        [Min(.05f)] public float shockTick = .50f;
        [Range(0,4)] public int chainHops = 2;
        [Min(0)] public float chainDamage = 15;
        [Min(.1f)] public float chainRange = 2.7f;
        [Min(.05f)] public float chainCooldown = .6f;
        [Min(.1f)] public float dischargeRadius = 1.5f;
        [Min(0)] public float dischargeDamage = 20;
        [Min(0)] public float dischargeStagger = .25f;

        [Header("철퇴 회전")]
        [Min(.1f)] public float windRadius = .84f;
        [Min(.1f)] public float windTurnsPerSecond = 4;
        [Min(.05f)] public float fullWindSeconds = .65f;
        [Min(1)] public float fullRadiusMultiplier = 1.4f;
        [Min(1)] public float fullWindRateMultiplier = 1.65f;
        [Min(1)] public float fullRangeMultiplier = 1.25f;
        [Min(1)] public float fullThrowSpeedMultiplier = 1.6f;

        [Header("양손 모션 · 표시 전용")]
        [Min(.05f)] public float windGripRadius = .43f;
        [Range(.45f, 1.2f)] public float reelScale = .82f;
        [Min(0)] public float reelPayoutSpeed = 1200;
        [Min(0)] public float reelReturnSpeed = 920;

        [Header("콘센트 벽 고정")]
        [Min(.01f)] public float wallContactProbe = .075f;
        [Min(.1f)] public float wallGripHeight = .66f;
        [Min(.1f)] public float wallGripReach = .80f;
        [Min(.05f)] public float wallRelatchDelay = .22f;
        [Min(.1f)] public float wallCarrierLimit = 18f;

        [Header("견인")]
        [Min(.1f)] public float towSpeed = 48;
        [Min(.01f)] public float skin = .035f;
        [Min(.01f)] public float arrivalTolerance = .065f;
        [Range(0, 1)] public float towCarry = .22f;

        [Header("스윙")]
        [Min(.1f)] public float minimumRope = 2;
        [Min(.1f)] public float maximumRope = 14;
        [Min(0)] public float swingGravity = 32;
        [Min(0)] public float swingPump = 15;
        [Min(.1f)] public float swingSpeedLimit = 22;

        public float Charge(float heldSeconds) => BluePlugRules.WindCharge(heldSeconds, fullWindSeconds);
        public float WindRate(float charge) => BluePlugRules.ChargedValue(windTurnsPerSecond, fullWindRateMultiplier, charge);
        public float WindRadius(float charge, float turns) =>
            BluePlugRules.ChargedValue(windRadius, fullRadiusMultiplier, charge) * Mathf.Clamp01(turns * 3);
        public float ThrowRange(float charge, float baseline) => BluePlugRules.ChargedValue(baseline, fullRangeMultiplier, charge);
        public float ThrowSpeed(float charge, float baseline) => BluePlugRules.ChargedValue(baseline, fullThrowSpeedMultiplier, charge);
    }
}
