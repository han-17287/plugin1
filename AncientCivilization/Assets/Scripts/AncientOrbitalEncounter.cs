using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 1차전은 다음 공격을 전조 직전에 결정한다. 전조가 시작된 뒤에는 표적과 방향을 보존한다.
    public sealed class AncientOrbitalEncounter : MonoBehaviour
    {
        enum Beat { Inactive, Arrival, Tell, Strike, LowerCore, Opening, Leaving }
        static readonly string[] Names = {
            "바닥 충격파", "착지점 찍기", "왕복 회전날", "앵커 봉쇄", "화물 하단 스윕", "화물 낙하",
            "상승 빔", "교차 빔", "반전 셔터", "칼날 회수선", "코어 옆 착지 함정", "입자 공급기 파손",
            "화물 궤도 역전", "연쇄 낙하", "중앙 셔터", "퇴로 추적 찍기", "협착 스윕", "화물 관통 돌진"
        };

        AncientGame game;
        AncientEnemy owner;
        AncientOrbitalHazards hazards;
        AncientOrbitalRig rig;
        GameObject equipment;
        Beat state;
        OrbitalMove? last, previous;
        OrbitalMove move;
        Vector2 target, startPosition, restingPosition;
        int side, facing, phase = 1, announcedPhase, chainStep, chainLength;
        float timer, duration, openingDuration, hitFlash;
        bool latch, phaseSixOpened, lensFired;

        public AncientOrbitalArena Arena { get; private set; }
        public int Phase => phase;
        public int ChainStep => chainStep;
        public int ChainLength => chainLength;
        public OrbitalMove CurrentMove => move;
        public float Left { get; private set; }
        public float Right { get; private set; }
        public Vector2 CorePoint => owner ? owner.AimPoint : (Vector2)transform.position + Vector2.up * 1.65f;
        public bool Active => state != Beat.Inactive && state != Beat.Leaving;
        public bool Telegraphing => state == Beat.Tell;
        public bool Recovering => state == Beat.Opening;
        public float RecoveryRemaining => Recovering ? Mathf.Clamp01(1 - timer / duration) : 0;
        public float WindupProgress => Telegraphing ? Mathf.Clamp01(timer / duration) : 0;
        public bool Physical => move != OrbitalMove.RisingBeam && move != OrbitalMove.CrossBeam &&
            move != OrbitalMove.ShutterFeint && move != OrbitalMove.CargoSweep && move != OrbitalMove.ClosingSweep;

        public string Hint
        {
            get
            {
                if (Recovering) return latch ? "잠금장치 파손 · 코어 장시간 개방" : "코어 개방 · 반격";
                if (state == Beat.LowerCore) return "코어가 내려온다";
                if (state == Beat.Arrival) return "궤도 봉쇄기 · 좌우 고정 앵커";
                return Names[(int)move] + (chainStep < chainLength ? " · 두 번째 점등" : "");
            }
        }

        public void Initialize(AncientGame world, AncientEnemy enemy, float left, float right)
        {
            game = world;
            owner = enemy;
            Left = left;
            Right = right;
            facing = -1;
            rig = new AncientOrbitalRig(transform);
            equipment = new GameObject("궤도 봉쇄 전장");
            equipment.transform.SetParent(game.transform, true);
            Arena = new AncientOrbitalArena(game, equipment.transform, left, right);
            hazards = new AncientOrbitalHazards(game, equipment.transform, left, right);
            startPosition = transform.position;
            state = Beat.Arrival;
            duration = .95f;
            transform.position = startPosition + Vector2.up * 2.4f;
            rig.Pose(0, facing, false, false, false, 0);
            game.Notice("상층 적하 궤도 / 기계 팔의 점등과 착지 표식을 확인한다.", 2.7f);
        }

        public void Tick()
        {
            if (!Active || !game || !game.hero || game.Paused) return;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            Arena.Tick(dt);
            if (!Active) return; // 화물과 접촉한 그 프레임에 재도전할 수 있다.
            hitFlash = Mathf.Max(0, hitFlash - dt);
            timer += dt;
            float p = Mathf.Clamp01(timer / duration);
            switch (state)
            {
                case Beat.Arrival:
                    transform.position = Vector2.Lerp(startPosition + Vector2.up * 2.4f, startPosition, Ease(p));
                    if (p >= 1) BeginChain();
                    break;
                case Beat.Tell:
                    hazards.CargoPosition = Arena.CargoPoint;
                    hazards.TickTell(p);
                    ShowTelegraph(p);
                    if (p >= 1) StartStrike();
                    break;
                case Beat.Strike:
                    hazards.CargoPosition = Arena.CargoPoint;
                    hazards.TickActive(p);
                    if (!Active) return;
                    if (move == OrbitalMove.PursuitStamp || move == OrbitalMove.CargoRush)
                    {
                        Vector2 centre = hazards.RushPosition;
                        transform.position = new Vector2(centre.x, Mathf.Max(0, centre.y - 1.65f));
                    }
                    bool firing = move == OrbitalMove.ShutterFeint && AncientOrbitalHazards.ShutterFiring(p);
                    rig.Pose(Physical ? 2 : 4, facing, false, chainStep < chainLength, firing, hitFlash);
                    if (p >= 1) FinishStrike();
                    break;
                case Beat.LowerCore:
                    transform.position = Vector2.Lerp(startPosition, restingPosition, Ease(p));
                    rig.Pose(5, facing, p > .78f, false, false, hitFlash);
                    if (p >= 1)
                    {
                        state = Beat.Opening;
                        timer = 0;
                        duration = openingDuration;
                        phaseSixOpened |= phase == 6;
                        game.Sound(latch ? AncientEquipmentCue.ShortCircuit : AncientEquipmentCue.Contact);
                    }
                    break;
                case Beat.Opening:
                    rig.Pose(5, facing, true, false, false, hitFlash);
                    if (p >= 1) BeginChain();
                    break;
            }
        }

        static float Ease(float t) => t * t * (3 - 2 * t);

        void BeginChain()
        {
            phase = AncientOrbitalRules.Phase(owner.HealthRatio);
            if (phase != announcedPhase)
            {
                if (announcedPhase != 0)
                {
                    game.Impact(CorePoint, AncientGame.Cyan, false);
                    game.Notice(phase == 4 ? "회수 함정 / E 투척 · 직접 접근 또는 와이어 회수 · R 입자 폭발" :
                        phase == 6 ? "긴급 정지 / 마지막 찍기를 주황색 화물 잠금장치로 유도" :
                        "궤도 봉쇄기 / " + phase + "페이즈", phase == 4 || phase == 6 ? 3.8f : 1.4f);
                }
                announcedPhase = phase;
            }
            chainLength = AncientOrbitalRules.ChainLength(phase, Random.value);
            chainStep = 1;
            latch = false;
            PrepareAttack();
        }

        void PrepareAttack()
        {
            Vector2 hero = game.hero.transform.position;
            var arenaState = new OrbitalArenaState(Left, Right, hero.x, game.hero.IsOnGround,
                Arena.IsAnchorReachable(-1, hero), Arena.IsAnchorReachable(1, hero));
            bool preferStamp = phase == 6 && chainStep == chainLength && Random.value < .45f;
            if (!AncientOrbitalRules.TryPick(phase, arenaState, last, previous, Random.value, out move, chainStep > 1, preferStamp))
            {
                BeginOpening();
                return;
            }
            previous = last;
            last = move;
            facing = hero.x < owner.transform.position.x ? -1 : 1;
            side = Random.value < .5f ? -1 : 1;
            if (move == OrbitalMove.AnchorLock && Mathf.Abs(hero.x - Arena.CentreX) > 3f)
                side = hero.x < Arena.CentreX ? 1 : -1; // 가까운 탈출 고리를 남긴다.
            if (move == OrbitalMove.BladeFerry) side = Arena.CargoPoint.x < Arena.CentreX ? 1 : -1;
            if (move == OrbitalMove.ReverseRail) side = -Arena.RailDirection;

            target = hero;
            if (move == OrbitalMove.PursuitStamp || move == OrbitalMove.LandingStamp)
            {
                float prediction = move == OrbitalMove.PursuitStamp ? .34f : .12f;
                target.x += Mathf.Clamp(game.hero.HorizontalSpeed * prediction, -2.8f, 2.8f);
                target.y = 0;
            }
            if (move == OrbitalMove.CoreTrap) target = CorePoint + Vector2.right * (facing * 1.45f);
            if (move == OrbitalMove.SupplyLeak) target = Arena.DispenserPoint;
            if (move == OrbitalMove.AnchorLock) target = Arena.FixedAnchorPoint(side);
            target.x = Mathf.Clamp(target.x, Left + 1.15f, Right - 1.15f);
            Arena.PrepareMove(move, side, target);
            if (move == OrbitalMove.DropCargo || move == OrbitalMove.CargoRush) target = Arena.DropPoint;

            Vector2 source = move == OrbitalMove.CargoSweep || move == OrbitalMove.BladeFerry || move == OrbitalMove.ReverseRail
                ? Arena.CargoPoint : CorePoint;
            hazards.CargoPosition = Arena.CargoPoint;
            hazards.Telegraph(move, source, target, side);
            lensFired = false;
            timer = 0;
            duration = AncientOrbitalHazards.TellSeconds(move);
            state = Beat.Tell;
            game.Sound(AncientEquipmentCue.Forge);
            ShowTelegraph(0);
        }

        void ShowTelegraph(float progress)
        {
            bool realLens = move == OrbitalMove.ShutterFeint && progress >= .75f;
            if (realLens && !lensFired)
            {
                lensFired = true;
                facing = target.x >= CorePoint.x ? -1 : 1;
                game.Sound(AncientEquipmentCue.Lens);
            }
            rig.Pose(Physical ? 1 : 3, facing, false, chainStep < chainLength, realLens, hitFlash);
        }

        void StartStrike()
        {
            state = Beat.Strike;
            timer = 0;
            duration = AncientOrbitalHazards.ActiveSeconds(move);
            Arena.BeginMove(move, side, target);
            if (!Active) return;
            game.Sound(Physical ? AncientEquipmentCue.MetalFall : AncientEquipmentCue.Sweep);
        }

        void FinishStrike()
        {
            if (chainStep == chainLength && phase == 6 &&
                (move == OrbitalMove.LandingStamp || move == OrbitalMove.PursuitStamp))
            {
                latch = Arena.LockHit(hazards.ImpactPoint);
                if (latch)
                {
                    game.Burst(Arena.LatchPoint, AncientGame.Cyan, 20);
                    game.Notice("화물 잠금 해제 / 코어 장시간 노출", 1.8f);
                }
            }
            hazards.Clear();
            if (chainStep < chainLength)
            {
                chainStep++;
                PrepareAttack();
            }
            else BeginOpening();
        }

        void BeginOpening()
        {
            hazards.Clear();
            Arena.OpenCounter();
            startPosition = transform.position;
            float heroX = game.hero.transform.position.x;
            float approachSide = heroX <= Arena.CentreX ? 1f : -1f;
            restingPosition = new Vector2(Mathf.Clamp(heroX + 2.05f * approachSide, Left + 1.65f, Right - 1.65f), .08f);
            facing = heroX < restingPosition.x ? -1 : 1;
            openingDuration = AncientOrbitalRules.OpeningSeconds(phase, latch);
            // 먼 쪽에서 끝난 공격도 코어가 실제 근접 높이에 도착한 뒤부터 반격 시간을 센다.
            duration = Mathf.Clamp(Vector2.Distance(startPosition, restingPosition) / 21f, .22f, 1.35f);
            state = Beat.LowerCore;
            timer = 0;
        }

        public float FilterDamage(float incoming, float health, float maxHealth)
        {
            if (!Active || state == Beat.Arrival) return 0;
            float damage = AncientOrbitalRules.LimitDamage(incoming * (Recovering ? 1f : .22f), health, maxHealth, phase);
            return phase == 6 && !phaseSixOpened ? Mathf.Min(damage, Mathf.Max(0, health - 1f)) : damage;
        }

        public void OnHit(float healthRatio)
        {
            if (Active) hitFlash = .09f;
        }

        public void BeginRetreat()
        {
            if (!Active) return;
            ReleaseField();
            state = Beat.Leaving;
            timer = 0;
            duration = .98f;
            startPosition = transform.position;
            game.Sound(AncientEquipmentCue.ShortCircuit);
            if (rig != null) rig.Pose(5, facing, false, false, false, .075f);
        }

        void Update()
        {
            // 퇴장 시에는 적 목록에서 이미 빠졌으므로 연출만 독립적으로 마친다.
            if (state != Beat.Leaving || !game || game.MainMenu || game.skills ||
                (game.Journal && game.Journal.Open) || !Application.isFocused) return;
            timer += Mathf.Min(Time.unscaledDeltaTime, .1f);
            float t = Mathf.Clamp01(timer / duration);
            transform.position = startPosition + new Vector2(-facing * 2.5f * t, t * t * 7.5f);
            if (rig != null) rig.Opacity(1 - Mathf.InverseLerp(.6f, 1f, t));
            if (t >= 1) Destroy(gameObject);
        }

        public void StopBattle()
        {
            state = Beat.Inactive;
            ReleaseField();
        }

        void ReleaseField()
        {
            if (game && game.Orbital == this) game.Orbital = null;
            if (hazards != null) { hazards.Dispose(); hazards = null; }
            if (Arena != null) { Arena.Dispose(); Arena = null; }
            if (equipment) { equipment.SetActive(false); Destroy(equipment); equipment = null; }
        }

        void OnDestroy() { ReleaseField(); }
    }
}
