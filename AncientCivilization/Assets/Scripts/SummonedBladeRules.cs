using System;

namespace Plugin.PlayerCore
{
    public enum SummonedBladeState { None, Summoning, Falling, Stuck, Equipped, Cooldown }
    public enum SummonedBladeHit { Impact, Field, Zip, Overload }

    [Serializable]
    public struct SummonedBladeCarry
    {
        public bool equipped;
        public float cooldown;
    }

    // 타이머가 음수로 넘어가거나 입력이 두 번 들어와도 한 번만 상태를 넘긴다.
    public sealed class SummonedBladeRules
    {
        public SummonedBladeState State { get; private set; }
        public float Remaining { get; private set; }
        public float FieldRemaining { get; private set; }
        public int ReservedParticles { get; private set; }
        float pulse;
        public bool Catchable => State == SummonedBladeState.Stuck || State == SummonedBladeState.Falling;
        public bool Equipped => State == SummonedBladeState.Equipped;

        // 장착한 장비와 남은 재생 시간만 넘긴다. 설치물은 떠나는 방에 남긴다.
        public SummonedBladeCarry Snapshot(float installedCooldown)
        {
            return new SummonedBladeCarry {
                equipped = Equipped,
                cooldown = State == SummonedBladeState.Stuck ? Math.Max(0, installedCooldown)
                    : State == SummonedBladeState.Cooldown ? Remaining : 0
            };
        }

        public void EnterRoom(SummonedBladeCarry carry)
        {
            if (State != SummonedBladeState.None) return;
            if (carry.equipped) State = SummonedBladeState.Equipped;
            else if (carry.cooldown > 0 && !float.IsInfinity(carry.cooldown)) Break(carry.cooldown);
        }

        public bool Begin(float warning, int particlesSpent = 0)
        {
            if (State != SummonedBladeState.None) return false;
            State = SummonedBladeState.Summoning;
            Remaining = Math.Max(.05f, warning);
            ReservedParticles = Math.Max(0, particlesSpent);
            return true;
        }

        public int RefundReservation()
        {
            int amount = ReservedParticles;
            ReservedParticles = 0;
            return amount;
        }

        public void Retarget(float warning)
        {
            if (State != SummonedBladeState.Summoning && State != SummonedBladeState.Falling) return;
            State = SummonedBladeState.Summoning;
            Remaining = Math.Max(.05f, warning);
        }

        public bool AdvanceWarning(float elapsed)
        {
            if (State != SummonedBladeState.Summoning) return false;
            Remaining = Math.Max(0, Remaining - Math.Max(0, elapsed));
            if (Remaining > .00001f) return false;
            State = SummonedBladeState.Falling;
            return true;
        }

        public bool Land(float lifetime)
        {
            if (State != SummonedBladeState.Falling) return false;
            ReservedParticles = 0;
            Install(lifetime, 0);
            return true;
        }

        public bool Equip()
        {
            if (!Catchable) return false;
            ReservedParticles = 0;
            State = SummonedBladeState.Equipped;
            Remaining = FieldRemaining = pulse = 0;
            return true;
        }

        public bool Detach(float lifetime, float fieldTime)
        {
            if (!Equipped) return false;
            Install(lifetime, fieldTime);
            return true;
        }

        void Install(float lifetime, float fieldTime)
        {
            State = SummonedBladeState.Stuck;
            Remaining = Math.Max(0, lifetime);
            FieldRemaining = Math.Min(Remaining, Math.Max(0, fieldTime));
            pulse = 0;
        }

        // 반초마다 누적된 실제 전력 시간을 피해로 환산한다. 마지막 반초 미만도 정산한다.
        public float AdvanceInstalled(float dt)
        {
            if (State != SummonedBladeState.Stuck) return 0;
            dt = Math.Max(0, dt);
            float active = Math.Min(FieldRemaining, dt);
            FieldRemaining = Math.Max(0, FieldRemaining - dt);
            Remaining = Math.Max(0, Remaining - dt);
            pulse += active;
            if (pulse < .5f && FieldRemaining > 0) return 0;
            float result = pulse;
            pulse = 0;
            return result;
        }

        public void Break(float cooldown)
        {
            State = SummonedBladeState.Cooldown;
            ReservedParticles = 0;
            Remaining = Math.Max(0, cooldown);
            FieldRemaining = pulse = 0;
        }

        public void AdvanceCooldown(float dt)
        {
            if (State != SummonedBladeState.Cooldown) return;
            Remaining = Math.Max(0, Remaining - Math.Max(0, dt));
            if (Remaining <= .00001f) Reset();
        }

        public void Reset()
        {
            State = SummonedBladeState.None;
            ReservedParticles = 0;
            Remaining = FieldRemaining = pulse = 0;
        }

        public static float Hit(float baseline, float perUnit, float units, float maximum) =>
            Math.Min(Math.Max(0, maximum), Math.Max(0, baseline) + Math.Max(0, units) * Math.Max(0, perUnit));
    }
}
