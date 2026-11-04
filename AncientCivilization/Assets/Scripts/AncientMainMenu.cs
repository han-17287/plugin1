using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientMainMenu : MonoBehaviour
    {
        AncientGame game;bool controls,confirmNew;GUIStyle logo,heading,body,caption;
        void Awake(){game=GetComponent<AncientGame>();}
        void Update()
        {
            if(!game.MainMenu)return;
            if(Input.GetKeyDown(KeyCode.Escape)){controls=false;confirmNew=false;}
        }
        void Label(Rect rect,string text,GUIStyle style,Color color)
        {GUI.color=color;GUI.Label(rect,text,style);GUI.color=Color.white;}
        void OnGUI()
        {
            if(!game||!game.MainMenu)return;
            var oldMatrix=GUI.matrix;var oldColor=GUI.color;int oldDepth=GUI.depth;
            var oldButton=GUI.skin.button;GUI.depth=-100;GUI.skin.button=AncientHUD.Button;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float w=Screen.width/scale,h=Screen.height/scale;
            if(logo==null)
            {
                logo=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=76,fontStyle=FontStyle.Bold};
                heading=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=30,fontStyle=FontStyle.Bold};
                body=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=17,wordWrap=true};
                caption=new GUIStyle(body){fontSize=13};
            }
            GUI.DrawTexture(new Rect(0,0,w,h),AncientArt.Texture("City"),ScaleMode.ScaleAndCrop);
            AncientHUD.Panel(new Rect(0,0,w,h),new Color(.005f,.02f,.035f,.58f));
            // Slow moving background motes remain animated while gameplay is paused.
            for(int i=0;i<28;i++)
            {
                float x=Mathf.Repeat(i*83.7f+Time.unscaledTime*(4+i%4),w);
                float y=Mathf.Repeat(i*47.3f-Time.unscaledTime*(6+i%3),h);
                GUI.color=new Color(.2f,.9f,1,.12f+(i%3)*.07f);
                GUI.DrawTexture(new Rect(x,y,2+i%2,2+i%2),Texture2D.whiteTexture);
            }
            GUI.color=Color.white;
            float left=(w-1120)*.5f,top=(h-620)*.5f;
            AncientHUD.Panel(new Rect(left-20,top,570,610),new Color(.015f,.035f,.055f,.86f));
            Label(new Rect(left+18,top+22,500,28),"CHAPTER 0  /  THE BURIED CITY",caption,AncientGame.Cyan);
            Label(new Rect(left+12,top+55,520,92),"PLUGIN",logo,Color.white);
            Label(new Rect(left+18,top+153,510,45),"ANCIENT CIVILIZATION",heading,Color.white);
            Label(new Rect(left+18,top+208,480,62),"A lower-city peace officer. An upper-level thief. Locally made energy cores stolen and carried away in a truck.",body,new Color(.65f,.77f,.81f));
            float xButton=left+18,yButton=top+298;
            bool canContinue=game.HasSession||game.HasSavedProgress;
            GUI.enabled=canContinue;
            if(GUI.Button(new Rect(xButton,yButton,450,48),game.HasSession?"01  /  RESUME":"01  /  CONTINUE"))
            {controls=confirmNew=false;game.BeginSession();}
            GUI.enabled=true;
            if(GUI.Button(new Rect(xButton,yButton+60,450,48),canContinue?"02  /  NEW GAME":"02  /  START GAME"))
            {if(canContinue)confirmNew=true;else game.BeginSession();}
            if(GUI.Button(new Rect(xButton,yButton+120,450,42),"03  /  CONTROLS")){controls=true;confirmNew=false;}
            if(GUI.Button(new Rect(xButton,yButton+174,450,42),"04  /  QUIT"))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying=false;
#else
                Application.Quit();
#endif
            }
            Label(new Rect(left+18,top+561,500,28),"F6  GRAPHICS     F7  AUDIO     v0.8.5 / WIRE IMPACT",caption,new Color(.42f,.59f,.64f));
            Rect portrait=new Rect(left+630,top+75,430,520);
            GUI.color=new Color(.8f,.95f,1,.9f);
            var hero=AncientArt.Hero(0);Rect tex=hero.textureRect;
            GUI.DrawTextureWithTexCoords(portrait,hero.texture,new Rect(tex.x/hero.texture.width,tex.y/hero.texture.height,tex.width/hero.texture.width,tex.height/hero.texture.height));
            GUI.color=Color.white;
            Label(new Rect(left+650,top+555,430,28),"PARTICLE WIRE  /  CAPTURE  /  COUNTER",caption,AncientGame.Cyan);
            if(controls||confirmNew)
            {
                AncientHUD.Panel(new Rect(0,0,w,h),new Color(0,.01f,.02f,.86f));
                float px=w/2-330,py=h/2-260;
                AncientHUD.Panel(new Rect(px,py,660,520),new Color(.025f,.055f,.08f,.99f));
                Label(new Rect(px+30,py+24,600,45),confirmNew?"START A NEW RUN?":"FIELD MANUAL",heading,Color.white);
                string text=confirmNew?"This replaces your story, civic points, cores, upgrades and checkpoint. Audio and graphics settings are kept.":
                    "A / D or arrows   Move\nSpace   Jump\nCtrl   Toggle run / E   Ride, park or use the lift\n\nWHEEL   Up: ATTACK / down: WIRE\nWIRE   Left click: pull to terrain or enemy / middle: terrain swing\nBOTH MODES   Hold RIGHT click: collect loose particles\nATTACK   LEFT tap: blade / hold and release: laser\nCapture energy increases damage and effect size (3 stages).\n\nF   Dismantle thrown objects / RIGHT click collects particles\nX   Motorcycle explosion     S / down   Brake\nK / Esc   Pause / upgrades     Space / Enter   Advance dialogue";
                Label(new Rect(px+30,py+85,600,350),text,body,new Color(.75f,.86f,.9f));
                if(confirmNew)
                {
                    if(GUI.Button(new Rect(px+30,py+440,290,45),"REPLACE SAVE / NEW GAME")){confirmNew=false;game.NewRun();}
                    if(GUI.Button(new Rect(px+340,py+440,290,45),"CANCEL"))confirmNew=false;
                }
                else if(GUI.Button(new Rect(px+30,py+455,600,40),"BACK / ESC"))controls=false;
            }
            GUI.skin.button=oldButton;GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.depth=oldDepth;
        }
    }
}
