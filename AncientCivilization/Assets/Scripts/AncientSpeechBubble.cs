using UnityEngine;

namespace Plugin.AncientCivilization
{
    // A small comic bubble follows a world actor; it never paints a screen-wide dimmer or portrait.
    public sealed class AncientSpeechBubble : System.IDisposable
    {
        public struct Frame
        {
            public AncientSpeechLayout.Placement Placement;
            public Rect Body,Name,Footer;
            public float Scale;
            public bool Interactive,Radio;
        }
        GUIStyle body,name,hint;
        readonly GUIContent measure=new GUIContent();
        Texture2D triangle;
        string measuredText,measuredSpeaker;int measuredStart=-1;
        float requestedWidth,requestedBodyHeight;bool measuredRadio;Texture2D radioTexture;
        static readonly Color Paper=new Color(.97f,.975f,.94f,.98f),Ink=new Color(.045f,.07f,.09f);
        void Styles()
        {
            if(body!=null)return;
            body=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=18,wordWrap=true,richText=false,clipping=TextClipping.Clip,padding=new RectOffset(0,0,0,0)};
            body.normal.textColor=Ink;
            name=new GUIStyle(body){fontSize=14,fontStyle=FontStyle.Bold,wordWrap=false};
            hint=new GUIStyle(GUI.skin.button){font=body.font,fontSize=11,alignment=TextAnchor.MiddleRight,padding=new RectOffset(0,0,0,0),border=new RectOffset(0,0,0,0)};
            hint.normal.background=hint.hover.background=hint.active.background=hint.focused.background=null;
            hint.normal.textColor=new Color(.32f,.39f,.41f);hint.hover.textColor=new Color(.08f,.4f,.52f);hint.active.textColor=Ink;
            const int size=32;triangle=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Comic speech tail",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float t=(size-1-y)/(float)(size-1),u=x/(float)(size-1);
                if(Mathf.Abs(u-.5f)<=(1-t)*.5f)pixels[y*size+x]=new Color32(255,255,255,255);
            }
            triangle.SetPixels32(pixels);triangle.Apply(false,true);
        }
        public Frame Measure(AncientGame game,string speaker,string text,int start,Vector2 head,Vector2 feet,float actorWidth,bool interactive=true,bool radio=false)
        {
            Styles();body.fontSize=18;
            float scale=Mathf.Min(1.6f,Mathf.Min(Screen.width/1280f,Screen.height/720f));scale=Mathf.Max(.05f,scale);
            if(measuredText!=text||measuredSpeaker!=speaker||measuredStart!=start||measuredRadio!=radio)
            {
                measuredText=text;measuredSpeaker=speaker;measuredStart=start;measuredRadio=radio;
                measure.text=text.Substring(Mathf.Clamp(start,0,text.Length));float textWidth=body.CalcSize(measure).x;
                measure.text=speaker;float nameWidth=name.CalcSize(measure).x;
                requestedWidth=Mathf.Clamp(Mathf.Max(textWidth+(radio?48:0),nameWidth)+30,radio?230:160,380);
                measure.text=text.Substring(Mathf.Clamp(start,0,text.Length));
                requestedBodyHeight=Mathf.Clamp(body.CalcHeight(measure,requestedWidth-(radio?76:28)),radio?56:24,76);
            }
            Vector3 hp=game.View.WorldToScreenPoint(head),fp=game.View.WorldToScreenPoint(feet);
            float hx=hp.x/scale,hy=(Screen.height-hp.y)/scale,fy=(Screen.height-fp.y)/scale;
            float width=actorWidth*Screen.height/(game.View.orthographicSize*2)/scale;
            var actor=new AncientSpeechLayout.Box(hx-width*.5f,Mathf.Min(hy,fy),width,Mathf.Abs(fy-hy));
            var placement=AncientSpeechLayout.Compute(Screen.width/scale,Screen.height/scale,new AncientSpeechLayout.Point(hx,hy),
                requestedWidth,requestedBodyHeight+(interactive?58:44),actor);
            var box=placement.Bubble;float footer=interactive?22:8;
            var frame=new Frame{Placement=placement,Scale=scale,Interactive=interactive,Radio=radio,
                Name=new Rect(box.X+15,box.Y+7,box.Width-28,20),
                Body=new Rect(box.X+(radio?62:14),box.Y+30,box.Width-(radio?76:28),Mathf.Max(14,box.Height-30-footer)),
                Footer=new Rect(box.X+box.Width-146,box.Bottom-22,132,17)};
            body.fontSize=Mathf.Min(18,Mathf.Max(10,Mathf.FloorToInt(frame.Body.height*.8f)));
            return frame;
        }
        public int PageEnd(string text,int start,Frame frame)
        {
            if(start>=text.Length)return text.Length;
            int low=start+1,high=text.Length,best=low;
            while(low<=high)
            {
                int end=low+(high-low)/2;measure.text=text.Substring(start,end-start);
                if(body.CalcHeight(measure,frame.Body.width)<=frame.Body.height+.1f){best=end;low=end+1;}else high=end-1;
            }
            // Prefer a word/sentence boundary without wasting most of the final line.
            if(best<text.Length)
            {
                int earliest=start+Mathf.Max(1,Mathf.FloorToInt((best-start)*.72f));
                for(int i=best-1;i>=earliest;i--)if(char.IsWhiteSpace(text[i])||text[i]=='.'||text[i]=='?'||text[i]=='!'||text[i]=='…'){best=i+1;break;}
                if(best>start&&best<text.Length&&char.IsHighSurrogate(text[best-1])&&char.IsLowSurrogate(text[best]))best--;
            }
            return Mathf.Clamp(best,start+1,text.Length);
        }
        public bool Draw(Frame frame,string speaker,string text,Color accent,bool complete,bool ready,int displayDepth=-75)
        {
            var old=GUI.matrix;var color=GUI.color;int depth=GUI.depth;bool enabled=GUI.enabled;bool clicked=false;
            try
            {
                GUI.matrix=Matrix4x4.Scale(Vector3.one*frame.Scale);GUI.depth=displayDepth;
                var p=frame.Placement;var b=p.Bubble;
                if(p.TailVisible)
                {
                    var a=new Vector2(p.TailA.X,p.TailA.Y);var c=new Vector2(p.TailB.X,p.TailB.Y);var tip=new Vector2(p.TailTip.X,p.TailTip.Y);
                    Triangle(a,c,tip,Ink);Vector2 centre=(a+c+tip)/3;
                    Triangle(Vector2.MoveTowards(a,centre,2),Vector2.MoveTowards(c,centre,2),Vector2.MoveTowards(tip,centre,2),Paper);
                }
                PixelBox(new Rect(b.X,b.Y,b.Width,b.Height),Ink,Paper);
                if(p.TailVisible)
                {
                    GUI.color=Paper;
                    if(Mathf.Abs(p.TailA.Y-p.TailB.Y)<.1f)GUI.DrawTexture(new Rect(Mathf.Min(p.TailA.X,p.TailB.X)+2,p.TailA.Y-1,Mathf.Abs(p.TailB.X-p.TailA.X)-4,3),Texture2D.whiteTexture);
                    else GUI.DrawTexture(new Rect(p.TailA.X-1,Mathf.Min(p.TailA.Y,p.TailB.Y)+2,3,Mathf.Abs(p.TailB.Y-p.TailA.Y)-4),Texture2D.whiteTexture);
                }
                GUI.color=accent;GUI.DrawTexture(new Rect(b.X+9,b.Y+10,3,13),Texture2D.whiteTexture);
                GUI.color=Color.white;GUI.Label(frame.Name,speaker,name);GUI.Label(frame.Body,text,body);
                if(frame.Radio)
                {
                    if(!radioTexture)radioTexture=AncientArt.Texture("DialogueRadio");
                    Rect r=AncientRadioAtlas.Bounds;float height=Mathf.Min(50,frame.Body.height),width=height*r.width/r.height;
                    GUI.DrawTextureWithTexCoords(new Rect(b.X+12+(38-width)*.5f,b.Y+31,width,height),radioTexture,
                        new Rect(r.x/radioTexture.width,1-r.yMax/radioTexture.height,r.width/radioTexture.width,r.height/radioTexture.height));
                }
                if(frame.Interactive)
                {
                    GUI.enabled=enabled&&ready;
                    clicked=GUI.Button(frame.Footer,complete?"Space / Enter  ▶":"Space / Enter  전체",hint);
                }
            }
            finally {GUI.matrix=old;GUI.color=color;GUI.depth=depth;GUI.enabled=enabled;}
            return clicked;
        }
        void Triangle(Vector2 a,Vector2 b,Vector2 tip,Color tint)
        {
            if(Event.current.type!=EventType.Repaint)return;
            Matrix4x4 old=GUI.matrix,map=Matrix4x4.identity;Vector2 centre=(a+b)*.5f;
            map.m00=b.x-a.x;map.m10=b.y-a.y;map.m01=tip.x-centre.x;map.m11=tip.y-centre.y;map.m03=a.x;map.m13=a.y;
            GUI.matrix=old*map;GUI.color=tint;GUI.DrawTexture(new Rect(0,0,1,1),triangle);GUI.matrix=old;
        }
        static void PixelBox(Rect r,Color edge,Color fill)
        {
            GUI.color=edge;
            GUI.DrawTexture(new Rect(r.x+5,r.y,r.width-10,r.height),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x,r.y+5,r.width,r.height-10),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),Texture2D.whiteTexture);
            GUI.color=fill;
            GUI.DrawTexture(new Rect(r.x+5,r.y+2,r.width-10,r.height-4),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x+2,r.y+5,r.width-4,r.height-10),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x+4,r.y+4,r.width-8,r.height-8),Texture2D.whiteTexture);
        }
        public void Dispose(){if(triangle)Object.Destroy(triangle);triangle=null;}
    }
}
