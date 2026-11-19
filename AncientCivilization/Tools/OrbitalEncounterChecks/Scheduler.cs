using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public static class OrbitalEncounterChecks
    {
        const float Frame = 1f / 60;
        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        static AncientOrbitalEncounter Spawn(AncientGame game, out AncientEnemy boss)
        {
            var actor = new GameObject("Upper core warden");
            boss = actor.AddComponent<AncientEnemy>();
            var battle = actor.AddComponent<AncientOrbitalEncounter>();
            actor.transform.position = new Vector2(26, 0);
            game.hero.transform.position = new Vector2(10, 1);
            game.Orbital = battle;
            battle.Initialize(game, boss, 0, 32);
            return battle;
        }

        static void CheckInterruptedTell()
        {
            var game = new AncientGame();
            var battle = Spawn(game, out _);
            for (int frame = 0; frame < 200 && !battle.Telegraphing; frame++) battle.Tick();
            Check(battle.Telegraphing, "Failed to enter first tell");
            game.Paused = true;
            float progress = battle.WindupProgress;
            for (int frame = 0; frame < 240; frame++) battle.Tick();
            Check(battle.WindupProgress == progress, "Paused tell advanced");
            var arena = battle.Arena;
            battle.StopBattle();
            game.Paused = false;
            for (int frame = 0; frame < 360; frame++) battle.Tick();
            Check(!battle.Active && game.Orbital == null && arena.Released && arena.Executions == 0,
                "Cancelled tell executed after retry");
        }

        public static void Main()
        {
            Time.deltaTime = Frame;
            Time.unscaledDeltaTime = Frame;
            CheckInterruptedTell();
            var game = new AncientGame();
            var battle = Spawn(game, out var boss);
            var arena = battle.Arena;
            var moves = new HashSet<OrbitalMove>();
            int phaseSeen = 0, chains = 0, windows = 0, ticks = 0;
            int openedPhase = 0, openFrames = 0;
            bool inTell = false, inOpening = false, phaseFiveFollowup = false;
            bool latchWindow = false, ordinaryWindow = false, finalBlow = false, feintLens = false;
            bool observedFiring = false;
            int feintActiveFrame = -1;
            OrbitalMove? last = null, previous = null;
            for (; ticks < 120000 && !finalBlow; ticks++)
            {
                battle.Tick();
                if (battle.Telegraphing && !inTell)
                {
                    var move = battle.CurrentMove;
                    Check(move != last && move != previous, "Repeated one of two recent moves");
                    Check((int)move / 3 < battle.Phase, "Used a move before unlocking its phase");
                    Check(AncientOrbitalRig.FollowupLamp == (battle.ChainStep < battle.ChainLength),
                        "Second lamp disagrees with remaining attacks");
                    previous = last;
                    last = move;
                    moves.Add(move);
                    if (battle.ChainStep == 1)
                    {
                        chains++;
                        Check(battle.Phase == phaseSeen || battle.Phase == phaseSeen + 1, "Skipped an HP gate");
                        phaseSeen = battle.Phase;
                    }
                    if (battle.Phase == 5 && battle.ChainLength == 2) phaseFiveFollowup = true;
                    if (battle.Phase == 6)
                        Check(battle.ChainLength == 2 || battle.ChainLength == 3, "Invalid final chain length");
                    // 예고한 좌표와 집행한 좌표는 월드 픽스처에서 비교한다.
                    game.hero.transform.position = new Vector2(12, 2);
                }
                if (!battle.Telegraphing) game.hero.transform.position = new Vector2(10, 1);
                if (battle.Telegraphing && battle.CurrentMove == OrbitalMove.ShutterFeint &&
                    battle.WindupProgress > .8f && AncientOrbitalRig.LensOpen) feintLens = true;

                if (inTell && !battle.Telegraphing && battle.CurrentMove == OrbitalMove.ShutterFeint)
                    feintActiveFrame = 0;
                else if (feintActiveFrame >= 0)
                {
                    float progress = ++feintActiveFrame * Frame / AncientOrbitalHazards.ActiveSeconds(OrbitalMove.ShutterFeint);
                    if (progress >= .94f) feintActiveFrame = -1;
                    else if (progress > .18f && progress < .7f)
                    {
                        Check(AncientOrbitalRig.LensOpen, "Closed physical shutter while the real shot was firing");
                        observedFiring = true;
                    }
                    else if (progress > .8f) Check(!AncientOrbitalRig.LensOpen, "Live lens remained on after shot");
                }

                if (battle.Recovering)
                {
                    if (!inOpening)
                    {
                        windows++;
                        openFrames = 0;
                        openedPhase = battle.Phase;
                        Check(windows == chains, "Missed counter window after a committed chain");
                    }
                    openFrames++;
                    if (openFrames == 3 && battle.Phase < 6 && (battle.Phase < 5 || phaseFiveFollowup))
                    {
                        float damage = battle.FilterDamage(100000, boss.Health, AncientEnemy.MaxHealth);
                        boss.Health -= damage;
                        battle.OnHit(boss.HealthRatio);
                        Check(boss.Health > 0, "A single hit defeated an earlier phase");
                    }
                    if (openFrames == 3 && battle.Phase == 6 && latchWindow && ordinaryWindow && moves.Count == 18)
                    {
                        Check(battle.FilterDamage(100000, boss.Health, AncientEnemy.MaxHealth) >= boss.Health,
                            "Completed final chain retained damage gate");
                        battle.BeginRetreat();
                        finalBlow = true;
                    }
                }
                if (inOpening && !battle.Recovering && !finalBlow)
                {
                    float seconds = openFrames * Frame;
                    bool standard = Math.Abs(seconds - AncientOrbitalRules.OpeningSeconds(openedPhase, false)) <= Frame + .0001f;
                    bool induced = openedPhase == 6 &&
                        Math.Abs(seconds - AncientOrbitalRules.OpeningSeconds(openedPhase, true)) <= Frame + .0001f;
                    Check(standard || induced, $"Counter window ended outside its reward duration, phase {openedPhase}, {seconds:F4}s");
                    if (openedPhase == 6) { ordinaryWindow |= standard; latchWindow |= induced; }
                }
                inTell = battle.Telegraphing;
                inOpening = battle.Recovering;
            }
            Check(finalBlow && phaseSeen == 6 && moves.Count == 18 && feintLens && observedFiring, "Incomplete fight coverage");
            Check(!battle.Active && battle.Arena == null && game.Orbital == null && arena.Released, "Leaked encounter");
            Check(arena.Announcements == arena.Executions && windows == chains, "Missing tell or counter window");
            Console.WriteLine($"PASS: {ticks} encounter ticks, {arena.Executions} frozen tells, {chains} chains and " +
                $"{windows} core openings, six phases, 18 attacks, pause, interruption and living retreat.");
        }
    }
}
