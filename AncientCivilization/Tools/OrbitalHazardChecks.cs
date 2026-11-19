using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class OrbitalTestHero : UnityEngine.Object
    {
        public readonly Transform transform = new Transform();
        public int Hits { get; private set; }
        public void Hurt(float damage) { if (damage > 0) Hits++; }
    }

    // 검사는 실제 패턴 사격선을 생성하고, 같은 피격 함수 호출을 관찰한다.
    public sealed class AncientGame : UnityEngine.Object
    {
        public readonly OrbitalTestHero hero = new OrbitalTestHero();
        public readonly List<LineRenderer> Lines = new List<LineRenderer>();
        public bool Paused;
        public LineRenderer Line(string name, Color color, float width, int order)
        {
            var stroke = new GameObject(name).AddComponent<LineRenderer>();
            Lines.Add(stroke);
            return stroke;
        }
    }

    public static class OrbitalHazardChecks
    {
        static int assertions;
        static readonly AncientGame world = new AncientGame();
        static readonly AncientOrbitalHazards hazards =
            new AncientOrbitalHazards(world, new GameObject("arena").transform, -12, 12);

        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            assertions++;
        }

        static int Probe(OrbitalMove move, Vector2 origin, Vector2 target, Vector2 hero, int frames)
        {
            world.hero.transform.position = hero;
            int initialHits = world.hero.Hits;
            hazards.Telegraph(move, origin, target, -1);
            for (int i = 0; i <= 10; i++) hazards.TickTell(i / 10f);
            Check(world.hero.Hits == initialHits, move + " dealt damage during its tell");
            Check(world.Lines.Any(line => line.enabled), move + " showed no tell");
            for (int i = 0; i <= frames; i++) hazards.TickActive((float)i / frames);
            int hits = world.hero.Hits;
            hazards.Clear();
            Check(world.Lines.All(line => !line.enabled), move + " retained a live stroke");
            hazards.TickActive(1);
            Check(world.hero.Hits == hits, move + " dealt damage after cleanup");
            return hits - initialHits;
        }

        public static void Main()
        {
            var centre = new Vector2(0, 1);
            var left = new Vector2(-3, 1.2f);
            var right = new Vector2(3, 1.2f);
            foreach (OrbitalMove move in Enum.GetValues(typeof(OrbitalMove)))
            {
                int coarse = Probe(move, left, centre, centre, 5);
                int fine = Probe(move, left, centre, centre, 110);
                Check(coarse == fine, move + " lost or repeated a hit at low frame rate");
                Check(Probe(move, left, centre, new Vector2(0, 14), 10) == 0, move + " hit beyond arena");
            }
            Check(Probe(OrbitalMove.ReturnBlade, left, centre, centre, 9) == 2, "outbound and returning blade");
            Check(Probe(OrbitalMove.ShutterFeint, centre, right, right, 9) == 0, "bait direction must stay safe");
            Check(Probe(OrbitalMove.ShutterFeint, centre, right, left, 9) == 1, "opposite shutter direction");
            Check(Probe(OrbitalMove.LandingStamp, centre, right, new Vector2(6, 1), 15) == 0, "stamp retargeted");
            Check(Probe(OrbitalMove.LandingStamp, centre, right, right, 15) == 1, "stamp repeated one hit");
            Check(Probe(OrbitalMove.ClosingSweep, centre, right, centre, 9) == 0, "missing centre pocket");
            Check(Probe(OrbitalMove.SplitShutter, centre, right, new Vector2(0, 1), 10) == 0, "low shutter route");
            Check(Probe(OrbitalMove.SplitShutter, centre, right, new Vector2(0, 6.6f), 10) == 0, "high shutter route");
            Check(Probe(OrbitalMove.SplitShutter, centre, right, new Vector2(0, 3.9f), 10) == 1, "shutter pinch");

            world.hero.transform.position = centre;
            int beforePause = world.hero.Hits;
            hazards.Telegraph(OrbitalMove.LandingStamp, centre, centre, -1);
            world.Paused = true;
            hazards.TickActive(.6f);
            Check(world.hero.Hits == beforePause, "paused stamp dealt damage");
            hazards.Dispose();
            Console.WriteLine("PASS: " + assertions + " orbital attack geometry, readable tells, sweep and cleanup checks.");
        }
    }
}
