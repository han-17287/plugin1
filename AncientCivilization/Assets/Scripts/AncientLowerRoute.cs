using System.Collections.Generic;
using UnityEngine;
namespace Plugin.AncientCivilization
{
    // One active stop, one gate and at most two local guards, irrespective of chapter length.
    public sealed class AncientLowerRoute : MonoBehaviour
    {
        AncientGame game;int current=-1,journalCase,journalStop=-1,journalFloor=-1;bool spawned,resolved;float supplyReady;
        readonly int[] done=new int[8],heard=new int[8],acted=new int[8];
        readonly List<AncientEnemy> guards=new List<AncientEnemy>(2);
        AncientRouteResidents residents;readonly SpriteRenderer[] devices=new SpriteRenderer[AncientLowerRouteRules.StopCount];
        SpriteRenderer actor,target,supply,gate;BoxCollider2D gateSolid;GUIStyle label;
        System.Func<int,bool> knownPredicate,availablePredicate;
        public static readonly string[] ProgressKeys=BuildKeys();
        static string[] BuildKeys(){var keys=new string[24];for(int i=0;i<8;i++){keys[i]="lowerStopMask"+i;keys[i+8]="lowerHeardMask"+i;keys[i+16]="lowerActionMask"+i;}return keys;}
        AncientRouteStop Stop {get{return current>=0?AncientLowerRouteData.Stops[current]:null;}}
        bool Started {get{return current>=0&&AncientLowerRouteRules.Done(heard[current/9],current);}}
        public int CompletedMask(int mission){return AncientLowerRouteRules.Supports(mission)?done[mission]:0;}
        public int CurrentStop {get{return current;}}
        public void Initialize(AncientGame owner)
        {
            game=owner;knownPredicate=Known;availablePredicate=AvailableConversation;
            var tasks=new int[20];for(int id=0;id<20;id++)tasks[id]=game.Campaign.RecordedTasks(id);
            for(int mission=0;mission<8;mission++)
            {
                done[mission]=AncientLowerRouteRules.Restore(mission,AncientGame.Saved(ProgressKeys[mission]),game.Campaign.CompletedMask,tasks[mission]);
                // Migrating an old completed objective opens the road, but does
                // not mean the player heard a newly written conversation.
                heard[mission]=Mathf.Max(0,AncientGame.Saved(ProgressKeys[mission+8]))&AncientLowerRouteRules.Mask;
                acted[mission]=Mathf.Max(0,AncientGame.Saved(ProgressKeys[mission+16]))&heard[mission];
                AncientGame.SetSaved(ProgressKeys[mission],done[mission]);AncientGame.SetSaved(ProgressKeys[mission+8],heard[mission]);AncientGame.SetSaved(ProgressKeys[mission+16],acted[mission]);
            }
            residents=new AncientRouteResidents(game,transform,knownPredicate);actor=residents.At(0);
            supply=AncientArt.Display("Repeatable route energy plate",AncientArt.MetalProjectile(1),Vector2.zero,7,transform);supply.transform.localScale=Vector3.one*.5f;
            var wall=game.Box("Local route checkpoint gate",Vector2.zero,new Vector2(.18f,10.4f),new Color(.15f,.65f,.85f,.35f),5);
            wall.layer=8;wall.transform.SetParent(transform,true);gate=wall.GetComponent<SpriteRenderer>();gateSolid=wall.AddComponent<BoxCollider2D>();
            supply.enabled=gate.enabled=gateSolid.enabled=false;
            for(int id=0;id<AncientLowerRouteRules.StopCount;id++)
            {
                Vector2 p=Point(id),basePoint=BasePoint(id);var action=AncientLowerRouteData.Stops[id].Action;
                var device=AncientArt.Display("Persistent route device "+id,AncientRouteDeviceArt.Get(action),p,7,transform);
                device.enabled=false;devices[id]=device;
                game.StaticBox("Route safe rest marker",new Vector2(basePoint.x,basePoint.y-1.27f),new Vector2(5,.06f),new Color(.24f,.62f,.75f),3);
                if(action==AncientRouteAction.Wire)
                {game.Platform(p.x-2.3f,p.x+2.3f,p.y-1.3f);game.StaticBox("Upper service station",p-Vector2.up*1.1f,new Vector2(3,.12f),AncientGame.Cyan,4);}
                if(action==AncientRouteAction.Throw)game.Living.AddRouteObject(basePoint+new Vector2(-10,-.3f),id==45);
            }
        }
        public static Vector2 BasePoint(int id)
        {float x,y,feet;AncientLowerRouteRules.Location(id,out x,out y,out feet);return new Vector2(x,feet+1.3f);}
        public static Vector2 Point(int id)
        {float x,y,feet;AncientLowerRouteRules.Location(id,out x,out y,out feet);return new Vector2(x,y);}
        public bool SectorComplete(int mission,int sector)
        {return !AncientLowerRouteRules.Supports(mission)||AncientLowerRouteRules.SectorComplete(mission,sector,done[mission]);}
        public Vector2 Checkpoint(int mission,int sector,Vector2 fallback)
        {
            if(!AncientLowerRouteRules.Supports(mission))return fallback;
            for(int stop=2;stop>=0;stop--)
            {int id=AncientLowerRouteRules.Id(mission,sector,stop);if(AncientLowerRouteRules.Done(done[mission],id))return Point(id);}
            return fallback;
        }
        public bool TryActor(string speaker,out Transform view,out float offset)
        {return residents.TryActor(speaker,out view,out offset);}
        bool Known(int id){return id>=0&&id<AncientLowerRouteRules.StopCount&&AncientLowerRouteRules.Done(heard[id/9],id);}
        bool AvailableConversation(int id)
        {return Known(id)||id>=0&&id<AncientLowerRouteRules.StopCount&&AncientLowerRouteRules.Done(done[id/9],id);}
        public bool TryGuide(out Vector2 p,out string text)
        {p=Vector2.zero;text="";if(current<0||Stop.Optional)return false;p=Started&&!resolved?Point(current):BasePoint(current)+new Vector2(-6,-.3f);text=Stop.Title+" · "+(!Started||resolved?"E: 대화":AncientLowerRouteData.Hint(Stop.Action));return true;}
        public void ResetEncounter()
        {
            ClearGuards();spawned=resolved=false;current=-1;
            if(supply)supply.enabled=gate.enabled=gateSolid.enabled=false;
        }
        void ClearGuards()
        {foreach(var foe in guards)if(foe){game.enemies.Remove(foe);foe.gameObject.SetActive(false);Destroy(foe.gameObject);}guards.Clear();}
        void Select(int id)
        {
            ClearGuards();current=id;spawned=false;resolved=id>=0&&AncientLowerRouteRules.Done(acted[id/9],id);supplyReady=0;
            supply.enabled=gate.enabled=gateSolid.enabled=id>=0;
            if(id<0)return;
            gate.enabled=gateSolid.enabled=!Stop.Optional;
            Vector2 p=Point(id),basePoint=BasePoint(id);
            residents.Focus(id);actor=residents.At(id);target=devices[id];
            supply.transform.position=basePoint+Vector2.left*3.5f;supply.color=new Color(1,.83f,.2f);
            gate.transform.position=new Vector2(p.x+18,4.8f);
        }
        void Save(){for(int m=0;m<8;m++){AncientGame.SetSaved(ProgressKeys[m],done[m]);AncientGame.SetSaved(ProgressKeys[m+8],heard[m]);AncientGame.SetSaved(ProgressKeys[m+16],acted[m]);}game.Save();}
        void ResolveAction()
        {
            if(current<0||resolved)return;
            resolved=true;acted[current/9]=AncientLowerRouteRules.Mark(acted[current/9],current);Save();
        }
        void Finish(int id)
        {
            if(current!=id||AncientLowerRouteRules.Done(done[id/9],id))return;
            done[id/9]=AncientLowerRouteRules.Mark(done[id/9],id);heard[id/9]=AncientLowerRouteRules.Mark(heard[id/9],id);
            game.hero.hp=Mathf.Min(game.hero.MaxHP,game.hero.hp+(Stop.Optional?16:8));if(Stop.Optional)game.credits+=3;Save();game.Sound(2);
            game.Notice("구간 확보 · "+Stop.Title+" / 진행 저장",2);Select(-1);
        }
        void StartAction(int id)
        {if(current!=id)return;heard[id/9]=AncientLowerRouteRules.Mark(heard[id/9],id);Save();}
        void Remember(int id)
        {heard[id/9]=AncientLowerRouteRules.Mark(heard[id/9],id);Save();}
        public bool TryInteract()
        {
            if(game.Paused||!game.GameplayInputReady||game.hero.bike)return false;
            int previous=residents.NearestKnown(availablePredicate);
            if(previous>=0&&previous!=current)
            {
                if(!game.hero.IsOnGround){game.Notice("착지한 뒤 주민과 대화하세요.");return true;}
                var old=AncientLowerRouteData.Stops[previous];bool finished=AncientLowerRouteRules.Done(done[previous/9],previous);
                if(finished&&!Known(previous))
                {
                    bool talk=old.Action==AncientRouteAction.Talk;
                    game.Story.Conversation(()=>Remember(previous),previous%4,
                        talk?new[]{old.Speaker,"백 경위",old.Speaker,"백 경위",old.Speaker,"백 경위"}:new[]{old.Speaker,"백 경위",old.Speaker,old.Speaker,"백 경위"},
                        talk?new[]{old.Problem,old.Reply,old.Evidence,AncientLowerRouteData.FollowUp(previous),old.Outcome,old.Closing}:new[]{old.Problem,old.Reply,old.Evidence,old.Outcome,old.Closing});return true;
                }
                game.Story.Conversation(null,previous%4,new[]{old.Speaker,"백 경위",old.Speaker},new[]{finished?old.Outcome:old.Problem,finished?old.Closing:old.Reply,old.Evidence});return true;
            }
            if(current<0)return false;
            Vector2 player=game.hero.transform.position,witness=(Vector2)actor.transform.position+Vector2.up,p=Point(current);
            if(Started&&!resolved&&Stop.Action==AncientRouteAction.Wire&&(p-player).sqrMagnitude<2.5f*2.5f)
            {ResolveAction();game.hero.ReleaseWire();game.Notice("상부 접속 완료 · 아래 작업자에게 확인",2);return true;}
            if(!residents.InPlace(current)||(witness-player).sqrMagnitude>3.2f*3.2f)return false;
            int id=current;var data=Stop;
            if(!game.hero.IsOnGround){game.Notice("작업대에 착지한 뒤 대화하세요.");return true;}
            if(!Started)
            {
                if(data.Action==AncientRouteAction.Talk)
                    game.Story.Conversation(()=>Finish(id),id%4,new[]{data.Speaker,"백 경위",data.Speaker,"백 경위",data.Speaker,"백 경위"},new[]{data.Problem,data.Reply,data.Evidence,AncientLowerRouteData.FollowUp(id),data.Outcome,data.Closing});
                else game.Story.Conversation(()=>StartAction(id),id%4,new[]{data.Speaker,"백 경위",data.Speaker},new[]{data.Problem,data.Reply,data.Evidence});
            }
            else if(resolved)
                game.Story.Conversation(()=>Finish(id),id%4,new[]{data.Speaker,"백 경위"},new[]{data.Outcome,data.Closing});
            else game.Notice(AncientLowerRouteData.Hint(data.Action),4);
            return true;
        }
        public bool TryBlade(Vector2 origin,Vector2 direction)
        {
            if(current<0||!Started||resolved)return false;
            bool relay=Stop.Action==AncientRouteAction.Relay;
            if(Stop.Action!=AncientRouteAction.Blade&&!relay)return false;
            Vector2 p=relay?(Vector2)supply.transform.position:Point(current),d=p-origin;
            if(d.sqrMagnitude>3.1f*3.1f||Vector2.Dot(d.normalized,direction)<-.15f||Physics2D.Linecast(origin,p,1<<8)||Time.time<supplyReady)return false;
            supplyReady=Time.time+1;game.hero.Combat.EmitParticles(p,32);game.Impact(p,AncientGame.Cyan,true);
            if(!relay)ResolveAction();return true;
        }
        public void OnLaser(Vector2 start,Vector2 end,float radius)
        {
            if(current<0||!Started||resolved||Stop.Action!=AncientRouteAction.Relay)return;
            Vector2 p=Point(current);
            if(AncientLowerRouteRules.SegmentHit(start.x,start.y,end.x,end.y,p.x,p.y,radius+.8f)&&!Physics2D.Linecast(start,p,1<<8))
            {ResolveAction();game.Impact(p,AncientGame.Cyan,true);game.Sound(2);}
        }
        public bool TryThrownContact(Vector2 start,Vector2 end,out float contact)
        {
            contact=0;
            if(current<0||!Started||resolved||Stop.Action!=AncientRouteAction.Throw)return false;
            Vector2 p=Point(current),delta=end-start,targetOffset=p-start;
            return AncientProjectileRules.CircleEntry(delta.x,delta.y,targetOffset.x,targetOffset.y,1.25f,out contact)&&!Physics2D.Linecast(start,p,1<<8);
        }
        public bool HitThrown(Vector2 start,Vector2 end)
        {
            float contact;if(!TryThrownContact(start,end,out contact))return false;
            Vector2 p=Point(current);
            ResolveAction();game.Impact(p,AncientGame.Cyan,true);return true;
        }
        void Update()
        {
            if(!game||!game.hero||!game.Story||!game.HasSession||game.Paused)return;
            int mission=game.Campaign.CurrentRoom,sector=game.Campaign.CurrentSector;
            int next=AncientLowerRouteRules.Supports(mission)?AncientLowerRouteRules.Next(mission,sector,done[mission]):-1;
            int physical=AncientCampaignRules.RoomAt(game.hero.transform.position.x);
            if(AncientLowerRouteRules.Supports(physical)&&physical/4==game.Story.CurrentFloor&&AncientCampaignRules.Complete(game.Campaign.CompletedMask,physical))
            {
                residents.Revisit(physical,game.hero.transform.position.x,availablePredicate);
                mission=physical;sector=AncientFieldExpansion.SectorAt(physical,game.hero.transform.position.x-AncientChapterLayout.CaseLeft(physical));
                next=AncientLowerRouteRules.OptionalOnReturn(physical,sector,done[physical]);
            }
            if(next!=current)Select(next);
            if(current<0)return;
            Vector2 p=Point(current);float distance=Mathf.Abs(game.hero.transform.position.x-p.x);
            target.enabled=distance<65&&Started&&!resolved&&Stop.Action!=AncientRouteAction.Ambush&&Stop.Action!=AncientRouteAction.Talk;
            supply.enabled=distance<65&&Started&&!resolved&&Stop.Action==AncientRouteAction.Relay;
            if(Started&&!resolved&&Stop.Action==AncientRouteAction.Ambush&&distance<32)
            {
                if(!spawned)
                {
                    if(mission>=4&&game.Living.NearbyGuards(p.x)>0)return;
                    spawned=true;Vector2 b=BasePoint(current);
                    for(int i=0;i<2;i++)
                    {
                        int kind=AncientLowerRouteRules.GuardType(current,i);
                        var enemy=new GameObject("Local route guard").AddComponent<AncientEnemy>();enemy.Initialize(kind);
                        enemy.transform.position=new Vector2(b.x+4+i*8,b.y-1.3f+(kind==4?2.8f:0));
                        enemy.ConfigureDistrict(mission/4,false);guards.Add(enemy);game.enemies.Add(enemy);
                    }
                }
                bool alive=false;foreach(var enemy in guards)if(enemy&&enemy.IsAlive)alive=true;
                if(!alive)ResolveAction();
            }
            if(resolved)target.enabled=supply.enabled=false;
            // A fast wire move cannot bypass an unresolved gate between physics ticks.
            if(!Stop.Optional&&game.hero.transform.position.x>p.x+18.5f)
            {game.hero.TravelTo(Checkpoint(mission,sector,new Vector2(AncientChapterLayout.CaseLeft(mission)+AncientFieldExpansion.EntranceOf(mission,sector),1.3f)));game.SnapCameraToHero();}
        }
        void LateUpdate()
        {
            if(residents==null||!game||!game.Story)return;residents.Tick();
            for(int id=0;id<devices.Length;id++)
            {
                var device=devices[id];if(!device)continue;var action=AncientLowerRouteData.Stops[id].Action;
                bool finished=AncientLowerRouteRules.Done(done[id/9],id)||AncientLowerRouteRules.Done(acted[id/9],id);
                bool visible=action!=AncientRouteAction.Talk&&action!=AncientRouteAction.Ambush&&(Known(id)||finished)&&(!finished||action==AncientRouteAction.Relay||action==AncientRouteAction.Wire);
                device.enabled=visible&&game.HasSession&&!game.MainMenu&&AncientChapterView.NearCamera(game,device.transform.position,id/36);
                if(device.enabled)device.color=finished?new Color(.3f,1,.65f):Color.white;
            }
        }
        public void DrawJournal(int floor,Rect area,GUIStyle text,GUIStyle small)
        {
            if(floor<0||floor>1)return;
            if(journalFloor!=floor){journalFloor=floor;journalCase=0;journalStop=-1;}
            for(int i=0;i<4;i++)if(GUI.Button(new Rect(area.x+i*215,area.y,205,28),AncientCampaignData.Missions[floor*4+i].Title,AncientHUD.Button)){journalCase=i;journalStop=-1;}
            if(journalStop>=0)
            {
                var d=AncientLowerRouteData.Stops[journalStop];bool finished=AncientLowerRouteRules.Done(done[journalStop/9],journalStop);
                GUI.Label(new Rect(area.x,area.y+38,650,28),d.Title+" / "+d.Speaker,text);
                if(GUI.Button(new Rect(area.x+700,area.y+38,145,26),"현장 목록",AncientHUD.Button))journalStop=-1;
                string[] lines=d.Action==AncientRouteAction.Talk?
                    new[]{d.Problem,"백 경위: "+d.Reply,d.Evidence,"백 경위: "+AncientLowerRouteData.FollowUp(journalStop),finished?d.Outcome:"아직 확인하지 않은 결과입니다.",finished?"백 경위: "+d.Closing:"현장 대화를 마치면 기록됩니다."}:
                    new[]{d.Problem,"백 경위: "+d.Reply,d.Evidence,finished?d.Outcome:"아직 해결하지 않은 현장입니다.",finished?"백 경위: "+d.Closing:AncientLowerRouteData.Hint(d.Action)};
                float rowStep=Mathf.Min(44,(area.height-76)/lines.Length);
                for(int i=0;i<lines.Length;i++)GUI.Label(new Rect(area.x,area.y+76+i*rowStep,area.width,rowStep-2),lines[i],small);
                return;
            }
            for(int i=0;i<9;i++)
            {
                int id=(floor*4+journalCase)*9+i;var d=AncientLowerRouteData.Stops[id];bool known=AncientLowerRouteRules.Done(heard[id/9],id),finished=AncientLowerRouteRules.Done(done[id/9],id);
                GUI.Label(new Rect(area.x,area.y+40+i*30,680,28),(finished?known?"[확보] ":"[승계] ":known?"[진행] ":"[미확인] ")+(known?d.Title+(d.Optional?" · 선택":""):finished?"기존 수사에서 확보 · 주민의 새 증언은 미확인":"동쪽 경로의 현장"),small);
                GUI.enabled=known;if(GUI.Button(new Rect(area.x+700,area.y+40+i*30,145,27),"증언과 단서",AncientHUD.Button))journalStop=id;GUI.enabled=true;
            }
        }
        void OnGUI()
        {
            if(!game||game.MainMenu||!game.HasSession||game.Paused||residents==null||Event.current.type!=EventType.Repaint)return;
            int previous=residents.NearestKnown(availablePredicate);bool archive=previous>=0&&previous!=current;
            if(!archive&&(current<0||!actor.enabled||!residents.InPlace(current)))return;
            if(label==null)label=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=13,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            Vector2 point=archive?(Vector2)residents.At(previous).transform.position+Vector2.up:Started&&!resolved?Point(current):(Vector2)actor.transform.position+Vector2.up;
            Vector3 screen=game.View.WorldToScreenPoint(point+Vector2.up*2.5f);if(screen.z<=0||screen.x<-120||screen.x>Screen.width+120)return;
            string text=archive?Known(previous)?"E · 증언 다시 듣기":"E · 이전 현장 증언 확인":!Started?(Stop.Optional?"선택 · E · ":"E · ")+Stop.Speaker:resolved?"E · 확인하고 계속 이동":AncientLowerRouteData.Hint(Stop.Action);
            var rect=new Rect(Mathf.Clamp(screen.x-150,12,Mathf.Max(12,Screen.width-312)),Mathf.Clamp(Screen.height-screen.y-36,216,Mathf.Max(216,Screen.height-148)),300,38);
            Vector3 lo=game.View.WorldToScreenPoint(game.hero.transform.position+new Vector3(-.6f,-1.15f,0)),hi=game.View.WorldToScreenPoint(game.hero.transform.position+new Vector3(.6f,1.15f,0));
            if(rect.Overlaps(Rect.MinMaxRect(Mathf.Min(lo.x,hi.x),Screen.height-Mathf.Max(lo.y,hi.y),Mathf.Max(lo.x,hi.x),Screen.height-Mathf.Min(lo.y,hi.y))))return;
            Color color=GUI.color;GUI.color=new Color(.05f,.08f,.12f,.88f);GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=Color.white;GUI.Label(rect,text,label);GUI.color=color;
        }
    }
}
