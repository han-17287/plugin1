using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.PlayerCore
{
    // 평상복과 플러그 자세가 이어질 때 원래 선화, 프레임 크기, 발바닥 피벗을 맞춘다.
    internal static class BluePlugOfficerArt
    {
        static readonly Sprite[] frames = new Sprite[8];
        static readonly Texture2D[] sheets = new Texture2D[2];

        const int Cell = 256, Columns = 8, Rows = 7;
        const float Density = 110;
        static readonly Sprite[] fieldFrames = new Sprite[Columns * Rows];
        static Texture2D gaitSheet;
        static byte[] gripPixels;
        static bool gaitLoaded;

        // 의상·두 손·부츠를 한 번 구운 셀이다. 매 프레임 텍스처 합성이나 GPU 판독을 하지 않는다.
        internal static Sprite FieldFrame(int index, int fallback)
        {
            if (!gaitLoaded) PrepareField();
            return gaitSheet ? fieldFrames[index] : Frame(fallback);
        }

        internal static void PrepareField()
        {
            if (gaitLoaded) return;
            gaitLoaded = true;
            var texture = Resources.Load<Texture2D>("Traversal/OfficerFieldGait");
            var sockets = Resources.Load<TextAsset>("Traversal/OfficerFieldSockets");
            var pixels = sockets ? sockets.bytes : null;
            if (!texture || texture.width != Columns * Cell || texture.height != Rows * Cell ||
                pixels == null || pixels.Length != fieldFrames.Length * 4) return;

            gaitSheet = texture;
            gripPixels = pixels;
            var feet = new Vector2(.5f, 8f / Cell);
            for (int index = 0; index < fieldFrames.Length; index++)
            {
                var rect = new Rect(index % Columns * Cell, (Rows - 1 - index / Columns) * Cell, Cell, Cell);
                var sprite = Sprite.Create(texture, rect, feet, Density, 0, SpriteMeshType.FullRect);
                sprite.name = "순찰 자세 " + index;
                fieldFrames[index] = sprite;
            }
        }

        internal static bool TryFieldGrip(Sprite sprite, bool left, out Vector2 grip)
        {
            grip = Vector2.zero;
            if (!gaitSheet || sprite.texture != gaitSheet) return false;
            var rect = sprite.rect;
            int index = (Rows - 1 - (int)rect.y / Cell) * Columns + (int)rect.x / Cell;
            int at = index * 4 + (left ? 0 : 2);
            grip = new Vector2(gripPixels[at] - Cell / 2, Cell - 8 - gripPixels[at+1]) / Density;
            return true;
        }

        internal static Sprite Frame(int frame)
        {
            if (frames[frame]) return frames[frame];
            Sprite original = AncientArt.Hero(frame);
            int sheetIndex = frame == 5 || frame == 6 ? 1 : 0;
            Texture2D source = sheets[sheetIndex];
            if (!source)
            {
                string sheet = sheetIndex == 0 ? "Hero" : "HeroEmitterBody";
                source = Resources.Load<Texture2D>("Art/" + sheet);
                if (!source) return original;
                source.filterMode = FilterMode.Bilinear;
                sheets[sheetIndex] = source;
            }
            Rect rect = original.rect;
            // 8픽셀 격자로 잘랐던 오른쪽 끝의 원화 윤곽까지 포함한다. 왼쪽·아래 기준점은 그대로다.
            if (frame == 2 || frame == 3 || frame == 4) rect.width += 5;
            Vector2 pivot = new Vector2(original.pivot.x / rect.width, original.pivot.y / rect.height);
            Sprite sprite = Sprite.Create(source, rect, pivot, original.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = original.name;
            return frames[frame] = sprite;
        }

        internal static bool IsFrame(Sprite sprite, int frame) => sprite && sprite == frames[frame];
    }
}
