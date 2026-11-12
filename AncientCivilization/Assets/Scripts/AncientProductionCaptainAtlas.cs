using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Original six-pose captain sheet, 1536x1024. These top-left pixel rectangles
    // enclose the exact bottom-up native 8px grid with three transparent pixels.
    // Each pose's own lower margin keeps its boots at one world baseline even
    // though the original sheet's second row places the feet eight pixels lower.
    public static class AncientProductionCaptainAtlas
    {
        public const int Count=6,GridSize=8;
        public const float PixelsPerUnit=200;
        public static readonly Rect[] Rectangles={
            new Rect(149,29,198,446),   // 0: idle.
            new Rect(573,85,318,390),   // 1: melee windup.
            new Rect(1069,117,430,358), // 2: melee followthrough.
            new Rect(77,533,350,462),   // 3: plate windup.
            new Rect(565,605,422,390),  // 4: throw followthrough.
            new Rect(1149,637,254,358)  // 5: recovery.
        };
        // Midpoint of the two opaque planted boot spans over the lowest 32px.
        // Extended arms and the raised plate therefore cannot drag the actor pivot.
        public static readonly float[] RootX={256f,728f,1236f,260f,728f,1256f};
        public static readonly float[] FootY={472f,472f,472f,992f,992f,992f};
        public static Rect Bounds(int pose){return Rectangles[Mathf.Clamp(pose,0,Count-1)];}
    }
}
