using UnityEngine;
using Cue = Plugin.AncientCivilization.AncientBossCue;
using Unity.Profiling;

namespace Plugin.AncientCivilization
{
    // 본편 행정구 아레나의 컷신, 페이즈 전환, 공격 순서 및 재도전 정리.
    public sealed partial class AncientEnforcerEncounter : MonoBehaviour
    {
        const float DrainPerSecond = 8f;
        const float StunSeconds = 3.2f;
        const float WaveCrossingSeconds = 6.5f;
        const float ExplosionDamage = 65f;
        static readonly Color Light = new Color(.45f, .83f, 1f);

        AncientGame game;
        AncientEnemy owner;
        AncientEnforcerVisuals visuals;
        AncientEnforcerHazards hazards;
        AncientEnforcerEchoes echoes;
        SpriteRenderer sculpture;
        readonly SpriteRenderer[] sideMissiles = new SpriteRenderer[4];
        LineRenderer counterMark, explosionCore, explosionGlow;
        Material explosionMaterial;
        AncientEnforcerMove move;
        Vector2 counterTarget;
        Vector2 retreatStart, explosionCenter;
        float left, right, middle, stageTime, nextEvent, rest, recoveryLength;
        float counterTime, counterCooldown, counterLight, absorbed, targetX, retreatTime = -1f, explosionTime = -1f;
        float forgeGestureAge;
        int phase = 1, moveIndex, emitted;
        bool intro, activeMove, barrier, stopped, inBack;

        public bool InputLocked => intro && !stopped;
        static readonly ProfilerMarker HazardTick = new ProfilerMarker("PLUGIN.Enforcer.Hazards");
        static readonly ProfilerMarker EchoTick = new ProfilerMarker("PLUGIN.Enforcer.Echoes");
        static readonly ProfilerMarker ForgeTick = new ProfilerMarker("PLUGIN.Enforcer.Forge");

        public int Phase => phase;
        internal int PatternId => activeMove ? (int)move : -1;
        internal int HazardPoolSize => hazards?.AllocatedSlots ?? 0;
        internal int DeniedHazardSpawns => hazards?.DeniedSpawns ?? 0;
        public int ChargeRemaining => barrier ? Mathf.CeilToInt(AncientEnforcerRules.ChargeSeconds - stageTime) : 0;
        public bool Recovering => !intro && rest > 0;
        public bool Telegraphing => counterTime > 0 || activeMove &&
            (barrier || (move == AncientEnforcerMove.Echoes ? echoes.Telegraphing : emitted == 0));
        public float RecoveryRemaining => recoveryLength > 0 ? Mathf.Clamp01(rest / recoveryLength) : 0;
        public float WindupProgress => barrier ? Mathf.Clamp01(stageTime / AncientEnforcerRules.ChargeSeconds) :
            nextEvent > 0 ? Mathf.Clamp01(stageTime / nextEvent) : 0;
        public string Hint => barrier ? "낙하물을 베거나 던지기" : counterTime > 0 ? "반격 / 조준선 회피" : "입자 조형 / 전조 회피";

        public void Initialize(AncientGame world, AncientEnemy enemy)
        {
            game = world;
            owner = enemy;
            left = AncientCityLayout.ArenaLeft(2);
            right = AncientCityLayout.ArenaRight(2);
            middle = (left + right) * .5f;
            // 컷신 중 처음 쓰는 자세를 만들면서 화면이 끊기지 않게 미리 준비한다.
            for (int i = 0; i < 6; i++) AncientEnforcerVisuals.PoseSprite(i);
            AncientEnforcerVisuals.PrewarmTransitions();
            for (int i = 0; i < 5; i++) AncientEnforcerVisuals.ObjectSprite(i);
            visuals = new AncientEnforcerVisuals(game, transform);
            hazards = new AncientEnforcerHazards(game, transform, left, right);
            echoes = new AncientEnforcerEchoes(game, transform, left, right, 0);
            hazards.Interrupt = InterruptCharge;
            hazards.ThrowTarget = () => owner.AimPoint;
            hazards.ThrowHit = damage => { if (!stopped && owner.IsAlive) owner.Hit(damage, true); };
            hazards.AwardEnergy = amount => { game.hero.AddCapturedEnergy(amount); game.PickupSound(); };
            counterMark = game.Line("집행관 반격 조준선", new Color(1, .62f, .28f, .86f), .04f, 21);
            counterMark.transform.SetParent(transform, true);
            counterMark.enabled = false;
            explosionGlow = BuildExplosionRing(.85f, 20);
            explosionCore = BuildExplosionRing(.16f, 21);
            Shader glowShader = Resources.Load<Shader>("Shaders/ParticleGlow");
            if (glowShader && glowShader.isSupported)
            {
                explosionMaterial = new Material(glowShader);
                explosionGlow.sharedMaterial = explosionMaterial;
                explosionCore.sharedMaterial = explosionMaterial;
            }
            sculpture = AncientArt.Display("입자 조형 실루엣", AncientEnforcerVisuals.ObjectSprite(0), Vector2.zero, 16, transform);
            sculpture.enabled = false;
            for (int i = 0; i < sideMissiles.Length; i++)
            {
                sideMissiles[i] = AncientArt.Display("폭격 미사일 조형", AncientEnforcerVisuals.ObjectSprite(4), Vector2.zero, 16, transform);
                sideMissiles[i].enabled = false;
            }
            game.Enforcer = this;
            if (game.EnforcerIntroSeen) BeginRecovery(.85f);
            else BeginIntro();
        }

        public void Tick()
        {
            if (stopped || intro) return;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            MoveBoss(dt);
            counterCooldown = Mathf.Max(0, counterCooldown - dt);
            TickExplosion(dt);
            TickCounter(dt);
            using (HazardTick.Auto()) hazards.Tick(dt);
            if (stopped) return;
            using (EchoTick.Auto()) echoes.Tick(dt);
            if (stopped) return;
            if (rest > 0)
            {
                rest = Mathf.Max(0, rest - dt);
                return;
            }
            if (!activeMove) BeginMove();
            stageTime += dt;
            if (barrier) { TickCharge(dt); return; }
            if (move == AncientEnforcerMove.Echoes)
            {
                if (!echoes.Active) EndMove(1.65f);
                return;
            }
            using (ForgeTick.Auto()) UpdateSculpture();
            if (stageTime >= nextEvent)
            {
                int previous = emitted;
                AdvanceMove();
                // 두 번째 파도 경고만 나간 순간에는 발사 반동을 주지 않는다.
                if (emitted > previous && (move != AncientEnforcerMove.Waves || previous != 1))
                {
                    bool gun = move == AncientEnforcerMove.Gun;
                    bool directing = ForgeGestures() > 0;
                    if (directing) forgeGestureAge = 0;
                    owner.ReleaseEnforcerGesture(gun && previous < AncientEnforcerRules.GunBursts ? .56f : .88f, gun || directing);
                }
            }
        }

        void MoveBoss(float dt)
        {
            Vector2 hero = game.hero.transform.position;
            Vector2 position = transform.position;
            float facing = hero.x < position.x ? -1 : 1;
            float x = Mathf.Clamp(hero.x - facing * 5.6f, left + 2.25f, right - 2.25f);
            float y = 1.85f + Mathf.Sin(Time.time * 2.5f) * .21f;
            if (rest > 0) { x = position.x; y = .18f; }
            else if (barrier) { x = middle; y = .45f; }
            else if (inBack) { x = middle; y = 3.3f; }
            else if (activeMove && move == AncientEnforcerMove.Gun) { x = targetX; y = .78f; }
            transform.position = Vector2.MoveTowards(position, new Vector2(x, y), (rest > 0 ? 5.8f : 6.7f) * dt);
            int pose = rest > 0 ? 3 : Telegraphing ? 1 : activeMove && move == AncientEnforcerMove.Gun ? 2 : 0;
            // 방아쇠가 떨어지기 전에 기록 장갑을 펼쳐 실제 발사 프레임에는 지시 자세에 도착한다.
            bool preparingGun = activeMove && move == AncientEnforcerMove.Gun && emitted == 0 && rest <= 0;
            if (preparingGun && nextEvent - stageTime <= AncientEnforcerBodyMotion.GestureSeconds + dt) pose = 2;
            int forgeCount = ForgeGestures();
            if (rest <= 0 && activeMove && forgeCount > 0)
            {
                forgeGestureAge += dt;
                pose = AncientEnforcerBodyMotion.ForgePose(emitted, forgeCount, forgeGestureAge,
                    nextEvent - stageTime, dt, move == AncientEnforcerMove.Missiles);
            }
            bool planted = (rest > 0 && transform.position.y <= .2f) || barrier;
            owner.SetEnforcerPose(pose, facing, inBack ? .7f : 1, 1, planted, Telegraphing ? WindupProgress : 0, dt);
        }

        int ForgeGestures()
        {
            switch (move)
            {
                case AncientEnforcerMove.Drop: return AncientEnforcerRules.Drops;
                case AncientEnforcerMove.Bombard: return AncientEnforcerRules.BombardVolleys;
                case AncientEnforcerMove.Blocks: return phase == 3 ? 4 : 3;
                case AncientEnforcerMove.Missiles: return 7;
                case AncientEnforcerMove.Planes: return AncientEnforcerRules.PlaneCount;
                default: return 0;
            }
        }

        void BeginMove()
        {
            activeMove = true;
            move = AncientEnforcerRules.Move(phase, moveIndex);
            emitted = 0;
            stageTime = 0;
            nextEvent = .82f;
            targetX = ClampX(game.hero.transform.position.x, 1.1f);
            switch (move)
            {
                case AncientEnforcerMove.Drop: nextEvent = .98f; game.Sound(AncientEquipmentCue.Forge); break;
                case AncientEnforcerMove.Gun:
                    targetX = game.hero.transform.position.x < middle ? right - 4f : left + 4f;
                    nextEvent = 1.55f;
                    break;
                case AncientEnforcerMove.Bombard:
                case AncientEnforcerMove.Waves:
                case AncientEnforcerMove.Blocks:
                    inBack = true;
                    nextEvent = 1.15f;
                    if (move == AncientEnforcerMove.Waves)
                    {
                        game.Sound(AncientEquipmentCue.Sweep, -.68f);
                        game.Notice("왼쪽 파도 / 오른쪽 끝으로", 2.6f);
                        Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Tell, new Vector2(left + .45f, 2.3f), Vector2.right, .85f);
                    }
                    break;
                case AncientEnforcerMove.Echoes:
                    inBack = true;
                    echoes.Start(phase == 3);
                    break;
                case AncientEnforcerMove.Charge:
                    hazards.Clear(preserveEnergy: true);
                    barrier = true;
                    absorbed = 0;
                    nextEvent = .25f;
                    counterTime = 0;
                    counterLight = 0;
                    counterMark.enabled = false;
                    visuals.ClearBeam();
                    transform.position = new Vector2(middle, .45f);
                    game.Notice("입자 흡수 / 15초 안에 낙하물을 베거나 집행관에게 던지세요.", 4.3f);
                    game.Sound(AncientEquipmentCue.Overload);
                    Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Tell, owner.AimPoint, Vector2.up, 1.1f);
                    break;
            }
        }

        void UpdateSculpture()
        {
            bool drop = move == AncientEnforcerMove.Drop && emitted < AncientEnforcerRules.Drops;
            bool gun = move == AncientEnforcerMove.Gun && emitted <= AncientEnforcerRules.GunBursts;
            bool plane = move == AncientEnforcerMove.Planes && emitted < AncientEnforcerRules.PlaneCount;
            bool missile = (move == AncientEnforcerMove.Bombard && emitted < AncientEnforcerRules.BombardVolleys) ||
                (move == AncientEnforcerMove.Missiles && emitted < 7);
            foreach (var preview in sideMissiles) preview.enabled = false;
            if (!drop && !gun && !plane && !missile) { sculpture.enabled = false; visuals.ClearVortex(); return; }
            Vector2 center = owner.AimPoint + new Vector2(owner.AimPoint.x > game.hero.transform.position.x ? -.9f : .9f, .22f);
            int shape = gun ? 2 : plane ? 3 : missile ? 4 : emitted == 4 ? 1 : 0;
            if (drop) center = new Vector2(targetX, 7.8f);
            if (missile && move == AncientEnforcerMove.Bombard) center = new Vector2(middle, 7.6f);
            float interval = drop ? (emitted == 4 ? 1.35f : .88f) : .75f;
            float fill = gun && emitted > 0 ? 1 : Mathf.Clamp01(1 - (nextEvent - stageTime) / interval);
            if (fill < .96f) visuals.Vortex(center, fill, Time.time, drop ? 1.55f : 1.24f);
            else visuals.ClearVortex();
            float opacity = Mathf.Clamp01((fill - .42f) / .58f);
            ShowSculpture(shape, center, drop ? new Vector2(shape == 1 ? 2.5f : 1.25f, shape == 1 ? 1.75f : .94f) : new Vector2(1.45f, .65f),
                opacity, gun ? 0 : drop ? 9 : -12);
            if (missile && move == AncientEnforcerMove.Bombard)
            {
                for (int i = 0; i < sideMissiles.Length; i++)
                {
                    Vector2 point = center + Vector2.right * (i < 2 ? i - 2 : i - 1) * 2.65f;
                    Vector2 direction = AimFrom(point);
                    PlaceSculpture(sideMissiles[i], AncientEnforcerVisuals.ObjectSprite(4), point, new Vector2(1.45f, .65f), opacity,
                        Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                }
            }
        }

        void AdvanceMove()
        {
            switch (move)
            {
                case AncientEnforcerMove.Drop: AdvanceDrop(); break;
                case AncientEnforcerMove.Bombard: AdvanceMissiles(true); break;
                case AncientEnforcerMove.Missiles: AdvanceMissiles(false); break;
                case AncientEnforcerMove.Gun: AdvanceGun(); break;
                case AncientEnforcerMove.Waves: AdvanceWave(); break;
                case AncientEnforcerMove.Blocks:
                    if (emitted < (phase == 3 ? 4 : 3))
                    {
                        float duration = phase == 3 ? 1.75f : 2.6f;
                        hazards.Blocks(game.hero.transform.position.x, duration, phase == 3);
                        emitted++;
                        nextEvent += duration + .34f;
                    }
                    else EndMove(1.4f);
                    break;
                case AncientEnforcerMove.Planes:
                    if (emitted >= AncientEnforcerRules.PlaneCount) { EndMove(1.55f); break; }
                    Vector2 launch = owner.AimPoint + Vector2.up * .25f;
                    hazards.Shot(launch, AimFrom(launch) * 7.5f, 13, AncientEnforcerShot.Plane, 3.9f);
                    game.Sound(4);
                    emitted++;
                    nextEvent += emitted == AncientEnforcerRules.PlaneCount ? 1.15f : .68f;
                    break;
            }
        }

        void AdvanceDrop()
        {
            if (emitted >= AncientEnforcerRules.Drops) { EndMove(1.85f); return; }
            bool container = emitted == AncientEnforcerRules.Drops - 1;
            hazards.Drop(targetX, container ? .4f : .25f, container ? 26 : 16, container, false, 7.8f, sculpture);
            emitted++;
            targetX = ClampX(game.hero.transform.position.x, 1.6f);
            nextEvent += emitted >= AncientEnforcerRules.Drops ? .8f : emitted == 4 ? 1.55f : .99f;
        }

        void AdvanceMissiles(bool bombard)
        {
            int volleys = bombard ? AncientEnforcerRules.BombardVolleys : 7;
            if (emitted >= volleys) { EndMove(1.68f); return; }
            int count = bombard ? AncientEnforcerRules.MissilesPerVolley : 4;
            for (int i = 0; i < count; i++)
            {
                Vector2 launch = bombard ? new Vector2(middle + (i - 2) * 2.65f, 7.7f) : owner.AimPoint + Vector2.up * .3f;
                Vector2 velocity = AimFrom(launch) * (bombard ? 11 : 13);
                if (!bombard) velocity = Rotate(velocity, (i - 1.5f) * 15);
                // 다섯 발 폭격은 각 발사대에, 한 총구의 부채꼴은 가운데 한 발에만 섬광을 붙인다.
                hazards.Shot(launch, velocity, 13, AncientEnforcerShot.Missile, 3.4f, bombard || i == count / 2);
            }
            emitted++;
            game.Sound(4);
            nextEvent += emitted == volleys ? 1.15f : bombard ? .93f : .37f;
        }

        void AdvanceGun()
        {
            Vector2 muzzle = owner.AimPoint + new Vector2(game.hero.transform.position.x < transform.position.x ? -1 : 1, .12f);
            if (emitted < AncientEnforcerRules.GunBursts)
            {
                int count = phase == 2 ? AncientEnforcerRules.EnhancedPellets : 1;
                Vector2 aim = AimFrom(muzzle) * 15.5f;
                for (int i = 0; i < count; i++)
                    hazards.Shot(muzzle, Rotate(aim, (i - (count - 1) * .5f) * 13f), 9, AncientEnforcerShot.Bullet, 2.6f, i == count / 2);
                emitted++;
                game.Sound(4);
                nextEvent += emitted == AncientEnforcerRules.GunBursts ? .42f : .24f;
                return;
            }
            if (emitted == AncientEnforcerRules.GunBursts && phase == 1)
            {
                hazards.Shot(muzzle, AimFrom(muzzle) * 13.5f, 21, AncientEnforcerShot.Gun, 1.75f);
                emitted++;
                nextEvent += 1.15f;
                return;
            }
            EndMove(1.7f);
        }

        void AdvanceWave()
        {
            switch (emitted)
            {
                case 0:
                    game.Notice("왼쪽 파도 / 오른쪽 끝이 안전", 3f);
                    hazards.Wave(true, WaveCrossingSeconds, 19);
                    nextEvent += WaveCrossingSeconds + .1f;
                    break;
                case 1:
                    game.Sound(AncientEquipmentCue.Sweep, .68f);
                    game.Notice("곧 오른쪽 파도 / 지금 왼쪽 끝으로", 4.7f);
                    Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Tell, new Vector2(right - .45f, 2.3f), Vector2.left, .85f);
                    nextEvent += 4.7f;
                    break;
                case 2:
                    hazards.Wave(false, WaveCrossingSeconds, 19);
                    nextEvent += WaveCrossingSeconds + .15f;
                    break;
                default:
                    EndMove(1.5f);
                    return;
            }
            emitted++;
        }

        void TickCharge(float dt)
        {
            absorbed += game.hero.DrainEnforcerParticles(DrainPerSecond * dt);
            absorbed += hazards.AbsorbEnergy(owner.AimPoint, dt);
            visuals.Barrier(owner.AimPoint, stageTime / AncientEnforcerRules.ChargeSeconds, Time.time);
            if (stageTime >= AncientEnforcerRules.ChargeSeconds)
            {
                barrier = false;
                hazards.Clear();
                visuals.ClearBarrier();
                Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Strike, owner.AimPoint, Vector2.up, 1.48f);
                explosionCenter = owner.AimPoint;
                explosionTime = 0;
                TickExplosion(0);
                game.Shake(.42f);
                game.Sound(8);
                game.Notice("입자 대폭발", 1.1f);
                EndMove(1.45f);
                game.hero.HurtEnforcerExplosion(ExplosionDamage + Mathf.Min(15, absorbed * .08f));
                return;
            }
            if (stageTime < nextEvent) return;
            if (!hazards.AvailableInterrupt)
            {
                float heroX = game.hero.transform.position.x;
                float offset = heroX < middle ? 1.45f : -1.45f;
                hazards.Drop(ClampX(heroX + offset, 1.4f), .32f, 0, false, true, 6.2f);
            }
            nextEvent = stageTime + .28f;
        }

        void InterruptCharge()
        {
            if (!barrier || stopped) return;
            barrier = false;
            hazards.Clear();
            visuals.ClearBarrier();
            Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Contact, owner.AimPoint, Vector2.up, 1.18f);
            game.Shake(.18f);
            game.Sound(AncientEquipmentCue.ShortCircuit);
            game.Notice("흡수 중단 / 집행관 스턴", StunSeconds);
            EndMove(StunSeconds);
        }

        public float FilterDamage(float raw, float health, float maxHealth)
        {
            if (stopped || intro || barrier) return 0;
            return AncientEnforcerRules.LimitDamage(raw, health, maxHealth, phase);
        }

        public void OnHit(float healthRatio)
        {
            int newPhase = AncientEnforcerRules.Phase(healthRatio);
            if (newPhase != phase)
            {
                ResetPattern(newPhase, 0);
                BeginRecovery(1.05f);
                game.Sound(8);
                game.Impact(owner.AimPoint, Light, true);
                game.Notice("기록 집행관 / " + phase + "페이즈", 1.4f);
                return;
            }
            if (phase != 3 || rest > 0 || barrier || counterCooldown > 0 || counterTime > 0 ||
                !AncientEnforcerRules.Counter(Random.value)) return;
            game.Sound(AncientEquipmentCue.Counter);
            counterTime = .53f;
            counterCooldown = 2.15f;
            counterTarget = game.hero.transform.position;
            Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Tell, owner.AimPoint, counterTarget - owner.AimPoint, .68f);
        }

        // 페이즈 경계와 에디터 재현 모두 남아 있는 공격을 같은 순서로 회수한다.
        void ResetPattern(int nextPhase, int firstMove)
        {
            phase = nextPhase;
            moveIndex = firstMove;
            activeMove = barrier = inBack = false;
            counterTime = counterLight = 0;
            counterCooldown = .45f;
            hazards.Clear();
            echoes.Clear();
            ClearPresentation();
        }

#if UNITY_EDITOR
        public bool PreviewReady => game && game.Enforcer == this && AncientGame.IsQAProfile &&
            !game.MainMenu && !stopped && !intro && owner && owner.IsAlive;

        public void PreviewPattern(int nextPhase, int index)
        {
            if (!PreviewReady || nextPhase < 1 || nextPhase > 3 ||
                index < 0 || index >= AncientEnforcerRules.MoveCount(nextPhase)) return;
            if (AncientEnforcerRules.Phase(owner.HealthRatio) != nextPhase)
                owner.SetEnforcerPreviewHealth(nextPhase == 1 ? 1f : nextPhase == 2 ?
                    AncientEnforcerRules.SecondPhase : AncientEnforcerRules.ThirdPhase);
            ResetPattern(nextPhase, index);
            BeginRecovery(0);
            BeginMove();
        }

        public void PreviewHealth(float ratio)
        {
            if (!PreviewReady) return;
            owner.SetEnforcerPreviewHealth(ratio);
            int nextPhase = AncientEnforcerRules.Phase(owner.HealthRatio);
            if (nextPhase == phase) return;
            ResetPattern(nextPhase, 0);
            BeginRecovery(1.05f);
        }
#endif

        void TickCounter(float dt)
        {
            if (counterLight > 0)
            {
                counterLight -= dt;
                if (counterLight <= 0) visuals.ClearBeam();
            }
            if (counterTime <= 0) return;
            Vector2 begin = owner.AimPoint;
            Vector2 delta = (counterTarget - begin).normalized;
            if (delta.sqrMagnitude < .001f) delta = Vector2.down;
            RaycastHit2D wall = Physics2D.Raycast(begin, delta, 19f, 1 << 8);
            Vector2 end = wall ? wall.point : begin + delta * 19f;
            counterMark.enabled = true;
            counterMark.SetPosition(0, begin);
            counterMark.SetPosition(1, end);
            counterTime -= dt;
            if (counterTime > 0) return;
            counterMark.enabled = false;
            hazards.Beam(begin, end, .56f, .16f, 17);
            visuals.Beam(begin, end, .56f);
            counterLight = .18f;
            game.Sound(4);
        }

        LineRenderer BuildExplosionRing(float width, int order)
        {
            var ring = game.Line("기록실 폭발 파동", Light, width, order);
            ring.transform.SetParent(transform, true);
            ring.positionCount = 48;
            ring.loop = true;
            ring.enabled = false;
            return ring;
        }

        void TickExplosion(float dt)
        {
            if (explosionTime < 0) return;
            explosionTime += dt;
            float progress = Mathf.Clamp01(explosionTime / .56f);
            if (progress >= 1) { HideExplosion(); return; }
            float radius = Mathf.Lerp(.6f, (right - left) * .63f, 1 - Mathf.Pow(1 - progress, 2));
            for (int i = 0; i < explosionCore.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / explosionCore.positionCount;
                Vector2 point = explosionCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .57f) * radius;
                explosionCore.SetPosition(i, point);
                explosionGlow.SetPosition(i, point);
            }
            explosionGlow.startColor = explosionGlow.endColor = new Color(.27f, .76f, 1, .46f * (1 - progress));
            explosionCore.startColor = explosionCore.endColor = new Color(.82f, .98f, 1, 1 - progress);
            explosionCore.enabled = explosionGlow.enabled = true;
        }

        void HideExplosion()
        {
            explosionTime = -1;
            if (explosionCore) explosionCore.enabled = false;
            if (explosionGlow) explosionGlow.enabled = false;
        }

        void EndMove(float recovery)
        {
            moveIndex++;
            activeMove = inBack = false;
            sculpture.enabled = false;
            visuals.ClearVortex();
            BeginRecovery(recovery);
        }

        void BeginRecovery(float seconds) { rest = recoveryLength = seconds; }
        float ClampX(float x, float inset) => Mathf.Clamp(x, left + inset, right - inset);
        Vector2 AimFrom(Vector2 start)
        {
            Vector2 direction = (Vector2)game.hero.transform.position - start;
            return direction.sqrMagnitude > .001f ? direction.normalized : Vector2.down;
        }
        static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sine = Mathf.Sin(radians), cosine = Mathf.Cos(radians);
            return new Vector2(v.x * cosine - v.y * sine, v.x * sine + v.y * cosine);
        }

        void ShowSculpture(int shape, Vector2 position, Vector2 size, float opacity, float rotation)
        {
            PlaceSculpture(sculpture, AncientEnforcerVisuals.ObjectSprite(shape), position, size, opacity, rotation);
        }

        static void PlaceSculpture(SpriteRenderer renderer, Sprite sprite, Vector2 position, Vector2 size, float opacity, float rotation)
        {
            renderer.sprite = sprite;
            renderer.enabled = opacity > .01f;
            renderer.color = new Color(.86f, .97f, 1f, opacity);
            renderer.transform.position = position;
            renderer.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            renderer.transform.rotation = Quaternion.Euler(0, 0, rotation);
        }

        public bool TryThrow(Vector2 cursor, Vector2 hero) => !stopped && !intro && hazards.TryThrow(cursor, hero, owner.AimPoint);
        public bool TryWireTarget(Vector2 cursor, Vector2 hero, out Vector2 point)
        {
            point = Vector2.zero;
            return !stopped && !intro && hazards.TryWireTarget(cursor, hero, out point);
        }
        internal int PlugObjectSlots => stopped || intro ? 0 : hazards.AllocatedSlots;
        internal bool PlugObjectHeld => !stopped && hazards.PlugObjectHeld;
        internal void CancelPlugObject() { if (!stopped) hazards.CancelPlugObject(); }
        internal bool TryGetPlugObject(int slot, out Vector2 point, out uint generation)
        {
            point = Vector2.zero;
            generation = 0;
            return !stopped && !intro && hazards.TryGetPlugObject(slot, out point, out generation);
        }
        internal bool TryUsePlugObject(int slot, uint generation, Vector2 origin) =>
            !stopped && !intro && hazards.TryUsePlugObject(slot, generation, origin, owner.AimPoint);
        internal bool UnsafeBladeGround(Vector2 point) => !stopped && !intro && hazards != null && hazards.UnsafeBladeGround(point);
        internal void BreakObjectsInRadius(Vector2 point, float radius)
        {
            if (!stopped && !intro && hazards != null) hazards.BreakObjectsInRadius(point,radius);
        }
        public bool TryBlade(Vector2 origin, Vector2 direction, float reach, float width) =>
            !stopped && !intro && hazards.TryBlade(origin, direction, reach, width);
        public void Collect(Vector2 point, float radius, float dt)
        {
            if (!stopped && !intro) hazards.Collect(point, radius, dt);
        }
        public bool TryAbsorbMote(Vector2 point, out Vector2 center)
        {
            center = owner ? owner.AimPoint : Vector2.zero;
            return !stopped && barrier && (point - center).sqrMagnitude < 28 * 28;
        }

        void ClearPresentation()
        {
            Cue.Clear(game, Cue.Style.Enforcer);
            HideExplosion();
            if (counterMark) counterMark.enabled = false;
            if (sculpture) sculpture.enabled = false;
            foreach (var preview in sideMissiles) if (preview) preview.enabled = false;
            visuals?.ClearVortex();
            visuals?.ClearBarrier();
            visuals?.ClearIntro();
            visuals?.ClearBeam();
        }

        public void StopBattle()
        {
            if (stopped) return;
            if (intro) cinematic.Exit();
            stopped = true;
            activeMove = intro = barrier = false;
            hazards?.Clear();
            echoes?.Clear();
            ClearPresentation();
            ClearIntroEffects();
            if (game && game.Enforcer == this) game.Enforcer = null;
        }

        public void Retreat()
        {
            StopBattle();
            retreatStart = transform.position;
            retreatTime = 0;
            owner.SetEnforcerPose(0, game.hero.transform.position.x < retreatStart.x ? -1 : 1, 1, 1);
        }

        void OnDestroy()
        {
            StopBattle();
            hazards?.Dispose();
            echoes?.Dispose();
            visuals?.Dispose();
            if (explosionMaterial) Destroy(explosionMaterial);
        }
    }
}
