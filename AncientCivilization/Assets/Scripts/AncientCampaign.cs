using System.Collections.Generic;
using UnityEngine;
namespace Plugin.AncientCivilization
{
    // A chapter is one physical road. Case milestones advance without moving the player.
    public sealed class AncientCampaign : MonoBehaviour
    {
        int restMission=-1,restSlot=-1;readonly SpriteRenderer[,] restBeacons=new SpriteRenderer[8,24];
        AncientGame game;int completed,seen,beforeSeen,afterSeen,chapters,active=-1,restoreId=-1,taskMission=-1,tasks,checkpoint,defeated,encounterSector=-1;bool restored,legacy;
        readonly SpriteRenderer[,] nodes=new SpriteRenderer[20,3];readonly SpriteRenderer[] citizens=new SpriteRenderer[25];
        readonly SpriteRenderer[,] fieldGates=new SpriteRenderer[20,2],terminals=new SpriteRenderer[20,3],powerStations=new SpriteRenderer[20,3];
        readonly BoxCollider2D[,] fieldGateSolids=new BoxCollider2D[20,2];
        readonly List<AncientEnemy> foes=new List<AncientEnemy>();readonly List<int> foeSlots=new List<int>();SpriteRenderer escort,rescuedReha;bool following;
        const string RehaName="야간 작업자 재하";
        GUIStyle label;string objective="";int journalFloor=-1,journalMission=-1,journalCitizen=-1;bool journalPeople,routeJournal;float guardNoticeAt,powerEmitReady,escortCycle;int escortFrame=-1;
        GUIStyle fieldTitleStyle,fieldBodyStyle,fieldHintStyle,fieldWorldStyle,fieldEdgeStyle;float fieldStyleScale=-1;
        int uiMission=-1,uiSector=-1,uiNext=-2,uiGuideKind=-1,uiGuideId=-1,edgeMetres=-1,edgeDirection=-1;bool uiFollowing;
        bool fieldGuideVisible;Rect fieldGuideRect;
        string fieldTitle="",fieldNext="",fieldHint="",guideCaption="",edgeText="";
        readonly string[] fieldNodeLabels=new string[3];
        static readonly string[] terminalLabels={"안전 거점 1 / 자동 저장 · E: 확인","안전 거점 2 / 자동 저장 · E: 확인","안전 거점 3 / 자동 저장 · E: 확인"};
        readonly SpriteRenderer[] caseExits=new SpriteRenderer[20];readonly BoxCollider2D[] caseExitSolids=new BoxCollider2D[20];
        readonly SpriteRenderer[] chapterEntries=new SpriteRenderer[5];readonly BoxCollider2D[] chapterEntrySolids=new BoxCollider2D[5];
        readonly int[] routeTasks=new int[20],routeCheckpoints=new int[20],routeDefeated=new int[20];
        AncientWorkshopCircuit workshopCircuit;
        public static readonly string[] ProgressKeys=BuildProgressKeys();
        static string[] BuildProgressKeys()
        {
            var keys=new string[60];for(int id=0;id<20;id++)
            {keys[id*3]="routeTask"+id;keys[id*3+1]="routeCheckpoint"+id;keys[id*3+2]="routeDefeated"+id;}
            return keys;
        }
        static readonly string[] guideArrows={"◀ ","▶ ","▲ ","▼ "};
        public bool Active {get{return active>=0;}}
        public int CurrentRoom {get{return active;}}
        public int CurrentSector {get{return Active&&game.hero?AncientFieldExpansion.SectorAt(active,game.hero.transform.position.x-AncientCampaignRules.Left(active)):-1;}}
        public int EarnedCheckpoint {get{return checkpoint;}}
        public int DefeatedMask {get{return defeated;}}
        public int CompletedMask {get{return completed;}}
        public int RecordedTasks(int mission)
        {
            if(mission<0||mission>=routeTasks.Length)return 0;
            return AncientCampaignRules.Complete(completed,mission)?7:mission==active?tasks:routeTasks[mission];
        }
        public bool Legacy {get{return legacy;}}
        public string ObjectiveText {get{return objective;}}
        public bool TryDialogueActor(string speaker,out Transform actor,out float headOffset)
        {
            actor=null;headOffset=1.9f;
            if(string.IsNullOrEmpty(speaker)||!game||!game.Story)return false;
            if(Active&&following&&escort&&escort.enabled)
            {
                string witness=AncientCampaignData.Citizens[AncientCampaignRules.Witness(active)].Name;
                if(speaker==AncientCampaignData.Missions[active].Speaker||(active==2?IsRehaSpeaker(speaker):MatchesDialogueName(speaker,witness)))
                {actor=escort.transform;headOffset=DialogueHeadOffset(escort);return true;}
            }
            if(rescuedReha&&rescuedReha.enabled&&rescuedReha.gameObject.activeInHierarchy&&IsRehaSpeaker(speaker))
            {actor=rescuedReha.transform;headOffset=DialogueHeadOffset(rescuedReha);return true;}
            foreach(var citizen in AncientCampaignData.Citizens)
            {
                if(citizen.Floor!=game.Story.CurrentFloor||!MatchesDialogueName(speaker,citizen.Name))continue;
                var view=citizens[citizen.Id];if(!view||!view.enabled||!view.gameObject.activeInHierarchy)continue;
                actor=view.transform;headOffset=DialogueHeadOffset(view);return true;
            }
            return false;
        }
        static bool IsRehaSpeaker(string speaker){return speaker=="재하"||MatchesDialogueName(speaker,RehaName);}
        static bool MatchesDialogueName(string speaker,string name)
        {
            if(speaker==name)return true;int space=name.LastIndexOf(' ');
            if(space<0)return false;int length=name.Length-space;
            return speaker.Length>=length&&string.Compare(speaker,speaker.Length-length,name,space,length,System.StringComparison.Ordinal)==0;
        }
        static float DialogueHeadOffset(SpriteRenderer view)
        {return Mathf.Clamp(view.bounds.max.y-view.transform.position.y,.9f,2.4f);}
        public float CameraLeft {get{return AncientCityLayout.Left(game.Story.CurrentFloor)+5;}}
        public float CameraRight {get{return AncientCityLayout.Right(game.Story.CurrentFloor)-6;}}
        public bool ChapterStarted(int floor){return (chapters&(1<<floor))!=0;}
        static readonly int[] givers={3,5,10,15,20};
        readonly SpriteRenderer[] gates=new SpriteRenderer[5];readonly BoxCollider2D[] gateSolids=new BoxCollider2D[5];readonly bool[] gateShown=new bool[5];
        static int GiverFor(int floor,int mission){return floor==4&&mission==19?24:givers[floor];}
        public void Initialize(AncientGame owner)
        {
            game=owner;completed=AncientCampaignRules.SanitizeMask(AncientGame.Saved("campaignComplete"));seen=AncientGame.Saved("campaignSeen");beforeSeen=AncientGame.Saved("campaignBeforeSeen");afterSeen=AncientGame.Saved("campaignAfterSeen");
            restMission=AncientGame.Saved("campaignRestMission",-1);restSlot=AncientGame.Saved("campaignRestSlot",-1);
            restoreId=AncientGame.Saved("campaignActive",-1);taskMission=AncientGame.Saved("campaignTaskMission",-1);tasks=AncientGame.Saved("campaignTasks")&7;
            checkpoint=AncientGame.Saved("campaignCheckpoint");defeated=AncientFieldExpansion.SanitizeEnemies(AncientGame.Saved("campaignDefeated"));
            chapters=AncientCampaignRules.RestoreChapters(AncientGame.Saved("campaignChapterStarted"),completed,restoreId);
            for(int id=0;id<20;id++)
            {
                routeTasks[id]=AncientGame.Saved(ProgressKeys[id*3])&7;
                routeDefeated[id]=AncientFieldExpansion.SanitizeEnemies(AncientGame.Saved(ProgressKeys[id*3+2]));
                routeCheckpoints[id]=AncientFieldExpansion.ResolveCheckpoint(AncientGame.Saved(ProgressKeys[id*3+1]),routeTasks[id],routeDefeated[id]);
            }
            if(taskMission>=0&&taskMission<20&&!AncientCampaignRules.Complete(completed,taskMission))
            {routeTasks[taskMission]=tasks;routeDefeated[taskMission]=defeated;routeCheckpoints[taskMission]=checkpoint;}
            if(restMission<0||restMission>=8||restSlot<0||restSlot>=24||restSlot/8>routeCheckpoints[restMission]){restMission=-1;restSlot=-1;}
            legacy=AncientGame.Saved("campaignLegacy")!=0||(AncientGame.Saved("campaignVersion")==0&&AncientGame.Saved("questStage")>0);
            AncientGame.SetSaved("campaignVersion",AncientCampaignRules.RouteVersion);AncientGame.SetSaved("campaignLegacy",legacy?1:0);
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
            escort=AncientArt.Display("Protected witness",AncientCitizenArt.ForCitizen(6),Vector2.zero,9,transform);escort.transform.localScale=Vector3.one*.9f;escort.enabled=false;
            rescuedReha=AncientArt.Display("Rescued night worker Reha",AncientCitizenArt.ForCitizen(4),new Vector2(AncientChapterLayout.CaseRight(2)-16,0),9,transform);
            rescuedReha.transform.localScale=Vector3.one*.9f;rescuedReha.enabled=false;Refresh();
        }
        void BuildRoom(AncientMission mission)
        {
            float left=AncientCampaignRules.Left(mission.Id),right=AncientChapterLayout.CaseRight(mission.Id);
            if(mission.Id%4==0)
            {
                var start=game.Box("Chapter assignment checkpoint",new Vector2(left-2,4.8f),new Vector2(.2f,10.4f),new Color(.2f,.7f,.8f,.35f),5);
                start.layer=8;chapterEntries[mission.Floor]=start.GetComponent<SpriteRenderer>();chapterEntrySolids[mission.Floor]=start.AddComponent<BoxCollider2D>();
            }
            if(mission.Id<8)
            {
                for(int s=0;s<3;s++)for(int b=0;b<4;b++)
                {
                    float origin=left+AncientFieldExpansion.SectorStart(mission.Id,s)+b*AncientFieldExpansion.SectorSpan;
                    if(AncientFieldExpansion.TallStreetBay(mission.Id,s,b))
                    {game.Ceiling(origin,origin+12,10);game.Ceiling(origin+12,origin+78,18);game.Ceiling(origin+78,origin+208,10);}
                    else game.Ceiling(origin,origin+208,10);
                }
            }
            else game.Ceiling(left,right,10);
            game.Platform(left,right,-4.5f);
            Color tint=mission.Floor==0?new Color(.12f,.17f,.2f):mission.Floor==1?new Color(.13f,.21f,.2f):mission.Floor==2?new Color(.12f,.18f,.28f):mission.Floor==3?new Color(.24f,.23f,.19f):new Color(.19f,.13f,.24f);
            Color facade=tint;facade.a=.68f;Color window=tint*1.55f;window.a=.30f;
            for(int sector=0;sector<AncientFieldExpansion.SectorCount;sector++)
            {
                float sectorLeft=left+AncientFieldExpansion.SectorStart(mission.Id,sector);
                bool jumping=AncientFieldExpansion.IsJumpSector(mission.Id,sector);
                for(int block=0;block<AncientFieldExpansion.Blocks(mission.Id);block++)
                    AncientFieldScenery.Build(game,mission.Floor,sectorLeft+block*AncientFieldExpansion.SectorSpan,(mission.Id+sector+block)%4,AncientFieldExpansion.SectorSpan);
                int facades=Mathf.CeilToInt(AncientFieldExpansion.Span(mission.Id)/12f);float facadeStep=AncientFieldExpansion.Span(mission.Id)/(float)facades;
                for(int i=0;i<facades;i++)
                {
                    float x=sectorLeft+(i+.5f)*facadeStep;
                    game.StaticBox("Investigation facade",new Vector2(x,4.5f),new Vector2(facadeStep-.6f,9),facade,-8);
                    game.StaticBox("Investigation window",new Vector2(x,6.6f),new Vector2(5,1.4f),window,-7);
                    game.StaticBox("District floor stripe",new Vector2(x,9.88f),new Vector2(8,.06f),new Color(.2f,.65f,.7f),3);
                }
                if(AncientFieldExpansion.HasStreetDesign(mission.Id,sector))BuildStreet(mission,left,sector);
                else if(!jumping){var road=AncientFieldExpansion.GroundRoad(mission.Id,sector);game.Platform(left+road.left,left+road.right,road.top);}
                else
                {
                    for(int block=0;block<AncientFieldExpansion.Blocks(mission.Id);block++)
                    {var entry=AncientFieldExpansion.BlockLanding(mission.Id,sector,block,false);var end=AncientFieldExpansion.BlockLanding(mission.Id,sector,block,true);
                    game.Platform(left+entry.left,left+entry.right,entry.top);game.Platform(left+end.left,left+end.right,end.top); }
                }
                for(int block=0;block<AncientFieldExpansion.Blocks(mission.Id);block++)for(int i=0;i<9;i++)
                {
                    var segment=AncientFieldExpansion.BlockSegment(mission.Id,sector,block,i);float x=left+segment.left,top=segment.top;
                    if(jumping)game.Platform(x,left+segment.right,top);
                    if(jumping||mission.Id>=8)game.StaticBox("Field route edge",new Vector2(left+(segment.left+segment.right)*.5f,(mission.Kind==2?0:top)+.025f),new Vector2(segment.right-segment.left,.05f),AncientGame.Cyan,3);
                    if(jumping&&i%3==1)
                        BuildExpertCatwalk(left,AncientFieldExpansion.BlockExpertSegment(mission.Id,sector,block,i));
                }
                if(mission.Floor==0)for(int index=3;index<=6;index+=3)
                {
                    var segment=AncientFieldExpansion.Segment(mission.Id,sector,index);
                    float top=mission.Kind==2?0:segment.top,x=left+segment.left,width=segment.right-segment.left;
                    game.StaticBox("Visible field conveyor",new Vector2(x+width*.5f,top+.04f),new Vector2(width,.08f),index==3?AncientGame.Cyan:AncientGame.Orange,4);
                }
                if(mission.Id<8)for(int pad=0;pad<8;pad++)
                {
                    float px,py;if(!AncientFieldExpansion.TryRestPoint(mission.Id,sector,pad,out px,out py))continue;
                    var beacon=AncientArt.Display("Traversal retry beacon",AncientArt.World(2),new Vector2(left+px,.45f),5,transform);
                    beacon.transform.localScale=Vector3.one*.16f;beacon.color=new Color(.3f,.7f,.8f);restBeacons[mission.Id,sector*8+pad]=beacon;
                    game.StaticBox("Safe landing paint",new Vector2(left+px,.04f),new Vector2(3,.08f),new Color(.25f,.85f,.75f),4);
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
                if(jumping)for(int block=0;block<AncientFieldExpansion.Blocks(mission.Id);block++)for(int i=0;i<2;i++)
                {
                    float x=sectorLeft+block*AncientFieldExpansion.SectorSpan+63+i*68;
                    if(mission.Floor==0)
                    {var crane=new GameObject("Field moving crane").AddComponent<AncientMovingAnchor>();crane.Initialize(game,new Vector2(x,7.6f),mission.Id+sector*2+i);cranes.Add(crane);}
                    if(mission.Floor==2)new GameObject("Field security shutter").AddComponent<AncientSecurityShutter>().Initialize(game,new Vector2(x,6.4f),mission.Id+sector*2+i);
                    if(mission.Floor==3)new GameObject("Field ventilation").AddComponent<AncientCityVent>().Initialize(game,new Vector2(x,1.5f),3);
                    if(mission.Floor==4)new GameObject("Field crumble shortcut").AddComponent<AncientCrumbleLedge>().Initialize(game,new Vector2(x,6.8f));
                    if(mission.Floor==1)
                    {var buoy=new GameObject("Field blade rebound buoy").AddComponent<AncientAirTarget>();buoy.Initialize(game,new Vector2(x,6));buoys.Add(buoy);}
                }
            }
            var exit=game.Box("Forward route security seal",new Vector2(right-.2f,4.8f),new Vector2(.2f,10.4f),new Color(.2f,.7f,.8f,.35f),5);
            exit.layer=8;caseExits[mission.Id]=exit.GetComponent<SpriteRenderer>();caseExitSolids[mission.Id]=exit.AddComponent<BoxCollider2D>();
            if(mission.Id%4<3)
            {
                game.Platform(right,AncientCampaignRules.Left(mission.Id+1),0);game.Ceiling(right,AncientCampaignRules.Left(mission.Id+1),10);
                AncientFieldScenery.Build(game,mission.Floor,right,mission.Id%4,AncientChapterLayout.BridgeWidth);
            }
            for(int i=0;i<3;i++)
            {
                var p=NodePoint(mission.Id,i);
                bool blade=UsesBlade(mission.Kind,i),person=mission.Kind==2&&i==1,receiver=mission.Kind==3&&i==2;
                nodes[mission.Id,i]=AncientArt.Display(mission.Tasks[i],person?AncientCitizenArt.ForCitizen(AncientCampaignRules.Witness(mission.Id)):blade?AncientArt.MetalProjectile(1):AncientArt.World(2),person?p-Vector2.up*1.2f:p,10,transform);
                nodes[mission.Id,i].transform.localScale=Vector3.one*(person?.9f:blade?.65f:receiver?.5f:.32f);
                nodes[mission.Id,i].color=person?Color.white:blade?new Color(1,.82f,.25f):receiver?new Color(.2f,1,.85f):new Color(.4f,.85f,1);nodes[mission.Id,i].enabled=false;
            }
            if(mission.Id==1)
            {
                var lampPositions=new Vector2[3];for(int sector=0;sector<3;sector++)lampPositions[sector]=NodePoint(1,sector);
                workshopCircuit=new AncientWorkshopCircuit(game,transform,left,lampPositions,RecordedTasks(1));
            }
        }
        void BuildStreet(AncientMission mission,float left,int sector)
        {
            for(int block=0;block<AncientFieldExpansion.Blocks(mission.Id);block++)
            {
                for(int i=0;i<AncientFieldExpansion.StreetPieceCount(mission.Id,sector,block);i++)
                {
                    var piece=AncientFieldExpansion.StreetPiece(mission.Id,sector,block,i);
                    game.Platform(left+piece.left,left+piece.right,piece.top);
                    // Grounded machinery/roof foundations make raised surfaces read as
                    // industrial architecture rather than unexplained floating slabs.
                    if(piece.top>1)
                    {
                        float x=left+(piece.left+piece.right)*.5f;
                        var foundation=new GameObject("Solid street structural foundation");foundation.layer=8;foundation.transform.SetParent(transform,false);
                        foundation.transform.position=new Vector2(x,(piece.top-5)*.5f);foundation.AddComponent<BoxCollider2D>().size=new Vector2(piece.right-piece.left,piece.top+3);
                        game.StaticBox("Street structural foundation",new Vector2(x,(piece.top-4)*.5f),new Vector2(piece.right-piece.left,piece.top+4),new Color(.08f,.14f,.18f),-1);
                    }
                    if(piece.top!=0)
                    {
                        foreach(float edge in new[]{piece.left+.25f,piece.right-.25f})
                            game.StaticBox("Traversal edge warning paint",new Vector2(left+edge,piece.top+.035f),new Vector2(.38f,.07f),new Color(1,.76f,.3f),4);
                    }
                }
                if(AncientFieldExpansion.TallStreetBay(mission.Id,sector,block))
                {
                    var core=AncientFieldExpansion.StreetObstacle(mission.Id,sector,block);float width=core.right-core.left;
                    game.StaticBox("Tall service hall rear wall",new Vector2(left+core.left+5,13),new Vector2(66,10),new Color(.035f,.075f,.11f),-9);
                    for(int rib=0;rib<5;rib++)game.StaticBox("Tall service hall structural ribs",new Vector2(left+core.left-17+rib*12,13),new Vector2(.22f,10),new Color(.12f,.24f,.29f),-4);
                    var solid=game.Box("Vertical service shaft machine",new Vector2(left+(core.left+core.right)*.5f,2.5f),new Vector2(width,13),new Color(.1f,.19f,.23f),1);
                    solid.layer=8;solid.AddComponent<BoxCollider2D>();
                    for(int rib=0;rib<5;rib++)game.StaticBox("Shaft machine cooling ribs",new Vector2(left+(core.left+core.right)*.5f,1+rib*1.7f),new Vector2(width-.5f,.15f),new Color(.25f,.47f,.51f),2);
                    game.StaticBox("Shaft machine walkable crown",new Vector2(left+(core.left+core.right)*.5f,core.top),new Vector2(width,.12f),AncientGame.Cyan,4);
                }
                for(int i=0;i<3;i++)BuildExpertCatwalk(left,AncientFieldExpansion.StreetShortcut(mission.Id,sector,block,i));
                var duct=AncientFieldExpansion.StreetBaffle(mission.Id,sector,block);
                game.Ceiling(left+duct.left,left+duct.right,duct.top);
                float origin=left+AncientFieldExpansion.SectorStart(mission.Id,sector)+block*AncientFieldExpansion.SectorSpan;
                // Hoists lead toward the upper route. The landing and next socket
                // remain visible within one camera view and a single wire reach.
                for(int i=0;i<3;i++)
                {
                    float x=origin+21+i*20,y=AncientFieldExpansion.StreetHookHeight(mission.Id,sector,block,i),roof=AncientFieldExpansion.TallStreetBay(mission.Id,sector,block)?18:10;game.anchors.Add(new Vector2(x,y));
                    game.StaticBox("Street hoist suspension",new Vector2(x,(roof+y)*.5f),new Vector2(.08f,roof-y),new Color(.25f,.42f,.46f),2);
                    game.WorldBatch.Sprite(AncientArt.WireParticle(),new Vector2(x,y),Vector2.one*.65f,AncientGame.Cyan,5);
                }
                if(mission.Floor==1)
                {var buoy=new GameObject("Alley blade rebound shortcut").AddComponent<AncientAirTarget>();buoy.Initialize(game,new Vector2(origin+54,5.6f));buoys.Add(buoy);}
            }
        }
        void BuildExpertCatwalk(float roomLeft,AncientTraversalLayout.Ledge span)
        {
            float width=span.right-span.left,x=roomLeft+(span.left+span.right)*.5f,depth=AncientFieldExpansion.ExpertLedgeDepth;
            var ledge=new GameObject("Expert service catwalk");ledge.layer=8;ledge.transform.SetParent(transform,false);
            ledge.transform.position=new Vector2(x,span.top-depth*.5f);ledge.AddComponent<BoxCollider2D>().size=new Vector2(width,depth);
            game.StaticBox("Expert service catwalk steel",new Vector2(x,span.top-depth*.5f),new Vector2(width,depth),new Color(.25f,.36f,.38f),2);
            for(float slot=span.left+.22f;slot<span.right;slot+=.4f)
                game.StaticBox("Expert catwalk grating slot",new Vector2(roomLeft+slot,span.top-.06f),new Vector2(.13f,.06f),new Color(.05f,.1f,.13f),3);
            game.StaticBox("Expert field shortcut",new Vector2(x,span.top+.03f),new Vector2(width,.05f),new Color(1,.82f,.2f),4);
        }
        readonly List<AncientAirTarget> buoys=new List<AncientAirTarget>();
        readonly List<AncientMovingAnchor> cranes=new List<AncientMovingAnchor>();
        static bool UsesBlade(int kind,int node){return kind==1||kind==2&&node==0||kind==3&&node==0;}
        static Vector2 NodePoint(int id,int index)
        {
            var mission=AncientCampaignData.Missions[id];float left=AncientCampaignRules.Left(id);
            var segment=AncientFieldExpansion.Segment(id,index,5);float x=left+(segment.left+segment.right)*.5f;
            if(mission.Kind==2)return new Vector2(index==2?left+AncientChapterLayout.Width(id)-16:x,1.2f);
            return new Vector2(x,segment.top+1.3f);
        }
        static Vector2 PowerPoint(int id,int sector)
        {var segment=AncientFieldExpansion.Segment(id,sector,4);return new Vector2(AncientCampaignRules.Left(id)+(segment.left+segment.right)*.5f,segment.top+1.3f);}
        public bool TryMovingAnchor(Vector2 point,out Transform target)
        {
            target=null;if(!game||!game.Story)return false;
            foreach(var crane in cranes)
                if(crane&&crane.isActiveAndEnabled&&AncientCityLayout.VisualSector(crane.transform.position.x)==game.Story.CurrentFloor&&
                    ((Vector2)crane.transform.position-point).sqrMagnitude<.04f)
                {target=crane.transform;return true;}
            return false;
        }
        public bool ReadyForBoss(int floor){return legacy||AncientCampaignRules.ReadyForBoss(completed,floor);}
        public bool ReadyForLift(int destination){return legacy||destination!=1||AncientCampaignRules.FloorCount(completed,0)==4;}
        public bool ReadyForFinale {get{return legacy||AncientCampaignRules.FloorCount(completed,4)==4;}}
        public void WriteSave()
        {
            StoreProgress();
            AncientGame.SetSaved("campaignRestMission",restMission);AncientGame.SetSaved("campaignRestSlot",restSlot);
            AncientGame.SetSaved("campaignComplete",completed);AncientGame.SetSaved("campaignSeen",seen);AncientGame.SetSaved("campaignBeforeSeen",beforeSeen);AncientGame.SetSaved("campaignAfterSeen",afterSeen);AncientGame.SetSaved("campaignActive",active);
            AncientGame.SetSaved("campaignTaskMission",taskMission);AncientGame.SetSaved("campaignTasks",tasks);
            AncientGame.SetSaved("campaignCheckpoint",checkpoint);AncientGame.SetSaved("campaignDefeated",defeated);
            AncientGame.SetSaved("campaignChapterStarted",chapters);
            if(active>=0)
            {
                AncientGame.SetSaved(ProgressKeys[active*3],routeTasks[active]);
                AncientGame.SetSaved(ProgressKeys[active*3+1],routeCheckpoints[active]);
                AncientGame.SetSaved(ProgressKeys[active*3+2],routeDefeated[active]);
            }
        }
        void StoreProgress()
        {if(active>=0){routeTasks[active]=tasks;routeCheckpoints[active]=checkpoint;routeDefeated[active]=defeated;}}
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
            if(active>=4&&active<=7){defeated=AncientFieldExpansion.EnemyMask;return;}
            var mission=AncientCampaignData.Missions[active];int floor=mission.Floor;
            for(int i=0;i<AncientFieldExpansion.EnemiesPerSector;i++)
            {
                if((defeated&AncientFieldExpansion.EnemyBit(sector,i))!=0)continue;
                float x,y;AncientFieldExpansion.EncounterPosition(active,mission.Kind,sector,i,out x,out y);x+=AncientCampaignRules.Left(active);
                int kind=AncientFieldExpansion.EncounterType(floor,mission.Kind,sector,i);
                if(kind==4)y+=2.5f;
                var enemy=new GameObject("Field criminal / sector "+(sector+1)).AddComponent<AncientEnemy>();enemy.Initialize(kind);
                enemy.transform.position=new Vector2(x,y);enemy.ConfigureDistrict(floor,false);foes.Add(enemy);foeSlots.Add(sector*2+i);game.enemies.Add(enemy);
            }
        }
        Vector2 CheckpointPoint()
        {
            var point=new Vector2(AncientCampaignRules.Left(active)+AncientFieldExpansion.EntranceOf(active,checkpoint),1.3f);
            if(game.LowerRoute)point=game.LowerRoute.Checkpoint(active,checkpoint,point);
            float x,y;
            if(restMission==active&&restSlot>=0&&restSlot<24&&restSlot/8==checkpoint&&AncientFieldExpansion.TryRestPoint(active,checkpoint,restSlot%8,out x,out y))
            {x+=AncientCampaignRules.Left(active);if(x>point.x&&(!game.LowerRoute||x<=game.LowerRoute.RecoveryLimit(active,checkpoint)))return new Vector2(x,y);}
            return point;
        }
        void RememberLanding(int sector,Vector2 player)
        {
            if(active<0||active>=8||sector!=checkpoint||!game.hero.IsOnGround)return;
            float left=AncientCampaignRules.Left(active),limit=game.LowerRoute?game.LowerRoute.RecoveryLimit(active,sector):left+AncientFieldExpansion.WidthFor(active);
            for(int pad=0;pad<8;pad++)
            {
                int slot=sector*8+pad;float x,y;
                if(restMission==active&&slot<=restSlot||!AncientFieldExpansion.TryRestPoint(active,sector,pad,out x,out y))continue;
                x+=left;if(x>limit||Mathf.Abs(player.x-x)>2||Mathf.Abs(player.y-y)>.4f)continue;
                if(restMission>=0&&restMission<8&&restSlot>=0&&restSlot<24&&restBeacons[restMission,restSlot])restBeacons[restMission,restSlot].color=new Color(.3f,.7f,.8f);
                restMission=active;restSlot=slot;restBeacons[active,slot].color=new Color(.35f,1,.55f);
                Persist();game.Impact(new Vector2(x,.45f),new Color(.35f,1,.55f),false);game.Notice("안전 발판 저장",1.2f);return;
            }
        }
        void RestoreEscort()
        {
            if(AncientCampaignData.Missions[active].Kind!=2||(tasks&2)==0||(tasks&4)!=0)return;
            float x=Mathf.Max(NodePoint(active,1).x,CheckpointPoint().x-1.2f);
            escort.transform.position=new Vector2(x,0);following=true;
        }
        void Begin(int id)
        {ActivateCase(id,true);}
        void ActivateCase(int id,bool relocate)
        {
            if(!AncientCampaignRules.CanStart(completed,id,game.Story.CurrentFloor,game.Story.Stage,game.City.Cleared))return;
            if(Active){RecordDefeats();StoreProgress();}ClearFoes();if(game.LowerRoute)game.LowerRoute.ResetEncounter();
            active=id;escortCycle=0;escortFrame=-1;escort.sprite=AncientCitizenArt.ForCitizen(AncientCampaignRules.Witness(id));
            chapters|=1<<(id/4);
            if(taskMission!=id){taskMission=id;tasks=routeTasks[id];checkpoint=routeCheckpoints[id];defeated=routeDefeated[id];}
            checkpoint=AncientFieldExpansion.ResolveCheckpoint(checkpoint,tasks,defeated);following=false;
            if(relocate){game.City.OnEnterFloor();game.hero.TravelTo(CheckpointPoint());}
            ActivateSector(checkpoint);RestoreEscort();Refresh();Persist();
            if(relocate){game.SnapCameraToHero();game.BlockGameplayInput();}
        }
        void StartChapter(int id)
        {
            game.City.OnEnterFloor();ActivateCase(id,false);
            game.Feel.Signal(ChapterTitle(id/4),AncientGame.Cyan,2);
        }
        public bool TryContinueProductionRoute()
        {
            // The arrest radio has already assigned this investigation. Open the forward
            // road at the handoff instead of sending the officer back to the same briefing.
            if(!game||!game.Story||!game.hero||!game.hero.Combat||!game.HasSession||game.MainMenu||game.Paused||!Application.isFocused)return false;
            if(game.Story.Stage!=8||game.Story.CurrentFloor!=0||Active||ChapterStarted(0)||AncientCampaignRules.Next(completed,0)!=0)return false;
            if(game.Production&&(game.Production.Active||game.Production.Cinematic||game.Production.InChaseLane))return false;
            // Mark restoration handled before activation: the ordinary save-resume path
            // relocates to a safety terminal and must not run after this continuous entry.
            restored=true;restoreId=-1;ActivateCase(0,false);
            if(active!=0)return false;
            // ActivateCase persists the chapter bit first. No dialogue, reward, travel or
            // repeated radio line is needed on retries, menus or subsequent reloads.
            if(game.Feel)game.Feel.Signal(ChapterTitle(0),AncientGame.Cyan,2);
            return true;
        }
        public void ContinueAfterSniper()
        {
            if(game.Story.CurrentFloor==4&&ChapterStarted(4)&&AncientCampaignRules.Next(completed,4)==19)
                ActivateCase(19,false);
        }
        public void LeaveFloor()
        {
            if(Active){RecordDefeats();StoreProgress();Persist();}
            ClearFoes();if(game.LowerRoute)game.LowerRoute.ResetEncounter();active=-1;following=false;escort.enabled=false;Refresh();
        }
        public void EnterFloor()
        {
            restored=true;restoreId=-1;
            int floor=game.Story.CurrentFloor,next=AncientCampaignRules.Next(completed,floor);
            if(ChapterStarted(floor)&&next>=0&&AncientCampaignRules.CanStart(completed,next,floor,game.Story.Stage,game.City.Cleared))ActivateCase(next,false);
            else Refresh();
        }
        void FinishCase()
        {
            if(!Active||!AncientFieldExpansion.ExitReady(tasks,defeated)||ThreatsAlive())return;
            int id=active;var mission=AncientCampaignData.Missions[id];
            StoreProgress();
            completed=AncientCampaignRules.Claim(completed,id);game.credits+=5;
            ClearFoes();active=-1;following=false;escort.enabled=false;taskMission=-1;tasks=checkpoint=defeated=0;
            int next=AncientCampaignRules.ForwardCase(completed,id,game.Story.Stage,game.City.Cleared);
            if(next>=0)ActivateCase(next,false);else{Refresh();Persist();}
            game.CombatCheckpoint(mission.Floor,new Vector2(game.hero.transform.position.x,1.3f));
            game.Story.RadioBark("백 경위",mission.Result);
            game.Story.RadioBark(mission.Speaker,AncientCampaignData.Reactions[id]);
            game.Story.RadioBark("통신관",mission.Clue);
            game.Feel.Signal(next>=0?"기록 전송 · 동쪽으로 계속 이동":id==18?"관제실 전방 · 미등록 사격 신호":"목적지 도착 · 동쪽 통로로",new Color(.55f,1,.85f),1.6f);
        }
        static string ChapterTitle(int floor)
        {return floor==0?"회수 운송로의 끝":floor==1?"도둑이 지나간 길":floor==2?"지워진 승인":floor==3?"보류된 승진":"중앙 관제의 침묵";}
        static string ChapterBrief(int floor)
        {
            return floor==0?"회수품의 운송로를 끝까지 따라가라. 작업장과 봉쇄 구역을 지나 동쪽 인계선까지 조사해 줘. 확보한 기록은 무전으로 받겠다.":
                floor==1?"코어를 든 도둑이 동쪽 정비 골목으로 갔어요. 그 길의 흔적을 따라가면 배급 창고 뒤 본거지에 닿을 거예요.":
                floor==2?"신고 접수로부터 원본 보관소까지 기록이 같은 승인선으로 묶였습니다. 동쪽 통제구까지 따라가면 누가 기록을 막는지 확인할 수 있습니다.":
                floor==3?"공용 전력의 분기선을 따라 동쪽 외벽까지 가 보죠. 계약과 이송 명단도 그 길에 있습니다. 제 계측 기록을 보내겠습니다.":
                "정비로를 따라 중앙 관제실 너머 회장실까지 갈 수 있어요. 감시 회선을 피하면서 지워진 기록을 찾아 주세요. 전방의 사격 신호는 경비망에 등록돼 있지 않습니다.";
        }
        public bool Retry(){if(!Active)return false;Begin(active);return true;}
        void Refresh()
        {
            for(int floor=0;floor<5;floor++)if(chapterEntries[floor])
            {bool closed=!ChapterStarted(floor);chapterEntries[floor].enabled=chapterEntrySolids[floor].enabled=closed;}
            for(int id=0;id<20;id++)if(caseExits[id])
            {bool closed=!AncientCampaignRules.Complete(completed,id);caseExits[id].enabled=caseExitSolids[id].enabled=closed;}
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
            RefreshResidents();
        }
        void RefreshResidents()
        {
            if(!game||!game.Story)return;
            foreach(var resident in AncientCampaignData.Citizens)
            {
                var view=citizens[resident.Id];if(!view)continue;
                int mission=AncientCampaignRules.ResidentEscortMission(resident.Id);
                int savedTasks=mission==active?tasks:mission>=0?routeTasks[mission]:0;
                bool rescued=AncientCampaignRules.ResidentRescued(resident.Id,completed,savedTasks);
                view.transform.position=new Vector2(AncientCampaignRules.ResidentPositionX(resident.Id,resident.X,completed,savedTasks),rescued?0:resident.Y);
                view.enabled=resident.Floor==game.Story.CurrentFloor&&(resident.Id!=3||game.Story.Stage>=8)&&AncientCampaignRules.ResidentVisible(resident.Id,active,tasks);
            }
            if(rescuedReha)
            {
                // Citizen 4 is the production foreman, not Reha. Keep the rescued worker
                // at the same exit where his escort stopped, independently of that resident.
                int savedRehaTasks=active==2?tasks:routeTasks[2];
                bool rescued=AncientCampaignRules.Complete(completed,2)||(savedRehaTasks&4)!=0;
                bool walking=active==2&&escort&&escort.enabled;
                rescuedReha.enabled=game.Story.Stage>=8&&game.Story.CurrentFloor==0&&rescued&&!walking;
                if(rescuedReha.enabled&&game.hero&&Mathf.Abs(game.hero.transform.position.x-rescuedReha.transform.position.x)<7)
                    rescuedReha.flipX=game.hero.transform.position.x<rescuedReha.transform.position.x;
            }
        }
        void Mark(int index)
        {
            if((tasks&(1<<index))!=0||!AncientFieldExpansion.ObjectiveAvailable(tasks,index)||game.LowerRoute&&!game.LowerRoute.SectorComplete(active,index))return;tasks=AncientCampaignRules.Objective(tasks,index);Refresh();Persist();game.Sound(2);
            game.Feel.Signal(active==1&&index==2?"작업장 전력 복구 · 조명 재가동":"확보 / "+AncientCampaignData.Missions[active].Tasks[index],new Color(.65f,1,.85f),1.3f);
        }
        public bool TryBladeTarget(Vector2 origin,Vector2 direction)
        {
            if(!Active||active>=4&&active<=7)return false;var m=AncientCampaignData.Missions[active];
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
        public void OnLaser(Vector2 start,Vector2 end,float bodyRadius=0)
        {
            if(!Active||AncientCampaignData.Missions[active].Kind!=3||(tasks&3)!=3)return;
            Vector2 p=NodePoint(active,2),delta=end-start;
            Vector2 target=p-start;
            if(AncientBeamRules.IntersectsCircle(delta.x,delta.y,target.x,target.y,bodyRadius,1)&&!Physics2D.Linecast(start,p,1<<8))Mark(2);
        }
        public bool TryInteract()
        {
            if(!game.Story||game.hero.bike||game.ArenaActive||(game.Finale&&game.Finale.Phase!=0))return false;
            if(NearRescuedReha())
            {
                if(game.Paused||!game.GameplayInputReady)return false;
                if(game.hero.Wired||!game.hero.IsOnGround){game.Notice("착지한 뒤 재하와 대화하세요.");return true;}
                game.Story.Conversation(null,0,new[]{RehaName,"백명"},
                    new[]{"꺼내 줘서 고맙습니다. 전 안에 남아 있었어요. 피난 완료라고 쓰인 기록은 제 증언으로 바로잡고 싶습니다.","직접 나오셨다는 사실과 잠금 기록을 함께 남기겠습니다. 안전선 안에서 기다리세요."});return true;
            }
            int id=NearestCitizen();if(id<0)return Active&&InteractRoom();
            if(!game.hero.IsOnGround){game.Notice("착지한 뒤 주민과 대화하세요.");return true;}
            var npc=AncientCampaignData.Citizens[id];seen|=1<<id;Persist();
            int next=AncientCampaignRules.Next(completed,npc.Floor);
            if(next>=0&&!ChapterStarted(npc.Floor)&&id==givers[npc.Floor]&&AncientCampaignRules.CanStart(completed,next,npc.Floor,game.Story.Stage,game.City.Cleared))
            {
                var m=AncientCampaignData.Missions[next];
                game.Story.Conversation(()=>StartChapter(next),npc.Role,
                    new string[]{npc.Name,"백명","통신관"},
                    new string[]{ChapterBrief(npc.Floor),m.Reply,"확보한 기록은 이동하면서 전송해라. 나는 무전으로 확인하겠다. 동쪽 끝까지 따라가 보자."});
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
            for(int sector=0;sector<=checkpoint;sector++)if(Mathf.Abs(hero.x-left-AncientFieldExpansion.EntranceOf(active,sector))<2.5f&&Mathf.Abs(hero.y-1.3f)<1.5f)
            {Persist();game.Notice("안전 거점 / 확보한 기록과 수사 진행을 저장했습니다.",2);return true;}
            if(m.Floor==1)return false;
            if(hero.x>left+AncientChapterLayout.Width(active)-10)
            {
                if(!AncientFieldExpansion.ExitReady(tasks,defeated)||ThreatsAlive())game.Notice("남아 있는 기록과 위협을 확인하면 동쪽 통로가 열립니다.",2);
                else FinishCase();return true;
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
                bool rescuedWitness=m.Kind==2&&i==1;
                game.Story.Conversation(null,m.Kind==2?2:3,new string[]{m.Speaker,"백명"},
                    new string[]{rescuedWitness?m.Brief:AncientCampaignData.Details[active][i],rescuedWitness?"잠금 장치는 잘랐습니다. 제 뒤로 오세요. 증언은 안전한 곳에서 듣겠습니다.":"확인했습니다. 사본을 남기겠습니다."});return true;
            }
            return false;
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
                var view=citizens[npc.Id];if(!view||!view.enabled||!view.gameObject.activeInHierarchy)continue;
                if(npc.Floor!=game.Story.CurrentFloor)continue;float d=(hero-((Vector2)view.transform.position+Vector2.up)).sqrMagnitude;
                if(d<best){best=d;nearest=npc.Id;}
            }
            return nearest;
        }
        bool NearRescuedReha()
        {
            if(!rescuedReha||!rescuedReha.enabled||!rescuedReha.gameObject.activeInHierarchy||!game.hero)return false;
            Vector2 target=(Vector2)rescuedReha.transform.position+Vector2.up;
            return ((Vector2)game.hero.transform.position-target).sqrMagnitude<2.2f*2.2f&&!Physics2D.Linecast(game.hero.transform.position,target,1<<8);
        }
        void RecoverToCheckpoint()
        {
            RecordDefeats();checkpoint=AncientFieldExpansion.ResolveCheckpoint(checkpoint,tasks,defeated);
            ClearFoes();if(game.LowerRoute)game.LowerRoute.ResetEncounter();game.hero.TravelTo(CheckpointPoint());ActivateSector(checkpoint);RestoreEscort();Refresh();
            game.SnapCameraToHero();game.BlockGameplayInput();game.Notice("안전 거점 회수 / 확보한 기록과 제압한 위협은 유지됩니다.");
        }
        void Update()
        {
            if(!game.Story||!game.hero||!game.HasSession)return;
            RefreshResidents();
            if(game.Finale&&game.Finale.Phase!=0)return;
            for(int floor=1;floor<=4;floor++)
            {
                bool closed=!Active&&game.Story.CurrentFloor==floor&&!ReadyForBoss(floor);
                if(gateShown[floor]!=closed){gateShown[floor]=closed;gates[floor].enabled=gateSolids[floor].enabled=closed;}
            }
            if(!restored)
            {
                restored=true;int resume=AncientCampaignRules.ResumeCase(completed,chapters,restoreId,game.Story.CurrentFloor,game.Story.Stage,game.City.Cleared);
                if(resume>=0)Begin(resume);
            }
            if(!Active)TryContinueProductionRoute();
            if(!Active||game.Paused)return;
            RecordDefeats();
            if(active>=4&&active<=7)
            {
                for(int i=0;i<3;i++)if(game.hero.transform.position.x>=NodePoint(active,i).x-3&&(active!=5||i!=0||game.Living.ThrowTaught))Mark(i);
            }
            float left=AncientCampaignRules.Left(active);Vector2 p=game.hero.transform.position;
            if(p.y<-2.5f||p.x<AncientCityLayout.Left(game.Story.CurrentFloor)-1||p.x>left+AncientChapterLayout.Width(active)+1)
            {RecoverToCheckpoint();return;}
            if(p.x>=left+AncientChapterLayout.Width(active)-10&&AncientFieldExpansion.ExitReady(tasks,defeated)&&!ThreatsAlive())
            {FinishCase();return;}
            int sector=AncientFieldExpansion.SectorAt(active,p.x-left),allowed=AncientFieldExpansion.MaximumCheckpoint(tasks,defeated);
            if(sector>allowed){RecoverToCheckpoint();return;}
            if(sector>checkpoint)
            {
                int previous=checkpoint;checkpoint=sector;float before=game.hero.hp;
                game.hero.hp=AncientProgressRules.RecoverAtCheckpoint(before,game.hero.MaxHP,previous,checkpoint);
                float gained=game.hero.hp-before;
                Refresh();Persist();
                string recovery=gained>0?" · 체력 +"+gained.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture):"";
                game.Notice("안전 거점 저장 / "+AncientFieldExpansion.Names[active][sector]+recovery,2);
                game.Sound(2);if(gained>0)game.Impact(game.hero.transform.position,new Color(.45f,1,.75f),false);
            }
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
                witness.x=left+AncientFieldExpansion.EscortStep(active,witness.x-left,p.x-left,game.hero.HorizontalSpeed,Time.deltaTime,tasks,defeated,blocked);
                float moved=Mathf.Abs(witness.x-previousX);escortCycle+=moved*2.2f;
                int frame=moved>.0001f?(int)escortCycle%4:-1;
                if(frame!=escortFrame)
                {escortFrame=frame;int person=AncientCampaignRules.Witness(active);escort.sprite=frame<0?AncientCitizenArt.ForCitizen(person):AncientArt.CitizenWalk(person,frame);}
                escort.transform.position=witness;
                if(witness.x>=left+AncientChapterLayout.Width(active)-16){following=false;Mark(2);}
                else if(blocked&&Time.time>=guardNoticeAt){guardNoticeAt=Time.time+5;game.Notice("위협을 먼저 제압하세요. 주민은 안전이 확인되면 따라옵니다.",2);}
            }
        }
        void LateUpdate()
        {if(workshopCircuit!=null)workshopCircuit.Update(RecordedTasks(1));}
        void EnsureFieldStyles(float scale)
        {
            if(fieldTitleStyle==null)
            {
                fieldTitleStyle=new GUIStyle(label){fontStyle=FontStyle.Bold,wordWrap=false,clipping=TextClipping.Clip};
                fieldBodyStyle=new GUIStyle(label){wordWrap=false,clipping=TextClipping.Clip};
                fieldHintStyle=new GUIStyle(fieldBodyStyle);fieldWorldStyle=new GUIStyle(label);fieldEdgeStyle=new GUIStyle(label){fontStyle=FontStyle.Bold};
                var padding=new RectOffset(0,0,0,0);
                foreach(var style in new[]{fieldTitleStyle,fieldBodyStyle,fieldHintStyle,fieldWorldStyle,fieldEdgeStyle})style.padding=padding;
            }
            if(Mathf.Approximately(fieldStyleScale,scale))return;fieldStyleScale=scale;
            fieldTitleStyle.fontSize=Mathf.Max(1,Mathf.RoundToInt(14*scale));fieldBodyStyle.fontSize=Mathf.Max(1,Mathf.RoundToInt(13*scale));
            fieldHintStyle.fontSize=fieldEdgeStyle.fontSize=Mathf.Max(1,Mathf.RoundToInt(12*scale));fieldWorldStyle.fontSize=fieldBodyStyle.fontSize;
        }
        int MissingObjective()
        {for(int i=0;i<3;i++)if((tasks&(1<<i))==0)return i;return -1;}
        // Guide kinds: objective, waiting witness, current threat, next encounter, report exit, energy panel.
        void GuideTarget(int next,out Vector2 point,out int kind,out int id)
        {
            float left=AncientCampaignRules.Left(active);id=next;kind=0;
            if(game.LowerRoute&&game.LowerRoute.TryGuide(out point,out guideCaption)){kind=6;id=game.LowerRoute.CurrentStop;return;}
            if(next==2&&AncientFieldExpansion.NeedsPowerRefill(AncientCampaignData.Missions[active].Kind,tasks,game.hero.CaptureEnergy))
            {kind=5;id=CurrentSector;point=PowerPoint(active,id);return;}
            bool escortThreat=next==2&&following&&escort&&ThreatsAlive();
            if(next==2&&following&&escort&&!escortThreat&&Mathf.Abs(game.hero.transform.position.x-escort.transform.position.x)>12)
            {kind=1;point=(Vector2)escort.transform.position+Vector2.up*1.2f;return;}
            if(next>=0&&next<=CurrentSector&&!escortThreat){point=NodePoint(active,next);return;}
            float nearest=float.MaxValue;AncientEnemy threat=null;
            for(int i=0;i<foes.Count;i++)if(foes[i]&&foes[i].IsAlive)
            {
                float distance=(foes[i].AimPoint-(Vector2)game.hero.transform.position).sqrMagnitude;
                if(distance<nearest){nearest=distance;threat=foes[i];id=foeSlots[i];}
            }
            if(threat){kind=2;point=threat.AimPoint;return;}
            if(next>=0){point=NodePoint(active,next);return;}
            for(int sector=0;sector<3;sector++)if(!AncientFieldExpansion.SectorClear(defeated,sector))
            {kind=3;id=sector;point=new Vector2(left+AncientFieldExpansion.EntranceOf(active,sector),1.3f);return;}
            kind=4;id=-1;point=new Vector2(left+AncientChapterLayout.Width(active)-6,1.3f);
        }
        void RefreshFieldText(int sector,int next,int kind,int id)
        {
            if(kind!=6&&uiMission==active&&uiSector==sector&&uiNext==next&&uiGuideKind==kind&&uiGuideId==id&&uiFollowing==following)return;
            var mission=AncientCampaignData.Missions[active];
            if(uiMission!=active)for(int i=0;i<3;i++)
                fieldNodeLabels[i]=(mission.Floor==1?"경로 · ":UsesBlade(mission.Kind,i)?"베기 · ":mission.Kind==3&&i==2?"레이저 · ":mission.Kind==2&&i==2?"보호 · ":"E · ")+mission.Tasks[i];
            uiMission=active;uiSector=sector;uiNext=next;uiGuideKind=kind;uiGuideId=id;uiFollowing=following;edgeMetres=-1;
            fieldTitle=ChapterTitle(mission.Floor)+" · "+(active%4+1)+"/4 · "+(sector+1)+"/3 · "+AncientFieldExpansion.Names[active][sector]+(AncientFieldExpansion.IsJumpSector(active,sector)?" · 와이어":"");
            guideCaption=kind==6?guideCaption:kind==1?"보호 대상과 동행":kind==2?"현장 위협 제압":kind==3?"다음 구간 위협 제압":kind==4?"동쪽 통로로 계속 이동":kind==5?"베기 · 입자 재채취":
                mission.Kind==2&&next==2?"보호 대상과 출구로":fieldNodeLabels[next];
            fieldNext="다음 / "+guideCaption;
            fieldHint=kind==6?"짧은 구간마다 진행 저장 · 해결한 사건은 유지됩니다.":kind==5?"패널을 베고 우클릭으로 수집 · 길게 눌러 레이저":kind==1?"위협이 없는 통로에서는 주민이 뒤따라옵니다.":mission.Kind==2&&following?"주민을 보호하며 동쪽 출구로 이동":
                kind==2||kind==3?"위협을 제압하세요 · 안전 거점 자동 저장":"기록은 무전으로 전송 · 계속 동쪽으로 이동";
        }
        static bool InView(Vector3 point)
        {return point.z>0&&point.x>=0&&point.x<=Screen.width&&point.y>=0&&point.y<=Screen.height;}
        void FieldWorldLabel(Vector3 point,string text,float width,float height,float scale)
        {
            if(!InView(point))return;
            width=Mathf.Min(width*scale,Screen.width-24*scale);height*=scale;
            float x=Mathf.Clamp(point.x-width*.5f,12*scale,Screen.width-12*scale-width);
            float y=Mathf.Clamp(Screen.height-point.y-height,216*scale,Screen.height-110*scale-height);
            var rect=new Rect(x,y,width,height);
            if(fieldGuideVisible&&rect.Overlaps(fieldGuideRect))
            {
                float above=fieldGuideRect.y-height-6*scale,below=fieldGuideRect.yMax+6*scale;
                rect.y=above>=216*scale?above:Mathf.Min(below,Screen.height-110*scale-height);
                if(rect.Overlaps(fieldGuideRect))return;
            }
            // Low world prompts must not cover the officer while interacting or fighting.
            var heroLow=game.View.WorldToScreenPoint(game.hero.transform.position+new Vector3(-.55f,-1.15f,0));
            var heroHigh=game.View.WorldToScreenPoint(game.hero.transform.position+new Vector3(.55f,1.15f,0));
            if(heroLow.z>0&&heroHigh.z>0&&rect.Overlaps(Rect.MinMaxRect(Mathf.Min(heroLow.x,heroHigh.x),Screen.height-Mathf.Max(heroLow.y,heroHigh.y),Mathf.Max(heroLow.x,heroHigh.x),Screen.height-Mathf.Min(heroLow.y,heroHigh.y))))return;
            AncientHUD.Panel(rect,new Color(.025f,.05f,.065f,.86f));
            GUI.Label(new Rect(rect.x+4*scale,rect.y,rect.width-8*scale,rect.height),text,fieldWorldStyle);
        }
        void FieldEdgeGuide(Vector2 target,int kind,float scale)
        {
            fieldGuideVisible=false;Vector3 point=game.View.WorldToScreenPoint(target);if(InView(point))return;
            float reserve=334*scale,width=260*scale,height=44*scale,margin=12*scale,x,y;int direction;
            if(point.z<=0||point.x<0||point.x>Screen.width)
            {
                bool left=point.z<=0?target.x<game.hero.transform.position.x:point.x<0;direction=left?0:1;
                x=left?margin:Screen.width-margin-width;
                y=Mathf.Clamp(Screen.height-point.y-height*.5f,226*scale,Screen.height-170*scale-height);
            }
            else
            {
                bool above=point.y>Screen.height;direction=above?2:3;
                x=Mathf.Clamp(point.x-width*.5f,reserve,Screen.width-reserve-width);y=above?176*scale:Screen.height-150*scale;
            }
            int metres=Mathf.CeilToInt(Vector2.Distance(game.hero.transform.position,target));
            if(metres!=edgeMetres||direction!=edgeDirection)
            {edgeMetres=metres;edgeDirection=direction;edgeText=guideArrows[direction]+guideCaption+"\n"+metres+"m";}
            var rect=new Rect(x,y,width,height);fieldGuideRect=rect;fieldGuideVisible=true;AncientHUD.Panel(rect,new Color(.025f,.055f,.075f,.84f));
            GUI.color=kind==2||kind==3?new Color(1,.72f,.35f):new Color(.5f,1,1);GUI.Label(rect,edgeText,fieldEdgeStyle);GUI.color=Color.white;
        }
        void DrawFieldGui()
        {
            float scale=Mathf.Min(2,Mathf.Min(Screen.width/1280f,Screen.height/720f));EnsureFieldStyles(scale);
            int sector=CurrentSector,next=MissingObjective(),kind,id;Vector2 target;GuideTarget(next,out target,out kind,out id);RefreshFieldText(sector,next,kind,id);
            float reserve=334*scale,width=Mathf.Min(760*scale,Screen.width-reserve*2);
            var header=new Rect((Screen.width-width)*.5f,16*scale,width,76*scale);
            AncientHUD.Panel(header,new Color(.025f,.045f,.065f,.84f));
            GUI.color=new Color(.5f,1,1);GUI.Label(new Rect(header.x+10*scale,header.y+4*scale,header.width-20*scale,22*scale),fieldTitle,fieldTitleStyle);
            GUI.color=Color.white;GUI.Label(new Rect(header.x+10*scale,header.y+26*scale,header.width-20*scale,22*scale),fieldNext,fieldBodyStyle);
            GUI.color=new Color(.65f,.77f,.8f);GUI.Label(new Rect(header.x+10*scale,header.y+48*scale,header.width-20*scale,22*scale),fieldHint,fieldHintStyle);GUI.color=Color.white;
            FieldEdgeGuide(target,kind,scale);
            for(int i=0;i<=checkpoint;i++)FieldWorldLabel(game.View.WorldToScreenPoint(new Vector2(AncientCampaignRules.Left(active)+AncientFieldExpansion.EntranceOf(active,i),2.3f)),terminalLabels[i],220,40,scale);
            int gate=AncientFieldExpansion.MaximumCheckpoint(tasks,defeated);
            if(gate<2)FieldWorldLabel(game.View.WorldToScreenPoint(new Vector2(AncientCampaignRules.Left(active)+AncientFieldExpansion.SectorStart(active,gate+1)-.4f,5)),"다음 구간 / 조사·위협 해결 후 통과",300,44,scale);
            var mission=AncientCampaignData.Missions[active];
            if(mission.Kind==2&&following&&(tasks&4)==0)
                FieldWorldLabel(game.View.WorldToScreenPoint(escort.transform.position+Vector3.up*2.1f),"보호 대상 / 가까이 동행",240,36,scale);
            if(mission.Floor==0)for(int index=3;index<=6;index+=3)
            {
                var segment=AncientFieldExpansion.Segment(active,sector,index);float x=AncientCampaignRules.Left(active)+(segment.left+segment.right)*.5f,top=mission.Kind==2?0:segment.top;
                FieldWorldLabel(game.View.WorldToScreenPoint(new Vector2(x,top+.7f)),index==3?"운송 벨트 ▶":"◀ 역방향 벨트",180,28,scale);
            }
            for(int i=0;i<3;i++)if((tasks&(1<<i))==0)FieldWorldLabel(game.View.WorldToScreenPoint(NodePoint(active,i)),fieldNodeLabels[i],260,44,scale);
            if(next<0&&kind==4)FieldWorldLabel(game.View.WorldToScreenPoint(target),guideCaption,220,36,scale);
            if(mission.Kind==3&&(tasks&1)!=0&&(tasks&4)==0)
                FieldWorldLabel(game.View.WorldToScreenPoint(PowerPoint(active,sector)),"보조 전력 패널 / 베기: 입자 재채취",250,36,scale);
        }
        void OnGUI()
        {
            if(!game.Story||!game.hero||!game.View||game.Paused||game.MainMenu||Event.current.type!=EventType.Repaint)return;
            if(label==null)label=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=14,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;GUI.matrix=Matrix4x4.identity;
            if(Active){GUI.depth=-8;DrawFieldGui();}
            if(!(game.Finale&&game.Finale.Phase!=0))
            {
                int nearest=NearestCitizen();
                if(NearRescuedReha())
                {
                    var p=game.View.WorldToScreenPoint(rescuedReha.transform.position+Vector3.up*2.2f);
                    if(InView(p))GUI.Label(new Rect(p.x-170,Screen.height-p.y-25,340,55),RehaName+" / E: 대화",label);
                }
                else if(nearest>=0)
                {
                    var npc=AncientCampaignData.Citizens[nearest];var p=game.View.WorldToScreenPoint(citizens[nearest].transform.position+Vector3.up*2.2f);
                    GUI.Label(new Rect(p.x-170,Screen.height-p.y-25,340,55),npc.Name+" / E: 대화",label);
                }
                int floor=game.Story.CurrentFloor;
                if(!Active&&game.Story.Stage>=8&&!ChapterStarted(floor))
                {
                    int nextCase=AncientCampaignRules.Next(completed,floor);
                    if(nextCase>=0)
                    {
                        var giver=AncientCampaignData.Citizens[GiverFor(floor,nextCase)];
                        var position=citizens[giver.Id].transform.position;var pixel=game.View.WorldToScreenPoint(position+Vector3.up*2);
                        float x=Mathf.Clamp(pixel.x,100,Mathf.Max(100,Screen.width-100));
                        if(Mathf.Abs(game.hero.transform.position.x-position.x)>4)
                            GUI.Label(new Rect(x-100,Screen.height-195,200,30),(position.x<game.hero.transform.position.x?"◀ ":"▶ ")+giver.Name,label);
                    }
                }
                if(!Active&&game.Story.Stage>=8&&!game.Story.NearLiftTerminal)
                {
                    int next=AncientCampaignRules.Next(completed,floor);
                    string hint=next==19&&(game.City.Cleared&16)==0?"전방 저격수와 대치 / 동결된 통행 심사의 단서 확인":
                        next>=0&&!ChapterStarted(floor)?AncientCampaignData.Citizens[givers[floor]].Name+"에게 긴 수사로의 임무 확인":
                        next>=0?"동쪽 수사로를 따라 계속 이동":"수사로 끝 / 동쪽 목적지와 이동 엘리베이터 확인";
                    GUI.Label(new Rect(Screen.width/2-330,Screen.height-145,660,50),hint,label);
                }
            }
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
        public void DrawJournal(Rect area,GUIStyle text,GUIStyle small)
        {
            if(journalFloor<0)journalFloor=game.Story.CurrentFloor;
            for(int floor=0;floor<5;floor++)if(GUI.Button(new Rect(area.x+floor*165,area.y,155,30),AncientCityLayout.Name(floor),AncientHUD.Button)){journalFloor=floor;journalMission=journalCitizen=-1;}
            if(journalFloor<2&&game.LowerRoute&&GUI.Button(new Rect(area.x+240,area.y+40,220,30),routeJournal?"기존 사건 기록":"하층 현장 증언",AncientHUD.Button))routeJournal=!routeJournal;
            if(routeJournal&&journalFloor<2&&game.LowerRoute){game.LowerRoute.DrawJournal(journalFloor,new Rect(area.x,area.y+80,area.width,area.height-80),text,small);return;}
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
                    GUI.Label(new Rect(area.x,area.y+109+i*63,area.width,38),known?((afterSeen&(1<<npc.Id))!=0?npc.After[0]:(beforeSeen&(1<<npc.Id))!=0?npc.Before[0]:"수사로 안내를 들었다. 동쪽 길을 따라 기록을 확인한다."):"해당 구역에서 주민과 대화하면 기록됩니다.",small);
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
