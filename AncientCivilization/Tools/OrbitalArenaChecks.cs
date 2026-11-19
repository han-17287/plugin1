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
        var target=new Vector2(12,0);
        Start(OrbitalMove.DropCargo,target);
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
        Hero.transform.position=new Vector2(2,1);
        Advance(5);
    }
    static Vector2 ThrowAndStick()
    {
        Hero.transform.position=new Vector2(2,1);
        arena.ThrowBlade();
        Check(arena.BladeAway && !arena.BladeStuck,"Blade enters flight");
        Advance(1);
        Check(arena.BladeStuck,"Blade embeds in freight");
        return Anchor(4);
    }
    static void Recovery()
    {
        Vector2 socket=ThrowAndStick();
        Hero.transform.position=socket-Vector2.up*.26f;
        Advance(.01f);
        Check(!arena.BladeAway && Anchor(4).x<0,"Direct contact equips blade and removes anchor");
        socket=ThrowAndStick();
        Check(arena.TryAttach(socket,out var support) && support,"Stuck blade offers moving wire support");
        Hero.transform.position=socket+Vector2.left*1.5f;
        Advance(.01f);
        Check(!arena.BladeAway,"Wire arrival recovers blade before hull collides");
        socket=ThrowAndStick();
        var near=new AncientEnemy{AimPoint=socket+Vector2.up};
        var far=new AncientEnemy{AimPoint=socket+Vector2.up*10};
        game.enemies.Add(near);
        game.enemies.Add(far);
        Hero.CaptureEnergy=9;
        Input.Recall=true;
        arena.Tick(.01f);
        Check(arena.BladeStuck && Hero.CaptureEnergy==9,"Below minimum no charge is spent");
        Hero.CaptureEnergy=45;
        arena.Tick(.01f);
        Input.Recall=false;
        Check(!arena.BladeAway && Hero.CaptureEnergy==0 && near.Received==28 && far.Received==0 && Hero.HurtCalls==1,"Remote recovery consumes all, applies local damage, spares hero");
        ThrowAndStick();
        Hero.CaptureEnergy=50;
        Input.Recall=true;
        arena.Tick(.01f);
        Input.Recall=false;
        Check(arena.BladeStuck && Hero.CaptureEnergy==50,"A second throw respects explosion cooldown");
        Advance(5.2f);
        Input.Recall=true;
        arena.Tick(.01f);
        Input.Recall=false;
        Check(!arena.BladeAway && Hero.CaptureEnergy==0,"Explosion usable again after six seconds");
    }
    static void FreightAndMachinery()
    {
        var p=arena.DispenserPoint;
        Check(arena.CutSupply(p+Vector2.left,Vector2.right) && !arena.CutSupply(p+Vector2.left,Vector2.right) && Hero.Combat.Emissions==1,"Breaking supply yields once");
        Advance(7.1f);
        Check(arena.CutSupply(p+Vector2.left,Vector2.right),"Dispenser recharges");
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
        Start(OrbitalMove.BladeFerry,default,1);
        Step(100);
        Check(arena.CargoPoint.x>26 && arena.RailDirection==-1,"Ferry crosses entire lane and reverses at the far end");
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
        Recovery();
        FreightAndMachinery();
        arena.Dispose();
        Check(game.anchors[0].x==33 && Anchor(0).x<0 && Anchor(4).x<0,"Cleanup preserves original anchors");
        arena=new AncientOrbitalArena(game,null,0,32);
        Check(game.anchors.Count==6 && arena.LeftAnchorAvailable,"Retries reuse the same anchor slots");
        Console.WriteLine($"Orbital arena passed {checks} checks.");
    }
}
