using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientStory : MonoBehaviour
    {
        public int Stage {get;private set;}
        public bool DialogueOpen {get{return lines.Count>0;}}
        public bool InUpperCity {get;private set;}
        public AncientTruck Truck {get;private set;}
        public int ResidencePoints {get;private set;}
        AncientGame game;int flags,lineIndex;float nextTrainingShot,nextLetterAt;int revealedChars;bool showFull;Action afterDialogue;string shownText="";int visibleChars=-1;
        struct Line {public string speaker,text;public int portrait;public Line(string who,string words,int face){speaker=who;text=words;portrait=face;}}
        readonly List<Line> lines=new List<Line>();GUIStyle body,nameStyle,small,nextButton;int spawnedEncounters;
        public void Initialize(AncientGame owner)
        {game=owner;Stage=AncientGame.Saved("questStage");flags=AncientGame.Saved("questFlags");ResidencePoints=AncientGame.Saved("residencePoints");InUpperCity=AncientGame.Saved("upperCity")!=0;}
        void Start(){if(game.HasSession)Begin();}
        void Persist()
        {
            AncientGame.SetSaved("questStage",Stage);AncientGame.SetSaved("questFlags",flags);
            AncientGame.SetSaved("residencePoints",ResidencePoints);AncientGame.SetSaved("upperCity",InUpperCity?1:0);game.Save();
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
                    new Line("치안경찰","알겠습니다. 기계팔과 입자 와이어, 순찰 오토바이부터 점검하겠습니다.",0));
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
                new Line("치안경찰","저 코어가 끊기면 이 구역이 멈춥니다. 주민부터 대피시켜 주세요. 제가 쫓겠습니다.",0));
        }
        void ArrestDialogue()
        {
            Talk(()=>{SetStage(8);},new Line("치안경찰","당신을 에너지 코어 절도 혐의로 체포한다. 상층에 사는 사람이 왜 아래층의 코어를 훔친 거지?",0),
                new Line("용의자","포인트가 사람의 값을 정해 준다고 믿어? 너희가 만든 코어가 어디로 가는지나 알아봐.",2),
                new Line("치안경찰","그걸 대답이라고 하나. 위에서 살 포인트도 있었을 텐데… 이 코어 때문에 전부 걸었다고?",0),
                new Line("통신관","검거 확인. 이번 보상으로 시민 포인트가 "+ResidencePoints+"점에 도달했다. 한 단계 위 도시의 거주·통행 자격이 승인됐다.",1),
                new Line("치안경찰","우리 동력을 되찾았더니 올라가도 된다는 건가. 저자가 왜 그걸 원했는지… 위에서 알아봐야겠군.",0));
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
                    var shot=new GameObject("Training thrown object").AddComponent<AncientProjectile>();
                    shot.transform.position=hero.transform.position+new Vector3(side*5,.35f,0);
                    shot.velocity=Vector2.left*side*4;shot.damage=4;shot.scrap=true;game.shots.Add(shot);
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
                {game.SpawnEnemy(109,0,3);game.boss.PrepareMiniBoss();game.Notice("미니 보스 / 화물 수호기",3);}
            }
        }
        public bool TryUseLift()
        {
            if(!AncientRules.CanEnterUpperCity(Stage,ResidencePoints,InUpperCity)||!game.hero||Mathf.Abs(game.hero.transform.position.x-115)>4)return false;
            if(DialogueOpen)return true;
            if(game.hero.bike)return false;
            if(!game.hero.IsOnGround){game.Notice("지면에 착지한 뒤 승강기를 이용하세요.");return true;}
            InUpperCity=true;SetStage(9);game.hero.TravelTo(new Vector2(132,1.3f));game.Checkpoint(3);game.SnapCameraToHero();game.Save();
            Talk(null,new Line("통신관","통행 허가 확인. 다음 층 도시로 진입한다.",1),new Line("치안경찰","거리도, 불빛도 다르군. 그런데 이곳의 누군가는 우리가 아래에서 만든 것이 필요했다….",0));return true;
        }
        string Check(int bit,string label){return (Has(bit)?"[x] ":"[ ] ")+label;}
        string Objective()
        {
            if(Stage<=1)return "순찰 / "+Check(0,"이동")+"  "+Check(1,"점프")+"  "+Check(2,"Ctrl 달리기");
            if(Stage==2)return "장비 점검 / "+Check(3,"입자 와이어")+"  "+Check(4,"E 오토바이 탑승");
            if(Stage==3)return "블레이드 훈련 / "+Check(5,"물체 파괴")+"  "+Check(6,"우클릭 입자 수집")+"  "+Check(7,"눌러 조준 / 놓아 발사");
            if(Stage<=5)return "코어 도난 / 트럭 추격 · 에너지 코어 회수 "+game.Cores+" / 3";
            if(Stage==6)return "미니 보스 / 화물 수호기 제압 · 용의자 검거";
            if(Stage==7)return "검거 / 상층 주민이 코어를 훔친 이유를 물어보자";
            if(Stage==8)return "통행 허가 / "+ResidencePoints+"점 · 동쪽 승강기에서 E";
            return "상층 도시 / 진입 허가 · 절도의 동기는 아직 알 수 없다";
        }
        void Portrait(int index,Rect rect)
        {
            var sprite=AncientArt.DialoguePortrait(index);var r=sprite.textureRect;var t=sprite.texture;
            GUI.DrawTextureWithTexCoords(rect,t,new Rect(r.x/t.width,r.y/t.height,r.width/t.width,r.height/t.height));
        }
        void OnGUI()
        {
            if(!game||game.MainMenu||!game.HasSession)return;
            var old=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-50;
            float w=Screen.width/scale,h=Screen.height/scale;
            if(body==null){body=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=21,wordWrap=true};nameStyle=new GUIStyle(body){fontSize=22,fontStyle=FontStyle.Bold};small=new GUIStyle(body){fontSize=14};nextButton=new GUIStyle(AncientHUD.Button){font=body.font,fontSize=14};}
            if(!DialogueOpen)
            {
                if(!game.skills)
                {
                    AncientHUD.Panel(new Rect(w/2-250,16,500,64),new Color(.015f,.045f,.065f,.94f));
                    GUI.Label(new Rect(w/2-236,24,472,52),Objective(),small);
                    GUI.Label(new Rect(28,200,285,42),"시민 포인트 "+ResidencePoints+" / "+(InUpperCity?"상층 도시":"하층 도시"),small);
                    if(Stage==8&&Mathf.Abs(game.hero.transform.position.x-115)<4)GUI.Label(new Rect(w/2-220,h-145,440,35),(game.hero.bike?"E / 먼저 오토바이 주차":"E / 다음 층 도시로 이동"),nameStyle);
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
                    nextThrow=Time.time+1.1f;var shot=new GameObject("Truck thrown cargo").AddComponent<AncientProjectile>();
                    shot.transform.position=transform.position+new Vector3(-2,1.8f,0);shot.scrap=true;shot.damage=12;
                    shot.velocity=((Vector2)game.hero.transform.position-(Vector2)shot.transform.position).normalized*8;game.shots.Add(shot);
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
