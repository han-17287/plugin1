using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 대기↔열람↔시전↔후딜↔대기의 각 전환에 실제 팔·손 중간 자세 두 장을 등록한다.
    internal static class AncientEnforcerBridgeAtlas
    {
        internal static readonly Rect[] Cells = {
            new Rect(4, 4, 254, 499), new Rect(262, 4, 252, 499),
            new Rect(518, 4, 259, 499), new Rect(781, 4, 363, 499),
            new Rect(4, 507, 401, 499), new Rect(409, 507, 330, 499),
            new Rect(743, 507, 299, 499), new Rect(1046, 507, 277, 499)
        };
        internal static readonly Vector2[] Feet = {
            new Vector2(119, 498), new Vector2(377, 498),
            new Vector2(671, 498), new Vector2(996, 498),
            new Vector2(219, 1001), new Vector2(562, 1001),
            new Vector2(858, 1001), new Vector2(1161, 1001)
        };
        internal static readonly Rect[] OpticCells = {
            new Rect(4, 4, 45, 162), new Rect(53, 4, 17, 129),
            new Rect(74, 4, 27, 130), new Rect(105, 4, 116, 101),
            new Rect(225, 4, 126, 68), new Rect(355, 4, 119, 75),
            new Rect(4, 170, 99, 115), new Rect(107, 170, 72, 188)
        };
        internal static readonly Vector2[] OpticCenters = {
            new Vector2(108.50f, 119), new Vector2(380.50f, 102.50f),
            new Vector2(670.50f, 103), new Vector2(1055, 88.50f),
            new Vector2(283, 575), new Vector2(621.50f, 578.50f),
            new Vector2(906.50f, 598.50f), new Vector2(1137, 635)
        };
        internal static readonly Vector2[] Lenses = {
            new Vector2(125, 42), new Vector2(383, 42),
            new Vector2(678, 42), new Vector2(1004, 42),
            new Vector2(227.33f, 545), new Vector2(569.67f, 545),
            new Vector2(864.67f, 545), new Vector2(1167.33f, 545)
        };
        internal static readonly Vector2[] Dossiers = {
            new Vector2(223.54f, 139.36f), new Vector2(479.02f, 96.86f),
            new Vector2(693.23f, 140.91f), new Vector2(940.97f, 184.16f),
            new Vector2(188.05f, 685.99f), new Vector2(632.40f, 641.57f),
            new Vector2(1008.19f, 597.52f), new Vector2(1289.62f, 641.19f)
        };
        internal static readonly Vector4[] DossierAxes = {
            new Vector4(0.92f, -0.14f, -0.13f, 0.83f), new Vector4(0.95f, -0.12f, -0.13f, 0.86f),
            new Vector4(0.95f, -0.12f, -0.13f, 0.88f), new Vector4(0.93f, -0.14f, -0.15f, 0.85f),
            new Vector4(0.91f, -0.15f, -0.16f, 0.81f), new Vector4(0.90f, -0.14f, -0.15f, 0.80f),
            new Vector4(0.90f, -0.14f, -0.15f, 0.78f), new Vector4(0.90f, -0.15f, -0.14f, 0.79f)
        };
    }
}
