using System;
using System.Numerics;
using Plugin.PlayerCore;

static class BluePlugOrbitChecks
{
    static void Require(bool pass, string label)
    {
        if (!pass) throw new Exception(label);
    }

    static void Main()
    {
        foreach (int sign in new[] { 1, -1 }) CheckRecordedArc(sign);
        var orbit = new BluePlugOrbitHistory();
        orbit.Add(new Vector2(1, 0), 0, 0);
        orbit.Add(new Vector2(.8f, .2f), 22f, .02f);
        orbit.Add(new Vector2(.82f, .2f), 22f, .021f);
        Require(orbit.Count == 2 && orbit.Point(0, 1) == new Vector2(.82f, .2f),
            "Stationary angle updates its tip without adding a duplicate segment.");
        orbit.Add(new Vector2(4, 4), 44f, .04f);
        Require(orbit.Count == 1, "A teleport cannot draw a connecting slash.");
        orbit.Add(new Vector2(4.3f, 4.1f), 57f, .35f);
        Require(orbit.Count == 1, "A skipped stretch of time starts with a blank orbit.");
        orbit.Clear();
        Require(orbit.Count == 0, "An interruption leaves no points for the next throw.");
        Console.WriteLine("PASS: measured two-way winding arcs, startup, world-space drift, trim, duplicate samples and reset.");
    }

    static void CheckRecordedArc(int sign)
    {
        var history = new BluePlugOrbitHistory();
        const float step = 18f;
        var seen = new Vector2[91];
        for (int i = 0; i < seen.Length; i++)
        {
            float degrees = sign * step * i;
            float angle = degrees * MathF.PI / 180f;
            // 테스트에서도 이동 중인 손/비대칭 궤적을 사용한다. 고정식을 역산한 원은 이 검사를 통과할 수 없다.
            seen[i] = new Vector2(10f + .035f * i + MathF.Cos(angle),
                -6f + .72f * MathF.Sin(angle) + .045f * MathF.Cos(2f * angle));
            float wrapped = MathF.Atan2(MathF.Sin(angle), MathF.Cos(angle)) * (180f / MathF.PI);
            history.Add(seen[i], wrapped, i * .012f);
            if (i == 0) Require(history.Count == 1, "The first sample has no retrospective arc.");
            if (i < 10) Require(history.Count == i + 1 && history.FirstFraction == 0,
                "Initial samples only draw points actually visited.");
            if (history.Count < 2) continue;
            int first = i + 1 - history.Count;
            for (int j = 0; j < history.Count - 1; j++)
            {
                Require(Vector2.Distance(history.Point(j, 0), seen[first+j]) < .0001f &&
                    Vector2.Distance(history.Point(j, 1), seen[first+j+1]) < .0001f,
                    "Smoothed ribbon interpolates exact recorded world positions.");
            }
            if (i >= 19)
            {
                float arc = step * (history.Count - 1 - history.FirstFraction);
                Require(MathF.Abs(arc - BluePlugOrbitHistory.ArcDegrees) < .002f, "Only the newest near-turn remains.");
            }
        }
    }
}
