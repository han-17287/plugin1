using UnityEngine;
namespace Plugin.AncientCivilization
{
    public sealed class AncientProjectilePool : MonoBehaviour
    {
        readonly AncientProjectile[] slots=new AncientProjectile[64];int cursor;
        public AncientGame Game {get;private set;}
        public int DroppedSpawns {get;private set;}
        public int Capacity {get{return slots.Length;}}
        public void Initialize(AncientGame owner)
        {
            Game=owner;
            // Force sprite decoding during loading, never on the first incoming attack.
            AncientArt.Scrap();for(int i=0;i<3;i++)AncientArt.MetalProjectile(i);
            // Enemy slices scan their atlas alpha once. Warm all six here so a
            // newly introduced guard does not decode the sheet midway through a run.
            for(int kind=0;kind<6;kind++)AncientArt.Enemy(kind);
            for(int floor=0;floor<6;floor++)for(int pose=0;pose<6;pose++)AncientArt.Boss(floor,pose);
            for(int i=0;i<slots.Length;i++)
            {
                var o=new GameObject("Pooled thrown object "+i);o.transform.SetParent(transform,false);
                slots[i]=o.AddComponent<AncientProjectile>();slots[i].Initialize(this);o.SetActive(false);
            }
        }
        public AncientProjectile Spawn(Vector2 position,Vector2 velocity,float damage,bool scrap=false,int kind=0,float gravity=0,int ricochets=0)
        {
            for(int i=0;i<slots.Length;i++)
            {
                int at=(cursor+i)%slots.Length;
                if(!slots[at].Available)continue;
                cursor=(at+1)%slots.Length;slots[at].Play(position,velocity,damage,scrap,kind,gravity,ricochets);return slots[at];
            }
            // Gameplay projectiles are never stolen/repositioned while alive.
            DroppedSpawns++;return null;
        }
    }
}
