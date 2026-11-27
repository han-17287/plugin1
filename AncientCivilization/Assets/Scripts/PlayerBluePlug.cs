using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Plugin.PlayerCore
{
    [DefaultExecutionOrder(40)]
    [RequireComponent(typeof(PlayerStateMachine), typeof(BluePlugVisuals))]
    public sealed class PlayerBluePlug : MonoBehaviour
    {
        [SerializeField] BluePlugData data;
        readonly BluePlugSession session = new BluePlugSession();
        readonly RaycastHit2D[] hits = new RaycastHit2D[24];
        BluePlugSocket[] sockets = Array.Empty<BluePlugSocket>();
        Func<BluePlugSkill> skills;
        Action<string> notice;
        PlayerStateMachine player;
        PlayerInputReader input;
        PlayerModeController mode;
        SummonedBlade blade;
        ElectricPlugCombat electricity;
        bool bladeFlight, caughtFalling, gripTargeted;
        uint gripGeneration;
        Vector2 bladeStart;
        double previewAfter;
        public ElectricPlugCombat Electricity => electricity;
        public void BindWeapon(SummonedBlade summoned) => blade = summoned;
        public bool SuppliesAt(Vector2 point) => blade && blade.Supplies(point) && blade.Handle &&
            TipClear(blade.Handle.Point, point - blade.Handle.Point);
        bool IsHandle(BluePlugSocket port) => blade && blade.Catchable && port && port == blade.Handle;
        Rigidbody2D body;
        CapsuleCollider2D shape;
        BluePlugVisuals visuals;
        Camera view;
        ContactFilter2D solids;
        BluePlugSocket socket;
        BluePlugSocket releaseSocket, restSocket;
        Vector2 restOffset, restPreviousPoint, restVelocity;
        float restDelay;
        bool requireRestNeutral;
        Vector2? visualLeftGrip, visualRightGrip;
        Vector2 plug, flightGoal, flightDirection, towGoal, towDirection;
        Vector2 releaseAim, previousBody, previousTowPoint, previousPivot, swingPivotVelocity;
        float windAngle, windTurns, windSign, windHeld;
        float flightRange, flightBudget, flightSpeed, recastLeft, towSpeed, towCarry;
        float swingRadius, swingSpeed;
        bool ownsData, suppressUntilRelease, suppressPrimaryUntilRelease, primaryWind, releaseQueued;
        bool reservedSocket, towFollowsPatrol, towHasStep;

        public BluePlugPhase Phase => session.Phase;
        // 회전 궤도와 조준 방향을 분리한다. 손은 돌고, 경위의 시선은 던질 곳을 본다.
        public Vector2? VisualAimDirection { get; private set; }
        public Vector2 VisualTargetDelta => Phase switch
        {
            BluePlugPhase.Ready => Vector2.zero,
            BluePlugPhase.Winding => WindRadial(),
            BluePlugPhase.Flying => flightDirection,
            _ => VisualEnd - Hand
        };
        // 실제 접점이 맞물린 동안만 설비에 전류를 공급한다. 회전·비행 중 목표와 구별한다.
        public BluePlugSocket SeatedSocket { get; private set; }
        public event Action<BluePlugSocket> SeatedSocketChanged;
        public event Action LinkReset;

        // 붙어 있는 손잡이와 전원을 공급 중인 소켓은 서로 다를 수 있다.
        public bool WallResting => player && player.State == PlayerStateId.WallRest && restSocket;
        public BluePlugSocket RestingSocket => WallResting ? restSocket : null;
        public Vector2? WallGripPoint
        {
            get
            {
                if (!WallResting || !restSocket.CanWallRest) return null;
                // 표시판 중심은 벽 앞쪽에 떠 있다. 손·발은 충돌한 지정 고정대의 면에 맞춘다.
                var bounds = restSocket.WallFace.bounds;
                float x = restSocket.WallOutwardSign < 0 ? bounds.min.x : bounds.max.x;
                return new Vector2(x, restSocket.Point.y);
            }
        }
        public bool Available => sockets.Length != 0 || blade;
        public bool Moving => Phase == BluePlugPhase.Towing || Phase == BluePlugPhase.Swinging;
        public bool HazardInvulnerable => Phase == BluePlugPhase.Towing;
        public BluePlugSocket LastArrival { get; private set; }
        public int ArrivalSequence { get; private set; }
        // 방의 연계 판정은 물리 틱 사이에서 회수·재투척이 끝나도 실제 입력을 구별한다.
        public uint ManualRecallSequence { get; private set; }
        public uint WallJumpAtManualRecall { get; private set; }
        public BluePlugSocket LastManualRecall { get; private set; }
        public uint ThrowSequence { get; private set; }
        public uint WallJumpSequence { get; private set; }
        public BluePlugSocket LastWallJump { get; private set; }
        public string Status
        {
            get
            {
                if (WallResting && (Phase == BluePlugPhase.Ready || Phase == BluePlugPhase.Connected))
                    return "벽 고정 · Space로 도약 · S로 내려놓기";
                switch (Phase)
                {
                    case BluePlugPhase.Winding: return releaseQueued ? "플러그 던지기" :
                        primaryWind ? "회전 중 · 좌클릭을 놓으면 투척" : "회전 중 · G를 놓으면 투척";
                    case BluePlugPhase.Flying: return "플러그 투척 중";
                    case BluePlugPhase.Retracting: return "플러그를 감는 중";
                    case BluePlugPhase.Connected: return "연결 · 걸어서 출발 위치를 고르세요";
                    case BluePlugPhase.Towing: return "직선 견인";
                    case BluePlugPhase.Swinging: return "A/D 스윙 가속 · V 이탈";
                    default:
                        if (recastLeft > 0) return "플러그 재장전 중";
                        return mode && mode.Mode == PlayerMode.Attack ? "블레이드 장착 · E 분리" : "좌클릭을 눌러 회전 · Q 블레이드 소환";
                }
            }
        }

        Vector2 Hand => body.position + Vector2.up * data.handHeight;
        Vector2 VisualEnd => Phase == BluePlugPhase.Winding || Phase == BluePlugPhase.Flying || !socket
            ? plug : socket.Point;
        BluePlugSkill Learned => skills?.Invoke() ?? BluePlugSkill.None;
        float Charge => data.Charge(windHeld);
        bool CanMove => player.State <= PlayerStateId.WallJump || WallResting;
        bool CanHandlePlug => player.State <= PlayerStateId.Dash || WallResting;

        void Awake()
        {
            player = GetComponent<PlayerStateMachine>();
            input = GetComponent<PlayerInputReader>();
            mode = GetComponent<PlayerModeController>();
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<CapsuleCollider2D>();
            visuals = GetComponent<BluePlugVisuals>();
            if (!data) data = Resources.Load<BluePlugData>("Traversal/DefaultBluePlug");
            if (!data)
            {
                data = ScriptableObject.CreateInstance<BluePlugData>();
                ownsData = true;
            }
            electricity = ElectricPlugCombat.Attach(gameObject, data);
            solids = new ContactFilter2D();
            solids.SetLayerMask(data.solids);
            solids.useTriggers = false;
        }

        void OnEnable()
        {
            player.ExternalStep += StepMotion;
            player.StateChanged += OnStateChanged;
            player.Respawned += ResetLink;
            mode.Changed += OnModeChanged;
        }

        void OnDisable()
        {
            player.ExternalStep -= StepMotion;
            player.StateChanged -= OnStateChanged;
            player.Respawned -= ResetLink;
            mode.Changed -= OnModeChanged;
            ResetLink();
        }

        void OnDestroy() { if (ownsData && data) Destroy(data); }

        public void ConfigureRoom(BluePlugSocket[] roomSockets, Camera cameraView,
            Func<BluePlugSkill> unlocked, Action<string> showNotice)
        {
            ResetLink();
            sockets = roomSockets ?? Array.Empty<BluePlugSocket>();
            view = cameraView;
            skills = unlocked;
            notice = showNotice;
        }

        public void ResetLink()
        {
            LeaveRest(0, Vector2.zero);
            requireRestNeutral = true;
            LastManualRecall = LastWallJump = null;
            Clear(Vector2.zero);
            electricity?.ResetCombat();
            visuals.ResetFeedback();
            LinkReset?.Invoke();
            recastLeft = 0;
            suppressUntilRelease = true;
            suppressPrimaryUntilRelease = true;
            foreach (var item in sockets)
                if (item) item.SetIndicator(BluePlugSocketState.Idle, Learned);
        }

        // 왼손은 릴, 오른손은 회전 중 안내점이다. 두 좌표 모두 화면에 그리는 전선에만 사용한다.
        public void SetVisualGrips(Vector2? leftGrip, Vector2? rightGrip = null)
        {
            visualLeftGrip = leftGrip;
            visualRightGrip = rightGrip;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && Available) ResetLink();
        }

        void OnStateChanged(PlayerStateId previous, PlayerStateId next)
        {
            if (previous == PlayerStateId.WallRest && next != previous)
            {
                restSocket = null;
                restVelocity = Vector2.zero;
                restDelay = data.wallRelatchDelay;
            }
            if (next == PlayerStateId.Hurt || next == PlayerStateId.Dead)
                ResetLink();
            else if (Moving && next != PlayerStateId.GrappleDash)
                Clear(Vector2.zero);
        }

        void OnModeChanged(PlayerMode next)
        {
            if (next == PlayerMode.Hook) return;
            suppressPrimaryUntilRelease = true;
            if (Phase == BluePlugPhase.Winding) BeginRecall();
            else if (Phase != BluePlugPhase.Ready && Phase != BluePlugPhase.Retracting) Clear(Vector2.zero);
        }

        void Update()
        {
            if (!Available || !input.Ready || !view) return;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (input.Paused || !Application.isFocused)
            {
                if (Phase == BluePlugPhase.Winding) Clear(Vector2.zero);
                suppressUntilRelease = true;
                suppressPrimaryUntilRelease = true;
                return;
            }
            bool plugMode = mode.Mode == PlayerMode.Hook;
            if (plugMode) input.Buffer.Remove(PlayerCommand.Primary);
            bool gHeld = keyboard?.gKey.isPressed == true;
            bool cHeld = keyboard?.cKey.isPressed == true;
            bool vHeld = keyboard?.vKey.isPressed == true;
            // 재시도·메뉴를 누른 채 빠져나와도 키를 놓기 전에는 연결이 돌아오지 않는다.
            if (suppressUntilRelease && !gHeld && !cHeld && !vHeld)
                suppressUntilRelease = false;
            if (suppressPrimaryUntilRelease && !input.PrimaryHeld && !input.PrimaryPressed)
                suppressPrimaryUntilRelease = false;

            bool gPressed = !suppressUntilRelease && keyboard?.gKey.wasPressedThisFrame == true;
            bool cPressed = !suppressUntilRelease && keyboard?.cKey.wasPressedThisFrame == true;
            bool vPressed = !suppressUntilRelease && keyboard?.vKey.wasPressedThisFrame == true;
            bool mousePressed = plugMode && !suppressPrimaryUntilRelease && input.PrimaryPressed;

            Vector2 aim = view.ScreenToWorldPoint(input.Pointer);
            VisualAimDirection = (releaseQueued ? releaseAim : aim) - Hand;
            BluePlugSocket target = plugMode ? Pick(aim) : null;
            // 화면 갱신률과 무관하게 감전 표적 표시는 초당 20번까지만 탐색한다.
            if (Phase == BluePlugPhase.Winding && !releaseQueued && Time.timeAsDouble >= previewAfter &&
                !Aimable(target) && electricity)
            {
                previewAfter = Time.timeAsDouble + .05;
                electricity.TryAim(Hand, aim, data.ThrowRange(Charge, data.range), data.aimRadius, out _);
            }
            if (gPressed && plugMode)
            {
                if (Phase == BluePlugPhase.Ready && CanHandlePlug && plugMode) BeginWindup(false);
                else if (Moving)
                    Clear(Phase == BluePlugPhase.Swinging ? TangentVelocity() : Vector2.zero);
                else
                {
                    if (Phase == BluePlugPhase.Connected && Usable(SeatedSocket))
                    {
                        LastManualRecall = SeatedSocket;
                        WallJumpAtManualRecall = WallJumpSequence;
                        ManualRecallSequence++;
                    }
                    BeginRecall();
                }
            }
            else if (mousePressed && Phase == BluePlugPhase.Ready && CanHandlePlug)
                BeginWindup(true);
            else if (vPressed && Phase == BluePlugPhase.Swinging)
                Clear(TangentVelocity());
            else if (plugMode && CanMove && Phase == BluePlugPhase.Connected && cPressed != vPressed)
                TryMove(cPressed ? BluePlugSkill.Tow : BluePlugSkill.Swing);

            if (Phase == BluePlugPhase.Winding && !releaseQueued && !primaryWind && keyboard == null)
                BeginRecall();
            if (Phase == BluePlugPhase.Winding && !releaseQueued &&
                (primaryWind ? input.PrimaryReleased : !gHeld))
            {
                releaseQueued = true;
                releaseAim = aim;
                releaseSocket = Aimable(target) ? target : null;
            }
            RefreshIndicators(Phase == BluePlugPhase.Winding && releaseQueued ? releaseSocket : target);
#endif
        }

        BluePlugSocket Pick(Vector2 aim)
        {
            float radiusSq = data.aimRadius * data.aimRadius;
            var handle = blade && blade.Catchable ? blade.Handle : null;
            bool nearHandle = handle && (aim - handle.Point).sqrMagnitude <= radiusSq;
            if (nearHandle && Aimable(handle)) return handle;
            BluePlugSocket nearest = null, blocked = nearHandle ? handle : null;
            float best = radiusSq, blockedBest = nearHandle ? (aim - handle.Point).sqrMagnitude : radiusSq;
            foreach (var item in sockets)
            {
                if (!item || !item.isActiveAndEnabled) continue;
                float distance = (aim - item.Point).sqrMagnitude;
                if (distance > radiusSq) continue;
                if (!Aimable(item))
                {
                    if (distance <= blockedBest) { blocked = item; blockedBest = distance; }
                }
                else if (distance <= best) { nearest = item; best = distance; }
            }
            return nearest ? nearest : blocked;
        }

        bool Aimable(BluePlugSocket item)
        {
            if (!Usable(item)) return false;
            Vector2 delta = item.Point - Hand;
            float reach = data.ThrowRange(Phase == BluePlugPhase.Winding ? Charge : 0, item.Range(data.range));
            return delta.magnitude <= reach + .0001f && TipClear(Hand, delta) &&
                (Phase != BluePlugPhase.Winding || TipClear(plug, item.Point - plug));
        }

        static bool Usable(BluePlugSocket item) => item && item.Operational;

        void BeginWindup(bool fromPrimary)
        {
            if (recastLeft > 0 || !session.BeginWindup()) return;
            windSign = player.Facing >= 0 ? 1 : -1;
            windAngle = windSign > 0 ? Mathf.PI : 0;
            windTurns = windHeld = 0;
            primaryWind = fromPrimary;
            releaseQueued = false;
            plug = Hand;
        }

        Vector2 WindRadial() => new Vector2(Mathf.Cos(windAngle), Mathf.Sin(windAngle));

        void StepWindup(float dt)
        {
            Vector2 hand = Hand;
            if (!releaseQueued)
            {
                windHeld += dt;
                float turn = data.WindRate(Charge) * dt;
                windTurns += turn;
                windAngle = (windAngle + turn * windSign * (2f * Mathf.PI)) % (2f * Mathf.PI);
                Vector2 radial = WindRadial();
                float radius = data.WindRadius(Charge, windTurns);
                Vector2 next = hand + radial * ClearWindRadius(hand, radial, radius);
                plug = TipClear(plug, next - plug) ? next : hand;
                electricity?.Spin(hand, plug);
                return;
            }
            // 버튼을 놓은 뒤 첫 물리 틱에 마지막 팁 위치에서 출발한다.
            float charge = Charge;
            var clearSocket = Aimable(releaseSocket) ? releaseSocket : null;
            Vector2 aim = clearSocket ? clearSocket.Point : releaseAim;
            if (!clearSocket && electricity && electricity.TryAim(hand, releaseAim,
                data.ThrowRange(charge, data.range), data.aimRadius, out Vector2 enemyAim))
            {
                aim = enemyAim;
            }
            Vector2 delta = aim - plug;
            if (delta.sqrMagnitude < .0001f)
            {
                BeginRecall();
                return;
            }
            if (!session.Launch()) return;
            ThrowSequence++;
            float range = data.ThrowRange(charge, clearSocket ? clearSocket.Range(data.range) : data.range);
            // 조준 표시와 같은 손 기준 사거리를 사용하고, 실제 팁에서는 출발 위치만 보정한다.
            socket = clearSocket;
            gripTargeted = IsHandle(socket);
            if (gripTargeted) gripGeneration = blade.HandleGeneration;
            releaseSocket = null;
            releaseQueued = false;
            flightRange = range;
            flightBudget = socket ? Mathf.Max(range, delta.magnitude) : range;
            flightSpeed = data.ThrowSpeed(charge, data.throwSpeed);
            flightDirection = delta.normalized;
            flightGoal = socket ? socket.Point : plug + flightDirection * range;
        }

        float ClearWindRadius(Vector2 hand, Vector2 direction, float radius)
        {
            if (radius < .001f) return 0;
            int count = Physics2D.CircleCast(hand, data.tipRadius, direction, solids, hits, radius);
            if (count >= hits.Length) return 0;
            for (int i = 0; i < count; i++)
                if (Blocks(hits[i], direction, hand.y - data.tipRadius, false))
                    radius = Mathf.Min(radius, Mathf.Max(0, hits[i].distance - data.skin));
            return radius;
        }

        void FixedUpdate()
        {
            if (!Available || input.Paused || !Application.isFocused) return;
            float dt = Time.fixedDeltaTime;
            recastLeft = Mathf.Max(0, recastLeft - dt);
            electricity?.Tick(dt);
            FindWallRest(dt);
            // 같은 손잡이 오브젝트가 다른 위치에 재사용돼도 이전 투척이 새 칼로 순간이동하지 않는다.
            if (gripTargeted && (!IsHandle(socket) || gripGeneration != blade.HandleGeneration))
            {
                if (Moving) Clear(Vector2.zero); else BeginRecall();
                return;
            }
            // 손잡이만 장착을 위한 이동을 스스로 시작한다. 일반 콘센트에는 새 C/V 입력이 필요하다.
            if (Phase == BluePlugPhase.Connected && bladeFlight && IsHandle(socket))
            {
                if (CanMove) TryMove(BluePlugSkill.Tow);
                else BeginRecall();
            }
            if (Phase == BluePlugPhase.Retracting)
            {
                StepRecall(dt);
                return;
            }
            if (Phase == BluePlugPhase.Winding)
            {
                StepWindup(dt);
                return;
            }
            if ((Phase == BluePlugPhase.Connected || Moving) && !Usable(socket))
            {
                if (Moving) Clear(Vector2.zero);
                else BeginRecall();
                return;
            }
            if (Phase != BluePlugPhase.Flying) return;
            if (socket && !Usable(socket))
            {
                BeginRecall();
                return;
            }
            if (socket)
            {
                flightGoal = socket.Point;
                Vector2 nextDirection = flightGoal - plug;
                if (nextDirection.sqrMagnitude > .0001f) flightDirection = nextDirection.normalized;
            }
            float remaining = Vector2.Distance(plug, flightGoal);
            float travel = GrappleRules.Step(Mathf.Min(remaining, flightBudget), flightSpeed, dt);
            Vector2 step = flightDirection * travel;
            if (!TipClear(plug, step))
            {
                BeginRecall();
                notice?.Invoke("투척 경로가 막혔습니다.");
                return;
            }
            if (!socket && electricity && electricity.ThrowHit(plug, plug + step, out Vector2 contact))
            {
                plug = contact;
                BeginRecall();
                return;
            }
            plug += step;
            flightBudget -= travel;
            if (travel < remaining - .0001f)
            {
                if (flightBudget <= .0001f) BeginRecall();
                return;
            }
            if (!socket || !session.Dock(Time.frameCount))
            {
                BeginRecall();
                return;
            }
            plug = socket.Point;
            bladeFlight = IsHandle(socket);
            caughtFalling = bladeFlight && blade.State == SummonedBladeState.Falling;
            bladeStart = body.position;
            Seat(socket);
            visuals.Click();
        }

        void FindWallRest(float dt)
        {
            float horizontal = input.Move.x;
            if (Mathf.Abs(horizontal) < .3f) requireRestNeutral = false;
            restDelay = Mathf.Max(0, restDelay - dt);
            if (player.State > PlayerStateId.WallJump || requireRestNeutral || restDelay > 0 ||
                Mathf.Abs(horizontal) < .45f || input.Move.y < -.5f) return;
            foreach (var candidate in sockets)
            {
                if (!candidate || !candidate.CanWallRest || horizontal * candidate.WallOutwardSign >= 0) continue;
                Vector2 offset = body.position - candidate.Point;
                if (offset.x * candidate.WallOutwardSign < -.04f || Mathf.Abs(offset.x) > data.wallGripReach ||
                    Mathf.Abs(offset.y) > data.wallGripHeight) continue;
                // 실제로 플레이어 캡슐이 닿은 판이 이 소켓의 지정 판이어야 한다.
                Vector2 toward = Vector2.right * -candidate.WallOutwardSign;
                int count = body.Cast(toward, solids, hits, data.wallContactProbe);
                if (count >= hits.Length) continue;
                float faceDistance = float.PositiveInfinity, blockerDistance = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    var hit = hits[i];
                    if (hit.collider == candidate.WallFace && hit.normal.x * candidate.WallOutwardSign > .65f)
                        faceDistance = Mathf.Min(faceDistance, hit.distance);
                    else if (Blocks(hit, toward, shape.bounds.min.y, false))
                        blockerDistance = Mathf.Min(blockerDistance, hit.distance);
                }
                if (float.IsPositiveInfinity(faceDistance) || blockerDistance <= faceDistance ||
                    !player.BeginWallRest(candidate.WallOutwardSign)) continue;
                restSocket = candidate;
                restOffset = offset;
                restPreviousPoint = candidate.Point;
                restVelocity = Vector2.zero;
                return;
            }
        }

        void StepWallRest(float dt)
        {
            if (!restSocket || !restSocket.CanWallRest)
            {
                LeaveRest(0, Vector2.zero);
                return;
            }
            double now = Time.timeAsDouble;
            Vector2 point = restSocket.Point;
            Vector2 travel = point + restOffset - body.position;
            float limit = data.wallCarrierLimit * dt;
            if ((point - restPreviousPoint).sqrMagnitude > limit * limit ||
                travel.magnitude > limit + data.wallContactProbe || !WallCarryClear(travel))
            {
                LeaveRest(0, Vector2.zero);
                return;
            }
            restVelocity = (point - restPreviousPoint) / dt;
            restPreviousPoint = point;
            if (input.Buffer.Take(PlayerCommand.Drop, now) || input.Move.y < -.5f)
            {
                input.Buffer.Remove(PlayerCommand.Jump);
                LeaveRest(0, restVelocity);
                return;
            }
            if (input.Buffer.Take(PlayerCommand.Jump, now))
            {
                LastWallJump = restSocket;
                WallJumpSequence++;
                LeaveRest(restSocket.WallOutwardSign, restVelocity);
                return;
            }
            if (input.Buffer.Has(PlayerCommand.Dash, now) && player.TryExternalDash())
            {
                input.Buffer.Take(PlayerCommand.Dash, now);
                return;
            }
            if (input.Move.x * restSocket.WallOutwardSign > .45f)
            {
                LeaveRest(0, restVelocity);
                return;
            }
            player.SetExternalVelocity(travel / dt);
        }

        bool WallCarryClear(Vector2 travel)
        {
            float distance = travel.magnitude;
            if (distance < .0001f) return true;
            Vector2 direction = travel / distance;
            int count = body.Cast(direction, solids, hits, distance + data.skin);
            if (count >= hits.Length) return false;
            for (int i = 0; i < count; i++)
                if (hits[i].collider != restSocket.WallFace && Blocks(hits[i], direction, shape.bounds.min.y, false))
                    return false;
            return true;
        }

        void LeaveRest(int jumpSign, Vector2 carried)
        {
            if (player && player.State == PlayerStateId.WallRest) player.LeaveWallRest(jumpSign, carried);
            restSocket = null;
            restVelocity = Vector2.zero;
        }

        void TryMove(BluePlugSkill requested)
        {
            if (!Usable(socket))
            {
                BeginRecall();
                return;
            }
            var allowed = bladeFlight && IsHandle(socket) ? BluePlugSkill.Tow : Learned;
            if ((socket.Supported & requested) == 0 || (allowed & requested) == 0)
            {
                notice?.Invoke((socket.Supported & requested) == 0
                    ? "이 콘센트에서는 사용할 수 없는 이동입니다."
                    : "정비소에서 이 이동 기술을 해금하세요.");
                return;
            }
            bool tow = requested == BluePlugSkill.Tow;
            Vector2 goal = socket.TowPoint;
            float speed = socket.TowSpeed(data.towSpeed);
            bool movingTarget = tow && socket.PatrolDriven;
            Vector2 delta = goal - body.position;
            Vector2 targetSpeed = movingTarget ? socket.AnchorVelocity : Vector2.zero;
            float lead = 0;
            bool hasIntercept = !movingTarget || GrappleRules.TryIntercept(delta.x, delta.y,
                targetSpeed.x, targetSpeed.y, 0, speed, out lead);
            if (movingTarget && hasIntercept)
            {
                goal += targetSpeed * lead;
                delta = goal - body.position;
            }
            Vector2 radial = body.position - socket.Point;
            float length = radial.magnitude;
            float towRange = Mathf.Max(flightRange, socket.Range(data.range));
            bool clear = tow
                ? hasIntercept && delta.magnitude > data.arrivalTolerance &&
                    delta.magnitude <= towRange && BodyClear(delta)
                : length >= data.minimumRope && length <= data.maximumRope && CableClear(body.position);
            if (bladeFlight && delta.magnitude <= data.arrivalTolerance && BodyClear(delta))
            {
                CompleteTow(Time.fixedDeltaTime);
                return;
            }
            if (!session.Activate(requested, allowed, socket.Supported, clear, Time.frameCount))
            {
                if (!clear && bladeFlight) BeginRecall();
                if (!clear) notice?.Invoke(tow ? "몸이 지날 견인 경로를 비우세요." : "장애물을 피해 적절한 거리에서 스윙하세요.");
                return;
            }
            reservedSocket = socket.Reserve();
            if (!reservedSocket || !player.BeginExternal(PlayerStateId.GrappleDash))
            {
                Clear(Vector2.zero);
                return;
            }
            if (tow)
            {
                towGoal = goal;
                towDirection = delta.normalized;
                towSpeed = speed;
                towCarry = socket.TowCarry(data.towCarry);
                towFollowsPatrol = movingTarget;
                towHasStep = false;
            }
            else
            {
                swingRadius = length;
                previousPivot = socket.Point;
                swingPivotVelocity = Vector2.zero;
                float incoming = Vector2.Dot(body.linearVelocity, Perpendicular(radial / length));
                swingSpeed = Mathf.Clamp(incoming, -data.swingSpeedLimit, data.swingSpeedLimit);
            }
        }

        void StepMotion(float dt)
        {
            if (player.State == PlayerStateId.WallRest)
            {
                StepWallRest(dt);
                return;
            }
            if (!Moving) return;
            if (!Usable(socket))
            {
                Clear(Vector2.zero);
                return;
            }
            if (Phase == BluePlugPhase.Towing)
            {
                if (bladeFlight)
                {
                    Vector2 run = socket.TowPoint - body.position;
                    if (run.magnitude <= data.arrivalTolerance) { CompleteTow(dt); return; }
                    towDirection = run.normalized;
                    ApplySweptStep(towDirection * Mathf.Min(run.magnitude, towSpeed * dt), dt,
                        "블레이드까지 가는 길이 막혔습니다.");
                    return;
                }
                float remaining = Vector2.Dot(towGoal - body.position, towDirection);
                if (towFollowsPatrol && towHasStep && TouchedMovingSocket())
                {
                    CompleteTow(dt);
                    return;
                }
                if (remaining <= data.arrivalTolerance)
                {
                    if (towFollowsPatrol) Clear(Vector2.zero);
                    else CompleteTow(dt);
                    return;
                }
                previousBody = body.position;
                previousTowPoint = socket.TowPoint;
                towHasStep = true;
                Vector2 travel = towDirection * GrappleRules.Step(remaining, towSpeed, dt);
                ApplySweptStep(travel, dt, "견인 경로에 장애물이 생겼습니다.");
                return;
            }

            Vector2 pivot = socket.Point;
            Vector2 radial = body.position - previousPivot;
            float length = radial.magnitude;
            if (length < .01f || Mathf.Abs(length - swingRadius) > .20f ||
                (pivot - previousPivot).sqrMagnitude > .36f)
            {
                Clear(Vector2.zero);
                return;
            }
            radial /= length;
            Vector2 tangent = Perpendicular(radial);
            swingSpeed = BluePlugRules.SwingSpeed(swingSpeed, tangent.y, input.Move.x * tangent.x,
                data.swingGravity, data.swingPump, data.swingSpeedLimit, dt);
            float angle = BluePlugRules.SwingAngleStep(swingSpeed, swingRadius, dt);
            Vector2 next = pivot + (radial * Mathf.Cos(angle) + tangent * Mathf.Sin(angle)) * swingRadius;
            if (!CableClear(next))
            {
                Clear(Vector2.zero);
                notice?.Invoke("전선이 장애물을 만나 스윙을 멈췄습니다.");
                return;
            }
            ApplySweptStep(next - body.position, dt, "스윙 경로가 막혔습니다.");
            swingPivotVelocity = (pivot - previousPivot) / dt;
            previousPivot = pivot;
        }

        void CompleteTow(float dt)
        {
            bool equip = bladeFlight && IsHandle(socket);
            float carriedDistance = Vector2.Distance(bladeStart, body.position);
            bool perfect = caughtFalling;
            LastArrival = socket;
            ArrivalSequence++;
            visuals.Arrival(socket.TowPoint);
            Vector2 carry = towDirection * (towSpeed * towCarry);
            electricity?.Arrive(body.position);
            Clear(equip ? Vector2.zero : BodyClear(carry * dt) ? carry : Vector2.zero, true);
            if (equip && blade.TryEquip(carriedDistance, perfect)) mode.Set(PlayerMode.Attack);
        }

        bool TouchedMovingSocket()
        {
            Vector2 relative = previousBody - previousTowPoint;
            Vector2 socketMove = socket.TowPoint - previousTowPoint;
            if (socketMove.sqrMagnitude > 1f) return false;
            Vector2 movement = (body.position - previousBody) - socketMove;
            float time = movement.sqrMagnitude > .000001f
                ? Mathf.Clamp01(-Vector2.Dot(relative, movement) / movement.sqrMagnitude) : 1f;
            return (relative + movement * time).sqrMagnitude <= .20f * .20f &&
                BodyClear(socket.TowPoint - body.position);
        }

        void ApplySweptStep(Vector2 step, float dt, string blocked)
        {
            if (!BodyClear(step))
            {
                Clear(Vector2.zero);
                notice?.Invoke(blocked);
                return;
            }
            player.SetExternalVelocity(step / dt);
        }

        Vector2 TangentVelocity() => socket
            ? Perpendicular((body.position - previousPivot).normalized) * swingSpeed + swingPivotVelocity
            : Vector2.zero;
        static Vector2 Perpendicular(Vector2 vector) => new Vector2(-vector.y, vector.x);

        bool TipClear(Vector2 origin, Vector2 step)
        {
            float distance = step.magnitude;
            if (distance < .001f) return true;
            Vector2 direction = step / distance;
            int count = Physics2D.CircleCast(origin, data.tipRadius, direction, solids, hits, distance);
            return CastClear(count, direction, origin.y - data.tipRadius, false);
        }

        bool BodyClear(Vector2 step)
        {
            float distance = step.magnitude;
            if (distance < .001f) return true;
            Vector2 direction = step / distance;
            int count = body.Cast(direction, solids, hits, distance + data.skin);
            return CastClear(count, direction, shape.bounds.min.y, false);
        }

        bool CableClear(Vector2 origin)
        {
            Vector2 delta = socket.Point - origin;
            float distance = delta.magnitude;
            if (distance < .001f) return true;
            Vector2 direction = delta / distance;
            int count = Physics2D.Raycast(origin, direction, solids, hits, distance);
            return CastClear(count, direction, origin.y, true);
        }

        bool CastClear(int count, Vector2 direction, float bottom, bool cable)
        {
            // 결과가 가득 차면 누락된 장애물이 있을 수 있으므로 안전하게 멈춘다.
            if (count >= hits.Length) return false;
            for (int i = 0; i < count; i++)
                if (Blocks(hits[i], direction, bottom, cable)) return false;
            return true;
        }

        bool Blocks(RaycastHit2D hit, Vector2 direction, float bottom, bool cable)
        {
            if (!hit.collider) return false;
            bool oneWay = hit.collider.usedByEffector &&
                hit.collider.TryGetComponent<PlatformEffector2D>(out var effector) && effector.useOneWay;
            if (oneWay && (cable || direction.y >= -.001f || hit.normal.y < .5f ||
                bottom < hit.collider.bounds.max.y - data.skin)) return false;
            return Vector2.Dot(hit.normal, direction) < -.001f;
        }

        void RefreshIndicators(BluePlugSocket target)
        {
            BluePlugSkill learned = Learned;
            if (blade && blade.Catchable && blade.Handle)
            {
                var h = blade.Handle;
                var state = h == SeatedSocket ? BluePlugSocketState.Linked : h != target ||
                    (Phase != BluePlugPhase.Winding && Phase != BluePlugPhase.Ready) ? BluePlugSocketState.Idle :
                    Aimable(h) ? BluePlugSocketState.Ready : BluePlugSocketState.Blocked;
                h.SetIndicator(state, BluePlugSkill.Tow);
            }
            foreach (var item in sockets)
            {
                if (!item) continue;
                BluePlugSocketState indicator = BluePlugSocketState.Idle;
                if (item == SeatedSocket)
                    indicator = BluePlugSocketState.Linked;
                else if (target == item && (Phase == BluePlugPhase.Ready || Phase == BluePlugPhase.Winding))
                {
                    // 투척은 기본 기술이다. 미학습 견인·스윙은 각각의 이동 문양에 자물쇠로 표시한다.
                    indicator = Aimable(item) ? BluePlugSocketState.Ready : BluePlugSocketState.Blocked;
                }
                item.SetIndicator(indicator, learned);
            }
        }

        void LateUpdate()
        {
            if (Phase == BluePlugPhase.Ready) return;
            Vector2 origin = visualLeftGrip ?? Hand;
            if (Phase == BluePlugPhase.Winding)
            {
                float range = data.ThrowRange(Charge, data.range);
                Vector2 hand = Hand;
                Vector2 projected = visuals.ProjectOrbit(hand, plug);
                Vector2 tip = TipClear(plug, projected - plug) && TipClear(hand, projected - hand) ? projected : plug;
                visuals.Show(origin, tip, Perpendicular(WindRadial()) * windSign, Phase, hand, Charge, range, visualRightGrip);
                return;
            }
            Vector2 direction = Phase == BluePlugPhase.Retracting ? (Hand - plug).normalized :
                Phase == BluePlugPhase.Flying || !socket ? flightDirection : socket.EntryDirection;
            if (Phase == BluePlugPhase.Flying && socket)
            {
                float blend = 1 - Mathf.Clamp01(Vector2.Distance(plug, flightGoal) / .60f);
                Vector2 insertion = socket.EntryDirection;
                float from = Mathf.Atan2(flightDirection.y, flightDirection.x) * Mathf.Rad2Deg;
                float to = Mathf.Atan2(insertion.y, insertion.x) * Mathf.Rad2Deg;
                float angle = Mathf.LerpAngle(from, to, blend) * Mathf.Deg2Rad;
                direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            visuals.Show(origin, VisualEnd, direction, Phase);
        }

        void BeginRecall()
        {
            // 도킹된 경우 이동 콘센트의 현재 위치를 회수 시작점으로 사용한다.
            Vector2 from = VisualEnd;
            if (!session.BeginRetract()) return;
            Seat(null);
            plug = from;
            socket = null;
            gripTargeted = bladeFlight = caughtFalling = false;
            releaseSocket = null;
            releaseQueued = primaryWind = false;
        }

        void StepRecall(float dt)
        {
            Vector2 delta = Hand - plug;
            float distance = delta.magnitude;
            float travel = GrappleRules.Step(distance, Mathf.Max(.1f, data.retractSpeed), dt);
            if (distance - travel > .01f)
            {
                Vector2 step = delta * (travel / distance);
                if (TipClear(plug, step))
                {
                    plug += step;
                    return;
                }
            }
            // 새로 닫힌 셔터에서는 전선을 끊어 회수하고 팁이 벽을 통과하는 그림을 막는다.
            Clear(Vector2.zero);
            recastLeft = Mathf.Max(0, data.recastDelay);
        }

        void Seat(BluePlugSocket next)
        {
            if (ReferenceEquals(SeatedSocket, next)) return;
            if (SeatedSocket) SeatedSocket.SetIndicator(BluePlugSocketState.Idle, Learned);
            SeatedSocket = next;
            if (next)
            {
                next.SetIndicator(BluePlugSocketState.Linked, IsHandle(next) ? BluePlugSkill.Tow : Learned);
                electricity?.Dock(next);
            }
            SeatedSocketChanged?.Invoke(next);
        }

        void Clear(Vector2 launch, bool resetAirActions = false)
        {
            bool wasMoving = Moving;
            if (reservedSocket && socket) socket.Release(resetAirActions);
            reservedSocket = false;
            Seat(null);
            session.Clear();
            socket = null;
            gripTargeted = bladeFlight = caughtFalling = false;
            releaseSocket = null;
            releaseQueued = primaryWind = false;
            VisualAimDirection = null;
            SetVisualGrips(null);
            visuals.Show(Vector2.zero, Vector2.zero, Vector2.right, BluePlugPhase.Ready);
            if (wasMoving && player.State == PlayerStateId.GrappleDash)
                player.FinishExternal(launch, resetAirActions);
        }
    }
}
