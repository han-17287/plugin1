using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed partial class AncientEnforcerEncounter
    {
        const float IntroDuration = 8.65f;
        const float CameraToForge = 1.12f;
        const float CameraToDrop = 2.58f;
        const float DropStarts = 2.92f;
        const float FallSeconds = .49f;
        const float ImpactAt = DropStarts + FallSeconds;
        const float CameraToOfficer = 4.38f;
        const float CameraToLens = 5.68f;
        const float CameraToArena = 7.1f;

        [SerializeField, Min(.2f)] float introSkipHold = .6f;
        readonly AncientEncounterCutscene cinematic = new AncientEncounterCutscene();
        AncientEnforcerCinematicFx introFx;
        Sprite introMetal;
        AncientRadioCue openingRadio;
        Vector2 introStart, introDrop, introReveal, heroRest;
        float introTime;
        bool dropped, forgeSound, revealSound;

        void BeginIntro()
        {
            intro = true;
            game.ClearBattleProjectiles();
            cinematic.Enter(game.hero);
            float startX = Mathf.Clamp(game.hero.transform.position.x, left + 1.5f, right - 11f);
            introStart = new Vector2(startX, 1.3f);
            introDrop = introStart + Vector2.right * 2.55f;
            heroRest = introStart - Vector2.right * .22f;
            introReveal = new Vector2(introDrop.x + 4.72f, .18f);
            introFx = new AncientEnforcerCinematicFx(game, transform);
            introMetal = AncientArt.Scrap();
            openingRadio = Resources.Load<AncientRadioCue>("Radio/EnforcerEntry");
            game.hero.CinematicMove(introStart);
            transform.position = introReveal;
            owner.SetEnforcerPose(0, -1, 1, 0);
        }

        void Update()
        {
            if (retreatTime >= 0)
            {
                UpdateRetreat();
                return;
            }
            if (!InputLocked) return;
            if (!IntroVisible)
            {
                cinematic.SuspendSkip();
                return;
            }
            introTime += Time.unscaledDeltaTime;
            bool canSkip = introTime > .5f && game.GameplayInputReady;
            if (!canSkip) cinematic.SuspendSkip();
            if (introTime >= IntroDuration || canSkip && cinematic.PollSkip(Time.unscaledDeltaTime, introSkipHold))
            {
                EndIntro();
                return;
            }

            AnimateOfficer();
            AnimateFallingMetal();
            AnimateReveal();
            float light = Mathf.Lerp(.45f, 1, Ease(introTime / .48f)) * (1 - Ease((introTime - 7.65f) / 1f));
            introFx.Atmosphere(new Vector2(introDrop.x + 1.8f, 9.52f), 0f, introTime, light);
        }

        void UpdateRetreat()
        {
            if (!game || !game.Story || game.Story.CurrentFloor != 2)
            {
                Destroy(gameObject);
                return;
            }
            if (game.Paused)
            {
                // 퇴장 대사 첫 화면에서도 팔을 내려놓는 동작을 끝낸다. 실제 퇴장 시계는 멈춘다.
                if (game.Story.DialogueOpen && !game.MainMenu && !game.DeveloperMenuOpen && Application.isFocused)
                    owner.SetEnforcerPose(0, game.hero.transform.position.x < retreatStart.x ? -1 : 1,
                        1, 1, true, 0, Time.unscaledDeltaTime);
                return;
            }

            // 마지막 대사를 듣고 나면 추진기를 켜고 출구 너머까지 직접 비행한다.
            const float turnSeconds = .23f;
            const float accelerationSeconds = .8f;
            const float flightSpeed = 23f;
            retreatTime += Time.deltaTime;
            float flight = Mathf.Max(0, retreatTime - turnSeconds);
            float accelerating = Mathf.Min(flight, accelerationSeconds);
            float distance = flightSpeed * (.5f * accelerating * accelerating / accelerationSeconds
                + Mathf.Max(0, flight - accelerationSeconds));
            float rise = Ease(flight / .7f);
            transform.position = new Vector2(retreatStart.x + distance,
                Mathf.Lerp(retreatStart.y, 3.45f, rise));
            owner.SetEnforcerPose(flight > .1f ? 4 : 5, 1, 1, 1, flight <= .1f, 0, Time.deltaTime);

            // 울트라와이드나 빠르게 따라온 플레이어에게도 퇴장 중인 인물이 끊겨 보이지 않게 한다.
            Camera view = game.View;
            float visibleRight = view ? view.transform.position.x + view.orthographicSize * view.aspect : right;
            if (transform.position.x > Mathf.Max(right, visibleRight) + 3.5f) Destroy(gameObject);
        }

        void AnimateOfficer()
        {
            // 발이 닿을 때까지 이동을 이어가고, 준비·도약·착지 압축은 시각 자식에만 적용한다.
            const float runEnd = .88f, braceAt = 2.72f, jumpAt = 2.96f, landAt = 3.38f;
            float approach = Ease(introTime / runEnd);
            float dodge = Mathf.InverseLerp(jumpAt, landAt, introTime);
            float brake = Mathf.InverseLerp(landAt, landAt + .22f, introTime);
            Vector2 nearMiss = heroRest + Vector2.right * .24f;
            Vector2 run = Vector2.Lerp(introStart, introDrop, approach);
            Vector2 point = Vector2.Lerp(run, nearMiss, Ease(dodge));
            point += (heroRest - nearMiss) * (1 - (1 - brake) * (1 - brake));
            point.y += .52f * 4 * dodge * (1 - dodge);
            game.hero.CinematicMove(point);
            if (introTime < runEnd)
            {
                float walked = approach * Vector2.Distance(introStart, introDrop);
                game.hero.ProductionCinematicRun(walked / 7f, 1);
                game.hero.CinematicPosture(-3f * Mathf.Sin(Mathf.PI * approach), 0);
                return;
            }
            float brace = Ease((introTime - braceAt) / (jumpAt - braceAt)) * (1 - Ease(dodge * 3));
            float extension = Mathf.Sin(Mathf.PI * dodge);
            float landing = Mathf.Sin(Mathf.PI * brake) * (1 - brake);
            game.hero.CinematicPose(brace > .1f || extension > .08f || landing > .10f, 1);
            game.hero.CinematicPosture(-5 * brace + 12 * extension - 6 * landing,
                .12f * brace - .055f * extension + .22f * landing);
        }

        void AnimateFallingMetal()
        {
            Vector2 formingAt = new Vector2(introDrop.x, 7.17f);
            float progress = Mathf.Clamp01((introTime - CameraToForge) / (DropStarts - CameraToForge));
            float fall = Mathf.Clamp01((introTime - DropStarts) / FallSeconds);
            float vanish = Ease((introTime - 4.2f) / .66f);
            Vector2 center = new Vector2(formingAt.x - Mathf.Sin(fall * Mathf.PI) * .17f,
                Mathf.Lerp(formingAt.y, 1.08f, fall * fall));
            if (introTime >= 1.02f && introTime < DropStarts)
            {
                introFx.Condense(formingAt, progress, introTime, 2.66f);
                if (!forgeSound && progress >= .14f)
                {
                    forgeSound = true;
                    game.Sound(AncientEquipmentCue.Forge);
                }
            }
            else introFx.ClearCondensation();
            if (introTime >= DropStarts && introTime < ImpactAt) introFx.Falling(center, fall);
            else introFx.ClearFall();

            float opacity = Ease((progress - .19f) / .67f) * (1 - vanish);
            if (opacity > .002f)
            {
                float squash = Mathf.Sin(Mathf.Clamp01((introTime - ImpactAt) / .16f) * Mathf.PI) * (introTime >= ImpactAt ? .13f : 0);
                Vector2 size = new Vector2(2.85f * (1 + squash * .6f), 2.16f * (1 - squash));
                float tilt = Mathf.Lerp(-11f + 4f * Mathf.Sin(introTime * 3.3f), 0, fall);
                PlaceSculpture(sculpture, introMetal, center - Vector2.up * squash * .6f, size, opacity, tilt);
            }
            else sculpture.enabled = false;

            if (introTime < ImpactAt) return;
            if (!dropped)
            {
                dropped = true;
                game.Shake(.38f);
                game.Sound(AncientEquipmentCue.MetalFall);
            }
            introFx.Impact(new Vector2(introDrop.x, .12f), introTime - ImpactAt);
        }

        void AnimateReveal()
        {
            float appear = Ease((introTime - 4.13f) / .65f);
            float settle = Ease((introTime - 4.65f) / .8f);
            transform.position = introReveal + new Vector2((1 - settle) * .95f, (1 - settle) * .57f);
            int posture = introTime < 4.83f ? 4 : introTime < CameraToLens + .03f || introTime > CameraToArena + .15f ? 0 : 1;
            owner.SetEnforcerPose(posture, -1, 1f, appear, settle >= .99f, 0, Time.unscaledDeltaTime);
            float light = appear * (1 - Ease((introTime - CameraToArena) / 1.05f));
            introFx.Reveal(Vector2.Lerp(owner.EnforcerDossierWorld, owner.EnforcerLensWorld, .48f), introTime, light);
            if (!revealSound && introTime >= CameraToLens)
            {
                revealSound = true;
                game.Sound(AncientEquipmentCue.Lens);
            }
        }

        void EndIntro()
        {
            intro = false;
            game.EnforcerIntroSeen = true;
            transform.position = introReveal;
            owner.SetEnforcerPose(0, -1, 1, 1);
            game.hero.CinematicMove(heroRest);
            game.hero.CinematicPose(false, 1);
            cinematic.Exit();
            game.BlockGameplayInput();
            game.CutCamera(new Vector2(middle, 4.6f), ArenaZoom());
            ClearPresentation();
            ClearIntroEffects();
            BeginRecovery(1.15f);
            if (openingRadio) openingRadio.Play(game.Story);
        }

        public bool TryCamera(out Vector2 focus, out float zoom)
        {
            focus = Vector2.zero;
            zoom = 5.8f;
            if (!InputLocked) return false;
            if (introTime < CameraToForge)
            {
                float slide = Ease(introTime / CameraToForge);
                focus = new Vector2(Mathf.Lerp(introStart.x + .22f, introDrop.x - .12f, slide), 1.57f + .42f * slide);
                zoom = Mathf.Lerp(2.30f, 2.05f, slide);
                return true;
            }
            if (introTime < CameraToDrop)
            {
                float approach = Ease((introTime - CameraToForge) / (CameraToDrop - CameraToForge));
                focus = new Vector2(introDrop.x - .44f + .46f * approach, 6.68f + .32f * approach);
                zoom = Mathf.Lerp(3.28f, 2.37f, approach);
                return true;
            }
            if (introTime < CameraToOfficer)
            {
                float impact = Ease((introTime - ImpactAt) / .74f);
                focus = new Vector2(introDrop.x + .22f + .56f * impact, Mathf.Lerp(4.14f, 2.84f, impact));
                zoom = Mathf.Lerp(5.44f, 4.36f, impact);
                return true;
            }
            if (introTime < CameraToLens)
            {
                float approach = Ease((introTime - CameraToOfficer) / (CameraToLens - CameraToOfficer));
                // 서류철과 단안 렌즈를 함께 보여준 뒤 눈 클로즈업으로 자른다.
                focus = Vector2.Lerp(owner.EnforcerDossierWorld, owner.EnforcerLensWorld, .43f)
                    + new Vector2(Mathf.Lerp(-.32f, -.12f, approach), -.18f);
                zoom = Mathf.Lerp(2.10f, 1.83f, approach);
                return true;
            }
            float wide = Ease((introTime - CameraToArena) / (IntroDuration - CameraToArena));
            float stare = Ease((introTime - CameraToLens) / (CameraToArena - CameraToLens));
            Vector2 close = owner.EnforcerLensWorld + new Vector2(Mathf.Lerp(-.08f, -.16f, stare), .03f);
            focus = Vector2.Lerp(close, new Vector2(middle, 4.6f), wide);
            zoom = Mathf.Lerp(Mathf.Lerp(1.20f, 1.08f, stare), ArenaZoom(), wide);
            return true;
        }

        float ArenaZoom() => Mathf.Max(5.8f, (right - left + 3) / (2 * Mathf.Max(.5f, game.View.aspect)));

        internal bool PlayingIntro => InputLocked && IntroVisible;

        bool IntroVisible => game && !game.DeveloperMenuOpen && !game.MainMenu && !game.skills && Application.isFocused &&
            !(game.Story && (game.Story.DialogueOpen || game.Story.LiftBusy)) && !(game.Journal && game.Journal.Open);

        void OnApplicationFocus(bool focused)
        {
            if (!focused) cinematic.SuspendSkip();
        }

        void OnGUI()
        {
            if (!InputLocked || !IntroVisible || Event.current.type != EventType.Repaint) return;
            visuals.DrawIntroOverlay(introTime, IntroDuration);
            if (introTime > .5f && game.GameplayInputReady) cinematic.DrawSkip(introSkipHold);
        }

        void ClearIntroEffects()
        {
            introFx?.Dispose();
            introFx = null;
        }
    }
}
