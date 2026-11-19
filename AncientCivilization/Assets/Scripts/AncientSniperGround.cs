using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Owns a temporary, physically breakable version of the existing arena floor.
    // Originals are restored when this encounter helper is destroyed.
    public sealed class AncientSniperGround : MonoBehaviour
    {
        const float TileWidth=1.5f,SlabDepth=.55f,RepairSeconds=6;
        struct Tile
        {
            public float left,right,repairAt;
            public BoxCollider2D collider;
            public SpriteRenderer surface,hole,edge;
            public bool broken;
        }
        AncientGame game;GameObject holder;Tile[] tiles;
        readonly List<BoxCollider2D> originals=new List<BoxCollider2D>(4);
        float floorLeft;bool ready;
        public void Initialize(AncientGame owner,float left,float right)
        {
            if(ready)return;
            game=owner;
            if(!game||right<=left)return;
            Physics2D.SyncTransforms();
            var found=Physics2D.OverlapBoxAll(new Vector2((left+right)*.5f,-.12f),new Vector2(right-left+.08f,.35f),0,1<<8);
            float coverLeft=left,coverRight=right;
            for(int i=0;i<found.Length;i++)
            {
                var floor=found[i] as BoxCollider2D;
                if(!floor||!floor.enabled||floor.isTrigger)continue;
                Bounds bounds=floor.bounds;
                if(Mathf.Abs(bounds.max.y)>.045f||bounds.size.y<.5f||bounds.max.x<=left||bounds.min.x>=right)continue;
                // Ceiling, walls and raised ledges cannot satisfy this top/height filter.
                originals.Add(floor);coverLeft=Mathf.Min(coverLeft,bounds.min.x);coverRight=Mathf.Max(coverRight,bounds.max.x);
            }
            if(originals.Count==0)
            {
                Debug.LogWarning("Sniper ground: arena floor was not found; terrain replacement was not enabled.");
                return;
            }
            floorLeft=coverLeft;
            holder=new GameObject("Temporary sniper arena ground");holder.transform.SetParent(game.transform,false);
            holder.transform.position=Vector3.zero;
            int count=Mathf.CeilToInt((coverRight-coverLeft)/TileWidth);tiles=new Tile[count];
            for(int i=0;i<count;i++)
            {
                float start=coverLeft+i*TileWidth,end=Mathf.Min(start+TileWidth,coverRight),width=end-start,centre=(start+end)*.5f;
                var tile=new Tile{left=start,right=end};
                tile.surface=AncientArt.Display("Fracturable arena slab",AncientArt.Rooftop(width,true),new Vector2(centre,0),2,holder.transform);
                var solid=new GameObject("Fracturable ground collider");solid.layer=8;solid.transform.SetParent(holder.transform,false);
                solid.transform.position=new Vector2(centre,-SlabDepth*.5f);
                tile.collider=solid.AddComponent<BoxCollider2D>();tile.collider.size=new Vector2(width,SlabDepth);
                var hole=game.Box("Recessed broken ground",new Vector2(centre,-1.98f),new Vector2(width+.015f,4.04f),new Color(.014f,.027f,.04f),4);
                hole.transform.SetParent(holder.transform,true);tile.hole=hole.GetComponent<SpriteRenderer>();tile.hole.enabled=false;
                var edge=game.Box("Fracture slab upper seam",new Vector2(centre,.015f),new Vector2(Mathf.Max(.03f,width-.08f),.035f),new Color(.3f,.49f,.55f),3);
                edge.transform.SetParent(holder.transform,true);tile.edge=edge.GetComponent<SpriteRenderer>();
                tiles[i]=tile;
            }
            for(int i=0;i<originals.Count;i++)originals[i].enabled=false;
            ready=true;Physics2D.SyncTransforms();
        }
        public bool Explode(Vector2 impact)
        {
            if(!ready||impact.y>.3f||impact.y<-.65f)return false;
            int index=AncientSniperRules.FloorCell(impact.x,floorLeft,tiles.Length,TileWidth);
            if(index<0||impact.x<tiles[0].left-.25f||impact.x>tiles[tiles.Length-1].right+.25f)return false;
            bool changed=Break(index);
            // A blast near a seam removes the neighbouring slab as well.
            Tile primary=tiles[index];
            if(impact.x-primary.left<.4f&&index>0)changed=Break(index-1)||changed;
            else if(primary.right-impact.x<.4f&&index+1<tiles.Length)changed=Break(index+1)||changed;
            return changed;
        }
        bool Break(int index)
        {
            Tile tile=tiles[index];if(tile.broken)return false;
            tile.broken=true;tile.repairAt=Time.time+RepairSeconds;tile.collider.enabled=false;
            tile.surface.enabled=tile.edge.enabled=false;tile.hole.enabled=true;tiles[index]=tile;
            Vector2 burst=new Vector2((tile.left+tile.right)*.5f,.2f);
            if(game.hero&&game.hero.Combat)game.hero.Combat.EmitParticles(burst,14);
            game.Burst(burst,new Color(.46f,.61f,.65f),10);game.Impact(burst,AncientGame.Cyan,false);
            return true;
        }
        bool HeroOccupies(Tile tile)
        {
            if(!game.hero)return false;Vector2 hero=game.hero.transform.position;
            return hero.x>tile.left-.5f&&hero.x<tile.right+.5f&&hero.y>-.95f&&hero.y<.95f;
        }
        void Update()
        {
            if(!ready||!game||game.Paused)return;
            for(int i=0;i<tiles.Length;i++)
            {
                Tile tile=tiles[i];if(!tile.broken||Time.time<tile.repairAt)continue;
                if(HeroOccupies(tile)){tile.repairAt=Time.time+.5f;tiles[i]=tile;continue;}
                Repair(i);
            }
        }
        void Repair(int index)
        {
            Tile tile=tiles[index];tile.broken=false;tile.repairAt=0;
            tile.collider.enabled=true;tile.surface.enabled=tile.edge.enabled=true;tile.hole.enabled=false;tiles[index]=tile;
        }
        public void RestoreAll()
        {
            if(!ready)return;
            for(int i=0;i<tiles.Length;i++)if(tiles[i].broken)
            {
                // Transition cleanup must not materialize ground inside a falling hero.
                if(HeroOccupies(tiles[i])){Tile tile=tiles[i];tile.repairAt=Time.time+.1f;tiles[i]=tile;}
                else Repair(i);
            }
            Physics2D.SyncTransforms();
        }
        void OnDestroy()
        {
            for(int i=0;i<originals.Count;i++)if(originals[i])originals[i].enabled=true;
            if(holder)Destroy(holder);
        }
    }
}
