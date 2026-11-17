using UnityEngine;
namespace Plugin.AncientCivilization
{
    // Small authored pixel silhouettes distinguish the four local mechanisms.
    // All are centred on the actual interaction point; no colliders are added by art.
    public static class AncientRouteDeviceArt
    {
        const int Size=48;
        static readonly Sprite[] cache=new Sprite[4];
        static readonly Color32 ink=new Color32(9,17,26,255),steel=new Color32(59,78,89,255),edge=new Color32(153,182,192,255);
        static readonly Color32 white=new Color32(232,245,241,255),blue=new Color32(54,172,230,255),deep=new Color32(25,80,119,255),gold=new Color32(248,208,69,255);
        static Color32[] pixels;
        public static Sprite Get(AncientRouteAction action)
        {
            int id=action==AncientRouteAction.Relay?1:action==AncientRouteAction.Wire?2:action==AncientRouteAction.Throw?3:0;
            if(cache[id])return cache[id];pixels=new Color32[Size*Size];
            if(id==0)
            {
                Panel(8,5,32,38);R(13,10,22,28,deep);
                for(int y=11;y<36;y+=7){R(13,y,22,2,steel);R(14,y+2,20,1,edge);}
                for(int i=0;i<8;i++){R(15+i*2,10+i*3,5,5,ink);R(16+i*2,10+i*3,2,4,gold);}
                R(5,20,4,8,steel);R(39,20,4,8,steel);
            }
            else if(id==1)
            {
                Panel(5,7,38,34);R(10,12,28,24,deep);R(15,15,18,18,ink);
                R(20,18,8,12,blue);R(22,19,4,10,white);
                R(8,22,9,4,blue);R(31,22,9,4,blue);R(22,9,4,8,blue);R(22,31,4,8,blue);
                R(17,1,14,7,ink);R(20,2,8,6,steel);R(21,3,6,2,edge);
            }
            else if(id==2)
            {
                R(17,2,14,22,ink);R(20,4,8,18,steel);R(21,5,3,16,edge);
                Panel(7,21,34,23);R(12,26,24,13,deep);
                R(17,29,14,7,blue);R(21,27,6,11,blue);R(22,30,4,5,white);
                R(4,28,3,9,edge);R(41,28,3,9,edge);R(12,2,24,4,ink);R(15,3,18,2,edge);
            }
            else
            {
                Panel(4,4,40,40);R(10,10,28,28,deep);
                R(15,15,18,18,steel);R(18,18,12,12,ink);R(21,21,6,6,gold);
                for(int i=0;i<3;i++)
                {
                    R(8+i*3,22-i*2,4,4,white);R(8+i*3,24+i*2,4,4,white);
                    R(36-i*3,22-i*2,4,4,white);R(36-i*3,24+i*2,4,4,white);
                }
                R(20,8,8,3,gold);R(20,37,8,3,gold);
            }
            var texture=new Texture2D(Size,Size,TextureFormat.RGBA32,false){name="Route mechanism "+id,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply();pixels=null;
            return cache[id]=Sprite.Create(texture,new Rect(0,0,Size,Size),Vector2.one*.5f,32);
        }
        static void Panel(int x,int y,int w,int h)
        {
            R(x,y,w,h,ink);R(x+2,y+2,w-4,h-4,steel);R(x+3,y+h-5,w-6,2,edge);
            R(x+3,y+3,2,2,white);R(x+w-5,y+3,2,2,white);
        }
        static void R(int x,int y,int w,int h,Color32 color)
        {for(int py=Mathf.Max(0,y);py<Mathf.Min(Size,y+h);py++)for(int px=Mathf.Max(0,x);px<Mathf.Min(Size,x+w);px++)pixels[py*Size+px]=color;}
    }
}
