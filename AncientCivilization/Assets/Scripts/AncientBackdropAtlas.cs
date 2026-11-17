using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Five sealed city interiors measured on the unchanged 1536x1024 opaque atlas.
    // Top-left source pixels; the 3x2 atlas gutters and unused sixth panel are excluded.
    public static class AncientBackdropAtlas
    {
        public const int Count=5;
        public static readonly Rect[] Rectangles={
            new Rect(2,106,508,286),   // Lower production: rust, pipes and processing tanks.
            new Rect(514,106,508,286), // Working-class residences and a small clinic.
            new Rect(1026,106,508,286),// Administrative archives and server hall.
            new Rect(2,618,508,286),   // Enclosed ivory-and-gold residential garden.
            new Rect(514,618,508,286)  // Central control grand hall.
        };
        public static Rect Bounds(int floor){return Rectangles[Mathf.Clamp(floor,0,Count-1)];}
    }
}
