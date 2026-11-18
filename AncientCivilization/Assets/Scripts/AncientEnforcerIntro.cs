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

        AncientEnforcerCinematicFx introFx;
        Sprite introMetal;
        Vector2 introStart, introDrop, introReveal, heroRest;
        float introTime;
        bool dropped, forgeSound, revealSound;

        void BeginIntro()
        {
            intro = true;
            game.ClearBattleProjectiles();
            game.hero.HoldForEncounter(true);
            float startX = Mathf.Clamp(game.hero.transform.position.x, left + 1.5f, right - 11f);
            introStart = new Vector2(startX, 1.3f);
            introDrop = introStart + Vector2.right * 2.55f;
            heroRest = introStart - Vector2.right * .22f;
            introReveal = new Vector2(introDrop.x + 4.72f, .18f);
            introFx = new AncientEnforcerCinematicFx(game, transform);
            introMetal = AncientArt.Scrap();
            game.hero.CinematicMove(introStart);
            transform.position = introReveal;
            owner.SetEnforcerPose(0, -1, 1, 0);
        }

        void Update()
        {
            if (retreatTime >= 0)
            {
                if (!game || game.MainMenu || game.Story.CurrentFloor != 2) { Destroy(gameObject); return; }
                if (game.skills || !Application.isFocused) return;
                retreatTime += Time.unscaledDeltaTime;
                float fade = Ease((retreatTime - .32f) / .64f);
                transform.position = retreatStart + new Vector2(1.8f * fade, .9f * fade);
                owner.SetEnforcerPose(5, -1, Mathf.Lerp(1, .8f, fade), 1 - fade);
                if (retreatTime >= .98f) Destroy(gameObject);
                return;
            }
            if (!InputLocked || game.MainMenu || game.skills || !Application.isFocused ||
                (game.Story && (game.Story.DialogueOpen || game.Story.LiftBusy)) || (game.Journal && game.Journal.Open)) return;
            introTime += Time.unscaledDeltaTime;
            if (introTime >= IntroDuration || introTime > .5f && game.GameplayInputReady && Input.GetKeyDown(KeyCode.Space))
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

        void AnimateOfficer()
        {
            // 백 경위는 멈춘 다음 뒤로 짧게 몸을 던지고 발을 디뎌 제동한다.
            Vector2 point = Vector2.Lerp(introStart, introDrop, Ease(introTime / .88f));
            const float dodgeAt = 2.96f;
            float dodge = Ease((introTime - dodgeAt) / .31f);
            point = Vector2.Lerp(point, heroRest, dodge) + Vector2.up * (Mathf.Sin(dodge * Mathf.PI) * .38f);
            game.hero.CinematicMove(point);
            if (introTime < .88f) game.hero.ProductionCinematicRun(introTime, 1);
            else if (introTime >= dodgeAt && introTime < ImpactAt + .48f)
            {
                game.hero.CinematicPose(true, 1);
                game.hero.ProductionCinematicRoll(Mathf.Sin(dodge * Mathf.PI) * 14f);
            }
            else game.hero.CinematicPose(false, 1);
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
                    game.Sound(2);
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
                game.Sound(8);
            }
            introFx.Impact(new Vector2(introDrop.x, .12f), introTime - ImpactAt);
        }

        void AnimateReveal()
        {
            float appear = Ease((introTime - 4.13f) / .65f);
            float settle = Ease((introTime - 4.65f) / .8f);
            transform.position = introReveal + new Vector2((1 - settle) * .95f, (1 - settle) * .57f);
            int posture = introTime < 4.83f ? 4 : introTime < CameraToLens + .03f || introTime > CameraToArena + .15f ? 0 : 1;
            owner.SetEnforcerPose(posture, -1, 1f, appear);
            float light = appear * (1 - Ease((introTime - CameraToArena) / 1.05f));
            introFx.Reveal((Vector2)transform.position + new Vector2(-.08f, 1.67f), introTime, light);
            if (!revealSound && introTime >= CameraToLens)
            {
                revealSound = true;
                game.Sound(5);
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
            game.hero.HoldForEncounter(false);
            game.BlockGameplayInput();
            game.CutCamera(new Vector2(middle, 4.6f), ArenaZoom());
            ClearPresentation();
            ClearIntroEffects();
            BeginRecovery(1.15f);
            game.Notice("기록 집행관 / 입자가 모이는 곳을 피하세요.", 2.8f);
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
                focus = (Vector2)transform.position + new Vector2(Mathf.Lerp(-.55f, -.24f, approach), 1.71f);
                zoom = Mathf.Lerp(2.52f, 2.20f, approach);
                return true;
            }
            float wide = Ease((introTime - CameraToArena) / (IntroDuration - CameraToArena));
            float stare = Ease((introTime - CameraToLens) / (CameraToArena - CameraToLens));
            Vector2 close = introReveal + new Vector2(Mathf.Lerp(-.20f, -.38f, stare), 2.33f + .12f * stare);
            focus = Vector2.Lerp(close, new Vector2(middle, 4.6f), wide);
            zoom = Mathf.Lerp(Mathf.Lerp(1.42f, 1.30f, stare), ArenaZoom(), wide);
            return true;
        }

        float ArenaZoom() => Mathf.Max(5.8f, (right - left + 3) / (2 * Mathf.Max(.5f, game.View.aspect)));

        void OnGUI()
        {
            if (!InputLocked || !game || game.MainMenu || game.skills || Event.current.type != EventType.Repaint) return;
            visuals.DrawIntroOverlay(introTime, IntroDuration);
        }

        void ClearIntroEffects()
        {
            introFx?.Dispose();
            introFx = null;
        }
    }
}
