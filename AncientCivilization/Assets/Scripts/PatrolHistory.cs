using System;

namespace Plugin.PlayerCore
{
    // 이번 주행의 실제 위치를 고른 시간 간격으로 보관한다. 긴 탐색에서는 간격을 두 배로 늘린다.
    internal sealed class PatrolHistory
    {
        internal const int Capacity = 192;
        const float FirstStep = .08f;
        readonly int[] points = new int[Capacity];
        int count, tail;
        public float Duration { get; private set; }
        public float Step { get; private set; } = FirstStep;
        public bool HasRun => count > 0;

        internal static int Pack(float x, float y, bool left, int frame)
        {
            int px = Math.Max(0, Math.Min(8191, (int)Math.Round((x + 8) * 100)));
            int py = Math.Max(0, Math.Min(8191, (int)Math.Round((y + 24) * 100)));
            return px | (py << 13) | (left ? 1 << 26 : 0) | (Math.Max(0, Math.Min(7, frame)) << 27);
        }
        static void ReadPoint(int point, out float x, out float y, out bool left)
        {
            x = (point & 8191) * .01f - 8;
            y = ((point >> 13) & 8191) * .01f - 24;
            left = (point & (1 << 26)) != 0;
        }

        public void Begin(float x, float y, bool left, int frame = 0)
        {
            Step = FirstStep;
            Duration = 0;
            count = 1;
            points[0] = tail = Pack(x, y, left, frame);
        }

        public void Record(float dt, float x, float y, bool left, int frame = 0)
        {
            if (!HasRun || !(dt > 0) || float.IsInfinity(dt) || float.IsNaN(x) || float.IsNaN(y)) return;
            float nextTime = Duration + dt;
            ReadPoint(tail, out float lastX, out float lastY, out _);
            while (count * Step <= nextTime)
            {
                if (count == Capacity)
                {
                    for (int i = 1; i < count / 2; i++) points[i] = points[i * 2];
                    count /= 2;
                    Step *= 2;
                }
                float fraction = (count * Step - Duration) / dt;
                points[count++] = Pack(lastX + (x - lastX) * fraction, lastY + (y - lastY) * fraction, left, frame);
            }
            Duration = nextTime;
            tail = Pack(x, y, left, frame);
        }

        public bool Sample(float t, out float x, out float y, out bool left, out int frame)
        {
            frame = 0;
            x = y = 0; left = false;
            if (!HasRun || t < 0 || t > Duration || float.IsNaN(t)) return false;
            int index = Math.Min(count - 1, (int)(t / Step));
            bool last = index + 1 == count;
            int end = last ? tail : points[index + 1];
            ReadPoint(points[index], out float ax, out float ay, out bool al);
            ReadPoint(end, out float bx, out float by, out bool bl);
            float width = last ? Duration - index * Step : Step;
            float f = width <= .00001f ? 1 : Math.Max(0, Math.Min(1, (t - index * Step) / width));
            x = ax + (bx - ax) * f;
            y = ay + (by - ay) * f;
            left = f < .5f ? al : bl;
            frame = (f < .5f ? points[index] : end) >> 27;
            return true;
        }

        public void Save(string key, Action<string, int> write)
        {
            if (!HasRun || Duration <= 0) return;
            write(key + ".duration", (int)Math.Round(Duration * 1000));
            write(key + ".step", (int)Math.Round(Step * 1000));
            write(key + ".end", tail);
            for (int i = 0; i < count; i++) write(key + "." + i, points[i]);
            write(key + ".n", count);
        }

        public void Load(string key, Func<string, int, int> read)
        {
            count = 0;
            Duration = read(key + ".duration", 0) * .001f;
            Step = read(key + ".step", 0) * .001f;
            int n = read(key + ".n", 0);
            if (n < 1 || n > Capacity || Step < .01f || Duration <= 0 ||
                Duration + .002f < (n - 1) * Step || Duration > n * Step + .002f) return;
            for (int i = 0; i < n; i++)
            {
                int value = read(key + "." + i, -1);
                if (value < 0 || value >= 1 << 30) return;
                points[i] = value;
            }
            tail = read(key + ".end", -1);
            if (tail < 0 || tail >= 1 << 30) return;
            count = n;
        }
    }
}
