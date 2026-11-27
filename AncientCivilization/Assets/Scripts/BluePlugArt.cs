using System;
using UnityEngine;

namespace Plugin.PlayerCore
{
    // 원화와 셰이더를 공유하므로 소켓을 풀에서 꺼낼 때 새 텍스처나 재질을 만들지 않는다.
    internal static class BluePlugArt
    {
        // 이동 신호의 원색 #5CCBFF. HDR 세기는 발광 재질에서 별도로 조절한다.
        internal static readonly Color MovementColor = new Color32(0x5C, 0xCB, 0xFF, 0xFF);
        internal const float PlugTailOffset = 55f / 52f;
        static readonly Vector2 PlugPivot = new Vector2(58.5f / 60f, 16.5f / 34f);
        static readonly Vector2 Center = Vector2.one * .5f;

        static Material solidMaterial, glowMaterial;
        static Texture2D atlas, finaleAtlas;
        static Sprite finalePlug, finaleEmitter, finaleLinked;
        static Sprite halo, socket, socketEmitter, socketSlots, openSlots, claspedSlots, plug, plugEmitter;
        static Sprite idle, aim, cross, linked, padlock, arrow, arc;

        internal static Material SolidMaterial => solidMaterial
            ? solidMaterial : solidMaterial = new Material(Shader.Find("Sprites/Default"));
        internal static Material GlowMaterial
        {
            get
            {
                if (!glowMaterial)
                {
                    glowMaterial = Resources.Load<Material>("Traversal/BluePlugEmission");
                    if (!glowMaterial || !glowMaterial.shader || !glowMaterial.shader.isSupported)
                        glowMaterial = SolidMaterial;
                }
                return glowMaterial;
            }
        }

        internal static Sprite Halo => halo ? halo : halo = Draw("플러그 후광", 32, 32, 32, (x, y) =>
        {
            float distance = new Vector2(x - 15.5f, y - 15.5f).magnitude / 15.5f;
            return new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - distance), 1.85f));
        }, Vector2.one * .5f, FilterMode.Bilinear);

        // 원화와 흰색 발광 마스크는 각각 같은 캔버스·피벗을 사용한다.
        internal static Sprite Plug => plug ? plug :
            plug = Slice("유리창 절연 플러그", 0, 0, 60, 34, 52, PlugPivot);
        internal static Sprite PlugEmitter => plugEmitter ? plugEmitter :
            plugEmitter = Slice("플러그 발광 필라멘트", 64, 0, 60, 34, 52, PlugPivot);
        // 마지막 포탑 구간에는 기존에 쓰던 원화와 마스크를 그대로 보여 준다.
        internal static Sprite FinalePlug => finalePlug ? finalePlug :
            finalePlug = Slice("결전 플러그", 0, 0, 60, 34, 52, PlugPivot, true);
        internal static Sprite FinaleEmitter => finaleEmitter ? finaleEmitter :
            finaleEmitter = Slice("결전 필라멘트", 64, 0, 60, 34, 52, PlugPivot, true);
        internal static Sprite Socket => socket ? socket :
            socket = Slice("매입형 금속 콘센트", 0, 38, 51, 51, 44, Center);
        internal static Sprite SocketEmitter => socketEmitter ? socketEmitter :
            socketEmitter = Slice("콘센트 표시등", 77, 73, 51, 51, 44, Center);
        internal static Sprite SocketSlots => socketSlots ? socketSlots :
            socketSlots = Slice("회전식 2핀 삽입구", 52, 38, 29, 29, 44, Center);
        // 대기 중인 단자, 열린 가이드, 잠긴 걸쇠의 윤곽은 조명 없이도 다르다.
        internal static Sprite OpenSlots => openSlots ? openSlots :
            openSlots = Slice("벌어진 네 모서리와 두 홈", 83, 38, 29, 29, 44, Center);
        internal static Sprite ClaspedSlots => claspedSlots ? claspedSlots :
            claspedSlots = Slice("맞물린 단자와 잠금쇠", 0, 94, 29, 29, 44, Center);

        const int ContactCels = 6;
        static readonly Sprite[] contacts = new Sprite[ContactCels];

        internal static void LoadContacts()
        {
            if (contacts[0]) return;
            var sheet = Resources.Load<Texture2D>("Traversal/BluePlugContact");
            if (!sheet || sheet.width != sheet.height * ContactCels) return;
            int side = sheet.height;
            for (int i = 0; i < ContactCels; i++)
                contacts[i] = Sprite.Create(sheet, new Rect(i * side, 0, side, side), Center,
                    side * .75f, 0, SpriteMeshType.FullRect);
        }

        internal static Sprite Contact(float progress) =>
            contacts[Mathf.Clamp(Mathf.FloorToInt(progress * ContactCels), 0, ContactCels - 1)];

        static Sprite Slice(string label, int x, int y, int width, int height, int pixelsPerUnit, Vector2 pivot, bool finale = false)
        {
            if (finale && !finaleAtlas) finaleAtlas = Resources.Load<Texture2D>("Traversal/BluePlugFinaleAtlas");
            if (!atlas) atlas = Resources.Load<Texture2D>("Traversal/BluePlugAtlas");
            // 보통 장비 원화는 두 배 밀도다. 소켓·핀의 월드 크기와 정규화 피벗은 결전 원본과 같다.
            int density = finale ? 1 : 2;
            var rect = new Rect(x * density, y * density, width * density, height * density);
            Sprite sprite = Sprite.Create(finale ? finaleAtlas : atlas, rect, pivot,
                pixelsPerUnit * density, 0, SpriteMeshType.FullRect);
            sprite.name = label;
            return sprite;
        }

        internal static Sprite Idle => idle ? idle : idle = Glyph("대기 표시", (x, y) =>
            Mathf.Abs(x - 13) + Mathf.Abs(y - 13) <= 4);
        internal static Sprite Aim => aim ? aim : aim = Glyph("조준 표시", (x, y) =>
        {
            float ax = Mathf.Abs(x - 13), ay = Mathf.Abs(y - 13);
            return ax <= 10 && ay <= 9 && (ax >= 8 && ay >= 3 || ay >= 7 && ax >= 4)
                || ax + ay <= 2;
        });
        internal static Sprite Cross => cross ? cross : cross = Glyph("가림 표시", (x, y) =>
            Stroke(x, y, 6, 6, 20, 20, 1.8f) || Stroke(x, y, 6, 20, 20, 6, 1.8f));
        internal static Sprite Linked => linked ? linked : linked = Glyph("체결 표시", LinkShape);
        internal static Sprite FinaleLinked => finaleLinked ? finaleLinked :
            finaleLinked = Draw("결전 체결 표시", 27, 27, 44,
                (x, y) => LinkShape(x, y) ? Color.white : Color.clear, Center);

        static bool LinkShape(float x, float y)
        {
            float radius = new Vector2(x - 13, y - 13).sqrMagnitude;
            return radius > 92 && radius <= 133 && Mathf.Abs(x - 13) >= 4
                || Stroke(x, y, 7, 13, 11, 9, 1.4f) || Stroke(x, y, 11, 9, 19, 17, 1.4f);
        }
        internal static Sprite Lock => padlock ? padlock : padlock = Glyph("잠금 표시", (x, y) =>
            x >= 5 && x <= 21 && y >= 3 && y <= 14 && !(x >= 12 && x <= 14 && y >= 6 && y <= 10)
            || x >= 8 && x <= 18 && y >= 14 && y <= 23 && (x <= 10 || x >= 16 || y >= 21));
        internal static Sprite Arrow => arrow ? arrow : arrow = Glyph("직선 견인 문양", (x, y) =>
            Stroke(x, y, 5, 5, 21, 21, 1.7f) || Stroke(x, y, 12, 21, 21, 21, 1.5f)
            || Stroke(x, y, 21, 21, 21, 12, 1.5f));
        internal static Sprite Arc => arc ? arc : arc = Glyph("진자 스윙 문양", (x, y) =>
        {
            float radius = new Vector2(x - 13, y - 18).sqrMagnitude;
            return y <= 15 && radius >= 86 && radius <= 142
                || Stroke(x, y, 23, 15, 18, 14, 1.5f) || Stroke(x, y, 23, 15, 24, 10, 1.4f)
                || Stroke(x, y, 13, 20, 7, 11, 1f) || Mathf.Abs(x - 13) + Mathf.Abs(y - 20) <= 2;
        });

        static bool Stroke(float x, float y, float ax, float ay, float bx, float by, float width)
        {
            var offset = new Vector2(x - ax, y - ay);
            var segment = new Vector2(bx - ax, by - ay);
            float t = Mathf.Clamp01(Vector2.Dot(offset, segment) / segment.sqrMagnitude);
            return (offset - segment * t).sqrMagnitude <= width * width;
        }

        static Sprite Glyph(string label, Func<float, float, bool> inside)
        {
            // 안내 문양도 선을 다시 계산해 두 배 해상도로 굽는다. 각 출력 픽셀에서 네 지점의 점유율을 구한다.
            return Draw(label, 54, 54, 88, (x, y) =>
            {
                float u = x * .5f - .375f, v = y * .5f - .375f;
                int hits = (inside(u, v) ? 1 : 0) + (inside(u + .25f, v) ? 1 : 0)
                    + (inside(u, v + .25f) ? 1 : 0) + (inside(u + .25f, v + .25f) ? 1 : 0);
                return new Color(1f, 1f, 1f, hits * .25f);
            }, Center, FilterMode.Bilinear);
        }

        static Sprite Draw(string label, int width, int height, float pixelsPerUnit, Func<int, int, Color> shade,
            Vector2 pivot, FilterMode filter = FilterMode.Point)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = label;
            texture.filterMode = filter;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = shade(x, y);
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, width, height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        internal static SpriteRenderer AddSprite(Transform parent, string label, Sprite sprite, Vector2 local,
            int order, float scale = 1f)
        {
            var child = new GameObject(label);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = local;
            child.transform.localScale = Vector3.one * scale;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
