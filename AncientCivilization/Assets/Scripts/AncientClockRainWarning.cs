using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 천장의 침 모양 눈금과 바닥 양끝 표식이 실제로 곧 내려올 한 열을 가리킨다.
    internal sealed class AncientClockRainWarning
    {
        readonly GameObject root;
        readonly LineRenderer[] ceiling, floor;
        readonly float left, right;
        static readonly Color[] ink =
        {
            new Color32(237, 186, 101, 255), new Color32(202, 219, 221, 255), new Color32(245, 121, 92, 255)
        };

        internal AncientClockRainWarning(AncientGame game, int lanes, float left, float right)
        {
            this.left = left;
            this.right = right;
            root = new GameObject("비서실장 · 침비 예고");
            root.transform.SetParent(game.transform, false);
            ceiling = new LineRenderer[lanes];
            floor = new LineRenderer[lanes];
            root.SetActive(false);
            for (int i = 0; i < lanes; i++)
            {
                ceiling[i] = Mark(game, "천장 침 눈금", 7);
                floor[i] = Mark(game, "착지 폭", 7);
            }
        }

        LineRenderer Mark(AncientGame game, string name, int points)
        {
            LineRenderer line = game.Line(name, Color.white, .045f, 14);
            line.transform.SetParent(root.transform, false);
            line.useWorldSpace = false;
            line.positionCount = points;
            return line;
        }

        internal void Show(int volley)
        {
            for (int i = 0; i < ceiling.Length; i++)
            {
                int kind = AncientClockRules.RainHand(volley, i);
                float x = AncientClockRules.RainX(left, right, ceiling.Length, volley, i);
                var top = ceiling[i];
                var foot = floor[i];
                top.transform.position = new Vector2(x, AncientClockRules.RainHeight);
                foot.transform.position = new Vector2(x, .07f);
                top.startColor = top.endColor = foot.startColor = foot.endColor = ink[kind];

                // 시침은 넓고 짧다. 초침은 밝은 빛을 꺼도 실루엣만으로 더 길고 가늘다.
                float half = .22f - kind * .055f;
                float stem = .57f + kind * .12f;
                top.SetPosition(0, new Vector2(-half * .6f, stem));
                top.SetPosition(1, new Vector2(half * .6f, stem));
                top.SetPosition(2, new Vector2(0f, stem));
                top.SetPosition(3, new Vector2(0f, .34f));
                top.SetPosition(4, new Vector2(-half, .34f));
                top.SetPosition(5, Vector2.zero);
                top.SetPosition(6, new Vector2(half, .34f));

                float a = Mathf.Max(left - x, -AncientClockRules.SpearHitRadius);
                float b = Mathf.Min(right - x, AncientClockRules.SpearHitRadius);
                foot.SetPosition(0, new Vector2(a, .24f));
                foot.SetPosition(1, new Vector2(a, 0f));
                foot.SetPosition(2, new Vector2(-.13f, 0f));
                foot.SetPosition(3, new Vector2(0f, .12f));
                foot.SetPosition(4, new Vector2(.13f, 0f));
                foot.SetPosition(5, new Vector2(b, 0f));
                foot.SetPosition(6, new Vector2(b, .24f));
            }
            Focus(0f);
            root.SetActive(true);
        }

        internal void Focus(float progress)
        {
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
            float width = Mathf.Lerp(.040f, .073f, p);
            float cock = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((p - .50f) * 2f));
            for (int i = 0; i < ceiling.Length; i++)
            {
                // 마지막에 천장 핀이 제자리로 밀려들고 착지 홈이 서면서 실제 낙하 폭은 계속 같은 위치에 남는다.
                ceiling[i].transform.localScale = new Vector3(1.18f - .18f * cock, .55f + .45f * p, 1f);
                floor[i].transform.localScale = new Vector3(1f, .67f + .60f * cock, 1f);
                ceiling[i].startWidth = width * .7f;
                ceiling[i].endWidth = width * 1.3f;
                floor[i].startWidth = floor[i].endWidth = width;
            }
        }

        internal void Hide() { if (root) root.SetActive(false); }
        internal void Dispose()
        {
            if (!root) return;
            root.SetActive(false);
            Object.Destroy(root);
        }
    }
}
