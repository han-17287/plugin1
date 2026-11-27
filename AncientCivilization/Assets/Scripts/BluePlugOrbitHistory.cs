using System;
using System.Numerics;

namespace Plugin.PlayerCore
{
    // 화면에 그린 팁의 월드 위치만 보관한다. 지나지 않은 원의 나머지 부분은 만들지 않는다.
    internal sealed class BluePlugOrbitHistory
    {
        internal const int Capacity = 64;
        internal const float ArcDegrees = 326f;

        struct Sample
        {
            public Vector2 Position;
            public double Arc;
        }

        readonly Sample[] samples = new Sample[Capacity];
        int first;
        float previousPhase, previousTime;

        internal int Count { get; private set; }
        int Index(int offset) => (first + offset) % Capacity;
        internal void Clear() { first = Count = 0; }

        internal void Add(Vector2 position, float phaseDegrees, float time)
        {
            int last = Index(Math.Max(0, Count - 1));
            float step = Math.Abs((phaseDegrees - previousPhase + 540f) % 360f - 180f);
            // 일시정지·순간이동·건너뛴 반 바퀴를 가로질러 직선을 그리지 않는다.
            if (Count > 0 && (time < previousTime || time - previousTime > .12f ||
                Vector2.DistanceSquared(position, samples[last].Position) > 9f || step > 145f))
                Clear();
            previousPhase = phaseDegrees;
            previousTime = time;
            if (Count > 0 && step < .1f)
            {
                samples[last].Position = position;
                return;
            }
            double arc = Count == 0 ? 0 : samples[last].Arc + step;
            if (Count == Capacity) RemoveOldest();
            samples[Index(Count)] = new Sample { Position = position, Arc = arc };
            Count++;
            while (Count > 2 && arc - samples[Index(1)].Arc >= ArcDegrees) RemoveOldest();
        }

        void RemoveOldest() { first = Index(1); --Count; }

        // 첫 구간의 일부만 남겨 프레임 속도에 관계없이 꼬리가 한 바퀴 직전에 끝나게 한다.
        internal float FirstFraction
        {
            get
            {
                if (Count < 2) return 0;
                double trim = samples[Index(Count - 1)].Arc - samples[first].Arc - ArcDegrees;
                return (float)Math.Clamp(trim / (samples[Index(1)].Arc - samples[first].Arc), 0, 1);
            }
        }

        internal Vector2 Point(int segment, float t)
        {
            var p0 = samples[Index(Math.Max(0, segment - 1))].Position;
            var p1 = samples[Index(segment)].Position;
            var p2 = samples[Index(segment + 1)].Position;
            var p3 = samples[Index(Math.Min(Count - 1, segment + 2))].Position;
            // 측정점 사이만 부드럽게 잇는다. 마지막 점은 항상 이번 프레임 실제 단자 위치다.
            float t2 = t * t, t3 = t2 * t;
            return (2 * t3 - 3 * t2 + 1) * p1 + (t3 - 2 * t2 + t) * .5f * (p2 - p0)
                + (-2 * t3 + 3 * t2) * p2 + (t3 - t2) * .5f * (p3 - p1);
        }
    }
}
