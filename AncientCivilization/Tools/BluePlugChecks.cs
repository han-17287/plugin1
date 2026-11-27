using System;
using System.Numerics;
using Plugin.PlayerCore;

static class BluePlugChecks
{
    const BluePlugSkill Both = BluePlugSkill.Tow | BluePlugSkill.Swing;
    static int checks;

    static void Main()
    {
        CheckConnectionAndInputs();
        CheckRecovery();
        CheckMotionTimeline();
        CheckWindChargeAndRetract();
        CheckPendulum();
        CheckLaundryRoutes();
        Console.WriteLine($"PASS: {checks} Blue Plug rules and authored laundry route envelope checks; native Unity casts not simulated.");
    }

    static void CheckWindChargeAndRetract()
    {
        Check(Close(BluePlugRules.WindCharge(0, .65f), 0), "A tap uses the existing baseline reach");
        Check(Close(BluePlugRules.WindCharge(.325f, .65f), .5f), "Half the hold gives half charge");
        Check(Close(BluePlugRules.WindCharge(4, .65f), 1), "Holding the button cannot grow the charge beyond the cap");
        Check(Close(BluePlugRules.WindCharge(-1, .65f), 0), "Negative hold cannot charge");
        Check(Close(BluePlugRules.ChargedValue(16, 1.25f, .5f), 18), "Half charge interpolates range");
        Check(Close(BluePlugRules.ChargedValue(36, 1.6f, 1), 57.6f), "Full charge applies the configured speed multiplier");

        var plug = new BluePlugSession();
        Check(!plug.BeginRetract(), "An idle plug cannot retract");
        Check(plug.BeginWindup() && plug.BeginRetract(), "Winding can be cancelled into retraction");
        Check(!plug.Launch() && !plug.BeginWindup(), "Retraction locks out launch and a second wind-up");
        plug.Clear();
        Check(plug.BeginWindup() && plug.Launch() && plug.BeginRetract(), "A missed throw retracts");
        Check(!plug.Dock(10), "Retraction cannot dock across a socket");
        plug.Clear();
        Check(plug.BeginWindup() && plug.Launch() && plug.Dock(11) && plug.BeginRetract(), "A connected plug can be recalled");
        Check(!plug.Activate(BluePlugSkill.Tow, Both, Both, true, 12), "A recalled plug cannot move the player");
    }

    static void CheckConnectionAndInputs()
    {
        var plug = new BluePlugSession();
        Check(!plug.Dock(2), "Docking requires an active flight");
        Check(!plug.Activate(BluePlugSkill.Tow, Both, Both, true, 3), "Ready cannot move the player");
        Check(!plug.Launch(), "The plug cannot fly before winding");
        Check(plug.BeginWindup() && plug.Phase == BluePlugPhase.Winding, "Wind-up prepares a throw");
        Check(!plug.BeginWindup(), "A second wind-up cannot summon another plug");
        Check(!plug.Dock(4), "The winding plug cannot latch onto a socket");
        Check(!plug.Activate(BluePlugSkill.Tow, Both, Both, true, 5), "Wind-up rejects a tow press");
        Check(!plug.Activate(BluePlugSkill.Swing, Both, Both, true, 6), "Wind-up rejects a swing press");
        Check(plug.Phase == BluePlugPhase.Winding, "Wind-up leaves the selected movement inactive");
        Check(plug.Launch() && plug.Phase == BluePlugPhase.Flying, "A prepared throw starts flight only");
        Check(!plug.BeginWindup(), "A second wind-up cannot replace the plug in flight");
        Check(!plug.Launch(), "Another throw is ignored in flight");
        Check(!plug.Activate(BluePlugSkill.Tow, Both, Both, true, 10), "Flight rejects a tow press");
        Check(!plug.Activate(BluePlugSkill.Swing, Both, Both, true, 11), "Flight rejects a swing press");
        Check(plug.Dock(20) && plug.Phase == BluePlugPhase.Connected, "Docking ends in a passive connection");
        Check(!plug.Dock(21) && !plug.Launch() && !plug.BeginWindup(),
            "A connected line rejects another dock, launch or wind-up");
        foreach (var skill in new[] { BluePlugSkill.Tow, BluePlugSkill.Swing })
        {
            Check(!plug.Activate(skill, Both, Both, true, 6), "A press from wind-up must expire");
            Check(!plug.Activate(skill, Both, Both, true, 19), "A press from flight must expire");
            Check(!plug.Activate(skill, Both, Both, true, 20), "The docking sample cannot also start movement");
            Check(!plug.Activate(skill, BluePlugSkill.None, Both, true, 21), "An unlearned skill cannot move");
            Check(!plug.Activate(skill, Both, Both ^ skill, true, 21), "The socket must support the requested skill");
            Check(!plug.Activate(skill, Both, Both, false, 21), "An obstructed route cannot start movement");
        }
        Check(!plug.Activate(BluePlugSkill.None, Both, Both, true, 21), "No input must leave the connection passive");
        Check(!plug.Activate(Both, Both, Both, true, 21), "A combined request cannot implicitly pick a skill");
        Check(!plug.Activate((BluePlugSkill)4, (BluePlugSkill)4, (BluePlugSkill)4, true, 21), "Future skill flags are not callable");
        Check(plug.Phase == BluePlugPhase.Connected, "Rejected commands keep a passive connection");
        Check(plug.Activate(BluePlugSkill.Tow, BluePlugSkill.Tow, Both, true, 21), "A fresh eligible tow activates");
        Check(plug.Phase == BluePlugPhase.Towing, "Tow enters its own state");
        Check(!plug.Activate(BluePlugSkill.Swing, Both, Both, true, 22), "First trial has no active skill switching");
        Check(!plug.Launch(), "The active line owns the only plug");
    }

    static void CheckRecovery()
    {
        foreach (BluePlugPhase phase in Enum.GetValues<BluePlugPhase>())
        {
            var plug = new BluePlugSession();
            if (phase != BluePlugPhase.Ready) plug.BeginWindup();
            if (phase != BluePlugPhase.Ready && phase != BluePlugPhase.Winding) plug.Launch();
            if (phase == BluePlugPhase.Retracting) plug.BeginRetract();
            if (phase == BluePlugPhase.Connected || phase == BluePlugPhase.Towing || phase == BluePlugPhase.Swinging)
                plug.Dock(30);
            if (phase == BluePlugPhase.Towing || phase == BluePlugPhase.Swinging)
            {
                BluePlugSkill skill = phase == BluePlugPhase.Towing ? BluePlugSkill.Tow : BluePlugSkill.Swing;
                Check(plug.Activate(skill, skill, Both, true, 31), "The unlocked documented skill can activate by itself");
            }
            Check(plug.Phase == phase, "Every phase can be reached to test cleanup");
            plug.Clear();
            plug.Clear();
            Check(plug.Phase == BluePlugPhase.Ready, "Cleanup is idempotent for all phases");
            Check(!plug.Activate(BluePlugSkill.Tow, Both, Both, true, 99), "Cleanup leaves no retained connection");
            Check(!plug.Launch(), "A launch cannot reuse a cleared wind-up");
            Check(plug.BeginWindup() && plug.Launch() && plug.Dock(100), "A clean throw can occur after cleanup");
            Check(!plug.Activate(BluePlugSkill.Swing, Both, Both, true, 99), "The new connection rejects the old press");
            Check(plug.Activate(BluePlugSkill.Swing, BluePlugSkill.Swing, Both, true, 101), "A new swing works after cleanup");
        }
    }

    static void CheckMotionTimeline()
    {
        const float dt = 1f / 60;
        var timeline = new BluePlugMotionTimeline();
        Check(timeline.Step(BluePlugPhase.Ready, dt) == BluePlugPose.None, "An idle plug yields to locomotion");
        Check(timeline.Step(BluePlugPhase.Winding, dt) == BluePlugPose.Windup, "Winding raises the arm");
        Check(timeline.Step(BluePlugPhase.Flying, dt) == BluePlugPose.Throw, "The release enters the throw pose immediately");
        Check(timeline.Step(BluePlugPhase.Connected, dt) == BluePlugPose.Throw, "A quick latch keeps the throw follow-through");
        for (int i = 0; i < 16; i++) timeline.Step(BluePlugPhase.Connected, dt);
        Check(timeline.Step(BluePlugPhase.Connected, dt) == BluePlugPose.None, "A passive link returns to locomotion");
        Check(timeline.Step(BluePlugPhase.Connected, dt, true) == BluePlugPose.WallBrace,
            "Only reaching the socket's physical handhold changes a passive link to the wall pose");
        Check(timeline.Step(BluePlugPhase.Connected, dt, false) == BluePlugPose.None,
            "Leaving the wall releases the braced pose while the plug can remain connected");
        Check(timeline.Step(BluePlugPhase.Ready, dt, true) == BluePlugPose.WallBrace,
            "The handhold can brace the character even when the plug is still holstered");
        Check(timeline.Step(BluePlugPhase.Winding, dt, true) == BluePlugPose.Windup,
            "The free arm can wind a throw while the player holds the wall");
        timeline.Clear();
        timeline.Step(BluePlugPhase.Winding, dt);
        timeline.Step(BluePlugPhase.Flying, dt);
        Check(timeline.Step(BluePlugPhase.Retracting, dt) == BluePlugPose.Reel, "A failed throw rewinds with its own pose immediately");
        Check(timeline.Step(BluePlugPhase.Ready, dt) == BluePlugPose.None, "Fast reel-in does not replay the interrupted throw");
        Check(timeline.Step(BluePlugPhase.Towing, dt) == BluePlugPose.Tow, "Tow has its own pose");
        Check(timeline.Step(BluePlugPhase.Ready, dt) == BluePlugPose.Recovery, "Tow arrival begins a short recovery");
        Check(timeline.Step(BluePlugPhase.Winding, dt) == BluePlugPose.Windup, "A new wind-up interrupts old recovery");
        Check(timeline.Step(BluePlugPhase.Connected, dt) == BluePlugPose.Throw, "A latch between rendered frames still shows a throw");
        Check(timeline.Step(BluePlugPhase.Swinging, dt) == BluePlugPose.Swing, "Swing overrides a throw that is still finishing");
        Check(timeline.Step(BluePlugPhase.Ready, dt) == BluePlugPose.Recovery, "Swing release starts recovery");
        timeline.Clear();
        Check(timeline.Step(BluePlugPhase.Ready, dt) == BluePlugPose.None, "Damage and retry clear the animation immediately");
        timeline.Step(BluePlugPhase.Towing, dt);
        for (int i = 0; i < 20; i++) timeline.Step(BluePlugPhase.Ready, dt);
        Check(timeline.Step(BluePlugPhase.Ready, dt) == BluePlugPose.None, "Recovery gives control back to the locomotion pose");
        Check(timeline.Step(BluePlugPhase.Ready, dt, grounded:true) == BluePlugPose.Landing,
            "A late real landing after an airborne tow still absorbs the impact");
        for (int i=0; i<12; ++i) timeline.Step(BluePlugPhase.Ready, dt, grounded:true);
        Check(timeline.Step(BluePlugPhase.Ready, dt, grounded:true) == BluePlugPose.None,
            "Standing on the same floor cannot replay the landing");
        timeline.Step(BluePlugPhase.Swinging, dt);
        timeline.Step(BluePlugPhase.Ready, dt);
        timeline.InterruptRecovery();
        Check(timeline.Step(BluePlugPhase.Ready, dt, grounded:true) == BluePlugPose.None,
            "An intentional jump cancels both the swing release and its queued landing");
        timeline.Step(BluePlugPhase.Towing, dt);
        timeline.InterruptRecovery();
        Check(timeline.Step(BluePlugPhase.Ready, dt, grounded:true) == BluePlugPose.None,
            "A buffered jump already taken by physics wins even before the next visual frame");
        timeline.Step(BluePlugPhase.Towing, dt, showLanding:false);
        Check(timeline.Step(BluePlugPhase.Ready, dt, grounded:true, showLanding:false) == BluePlugPose.Recovery,
            "The final sniper encounter keeps its original recovery when the feet reach the floor");
        for (int i=0; i<20; ++i) timeline.Step(BluePlugPhase.Ready, dt, grounded:true, showLanding:false);
        Check(timeline.Step(BluePlugPhase.Ready, dt, grounded:true) == BluePlugPose.None,
            "Leaving the protected encounter cannot replay a deferred landing");
    }

    static void CheckPendulum()
    {
        const float gravity = 32, pump = 12, limit = 20, dt = 1f / 60;
        Check(Close(BluePlugRules.SwingSpeed(0, 0, 0, gravity, pump, limit, dt), 0), "A stationary low point cannot gain speed without input");
        Check(Close(BluePlugRules.SwingSpeed(7, 0, 0, gravity, pump, limit, dt), 7), "A neutral low point keeps signed speed");
        Check(Close(BluePlugRules.SwingSpeed(1, -.6f, 0, gravity, pump, limit, dt),
            1 + gravity * .6f * dt), "Descending gravity accelerates along the tangent");
        Check(Close(BluePlugRules.SwingSpeed(1, .6f, 0, gravity, pump, limit, dt),
            1 - gravity * .6f * dt), "Ascending gravity decelerates along the tangent");
        Check(Close(BluePlugRules.SwingSpeed(19.9f, -1, 1, gravity, pump, limit, dt), limit), "Positive swing speed is capped");
        Check(Close(BluePlugRules.SwingSpeed(-19.9f, 1, -1, gravity, pump, limit, dt), -limit), "Negative swing speed is capped");
        Check(Close(BluePlugRules.SwingAngleStep(8, 4, dt), 2 * dt), "Angular change is tangent speed divided by radius");
        Check(Close(BluePlugRules.SwingAngleStep(-8, 4, dt), -2 * dt), "Reverse travel keeps the angular sign");
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, 0f, -1f })
            Check(BluePlugRules.SwingAngleStep(10, invalid, dt) == 0, "Invalid radius cannot produce an angular step");

        var pivot = new Vector2(23.5f, 9.25f);
        const float radius = 7.5f;
        float angle = -2.57f, speed = 3;
        for (int i = 0; i < 240; i++)
        {
            var radial = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var tangent = new Vector2(-radial.Y, radial.X);
            speed = BluePlugRules.SwingSpeed(speed, tangent.Y, tangent.X, gravity, pump, limit, dt);
            angle += BluePlugRules.SwingAngleStep(speed, radius, dt);
            var position = pivot + radius * new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            Check(Math.Abs(Vector2.Distance(position, pivot) - radius) < .0001f, "The step traces a fixed-radius path");
        }
        var releaseRadial = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var releaseVelocity = new Vector2(-releaseRadial.Y, releaseRadial.X) * speed;
        Check(Math.Abs(Vector2.Dot(releaseRadial, releaseVelocity)) < .0001f,
            "Releasing at this instant keeps the tangent and adds no radial impulse");
        Check(Close(releaseVelocity.Length(), Math.Abs(speed)), "Swing release keeps the current speed");
    }

    static void CheckLaundryRoutes()
    {
        // PatrolLiving authored geometry and DefaultBluePlug / DefaultMovement settings.
        var pivot = new Vector2(23.5f, 9.25f);
        var roofApproach = new Vector2(23.5f, 10.20f);
        const float roofY = 9.15f + .9f;
        const float windowY = 3.50f + .9f;
        const float dt = 1f / 60;
        const float towSpeed = 48;
        const float carry = .22f;
        var takeoffs = new[]
        {
            (new Vector2(17.10f, 5.18f), new Vector2(8, -.3f)),
            (new Vector2(17.25f, 5.14f), new Vector2(8, -1)),
            (new Vector2(17.30f, 5.10f), new Vector2(6, -2))
        };
        foreach (var (takeoff, incoming) in takeoffs)
        {
            var hand = takeoff + new Vector2(0, .34f);
            Check(Vector2.Distance(hand, pivot) < 16, "The start fits into throw range");
            foreach (float y in new[] { HeightAt(hand, pivot, 20.15f), HeightAt(takeoff, roofApproach, 20.15f) })
                Check(y > 6.10f && y < 8.80f, "The closed test shutter intersects both throw and tow lines");

            var move = roofApproach - takeoff;
            var direction = Vector2.Normalize(move);
            float remaining = move.Length();
            while (remaining > .065f)
            {
                float step = GrappleRules.Step(remaining, towSpeed, dt);
                remaining -= step;
            }
            var towFinish = roofApproach - direction * remaining;
            Check(towFinish.Y > roofY, "The tow capsule clears the roof before releasing");
            Vector2 roofLanding = Land(towFinish, direction * (towSpeed * carry), 0, roofY);
            Check(roofLanding.X > 21.4f + .36f && roofLanding.X < 31.6f - .36f,
                "The direct route hands off onto the rooftop with room for the complete capsule");

            Vector2 fromPivot = takeoff - pivot;
            float radius = fromPivot.Length();
            float angle = MathF.Atan2(fromPivot.Y, fromPivot.X);
            float speed = Vector2.Dot(incoming, new Vector2(-MathF.Sin(angle), MathF.Cos(angle)));
            Vector2 swingPosition = takeoff;
            for (int i = 0; i < 120 && swingPosition.X < 28.7f; i++)
            {
                var tangent = new Vector2(-MathF.Sin(angle), MathF.Cos(angle));
                speed = BluePlugRules.SwingSpeed(speed, tangent.Y, tangent.X, 32, 15, 22, dt);
                angle += BluePlugRules.SwingAngleStep(speed, radius, dt);
                swingPosition = pivot + radius * new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                Check(swingPosition.X > 16.65f + .36f + .035f, "An off-lip swing stays clear of the launch shelf");
                Check(swingPosition.Y > .9f + .035f && swingPosition.Y < roofY,
                    "The arc fits above the lower alley and under the tow deck");
            }
            Check(swingPosition.X >= 28.7f, "The right pump reaches the marked release point");
            var tangentAtRelease = new Vector2(-MathF.Sin(angle), MathF.Cos(angle));
            Vector2 windowLanding = Land(swingPosition, tangentAtRelease * speed, -1, windowY);
            Check(windowLanding.X > 28.6f + .36f && windowLanding.X < 34.4f - .36f,
                "Release followed by A braking reaches the distinct workshop windowsill");
        }
    }

    static float HeightAt(Vector2 from, Vector2 to, float x) =>
        from.Y + (to.Y - from.Y) * ((x - from.X) / (to.X - from.X));

    static Vector2 Land(Vector2 position, Vector2 velocity, int axis, float capsuleLandingY)
    {
        const float dt = 1f / 60;
        const float runSpeed = 9;
        float target = axis * runSpeed;
        for (int i = 0; i < 120; i++)
        {
            float error = Math.Min(1, Math.Abs(target - velocity.X) / runSpeed);
            float rate = axis == 0 ? .45f + .55f * error : .35f + .65f * error * error * (3 - 2 * error);
            float drift = Math.Clamp(target - velocity.X, -32 * rate * dt, 32 * rate * dt);
            velocity.X += drift;
            velocity.Y = Math.Max(-24, velocity.Y - (velocity.Y < 0 ? 52.8f : 32) * dt);
            var step = velocity * dt;
            if (position.Y >= capsuleLandingY && position.Y + step.Y <= capsuleLandingY && step.Y < 0)
                return position + step * ((capsuleLandingY - position.Y) / step.Y);
            position += step;
        }
        throw new Exception("The modeled movement cannot reach this floor");
    }

    static bool Close(float a, float b) => Math.Abs(a - b) < .0001f;

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
}
