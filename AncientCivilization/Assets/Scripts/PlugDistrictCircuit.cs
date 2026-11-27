using System;

namespace Plugin.PlayerCore
{
    // 콘센트에 실제로 꽂힌 사건만 설비를 작동시킨다. 플레이어의 속도·입력과 독립한다.
    public sealed class PlugDistrictCircuit
    {
        readonly int floor;
        readonly float shutterSeconds, sequenceSeconds, relaySeconds, releaseHold, canopyInterval;
        int linked = -1;
        float timeLeft, canopyAge;
        bool energized, latched, canopyStarted;

        // 두 번째 현장은 같은 층의 설비를 다른 용도로 사용한다.
        static readonly int[] SecondMechanisms = { 1, 3, 4, 0, 2 };

        public static int KindFor(int floor, bool advanced)
        {
            if ((uint)floor >= SecondMechanisms.Length) throw new ArgumentOutOfRangeException(nameof(floor));
            return advanced ? SecondMechanisms[floor] : floor;
        }

        public int SocketCount => floor == 2 || floor == 4 ? 2 : 1;
        public bool Powered => energized;
        public bool Completed => latched;
        public float Remaining => timeLeft;
        public int Linked => linked;

        public PlugDistrictCircuit(int floor, float shutterSeconds, float sequenceSeconds,
            float relaySeconds, float releaseHold, float canopyInterval)
        {
            if (floor < 0 || floor > 4) throw new ArgumentOutOfRangeException(nameof(floor));
            this.floor = floor;
            this.shutterSeconds = Math.Max(.25f, shutterSeconds);
            this.sequenceSeconds = Math.Max(.25f, sequenceSeconds);
            this.relaySeconds = Math.Max(.25f, relaySeconds);
            this.releaseHold = Math.Max(.25f, releaseHold);
            this.canopyInterval = Math.Max(.05f, canopyInterval);
        }

        public bool Available(int slot)
        {
            if (slot == 0) return true;
            if (slot != 1 || SocketCount != 2) return false;
            return linked == 1 || energized || (floor == 2 && latched);
        }

        public bool Dock(int slot)
        {
            if (linked >= 0 || !Available(slot)) return false;
            linked = slot;
            if (slot == 1)
            {
                latched = true;
                if (floor == 4) timeLeft = releaseHold;
                return true;
            }
            if (floor == 3 && !energized) { canopyAge = 0; canopyStarted = true; }
            energized = true;
            switch (floor)
            {
                case 1: timeLeft = shutterSeconds; break;
                case 2: timeLeft = sequenceSeconds; break;
                case 4: timeLeft = relaySeconds; break;
                default: timeLeft = releaseHold; break;
            }
            return true;
        }

        public void Release(int slot)
        {
            if (linked != slot || slot < 0) return;
            linked = -1;
            if (floor == 4 && slot == 1) timeLeft = releaseHold;
        }

        public void Step(float dt)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            if (canopyStarted && energized)
                canopyAge = Math.Min(canopyInterval * 3, canopyAge + dt);
            // 승강기와 차양은 연결 중에 전원을 유지한다. 상단 중계는 잡은 B를 도중에 끄지 않는다.
            if ((linked == 0 && (floor == 0 || floor == 3)) || (linked == 1 && floor == 4)) return;
            timeLeft = Math.Max(0, timeLeft - dt);
            if (timeLeft > 0) return;
            energized = false;
            if (floor == 4) latched = false;
            if (floor == 3) { canopyStarted = false; canopyAge = 0; }
        }

        public bool WantsSurface(int index)
        {
            switch (floor)
            {
                case 0: case 1: return energized;
                case 2: return latched;
                case 3: return index >= 0 && index < 3 && canopyStarted && energized && canopyAge >= index * canopyInterval;
                case 4: return latched;
                default: return false;
            }
        }

        public void Reset()
        {
            linked = -1;
            timeLeft = canopyAge = 0;
            energized = latched = canopyStarted = false;
        }
    }
}
