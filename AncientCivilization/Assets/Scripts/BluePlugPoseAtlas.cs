using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 원화에서 분리한 몸과 장비를 불러온다. 스프라이트의 픽셀 밀도만 올리고 관절의 월드 좌표는 유지한다.
    static class BluePlugPoseAtlas
    {
        public enum Part { ClothUpper, ClothForearm, MetalUpper, MetalForearm, LeftFist, RightFist, RightOpen, Reel }
        const int Cell = 256;
        const float Density = 110;
        static readonly Sprite[] frames = new Sprite[18];
        static readonly Sprite[] parts = new Sprite[8];
        static readonly Sprite[] towGhosts = new Sprite[2];
        static readonly Vector2 feet = new Vector2(.5f, 8f / Cell);
        static readonly Vector2 swingHip = new Vector2(.5f, 136f / Cell);
        static Texture2D sheet;
        static bool loaded;
        public static bool Available => sheet;

        public static void Prepare()
        {
            if (loaded) return;
            loaded = true;
            sheet = Resources.Load<Texture2D>("Traversal/BluePlugOfficer");
            if (!sheet) return;
            for (int i = 0; i < frames.Length; i++)
            {
                Vector2 pivot = i < 5 || i == 7 || i == 8 ? feet : i == 9 ? Vector2.one * .5f : swingHip;
                string label = i < 5 ? "플러그 보행 " + i : i < 7 ? "견인 " + (i - 5) :
                    i < 9 ? "충격 흡수 " + (i - 7) : i == 9 ? "두 무릎을 접고 벽 손잡이에 기대기" : "플러그 전용 몸통 " + (i - 10);
                frames[i] = Slice((i % 4) * Cell, (i / 4) * Cell, Cell, Cell, pivot, label);
            }
            int[] widths = {36,36,36,44,24,24,36,44}, heights = {16,16,16,16,20,20,32,44};
            for (int i = 0; i < parts.Length; i++)
            {
                Vector2 pivot = new Vector2(i < 4 ? 4f / widths[i] : .5f, .5f);
                parts[i] = Slice(512 + (i % 4) * 64, 1024 + (i / 4) * 64, widths[i], heights[i], pivot, ((Part)i).ToString());
            }
            for (int i = 0; i < towGhosts.Length; i++)
                towGhosts[i] = Slice(i * Cell, 5 * Cell, Cell, Cell, swingHip, "견인 전신 잔상 " + i);
        }

        static Sprite Slice(int x, int top, int width, int height, Vector2 pivot, string label)
        {
            var rect = new Rect(x, sheet.height - top - height, width, height);
            var result = Sprite.Create(sheet, rect, pivot, Density, 0, SpriteMeshType.FullRect);
            result.name = label;
            return result;
        }
        public static Sprite Ground(int frame) => frames[frame];
        public static Sprite Travel(int frame) => frames[5 + frame];
        public static Sprite Wall => frames[9];
        public static Sprite Air(int frame) => frames[10 + frame];
        public static Sprite Equipment(Part part) => parts[(int)part];
        // 두 견인 키포즈에 팔·장갑·릴을 미리 합성했다. 현재 본체와 피벗 및 월드 크기가 같다.
        public static Sprite FullTow(Sprite body) => !sheet || !body ? null :
            body == Travel(0) ? towGhosts[0] : body == Travel(1) ? towGhosts[1] : null;
    }
}
