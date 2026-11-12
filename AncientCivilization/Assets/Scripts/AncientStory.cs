using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientStory : MonoBehaviour
    {
        public int Stage {get;private set;}
        public bool DialogueOpen {get{return lines.Count>0||choiceOpen;}}
        public int CurrentFloor {get;private set;}
        public bool InUpperCity {get{return CurrentFloor>0;}}
        public bool LiftBusy {get{return liftMenu||liftTravelling;}}
        bool choiceOpen;string choicePrompt;string[] choiceEvidence,choiceOptions;Action<int> choiceAction;
        bool liftMenu,liftTravelling;int liftDestination;float liftElapsed;
        float LiftX
        {
            get
            {
                float entry=AncientCityLayout.Entry(CurrentFloor),exit=AncientChapterLayout.ExitLift(CurrentFloor);
                return game&&game.hero&&Mathf.Abs(game.hero.transform.position.x-exit)<Mathf.Abs(game.hero.transform.position.x-entry)?exit:entry;
            }
        }
        public AncientTruck Truck {get;private set;}
        public int ResidencePoints {get;private set;}
        AncientGame game;int flags,lineIndex;float nextTrainingShot,nextLetterAt;int revealedChars;bool showFull;Action afterDialogue;string shownText="";int visibleChars=-1;
        struct Line {public string speaker,text;public int portrait;public Line(string who,string words,int face){speaker=who;text=words.Replace("백명 경위","백 경위").Replace("백명","백 경위");portrait=face;}}
        readonly List<Line> lines=new List<Line>();GUIStyle body,nameStyle,small,nextButton;bool inspectionReadyPrompted,reportPending;
        int coachedStep=-1;float nextTrainingHint;AncientProjectile trainingObject;
        const float TrainingLaunchClearance=2.2f;
        int guideStage=-1,guideFlags=-1,guideSupplyDirection;bool guideEnergy,guideEmitterClose;string productionTutorialObjective="";
        public string ProductionTutorialObjective
        {
            get
            {
                bool energy=game&&game.hero&&game.hero.CaptureEnergy>0;
                bool emitterClose=Stage==3&&game&&game.hero&&TooCloseToTrainingEmitter();
                int supplyDirection=Stage==3&&game&&game.hero&&TrainingPracticeStep()<2?AncientProductionLayout.TrainingSupplyDirection(game.hero.transform.position.x):0;
                if(guideStage!=Stage||guideFlags!=flags||guideEnergy!=energy||guideEmitterClose!=emitterClose||guideSupplyDirection!=supplyDirection)
                {guideStage=Stage;guideFlags=flags;guideEnergy=energy;guideEmitterClose=emitterClose;guideSupplyDirection=supplyDirection;productionTutorialObjective=Objective();}
                return productionTutorialObjective;
            }
        }
        AncientSpeechBubble speech;
        int pageStart,pageEnd;bool pageReady;float pageWidth,pageHeight;
        string bubbleName="";int bubbleNameLine=-1,bubbleNameKind=-1;
        bool dialogueFocusReady;Vector2 dialogueFocus;float dialogueWidth,dialogueHeight;
        AncientRouteRadio routeRadio;
        public void RadioBark(string speaker,string text)
        {
            if(!routeRadio){routeRadio=gameObject.AddComponent<AncientRouteRadio>();routeRadio.Initialize(game);}
            routeRadio.Show(speaker,text);
        }
        public void ClearProductionRadio()
        {if(routeRadio){routeRadio.enabled=false;Destroy(routeRadio);routeRadio=null;}}
        public void Initialize(AncientGame owner)
        {
            game=owner;Stage=AncientGame.Saved("questStage");flags=AncientGame.Saved("questFlags");ResidencePoints=AncientGame.Saved("residencePoints");
            CurrentFloor=Mathf.Clamp(AncientGame.Saved("cityFloor",AncientGame.Saved("upperCity")!=0?1:0),0,4);
            // A legacy unfinished guardian save resumes the new pursuit rather than spawning the retired arena.
            if(Stage==6&&!game.bossDefeated){Stage=5;AncientGame.SetSaved("questStage",5);}
        }
        void Start(){if(game.HasSession)Begin();}
        void Persist()
        {
            AncientGame.SetSaved("questStage",Stage);AncientGame.SetSaved("questFlags",flags);
            AncientGame.SetSaved("residencePoints",ResidencePoints);AncientGame.SetSaved("upperCity",InUpperCity?1:0);AncientGame.SetSaved("cityFloor",CurrentFloor);game.Save();
        }
        void SetStage(int value){Stage=value;Persist();}
        bool Has(int bit){return (flags&(1<<bit))!=0;}
        public void OnTraining(int bit)
        {
            if(Stage==3&&bit==5)nextTrainingShot=Mathf.Max(nextTrainingShot,Time.time+6);
            if(Has(bit))return;flags|=1<<bit;Persist();
        }
        public void Begin()
        {
            if(Stage==0)
            {
                SetStage(1);
                Talk(null,new Line("통신관","백 경위. 생산장 코어 재고를 확인해라. 교대 통로를 따라 동쪽 검수실로 가면 된다.",1),
                    new Line("백명","오늘은 출고가 잦군요. 장부와 실물, 둘 다 확인하겠습니다.",0),
                    new Line("통신관","이동하며 장비부터 점검해라. C-00 예비분은 별도 승인 물량이다. 봉인은 그대로 두고.",1));
            }
            else if(Stage==4)TheftReport();
            else if(Stage==5)StartProductionIfReady();
            else if(Stage==7)BeginArrestIfReady();
        }
        void BeginArrestIfReady()
        {
            if(!game.hero||!game.hero.Combat)return;
            if(game.Production)game.Production.ResumeArrestScene();
            ArrestDialogue();
        }
        void StartProductionIfReady()
        {if(game.Production&&game.hero&&game.hero.Combat&&!game.Production.Active)game.Production.BeginOrResume();}
        int citizenPortrait=3;
        void Talk(Action done,params Line[] script)
        {
            reportPending=false;choiceOpen=false;choiceAction=null;lines.Clear();lines.AddRange(script);lineIndex=0;ResetTyping();afterDialogue=done;dialogueFocusReady=false;game.BlockGameplayInput();
            if(game.hero&&game.hero.Combat)game.hero.Combat.CancelAttacks();
            if(game.hero&&!game.hero.bike&&game.hero.IsOnGround)game.hero.CinematicPose(false,game.hero.Facing);
        }
        void ResetTyping()
        {revealedChars=pageStart=pageEnd=0;pageReady=false;pageWidth=pageHeight=0;nextLetterAt=Time.unscaledTime+.08f;showFull=false;visibleChars=-1;shownText="";bubbleNameLine=-1;}
        void TickTyping()
        {
            if(choiceOpen||showFull||!DialogueOpen||!pageReady||Time.unscaledTime<nextLetterAt)return;
            string text=lines[lineIndex].text;if(revealedChars>=pageEnd)return;
            float now=Time.unscaledTime;
            // Keep the same reading pace at 30/60/120 Hz, without flooding a line after a long stall.
            if(now-nextLetterAt>.3f)nextLetterAt=now-.1f;
            for(int catchup=0;catchup<4&&revealedChars<pageEnd&&now>=nextLetterAt;catchup++)
            {
                char letter=text[revealedChars++];
                float pause=letter=='.'||letter=='?'||letter=='!'||letter=='…'?.16f:letter==','?.08f:0;
                nextLetterAt+=.045f+pause;
            }
        }
        void Advance()
        {
            // Opening a dialogue consumes its triggering input; the same jump/click must not reveal a line.
            if(choiceOpen||!DialogueOpen||!pageReady||!Application.isFocused||!game.GameplayInputReady||game.MainMenu||game.skills||(game.Journal&&game.Journal.Open)||LiftBusy)return;
            if(!showFull&&revealedChars<pageEnd){showFull=true;return;}
            if(pageEnd<lines[lineIndex].text.Length)
            {
                pageStart=pageEnd;revealedChars=pageStart;pageReady=false;showFull=false;visibleChars=-1;shownText="";nextLetterAt=Time.unscaledTime+.045f;return;
            }
            lineIndex++;ResetTyping();
            if(lineIndex>=lines.Count)
            {lines.Clear();var action=afterDialogue;afterDialogue=null;game.BlockGameplayInput();action?.Invoke();}
        }
        void TheftReport()
        {
            ClearProductionRadio();
            Talk(StartProductionBreach,
                new Line("백명","생산장 도착. 장부에는 120기, 실물은 117기입니다. 코어 세 기가 부족합니다.",0),
                new Line("통신관","예정된 출고는 없다. 운송 기록부터 대조해 봐라.",1),
                new Line("백명","출고 기록은 없고, 인계 서명은… 잠깐, 격벽 쪽에서…",0));
            reportPending=true;
        }
        void StartProductionBreach()
        {
            if(!reportPending||Stage!=4)return;
            reportPending=false;lines.Clear();afterDialogue=null;lineIndex=0;ResetTyping();dialogueFocusReady=false;
            SetStage(5);game.BlockGameplayInput();if(game.Production)game.Production.BeginBreach();
        }
        public bool TryProductionStock()
        {
            if(CurrentFloor!=0||Stage<1||Stage>4||!game.hero)return false;
            Vector2 point=new Vector2(AncientProductionLayout.StockX,1.3f);
            if(((Vector2)game.hero.transform.position-point).sqrMagnitude>2.7f*2.7f)return false;
            if(DialogueOpen||game.Paused||!game.GameplayInputReady)return true;
            if(game.hero.Wired||!game.hero.IsOnGround)
            {game.Notice("재고 단말 앞 통로에 착지한 뒤 E로 수량을 확인하세요.",1.8f);return true;}
            if(Stage<3)
            {game.Notice(Stage==1?"장비 점검 / Space 점프와 Ctrl 달리기를 한 번씩 확인하세요.":"검수 회랑을 지나 생산장 재고 단말로 이동하세요.",2);return true;}
            if(!AncientRules.TutorialReady(3,flags))
            {game.Notice(ProductionTutorialObjective,3);return true;}
            var combat=game.hero.Combat;
            if(combat&&(combat.Firing||combat.BladeAnimating||combat.Charging))
            {game.Notice("장비 동작이 끝나면 E로 재고를 확인하세요.",1.2f);return true;}
            game.ClearBattleProjectiles();SetStage(4);TheftReport();return true;
        }
        public void ProductionCleared()
        {
            if(Stage<5||Stage>=7)return;
            ClearProductionRadio();
            game.coreMask|=7;game.bossDefeated=true;
            ResidencePoints=AncientRules.PointsAfterArrest(ResidencePoints,6);
            SetStage(7);ArrestDialogue();
        }
        void ArrestDialogue()
        {
            Talk(()=>{if(game.Production)game.Production.CloseAfterArrest();SetStage(8);},
                new Line("백명","코어는 확보했다. 당신을 에너지 코어 절도 혐의로 체포한다.",0),
                new Line("백명","상층 주민이 왜 아래층 코어를 훔쳤지? 위에서 살 포인트도 있었을 텐데.",0),
                new Line("운송대장","그 코어, 여기서 쓸 물건이라고 들었지? 운송장 마지막 칸까지 봤어?",2),
                new Line("통신관","검거 확인. 시민 포인트 "+ResidencePoints+"점. 한 단계 위 도시의 거주·통행 자격이 승인됐다.",1),
                new Line("백명","잠깐, 수령처부터 확인하겠습니다. 용의자도 아직 할 말이 있는 것 같습니다.",0),
                new Line("통신관","회수품과 용의자는 별도 이송 명령이다. 승인 코드 C-00… 원본은 내 권한으로도 열리지 않아.",1),
                new Line("백명","점검에서도 빠지고, 수령처도 못 본다… 운송장 사본부터 남겨 주세요.",0),
                new Line("통신관","보관했다. 인계를 마치고 동쪽 운송로를 확인해라. 위층으로 갈 때는 이동 엘리베이터를 이용하고.",1));
        }
        public void ResetChaseBikeForRetry()
        {if(Stage==5)StartProductionIfReady();}
        int TrainingPracticeStep()
        {
            if(!Has(5))return 0;
            if(!Has(6)||(!Has(7)&&game.hero.CaptureEnergy<=0))return 1;
            return Has(7)?3:2;
        }
        bool TooCloseToTrainingEmitter()
        {
            foreach(float x in AncientProductionLayout.TrainingEmitterX)
                if(Mathf.Abs(game.hero.transform.position.x-x)<TrainingLaunchClearance)return true;
            return false;
        }
        void TrainingHint(int step)
        {
            ClearProductionRadio();
            if(step==0)RadioBark("통신관","장비 검수 배출구다. 휠을 위로 돌린 뒤, 다가오는 빈 하우징을 짧은 좌클릭으로 베어라. 훈련 물체는 안전하다.");
            else if(step==1)RadioBark("통신관",Has(6)?"입자가 비었군. 빈 하우징을 다시 베고, 파란 입자 가까이에서 우클릭을 누르고 있어라.":"베인 물체가 파란 입자로 변했다. 가까이 가서 우클릭을 누르고 있으면 수집된다. 모드는 그대로 둬도 된다.");
            else if(step==2)RadioBark("통신관","입자가 모였다. 빈 공간을 겨냥하고 좌클릭을 유지해라. 조준선이 금색이 되면 놓아서 레이저를 발사한다.");
            else RadioBark("통신관","장비 확인 완료. 동쪽 재고 단말에서 E를 눌러 장부와 실제 수량을 대조해라.");
            nextTrainingHint=Time.time+18;
        }
        void TickTraining()
        {
            int step=TrainingPracticeStep();
            if(step!=coachedStep)
            {
                bool justCut=coachedStep==0&&step==1;
                coachedStep=step;TrainingHint(step);
                if(trainingObject&&!trainingObject.Available)trainingObject.Remove();
                // A broken object leaves eight seconds of collectible motes. Give collection
                // and aiming their own quiet window before offering another safe housing.
                nextTrainingShot=Time.time+(justCut?6:step<2?1.1f:0);
            }
            if(step==3)
            {
                if(!inspectionReadyPrompted){inspectionReadyPrompted=true;game.ClearBattleProjectiles();}
                return;
            }
            if(Time.time>=nextTrainingHint)TrainingHint(step);
            if(step==2||Time.time<nextTrainingShot||(trainingObject&&!trainingObject.Available))return;
            var hero=game.hero;var combat=hero.Combat;
            if(hero.Wired||!combat||combat.Charging||combat.Firing)return;
            float nearest=AncientProductionLayout.TrainingEmitterRange,emitter=0;bool found=false;
            foreach(float x in AncientProductionLayout.TrainingEmitterX)
            {
                float distance=Mathf.Abs(hero.transform.position.x-x);
                if(distance<TrainingLaunchClearance||distance>nearest)continue;
                nearest=distance;emitter=x;found=true;
            }
            if(!found)return;
            // The launcher is fixed in the scenery, rather than materialising an attack
            // beside a running player. Empty housings are harmless if a first cut misses.
            float side=hero.transform.position.x>=emitter?1:-1;
            trainingObject=game.Projectiles.Spawn(new Vector2(emitter+side*.35f,AncientProductionLayout.TrainingEmitterY),Vector2.right*side*4,8,true);
            if(trainingObject)trainingObject.TrainingObject=true;
            nextTrainingShot=Time.time+2.4f;
        }
        void Update()
        {
            if(!Application.isFocused)return;
            if(Stage>=8&&Truck){Destroy(Truck.gameObject);Truck=null;}
            if(choiceOpen)
            {
                if(!game.MainMenu&&game.GameplayInputReady)
                {
                    if(Input.GetKeyDown(KeyCode.Escape))CloseChoice();
                    else for(int i=0;i<choiceOptions.Length;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))){Choose(i);break;}
                }
                return;
            }
            if(liftTravelling)
            {
                if(!game.MainMenu&&!game.skills&&!(game.Journal&&game.Journal.Open))
                {liftElapsed+=Mathf.Min(.25f,Time.unscaledDeltaTime);if(liftElapsed>=1.6f)FinishLiftTravel();}
                return;
            }
            if(liftMenu)
            {
                if(Input.GetKeyDown(KeyCode.Escape)){liftMenu=false;game.BlockGameplayInput();}
                return;
            }
            if(DialogueOpen)
            {
                bool externalPause=game.MainMenu||game.skills||(game.Journal&&game.Journal.Open)||LiftBusy;
                if(!externalPause)
                {
                    TickTyping();
                    // The first two report lines are read normally; the wall cuts off the third mid-sentence.
                    if(Stage==4&&reportPending&&lineIndex==2&&pageReady&&game.HasSession&&game.GameplayInputReady&&Application.isFocused&&(showFull||revealedChars>=14))
                    {StartProductionBreach();return;}
                    if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return))Advance();
                }
                return;
            }
            if(game.Paused||!game.HasSession||!game.GameplayInputReady||!game.hero)return;
            var hero=game.hero;
            if(Stage<=3)
            {
                if(hero.transform.position.x>AncientProductionLayout.TutorialSpawn+3)OnTraining(0);
                if(Input.GetKeyDown(KeyCode.Space))OnTraining(1);
                if(hero.running)OnTraining(2);
                if(hero.Wired)OnTraining(3);
            }
            if(Stage==1&&hero.transform.position.x>=-154&&AncientRules.TutorialReady(1,flags))
            {
                SetStage(2);RadioBark("통신관","휠 아래는 와이어 모드다. 천장에 좌클릭하면 직선 견인, 가운데 클릭하면 스윙한다. 위는 지름길이고 아래도 안전하다.");
            }
            else if(Stage==2&&hero.transform.position.x>=-60)
            {
                SetStage(3);coachedStep=-1;TickTraining();
            }
            else if(Stage==3)TickTraining();
            else if(Stage==5)StartProductionIfReady();
            else if(Stage==6&&game.bossDefeated)ProductionCleared();
            else if(Stage==7)BeginArrestIfReady();
        }
        public bool TryUseLift()
        {
            if(Stage<8||(game.Finale&&game.Finale.Active)||CurrentFloor==4||!game.hero||Mathf.Abs(game.hero.transform.position.x-LiftX)>2.5f)return false;
            if(DialogueOpen||LiftBusy)return true;
            if(game.hero.bike){game.Notice("오토바이를 주차한 뒤 이동 엘리베이터에 탑승하세요.");return false;}
            if(!game.hero.IsOnGround){game.Notice("지면에 착지한 뒤 엘리베이터를 이용하세요.");return true;}
            liftMenu=true;game.BlockGameplayInput();return true;
        }
        void StartLiftTravel(int destination)
        {
            if(!AncientCityLayout.CanLift(Stage,ResidencePoints,game.City.Cleared,CurrentFloor,destination)||!game.Campaign.ReadyForLift(destination))return;
            game.Campaign.LeaveFloor();
            liftMenu=false;liftTravelling=true;liftDestination=destination;liftElapsed=0;
            game.hero.TravelTo(new Vector2(LiftX,1.3f));game.BlockGameplayInput();game.Sound(2);
        }
        void FinishLiftTravel()
        {
            bool firstArrival=liftDestination==1&&Stage==8;
            CurrentFloor=liftDestination;if(firstArrival)Stage=9;
            bool finished=AncientCampaignRules.FloorCount(game.Campaign.CompletedMask,CurrentFloor)==4;
            Vector2 arrival=new Vector2(finished?AncientChapterLayout.ExitLift(CurrentFloor):AncientCityLayout.Entry(CurrentFloor),1.3f);
            AncientGame.SetSaved("cityFloor",CurrentFloor);AncientGame.SetSaved("upperCity",InUpperCity?1:0);AncientGame.SetSaved("questStage",Stage);
            game.hero.TravelTo(arrival);game.City.OnEnterFloor();game.SetLiftCheckpoint(CurrentFloor,arrival);game.Campaign.EnterFloor();
            Persist();game.SnapCameraToHero();liftTravelling=false;game.BlockGameplayInput();
            game.Notice(InUpperCity?"상층 도시 도착 / 같은 엘리베이터로 하층에 돌아갈 수 있습니다.":"하층 도시 도착 / 상층 출입 권한은 유지됩니다.",4);
            if(firstArrival)Talk(null,new Line("통신관","상층 도착. 귀환할 때도 이동 엘리베이터를 이용해라. 하층 출입 권한도 유지된다.",1),
                new Line("백명","엘리베이터 관제에도 C-00이 찍혀 있군. 생산소 배정 번호인 줄 알았는데.",0),
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
                float progress=Mathf.Clamp01(liftElapsed/1.6f);
                AncientHUD.Panel(new Rect(x+28,y+212,544*progress,6),AncientGame.Cyan);return;
            }
            GUI.Label(new Rect(x+28,y+68,544,48),"관제 C-00 / "+AncientCityLayout.Name(CurrentFloor)+" / "+ResidencePoints+"점",small);
            for(int floor=0;floor<=4;floor++)
            {
                bool current=CurrentFloor==floor;
                bool permission=AncientCityLayout.CanLift(Stage,ResidencePoints,game.City.Cleared,CurrentFloor,floor),routeReady=game.Campaign.ReadyForLift(floor);
                bool allowed=permission&&routeReady;
                string text=AncientCityLayout.Name(floor);
                if(current)text+="  [현재 층]";else if(floor==4)text+="  [특별 승인: 승진으로 진입 불가]";else if(permission&&!routeReady)text+="  [통행 승인 / 하층 운송로 조사 중]";else if(!allowed)text+="  [잠김: 해당 구역의 현장 조사·사건 해결 필요]";
                GUI.enabled=allowed;
                if(GUI.Button(new Rect(x+28,y+120+floor*57,544,48),text,nextButton))StartLiftTravel(floor);
                GUI.enabled=true;
            }
            if(GUI.Button(new Rect(x+28,y+431,544,42),"닫기 / Esc",nextButton)){liftMenu=false;game.BlockGameplayInput();}
        }
        public void FinaleBriefing(string first,string second)
        {Talk(null,new Line("백명",first,0),
            new Line("회수 계획 주석","기동 준비된 코어는 보호부를 분리하지 말 것. 외부 충격에 의한 보호부 파손은 연쇄 활성화를 유발한다.",1),new Line("통신관",second,1));}
        public void AskEvidence(string prompt,string[] evidence,string[] options,Action<int> answer)
        {
            if(options==null||options.Length<2||options.Length>3||answer==null)return;
            choicePrompt=prompt;choiceEvidence=evidence;choiceOptions=options;choiceAction=answer;choiceOpen=true;game.BlockGameplayInput();
        }
        void CloseChoice(){choiceOpen=false;choiceAction=null;game.BlockGameplayInput();}
        void Choose(int index)
        {
            if(!choiceOpen||!game.GameplayInputReady||index<0||index>=choiceOptions.Length)return;
            var action=choiceAction;CloseChoice();action?.Invoke(index);
        }
        void DrawChoice(float w,float h)
        {
            AncientHUD.Panel(new Rect(0,0,w,h),new Color(.005f,.015f,.025f,.92f));float x=w/2-410,y=h/2-280;
            AncientHUD.Panel(new Rect(x,y,820,560),new Color(.02f,.045f,.065f,.99f));
            GUI.Label(new Rect(x+28,y+18,764,62),"증거 대조 / "+choicePrompt,nameStyle);
            if(choiceEvidence!=null)for(int i=0;i<Mathf.Min(2,choiceEvidence.Length);i++)
            {
                AncientHUD.Panel(new Rect(x+28,y+92+i*80,764,72),new Color(.035f,.075f,.095f));
                GUI.Label(new Rect(x+42,y+100+i*80,736,60),choiceEvidence[i],small);
            }
            GUI.enabled=game.GameplayInputReady;
            for(int i=0;i<choiceOptions.Length;i++)if(GUI.Button(new Rect(x+28,y+270+i*62,764,50),(i+1)+" / "+choiceOptions[i],nextButton))Choose(i);
            if(GUI.Button(new Rect(x+28,y+510,764,32),"돌아가서 다시 확인 / Esc",nextButton))CloseChoice();GUI.enabled=true;
        }
        public void Conversation(Action done,int role,string[] speakers,string[] words)
        {
            if(speakers==null||words==null||speakers.Length!=words.Length||words.Length==0)return;
            citizenPortrait=Mathf.Clamp(role,0,3);var script=new Line[words.Length];
            for(int i=0;i<words.Length;i++)
            {
                int portrait=-10-citizenPortrait;
                foreach(var npc in AncientCampaignData.Citizens)
                    if(npc.Name==speakers[i]||speakers[i].EndsWith(" "+npc.Name.Substring(npc.Name.LastIndexOf(' ')+1)))
                    {portrait=-100-npc.Id;break;}
                if(speakers[i]=="비서실장")portrait=-3;
                if(speakers[i].Contains("작업반장")||speakers[i].Contains("재하"))portrait=-104;
                script[i]=new Line(speakers[i],words[i],speakers[i]=="백명"?0:speakers[i]=="통신관"?1:portrait);
            }
            Talk(done,script);
        }
        public void FieldDialogue(string speaker,string words)
        {Talk(null,new Line(speaker,words,1));}
        public void OnDistrictCleared(int floor,Action done=null)
        {
            if(floor<3)ResidencePoints=Mathf.Max(ResidencePoints,(floor+1)*1000);
            Persist();
            if(floor==4)
                Talk(done,new Line("백명","정문을 피해서 들어왔군. 나를 쫓아온 건가?",0),
                    new Line("저격수","쫓기는 쪽이야. 포인트는 동결됐고 이의 신청은 사라졌지. 너도 정식 출입 기록이 없군.",-2),
                    new Line("백명","기록을 조회하자 내 심사도 멈췄다. 여기 오기 위해 승진한 건 아니었는데.",0),
                    new Line("저격수","그럼 길을 막을 이유는 없겠네. 난 내 기록을 찾겠다.",-2),
                    new Line("백명","나는 코어 회수 계획을 확인한다. 각자 움직이자.",0));
            else if(floor==3)
                Talk(done,new Line("통신관","구역은 확보했다. 하지만 네 포인트 심사가 보류됐다. 최상층은 승진 대상도 아니야. 특별 승인 없이는 엘리베이터가 열리지 않아.",1),
                    new Line("백명","동쪽 외벽에 정비 통로가 있습니다. 기록을 보려면 돌아서 가야겠군요.",0));
            else Talk(done,new Line("통신관","구역 사건 해결 확인. 다음 층 출입 권한이 열렸다. 동쪽 이동 엘리베이터에서 목적지를 선택해라.",1));
        }
        public void TravelBySidePassage(int destination)
        {
            if(game.Paused||game.hero.bike||!game.hero.IsOnGround)return;
            if(destination==4&&!AncientCityLayout.CanInfiltrate(game.City.Cleared,CurrentFloor))return;
            if(destination==3&&CurrentFloor!=4)return;
            game.Campaign.LeaveFloor();CurrentFloor=destination;
            Vector2 arrival=new Vector2(destination==4?AncientCityLayout.Entry(4):AncientCityLayout.Right(3)-8,1.3f);
            AncientGame.SetSaved("cityFloor",CurrentFloor);AncientGame.SetSaved("upperCity",InUpperCity?1:0);
            game.hero.TravelTo(arrival);game.City.OnEnterFloor();game.SetLiftCheckpoint(destination,arrival);game.Campaign.EnterFloor();
            Persist();game.BlockGameplayInput();game.SnapCameraToHero();
            game.Notice(destination==4?"최상층 외벽 샛길 / 불법 침입":"상층 거주구 귀환",3);
        }
        string cachedObjective="",cachedResidence="";int shownStage=-1,shownFlags=-1,shownFloor=-1,shownCores=-1,shownPoints=-1;
        void RefreshHudLabels()
        {
            if(shownStage!=Stage||shownFlags!=flags||shownFloor!=CurrentFloor||shownCores!=game.Cores)
            {shownStage=Stage;shownFlags=flags;shownFloor=CurrentFloor;shownCores=game.Cores;cachedObjective=Objective();}
            // Both HUD locations follow current energy and emitter distance, including
            // a retry that clears energy without changing the saved lesson flags.
            if(Stage<=4)cachedObjective=ProductionTutorialObjective;
            else if(Stage==5&&game.Production)cachedObjective=game.Production.Objective;
            if(shownPoints!=ResidencePoints||cachedResidenceFloor!=CurrentFloor)
            {shownPoints=ResidencePoints;cachedResidenceFloor=CurrentFloor;cachedResidence="시민 포인트 "+ResidencePoints+" / "+AncientCityLayout.Name(CurrentFloor);}
        }
        int cachedResidenceFloor=-1;
        string Check(int bit,string label){return (Has(bit)?"[x] ":"[ ] ")+label;}
        string Objective()
        {
            if(Stage<=1)return "생산장으로 이동 / "+Check(0,"이동")+"  "+Check(1,"Space 점프")+"  "+Check(2,"Ctrl 달리기 전환");
            if(Stage==2)return "검수 회랑으로 이동 / 천장 와이어 연습 · 아래는 안전 통로";
            if(Stage==3)
            {
                int step=TrainingPracticeStep();
                int supplyDirection=step<2?AncientProductionLayout.TrainingSupplyDirection(game.hero.transform.position.x):0;
                if(supplyDirection!=0)
                {
                    string direction=supplyDirection<0?"◀":"▶";
                    return step==0?"장비 확인 / "+direction+" 검수 배출구로 이동 · 빈 하우징 베기":
                        "파란 입자 우클릭 수집 · 새 물체는 "+direction+" 검수 배출구로";
                }
                if(step==0&&TooCloseToTrainingEmitter())return "장비 확인 / 배출구에서 두 걸음 물러나 빈 하우징을 베세요";
                if(step==0)return "장비 확인 1 / 휠 위 ▶ 짧은 좌클릭으로 빈 하우징 베기";
                if(step==1&&TooCloseToTrainingEmitter())return "장비 확인 2 / 파란 입자 우클릭 수집 · 새 물체는 배출구에서 두 걸음 뒤로";
                if(step==1)return "장비 확인 2 / 파란 입자 근처에서 우클릭 유지 · 놓쳤다면 다시 베기";
                if(step==2)return "장비 확인 3 / 좌클릭 유지 ▶ 금색 조준선 확인 ▶ 놓아서 레이저";
                return "장비 확인 완료 / 동쪽 재고 단말 앞에서 E로 수량 확인";
            }
            if(Stage==4)return "생산장 재고 보고 / 장부 120기 · 실제 117기";
            if(Stage==5)return game.Production?game.Production.Objective:"코어 탈취 차량 추격 / 자동주행 · Space 점프";
            if(Stage==6)return "코어 확보 / 운송대장 검거";
            if(Stage==7)return "검거 / 상층 주민이 코어를 훔친 이유를 물어보자";
            if(Stage==8)return "회수 운송로 수사 / 동쪽 길 끝의 이동 엘리베이터로";
            return AncientCityLayout.Name(CurrentFloor)+" / 수사로를 따라 동쪽 목적지로 이동";
        }
        static bool IsHero(string speaker){return speaker=="백명"||speaker=="백 경위"||speaker=="백명 경위";}
        static string DisplaySpeaker(string speaker){return IsHero(speaker)?"백 경위":speaker=="통신관"?"사령관":speaker;}
        void SpeakerAnchor(string speaker,out Vector2 head,out Vector2 feet,out float width,out int kind)
        {
            Vector2 player=game.hero.transform.position;head=player+Vector2.up*1.15f;feet=player-Vector2.up*1.05f;width=.9f;kind=0;
            if(IsHero(speaker))return;
            if(speaker=="통신관"||speaker=="사령관"){kind=1;return;}
            Transform actor=null;float offset=2;
            bool found=(game.Production&&game.Production.TryDialogueActor(speaker,out actor,out offset));
            if(!found)found=game.ProductionResidents&&game.ProductionResidents.TryDialogueActor(speaker,out actor,out offset);
            if(!found)found=game.City&&game.City.TryDialogueActor(speaker,out actor,out offset);
            if(!found)found=game.Campaign&&game.Campaign.TryDialogueActor(speaker,out actor,out offset);
            if(found&&actor)
            {
                if(Vector2.Distance(player,actor.position)<26){feet=actor.position;head=feet+Vector2.up*offset;width=offset>2.2f?1.2f:.9f;return;}
                kind=1;return;
            }
            if(speaker=="용의자"||speaker=="운송대장")
            {feet=player+Vector2.right*2;head=feet+Vector2.up*1.8f;width=1;return;}
            kind=speaker=="진료소 기록"||speaker=="보관된 녹음"||speaker=="회수 계획 주석"||speaker=="현장 기록"?2:1;
        }
        public bool TryDialogueFrame(out Vector2 focus,out float zoom)
        {
            focus=Vector2.zero;zoom=5.8f;if(!game.hero||!DialogueOpen||choiceOpen||LiftBusy)return false;
            if(!dialogueFocusReady)
            {
                Vector2 player=game.hero.transform.position;float left=player.x-.5f,right=player.x+.5f,bottom=player.y-1.05f,top=player.y+1.2f;
                foreach(var line in lines)
                {
                    Vector2 head,feet;float width;int kind;SpeakerAnchor(line.speaker,out head,out feet,out width,out kind);
                    if(kind!=0)continue;
                    left=Mathf.Min(left,feet.x-width*.5f);right=Mathf.Max(right,feet.x+width*.5f);bottom=Mathf.Min(bottom,feet.y);top=Mathf.Max(top,head.y);
                }
                dialogueFocus=new Vector2((left+right)*.5f,Mathf.Max(4.6f,(bottom+top+2.5f)*.5f));dialogueWidth=right-left+6;dialogueHeight=top-bottom+3;
                dialogueFocusReady=true;
            }
            focus=dialogueFocus;zoom=Mathf.Max(5.8f,Mathf.Max(dialogueWidth/(2*Mathf.Max(.3f,game.View.aspect)),dialogueHeight*.5f));return true;
        }
        void DrawSpeech()
        {
            if(speech==null)speech=new AncientSpeechBubble();
            var line=lines[lineIndex];Vector2 head,feet;float width;int kind;SpeakerAnchor(line.speaker,out head,out feet,out width,out kind);
            if(bubbleNameLine!=lineIndex||bubbleNameKind!=kind)
            {bubbleNameLine=lineIndex;bubbleNameKind=kind;bubbleName=DisplaySpeaker(line.speaker)+(kind==1?" · 무전":kind==2?" · 기록":"");}
            var frame=speech.Measure(game,bubbleName,line.text,pageStart,head,feet,width,true,kind==1);
            if(!pageReady||Mathf.Abs(pageWidth-frame.Body.width)>.5f||Mathf.Abs(pageHeight-frame.Body.height)>.5f)
            {pageEnd=speech.PageEnd(line.text,pageStart,frame);pageWidth=frame.Body.width;pageHeight=frame.Body.height;pageReady=true;visibleChars=-1;}
            int chars=Mathf.Clamp(showFull?pageEnd:revealedChars,pageStart,pageEnd);
            if(chars!=visibleChars){visibleChars=chars;shownText=line.text.Substring(pageStart,chars-pageStart);}
            Color accent=IsHero(line.speaker)?new Color(.08f,.5f,.65f):kind==1?new Color(.1f,.4f,.5f):new Color(.7f,.44f,.12f);
            if(speech.Draw(frame,bubbleName,shownText,accent,showFull||revealedChars>=pageEnd,game.GameplayInputReady))Advance();
        }
        void OnDestroy(){if(speech!=null)speech.Dispose();}
        void OnGUI()
        {
            if(!game||!game.hero||!game.View||game.MainMenu||!game.HasSession||(game.Journal&&game.Journal.Open)||(game.Finale&&(game.Finale.Active||game.Finale.Finished)))return;
            if(game.Production&&game.Production.Cinematic&&!DialogueOpen)return;
            if(!LiftBusy&&!DialogueOpen&&Event.current.type!=EventType.Repaint)return;
            if(DialogueOpen&&!choiceOpen){DrawSpeech();return;}
            RefreshHudLabels();var old=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-50;
            float w=Screen.width/scale,h=Screen.height/scale;
            if(body==null){body=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=21,wordWrap=true};nameStyle=new GUIStyle(body){fontSize=22,fontStyle=FontStyle.Bold};small=new GUIStyle(body){fontSize=14};nextButton=new GUIStyle(AncientHUD.Button){font=body.font,fontSize=14};}
            if(choiceOpen)DrawChoice(w,h);
            else if(LiftBusy)DrawLift(w,h);
            else if(!DialogueOpen)
            {
                if(!game.skills)
                {
                    bool controllerGuide=(game.Campaign&&game.Campaign.Active)||(game.Production&&(game.Production.Active||game.Production.InChaseLane));
                    if(!controllerGuide)
                    {
                        AncientHUD.Panel(new Rect(w/2-250,16,500,64),new Color(.015f,.045f,.065f,.94f));
                        GUI.Label(new Rect(w/2-236,24,472,52),cachedObjective,small);
                    }
                    GUI.Label(new Rect(28,200,285,42),cachedResidence,small);
                    if(Stage>=8&&CurrentFloor<4&&Mathf.Abs(game.hero.transform.position.x-LiftX)<2.5f)GUI.Label(new Rect(w/2-260,h-145,520,35),(game.hero.bike?"먼저 오토바이를 주차하세요":"E / 이동 엘리베이터 · 목적 층 선택"),nameStyle);
                }
            }
            GUI.matrix=old;GUI.color=color;GUI.depth=depth;
        }
    }
    public sealed class AncientTruck : MonoBehaviour
    {
        AncientGame game;float nextThrow,health=180,suppressedUntil;bool dropped;int lastLaserPulse;
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
        public void HitByLaser(Vector2 start,Vector2 end,float damage,float bodyRadius=0,int pulseId=0)
        {
            if(pulseId!=0&&lastLaserPulse==pulseId)return;
            if(Stopped)return;Vector2 delta=end-start;if(delta.sqrMagnitude<.01f)return;
            Vector2 center=(Vector2)transform.position+Vector2.up*1.4f,offset=center-start;
            if(AncientBeamRules.IntersectsBox(delta.x,delta.y,offset.x,offset.y,bodyRadius,3,1.4f)&&!Physics2D.Linecast(start,center,1<<8))
            {if(pulseId!=0)lastLaserPulse=pulseId;health=Mathf.Max(0,health-damage);suppressedUntil=Time.time+3;game.Burst(transform.position+Vector3.up,AncientGame.Orange,12);}
        }
    }
}
