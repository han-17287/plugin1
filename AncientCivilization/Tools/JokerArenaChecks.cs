using System;
using System.Collections.Generic;
using System.Reflection;
using Plugin.AncientCivilization;
using UnityEngine;

static class JokerArenaChecks
{
    static int count, rays;
    static bool gap;
    static readonly AncientGame game = new AncientGame();
    static AncientJokerArena arena;
    static int Slot => game.JokerAnchorSlot;

    static void Check(bool pass, string why)
    {
        if (!pass) throw new Exception(why);
        count++;
    }

    static T Field<T>(object obj, string field)
    {
        return (T)obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(obj);
    }

    static object Panel(int index) => Field<Array>(arena,"panels").GetValue(index);
    static BoxCollider2D Solid(int i) => Field<BoxCollider2D>(Panel(i),"solid");
    static Rigidbody2D Body(int i) => Field<Rigidbody2D>(Panel(i),"body");

    static void Step(float seconds)
    {
        int frames=(int)Math.Ceiling(seconds*100);
        Time.deltaTime = seconds/frames;
        for (int i=0; i<frames; i++)
        {
            Time.frameCount++;
            arena.Tick();
            arena.FixedTick(Time.deltaTime);
        }
    }

    static bool Floor(Vector2 p, Vector2 direction, float distance, int mask)
    {
        rays++;
        return mask==(1<<8) && direction.y<0 && p.y>0 && distance>p.y && (!gap || p.x<7 || p.x>8.5f);
    }

    static void Main()
    {
        Physics2D.RayHit=Floor;
        game.anchors.Add(new Vector2(0,9));
        game.hero.transform.position=new Vector2(9,1.08f);
        arena=new AncientJokerArena(game,game.transform,0,30);
        game.Sniper.Arena=arena;
        Check(game.anchors.Count==2 && game.anchors[Slot].x < -1000,"Exactly one dormant wire slot");
        var mirrors=Field<SpriteRenderer[]>(arena,"mirrors");
        var cold=mirrors[0].sprite;
        arena.SetMirrors(1,1);
        Check(mirrors[1].sprite!=cold && mirrors[0].sprite==cold,"Requested mirror illuminates alone");
        arena.SetMirrors(1,2);
        Check(mirrors[0].sprite==mirrors[1].sprite,"Two bounces light both mirrors");

        arena.SetPhase(2);
        arena.MarkReturn(1);
        var shadow=Field<SpriteRenderer>(arena,"shadow");
        Check(shadow.enabled && Math.Abs(shadow.transform.position.x-arena.LandingPoint(1).x)<.001f,
            "Authentic hat marks the exact future landing");
        Check(arena.RailPoint(1).y>arena.LandingPoint(1).y && !Solid(0).enabled,"Layered rear rail and shutter roof");
        arena.SetPhase(3);
        Check(!shadow.enabled && mirrors[0].sprite==cold && mirrors[1].sprite==cold,"Phase change cleans tell art");
        var a=Body(1);var b=Body(4);
        float x=a.position.x,y=b.position.x;
        arena.BeginRewind();
        Step(.34f);
        Check(a.position.x==x && b.position.x==y,"Rewind warning leads its physical displacement");
        Step(.93f);
        Check(a.position.x>x+.1f && b.position.x<y-.1f && Solid(1).enabled && Solid(4).enabled,"Ridable kinematic roofs move opposite");
        game.Paused=true;
        float frozen=a.position.x;
        Step(.55f);
        Check(a.position.x==frozen,"Pause freezes moving decks");
        game.Paused=false;
        Step(3);
        Check(Math.Abs(a.position.x-x)<.001f && Math.Abs(b.position.x-y)<.001f,"Rewind returns to staging");

        arena.SetPhase(4);
        var airborne=new Vector2(20,6);
        arena.MarkReturn(1);
        arena.SetRing(airborne);
        Check(arena.TryAttach(airborne,out var ring) && !arena.TryAttach(airborne+Vector2.up,out _),"Precise ring accepts real grapple");
        arena.SetRing(airborne+Vector2.left);
        Check((game.anchors[Slot]-(Vector2)ring.position).sqrMagnitude<.0001f,"Wire target tracks changing position");
        arena.CloseRing();
        Check(!arena.TryAttach(ring.position,out _) && shadow.enabled,"Air opportunity expires before the landing tell");
        arena.ClearReturn();
        Check(!arena.TryAttach(ring.position,out _) && !ring.gameObject.activeSelf &&
            game.anchors[Slot].x<-1000 && !shadow.enabled,"Clearing a return releases physical grapple and hat");

        arena.SetPhase(5);
        Check(arena.SupportedLanes==7 && arena.CanFold(0,9),"Continuous lower route exists");
        int firstReads=rays;
        Check(arena.SupportedLanes==7 && arena.CanFold(1,22) && rays==firstReads,"One ground survey per decision frame");
        gap=true;Time.frameCount++;
        Check(!arena.CanFold(1,9),"Real floor gap vetoes fold");
        gap=false;Time.frameCount++;
        arena.BeginFold(0,1);
        Step(.58f);
        Check(!Solid(0).enabled && Solid(1).enabled,"Folds start separately");
        Step(1.05f);
        Check(!Solid(0).enabled && !Solid(1).enabled && Solid(2).enabled && Solid(3).enabled && !Solid(4).enabled && !Solid(5).enabled,
            "Protected route remains while four other roofs fold");
        game.hero.transform.position=Body(0).position;
        Step(3.5f);
        Check(!Solid(0).enabled && Solid(1).enabled,"Recovery avoids rematerializing a roof in hero");
        game.hero.transform.position=new Vector2(20,1.08f);
        Step(.03f);
        Check(Solid(0).enabled,"Vacated roof restores collision");
        arena.BeginFold(1,0);Step(.64f);
        Check(!Solid(5).enabled && Solid(4).enabled && Solid(0).enabled && Solid(1).enabled,"Opposite collapse preserves selected left escape");
        arena.SetPhase(6);
        for (int i=0; i<6; i++) Check(!Solid(i).enabled && !Body(i).gameObject.activeSelf,"Finale clears roof "+i);
        arena.Dispose();
        var fresh = new AncientJokerArena(game,game.transform,0,30);
        game.Sniper.Arena=fresh;fresh.SetPhase(4);fresh.SetRing(airborne);
        Check(game.anchors.Count==2 && fresh.TryAttach(airborne,out _),"Retry reuses the registered slot");
        fresh.Dispose();fresh.Dispose();
        Check(game.anchors[Slot].x<-1000,"Disposal is idempotent");
        Console.WriteLine(count+" Joker arena checks passed");
    }
}

namespace UnityEngine
{
    public struct Rect { public Rect(float x,float y,float w,float h) {} }
    public class PlatformEffector2D : Component { public bool useOneWay; public float surfaceArc; }
}

namespace Plugin.AncientCivilization
{
    public class AncientSniperEncounter : Component { public AncientJokerArena Arena; }
    public class AncientHero : Component { public void ReleaseWireAt(Vector2 point) {} }
    public class AncientGame : Component
    {
        public int JokerAnchorSlot=-1;
        public bool Paused;
        public readonly List<Vector2> anchors=new List<Vector2>();
        public readonly AncientSniperEncounter Sniper=new AncientSniperEncounter();
        public readonly AncientHero hero=new AncientHero();
        public GameObject Box(string name,Vector2 pos,Vector2 size,Color color,int order)
        {
            var part=new GameObject(name);
            part.transform.position=pos; part.AddComponent<SpriteRenderer>().color=color;
            return part;
        }
    }
    public static class AncientArt
    {
        public static Sprite Slice(string sheet,string name,Rect frame,float ppu,Vector2 pivot,bool trim) => new Sprite();
        public static SpriteRenderer Display(string name,Sprite sprite,Vector2 point,int order,Transform parent)
        {
            var obj=new GameObject(name);
            obj.transform.SetParent(parent,false);obj.transform.localPosition=point;
            var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=sprite;
            return renderer;
        }
    }
}
