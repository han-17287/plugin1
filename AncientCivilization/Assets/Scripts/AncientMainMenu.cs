using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientMainMenu : MonoBehaviour
    {
        const int BackgroundCount=6;
        const string PreviousBackgroundKey="PLUGIN.Presentation.MenuBackdrop";
        static int sessionBackground=-1;
        static Texture2D bladePhoto;
        static readonly string[] backdropCaptions={"입자 블레이드","생산구의 밤","꺼지지 않은 불빛","보관된 기록","천장 아래의 정원","침묵의 통제실"};
        AncientGame game;bool controls,confirmNew,backgroundReady;Sprite backdrop;
        GUIStyle logo,heading,body,caption,primary,secondary;
        Color accent;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLaunch(){sessionBackground=-1;}
        void Awake(){game=GetComponent<AncientGame>();}
        void EnsureBackground()
        {
            if(backgroundReady)return;
            if(sessionBackground<0)
            {
                // Presentation preference survives a new game, while QA never changes the normal preference.
                int previous=AncientGame.IsQAProfile?-1:PlayerPrefs.GetInt(PreviousBackgroundKey,-1);
                if(AncientGame.IsQAProfile)sessionBackground=0;
                else if(previous>=0&&previous<BackgroundCount)
                {int roll=Random.Range(0,BackgroundCount-1);sessionBackground=roll>=previous?roll+1:roll;}
                else sessionBackground=Random.Range(0,BackgroundCount);
                if(!AncientGame.IsQAProfile){PlayerPrefs.SetInt(PreviousBackgroundKey,sessionBackground);PlayerPrefs.Save();}
            }
            if(sessionBackground==0)
            {
                if(!bladePhoto)bladePhoto=Resources.Load<Texture2D>("Art/MenuBlade");
                if(bladePhoto){bladePhoto.filterMode=FilterMode.Bilinear;bladePhoto.wrapMode=TextureWrapMode.Clamp;}
            }
            else backdrop=AncientArt.DistrictBackdrop(sessionBackground-1);
            accent=sessionBackground==0||sessionBackground==2||sessionBackground==4?new Color(1,.8f,.35f):new Color(.35f,.86f,1);
            backgroundReady=true;
        }
        void Update()
        {
            if(!game.MainMenu||AncientBootIntro.Active)return;
            if(Input.GetKeyDown(KeyCode.Escape)){controls=false;confirmNew=false;}
        }
        void Styles()
        {
            if(logo!=null)return;
            logo=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=82,fontStyle=FontStyle.Bold,padding=new RectOffset(0,0,0,0)};
            heading=new GUIStyle(logo){fontSize=27};
            body=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=16,wordWrap=true,padding=new RectOffset(0,0,0,0)};
            caption=new GUIStyle(body){fontSize=12};
            primary=new GUIStyle(AncientHUD.Button){fontSize=20,fontStyle=FontStyle.Bold,padding=new RectOffset(20,16,8,8)};
            secondary=new GUIStyle(AncientHUD.Button){fontSize=16,padding=new RectOffset(20,16,6,6)};
        }
        static void Fill(Rect rect,Color color)
        {GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
        void Label(Rect rect,string text,GUIStyle style,Color color)
        {GUI.color=color;GUI.Label(rect,text,style);GUI.color=Color.white;}
        void DrawBackground(float w,float h)
        {
            Fill(new Rect(0,0,w,h),new Color(.018f,.028f,.045f));
            if(sessionBackground==0&&bladePhoto)
            {
                // The original is 2:1. Fit its full image so the long glowing blade's tip is never cropped.
                float fit=Mathf.Min(w/bladePhoto.width,h/bladePhoto.height);
                float width=bladePhoto.width*fit,height=bladePhoto.height*fit;
                GUI.color=Color.white;GUI.DrawTexture(new Rect((w-width)*.5f,(h-height)*.5f,width,height),bladePhoto,ScaleMode.StretchToFill);
                Fill(new Rect(0,0,w,h),new Color(.005f,.012f,.023f,.12f));
            }
            else if(backdrop)
            {
                Rect uv=backdrop.textureRect;float sourceAspect=uv.width/uv.height,targetAspect=w/h;
                uv=new Rect(uv.x/backdrop.texture.width,uv.y/backdrop.texture.height,uv.width/backdrop.texture.width,uv.height/backdrop.texture.height);
                if(targetAspect>sourceAspect){float visible=sourceAspect/targetAspect;uv.y+=uv.height*(1-visible)*.5f;uv.height*=visible;}
                else {float visible=targetAspect/sourceAspect;uv.x+=uv.width*(1-visible)*.5f;uv.width*=visible;}
                GUI.color=Color.white;GUI.DrawTextureWithTexCoords(new Rect(0,0,w,h),backdrop.texture,uv);
                Fill(new Rect(0,0,w,h),new Color(.005f,.012f,.023f,.36f));
            }
            // Subtle atmosphere stays active on the paused title screen without changing the image selection.
            for(int i=0;i<14;i++)
            {
                float x=Mathf.Repeat(i*127.7f+Time.unscaledTime*(2+i%3),w);
                float y=Mathf.Repeat(i*83.3f-Time.unscaledTime*(3+i%4),h);
                Fill(new Rect(x,y,2,2),new Color(.25f,.75f,1,.10f+(i%3)*.035f));
            }
            for(float y=0;y<h;y+=36)Fill(new Rect(0,y,w,1),new Color(0,.01f,.02f,.025f));
        }
        void OnGUI()
        {
            if(!game||!game.MainMenu||AncientBootIntro.Active)return;
            EnsureBackground();Styles();
            var oldMatrix=GUI.matrix;var oldColor=GUI.color;int oldDepth=GUI.depth;bool oldEnabled=GUI.enabled;
            var oldButton=GUI.skin.button;
            try
            {
                GUI.depth=-100;GUI.skin.button=secondary;
                float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
                GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
                float w=Screen.width/scale,h=Screen.height/scale;
                DrawBackground(w,h);
                float left=Mathf.Max(48,w*.05f),top=(h-608)*.5f;
                var rail=new Rect(left,top,476,608);
                Fill(new Rect(rail.x+7,rail.y+8,rail.width,rail.height),new Color(0,0,0,.18f));
                Fill(rail,new Color(.012f,.025f,.038f,.90f));
                Fill(new Rect(rail.x,rail.y,3,rail.height),new Color(accent.r,accent.g,accent.b,.75f));
                float x=left+30;
                Label(new Rect(x,top+25,416,24),"HWASAN GAMES  /  STORY ACTION",caption,accent);
                Label(new Rect(x-3,top+61,426,100),"PLUGIN",logo,Color.white);
                Label(new Rect(x,top+158,426,40),"ANCIENT CIVILIZATION",heading,new Color(.88f,.94f,.96f));
                Fill(new Rect(x,top+211,64,2),accent);
                Label(new Rect(x,top+235,408,62),"백 경위의 순찰은 끝나지 않았다.\n사라진 코어와 지워진 기록을 쫓아라.",body,new Color(.64f,.75f,.80f));
                float buttons=top+324;bool canContinue=game.HasSession||game.HasSavedProgress;
                Fill(new Rect(x-4,buttons,3,52),accent);
                if(GUI.Button(new Rect(x,buttons,416,52),canContinue?(game.HasSession?"01  /  계속하기":"01  /  이어하기"):"01  /  새 순찰 시작",primary))
                {controls=confirmNew=false;game.BeginSession();}
                GUI.enabled=oldEnabled&&canContinue;
                if(GUI.Button(new Rect(x,buttons+64,416,42),"02  /  새 게임",secondary)){confirmNew=true;controls=false;}
                GUI.enabled=oldEnabled;
                if(GUI.Button(new Rect(x,buttons+116,416,42),"03  /  조작 안내",secondary)){controls=true;confirmNew=false;}
                if(GUI.Button(new Rect(x,buttons+168,416,42),"04  /  종료",secondary))
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying=false;
#else
                    Application.Quit();
#endif
                }
                Label(new Rect(x,top+561,420,23),"F6 효과   F7 소리   F8 프레임 목표  /  v0.15.0",caption,new Color(.4f,.53f,.59f));
                Label(new Rect(w-450,h-47,402,24),backdropCaptions[sessionBackground]+"  /  기록은 흔적을 남긴다",caption,new Color(.67f,.79f,.84f));
                if(controls||confirmNew)DrawModal(w,h);
            }
            finally {GUI.skin.button=oldButton;GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.depth=oldDepth;GUI.enabled=oldEnabled;}
        }
        void DrawModal(float w,float h)
        {
            Fill(new Rect(0,0,w,h),new Color(.002f,.008f,.015f,.88f));
            float px=w/2-340,py=h/2-270;
            Fill(new Rect(px,py,680,540),new Color(.02f,.045f,.065f,.99f));
            Fill(new Rect(px,py,680,2),accent);
            Label(new Rect(px+30,py+25,620,42),confirmNew?"새 게임을 시작할까요?":"현장 조작 안내",heading,Color.white);
            string text=confirmNew?"스토리·시민 포인트·코어·업그레이드·체크포인트를 새로 시작합니다. 소리와 그래픽 설정은 유지합니다.":
                "A/D 또는 방향키 이동 · Space 점프 · Ctrl 달리기\nE 대화·현장 조사·엘리베이터\n\n휠 위 공격 / 휠 아래 와이어\n와이어 좌클릭: 직선 견인 · 가운데 클릭: 스윙\n공격 좌클릭: 짧게 베기 · 길게 조준 후 놓아 레이저\n우클릭: 파괴되어 나온 파란 입자 수집\n\n추격전: E 오토바이 탑승/하차 · S/↓ 제동 · X 폭발 도약\n대화: Space/Enter 전체 표시 · 다음 말풍선\nTab 기록 · K/Esc 정비 · F 물체 분해\nF6 효과 · F7 소리 · F8 프레임 목표 · F9 성능 표시";
            Label(new Rect(px+30,py+85,620,365),text,body,new Color(.74f,.85f,.90f));
            if(confirmNew)
            {
                if(GUI.Button(new Rect(px+30,py+461,300,46),"기존 진행을 지우고 새 게임",secondary)){confirmNew=false;game.NewRun();}
                if(GUI.Button(new Rect(px+350,py+461,300,46),"취소",secondary))confirmNew=false;
            }
            else if(GUI.Button(new Rect(px+30,py+475,620,40),"뒤로 / Esc",secondary))controls=false;
        }
    }
}
