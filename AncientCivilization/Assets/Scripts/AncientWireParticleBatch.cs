using UnityEngine;
using UnityEngine.Rendering;
namespace Plugin.AncientCivilization
{
    // The same 16px diamond texture and helix positions, submitted as one dynamic mesh.
    public sealed class AncientWireParticleBatch : MonoBehaviour
    {
        const int Capacity=24;
        readonly Vector3[] vertices=new Vector3[Capacity*4];
        readonly Color32[] colors=new Color32[Capacity*4];
        Mesh mesh;MeshRenderer display;Material material;int previousCount;bool visible;
        public void Initialize(Sprite sprite)
        {
            mesh=new Mesh{name="Wire helix flowing diamonds / one draw"};mesh.MarkDynamic();
            var uv=new Vector2[Capacity*4];var triangles=new int[Capacity*6];
            for(int i=0;i<Capacity;i++)
            {
                int v=i*4,k=i*6;
                uv[v]=Vector2.zero;uv[v+1]=Vector2.right;uv[v+2]=Vector2.up;uv[v+3]=Vector2.one;
                triangles[k]=v;triangles[k+1]=v+1;triangles[k+2]=v+2;
                triangles[k+3]=v+2;triangles[k+4]=v+1;triangles[k+5]=v+3;
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.colors32=colors;mesh.triangles=triangles;
            // Wire reach is 22 units; a fixed local bound avoids rebuilding it every frame.
            mesh.bounds=new Bounds(Vector3.zero,Vector3.one*60);
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;display=gameObject.AddComponent<MeshRenderer>();
            material=new Material(Shader.Find("Sprites/Default"));material.mainTexture=sprite.texture;display.sharedMaterial=material;
            display.sortingOrder=19;display.shadowCastingMode=ShadowCastingMode.Off;display.receiveShadows=false;display.enabled=false;
        }
        public void Hide(){if(!visible)return;visible=false;display.enabled=false;}
        public void Draw(Vector2 hand,Vector2 end,float age,bool highQuality)
        {
            if(!visible){visible=true;display.enabled=true;}
            float length=Vector2.Distance(hand,end);Vector2 normal=Vector2.Perpendicular(end-hand).normalized;
            int count=Mathf.Clamp(Mathf.CeilToInt(length*1.2f),6,highQuality?24:12);
            Vector3 origin=transform.position;
            for(int i=0;i<count;i++)
            {
                float t=Mathf.Repeat(age*1.6f+i/(float)count,1),offset,depth;
                AncientRules.WireHelixOffset(length,age,t,i%2,out offset,out depth);
                Vector2 p=Vector2.Lerp(hand,end,t)+normal*offset;
                Vector3 centre=new Vector3(p.x,p.y,depth)-origin;
                float sparkle=.5f+.5f*Mathf.Sin(age*17+i*2),half=(.055f+sparkle*.035f)*.5f;
                Color32 tint=Color.Lerp(new Color(.2f,.65f,1),Color.white,sparkle*.7f);int at=i*4;
                vertices[at]=centre+new Vector3(-half,-half,0);vertices[at+1]=centre+new Vector3(half,-half,0);
                vertices[at+2]=centre+new Vector3(-half,half,0);vertices[at+3]=centre+new Vector3(half,half,0);
                colors[at]=colors[at+1]=colors[at+2]=colors[at+3]=tint;
            }
            for(int i=count;i<previousCount;i++)
            {int at=i*4;vertices[at]=vertices[at+1]=vertices[at+2]=vertices[at+3]=Vector3.zero;}
            previousCount=count;mesh.vertices=vertices;mesh.colors32=colors;
        }
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
