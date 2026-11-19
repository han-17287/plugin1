using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 컷신 동안 캐릭터 위치의 소유권을 한 곳에 두고 모든 종료 경로로 같은 물리 설정을 복구한다.
    internal sealed class AncientEncounterCutscene
    {
        AncientHero hero;
        Rigidbody2D body;
        RigidbodyType2D originalType;
        RigidbodyInterpolation2D originalInterpolation;
        bool originalSimulation;
        readonly AncientCinematicSkip skip = new AncientCinematicSkip();
        GUIStyle skipLabel;

        public void Enter(AncientHero actor)
        {
            if (hero || !actor) return;
            hero = actor;
            body = actor.GetComponent<Rigidbody2D>();
            if (body)
            {
                originalType = body.bodyType;
                originalInterpolation = body.interpolation;
                originalSimulation = body.simulated;
                hero.HoldForEncounter(true);
                body.bodyType = RigidbodyType2D.Kinematic;
                body.interpolation = RigidbodyInterpolation2D.None;
            }
            skip.Suspend();
        }

        public void Exit()
        {
            if (body)
            {
                body.bodyType = originalType;
                body.interpolation = originalInterpolation;
                body.simulated = originalSimulation;
            }
            hero = null;
            body = null;
            skip.Suspend();
        }

        public void SuspendSkip() => skip.Suspend();

        public bool PollSkip(float seconds, float holdTime) =>
            skip.Tick(Input.GetKey(KeyCode.Space), seconds, holdTime);

        public void DrawSkip(float holdTime)
        {
            if (skipLabel == null)
                skipLabel = new GUIStyle(GUI.skin.label) { font = AncientKoreanUI.Font, alignment = TextAnchor.MiddleRight };
            float height = Mathf.Clamp(Screen.height * .026f, 18, 31);
            float width = Mathf.Min(200, Screen.width * .36f);
            Rect label = new Rect(Screen.width - width - 18, Screen.height - height - 12, width, height);
            skipLabel.fontSize = Mathf.RoundToInt(height * .7f);
            int depth = GUI.depth;
            Color color = GUI.color;
            GUI.depth = -21;
            GUI.color = Color.white;
            GUI.Label(label, "Space 길게 · 건너뛰기", skipLabel);
            float progress = skip.Progress(holdTime);
            if (progress > 0)
            {
                GUI.color = new Color(.45f, .85f, 1);
                float total = Mathf.Min(width, height * 6);
                GUI.DrawTexture(new Rect(label.xMax - total, label.yMax, total * progress, 2), Texture2D.whiteTexture);
            }
            GUI.color = color;
            GUI.depth = depth;
        }
    }
}
