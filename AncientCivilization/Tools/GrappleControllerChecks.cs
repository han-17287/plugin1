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
    static void PatrolInterceptions()
    {
        foreach(int hz in new[]{30,50,120})
        {
            float dt=1f/hz;Time.fixedDeltaTime=dt;
            var f=new Fixture();f.grapple.data.launchDelay=.046f;
            Vector2 motion=new Vector2(-3,14),point=new Vector2(10,2);
            var hook=f.Anchor<FixedGrappleAnchor>(point);
            hook.MovePatrolSocket(point-motion*dt,dt);
            hook.MovePatrolSocket(point,dt);
            f.input.Pointer=point;
            Check(f.player.Primary()&&hook.Leased,"Mouse selects moving patrol socket using its current visible position");
            Vector2 firstVelocity=Vector2.zero;
            for(int i=0;i<60&&f.grapple.Active;i++)
            {
                f.player.Tick(dt);
                if(firstVelocity.sqrMagnitude<1 && f.player.Velocity.sqrMagnitude>1)firstVelocity=f.player.Velocity;
                var p=f.body.position;
                Check(Math.Abs(firstVelocity.x*p.y-firstVelocity.y*p.x)<.003f,"Moving target never steers the straight dash");
                point+=motion*dt;hook.MovePatrolSocket(point,dt);
            }
            Check(firstVelocity.y/firstVelocity.x>.38f,"Delayed projectile anticipates a crossing socket");
            Check(f.grapple.LastArrival==hook && f.grapple.ArrivalSequence==1 && f.grapple.ZipStrikeReady && !hook.Leased,
                "Live moving socket contact triggers one genuine arrival and an attack opening");
        }
        Time.fixedDeltaTime=.02f;
        {
            var f=new Fixture();var hook=f.Anchor<FixedGrappleAnchor>(new Vector2(12.5f,0));
            hook.MovePatrolSocket(new Vector2(12.5f,0),.02f);hook.MovePatrolSocket(new Vector2(12.7f,0),.02f);
            Check(!f.grapple.TryStartAt(hook)&&!hook.Leased,"Cannot zip to an intercept beyond physical range");
            hook.MovePatrolSocket(new Vector2(12.5f,0),.02f);
            Check(f.grapple.TryStartAt(hook),"The same ring is catchable when it comes back");
            f.grapple.ResetForRetry();
        }
        {
            var f=new Fixture();var hook=f.Anchor<OneUseGrappleAnchor>(new Vector2(11.1f,0));
            hook.MovePatrolSocket(new Vector2(11.1f,0),.02f);hook.MovePatrolSocket(new Vector2(11.3f,0),.02f);
            Physics2D.WallX=12;
            Check(!f.grapple.TryStartAt(hook)&&!hook.Leased&&!hook.Spent,"Visible ring with a blocked intercept is not reserved");
            f.input.Pointer=hook.Point;
            Check(!f.player.Primary(),"Click on an unreachable moving socket does not silently zip to the wall behind it");
        }
        {
            var f=new Fixture();var hook=f.Anchor<OneUseGrappleAnchor>(new Vector2(10,0));
            hook.MovePatrolSocket(hook.Point,.02f);
            Check(f.grapple.TryStartAt(hook),"Moving socket with zero sampled speed can be targeted");
            for(int i=0;i<30&&f.grapple.Active;i++)
            {
                if(i==3)hook.MovePatrolSocket(new Vector2(10,3.1f),.02f);
                f.player.Tick(.02f);
            }
            Check(f.grapple.ArrivalSequence==0&&!hook.Spent&&hook.Available&&!f.grapple.ZipStrikeReady,
                "Reaching the former position cannot spend a displaced socket or trigger patrol arrival");
        }
        {
            var f=new Fixture();var hook=f.Anchor<FixedGrappleAnchor>(new Vector2(10,0));
            hook.MovePatrolSocket(hook.Point,.02f);
            Check(f.grapple.TryStartAt(hook),"Curving patrol hook starts a straight dash");
            for(int i=0;i<40&&f.grapple.Active;i++)
            {
                f.player.Tick(.02f);
                float t=Math.Min(1,(i+1)/8f);
                hook.MovePatrolSocket(new Vector2(10,.43f*t*t),.02f);
            }
            Check(f.grapple.LastArrival==hook&&f.grapple.ArrivalSequence==1,"A curved .43-unit deviation still intersects capsule and ring");
        }
        {
            var f=new Fixture();var hook=f.Anchor<FixedGrappleAnchor>(new Vector2(9,1));
            Check(f.grapple.TryStartAt(hook),"First room tick can precede the first patrol sample");
            hook.MovePatrolSocket(new Vector2(9,5),.02f);
            for(int i=0;i<30&&f.grapple.Active;i++)f.player.Tick(.02f);
            Check(f.grapple.ArrivalSequence==0&&hook.Available,"First movement invalidates stale arrivals on the already committed line");
            Call(hook,"OnDisable");Call(hook,"OnEnable");
            Check(hook.PatrolDriven && hook.PatrolVelocity.sqrMagnitude==0,"Reactivated moving socket retains contact semantics and discards stale speed");
        }
        {
            // 47번 방의 교차 궤도와 서쪽 지붕에서 실제 두 번의 견인을 연결한다.
            var f=new Fixture();f.body.position=new Vector2(10,6.7f);
            var west=f.Anchor<FixedGrappleAnchor>(new Vector2(10,7.5f));
            var east=f.Anchor<FixedGrappleAnchor>(new Vector2(37,8.9f));
            float time=2f;
            void MoveRails(float t)
            {
                float wave=(1f-(float)Math.Cos(t*1.31f))*.5f;
                west.MovePatrolSocket(new Vector2(9.6f+12.7f*wave,7.45f+wave*2.65f),.02f);
                east.MovePatrolSocket(new Vector2(37.4f-12.3f*wave,8.55f+wave*2.65f),.02f);
            }
            MoveRails(time-.02f);MoveRails(time);
            int stops=0;
            foreach(var hook in new[]{west,east})
            {
                Check(f.grapple.TryStartAt(hook),"At the cable exchange the next socket can be reached from the last actual landing");
                for(int i=0;i<80&&f.grapple.Active;i++)
                {f.player.Tick(.02f);time+=.02f;MoveRails(time);}
                Check(f.grapple.ArrivalSequence==++stops && f.grapple.LastArrival==hook,"Each crossing transfer credits its own moving ring");
            }
        }
        Check(!GrappleRules.TryIntercept(10,0,72,0,.02f,72,out _) &&
            GrappleRules.TryIntercept(10,0,-72,0,0,72,out float catchTime) && Math.Abs(catchTime-10f/144f)<.0001f,
            "Equal-speed receding target is unreachable, approaching target has a stable finite intercept");
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
        {
            var f=new Fixture();var anchor=f.Anchor<FixedGrappleAnchor>(new Vector2(10,0));anchor.SetOperational(false);f.input.Pointer=new Vector2(10,0);Physics2D.WallX=12;
            Check(!f.player.Primary()&&!f.grapple.Active,"Clicking a closed timed socket cannot unexpectedly fall through to a wall behind it");
            anchor.SetOperational(true);Check(f.player.Primary(),"The same socket becomes a valid target when its window opens");
        }
        PatrolInterceptions();
        Console.WriteLine("PASS: "+checks+" shipping grapple/anchor controller checks with a narrow wall and movement-owner double; native physics/input not simulated");
    }
}
