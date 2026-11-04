using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Plugin.AncientCivilization
{
    // Static scenery only. Gameplay colliders and moving/interactive objects remain separate.
    // Managed texture references avoid obsolete numeric instance IDs across Unity versions.
    public sealed class AncientWorldBatch : MonoBehaviour
    {
        struct Key : System.IEquatable<Key>
        {
            public Texture texture;public int order,floor,room;
            public bool Equals(Key other){return object.ReferenceEquals(texture,other.texture)&&order==other.order&&floor==other.floor&&room==other.room;}
            public override bool Equals(object value){return value is Key&&Equals((Key)value);}
            public override int GetHashCode(){unchecked{return ((System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(texture)*397^order)*397^floor)*397^room;}}
        }
        sealed class Build
        {
            public Texture texture;
            public readonly List<Vector3> vertices=new List<Vector3>();
            public readonly List<Vector2> uv=new List<Vector2>();
            public readonly List<Color32> colors=new List<Color32>();
            public readonly List<int> triangles=new List<int>();
        }
        struct Entry {public Mesh mesh;public MeshRenderer renderer;public int floor,room;}
        readonly Dictionary<Key,Build> builders=new Dictionary<Key,Build>();
        readonly Dictionary<Texture,Material> materials=new Dictionary<Texture,Material>();
        readonly List<Entry> entries=new List<Entry>();
        AncientGame game;Sprite white;int shownFloor=-99,shownRoom=-99;
        public int QuadCount {get;private set;}
        public int RendererCount {get{return entries.Count;}}
        public int ActiveRendererCount {get;private set;}
        public void Initialize(AncientGame owner,Sprite square){game=owner;white=square;}
        public void Box(Vector2 position,Vector2 size,Color color,int order)
        {
            int floor=size.x>180?-1:AncientCityLayout.VisualSector(position.x);
            Add(white,position,size,color,order,floor);
        }
        public void Sprite(Sprite sprite,Vector2 position,Vector2 scale,Color color,int order)
        {Add(sprite,position,scale,color,order,AncientCityLayout.VisualSector(position.x));}
        public void RotatedSprite(Sprite sprite,Vector2 position,Vector2 scale,Color color,int order,float angle)
        {Add(sprite,position,scale,color,order,AncientCityLayout.VisualSector(position.x),angle);}
        void Add(Sprite sprite,Vector2 position,Vector2 scale,Color color,int order,int floor,float angle=0)
        {
            Key key=new Key{texture=sprite.texture,order=order,floor=floor,room=AncientCampaignRules.RoomAt(position.x)};Build build;
            if(!builders.TryGetValue(key,out build)){build=new Build{texture=sprite.texture};builders.Add(key,build);}
            float ppu=sprite.pixelsPerUnit;
            Vector2 size=sprite.rect.size/ppu;
            // Box uses the built-in 1-unit sprite, while authored sprites retain their real pivot/size.
            Vector2 pivot=sprite.pivot/ppu;
            float left=position.x-pivot.x*scale.x,bottom=position.y-pivot.y*scale.y;
            float right=left+size.x*scale.x,top=bottom+size.y*scale.y;
            Rect r=sprite.textureRect;float u0=r.x/sprite.texture.width,u1=r.xMax/sprite.texture.width;
            float v0=r.y/sprite.texture.height,v1=r.yMax/sprite.texture.height;
            int at=build.vertices.Count;
            build.vertices.Add(new Vector3(left,bottom,0));build.vertices.Add(new Vector3(right,bottom,0));
            build.vertices.Add(new Vector3(left,top,0));build.vertices.Add(new Vector3(right,top,0));
            if(Mathf.Abs(angle)>.001f)
            {
                float sn=Mathf.Sin(angle*Mathf.Deg2Rad),cs=Mathf.Cos(angle*Mathf.Deg2Rad);
                for(int i=at;i<at+4;i++)
                {Vector2 delta=(Vector2)build.vertices[i]-position;build.vertices[i]=position+new Vector2(delta.x*cs-delta.y*sn,delta.x*sn+delta.y*cs);}
            }
            build.uv.Add(new Vector2(u0,v0));build.uv.Add(new Vector2(u1,v0));build.uv.Add(new Vector2(u0,v1));build.uv.Add(new Vector2(u1,v1));
            for(int i=0;i<4;i++)build.colors.Add(color);
            build.triangles.Add(at);build.triangles.Add(at+1);build.triangles.Add(at+2);
            build.triangles.Add(at+2);build.triangles.Add(at+1);build.triangles.Add(at+3);QuadCount++;
        }
        public void Complete()
        {
            foreach(var pair in builders)
            {
                Key key=pair.Key;Build build=pair.Value;
                var o=new GameObject("Static scenery / floor "+key.floor+" / layer "+key.order);o.transform.SetParent(transform,false);
                var mesh=new Mesh{name=o.name};
                if(build.vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
                mesh.SetVertices(build.vertices);mesh.SetUVs(0,build.uv);mesh.SetColors(build.colors);mesh.SetTriangles(build.triangles,0);mesh.RecalculateBounds();
                o.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=o.AddComponent<MeshRenderer>();
                renderer.sortingOrder=key.order;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                Material material;
                if(!materials.TryGetValue(key.texture,out material))
                {material=new Material(Shader.Find("Sprites/Default"));material.mainTexture=build.texture;materials.Add(key.texture,material);}
                renderer.sharedMaterial=material;entries.Add(new Entry{mesh=mesh,renderer=renderer,floor=key.floor,room=key.room});
            }
            builders.Clear();UpdateVisibility(0,-1);
        }
        void UpdateVisibility(int floor,int room)
        {
            shownFloor=floor;shownRoom=room;ActiveRendererCount=0;
            foreach(var entry in entries)
            {bool visible=entry.floor<0||entry.floor==floor&&entry.room==room;entry.renderer.enabled=visible;if(visible)ActiveRendererCount++;}
        }
        void LateUpdate()
        {int floor=game&&game.Finale&&game.Finale.Active?5:game&&game.Story?game.Story.CurrentFloor:0;int room=game&&game.Campaign?game.Campaign.CurrentRoom:-1;if(floor!=shownFloor||room!=shownRoom)UpdateVisibility(floor,room);}
        void OnDestroy()
        {
            foreach(var entry in entries)if(entry.mesh)Destroy(entry.mesh);
            foreach(var material in materials.Values)if(material)Destroy(material);
        }
    }
}
