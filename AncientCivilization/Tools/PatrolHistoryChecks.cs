using System;
using System.Collections.Generic;
using Plugin.PlayerCore;

static class PatrolHistoryChecks
{
    static int checks;
    static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(message);
    }

    static void Main()
    {
        var run = new PatrolHistory();
        var loaded = new PatrolHistory();
        var save = new Dictionary<string, int>();
        int Read(string key, int fallback) => save.TryGetValue(key, out int n) ? n : fallback;
        void Write(string key, int value) => save[key] = value;
        Check(!run.Sample(0, out _, out _, out _, out _), "No synthetic ghost before a real run");
        run.Begin(2, 1.1f, false, 2);
        const int frames = 6401;
        double t = 0;
        for (int i = 0; i < frames; i++)
        {
            float dt = i % 3 == 0 ? .01f : .02f;
            t += dt;
            run.Record(dt, (float)(2 + t * .3), (float)(1.1 + t * .1), i > frames / 2, i > frames / 2 ? 5 : 2);
        }
        run.Save("room", Write);
        Check(save.Count <= PatrolHistory.Capacity + 4, "Bounded save size even after compaction");
        loaded.Load("room", Read);
        Check(loaded.HasRun && loaded.Step > .08f, "Long actual run deserializes after several compactions");
        for (float sample = .01f; sample < t - .05; sample += .07f)
        {
            Check(loaded.Sample(sample, out float x, out float y, out bool left, out int frame), "Valid replay time");
            Check(Math.Abs(x - (2 + sample * .3)) < .055 && Math.Abs(y - (1.1 + sample * .1)) < .055, "Recorded route is interpolated in time order");
            if (Math.Abs(sample - t * .5) > 1.5)
            {
                Check(left == (sample > t * .5), "Facing comes from the recorded run");
                Check(frame == (left ? 5 : 2), "Actual animation frame, including directional grapple pose, survives serialization");
            }
        }
        Check(!loaded.Sample(loaded.Duration + 1, out _, out _, out _, out _), "Old ghost disappears after its own finish time");
        Check(loaded.Sample(loaded.Duration, out float end, out _, out _, out _) && Math.Abs(end - (2 + t * .3)) < .045, "Exact final point retained outside uniform grid");

        var retry = new PatrolHistory(); retry.Begin(2, 1, true);
        retry.Record(.031f, 2.25f, 1.3f, false);
        loaded.Load("room", Read);
        Check(loaded.Duration > 100, "Unfinished/fallen retry cannot replace previous ghost");
        retry.Save("room", Write); loaded.Load("room", Read);
        Check(Math.Abs(loaded.Duration - .031f) < .0001f, "Most recent completion replaces previous even when shorter");
        Check(loaded.Sample(.02f, out float shortX, out _, out _, out _) && shortX > 2 && shortX < 2.25f, "Sub-frame run uses real tail");
        save["room.end"] = -1;
        loaded.Load("room", Read);
        Check(!loaded.HasRun, "Corrupt record rejected without exceptions");
        save["room.n"] = 3000; loaded.Load("room", Read);
        Check(!loaded.HasRun, "Out-of-bounds size rejected");
        loaded.Load("no-such-room", Read);
        Check(!loaded.HasRun, "No previous room's ghost leaks into unvisited room");
        Console.WriteLine($"PASS: {checks} patrol ghost recording, interpolation, facing, retry, persistence and corruption checks");
    }
}
