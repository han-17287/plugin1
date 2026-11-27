using UnityEngine;

namespace Plugin.AncientCivilization
{
    public static class AncientJokerDuelArt
    {
        // 원래 결투의 골반·발 좌표에 검은 전술복과 단검 포즈를 맞췄다. 각 칸은 투명 여백만 자른다.
        static readonly Rect[] cuts = {
            new Rect(40, 65, 140, 220), new Rect(9, 77, 199, 208),
            new Rect(38, 74, 131, 211), new Rect(16, 67, 185, 218)
        };
        static readonly Sprite[] frames = new Sprite[4];
        const int Width = 288, Height = 288;

        public static Sprite Frame(int pose)
        {
            pose = Mathf.Clamp(pose, 0, frames.Length - 1);
            if (frames[pose]) return frames[pose];
            Rect crop = cuts[pose];
            Vector2 pivot = new Vector2((103.5f - crop.x) / crop.width, (crop.yMax - 281.25f) / crop.height);
            crop.x += pose * Width;
            crop.y += 5 * Height;
            var sprite = AncientArt.Slice("JokerRifleMotion", "joker-club-duel-" + pose, crop, 82.5f, pivot, false);
            sprite.texture.filterMode = FilterMode.Bilinear;
            return frames[pose] = sprite;
        }
    }
}
