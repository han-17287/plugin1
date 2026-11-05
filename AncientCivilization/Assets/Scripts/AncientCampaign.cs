using System.Collections.Generic;
using UnityEngine;
namespace Plugin.AncientCivilization
{
    // Same-floor investigation interiors; only the actual city elevator changes floors.
    public sealed class AncientCampaign : MonoBehaviour
    {
        AncientGame game;int completed,seen,beforeSeen,afterSeen,active=-1,restoreId=-1,taskMission=-1,tasks,checkpoint,defeated,encounterSector=-1;bool restored,legacy;
        readonly SpriteRenderer[,] nodes=new SpriteRenderer[20,3];readonly SpriteRenderer[] citizens=new SpriteRenderer[25];
        readonly SpriteRenderer[,] fieldGates=new SpriteRenderer[20,2],terminals=new SpriteRenderer[20,3],powerStations=new SpriteRenderer[20,3];
        readonly BoxCollider2D[,] fieldGateSolids=new BoxCollider2D[20,2];
        readonly List<AncientEnemy> foes=new List<AncientEnemy>();readonly List<int> foeSlots=new List<int>();SpriteRenderer escort;bool following;
        GUIStyle label;string objective="";int journalFloor=-1,journalMission=-1,journalCitizen=-1;bool journalPeople;float guardNoticeAt,powerEmitReady,escortCycle;int escortFrame=-1;
        public bool Active {get{return active>=0;}}
        public int CurrentRoom {get{return active;}}
        public int CurrentSector {get{return Active&&game.hero?AncientFieldExpansion.SectorAt(game.hero.transform.position.x-AncientCampaignRules.Left(active)):-1;}}
        public int EarnedCheckpoint {get{return checkpoint;}}
        public int DefeatedMask {get{return defeated;}}
        public int CompletedMask {get{return completed;}}
        public bool Legacy {get{return legacy;}}
        public string ObjectiveText {get{return objective;}}
        public float CameraLeft {get{return AncientCampaignRules.Left(active)+5;}}
        public float CameraRight {get{return AncientCampaignRules.Left(active)+AncientCampaignRules.RoomWidth-6;}}
        static readonly int[] givers={3,5,10,15,20};
        readonly SpriteRenderer[] gates=new SpriteRenderer[5];readonly BoxCollider2D[] gateSolids=new BoxCollider2D[5];readonly bool[] gateShown=new bool[5];
        static int GiverFor(int floor,int mission){return floor==4&&mission==19?24:givers[floor];}
        public void Initialize(AncientGame owner)
        {
            game=owner;completed=AncientCampaignRules.SanitizeMask(AncientGame.Saved("campaignComplete"));seen=AncientGame.Saved("campaignSeen");beforeSeen=AncientGame.Saved("campaignBeforeSeen");afterSeen=AncientGame.Saved("campaignAfterSeen");
            restoreId=AncientGame.Saved("campaignActive",-1);taskMission=AncientGame.Saved("campaignTaskMission",-1);tasks=AncientGame.Saved("campaignTasks")&7;
            checkpoint=AncientGame.Saved("campaignCheckpoint");defeated=AncientFieldExpansion.SanitizeEnemies(AncientGame.Saved("campaignDefeated"));
            legacy=AncientGame.Saved("campaignLegacy")!=0||(AncientGame.Saved("campaignVersion")==0&&AncientGame.Saved("questStage")>0);
            AncientGame.SetSaved("campaignVersion",1);AncientGame.SetSaved("campaignLegacy",legacy?1:0);
            foreach(var mission in AncientCampaignData.Missions)BuildRoom(mission);
            for(int floor=1;floor<=4;floor++)
            {
                var gate=game.Box("Investigation checkpoint barrier",new Vector2(AncientCityLayout.ArenaLeft(floor)+1,5),new Vector2(.12f,10),new Color(.2f,.7f,.8f,.32f),5);
                gate.layer=8;gates[floor]=gate.GetComponent<SpriteRenderer>();gateSolids[floor]=gate.AddComponent<BoxCollider2D>();gates[floor].enabled=gateSolids[floor].enabled=false;
            }
            foreach(var citizen in AncientCampaignData.Citizens)
            {
                citizens[citizen.Id]=AncientArt.Display(citizen.Name,AncientCitizenArt.ForCitizen(citizen.Id),new Vector2(citizen.X,citizen.Y),9,transform);
                citizens[citizen.Id].transform.localScale=Vector3.one*(citizen.Id==2?1:.9f);
            }
            for(int i=0;i<4;i++)foreach(int person in new[]{4,6,14,18})AncientArt.CitizenWalk(person,i);
            escort=AncientArt.Display("Protected witness",AncientCitizenArt.ForCitizen(6),Vector2.zero,9,transform);escort.transform.localScale=Vector3.one*.9f;escort.enabled=false;Refresh();
        }
        void BuildRoom(AncientMission mission)
        {
            float left=AncientCampaignRules.Left(mission.Id),right=left+AncientCampaignRules.RoomWidth;
            game.Ceiling(left,right,10);game.Platform(left,right,-4.5f);
            Color tint=mission.Floor==0?new Color(.12f,.17f,.2f):mission.Floor==1?new Color(.13f,.21f,.2f):mission.Floor==2?new Color(.12f,.18f,.28f):mission.Floor==3?new Color(.24f,.23f,.19f):new Color(.19f,.13f,.24f);
            if(mission.Kind==2)game.Platform(left,right,0);
            for(int sector=0;sector<AncientFieldExpansion.SectorCount;sector++)
            {
                float sectorLeft=left+AncientFieldExpansion.SectorStart(sector);
                AncientFieldScenery.Build(game,mission.Floor,sectorLeft,(mission.Id+sector)%4);
                for(int i=0;i<12;i++)
                {
                    float x=sectorLeft+6+i*12;
                    game.StaticBox("Investigation facade",new Vector2(x,4.5f),new Vector2(11,9),tint,-8);
                    game.StaticBox("Investigation window",new Vector2(x,6.6f),new Vector2(5,1.4f),tint*1.55f,-7);
                    game.StaticBox("District floor stripe",new Vector2(x,9.88f),new Vector2(8,.06f),new Color(.2f,.65f,.7f),3);
                }
                if(mission.Kind!=2)
                {
                    var entry=AncientFieldExpansion.EntrancePlatform(sector);var end=AncientFieldExpansion.TerminalPlatform(sector);
                    game.Platform(left+entry.left,left+entry.right,entry.top);game.Platform(left+end.left,left+end.right,end.top);
                }
                for(int i=0;i<9;i++)
                {
                    var segment=AncientFieldExpansion.Segment(mission.Id,sector,i);float x=left+segment.left,top=segment.top;
                    if(mission.Kind!=2)game.Platform(x,left+segment.right,top);
                    game.StaticBox("Field route edge",new Vector2(left+(segment.left+segment.right)*.5f,(mission.Kind==2?0:top)+.025f),new Vector2(segment.right-segment.left,.05f),AncientGame.Cyan,3);
                    if(i%3==1)
                    {float fastTop=Mathf.Max(6.2f,top+2.2f);game.Platform(x+1,x+3.2f,fastTop);game.StaticBox("Expert field shortcut",new Vector2(x+2.1f,fastTop+.03f),new Vector2(2.2f,.05f),new Color(1,.82f,.2f),4);}
                }
                if(mission.Floor==0)for(int index=3;index<=6;index+=3)
                {
                    var segment=AncientFieldExpansion.Segment(mission.Id,sector,index);
                    float top=mission.Kind==2?0:segment.top,x=left+segment.left,width=segment.right-segment.left;
                    game.StaticBox("Visible field conveyor",new Vector2(x+width*.5f,top+.04f),new Vector2(width,.08f),index==3?AncientGame.Cyan:AncientGame.Orange,4);
                }
                terminals[mission.Id,sector]=AncientArt.Display("Field safety terminal",AncientArt.World(2),new Vector2(sectorLeft+6,1),5,transform);
                terminals[mission.Id,sector].transform.localScale=Vector3.one*.22f;terminals[mission.Id,sector].enabled=false;
                if(mission.Kind==3)
                {
                    powerStations[mission.Id,sector]=AncientArt.Display("Repeatable local energy panel",AncientArt.MetalProjectile(1),PowerPoint(mission.Id,sector),10,transform);
                    powerStations[mission.Id,sector].transform.localScale=Vector3.one*.5f;powerStations[mission.Id,sector].color=new Color(1,.82f,.25f);powerStations[mission.Id,sector].enabled=false;
                }
                if(sector>0)
                {
                    var gate=game.Box("Field investigation sector gate",new Vector2(sectorLeft-.4f,4.8f),new Vector2(.2f,10.4f),new Color(.2f,.7f,.8f,.35f),5);
                    gate.layer=8;fieldGates[mission.Id,sector-1]=gate.GetComponent<SpriteRenderer>();fieldGateSolids[mission.Id,sector-1]=gate.AddComponent<BoxCollider2D>();
                    fieldGates[mission.Id,sector-1].enabled=fieldGateSolids[mission.Id,sector-1].enabled=false;
                }
                for(int i=0;i<2;i++)
                {
                    float x=sectorLeft+43+i*44;
                    if(mission.Floor==0)
                    {var crane=new GameObject("Field moving crane").AddComponent<AncientMovingAnchor>();crane.Initialize(game,new Vector2(x,7.6f),mission.Id+sector*2+i);cranes.Add(crane);}
                    if(mission.Floor==2)new GameObject("Field security shutter").AddComponent<AncientSecurityShutter>().Initialize(game,new Vector2(x,6.4f),mission.Id+sector*2+i);
                    if(mission.Floor==3)new GameObject("Field ventilation").AddComponent<AncientCityVent>().Initialize(game,new Vector2(x,1.5f),3);
                    if(mission.Floor==4)new GameObject("Field crumble shortcut").AddComponent<AncientCrumbleLedge>().Initialize(game,new Vector2(x,6.8f));
                    if(mission.Floor==1)
                    {var buoy=new GameObject("Field blade rebound buoy").AddComponent<AncientAirTarget>();buoy.Initialize(game,new Vector2(x,6));buoys.Add(buoy);}
                }
            }
            foreach(float x in new[]{left-.2f,right+.2f})
            {var wall=game.Box("Field boundary",new Vector2(x,4),new Vector2(.4f,12),tint,0);wall.layer=8;wall.AddComponent<BoxCollider2D>();}
            AncientArt.Display("Field return doorway",AncientArt.World(3),new Vector2(left+3,0),1,transform).transform.localScale=Vector3.one*.65f;
            AncientArt.Display("Field evidence exit",AncientArt.World(3),new Vector2(right-5,0),1,transform).transform.localScale=Vector3.one*.65f;
            for(int i=0;i<3;i++)
            {
                var p=NodePoint(mission.Id,i);
                bool blade=UsesBlade(mission.Kind,i),person=mission.Kind==2&&i==1,receiver=mission.Kind==3&&i==2;
                nodes[mission.Id,i]=AncientArt.Display(mission.Tasks[i],person?AncientCitizenArt.ForCitizen(AncientCampaignRules.Witness(mission.Id)):blade?AncientArt.MetalProjectile(1):AncientArt.World(2),person?p-Vector2.up*1.2f:p,10,transform);
                nodes[mission.Id,i].transform.localScale=Vector3.one*(person?.9f:blade?.65f:receiver?.5f:.32f);
                nodes[mission.Id,i].color=person?Color.white:blade?new Color(1,.82f,.25f):receiver?new Color(.2f,1,.85f):new Color(.4f,.85f,1);nodes[mission.Id,i].enabled=false;
            }
        }
        readonly List<AncientAirTarget> buoys=new List<AncientAirTarget>();
        readonly List<AncientMovingAnchor> cranes=new List<AncientMovingAnchor>();
        static bool UsesBlade(int kind,int node){return kind==1||kind==2&&node==0||kind==3&&node==0;}
        static Vector2 NodePoint(int id,int index)
        {
            var mission=AncientCampaignData.Missions[id];float left=AncientCampaignRules.Left(id);
            var segment=AncientFieldExpansion.Segment(id,index,5);float x=left+(segment.left+segment.right)*.5f;
            if(mission.Kind==2)return new Vector2(index==2?left+416:x,1.2f);
            return new Vector2(x,segment.top+1.3f);
        }
        static Vector2 PowerPoint(int id,int sector)
        {var segment=AncientFieldExpansion.Segment(id,sector,4);return new Vector2(AncientCampaignRules.Left(id)+(segment.left+segment.right)*.5f,segment.top+1.3f);}
        public bool TryMovingAnchor(Vector2 point,out Transform target)
        {
            target=null;if(!Active)return false;
            foreach(var crane in cranes)if(((Vector2)crane.transform.position-point).sqrMagnitude<.04f){target=crane.transform;return true;}return false;
        }
        public bool ReadyForBoss(int floor){return legacy||AncientCampaignRules.ReadyForBoss(completed,floor);}
        public bool ReadyForLift(int destination){return legacy||destination!=1||AncientCampaignRules.FloorCount(completed,0)==4;}
        public bool ReadyForFinale {get{return legacy||AncientCampaignRules.FloorCount(completed,4)==4;}}
        public void WriteSave()
        {
            AncientGame.SetSaved("campaignComplete",completed);AncientGame.SetSaved("campaignSeen",seen);AncientGame.SetSaved("campaignBeforeSeen",beforeSeen);AncientGame.SetSaved("campaignAfterSeen",afterSeen);AncientGame.SetSaved("campaignActive",active);
            AncientGame.SetSaved("campaignTaskMission",taskMission);AncientGame.SetSaved("campaignTasks",tasks);
            AncientGame.SetSaved("campaignCheckpoint",checkpoint);AncientGame.SetSaved("campaignDefeated",defeated);
        }
        void Persist(){WriteSave();game.Save();}
        void ClearFoes()
        {
            foreach(var enemy in foes)if(enemy){enemy.gameObject.SetActive(false);game.enemies.Remove(enemy);Destroy(enemy.gameObject);}foes.Clear();foeSlots.Clear();encounterSector=-1;
            foreach(var shot in game.shots.ToArray())if(shot)shot.Remove();
        }
        bool ThreatsAlive(){foreach(var enemy in foes)if(enemy&&enemy.IsAlive)return true;return false;}
        void RecordDefeats()
        {
            int before=defeated;
            for(int i=0;i<foes.Count;i++)if(!foes[i]||!foes[i].IsAlive)defeated|=1<<foeSlots[i];
            if(defeated!=before){Refresh();Persist();}
        }
        void ActivateSector(int sector)
        {
            if(encounterSector==sector)return;RecordDefeats();ClearFoes();encounterSector=sector;
            int floor=AncientCampaignData.Missions[active].Floor;
            for(int i=0;i<AncientFieldExpansion.EnemiesPerSector;i++)
            {
                if((defeated&AncientFieldExpansion.EnemyBit(sector,i))!=0)continue;
                int ledge=AncientFieldExpansion.EnemySegment(sector,i);var segment=AncientFieldExpansion.Segment(active,ledge/9,ledge%9);
                float x=AncientCampaignRules.Left(active)+(segment.left+segment.right)*.5f,y=AncientCampaignData.Missions[active].Kind==2?0:segment.top;
                int kind=i==0?0:floor==3?4:floor==2?5:(active+sector)%3==0?2:1;
                if(kind==4)y+=2.5f;
                var enemy=new GameObject("Field criminal / sector "+(sector+1)).AddComponent<AncientEnemy>();enemy.Initialize(kind);
                enemy.transform.position=new Vector2(x,y);enemy.ConfigureDistrict(floor,false);foes.Add(enemy);foeSlots.Add(sector*2+i);game.enemies.Add(enemy);
            }
        }
        Vector2 CheckpointPoint()
        {return new Vector2(AncientCampaignRules.Left(active)+AncientFieldExpansion.EntranceOf(checkpoint),1.3f);}
        void RestoreEscort()
        {
            if(AncientCampaignData.Missions[active].Kind!=2||(tasks&2)==0||(tasks&4)!=0)return;
            float x=Mathf.Max(NodePoint(active,1).x,CheckpointPoint().x-1.2f);
            escort.transform.position=new Vector2(x,0);following=true;
        }
        void Begin(int id)
        {
            if(!AncientCampaignRules.CanStart(completed,id,game.Story.CurrentFloor,game.Story.Stage,game.City.Cleared))return;
            if(active==id)RecordDefeats();
            active=id;escortCycle=0;escortFrame=-1;escort.sprite=AncientCitizenArt.ForCitizen(AncientCampaignRules.Witness(id));
            if(taskMission!=id){taskMission=id;tasks=0;checkpoint=defeated=0;}
            checkpoint=AncientFieldExpansion.ResolveCheckpoint(checkpoint,tasks,defeated);following=false;
            ClearFoes();game.City.OnEnterFloor();game.hero.TravelTo(CheckpointPoint());ActivateSector(checkpoint);RestoreEscort();
            Refresh();Persist();game.SnapCameraToHero();game.BlockGameplayInput();game.Notice("현장 진입 / "+AncientCampaignData.Missions[id].Title+" · 안전 거점 E로 중단/복귀",4);
        }
        void ReturnCity()
        {
            int floor=game.Story.CurrentFloor,previous=active;RecordDefeats();ClearFoes();active=-1;following=false;escort.enabled=false;
            var citizen=AncientCampaignData.Citizens[GiverFor(floor,previous)];game.hero.TravelTo(new Vector2(citizen.X-1.4f,citizen.Y+1.3f));
            game.City.OnEnterFloor();Refresh();Persist();game.SnapCameraToHero();game.BlockGameplayInput();
        }
        public bool Retry(){if(!Active)return false;Begin(active);return true;}
        void Refresh()
        {
            for(int id=0;id<20;id++)for(int i=0;i<3;i++)
            {
                bool refill=AncientCampaignRules.RepeatableEnergyNode(AncientCampaignData.Missions[id].Kind,tasks,i);
                nodes[id,i].enabled=id==active&&(((tasks&(1<<i))==0&&AncientFieldExpansion.ObjectiveAvailable(tasks,i))||refill);
                terminals[id,i].enabled=id==active;
                terminals[id,i].color=i<=checkpoint?new Color(.3f,1,.8f):new Color(.3f,.45f,.5f);
                if(powerStations[id,i])powerStations[id,i].enabled=id==active&&(tasks&1)!=0&&(tasks&4)==0;
                if(i<2)
                {
                    bool closed=id==active&&i+1>AncientFieldExpansion.MaximumCheckpoint(tasks,defeated);
                    fieldGates[id,i].enabled=fieldGateSolids[id,i].enabled=closed;
                }
            }
            escort.enabled=Active&&AncientCampaignData.Missions[active].Kind==2&&(tasks&2)!=0&&(tasks&4)==0;
            if(Active)
            {
                var m=AncientCampaignData.Missions[active];objective=m.Title+" / ";
                for(int i=0;i<3;i++)objective+=((tasks&(1<<i))!=0?"[완료] ":"[ ] ")+m.Tasks[i]+(i<2?" · ":"");
            }
            else objective="";
        }
        void Mark(int index)
        {
            if((tasks&(1<<index))!=0||!AncientFieldExpansion.ObjectiveAvailable(tasks,index))return;tasks=AncientCampaignRules.Objective(tasks,index);Refresh();Persist();game.Sound(2);
            game.Feel.Signal("확보 / "+AncientCampaignData.Missions[active].Tasks[index],new Color(.65f,1,.85f),1.3f);
        }
        public bool TryBladeTarget(Vector2 origin,Vector2 direction)
        {
            if(!Active)return false;var m=AncientCampaignData.Missions[active];
            for(int i=0;i<3;i++)
            {
                Vector2 p=NodePoint(active,i),delta=p-origin;
                bool refill=AncientCampaignRules.RepeatableEnergyNode(m.Kind,tasks,i);
                if(((tasks&(1<<i))!=0&&!refill)||!AncientFieldExpansion.ObjectiveAvailable(tasks,i)||(refill&&Time.time<powerEmitReady)||!UsesBlade(m.Kind,i)||delta.sqrMagnitude>3.1f*3.1f||Vector2.Dot(delta.normalized,direction)<-.15f||Physics2D.Linecast(origin,p,1<<8))continue;
                if(refill)powerEmitReady=Time.time+.85f;Mark(i);game.hero.Combat.EmitParticles(p,24);game.Impact(p,new Color(1,.85f,.2f),true);return true;
            }
            if(m.Kind==3&&(tasks&1)!=0&&(tasks&4)==0&&Time.time>=powerEmitReady)for(int sector=0;sector<AncientFieldExpansion.SectorCount;sector++)
            {
                Vector2 point=PowerPoint(active,sector),delta=point-origin;
                if(delta.sqrMagnitude>3.1f*3.1f||Vector2.Dot(delta.normalized,direction)<-.15f||Physics2D.Linecast(origin,point,1<<8))continue;
                powerEmitReady=Time.time+.85f;game.hero.Combat.EmitParticles(point,24);game.Impact(point,new Color(1,.85f,.2f),true);return true;
            }
            foreach(var buoy in buoys)
            {
                if(!buoy||!buoy.Ready)continue;Vector2 delta=(Vector2)buoy.transform.position-origin;
                if(delta.sqrMagnitude<3.1f*3.1f&&Vector2.Dot(delta.normalized,direction)>-.15f&&!Physics2D.Linecast(origin,buoy.transform.position,1<<8)){buoy.Strike();return true;}
            }
            return false;
        }
        public void OnLaser(Vector2 start,Vector2 end)
        {
            if(!Active||AncientCampaignData.Missions[active].Kind!=3||(tasks&3)!=3)return;
            Vector2 p=NodePoint(active,2),delta=end-start;
            float t=delta.sqrMagnitude>.001f?Mathf.Clamp01(Vector2.Dot(p-start,delta)/delta.sqrMagnitude):0;
            if((p-start-delta*t).sqrMagnitude<1&&!Physics2D.Linecast(start,p,1<<8))Mark(2);
        }
        public bool TryInteract()
        {
            if(!game.Story||game.hero.bike||game.ArenaActive||(game.Finale&&game.Finale.Phase!=0))return false;
            if(Active)return InteractRoom();
            int id=NearestCitizen();if(id<0)return false;
            if(!game.hero.IsOnGround){game.Notice("착지한 뒤 주민과 대화하세요.");return true;}
            var npc=AncientCampaignData.Citizens[id];seen|=1<<id;Persist();
            int next=AncientCampaignRules.Next(completed,npc.Floor);
            if(next>=0&&id==GiverFor(npc.Floor,next)&&AncientCampaignRules.CanStart(completed,next,npc.Floor,game.Story.Stage,game.City.Cleared))
            {
                var m=AncientCampaignData.Missions[next];
                game.Story.Conversation(()=>Begin(next),npc.Role,
                    new string[]{m.Speaker,"백명"},
                    new string[]{m.Brief,m.Reply});
            }
            else
            {
                bool clue=false;foreach(string line in npc.Before)if(line.Contains("조커")){clue=true;break;}
                bool later=AncientCampaignRules.UseLaterDialogue(AncientCampaignRules.FloorCount(completed,npc.Floor)>=2,clue,(beforeSeen&(1<<id))!=0);
                if(later)afterSeen|=1<<id;else beforeSeen|=1<<id;Persist();
                string[] words=later?npc.After:npc.Before;var speakers=new string[words.Length+1];var script=new string[words.Length+1];
                for(int i=0;i<words.Length;i++){speakers[i]=npc.Name;script[i]=words[i];}
                speakers[words.Length]="백명";script[words.Length]="들었습니다. 현장 기록에 남기겠습니다.";
                game.Story.Conversation(null,npc.Role,speakers,script);
            }
            return true;
        }
        bool InteractRoom()
        {
            var m=AncientCampaignData.Missions[active];float left=AncientCampaignRules.Left(active);Vector2 hero=game.hero.transform.position;
            RecordDefeats();
            for(int sector=0;sector<=checkpoint;sector++)if(Mathf.Abs(hero.x-left-AncientFieldExpansion.EntranceOf(sector))<2.5f&&Mathf.Abs(hero.y-1.3f)<1.5f)
            {ReturnCity();game.Notice("현장 중단 / 조사 항목과 안전 거점은 저장됩니다.");return true;}
            if(hero.x>left+AncientCampaignRules.RoomWidth-10)
            {
                if(!AncientFieldExpansion.ExitReady(tasks,defeated)||ThreatsAlive()){game.Notice("조사 세 항목과 각 구간의 위협을 확인한 뒤 출구에서 E로 보고하세요.",3);return true;}
                int id=active;completed=AncientCampaignRules.Claim(completed,id);game.credits+=5;taskMission=-1;tasks=0;Persist();
                game.Story.Conversation(ReturnCity,3,new string[]{"백명",m.Speaker,"통신관"},
                    new string[]{m.Result,AncientCampaignData.Reactions[id],m.Clue});return true;
            }
            for(int i=0;i<3;i++)
            {
                if((tasks&(1<<i))!=0||!AncientFieldExpansion.ObjectiveAvailable(tasks,i)||UsesBlade(m.Kind,i)||m.Kind==3&&i==2||m.Kind==2&&i==2)continue;
                if((hero-NodePoint(active,i)).sqrMagnitude>2*2)continue;
                if(m.Kind==2&&i==1&&(tasks&1)==0){game.Notice("봉인 장치를 먼저 베어 통로를 여세요.");return true;}
                if(i==2&&AncientEvidenceQuestions.Find(active)!=null)
                {
                    if(!AncientEvidenceQuestions.CanCompare(active,tasks,i)){game.Notice("앞의 두 기록을 확보한 뒤 대조하세요.");return true;}
                    CompareEvidence();return true;
                }
                Mark(i);
                if(m.Kind==2&&i==1){escort.transform.position=NodePoint(active,1)-Vector2.up*1.2f;following=true;Refresh();}
                game.Story.Conversation(null,m.Kind==2?2:3,new string[]{m.Speaker,"백명"},new string[]{AncientCampaignData.Details[active][i],"확인했습니다. 사본을 남기겠습니다."});return true;
            }
            return true;
        }
        void CompareEvidence()
        {
            int mission=active;var question=AncientEvidenceQuestions.Find(mission);if(question==null)return;
            game.Story.AskEvidence(question.Prompt,new[]{AncientCampaignData.Details[mission][0],AncientCampaignData.Details[mission][1]},question.Options,
                answer=>
                {
                    if(active!=mission)return;
                    if(answer==question.Answer)
                    {
                        Mark(2);game.Story.Conversation(null,3,new[]{"통신관"},new[]{AncientCampaignData.Details[mission][2]});
                    }
                    else game.Story.Conversation(CompareEvidence,3,new[]{"통신관"},new[]{question.Feedback});
                });
        }
        int NearestCitizen()
        {
            int nearest=-1;float best=1.9f*1.9f;Vector2 hero=game.hero.transform.position;
            foreach(var npc in AncientCampaignData.Citizens)
            {
                if(npc.Floor!=game.Story.CurrentFloor)continue;float d=(hero-new Vector2(npc.X,npc.Y+1)).sqrMagnitude;
                if(d<best){best=d;nearest=npc.Id;}
            }
            return nearest;
        }
        void RecoverToCheckpoint()
        {
            RecordDefeats();checkpoint=AncientFieldExpansion.ResolveCheckpoint(checkpoint,tasks,defeated);
            ClearFoes();game.hero.TravelTo(CheckpointPoint());ActivateSector(checkpoint);RestoreEscort();Refresh();
            game.SnapCameraToHero();game.BlockGameplayInput();game.Notice("안전 거점 회수 / 확보한 기록과 제압한 위협은 유지됩니다.");
        }
        void Update()
        {
            if(!game.Story||!game.hero||!game.HasSession)return;
            for(int floor=1;floor<=4;floor++)
            {
                bool closed=!Active&&game.Story.CurrentFloor==floor&&!ReadyForBoss(floor);
                if(gateShown[floor]!=closed){gateShown[floor]=closed;gates[floor].enabled=gateSolids[floor].enabled=closed;}
            }
            if(!restored)
            {restored=true;if(restoreId>=0&&restoreId<20&&!AncientCampaignRules.Complete(completed,restoreId))Begin(restoreId);}
            if(!Active||game.Paused)return;
            RecordDefeats();
            float left=AncientCampaignRules.Left(active);Vector2 p=game.hero.transform.position;
            if(p.y<-2.5f||p.x<left-1||p.x>left+AncientCampaignRules.RoomWidth+1)
            {RecoverToCheckpoint();return;}
            int sector=AncientFieldExpansion.SectorAt(p.x-left),allowed=AncientFieldExpansion.MaximumCheckpoint(tasks,defeated);
            if(sector>allowed){RecoverToCheckpoint();return;}
            if(sector>checkpoint)
            {checkpoint=sector;Refresh();Persist();game.Notice("안전 거점 확보 / "+AncientFieldExpansion.Names[active][sector],2);}
            ActivateSector(sector);
            if(AncientCampaignData.Missions[active].Floor==0&&game.hero.IsOnGround)
            {
                for(int index=3;index<=6;index+=3)
                {
                    var segment=AncientFieldExpansion.Segment(active,sector,index);float x=left+segment.left,top=AncientCampaignData.Missions[active].Kind==2?0:segment.top;
                    if(p.x>=x&&p.x<=left+segment.right&&Mathf.Abs(p.y-(top+1.05f))<.3f)game.hero.ConveyorDrift(index==3?1:-1);
                }
            }
            if(AncientCampaignData.Missions[active].Kind==2&&following&&(tasks&4)==0)
            {
                Vector2 witness=escort.transform.position;float previousX=witness.x;bool blocked=false;
                foreach(var enemy in foes)if(enemy&&enemy.IsAlive&&Mathf.Abs(enemy.transform.position.x-witness.x)<8)blocked=true;
                if(!blocked&&Mathf.Abs(p.x-witness.x)<12&&p.x>witness.x+1.2f)
                    witness.x=Mathf.MoveTowards(witness.x,p.x-1.2f,Mathf.Clamp(Mathf.Abs(game.hero.HorizontalSpeed)+1,4.3f,12)*Time.deltaTime);
                float moved=Mathf.Abs(witness.x-previousX);escortCycle+=moved*2.2f;
                int frame=moved>.0001f?(int)escortCycle%4:-1;
                if(frame!=escortFrame)
                {escortFrame=frame;int person=AncientCampaignRules.Witness(active);escort.sprite=frame<0?AncientCitizenArt.ForCitizen(person):AncientArt.CitizenWalk(person,frame);}
                escort.transform.position=witness;
                if(witness.x>=left+416){following=false;Mark(2);}
                else if(blocked&&Time.time>=guardNoticeAt){guardNoticeAt=Time.time+5;game.Notice("위협을 먼저 제압하세요. 주민은 안전이 확인되면 따라옵니다.",2);}
            }
        }
        void OnGUI()
        {
            if(!game.Story||game.Paused||game.MainMenu||Event.current.type!=EventType.Repaint)return;
            if(label==null)label=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=14,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            var matrix=GUI.matrix;var color=GUI.color;GUI.matrix=Matrix4x4.identity;
            if(Active)
            {
                int sector=CurrentSector;
                GUI.Label(new Rect(Screen.width/2-380,85,760,65),(sector+1)+" / 3 · "+AncientFieldExpansion.Names[active][sector]+"\n노란 판: 베기 / 입자: 우클릭 / 청록 수광부: 레이저 / 기록·증언: E\n안전 거점 E: 중단 · 동쪽 출구 E: 조사 보고",label);
                for(int i=0;i<=checkpoint;i++)
                {
                    var terminal=game.View.WorldToScreenPoint(new Vector2(AncientCampaignRules.Left(active)+AncientFieldExpansion.EntranceOf(i),2.3f));
                    if(terminal.x>0&&terminal.x<Screen.width)GUI.Label(new Rect(terminal.x-110,Screen.height-terminal.y-25,220,45),"안전 거점 "+(i+1)+" / E: 도시 복귀",label);
                }
                int next=AncientFieldExpansion.MaximumCheckpoint(tasks,defeated);
                if(next<2)
                {
                    var gate=game.View.WorldToScreenPoint(new Vector2(AncientCampaignRules.Left(active)+AncientFieldExpansion.SectorStart(next+1)-.4f,5));
                    if(gate.x>-150&&gate.x<Screen.width+150)GUI.Label(new Rect(gate.x-150,Screen.height-gate.y-30,300,60),"다음 구간 / 조사 목표 확보 + 위협 제압",label);
                }
                if(AncientCampaignData.Missions[active].Kind==2&&following&&(tasks&4)==0)
                {
                    var witness=game.View.WorldToScreenPoint(escort.transform.position+Vector3.up*2.1f);
                    float x=Mathf.Clamp(witness.x,150,Mathf.Max(150,Screen.width-150));
                    GUI.Label(new Rect(x-150,Screen.height-210,300,45),"보호 대상 / 가까이 동행 · 위협 우선 제압",label);
                }
                if(AncientCampaignData.Missions[active].Floor==0)for(int index=3;index<=6;index+=3)
                {
                    var segment=AncientFieldExpansion.Segment(active,sector,index);float x=AncientCampaignRules.Left(active)+(segment.left+segment.right)*.5f,top=AncientCampaignData.Missions[active].Kind==2?0:segment.top;
                    var point=game.View.WorldToScreenPoint(new Vector2(x,top+.7f));
                    if(point.x>0&&point.x<Screen.width)GUI.Label(new Rect(point.x-100,Screen.height-point.y-20,200,30),index==3?"운송 벨트 →":"← 역방향 벨트",label);
                }
                for(int i=0;i<3;i++)if((tasks&(1<<i))==0)
                {
                    var p=game.View.WorldToScreenPoint(NodePoint(active,i)+Vector2.up*.7f);
                    if(p.x>-140&&p.x<Screen.width+140)GUI.Label(new Rect(p.x-140,Screen.height-p.y-30,280,60),(UsesBlade(AncientCampaignData.Missions[active].Kind,i)?"베기 / ":AncientCampaignData.Missions[active].Kind==3&&i==2?"레이저 / ":AncientCampaignData.Missions[active].Kind==2&&i==2?"보호 / ":"E / ")+AncientCampaignData.Missions[active].Tasks[i],label);
                }
                if(AncientCampaignData.Missions[active].Kind==3&&(tasks&1)!=0&&(tasks&4)==0)
                {
                    var power=game.View.WorldToScreenPoint(PowerPoint(active,sector)+Vector2.up*.7f);
                    if(power.x>-120&&power.x<Screen.width+120)GUI.Label(new Rect(power.x-120,Screen.height-power.y-25,240,45),"보조 전력 패널 / 베기: 입자 재채취",label);
                }
            }
            else if(!(game.Finale&&game.Finale.Phase!=0))
            {
                int nearest=NearestCitizen();
                if(nearest>=0)
                {
                    var npc=AncientCampaignData.Citizens[nearest];var p=game.View.WorldToScreenPoint(new Vector2(npc.X,npc.Y+2.2f));
                    GUI.Label(new Rect(p.x-170,Screen.height-p.y-25,340,55),npc.Name+" / E: 대화·현장 조사",label);
                }
                int floor=game.Story.CurrentFloor;
                if(game.Story.Stage>=8)
                {
                    int nextCase=AncientCampaignRules.Next(completed,floor);
                    if(nextCase>=0)
                    {
                        var giver=AncientCampaignData.Citizens[GiverFor(floor,nextCase)];
                        var pixel=game.View.WorldToScreenPoint(new Vector2(giver.X,giver.Y+2));
                        float x=Mathf.Clamp(pixel.x,100,Mathf.Max(100,Screen.width-100));
                        if(Mathf.Abs(game.hero.transform.position.x-giver.X)>4)
                            GUI.Label(new Rect(x-100,Screen.height-195,200,30),(giver.X<game.hero.transform.position.x?"← ":"→ ")+giver.Name,label);
                    }
                }
                if(game.Story.Stage>=8)
                {int next=AncientCampaignRules.Next(completed,floor);GUI.Label(new Rect(Screen.width/2-330,Screen.height-145,660,50),next==19&&(game.City.Cleared&16)==0?"전방 저격수와 대치 / 동결된 통행 심사의 단서 확인":next>=0?"구역 사건 "+AncientCampaignRules.FloorCount(completed,floor)+" / 4 · "+AncientCampaignData.Citizens[GiverFor(floor,next)].Name+"에게 다음 현장 확인":"구역 조사 완료 / 전투와 출입 권한 확인",label);}
            }
            GUI.matrix=matrix;GUI.color=color;
        }
        public void DrawJournal(Rect area,GUIStyle text,GUIStyle small)
        {
            if(journalFloor<0)journalFloor=game.Story.CurrentFloor;
            for(int floor=0;floor<5;floor++)if(GUI.Button(new Rect(area.x+floor*165,area.y,155,30),AncientCityLayout.Name(floor),AncientHUD.Button)){journalFloor=floor;journalMission=journalCitizen=-1;}
            if(GUI.Button(new Rect(area.x,area.y+40,220,30),journalPeople?"사건 기록 보기":"주민 대화 기록",AncientHUD.Button))journalPeople=!journalPeople;
            if(journalPeople&&journalCitizen>=0&&journalCitizen/5==journalFloor)
            {
                var npc=AncientCampaignData.Citizens[journalCitizen];GUI.Label(new Rect(area.x,area.y+85,area.width,30),npc.Name,text);float y=area.y+125;
                if((beforeSeen&(1<<npc.Id))!=0)foreach(var words in npc.Before){GUI.Label(new Rect(area.x,y,area.width,58),words,small);y+=62;}
                if((afterSeen&(1<<npc.Id))!=0)foreach(var words in npc.After){GUI.Label(new Rect(area.x,y,area.width,58),words,small);y+=62;}
                if((beforeSeen&(1<<npc.Id))==0&&(afterSeen&(1<<npc.Id))==0)GUI.Label(new Rect(area.x,y,area.width,60),"현장 브리핑을 들었다. 주변에서 다시 대화하면 주민의 이야기를 들을 수 있다.",small);
                if(GUI.Button(new Rect(area.x+650,area.y+40,180,30),"주민 목록",AncientHUD.Button))journalCitizen=-1;
            }
            else if(journalPeople)
            {
                for(int i=0;i<5;i++)
                {
                    var npc=AncientCampaignData.Citizens[journalFloor*5+i];bool known=(seen&(1<<npc.Id))!=0;
                    GUI.enabled=known;if(GUI.Button(new Rect(area.x+700,area.y+85+i*63,145,23),"대화 기록",AncientHUD.Button))journalCitizen=npc.Id;GUI.enabled=true;
                    GUI.Label(new Rect(area.x,area.y+85+i*63,680,23),known?npc.Name:"아직 만나지 않은 주민",small);
                    GUI.Label(new Rect(area.x,area.y+109+i*63,area.width,38),known?((afterSeen&(1<<npc.Id))!=0?npc.After[0]:(beforeSeen&(1<<npc.Id))!=0?npc.Before[0]:"현장 안내를 들었다. 다음 현장은 이 주민에게 확인한다."):"해당 구역에서 주민과 대화하면 기록됩니다.",small);
                }
            }
            else if(journalMission>=0&&journalMission/4==journalFloor&&AncientCampaignRules.Complete(completed,journalMission))
            {
                var m=AncientCampaignData.Missions[journalMission];GUI.Label(new Rect(area.x,area.y+85,area.width,30),m.Title,text);
                for(int i=0;i<3;i++)GUI.Label(new Rect(area.x,area.y+128+i*67,area.width,60),m.Tasks[i]+" / "+AncientCampaignData.Details[journalMission][i],small);
                GUI.Label(new Rect(area.x,area.y+334,area.width,58),m.Clue,small);
                if(GUI.Button(new Rect(area.x+650,area.y+40,180,30),"사건 목록",AncientHUD.Button))journalMission=-1;
            }
            else for(int i=0;i<4;i++)
            {
                int id=journalFloor*4+i;var m=AncientCampaignData.Missions[id];bool done=AncientCampaignRules.Complete(completed,id);
                GUI.enabled=done;if(GUI.Button(new Rect(area.x+700,area.y+88+i*74,145,26),"확보 자료",AncientHUD.Button))journalMission=id;GUI.enabled=true;
                GUI.Label(new Rect(area.x,area.y+88+i*74,680,26),(done?"[완료] ":active==id?"[진행] ":"[미해결] ")+m.Title,text);
                GUI.Label(new Rect(area.x,area.y+116+i*74,area.width,42),done?m.Clue:"현장 조사와 증언을 통해 확인합니다.",small);
            }
        }
    }
}
