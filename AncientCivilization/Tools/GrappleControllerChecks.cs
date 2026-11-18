using System;
using System.Reflection;
using Plugin.PlayerCore;
using UnityEngine;
class GrappleControllerChecks
{
    static int checks;
    static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);checks++;}
    static void Call(object o,string method){o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);}
    sealed class Fixture
    {
        public readonly GameObject root=new GameObject("Hero");public readonly PlayerStateMachine player;public readonly PlayerGrapple grapple;public readonly PlayerInputReader input;public readonly PlayerModeController mode;public readonly Rigidbody2D body;
        public Fixture(){typeof(GrappleAnchor).GetMethod("ClearRegistry",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);Physics2D.WallX=float.NaN;Application.isFocused=true;Time.time=0;body=root.AddComponent<Rigidbody2D>();root.AddComponent<CapsuleCollider2D>();mode=root.AddComponent<PlayerModeController>();input=root.AddComponent<PlayerInputReader>();player=root.AddComponent<PlayerStateMachine>();grapple=root.AddComponent<PlayerGrapple>();grapple.data=new GrappleData();Call(grapple,"Awake");Call(grapple,"OnEnable");}
        public T Anchor<T>(Vector2 point)where T:GrappleAnchor,new(){var go=new GameObject("Anchor");go.transform.position=point;var a=go.AddComponent<T>();a.data=new GrappleAnchorData();a.clockOwner=input;Call(a,"OnEnable");return a;}
    }
    static void Main()
    {
        foreach(int hz in new[]{30,50,60,120,240})foreach(int kind in new[]{0,1,2})
        {
            var f=new Fixture();GrappleAnchor a=kind==0?f.Anchor<FixedGrappleAnchor>(new Vector2(10,4)):kind==1?f.Anchor<OneUseGrappleAnchor>(new Vector2(10,4)):f.Anchor<MovingGrappleAnchor>(new Vector2(10,4));
            Vector2 endpoint=a.Point;Check(f.grapple.TryStartAt(a)&&a.Leased,"A real controller start reserves the selected anchor");int frames=0;
            while(f.grapple.Active&&frames++<hz*2)
            {
                if(kind==2)Call(a,"FixedUpdate");Check((a.Point-endpoint).sqrMagnitude<.0001f,"The leased moving target cannot turn a straight shot into pursuit");
                f.player.Tick(1f/hz);Vector2 p=f.body.position;Check(Math.Abs(p.x*endpoint.y-p.y*endpoint.x)<.001f,"Each shipping-controller step remains on the captured straight line");
            }
            Check(!f.grapple.Active&&f.grapple.ZipStrikeReady&&f.player.ArrivalJumpAvailable&&f.player.Dashes==1,"Successful arrival grants the integration's strike window and airborne reset");
            Check(!a.Leased&&a.Spent==(kind==1),"Only a successfully reached one-use anchor is spent");
            a.ResetForRetry();Check(a.Available&&!a.Spent,"Retry restores every anchor kind");
        }
        {
            var f=new Fixture();var a=f.Anchor<OneUseGrappleAnchor>(new Vector2(10,0));Check(f.grapple.TryStartAt(a),"Clear shot begins");Physics2D.WallX=5;
            for(int n=0;n<100&&f.grapple.Active;n++)f.player.Tick(.02f);
            Check(f.body.position.x<5&&!a.Spent&&a.Available&&!f.grapple.ZipStrikeReady,"A wall introduced during flight cancels before crossing, without spending the anchor or granting arrival bonuses");
        }
        foreach(bool dead in new[]{false,true})
        {
            var f=new Fixture();var a=f.Anchor<OneUseGrappleAnchor>(new Vector2(8,4));f.grapple.TryStartAt(a);if(dead)f.player.Die();else f.player.Hurt();
            Check(!f.grapple.Active&&!a.Leased&&a.Available,"Hurt/death synchronously releases the active target");
            Check(f.player.State==(dead?PlayerStateId.Dead:PlayerStateId.Hurt)&&!f.grapple.ZipStrikeReady,"Cancellation does not replace a higher-priority state");
        }
        {
            var f=new Fixture();var a=f.Anchor<FixedGrappleAnchor>(new Vector2(10,4));f.grapple.TryStartAt(a);f.player.Tick(.02f);f.player.Tick(.02f);Vector2 paused=f.body.position;
            f.input.Paused=true;for(int i=0;i<20;i++)f.player.Tick(.1f);Check((f.body.position-paused).sqrMagnitude==0&&a.Leased,"Pause keeps the exact flight and lease frozen");f.input.Paused=false;
            a.enabled=false;f.player.Tick(.02f);Check(!f.grapple.Active&&!a.Leased&&!f.grapple.ZipStrikeReady,"Disabling an in-flight target cancels without success");
        }
        foreach(bool allow in new[]{false,true})
        {
            var f=new Fixture();var a=f.Anchor<FixedGrappleAnchor>(new Vector2(10,4));f.grapple.data.allowModeCancel=allow;f.grapple.TryStartAt(a);f.mode.Set(PlayerMode.Attack);
            Check(f.grapple.Active==!allow,"Mode change follows the configured cancellation policy");f.grapple.ResetForRetry();Check(!f.grapple.Active&&!a.Leased,"Room/reset cleanup releases either mode policy");
        }
        {
            var f=new Fixture();var a=f.Anchor<FixedGrappleAnchor>(new Vector2(15,0));Check(!f.grapple.TryStartAt(a)&&a.Available,"Out-of-range input cannot reserve an anchor");
            a.transform.position=new Vector2(10,0);Physics2D.WallX=5;Check(!f.grapple.TryStartAt(a)&&a.Available,"An occluded anchor cannot be acquired");
            Physics2D.WallX=float.NaN;f.input.Pointer=new Vector2(10,0);f.mode.Set(PlayerMode.Attack);Check(!f.player.Primary(),"Attack-mode primary cannot fire the grapple service");f.mode.Set(PlayerMode.Hook);Check(f.player.Primary(),"Hook-mode primary is routed through the shipping grapple request handler");
            f.grapple.ResetForRetry();Check(!f.grapple.ZipStrikeReady&&!a.Leased,"Explicit retry removes stale strike and lease state");
        }
        {
            var f=new Fixture();var a=f.Anchor<FixedGrappleAnchor>(new Vector2(6,2));Check(f.grapple.TryStartAt(a),"A short arrival begins");
            Check(!f.grapple.HazardInvulnerable,"Launch preparation does not grant traversal hazard immunity");
            f.player.Tick(.02f);f.player.Tick(.02f);Check(f.grapple.HazardInvulnerable,"Only actual GrappleDash grants traversal hazard immunity");
            for(int i=0;i<100&&f.grapple.Active;i++)f.player.Tick(.02f);
            Check(!f.grapple.HazardInvulnerable,"Arrival ends traversal immunity");
            float bonus;Check(f.grapple.TryConsumeZipStrike(out bonus)&&bonus>1&&!f.grapple.TryConsumeZipStrike(out bonus),"P4 can consume the arrival bonus once rather than amplifying multiple cuts");
        }
        foreach(bool allow in new[]{false,true})
        {
            var f=new Fixture();var a=f.Anchor<FixedGrappleAnchor>(new Vector2(10,4));f.grapple.data.allowDashCancel=allow;f.player.Dashes=1;f.grapple.TryStartAt(a);
            f.input.Buffer.Put(PlayerCommand.Dash,Time.time,.15);f.player.Tick(.02f);
            Check(f.grapple.Active==!allow&&a.Leased==!allow,"Dash interruption follows the inspector cancellation policy and releases the lease");
            if(allow)Check(f.player.State==PlayerStateId.Dash&&!f.input.Buffer.Has(PlayerCommand.Dash,Time.time),"An accepted dash cancellation consumes its buffered command exactly once");
        }
        Console.WriteLine("PASS: "+checks+" shipping grapple/anchor controller checks with a narrow wall and movement-owner double; native physics/input not simulated");
    }
}
