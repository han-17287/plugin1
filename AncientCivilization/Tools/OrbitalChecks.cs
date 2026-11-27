using System;
using System.Collections.Generic;
using Plugin.AncientCivilization;

internal static class OrbitalChecks
{
    static int passed;

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        passed++;
    }

    static HashSet<OrbitalMove> Candidates(int phase, OrbitalArenaState arena,
        OrbitalMove? last = null, OrbitalMove? previous = null, bool followup = false)
    {
        var result = new HashSet<OrbitalMove>();
        for (int ticket = 0; ticket <= 96; ticket++)
            if (AncientOrbitalRules.TryPick(phase, arena, last, previous, ticket / 96f, out var pick, followup))
                result.Add(pick);
        return result;
    }

    static void DamageAndWindows()
    {
        foreach (var maximum in new[] { 427f, 812.5f, 1087.25f })
        {
            float hp = maximum;
            for (int p = 1; p <= 6; p++)
            {
                Check(AncientOrbitalRules.Phase(hp / maximum) == p, "one boundary per phase");
                Check(AncientOrbitalRules.LimitDamage(3f, hp, maximum, p) == 3f, "ordinary hit");
                hp -= AncientOrbitalRules.LimitDamage(2000f, hp, maximum, p);
                Check(Math.Abs(AncientOrbitalRules.LimitDamage(2000f, hp, maximum, p)) < .0001f,
                    "cannot skip a phase while a committed attack is still busy");
            }
            Check(hp == 0, "completion only in phase 6");
        }
        var cuts = new[] { .82f, .65f, .48f, .31f, .15f };
        for (int i = 0; i < cuts.Length; i++)
        {
            Check(AncientOrbitalRules.Phase(cuts[i] + .001f) == i + 1, "above health threshold");
            Check(AncientOrbitalRules.Phase(cuts[i]) == i + 2, "at health threshold");
        }
        for (int i = 1; i <= 4; i++)
            Check(AncientOrbitalRules.ChainLength(i, .99f) == 1, "early solo attacks");
        Check(AncientOrbitalRules.ChainLength(5,.1f) == 1 && AncientOrbitalRules.ChainLength(5,.9f) == 2,
            "phase 5 both chain lengths");
        Check(AncientOrbitalRules.ChainLength(6,.1f) == 2 && AncientOrbitalRules.ChainLength(6,.9f) == 3,
            "phase 6 both chain lengths");
        Check(AncientOrbitalRules.OpeningSeconds(6,false) >= 1f &&
              AncientOrbitalRules.OpeningSeconds(6,true) > 2 * AncientOrbitalRules.OpeningSeconds(6,false),
              "normal final dodge and baited latch both have openings");
    }

    static void SpatialFilters()
    {
        var middle = new OrbitalArenaState(0, 24, 12, true, true, true, installedBlade:true);
        for (int phase = 1; phase <= 6; phase++)
        {
            var moves = Candidates(phase, middle);
            Check(moves.Count == phase * 3, "all previous moves plus exactly three newly available");
            for (int i = 0; i < phase * 3; i++) Check(moves.Contains((OrbitalMove)i), "individual move available without dash");
        }

        var stranded = new OrbitalArenaState(0, 24, .25f, false, false, false);
        var safeOnly = Candidates(6, stranded);
        Check(!safeOnly.Contains(OrbitalMove.AnchorLock) && !safeOnly.Contains(OrbitalMove.ClosingSweep) &&
              !safeOnly.Contains(OrbitalMove.CrossBeam) && !safeOnly.Contains(OrbitalMove.CoreTrap), "wall + no anchor + no air dash");
        Check(!safeOnly.Contains(OrbitalMove.BladeHunt), "an uninstalled blade cannot be hunted");
        Check(!Candidates(6,new OrbitalArenaState(0,24,12,true,true,true)).Contains(OrbitalMove.BladeHunt),
            "open ground does not arm the hunt without an installed target");
        int hunts=0, traps=0;
        for(int i=0;i<1900;i++)
        {
            AncientOrbitalRules.TryPick(4,middle,null,null,(i+.5f)/1900f,out var pick);
            if(pick==OrbitalMove.BladeHunt)hunts++;
            if(pick==OrbitalMove.CoreTrap)traps++;
        }
        Check(hunts==traps*3, "installed blade has triple the weight of its phase peers");
        foreach (bool left in new[] {true, false})
        {
            var nearX = left ? .25f : 23.75f;
            var outward = new OrbitalArenaState(0, 24, nearX, false, left, !left);
            var inward = new OrbitalArenaState(0, 24, nearX, false, !left, left);
            Check(!Candidates(6, outward, OrbitalMove.ChainDrop, null, true).Contains(OrbitalMove.CargoRush), "no inward aerial escape after chain drop");
            Check(Candidates(6, inward, OrbitalMove.ChainDrop, null, true).Contains(OrbitalMove.CargoRush), "reachable inward anchor supports this followup");
            Check(Candidates(6, new OrbitalArenaState(0,24,nearX,false,left,!left,true), OrbitalMove.ChainDrop, null, true).Contains(OrbitalMove.CargoRush), "actual available air dash relaxes same constraint");
            Check(!Candidates(2, outward).Contains(OrbitalMove.AnchorLock), "cannot lock final anchor");
        }
        Check(!AncientOrbitalRules.TryPick(6, new OrbitalArenaState(10,12,11,true,true,true),null,null,.5f,out _),"yield when no clear lane");
        Check(!AncientOrbitalRules.TryPick(6, new OrbitalArenaState(0,24,float.NaN,true,true,true),null,null,.5f,out _),"invalid hero position");
        foreach (var roll in new[] {0f, .33f, .68f, 1f})
        {
            Check(AncientOrbitalRules.TryPick(6, middle, null, null, roll, out var pick, true, true)
                && (pick == OrbitalMove.LandingStamp || pick == OrbitalMove.PursuitStamp), "available baitable final strike");
            Check(AncientOrbitalRules.TryPick(6, middle, OrbitalMove.PursuitStamp, OrbitalMove.ReturnBlade, roll, out pick, true, true)
                && pick == OrbitalMove.LandingStamp, "a preferred final stamp still respects history");
            Check(AncientOrbitalRules.TryPick(6, middle, OrbitalMove.PursuitStamp, OrbitalMove.LandingStamp, roll, out pick, true, true)
                && pick != OrbitalMove.PursuitStamp && pick != OrbitalMove.LandingStamp, "normal legal fallback if both stamps recent");
        }
    }

    static void SimulatedRounds()
    {
        var first = new HashSet<OrbitalMove>();
        var secondOrder = new HashSet<int>();
        for (int seed = 0; seed < 96; seed++)
        {
            var random = new Random(seed);
            OrbitalMove? last = null, previous = null;
            for (int phase = 1; phase <= 6; phase++)
            {
                // 검사 전장과 같은 넓이에서 양끝, 바닥, 공중과 남은 앵커 조합을 모두 오간다.
                for (int cycle = 0; cycle < 16; cycle++)
                {
                    int length = AncientOrbitalRules.ChainLength(phase,(float)random.NextDouble());
                    for (int step = 0; step < length; step++)
                    {
                        var arena = new OrbitalArenaState(0, 24, (float)random.NextDouble()*24,
                            random.Next(2)==1,random.Next(2)==1,random.Next(2)==1);
                        bool ok=AncientOrbitalRules.TryPick(phase,arena,last,previous,(float)random.NextDouble(),out var pick,step > 0);
                        Check(ok,"at least one safe option even with history and no air dash");
                        Check((int)pick < phase*3 && pick!=last && pick!=previous,"unlocked and excludes previous two across phase boundaries");
                        if (phase==1 && cycle==0) first.Add(pick);
                        if (phase==1 && cycle==1) secondOrder.Add((int)last.Value * 10 + (int)pick);
                        previous = last;
                        last = pick;
                    }
                }
            }
        }
        Check(first.Count == 3 && secondOrder.Count == 6, "new attempt varies opening order");
    }

    static void Main()
    {
        DamageAndWindows();
        SpatialFilters();
        SimulatedRounds();
        Console.WriteLine($"PASS: {passed} orbital selection, health gate, escape-route and chain-opening checks");
    }
}
