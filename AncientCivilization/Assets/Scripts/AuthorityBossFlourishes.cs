using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 좌표는 발동 지점 기준이다. +X는 공격 방향, 지면 접촉에서는 표면의 바깥 방향이다.
    internal static class AuthorityBossFlourishes
    {
        static readonly Color Ink = new Color(.035f, .16f, .29f);
        static readonly Color Film = new Color(.09f, .44f, .73f);
        static readonly Color Fold = new Color(.22f, .76f, .94f);
        static readonly Color Ice = new Color(.82f, .99f, 1f);
        static readonly Color Bronze = new Color(.36f, .16f, .09f);
        static readonly Color Gilt = new Color(.96f, .62f, .24f);
        static readonly Color Enamel = new Color(1f, .91f, .65f);
        static readonly Color Ruby = new Color(.83f, .24f, .22f);

        internal static void Draw(BossFlourishBrush b, AncientBossCue.Style style, AncientBossCue.Beat beat, float t)
        {
            if (t < 0f || t >= 1f) return;
            float open = t * (2f - t);
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.40f, 1f, t));
            if (style == AncientBossCue.Style.Clock)
            {
                if (beat == AncientBossCue.Beat.Tell) WindRatchet(b, open, fade);
                else if (beat == AncientBossCue.Beat.Strike) UncaseDial(b, open, fade);
                else BreakEnamel(b, t, open, fade);
            }
            else if (style == AncientBossCue.Style.Enforcer)
            {
                if (beat == AncientBossCue.Beat.Tell) Register(b, open, fade);
                else if (beat == AncientBossCue.Beat.Strike) UnfoldWrit(b, open, fade);
                else ScatterSeals(b, t, open, fade);
            }
        }

        static Color Opacity(Color c, float alpha)
        {
            c.a = alpha;
            return c;
        }

        static Vector2 Arm(float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        static Vector2 Cross(Vector2 v) => new Vector2(-v.y, v.x);

        static void Register(BossFlourishBrush b, float open, float fade)
        {
            // 비스듬한 전표 두 장이 겹치면서 가운데 재단선이 맞물린다.
            Vector2 pen = Arm(-12f + open * 8f);
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Vector2 back = new Vector2(-.62f + i * .18f, side * (.26f - .13f * open));
                Writ(b, back, pen, .85f + .16f * open, .20f, side, fade * (.65f + .25f * i));
            }
            for (int i = 0; i < 3; i++)
            {
                float y = (i - 1) * .17f;
                b.Stroke(new Vector2(.51f, y), new Vector2(.57f + .10f * open, y), .037f, Opacity(Ice, fade));
            }
        }

        static void UnfoldWrit(BossFlourishBrush b, float open, float fade)
        {
            // 큰 접힌 면을 먼저 읽고, 열린 가장자리에서만 흰 인쇄선이 빛난다.
            for (int i = 0; i < 3; i++)
            {
                int side = i - 1;
                Vector2 axis = Arm(side * (14f + 35f * open));
                Vector2 origin = new Vector2(-.34f + .29f * open, side * (.06f + .12f * open));
                Writ(b, origin, axis, (.63f + .96f * open) * (side == 0 ? 1f : .82f),
                    .12f + .13f * open, side < 0 ? -1f : 1f, fade * (side == 0 ? .95f : .76f));
            }
            for (int i = 0; i < 4; i++)
            {
                Vector2 axis = Arm((i < 2 ? -1 : 1) * (28f + i % 2 * 29f));
                Vector2 tip = axis * (.65f + open * (1.10f + i % 2 * .22f));
                b.Stroke(tip - axis * .20f, tip, .040f, Opacity(Fold, fade));
            }
        }

        static void Writ(BossFlourishBrush b, Vector2 heel, Vector2 axis, float length, float width, float side, float alpha)
        {
            Vector2 lateral = Cross(axis) * side;
            Vector2 corner = heel + lateral * width;
            Vector2 spine = heel + axis * (length * .82f);
            Vector2 tip = spine + axis * (length * .18f) + lateral * (width * 1.5f);
            Vector2 bevel = heel + axis * (length * .53f) + lateral * (width * 1.83f);
            b.Triangle(heel, corner, spine, Opacity(Ink, alpha * .83f));
            b.Triangle(corner, bevel, spine, Opacity(Film, alpha * .90f));
            b.Triangle(bevel, tip, spine, Opacity(Fold, alpha * .81f));
            b.Stroke(heel, spine, .048f, Opacity(Fold, alpha));
            b.Stroke(corner, bevel, .030f, Opacity(Fold, alpha * .74f));
            b.Stroke(bevel, tip, .048f, Opacity(Ice, alpha));
            // 접힌 전표의 등록 번호, 잘린 절취선, 각진 압인 도장을 같은 종이 위에 인쇄한다.
            Vector2 register = corner + axis * (length * .18f);
            b.Stroke(register, register + axis * (length * .24f), .035f, Opacity(Ice, alpha * .86f));
            register -= lateral * (width * .35f);
            b.Stroke(register, register + axis * (length * .14f), .027f, Opacity(Fold, alpha * .9f));
            Vector2 stamp = bevel - lateral * (width * .46f);
            b.Splinter(stamp - axis * .041f, stamp + axis * .041f, .032f, Opacity(Ice, alpha));
        }

        static void ScatterSeals(BossFlourishBrush b, float t, float open, float fade)
        {
            // 부서진 도장이 표면 바깥으로 갈라진다. 직사각형 실루엣과 서로 다른 세 면을 유지한다.
            for (int i = 0; i < 7; i++)
            {
                int rank = i / 2;
                int side = i % 2 == 0 ? -1 : 1;
                float reach = .48f + .45f * (3 - rank);
                float lift = rank == 3 ? 1.35f : .47f + .29f * rank;
                Vector2 center = new Vector2(.13f + open * lift - .31f * t * t, side * (.05f + open * reach));
                Vector2 edge = Arm(side * (19f + (16f + 7f * rank) * open)) * (.16f - .021f * rank);
                Vector2 shortEdge = Cross(edge) * (.69f + .23f * open);
                Vector2 depth = new Vector2(-.10f, side * .064f) * (1f - .48f * t);
                Vector2 a = center - edge - shortEdge, c = center + edge + shortEdge;
                b.Triangle(a, center + edge - shortEdge, c, Opacity(Fold, fade * .83f));
                b.Triangle(a, c, center - edge + shortEdge, Opacity(Film, fade * .95f));
                b.Triangle(a, a + depth, center - edge + shortEdge + depth, Opacity(Ink, fade * .9f));
                b.Triangle(a, center - edge + shortEdge + depth, center - edge + shortEdge, Opacity(Ink, fade * .9f));
                b.Stroke(center - edge + shortEdge, c, .045f, Opacity(Ice, fade));
                if (rank < 2) b.Stroke(a - new Vector2(.06f, side * .17f), a, .034f, Opacity(Fold, fade * .7f));
            }
        }

        static void WindRatchet(BossFlourishBrush b, float open, float fade)
        {
            float r = .55f - .13f * open;
            float turn = 25f * open;
            for (int half = 0; half < 2; half++)
            {
                float start = half * 180f + turn;
                b.Arc(Vector2.zero, r, start, 137f, .075f, Opacity(Bronze, fade), 10);
                b.Arc(Vector2.zero, r + .036f, start, 99f, .036f, Opacity(Enamel, fade), 8);
                Vector2 pallet = Arm(start + 148f);
                b.Splinter(pallet * (r + .15f), pallet * (r - .06f), .075f, Opacity(Gilt, fade));
                for (int i = 0; i < 4; i++)
                {
                    Vector2 ray = Arm(start + i * 29f);
                    b.Stroke(ray * r, ray * (r - .10f), .042f, Opacity(Gilt, fade));
                }
            }
            b.Splinter(new Vector2(-.16f, 0f), new Vector2(.16f, 0f), .11f, Opacity(Ruby, fade));
        }

        static void UncaseDial(BossFlourishBrush b, float open, float fade)
        {
            // 네 개의 베젤이 같은 간격으로 분해된다. 대칭 문양이어서 합력 창의 방향을 예고하지 않는다.
            float r = .48f + .69f * open;
            for (int i = 0; i < 4; i++)
            {
                float start = 12f + 90f * i + 19f * open;
                Vector2 radial = Arm(start + 26f);
                Vector2 center = radial * (.05f + .12f * open);
                b.Arc(center, r, start, 57f, .11f, Opacity(Bronze, fade * .92f), 6);
                b.Arc(center, r + .04f, start, 49f, .048f, Opacity(Enamel, fade), 5);
                b.Arc(center, r - .18f, start + 6f, 26f, .039f, Opacity(Gilt, fade * .84f), 3);
                for (int j = 0; j < 2; j++)
                {
                    Vector2 tooth = Arm(start + 8f + j * 29f);
                    b.Stroke(center + tooth * (r - .11f), center + tooth * (r + .075f), .047f, Opacity(Gilt, fade));
                }
                Vector2 screw = center + Arm(start + 49f) * r;
                Vector2 tangent = Cross(radial);
                b.Splinter(screw - tangent * .078f, screw + tangent * .078f, .061f, Opacity(Ruby, fade));
                b.Stroke(screw - tangent * .031f, screw + tangent * .031f, .024f, Opacity(Enamel, fade));
                b.Splinter(center + radial * (r + .11f), center + radial * (r + .32f), .053f, Opacity(Gilt, fade));
            }
        }

        static void BreakEnamel(BossFlourishBrush b, float t, float open, float fade)
        {
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float rank = i / 2;
                Vector2 center = new Vector2(.20f + (.45f + .44f * rank) * open - .27f * t * t,
                    side * (.16f + (1.09f - .45f * rank) * open));
                float a = side * (39f + 38f * rank + open * (47f + 13f * rank));
                float r = .27f - rank * .04f;
                b.Arc(center, r, a - 41f, 78f, .084f, Opacity(Bronze, fade), 5);
                b.Arc(center, r + .03f, a - 36f, 66f, .040f, Opacity(Enamel, fade), 4);
                Vector2 stem = Arm(a);
                b.Stroke(center + stem * (r - .10f), center + stem * (r + .03f), .046f, Opacity(Gilt, fade));
                Vector2 nextTick = Arm(a + 19f);
                b.Stroke(center + nextTick * (r - .065f), center + nextTick * r, .028f, Opacity(Gilt, fade * .88f));
                Vector2 chip = center + Cross(stem) * (.38f + .07f * open);
                b.Splinter(chip, chip + stem * .16f, .058f, Opacity(i < 2 ? Ruby : Gilt, fade));
            }
        }
    }
}
