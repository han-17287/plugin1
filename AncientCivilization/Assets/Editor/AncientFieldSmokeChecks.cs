#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Plugin.AncientCivilization.EditorChecks
{
    // Opt-in actual-Unity smoke checks. No normal profile is loaded or written.
    // Run from the menu with controls untouched; full playability/timing still needs a human playthrough.
    [InitializeOnLoad]
    public static class AncientFieldSmokeChecks
    {
        const string Menu="PLUGIN/검사/연속 챕터 진행";
        const string Root="PLUGIN.QA.FieldSmoke.",ProfileKey="PLUGIN.QA.ActiveProfile",NormalPrefix="PLUGIN.Ancient.v2.",QAPrefix="PLUGIN.Ancient.QA.";
        const double TimeoutSeconds=90;
        static readonly string[] optionKeys={"muted","graphics","frameTarget"};
        [Serializable] sealed class PrefSnapshot {public string[] keys;public bool[] present;public int[] values;}

        static AncientFieldSmokeChecks()
        {
            EditorApplication.update+=Update;
            EditorApplication.playModeStateChanged+=PlayModeChanged;
            Application.logMessageReceived+=LogReceived;
        }
        [MenuItem(Menu,true)]
        static bool CanRun(){return !EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!SessionState.GetBool(Root+"Running",false)&&string.IsNullOrEmpty(SessionState.GetString(ProfileKey,""));}
        [MenuItem(Menu)]
        public static void Run()
        {
            if(!CanRun())return;
            string profile=QAPrefix+Guid.NewGuid().ToString("N")+".";
            SnapshotNormalProfile();
            SessionState.SetBool(Root+"Running",true);SessionState.SetBool(Root+"Finishing",false);SessionState.SetBool(Root+"Passed",false);
            SessionState.SetString(Root+"Profile",profile);SessionState.SetString(ProfileKey,profile);SessionState.SetString(Root+"Log","");
            SetTime("Started",Now);SetStage(0);
            Set(profile,"questStage",8);Set(profile,"cityFloor",0);Set(profile,"storyBoss",1);Set(profile,"storyCores",7);
            Set(profile,"residencePoints",5000);Set(profile,"started",1);Set(profile,"campaignVersion",AncientCampaignRules.RouteVersion);
            PlayerPrefs.Save();Append("START: isolated continuous-chapter structural smoke. Leave controls untouched.");
            Append("Uses directed positions/objective seeds to test streaming, retry, save and resource recovery; this does not verify a human traversal playthrough.");
            Append("Normal profile: "+NormalPrefix+"; test profile: "+profile);
            EditorApplication.isPlaying=true;
        }
        static double Now {get{return EditorApplication.timeSinceStartup;}}
        static void Set(string prefix,string key,int value){PlayerPrefs.SetInt(prefix+key,value);}
        static void SetTime(string key,double value){SessionState.SetString(Root+key,value.ToString("R",CultureInfo.InvariantCulture));}
        static double TimeValue(string key)
        {double value;return double.TryParse(SessionState.GetString(Root+key,"0"),NumberStyles.Float,CultureInfo.InvariantCulture,out value)?value:0;}
        static double StageAge {get{return Now-TimeValue("StageAt");}}
        static void SetStage(int stage){SessionState.SetInt(Root+"Stage",stage);SetTime("StageAt",Now);}
        static void Append(string line)
        {SessionState.SetString(Root+"Log",SessionState.GetString(Root+"Log","")+DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture)+" "+line+"\n");}
        static void Assert(bool condition,string message)
        {if(!condition)throw new InvalidOperationException(message);Append("PASS: "+message);}
        static void SnapshotNormalProfile()
        {
            string[] keys=new string[AncientGame.ProgressSaveKeys.Length+optionKeys.Length];AncientGame.ProgressSaveKeys.CopyTo(keys,0);optionKeys.CopyTo(keys,AncientGame.ProgressSaveKeys.Length);
            var snapshot=new PrefSnapshot{keys=keys,present=new bool[keys.Length],values=new int[keys.Length]};
            for(int i=0;i<keys.Length;i++){snapshot.present[i]=PlayerPrefs.HasKey(NormalPrefix+keys[i]);snapshot.values[i]=PlayerPrefs.GetInt(NormalPrefix+keys[i]);}
            SessionState.SetString(Root+"NormalSnapshot",JsonUtility.ToJson(snapshot));
        }
        static bool VerifyNormalProfile()
        {
            var json=SessionState.GetString(Root+"NormalSnapshot","");if(string.IsNullOrEmpty(json)){Append("FAIL: normal-profile snapshot missing.");return false;}
            var snapshot=JsonUtility.FromJson<PrefSnapshot>(json);bool equal=true;
            for(int i=0;i<snapshot.keys.Length;i++)
            {
                string key=NormalPrefix+snapshot.keys[i];
                if(PlayerPrefs.HasKey(key)!=snapshot.present[i]||PlayerPrefs.GetInt(key)!=snapshot.values[i]){Append("FAIL: normal-profile value changed: "+snapshot.keys[i]);equal=false;}
            }
            if(equal)Append("PASS: normal profile keys/options unchanged after leaving Play Mode.");return equal;
        }
        static readonly BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Call(AncientCampaign campaign,string method,params object[] args)
        {
            var target=typeof(AncientCampaign).GetMethod(method,PrivateInstance|BindingFlags.Public);
            if(target==null)throw new MissingMethodException("AncientCampaign."+method);target.Invoke(campaign,args);
        }
        static Vector2 Point(string method,int id,int index)
        {
            var target=typeof(AncientCampaign).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic);
            if(target==null)throw new MissingMethodException("AncientCampaign."+method);return (Vector2)target.Invoke(null,new object[]{id,index});
        }
        static int Tasks(AncientCampaign campaign)
        {return (int)typeof(AncientCampaign).GetField("tasks",PrivateInstance).GetValue(campaign);}
        static int LiveEnemies(AncientGame game)
        {int count=0;foreach(var enemy in game.enemies)if(enemy&&enemy.IsAlive)count++;return count;}
        static void DefeatCurrent(AncientGame game)
        {foreach(var enemy in game.enemies.ToArray())if(enemy&&enemy.IsAlive)enemy.Hit(100000,true);}
        static int LiveMotes(AncientHero hero)
        {
            int count=0;foreach(var mote in Motes(hero))if(mote&&mote.gameObject.activeSelf)count++;return count;
        }
        static AncientEnergyMote[] Motes(AncientHero hero)
        {return (AncientEnergyMote[])typeof(AncientCombat).GetField("motes",PrivateInstance).GetValue(hero.Combat);}
        static SpriteRenderer Resident(AncientCampaign campaign,int id)
        {return ((SpriteRenderer[])typeof(AncientCampaign).GetField("citizens",PrivateInstance).GetValue(campaign))[id];}
        static int NearestResident(AncientCampaign campaign)
        {return (int)typeof(AncientCampaign).GetMethod("NearestCitizen",PrivateInstance).Invoke(campaign,null);}
        static void MoveToSector(AncientGame game,int sector)
        {game.hero.TravelTo(new Vector2(AncientCampaignRules.Left(game.Campaign.CurrentRoom)+AncientFieldExpansion.EntranceOf(sector),1.3f));}
        static void CheckExpertCatwalkGeometry(AncientHero hero)
        {
            Physics2D.SyncTransforms();int grates=0;Vector2 size=hero.GetComponent<CapsuleCollider2D>().size;float standing=size.y*.5f+.01f;
            foreach(int mission in new[]{3,6,9,15,18})foreach(int index in new[]{1,4,7})
            {
                var support=AncientFieldExpansion.Segment(mission,1,index);var expert=AncientFieldExpansion.ExpertSegment(mission,1,index);
                float x=AncientCampaignRules.Left(mission)+(expert.left+expert.right)*.5f;
                var hit=Physics2D.Raycast(new Vector2(x,expert.top+.1f),Vector2.down,.3f,1<<8);
                Assert(hit.collider&&hit.collider is BoxCollider2D&&hit.collider.gameObject.name=="Expert service catwalk"&&Mathf.Abs(hit.collider.bounds.size.y-AncientFieldExpansion.ExpertLedgeDepth)<.001f&&Mathf.Abs(hit.point.y-expert.top)<.002f,"The actual expert catwalk is the authored thin collider at mission "+mission+" / grate "+index);
                Assert(!Physics2D.OverlapCapsule(new Vector2(x,support.top+standing),size,CapsuleDirection2D.Vertical,0,1<<8),"The actual hero capsule fits on the broad landing beneath the expert grate");
                Assert(!Physics2D.OverlapCapsule(new Vector2(x,expert.top+standing),size,CapsuleDirection2D.Vertical,0,1<<8),"The actual hero capsule standing on the expert grate fits below the physical ceiling");grates++;
            }
            Assert(grates==15,"All authored expert grates across the five floor mechanics pass real collider-clearance probes");
        }
        static void CheckProductionContinuation(AncientGame game)
        {
            var campaign=game.Campaign;var hero=game.hero;var body=hero.GetComponent<Rigidbody2D>();
            Assert(game.Story.Stage==8&&campaign.Active&&campaign.CurrentRoom==0&&campaign.ChapterStarted(0),"The real Stage 8 load automatically opens the assigned lower investigation without returning to the chief");
            Assert((AncientGame.Saved("campaignChapterStarted")&1)!=0&&AncientGame.Saved("campaignActive")==0&&!game.Story.DialogueOpen,"The automatic lower assignment is persisted without replaying another briefing");
            // Seed only the old idle-save state. The actual arm attachment and director
            // calls below must preserve live movement; a mock body cannot establish that.
            Call(campaign,"LeaveFloor");typeof(AncientCampaign).GetField("chapters",PrivateInstance).SetValue(campaign,0);
            typeof(AncientCampaign).GetField("restored",PrivateInstance).SetValue(campaign,false);typeof(AncientCampaign).GetField("restoreId",PrivateInstance).SetValue(campaign,-1);
            hero.TravelTo(new Vector2(115,1.3f));hero.SetProductionAttackMode(false);body.linearVelocity=new Vector2(4,2);Physics2D.SyncTransforms();
            var attach=typeof(AncientHero).GetMethod("AttachWire",PrivateInstance);
            attach.Invoke(hero,new object[]{new Vector2(119,10),null,Vector2.down});
            Assert(hero.Wired,"The continuous-assignment probe starts with an actual ceiling wire attached");
            Vector2 position=body.position,motion=body.linearVelocity,anchor=(Vector2)typeof(AncientHero).GetField("anchor",PrivateInstance).GetValue(hero);
            int input=(int)typeof(AncientGame).GetField("inputReadyFrame",PrivateInstance).GetValue(game),credits=game.credits,score=game.score,points=game.Story.ResidencePoints,cores=game.Cores;
            float health=hero.hp;
            Assert(campaign.TryContinueProductionRoute(),"The real continuation action accepts the legacy Stage 8 idle assignment");
            Assert(body.position==position&&body.linearVelocity==motion&&hero.Wired&&(Vector2)typeof(AncientHero).GetField("anchor",PrivateInstance).GetValue(hero)==anchor,"Starting the lower investigation preserves actual position, velocity and the existing wire anchor");
            Assert((int)typeof(AncientGame).GetField("inputReadyFrame",PrivateInstance).GetValue(game)==input&&!game.Story.DialogueOpen,"Continuous assignment neither clears live input nor opens a second briefing");
            Assert(hero.hp==health&&game.credits==credits&&game.score==score&&game.Story.ResidencePoints==points&&game.Cores==cores,"Opening the already assigned investigation grants no duplicate recovery, score, promotion or currency reward");
            var enemy=game.enemies.Count>0?game.enemies[0]:null;int enemies=game.enemies.Count;
            Assert(!campaign.TryContinueProductionRoute(),"A repeated continuation call cannot restart an active investigation");Call(campaign,"Update");
            Assert(body.position==position&&body.linearVelocity==motion&&hero.Wired&&game.enemies.Count==enemies&&(enemies==0||game.enemies[0]==enemy),"The next real campaign update cannot relocate the wired officer or replace the newly active encounter");
            Assert((AncientGame.Saved("campaignChapterStarted")&1)!=0&&AncientGame.Saved("campaignActive")==0,"The live continuation saves its chapter and active case before play proceeds");
            hero.TravelTo(position);body.linearVelocity=Vector2.zero;
            // The remaining pre-existing probes cover independent upper-floor cases.
            typeof(AncientStory).GetProperty("Stage").GetSetMethod(true).Invoke(game.Story,new object[]{9});AncientGame.SetSaved("questStage",9);PlayerPrefs.Save();
        }
        static void CheckCheckpointCrossing(AncientGame game,int sector,float before,float expected)
        {
            game.hero.hp=before;MoveToSector(game,sector);Call(game.Campaign,"Update");
            Assert(game.Campaign.EarnedCheckpoint==sector&&Mathf.Abs(game.hero.hp-expected)<.001f,"Crossing earned entrance "+sector+" gives one capped health reward");
            Assert(AncientGame.Saved("campaignCheckpoint")==sector,"The rewarded entrance is already persisted");
        }
        static void CheckCheckpointRevisit(AncientGame game)
        {
            game.hero.hp=40;MoveToSector(game,0);Call(game.Campaign,"Update");MoveToSector(game,1);Call(game.Campaign,"Update");
            Assert(game.hero.hp==40&&game.Campaign.EarnedCheckpoint==1,"Backtracking and revisiting an earned entrance cannot farm healing");
        }
        static void CheckCacheMotes(AncientGame game,Vector2 source)
        {
            float energy=0;int count=0;
            foreach(var mote in Motes(game.hero))if(mote&&mote.gameObject.activeSelf)
            {
                count++;energy+=(float)typeof(AncientEnergyMote).GetField("energy",PrivateInstance).GetValue(mote);
                var sprite=mote.GetComponent<SpriteRenderer>();
                Assert(sprite&&sprite.sprite&&sprite.sprite.name=="Blue particle crystal"&&((Vector2)mote.transform.position-source).sqrMagnitude<.001f,"A cache releases actual pooled blue motes at its source");
            }
            Assert(count==3&&Mathf.Abs(energy-24)<.001f&&game.hero.CaptureEnergy==0,"Cache motes hold twenty-four collectible energy instead of auto-filling the collector");
        }
        static void CheckCacheReward(AncientGame game)
        {
            Assert(game.Exploration,"Route exploration exists in the real scene");game.hero.Combat.ResetCombat();game.hero.SpendCapturedEnergy();
            int mask=game.Exploration.CacheMask,credits=game.credits;Vector2 source=(Vector2)game.hero.transform.position+Vector2.up;
            game.hero.hp=40;Assert(game.Exploration.Claim(9,source),"An unclaimed optional cache can be collected");
            Assert(game.Exploration.CacheMask==(mask|(1<<9))&&AncientGame.Saved("routeCaches")==game.Exploration.CacheMask&&game.credits==credits+4&&AncientGame.Saved("credits")==game.credits&&game.hero.hp==58,"The actual cache saves its bit and TECH reward and restores eighteen health");
            CheckCacheMotes(game,source);game.hero.hp=40;
            Assert(!game.Exploration.Claim(9,source)&&game.Exploration.CacheMask==(mask|(1<<9))&&game.credits==credits+4&&game.hero.hp==40,"Claiming the same cache again awards no currency or healing");
            CheckCacheMotes(game,source);game.hero.Combat.ResetCombat();
            game.hero.hp=game.hero.MaxHP-5;Assert(game.Exploration.Claim(8,source)&&game.hero.hp==game.hero.MaxHP,"Cache recovery stops at the player's actual maximum health");
            game.hero.Combat.ResetCombat();game.hero.hp=10000;
            Assert(game.Exploration.Claim(7,source)&&game.hero.hp==10000,"Cache recovery preserves existing isolated QA overheal");game.hero.Combat.ResetCombat();
        }
        static void CheckIndependentForeman(AncientGame game)
        {
            var view=Resident(game.Campaign,4);Vector2 previous=game.hero.transform.position;
            Assert(game.Campaign.CurrentRoom==2&&view&&view.enabled,"The production foreman remains present while the different witness Reha awaits rescue");
            game.hero.TravelTo(view.transform.position+Vector3.up*1.3f);
            Assert(NearestResident(game.Campaign)==4,"The independent foreman remains an actual visible interaction target");game.hero.TravelTo(previous);
        }
        static void CheckNamedResidentRescue(AncientGame game)
        {
            var campaign=game.Campaign;Call(campaign,"LeaveFloor");
            typeof(AncientStory).GetProperty("CurrentFloor").GetSetMethod(true).Invoke(game.Story,new object[]{1});
            typeof(AncientCampaign).GetField("completed",PrivateInstance).SetValue(campaign,campaign.CompletedMask|(1<<4));
            Call(campaign,"Begin",5);var view=Resident(campaign,6);
            Assert(campaign.CurrentRoom==5&&view&&!view.enabled,"Sora's static resident is hidden during her own active rescue");
            game.hero.TravelTo(new Vector2(AncientCampaignData.Citizens[6].X,1.3f));
            Assert(NearestResident(campaign)!=6,"A hidden duplicate cannot remain an invisible interaction target");
            // Seed only objective progress; the probe checks real renderers and actor lookup, not human escort pacing.
            Call(campaign,"Mark",0);Call(campaign,"Mark",1);Call(campaign,"RestoreEscort");Call(campaign,"Refresh");
            var escort=(SpriteRenderer)typeof(AncientCampaign).GetField("escort",PrivateInstance).GetValue(campaign);Transform actor;float offset;
            Assert(escort&&escort.enabled&&!view.enabled&&campaign.TryDialogueActor("배달원 소라",out actor,out offset)&&actor==escort.transform,"The rescued speaker uses one moving escort renderer and its real dialogue anchor");
            Call(campaign,"Mark",2);float exit=AncientCampaignRules.Left(5)+AncientCampaignRules.RoomWidth-16;
            Assert(!escort.enabled&&view.enabled&&Mathf.Abs(view.transform.position.x-exit)<.001f&&Mathf.Abs(view.transform.position.y)<.001f,"Completing rescue swaps Sora to the protected ground-level exit in the same transition");
            game.hero.TravelTo(view.transform.position+Vector3.up*1.3f);
            Assert(NearestResident(campaign)==6&&campaign.TryDialogueActor("배달원 소라",out actor,out offset)&&actor==view.transform,"Interaction and speech anchors follow the rescued resident's rendered exit position");
            game.hero.TravelTo(new Vector2(AncientCampaignData.Citizens[6].X,1.3f));Assert(NearestResident(campaign)!=6,"The rescued resident no longer interacts from her old authored bridge");
            Call(campaign,"LeaveFloor");Call(campaign,"EnterFloor");
            Assert(AncientGame.Saved("routeTask5")==7&&view.enabled&&Mathf.Abs(view.transform.position.x-exit)<.001f&&!escort.enabled,"Existing saved rescue tasks preserve the single exit resident after floor re-entry");
        }
        static void FinishOptionalDialogue(AncientGame game)
        {
            var story=game.Story;var lines=(System.Collections.IList)typeof(AncientStory).GetField("lines",PrivateInstance).GetValue(story);
            for(int i=0;i<4&&story.DialogueOpen;i++)
            {
                int index=(int)typeof(AncientStory).GetField("lineIndex",PrivateInstance).GetValue(story);var line=lines[index];int length=((string)line.GetType().GetField("text").GetValue(line)).Length;
                typeof(AncientStory).GetField("pageReady",PrivateInstance).SetValue(story,true);typeof(AncientStory).GetField("pageEnd",PrivateInstance).SetValue(story,length);typeof(AncientStory).GetField("showFull",PrivateInstance).SetValue(story,true);
                typeof(AncientGame).GetField("inputReadyFrame",PrivateInstance).SetValue(game,Time.frameCount-1);typeof(AncientStory).GetMethod("Advance",PrivateInstance).Invoke(story,null);
            }
            Assert(!story.DialogueOpen,"The optional rescued-worker conversation completes through the actual dialogue callback");
        }
        static void CheckRehaRescue(AncientGame game)
        {
            var campaign=game.Campaign;var hero=game.hero;var foreman=Resident(campaign,4);Vector3 foremanPoint=foreman.transform.position;
            var escort=(SpriteRenderer)typeof(AncientCampaign).GetField("escort",PrivateInstance).GetValue(campaign);
            var rescued=(SpriteRenderer)typeof(AncientCampaign).GetField("rescuedReha",PrivateInstance).GetValue(campaign);Transform actor;float offset;
            Assert(campaign.CurrentRoom==2&&Tasks(campaign)==3&&escort.enabled&&!rescued.enabled,"Before the exit is secured, Reha has one moving escort and no duplicate static body");
            Assert(campaign.TryDialogueActor("재하",out actor,out offset)&&actor==escort.transform,"Reha's short name resolves the actual moving escort");
            Assert(campaign.TryDialogueActor("생산소 작업반장",out actor,out offset)&&actor==foreman.transform,"The independent foreman cannot borrow Reha's escort speech anchor");
            Call(campaign,"Mark",2);float exit=AncientChapterLayout.CaseRight(2)-16;
            Assert(!escort.enabled&&rescued.enabled&&Mathf.Abs(rescued.transform.position.x-exit)<.001f&&Mathf.Abs(rescued.transform.position.y)<.001f,"Finishing the escort immediately transfers Reha to the actual protected ground-level exit");
            Assert(campaign.TryDialogueActor("야간 작업자 재하",out actor,out offset)&&actor==rescued.transform&&foreman.enabled&&foreman.transform.position==foremanPoint,"The rescued worker keeps her own speech anchor while the independent foreman stays in place");
            hero.TravelTo(new Vector2(exit,1.06f));Physics2D.SyncTransforms();typeof(AncientHero).GetField("groundSampleTick",PrivateInstance).SetValue(hero,-1f);
            int credits=game.credits,points=game.Story.ResidencePoints,cores=game.Cores;typeof(AncientGame).GetField("inputReadyFrame",PrivateInstance).SetValue(game,Time.frameCount-1);
            Assert(campaign.TryInteract()&&game.Story.DialogueOpen,"The rescued Reha remains an actual optional E interaction at the exit");FinishOptionalDialogue(game);
            Assert(Tasks(campaign)==7&&game.credits==credits&&game.Story.ResidencePoints==points&&game.Cores==cores,"Talking to Reha cannot award a second rescue or advance another task");
            DefeatCurrent(game);hero.TravelTo(new Vector2(AncientChapterLayout.CaseRight(2)-6,1.3f));Call(campaign,"Update");
            Assert(campaign.CurrentRoom==3&&AncientCampaignRules.Complete(campaign.CompletedMask,2)&&rescued.enabled&&!escort.enabled,"Reha remains visible after the continuous chapter advances to the following case");
            Call(campaign,"LeaveFloor");var floorSetter=typeof(AncientStory).GetProperty("CurrentFloor").GetSetMethod(true);floorSetter.Invoke(game.Story,new object[]{1});Call(campaign,"EnterFloor");
            Assert(!rescued.enabled&&!campaign.TryDialogueActor("재하",out actor,out offset),"The protected lower worker cannot appear as an actor on another floor");
            Call(campaign,"LeaveFloor");floorSetter.Invoke(game.Story,new object[]{0});hero.TravelTo(new Vector2(exit,1.3f));Call(campaign,"EnterFloor");
            Assert(AncientGame.Saved("routeTask2")==7&&rescued.enabled&&!escort.enabled&&campaign.TryDialogueActor("재하",out actor,out offset)&&actor==rescued.transform,"Saved rescue goals restore the same exit worker and speech anchor after a lift return");
        }
        static void CheckCompletedCrane(AncientGame game)
        {
            var campaign=game.Campaign;var hero=game.hero;int oldFloor=game.Story.CurrentFloor;
            FieldInfo completed=typeof(AncientCampaign).GetField("completed",PrivateInstance),chapters=typeof(AncientCampaign).GetField("chapters",PrivateInstance);
            int oldCompleted=campaign.CompletedMask,oldChapters=(int)chapters.GetValue(campaign);Vector2 original=hero.transform.position;
            var floorSetter=typeof(AncientStory).GetProperty("CurrentFloor").GetSetMethod(true);
            AncientMovingAnchor selected=null;Vector3 previous=Vector3.zero,camera=game.View.transform.position;bool enabled=false,active=false,shown=false,nodeEnabled=false,railEnabled=false;int slot=-1;Vector2 savedAnchor=Vector2.zero;SpriteRenderer node=null;LineRenderer rail=null;
            try
            {
                Call(campaign,"LeaveFloor");completed.SetValue(campaign,oldCompleted|15);chapters.SetValue(campaign,oldChapters|1);floorSetter.Invoke(game.Story,new object[]{0});Call(campaign,"EnterFloor");
                Assert(!campaign.Active&&AncientCampaignRules.FloorCount(campaign.CompletedMask,0)==4,"The moving-hook revisit runs after the entire lower investigation is completed and no case is active");
                var cranes=(System.Collections.Generic.List<AncientMovingAnchor>)typeof(AncientCampaign).GetField("cranes",PrivateInstance).GetValue(campaign);
                foreach(var crane in cranes)if(crane&&AncientCityLayout.VisualSector(crane.transform.position.x)==0){selected=crane;break;}
                Assert(selected,"The completed lower chapter retains a real authored moving crane");previous=selected.transform.position;enabled=selected.enabled;active=selected.gameObject.activeSelf;
                selected.gameObject.SetActive(true);selected.enabled=true;
                Vector2 centre=(Vector2)typeof(AncientMovingAnchor).GetField("centre",PrivateInstance).GetValue(selected);int index=(int)typeof(AncientMovingAnchor).GetField("index",PrivateInstance).GetValue(selected);
                slot=(int)typeof(AncientMovingAnchor).GetField("slot",PrivateInstance).GetValue(selected);savedAnchor=game.anchors[slot];shown=(bool)typeof(AncientMovingAnchor).GetField("shown",PrivateInstance).GetValue(selected);
                node=(SpriteRenderer)typeof(AncientMovingAnchor).GetField("node",PrivateInstance).GetValue(selected);rail=(LineRenderer)typeof(AncientMovingAnchor).GetField("rail",PrivateInstance).GetValue(selected);nodeEnabled=node.enabled;railEnabled=rail.enabled;
                game.View.transform.position=new Vector3(centre.x+1000,centre.y,-10);selected.transform.position=centre+Vector2.right*99;
                typeof(AncientMovingAnchor).GetMethod("Update",PrivateInstance).Invoke(selected,null);Vector2 expected=centre+new Vector2(AncientRules.CraneOffset(Time.time,index),0);
                Assert(((Vector2)selected.transform.position-expected).sqrMagnitude<.00001f&&(game.anchors[slot]-expected).sqrMagnitude<.00001f&&!node.enabled&&!rail.enabled,"An off-screen current-floor crane updates its real moving hook and selectable anchor while both renderers stay culled");
                selected.transform.position=centre+Vector2.right*88;game.skills=true;typeof(AncientMovingAnchor).GetMethod("Update",PrivateInstance).Invoke(selected,null);game.skills=false;
                Assert((Vector2)selected.transform.position==centre+Vector2.right*88&&game.anchors[slot]==expected,"A paused off-screen crane cannot advance motion or its selectable anchor");
                floorSetter.Invoke(game.Story,new object[]{1});selected.transform.position=centre+Vector2.right*77;typeof(AncientMovingAnchor).GetMethod("Update",PrivateInstance).Invoke(selected,null);
                Assert((Vector2)selected.transform.position==centre+Vector2.right*77&&game.anchors[slot]==expected,"A crane on an inactive floor cannot advance motion or its selectable anchor");floorSetter.Invoke(game.Story,new object[]{0});
                typeof(AncientMovingAnchor).GetMethod("Update",PrivateInstance).Invoke(selected,null);Transform found;Vector2 point=(Vector2)selected.transform.position+Vector2.right*.08f;
                Assert(campaign.TryMovingAnchor(point,out found)&&found==selected.transform,"The inactive completed chapter still resolves its live crane as a moving support");
                hero.TravelTo((Vector2)selected.transform.position+Vector2.left*3);hero.SetProductionAttackMode(false);Physics2D.SyncTransforms();
                typeof(AncientHero).GetMethod("AttachWire",PrivateInstance).Invoke(hero,new object[]{point,null,Vector2.zero});
                var moving=typeof(AncientHero).GetField("wireMovingAnchor",PrivateInstance);var local=typeof(AncientHero).GetField("wireMovingLocal",PrivateInstance);var anchor=typeof(AncientHero).GetField("anchor",PrivateInstance);
                Assert(hero.Wired&&(Transform)moving.GetValue(hero)==selected.transform,"The actual hero wire binds to the revisited crane rather than freezing at a world-space point");
                Vector2 offset=(Vector2)local.GetValue(hero);selected.transform.position+=new Vector3(.45f,.15f,0);Physics2D.SyncTransforms();
                typeof(AncientHero).GetMethod("FixedUpdate",PrivateInstance).Invoke(hero,null);
                Assert(hero.Wired&&((Vector2)anchor.GetValue(hero)-(Vector2)selected.transform.TransformPoint(offset)).sqrMagnitude<.00001f,"A real hero physics step follows the completed chapter's moving hook and its clicked offset");
                point=(Vector2)selected.transform.position;floorSetter.Invoke(game.Story,new object[]{1});
                Assert(!campaign.TryMovingAnchor(point,out found)&&!found,"A crane from another floor cannot become the current floor's moving support");floorSetter.Invoke(game.Story,new object[]{0});
                selected.enabled=false;Assert(!campaign.TryMovingAnchor(point,out found)&&!found,"A disabled crane component cannot accept a moving attachment");selected.enabled=true;
                selected.gameObject.SetActive(false);Assert(!campaign.TryMovingAnchor(point,out found)&&!found,"An inactive crane object cannot accept a moving attachment");
            }
            finally
            {
                game.skills=false;game.View.transform.position=camera;
                if(selected){selected.transform.position=previous;selected.enabled=enabled;selected.gameObject.SetActive(active);if(slot>=0)game.anchors[slot]=savedAnchor;if(node)node.enabled=nodeEnabled;if(rail)rail.enabled=railEnabled;typeof(AncientMovingAnchor).GetField("shown",PrivateInstance).SetValue(selected,shown);}
                hero.TravelTo(original);floorSetter.Invoke(game.Story,new object[]{oldFloor});completed.SetValue(campaign,oldCompleted);chapters.SetValue(campaign,oldChapters);Call(campaign,"EnterFloor");
            }
        }
        static void Update()
        {
            if(!SessionState.GetBool(Root+"Running",false))return;
            if(SessionState.GetBool(Root+"Finishing",false))
            {if(!EditorApplication.isPlayingOrWillChangePlaymode)Cleanup();return;}
            if(!EditorApplication.isPlaying)
            {if(!EditorApplication.isPlayingOrWillChangePlaymode&&Now-TimeValue("Started")>2)Finish(false,"Play Mode was cancelled.");return;}
            if(EditorApplication.isPaused)return;
            try
            {
                if(Now-TimeValue("Started")>TimeoutSeconds)throw new TimeoutException("Smoke check exceeded 90 seconds. Stage="+SessionState.GetInt(Root+"Stage",0));
                var game=AncientGame.Instance;if(!game||!game.hero||!game.hero.Combat||!game.Campaign)return;
                if(!AncientGame.IsQAProfile)throw new InvalidOperationException("Refusing to modify a normal gameplay profile.");
                game.hero.hp=10000; // Isolated structural check only; avoids incidental enemy interruption.
                var campaign=game.Campaign;int stage=SessionState.GetInt(Root+"Stage",0);
                if(StageAge<.2)return;
                if(stage==0)
                {
                    if(!game.GameplayInputReady||game.Paused)return;
                    CheckCacheReward(game);
                    CheckProductionContinuation(game);SetStage(1);return;
                }
                if(stage==1)
                {
                    Assert(campaign.CurrentRoom==0&&campaign.CurrentSector==0,"Case 0 begins in sector 1");
                    Assert(campaign.CameraRight-campaign.CameraLeft>AncientCampaignRules.RoomWidth*4,"The camera spans the connected chapter rather than clamping to one case");
                    Assert(LiveEnemies(game)==2,"Exactly two criminals are active in the first sector");
                    CheckExpertCatwalkGeometry(game.hero);
                    Physics2D.SyncTransforms();
                    for(int sector=0;sector<3;sector++)
                    {
                        float left=AncientCampaignRules.Left(0)+AncientFieldExpansion.SectorStart(sector);
                        Assert(Physics2D.OverlapPoint(new Vector2(left+AncientFieldExpansion.SectorSpan-12,-.1f),1<<8),"Sector "+sector+" has its broad terminal collision platform");
                        Assert(Physics2D.Raycast(new Vector2(left+75,9),Vector2.up,2,1<<8),"Sector "+sector+" has a wire-capable solid ceiling");
                    }
                    Call(campaign,"Mark",2);Assert(Tasks(campaign)==0,"A future objective cannot be granted before its prerequisites");
                    MoveToSector(game,2);SetStage(13);return;
                }
                if(stage==13)
                {
                    Assert(campaign.CurrentSector==0&&campaign.EarnedCheckpoint==0&&campaign.DefeatedMask==0,"Bypassing a closed sector gate recovers without granting progress");
                    Assert(LiveEnemies(game)==2,"Illegal sector entry does not duplicate the initial encounter");
                    DefeatCurrent(game);Call(campaign,"Mark",0);CheckCheckpointCrossing(game,1,40,60);CheckCheckpointRevisit(game);SetStage(2);return;
                }
                if(stage==2)
                {
                    Assert(campaign.CurrentSector==1&&campaign.EarnedCheckpoint==1,"Cleared first objective/encounter earns the second entrance");
                    Assert(campaign.DefeatedMask==3&&LiveEnemies(game)==2,"Only second-sector criminals replace the defeated first pair");
                    Assert(AncientGame.Saved("campaignCheckpoint")==1&&AncientGame.Saved("campaignDefeated")==3,"Earned checkpoint and defeat bits are persisted");
                    game.hero.hp=40;Assert(campaign.Retry()&&game.hero.hp==40,"Retry is accepted without awarding another checkpoint heal");SetStage(3);return;
                }
                if(stage==3)
                {
                    Assert(campaign.CurrentSector==1&&campaign.EarnedCheckpoint==1&&campaign.DefeatedMask==3,"Retry preserves the earned entrance and previous defeats");
                    Assert(LiveEnemies(game)==2,"Retry does not duplicate the active encounter");
                    Vector2 position=game.hero.transform.position;game.hero.hp=40;
                    Assert(campaign.TryInteract()&&campaign.Active&&((Vector2)game.hero.transform.position-position).sqrMagnitude<.001f,"The earned safety terminal saves in place without returning to a chief");
                    Assert(game.hero.hp==40,"Using the safety terminal cannot farm its earned recovery reward");
                    Assert(AncientGame.Saved("campaignActive")==0&&AncientGame.Saved("campaignTasks")==1,"The checkpoint keeps the active case and its partial goals");
                    Call(campaign,"LeaveFloor");
                    Assert(!campaign.Active,"Leaving a floor releases its active encounter without granting completion");
                    Assert(AncientGame.Saved("routeTask0")==1&&AncientGame.Saved("routeCheckpoint0")==1&&AncientGame.Saved("routeDefeated0")==3,"The floor handoff stores this case's own partial progress");
                    SetStage(4);return;
                }
                if(stage==4)
                {
                    Vector2 position=game.hero.transform.position;game.hero.hp=40;Call(campaign,"EnterFloor");
                    Assert(((Vector2)game.hero.transform.position-position).sqrMagnitude<.001f,"Returning by the lift keeps its arrival position instead of teleporting to the field");
                    Assert(game.hero.hp==40,"Floor re-entry restores progress without awarding checkpoint healing again");
                    SetStage(5);return;
                }
                if(stage==5)
                {
                    Assert(campaign.CurrentSector==1&&campaign.DefeatedMask==3&&Tasks(campaign)==1,"Re-entering the same floor resumes its saved objective and defeat state");
                    DefeatCurrent(game);Call(campaign,"Mark",1);CheckCheckpointCrossing(game,2,game.hero.MaxHP-5,game.hero.MaxHP);SetStage(6);return;
                }
                if(stage==6)
                {
                    Assert(campaign.CurrentSector==2&&campaign.EarnedCheckpoint==2&&campaign.DefeatedMask==15,"The final entrance requires both earlier objectives/encounters");
                    Assert(LiveEnemies(game)==2,"Only the final pair is active");
                    game.hero.TravelTo(new Vector2(game.hero.transform.position.x,-3));SetStage(7);return;
                }
                if(stage==7)
                {
                    Assert(campaign.CurrentSector==2&&game.hero.transform.position.y>0,"A field fall recovers to the earned final entrance");
                    Assert(campaign.DefeatedMask==15&&LiveEnemies(game)==2,"Fall recovery does not clear or duplicate living threats");
                    DefeatCurrent(game);Call(campaign,"RecordDefeats");Call(campaign,"Mark",2);
                    game.hero.TravelTo(new Vector2(AncientCampaignRules.Left(0)+AncientCampaignRules.RoomWidth-6,1.3f));
                    var body=game.hero.GetComponent<Rigidbody2D>();Vector2 position=body.position,motion=new Vector2(9,1);body.linearVelocity=motion;
                    // Synthetic attachment tests state preservation only; casting and swinging still need a human playthrough.
                    var wired=typeof(AncientHero).GetField("wired",PrivateInstance);wired.SetValue(game.hero,true);
                    Call(campaign,"Update");
                    Assert(campaign.CurrentRoom==1&&(campaign.CompletedMask&1)!=0,"All goals and six defeats hand off directly to the next case");
                    Assert(body.position==position&&body.linearVelocity==motion&&game.hero.Wired,"The forward handoff preserves position, movement and an attached wire");
                    wired.SetValue(game.hero,false);body.linearVelocity=Vector2.zero;
                    Assert(!game.Story.DialogueOpen,"Sending the milestone report does not open a blocking return dialogue");
                    Assert(AncientGame.Saved("campaignActive")==1&&(AncientGame.Saved("campaignComplete")&1)!=0,"Completion and the next active case are saved together");
                    SetStage(8);return;
                }
                if(stage==8)
                {
                    Assert(campaign.CurrentRoom==1&&game.hero.transform.position.x<AncientCampaignRules.Left(1),"The player remains on the previous section while the next objective activates");
                    Physics2D.SyncTransforms();float right=AncientChapterLayout.CaseRight(0),next=AncientChapterLayout.CaseLeft(1);
                    for(float x=right+2;x<next-2;x+=4)
                        Assert(Physics2D.OverlapPoint(new Vector2(x,-.1f),1<<8),"The case-to-case bridge is physical ground at x="+x);
                    Assert(Physics2D.Raycast(new Vector2((right+next)*.5f,9),Vector2.up,2,1<<8),"The joining road has a continuous wire ceiling");
                    MoveToSector(game,0);SetStage(9);return;
                }
                if(stage==9)
                {
                    Assert(campaign.CurrentRoom==1&&campaign.DefeatedMask==0&&campaign.EarnedCheckpoint==0,"The next case starts with fresh encounter/checkpoint state");
                    Vector2 panel=Point("NodePoint",1,0);
                    Assert(campaign.TryBladeTarget(panel-Vector2.right,Vector2.right)&&Tasks(campaign)==1,"The power case starts by breaking its first panel");
                    DefeatCurrent(game);MoveToSector(game,1);SetStage(10);return;
                }
                if(stage==10){Call(campaign,"Mark",1);DefeatCurrent(game);MoveToSector(game,2);SetStage(11);return;}
                if(stage==11)
                {
                    if(StageAge<1.2)return; // Let the original panel's refill cooldown elapse in scaled game time.
                    Assert(campaign.CurrentSector==2&&Tasks(campaign)==3,"Power restoration reaches its final receiver with prior goals retained");
                    game.hero.Combat.ResetCombat();Vector2 panel=Point("PowerPoint",1,2);
                    Assert(campaign.TryBladeTarget(panel-Vector2.right,Vector2.right)&&LiveMotes(game.hero)==3,"The final sector can regenerate collectible energy after all prior motes are lost");
                    Assert(Tasks(campaign)==3,"Refill does not grant the laser objective");
                    Vector2 receiver=Point("NodePoint",1,2);campaign.OnLaser(receiver-Vector2.right*3,receiver+Vector2.right*3);
                    Assert(Tasks(campaign)==7,"A visible ray through the final receiver completes the power objective");
                    game.hero.TravelTo(new Vector2(AncientCampaignRules.Left(1)+AncientCampaignRules.RoomWidth-6,1.3f));Call(campaign,"Update");
                    Assert(campaign.CurrentRoom==1&&(campaign.CompletedMask&2)==0,"The onward road still refuses a live final encounter");
                    DefeatCurrent(game);Call(campaign,"RecordDefeats");Vector2 position=game.hero.transform.position;Call(campaign,"Update");
                    Assert(campaign.CurrentRoom==2&&((Vector2)game.hero.transform.position-position).sqrMagnitude<.001f,"The power case hands off to the escort without a report trip");
                    MoveToSector(game,0);SetStage(12);return;
                }
                if(stage==12)
                {
                    Assert(campaign.CurrentRoom==2,"The escort case starts after the power case");Physics2D.SyncTransforms();
                    CheckIndependentForeman(game);
                    float left=AncientCampaignRules.Left(2);
                    for(int i=1;i<AncientCampaignRules.RoomWidth/10;i++)Assert(Physics2D.OverlapPoint(new Vector2(left+i*10,-.1f),1<<8),"Escort ground remains continuous at local x="+(i*10));
                    DefeatCurrent(game);Call(campaign,"Mark",0);MoveToSector(game,1);SetStage(14);return;
                }
                if(stage==14)
                {DefeatCurrent(game);Call(campaign,"Mark",1);MoveToSector(game,2);SetStage(15);return;}
                if(stage==15)
                {Assert(campaign.Retry(),"An escort retry is accepted at the final entrance");SetStage(16);return;}
                if(stage==16)
                {
                    if(game.Paused||game.hero.bike||Time.deltaTime<=0||Input.GetKey(KeyCode.F))return;
                    var escort=(SpriteRenderer)typeof(AncientCampaign).GetField("escort",PrivateInstance).GetValue(campaign);
                    Assert(escort&&escort.enabled&&Mathf.Abs(escort.transform.position.x-game.hero.transform.position.x)<3,"A rescued witness resumes near the earned safe entrance instead of becoming stranded behind the player");
                    Assert(Tasks(campaign)==3,"Witness recovery does not grant escort completion");
                    Vector2 arrival=game.hero.transform.position;Call(campaign,"LeaveFloor");
                    var floorSetter=typeof(AncientStory).GetProperty("CurrentFloor").GetSetMethod(true);
                    floorSetter.Invoke(game.Story,new object[]{1});Call(campaign,"Begin",4);Call(campaign,"Mark",0);Call(campaign,"LeaveFloor");
                    Assert(AncientGame.Saved("routeTask2")==3&&AncientGame.Saved("routeTask4")==1,"Working on another floor preserves both cases' separate partial saves");
                    floorSetter.Invoke(game.Story,new object[]{0});game.hero.TravelTo(arrival);Call(campaign,"EnterFloor");
                    Assert(campaign.CurrentRoom==2&&Tasks(campaign)==3&&campaign.EarnedCheckpoint==2&&campaign.DefeatedMask==15,"Returning to the escort chapter restores its own goals, checkpoint and defeat bits");
                    Assert(((Vector2)game.hero.transform.position-arrival).sqrMagnitude<.001f,"The floor resume never replaces the lift's actual arrival with a field teleport");
                    CheckRehaRescue(game);CheckNamedResidentRescue(game);
                    CheckCompletedCrane(game);
                    AncientProjectileSmokeChecks.RunInQAProfile(game);
                    Append("PASS: isolated real-object projectile wall/hero ordering and clock-hand floor probes completed.");
                    Finish(true,"Continuous-chapter structural smoke completed, including one-time checkpoint/cache recovery, rescued resident interactions and projectile collision ordering. Human traversal, live escort pacing, boss transitions, exact shutter timing and wire shortcuts still need manual playtesting.");
                }
            }
            catch(Exception error){Finish(false,error.ToString());}
        }
        static void Finish(bool passed,string detail)
        {
            if(SessionState.GetBool(Root+"Finishing",false))return;
            SessionState.SetBool(Root+"Finishing",true);SessionState.SetBool(Root+"Passed",passed);Append((passed?"PASS: ":"FAIL: ")+detail);
            if(EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.isPlaying=false;else Cleanup();
        }
        static void PlayModeChanged(PlayModeStateChange change)
        {
            if(!SessionState.GetBool(Root+"Running",false))return;
            if(change==PlayModeStateChange.EnteredPlayMode)EditorApplication.ExecuteMenuItem("Window/General/Game");
            if(change==PlayModeStateChange.EnteredEditMode)
            {
                if(!SessionState.GetBool(Root+"Finishing",false)){SessionState.SetBool(Root+"Finishing",true);SessionState.SetBool(Root+"Passed",false);Append("CANCELLED: left Play Mode before completion.");}
                Cleanup();
            }
        }
        static void LogReceived(string message,string stack,LogType type)
        {
            if(!SessionState.GetBool(Root+"Running",false)||SessionState.GetBool(Root+"Finishing",false))return;
            if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,"Unity logged "+type+": "+message+"\n"+stack);
        }
        static void Cleanup()
        {
            if(!SessionState.GetBool(Root+"Running",false))return;
            bool normalIntact=VerifyNormalProfile();bool passed=SessionState.GetBool(Root+"Passed",false)&&normalIntact;
            string profile=SessionState.GetString(Root+"Profile","");
            if(profile.StartsWith(QAPrefix,StringComparison.Ordinal))
            {foreach(string key in AncientGame.ProgressSaveKeys)PlayerPrefs.DeleteKey(profile+key);foreach(string key in optionKeys)PlayerPrefs.DeleteKey(profile+key);PlayerPrefs.Save();}
            if(SessionState.GetString(ProfileKey,"")==profile)SessionState.EraseString(ProfileKey);
            Append("END: "+(passed?"PASS":"FAIL / CANCELLED")+". Only the isolated QA profile was cleaned up.");
            string report=SessionState.GetString(Root+"Log","");SessionState.SetBool(Root+"Running",false);SessionState.SetBool(Root+"Finishing",false);
            try
            {
                string folder=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Diagnostics");Directory.CreateDirectory(folder);
                string path=Path.Combine(folder,"field-smoke-last.txt");File.WriteAllText(path,report,new UTF8Encoding(false));
                if(passed)Debug.Log("Field smoke PASS. Report: "+path);else Debug.LogError("Field smoke did not pass. Report: "+path);
            }
            catch(Exception error){Debug.LogError("Could not write field smoke report: "+error.Message+"\n"+report);}
        }
    }
}
#endif
