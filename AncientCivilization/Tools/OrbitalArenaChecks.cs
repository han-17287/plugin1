using System;
using System.Reflection;
using Plugin.AncientCivilization;
using UnityEngine;

static class OrbitalArenaChecks
{
    static readonly AncientGame game=new AncientGame();
    static AncientOrbitalArena arena;
    static int checks;
    static AncientHero Hero => game.hero;
    static Vector2 Anchor(int offset) => game.anchors[game.OrbitalAnchorSlot+offset];
    static void Check(bool condition,string label)
    {
        if (!condition) throw new Exception(label);
        checks++;
    }
    static void Advance(float seconds)
    {
        for (int i=0;i<(int)Math.Ceiling(seconds*100);i++) arena.Tick(.01f);
    }
    static T ArenaField<T>(string name)
    {
        return (T)typeof(AncientOrbitalArena).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(arena);
    }
    static void Start(OrbitalMove move,Vector2 target,int side=1)
    {
        arena.PrepareMove(move,side,target);
        arena.BeginMove(move,side,target);
    }

    static void Anchors()
    {
        Hero.transform.position=new Vector2(2,1);
        Check(arena.IsAnchorReachable(-1,Hero.transform.position) && !arena.IsAnchorReachable(1,Hero.transform.position),"Far anchor is outside real wire range");
        Check(!arena.IsAnchorReachable(-1,arena.FixedAnchorPoint(-1)-Vector2.up*.35f),"An anchor already under the hand cannot provide a new departure");
        Physics2D.Blocked=true;
        Physics2D.HitPoint=new Vector2(2,3);
        Check(!arena.IsAnchorReachable(-1,Hero.transform.position),"Geometry between hero and anchor");
        Physics2D.Blocked=false;
        Start(OrbitalMove.AnchorLock,arena.FixedAnchorPoint(-1),-1);
        Check(!arena.LeftAnchorAvailable && arena.RightAnchorAvailable && Anchor(0).x<0,"Only the selected anchor disappears");
        Check(Vector2.Distance(Hero.ReleasedAnchor,arena.FixedAnchorPoint(-1))<.01f,"Already attached player releases the locked ring");
        Start(OrbitalMove.AnchorLock,arena.FixedAnchorPoint(1));
        Check(!arena.LeftAnchorAvailable && arena.RightAnchorAvailable,"Both rings cannot be locked at once");
        arena.OpenCounter();
        Check(arena.LeftAnchorAvailable && arena.RightAnchorAvailable,"Both anchors return for the opening");
    }
    static void LandingPlatforms()
    {
        AncientBossCue.Reset();
        var target=new Vector2(12,0);
        Start(OrbitalMove.DropCargo,target);
        Check(arena.UnsafeBladeGround(target) && !arena.UnsafeBladeGround(target+Vector2.left*4),"Blade summon excludes the falling cargo footprint");
        var hook=Anchor(3);
        Check(hook.y>8 && arena.TryAttach(hook,out _),"Hook exists on falling cargo");
        Check(!arena.TryAttach(hook,out _) && Anchor(3).x<0,"One-use hook spent only once");
        Advance(.70f);
        var crates=ArenaField<GameObject[]>("crates");
        var old=crates[0];
        Check(old.activeSelf && old.GetComponent<BoxCollider2D>().enabled && Math.Abs(old.transform.position.y-.48f)<.001f,"Landed platform is solid");
        Start(OrbitalMove.CargoRush,new Vector2(21,0));
        Check(old.activeSelf && old.transform.position.x==12 && crates[1].transform.position.x==21 && Anchor(3).x==21,"Chained rush keeps previously landed platform");
        Advance(3.65f);
        Check(old.activeSelf,"Original remains underfoot until its own expiry");
        Advance(.38f);
        Check(!old.activeSelf && crates[1].activeSelf,"Independent landing expiry");
        Advance(.85f);
        Check(!crates[1].activeSelf,"Second platform expires");
        Start(OrbitalMove.DropCargo,target);
        Hero.transform.position=new Vector2(12,1);
        Advance(.72f);
        Check(Hero.HurtCalls==1,"No repeat damage from the same falling box");
        int groundImpacts=0, bodyImpacts=0;
        foreach(var cue in AncientBossCue.Events)
        {
            if (cue.Beat!=AncientBossCue.Beat.Contact || cue.Style!=AncientBossCue.Style.Orbital) continue;
            if (cue.Direction.y>.9f) groundImpacts++;
            if (cue.Direction.y<-.9f) bodyImpacts++;
        }
        Check(groundImpacts==3 && bodyImpacts==1,"Three landed crates and the single physical body hit each light once");
        Hero.transform.position=new Vector2(2,1);
        Advance(5);
    }
    static void FreightAndMachinery()
    {
        var p=arena.DispenserPoint;
        Check(arena.CutSupply(p+Vector2.left,Vector2.right) && !arena.CutSupply(p+Vector2.left,Vector2.right) && Hero.Combat.Emissions==1,"Breaking supply yields once");
        Advance(7.1f);
        arena.BreakSupplyInRadius(p+Vector2.right*4,1);
        arena.BreakSupplyInRadius(p+Vector2.left,2);
        Check(Hero.Combat.Emissions==2 && !arena.CutSupply(p+Vector2.left,Vector2.right),"Recharged supply breaks once in the blade burst");
        Check(arena.LockHit(arena.LatchPoint) && !arena.LockHit(arena.LatchPoint+Vector2.right*3),"Stamped latch selects long opening");
        Start(OrbitalMove.ReverseRail,default,-1);
        var rail=ArenaField<AncientOrbitalFreight>("rail");
        var tick=typeof(AncientOrbitalFreight).GetMethod("FixedUpdate",BindingFlags.NonPublic|BindingFlags.Instance);
        void Step(int count){for(int i=0;i<count;i++)tick.Invoke(rail,null);}
        float x=arena.CargoPoint.x;
        Step(1);
        Check(arena.CargoPoint.x<x,"Reversed freight moves on fixed ticks");
        game.Paused=true;
        x=arena.CargoPoint.x;
        Step(1);
        Check(arena.CargoPoint.x==x,"No movement during pause");
        game.Paused=false;
        rail.transform.position=new Vector2(3.21f,arena.CargoPoint.y);
        arena.PrepareMove(OrbitalMove.ReverseRail,1,default);
        x=arena.CargoPoint.x;
        Step(70);
        Check(arena.CargoPoint.x==x && arena.RailDirection==-1,"Rail brakes through reversal tell even at its endpoint");
        arena.BeginMove(OrbitalMove.ReverseRail,1,default);
        Step(1);
        Check(arena.CargoPoint.x>x && arena.RailDirection==1,"Rail leaves in its announced direction");
    }
    static void Main()
    {
        game.anchors.Add(new Vector2(33,9));
        arena=new AncientOrbitalArena(game,null,0,32);
        Anchors();
        LandingPlatforms();

        FreightAndMachinery();
        arena.Dispose();
        Check(game.anchors[0].x==33 && Anchor(0).x<0 && Anchor(3).x<0,"Cleanup preserves original anchors");
        arena=new AncientOrbitalArena(game,null,0,32);
        Check(game.anchors.Count==5 && arena.LeftAnchorAvailable,"Retries reuse the same anchor slots");
        Console.WriteLine($"Orbital arena passed {checks} checks.");
    }
}
