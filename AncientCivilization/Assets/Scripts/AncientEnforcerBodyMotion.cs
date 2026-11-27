using System;

namespace Plugin.AncientCivilization
{
    // 발 피벗 위에서만 자세를 바꾼다. 이 시계는 공격의 발동이나 판정을 예약하지 않는다.
    internal sealed class AncientEnforcerBodyMotion
    {
        internal const float GestureSeconds = .15f;
        const float RecoilSeconds = .22f;
        bool ready, planted;
        float breathPhase, spine, chest, arm, hem, poseLean;
        public float Spine => spine;
        public float Chest => chest;
        public float Arm => arm;
        public float Hem => hem;
        float leanFacing, depth, weight, recoilAge = RecoilSeconds, recoilStrength;
        public int From { get; private set; }
        public int To { get; private set; }
        public float Blend { get; private set; } = 1;
        public int Facing => leanFacing < 0 ? -1 : 1;
        public float Depth => depth;
        public float ScaleX { get; private set; }
        public float ScaleY { get; private set; }
        public float Angle { get; private set; }
        public float OffsetX { get; private set; }
        public float OffsetY => 0;

        public void Step(float dt, int pose, int direction, float nextDepth, bool feetPlanted, float anticipation)
        {
            dt = Math.Max(0, dt);
            int sign = direction < 0 ? -1 : 1;
            planted = feetPlanted;
            if (!ready)
            {
                ready = true;
                From = To = pose;
                depth = nextDepth;
                leanFacing = sign;
            }
            // 편집 컷 대신 두 발 사이에 무게를 실어 돌아선다. 후방 원근 변화도 같은 발 피벗을 쓴다.
            depth += (nextDepth - depth) * EaseRate(dt, 16);
            leanFacing += Math.Sign(sign - leanFacing) * Math.Min(Math.Abs(sign - leanFacing), 13f * dt);
            if (To != pose)
            {
                if (pose == From && Blend < 1)
                {
                    From = To;
                    To = pose;
                    Blend = 1 - Blend;
                }
                else if (Blend >= .5f || From == To)
                {
                    From = To;
                    To = pose;
                    Blend = 0;
                }
            }
            Blend = Math.Min(1, Blend + dt / GestureSeconds);
            float load = pose == 1 ? .3f + .7f * Unit(anticipation) : pose == 3 ? .48f : 0;
            weight += (load - weight) * EaseRate(dt, 16);
            recoilAge = Math.Min(RecoilSeconds, recoilAge + dt);
            Sample();
            // 손이 지시를 시작하고 외투 끝은 뒤늦게 따라온다. 기준점은 발이므로 접지 높이는 일정하다.
            breathPhase = (breathPhase + dt * 2.7f) % 6.2831853f;
            float inhale = (float)Math.Sin(breathPhase);
            float nextLean = pose == 1 ? -.10f : pose == 2 ? .15f : pose == 3 ? -.055f : 0;
            poseLean += (nextLean - poseLean) * EaseRate(dt, 13);
            float kick = Recoil();
            spine = poseLean + .014f * inhale - .13f * kick;
            chest = .018f * inhale - .041f * weight - .061f * kick;
            arm = poseLean * .52f - kick * .095f;
            float skirt = -.52f * spine + (planted ? .028f : .082f) * (float)Math.Sin(breathPhase * 2 - .7f);
            hem += (skirt - hem) * EaseRate(dt, 8);
        }

        // 폭격·낙하·투영은 첫 발 뒤에도 각 투사체 생성 시계에 맞춰 열람→지시→회수한다.
        public static int ForgePose(int emitted, int count, float sinceRelease, float untilNext, float dt, bool rapid)
        {
            // 0.37초 간격 난사는 장갑을 다시 접지 않고 발사마다 팔과 흉곽에서 반동을 받는다.
            if (emitted > 0 && (sinceRelease < .075f || (rapid && emitted < count))) return 2;
            if (emitted < count && untilNext <= GestureSeconds + dt) return 2;
            if (emitted > 0 && sinceRelease < .28f) return 3;
            if (emitted < count && (emitted == 0 || untilNext <= .50f)) return 1;
            return 0;
        }

        // 광학 장비와 컷신의 시선 지점에도 셰이더와 같은 월드 단위 굽힘을 적용한다.
        public void Bend(float x, float y, out float dx, out float dy)
        {
            float chestWeight = Smooth(Unit((y - .52f) / 1.13f));
            float hemWeight = Smooth(Unit((y - .10f) / .34f)) * (1 - Smooth(Unit((y - .76f) / .59f))) *
                              Smooth(Unit((Math.Abs(x) - .14f) / .29f));
            float armWeight = chestWeight * Smooth(Unit((Math.Abs(x) - .29f) / .58f));
            dx = Facing * (spine * chestWeight + hem * hemWeight);
            dy = chest * chestWeight + arm * armWeight;
        }

        // 탄 또는 조형물이 생긴 바로 그 프레임에는 정확한 지시 자세를 보인다. 반동은 이후에 전파된다.
        public void Release(int direction, float strength, bool aimPose)
        {
            if (!ready) return;
            leanFacing = direction < 0 ? -1 : 1;
            recoilStrength = Unit(strength);
            recoilAge = 0;
            if (aimPose)
            {
                From = To = 2;
                Blend = 1;
                weight = 0;
            }
            Sample();
        }

        float Recoil()
        {
            float age = recoilAge / RecoilSeconds;
            // 초기 25%에 눌리고 뒤쪽 75%에 풀려, 연속 사격에서도 한 발씩 읽힌다.
            float recoil = age <= .25f ? Smooth(age * 4) : 1 - Smooth((age - .25f) / .75f);
            return recoil * recoilStrength;
        }

        void Sample()
        {
            float recoil = Recoil();
            float turnWidth = .72f + .28f * Math.Abs(leanFacing);
            ScaleX = depth * turnWidth * (1 + .019f * weight + .027f * recoil);
            ScaleY = depth * (1 - .036f * weight - .042f * recoil);
            Angle = planted ? 0 : Facing * (-1.75f * weight + 4.6f * recoil);
            OffsetX = planted ? 0 : Facing * depth * (.02f * weight - .066f * recoil);
        }

        static float Unit(float value) => Math.Min(1, Math.Max(0, value));
        static float Smooth(float t) => t * t * (3 - 2 * t);
        static float EaseRate(float dt, float frequency) => (float)(1 - Math.Exp(-dt * frequency));
    }
}
