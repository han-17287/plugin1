using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 한 조우에서 같은 시계 원화를 공유한다. 다이얼의 장식에는 충돌체가 없다.
    public sealed class AncientClockDial : MonoBehaviour
    {
        enum Part { Case, Pinion, Balance, Pallet, Jewel, LockRing, Limit, Hour, Minute, Second }

        readonly Sprite[] parts = new Sprite[10], flying = new Sprite[3];
        readonly SpriteRenderer[] hands = new SpriteRenderer[2];
        readonly LineRenderer[] refuge = new LineRenderer[2];
        Transform works;
        SpriteRenderer bezel, drive, follower, balance, pallet, jewel, lockRing, limit;
        static readonly Color SafeInk = new Color(.88f, .96f, .80f, .84f);

        internal AncientClockStrikeFX Strikes { get; private set; }

        public void Initialize(AncientGame game, int lanes = 1)
        {
            Strikes = new AncientClockStrikeFX(game, lanes);
            var sheet = Resources.Load<Texture2D>("Art/SecretaryEscapement");
            parts[(int)Part.Case] = Cut(sheet, 0, 0, 512, 512, .5f, .5f, 248);
            parts[(int)Part.Pinion] = Cut(sheet, 512, 0, 256, 256, .5f, .5f, 119);
            parts[(int)Part.Balance] = Cut(sheet, 768, 0, 128, 128, .5f, .5f, 53);
            parts[(int)Part.Pallet] = Cut(sheet, 896, 0, 96, 96, 43f / 96, 31f / 96, 42);
            parts[(int)Part.Jewel] = Cut(sheet, 768, 128, 128, 128, .5f, .5f, 46);
            parts[(int)Part.LockRing] = Cut(sheet, 512, 256, 256, 256, .5f, .5f, 120);
            parts[(int)Part.Limit] = Cut(sheet, 768, 256, 256, 256, .5f, .5f, 123);
            for (int i = 0; i < 3; i++)
            {
                // 회전축부터 끝까지 정확히 320px. 발사체는 같은 원화의 무게중심을 기준으로 쓴다.
                parts[(int)Part.Hour + i] = Cut(sheet, 0, 512 + 80 * i, 384, 72, 54f / 384, .5f, 320);
                flying[i] = Cut(sheet, 0, 512 + 80 * i, 384, 72, 190f / 384, .5f, 368);
            }

            works = new GameObject("발동부 · 황동 시계").transform;
            works.SetParent(transform, false);
            bezel = Piece(Part.Case, works, 5);
            drive = Piece(Part.Pinion, works, 6);
            follower = Piece(Part.Pinion, works, 6);
            balance = Piece(Part.Balance, works, 6);
            pallet = Piece(Part.Pallet, works, 7);
            lockRing = Piece(Part.LockRing, works, 7);
            limit = Piece(Part.Limit, transform, 7);
            hands[0] = Piece(Part.Hour, transform, 16);
            hands[1] = Piece(Part.Minute, transform, 15);
            jewel = Piece(Part.Jewel, transform, 17);
            SetMount(drive, -.65f, -.18f, .165f);
            SetMount(follower, -.47f, -.37f, .122f);
            SetMount(balance, -.28f, -.47f, .115f);
            SetMount(pallet, -.375f, -.48f, .065f);
            for (int i = 0; i < refuge.Length; i++)
            {
                var line = game.Line("중앙 시계 아래 · 피난 경계", SafeInk, .048f, 14);
                line.transform.SetParent(transform, false);
                line.useWorldSpace = false;
                line.positionCount = 4;
                line.enabled = false;
                refuge[i] = line;
            }
            Hide();
        }

        static Sprite Cut(Texture2D texture, int x, int y, int w, int h, float px, float py, float density)
        {
            return Sprite.Create(texture, new Rect(x, texture.height - y - h, w, h),
                new Vector2(px, py), density, 0, SpriteMeshType.FullRect);
        }

        SpriteRenderer Piece(Part part, Transform parent, int order)
        {
            var renderer = new GameObject(part.ToString()).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = parts[(int)part];
            renderer.sortingOrder = order;
            return renderer;
        }

        static void SetMount(SpriteRenderer part, float x, float y, float scale)
        {
            part.transform.localPosition = new Vector3(x, y, 0);
            part.transform.localScale = Vector3.one * scale;
        }

        public Sprite Projectile(int kind) => flying[kind];

        static void SetTurn(SpriteRenderer part, float degrees)
        {
            part.transform.localRotation = Quaternion.Euler(0, 0, degrees);
        }

        static void Ink(SpriteRenderer part, float opacity)
        {
            part.color = new Color(1, 1, 1, Mathf.Clamp01(opacity));
        }

        void Mechanism(Vector2 origin, float radius, float driveAngle, float winding, float opacity)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            transform.position = origin;
            works.localScale = Vector3.one * radius;
            Ink(bezel, opacity);
            Ink(drive, opacity * .86f);
            Ink(follower, opacity * .80f);
            Ink(balance, opacity * .94f);
            Ink(pallet, opacity);
            float pawl = Mathf.Sin(driveAngle * 8 * Mathf.Deg2Rad);
            SetTurn(drive, driveAngle);
            SetTurn(follower, 10f - driveAngle);
            SetTurn(balance, pawl * 36);
            SetTurn(pallet, pawl * 13);
            lockRing.transform.localScale = Vector3.one * (.72f + .062f * winding);
            SetTurn(lockRing, 7f + driveAngle * .12f);
            Ink(lockRing, opacity * (.23f + .72f * winding));
            jewel.transform.localScale = Vector3.one * Mathf.Min(radius * .057f, .19f);
            Ink(jewel, opacity);
        }

        void HandPose(int index, float angle, float length, float girth, float opacity)
        {
            var hand = hands[index];
            hand.enabled = true;
            hand.transform.localScale = new Vector3(length, girth, 1);
            SetTurn(hand, angle * Mathf.Rad2Deg);
            Ink(hand, opacity);
        }

        void ClearBounds()
        {
            limit.enabled = false;
            refuge[0].enabled = refuge[1].enabled = false;
        }

        public void Wind(Vector2 origin, float progress, float hour, float minute)
        {
            float remaining = 1 - progress;
            float wound = 1 - remaining * remaining * remaining;
            Mechanism(origin, 3.55f, -378f * wound, progress, .85f);
            ClearBounds();
            HandPose(0, hour, AncientClockRules.HourForce, 2.55f, 1);
            HandPose(1, minute, AncientClockRules.MinuteForce, 2.80f, 1);
        }

        public void Strike(Vector2 origin, float elapsed)
        {
            // 추의 중심에서 래칫이 한 칸 풀린다. 발사 후에도 합벡터를 가리키는 표식은 만들지 않는다.
            float release = Mathf.Clamp01(elapsed / .24f);
            if (release >= 1) { Hide(); return; }
            float eased = 1 - (1 - release) * (1 - release);
            Mechanism(origin, 3.55f * (1 + .045f * eased), -378f - 19 * eased, 1, 1 - release);
            ClearBounds();
            hands[0].enabled = hands[1].enabled = false;
        }

        public void Destination(Vector2 point, float progress, bool central)
        {
            Mechanism(point, (central ? .98f : .67f) * (1 - .12f * progress),
                -24 - 94 * progress, progress, .55f + .4f * progress);
            ClearBounds();
            hands[0].enabled = hands[1].enabled = false;
        }

        public void Area(Vector2 origin, float radius, float angle, float elapsed, float progress,
            bool active, bool central, float safeHalfWidth)
        {
            float radiusOnCase = Mathf.Min(radius, 3.55f);
            float strength = active ? 1 : Mathf.Clamp01(progress);
            Mechanism(origin, radiusOnCase, -elapsed * 122, strength, active ? 1 : .48f + .35f * strength);
            HandPose(0, angle, radius, active ? 2.9f : 2.2f, active ? 1 : .77f);
            HandPose(1, -angle, radius, active ? 2.95f : 2.35f, active ? 1 : .77f);
            limit.enabled = true;
            limit.transform.localScale = Vector3.one * radius;
            Ink(limit, active ? .96f : .55f + .36f * strength);
            for (int i = 0; i < refuge.Length; i++)
            {
                var line = refuge[i];
                line.enabled = central;
                if (!central) continue;
                float side = i == 0 ? -1 : 1;
                float edge = side * safeHalfWidth;
                float foot = .07f - origin.y;
                // 안전 조건은 보스 기준 y < -.75, 중앙 ±safeHalfWidth. 열린 아래쪽으로 진입한다.
                line.SetPosition(0, new Vector2(edge + side * .22f, -.75f));
                line.SetPosition(1, new Vector2(edge, -.75f));
                line.SetPosition(2, new Vector2(edge, foot));
                line.SetPosition(3, new Vector2(edge + side * .25f, foot));
            }
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            Strikes?.Dispose();
            foreach (var item in parts) if (item) Destroy(item);
            foreach (var item in flying) if (item) Destroy(item);
        }
    }
}
