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
        const string Menu="PLUGIN/검사/확장 현장 3구간";
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
            Set(profile,"questStage",9);Set(profile,"cityFloor",0);Set(profile,"storyBoss",1);Set(profile,"storyCores",7);
            Set(profile,"residencePoints",5000);Set(profile,"started",1);Set(profile,"campaignVersion",1);
            PlayerPrefs.Save();Append("START: isolated expanded-field structural smoke. Leave controls untouched.");
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
            var target=typeof(AncientCampaign).GetMethod(method,PrivateInstance);
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
            var motes=(AncientEnergyMote[])typeof(AncientCombat).GetField("motes",PrivateInstance).GetValue(hero.Combat);int count=0;
            foreach(var mote in motes)if(mote&&mote.gameObject.activeSelf)count++;return count;
        }
        static void CloseDialogue(AncientStory story)
        {
            var advance=typeof(AncientStory).GetMethod("Advance",PrivateInstance);
            var guard=typeof(AncientGame).GetField("inputReadyFrame",PrivateInstance);
            // This fixture deliberately skips dialogue to test the report callback, not physical input.
            // Production Advance now rejects the same-frame input that opened a conversation.
            for(int i=0;i<32&&story.DialogueOpen;i++)
            {guard.SetValue(AncientGame.Instance,Time.frameCount-1);advance.Invoke(story,null);}
            Assert(!story.DialogueOpen,"QA dialogue callback can finish without ordinary player input");
        }
        static void MoveToSector(AncientGame game,int sector)
        {game.hero.TravelTo(new Vector2(AncientCampaignRules.Left(game.Campaign.CurrentRoom)+AncientFieldExpansion.EntranceOf(sector),1.3f));}
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
                    Call(campaign,"Begin",0);SetStage(1);return;
                }
                if(stage==1)
                {
                    Assert(campaign.CurrentRoom==0&&campaign.CurrentSector==0,"Case 0 begins in sector 1");
                    Assert(campaign.CameraRight-campaign.CameraLeft>400,"Field camera spans the complete expanded room");
                    Assert(LiveEnemies(game)==2,"Exactly two criminals are active in the first sector");
                    Physics2D.SyncTransforms();
                    for(int sector=0;sector<3;sector++)
                    {
                        float left=AncientCampaignRules.Left(0)+AncientFieldExpansion.SectorStart(sector);
                        Assert(Physics2D.OverlapPoint(new Vector2(left+132,-.1f),1<<8),"Sector "+sector+" has its broad terminal collision platform");
                        Assert(Physics2D.Raycast(new Vector2(left+75,9),Vector2.up,2,1<<8),"Sector "+sector+" has a wire-capable solid ceiling");
                    }
                    Call(campaign,"Mark",2);Assert(Tasks(campaign)==0,"A future objective cannot be granted before its prerequisites");
                    MoveToSector(game,2);SetStage(13);return;
                }
                if(stage==13)
                {
                    Assert(campaign.CurrentSector==0&&campaign.EarnedCheckpoint==0&&campaign.DefeatedMask==0,"Bypassing a closed sector gate recovers without granting progress");
                    Assert(LiveEnemies(game)==2,"Illegal sector entry does not duplicate the initial encounter");
                    DefeatCurrent(game);Call(campaign,"Mark",0);MoveToSector(game,1);SetStage(2);return;
                }
                if(stage==2)
                {
                    Assert(campaign.CurrentSector==1&&campaign.EarnedCheckpoint==1,"Cleared first objective/encounter earns the second entrance");
                    Assert(campaign.DefeatedMask==3&&LiveEnemies(game)==2,"Only second-sector criminals replace the defeated first pair");
                    Assert(AncientGame.Saved("campaignCheckpoint")==1&&AncientGame.Saved("campaignDefeated")==3,"Earned checkpoint and defeat bits are persisted");
                    Assert(campaign.Retry(),"Retry is accepted in the active field");SetStage(3);return;
                }
                if(stage==3)
                {
                    Assert(campaign.CurrentSector==1&&campaign.EarnedCheckpoint==1&&campaign.DefeatedMask==3,"Retry preserves the earned entrance and previous defeats");
                    Assert(LiveEnemies(game)==2,"Retry does not duplicate the active encounter");
                    Assert(campaign.TryInteract()&&!campaign.Active,"The earned safety terminal returns to the city");
                    Assert(AncientGame.Saved("campaignActive")==-1&&AncientGame.Saved("campaignTasks")==1,"City return stores goals without leaving an active field");
                    SetStage(4);return;
                }
                if(stage==4){Call(campaign,"Begin",0);SetStage(5);return;}
                if(stage==5)
                {
                    Assert(campaign.CurrentSector==1&&campaign.DefeatedMask==3,"Re-entering the same case resumes its saved entrance");
                    DefeatCurrent(game);Call(campaign,"Mark",1);MoveToSector(game,2);SetStage(6);return;
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
                    DefeatCurrent(game);Call(campaign,"Mark",2);
                    game.hero.TravelTo(new Vector2(AncientCampaignRules.Left(0)+426,1.3f));
                    Assert(campaign.TryInteract()&&(campaign.CompletedMask&1)!=0,"All three objectives and all six defeats allow the exit report");
                    CloseDialogue(game.Story);SetStage(8);return;
                }
                if(stage==8){Assert(!campaign.Active,"Reporting leaves the field");Call(campaign,"Begin",1);SetStage(9);return;}
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
                    game.hero.TravelTo(new Vector2(AncientCampaignRules.Left(1)+426,1.3f));campaign.TryInteract();
                    Assert(campaign.Active&&(campaign.CompletedMask&2)==0,"The exit still refuses a live final encounter");
                    DefeatCurrent(game);campaign.TryInteract();CloseDialogue(game.Story);Call(campaign,"Begin",2);SetStage(12);return;
                }
                if(stage==12)
                {
                    Assert(campaign.CurrentRoom==2,"The escort case starts after the power case");Physics2D.SyncTransforms();
                    float left=AncientCampaignRules.Left(2);
                    for(int i=1;i<43;i++)Assert(Physics2D.OverlapPoint(new Vector2(left+i*10,-.1f),1<<8),"Escort ground remains continuous at local x="+(i*10));
                    DefeatCurrent(game);Call(campaign,"Mark",0);MoveToSector(game,1);SetStage(14);return;
                }
                if(stage==14)
                {DefeatCurrent(game);Call(campaign,"Mark",1);MoveToSector(game,2);SetStage(15);return;}
                if(stage==15)
                {Assert(campaign.Retry(),"An escort retry is accepted at the final entrance");SetStage(16);return;}
                if(stage==16)
                {
                    var escort=(SpriteRenderer)typeof(AncientCampaign).GetField("escort",PrivateInstance).GetValue(campaign);
                    Assert(escort&&escort.enabled&&Mathf.Abs(escort.transform.position.x-game.hero.transform.position.x)<3,"A rescued witness resumes near the earned safe entrance instead of becoming stranded behind the player");
                    Assert(Tasks(campaign)==3,"Witness recovery does not grant escort completion");
                    Finish(true,"Expanded-field structural smoke completed. Human traversal, live escort pacing, exact shutter timing and wire shortcuts still need manual playtesting.");
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
            catch(Exception error){Debug.LogError("Could not write sniper smoke report: "+error.Message+"\n"+report);}
        }
    }
}
#endif
