using UnityEngine;

namespace Plugin.AncientCivilization
{
    internal enum SecretaryGesture { Rest, ReadWatch, Dispatch, Fold, Brace, Sweep, RaiseWatch, ConductRain }

    internal static class AncientSecretaryGestures
    {
        const int Frames = 8;
        const int Width = 160, Height = 224;
        // 원화의 195 PPU를 40%로 축소했다. 모든 포즈의 뒷굽을 같은 지면에 둔다.
        const float PixelsPerUnit = 78f;
        static readonly Vector2 SolePivot = new Vector2(.5f, 8f / Height);
        static readonly Sprite[] cache = new Sprite[Frames * 8];

        internal static Sprite Sample(SecretaryGesture gesture, float progress)
        {
            int frame = Mathf.RoundToInt(Mathf.Clamp01(progress) * (Frames - 1));
            int row = (int)gesture;
            int index = row * Frames + frame;
            if (cache[index]) return cache[index];

            var rect = new Rect(frame * Width, row * Height, Width, Height);
            var sprite = AncientArt.Slice("SecretaryGestures", "secretary-watch-" + index,
                rect, PixelsPerUnit, SolePivot, false);
            sprite.texture.filterMode = FilterMode.Bilinear;
            return cache[index] = sprite;
        }
    }
}
