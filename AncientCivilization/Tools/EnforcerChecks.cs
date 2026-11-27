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

    static void CheckGestures(int hz)
    {
        float dt = 1f / hz;
        var body = new AncientEnforcerBodyMotion();
        body.Step(0, 0, 1, 1, true, 0);
        foreach (int goal in new[] { 1, 2, 3, 0 })
        {
            bool first = false, second = false;
            for (int i = 0; i < hz / 4; i++)
            {
                body.Step(dt, goal, 1, 1, true, .8f);
                first |= body.Blend >= .18f && body.Blend < .5f;
                second |= body.Blend >= .5f && body.Blend <= .82f;
                Check(body.OffsetX == 0 && body.OffsetY == 0 && body.Angle == 0, "Planted boots keep their pivot");
            }
            Check(first && second && body.Blend == 1 && body.To == goal, "Both upper-arm bridge exposures precede the final pose");
        }
        body.Release(-1, .8f, true);
        Check(body.To == 2 && body.From == 2 && body.Blend == 1 && body.Facing == -1 && Math.Abs(body.ScaleY - 1) < .00001f,
            "Actual discharge uses exact aimed art and no recoil displacement");
        float farthest = 0;
        for (int i = 0; i < hz / 3; i++)
        {
            body.Step(dt, 2, -1, 1, false, 0);
            farthest = Math.Max(farthest, body.OffsetX);
        }
        Check(farthest > .047f && Math.Abs(body.OffsetX) < .00001f && Math.Abs(body.Angle) < .00001f,
            "Recoil follows the shot, peaks, then settles before the next burst");
        body.Step(dt, 2, 1, .7f, false, 0);
        Check(body.Depth > .7f && body.Depth < 1 && body.Facing == -1,
            "Returning to front / rear and turning cross intermediate scales");
        for (int i = 0; i < hz; i++) body.Step(dt, 0, 1, .7f, false, 0);
        Check(Math.Abs(body.Depth - .7f) < .001f && body.Facing == 1, "Turn completes and rear-plane depth settles");
    }

    static void CheckUniform(int hz)
    {
        var motion = new AncientEnforcerBodyMotion();
        float dt = 1f / hz;
        for (int i = 0; i < hz; i++) motion.Step(dt, 1, 1, 1, true, .9f);
        motion.Bend(.78f, 1.9f, out float readX, out float readY);
        for (int i = 0; i < hz / 2; i++) motion.Step(dt, 2, 1, 1, true, 0);
        motion.Bend(.78f, 1.9f, out float aimX, out float aimY);
        motion.Bend(.54f, 0, out float footX, out float footY);
        Check(footX == 0 && footY == 0, "Both boot soles stay registered through an upper-body gesture");
        Check(aimX - readX > .18f && aimY - readY > .06f, "Raised wrist and shoulder travel visibly between held cast poses");
        float priorHem = motion.Hem;
        motion.Release(1, .88f, true);
        for (int i = 0; i < Math.Max(1, hz / 16); i++) motion.Step(dt, 2, 1, 1, true, 0);
        motion.Bend(.78f, 1.9f, out float kickX, out float kickY);
        Check(kickX < aimX - .065f && kickY < aimY - .045f, "A projectile release kicks the upper arm while the feet stay planted");
        Check(Math.Abs(motion.Hem-priorHem) < .09f, "The hem follows rather than snapping to the recoiling arm");
        Check(AncientEnforcerBodyMotion.ForgePose(1, 5, .03f, .9f, dt, false) == 2 &&
              AncientEnforcerBodyMotion.ForgePose(1, 5, .16f, .77f, dt, false) == 3 &&
              AncientEnforcerBodyMotion.ForgePose(1, 5, .6f, .33f, dt, false) == 1 &&
              AncientEnforcerBodyMotion.ForgePose(1, 5, .82f, .11f, dt, false) == 2,
              "The second bombardment gets a follow-through, dossier read and directed release");
        Check(AncientEnforcerBodyMotion.ForgePose(3, 7, .20f, .17f, dt, true) == 2 &&
              AncientEnforcerBodyMotion.ForgePose(5, 5, .60f, .01f, dt, false) == 0,
              "Rapid fans hold their aim and the final cooldown does not prepare a nonexistent shot");
    }

    static void Main()
    {
        foreach (int hz in new[] { 30, 60, 144 }) { CheckGestures(hz); CheckUniform(hz); }
        var skip = new AncientCinematicSkip();
        // 접촉 프레임에 유지하던 점프, 짧은 탭, 포커스 밖의 입력은 컷신 종료를 만들지 않는다.
        for (int i = 0; i < 90; i++) Check(!skip.Tick(true, 1f / 60, .6f), "Entry press is not a confirmation");
        skip.Tick(false, .016f, .6f);
        Check(!skip.Tick(true, 3, .6f) && skip.Progress(.6f) < .2f, "A long rendering hitch is not a hold");
        skip.Suspend();
        Check(skip.Progress(.6f) == 0 && !skip.Tick(true, .1f, .6f), "A pause resets and disarms the gate");
        foreach (int hz in new[] {30, 60, 144})
        {
            skip.Suspend();
            skip.Tick(false, 0, .6f);
            float elapsed = 0;
            while (!skip.Tick(true, 1f / hz, .6f) && elapsed < 1) elapsed += 1f / hz;
            Check(Math.Abs(elapsed + 1f / hz - .6f) <= 1.01f / hz, "Fresh hold confirms in real time");
            skip.Tick(false, .01f, .6f);
            Check(skip.Progress(.6f) == 0, "Releasing clears partial progress");
        }
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
        Console.WriteLine("PASS: " + checks + " enforcer phase boundary, repeat order, fixed pattern, registered gesture timing, grounded pivots, recoil and cinematic skip rules. Unity combat not simulated.");
    }
}
