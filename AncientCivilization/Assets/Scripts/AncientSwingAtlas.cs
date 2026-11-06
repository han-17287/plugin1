using UnityEngine;
namespace Plugin.AncientCivilization
{
    // Source pixels, top-left origin. Hip pivots preserve the body root through tucked poses.
    // The source PNG is unchanged; rectangles follow the runtime eight-pixel alpha grid.
    public static class AncientSwingAtlas
    {
        public const float PixelsPerUnit=220f;
        public static readonly Rect[] Bounds={
            new Rect(37,21,270,462),
            new Rect(405,29,302,430),
            new Rect(781,29,350,446),
            new Rect(1197,53,326,366),
            new Rect(29,557,382,422),
            new Rect(429,541,294,318),
            new Rect(797,557,374,414),
            new Rect(1205,573,278,382)
        };
        public static readonly Vector2[] PivotPixels={new Vector2(170f,220f),new Vector2(548f,235f),new Vector2(940f,264f),new Vector2(1349f,270f),new Vector2(202f,780f),new Vector2(569f,742f),new Vector2(953f,772f),new Vector2(1337f,784f)};
        public static readonly Vector2[] EmitterPixels={new Vector2(288f,32f),new Vector2(692f,36f),new Vector2(1118.666667f,38.666667f),new Vector2(1508f,60f),new Vector2(332f,568f),new Vector2(708f,552f),new Vector2(1094.666667f,574.666667f),new Vector2(1468f,684f)};
    }
}
