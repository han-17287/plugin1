using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed partial class AncientSniperEncounter
    {
        // 판정 좌표와 탄창 시계를 읽기만 한다. 본체, 손, 장비는 같은 피벗을 가진 한 장의 포즈다.
        const int CellWidth = 96, CellHeight = 96, ArtDensity = 3;
        const float JokerPpu = 27.5f;
        static readonly Sprite[] rifleFrames = new Sprite[30];
        static readonly Sprite[] transitionFrames = new Sprite[25];
        static readonly Sprite[] articulationFrames = new Sprite[26];
        readonly RifleView[] rifleViews = { new RifleView(), new RifleView() };
        AudioClip reloadClick, throwFoley;
        AncientAudio ordinaryAudio;
        SpriteRenderer relayEmitter, relayLens;

        sealed class RifleView
        {
            internal AncientEnemy Actor;
            internal SpriteRenderer Art, Echo, Flash;
            internal AudioSource Voice;
            internal Sprite PreviousFrame;
            internal Vector2 LastFoot, EchoFoot;
            internal Vector3 HomeLocal;
            internal AncientJokerLaneTransition Crossing;
            internal Vector3 LastScale;
            internal bool Rear, LastFlip, Seen, InReload;
            internal int SightElevation;
            internal float EchoStart = -1, LastRelease = float.NegativeInfinity;
        }

        // 첫 렌더링, 포탑 조종 분신과 전투 후 생존 대화도 같은 장비를 착용한다.
        internal static Sprite OrdinaryRiflePose(int legacyPose)
        {
            switch (legacyPose)
            {
                case 1: return RifleFrame(20);
                case 2: case 3: return RifleFrame(3);
                case 4: return RifleFrame(4);
                case 5: return RifleFrame(7);
                default: return RifleFrame(0);
            }
        }

        static Sprite RifleFrame(int frame)
        {
            if (rifleFrames[frame]) return rifleFrames[frame];
            Vector2 pivot = frame < 28 ? new Vector2(34.5f / CellWidth, 2.25f / CellHeight) :
                new Vector2(48.5f / CellWidth, 36.5f / CellHeight);
            // 조인트와 판정은 같은 좌표계를 쓰고, 원화만 세 배의 밀도로 읽는다.
            int width = CellWidth * ArtDensity, height = CellHeight * ArtDensity;
            Sprite sprite = AncientArt.Slice("JokerRifleMotion", "joker-rifle-" + frame,
                new Rect(frame % 6 * width, frame / 6 * height, width, height), JokerPpu * ArtDensity, pivot, false);
            // 공용 도트 로더를 거친 뒤 이 시트에만 하위 픽셀 명암을 보간한다.
            sprite.texture.filterMode = FilterMode.Bilinear;
            return rifleFrames[frame] = sprite;
        }

        // 중간 원화는 두 키포즈의 관절·손·장비 위치를 보간해 그렸다. 판정과 탄창의 시간은 그대로다.
        internal static Sprite TransitionFrame(int index)
        {
            if (transitionFrames[index]) return transitionFrames[index];
            int width = CellWidth * ArtDensity, height = CellHeight * ArtDensity;
            var cell = new Rect(index % 5 * width, index / 5 * height, width, height);
            var sole = new Vector2(34.5f / CellWidth, 2.25f / CellHeight);
            Sprite sprite = AncientArt.Slice("JokerRifleTransitions", "joker-field-transition-" + index,
                cell, JokerPpu * ArtDensity, sole, false);
            sprite.texture.filterMode = FilterMode.Bilinear;
            return transitionFrames[index] = sprite;
        }

        static Sprite BodyFrame(int pose)
        {
            if (pose < AncientJokerMotion.Inbetween) return RifleFrame(pose);
            if (pose < AncientJokerMotion.Articulation) return TransitionFrame(pose - AncientJokerMotion.Inbetween);
            int index = pose - AncientJokerMotion.Articulation;
            if (articulationFrames[index]) return articulationFrames[index];
            // 원래 288칸의 (3,50)..(231,286)만 사용하므로 발 피벗도 같은 양만큼 옮긴다.
            const int w = 228, h = 236;
            var sprite = AncientArt.Slice("JokerRifleArticulation", "joker-field-action-" + index,
                new Rect(index % 7 * w, index / 7 * h, w, h), JokerPpu * ArtDensity,
                new Vector2((103.5f - 3) / w, (286 - 281.25f) / h), false);
            sprite.texture.filterMode = FilterMode.Bilinear;
            return articulationFrames[index] = sprite;
        }

        // 원래 보스 Update가 이 프레임의 시선·탄도·난간 위치를 확정한 뒤에만 본다.
        void LateUpdate()
        {
            if (phase >= 6 || !StillFighting || !isActiveAndEnabled)
            {
                if (ordinaryRigActive) ClearOrdinaryPresentation();
                return;
            }
            if (game.Paused)
            {
                foreach (var view in rifleViews)
                    if (view.Voice && view.Voice.isPlaying) view.Voice.Stop();
                return;
            }
            if (!ordinaryRigActive) PrepareOrdinaryPresentation();

            bool emitterOn = false;
            for (int slot = 0; slot < rifleViews.Length; slot++)
            {
                var actor = actors[slot];
                var view = rifleViews[slot];
                if (actor == null || !actor.enemy || !actor.enemy.IsAlive || !actor.enemy.SniperBodyArt)
                {
                    HideActor(view);
                    continue;
                }
                var enemy = actor.enemy;
                var body = enemy.SniperBodyArt;
                if (view.Actor != enemy)
                {
                    HideActor(view);
                    view.Actor = enemy;
                    view.Art = body;
                    view.HomeLocal = body.transform.localPosition;
                    view.LastRelease = actor.lastGrenadeRelease;
                }

                bool rear = InRearLane(slot);
                Vector2 foot = enemy.transform.position;
                bool oldPose = view.Seen;
                if (oldPose && view.Rear != rear)
                {
                    view.Echo.sprite = view.PreviousFrame;
                    view.EchoFoot = view.LastFoot;
                    view.Echo.transform.localScale = view.LastScale;
                    view.Echo.sortingLayerID = body.sortingLayerID;
                    view.Echo.sortingOrder = view.Rear ? 4 : 7;
                    view.Echo.flipX = view.LastFlip;
                    view.EchoStart = Time.time;
                }
                float echo = Mathf.Clamp01(1 - (Time.time - view.EchoStart) / .16f);
                view.Echo.enabled = echo > 0 && view.PreviousFrame;
                if (view.Echo.enabled)
                {
                    // 본체가 다른 레인으로 이동해도 직전 모자와 총의 잔상은 원래 발 위치에 남는다.
                    view.Echo.transform.position = view.EchoFoot;
                    view.Echo.color = new Color(.51f, .63f, .76f, echo * .62f);
                }

                bool shooter = phase < 4 ? slot == 0 : enemy == (phase == 5 ? ChainShooter : owner);
                bool mirror = attacks.MirrorSight(out Vector2 mirrorOrigin, out Vector2 mirrorTarget) && shooter;
                bool sweep = attacks.SweepEmitter(foot.x, out Vector2 sweepEdge) && shooter;
                bool duel = slot == 0 && ClubDuelFrame >= 0;
                float now = Time.time;
                float budget = phase < 4 ? actor.until - now : endsBeat - now;
                bool neutral = !mirror && !sweep && !duel && actor.flash <= now &&
                    (phase < 4 ? actor.state == 0 : relay == RelayBeat.Run) &&
                    Vector2.Distance(foot, game.hero.transform.position) > 2.3f;
                if (oldPose && view.Rear != rear && neutral)
                    view.Crossing.Begin(view.LastFoot, foot, view.LastScale.x, now, budget);
                view.Crossing.Sample(foot, enemy.SniperPresentationScale, now, neutral,
                    out Vector2 displayFoot, out float displayScale);
                if (view.Crossing.Active || (view.EchoFoot - displayFoot).sqrMagnitude < 1.6f * 1.6f)
                    view.Echo.enabled = false;
                // 피격체와 투사체 원점은 움직이지 않는다. 예고가 들어온 프레임부터는 원위치다.
                body.transform.localPosition = view.HomeLocal + enemy.transform.InverseTransformVector(displayFoot - foot);
                // 결투는 등에 멘 소총과 전용 단검 시트를 그대로 사용한다.
                int pose = NormalRifleFrame(slot, actor, view, mirror, mirrorOrigin, mirrorTarget, sweep);
                body.transform.localScale = new Vector3(displayScale, displayScale, 1);
                if (!duel)
                {
                    if (view.Crossing.Active) pose = 24;
                    body.sprite = BodyFrame(pose);
                    // 피해 판정에 쓰는 facing은 건드리지 않는다. 고정된 거울의 총구 쪽을 본다.
                    if (mirror) body.flipX = mirrorOrigin.x < foot.x;
                    else if (phase >= 4 && relay == RelayBeat.Run)
                        body.flipX = slot == 0 ? travelEnd.x < travelStart.x : travelStart.x < travelEnd.x;
                }
                else body.sprite = AncientJokerDuelArt.Frame(ClubDuelFrame);
                bool firedRay = mirror && attacks.StartedFiring;
                float flash = actor.flash > Time.time ? Mathf.Clamp01((actor.flash - Time.time) / .18f) :
                    firedRay ? Mathf.Clamp01(1f - (attacks.Progress * attacks.Duration - AncientSniperRules.AimSeconds) / .095f) : 0;
                view.Flash.enabled = !duel && (flash > 0 || sweep && !attacks.StartedFiring);
                if (view.Flash.enabled)
                {
                    float lens = sweep ? Mathf.Lerp(.15f, .63f, attacks.WindupProgress) : flash;
                    Vector2 muzzle = mirror ? mirrorOrigin : enemy.SniperMuzzle;
                    Vector2 kick = AncientJokerMotion.MuzzleKick(pose);
                    if (body.flipX) kick.x = -kick.x;
                    view.Flash.transform.position = muzzle + kick * (enemy.SniperPresentationScale / JokerPpu);
                    view.Flash.transform.localScale = Vector3.one * (.5f + lens * .55f);
                    view.Flash.sortingLayerID = body.sortingLayerID;
                    view.Flash.sortingOrder = body.sortingOrder + 1;
                    view.Flash.color = new Color(1f, .82f, .70f, lens);
                }
                if (sweep)
                {
                    emitterOn = true;
                    DrawRelayEmitter(sweepEdge);
                }
                bool inReload = !duel && phase < 4 && actor.state == 2 && flash <= .01f;
                if (actor.lastGrenadePhase == phase && actor.lastGrenadeRelease != view.LastRelease && actor.lastGrenadeRelease <= Time.time &&
                    Time.time - actor.lastGrenadeRelease < .10f)
                    PlayActorCue(view, throwFoley);
                else if (inReload && !view.InReload)
                    PlayActorCue(view, reloadClick);
                view.LastRelease = actor.lastGrenadeRelease;
                view.InReload = inReload;
                view.Seen = true;
                view.Rear = rear;
                view.LastFoot = displayFoot;
                view.LastScale = body.transform.lossyScale;
                view.LastFlip = body.flipX;
                view.PreviousFrame = body.sprite;
                view.Voice.mute = !Application.isFocused || ordinaryAudio && ordinaryAudio.Muted;
            }
            relayEmitter.enabled = relayLens.enabled = emitterOn;
        }

        int NormalRifleFrame(int slot, Actor actor, RifleView view, bool mirror, Vector2 mirrorOrigin, Vector2 mirrorTarget, bool sweep)
        {
            if (sweep)
                return AncientJokerMotion.Sweep(attacks.StartedFiring,
                    attacks.StartedFiring ? attacks.FiringAge : attacks.WindupProgress);
            if (mirror)
            {
                if (attacks.StartedFiring)
                    return AncientJokerMotion.Shot(attacks.FiringAge, view.SightElevation);
                return AncientJokerMotion.Aim(attacks.WindupProgress, mirrorTarget - mirrorOrigin, ref view.SightElevation);
            }
            if (phase >= 4)
            {
                if (relay == RelayBeat.Return && slot == 0 || relay == RelayBeat.Feint && slot == 1)
                    return AncientJokerMotion.Return(BeatProgress);
                if (relay == RelayBeat.Run)
                    return AncientJokerMotion.Run(actor.enemy.transform.position.x - (slot == 0 ? travelStart.x : travelEnd.x));
                return relay == RelayBeat.Opening ? AncientJokerMotion.Land(Time.time - beganBeat) : 0;
            }
            if (phase == 2 && swapping && actor.state == 0) return 24;
            if (actor.flash > Time.time)
            {
                return AncientJokerMotion.Shot(Time.time - (actor.flash - .18f), view.SightElevation);
            }
            if (actor.state == 2)
            {
                float loaded = 1 - Mathf.Clamp01((actor.until - Time.time) / AncientSniperRules.ReloadSeconds);
                return AncientJokerMotion.Reload(loaded);
            }
            float thrown = Time.time - actor.lastGrenadeRelease;
            if (actor.state == 3 && actor.grenades > 0 && actor.lastGrenadePhase == phase && thrown >= 0)
                return AncientJokerMotion.RepeatGrenade(thrown);
            if (actor.lastGrenadePhase == phase && thrown >= 0 && thrown < .17f)
                return AncientJokerMotion.ReleaseGrenade(thrown);
            if (actor.grenades > 0 && (actor.state == 1 || actor.state == 3))
            {
                if (actor.state == 3) return 207;
                float wind = Mathf.InverseLerp(actor.began, actor.until, Time.time);
                return AncientJokerMotion.DrawGrenade(wind);
            }
            if (actor.state == 1)
                return AncientJokerMotion.Aim(Mathf.InverseLerp(actor.began, actor.until, Time.time),
                    actor.aim - actor.enemy.SniperMuzzle, ref view.SightElevation);
            if (actor.lastGrenadePhase == phase && thrown >= .17f && thrown < .32f)
                return thrown < .24f ? 208 : 210;
            if (actor.flash > 0 && Time.time < actor.flash + .18f)
                return AncientJokerMotion.LowerStock(Time.time - actor.flash);
            return AncientJokerMotion.Idle(Time.time + slot * .73f);
        }

        bool ordinaryRigActive;
        void PrepareOrdinaryPresentation()
        {
            ordinaryRigActive = true;
            if (relayEmitter) return;
            for (int frame = 0; frame < rifleFrames.Length; frame++) RifleFrame(frame);
            for (int frame = 0; frame < transitionFrames.Length; frame++) TransitionFrame(frame);
            for (int frame = 0; frame < articulationFrames.Length; frame++) BodyFrame(AncientJokerMotion.Articulation + frame);
            ordinaryAudio = game.GetComponent<AncientAudio>();
            reloadClick = Resources.Load<AudioClip>("Audio/JokerMagazine");
            throwFoley = Resources.Load<AudioClip>("Audio/JokerToss");
            foreach (var view in rifleViews)
            {
                view.Echo = Visual("모자와 총의 철골 잔상", RifleFrame(24), 4);
                view.Flash = Visual("조커 총구의 셔터", RifleFrame(29), 10);
                view.Voice = view.Flash.gameObject.AddComponent<AudioSource>();
                view.Voice.playOnAwake = false;
                view.Voice.spatialBlend = 0;
                view.Voice.priority = 48;
                view.Voice.outputAudioMixerGroup = game.Feedback.effects;
            }
            relayEmitter = Visual("옥상 스윕의 광학 중계기", RifleFrame(28), 16);
            relayLens = Visual("중계기 발광 구경", RifleFrame(29), 20);
        }

        SpriteRenderer Visual(string label, Sprite frame, int order)
        {
            var sprite = AncientArt.Display(label, frame, Vector2.zero, order, transform);
            sprite.enabled = false;
            return sprite;
        }

        void DrawRelayEmitter(Vector2 edge)
        {
            // 피격선이 시작되는 바로 그 끝점에 렌즈를 둔다. 총과 중계기 사이는 배경 레인이다.
            bool leftSide = edge.x < (left + right) * .5f;
            relayEmitter.transform.position = relayLens.transform.position = edge;
            relayEmitter.flipX = !leftSide;
            bool firing = attacks.StartedFiring;
            float aperture = firing ? .95f : Mathf.Lerp(.12f, .70f, attacks.WindupProgress);
            relayLens.transform.localScale = Vector3.one * aperture;
            relayLens.color = new Color(1, .60f, .68f, aperture);
        }

        void PlayActorCue(RifleView view, AudioClip clip)
        {
            if (!clip || !Application.isFocused || ordinaryAudio && ordinaryAudio.Muted) return;
            view.Voice.clip = clip;
            view.Voice.volume = game.Feedback.warningVolume;
            view.Voice.panStereo = Mathf.Clamp((view.Actor.transform.position.x - game.hero.transform.position.x) / 18, -.68f, .68f);
            view.Voice.Play();
        }

        static void HideActor(RifleView view)
        {
            if (view.Art) view.Art.transform.localPosition = view.HomeLocal;
            view.Crossing = default;
            view.SightElevation = 0;
            if (view.Echo) view.Echo.enabled = false;
            if (view.Flash) view.Flash.enabled = false;
            if (view.Voice && view.Voice.clip)
            {
                view.Voice.Stop();
                view.Voice.clip = null;
            }
            view.Actor = null;
            view.Art = null;
            view.Seen = view.InReload = false;
            view.PreviousFrame = null;
            view.EchoStart = -1;
        }

        void ClearOrdinaryPresentation()
        {
            foreach (var view in rifleViews) HideActor(view);
            if (relayEmitter) relayEmitter.enabled = false;
            if (relayLens) relayLens.enabled = false;
            ordinaryRigActive = false;
        }

        void OnDisable()
        {
            if (phase < 6 && game && game.Sniper == this) AncientBossCue.Clear(game, AncientBossCue.Style.Joker);
            if (ordinaryRigActive) ClearOrdinaryPresentation();
        }
    }
}
