using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // One static colored mesh gives the road a coherent district without one renderer per window.
    public sealed class AncientDistrict : MonoBehaviour
    {
        AncientGame game;Mesh mesh;Material material;GUIStyle label;
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<int> triangles=new List<int>();
        readonly List<Color> colors=new List<Color>();
        struct Sign {public string text;public Vector2 point;public Sign(string t,float x,float y){text=t;point=new Vector2(x,y);}}
        readonly List<Sign> signs=new List<Sign>();
        static readonly Color Wall=new Color(.065f,.105f,.15f),Edge=new Color(.14f,.22f,.27f);
        static readonly Color Warm=new Color(.85f,.56f,.24f),Teal=new Color(.18f,.62f,.57f);
        void Quad(float x,float y,float w,float h,Color color)
        {
            int i=vertices.Count;float l=x-w*.5f,r=x+w*.5f,b=y-h*.5f,t=y+h*.5f;
            vertices.Add(new Vector3(l,b,0));vertices.Add(new Vector3(r,b,0));
            vertices.Add(new Vector3(l,t,0));vertices.Add(new Vector3(r,t,0));
            triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);
            triangles.Add(i+2);triangles.Add(i+1);triangles.Add(i+3);
            for(int n=0;n<4;n++)colors.Add(color);
        }
        void Building(float left,float right,float baseY,float height,int seed)
        {
            Quad((left+right)*.5f,baseY+height*.5f,right-left,height,Wall);
            Quad((left+right)*.5f,baseY+height-.15f,right-left+.2f,.3f,Edge);
            Quad(left+.12f,baseY+height*.5f,.24f,height,Edge);
            Quad(right-.12f,baseY+height*.5f,.24f,height,Edge);
            int window=0;
            for(float x=left+.85f;x<right-.5f;x+=1.55f)
                for(float y=baseY+3.5f;y<baseY+height-.7f;y+=1.35f)
                {
                    Quad(x,y,.78f,.88f,new Color(.025f,.045f,.065f));
                    Quad(x,y,.57f,.66f,(window++ + seed)%3==0?Warm:new Color(.16f,.27f,.31f));
                    Quad(x,y,.055f,.68f,Edge);
                }
            for(float x=left+.8f;x<right-.5f;x+=2.8f)
            {
                Quad(x,baseY+1.15f,1.45f,2.3f,new Color(.025f,.04f,.055f));
                Quad(x+.5f,baseY+1.1f,.06f,.25f,Warm);
            }
        }
        void Plate(string text,float x,float y,float width,Color accent)
        {
            Quad(x,y,width,.8f,new Color(.025f,.055f,.075f));
            Quad(x,y+.4f,width,.055f,accent);Quad(x,y-.4f,width,.055f,accent);
            signs.Add(new Sign(text,x,y+.05f));
        }
        void Lamp(float x,float floor)
        {
            Quad(x,floor+2.1f,.08f,4.2f,Edge);Quad(x+.35f,floor+4.2f,.8f,.1f,Edge);
            Quad(x+.62f,floor+4.12f,.3f,.16f,Warm);
        }
        void Ramp(float left,float right,float from,float to)
        {
            float length=new Vector2(right-left,to-from).magnitude;
            var ramp=game.Box("Loading bay road ramp",new Vector2((left+right)*.5f,(from+to)*.5f),
                new Vector2(length,.18f),new Color(.19f,.25f,.28f),1);
            ramp.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(to-from,right-left)*Mathf.Rad2Deg);
            ramp.layer=8;ramp.AddComponent<BoxCollider2D>().size=Vector2.one;
        }
        public static float RoadHeight(float x)
        {
            if(x<40)return 0;if(x<43)return (x-40)/3;
            if(x<=58)return 1;if(x<61)return 1-(x-58)/3;return 0;
        }
        public void Initialize(AncientGame owner)
        {
            game=owner;
            // Patrol station and its canopy are the safe movement/tutorial starting area.
            Building(-4,7,0,8.5f,1);
            Quad(2,1.3f,1.8f,2.6f,new Color(.02f,.07f,.1f));
            Quad(2,2.9f,2,.12f,Teal);
            Plate("하층 치안 순찰소",1.5f,5.6f,4.8f,Teal);
            Plate("순찰로  →",4.8f,1.8f,2.2f,Warm);
            // Open garage frontage leaves the bike and vertical wire practice space readable.
            Building(8,19.5f,0,7.9f,2);
            Quad(12.4f,1.35f,7,2.7f,new Color(.03f,.045f,.055f));
            for(float x=9.2f;x<15.8f;x+=.45f)Quad(x,2.6f,.16f,.32f,Edge);
            Plate("정비소 / 순찰 오토바이",13,5.5f,7,Teal);
            Plate("입자 와이어 점검",8.8f,7.5f,3.8f,Teal);
            Quad(17.4f,1.35f,1.35f,1.65f,new Color(.2f,.25f,.27f));
            Quad(17.4f,1.35f,.85f,.85f,Warm);Quad(17.4f,1.35f,.48f,.48f,Wall);
            Plate("장비 훈련 구역",17.4f,3.1f,3.6f,Warm);
            // Workshops and production infrastructure establish why the stolen cores matter.
            Building(21,39,0,8.8f,3);
            Plate("하층 공동 작업장",28.5f,5.6f,6.6f,Teal);
            for(float x=22;x<38;x+=3)
            {
                Quad(x,1.7f,.14f,3.4f,Edge);Quad(x+.6f,3.35f,1.3f,.12f,Edge);
                Quad(x+.6f,1.15f,.9f,1.8f,new Color(.11f,.18f,.21f));
                Quad(x+.6f,1.6f,.55f,.12f,Teal);
            }
            Building(41,62,1,8.1f,4);
            Plate("에너지 코어 생산소",50,6.7f,8.6f,Teal);
            for(int i=0;i<3;i++)
            {
                float x=46+i*3.7f;
                Quad(x,3,1.35f,2.7f,new Color(.12f,.27f,.25f));
                Quad(x,3,.75f,2.2f,new Color(.18f,.62f,.47f));
                Quad(x,4.4f,1.55f,.18f,Edge);Quad(x,1.6f,1.55f,.18f,Edge);
                Quad(x+.15f,3,.12f,1.9f,new Color(.55f,.95f,.69f));
            }
            Ramp(40,43,0,1);Ramp(58,61,1,0);
            // Residential balconies, laundry and warm windows show a lived-in lower district.
            Building(64,83,0,9.2f,5);Plate("주거 골목",73,5.8f,4.8f,Warm);
            for(float x=65;x<82;x+=3.1f)
            {
                Quad(x,3.4f,2.6f,.16f,Edge);Quad(x-1.2f,3.8f,.08f,.8f,Edge);
                Quad(x+1.2f,3.8f,.08f,.8f,Edge);Quad(x,4.15f,2.5f,.06f,Edge);
                Quad(x,5.1f,2.8f,.04f,new Color(.3f,.34f,.33f));
                Quad(x-.55f,4.8f,.5f,.62f,new Color(.45f,.3f,.23f));
                Quad(x+.4f,4.76f,.5f,.7f,new Color(.19f,.32f,.39f));
            }
            Building(85,97,0,8.5f,6);
            Plate("동쪽 검문소",90.5f,6.1f,6.5f,Teal);
            Quad(94,1.7f,.18f,3.4f,Edge);Quad(95.2f,3.35f,2.6f,.18f,Edge);
            Plate("화물 도로  →",94,4.7f,4.8f,Warm);
            Building(99,118,0,8.9f,7);Plate("화물 승강장",109,6.6f,7.4f,Warm);
            for(float x=101;x<117;x+=2)
            {Quad(x,1.2f,1.5f,2.4f,new Color(.09f,.16f,.19f));Quad(x,1.25f,1.2f,.08f,Edge);}
            for(float x=-3;x<118;x+=7.5f)Lamp(x,RoadHeight(x));
            for(float x=-4;x<118;x+=2.5f)Quad(x,RoadHeight(x)+.025f,1.25f,.045f,new Color(.31f,.38f,.39f));
            for(float x=40.5f;x<60;x+=1.1f)Quad(x,RoadHeight(x)+.045f,.42f,.055f,Warm);
            var o=new GameObject("Batched lower-city architecture");o.transform.SetParent(transform,false);
            mesh=new Mesh{name="Patrol district architecture"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateBounds();
            o.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=o.AddComponent<MeshRenderer>();renderer.sortingOrder=-5;
            material=new Material(Shader.Find("Sprites/Default"));renderer.sharedMaterial=material;
        }
        void OnGUI()
        {
            if(!game||!game.hero||game.Paused||game.MainMenu||!game.View||(game.Story&&game.Story.InUpperCity))return;
            if(label==null)label=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=15,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            GUI.matrix=Matrix4x4.identity;GUI.depth=10;GUI.color=new Color(.85f,.94f,.88f);
            float scale=Mathf.Clamp(Screen.height/900f,.8f,1.5f);label.fontSize=Mathf.RoundToInt(15*scale);
            foreach(var sign in signs)
            {
                Vector3 p=game.View.WorldToScreenPoint(sign.point);
                if(p.z<=0||p.x<-120||p.x>Screen.width+120||p.y<0||p.y>Screen.height)continue;
                GUI.Label(new Rect(p.x-150*scale,Screen.height-p.y-14*scale,300*scale,30*scale),sign.text,label);
            }
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
