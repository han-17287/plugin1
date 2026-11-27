using System;
using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.PlayerCore
{
    // 입력은 Update에서 한 번만 소비한다. 방의 센서와 물리 장치는 발사 번호를 보고 결과를 읽는다.
    [DefaultExecutionOrder(25), DisallowMultipleComponent]
    public sealed class PlayerSummonLoadout : MonoBehaviour
    {
        [SerializeField, Min(.1f)] float bladeReach = 1.1f, bladeRadius = 1.0f, laserReach = 26;
        [SerializeField, Range(0,.1f)] float contactHold = .045f;
        [SerializeField, Min(.05f)] float laserChargeTime = .78f;
        [SerializeField, Min(1)] int laserUnitCost = 5;
        [SerializeField, Min(0)] float laserDamage = 120;
        [SerializeField, Min(.05f)] float laserWidth = .28f;
        PlayerStateMachine player;
        PlayerModeController mode;
        PlayerInputReader input;
        PlayerBluePlug plug;
        SummonedBlade weapon;
        FilamentStrikeFX fx;
        Camera view;
        Func<int> balance;
        Func<int,bool> spend;
        Action<string> notify;
        bool charging, cutting, buffered, pendingImpact;
        SummonedBladeCarry? carryOnRespawn;
        float charged, elapsed, endedAt = -100, heldPose;
        int practiceCharge;
        Vector2 cutDirection;
        int step, detachFrame = -1;

        public bool Equipped => weapon && weapon.Equipped;
        public bool BlocksInteract => Equipped || detachFrame == Time.frameCount;
        public bool Cutting => cutting && AncientBladeRules.IsActive(step, elapsed);
        public bool AnimatingCut => cutting;
        public float CutElapsed => elapsed;
        public int CutStep => step;
        public int CutFrame => AncientBladeRules.AnimationFrame(step, elapsed);
        public Vector2 CutDirection => cutDirection;
        public float CutFacing { get; private set; }
        public int LaserSequence { get; private set; }
        public Vector2 LaserStart { get; private set; }
        public Vector2 LaserEnd { get; private set; }
        public int SupplyTarget => weapon.Data.summonCost + laserUnitCost * 3;
        public int Particles => balance?.Invoke() ?? 0;

        public static PlayerSummonLoadout Install(PlayerBluePlug plug, Camera view,
            Func<int> balance, Func<int,bool> spend, Action<int> grant, Action<string> notice, Action<int> refund = null)
        {
            if (!plug.TryGetComponent<PlayerSummonLoadout>(out var loadout))
                loadout = plug.gameObject.AddComponent<PlayerSummonLoadout>();
            loadout.plug = plug;
            loadout.view = view;
            loadout.balance = balance;
            loadout.spend = spend;
            loadout.notify = notice;
            if (!plug.TryGetComponent<SummonedBlade>(out loadout.weapon))
                loadout.weapon = plug.gameObject.AddComponent<SummonedBlade>();
            loadout.weapon.Configure(view, balance, spend, grant, plug.Electricity.Area, loadout.player.Protect, notice, refund:refund);
            plug.BindWeapon(loadout.weapon);
            return loadout;
        }

        public static PlayerSummonLoadout Practice(PlayerBluePlug plug, Camera view, Action<string> notice)
        {
            if (!plug.TryGetComponent<PlayerSummonLoadout>(out var runner))
                runner = plug.gameObject.AddComponent<PlayerSummonLoadout>();
            runner.practiceCharge = 60;
            return Install(plug, view, () => runner.practiceCharge, amount => {
                if (runner.practiceCharge < amount) return false;
                runner.practiceCharge -= amount;
                return true;
            }, amount => runner.practiceCharge = Mathf.Clamp(runner.practiceCharge + amount, 0, 100), notice);
        }

        void Awake()
        {
            player = GetComponent<PlayerStateMachine>();
            input = GetComponent<PlayerInputReader>();
            mode = GetComponent<PlayerModeController>();
            fx = gameObject.AddComponent<FilamentStrikeFX>();
        }
        void OnEnable() { player.Respawned += ResetOnSpawn; player.StateChanged += StateChanged; }
        void OnDisable()
        {
            carryOnRespawn = null;
            player.Respawned -= ResetOnSpawn;
            player.StateChanged -= StateChanged;
            if (weapon) weapon.ResetEncounter();
            CancelHands();
        }
        public void CarryWeaponOnNextSpawn()
        {
            if (weapon && !carryOnRespawn.HasValue) carryOnRespawn = weapon.LeaveRoom();
        }
        void ResetOnSpawn()
        {
            CancelHands();
            if (weapon)
            {
                if (carryOnRespawn.HasValue) weapon.EnterRoom(carryOnRespawn.Value);
                else weapon.ResetEncounter();
            }
            carryOnRespawn = null;
            mode.Set(Equipped ? PlayerMode.Attack : PlayerMode.Hook);
        }
        void StateChanged(PlayerStateId before, PlayerStateId after)
        {
            if (after != PlayerStateId.Hurt && after != PlayerStateId.Dead) return;
            CancelHands();
            if (after == PlayerStateId.Dead && weapon) { weapon.ResetEncounter(); mode.Set(PlayerMode.Hook); }
        }
        void CancelHands()
        {
            charging = cutting = buffered = pendingImpact = false;
            endedAt = -100;
            step = 0;
            heldPose = 0;
            if (fx) fx.Clear();
        }

        void Update()
        {
            if (!weapon || !input.Ready || input.Paused || !Application.isFocused)
            { if (cutting || charging) CancelHands(); return; }
            double now = Time.timeAsDouble;
            float dt = Time.deltaTime;
            weapon.Tick(dt);
            bool summon = input.Buffer.Take(PlayerCommand.Summon, now);
            bool detach = input.Buffer.Take(PlayerCommand.Detach, now);
            bool overload = input.Buffer.Take(PlayerCommand.Overload, now);
            bool press = input.Buffer.Take(PlayerCommand.LaserPress, now);
            bool release = input.Buffer.Take(PlayerCommand.LaserRelease, now);
            bool primary = Equipped && input.Buffer.Take(PlayerCommand.Primary, now);
            mode.Set(Equipped ? PlayerMode.Attack : PlayerMode.Hook);
            if (player.State == PlayerStateId.Dead || player.State == PlayerStateId.Hurt || plug.Moving)
                return;
            if (Equipped && detach)
            {
                detachFrame = Time.frameCount;
                if (weapon.TryDetach()) { CancelHands(); mode.Set(PlayerMode.Hook); }
                return;
            }
            if (summon && !Equipped) { weapon.TrySummon(); return; }
            if (!Equipped || player.State == PlayerStateId.Dash) { if (cutting || charging) CancelHands(); return; }
            if (overload && weapon.TryOverload()) { CancelHands(); mode.Set(PlayerMode.Hook); return; }
            if (press && !cutting) { charging = true; charged = 0; }
            if (charging)
            {
                charged += dt;
                if (release) { fx.StopAim(); FireLaser(); charging = false; }
                else
                {
                    Vector2 origin = transform.position;
                    Vector2 end = BeamEnd(origin, Aim(), out _);
                    fx.Aim(origin,end,Mathf.Clamp01(charged/laserChargeTime));
                }
                return;
            }
            if (primary)
            {
                if (!cutting) BeginCut();
                else if (AncientBladeRules.CanBuffer(AncientBladeRules.Duration(step) - elapsed)) buffered = true;
            }
            if (!cutting) return;
            if (heldPose > 0)
            {
                // 실제로 맞춘 칼 프레임만 잠깐 붙든다. 입력과 설비·음악의 시계는 계속 흐른다.
                float held = Mathf.Min(dt, heldPose);
                heldPose -= held;
                dt -= held;
            }
            elapsed += dt;
            if (AncientBladeRules.ConsumeScheduledImpact(step, elapsed, ref pendingImpact))
            {
                fx.SwingSound();
                int damage = step == 1 ? weapon.Data.comboFirst : step == 2 ? weapon.Data.comboSecond : weapon.Data.comboThird;
                Vector2 origin = transform.position;
                var wall = Physics2D.Raycast(origin, cutDirection, bladeReach, player.data.solidLayers);
                Vector2 contact = wall ? wall.point - cutDirection * .025f : origin + cutDirection * bladeReach;
                if (plug.Electricity.Melee(contact, bladeRadius, damage))
                {
                    heldPose = contactHold;
                    fx.BladeContact(plug.Electricity.LastBladeContact, -cutDirection,true);
                }

            }
            if (elapsed < AncientBladeRules.Duration(step)) return;
            cutting = false;
            endedAt = Time.time;
            if (buffered) BeginCut();
        }

        Vector2 Aim()
        {
            Vector2 direction = (Vector2)view.ScreenToWorldPoint(input.Pointer) - (Vector2)transform.position;
            return direction.sqrMagnitude < .001f ? Vector2.right * player.Facing : direction.normalized;
        }
        void BeginCut()
        {
            step = AncientBladeRules.NextStep(step, endedAt, Time.time);
            elapsed = 0;
            cutDirection = Aim();
            CutFacing = Mathf.Abs(cutDirection.x) > .05f ? Mathf.Sign(cutDirection.x) : player.Facing;
            fx.BeginBlade(step);
            cutting = pendingImpact = true;
            buffered = false;
        }
        Vector2 BeamEnd(Vector2 origin, Vector2 direction, out RaycastHit2D hit)
        {
            hit = Physics2D.Raycast(origin, direction, laserReach, player.data.solidLayers);
            return hit ? hit.point : origin + direction * laserReach;
        }
        void FireLaser()
        {
            int tier = Mathf.Min(Mathf.Min(3, 1 + Mathf.FloorToInt(charged / laserChargeTime * 2)),
                Mathf.Max(0, balance()) / laserUnitCost);
            if (tier == 0 || !spend(tier * laserUnitCost))
            { notify?.Invoke("레이저 입자 부족 · 보급 전구 옆에서 우클릭"); return; }
            LaserStart = transform.position;
            Vector2 direction = Aim();
            LaserEnd = BeamEnd(LaserStart, direction, out var hit);
            ++LaserSequence;
            float width = laserWidth * tier;
            plug.Electricity.DamageBeam(LaserStart, LaserEnd, width * .5f, laserDamage * (1 + .55f * (tier - 1)));
            fx.Laser(LaserStart, LaserEnd, width, tier / 3f, .12f + .06f * tier);
            if (hit) fx.Spark(LaserEnd, -direction);
        }
        void LateUpdate()
        {
            if (!Equipped || cutting) return;
            Vector2 tip = charging ? Aim() : new Vector2(player.Facing,.45f);
            float face = Mathf.Abs(tip.x) > .05f ? Mathf.Sign(tip.x) : player.Facing;
            Vector2 p = transform.position;
            weapon.SetEquippedPose(p + new Vector2(-.16f * face,.15f), p + new Vector2(.36f * face,.13f),
                tip, !cutting);
        }
        public void RestockPractice() { if (input.CollectHeld) practiceCharge = Mathf.Max(practiceCharge, SupplyTarget); }
    }
}
