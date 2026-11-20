using System;
using System.Collections.Generic;
using Plugin.AncientCivilization;
class SniperChecks
{
    static int checks;
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    static AncientSniperClash NewClash(){var c=new AncientSniperClash();c.Reset();return c;}

    static void CheckPhases()
    {
        float[] gates = { .8f, .6f, .45f, .35f, .25f };
        Check(AncientSniperRules.Phase(1f) == 1, "Full health starts the duel");
        for (int i = 0; i < gates.Length; i++)
        {
            int phase = i + 1;
            Check(AncientSniperRules.HealthFloor(phase) == gates[i], "Burst damage stops at this phase's gate");
            Check(AncientSniperRules.Phase(gates[i] + .0001f) == phase, "Old phase continues above gate");
            Check(AncientSniperRules.Phase(gates[i]) == phase + 1, "Exactly reaching a gate advances once");
            foreach (float maxHp in new[] { 73f, 117.3f, 310f, 10667f })
            {
                float hp = maxHp * gates[i];
                Check(AncientSniperRules.Phase(hp / maxHp) == phase + 1, "Rounded HP ratio cannot stick at a gate");
            }
        }
        Check(AncientSniperRules.Phase(0) == 6 && AncientSniperRules.HealthFloor(6) == 0f,
            "The final quarter belongs to the laser clash");
        Check(AncientSniperRules.Phase(float.NaN) == 1, "Invalid ratio never awards a phase transition");
    }

    static void CheckFullHouse()
    {
        var signatures = new HashSet<int>();
        var finishes = new HashSet<JokerMove>();
        int pairs = 0;
        for (int variation = 0; variation < 36; variation++)
        {
            Check(AncientSniperRules.TryChain(variation, -1, 1, 7, (move, beat) => 7, out var chain),
                "Open roofs admit a chain");
            Check(signatures.Add(chain.Signature), "Each permutation has a distinct signature");
            if (chain.At(0) == JokerMove.DoubleRicochet) finishes.Add(chain.At(chain.Length - 1));
            if (chain.Length == 2) pairs++;
            Check(AncientSniperRules.TryChain(variation, chain.Signature, 1, 7, (move, beat) => 7, out var next)
                && next.Signature != chain.Signature, "Previous chain is excluded even on identical seed");
        }
        Check(pairs == 12 && signatures.Count - pairs == 24 && finishes.Count == 3,
            "Both chain lengths and different endings for a shared opener are available");
        Check(!AncientSniperRules.TryChain(0, -1, 0, 7, (move, beat) => 4, out _),
            "Leftmost player cannot reach the opposite lane in a single tell");
        Check(!AncientSniperRules.TryChain(0, -1, 0, 1, (move, beat) => 2, out _),
            "An escape lane without a roof cannot be selected");
        int FoldThenSweep(JokerMove move, int beat)
        {
            if (beat == 0 && move == JokerMove.Fold) return 1;
            if (beat == 1 && move == JokerMove.Sweep) return 2;
            return 0;
        }
        Check(!AncientSniperRules.TryChain(0, -1, 1, 7, FoldThenSweep, out _),
            "The escape path cannot cross a roof already folded earlier in the same chain");
        int Dogleg(JokerMove move, int beat)
        {
            if (beat == 0 && move == JokerMove.DoubleRicochet) return 3;
            if (beat == 1 && move == JokerMove.Sweep) return 4;
            return 0;
        }
        Check(AncientSniperRules.TryChain(0, -1, 0, 7, Dogleg, out var detour)
            && detour.SafeLaneAt(0) == 1 && detour.SafeLaneAt(1) == 2,
            "A valid route can require moving to the middle one beat in advance");
        Check(!AncientSniperRules.TryChain(0, detour.Signature, 0, 7, Dogleg, out _),
            "If only the previous chain fits, wait and resample");
        var rng = new Random(17287);
        for (int trial = 0; trial < 500; trial++)
        {
            int start = rng.Next(3), supports = rng.Next(1,8);
            int[,] projected = new int[4,3];
            for (int card = 0; card < 4; card++)
            for (int beat = 0; beat < 3; beat++) projected[card,beat] = rng.Next(8);
            int Safe(JokerMove move, int beat) => projected[(int)move,beat];
            if (!AncientSniperRules.TryChain(rng.Next(), -1, start, supports, Safe, out var route)) continue;
            int lane = start;
            for (int beat = 0; beat < route.Length; beat++)
            {
                JokerMove move = route.At(beat);
                int open = supports & Safe(move, beat);
                int next = route.SafeLaneAt(beat);
                Check(Math.Abs(lane - next) <= 1 && (open & (1 << next)) != 0,
                    "Every planned escape has support, lies outside the attack and can be reached in one tell");
                if (move == JokerMove.Fold) supports = open;
                lane = next;
            }
        }
    }
    static void Main()
    {
        Check(AncientSniperRules.AimSeconds==2,"Every aimed shot takes two seconds");
        Check(AncientSniperRules.MagazineSize==5,"Five shots precede a reload");
        CheckPhases();
        CheckFullHouse();
        AncientSniperEncounter.CheckChoreography(Check);
        Check(AncientSniperRules.FloorCell(-100,0,10,2)==0,"Left grenade impact clamps to first floor cell");
        Check(AncientSniperRules.FloorCell(100,0,10,2)==9,"Right grenade impact clamps to final floor cell");
        Check(AncientSniperRules.FloorCell(4,0,10,2)==2,"Exact impact boundary chooses the cell on its right");
        Check(AncientSniperRules.FloorCell(3.999f,0,10,2)==1,"Impact before cell boundary remains in previous cell");
        Check(AncientSniperRules.FloorCell(float.NaN,0,10,2)==-1,"Invalid impact cannot select terrain");
        Check(AncientSniperRules.FloorCell(3,0,0,2)==-1&&AncientSniperRules.FloorCell(3,0,2,0)==-1,"Invalid terrain configuration has no target");
        Check(AncientSniperRules.FloorCell(3,float.PositiveInfinity,2,2)==-1,"Nonfinite terrain origin is rejected");
        foreach(int fps in new[]{30,60,120,144,240})
        {
            var idle=NewClash();float dt=1f/fps;
            for(int i=0;i<fps*9;i++)idle.Tick(dt,false,false);
            Check(idle.Result==AncientSniperClashResult.Lost,"No input eventually fails at every tested frame rate");
            Check(Near(idle.Elapsed,8),"Timeout accounts only eight seconds");
            Check(idle.SpaceTaps==0&&idle.MouseTaps==0,"No input invents no taps");
            var held=NewClash();for(int i=0;i<fps*9;i++)held.Poll(dt,true,true);
            Check(held.Result==AncientSniperClashResult.Lost,"Holding both buttons cannot win");
            Check(held.SpaceTaps==1&&held.MouseTaps==1,"Held buttons count once regardless of render frequency");
            var mouse=NewClash();for(int i=0;i<fps*9;i++)mouse.Tick(dt,false,true);
            Check(mouse.Result==AncientSniperClashResult.Lost,"Mouse-only spam cannot win even if meter fills");
            Check(mouse.SpaceTaps==0,"Mouse input never substitutes for Space");
            var space=NewClash();for(int i=0;i<fps*9;i++)space.Tick(dt,true,false);
            Check(space.Result==AncientSniperClashResult.Lost,"Space-only spam cannot win even if meter fills");
            var both=NewClash();for(int i=0;i<fps*7;i++)both.Poll(dt,i%(fps/5)<2,i%(fps/5)<2);
            Check(both.Result==AncientSniperClashResult.Won,"Repeated independent presses can win at every tested frame rate");
            Check(both.SpaceTaps>=8&&both.MouseTaps>=8,"Winning requires the minimum in both input channels");
        }
        var early=NewClash();for(int i=0;i<7;i++)early.Tick(.02f,true,true);
        Check(early.Result==AncientSniperClashResult.Running,"Seven presses in each channel cannot win");
        var deadline=NewClash();deadline.Tick(8,true,true);
        Check(deadline.Result==AncientSniperClashResult.Lost&&deadline.SpaceTaps==0,"Presses sampled at timeout cannot rescue the attempt");
        var overshoot=NewClash();overshoot.Tick(20,true,true);
        Check(overshoot.Result==AncientSniperClashResult.Lost&&Near(overshoot.Elapsed,8),"Frame stall cannot extend the clash deadline");
        var invalid=NewClash();foreach(float dt in new[]{0f,-1,float.NaN,float.PositiveInfinity})invalid.Tick(dt,true,true);
        Check(Near(invalid.Elapsed,0)&&invalid.SpaceTaps==0&&invalid.MouseTaps==0,"Paused or invalid time cannot count inputs");
        var reset=NewClash();reset.Tick(8,false,false);reset.Reset();
        Check(reset.Result==AncientSniperClashResult.Running&&Near(reset.Progress,.4f)&&reset.SpaceTaps==0&&reset.MouseTaps==0,"Recollection retry starts with clean clash state");
        var stable=NewClash();for(int i=0;i<30&&stable.Result==AncientSniperClashResult.Running;i++)stable.Tick(.03f,true,true);
        float frozen=stable.Progress,elapsed=stable.Elapsed;int taps=stable.SpaceTaps;
        stable.Tick(8,false,false);
        Check(stable.Result==AncientSniperClashResult.Won&&Near(frozen,stable.Progress)&&Near(elapsed,stable.Elapsed)&&taps==stable.SpaceTaps,"Resolved clash cannot change result or consume new input");
        var oneHeld=NewClash();for(int i=0;i<600;i++)oneHeld.Poll(.02f,true,i%4==0);
        Check(oneHeld.Result==AncientSniperClashResult.Lost&&oneHeld.SpaceTaps==1&&oneHeld.MouseTaps>=8,"Holding Space while repeatedly clicking cannot bypass the independent Space requirement");
        var inverseHeld=NewClash();for(int i=0;i<600;i++)inverseHeld.Poll(.02f,i%4==0,true);
        Check(inverseHeld.Result==AncientSniperClashResult.Lost&&inverseHeld.MouseTaps==1&&inverseHeld.SpaceTaps>=8,"Holding mouse while tapping Space cannot bypass the independent mouse requirement");
        var edges=NewClash();edges.Poll(.02f,true,true);edges.Poll(.02f,false,false);edges.Poll(.02f,true,true);
        Check(edges.SpaceTaps==2&&edges.MouseTaps==2,"Release and repress create separate edges in both channels");
        edges.Reset();edges.Poll(.02f,true,true);
        Check(edges.SpaceTaps==1&&edges.MouseTaps==1,"Retry reset clears held-input latches as well as scores");
        var continuous=NewClash();continuous.Tick(1.25f,false,false);
        var jittered=NewClash();foreach(float dt in new[]{.01f,.14f,.3f,.2f,.6f})jittered.Tick(dt,false,false);
        Check(Near(continuous.Progress,jittered.Progress)&&Near(continuous.Elapsed,jittered.Elapsed),"Equal active time drains the same amount across irregular frame pacing");
        Console.WriteLine("PASS: "+checks+" sniper rule checks");
    }
}
