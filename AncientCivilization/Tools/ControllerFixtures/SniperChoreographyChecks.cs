using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public static class Random
    {
        static readonly System.Random rng = new System.Random(17287);
        public static int Range(int min, int max) => rng.Next(min, max);
    }
}

namespace Plugin.AncientCivilization
{
    // 실제 연출 컨트롤러의 시계와 게임 경계만 대체한다. 물리와 렌더링은 Unity 검사에서 확인한다.
    public sealed class AncientEnemy : Component
    {
        public bool IsAlive = true;
        public Vector2 AimPoint => (Vector2)transform.position + Vector2.up;
        public Vector2 SniperMuzzle => AimPoint;
        public void FaceToward(float x) { }
        public void BossVisual(int pose, float face, float progress, bool aiming, bool recovering) { }
    }

    public sealed class AncientGame : Component
    {
        public AncientEnemy hero = new AncientEnemy(), boss;
        public AncientSniperEncounter Sniper;
        public bool Paused;
        public int Impacts;
        public static Color Cyan => Color.white;
        public void Burst(Vector2 point, Color color, int count) { }
        public void Impact(Vector2 point, Color color, bool heavy) { Impacts++; }
    }

    public sealed class AncientJokerArena
    {
        public int SupportedLanes { get; set; } = 7;
        public bool Foldable = true;
        public int MarkedSide = -1, Rings, Folds;
        public Vector2 RailPoint(int side) => new Vector2(side == 0 ? 4 : 26, 6);
        public Vector2 LandingPoint(int side) => new Vector2(side == 0 ? 6 : 24, 0);
        public void ClearReturn() { MarkedSide = -1; }
        public void MarkReturn(int side) { MarkedSide = side; }
        public void SetRing(Vector2 point) { Rings++; }
        public void CloseRing() { }
        public void FixedTick(float dt) { }
        public bool CanFold(int side, float x) => Foldable;
        public void BeginFold(int side, int lane) { Folds++; }
    }

    public sealed class AncientJokerAttacks
    {
        float ends;
        public int Lasers, Bounces;
        public readonly List<Vector2> Rings = new List<Vector2>();
        public bool Busy => Time.time < ends;
        public bool Telegraphing => Busy;
        public string Hint => "";
        public float Duration => .2f;
        public float WindupProgress => 0;
        public void Mirror(Vector2 origin, Vector2 hero, int count, int safeLane) { Bounces += count; ends = Time.time + Duration; }
        public int MirrorSafeLanes(Vector2 origin, Vector2 target, int count) => 7;
        public void Laser(bool down, int lane = -1) { Lasers++; ends = Time.time + Duration; }
        public void Ring(Vector2 origin) { Rings.Add(origin); ends = Time.time + Duration; }
    }

    public sealed partial class AncientSniperEncounter
    {
        sealed class Actor
        {
            public LineRenderer tell = new LineRenderer();
            public AncientEnemy enemy;
            public int state, shots, grenades, magazine;
            public bool grenadeUsed, duelUsed, lateLock, coveringPlate, ejected;
            public float began, flash, until;
        }

        readonly AncientEnemy owner = new AncientEnemy();
        readonly AncientGame game = new AncientGame();
        readonly Actor[] actors = { new Actor(), new Actor(), new Actor(), new Actor() };
        static readonly string[] phaseHints = new string[7];
        float left = 0, right = 30;
        int phase;
        bool finished = false;
        public bool Recovering => relay == RelayBeat.Opening;
        void SpawnClone(int slot, Vector2 position)
        {
            actors[slot].enemy = new AncientEnemy();
            actors[slot].enemy.transform.position = position;
        }

        static AncientSniperEncounter Scene(int phase)
        {
            Time.time = 0;
            var e = new AncientSniperEncounter { phase = phase, arena = new AncientJokerArena(), attacks = new AncientJokerAttacks() };
            e.actors[0].enemy = e.owner;
            e.game.boss = e.owner;
            e.game.Sniper = e;
            e.game.hero.transform.position = new Vector2(15, 1.08f);
            return e;
        }

        void AdvanceRelay()
        {
            Time.time += .04f;
            TickRelay();
        }

        public static void CheckChoreography(Action<bool, string> check)
        {
            var e = Scene(2);
            e.realSide = 1;
            e.owner.transform.position = e.arena.RailPoint(1);
            Vector2 oldBody = e.owner.transform.position, oldCopy = e.arena.LandingPoint(0);
            e.SpawnClone(1, oldCopy);
            e.inBackground = true;
            e.actors[1].state = 1;
            check(!e.TickSwap() && !e.swapping, "A pending clone bullet keeps its warning across a swap deadline");
            e.actors[1].state = 0;
            check(e.TickSwap() && e.arena.MarkedSide == 0 && !e.CanWireOwner, "Hat shadow marks the genuine foreground destination before swap");
            Time.time += .7f;
            e.TickSwap();
            check(e.CanWireOwner && Vector2.Distance(e.owner.transform.position, oldCopy) < .01f
                && Vector2.Distance(e.actors[1].enemy.transform.position, oldBody) < .01f, "Diamond changes the depth and sides of both shooters");
            check(e.attacks.Rings.Count == 1 && Math.Abs(e.attacks.Rings[0].x - oldCopy.x) < .01f, "Energy ring originates at the vacated clone");

            e = Scene(4);
            e.StartRailRun();
            check(!e.CanWireOwner, "The background runner cannot be hooked");
            int openings = 0;
            bool sawHeart = false;
            for (int i = 0; i < 550 && openings < 3; i++)
            {
                bool previouslyOpen = e.Recovering;
                e.AdvanceRelay();
                if (!sawHeart && e.arena.Rings > 0)
                {
                    e.OnWireImpact();
                    e.OnWireImpact();
                    sawHeart = e.airCatch && e.game.Impacts == 1;
                }
                if (!previouslyOpen && e.Recovering)
                {
                    check(e.CanWireOwner && !e.airBladeHit, "Each landing offers a fresh foreground punish");
                    openings++;
                }
            }
            check(sawHeart && openings == 3 && e.attacks.Lasers >= 1, "Relay repeats: catch window, landing, normal sweep and return");

            e = Scene(5);
            e.StartRailRun();
            var signatures = new HashSet<int>();
            var openers = new Dictionary<JokerMove,HashSet<JokerMove>>();
            int completed = 0, previous = 0;
            for (int i = 0; i < 20000 && completed < 70; i++)
            {
                bool previouslyOpen = e.Recovering;
                e.AdvanceRelay();
                if (previouslyOpen || !e.Recovering) continue;
                int length = e.LastFullHouseLength;
                check(length >= 2 && length <= 3 && e.chainStep == length, "Every selected Full House plays all cards before a punish");
                check(e.LastFullHouseSignature != previous && e.CanWireOwner, "Successive playable sequences differ and give a foreground punish");
                var first = e.chain.At(0);
                if (!openers.TryGetValue(first,out var endings)) openers[first] = endings = new HashSet<JokerMove>();
                endings.Add(e.chain.At(length-1));
                signatures.Add(previous = e.LastFullHouseSignature);
                completed++;
            }
            check(completed == 70 && signatures.Count >= 8 && openers[JokerMove.DoubleRicochet].Count > 1,
                "A shared opener changes endings across repeated scheduler turns");
            check(e.attacks.Bounces > 0 && e.attacks.Lasers > 0 && e.arena.Folds > 0, "Scheduler dispatches actual mirror, sweep and fold operations");

            e = Scene(5);
            e.arena.SupportedLanes = 0;
            e.StartRailRun();
            for (int i = 0; i < 80 && !e.Recovering; i++) e.AdvanceRelay();
            check(e.Recovering && e.CanWireOwner && e.LastFullHouseLength == 0 && e.attacks.Bounces == 0 && e.attacks.Lasers == 0,
                "No valid escape yields a free return and the duel can continue");
        }
    }
}
