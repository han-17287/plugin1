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
        AncientGame game;int flags,lineIndex;float nextTrainingShot,typedAt;bool showFull;Action afterDialogue;string shownText="";int visibleChars=-1;
        struct Line {public string speaker,text;public int portrait;public Line(string who,string words,int face){speaker=who;text=words;portrait=face;}}
        readonly List<Line> lines=new List<Line>();GUIStyle body,nameStyle,small;int spawnedEncounters;
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
                Talk(null,new Line("DISPATCH","Morning patrol, Officer. Keep the lower district safe. Points buy people a life above us; down here, we keep each other alive.",1),
                    new Line("OFFICER","Understood. I'll check the streets, my chain relic, and the patrol bike before taking calls.",0));
            }
            else if(Stage==4)TheftReport();
            else if(Stage==7)ArrestDialogue();
        }
        void Talk(Action done,params Line[] script)
        {lines.Clear();lines.AddRange(script);lineIndex=0;typedAt=Time.unscaledTime;showFull=false;visibleChars=-1;afterDialogue=done;game.BlockGameplayInput();}
        void Advance()
        {
            if(!DialogueOpen)return;
            if(!showFull&&Time.unscaledTime-typedAt<lines[lineIndex].text.Length*.025f){showFull=true;return;}
            lineIndex++;showFull=false;visibleChars=-1;typedAt=Time.unscaledTime;
            if(lineIndex>=lines.Count)
            {lines.Clear();var action=afterDialogue;afterDialogue=null;game.BlockGameplayInput();action?.Invoke();}
        }
        void TheftReport()
        {
            Talk(()=>SetStage(5),new Line("DISPATCH","Priority call. An upper-city suspect has loaded locally manufactured energy cores into a cargo truck. That is their getaway vehicle, not the stolen property.",1),
                new Line("OFFICER","Those cores power this district. I'm pursuing. Get civilians clear of the road.",0));
        }
        void ArrestDialogue()
        {
            Talk(()=>{SetStage(8);},new Line("OFFICER","You're under arrest. You're from the upper city. Why steal energy cores made down here?",0),
                new Line("SUSPECT","You think the points tell you what a person is worth? Ask where your own cores go.",2),
                new Line("OFFICER","That doesn't explain it. You had the points to live above us... why risk all of that for these cores?",0),
                new Line("DISPATCH","Arrest confirmed. Your civic points have reached "+ResidencePoints+". Residence and transit clearance: one city level higher.",1),
                new Line("OFFICER","A promotion for recovering our own power. And I still don't know why they wanted it. I'll take the lift and find out.",0));
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
            {if(!game.MainMenu&&(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return)))Advance();return;}
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
            {SetStage(2);Talk(null,new Line("DISPATCH","Good. Test the chain on the ceiling, then ride the parked motorcycle with E. Park it again before the blade drill.",1));}
            else if(Stage==2&&AncientRules.TutorialReady(Stage,flags))
            {SetStage(3);Talk(null,new Line("DISPATCH","Training objects incoming. Wheel up selects attack. Tap left click to cut one with the yellow filament blade and collect its particles. Hold left click, aim, then release when the guide is gold to fire a laser.",1));}
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
                {SetStage(6);Talk(null,new Line("SUSPECT","Stay back! The cargo enforcer still answers to my clearance.",2),new Line("OFFICER","The cores are secured. Shut it down and step away from the truck.",0));}
            }
            else if(Stage==6)
            {
                EnsureTruck(true);
                if(game.bossDefeated)
                {ResidencePoints=AncientRules.PointsAfterArrest(ResidencePoints,Stage);SetStage(7);ArrestDialogue();}
                else if(!game.boss&&hero.transform.position.x>98)
                {game.SpawnEnemy(109,0,3);game.boss.PrepareMiniBoss();game.Notice("MINI BOSS / CARGO ENFORCER",3);}
            }
        }
        public bool TryUseLift()
        {
            if(!AncientRules.CanEnterUpperCity(Stage,ResidencePoints,InUpperCity)||!game.hero||Mathf.Abs(game.hero.transform.position.x-115)>4)return false;
            if(DialogueOpen)return true;
            if(game.hero.bike)return false;
            if(!game.hero.IsOnGround){game.Notice("Land before using the lift.");return true;}
            InUpperCity=true;SetStage(9);game.hero.TravelTo(new Vector2(132,1.3f));game.Checkpoint(3);game.SnapCameraToHero();game.Save();
            Talk(null,new Line("DISPATCH","Transit authorized. Welcome to the next city level, Officer.",1),new Line("OFFICER","Cleaner streets. Brighter lights. But somebody up here needed what we made below.",0));return true;
        }
        string Check(int bit,string label){return (Has(bit)?"[x] ":"[ ] ")+label;}
        string Objective()
        {
            if(Stage<=1)return "PATROL / "+Check(0,"Move")+"  "+Check(1,"Jump")+"  "+Check(2,"Ctrl run");
            if(Stage==2)return "EQUIPMENT / "+Check(3,"Ceiling chain")+"  "+Check(4,"E ride motorcycle");
            if(Stage==3)return "BLADE DRILL / "+Check(5,"Break object")+"  "+Check(6,"LMB collect")+"  "+Check(7,"Hold / release laser");
            if(Stage<=5)return "CORE THEFT / Pursue the truck. Recover energy cores "+game.Cores+" / 3";
            if(Stage==6)return "MINI BOSS / Disable the cargo enforcer and arrest the suspect";
            if(Stage==7)return "ARREST / Find out why an upper-city resident stole lower-city cores";
            if(Stage==8)return "PROMOTION / "+ResidencePoints+" points. Reach the east lift and press E";
            return "UPPER CITY / Entry authorized. The theft's motive remains unresolved";
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
            if(body==null){body=new GUIStyle(GUI.skin.label){fontSize=21,wordWrap=true};nameStyle=new GUIStyle(body){fontSize=22,fontStyle=FontStyle.Bold};small=new GUIStyle(body){fontSize=14};}
            if(!DialogueOpen)
            {
                if(!game.skills)
                {
                    AncientHUD.Panel(new Rect(w/2-250,16,500,64),new Color(.015f,.045f,.065f,.94f));
                    GUI.Label(new Rect(w/2-236,24,472,52),Objective(),small);
                    GUI.Label(new Rect(28,200,285,42),"CIVIC POINTS "+ResidencePoints+" / "+(InUpperCity?"UPPER CITY":"LOWER CITY"),small);
                    if(Stage==8&&Mathf.Abs(game.hero.transform.position.x-115)<4)GUI.Label(new Rect(w/2-220,h-145,440,35),(game.hero.bike?"E / PARK BEFORE USING THE LIFT":"E / TAKE THE LIFT TO THE NEXT CITY"),nameStyle);
                }
            }
            else
            {
                AncientHUD.Panel(new Rect(0,0,w,h),new Color(.005f,.015f,.025f,.35f));
                float y=h-232;AncientHUD.Panel(new Rect(28,y,w-56,210),new Color(.015f,.025f,.045f,.98f));
                var line=lines[lineIndex];Portrait(line.portrait,new Rect(38,y-72,180,268));
                GUI.color=new Color(1,.8f,.16f);GUI.DrawTexture(new Rect(232,y+22,3,154),Texture2D.whiteTexture);GUI.color=Color.white;
                GUI.Label(new Rect(255,y+18,w-320,38),line.speaker,nameStyle);
                int chars=showFull?line.text.Length:Mathf.Min(line.text.Length,Mathf.FloorToInt((Time.unscaledTime-typedAt)/.025f));
                if(chars!=visibleChars){visibleChars=chars;shownText=line.text.Substring(0,chars);}
                GUI.Label(new Rect(255,y+65,w-320,112),shownText,body);
                if(GUI.Button(new Rect(w-250,y+170,205,28),"SPACE / CONTINUE"))Advance();
            }
            GUI.matrix=old;GUI.color=color;GUI.depth=depth;
        }
    }
    public sealed class AncientTruck : MonoBehaviour
    {
        AncientGame game;float nextThrow,health=180,suppressedUntil;bool dropped;
        public bool Stopped {get{return transform.position.x>=105.9f;}}
        public void Initialize(AncientGame owner,float x)
        {game=owner;transform.position=new Vector2(x,x>=43&&x<=58?1:0);AncientArt.Display("Core theft cargo truck",AncientArt.Truck(),Vector2.zero,7,transform);}
        void Update()
        {
            if(!game||game.Paused||game.Story.Stage<5)return;
            if(!Stopped)
            {
                float lead=game.hero.transform.position.x+13-transform.position.x;
                float speed=Mathf.Clamp(lead*.85f,2,health<=0?4:8);
                float x=Mathf.Min(106,transform.position.x+speed*Time.deltaTime);
                transform.position=new Vector2(x,Mathf.MoveTowards(transform.position.y,x>=43&&x<=58?1:0,3*Time.deltaTime));
                if(Time.time>=nextThrow&&Time.time>suppressedUntil&&game.shots.Count<12&&Vector2.Distance(transform.position,game.hero.transform.position)<22)
                {
                    nextThrow=Time.time+1.1f;var shot=new GameObject("Truck thrown cargo").AddComponent<AncientProjectile>();
                    shot.transform.position=transform.position+new Vector3(-2,1.8f,0);shot.scrap=true;shot.damage=12;
                    shot.velocity=((Vector2)game.hero.transform.position-(Vector2)shot.transform.position).normalized*8;game.shots.Add(shot);
                }
            }
            else if(!dropped)
            {dropped=true;for(int i=0;i<3;i++)game.SpawnPickup(new Vector2(98+i*4,1.3f),i);game.Notice("TRUCK STOPPED / Recover the stolen cores, then secure the suspect.",5);}
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
