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
            Label(new Rect(left+18,top+22,500,28),"도시 1회차 / 아래에서 시작된 사건",caption,AncientGame.Cyan);
            Label(new Rect(left+12,top+55,520,92),"PLUGIN",logo,Color.white);
            Label(new Rect(left+18,top+153,510,45),"ANCIENT CIVILIZATION",heading,Color.white);
            Label(new Rect(left+18,top+208,480,62),"하층 치안경찰의 순찰. 사라진 에너지 코어. 검거한 사건의 기록은 왜 가려져 있을까.",body,new Color(.65f,.77f,.81f));
            float xButton=left+18,yButton=top+298;
            bool canContinue=game.HasSession||game.HasSavedProgress;
            GUI.enabled=canContinue;
            if(GUI.Button(new Rect(xButton,yButton,450,48),game.HasSession?"01  /  계속하기":"01  /  이어하기"))
            {controls=confirmNew=false;game.BeginSession();}
            GUI.enabled=true;
            if(GUI.Button(new Rect(xButton,yButton+60,450,48),canContinue?"02  /  새 게임":"02  /  시작하기"))
            {if(canContinue)confirmNew=true;else game.BeginSession();}
            if(GUI.Button(new Rect(xButton,yButton+120,450,42),"03  /  조작 안내")){controls=true;confirmNew=false;}
            if(GUI.Button(new Rect(xButton,yButton+174,450,42),"04  /  종료"))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying=false;
#else
                Application.Quit();
#endif
            }
            Label(new Rect(left+18,top+561,500,28),"F6 효과   F7 소리   v0.10.0 / CITY DESCENT",caption,new Color(.42f,.59f,.64f));
            Rect portrait=new Rect(left+630,top+75,430,520);
            GUI.color=new Color(.8f,.95f,1,.9f);
            var hero=AncientArt.Hero(0);Rect tex=hero.textureRect;
            GUI.DrawTextureWithTexCoords(portrait,hero.texture,new Rect(tex.x/hero.texture.width,tex.y/hero.texture.height,tex.width/hero.texture.width,tex.height/hero.texture.height));
            GUI.color=Color.white;
            Label(new Rect(left+650,top+555,430,28),"입자 와이어 / 블레이드 / 레이저",caption,AncientGame.Cyan);
            if(controls||confirmNew)
            {
                AncientHUD.Panel(new Rect(0,0,w,h),new Color(0,.01f,.02f,.86f));
                float px=w/2-330,py=h/2-260;
                AncientHUD.Panel(new Rect(px,py,660,520),new Color(.025f,.055f,.08f,.99f));
                Label(new Rect(px+30,py+24,600,45),confirmNew?"새 게임을 시작할까요?":"현장 조작 안내",heading,Color.white);
                string text=confirmNew?"스토리·시민 포인트·코어·업그레이드·체크포인트를 새로 시작합니다. 소리와 그래픽 설정은 유지합니다.":
                    "A/D 또는 방향키 이동 · Space 점프 · Ctrl 달리기\nE 탑승/주차·엘리베이터·현장 조사\n\n휠 위 공격 / 휠 아래 와이어\n와이어 좌클릭 견인 · 가운데 클릭 스윙\n공격 좌클릭 짧게 베기 · 길게 조준 후 놓아 레이저\n우클릭은 나온 입자 수집 (저장량에 따라 3단계)\n\nF 물체 분해 · X 오토바이 폭발 · S 제동\nTab 순찰 기록 · K/Esc 정비 메뉴\nF6 효과 · F7 소리 · F8 프레임 목표 · F9 성능 표시";
                Label(new Rect(px+30,py+85,600,350),text,body,new Color(.75f,.86f,.9f));
                if(confirmNew)
                {
                    if(GUI.Button(new Rect(px+30,py+440,290,45),"기존 진행을 지우고 새 게임")){confirmNew=false;game.NewRun();}
                    if(GUI.Button(new Rect(px+340,py+440,290,45),"취소"))confirmNew=false;
                }
                else if(GUI.Button(new Rect(px+30,py+455,600,40),"뒤로 / Esc"))controls=false;
            }
            GUI.skin.button=oldButton;GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.depth=oldDepth;
        }
    }
}
