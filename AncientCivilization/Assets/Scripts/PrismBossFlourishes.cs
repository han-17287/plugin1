using UnityEngine;

namespace Plugin.AncientCivilization
{
    internal static class PrismBossFlourishes
    {
        static readonly Color Ice = new Color(.35f, .94f, 1f);
        static readonly Color Ruby = new Color(1f, .23f, .43f);
        static readonly Color Violet = new Color(.59f, .44f, 1f);
        static readonly Color Pearl = new Color(1f, .97f, .87f);
        static readonly Color Copper = new Color(1f, .57f, .18f);
        static readonly Color Ion = new Color(.21f, .98f, .84f);
        static readonly Color Steel = new Color(.20f, .46f, .64f);
        static readonly Color Glass = new Color(.17f, .09f, .34f);

        internal static void Draw(BossFlourishBrush b, AncientBossCue.Style style, AncientBossCue.Beat beat, float t)
        {
            if (t >= 1f) return;
            t = Mathf.Clamp01(t);
            float light = 1f - t*t;
            if (style == AncientBossCue.Style.Joker)
                DrawPrism(b, beat, t, light);
            else if (style == AncientBossCue.Style.Orbital)
                DrawRail(b, beat, t, light);
        }

        static Color Tint(Color ink, float alpha)
        {
            ink.a = alpha;
            return ink;
        }

        static Vector2 Axis(float angle)
        {
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        // 모서리를 깎은 패의 한 면을 네 조각으로 나눠, 회전할 때마다 먼저 빛을 받는 면이 바뀐다.
        static void Card(BossFlourishBrush b, Vector2 center, Vector2 axis, float length, float width, float turn, float light)
        {
            Vector2 spine = axis*length;
            Vector2 face = new Vector2(-axis.y, axis.x)*(width*(.22f + .78f*Mathf.Abs(Mathf.Cos(turn))));
            Vector2 tail = center - spine, tip = center + spine;
            Vector2 top = tail + face*.76f, heel = tail - face*.54f;
            Vector2 corner = tip + face*.55f, bevel = tip - face*.26f;
            Vector2 ridge = center + spine*.16f + face*.37f;
            Color rim = Mathf.Cos(turn) > 0f ? Ice : Ruby;
            b.Triangle(top, heel, ridge, Tint(Glass, .85f*light));
            b.Triangle(heel, bevel, ridge, Tint(Violet, .65f*light));
            b.Triangle(bevel, corner, ridge, Tint(rim, .82f*light));
            b.Triangle(corner, top, ridge, Tint(Pearl, .40f*light));
            b.Stroke(top, corner, .027f, Tint(rim, light));
            b.Stroke(corner, bevel, .032f, Tint(Pearl, light));
            // 다이아 모양 인쇄는 패와 같은 원근으로 옆면에서 납작해진다.
            Vector2 suit = center - spine*.16f;
            b.Triangle(suit-spine*.24f, suit-face*.28f, suit+spine*.24f, Tint(Pearl, .96f*light));
            b.Triangle(suit+spine*.24f, suit+face*.28f, suit-spine*.24f, Tint(Ruby, light));
        }

        static void DrawPrism(BossFlourishBrush b, AncientBossCue.Beat beat, float t, float light)
        {
            if (beat == AncientBossCue.Beat.Tell)
            {
                // 두 광학 날이 총구를 향해 접힌다. 중앙의 조준선과 아직 잠긴 다음 거울은 비워 둔다.
                float gap = Mathf.Lerp(.47f, .135f, t*(2f-t));
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 knee = new Vector2(-.48f, side*(gap+.12f));
                    Vector2 sill = new Vector2(-.08f, side*gap);
                    Color rim = side > 0 ? Ice : Ruby;
                    b.Stroke(new Vector2(-.93f, side*(gap+.20f)), knee, .043f, Tint(Violet, .58f*light));
                    b.Stroke(knee, sill, .054f, Tint(rim, light));
                    b.Splinter(sill, new Vector2(.34f, side*(gap+.02f)), .046f, Tint(Pearl, light));
                    Card(b, new Vector2(-.50f + t*.29f, side*(gap+.20f)), Axis(side*(.39f + t*.48f)),
                        .22f, .14f, t*2.1f+side, light);
                }
                return;
            }

            bool fire = beat == AncientBossCue.Beat.Strike;
            float fan = t*(2f-t);
            // 분광된 빛 한 가닥만 바깥쪽 패에 맺힌다. 총구의 섬광과 위치를 분리한다.
            Vector2 glint = fire ? new Vector2(.88f+fan*.72f, .32f+.07f*fan) :
                Axis(.58f)*(.34f+fan*1.48f) + Vector2.down*(t*t*.52f);
            b.Halo(glint, fire ? .36f : .30f, fire ? .13f : .19f, Tint(fire ? Ice : Violet, .44f*light));
            if (fire)
            {
                // 총구를 통과한 백색광의 양옆에서 빨강과 파랑이 다른 굴절각으로 갈라진다.
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 knee = new Vector2(.34f+fan*.41f, side*(.12f+.07f*fan));
                    Vector2 bend = knee + new Vector2(.40f, side*.20f);
                    Vector2 end = bend + new Vector2(.46f+fan*.63f, -side*.065f);
                    Color rim = side > 0 ? Ice : Ruby;
                    b.Triangle(knee, bend, end, Tint(rim, .22f*light));
                    b.Stroke(knee, bend, .045f, Tint(Pearl, .85f*light));
                    b.Splinter(bend, end, .043f, Tint(rim, light));
                }
                b.Splinter(new Vector2(.09f + t*.42f, 0f), new Vector2(1.10f + fan*1.47f, 0f),
                    .064f*(1f-.48f*t), Tint(Pearl, light));
            }
            int cards = fire ? 3 : 6;
            for (int i=0; i<cards; i++)
            {
                float angle = fire ? -.69f+i*.56f : -1.62f+i*.55f;
                Vector2 axis = Axis(angle);
                float travel = (.23f + (i%3)*.11f) + fan*(1.20f + (i%2)*.41f + (!fire ? .28f : 0f));
                Vector2 center = axis*travel;
                center.y -= t*t*(fire ? .18f : .52f);
                Vector2 turn = Axis(angle + (i%2 == 0 ? 1f : -1f)*(.17f + t*.85f));
                Card(b, center, turn, .26f+(i%2)*.10f, .12f+(i%3)*.035f, i+t*5f, light);
                if (!fire)
                {
                    Vector2 tail = center - axis*(.23f+fan*.36f);
                    b.Splinter(tail, center - axis*.19f, .035f, Tint(i%2 == 0 ? Ruby : Ice, .82f*light));
                }
            }
        }

        // 레일 축을 감는 나선의 앞쪽 권선에만 반사광을 더한다. 뒤쪽 선은 얇고 어둡게 통과한다.
        static void Winding(BossFlourishBrush b, float x, float span, float radius, float twist, float phase, int steps, float light)
        {
            Vector2 previous = default;
            for (int i=0; i<=steps; i++)
            {
                float u = (float)i/steps;
                float angle = u*twist+phase;
                float depth = Mathf.Cos(angle);
                Vector2 point = new Vector2(x+u*span+depth*radius*.29f, Mathf.Sin(angle)*radius);
                if (i != 0)
                {
                    bool front = depth >= 0f;
                    b.Stroke(previous, point, front ? .080f : .041f, Tint(front ? Ion : Steel, light*(front ? .90f : .59f)));
                    if (depth > .36f)
                        b.Stroke(previous, point, .026f, Tint(Pearl, .94f*light));
                }
                previous = point;
            }
        }

        static void DrawRail(BossFlourishBrush b, AncientBossCue.Beat beat, float t, float light)
        {
            float travel = t*(2f-t);
            bool tell = beat == AncientBossCue.Beat.Tell;
            bool contact = beat == AncientBossCue.Beat.Contact;
            float radius = tell ? .36f-.11f*t : contact ? .20f+.51f*travel : .22f+.15f*travel;
            float start = tell ? -.66f+t*.15f : contact ? -.25f : -.37f+travel*.41f;
            float span = tell ? .78f : contact ? .50f+.54f*travel : .88f+.87f*travel;
            float phase = t*(tell ? 2.6f : 4.3f);
            if (!tell)
            {
                Vector2 spark = contact ? Axis(.77f)*(.68f+travel*1.44f) :
                    new Vector2(1.21f+travel*.96f, radius*.16f);
                b.Halo(spark, contact ? .24f : .36f, .14f, Tint(contact ? Copper : Ion, .43f*light));
            }
            Winding(b, start, span, radius, tell ? 11f : 15f, phase, tell ? 18 : 26, light);

            // 두 레일 슈의 구리 접점, 발사 후 남는 가늘고 긴 이온 흔적, 충돌로 뽑혀나간 슬롯 조각.
            for (int side=-1; side<=1; side+=2)
            {
                float clearance = radius + (tell ? .13f : .17f);
                Vector2 heel = new Vector2(start-.23f, side*clearance);
                Vector2 toe = new Vector2(start+span*(tell ? .71f : .54f), side*clearance);
                Vector2 jaw = toe + new Vector2(.16f, -side*.11f);
                b.Stroke(heel, toe, .088f, Tint(Steel, .85f*light));
                b.Stroke(toe, jaw, .075f, Tint(Copper, light));
                b.Splinter(heel+new Vector2(.03f,side*.046f), toe, .026f, Tint(Pearl, .78f*light));
                if (tell) continue;
                if (contact)
                {
                    for (int i=0; i<3; i++)
                    {
                        Vector2 axis = Axis(side*(.34f + i*.43f));
                        float distance = .30f + travel*(1.12f+.32f*(2-i));
                        Vector2 root = axis*distance;
                        Vector2 tip = root + axis*(.29f+i*.08f) + new Vector2(-.07f, side*.10f);
                        b.Splinter(root-axis*(.30f+.11f*travel), root, .038f, Tint(Ion, .74f*light));
                        b.Splinter(root, tip, .089f, Tint(Copper, light));
                        b.Stroke(root, tip, .023f, Tint(Pearl, light));
                    }
                }
                else
                {
                    Vector2 wake = new Vector2(start+span*.62f, side*radius*.67f);
                    b.Splinter(wake, new Vector2(1.45f+travel*.96f, side*.055f), .066f, Tint(Ion, .91f*light));
                }
            }
            if (!tell && !contact)
                b.Splinter(new Vector2(span*.76f,0f), new Vector2(1.50f+travel*1.12f,0f), .065f, Tint(Copper, light));
        }
    }
}
