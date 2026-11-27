using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 침의 낙하와 명중 판정이 보내는 시각 신호만 받는다. 다이얼이 접혀도 착지 자국은 재생된다.
    internal sealed class AncientClockStrikeFX
    {
        enum Mark { Ground, Crown }
        const int Frames = 8;
        const float GroundSeconds = .24f, CrownSeconds = .19f;
        struct Slot
        {
            public SpriteRenderer image;
            public Mark mark;
            public float age;
        }

        readonly AncientGame game;
        readonly GameObject root;
        readonly Sprite[] frames = new Sprite[Frames * 2];
        readonly Slot[] slots;
        int cursor;

        internal AncientClockStrikeFX(AncientGame game, int lanes)
        {
            this.game = game;
            // 한 일제 투하의 발사 링과 앞 열의 착지 자국이 동시에 남을 수 있다.
            slots = new Slot[lanes * 2 + 2];
            root = new GameObject("시계 · 타격 인쇄");
            root.transform.SetParent(game.transform, false);
            var sheet = Resources.Load<Texture2D>("Art/ClockStrikeFX");
            for (int i = 0; i < Frames; ++i)
            {
                frames[i] = Cut(sheet, i, 80, .15f, 65f);
                frames[Frames + i] = Cut(sheet, i, 0, .5f, 60f);
            }
            for (int i = 0; i < slots.Length; ++i)
            {
                var image = new GameObject("태엽 자국").AddComponent<SpriteRenderer>();
                image.transform.SetParent(root.transform, false);
                image.sortingOrder = 15;
                image.enabled = false;
                slots[i].image = image;
            }
        }

        static Sprite Cut(Texture2D sheet, int column, int bottom, float pivotY, float density)
        {
            return Sprite.Create(sheet, new Rect(column * 128, bottom, 128, 80),
                new Vector2(.5f, pivotY), density, 0, SpriteMeshType.FullRect);
        }

        internal void Release(Vector2 origin, bool ceiling)
        {
            if (ceiling && !AncientVisuals.HighQuality) return;
            Play(Mark.Crown, origin, 0f, ceiling ? .62f : 1f, Color.white);
        }

        internal void Land(Vector2 point, Vector2 normal, int hand)
        {
            float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;
            Color tint = hand == 0 ? Color.white : hand == 1
                ? new Color(.86f, .94f, 1f) : new Color(1f, .78f, .65f);
            Play(Mark.Ground, point, angle, 1f, tint);
        }

        void Play(Mark mark, Vector2 point, float angle, float scale, Color tint)
        {
            ref Slot item = ref slots[cursor];
            cursor = (cursor + 1) % slots.Length;
            item.mark = mark;
            item.age = 0;
            var image = item.image;
            bool subdued = game.ReducedFeedback || !AncientVisuals.HighQuality;
            tint.a = subdued ? .61f : .96f;
            image.color = tint;
            image.transform.position = point;
            image.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            image.transform.localScale = Vector3.one * (scale * (subdued ? .75f : 1f));
            image.sprite = frames[(int)mark * Frames];
            image.enabled = true;
        }

        internal void Tick(float dt)
        {
            for (int i = 0; i < slots.Length; ++i)
            {
                ref Slot item = ref slots[i];
                if (!item.image.enabled) continue;
                item.age += dt;
                float duration = item.mark == Mark.Ground ? GroundSeconds : CrownSeconds;
                if (item.age >= duration) item.image.enabled = false;
                else item.image.sprite = frames[(int)item.mark * Frames + (int)(item.age * Frames / duration)];
            }
        }

        internal void Clear()
        {
            for (int i = 0; i < slots.Length; ++i)
                if (slots[i].image) slots[i].image.enabled = false;
            cursor = 0;
        }

        internal void Dispose()
        {
            if (root)
            {
                root.SetActive(false);
                Object.Destroy(root);
            }
            foreach (var sprite in frames) if (sprite) Object.Destroy(sprite);
        }
    }
}
