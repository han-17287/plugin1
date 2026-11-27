using System;

namespace Plugin.PlayerCore
{
    [Flags]
    public enum BluePlugSkill
    {
        None = 0,
        Tow = 1,
        Swing = 2
    }

    public enum BluePlugPhase
    {
        Ready,
        Winding,
        Flying,
        Connected,
        Towing,
        Swinging,
        Retracting
    }

    public static class BluePlugSkillKeys
    {
        public const string Tow = "living.bluePlug.tow";
        public const string Swing = "living.bluePlug.swing";
    }

    // 연결과 이동을 분리한다. 장착 순간까지의 입력은 이후 이동에 재사용하지 않는다.
    public sealed class BluePlugSession
    {
        long dockSequence;
        public BluePlugPhase Phase { get; private set; }

        public bool BeginWindup()
        {
            if (Phase != BluePlugPhase.Ready) return false;
            Phase = BluePlugPhase.Winding;
            return true;
        }

        public bool Launch()
        {
            if (Phase != BluePlugPhase.Winding) return false;
            Phase = BluePlugPhase.Flying;
            return true;
        }

        public bool Dock(long inputSequence)
        {
            if (Phase != BluePlugPhase.Flying) return false;
            dockSequence = inputSequence;
            Phase = BluePlugPhase.Connected;
            return true;
        }

        public bool BeginRetract()
        {
            if (Phase != BluePlugPhase.Winding && Phase != BluePlugPhase.Flying && Phase != BluePlugPhase.Connected)
                return false;
            dockSequence = 0;
            Phase = BluePlugPhase.Retracting;
            return true;
        }

        public bool Activate(BluePlugSkill requested, BluePlugSkill unlocked, BluePlugSkill supported,
            bool clear, long inputSequence)
        {
            if (Phase != BluePlugPhase.Connected || inputSequence <= dockSequence || !clear) return false;
            if (requested != BluePlugSkill.Tow && requested != BluePlugSkill.Swing) return false;
            if ((unlocked & supported & requested) == BluePlugSkill.None) return false;
            Phase = requested == BluePlugSkill.Tow ? BluePlugPhase.Towing : BluePlugPhase.Swinging;
            return true;
        }

        public void Clear()
        {
            Phase = BluePlugPhase.Ready;
            dockSequence = 0;
        }
    }

    public static class BluePlugRules
    {
        // 떼는 순간의 충전이 사거리와 비행 속도를 정한다. 릴리즈 이후에는 충전량이 늘지 않는다.
        public static float WindCharge(float heldSeconds, float fullChargeSeconds) =>
            GrappleRules.Finite(heldSeconds) && GrappleRules.Finite(fullChargeSeconds) && fullChargeSeconds > 0
                ? Math.Clamp(heldSeconds / fullChargeSeconds, 0, 1) : 0;

        public static float ChargedValue(float baseline, float fullMultiplier, float charge) =>
            baseline * (1 + (Math.Max(1, fullMultiplier) - 1) * Math.Clamp(charge, 0, 1));

        public static float SwingSpeed(float current, float tangentY, float axis, float gravity, float pump,
            float limit, float dt)
        {
            if (!GrappleRules.Finite(current) || !GrappleRules.Finite(tangentY) || !GrappleRules.Finite(axis) ||
                !GrappleRules.Finite(gravity) || !GrappleRules.Finite(pump) || !GrappleRules.Finite(limit) ||
                !GrappleRules.Finite(dt) || limit <= 0 || gravity < 0 || pump < 0 || dt < 0) return 0;

            // 중력과 조작 가속도를 호의 접선에 투영해 속도의 크기와 부호를 유지한다.
            float alongRope = -gravity * Math.Clamp(tangentY, -1f, 1f) + pump * Math.Clamp(axis, -1f, 1f);
            return Math.Clamp(current + alongRope * dt, -limit, limit);
        }

        public static float SwingAngleStep(float tangentSpeed, float radius, float dt)
        {
            if (!GrappleRules.Finite(tangentSpeed) || !GrappleRules.Finite(radius) ||
                !GrappleRules.Finite(dt) || radius <= 0 || dt <= 0) return 0;
            float step = tangentSpeed * dt / radius;
            return GrappleRules.Finite(step) ? step : 0;
        }
    }
}
