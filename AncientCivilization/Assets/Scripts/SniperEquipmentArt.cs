using UnityEngine;
namespace Plugin.AncientCivilization
{
    // Unchanged source atlas, measured after the runtime eight-pixel alpha sampling.
    public static class AncientSniperEquipmentArt
    {
        static readonly Sprite[] cache=new Sprite[6];
        static readonly Rect[] bounds={
            new Rect(21,165,470,286),
            new Rect(517,213,507,150),
            new Rect(1197,157,190,262),
            new Rect(21,709,454,150),
            new Rect(653,629,230,262),
            new Rect(1069,725,422,118)
        };
        static readonly float[] pixelsPerUnit={195.833333f,181.071429f,403.076923f,349.230769f,327.500000f,162.307692f};
        static readonly Vector2[] pivots={new Vector2(.5f,0),new Vector2(.2f,.5f),new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(.5f,0)};
        // 0 pedestal, 1 barrel, 2 grenade, 3 compressed plate, 4 energy core, 5 floor hatch.
        public static Sprite Sprite(int id)
        {
            id=Mathf.Clamp(id,0,5);if(cache[id])return cache[id];
            cache[id]=AncientArt.Slice("SniperEquipment","sniper-equipment-"+id,bounds[id],pixelsPerUnit[id],pivots[id],false);
            return cache[id];
        }
    }
}
