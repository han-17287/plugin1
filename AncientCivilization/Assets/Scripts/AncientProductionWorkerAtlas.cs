using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Original padded 2172x724 RGBA sheet; three 724px cells. The source bytes
    // remain intact, including faint guides that disappear below native alpha 110.
    // Top-left rectangles enclose bottom-up Grid(8) output with three clear pixels.
    public static class AncientProductionWorkerAtlas
    {
        public const int Count=3,GridSize=8;
        public const float PixelsPerUnit=240;
        public static readonly Rect[] Rectangles={
            new Rect(277,161,174,430),  // 0: shift worker with a mug.
            new Rect(1013,161,174,430), // 1: press technician with a meter.
            new Rect(1741,169,150,422)  // 2: stock clerk with a clipboard.
        };
        // Midpoint of the opaque boot span over the lowest 32 packed pixels.
        public static readonly float[] RootX={372f,1096f,1824f};
        public static readonly float[] FootY={588f,588f,588f};
        public static Rect Bounds(int worker){return Rectangles[Mathf.Clamp(worker,0,Count-1)];}
    }
}
