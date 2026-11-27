using System;

namespace Plugin.AncientCivilization
{
    // 패턴 번호 세 개가 한 묶음이다. 이미 연 묶음은 이후 페이즈에도 후보로 남는다.
    public enum OrbitalMove
    {
        GroundWave, LandingStamp, ReturnBlade,
        AnchorLock, CargoSweep, DropCargo,
        RisingBeam, CrossBeam, ShutterFeint,
        BladeHunt, CoreTrap, SupplyLeak,
        ReverseRail, ChainDrop, SplitShutter,
        PursuitStamp, ClosingSweep, CargoRush
    }

    public readonly struct OrbitalArenaState
    {
        public readonly float Left, Right, HeroX;
        public readonly bool Grounded, LeftAnchor, RightAnchor, AirDash, InstalledBlade;

        public OrbitalArenaState(float left, float right, float heroX, bool grounded,
            bool leftAnchor, bool rightAnchor, bool airDash = false, bool installedBlade = false)
        {
            Left = left;
            Right = right;
            HeroX = heroX;
            Grounded = grounded;
            LeftAnchor = leftAnchor;
            RightAnchor = rightAnchor;
            AirDash = airDash;
            InstalledBlade = installedBlade;
        }
    }

    public static class AncientOrbitalRules
    {
        static readonly float[] Gates = { .82f, .65f, .48f, .31f, .15f, 0f };

        public static int Phase(float healthRatio)
        {
            int phase = 1;
            // 큰 한 방을 정확히 경계에서 멈춘 뒤의 부동소수 반올림을 허용한다.
            while (phase < Gates.Length && healthRatio <= Gates[phase - 1] + .000001f) phase++;
            return phase;
        }

        public static float LimitDamage(float damage, float health, float maxHealth, int phase)
        {
            if (phase < 1 || phase > Gates.Length || !(damage > 0) || !(health > 0) || !(maxHealth > 0)) return 0;
            return Math.Max(0, Math.Min(damage, health - maxHealth * Gates[phase - 1]));
        }

        public static int ChainLength(int phase, float roll01)
        {
            if (phase <= 4) return 1;
            if (phase == 5) return roll01 < .5f ? 1 : 2;
            return roll01 < .5f ? 2 : 3;
        }

        public static float OpeningSeconds(int phase, bool latchHit)
        {
            if (phase >= 6 && latchHit) return 2.65f;
            return phase < 5 ? 1.35f : 1.15f;
        }

        // 실제 전장에는 연속으로 걸을 수 있는 하단 통로가 있다. 전조를 보기 전까지는
        // 잠정 후보일 뿐이며, 호출자는 뽑힌 패턴과 발사 방향을 전조 시작 시 고정한다.
        public static bool TryPick(int phase, OrbitalArenaState arena, OrbitalMove? last,
            OrbitalMove? previous, float roll01, out OrbitalMove pick, bool followup = false, bool preferStamp = false)
        {
            pick = default;
            if (phase < 1 || phase > Gates.Length || float.IsNaN(roll01)) return false;
            float roomLeft = arena.HeroX - arena.Left;
            float roomRight = arena.Right - arena.HeroX;
            if (!(roomLeft >= 0 && roomRight >= 0) || float.IsInfinity(roomLeft + roomRight) ||
                Math.Max(roomLeft, roomRight) < 3f) return false;

            int count = phase * 3, mask = 0, weight = 0;
            for (int i = 0; i < count; i++)
            {
                var move = (OrbitalMove)i;
                if (move == last || move == previous ||
                    !Safe(move, arena, roomLeft, roomRight, last, followup)) continue;
                mask |= 1 << i;
                weight += Weight(i, count);
            }
            if (weight == 0) return false;
            // 마지막 타에 잠금장치 유도 기회를 종종 제시한다. 두 찍기가 모두 최근에
            // 나왔다면 정상 후보를 유지해 반복을 피하고 연계를 끝까지 이을 수 있다.
            if (phase == 6 && followup && preferStamp)
            {
                int stampMask = mask & ((1 << (int)OrbitalMove.LandingStamp) | (1 << (int)OrbitalMove.PursuitStamp));
                if (stampMask != 0)
                {
                    mask = stampMask;
                    weight = ((mask & (1 << (int)OrbitalMove.LandingStamp)) != 0 ? 1 : 0)
                        + ((mask & (1 << (int)OrbitalMove.PursuitStamp)) != 0 ? 2 : 0);
                }
            }
            // UnityEngine.Random.value에는 정확히 1도 포함된다.
            float unit = Math.Max(0f, Math.Min(1f, roll01));
            int ticket = Math.Min(weight - 1, (int)(unit * weight));
            for (int i = 0; i < count; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                ticket -= Weight(i, count);
                if (ticket < 0)
                {
                    pick = (OrbitalMove)i;
                    return true;
                }
            }
            return false;
        }

        static int Weight(int i, int count)
        {
            int baseWeight = i >= count - 3 ? 2 : 1;
            return i == (int)OrbitalMove.BladeHunt ? baseWeight * 3 : baseWeight;
        }

        static bool Safe(OrbitalMove move, OrbitalArenaState arena, float roomLeft, float roomRight,
            OrbitalMove? last, bool followup)
        {
            bool anchor = arena.LeftAnchor || arena.RightAnchor;
            bool verticalExit = arena.Grounded || anchor || arena.AirDash;
            bool bothGroundLanes = Math.Min(roomLeft, roomRight) >= 2.7f;

            // 한쪽으로 몰린 공중 플레이어에게 직전 낙하/봉쇄 직후 다시 고도를 요구하면
            // 공중 대시가 강제된다. 남아 있는 앵커나 발 디딤이 없으면 그 연계를 뺀다.
            if (followup && !arena.AirDash && !arena.Grounded && !bothGroundLanes &&
                (last == OrbitalMove.ChainDrop || last == OrbitalMove.AnchorLock) &&
                (move == OrbitalMove.SplitShutter || move == OrbitalMove.CargoRush || move == OrbitalMove.CrossBeam))
            {
                bool inwardAnchor = roomLeft < roomRight ? arena.RightAnchor : arena.LeftAnchor;
                if (!inwardAnchor) return false;
            }

            switch (move)
            {
                case OrbitalMove.BladeHunt:
                    return arena.InstalledBlade;
                case OrbitalMove.AnchorLock:
                    return arena.LeftAnchor && arena.RightAnchor;
                case OrbitalMove.CargoSweep:
                case OrbitalMove.CargoRush:
                case OrbitalMove.ClosingSweep:
                    return verticalExit;
                case OrbitalMove.CrossBeam:
                case OrbitalMove.SupplyLeak:
                    return bothGroundLanes || (roomLeft > roomRight ? arena.LeftAnchor : arena.RightAnchor) || arena.AirDash;
                case OrbitalMove.CoreTrap:
                    return bothGroundLanes;
                case OrbitalMove.ChainDrop:
                    return Math.Max(roomLeft, roomRight) >= 5f || anchor;
                case OrbitalMove.SplitShutter:
                    return bothGroundLanes || verticalExit;
                default:
                    // 같은 세로선의 찍기/낙하는 옆으로, 왕복날·파동은 점프로도 피한다.
                    return true;
            }
        }
    }
}
