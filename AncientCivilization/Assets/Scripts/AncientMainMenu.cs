using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientMainMenu : MonoBehaviour
    {
        const int BackgroundCount=9;
        const string PreviousBackgroundKey="PLUGIN.Presentation.MenuBackdrop";
        static int sessionBackground=-1;
        static readonly Texture2D[] photographs=new Texture2D[4];
        static readonly string[] photoNames={"MenuBlade","MenuOfficer","MenuStandoff","MenuSlash"};
        static readonly string[] backdropCaptions={"입자 블레이드","생산구의 밤","꺼지지 않은 불빛","보관된 기록","천장 아래의 정원","침묵의 통제실","노란 빛의 경위","조우","돌파"};
        AncientGame game;bool controls,confirmNew,backgroundReady;Sprite backdrop;Texture2D photograph;
        GUIStyle logo,heading,body,caption,eyebrow,modalHeading,primary,secondary;
        Texture2D sideShade,edgeShade,softLight,buttonRest,buttonHover,buttonPress;
        Color accent;float backgroundStarted;string backdropCaption;
        static readonly Color White=new Color(.94f,.97f,.98f),Muted=new Color(.61f,.73f,.78f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLaunch(){sessionBackground=-1;}
        void Awake(){game=GetComponent<AncientGame>();}
        void EnsureBackground()
        {
            if(backgroundReady)return;
            if(sessionBackground<0)
            {
                // This presentation preference survives new games; isolated QA never changes it.
                int previous=AncientGame.IsQAProfile?-1:PlayerPrefs.GetInt(PreviousBackgroundKey,-1);
                if(AncientGame.IsQAProfile)sessionBackground=6;
                else if(previous>=0&&previous<BackgroundCount)
                {int roll=Random.Range(0,BackgroundCount-1);sessionBackground=roll>=previous?roll+1:roll;}
                else sessionBackground=Random.Range(0,BackgroundCount);
                if(!AncientGame.IsQAProfile){PlayerPrefs.SetInt(PreviousBackgroundKey,sessionBackground);PlayerPrefs.Save();}
            }
            if(sessionBackground==0||sessionBackground>=6)
            {
                int image=sessionBackground==0?0:sessionBackground-5;
                if(!photographs[image])photographs[image]=Resources.Load<Texture2D>("Art/"+photoNames[image]);
                photograph=photographs[image];
                if(photograph){photograph.filterMode=FilterMode.Bilinear;photograph.wrapMode=TextureWrapMode.Clamp;}
            }
            else backdrop=AncientArt.DistrictBackdrop(sessionBackground-1);
            accent=sessionBackground==0||sessionBackground==2||sessionBackground==4||sessionBackground>=6?new Color(1,.80f,.37f):new Color(.38f,.88f,1);
            backdropCaption=backdropCaptions[sessionBackground]+"  ·  기록은 흔적을 남긴다";
            backgroundReady=true;backgroundStarted=Time.unscaledTime;
        }
        void Update()
        {
            if(!game.MainMenu||AncientBootIntro.Active)return;
            if(Input.GetKeyDown(KeyCode.Escape)){controls=false;confirmNew=false;}
        }
        static Texture2D Surface(string name,Color tint)
        {
            var texture=new Texture2D(1,1,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear};
            texture.SetPixel(0,0,tint);texture.Apply(false,true);return texture;
        }
        void Styles()
        {
            if(logo!=null)return;
            // Smooth Latin title lettering sits alongside the bundled Korean font.
            var latin=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            logo=new GUIStyle(GUI.skin.label){font=latin?latin:AncientKoreanUI.Font,fontSize=92,fontStyle=FontStyle.Bold,padding=new RectOffset(0,0,0,0)};
            heading=new GUIStyle(logo){fontSize=22,fontStyle=FontStyle.Normal};
            body=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=16,wordWrap=true,padding=new RectOffset(0,0,0,0)};
            caption=new GUIStyle(body){fontSize=12};eyebrow=new GUIStyle(heading){fontSize=11};
            modalHeading=new GUIStyle(body){fontSize=25,fontStyle=FontStyle.Bold};
            buttonRest=Surface("Menu resting button",new Color(1,1,1,.025f));
            buttonHover=Surface("Menu highlighted button",new Color(1,1,1,.105f));
            buttonPress=Surface("Menu pressed button",new Color(1,1,1,.16f));
            secondary=new GUIStyle(GUI.skin.button){font=body.font,fontSize=17,alignment=TextAnchor.MiddleLeft,border=new RectOffset(0,0,0,0),padding=new RectOffset(20,34,6,6)};
            secondary.normal.background=buttonRest;secondary.hover.background=buttonHover;
            secondary.active.background=buttonPress;secondary.focused.background=buttonHover;
            secondary.normal.textColor=new Color(.74f,.82f,.85f);secondary.hover.textColor=secondary.focused.textColor=secondary.active.textColor=Color.white;
            primary=new GUIStyle(secondary){fontSize=21,fontStyle=FontStyle.Bold};primary.normal.textColor=Color.white;
            const int count=256;var shadePixels=new Color32[count];var edgePixels=new Color32[count];
            for(int i=0;i<count;i++)
            {
                float t=i/(count-1f);
                shadePixels[i]=new Color(.004f,.011f,.02f,Mathf.Pow(1-t,1.55f)*.99f);
                edgePixels[i]=new Color(.002f,.008f,.016f,Mathf.Pow(Mathf.Abs(t*2-1),3)*.72f);
            }
            sideShade=new Texture2D(count,1,TextureFormat.RGBA32,false){name="Menu reading gradient",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            sideShade.SetPixels32(shadePixels);sideShade.Apply(false,true);
            edgeShade=new Texture2D(1,count,TextureFormat.RGBA32,false){name="Menu letterbox vignette",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            edgeShade.SetPixels32(edgePixels);edgeShade.Apply(false,true);
            const int size=64;var lightPixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=(x+.5f)/size*2-1,dy=(y+.5f)/size*2-1;
                lightPixels[y*size+x]=new Color(1,1,1,Mathf.Exp(-(dx*dx+dy*dy)*5)*Mathf.Clamp01((1-dx*dx-dy*dy)*3));
            }
            softLight=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Menu floating light",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            softLight.SetPixels32(lightPixels);softLight.Apply(false,true);
        }
        static void Fill(Rect rect,Color color)
        {GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
        static void Label(Rect rect,string text,GUIStyle style,Color color)
        {GUI.color=color;GUI.Label(rect,text,style);GUI.color=Color.white;}
        void Glow(Rect rect,Color color)
        {GUI.color=color;GUI.DrawTexture(rect,softLight);GUI.color=Color.white;}
        void DrawPhotograph(float w,float h)
        {
            float fit=Mathf.Min(w/photograph.width,h/photograph.height),width=photograph.width*fit,height=photograph.height*fit;
            // A dim cover fills unusual aspect ratios; the sharp foreground remains fully fitted.
            float cover=Mathf.Max(w/photograph.width,h/photograph.height);
            Rect uv=sessionBackground>=6?new Rect(1,0,-1,1):new Rect(0,0,1,1);
            GUI.color=new Color(.42f,.50f,.58f,.32f);
            GUI.DrawTextureWithTexCoords(new Rect((w-photograph.width*cover)*.5f,(h-photograph.height*cover)*.5f,photograph.width*cover,photograph.height*cover),photograph,uv);
            float age=Mathf.Max(0,Time.unscaledTime-backgroundStarted);
            float drift=sessionBackground==0?0:Mathf.Sin(age*.13f)*3;
            // Mirror only at draw time so the officer stands right of the reading area.
            // Source PNGs and the saved reference stay unchanged; no zoom clips the tip.
            GUI.color=Color.white;
            GUI.DrawTextureWithTexCoords(new Rect((w-width)*.5f+drift,(h-height)*.5f,width,height),photograph,uv);
        }
        void DrawBackground(float w,float h)
        {
            Fill(new Rect(0,0,w,h),new Color(.009f,.019f,.031f));
            if(photograph)DrawPhotograph(w,h);
            else if(backdrop)
            {
                Rect uv=backdrop.textureRect;float sourceAspect=uv.width/uv.height,targetAspect=w/h;
                uv=new Rect(uv.x/backdrop.texture.width,uv.y/backdrop.texture.height,uv.width/backdrop.texture.width,uv.height/backdrop.texture.height);
                if(targetAspect>sourceAspect){float visible=sourceAspect/targetAspect;uv.y+=uv.height*(1-visible)*.5f;uv.height*=visible;}
                else {float visible=targetAspect/sourceAspect;uv.x+=uv.width*(1-visible)*.5f;uv.width*=visible;}
                GUI.color=Color.white;GUI.DrawTextureWithTexCoords(new Rect(0,0,w,h),backdrop.texture,uv);
                Fill(new Rect(0,0,w,h),new Color(.005f,.012f,.023f,.12f));
            }
            float time=Time.unscaledTime;
            Glow(new Rect(w*.57f,h*.25f,w*.45f,h*.72f),new Color(.1f,.65f,1,.035f+.012f*Mathf.Sin(time*.9f)));
            Glow(new Rect(w*.58f,h*.52f,w*.40f,h*.52f),new Color(1,.67f,.18f,.025f+.009f*Mathf.Sin(time*.65f+1)));
            for(int i=0;i<22;i++)
            {
                float x=Mathf.Repeat(i*173.77f+time*(1.7f+i%3),w),y=Mathf.Repeat(i*117.3f-time*(2+i%4),h);
                float radius=3+i%3;Glow(new Rect(x,y,radius*2,radius*2),new Color(.42f,.82f,1,.10f+(i%4)*.025f));
            }
            GUI.color=Color.white;GUI.DrawTexture(new Rect(0,0,Mathf.Min(w*.76f,1000),h),sideShade);
            GUI.DrawTexture(new Rect(0,0,w,h),edgeShade);
            Fill(new Rect(0,0,w,1),new Color(accent.r,accent.g,accent.b,.35f));
        }
        bool MenuButton(Rect rect,string text,bool important=false)
        {
            bool hover=GUI.enabled&&rect.Contains(Event.current.mousePosition);
            if(important)
            {
                Fill(rect,new Color(accent.r,accent.g,accent.b,hover?.14f:.075f));
                Fill(new Rect(rect.x,rect.y,3,rect.height),new Color(accent.r,accent.g,accent.b,hover?1:.8f));
            }
            if(hover&&!important)Fill(new Rect(rect.x,rect.y,2,rect.height),new Color(accent.r,accent.g,accent.b,.7f));
            bool clicked=GUI.Button(rect,text,important?primary:secondary);
            Label(new Rect(rect.xMax-36,rect.y+9,22,30),"▶",body,!GUI.enabled?new Color(.25f,.33f,.37f):hover?accent:new Color(.48f,.62f,.68f));
            return clicked;
        }
        void OnGUI()
        {
            if(!game||!game.MainMenu||AncientBootIntro.Active)return;
            EnsureBackground();Styles();
            var oldMatrix=GUI.matrix;var oldColor=GUI.color;int oldDepth=GUI.depth;bool oldEnabled=GUI.enabled;var oldButton=GUI.skin.button;
            try
            {
                GUI.depth=-100;GUI.skin.button=secondary;
                float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);scale=Mathf.Max(.05f,scale);
                GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
                DrawBackground(w,h);
                float x=Mathf.Max(54,w*.052f),top=Mathf.Max(44,(h-622)*.5f),width=354;
                Label(new Rect(x+2,top+2,width,20),"H W A S A N   G A M E S",eyebrow,accent);
                Label(new Rect(x-5,top+34,435,116),"PLUGIN",logo,White);
                Label(new Rect(x+2,top+145,440,30),"ANCIENT CIVILIZATION",heading,new Color(.80f,.88f,.92f));
                Fill(new Rect(x+2,top+197,52,2),accent);
                Label(new Rect(x+2,top+224,350,58),"사라진 코어. 지워진 기록.\n백 경위의 순찰은 끝나지 않았다.",body,Muted);
                float buttons=top+319;bool canContinue=game.HasSession||game.HasSavedProgress;
                if(MenuButton(new Rect(x,buttons,width,52),canContinue?(game.HasSession?"계속하기":"이어하기"):"새 순찰 시작",true))
                {controls=confirmNew=false;game.BeginSession();}
                GUI.enabled=oldEnabled&&canContinue;
                if(MenuButton(new Rect(x,buttons+64,width,44),"새 게임")){confirmNew=true;controls=false;}
                GUI.enabled=oldEnabled;
                if(MenuButton(new Rect(x,buttons+116,width,44),"조작 안내")){controls=true;confirmNew=false;}
                if(MenuButton(new Rect(x,buttons+168,width,44),"종료"))
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying=false;
#else
                    Application.Quit();
#endif
                }
                Label(new Rect(x+2,top+564,410,24),"F6 효과   F7 소리   F8 프레임 목표",caption,new Color(.48f,.61f,.67f));
                Label(new Rect(x+2,h-39,420,20),"HWASAN GAMES  /  v0.25.0",eyebrow,new Color(.38f,.50f,.57f));
                Label(new Rect(w-334,h-39,280,22),backdropCaption,caption,new Color(.55f,.68f,.73f));
                if(controls||confirmNew)DrawModal(w,h);
            }
            finally {GUI.skin.button=oldButton;GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.depth=oldDepth;GUI.enabled=oldEnabled;}
        }
        void DrawModal(float w,float h)
        {
            Fill(new Rect(0,0,w,h),new Color(.002f,.008f,.015f,.92f));float px=w/2-340,py=h/2-270;
            Fill(new Rect(px,py,680,540),new Color(.018f,.037f,.055f,.99f));Fill(new Rect(px,py,680,2),accent);
            Label(new Rect(px+30,py+25,620,42),confirmNew?"새 게임을 시작할까요?":"현장 조작 안내",modalHeading,White);
            string text=confirmNew?"스토리·시민 포인트·코어·업그레이드·체크포인트를 새로 시작합니다. 소리와 그래픽 설정은 유지합니다.":
                    "A/D 또는 방향키 이동 · Space 점프 · Ctrl 달리기\nE 대화·현장 조사·엘리베이터\n\n휠 위 공격 / 휠 아래 와이어\n와이어 좌클릭: 직선 견인 · 가운데 클릭: 스윙\n공격 좌클릭: 짧게 베기 · 길게 조준 후 놓아 레이저\n우클릭: 파괴되어 나온 파란 입자 수집\n\n초반 추격: 자동 전진 · Space 점프 · 좌클릭 와이어 승선\n마지막 추격: E 탑승/하차 · S/▼ 제동 · X 폭발 도약\n대화: Space/Enter 전체 표시 · 다음 말풍선\nTab 기록 · K/Esc 정비\nF6 효과 · F7 소리 · F8 프레임 목표 · F9 성능 표시";
            Label(new Rect(px+30,py+85,620,365),text,body,new Color(.74f,.85f,.90f));
            if(confirmNew)
            {
                if(MenuButton(new Rect(px+30,py+461,300,46),"진행을 지우고 새 게임",true)){confirmNew=false;game.NewRun();}
                if(MenuButton(new Rect(px+350,py+461,300,46),"취소"))confirmNew=false;
            }
            else if(MenuButton(new Rect(px+30,py+475,620,40),"뒤로 / Esc"))controls=false;
        }
        void OnDestroy()
        {
            foreach(var texture in new[]{sideShade,edgeShade,softLight,buttonRest,buttonHover,buttonPress})if(texture)Destroy(texture);
        }
    }
}
