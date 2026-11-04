using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientStory : MonoBehaviour
    {
        public int Stage {get;private set;}
        public bool DialogueOpen {get{return lines.Count>0;}}
        public int CurrentFloor {get;private set;}
        public bool InUpperCity {get{return CurrentFloor>0;}}
        public bool LiftBusy {get{return liftMenu||liftTravelling;}}
        bool liftMenu,liftTravelling;int liftDestination;float liftStarted;
        float LiftX {get{return AncientCityLayout.Entry(CurrentFloor);}}
        public AncientTruck Truck {get;private set;}
        public int ResidencePoints {get;private set;}
        AncientGame game;int flags,lineIndex;float nextTrainingShot,nextLetterAt;int revealedChars;bool showFull;Action afterDialogue;string shownText="";int visibleChars=-1;
        struct Line {public string speaker,text;public int portrait;public Line(string who,string words,int face){speaker=who;text=words;portrait=face;}}
        readonly List<Line> lines=new List<Line>();GUIStyle body,nameStyle,small,nextButton;int spawnedEncounters;
        public void Initialize(AncientGame owner)
        {game=owner;Stage=AncientGame.Saved("questStage");flags=AncientGame.Saved("questFlags");ResidencePoints=AncientGame.Saved("residencePoints");CurrentFloor=Mathf.Clamp(AncientGame.Saved("cityFloor",AncientGame.Saved("upperCity")!=0?1:0),0,4);}
        void Start(){if(game.HasSession)Begin();}
        void Persist()
        {
            AncientGame.SetSaved("questStage",Stage);AncientGame.SetSaved("questFlags",flags);
            AncientGame.SetSaved("residencePoints",ResidencePoints);AncientGame.SetSaved("upperCity",InUpperCity?1:0);AncientGame.SetSaved("cityFloor",CurrentFloor);game.Save();
        }
        void SetStage(int value){Stage=value;Persist();}
        bool Has(int bit){return (flags&(1<<bit))!=0;}
        public void OnTraining(int bit){if(Has(bit))return;flags|=1<<bit;Persist();}
        public void Begin()
        {
            if(Stage==0)
            {
                SetStage(1);
                Talk(null,new Line("통신관","아침 순찰 시작이다. 포인트를 모으면 위로 올라갈 수 있겠지만, 아래 구역의 안전은 우리가 지킨다.",1),
                    new Line("치안경찰","알겠습니다. 기계팔과 입자 와이어, 순찰 오토바이부터 점검하겠습니다.",0),
                    new Line("통신관","생산소의 C-00 예비분은 점검 대상에서 빼라. 정기 배정 물량이라고 한다.",1));
            }
            else if(Stage==4)TheftReport();
            else if(Stage==7)ArrestDialogue();
        }
        void Talk(Action done,params Line[] script)
        {lines.Clear();lines.AddRange(script);lineIndex=0;ResetTyping();afterDialogue=done;game.BlockGameplayInput();}
        void ResetTyping()
        {revealedChars=0;nextLetterAt=Time.unscaledTime+.08f;showFull=false;visibleChars=-1;shownText="";}
        void TickTyping()
        {
            if(showFull||!DialogueOpen||Time.unscaledTime<nextLetterAt)return;
            string text=lines[lineIndex].text;if(revealedChars>=text.Length)return;
            char letter=text[revealedChars++];
            float pause=letter=='.'||letter=='?'||letter=='!'||letter=='…'?.16f:letter==','?.08f:0;
            nextLetterAt=Time.unscaledTime+.045f+pause;
        }
        void Advance()
        {
            if(!DialogueOpen)return;
            if(!showFull&&revealedChars<lines[lineIndex].text.Length){showFull=true;return;}
            lineIndex++;ResetTyping();
            if(lineIndex>=lines.Count)
            {lines.Clear();var action=afterDialogue;afterDialogue=null;game.BlockGameplayInput();action?.Invoke();}
        }
        void TheftReport()
        {
            Talk(()=>SetStage(5),new Line("통신관","긴급 신고다. 상층 출신 용의자가 생산소의 에너지 코어를 트럭에 싣고 도주하고 있다.",1),
                new Line("치안경찰","저 코어가 끊기면 이 구역이 멈춥니다. 주민부터 대피시켜 주세요. 제가 쫓겠습니다.",0),
                new Line("통신관","운송장을 보내겠다. 이상하군… 수량은 있는데 최종 수령처가 비어 있다. 우선 현장부터 확보해라.",1));
        }
        void ArrestDialogue()
        {
            Talk(()=>{SetStage(8);},new Line("치안경찰","당신을 에너지 코어 절도 혐의로 체포한다. 상층에 사는 사람이 왜 아래층의 코어를 훔친 거지?",0),
                new Line("용의자","그 코어, 여기서 쓸 물건이라고 들었지? 운송장 마지막 칸까지 봤어?",2),
                new Line("치안경찰","그걸 대답이라고 하나. 위에서 살 포인트도 있었을 텐데… 이 코어 때문에 전부 걸었다고?",0),
                new Line("통신관","검거 확인. 이번 보상으로 시민 포인트가 "+ResidencePoints+"점에 도달했다. 한 단계 위 도시의 거주·통행 자격이 승인됐다. 이동 엘리베이터에서 목적 층을 선택하면 된다.",1),
                new Line("치안경찰","잠깐, 수령처부터 확인하겠습니다. 용의자도 아직 할 말이 있는 것 같습니다.",0),
                new Line("통신관","회수품과 용의자는 별도 이송으로 바뀌었다. 승인 코드 C-00… 원본 운송장은 내 권한으로도 열리지 않는다. 사본은 남겨 두겠다.",1),
                new Line("치안경찰","점검에서도 빠지고, 수령처도 볼 수 없고… 일단 사본부터 보내 주세요.",0));
        }
        void EnsureTruck(bool atGate=false)
        {
            if(Truck)return;
            Truck=new GameObject("Energy core getaway truck").AddComponent<AncientTruck>();
            Truck.Initialize(game,atGate?106:Mathf.Clamp(game.hero.transform.position.x+8,24,102));
        }
        void SpawnEncounters()
        {
            if(spawnedEncounters!=0)return;spawnedEncounters=1;
            game.SpawnEnemy(34,0,1);game.SpawnEnemy(66,0,0);game.SpawnEnemy(91,0,1);
        }
        void Update()
        {
            if(liftTravelling)
            {
                if(!game.MainMenu&&Time.unscaledTime-liftStarted>=1.6f)FinishLiftTravel();
                return;
            }
            if(liftMenu)
            {
                if(Input.GetKeyDown(KeyCode.Escape)){liftMenu=false;game.BlockGameplayInput();}
                return;
            }
            if(DialogueOpen)
            {if(!game.MainMenu){TickTyping();if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return))Advance();}return;}
            if(game.Paused||!game.HasSession||!game.GameplayInputReady||!game.hero)return;
            var hero=game.hero;
            if(Stage<=3)
            {
                if(hero.transform.position.x>5)OnTraining(0);
                if(Input.GetKeyDown(KeyCode.Space))OnTraining(1);
                if(hero.running)OnTraining(2);
                if(hero.Wired)OnTraining(3);
                if(hero.bike)OnTraining(4);
            }
            if(Stage==1&&AncientRules.TutorialReady(Stage,flags))
            {SetStage(2);Talk(null,new Line("통신관","천장에 입자 와이어를 연결해 봐라. 정비소의 오토바이는 E로 타고 내린다. 확인했으면 블레이드 훈련으로 넘어가자.",1));}
            else if(Stage==2&&AncientRules.TutorialReady(Stage,flags))
            {SetStage(3);Talk(null,new Line("통신관","훈련용 물체를 보낸다. 휠을 위로 돌리고 좌클릭을 짧게 눌러 부숴라. 나온 입자는 우클릭을 누르고 모은다. 좌클릭은 길게 눌러 조준한 뒤, 표시가 금색이 되면 놓아서 레이저를 쏜다.",1));}
            else if(Stage==3)
            {
                if(Time.time>=nextTrainingShot&&game.shots.Count<4)
                {
                    nextTrainingShot=Time.time+1.5f;float side=hero.transform.position.x>12?-1:1;
                    game.Projectiles.Spawn((Vector2)hero.transform.position+new Vector2(side*5,.35f),Vector2.left*side*4,4,true);
                }
                if(AncientRules.TutorialReady(Stage,flags)){SetStage(4);foreach(var shot in game.shots.ToArray())if(shot)shot.Remove();TheftReport();}
            }
            else if(Stage==4)TheftReport();
            else if(Stage==5)
            {
                EnsureTruck();SpawnEncounters();
                if(AncientRules.CanStartMiniBoss(Stage,Truck.Stopped,game.Cores,hero.transform.position.x))
                {SetStage(6);Talk(null,new Line("용의자","다가오지 마! 화물 수호기는 아직 내 통행 권한으로 움직인다고!",2),new Line("치안경찰","코어는 확보했다. 장치를 끄고 트럭에서 내려.",0));}
            }
            else if(Stage==6)
            {
                EnsureTruck(true);
                if(game.bossDefeated)
                {ResidencePoints=AncientRules.PointsAfterArrest(ResidencePoints,Stage);SetStage(7);ArrestDialogue();}
                else if(!game.boss&&hero.transform.position.x>98)
                {game.CombatCheckpoint(0,new Vector2(99.5f,1.3f));game.SpawnEnemy(109,0,3);game.boss.PrepareMiniBoss();game.Notice("미니 보스 / 화물 수호기",3);}
            }
        }
        public bool TryUseLift()
        {
            if((game.Finale&&game.Finale.Active)||CurrentFloor==4||!game.hero||Mathf.Abs(game.hero.transform.position.x-LiftX)>2.5f)return false;
            if(DialogueOpen||LiftBusy)return true;
            if(game.hero.bike){game.Notice("오토바이를 주차한 뒤 이동 엘리베이터에 탑승하세요.");return false;}
            if(!game.hero.IsOnGround){game.Notice("지면에 착지한 뒤 엘리베이터를 이용하세요.");return true;}
            liftMenu=true;game.BlockGameplayInput();return true;
        }
        void StartLiftTravel(int destination)
        {
            if(!AncientCityLayout.CanLift(Stage,ResidencePoints,game.City.Cleared,CurrentFloor,destination))return;
            liftMenu=false;liftTravelling=true;liftDestination=destination;liftStarted=Time.unscaledTime;
            game.hero.TravelTo(new Vector2(LiftX,1.3f));game.BlockGameplayInput();game.Sound(2);
        }
        void FinishLiftTravel()
        {
            bool firstArrival=liftDestination==1&&Stage==8;
            CurrentFloor=liftDestination;if(firstArrival)Stage=9;
            game.hero.TravelTo(new Vector2(LiftX,1.3f));game.SetLiftCheckpoint(CurrentFloor);game.City.OnEnterFloor();
            Persist();game.SnapCameraToHero();liftTravelling=false;game.BlockGameplayInput();
            game.Notice(InUpperCity?"상층 도시 도착 / 같은 엘리베이터로 하층에 돌아갈 수 있습니다.":"하층 도시 도착 / 상층 출입 권한은 유지됩니다.",4);
            if(firstArrival)Talk(null,new Line("통신관","상층 도착. 귀환할 때도 이동 엘리베이터를 이용해라. 하층 출입 권한도 유지된다.",1),
                new Line("치안경찰","엘리베이터 관제에도 C-00이 찍혀 있군. 생산소 배정 번호인 줄 알았는데.",0),
                new Line("통신관","공용 설비 규격일 수도 있다. 사본은 보관했다. 이곳을 조사하고 아래에서도 확인해 보자.",1));
        }
        void DrawLift(float w,float h)
        {
            AncientHUD.Panel(new Rect(0,0,w,h),new Color(.005f,.015f,.025f,.93f));
            float x=w/2-300,y=h/2-255;
            AncientHUD.Panel(new Rect(x,y,600,510),new Color(.025f,.055f,.08f,.99f));
            GUI.Label(new Rect(x+28,y+22,544,38),"도시 간 이동 엘리베이터",nameStyle);
            if(liftTravelling)
            {
                GUI.Label(new Rect(x+28,y+98,544,80),(liftDestination>CurrentFloor?"상승 중…":"하강 중…")+" / "+AncientCityLayout.Name(liftDestination),body);
                float progress=Mathf.Clamp01((Time.unscaledTime-liftStarted)/1.6f);
                AncientHUD.Panel(new Rect(x+28,y+212,544*progress,6),AncientGame.Cyan);return;
            }
            GUI.Label(new Rect(x+28,y+68,544,48),"관제 C-00 / "+AncientCityLayout.Name(CurrentFloor)+" / "+ResidencePoints+"점",small);
            for(int floor=0;floor<=4;floor++)
            {
                bool current=CurrentFloor==floor;
                bool allowed=AncientCityLayout.CanLift(Stage,ResidencePoints,game.City.Cleared,CurrentFloor,floor);
                string text=AncientCityLayout.Name(floor);
                if(current)text+="  [현재 층]";else if(floor==4)text+="  [특별 승인: 승진으로 진입 불가]";else if(!allowed)text+="  [잠김: 이전 구역 사건 해결 필요]";
                GUI.enabled=allowed;
                if(GUI.Button(new Rect(x+28,y+120+floor*57,544,48),text,nextButton))StartLiftTravel(floor);
                GUI.enabled=true;
            }
            if(GUI.Button(new Rect(x+28,y+431,544,42),"닫기 / Esc",nextButton)){liftMenu=false;game.BlockGameplayInput();}
        }
        public void FinaleBriefing(string first,string second)
        {Talk(null,new Line("치안경찰",first,0),
            new Line("회수 계획 주석","기동 준비된 코어는 보호부를 분리하지 말 것. 외부 충격에 의한 보호부 파손은 연쇄 활성화를 유발한다.",1),new Line("통신관",second,1));}
        public void FieldDialogue(string speaker,string words)
        {Talk(null,new Line(speaker,words,1));}
        public void OnDistrictCleared(int floor)
        {
            if(floor<3)ResidencePoints=Mathf.Max(ResidencePoints,(floor+1)*1000);
            Persist();
            if(floor==4)
                Talk(null,new Line("치안경찰","그 사격 자세… 너였어? 그때 같은 구역에서 근무하던….",0),
                    new Line("저격수","이제야 알아보네. 너도 정문으로는 못 올라왔나 보군.",0),
                    new Line("치안경찰","너와 싸울 이유는 없어. 나는 C-00 기록을 확인하러 왔다.",0),
                    new Line("저격수","그럼 각자 할 일을 하자. 살아서 내려가.",0));
            else if(floor==3)
                Talk(null,new Line("통신관","구역은 확보했다. 하지만 최상층은 승진 대상이 아니다. 특별 승인 없이는 엘리베이터가 열리지 않아.",1),
                    new Line("치안경찰","동쪽 외벽에 정비 통로가 있습니다. 기록을 보려면 돌아서 가야겠군요.",0));
            else Talk(null,new Line("통신관","구역 사건 해결 확인. 다음 층 출입 권한이 열렸다. 입구 엘리베이터에서 목적지를 선택해라.",1));
        }
        public void TravelBySidePassage(int destination)
        {
            if(game.Paused||game.hero.bike||!game.hero.IsOnGround)return;
            if(destination==4&&!AncientCityLayout.CanInfiltrate(game.City.Cleared,CurrentFloor))return;
            if(destination==3&&CurrentFloor!=4)return;
            CurrentFloor=destination;
            game.hero.TravelTo(new Vector2(destination==4?AncientCityLayout.Entry(4):AncientCityLayout.ArenaRight(3)-2,1.3f));
            game.SetLiftCheckpoint(destination);game.City.OnEnterFloor();Persist();game.BlockGameplayInput();game.SnapCameraToHero();
            game.Notice(destination==4?"최상층 외벽 샛길 / 불법 침입":"상층 거주구 귀환",3);
        }
        string cachedObjective="",cachedResidence="";int shownStage=-1,shownFlags=-1,shownFloor=-1,shownCores=-1,shownPoints=-1;
        void RefreshHudLabels()
        {
            if(shownStage!=Stage||shownFlags!=flags||shownFloor!=CurrentFloor||shownCores!=game.Cores)
            {shownStage=Stage;shownFlags=flags;shownFloor=CurrentFloor;shownCores=game.Cores;cachedObjective=Objective();}
            if(shownPoints!=ResidencePoints||cachedResidenceFloor!=CurrentFloor)
            {shownPoints=ResidencePoints;cachedResidenceFloor=CurrentFloor;cachedResidence="시민 포인트 "+ResidencePoints+" / "+AncientCityLayout.Name(CurrentFloor);}
        }
        int cachedResidenceFloor=-1;
        string Check(int bit,string label){return (Has(bit)?"[x] ":"[ ] ")+label;}
        string Objective()
        {
            if(Stage<=1)return "순찰 / "+Check(0,"이동")+"  "+Check(1,"점프")+"  "+Check(2,"Ctrl 달리기");
            if(Stage==2)return "장비 점검 / "+Check(3,"입자 와이어")+"  "+Check(4,"E 오토바이 탑승");
            if(Stage==3)return "블레이드 훈련 / "+Check(5,"물체 파괴")+"  "+Check(6,"우클릭 입자 수집")+"  "+Check(7,"눌러 조준 / 놓아 발사");
            if(Stage<=5)return "코어 도난 / 트럭 추격 · 에너지 코어 회수 "+game.Cores+" / 3";
            if(Stage==6)return "미니 보스 / 화물 수호기 제압 · 용의자 검거";
            if(Stage==7)return "검거 / 상층 주민이 코어를 훔친 이유를 물어보자";
            if(Stage==8)return "상층 출입 권한 해금 / 동쪽 이동 엘리베이터에서 E · 목적 층 선택";
            return CurrentFloor==0?"하층 생산구 / 엘리베이터로 해금 층 왕복":AncientCityLayout.Name(CurrentFloor)+" / 이동 구간 돌파 · 구역 사건 해결";
        }
        void Portrait(int index,Rect rect)
        {
            var sprite=AncientArt.DialoguePortrait(index);var r=sprite.textureRect;var t=sprite.texture;
            GUI.DrawTextureWithTexCoords(rect,t,new Rect(r.x/t.width,r.y/t.height,r.width/t.width,r.height/t.height));
        }
        void OnGUI()
        {
            if(!game||game.MainMenu||!game.HasSession||(game.Journal&&game.Journal.Open)||(game.Finale&&(game.Finale.Active||game.Finale.Finished)))return;
            if(!LiftBusy&&!DialogueOpen&&Event.current.type!=EventType.Repaint)return;
            RefreshHudLabels();var old=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-50;
            float w=Screen.width/scale,h=Screen.height/scale;
            if(body==null){body=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=21,wordWrap=true};nameStyle=new GUIStyle(body){fontSize=22,fontStyle=FontStyle.Bold};small=new GUIStyle(body){fontSize=14};nextButton=new GUIStyle(AncientHUD.Button){font=body.font,fontSize=14};}
            if(LiftBusy)DrawLift(w,h);
            else if(!DialogueOpen)
            {
                if(!game.skills)
                {
                    AncientHUD.Panel(new Rect(w/2-250,16,500,64),new Color(.015f,.045f,.065f,.94f));
                    GUI.Label(new Rect(w/2-236,24,472,52),cachedObjective,small);
                    GUI.Label(new Rect(28,200,285,42),cachedResidence,small);
                    if(CurrentFloor<4&&Mathf.Abs(game.hero.transform.position.x-LiftX)<2.5f)GUI.Label(new Rect(w/2-260,h-145,520,35),(game.hero.bike?"먼저 오토바이를 주차하세요":"E / 이동 엘리베이터 · 목적 층 선택"),nameStyle);
                }
            }
            else
            {
                AncientHUD.Panel(new Rect(0,0,w,h),new Color(.005f,.015f,.025f,.35f));
                float y=h-232;AncientHUD.Panel(new Rect(28,y,w-56,210),new Color(.015f,.025f,.045f,.98f));
                var line=lines[lineIndex];Portrait(line.portrait,new Rect(38,y-72,180,268));
                GUI.color=new Color(1,.8f,.16f);GUI.DrawTexture(new Rect(232,y+22,3,154),Texture2D.whiteTexture);GUI.color=Color.white;
                GUI.Label(new Rect(255,y+18,w-320,38),line.speaker,nameStyle);
                int chars=showFull?line.text.Length:revealedChars;
                if(chars!=visibleChars){visibleChars=chars;shownText=line.text.Substring(0,chars);}
                GUI.Label(new Rect(255,y+65,w-320,112),shownText,body);
                if(GUI.Button(new Rect(w-250,y+170,205,28),"Space / 다음",nextButton))Advance();
            }
            GUI.matrix=old;GUI.color=color;GUI.depth=depth;
        }
    }
    public sealed class AncientTruck : MonoBehaviour
    {
        AncientGame game;float nextThrow,health=180,suppressedUntil;bool dropped;
        public bool Stopped {get{return transform.position.x>=105.9f;}}
        public void Initialize(AncientGame owner,float x)
        {game=owner;transform.position=new Vector2(x,AncientDistrict.RoadHeight(x));AncientArt.Display("Core theft cargo truck",AncientArt.Truck(),Vector2.zero,7,transform);}
        void Update()
        {
            if(!game||game.Paused||game.Story.Stage<5)return;
            if(!Stopped)
            {
                float lead=game.hero.transform.position.x+13-transform.position.x;
                float speed=Mathf.Clamp(lead*.85f,2,health<=0?4:8);
                float x=Mathf.Min(106,transform.position.x+speed*Time.deltaTime);
                transform.position=new Vector2(x,Mathf.MoveTowards(transform.position.y,AncientDistrict.RoadHeight(x),3*Time.deltaTime));
                if(Time.time>=nextThrow&&Time.time>suppressedUntil&&game.shots.Count<12&&Vector2.Distance(transform.position,game.hero.transform.position)<22)
                {
                    nextThrow=Time.time+1.1f;Vector2 origin=(Vector2)transform.position+new Vector2(-2,1.8f);
                    game.Projectiles.Spawn(origin,((Vector2)game.hero.transform.position-origin).normalized*8,12,true);
                }
            }
            else if(!dropped)
            {dropped=true;for(int i=0;i<3;i++)game.SpawnPickup(new Vector2(98+i*4,1.3f),i);game.Notice("트럭 정지 / 도난 코어를 회수하고 용의자를 확보하세요.",5);}
        }
        public void HitByLaser(Vector2 start,Vector2 end,float damage)
        {
            if(Stopped)return;Vector2 delta=end-start;if(delta.sqrMagnitude<.01f)return;
            var bounds=new Bounds(transform.position+Vector3.up*1.4f,new Vector3(6,2.8f,1));float hit;
            if(bounds.IntersectRay(new Ray(start,delta.normalized),out hit)&&hit<=delta.magnitude)
            {health=Mathf.Max(0,health-damage);suppressedUntil=Time.time+3;game.Burst(transform.position+Vector3.up,AncientGame.Orange,12);}
        }
    }
}
