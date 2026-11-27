using Plugin.PlayerCore;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 기존 도트 시트에서 몸통을 보존하고 두 팔의 실루엣만 분리한다. 좌표의 원점은 시트 왼쪽 위다.
    static class BluePlugMotionArt
    {
        const int Grid = 8;
        const float PixelsPerUnit = AncientSwingAtlas.PixelsPerUnit / Grid;
        static readonly Sprite[] bodies = new Sprite[8];
        static readonly Sprite[] baseline = new Sprite[8];
        // 보행 원화의 발, 코트와 머리를 살린 전용 하체 시트다. 다섯 칸의 발바닥이 같은 선에 있다.
        static readonly Sprite[] grounded = new Sprite[5];
        static readonly Sprite[] travel = new Sprite[4];
        static readonly Vector2[] travelLeft = { new Vector2(3,16), new Vector2(5,17),
            new Vector2(2,30), new Vector2(2,39) };
        static readonly Vector2[] travelRight = { new Vector2(-4,16.5f), new Vector2(-2,17.5f),
            new Vector2(-5,30.5f), new Vector2(-5,39.5f) };
        static readonly Vector2[] groundLeft = {
            new Vector2(-1, 46), new Vector2(-.5f, 42), new Vector2(-5, 43),
            new Vector2(-3, 41.5f), new Vector2(3.5f, 43)
        };
        static readonly Vector2[] groundRight = {
            new Vector2(-8, 46.5f), new Vector2(-4.5f, 42.5f), new Vector2(-10, 43.5f),
            new Vector2(-6.5f, 42), new Vector2(.5f, 43.5f)
        };
        static readonly Vector2[] soleEdges = {
            new Vector2(-11, 5), new Vector2(-10, 0), new Vector2(-27, -22),
            new Vector2(-2, 2), new Vector2(10, 11)
        };
        static readonly Vector2[] leftShoulders = {
            new Vector2(128, 142), new Vector2(531, 160), new Vector2(981, 179), new Vector2(1350, 206),
            new Vector2(158, 700), new Vector2(525, 670), new Vector2(919, 674), new Vector2(1301, 657)
        };
        static readonly Vector2[] rightShoulders = {
            new Vector2(195, 109), new Vector2(598, 138), new Vector2(1053, 149), new Vector2(1420, 185),
            new Vector2(211, 677), new Vector2(587, 645), new Vector2(976, 649), new Vector2(1369, 665)
        };
        static readonly Vector2[] ordinaryLeft = {
            new Vector2(244, 253), new Vector2(637, 184), new Vector2(984, 174), new Vector2(1219, 213),
            new Vector2(118, 733), new Vector2(497, 770), new Vector2(801, 775), new Vector2(1309, 746)
        };
        static readonly Vector2[] ordinaryRight = {
            new Vector2(114, 243), new Vector2(475, 232), new Vector2(930, 239), new Vector2(1365, 245),
            new Vector2(274, 753), new Vector2(629, 584), new Vector2(969, 634), new Vector2(1264, 735)
        };
        static readonly Vector2[][] leftCuts = {
            Polygon(108,138, 135,141, 143,154, 94,246, 56,246, 63,191, 102,161),
            Polygon(513,146, 543,155, 552,162, 489,222, 440,229, 448,189, 502,154),
            Polygon(956,169, 980,183, 998,187, 1005,196, 926,235, 842,238, 842,195, 935,173),
            Polygon(1344,188, 1375,202, 1381,213, 1338,223, 1283,259, 1243,252, 1259,214, 1310,195),
            Polygon(139,691, 176,701, 181,716, 127,753, 131,794, 68,794, 60,756, 92,713),
            Polygon(511,654, 540,659, 552,672, 519,695, 517,725, 490,733, 451,724, 449,704, 460,678),
            Polygon(907,659, 939,671, 945,678, 928,708, 925,736, 910,745, 875,748, 854,732, 856,708, 891,673),
            Polygon(1280,651, 1324,655, 1325,665, 1281,690, 1277,724, 1244,737, 1218,730, 1219,706, 1253,662)
        };
        static readonly Vector2[][] rightCuts = {
            Polygon(188,95, 280,17, 309,18, 314,47, 234,135, 198,154, 189,145),
            Polygon(590,121, 616,83, 666,25, 704,15, 717,47, 642,119, 599,181, 593,164),
            Polygon(1030,131, 1040,101, 1052,84, 1080,43, 1107,14, 1145,16, 1145,59, 1100,107, 1053,160, 1040,179),
            Polygon(1404,166, 1462,74, 1495,37, 1531,39, 1535,70, 1481,122, 1422,208, 1414,216),
            Polygon(199,662, 275,563, 301,543, 344,543, 345,583, 301,624, 251,690, 214,723, 202,711),
            Polygon(578,628, 672,539, 729,538, 731,563, 661,624, 587,684, 581,681),
            Polygon(970,643, 1085,562, 1118,555, 1128,580, 1060,626, 980,673, 974,685),
            Polygon(1363,651, 1404,662, 1479,657, 1489,680, 1458,709, 1393,705, 1359,680)
        };

        public static Sprite ClothUpper { get; private set; }
        public static Sprite ClothForearm { get; private set; }
        public static Sprite MetalUpper { get; private set; }
        public static Sprite MetalForearm { get; private set; }
        public static Sprite LeftFist { get; private set; }
        public static Sprite RightFist { get; private set; }
        public static Sprite RightOpen { get; private set; }
        public static Sprite Reel { get; private set; }
        public static Sprite WallBody { get; private set; }
        // 벽 자세 시트의 피벗은 (32, 32), 두 부츠의 오른쪽 밑창 끝은 x=47이다.
        public static float WallSoleEdge => 15f / PixelsPerUnit;
        public static Vector2 WallShoulder(bool left) => (left ? new Vector2(1, 14) : new Vector2(-6, 14.5f)) / PixelsPerUnit;

        public static void Prepare()
        {
            if (Reel) return;
            ClothUpper = Tile("왼팔 옷소매", true, ".kkkkkkk.", "kdddsssdk", "kdsdddddk", ".kkkkkkk.");
            ClothForearm = Tile("왼팔 장갑 소매", true, ".kkkkkkk.", "kdsssdhkk", "kdddddhkk", ".kkkkkkk.");
            MetalUpper = Tile("오른팔 상박", true, ".kkkkkkk.", "kswwwwhkk", "kdhhhhdk.", ".kkkkkk..");
            MetalForearm = Tile("오른팔 청색 패널", true, ".kkkkkkkkk.", "kswwwwwwhkk", "kdabbbbbdkk", ".kkkkkkkkk.");
            LeftFist = Tile("왼손 감개 그립", false, ".kkkk.", "kdssdk", "kddsdk", "kssddk", ".kkkk.");
            RightFist = Tile("오른손 전선 그립", false, ".kkkk.", "khwwhk", "ksahhk", "kddhkk", ".kkkk.");
            RightOpen = Tile("펼친 오른손", false, "....kkkk.", "...khwwk.", ".kkhhkk..", "khwwwahkk", "kshwhkkkk", ".kkdhhkk.", "..kkhhwk.", "...kkkkk.");
            Reel = Tile("왼손 전선 감개", false, "...kkkkk...", "..khswshk..", ".khbababwk.", "ksbabbabask", "kdbbaabbadk", "kdbbwabbadk", "kdbbaabbadk", "ksbabbabask", ".khbababwk.", "..khsdshk..", "...kkkkk...");
            var groundSheet = Resources.Load<Texture2D>("Traversal/BluePlugGroundBody");
            var travelSheet = Resources.Load<Texture2D>("Traversal/BluePlugTravelBody");
            for (int i = 0; i < travel.Length; i++)
            {
                // 견인 두 셀은 골반, 착지 두 셀은 같은 발바닥 선을 중심으로 한다.
                travel[i] = Sprite.Create(travelSheet, new Rect(i * 64, 0, 64, 64),
                    new Vector2(.5f, i < 2 ? .5f : 2f/64), PixelsPerUnit, 0, SpriteMeshType.FullRect);
                travel[i].name = i < 2 ? "견인 " + i : "충격 흡수 " + (i - 2);
            }
            var wallSheet = Resources.Load<Texture2D>("Traversal/BluePlugWallBody");
            WallBody = Sprite.Create(wallSheet, new Rect(0, 0, 64, 64), Vector2.one * .5f,
                PixelsPerUnit, 0, SpriteMeshType.FullRect);
            WallBody.name = "두 무릎을 접고 벽 손잡이에 기대기";
            for (int i = 0; i < grounded.Length; i++)
            {
                // 캔버스의 아래에서 두 픽셀이 발 피벗이다.
                grounded[i] = Sprite.Create(groundSheet, new Rect(i * 64, 0, 64, 64),
                    new Vector2(.5f, 2f / 64), PixelsPerUnit, 0, SpriteMeshType.FullRect);
                grounded[i].name = "플러그 보행 " + i;
            }
            Texture2D source = AncientArt.Texture("HeroSwing");
            Color32[] pixels = source.GetPixels32();
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i] = MakeBody(i, source.width, source.height, pixels);
                baseline[i] = AncientArt.Hero(i);
            }
        }

        public static Sprite Body(int frame) => bodies[frame];

        public static Sprite GroundBody(int frame) => grounded[frame];
        public static Sprite TravelBody(int frame) => travel[frame];
        public static Vector2 TravelShoulder(int frame, bool left) =>
            (left ? travelLeft[frame] : travelRight[frame]) / PixelsPerUnit;

        public static float GroundSole(int frame, bool leftEdge) =>
            (leftEdge ? soleEdges[frame].x : soleEdges[frame].y) / PixelsPerUnit;

        public static Vector2 Shoulder(int frame, bool left, bool footPivot = false)
        {
            if (footPivot) return (left ? groundLeft[frame] : groundRight[frame]) / PixelsPerUnit;
            Vector2 offset = (left ? leftShoulders[frame] : rightShoulders[frame]) - AncientSwingAtlas.PivotPixels[frame];
            return new Vector2(offset.x, -offset.y) / AncientSwingAtlas.PixelsPerUnit;
        }

        public static Vector2 OrdinaryGrip(Sprite sprite, bool left)
        {
            if (!sprite) return Vector2.zero;
            if (BluePlugOfficerArt.TryFieldGrip(sprite, left, out Vector2 grip)) return grip;
            for (int i = 0; i < baseline.Length; i++)
            {
                if (sprite != baseline[i] && !BluePlugOfficerArt.IsFrame(sprite, i)) continue;
                Vector2 at = left ? ordinaryLeft[i] : ordinaryRight[i];
                var crop = sprite.rect;
                var point = new Vector2(at.x - crop.xMin, sprite.texture.height - at.y - crop.yMin);
                return (point - sprite.pivot) / sprite.pixelsPerUnit;
            }
            // 무기 전용 프레임에서도 몸 중심 가까이에 감개를 유지한다.
            var bounds = sprite.bounds;
            return new Vector2(left ? .03f : .32f, bounds.min.y + bounds.size.y * .48f);
        }

        static Sprite MakeBody(int frame, int sheetWidth, int sheetHeight, Color32[] sheet)
        {
            Rect rect = AncientSwingAtlas.Bounds[frame];
            int xStart = Mathf.FloorToInt(rect.xMin / Grid) * Grid;
            int yStart = Mathf.FloorToInt(rect.yMin / Grid) * Grid;
            int width = Mathf.CeilToInt((rect.xMax - xStart) / Grid);
            int height = Mathf.CeilToInt((rect.yMax - yStart) / Grid);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int sourceX = xStart + x * Grid + Grid / 2;
                int sourceY = yStart + y * Grid + Grid / 2;
                if (sourceX < rect.xMin || sourceX >= rect.xMax || sourceY < rect.yMin || sourceY >= rect.yMax) continue;
                if (Inside(leftCuts[frame], sourceX, sourceY) || Inside(rightCuts[frame], sourceX, sourceY)) continue;
                pixels[(height - y - 1) * width + x] = sheet[(sheetHeight - 1 - sourceY) * sheetWidth + sourceX];
            }
            var hip = AncientSwingAtlas.PivotPixels[frame];
            var pivot = new Vector2((hip.x - xStart) / (width * Grid), 1 - (hip.y - yStart) / (height * Grid));
            KeepBody(pixels, width, height, pivot);
            return MakeSprite("플러그 전용 몸통 " + frame, width, height, pivot, pixels);
        }

        // 팔을 가린 뒤 남은 잘린 픽셀은 몸통 연결 영역 밖에서 제거한다.
        static void KeepBody(Color32[] pixels, int width, int height, Vector2 pivot)
        {
            int x0 = Mathf.FloorToInt(pivot.x * width), y0 = Mathf.FloorToInt(pivot.y * height);
            int seed = -1, closest = int.MaxValue;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a == 0) continue;
                int dx = i % width - x0, dy = i / width - y0;
                int distance = dx * dx + dy * dy;
                if (distance >= closest) continue;
                closest = distance;
                seed = i;
            }
            if (seed < 0) return;

            var seen = new bool[pixels.Length];
            var queue = new int[pixels.Length];
            int tail = 0;
            queue[tail++] = seed;
            seen[seed] = true;
            for (int head = 0; head < tail; head++)
            {
                int current = queue[head];
                int x = current % width, y = current / width;
                for (int py = Mathf.Max(0, y - 1); py <= Mathf.Min(height - 1, y + 1); py++)
                for (int px = Mathf.Max(0, x - 1); px <= Mathf.Min(width - 1, x + 1); px++)
                {
                    int neighbor = py * width + px;
                    if (seen[neighbor] || pixels[neighbor].a == 0) continue;
                    seen[neighbor] = true;
                    queue[tail++] = neighbor;
                }
            }
            for (int i = 0; i < pixels.Length; i++)
                if (!seen[i]) pixels[i] = default;
        }

        static Vector2[] Polygon(params int[] coordinates)
        {
            var result = new Vector2[coordinates.Length / 2];
            for (int i = 0; i < result.Length; i++) result[i] = new Vector2(coordinates[i * 2], coordinates[i * 2 + 1]);
            return result;
        }

        static bool Inside(Vector2[] polygon, int x, int y)
        {
            bool inside = false;
            for (int i = 0, last = polygon.Length - 1; i < polygon.Length; last = i++)
            {
                Vector2 a = polygon[i], b = polygon[last];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        static Sprite Tile(string name, bool segment, params string[] rows)
        {
            int width = rows[0].Length, height = rows.Length;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++) pixels[(height - y - 1) * width + x] = Palette(rows[y][x]);
            return MakeSprite(name, width, height, new Vector2(segment ? 1f / width : .5f, .5f), pixels);
        }

        static Color32 Palette(char c)
        {
            switch (c)
            {
                case 'k': return new Color32(7, 12, 21, 255);
                case 'd': return new Color32(37, 50, 65, 255);
                case 's': return new Color32(66, 82, 100, 255);
                case 'h': return new Color32(106, 120, 132, 255);
                case 'w': return new Color32(231, 239, 246, 255);
                case 'a': return new Color32(39, 154, 232, 255);
                case 'b': return new Color32(32, 81, 160, 255);
                default: return new Color32(0, 0, 0, 0);
            }
        }

        static Sprite MakeSprite(string name, int width, int height, Vector2 pivot, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }
    }
}
