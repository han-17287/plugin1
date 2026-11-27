using System;
using UnityEngine;
using Plugin.AncientCivilization;

namespace Plugin.PlayerCore
{
    [DisallowMultipleComponent]
    public sealed class SummonedBlade : MonoBehaviour
    {
        [SerializeField] SummonedBladeData settings;
        readonly SummonedBladeRules rules = new SummonedBladeRules();
        SummonedBladePicker picker;
        SummonedBladePresentation art;
        SummonedBladeLanding landing;
        Camera view;
        Func<int> balance;
        Func<int,bool> spend;
        Action<int> grant, refund;
        Action<Vector2,float,float,SummonedBladeHit> area;
        Action<float> invulnerable;
        Action<string> notice;
        Vector2? previous;
        float drop, recheck, warningTotal, fieldAge;
        int rerolls;
        bool ownsData;
        PlayerStateMachine patrolActor;
        AncientHero campaignActor;

        public SummonedBladeState State => rules.State;
        public bool Equipped => rules.Equipped;
        public bool Catchable => rules.Catchable;
        public float Remaining => rules.Remaining;
        public float FieldRemaining => rules.FieldRemaining;
        public uint HandleGeneration { get; private set; }
        public bool ConfinedDeckFallback { get; set; }
        public uint InstallationId { get; private set; }
        public BluePlugSocket Handle => Catchable && art != null ? art.handle : null;
        public Vector2 GroundPoint => landing.support ? landing.Point : (Vector2)transform.position;
        public SummonedBladeData Data => settings;
        public SummonedBladeCarry CarrySnapshot => rules.Snapshot(settings ? settings.cooldown : 0);
        public int ReservedParticles => rules.ReservedParticles;
        public bool IgnoreCooldownForDebug { get; set; }
        public Vector2? FixedLandingForDebug { get; set; }
        public event Action OnSummoned, OnLanded, OnEquipped, OnDetached, OnDestroyed;

        public string Status
        {
            get
            {
                switch (State)
                {
                    case SummonedBladeState.Summoning: return "낙하 예고";
                    case SummonedBladeState.Falling: return "낙하 중 · 손잡이 조준";
                    case SummonedBladeState.Stuck: return "설치 · 손잡이 연결";
                    case SummonedBladeState.Equipped: return "장착 · E 분리";
                    case SummonedBladeState.Cooldown: return "전구 재생 중";
                    default: return "Q 블레이드 소환";
                }
            }
        }

        public void Configure(Camera view, Func<int> charge, Func<int,bool> spend, Action<int> grant,
            Action<Vector2,float,float,SummonedBladeHit> area, Action<float> invulnerable, Action<string> notice,
            Func<Vector2,bool> groundUnsafe = null, Action<int> refund = null)
        {
            ResetEncounter();
            this.view = view;
            balance = charge; this.spend = spend; this.grant = grant; this.refund = refund ?? grant;
            this.area = area; this.invulnerable = invulnerable; this.notice = notice;
            if (!settings) settings = Resources.Load<SummonedBladeData>("Traversal/DefaultSummonedBlade");
            if (!settings) { settings = ScriptableObject.CreateInstance<SummonedBladeData>(); ownsData = true; }
            picker = new SummonedBladePicker(settings, view, groundUnsafe);
            patrolActor = GetComponent<PlayerStateMachine>();
            campaignActor = GetComponent<AncientHero>();
        }

        public bool TrySummon()
        {
            if (picker == null) return false;
            if (IgnoreCooldownForDebug && State == SummonedBladeState.Cooldown) rules.Reset();
            if (State != SummonedBladeState.None) return false;
            if (balance == null || spend == null || balance() < settings.summonCost)
            {
                notice?.Invoke("블레이드를 부를 입자가 부족합니다.");
                return false;
            }
            picker.ConfinedDeckFallback = ConfinedDeckFallback;
            picker.FixedForDebug = FixedLandingForDebug.HasValue;
            picker.DebugWorld = FixedLandingForDebug.GetValueOrDefault();
            if (!picker.TryPick(transform.position, previous, out var target))
            { notice?.Invoke("소환할 바닥이 보이는 곳으로 이동하세요."); return false; }
            if (!spend(settings.summonCost)) return false;
            HandleGeneration++;
            landing = target;
            warningTotal = WarningTime();
            rules.Begin(warningTotal, settings.summonCost);
            recheck = .12f; rerolls = 0;
            EnsureArt();
            Present();
            OnSummoned?.Invoke();
            return true;
        }

        float WarningTime() => UnityEngine.Random.Range(Mathf.Max(.05f, settings.telegraphMin),
            Mathf.Max(settings.telegraphMin, settings.telegraphMax));

        public bool Supplies(Vector2 point)
        {
            return State == SummonedBladeState.Stuck && landing.support &&
                (point - art.handle.Point).sqrMagnitude <= settings.fieldRadius * settings.fieldRadius;
        }

        public void Tick(float dt)
        {
            if (picker == null || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            switch (State)
            {
                case SummonedBladeState.Summoning:
                    recheck -= dt;
                    if (recheck <= 0 || dt >= Remaining)
                    {
                        recheck = .12f;
                        if (!picker.Valid(landing, transform.position, true, true))
                        { Replan(); break; }
                    }
                    if (rules.AdvanceWarning(dt))
                    {
                        drop = picker.DropHeight(landing);
                        if (drop < .25f) { Replan(); break; }
                    }
                    break;
                case SummonedBladeState.Falling:
                    if (!landing.support || !landing.support.isActiveAndEnabled || !picker.Valid(landing, transform.position, true, false))
                    { Replan(); break; }
                    drop = Mathf.Max(0, drop - settings.fallSpeed * dt);
                    if (drop <= 0)
                    {
                        previous = GroundPoint;
                        rules.Land(settings.installedLifetime);
                        InstallationId++;
                        art.Pulse(GroundPoint, settings.impactRadius);
                        area?.Invoke(GroundPoint, settings.impactRadius, settings.impactDamage, SummonedBladeHit.Impact);
                        OnLanded?.Invoke();
                    }
                    break;
                case SummonedBladeState.Stuck:
                    if (!landing.support || !landing.support.isActiveAndEnabled) { Break(false); break; }
                    fieldAge += dt;
                    float active = rules.AdvanceInstalled(dt);
                    if (active > 0) area?.Invoke(GroundPoint, settings.fieldRadius, active * settings.fieldPerSecond, SummonedBladeHit.Field);
                    if (Remaining <= 0) Break(false);
                    break;
                case SummonedBladeState.Cooldown:
                    rules.AdvanceCooldown(dt);
                    break;
            }
            if (art != null)
            {
                art.StepPulse(dt);
                Present();
            }
        }

        void Replan()
        {
            var old = landing.support ? (Vector2?)landing.Point : previous;
            if (rerolls++ < 2 && picker.TryPick(transform.position, old, out var alternate))
            {
                HandleGeneration++;
                landing = alternate; drop = 0;
                warningTotal = WarningTime();
                rules.Retarget(warningTotal);
                recheck = .12f;
                return;
            }
            Refund();
            rules.Reset();
            landing = default;
            notice?.Invoke("낙하 지점이 막혀 소환 입자가 돌아왔습니다.");
            OnDestroyed?.Invoke();
        }

        public bool TryEquip(float distance, bool caughtFalling = false)
        {
            if (!Catchable || !landing.support || float.IsNaN(distance) || float.IsInfinity(distance)) return false;
            bool perfect = caughtFalling || State == SummonedBladeState.Falling;
            if (!rules.Equip()) return false;
            previous = GroundPoint;
            landing = default;
            Vector2 point = transform.position;
            Present();
            art.Pulse(point, settings.impactRadius);
            float damage = SummonedBladeRules.Hit(settings.zipBase, settings.zipPerTile, distance, settings.zipCap);
            if (perfect)
            {
                grant?.Invoke(settings.perfectCharge);
                invulnerable?.Invoke(settings.perfectInvulnerability);
            }
            area?.Invoke(point, settings.impactRadius, damage, SummonedBladeHit.Zip);
            OnEquipped?.Invoke();
            return true;
        }

        public bool TryDetach()
        {
            if (!Equipped) return false;
            if (!picker.TryPlantAtFeet(transform.position, out var site))
            { notice?.Invoke("발밑의 안전한 바닥에서 블레이드를 분리하세요."); return false; }
            landing = site;
            drop = fieldAge = 0;
            rules.Detach(settings.installedLifetime, settings.fieldLifetime);
            HandleGeneration++;
            InstallationId++;
            previous = GroundPoint;
            art.Pulse(GroundPoint, .8f);
            Present();
            OnDetached?.Invoke();
            return true;
        }

        public bool TryOverload()
        {
            if (!Equipped || balance == null || spend == null) return false;
            int units = balance();
            if (units <= 0 || !spend(units)) return false;
            Vector2 point = transform.position;
            float damage = SummonedBladeRules.Hit(0, settings.overloadPerParticle, units, settings.overloadCap);
            Break(true);
            art.Pulse(point, settings.overloadRadius);
            area?.Invoke(point, settings.overloadRadius, damage, SummonedBladeHit.Overload);
            return true;
        }

        public bool TryBreakInstalled()
        {
            if (State != SummonedBladeState.Stuck) return false;
            art.Pulse(GroundPoint, 1.1f);
            Break(false);
            return true;
        }

        void Break(bool overload)
        {
            rules.Break(settings.cooldown + (overload ? settings.overloadExtraCooldown : 0));
            landing = default;
            Present();
            OnDestroyed?.Invoke();
        }

        void Refund()
        {
            int amount = rules.RefundReservation();
            if (amount > 0) refund?.Invoke(amount);
        }

        // 다음 방의 지갑을 복사하기 전에 호출한다. 낙하를 끝내지 못한 칼의 입자는 한 번만 돌려준다.
        public SummonedBladeCarry LeaveRoom()
        {
            var carry = CarrySnapshot;
            Refund();
            ClearWorld();
            return carry;
        }

        public void EnterRoom(SummonedBladeCarry carry)
        {
            if (picker == null || State != SummonedBladeState.None) return;
            rules.EnterRoom(carry);
            if (Equipped) EnsureArt();
            Present();
        }

        public void ResetEncounter()
        {
            bool existed = State != SummonedBladeState.None && State != SummonedBladeState.Cooldown;
            Refund();
            ClearWorld();
            if (existed) OnDestroyed?.Invoke();
        }

        void ClearWorld()
        {
            landing = default;
            previous = null;
            rules.Reset();
            art?.Hide(true);
        }

        public void SetEquippedPose(Vector2 leftHand,Vector2 rightHand,Vector2 tipDirection,bool drawWeapon=true)
        {
            if (Equipped && art != null)
                art.InHand(leftHand, rightHand, tipDirection, settings.handleHeight, drawWeapon);
        }

        void EnsureArt() { if (art == null) art = new SummonedBladePresentation(gameObject.scene); }

        void Present()
        {
            if (art == null) return;
            if (Equipped)
            {
                float sign = campaignActor ? campaignActor.Facing : patrolActor ? patrolActor.Facing : 1;
                Vector2 body = transform.position;
                art.InHand(body + new Vector2(-sign*.23f,.10f), body + new Vector2(sign*.49f,.14f),
                    new Vector2(sign*.78f,.62f), settings.handleHeight, true);
            }
            else if ((State == SummonedBladeState.Summoning || Catchable) && landing.support)
            {
                art.OnGround(State, GroundPoint, landing.Normal, drop, settings.handleHeight, settings.fieldRadius,
                    rules.FieldRemaining, fieldAge, warningTotal > 0 ? rules.Remaining / warningTotal : 0);
            }
            else art.Hide(false);
        }

        void OnGUI()
        {
            if (!view || State != SummonedBladeState.Summoning || !landing.support) return;
            Vector3 point = view.WorldToScreenPoint((Vector3)(GroundPoint + Vector2.up*.48f));
            if (point.z <= 0) return;
            var old = GUI.color;
            GUI.color = new Color(1, .86f, .54f);
            GUI.Label(new Rect(point.x + 7, Screen.height - point.y - 15, 68, 24), Remaining.ToString("0.0"));
            GUI.color = old;
        }

#if UNITY_EDITOR
        [Header("Play Mode 검사")]
        [SerializeField] SummonedBladeState debugState = SummonedBladeState.None;
        [SerializeField] Vector2 debugGround = Vector2.zero;
        [SerializeField] bool useDebugGround = false;

        [ContextMenu("검사/소환 즉시 요청")]
        void DebugSummon()
        {
            if (!Application.isPlaying || picker == null || balance == null) return;
            FixedLandingForDebug = useDebugGround ? debugGround : (Vector2?)null;
            IgnoreCooldownForDebug = true;
            int shortage = settings.summonCost - balance();
            if (shortage > 0) grant?.Invoke(shortage);
            TrySummon();
        }

        [ContextMenu("검사/지정 상태로 이동")]
        void DebugState()
        {
            if (!Application.isPlaying || picker == null) return;
            ResetEncounter();
            if (debugState == SummonedBladeState.None) return;
            if (debugState == SummonedBladeState.Cooldown)
            {
                rules.Break(settings.cooldown);
                return;
            }
            DebugSummon();
            if (State == SummonedBladeState.None || debugState == SummonedBladeState.Summoning) return;
            Tick(Remaining + .01f);
            if (debugState == SummonedBladeState.Falling) return;
            Tick(settings.fallHeight / Mathf.Max(.1f, settings.fallSpeed) + .1f);
            if (debugState == SummonedBladeState.Equipped) TryEquip(0);
        }
#endif

        void OnDisable() { if (picker != null) ResetEncounter(); }

        void OnDestroy() { art?.Dispose(); if (ownsData && settings) Destroy(settings); }
    }
}
