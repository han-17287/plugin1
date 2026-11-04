using UnityEngine;
namespace Plugin.AncientCivilization
{
    public sealed class AncientJournal : MonoBehaviour
    {
        AncientGame game;public bool Open {get;private set;}
        GUIStyle title,text,small;int tab,evidencePage;
        static readonly string[] names={"점검 제외 물량","운송장 수령처","회수품과 용의자","층간 관제 코드","특별 승인 구역","옛 동료의 사격","진료소 배정 기록","운송장 원본 대조","재배치 물량"};
        static readonly string[] entries={
            "C-00 배정 코어는 현지 순찰 점검 대상에서 제외된다. 누가 승인한 물량인가?",
            "도난 코어 운송장에는 수량이 있지만 최종 수령처가 가려져 있다. 단순 누락인지 확인해야 한다.",
            "회수품과 용의자가 C-00 승인으로 별도 이송됐다. 통신관이 원본을 열지 못해 사본을 보관했다.",
            "첫 윗층의 엘리베이터에서도 C-00이 보인다. 생산소 배정과 이동 관제가 같은 체계인가?",
            "최상층은 포인트를 모아도 승진으로 진입할 수 없다. 상층 거주구 동쪽의 외벽 샛길을 이용한다.",
            "샛길의 저격수는 옛 동료였다. 서로의 목적을 확인하고 각자 움직이기로 했다.",
            "비상 전력을 복구한 진료소 기록에 따르면, 코어 미수령은 도난 사건 전부터 시작됐다.",
            "원본 캐시와 사본을 대조했다. 수량은 같은데 C-00 승인 뒤 수령처만 지워졌다.",
            "재배치 명단과 하층 배정 물량의 번호가 같다. 코어는 주민 배급이 아니라 최상층 직할 운송에 묶였다."};
        public void Initialize(AncientGame owner){game=owner;}
        public void Close(){Open=false;game.BlockGameplayInput();Time.timeScale=game.Paused?0:1;}
        void Update()
        {
            if(game.MainMenu||game.skills||game.Story.DialogueOpen||game.Story.LiftBusy||(game.Finale&&game.Finale.Cinematic))return;
            if(Input.GetKeyDown(KeyCode.Tab))
            {Open=!Open;game.BlockGameplayInput();Time.timeScale=game.Paused?0:1;}
            else if(Open&&Input.GetKeyDown(KeyCode.Escape))Close();
        }
        void OnGUI()
        {
            if(!Open||game.MainMenu)return;
            if(text==null)
            {
                text=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=18,wordWrap=true};
                title=new GUIStyle(text){fontSize=27,fontStyle=FontStyle.Bold};small=new GUIStyle(text){fontSize=14};
            }
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;GUI.depth=-90;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float w=Screen.width/scale,h=Screen.height/scale,x=w/2-470,y=h/2-300;
            AncientHUD.Panel(new Rect(0,0,w,h),new Color(.005f,.015f,.025f,.94f));
            AncientHUD.Panel(new Rect(x,y,940,600),new Color(.02f,.045f,.065f,.99f));
            GUI.Label(new Rect(x+24,y+20,850,44),"순찰 기록 / "+AncientCityLayout.Name(game.Story.CurrentFloor),title);
            if(GUI.Button(new Rect(x+24,y+80,210,38),"수사 단서",AncientHUD.Button))tab=0;
            if(GUI.Button(new Rect(x+248,y+80,210,38),"구역 / 탐색",AncientHUD.Button))tab=1;
            if(GUI.Button(new Rect(x+472,y+80,210,38),"현장 조작",AncientHUD.Button))tab=2;
            if(tab==0)
            {
                float row=y+136;
                for(int i=evidencePage*6;i<Mathf.Min(entries.Length,evidencePage*6+6);i++)
                {
                    bool known=AncientProgressRules.KnowsEvidence(i,game.Story.Stage,game.City.Cleared,game.Cases.CompleteMask);
                    GUI.color=known?new Color(.8f,.93f,.95f):new Color(.4f,.5f,.55f);
                    GUI.Label(new Rect(x+28,row,880,24),(known?"● ":"○ ")+(known?names[i]:"미확인 단서"),small);
                    GUI.Label(new Rect(x+28,row+25,880,38),known?entries[i]:"조사와 사건 진행을 통해 확인합니다.",small);row+=58;
                }
                if(GUI.Button(new Rect(x+28,y+503,170,30),"이전 기록",AncientHUD.Button))evidencePage=0;
                if(GUI.Button(new Rect(x+214,y+503,170,30),"다음 기록",AncientHUD.Button))evidencePage=1;
                GUI.Label(new Rect(x+410,y+508,200,26),(evidencePage+1)+" / 2",small);
            }
            else if(tab==1)
            {
                for(int floor=0;floor<=4;floor++)
                {
                    bool current=floor==game.Story.CurrentFloor;
                    bool visited=floor==0||floor==1&&game.Story.Stage>=9||(game.City.Cleared&(1<<floor))!=0||current;
                    string state=current?"현재 위치":floor==4?"정식 출입 불가":AncientCityLayout.CanLift(game.Story.Stage,game.Story.ResidencePoints,game.City.Cleared,game.Story.CurrentFloor,floor)?"엘리베이터 해금":"통행 잠김";
                    string line=AncientCityLayout.Name(floor)+" / "+state+" / 보급품 "+AncientProgressRules.CachesOnFloor(game.Exploration.CacheMask,floor)+" / 2";
                    GUI.color=current?new Color(1,.85f,.2f):visited?Color.white:new Color(.5f,.6f,.65f);
                    GUI.Label(new Rect(x+28,y+145+floor*64,880,50),line,text);
                }
                GUI.color=Color.white;GUI.Label(new Rect(x+28,y+477,880,44),"목적지 이동은 실제 엘리베이터에서 합니다. 최상층은 동쪽 샛길로 침입합니다.",small);
            }
            else
            {
                GUI.Label(new Rect(x+28,y+145,880,350),
                    "이동 A/D · 점프 Space · 달리기 Ctrl 토글\n휠 위 공격 / 휠 아래 와이어\n와이어 좌클릭 견인 · 가운데 클릭 스윙\n공격 좌클릭 짧게 베기 · 길게 조준 후 놓아 레이저\n우클릭은 파괴된 물체의 입자 수집\n공중 부유체를 베면 반동 · 보스의 금색 빈틈에는 피해 증가\nE 오토바이 / 엘리베이터 / 현장 상호작용\nK/Esc 정비 메뉴 · Tab 순찰 기록\nF6 효과 품질 · F7 소리 · F8 프레임 목표 · F9 성능 표시",text);
            }
            GUI.color=Color.white;
            if(GUI.Button(new Rect(x+24,y+545,890,36),"닫기 / Tab 또는 Esc",AncientHUD.Button))Close();
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
    }
}
