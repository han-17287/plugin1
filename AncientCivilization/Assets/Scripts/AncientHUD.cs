using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public static class AncientHUD
    {
        static GUIStyle button;
        static readonly List<Texture2D> owned=new List<Texture2D>();
        static Texture2D Surface(Color fill,Color edge)
        {
            var t=new Texture2D(64,32,TextureFormat.RGBA32,false);t.filterMode=FilterMode.Point;
            for(int y=0;y<32;y++)for(int x=0;x<64;x++)
                t.SetPixel(x,y,x==0||x==63||y==0||y==31?edge:Color.Lerp(fill,fill*1.18f,y/31f));
            t.Apply();owned.Add(t);return t;
        }
        public static GUIStyle Button
        {
            get
            {
                if(button!=null)return button;
                button=new GUIStyle(GUI.skin.button){font=AncientKoreanUI.Font,fontSize=14,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft,border=new RectOffset(2,2,2,2),padding=new RectOffset(16,12,6,6)};
                button.normal.background=Surface(new Color(.025f,.065f,.09f),new Color(.12f,.28f,.33f));
                button.hover.background=Surface(new Color(.04f,.14f,.19f),new Color(.18f,.8f,.9f));
                button.active.background=Surface(new Color(.02f,.08f,.11f),new Color(.7f,1,1));
                button.focused.background=button.hover.background;
                button.normal.textColor=new Color(.78f,.88f,.91f);button.hover.textColor=Color.white;
                button.active.textColor=AncientGame.Cyan;button.focused.textColor=Color.white;
                return button;
            }
        }
        public static void Panel(Rect r,Color color)
        {
            GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);
            if(r.height>=30&&r.width>100)
            {
                GUI.color=new Color(.18f,.6f,.7f,color.a*.55f);
                GUI.DrawTexture(new Rect(r.x,r.y,r.width,1),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(r.x,r.y,2,r.height),Texture2D.whiteTexture);
            }
            GUI.color=Color.white;
        }
        public static void Dispose()
        {foreach(var t in owned)if(t)Object.Destroy(t);owned.Clear();button=null;}
    }
}
