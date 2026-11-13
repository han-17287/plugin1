using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Pixel UI cursor; it never locks the mouse or captures it outside the Game view.
    public sealed class AncientPointer : MonoBehaviour
    {
        Texture2D aim,menu;bool previousVisible;CursorLockMode previousLock;float clickUntil;
        void Awake()
        {
            previousVisible=Cursor.visible;previousLock=Cursor.lockState;Cursor.lockState=CursorLockMode.None;
            aim=Make(false);menu=Make(true);
        }
        Texture2D Make(bool arrow)
        {
            const int size=32;var t=new Texture2D(size,size,TextureFormat.RGBA32,false);t.filterMode=FilterMode.Point;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int dx=x-16,dy=y-16;bool shape;
                if(arrow)
                    shape=(x>=5&&x<=19&&y>=9&&y<=26&&x-5<=(26-y)/2)||(x>=12&&x<=15&&y>=5&&y<=16);
                else
                {
                    int d=Mathf.Abs(dx)+Mathf.Abs(dy);
                    shape=(d>=10&&d<=12)||((dx==0||dy==0)&&d>=14&&d<=15)||(Mathf.Abs(dx)<=1&&Mathf.Abs(dy)<=1);
                }
                if(shape)pixels[y*size+x]=Color.white;
            }
            var original=(Color[])pixels.Clone();
            for(int y=1;y<size-1;y++)for(int x=1;x<size-1;x++)
            {
                if(original[y*size+x].a>0)continue;
                if(original[(y-1)*size+x].a>0||original[(y+1)*size+x].a>0||original[y*size+x-1].a>0||original[y*size+x+1].a>0)
                    pixels[y*size+x]=new Color(.015f,.025f,.035f,1);
            }
            t.SetPixels(pixels);t.Apply();return t;
        }
        bool Inside()
        {
            var p=Input.mousePosition;
            return Application.isFocused&&p.x>=0&&p.y>=0&&p.x<Screen.width&&p.y<Screen.height;
        }
        void Update()
        {
            Cursor.visible=!Inside();
            if(Input.GetMouseButtonDown(0))clickUntil=Time.unscaledTime+.1f;
        }
        void OnGUI()
        {
            if(Event.current.type!=EventType.Repaint||!Inside())return;
            var game=AncientGame.Instance;if(!game||!game.hero)return;
            Matrix4x4 oldMatrix=GUI.matrix;int oldDepth=GUI.depth;Color oldColor=GUI.color;
            GUI.matrix=Matrix4x4.identity;GUI.depth=-10000;
            bool isMenu=game.Paused||game.won;
            Color color=isMenu?new Color(.85f,.95f,1):(game.hero.AttackMode||game.hero.HasWireTarget)?AncientGame.Cyan:new Color(.5f,.6f,.65f);
            if(!isMenu&&!game.hero.AttackMode&&game.hero.WireCooldown<1)color=AncientGame.Orange;
            if(Time.unscaledTime<clickUntil)color=Color.white;
            GUI.color=color;
            float size=32*Mathf.Clamp(Screen.height/900f,.8f,1.4f);
            Vector2 mouse=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
            Vector2 hotspot=isMenu?new Vector2(size*5/32,size*5/32):Vector2.one*size*.5f;
            if(!isMenu&&!game.hero.AttackMode&&game.hero.ZipMode)GUIUtility.RotateAroundPivot(45,mouse);
            GUI.DrawTexture(new Rect(Mathf.Round(mouse.x-hotspot.x),Mathf.Round(mouse.y-hotspot.y),size,size),isMenu?menu:aim);
            GUI.color=oldColor;GUI.depth=oldDepth;GUI.matrix=oldMatrix;
        }
        void OnApplicationFocus(bool focus){if(!focus)Cursor.visible=true;}
        void OnDisable(){Cursor.visible=previousVisible;Cursor.lockState=previousLock;}
        void OnDestroy()
        {if(aim)Destroy(aim);if(menu)Destroy(menu);Cursor.visible=previousVisible;Cursor.lockState=previousLock;}
    }
}
