using System;
using System.Reflection;
using Plugin.PlayerCore;
using UnityEngine;
namespace Plugin.PlayerCore
{
    // The real controller is covered by its own assembly with keyboard and cast fixtures.
    public sealed class PlayerBluePlug:MonoBehaviour
    {public bool HazardInvulnerable {get;set;}}
}
class FloorMechanismChecks
{
    static int checks;
    static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);checks++;}
    static void Tick(TraversalFloorMechanism item,int times=1){for(int i=0;i<times;i++)typeof(TraversalFloorMechanism).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(item,null);}
    sealed class Fixture
    {
        public readonly PlayerStateMachine player;public readonly PlayerInputReader input;public readonly Rigidbody2D body;public readonly TraversalRhythmPractice lab;public readonly TraversalFloorMechanism mechanism;public readonly FloorTraversalProfile profile;public readonly PlayerBluePlug plug;
        public Fixture(TraversalFloor floor,bool dangerous=true)
        {
            Application.isFocused=true;Time.fixedDeltaTime=.02f;
            var root=new GameObject("Lab");lab=root.AddComponent<TraversalRhythmPractice>();var hero=new GameObject("Hero");body=hero.AddComponent<Rigidbody2D>();input=hero.AddComponent<PlayerInputReader>();player=hero.AddComponent<PlayerStateMachine>();plug=hero.AddComponent<PlayerBluePlug>();body.position=new Vector2(0,2);
            var go=new GameObject("Mechanism");go.transform.SetParent(root.transform,false);go.AddComponent<SpriteRenderer>();if(floor==TraversalFloor.Administration)go.AddComponent<BoxCollider2D>();
            profile=new FloorTraversalProfile{floor=floor};mechanism=go.AddComponent<TraversalFloorMechanism>();mechanism.Initialize(profile,input,dangerous);
        }
    }
    static void Main()
    {
        foreach(bool dangerous in new[]{false,true})
        {
            var f=new Fixture(TraversalFloor.Living,dangerous);f.profile.scanPeriod=10000;Tick(f.mechanism,20);float exposure=f.mechanism.Exposure;
            Check(exposure>.39f&&f.lab.Retries==0,"Searchlights give a visible reaction window instead of instant damage");
            f.input.Paused=true;Tick(f.mechanism,100);Check(f.mechanism.Exposure==exposure&&f.lab.Retries==0,"Pause does not accumulate searchlight exposure");f.input.Paused=false;
            Tick(f.mechanism,20);Check((f.lab.Retries>0)==dangerous,"The teaching room warns harmlessly; only the challenge room retries on sustained exposure");
            f.body.position=new Vector2(10,2);Tick(f.mechanism);Check(f.mechanism.Exposure==0,"Leaving the visible beam clears its accumulation");f.mechanism.ResetForRetry();Check(f.mechanism.Exposure==0,"Retry clears scan state");
        }
        {
            var f=new Fixture(TraversalFloor.Living);f.profile.scanPeriod=10000;
            f.plug.HazardInvulnerable=true;Tick(f.mechanism,40);
            Check(f.mechanism.Exposure==0&&f.lab.Retries==0,"A powered straight tow crosses the practice searchlight safely");
            f.plug.HazardInvulnerable=false;Tick(f.mechanism,40);
            Check(f.lab.Retries>0,"Searchlight exposure resumes when the straight tow ends");
        }
        {
            var f=new Fixture(TraversalFloor.Administration);Tick(f.mechanism,100);Check(f.mechanism.GateOpen&&!f.mechanism.GetComponent<BoxCollider2D>().enabled,"A closing shutter defers while the player occupies its volume");
            f.body.position=new Vector2(5,2);Tick(f.mechanism);Check(!f.mechanism.GateOpen&&f.mechanism.GetComponent<BoxCollider2D>().enabled,"Once clear, the closed phase restores the physical blocker");
            f.mechanism.ResetForRetry();Check(f.mechanism.GateOpen&&!f.mechanism.GetComponent<BoxCollider2D>().enabled,"Retry restores a predictable initial opening");
        }
        foreach(PlayerStateId state in new[]{PlayerStateId.Fall,PlayerStateId.Dash,PlayerStateId.GrappleDash,PlayerStateId.Hurt,PlayerStateId.Dead})
        {
            var f=new Fixture(TraversalFloor.Residence);f.body.position=new Vector2(0,3);f.player.State=state;f.player.Velocity=new Vector2(3,-5);Tick(f.mechanism);
            Check(f.player.LiftRequested==(state==PlayerStateId.Fall?12:0)&&f.player.Velocity.y==-5,"Vent lift affects ordinary movement without bending dash/grapple or overriding hurt/dead states");
        }
        {
            var f=new Fixture(TraversalFloor.Top);var go=new GameObject("Socket");var anchor=go.AddComponent<FixedGrappleAnchor>();anchor.data=new GrappleAnchorData();typeof(GrappleAnchor).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(anchor,null);
            f.mechanism.socket=anchor;f.mechanism.ResetForRetry();Check(anchor.Available&&anchor.TryAcquire(),"A timed socket begins available and can be reserved");Tick(f.mechanism,200);Check(anchor.Leased&&anchor.Operational,"The activation window cannot vanish under a leased grapple");anchor.Release(false);Tick(f.mechanism,125);Check(!anchor.Available&&!anchor.Operational,"Unleased sockets become unavailable during the closed phase");anchor.ResetForRetry();f.mechanism.ResetForRetry();Check(anchor.Available,"Checkpoint retry restores both availability and phase");
        }
        Console.WriteLine("PASS: "+checks+" shipping floor-mechanism checks with service doubles; native illumination/collision/vent motion not simulated");
    }
}
