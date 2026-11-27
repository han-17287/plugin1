using System;
using System.Reflection;
using Plugin.PlayerCore;
using UnityEngine;
using UnityEngine.InputSystem;

static class BluePlugRuntimeChecks
{
    sealed class Rig
    {
        public readonly PlayerBluePlug Plug;
        public readonly PlayerStateMachine Player;
        public readonly Rigidbody2D Body;
        public readonly PlayerInputReader Input;
        public readonly PlayerModeController Mode;
        public readonly BluePlugSocket Socket;
        public readonly BluePlugVisuals Visuals;
        public BluePlugSkill Skill = BluePlugSkill.Tow | BluePlugSkill.Swing;
        public string LastNotice;
        readonly Keyboard keyboard;

        public Rig()
        {
            Time.frameCount = 0;
            Physics2D.WallX = float.NaN;
            Physics2D.WallTop = float.PositiveInfinity;
            Physics2D.CeilingY = float.NaN;
            Application.isFocused = true;
            Keyboard.current = keyboard = new Keyboard();
            var user = new GameObject();
            Plug = user.Add<PlayerBluePlug>();
            Player = user.Add<PlayerStateMachine>();
            Body = user.Add<Rigidbody2D>();
            Input = user.Add<PlayerInputReader>();
            Mode = user.Add<PlayerModeController>();
            Visuals = user.Add<BluePlugVisuals>();
            user.Add<CapsuleCollider2D>();
            Body.position = new Vector2(0, 2);
            Socket = new GameObject().Add<BluePlugSocket>();
            Socket.Point = new Vector2(6, 6);
            Socket.TowPoint = Socket.Point + Vector2.up;
            Input.Pointer = Socket.Point;
            Call("Awake");
            Call("OnEnable");
            Plug.ConfigureRoom(new[] { Socket }, new Camera(), () => Skill, message => LastNotice = message);
            Frame();
        }

        void Call(string name) => typeof(PlayerBluePlug).GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Plug, null);

        public void Frame(bool g = false, bool c = false, bool v = false, bool advance = true,
            bool primary = false, PlayerMode? mode = null)
        {
            if (advance) Time.frameCount++;
            keyboard.gKey.Sample(g);
            keyboard.cKey.Sample(c);
            keyboard.vKey.Sample(v);
            bool modeChanged = mode.HasValue && Mode.Mode != mode.Value;
            Input.PrimaryPressed = !modeChanged && primary && !Input.PrimaryHeld;
            Input.PrimaryReleased = !modeChanged && !primary && Input.PrimaryHeld;
            Input.PrimaryHeld = primary;
            if (modeChanged)
            {
                Input.Buffer.Remove(PlayerCommand.Primary);
                Mode.Set(mode.Value);
            }
            if (Input.PrimaryPressed) Input.Buffer.Put(PlayerCommand.Primary, Time.frameCount, 4);
            Call("Update");
            Call("LateUpdate");
        }

        public void Step(bool integrate = false)
        {
            Call("FixedUpdate");
            if (integrate) Player.Tick();
        }

        public Vector2 ReadDrawnRadial()
        {
            Call("LateUpdate");
            return Visuals.Radial;
        }

        public void FinishWind()
        {
            for (int i = 0; i < 300 && Plug.Phase == BluePlugPhase.Winding; i++) Step();
        }

        public void FinishFlight()
        {
            for (int i = 0; i < 80 && Plug.Phase == BluePlugPhase.Flying; i++) Step();
        }

        public void FinishRecall(bool cooldown = false)
        {
            for (int i = 0; i < 80 && Plug.Phase == BluePlugPhase.Retracting; i++) Step();
            if (cooldown)
                for (int i = 0; i < 12; i++) Step();
        }

        public void Throw()
        {
            Frame(g: true);
            Check(Plug.Phase == BluePlugPhase.Winding, "Pressing G starts the hand orbit");
            Frame();
            FinishWind();
            Check(Plug.Phase == BluePlugPhase.Flying, "Releasing G sends the plug after winding");
        }

        public void Dock()
        {
            Throw();
            FinishFlight();
            Check(Plug.Phase == BluePlugPhase.Connected, "A clear dedicated socket seats the plug");
            Frame();
        }
    }

    static int verified;
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        verified++;
    }

    static float TurnBetween(Vector2 before, Vector2 after) =>
        MathF.Atan2(before.x * after.y - before.y * after.x, Vector2.Dot(before, after));

    static void CheckThrow()
    {
        var rig = new Rig();
        rig.Body.linearVelocity = new Vector2(4, 2);
        rig.Frame(g: true);
        rig.Frame(g: true, c: true);
        Vector2 previousRadius = rig.ReadDrawnRadial();
        float turn = 0;
        int orbitTicks = 0;
        for (; orbitTicks < 300 && MathF.Abs(turn) < 2 * MathF.PI + .1f; orbitTicks++)
        {
            rig.Step();
            Vector2 radius = rig.ReadDrawnRadial();
            turn += TurnBetween(previousRadius, radius);
            previousRadius = radius;
        }
        Check(MathF.Abs(turn) > 2 * MathF.PI && previousRadius.magnitude > .05f &&
            rig.Visuals.VisiblePhase == BluePlugPhase.Winding && rig.Plug.Phase == BluePlugPhase.Winding,
            "Held G draws a continuous orbit exceeding one turn without launching");
        Check(rig.Player.VelocityWrites == 0 && rig.Player.Exits == 0 && rig.Visuals.Clicks == 0 &&
            rig.Body.linearVelocity.x == 4 && rig.Body.linearVelocity.y == 2,
            "The orbit and held C never apply movement or signal a latch");

        rig.Frame(c: true);
        rig.FinishWind();
        Check(rig.Plug.Phase == BluePlugPhase.Flying, "Releasing G throws from the winding phase");
        rig.Frame(v: true);
        rig.FinishFlight();
        rig.Frame(v: true, advance: false);
        rig.Frame(v: true);
        Check(rig.Plug.Phase == BluePlugPhase.Connected && rig.Player.Exits == 0,
            "Held C/V pressed while winding or flying leave the successfully docked plug idle");
        Check(rig.Body.linearVelocity.x == 4 && rig.Body.linearVelocity.y == 2 &&
            rig.Player.VelocityWrites == 0 && rig.Visuals.Clicks == 1,
            "Docking clicks once and does not write player velocity");
        rig.Step(integrate: true);
        Check(rig.Body.position.x > 0 && rig.Plug.Phase == BluePlugPhase.Connected,
            "The connected plug permits ordinary player displacement without force");

        rig = new Rig();
        rig.Frame(g: true);
        rig.Frame();
        Vector2 launchRadius = rig.ReadDrawnRadial();
        rig.Step();
        Check(rig.Plug.Phase == BluePlugPhase.Flying && rig.Plug.ThrowSequence == 1,
            "A tap releases on the first physics tick without waiting for an orbit");
        rig.Step();
        Vector2 flightDirection = (rig.ReadDrawnRadial() - launchRadius).normalized;
        Check(Vector2.Dot(flightDirection, (rig.Socket.Point - rig.Body.position - Vector2.up * .34f).normalized) > .99f,
            "Immediate release flies towards the chosen socket even before the first orbit sample");
        rig.Frame(g: true);
        rig.FinishFlight();
        Check(rig.Plug.Phase == BluePlugPhase.Retracting && rig.Visuals.Clicks == 0,
            "Re-pressing G during flight reels in the plug without any movement");

        rig = new Rig();
        rig.Throw();
        rig.FinishFlight();
        rig.Frame(c: true, advance: false);
        Check(rig.Plug.Phase == BluePlugPhase.Connected,
            "A C sample in the exact docking frame is rejected");
        rig.Frame(c: true);
        Check(rig.Plug.Phase == BluePlugPhase.Connected, "A rejected, held C sample cannot fire in a later frame");

        rig = new Rig();
        rig.Frame(g: true);
        rig.Frame();
        rig.Input.Pointer = new Vector2(-12, 2);
        rig.Frame();
        rig.FinishWind();
        rig.FinishFlight();
        Check(rig.Plug.Phase == BluePlugPhase.Connected,
            "Moving the mouse after releasing G does not redirect the queued throw");

        rig = new Rig();
        Vector2 endpoint = rig.Body.position + new Vector2(16, .34f);
        rig.Socket.Move(endpoint - rig.Socket.Point);
        rig.Input.Pointer = endpoint;
        rig.Frame();
        Check(rig.Socket.Indicator == BluePlugSocketState.Ready, "The uncharged guide includes the exact hand-centered range edge");
        rig.Frame(primary: true);
        rig.Step();
        rig.ReadDrawnRadial();
        float reach = rig.Visuals.Range;
        endpoint = rig.Body.position + new Vector2(reach, .34f);
        rig.Socket.Move(endpoint - rig.Socket.Point);
        rig.Input.Pointer = endpoint;
        rig.Frame();
        rig.Step();
        Check(rig.Plug.Phase == BluePlugPhase.Flying && Vector2.Distance(rig.ReadDrawnRadial(), new Vector2(reach, 0)) > reach,
            "Releasing on the back of the orbit retains the full hand-centred reach");
        rig.FinishFlight();
        Check(rig.Plug.Phase == BluePlugPhase.Connected && rig.Visuals.Clicks == 1,
            "A selected socket at the guide edge seats even if the swinging tip starts farther away");
    }

    static void CheckPrimaryAndCharge()
    {
        var rig = new Rig();
        rig.Frame(primary: true, mode: PlayerMode.Attack);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && !rig.Input.Buffer.Has(PlayerCommand.Primary, Time.frameCount),
            "Mouse presses on the mode-switch frame do not enter the new attack buffer");
        rig.Frame();
        rig.Frame(primary: true, mode: PlayerMode.Hook);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && !rig.Input.Buffer.Has(PlayerCommand.Primary, Time.frameCount),
            "Mouse presses on the mode-switch frame do not begin plug winding");
        rig.Frame(primary: true);
        rig.Frame();
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.Visuals.Clicks == 0,
            "Holding then releasing a click from the mode-switch frame cannot throw");
        rig.Frame(mode: PlayerMode.Attack);
        rig.Frame(primary: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.Input.Buffer.Has(PlayerCommand.Primary, Time.frameCount),
            "Attack primary stays in the action buffer and does not start a plug wind");
        rig.Frame(primary: true, mode: PlayerMode.Hook);
        Check(rig.Plug.Phase == BluePlugPhase.Ready, "A primary held through mode switching cannot start winding");
        rig.Frame();
        rig.Input.Buffer.Put(PlayerCommand.Detach, Time.frameCount, 4);
        rig.Input.Buffer.Put(PlayerCommand.LaserPress, Time.frameCount, 4);
        rig.Input.Buffer.Put(PlayerCommand.Overload, Time.frameCount, 4);
        rig.Frame(primary: true);
        Check(rig.Plug.Phase == BluePlugPhase.Winding && !rig.Input.Buffer.Has(PlayerCommand.Primary, Time.frameCount) &&
            rig.Input.Buffer.Has(PlayerCommand.Detach, Time.frameCount) && rig.Input.Buffer.Has(PlayerCommand.LaserPress, Time.frameCount) &&
            rig.Input.Buffer.Has(PlayerCommand.Overload, Time.frameCount),
            "Plug-mode primary uses its live edge without consuming the E, F, or R buffers");
        for (int i = 0; i < 20; i++) rig.Step();
        rig.Frame(primary: true, mode: PlayerMode.Attack);
        Check(rig.Plug.Phase == BluePlugPhase.Retracting, "Leaving plug mode cancels a held mouse wind");
        rig.FinishRecall(cooldown: true);
        rig.Frame(primary: true, mode: PlayerMode.Hook);
        rig.Frame();
        rig.FinishWind();
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.Visuals.Clicks == 0 && rig.Player.VelocityWrites == 0,
            "The release of a cancelled mouse hold cannot launch or move the player");
        rig.Frame(primary: true);
        rig.Frame();
        rig.Frame(mode: PlayerMode.Attack);
        rig.FinishRecall(cooldown: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.Visuals.Clicks == 0,
            "Leaving plug mode before the release physics tick cancels the throw");
        rig.Frame(g: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready, "G cannot bypass the equipped blade to launch a second plug");

        rig = new Rig();
        rig.Frame(primary: true);
        rig.Frame();
        rig.ReadDrawnRadial();
        float tapRange = rig.Visuals.Range;
        float tapCharge = rig.Visuals.Charge;
        rig.FinishWind();
        var tapPosition = rig.ReadDrawnRadial();
        rig.Step();
        float tapStep = Vector2.Distance(tapPosition, rig.ReadDrawnRadial());
        Check(tapCharge < .001f && MathF.Abs(tapRange - 16) < .001f,
            "A tap retains baseline range and the charge at release");

        rig = new Rig();
        rig.Socket.Move(new Vector2(12.9f, -3.5f));
        rig.Input.Pointer = rig.Socket.Point;
        rig.Frame(primary: true);
        for (int i = 0; i < 45; i++) rig.Step();
        float fullRadius = rig.ReadDrawnRadial().magnitude;
        Check(rig.Plug.Phase == BluePlugPhase.Winding && rig.Visuals.Charge >= .999f &&
            rig.Visuals.Range > tapRange && fullRadius > .84f,
            "Holding primary increases the orbit radius, charge, and visible reach without launching");
        rig.Frame();
        rig.FinishWind();
        var chargedPosition = rig.ReadDrawnRadial();
        rig.Step();
        float chargedStep = Vector2.Distance(chargedPosition, rig.ReadDrawnRadial());
        Check(chargedStep > tapStep * 1.4f, "Charged flight travels faster than a tapped throw");
        rig.FinishFlight();
        Check(rig.Plug.Phase == BluePlugPhase.Connected && rig.Player.VelocityWrites == 0 && rig.Visuals.Clicks == 1,
            "A charged mouse throw docks beyond the baseline reach without auto-towing");
        rig.Frame(mode: PlayerMode.Attack);
        rig.Frame(primary: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.Input.Buffer.Has(PlayerCommand.Primary, Time.frameCount),
            "Equipping clears the socket; attack primary belongs to combat");
        rig.Frame(c: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready,
            "An equipped blade reserves the plug and rejects fresh movement keys");
    }

    static void CheckRetract()
    {
        var rig = new Rig();
        Physics2D.WallX = 3;
        rig.Frame(primary: true);
        rig.Frame();
        rig.FinishWind();
        rig.FinishFlight();
        var before = rig.ReadDrawnRadial().magnitude;
        Check(rig.Plug.Phase == BluePlugPhase.Retracting && rig.Visuals.VisiblePhase == BluePlugPhase.Retracting &&
            before > .1f && rig.Visuals.Clicks == 0, "A blocked throw visibly rewinds from the collision side");
        rig.Frame(primary: true);
        rig.Step();
        Check(rig.ReadDrawnRadial().magnitude < before && rig.Player.VelocityWrites == 0,
            "Rewinding shortens the cable and leaves velocity untouched");
        rig.FinishRecall();
        rig.Frame();
        rig.Frame(primary: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.Visuals.VisiblePhase == BluePlugPhase.Ready,
            "The recast cooldown rejects a fresh press immediately after reel-in");
        rig.FinishRecall(cooldown: true);
        rig.Frame(primary: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready, "A click held during the cooldown does not start automatically");
        rig.Frame();
        rig.Frame(primary: true);
        Check(rig.Plug.Phase == BluePlugPhase.Winding, "A new click after reel-in and cooldown starts a new throw");
        rig.Player.Respawn(new Vector2(0, 2));
        rig.Frame(primary: true);
        rig.Frame();
        Check(rig.Plug.Phase == BluePlugPhase.Ready, "Retry discards the held mouse wind and pending release");

        rig = new Rig();
        rig.Dock();
        Physics2D.WallX = 3;
        rig.Frame(g: true);
        bool crossedShutter = false;
        for (int i = 0; i < 18 && rig.Plug.Phase == BluePlugPhase.Retracting; i++)
        {
            rig.Step();
            if (rig.Plug.Phase == BluePlugPhase.Retracting && rig.ReadDrawnRadial().x < 3)
                crossedShutter = true;
        }
        Check(rig.Plug.Phase == BluePlugPhase.Ready && !crossedShutter && rig.Player.VelocityWrites == 0,
            "A shutter closing after the latch clears the return visual without moving the player or drawing the tip through it");
    }

    static void CheckBlockers()
    {
        var rig = new Rig();
        Physics2D.WallX = .28f;
        rig.Frame(g: true);
        float farthest = 0;
        for (int i = 0; i < 80; i++)
        {
            rig.Step();
            farthest = MathF.Max(farthest, rig.ReadDrawnRadial().x);
        }
        Check(rig.Plug.Phase == BluePlugPhase.Winding && farthest > .05f && farthest < .181f,
            "A tight full wall shortens the orbit before the plug tip can cross it");

        rig = new Rig();
        Physics2D.WallX = 3;
        rig.Frame();
        Check(rig.Socket.Indicator == BluePlugSocketState.Blocked, "The aim indicator reports an obstructed socket");
        rig.Throw();
        rig.FinishFlight();
        Check(rig.Plug.Phase == BluePlugPhase.Retracting && rig.Visuals.Clicks == 0 && rig.LastNotice?.Contains("투척") == true,
            "The plug tip cast stops before the wall and does not seat");
        rig.FinishRecall(cooldown: true);
        rig.Input.Pointer = new Vector2(5, 2);
        rig.Frame();
        rig.Throw();
        rig.FinishFlight();
        Check(rig.Plug.Phase == BluePlugPhase.Retracting && rig.Visuals.Clicks == 0,
            "Aimed ordinary wall is never a socket");
        rig.FinishRecall(cooldown: true);
        Physics2D.WallX = float.NaN;
        Physics2D.CeilingY = 7;
        rig.Input.Pointer = new Vector2(0, 15);
        rig.Frame();
        rig.Throw();
        rig.FinishFlight();
        Check(rig.Plug.Phase == BluePlugPhase.Retracting && rig.Visuals.Clicks == 0 && rig.LastNotice?.Contains("투척") == true,
            "An upward shot stops at the ordinary ceiling without attaching");
    }

    static void CheckTow()
    {
        var rig = new Rig { Skill = BluePlugSkill.None };
        rig.Frame();
        Check(rig.Socket.Indicator == BluePlugSocketState.Ready,
            "The standard socket accepts basic aiming even while both movement skills are locked");
        rig.Dock();
        rig.Frame(c: true);
        rig.Frame();
        rig.Frame(v: true);
        Check(rig.Plug.Phase == BluePlugPhase.Connected && rig.Player.VelocityWrites == 0,
            "Both locked skills reject a fresh action edge while the plug remains inserted");
        rig.Skill = BluePlugSkill.Tow;
        rig.Frame();
        Physics2D.WallX = 4;
        rig.Frame(c: true);
        Check(rig.Plug.Phase == BluePlugPhase.Connected && rig.LastNotice?.Contains("견인") == true,
            "A body obstruction blocks tow before the first step");
        Physics2D.WallX = float.NaN;
        rig.Frame(c: true);
        Check(rig.Plug.Phase == BluePlugPhase.Connected, "A held C does not activate when the obstruction goes away");
        rig.Frame();
        rig.Frame(c: true);
        Check(rig.Plug.Phase == BluePlugPhase.Towing, "A fresh unlocked C starts straight tow");
        rig.Step(integrate: true);
        Vector2 beforeShutter = rig.Body.position;
        Physics2D.WallX = beforeShutter.x + .68f;
        rig.Step(integrate: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready &&
            Vector2.Distance(rig.Body.position, beforeShutter) < .001f && rig.Body.linearVelocity.sqrMagnitude == 0,
            "A newly blocked step stops on the near side and recalls the plug");

        rig = new Rig();
        rig.Dock();
        rig.Frame(c: true);
        for (int i = 0; i < 80 && rig.Plug.Phase == BluePlugPhase.Towing; i++) rig.Step(integrate: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.Visuals.Arrivals == 1 &&
            Vector2.Distance(rig.Body.position, rig.Socket.TowPoint) < .25f,
            "An unobstructed tow arrives at the commissioned body destination");
        Check(rig.Body.linearVelocity.x > 0 && rig.Body.linearVelocity.y > 0 && rig.Player.ResetAirActions,
            "Tow arrival recalls, retains momentum and resets the existing air actions");
    }

    static void CheckAimAndLatchIndicators()
    {
        var rig = new Rig();
        rig.Frame(primary: true);
        for (int i = 0; i < 60; i++) rig.Step();
        rig.Frame();
        rig.FinishWind();
        rig.Frame();
        Check(rig.Socket.Indicator == BluePlugSocketState.Idle,
            "The panel does not claim the plug is seated during flight");
        rig.FinishFlight();
        Check(rig.Socket.Indicator == BluePlugSocketState.Linked && rig.Visuals.Clicks == 1,
            "Physics contact lights the socket before another input or aim update");
        rig.Frame(g: true);
        Check(rig.Socket.Indicator == BluePlugSocketState.Idle && rig.Plug.SeatedSocket == null,
            "Manual recall opens the seated panel immediately");
        rig.FinishRecall(cooldown: true);

        Vector2 hand = rig.Body.position + new Vector2(0, .34f);
        rig.Socket.Move(hand + new Vector2(16.40f, 0) - rig.Socket.Point);
        var reachable = new GameObject().Add<BluePlugSocket>();
        reachable.Point = hand + new Vector2(15.85f, 0);
        rig.Plug.ConfigureRoom(new[] { rig.Socket, reachable }, new Camera(), () => rig.Skill, null);
        rig.Input.Pointer = rig.Socket.Point;
        rig.Frame();
        Check(reachable.Indicator == BluePlugSocketState.Ready && rig.Socket.Indicator != BluePlugSocketState.Ready,
            "The new uncharged aim selects a reachable socket even after the previous throw was fully charged");

        rig = new Rig();
        Physics2D.WallX = 3;
        rig.Frame(primary: true);
        rig.Frame();
        Physics2D.WallX = float.NaN;
        rig.FinishWind();
        rig.FinishFlight();
        Check(rig.Visuals.Clicks == 0 && rig.Plug.SeatedSocket == null && rig.Plug.Phase == BluePlugPhase.Retracting,
            "A socket hidden on release cannot become the selected latch if a shutter opens mid-throw");
    }

    static void CheckSwing()
    {
        var rig = new Rig();
        rig.Dock();
        rig.Body.linearVelocity = new Vector2(4, 0);
        rig.Frame(v: true);
        Check(rig.Plug.Phase == BluePlugPhase.Swinging && rig.Body.linearVelocity.x == 4,
            "Entering a swing does not apply an instantaneous velocity kick");
        float length = Vector2.Distance(rig.Body.position, rig.Socket.Point);
        rig.Input.Move = Vector2.right;
        for (int i = 0; i < 12; i++) rig.Step(integrate: true);
        Check(rig.Body.position.x > .25f && MathF.Abs(Vector2.Distance(rig.Body.position, rig.Socket.Point) - length) < .01f,
            "The actual controller follows the original swing radius along a right-moving arc");
        rig.Frame();
        Vector2 radius = rig.Body.position - rig.Socket.Point;
        rig.Frame(v: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.Body.linearVelocity.x > 0 &&
            MathF.Abs(Vector2.Dot(radius.normalized, rig.Body.linearVelocity)) < .001f,
            "V exits and recalls along the instantaneous tangent");

        rig = new Rig();
        rig.Dock();
        rig.Frame(v: true);
        rig.Step(integrate: true);
        Physics2D.WallX = 3;
        rig.Step(integrate: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.LastNotice?.Contains("전선") == true &&
            rig.Body.linearVelocity.sqrMagnitude == 0, "A new shutter across the cable releases and recalls the swing safely");

        rig = new Rig();
        rig.Dock();
        rig.Body.linearVelocity = new Vector2(10, 0);
        rig.Frame(v: true);
        Vector2 obstructionStart = rig.Body.position;
        Physics2D.WallX = obstructionStart.x + .39f;
        Physics2D.WallTop = obstructionStart.y + .03f;
        rig.Step(integrate: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.LastNotice?.Contains("스윙 경로") == true &&
            Vector2.Distance(rig.Body.position, obstructionStart) < .001f,
            "A low wall that clears the cable still stops the character body sweep");
    }

    static void CheckMovingSocket()
    {
        var rig = new Rig();
        rig.Socket.Move(new Vector2(12, 0));
        rig.Socket.PatrolDriven = true;
        rig.Socket.AnchorVelocity = new Vector2(4, 1);
        rig.Socket.RangeOverride = 24;
        rig.Socket.TowSpeedOverride = 72;
        rig.Socket.TowCarryOverride = .30f;
        rig.Input.Pointer = rig.Socket.Point;
        Vector2 firstTarget = rig.Socket.Point;
        rig.Throw();
        for (int i = 0; i < 80 && rig.Plug.Phase == BluePlugPhase.Flying; i++)
        {
            rig.Socket.Move(rig.Socket.AnchorVelocity * Time.fixedDeltaTime);
            rig.Step();
        }
        Check(Vector2.Distance(firstTarget, rig.Socket.Point) > 1 &&
            rig.Plug.Phase == BluePlugPhase.Connected && rig.Visuals.Clicks == 1 &&
            Vector2.Distance(rig.ReadDrawnRadial(), rig.Socket.Point - (rig.Body.position + Vector2.up * .34f)) < .001f,
            "The throw travels past the default range and seats at the moving socket's updated position");
        Check(rig.Plug.ArrivalSequence == 0 && rig.Plug.LastArrival == null && !rig.Socket.Reserved,
            "Seating a moving plug alone does not report a tow arrival or claim the movement lease");

        rig.Frame(c: true);
        Check(rig.Plug.Phase == BluePlugPhase.Towing && rig.Socket.Reserved,
            "Moving tow reserves the socket and accepts the predicted intercept within its authored range");
        rig.Step(integrate: true);
        Check(MathF.Abs(rig.Body.linearVelocity.magnitude - rig.Socket.TowSpeedOverride) < .001f,
            "Tow uses the authored socket's speed override");
        for (int i = 0; i < 60 && rig.Plug.Phase == BluePlugPhase.Towing; i++)
        {
            rig.Socket.Move(rig.Socket.AnchorVelocity * Time.fixedDeltaTime);
            rig.Step(integrate: true);
        }
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.Plug.ArrivalSequence == 1 &&
            rig.Plug.LastArrival == rig.Socket && Vector2.Distance(rig.Body.position, rig.Socket.TowPoint) < .65f,
            "The moving tow reports exactly one arrival at contact with the authored socket");
        Check(!rig.Socket.Reserved && rig.Socket.Releases == 1 && rig.Socket.LastReleaseArrived &&
            MathF.Abs(rig.Body.linearVelocity.magnitude - rig.Socket.TowSpeedOverride * rig.Socket.TowCarryOverride) < .001f,
            "A confirmed arrival releases the lease as arrived and uses the socket's carry override");

        foreach (int side in new[] { -1, 1 })
        {
            var seated = new Rig();
            seated.Socket.EntryDirection = Vector2.right * side;
            seated.Dock();
            Vector2 grip = seated.Socket.Point - seated.Socket.EntryDirection * 3;
            seated.Plug.SetVisualGrips(grip);
            seated.Frame();
            Check(Vector2.Distance(seated.Visuals.PinDirection, seated.Socket.EntryDirection) < .001f &&
                Vector2.Distance(seated.Visuals.DrawnTip, seated.Socket.Point) < .001f,
                "The seated prongs hold the outlet's orientation on either wall");
            // 백 경위가 반대편으로 돌아서도 핀은 소켓과 같이 움직이고 전선 시작점만 손을 따라간다.
            seated.Socket.Move(new Vector2(.4f, .65f));
            seated.Socket.EntryDirection = Vector2.up;
            grip = seated.Socket.Point + Vector2.right * side * 3;
            seated.Plug.SetVisualGrips(grip);
            seated.Frame();
            Check(Vector2.Distance(seated.Visuals.Hand, grip) < .001f &&
                Vector2.Distance(seated.Visuals.DrawnTip, seated.Socket.Point) < .001f &&
                Vector2.Distance(seated.Visuals.PinDirection, Vector2.up) < .001f &&
                seated.Visuals.Clicks == 1 && seated.Player.VelocityWrites == 0,
                "A moving and rotating outlet stays seated as the officer changes sides without another click or pull");
        }

        rig = new Rig();
        rig.Socket.PatrolDriven = true;
        rig.Dock();
        rig.Frame(c: true);
        rig.Step(integrate: true);
        rig.Socket.Move(Vector2.up * 3);
        for (int i = 0; i < 80 && rig.Plug.Phase == BluePlugPhase.Towing; i++) rig.Step(integrate: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.Plug.ArrivalSequence == 0 &&
            rig.Plug.LastArrival == null && rig.Socket.Releases == 1 && !rig.Socket.LastReleaseArrived,
            "Reaching a stale predicted endpoint after the socket changes course is a miss, not an arrival");

        rig = new Rig();
        rig.Socket.PatrolDriven = true;
        rig.Dock();
        rig.Frame(c: true);
        rig.Step(integrate: true);
        Vector2 formerTowPoint = rig.Socket.TowPoint;
        Vector2 jumpBehindOfficer = rig.Body.position - (formerTowPoint - rig.Body.position).normalized * 2;
        rig.Socket.Move(jumpBehindOfficer - formerTowPoint);
        rig.Step(integrate: true);
        Check(rig.Plug.Phase == BluePlugPhase.Towing && rig.Plug.ArrivalSequence == 0 && rig.Socket.Reserved,
            "A socket teleport that crosses the officer only under interpolation does not report a contact");
        for (int i = 0; i < 80 && rig.Plug.Phase == BluePlugPhase.Towing; i++) rig.Step(integrate: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.Plug.ArrivalSequence == 0 &&
            rig.Socket.Releases == 1 && !rig.Socket.LastReleaseArrived,
            "After a teleport miss the tow still ends and releases the socket without consuming it");

        rig = new Rig();
        rig.Socket.PatrolDriven = true;
        rig.Dock();
        rig.Frame(c: true);
        rig.Socket.enabled = false;
        rig.Step(integrate: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && !rig.Socket.Reserved &&
            rig.Socket.Releases == 1 && !rig.Socket.LastReleaseArrived && rig.Plug.ArrivalSequence == 0,
            "Disabling a reserved socket cancels and releases the tow without an arrival event");

        rig = new Rig();
        rig.Socket.PatrolDriven = true;
        rig.Dock();
        rig.Frame(c: true);
        rig.Step(integrate: true);
        rig.Frame(g: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready && !rig.Socket.Reserved &&
            rig.Socket.Releases == 1 && !rig.Socket.LastReleaseArrived && rig.Plug.ArrivalSequence == 0,
            "Manual recall releases the reserved socket without consuming the arrival");
    }

    static void CheckCleanup()
    {
        var rig = new Rig();
        rig.Dock();
        rig.Frame(g: true);
        Check(rig.Plug.Phase == BluePlugPhase.Retracting && rig.Player.VelocityWrites == 0,
            "G manually recalls a docked plug without changing locomotion");
        rig.Frame();
        rig.FinishRecall(cooldown: true);
        rig.Dock();
        rig.Frame(c: true);
        int resets = rig.Visuals.FeedbackResets;
        rig.Player.Hurt(new Vector2(-3, 2));
        Check(rig.Plug.Phase == BluePlugPhase.Ready && rig.Visuals.VisiblePhase == BluePlugPhase.Ready &&
            rig.Visuals.FeedbackResets == resets + 1 && rig.Body.linearVelocity.x == -3,
            "Hurt hides the link and hard-resets feedback while keeping the existing knockback");

        rig.Player.Respawn(new Vector2(0, 2));
        rig.Frame();
        rig.Frame(g: true);
        rig.Player.Respawn(new Vector2(0, 2));
        rig.Frame(g: true);
        Check(rig.Plug.Phase == BluePlugPhase.Ready, "A held G across respawn cannot preserve or restart winding");
        rig.Frame();
        rig.Throw();
        rig.Player.Respawn(new Vector2(0, 2));
        rig.FinishFlight();
        Check(rig.Plug.Phase == BluePlugPhase.Ready, "Respawn cancels a released plug in flight");
        rig.Frame();
        rig.Dock();
        rig.Plug.ConfigureRoom(Array.Empty<BluePlugSocket>(), new Camera(), null, null);
        Check(!rig.Plug.Available && rig.Plug.Phase == BluePlugPhase.Ready &&
            rig.Visuals.VisiblePhase == BluePlugPhase.Ready, "Room change discards the socket and rendering");
    }

    static void CheckHandAnchors()
    {
        var rig = new Rig();
        Vector2 left = new Vector2(-.42f, 2.21f);
        Vector2 right = new Vector2(.63f, 2.48f);
        Vector2 centre = rig.Body.position + Vector2.up * .34f;
        rig.Plug.SetVisualGrips(left, right);
        rig.Visuals.OrbitHeight = .73f;
        rig.Frame(primary: true);
        Vector2 physicalRadius = Vector2.zero;
        for (int tick = 0; tick < 60; tick++)
        {
            rig.Step();
            rig.ReadDrawnRadial();
            physicalRadius = rig.Visuals.PhysicalTip - centre;
            if (physicalRadius.x > .45f && physicalRadius.y > .45f) break;
        }

        Check(rig.Visuals.WindCenter.HasValue && Vector2.Distance(rig.Visuals.WindCenter.Value, centre) < .001f &&
            rig.Visuals.WindingGrip.HasValue && Vector2.Distance(rig.Visuals.WindingGrip.Value, right) < .001f &&
            Vector2.Distance(rig.Visuals.Hand, left) < .001f && rig.Player.VelocityWrites == 0,
            "Wind rendering uses the reel hand and the guide hand independently of the physical orbit");
        Check(physicalRadius.x > .45f && physicalRadius.y > .45f &&
            MathF.Abs(rig.ReadDrawnRadial().y - physicalRadius.y * .73f) < .001f,
            "A clear physical orbit renders with a squashed vertical axis");

        Physics2D.WallX = centre.x + physicalRadius.x * .7f;
        float wallFraction = (Physics2D.WallX - centre.x - .1f) / physicalRadius.x;
        Physics2D.WallTop = centre.y + physicalRadius.y * wallFraction * .865f - .1f;
        Check(Vector2.Distance(rig.ReadDrawnRadial(), physicalRadius) < .001f,
            "An orbit that would visually cross a blocker falls back to its physical tip");
        Physics2D.WallX = float.NaN;

        rig.Frame();
        rig.FinishWind();
        rig.Frame();
        Check(rig.Visuals.VisiblePhase == BluePlugPhase.Flying && !rig.Visuals.WindingGrip.HasValue &&
            Vector2.Distance(rig.Visuals.Hand, left) < .001f, "Throw release drops the right-hand guide immediately");

        rig.FinishFlight();
        Vector2 walked = new Vector2(.3f, 0);
        rig.Body.position += walked;
        rig.Plug.SetVisualGrips(left + walked);
        rig.Frame();
        Check(rig.Visuals.VisiblePhase == BluePlugPhase.Connected && !rig.Visuals.WindingGrip.HasValue &&
            Vector2.Distance(rig.Visuals.Hand, left + walked) < .001f && rig.Player.VelocityWrites == 0,
            "After latching, the cable follows the animated left hand without driving the body");
        rig.Frame(c: true);
        Check(rig.Visuals.VisiblePhase == BluePlugPhase.Towing && !rig.Visuals.WindingGrip.HasValue &&
            Vector2.Distance(rig.Visuals.Hand, left + walked) < .001f,
            "Active tow keeps the cable on the left-hand reel only");

        int resets = rig.Visuals.FeedbackResets;
        for (int tick = 0; tick < 80 && rig.Plug.Phase == BluePlugPhase.Towing; tick++) rig.Step(integrate: true);
        Check(rig.Visuals.VisiblePhase == BluePlugPhase.Ready && rig.Visuals.Arrivals == 1 && rig.Visuals.FeedbackResets == resets,
            "Confirmed arrival hides the cable before a new render frame and preserves arrival feedback");
    }

    static void CheckPowerOutletSignals()
    {
        var rig = new Rig { Skill = BluePlugSkill.None };
        rig.Socket.PowersEquipment = true;
        int seats = 0, releases = 0, resets = 0;
        rig.Plug.SeatedSocketChanged += current =>
        {
            if (current == rig.Socket) ++seats;
            else if (current == null) ++releases;
            else throw new InvalidOperationException("Another socket received this station's power");
        };
        rig.Plug.LinkReset += () => ++resets;
        rig.Frame();
        Check(rig.Socket.Indicator == BluePlugSocketState.Ready, "Basic power is shown as usable before movement training");
        rig.Throw();
        Check(seats == 0 && rig.Plug.SeatedSocket == null, "Aiming and flight cannot energize equipment");
        rig.FinishFlight();
        rig.Frame(c: true);
        rig.Frame();
        rig.Frame(v: true);
        Check(seats == 1 && releases == 0 && rig.Plug.SeatedSocket == rig.Socket && rig.Player.VelocityWrites == 0,
            "First contact powers the device once with locked movement and a stationary officer");
        rig.Frame(g: true);
        Check(releases == 1 && !rig.Plug.SeatedSocket, "Manual recall cuts the current before the plug comes home");
        rig.Plug.ResetLink();
        rig.Plug.ResetLink();
        Check(resets == 2 && releases == 1, "Hard resets do not duplicate a previously delivered disconnect");
        rig.Frame();
        rig.Dock();
        rig.Plug.ResetLink();
        Check(seats == 2 && releases == 2 && resets == 3, "Reset a powered station and remove its live connection exactly once");
    }

    static void CheckRelayEdges()
    {
        var rig = new Rig();
        rig.Frame(primary: true);
        for (int i = 0; i < 35; i++) rig.Step();
        Check(rig.Plug.ThrowSequence == 0 && rig.Plug.ManualRecallSequence == 0,
            "Holding a winding plug is neither a released throw nor a connected manual recall");
        rig.Frame(primary: true, mode: PlayerMode.Attack);
        rig.FinishRecall(true);
        Check(rig.Plug.ThrowSequence == 0 && rig.Plug.ManualRecallSequence == 0,
            "Cancelling a windup by changing mode cannot advance a relay");
        rig.Frame(mode: PlayerMode.Hook);
        rig.Throw();
        Check(rig.Plug.ThrowSequence == 1 && rig.Plug.ManualRecallSequence == 0,
            "A real release increments the throw sequence exactly once before docking");
        rig.Frame(g: true);
        rig.FinishRecall(true);
        Check(rig.Plug.ThrowSequence == 1 && rig.Plug.ManualRecallSequence == 0 && !rig.Plug.LastManualRecall,
            "Reeling a plug out of free flight does not count as disconnecting a socket");
        rig.Frame();
        rig.Dock();
        rig.Frame(g: true);
        rig.FinishRecall(true);
        rig.Frame(g: true);
        Check(rig.Plug.ThrowSequence == 2 && rig.Plug.ManualRecallSequence == 1 && rig.Plug.LastManualRecall == rig.Socket,
            "G on a seated socket records its identity even after retraction finishes and does not repeat on hold");
        rig.Plug.ResetLink();
        Check(rig.Plug.ManualRecallSequence == 1 && rig.Plug.ThrowSequence == 2 && !rig.Plug.LastManualRecall,
            "Hard resets invalidate the socket without rewinding edges observed by a room");
        rig.Frame();
        rig.Dock();
        rig.Socket.enabled = false;
        rig.Frame(g: true);
        rig.Step();
        Check(rig.Plug.ManualRecallSequence == 1 && !rig.Plug.LastManualRecall,
            "A socket closing or despawning while connected cannot impersonate a G recall");
    }

    static Rig WallFixture(int outwardSign = -1)
    {
        var rig = new Rig { Skill = BluePlugSkill.None };
        Physics2D.WallX = 5;
        rig.Socket.Point = new Vector2(5 + outwardSign * .17f, 4);
        rig.Socket.WallOutwardSign = outwardSign;
        rig.Socket.WallFace = Physics2D.Wall;
        rig.Body.position = new Vector2(5 + outwardSign * .38f, 4);
        rig.Step();
        return rig;
    }

    static void CheckWallRest()
    {
        foreach (int side in new[] { -1, 1 })
        {
            var rig = WallFixture(side);
            Vector2 origin = rig.Body.position;
            rig.Body.linearVelocity = new Vector2(0, -3);
            rig.Step();
            Check(!rig.Plug.WallResting && rig.Body.linearVelocity.y == -3,
                "Walking by a contact does not engage a grip without direction input");
            rig.Input.Move = Vector2.right * -side;
            var wrongFace = new GameObject().Add<Collider2D>();
            rig.Socket.WallFace = wrongFace;
            rig.Step();
            Check(!rig.Plug.WallResting, "Ordinary walls and a different overlapping collider cannot start a grip");
            rig.Socket.WallFace = Physics2D.Wall;
            rig.Body.position = origin + Vector2.up * 1.05f;
            rig.Step();
            Check(!rig.Plug.WallResting, "The same tall wall outside the marked socket cannot be held");
            rig.Body.position = origin + Vector2.right * (side * .20f);
            rig.Step();
            Check(!rig.Plug.WallResting, "Standing near the marker without contacting its wall does not snap the actor");
            rig.Body.position = origin;
            rig.Step();
            Check(rig.Plug.RestingSocket == rig.Socket && rig.Plug.SeatedSocket == null && rig.Plug.Phase == BluePlugPhase.Ready,
                "The airborne character can choose to hold the marked socket with neither movement skill nor an electrical connection");
            rig.Input.Move = Vector2.zero;
            for (int tick = 0; tick < 120; tick++) rig.Step(true);
            Check(rig.Plug.WallResting && (origin - rig.Body.position).sqrMagnitude < .000001f,
                "Waiting motionless with the stick neutral preserves the contact through 120 physics ticks");
            for (int tick = 0; tick < 30; tick++)
            {
                rig.Socket.Move(new Vector2(0, .05f));
                rig.Step(true);
            }
            Check(rig.Plug.WallResting && Math.Abs(rig.Body.position.y - origin.y - 1.5f) < .0001f,
                "A moving mechanical grip carries the officer at its own speed");
            rig.Socket.Move(new Vector2(0, .05f));
            rig.Input.Buffer.Put(PlayerCommand.Jump, Time.frameCount, 1);
            rig.Step(true);
            Check(!rig.Plug.WallResting && rig.Player.State == PlayerStateId.WallJump &&
                side * rig.Body.linearVelocity.x > 9 && rig.Body.linearVelocity.y > 15 &&
                rig.Plug.WallJumpSequence == 1 && rig.Plug.LastWallJump == rig.Socket,
                "A jump leaves immediately on the free side and inherits the carrier's upward motion");
        }

        var puzzle = WallFixture();
        var supply = new GameObject().Add<BluePlugSocket>();
        supply.Point = new Vector2(.5f, 7);
        supply.TowPoint = supply.Point + Vector2.up;
        supply.PowersEquipment = true;
        puzzle.Body.position = new Vector2(0, 3);
        puzzle.Plug.ConfigureRoom(new[] { puzzle.Socket, supply }, new Camera(), () => puzzle.Skill, null);
        puzzle.Input.Pointer = supply.Point;
        puzzle.Frame();
        puzzle.Dock();
        puzzle.Body.position = new Vector2(4.62f, 4);
        puzzle.Input.Move = Vector2.right;
        puzzle.Step(true);
        Check(puzzle.Plug.SeatedSocket == supply && puzzle.Plug.RestingSocket == puzzle.Socket,
            "The grip on one wall and the circuit powered by the plug on another remain independent");
        puzzle.Frame(c: true);
        Check(puzzle.Plug.WallResting && !puzzle.Plug.Moving, "A locked tow preserves the mechanical grip");
        puzzle.Skill = BluePlugSkill.Tow;
        puzzle.Frame();
        puzzle.Frame(c: true);
        Check(!puzzle.Plug.WallResting && puzzle.Plug.Phase == BluePlugPhase.Towing && puzzle.Plug.SeatedSocket == supply,
            "A newly pressed, unlocked tow launches from the grip toward the independently powered socket");

        var drop = WallFixture();
        drop.Input.Move = Vector2.right;
        drop.Step(true);
        drop.Input.Move = new Vector2(0, -1);
        drop.Input.Buffer.Put(PlayerCommand.Jump, Time.frameCount, 1);
        drop.Step(true);
        Check(!drop.Plug.WallResting && drop.Player.State == PlayerStateId.Fall &&
            !drop.Input.Buffer.Has(PlayerCommand.Jump, Time.frameCount) &&
            drop.Plug.WallJumpSequence == 0 && !drop.Plug.LastWallJump, "Down drops without registering a jump edge");
        drop.Input.Move = Vector2.right;
        for (int tick = 0; tick < 5; tick++) drop.Step();
        Check(!drop.Plug.WallResting, "Reaching into the same wall immediately after dropping cannot cancel the release");

        var blocked = WallFixture();
        blocked.Input.Move = Vector2.right;
        blocked.Step(true);
        blocked.Socket.Move(new Vector2(0, .12f));
        Physics2D.CeilingY = blocked.Body.position.y + .42f;
        blocked.Step(true);
        Check(!blocked.Plug.WallResting && Math.Abs(blocked.Body.position.y - 4) < .0001f,
            "Moving support stops carrying before it can crush the actor against an overhead blocker");

        var reset = WallFixture();
        reset.Input.Move = Vector2.right;
        reset.Step(true);
        reset.Player.Hurt(new Vector2(-3, 5));
        Check(!reset.Plug.WallResting && reset.Body.linearVelocity.y == 5,
            "Taking damage detaches without eating the incoming knockback");
        reset.Player.Respawn(new Vector2(4.62f, 4));
        for (int tick = 0; tick < 45; tick++) reset.Step();
        Check(!reset.Plug.WallResting, "Held input across a retry does not re-grip");
        reset.Input.Move = Vector2.zero;
        reset.Step();
        reset.Input.Move = Vector2.right;
        reset.Step();
        Check(reset.Plug.WallResting, "Releasing and pressing again after retry allows a clean grip");
        reset.Plug.ConfigureRoom(Array.Empty<BluePlugSocket>(), new Camera(), () => BluePlugSkill.None, null);
        Check(!reset.Plug.WallResting && !reset.Plug.Available && reset.Player.State == PlayerStateId.Fall,
            "Changing rooms clears the previous carrier and returns control to movement");
    }

    static void CheckBladeHandle()
    {
        var rig = new Rig { Skill = BluePlugSkill.None };
        var handle = new GameObject().Add<BluePlugSocket>();
        handle.Point = rig.Socket.Point + new Vector2(.3f, 0);
        handle.TowPoint = handle.Point;
        var blade = new GameObject().Add<SummonedBlade>();
        blade.Handle = handle;
        blade.State = SummonedBladeState.Falling;
        rig.Plug.BindWeapon(blade);
        rig.Dock();
        Check(rig.Plug.SeatedSocket == handle && !blade.Equipped,
            "The moving grip outranks an overlapping fixture, and docking alone does not equip");
        blade.State = SummonedBladeState.Stuck;
        for (int i=0; i<100 && !blade.Equipped; i++) {rig.Frame(); rig.Step(true);}
        Check(blade.Equipped && blade.Perfect && blade.Distance>5 && rig.Mode.Mode==PlayerMode.Attack,
            "Arrival at a grip caught during its fall equips and preserves perfect credit without a tow skill");
        rig = new Rig { Skill = BluePlugSkill.None };
        handle.Point = rig.Socket.Point;
        handle.TowPoint = new Vector2(7,6);
        blade.State = SummonedBladeState.Stuck;
        rig.Plug.BindWeapon(blade);
        rig.Dock();
        Physics2D.WallX = 3;
        rig.Frame(); rig.Step(true);
        Check(!blade.Equipped && rig.Player.VelocityWrites==0 && rig.Plug.Phase!=BluePlugPhase.Towing,
            "Closing a shutter after the blade docks prevents equipping through it");
        rig = new Rig();
        rig.Plug.BindWeapon(blade);
        rig.Throw();
        blade.HandleGeneration++;
        rig.Step();
        Check(rig.Plug.Phase == BluePlugPhase.Retracting && !blade.Equipped,
            "A re-planned marker cannot steal a throw launched at an earlier blade generation");
    }

    static void CheckLoadoutControls()
    {
        var rig=new Rig();
        int particles=70;
        var loadout=PlayerSummonLoadout.Install(rig.Plug,new Camera(),()=>particles,n=>{
            if(particles<n)return false;particles-=n;return true;
        },n=>particles+=n,_=>{});
        var blade=rig.Plug.GetComponent<SummonedBlade>();
        var tick=typeof(PlayerSummonLoadout).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);
        void Frame(params PlayerCommand[] commands)
        {
            ++Time.frameCount;
            foreach(var key in commands)rig.Input.Buffer.Put(key,Time.timeAsDouble,.15f);
            tick.Invoke(loadout,null);
        }
        Frame(PlayerCommand.LaserPress,PlayerCommand.LaserRelease,PlayerCommand.Overload);
        Check(particles==70 && loadout.LaserSequence==0 && blade.State==SummonedBladeState.None,
            "Unequipped F and R cannot spend charge or fire");
        Frame(PlayerCommand.Summon);
        Frame(PlayerCommand.Summon);
        Check(particles==50 && blade.State==SummonedBladeState.Summoning,"Q summons once and spends from the patrol wallet");
        blade.State=SummonedBladeState.Equipped;
        Frame();
        Frame(PlayerCommand.LaserPress);
        for(int i=0;i<52;i++)Frame();
        Check(loadout.LaserSequence==0,"Holding F charges until a fresh release");
        Frame(PlayerCommand.LaserRelease);
        Check(loadout.LaserSequence==1 && particles==35 && rig.Plug.Electricity.LastDamage>240,"Full-charge beam fires once and spends its tier cost");
        Frame(PlayerCommand.Primary);
        for(int i=0;i<15;i++)Frame();
        Check(rig.Plug.Electricity.Hits==1 && rig.Plug.Electricity.LastDamage==35 && loadout.CutStep==1,
            "Equipped primary applies only one impact during the first cut");
        Frame(PlayerCommand.Primary);
        bool idleGap=false;
        for(int i=0;i<24;i++)
        {
            Frame();
            if(loadout.CutStep==1 && !loadout.AnimatingCut) idleGap=true;
        }
        var strikeFx=rig.Plug.GetComponent<FilamentStrikeFX>();
        Check(!idleGap && strikeFx.Starts==2 && strikeFx.Swishes==2 && strikeFx.Contacts==2,
            "Buffered melee starts the second pose without an idle frame or repeated contact sounds");
        Check(rig.Plug.Electricity.Hits==2 && loadout.CutStep==2,
            "Late pre-input buffers the second cut even while the contact frame pauses");
        loadout.CarryWeaponOnNextSpawn();rig.Player.Respawn(Vector2.zero);
        Check(loadout.Equipped && particles==35 && !loadout.AnimatingCut,"Changing streets preserves equipped gear and cancels the old cut");
        rig.Plug.Electricity.MeleeLands=false;
        Frame(PlayerCommand.Primary);
        for (int i=0;i<16;i++) Frame();
        Check(strikeFx.Starts==3 && strikeFx.Swishes==3 && strikeFx.Contacts==2,
            "A dry cut reaches the swing cue without producing a false enemy contact");
        Frame(PlayerCommand.Detach,PlayerCommand.LaserRelease,PlayerCommand.Overload);
        Check(!loadout.Equipped && loadout.BlocksInteract && blade.State==SummonedBladeState.Stuck && loadout.LaserSequence==1,
            "E detaches before R/F and cannot activate a nearby room interaction on the same edge");
        Frame(PlayerCommand.Detach);
        Check(!loadout.BlocksInteract,"E is available to ordinary street interactions without the blade");
        rig.Player.Respawn(Vector2.zero);
        Check(blade.State==SummonedBladeState.None && !loadout.Equipped,"Retry clears an installed blade");
    }

    static void Main()
    {
        CheckThrow();
        CheckPrimaryAndCharge();
        CheckRetract();
        CheckBlockers();
        CheckAimAndLatchIndicators();
        CheckTow();
        CheckPowerOutletSignals();
        CheckWallRest();
        CheckRelayEdges();
        CheckSwing();
        CheckMovingSocket();
        CheckBladeHandle();
        CheckLoadoutControls();
        CheckCleanup();
        CheckHandAnchors();
        Console.WriteLine($"PASS: {verified} actual Blue Plug controller checks with keyboard, collision and physics service doubles.");
    }
}
