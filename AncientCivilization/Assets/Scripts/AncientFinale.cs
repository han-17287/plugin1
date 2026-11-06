using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientFinale : MonoBehaviour
    {
        AncientGame game;int phase,zone;float countdown=60,phaseAt;bool restored;
        AncientFinalTruck truck;SpriteRenderer notebook,windowTruck;
        readonly SpriteRenderer[] boardingCrew=new SpriteRenderer[3];LineRenderer supportShot,blast;
        GUIStyle label,title,button;string caption="";float captionAt;string visibleCaption="";int visibleLetters=-1;
        readonly List<AncientEnemy> crew=new List<AncientEnemy>();
        readonly List<AncientParkedBike> chaseBikes=new List<AncientParkedBike>();
        AncientSpeechBubble speechBubble;
        bool fired;
        public bool Active {get{return phase>=2&&phase<=4;}}
        public bool Cinematic {get{return phase>=3;}}
        public bool Finished {get{return phase==5;}}
        public int Phase {get{return phase;}}
        public void Build(AncientGame owner)
        {
            game=owner;phase=Mathf.Clamp(AncientGame.Saved("finalePhase"),0,5);zone=Mathf.Clamp(AncientGame.Saved("finaleZone"),0,4);
            countdown=Mathf.Clamp(AncientGame.Saved("finaleCountdown",60),0,60);
            for(float x=AncientFinaleRules.Start;x<AncientFinaleRules.Finish;x+=12)
            {
                float end=Mathf.Min(AncientFinaleRules.Finish,x+12),y=AncientFinaleRules.Road(x),last=AncientFinaleRules.Road(end);
                float angle=Mathf.Atan2(last-y,end-x)*Mathf.Rad2Deg,length=new Vector2(end-x,last-y).magnitude;
                var ramp=new GameObject("Descending cargo road");ramp.layer=8;ramp.transform.position=new Vector2((x+end)/2,(y+last)/2);
                ramp.transform.rotation=Quaternion.Euler(0,0,angle);
                var road=ramp.AddComponent<BoxCollider2D>();road.size=new Vector2(length,3.9f);road.offset=new Vector2(0,-1.95f);
                for(float local=-length/2;local<length/2;local+=6)
                {
                    float width=Mathf.Min(6,length/2-local);
                    Vector2 point=ramp.transform.TransformPoint(new Vector2(local+width/2,0));
                    game.WorldBatch.RotatedSprite(AncientArt.Rooftop(width,true),point,Vector2.one,Color.white,0,angle);
                }
                game.Ceiling(x,end,y+10);
                int district=4-AncientFinaleRules.Zone(x);
                Color tint=district>=3?new Color(.14f,.16f,.22f):district==2?new Color(.08f,.18f,.24f):new Color(.1f,.14f,.16f);
                game.StaticBox("Cargo shaft district wall",new Vector2((x+end)/2,(y+last)/2+4.8f),new Vector2(end-x,10),tint,-9);
            }
            game.Platform(AncientFinaleRules.Finish,AncientFinaleRules.Finish+22,0);
            var desk=game.Box("Empty chairman desk",new Vector2(AncientChapterLayout.OfficeX,1),new Vector2(2.8f,1.1f),new Color(.22f,.15f,.1f),3);
            desk.transform.SetParent(transform,true);
            notebook=game.Box("Chairman recovery notebook",new Vector2(AncientChapterLayout.OfficeX,1.68f),new Vector2(.48f,.15f),new Color(.9f,.8f,.5f),5).GetComponent<SpriteRenderer>();notebook.transform.SetParent(transform,true);
            windowTruck=AncientArt.Display("Operation truck outside office",AncientArt.Truck(),new Vector2(AncientChapterLayout.OfficeX-8,4.8f),-5,transform);
            windowTruck.transform.localScale=Vector3.one*.4f;windowTruck.enabled=false;
            for(int i=0;i<boardingCrew.Length;i++)
            {
                boardingCrew[i]=AncientArt.Display(i==0?"Chairman boarding transport":"Special operations boarding transport",i==0?AncientCitizenArt.Sprite(3):AncientArt.Enemy(1),new Vector2(-5-i*1.2f,0),-4,windowTruck.transform);
                boardingCrew[i].transform.localScale=Vector3.one*(i==0?1:.65f);boardingCrew[i].enabled=false;
            }
            supportShot=game.Line("Sniper support shot",Color.white,.025f,28);supportShot.enabled=false;
            blast=game.Line("Core chain-reaction shockwave",new Color(.7f,.95f,1),.14f,27);blast.loop=true;blast.positionCount=40;blast.enabled=false;
        }
        void ShowBoarding(bool shown)
        {windowTruck.enabled=shown;foreach(var person in boardingCrew)person.enabled=shown;}
        public void WriteSave()
        {AncientGame.SetSaved("finalePhase",phase);AncientGame.SetSaved("finaleZone",zone);AncientGame.SetSaved("finaleCountdown",Mathf.CeilToInt(countdown));}
        void Persist(){WriteSave();game.Save();}
        public bool TryInteract()
        {
            if(game.Story.CurrentFloor!=4||(game.City.Cleared&(1<<4))==0||Mathf.Abs(game.hero.transform.position.x-AncientChapterLayout.OfficeX)>3)return false;
            if(phase==0)
            {
                if(!game.City.SecretaryCleared){game.Notice("회장실을 지키는 비서실장을 먼저 제압하세요.",3);return true;}
                if(!game.Campaign.ReadyForFinale){game.Notice("동쪽으로 이어지는 기록 통로에서 회장실 자료 대조를 먼저 마치세요.",4);return true;}
                phase=1;countdown=60;Persist();ShowBoarding(true);
                game.Story.FinaleBriefing("회장은 없다. 노트에는 최하층 아래에 묻힌 고대 문명이 적혀 있어… 옆에는 회수 계획. C-00 코어는 이 작전에 묶인 물량이었군.",
                    "작전 개시까지 1분. 창밖이다! 회장과 특수부대가 코어 운송 트럭에 타고 있어. 화물 승강로로 내려가면 따라붙을 수 있다.");return true;
            }
            if(phase==1){BeginChase();return true;}return false;
        }
        void SetCaption(string text)
        {if(caption==text)return;caption=text;captionAt=Time.unscaledTime;visibleLetters=-1;}
        void ClearTruck()
        {
            foreach(var enemy in crew)if(enemy){game.enemies.Remove(enemy);Destroy(enemy.gameObject);}crew.Clear();
            if(truck)Destroy(truck.gameObject);truck=null;
            foreach(var shot in game.shots.ToArray())if(shot)shot.Remove();
        }
        void PrepareChaseBikes()
        {
            // TravelTo has already released the currently mounted bike before any reset.
            for(int i=0;i<5;i++)
            {
                if(i<chaseBikes.Count&&chaseBikes[i]){chaseBikes[i].ResetAtSpawn();continue;}
                float x=AncientFinaleRules.Checkpoint(i);
                var bike=new GameObject("Cargo-road parked motorcycle").AddComponent<AncientParkedBike>();
                bike.Initialize(new Vector2(x+1,AncientFinaleRules.Road(x+1)+.84f));
                if(i<chaseBikes.Count)chaseBikes[i]=bike;else chaseBikes.Add(bike);
                game.parkedBikes.Add(bike);
            }
        }
        void ClearChaseBikes()
        {
            foreach(var bike in chaseBikes)
            {
                if(game)game.parkedBikes.Remove(bike);
                if(bike){bike.gameObject.SetActive(false);Destroy(bike.gameObject);}
            }
            chaseBikes.Clear();
        }
        void BeginChase()
        {
            phase=2;game.won=false;fired=false;ShowBoarding(false);supportShot.enabled=blast.enabled=false;ClearTruck();
            float start=AncientFinaleRules.Checkpoint(zone);
            game.hero.TravelTo(new Vector2(start,AncientFinaleRules.Road(start)+1.3f));PrepareChaseBikes();game.City.OnEnterFloor();
            truck=new GameObject("Chairman core transport truck").AddComponent<AncientFinalTruck>();truck.Initialize(game,this,start+13);
            var enemy=new GameObject("Truck special-operations guard").AddComponent<AncientEnemy>();enemy.Initialize(1);
            enemy.ConfigureDistrict(4,false);enemy.transform.SetParent(truck.transform,false);enemy.transform.localPosition=new Vector2(.7f,2.5f);
            game.enemies.Add(enemy);crew.Add(enemy);Persist();game.SnapCameraToHero();game.BlockGameplayInput();
            game.Notice("최상층 → 최하층 추격 / 배치된 오토바이는 E · 점프와 와이어로 트럭 지붕에 접근",5);
        }
        public bool Retry()
        {if(phase!=2)return false;BeginChase();return true;}
        public void HitTruck(Vector2 start,Vector2 end,float damage,float bodyRadius=0,int pulseId=0)
        {if(truck&&phase==2)truck.HitBeam(start,end,damage,bodyRadius,pulseId);}
        public void TruckArrived()
        {
            phase=3;phaseAt=Time.unscaledTime;game.hero.TravelTo((Vector2)truck.transform.position+new Vector2(.7f,3.65f));ClearChaseBikes();
            game.hero.AimWeaponAt(truck.transform.position+Vector3.right*2);game.hero.CinematicPose(false,1);
            var threat=new GameObject("Cab special-operations ambush").AddComponent<AncientEnemy>();threat.Initialize(1);threat.ConfigureDistrict(4,false);
            threat.transform.SetParent(truck.transform,false);threat.transform.localPosition=new Vector2(1.6f,2.5f);threat.FaceToward(game.hero.transform.position.x);
            game.enemies.Add(threat);crew.Add(threat);
            SetCaption("움직이지 마.");Persist();game.BlockGameplayInput();
        }
        void Update()
        {
            if(!game.Story||!game.hero)return;
            if(!restored&&game.HasSession)
            {
                restored=true;
                if(AncientFinaleRules.RestorePhase(phase)==2){phase=2;BeginChase();}
                else if(phase==5){game.won=true;phaseAt=Time.unscaledTime;}
                else if(phase==1)ShowBoarding(true);
            }
            if(game.MainMenu)return;
            if(phase==3)
            {
                float age=Time.unscaledTime-phaseAt;
                if(age>=1.2f&&!fired)
                {
                    fired=true;SetCaption("엎드려!");game.Sound(6);game.Shake(.18f);
                    Vector3 core=truck.transform.position+new Vector3(.5f,1.5f,0),weapon=truck.transform.position+new Vector3(1.6f,3.45f,0);
                    supportShot.enabled=true;supportShot.SetPosition(0,weapon+(weapon-core).normalized*16);supportShot.SetPosition(1,core);
                    game.hero.CinematicPose(true,1);truck.ActivateCore();
                    foreach(var enemy in crew)if(enemy)enemy.Hit(999,true);
                }
                if(age>1.32f)supportShot.enabled=false;
                if(age>1.9f)SetCaption("코어가…!");
                if(age>2.6f){phase=4;phaseAt=Time.unscaledTime;SetCaption("보호부가 깨졌어!");blast.enabled=true;game.Sound(4);Persist();}
                return;
            }
            if(phase==4)
            {
                float age=Time.unscaledTime-phaseAt;
                Vector2 centre=truck?truck.transform.position:(Vector2)game.hero.transform.position;
                float radius=.2f+age*12;
                for(int i=0;i<blast.positionCount;i++){float a=i*Mathf.PI*2/blast.positionCount;blast.SetPosition(i,centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);}
                blast.startColor=blast.endColor=new Color(.75f,.95f,1,Mathf.Clamp01(1-age/.9f));
                if(age>.6f)SetCaption("떨어진다!");
                Vector2 point=centre+new Vector2(.7f,3.65f-Mathf.Max(0,age-.5f)*8);game.hero.CinematicMove(point);
                if(age>=2.2f){phase=5;game.won=true;AncientGame.SetSaved("firstRunCleared",1);blast.enabled=false;Persist();}
                return;
            }
            if(game.Paused)return;
            if(phase==1)
            {
                countdown-=Time.deltaTime;
                float elapsed=60-countdown;
                for(int i=0;i<boardingCrew.Length;i++)
                {
                    float t=Mathf.Clamp01((elapsed-i*.65f)/2.3f);
                    boardingCrew[i].transform.localPosition=Vector2.Lerp(new Vector2(-5-i*1.2f,0),new Vector2(-.7f+i*.7f,2.15f),t);
                }
                if(countdown<=0)BeginChase();
            }
            if(phase==2&&truck)
            {
                int next=AncientFinaleRules.Zone(game.hero.transform.position.x);
                if(next>zone){zone=next;Persist();game.Feel.Signal(AncientCityLayout.Name(4-zone)+" 화물로 진입",new Color(.6f,.95f,1),1.8f);}
                if(game.hero.transform.position.y<AncientFinaleRules.Road(game.hero.transform.position.x)-6)game.hero.RetryFromCheckpoint();
            }
        }
        void ReturnToCity()
        {
            ClearTruck();
            phase=0;countdown=60;zone=0;game.won=false;game.hero.hp=game.hero.MaxHP;
            Vector2 officeReturn=new Vector2(AncientChapterLayout.OfficeX-3,1.3f);
            game.hero.TravelTo(officeReturn);ClearChaseBikes();
            foreach(var bike in game.parkedBikes)if(bike)bike.ResetAtSpawn();
            game.CombatCheckpoint(4,officeReturn);game.City.OnEnterFloor();
            Persist();game.BlockGameplayInput();game.SnapCameraToHero();Time.timeScale=1;
        }
        void DrawCaptionBubble()
        {
            if(speechBubble==null)speechBubble=new AncientSpeechBubble();
            bool threat=caption=="움직이지 마."&&truck,radio=caption=="엎드려!";
            Vector2 head,feet;string speaker;Color accent;
            if(threat)
            {
                head=(Vector2)truck.transform.position+new Vector2(1.6f,4.6f);feet=(Vector2)truck.transform.position+new Vector2(1.6f,2.5f);
                speaker="특수부대원";accent=AncientGame.Orange;
            }
            else
            {
                head=(Vector2)game.hero.transform.position+Vector2.up*1.15f;feet=(Vector2)game.hero.transform.position-Vector2.up*1.05f;
                speaker=radio?"저격수 · 무전":"백 경위";accent=AncientGame.Cyan;
            }
            var frame=speechBubble.Measure(game,speaker,caption,0,head,feet,threat?1:.9f,interactive:false,radio:radio);
            speechBubble.Draw(frame,speaker,visibleCaption,accent,complete:true,ready:false,displayDepth:-70);
        }
        void OnGUI()
        {
            if(!game.Story||game.MainMenu)return;
            if(label==null)
            {
                label=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=20,alignment=TextAnchor.MiddleCenter,wordWrap=true};
                title=new GUIStyle(label){fontSize=34,fontStyle=FontStyle.Bold};button=new GUIStyle(AncientHUD.Button){alignment=TextAnchor.MiddleCenter};
            }
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-70;
            float w=Screen.width/scale,h=Screen.height/scale;
            if(phase==0&&game.Story.CurrentFloor==4&&(game.City.Cleared&(1<<4))!=0&&!game.Paused)
            {GUI.Label(new Rect(w/2-230,h-165,460,50),game.City.SecretaryCleared?"회장실 책상 / E: 노트와 회수 계획 확인":game.Campaign.ReadyForFinale?"비서실장을 제압하고 회장실로 진입":"동쪽 기록 통로 / 마지막 자료 대조",label);}
            else if(phase==1&&!game.Story.DialogueOpen)
            {GUI.Label(new Rect(w/2-260,h-145,520,70),"회수 작전 개시까지 "+Mathf.CeilToInt(countdown)+"초\n회장실에서 E: 화물 승강로로 먼저 진입",label);}
            else if(phase==2&&!game.Paused)
            {GUI.Label(new Rect(w/2-260,20,520,60),AncientCityLayout.Name(4-AncientFinaleRules.Zone(game.hero.transform.position.x))+" 하강 추격\n트럭 끝에서 점프 / 와이어로 지붕에 올라타세요",label);}
            if(phase==3||phase==4)
            {
                float age=Time.unscaledTime-phaseAt;
                if(phase==4){GUI.color=new Color(1,1,1,Mathf.Clamp01(1-age*2));GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);}
                int chars=Mathf.Min(caption.Length,Mathf.FloorToInt((Time.unscaledTime-captionAt)/.045f));
                if(chars!=visibleLetters){visibleLetters=chars;visibleCaption=caption.Substring(0,chars);}
                GUI.color=Color.white;DrawCaptionBubble();
                if(phase==4){GUI.color=new Color(0,0,0,Mathf.Clamp01((age-.8f)/1.1f));GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);}
            }
            if(phase==5)
            {
                GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=Color.white;
                GUI.Label(new Rect(w/2-380,h/2-150,760,70),"도시 아래로",title);
                GUI.Label(new Rect(w/2-380,h/2-65,760,90),"빛이 꺼졌다.\n그리고, 아래에서 다른 빛이 깨어났다.",label);
                if(GUI.Button(new Rect(w/2-250,h/2+70,500,45),"도시로 돌아가기",button))ReturnToCity();
                if(GUI.Button(new Rect(w/2-250,h/2+130,500,45),"저장 후 메인 화면",button))game.ReturnToMainMenu();
            }
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
        void OnDestroy()
        {ClearChaseBikes();if(speechBubble!=null)speechBubble.Dispose();if(supportShot)Destroy(supportShot.gameObject);if(blast)Destroy(blast.gameObject);}
    }
    public sealed class AncientFinalTruck : MonoBehaviour
    {
        AncientGame game;AncientFinale finale;Rigidbody2D body;BoxCollider2D solid;SpriteRenderer core,casing;bool coreActive;float nextThrow,suppressedUntil;int lastLaserPulse;
        public void Initialize(AncientGame owner,AncientFinale story,float x)
        {
            game=owner;finale=story;gameObject.layer=8;transform.position=new Vector2(x,AncientFinaleRules.Road(x));
            body=gameObject.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.interpolation=RigidbodyInterpolation2D.Interpolate;
            solid=gameObject.AddComponent<BoxCollider2D>();solid.size=new Vector2(4.8f,2.5f);solid.offset=new Vector2(0,1.25f);
            var art=AncientArt.Display("Chairman getaway transport",AncientArt.Truck(),Vector2.zero,7,transform);art.transform.localScale=Vector3.one*.75f;
            var protection=game.Box("Core protective housing",Vector2.zero,new Vector2(.85f,1.05f),new Color(.06f,.16f,.2f),8);
            protection.transform.SetParent(transform,false);protection.transform.localPosition=new Vector2(.5f,1.5f);casing=protection.GetComponent<SpriteRenderer>();
            core=AncientArt.Display("Transported active energy core",AncientArt.WireParticle(),new Vector2(.5f,1.5f),9,transform);
            core.transform.localScale=Vector3.one*.6f;core.color=new Color(.2f,.75f,.9f);
        }
        void FixedUpdate()
        {
            if(game.Paused)return;
            float speed=AncientFinaleRules.TruckSpeed(game.hero.transform.position.x,body.position.x);
            float x=Mathf.Min(AncientFinaleRules.Finish-7,body.position.x+speed*Time.fixedDeltaTime);
            body.MovePosition(new Vector2(x,AncientFinaleRules.Road(x)));
            if(AncientFinaleRules.CanBoardEnd(x,game.hero.transform.position.x,game.hero.transform.position.y,game.hero.StandingOn(body)))finale.TruckArrived();
        }
        public void ActivateCore()
        {coreActive=true;game.Impact(core.transform.position,new Color(.7f,1,1),true);}
        void Update()
        {
            if(coreActive)
            {
                float pulse=Mathf.PingPong(Time.unscaledTime*7,1);
                core.color=Color.Lerp(new Color(.3f,.85f,1),Color.white,pulse);casing.color=Color.Lerp(new Color(.8f,.25f,.1f),Color.white,pulse*.7f);
                core.transform.localScale=Vector3.one*(.6f+pulse*.2f);
            }
            if(game.Paused||Time.time<nextThrow||Time.time<suppressedUntil||game.shots.Count>=14)return;
            if(((Vector2)game.hero.transform.position-(Vector2)transform.position).sqrMagnitude>24*24)return;
            nextThrow=Time.time+1.45f;
            Vector2 origin=(Vector2)transform.position+new Vector2(-2,2.8f);
            game.Projectiles.Spawn(origin,((Vector2)game.hero.transform.position-origin).normalized*9,12,true);
        }
        public void HitBeam(Vector2 start,Vector2 end,float damage,float bodyRadius=0,int pulseId=0)
        {
            if(pulseId!=0&&lastLaserPulse==pulseId)return;
            Vector2 delta=end-start;if(delta.sqrMagnitude<.01f)return;
            // Use the actual collider, including kinematic interpolation, rather than a separately estimated visual box.
            var bounds=solid.bounds;Vector2 offset=(Vector2)bounds.center-start;
            if(!AncientBeamRules.IntersectsBox(delta.x,delta.y,offset.x,offset.y,bodyRadius,bounds.extents.x,bounds.extents.y))return;
            if(bodyRadius>0)
            {
                float angle=Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg;
                var hit=Physics2D.BoxCast(start,new Vector2(AncientBeamRules.CastDepth,bodyRadius*2),angle,delta.normalized,delta.magnitude+AncientBeamRules.CastDepth,1<<8);
                if(hit.collider!=solid)return;
            }
            if(pulseId!=0)lastLaserPulse=pulseId;suppressedUntil=Time.time+3;game.Feel.Signal("운송 장치 억제 · 투척 정지",new Color(.6f,.95f,1),1);
        }
    }
}
