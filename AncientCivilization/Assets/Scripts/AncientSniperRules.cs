using System;
namespace Plugin.AncientCivilization
{
    public enum JokerMove { DoubleRicochet, FakeReturn, Sweep, Fold }

    // 각 비트의 공격과 그 공격이 끝났을 때 서 있을 수 있는 지붕을 함께 보관한다.
    public readonly struct JokerChain
    {
        readonly int moves, lanes;
        public int Length { get; }
        public int Signature => (1 << (Length * 2)) | moves;

        internal JokerChain(int length, int moves, int lanes)
        {
            Length = length;
            this.moves = moves;
            this.lanes = lanes;
        }

        public JokerMove At(int step) => (JokerMove)Read(moves, step);
        public int SafeLaneAt(int step) => Read(lanes, step);
        internal JokerChain WithRoute(int route) => new JokerChain(Length, moves, route);

        int Read(int packed, int step)
        {
            if (step < 0 || step >= Length) throw new ArgumentOutOfRangeException(nameof(step));
            return (packed >> (step * 2)) & 3;
        }
    }

    // Combat timing and the final clash are pure rules, independent of frame rate and Unity input.
    public static class AncientSniperRules
    {
        public const float AimSeconds=2f,ReloadSeconds=2.4f,ClashSeconds=8f;
        public const int MagazineSize=5,ClashRequiredTaps=8;
        public const float ClashStart=.4f,ClashDrainPerSecond=.065f,ClashTapGain=.055f;
        static readonly float[] phaseFloors = { .8f, .6f, .45f, .35f, .25f };
        static readonly int[] laneOffsets = { 0, -1, 1 };
        static readonly JokerChain[] fullHouse = BuildChains();
        public static int Phase(float healthRatio)
        {
            if (float.IsNaN(healthRatio)) return 1;
            for (int i = 0; i < phaseFloors.Length; i++)
            {
                // HP를 최대 HP로 다시 나눌 때 생기는 반올림으로 페이즈가 걸리지 않게 한다.
                if (healthRatio > phaseFloors[i] + .000001f) return i + 1;
            }
            return phaseFloors.Length + 1;
        }
        // 큰 피해도 다음 페이즈의 체력 경계까지만 적용한다.
        public static float HealthFloor(int phase)
        {
            return phase > phaseFloors.Length ? 0f : phaseFloors[Math.Max(phase, 1) - 1];
        }

        static JokerChain[] BuildChains()
        {
            var chains = new JokerChain[36]; // 서로 다른 카드 4장 중 2장 또는 3장을 순서대로 선택
            int next = 0;
            for (int first = 0; first < 4; first++)
            for (int second = 0; second < 4; second++)
            {
                if (first == second) continue;
                int pair = first | (second << 2);
                chains[next++] = new JokerChain(2, pair, 0);
                for (int third = 0; third < 4; third++)
                {
                    if (third != first && third != second)
                        chains[next++] = new JokerChain(3, pair | (third << 4), 0);
                }
            }
            return chains;
        }

        // availableMask / safeLanes의 하위 세 비트는 왼쪽·가운데·오른쪽 지붕이다.
        // 도탄, 빔, 접힐 지붕을 실제 배치대로 예측한 결과를 사용한다. 경로가 없으면 발동을 미룬다.
        public static bool TryChain(int variation, int previousSignature, int startLane, int availableMask,
            Func<JokerMove, int, int> safeLanes, out JokerChain chain)
        {
            chain = default;
            if (startLane < 0 || startLane > 2 || (availableMask & 7) == 0 || safeLanes == null)
                return false;
            int first = (int)(unchecked((uint)variation) % (uint)fullHouse.Length);
            for (int offset = 0; offset < fullHouse.Length; offset++)
            {
                var candidate = fullHouse[(first + offset) % fullHouse.Length];
                if (candidate.Signature == previousSignature) continue;
                if (FindRoute(candidate, 0, startLane, availableMask & 7, safeLanes, 0, out int path))
                {
                    chain = candidate.WithRoute(path);
                    return true;
                }
            }
            return false;
        }

        static bool FindRoute(JokerChain candidate, int step, int lane, int supports,
            Func<JokerMove, int, int> safeLanes, int route, out int result)
        {
            result = route;
            if (step == candidate.Length) return true;
            var move = candidate.At(step);
            int open = supports & safeLanes(move, step);
            // 접힌 발판은 같은 연계 안에서 다음 동작의 착지점으로 부활하지 않는다.
            if (move == JokerMove.Fold) supports = open;
            foreach (int offset in laneOffsets)
            {
                int next = lane + offset;
                if (next < 0 || next > 2) continue;
                if ((open & (1 << next)) == 0) continue;
                if (FindRoute(candidate, step + 1, next, supports, safeLanes,
                    route | (next << (2 * step)), out result)) return true;
            }
            return false;
        }
        public static int FloorCell(float x,float left,int cellCount,float cellWidth)
        {
            if(cellCount<=0||cellWidth<=0||float.IsNaN(cellWidth)||float.IsInfinity(cellWidth)||float.IsNaN(x)||float.IsInfinity(x)||float.IsNaN(left)||float.IsInfinity(left))return -1;
            double cell=Math.Floor(((double)x-left)/cellWidth);
            return cell<0?0:cell>=cellCount?cellCount-1:(int)cell;
        }
    }
    public enum AncientSniperClashResult { Running,Won,Lost }
    public struct AncientSniperClash
    {
        public float Progress {get;private set;}
        public float Elapsed {get;private set;}
        public int SpaceTaps {get;private set;}
        public int MouseTaps {get;private set;}
        public AncientSniperClashResult Result {get;private set;}
        bool spaceWasHeld,mouseWasHeld;
        // Reset is required for a fresh attempt, including every retry after recollecting particles.
        public void Reset()
        {Progress=AncientSniperRules.ClashStart;Elapsed=0;SpaceTaps=MouseTaps=0;Result=AncientSniperClashResult.Running;spaceWasHeld=mouseWasHeld=false;}
        // Use this overload with GetKey/GetMouseButton. A held button contributes once, never per frame.
        public void Poll(float seconds,bool spaceHeld,bool mouseHeld)
        {
            if(!ValidTime(seconds)||Result!=AncientSniperClashResult.Running)return;
            bool spacePressed=spaceHeld&&!spaceWasHeld,mousePressed=mouseHeld&&!mouseWasHeld;
            spaceWasHeld=spaceHeld;mouseWasHeld=mouseHeld;
            Tick(seconds,spacePressed,mousePressed);
        }
        // Use this overload only with fresh press edges (GetKeyDown/GetMouseButtonDown).
        public void Tick(float seconds,bool spacePressed,bool mousePressed)
        {
            if(Result!=AncientSniperClashResult.Running||!ValidTime(seconds))return;
            float step=Math.Min(seconds,Math.Max(0,AncientSniperRules.ClashSeconds-Elapsed));
            Progress=Math.Max(0,Progress-AncientSniperRules.ClashDrainPerSecond*step);Elapsed+=step;
            // Inputs sampled at or after the deadline cannot rescue an expired attempt.
            if(Elapsed>=AncientSniperRules.ClashSeconds){Result=AncientSniperClashResult.Lost;return;}
            if(spacePressed){SpaceTaps++;Progress+=AncientSniperRules.ClashTapGain;}
            if(mousePressed){MouseTaps++;Progress+=AncientSniperRules.ClashTapGain;}
            Progress=Math.Min(1,Progress);
            if(Progress>=1&&SpaceTaps>=AncientSniperRules.ClashRequiredTaps&&MouseTaps>=AncientSniperRules.ClashRequiredTaps)
                Result=AncientSniperClashResult.Won;
        }
        static bool ValidTime(float seconds)
        {return seconds>0&&!float.IsNaN(seconds)&&!float.IsInfinity(seconds);}
    }
}
