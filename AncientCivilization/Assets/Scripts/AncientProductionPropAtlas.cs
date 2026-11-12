using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Original factory machinery sheet, 1774x887 RGBA; source PNG remains unchanged.
    // Rectangles use top-left source coordinates and include three transparent pixels
    // around the occupied output of AncientPixels.Grid, which samples from the bottom.
    // The 887px source height means its topmost grid row is only three pixels high.
    public static class AncientProductionPropAtlas
    {
        public const int Count=8,GridSize=4;
        public const float PixelsPerUnit=64;
        public static readonly Rect[] Rectangles={
            new Rect(21,76,410,342),   // 0: weighing station.
            new Rect(465,64,414,354),  // 1: pressure tanks.
            new Rect(909,108,398,310), // 2: shift lockers.
            new Rect(1365,60,362,358), // 3: relay terminal.
            new Rect(21,488,402,334),  // 4: overhead hoist.
            new Rect(461,600,414,226), // 5: freight crates and hand pallet jack.
            new Rect(909,520,402,294), // 6: radiator and pipework.
            new Rect(1365,508,374,318) // 7: shielded energy core vault.
        };
        public static Rect Bounds(int prop){return Rectangles[Mathf.Clamp(prop,0,Count-1)];}
    }
}
