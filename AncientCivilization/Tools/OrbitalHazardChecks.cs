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
            AncientBossCue.Reset();
            world.hero.transform.position = hero;
            int initialHits = world.hero.Hits;
            hazards.Telegraph(move, origin, target, -1);
            for (int i = 0; i <= 10; i++) hazards.TickTell(i / 10f);
            Check(AncientBossCue.Events.Count > 0 && AncientBossCue.Events.All(e => e.Style == AncientBossCue.Style.Orbital &&
                e.Beat == AncientBossCue.Beat.Tell && e.Game == world), move + " emitted a strike early or missed its tell");
            Check(world.hero.Hits == initialHits, move + " dealt damage during its tell");
            Check(world.Lines.Any(line => line.enabled), move + " showed no tell");
            for (int i = 0; i <= frames; i++) hazards.TickActive((float)i / frames);
            Check(AncientBossCue.Events.Any(e => e.Beat == AncientBossCue.Beat.Strike), move + " missing discharge at the active source");
            int eventsAtFinish = AncientBossCue.Events.Count;
            int hits = world.hero.Hits;
            hazards.Clear();
            Check(world.Lines.All(line => !line.enabled), move + " retained a live stroke");
            hazards.TickActive(1);
            Check(world.hero.Hits == hits && eventsAtFinish == AncientBossCue.Events.Count, move + " dealt damage or lit a cue after cleanup");
            return hits - initialHits;
        }

        static void CueTimeline()
        {
            // 전조가 고정된 뒤 경위가 움직여도 실제 셔터는 75%에서만 반대로 열리고 발사는 12%에서 시작한다.
            var start = new Vector2(1,3);
            var target = new Vector2(5,3);
            world.hero.transform.position = new Vector2(0,14);
            AncientBossCue.Reset();
            hazards.Telegraph(OrbitalMove.ShutterFeint, start, target, -1);
            hazards.TickTell(.74f);
            Check(AncientBossCue.Events.Count == 1 && AncientBossCue.Events[0].Direction.x > 0, "feint leaked true firing side during false tell");
            hazards.TickTell(.75f);
            hazards.TickTell(.98f);
            Check(AncientBossCue.Events.Count == 2 && AncientBossCue.Events[1].Direction.x < 0, "shutter lens true tell once");
            hazards.TickActive(.119f);
            Check(AncientBossCue.Events.Count == 2, "shutter lit live cue before the gun became dangerous");
            hazards.TickActive(.12f);
            hazards.TickActive(.12f);
            var fired = AncientBossCue.Events.Last();
            Check(AncientBossCue.Events.Count == 3 && fired.Beat == AncientBossCue.Beat.Strike && fired.Direction.x < 0 &&
                  Vector2.Distance(fired.Point,start+Vector2.left*.64f)<.001f, "shutter strikes once at the real muzzle");

            AncientBossCue.Reset();
            hazards.Telegraph(OrbitalMove.ChainDrop, start, target, -1);
            hazards.TickActive(0);
            world.Paused = true;
            hazards.TickActive(.99f);
            Check(AncientBossCue.Events.Count == 2, "paused falling cargo left an impact");
            world.Paused = false;
            hazards.TickActive(1); hazards.TickActive(1);
            var landings = AncientBossCue.Events.Where(e => e.Beat == AncientBossCue.Beat.Contact).Select(e=>e.Point).ToArray();
            Check(landings.Length == 4 && landings.All(p => Math.Abs(p.y-.15f)<.001f) &&
                landings[0].x<landings[1].x && landings[1].x<landings[2].x && landings[2].x<landings[3].x,
                "a large frame step must retain all four ordered floor contacts");

            AncientBossCue.Reset();
            hazards.Telegraph(OrbitalMove.ReturnBlade, start, target, 1);
            hazards.TickActive(.46f);
            int before = AncientBossCue.Events.Count;
            hazards.TickActive(.48f); hazards.TickActive(.51f);
            var rim = AncientBossCue.Events.Last();
            Check(AncientBossCue.Events.Count == before+1 && rim.Beat == AncientBossCue.Beat.Contact &&
                Math.Abs(rim.Point.x+11.35f)<.001f && rim.Direction.x>0, "returning saw scrapes the actual opposite rail once");

            AncientBossCue.Reset();
            hazards.Telegraph(OrbitalMove.CargoSweep, start, target, 1);
            hazards.CargoPosition = new Vector2(6,5);
            hazards.TickActive(.02f);
            var cargo = AncientBossCue.Events.Last();
            Check(cargo.Beat == AncientBossCue.Beat.Strike && Vector2.Distance(cargo.Point,new Vector2(6,4.25f))<.001f,
                "moving freight emits from its current underside");
            hazards.Clear();
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

            CueTimeline();
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
