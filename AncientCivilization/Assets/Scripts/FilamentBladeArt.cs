using UnityEngine;

namespace Plugin.PlayerCore
{
    // 원화와 발광 마스크는 같은 피벗·픽셀 격자를 쓴다. 투명한 아래쪽 한 점이 칼끝이다.
    public static class FilamentBladeArt
    {
        const int Width = 36, Height = 96;
        const float PixelsPerUnit = 40;
        static Texture2D sheet;
        static Sprite blade, emission;

        public static Sprite Blade => blade ? blade : (blade = Slice(0, "소환 블레이드"));
        public static Sprite Emission => emission ? emission : (emission = Slice(Width, "필라멘트 발광"));

        static Sprite Slice(int x, string label)
        {
            if (!sheet) sheet = Resources.Load<Texture2D>("Traversal/FilamentBladeAtlas");
            if (!sheet) return null;
            var sprite = Sprite.Create(sheet, new Rect(x, 0, Width, Height), new Vector2(.5f, 0),
                PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = label;
            return sprite;
        }
    }
}
