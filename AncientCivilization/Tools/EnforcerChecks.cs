using System;
using Plugin.AncientCivilization;

static class EnforcerChecks
{
    static int checks;
    static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
        checks++;
    }

    static void Main()
    {
        // 강한 한 방이 다음 페이즈를 건너뛰지 않고 정확한 체력 경계에 도달한다.
        foreach (int maxHp in new[] { 320, 463, 840, 975 })
        {
            float hp = maxHp;
            for (int phase = 1; phase <= 3; phase++)
            {
                float smallHit = AncientEnforcerRules.LimitDamage(8, hp, maxHp, phase);
                Check(smallHit == 8, "Ordinary damage remains intact above the phase boundary");
                hp -= AncientEnforcerRules.LimitDamage(maxHp * 2, hp, maxHp, phase);
                if (phase < 3) Check(AncientEnforcerRules.Phase(hp / maxHp) == phase + 1, "Overkill advances exactly one phase");
                else Check(hp == 0, "Final phase permits a finishing blow");
            }
        }
        AncientEnforcerMove[][] order = {
            new[] { AncientEnforcerMove.Drop, AncientEnforcerMove.Bombard, AncientEnforcerMove.Gun, AncientEnforcerMove.Waves },
            new[] { AncientEnforcerMove.Blocks, AncientEnforcerMove.Echoes, AncientEnforcerMove.Gun, AncientEnforcerMove.Planes },
            new[] { AncientEnforcerMove.Echoes, AncientEnforcerMove.Blocks, AncientEnforcerMove.Missiles, AncientEnforcerMove.Charge }
        };
        for (int phase = 1; phase <= 3; phase++)
        for (int i = 0; i < 12; i++)
            Check(AncientEnforcerRules.Move(phase, i) == order[phase - 1][i % 4], "A phase repeats its own attack order");
        Check(AncientEnforcerRules.Counter(.399f) && !AncientEnforcerRules.Counter(.4f), "Counter cutoff is 40 percent");
        Check(AncientEnforcerRules.ChargeSeconds == 15 && AncientEnforcerRules.Drops == 5 && AncientEnforcerRules.PlaneCount == 5, "Specified charge length and attack counts");
        Check(AncientEnforcerRules.BombardVolleys * AncientEnforcerRules.MissilesPerVolley == 25, "Five simultaneous missiles per five salvos");
        Check(AncientEnforcerRules.EnhancedPellets == 5, "Enhanced gun preserves fivefold shot count");
        Console.WriteLine("PASS: " + checks + " enforcer phase boundary, repeat order and fixed pattern rules. Unity combat not simulated.");
    }
}
