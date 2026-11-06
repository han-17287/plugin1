using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Original empty truck for the lower production chase only. The legacy Truck
    // sprite used by the final chase is intentionally separate. Source is 1962x801.
    public static class AncientProductionTruckAtlas
    {
        public const int GridSize=8;
        public const float PixelsPerUnit=243;
        public const float RootX=980,FootY=769;
        // Exact native-grid alpha bounds plus three transparent pixels on every side.
        public static readonly Rect Bounds=new Rect(141,110,1678,662);
        public static Vector2 Pivot
        {get{return new Vector2((RootX-Bounds.x)/Bounds.width,(Bounds.yMax-FootY)/Bounds.height);}}
        // Opaque wheels rest at world zero; opaque roof is 656/243 = 2.69959m,
        // aligned to the 2.7m walkable roof. Each bumper overhangs its collider .1403m.
    }
}
