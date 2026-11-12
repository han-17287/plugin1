using UnityEngine;
namespace Plugin.AncientCivilization
{
    // Small native-pixel parts, articulated in world space; no filtered upscaling or humanoid suit silhouette.
    public static class AncientLivingArt
    {
        static readonly Sprite[] cache=new Sprite[8];
        static readonly Color32 ink=new Color32(10,17,28,255),steel=new Color32(53,69,82,255),light=new Color32(186,198,194,255),rust=new Color32(177,95,47,255),blue=new Color32(39,154,232,255);
        public static Sprite Part(int part)
        {
            part=Mathf.Clamp(part,0,7);if(cache[part])return cache[part];
            int w=part==0?64:part==4?56:32,h=part==0?64:32;var pixels=new Color32[w*h];
            if(part==0)
            {
                Rect(pixels,w,h,9,4,46,49,ink);Rect(pixels,w,h,12,7,40,43,steel);
                Rect(pixels,w,h,5,34,54,18,ink);Rect(pixels,w,h,8,37,48,12,rust);
                Rect(pixels,w,h,15,48,34,12,ink);Rect(pixels,w,h,18,50,28,7,light);
                Rect(pixels,w,h,24,52,16,3,blue);Rect(pixels,w,h,16,8,5,25,light);Rect(pixels,w,h,43,8,5,25,light);
                Rect(pixels,w,h,23,14,18,24,ink);Rect(pixels,w,h,26,17,12,18,steel);
                for(int i=0;i<4;i++){Rect(pixels,w,h,11+i*12,39,7,3,light);Rect(pixels,w,h,12+i*12,46,2,2,ink);}
            }
            else if(part==1)
            {Rect(pixels,w,h,7,2,18,28,ink);Rect(pixels,w,h,10,4,12,24,steel);Rect(pixels,w,h,8,17,16,11,rust);Rect(pixels,w,h,10,23,12,3,light);for(int y=5;y<16;y+=4)Rect(pixels,w,h,11,y,10,2,light);}
            else if(part==2)
            {Rect(pixels,w,h,3,5,26,22,ink);Rect(pixels,w,h,5,7,22,18,steel);Rect(pixels,w,h,7,9,18,12,rust);for(int x=7;x<26;x+=6)Rect(pixels,w,h,x,18,4,5,light);Rect(pixels,w,h,6,8,4,8,blue);}
            else if(part==3)
            {Rect(pixels,w,h,3,2,26,14,ink);Rect(pixels,w,h,6,5,20,9,steel);Rect(pixels,w,h,9,15,14,14,ink);Rect(pixels,w,h,12,17,8,10,light);Rect(pixels,w,h,5,5,23,3,rust);}
            else if(part==4)
            {Rect(pixels,w,h,3,7,50,15,ink);Rect(pixels,w,h,5,10,46,11,light);Rect(pixels,w,h,14,20,28,9,ink);Rect(pixels,w,h,17,22,22,5,blue);Rect(pixels,w,h,7,5,10,8,ink);Rect(pixels,w,h,39,5,10,8,ink);Rect(pixels,w,h,4,13,9,3,rust);}
            else if(part==5)
            {Rect(pixels,w,h,4,4,24,24,ink);Rect(pixels,w,h,7,7,18,18,rust);Rect(pixels,w,h,8,19,16,4,light);Rect(pixels,w,h,13,7,5,18,steel);Rect(pixels,w,h,6,5,20,3,light);}
            else if(part==6)
            {Rect(pixels,w,h,6,12,22,8,ink);Rect(pixels,w,h,8,14,18,4,light);Rect(pixels,w,h,22,13,5,6,rust);Rect(pixels,w,h,4,9,7,14,steel);}
            else
            {Rect(pixels,w,h,8,5,16,22,ink);Rect(pixels,w,h,11,8,10,16,blue);Rect(pixels,w,h,14,10,4,12,light);}
            var texture=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name="Living pixel part "+part};texture.SetPixels32(pixels);texture.Apply();
            return cache[part]=Sprite.Create(texture,new Rect(0,0,w,h),Vector2.one*.5f,part==0?14:16);
        }
        static void Rect(Color32[] p,int w,int h,int x,int y,int sx,int sy,Color32 c)
        {for(int j=Mathf.Max(0,y);j<Mathf.Min(h,y+sy);j++)for(int i=Mathf.Max(0,x);i<Mathf.Min(w,x+sx);i++)p[j*w+i]=c;}
    }
}
