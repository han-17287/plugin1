using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 두 플레이어가 같은 발 피벗, 원화 48프레임, 손잡이·칼끝 좌표를 공유한다.
    internal static class AncientBladeMotion
    {
        public static void Paint(SpriteRenderer body, int step, int frame, float elapsed,
            Vector2 aim, float facing, bool grounded, Vector2 velocity, Vector2 feet, float size)
        {
            float start = AncientBladeRules.ActiveStart(step);
            float end = AncientBladeRules.ActiveEnd(step);
            float duration = AncientBladeRules.Duration(step);
            float load = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / start));
            float drive = Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - start * .76f) / (end - start)));
            float settle = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(end, duration, elapsed));
            // 발의 하중 이동이 먼저 시작된다. 팔과 칼의 늦은 회전은 원본 프레임이 담당한다.
            float weight = (-.44f * load + 1.44f * drive) * (1 - settle);
            float lean = weight * (step == 3 ? 7 : 5);
            float squash = .045f * load * (1 - drive);
            float air = grounded ? 0 : Mathf.Clamp(velocity.y / 16f, -1, 1);
            float tilt = facing * (-lean + Mathf.Clamp(aim.y, -1, 1) * drive * (grounded ? 3 : 11) * (1 - settle)
                + air * 5 * (1 - settle));
            var rotation = Quaternion.Euler(0, 0, tilt);
            Vector3 scale = new Vector3(size * (1 + squash * .6f), size * (1 - squash), 1);
            Vector3 offset = new Vector3(facing * weight * (grounded ? .065f : -.05f), 0, 0);
            // 공중에서는 허리에서 각운동을 받으므로 발 피벗으로 몸 전체가 흔들리지 않는다.
            if (!grounded)
            {
                Vector3 hip = Vector3.up * size;
                offset += hip - rotation * hip;
            }
            body.sprite = AncientArt.FieldBlade(step, frame);
            body.flipX = facing < 0;
            body.transform.localPosition = (Vector3)feet + offset;
            body.transform.localRotation = rotation;
            body.transform.localScale = scale;
        }

        public static Vector2 World(SpriteRenderer actor, Vector2 local)
        {
            if (actor.flipX) local.x = -local.x;
            return actor.transform.TransformPoint(local);
        }

        public static void Sweep(SpriteRenderer actor, int step, int frame, Vector3[] tips, Vector3[] grips)
        {
            // 원화에 기록된 칼끝을 통과하는 곡선이다. 마지막 표본은 현재 자세의 칼끝과 일치한다.
            float from = Mathf.Max(4, frame - (step == 3 ? 3f : 2.5f));
            for (int i = 0; i < tips.Length; i++)
            {
                float pose = Mathf.Lerp(from, frame, i / (float)(tips.Length - 1));
                int a = Mathf.FloorToInt(pose);
                float t = pose - a;
                Vector2 start = AncientBladeAtlas.TipLocal(step, a);
                Vector2 end = AncientBladeAtlas.TipLocal(step, a + 1);
                Vector2 chord = end - start;
                float limit = chord.magnitude;
                Vector2 exit = Vector2.ClampMagnitude((end - AncientBladeAtlas.TipLocal(step, a - 1)) * .44f, limit);
                Vector2 enter = Vector2.ClampMagnitude((AncientBladeAtlas.TipLocal(step, a + 2) - start) * .44f, limit);
                float bend = t * (1 - t);
                Vector2 tip = start + chord * (t * t * (3 - 2 * t)) + bend * ((1 - t) * exit - t * enter);
                tips[i] = World(actor, tip);
                grips[i] = World(actor, Vector2.Lerp(AncientBladeAtlas.HiltLocal(step, a), AncientBladeAtlas.HiltLocal(step, a + 1), t));
            }
        }
    }
}
