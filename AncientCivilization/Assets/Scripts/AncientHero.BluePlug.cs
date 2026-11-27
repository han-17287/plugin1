using Plugin.PlayerCore;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed partial class AncientHero
    {
        internal const string CampaignPlugReadyStatus = "좌클릭 회전·투척";
        readonly BluePlugSession plugSession = new BluePlugSession();
        AncientBluePlugPorts plugPorts;
        AncientBluePlugPorts.Port plugPort, plugHighlight;
        BluePlugData plugSettings;
        BluePlugVisuals plugVisuals;
        BluePlugCharacterMotion plugCharacterMotion;
        Vector2 plugHead, plugAim, plugFlightDirection;
        Vector2 plugTowDirection;
        float plugTurns, plugAngle, plugSign, plugFlightAge, plugFlightDistance, plugSwingSpeed;
        float finaleReleaseTurn;
        float plugWindStartedAt, plugCharge, plugLaunchRange, plugFlightBudget, plugRecastAt;
        bool plugWindWithMouse, plugReleased, plugRequireKeyUp, ownsPlugSettings;
        bool IsCampaignPlugTow => plugSession.Phase == BluePlugPhase.Towing;
        bool IsCampaignPlugSwing => plugSession.Phase == BluePlugPhase.Swinging;
        bool IsCampaignPlugThrowing => plugSession.Phase is BluePlugPhase.Winding or BluePlugPhase.Flying;

        BluePlugSkill workshopSkills;
        internal void SetWorkshopSkill(BluePlugSkill skill, bool learned)
        {
            workshopSkills = learned ? workshopSkills | skill : workshopSkills & ~skill;
            AncientGame.SetSaved("storySocketSkills", (int)workshopSkills);
        }
        internal void RestoreStreetEnergy(float amount) => CaptureEnergy = Mathf.Clamp(amount, 0, 100);

        public BluePlugSkill CampaignPlugSkills
        {
            get
            {
                var story = AncientGame.Instance ? AncientGame.Instance.Story : null;
                int stage = story ? story.Stage : 0;
                var granted = workshopSkills;
                if (stage >= 2) granted |= BluePlugSkill.Tow;
                if (stage >= 3) granted |= BluePlugSkill.Swing;
                return granted;
            }
        }

        public string CampaignPlugStatus
        {
            get
            {
                switch (plugSession.Phase)
                {
                    case BluePlugPhase.Winding: return plugReleased ? "투척 준비" : plugWindWithMouse ? "회전 중 · 좌클릭 놓아 투척" : "회전 중 · G 놓아 투척";
                    case BluePlugPhase.Flying: return "플러그 투척";
                    case BluePlugPhase.Connected:
                        if (plugPort.IsBlade) return "손잡이 체결 · 블레이드 장착";
                        var hint = plugPorts.DeviceHint(plugPort);
                        if (hint != null) return hint;
                        var moves = plugPort.Supported & CampaignPlugSkills;
                        if (moves == (BluePlugSkill.Tow | BluePlugSkill.Swing)) return "체결 · C 견인 · V 스윙 · G 회수";
                        if (moves == BluePlugSkill.Tow) return "체결 · C 견인 · G 회수";
                        if (moves == BluePlugSkill.Swing) return "체결 · V 스윙 · G 회수";
                        return "체결 · 이동 기술 미해금 · G 회수";
                    case BluePlugPhase.Towing: return "플러그 견인";
                    case BluePlugPhase.Swinging: return "플러그 스윙 · V 이탈";
                    case BluePlugPhase.Retracting: return "전선 감는 중";
                    default:
                        return !AttackMode && HasWireTarget && plugPorts ?
                            plugPorts.DeviceHint(plugHighlight) ?? CampaignPlugReadyStatus : CampaignPlugReadyStatus;
                }
            }
        }

        void InitializeCampaignPlug(AncientGame game)
        {
            workshopSkills = (BluePlugSkill)AncientGame.Saved("storySocketSkills");
            plugPorts = AncientBluePlugPorts.For(game);
            plugVisuals = gameObject.AddComponent<BluePlugVisuals>();
            plugVisuals.SetActor(art);
            plugCharacterMotion = new BluePlugCharacterMotion(art);
            plugSettings = Resources.Load<BluePlugData>("Traversal/DefaultBluePlug");
            if (!plugSettings)
            {
                plugSettings = ScriptableObject.CreateInstance<BluePlugData>();
                ownsPlugSettings = true;
            }
            InitializeSummonedEquipment(game);
        }

        void TickCampaignPlugInput(AncientGame game, bool boarding, bool equipmentChanged)
        {
            if (!plugPorts || !aimCamera) return;
            if (plugPorts.PropInUse)
            {
                plugRequireKeyUp = true;
                return;
            }
            bool mouseHeld = Input.GetMouseButton(0);
            bool gHeld = Input.GetKey(KeyCode.G), cHeld = Input.GetKey(KeyCode.C), vHeld = Input.GetKey(KeyCode.V);
            if (Time.time < hurtUntil || !Application.isFocused || !body.simulated)
            {
                CancelCampaignPlug();
                return;
            }
            if (plugRequireKeyUp)
            {
                if (mouseHeld || gHeld || cHeld || vHeld) return;
                plugRequireKeyUp = false;
            }

            if (AttackMode || equipmentChanged) return;

            bool recallPressed = Input.GetKeyDown(KeyCode.G);
            if (recallPressed && (wired || plugSession.Phase != BluePlugPhase.Ready))
            {
                if (wired) StopWire(IsCampaignPlugSwing);
                else RetractCampaignPlug();
                return;
            }

            bool mousePressed = Input.GetMouseButtonDown(0);
            if ((mousePressed || recallPressed) && !wired && (!boarding || bike) && Time.time >= plugRecastAt &&
                plugSession.BeginWindup())
            {
                plugWindWithMouse = mousePressed && !recallPressed;
                plugReleased = false;
                plugWindStartedAt = Time.time;
                plugCharge = plugTurns = 0;
                plugSign = direction >= 0 ? 1 : -1;
                plugAngle = plugSign > 0 ? Mathf.PI : 0;
                plugHead = WireOrigin;
            }

            // 시작한 버튼의 릴리즈만 투척한다. 연결 후의 좌클릭은 견인이나 스윙으로 이어지지 않는다.
            if (plugSession.Phase == BluePlugPhase.Winding && !(plugWindWithMouse ? mouseHeld : gHeld) && !plugReleased)
            {
                plugReleased = true;
                finaleReleaseTurn = plugTurns;
                plugCharge = plugSettings.Charge(Time.time - plugWindStartedAt);
                plugLaunchRange = plugSettings.ThrowRange(plugCharge, AncientZipRules.Range);
                plugAim = aimCamera.ScreenToWorldPoint(Input.mousePosition);
                if (!plugPorts.TryPick(plugAim, WireOrigin, boarding, out plugPort, plugLaunchRange) && !boarding &&
                    plugElectricity.TryAim(WireOrigin, plugAim, plugLaunchRange, plugSettings.aimRadius, out Vector2 enemy))
                    plugAim = enemy;
            }
            if (Input.GetKeyDown(KeyCode.V) && plugSession.Phase == BluePlugPhase.Swinging)
            {
                StopWire(true);
                return;
            }
            if (plugSession.Phase != BluePlugPhase.Connected) return;
            if (autoEquipPending && plugPort.IsBlade && Time.frameCount > handleDockFrame)
            {
                // 손잡이에 정확히 명중한 경우만 장착 이동을 예약한다. 일반 콘센트는 새 입력을 기다린다.
                autoEquipPending = false;
                TryMoveCampaignPlug(game, BluePlugSkill.Tow);
                return;
            }
            bool tow = Input.GetKeyDown(KeyCode.C);
            bool swing = Input.GetKeyDown(KeyCode.V);
            if (tow != swing) TryMoveCampaignPlug(game, tow ? BluePlugSkill.Tow : BluePlugSkill.Swing);
        }

        void StepCampaignPlug(float dt)
        {
            switch (plugSession.Phase)
            {
                case BluePlugPhase.Winding:
                    StepCampaignWindup(dt);
                    break;
                case BluePlugPhase.Flying:
                    StepCampaignPlugFlight(dt);
                    break;
                case BluePlugPhase.Connected:
                    if (plugPorts.TryRefresh(plugPort, out plugPort)) plugHead = plugPort.Point;
                    else RetractCampaignPlug();
                    break;
                case BluePlugPhase.Retracting:
                    StepCampaignPlugRetract(dt);
                    break;
            }
        }

        void StepCampaignPlugRetract(float dt)
        {
            Vector2 hand = WireOrigin;
            Vector2 step = Vector2.ClampMagnitude(hand - plugHead, Mathf.Max(.1f, plugSettings.retractSpeed) * dt);
            if (step.sqrMagnitude > .00001f) plugFlightDirection = step.normalized;
            bool clear = CampaignTipClear(plugHead, step);
            if (clear) plugHead += step;
            // 되감는 중 닫힌 셔터 뒤로 플러그를 그리지 않는다.
            if (clear && (plugHead - hand).sqrMagnitude > .001f) return;
            ClearCampaignPlug();
            plugRecastAt = Time.time + plugSettings.recastDelay;
        }

        void ValidateMovingCampaignPlug()
        {
            if (!wired || !plugPort.IsValid || plugPort.IsBoarding) return;
            if (!plugPorts.TryRefresh(plugPort, out var current))
            {
                StopWire(false);
                return;
            }
            if (!wireMovingAnchor)
            {
                Vector2 next = wireZip ? current.Anchor : current.Point;
                wireCarrierVelocity = Vector2.ClampMagnitude(
                    (next - anchor) / Time.fixedDeltaTime, AncientZipRules.Speed);
                anchor = next;
            }
            plugPort = current;
        }

        int handleDockFrame;

        Vector2 CampaignWindRadial() => new Vector2(Mathf.Cos(plugAngle), Mathf.Sin(plugAngle));

        void StepCampaignWindup(float dt)
        {
            Vector2 hand = WireOrigin;
            bool finale = AncientGame.Instance && AncientGame.Instance.InSniperFinalPhase;
            if (!plugReleased || finale)
            {
                if (!plugReleased) plugCharge = plugSettings.Charge(Time.time - plugWindStartedAt);
                float turn = Mathf.Max(.1f, plugSettings.WindRate(plugCharge)) * dt;
                plugTurns += turn;
                plugAngle = (plugAngle + turn * plugSign * 2 * Mathf.PI) % (2 * Mathf.PI);
                Vector2 radial = CampaignWindRadial();
                float radius = plugSettings.WindRadius(plugCharge, plugTurns);
                Vector2 next = hand + radial * CampaignTipTravel(hand, radial, radius);
                plugHead = CampaignTipClear(plugHead, next - plugHead) ? next : hand;
                if (!bike) plugElectricity.Spin(hand, plugHead);
                if (!plugReleased) return;
                // 마지막 포탑전은 기존 한 바퀴 준비와 회전 접선에서의 발사 시점을 유지한다.
                if (plugTurns < 1) return;
            }
            // 릴리즈와 실제 손에서 분리되는 틱 사이에 셔터가 닫히거나 소켓이 이동할 수 있다.
            bool hasPort = plugPorts.TryRefresh(plugPort, out plugPort) &&
                (plugPort.Point - hand).sqrMagnitude <= plugLaunchRange * plugLaunchRange &&
                CampaignTipClear(hand, plugPort.Point - hand) && CampaignTipClear(plugHead, plugPort.Point - plugHead);
            Vector2 aim = hasPort ? plugPort.Point : plugAim;
            Vector2 delta = aim - plugHead;
            if (finale && plugTurns < Mathf.Max(1, finaleReleaseTurn) + 1 &&
                Vector2.Dot(Vector2.Perpendicular(CampaignWindRadial()) * plugSign, delta.normalized) < .86f) return;
            if (delta.sqrMagnitude < .0001f) { RetractCampaignPlug(); return; }
            if (!plugSession.Launch()) return;
            if (!hasPort) plugPort = default;
            // 사거리 원은 손을 기준으로 표시한다. 회전 끝이 손보다 뒤에 있어도 경계의 콘센트까지 닿는다.
            plugFlightBudget = plugPort.IsValid ? Mathf.Max(plugLaunchRange, delta.magnitude) : plugLaunchRange;
            plugFlightDirection = delta.normalized;
            plugFlightAge = plugFlightDistance = 0;
        }

        void StepCampaignPlugFlight(float dt)
        {
            // 자동 주행 중에는 차량의 진행 속도도 실어 가까워진 트럭의 콘센트를 따라잡는다.
            float speed = plugSettings.ThrowSpeed(plugCharge, plugSettings.throwSpeed) +
                (plugPort.IsBoarding ? Mathf.Abs(body.linearVelocity.x) : 0);
            plugFlightAge += dt;
            bool hasPort = plugPorts.TryRefresh(plugPort, out plugPort);
            Vector2 delta = hasPort ? plugPort.Point - plugHead : plugFlightDirection * speed * dt;
            // 이동 발판의 콘센트는 최신 위치를 사용한다. 매 틱 플러그 끝을 검사한다.
            if (hasPort && delta.sqrMagnitude > .00001f) plugFlightDirection = delta.normalized;
            float remaining = Mathf.Max(0, plugFlightBudget - plugFlightDistance);
            Vector2 step = Vector2.ClampMagnitude(delta, Mathf.Min(remaining, speed * dt));
            float distance = step.magnitude;
            Vector2 travelDirection = step.normalized;
            float safeDistance = CampaignTipTravel(plugHead, travelDirection, distance);
            Vector2 previous = plugHead;
            plugHead += travelDirection * safeDistance;
            if (!hasPort && !bike && safeDistance > .001f)
            {
                // 결전의 포탑도 플러그 자체로 부순다. 앞쪽 적·파편과 벽이 우선한다.
                var sniper=AncientGame.Instance.Sniper;
                Vector2 beforeTurret=plugHead;
                int turret=sniper ? sniper.TurretAlongPlug(previous,plugHead,plugSettings.tipRadius,out beforeTurret) : -1;
                if (plugElectricity.ThrowHit(previous,beforeTurret,out Vector2 impact))
                {
                    plugHead=impact;
                    RetractCampaignPlug();
                    return;
                }
                if (turret>=0)
                {
                    sniper.PlugTurret(turret,plugSettings.throwDamage);
                    plugHead=beforeTurret;
                    RetractCampaignPlug();
                    return;
                }
            }
            plugFlightDistance += safeDistance;
            if (safeDistance < distance - .001f)
            {
                RetractCampaignPlug();
                AncientGame.Instance.Notice("플러그 경로가 막혔습니다.", 1.3f);
                return;
            }
            if (hasPort && (plugHead - plugPort.Point).sqrMagnitude <= .0001f)
            {
                if (plugSession.Dock(Time.frameCount))
                {
                    if (plugPort.IsBlade)
                    {
                        autoEquipPending = true;
                        handleDockFrame = Time.frameCount;
                        caughtFalling = Loadout.State == SummonedBladeState.Falling;
                    }
                    plugPorts.DockDevice(plugPort);
                    plugVisuals.Click();
                }
                return;
            }
            if (plugFlightDistance >= plugFlightBudget - .001f || plugFlightAge > 2)
                RetractCampaignPlug();
        }

        void TryMoveCampaignPlug(AncientGame game, BluePlugSkill requested)
        {
            if (!plugPorts.TryRefresh(plugPort, out plugPort))
            {
                RetractCampaignPlug();
                return;
            }
            var skills = plugPort.IsBlade ? BluePlugSkill.Tow : CampaignPlugSkills;
            if ((skills & plugPort.Supported & requested) == 0)
            {
                game.Notice("이 이동이 잠겨 있거나 콘센트에서 지원되지 않습니다.", 1.6f);
                return;
            }

            bool clear = CampaignCableClear(body.position, plugPort.Point);
            if (requested == BluePlugSkill.Tow && !plugPort.IsProp)
            {
                Vector2 normal = plugPort.Normal;
                if (normal.sqrMagnitude < .1f) normal = (body.position - plugPort.Anchor).normalized;
                Vector2 goal = plugPort.IsDevice || plugPort.IsBlade ? plugPort.Anchor :
                    plugPort.Anchor + normal * AncientZipRules.Support(normal.x, normal.y, bike && !plugPort.IsBoarding);
                Vector2 travel = goal - body.position;
                clear &= (plugPort.IsBlade || travel.magnitude > AncientZipRules.Arrival) && travel.magnitude <= plugLaunchRange &&
                    CampaignBodyTravel(travel.normalized, travel.magnitude, plugPort.IsBoarding) >= travel.magnitude - .001f;
            }
            if (requested == BluePlugSkill.Swing)
            {
                float radius = Vector2.Distance(body.position, plugPort.Point);
                clear &= !bike && radius >= plugSettings.minimumRope && radius <= AncientZipRules.Range;
            }
            if (!plugSession.Activate(requested, skills, plugPort.Supported, clear, Time.frameCount))
            {
                if (!clear) game.Notice("몸이 지나갈 경로를 확보하세요.", 1.6f);
                return;
            }
            if (plugPort.IsProp)
            {
                plugPorts.TryUse(plugPort, body.position);
                ClearCampaignPlug();
                return;
            }
            if (plugPort.IsBoarding)
            {
                if (!TryProductionBoard(plugPort.Anchor)) ClearCampaignPlug();
                return;
            }

            ZipMode = requested == BluePlugSkill.Tow;
            if (plugPort.IsBlade) equipTravelDistance = Vector2.Distance(body.position, plugPort.Anchor);
            AttachWire(plugPort.Anchor, null, plugPort.Normal);
            if (!ZipMode && wired && !wireBrace)
            {
                anchor = plugPort.Point;
                if (wireMovingAnchor) wireMovingLocal = wireMovingAnchor.InverseTransformPoint(anchor);
                ResetCampaignSwing();
            }
        }

        void ResetCampaignSwing()
        {
            Vector2 radial = body.position - anchor;
            wireLength = radial.magnitude;
            plugSwingSpeed = Mathf.Clamp(Vector2.Dot(body.linearVelocity, Vector2.Perpendicular(radial.normalized)),
                -plugSettings.swingSpeedLimit, plugSettings.swingSpeedLimit);
        }

        Vector2 CampaignSwingTangent() => Vector2.Perpendicular((body.position - anchor).normalized) * plugSwingSpeed + wireCarrierVelocity;

        Vector2 FinishCampaignTow(float carrier)
        {
            Vector2 carry = new Vector2(carrier, 0);
            if (!IsCampaignPlugTow) return carry;
            plugVisuals.Arrival(body.position);
            usedAirDashes = 0;
            if (plugPort.IsBlade)
            {
                if (plugPorts.TryRefresh(plugPort, out var handle) && Vector2.Distance(body.position,handle.Anchor) < .55f)
                    Loadout.TryEquip(equipTravelDistance,caughtFalling);
                return carry;
            }
            if (!plugPort.IsBoarding) plugElectricity.Arrive(body.position);
            if (plugPort.IsBoarding) return carry;
            Vector2 velocity = carry + plugTowDirection * AncientZipRules.Speed * plugSettings.towCarry;
            float distance = velocity.magnitude * Time.fixedDeltaTime;
            if (distance < .001f) return Vector2.zero;
            return velocity.normalized * CampaignBodyTravel(velocity.normalized, distance, false) / Time.fixedDeltaTime;
        }

        Vector2 StepCampaignSwing(float dt)
        {
            Vector2 radial = body.position - anchor;
            float length = radial.magnitude;
            if (length < .01f || Mathf.Abs(length - wireLength) > .40f)
            {
                StopWire(false);
                return Vector2.zero;
            }
            radial /= length;
            Vector2 tangent = Vector2.Perpendicular(radial);
            plugSwingSpeed = BluePlugRules.SwingSpeed(plugSwingSpeed, tangent.y, axis * tangent.x,
                plugSettings.swingGravity, plugSettings.swingPump, plugSettings.swingSpeedLimit, dt);
            float turn = BluePlugRules.SwingAngleStep(plugSwingSpeed, wireLength, dt);
            Vector2 next = anchor + wireCarrierVelocity * dt +
                (radial * Mathf.Cos(turn) + tangent * Mathf.Sin(turn)) * wireLength;
            Vector2 step = next - body.position;
            if (!CampaignCableClear(next, anchor) ||
                CampaignBodyTravel(step.normalized, step.magnitude, false) < step.magnitude - .001f)
            {
                StopWire(false);
                return Vector2.zero;
            }
            arcTravel += Mathf.Abs(turn) * Mathf.Rad2Deg;
            if (Mathf.Abs(plugSwingSpeed * tangent.x) > .1f) swingDirection = Mathf.Sign(plugSwingSpeed * tangent.x);
            return step / dt;
        }

        internal bool CampaignTipClear(Vector2 from, Vector2 step) =>
            body && plugSettings && CampaignTipTravel(from, step.normalized, step.magnitude) >= step.magnitude - .001f;

        float CampaignTipTravel(Vector2 from, Vector2 direction, float length)
        {
            if (length < .001f) return length;
            int count = Physics2D.CircleCast(from, plugSettings.tipRadius, direction, zipCastFilter, zipCastHits, length);
            return CampaignAllowedTravel(count, from, direction, from.y - plugSettings.tipRadius, length, false);
        }

        float CampaignBodyTravel(Vector2 direction, float length, bool dismountedShape)
        {
            if (length < .001f) return length;
            Vector2 centre = body.position;
            float bottom = dismountedShape ? centre.y - heroCollider.size.y * .5f :
                bike ? bikeCollider.bounds.min.y : heroCollider.bounds.min.y;
            int count = dismountedShape
                ? Physics2D.CapsuleCast(centre, heroCollider.size, CapsuleDirection2D.Vertical, 0, direction,
                    zipCastFilter, zipCastHits, length + AncientZipRules.Skin)
                : body.Cast(direction, zipCastFilter, zipCastHits, length + AncientZipRules.Skin);
            return CampaignAllowedTravel(count, centre, direction, bottom, length, true);
        }

        float CampaignAllowedTravel(int count, Vector2 from, Vector2 direction, float bottom, float length, bool bodySweep)
        {
            if (count >= zipCastHits.Length) return 0;
            float safe = length;
            for (int index = 0; index < count; index++)
            {
                var hit = zipCastHits[index];
                if (!hit.collider || hit.rigidbody == body) continue;
                bool oneWay = hit.collider.usedByEffector &&
                    hit.collider.TryGetComponent<PlatformEffector2D>(out var effector) && effector.useOneWay;
                if (oneWay && (direction.y >= -.001f || hit.normal.y < .5f ||
                    bottom < hit.collider.bounds.max.y - AncientZipRules.Skin)) continue;
                if (bodySweep && hit.distance < .005f)
                {
                    Vector2 closest = hit.collider.ClosestPoint(from) - from;
                    if (!AncientZipRules.BlocksInitialContact(closest.x, closest.y, direction.x, direction.y)) continue;
                }
                else if (Vector2.Dot(hit.normal, direction) >= -.001f) continue;
                safe = Mathf.Min(safe, Mathf.Max(0, hit.distance - (bodySweep ? AncientZipRules.Skin : .018f)));
            }
            return safe;
        }

        bool CampaignCableClear(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < .00001f) return true;
            int count = Physics2D.Raycast(from, delta.normalized, zipCastFilter, zipCastHits, delta.magnitude);
            if (count >= zipCastHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var hit = zipCastHits[i];
                if (!hit.collider || hit.rigidbody == body || hit.distance > delta.magnitude - .08f) continue;
                if (hit.collider.usedByEffector && hit.collider.TryGetComponent<PlatformEffector2D>(out var effector) && effector.useOneWay) continue;
                return false;
            }
            return true;
        }

        bool ApplyCampaignPlugPose(bool allowed, bool grounded)
        {
            plugCharacterMotion?.SetFinalePose(AncientGame.Instance.InSniperFinalPhase);
            if (!allowed)
            {
                plugCharacterMotion?.Clear();
                if (!bike && plugSession.Phase != BluePlugPhase.Ready) plugCharacterMotion?.PresentPassive();
                swingMotion?.Clear();
                return false;
            }

            var phase = plugSession.Phase;
            // 연출은 몸체나 소켓 위치를 바꾸지 않는다. 구형 검사에서 강제로 시작한 스윙만 별도 모션을 쓴다.
            bool scriptedWire = wired && phase == BluePlugPhase.Ready;
            if (scriptedWire) plugCharacterMotion?.Clear();
            else if (plugCharacterMotion != null)
            {
                if (phase != BluePlugPhase.Ready) swingMotion?.Clear();
                Vector2 target = wired ? anchor : phase == BluePlugPhase.Connected ? plugPort.Point : plugHead;
                Vector2 bearing = phase == BluePlugPhase.Winding ? CampaignWindRadial() :
                    phase == BluePlugPhase.Flying ? plugFlightDirection : target - WireOrigin;
                Vector2? aim = phase == BluePlugPhase.Winding && aimCamera ?
                    (plugReleased ? plugAim : (Vector2)aimCamera.ScreenToWorldPoint(Input.mousePosition)) - WireOrigin : (Vector2?)null;
                if (plugCharacterMotion.Apply(phase, bearing, body.linearVelocity,
                    direction, grounded, Time.deltaTime, locomotion, aimDirection: aim,
                    useFinalePose: AncientGame.Instance.InSniperFinalPhase)) return true;
            }
            return swingMotion != null && swingMotion.Apply(scriptedWire && !wireZip && !wireBrace,
                !wired && !grounded, anchor - (Vector2)transform.position, body.linearVelocity,
                Time.time - wireAttachedAt, arcTravel, swingDirection, Time.time, Time.deltaTime);
        }

        void DrawCampaignPlug(AncientGame game)
        {
            if (!plugVisuals) return;
            BluePlugPhase phase = plugSession.Phase;
            Vector2 origin = WireOrigin;
            Vector2 target = plugHead, facing = plugFlightDirection;
            if (phase == BluePlugPhase.Winding)
            {
                facing = Vector2.Perpendicular(CampaignWindRadial()) * plugSign;
                Vector2 projected = plugVisuals.ProjectOrbit(origin, plugHead);
                if (CampaignTipClear(plugHead, projected - plugHead) && CampaignTipClear(origin, projected - origin))
                    target = projected;
            }
            if (wired)
            {
                phase = wireZip ? BluePlugPhase.Towing : BluePlugPhase.Swinging;
                target = anchor + (wireZip && plugPort.IsValid ? plugPort.Point - plugPort.Anchor : Vector2.zero);
                facing = plugPort.IsValid ? plugPort.EntryDirection : (anchor - origin).normalized;
            }
            // 물리 앵커(견인 도착점)와 실제 단자 홈은 별개다. 이동 장치에서도 프레임마다
            // 패널과 동일한 표면 좌표/삽입 방향으로 그려 핸들이 흔들리거나 거꾸로 꽂히지 않게 한다.
            if ((phase == BluePlugPhase.Connected || wired) && plugPorts.TryRefresh(plugPort, out var docked))
            { target = docked.Point; facing = docked.EntryDirection; }
            if (game.won || !Application.isFocused) phase = BluePlugPhase.Ready;
            Vector2 paintedHand = origin;
            if (wired && plugSession.Phase == BluePlugPhase.Ready && !wireZip && !wireBrace &&
                swingMotion != null && swingMotion.Presenting)
                paintedHand = swingMotion.EmitterWorld;
            else if (phase != BluePlugPhase.Ready && !bike && plugCharacterMotion != null)
                paintedHand = plugCharacterMotion.LeftGripWorld;
            Vector2? windingGrip = !bike && phase == BluePlugPhase.Winding && plugCharacterMotion != null
                ? plugCharacterMotion.RightGripWorld : (Vector2?)null;
            float range = plugSettings.ThrowRange(plugCharge, AncientZipRules.Range);
            plugVisuals.Show(paintedHand, target, facing, phase, origin, plugCharge, range, windingGrip);

            Vector2 aim = aimCamera ? (Vector2)aimCamera.ScreenToWorldPoint(Input.mousePosition) : body.position;
            bool boarding = game.Production && game.Production.Active && !game.Production.DeckCombat;
            bool winding = phase == BluePlugPhase.Winding;
            HasWireTarget = aimCamera && !game.won && !game.Paused && (!AttackMode || winding) &&
                (phase == BluePlugPhase.Ready || winding) &&
                (winding && plugReleased
                    ? plugPorts.TryRefresh(plugPort, out plugHighlight) &&
                        (plugHighlight.Point - origin).sqrMagnitude <= range * range &&
                        CampaignTipClear(origin, plugHighlight.Point - origin)
                    : plugPorts.TryPick(aim, origin, boarding, out plugHighlight, winding ? range : AncientZipRules.Range));
            WireTarget = HasWireTarget ? plugHighlight.Point : Vector2.zero;
            var indicated = plugPort.IsValid && (wired || phase == BluePlugPhase.Connected) ? plugPort :
                HasWireTarget ? plugHighlight : default;
            var state = BluePlugSocketState.Idle;
            if (indicated.IsValid)
            {
                bool linked = plugPort.IsValid && (wired || phase == BluePlugPhase.Connected);
                // 플러그 투척은 기본 능력이다. 미해금 이동은 패널의 개별 잠금 문양이 표시한다.
                state = linked ? BluePlugSocketState.Linked : BluePlugSocketState.Ready;
            }
            plugPorts.Highlight(indicated, state, CampaignPlugSkills);
        }

        void RetractCampaignPlug()
        {
            if (!plugSession.BeginRetract()) return;
            plugPorts?.ReleaseDevice();
            plugPort = default;
            autoEquipPending = caughtFalling = plugReleased = plugWindWithMouse = false;
            plugFlightDirection = (WireOrigin - plugHead).normalized;
        }

        void ClearCampaignPlug()
        {
            plugPorts?.ReleaseDevice();
            plugSession.Clear();
            plugPort = default;
            autoEquipPending = caughtFalling = plugReleased = plugWindWithMouse = false;
            plugCharge = plugLaunchRange = plugRecastAt = 0;
            if (plugVisuals) plugVisuals.Show(WireOrigin, Vector2.zero, Vector2.right, BluePlugPhase.Ready);
        }

        void CancelCampaignPlug()
        {
            if (wired) StopWire(false);
            else ClearCampaignPlug();
            if (plugVisuals) plugVisuals.ResetFeedback();
            HasWireTarget = false;
            WireTarget = Vector2.zero;
            plugHighlight = default;
            if (plugPorts) plugPorts.Highlight(default, BluePlugSocketState.Idle, CampaignPlugSkills);
            plugRequireKeyUp = true;
        }

        void ResetCampaignPlugAndProp(bool resetDevices = true)
        {
            CancelCampaignPlug();
            plugCharacterMotion?.Clear();
            if (plugPorts)
            {
                if (resetDevices) plugPorts.ResetDevices();
                plugPorts.CancelPropLink();
            }
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && body) ResetCampaignPlugAndProp(false);
        }

        void OnDisable()
        {
            footDust?.Clear();
            if (!body || !plugVisuals) return;
            CancelCampaignPlug();
            plugCharacterMotion?.Clear();
        }
    }
}
