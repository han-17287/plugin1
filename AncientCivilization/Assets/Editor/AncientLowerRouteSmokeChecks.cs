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
    // Explicit opt-in checks in the real Unity scene. A temporary profile keeps normal saves intact.
    // Input is not synthesized: physical mouse/keyboard timing and visual quality need a playthrough.
    [InitializeOnLoad]
    public static class AncientLowerRouteSmokeChecks
    {
        const string Menu="PLUGIN/검사/하층 확장 사건과 저장";
        const string Root="PLUGIN.QA.LowerRouteSmoke.",ProfileKey="PLUGIN.QA.ActiveProfile",NormalPrefix="PLUGIN.Ancient.v2.",QAPrefix="PLUGIN.Ancient.QA.";
        const BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string[] optionKeys={"muted","graphics","frameTarget"};
        [Serializable] sealed class PrefSnapshot {public string[] keys;public bool[] present;public int[] values;}

        static AncientLowerRouteSmokeChecks()
        {
            EditorApplication.update+=Update;
            EditorApplication.playModeStateChanged+=PlayModeChanged;
            Application.logMessageReceived+=LogReceived;
        }
        [MenuItem(Menu,true)]
        static bool CanRun()
        {return !EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!SessionState.GetBool(Root+"Running",false)&&string.IsNullOrEmpty(SessionState.GetString(ProfileKey,""));}
        [MenuItem(Menu)]
        public static void Run()
        {
            if(!CanRun())return;
            SnapshotNormalProfile();string profile=QAPrefix+Guid.NewGuid().ToString("N")+".";
            SessionState.SetBool(Root+"Running",true);SessionState.SetBool(Root+"Finishing",false);SessionState.SetBool(Root+"Passed",false);
            SessionState.SetString(Root+"Profile",profile);SessionState.SetString(ProfileKey,profile);SessionState.SetString(Root+"Log","");
            SetTime("Started",Now);SetStage(0);
            PlayerPrefs.SetInt(profile+"questStage",8);PlayerPrefs.SetInt(profile+"storyBoss",1);PlayerPrefs.SetInt(profile+"started",1);
            PlayerPrefs.SetInt(profile+"campaignVersion",AncientCampaignRules.RouteVersion);PlayerPrefs.SetInt(profile+"cityFloor",0);PlayerPrefs.Save();
            Append("START: isolated lower route smoke. Leave mouse, wheel and keyboard controls untouched.");
            EditorApplication.isPlaying=true;
        }
        static double Now {get{return EditorApplication.timeSinceStartup;}}
        static double StageAge {get{return Now-TimeValue("StageAt");}}
        static double TimeValue(string key)
        {double value;return double.TryParse(SessionState.GetString(Root+key,"0"),NumberStyles.Float,CultureInfo.InvariantCulture,out value)?value:0;}
        static void SetTime(string key,double value){SessionState.SetString(Root+key,value.ToString("R",CultureInfo.InvariantCulture));}
        static void SetStage(int stage){SessionState.SetInt(Root+"Stage",stage);SetTime("StageAt",Now);}
        static void Append(string text)
        {SessionState.SetString(Root+"Log",SessionState.GetString(Root+"Log","")+DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture)+" "+text+"\n");}
        static void Assert(bool condition,string detail)
        {if(!condition)throw new InvalidOperationException(detail);Append("PASS: "+detail);}
        static FieldInfo Field(Type type,string name)
        {var result=type.GetField(name,PrivateInstance);if(result==null)throw new MissingFieldException(type.FullName,name);return result;}
        static void Call(object target,string method,params object[] args)
        {target.GetType().GetMethod(method,PrivateInstance).Invoke(target,args);}
        static void CheckRoute(AncientGame game)
        {
            var route=game.LowerRoute;bool city=game.City.enabled,campaign=game.Campaign.enabled,enabled=route.enabled;Vector2 previous=game.hero.transform.position;
            Vector3 cameraPosition=game.View.transform.position;float zoom=game.View.orthographicSize;
            try
            {
                game.City.enabled=game.Campaign.enabled=route.enabled=false;
                var residents=(AncientRouteResidents)Field(typeof(AncientLowerRoute),"residents").GetValue(route);
                game.View.orthographicSize=5.8f;residents.Focus(0);
                game.View.transform.position=(Vector3)AncientLowerRoute.BasePoint(0)+Vector3.back*10;residents.Tick();
                Assert(residents.At(0)==residents.At(1)&&residents.InPlace(0)&&residents.At(0).enabled,"Repeated testimony shares one visible physical resident");
                Vector3 witnessPosition=residents.At(0).transform.position;residents.Focus(1);residents.Tick();
                Assert(residents.InPlace(0)&&residents.At(0).transform.position==witnessPosition,"A resident never teleports to the next conversation while the previous spot is on screen");
                game.View.transform.position=(Vector3)AncientLowerRoute.BasePoint(1)+Vector3.back*10;residents.Tick();
                Assert(residents.InPlace(1)&&residents.At(1).enabled&&!residents.InPlace(0),"Moving the camera away permits the same resident to take the next authored position");
                game.View.transform.position=(Vector3)AncientLowerRoute.BasePoint(0)+Vector3.back*10;
                residents.Revisit(0,AncientLowerRoute.BasePoint(0).x,id=>id==0||id==1);residents.Tick();
                Assert(residents.InPlace(0)&&residents.At(0).enabled,"Returning to an old completed landmark makes its original testimony reachable again");
                residents.Revisit(0,AncientLowerRoute.BasePoint(1).x,id=>id==0||id==1);residents.Tick();
                Assert(residents.InPlace(0),"Return-trip relocation also waits while the resident is still on screen");
                Call(route,"Select",2);
                Assert(!((BoxCollider2D)Field(typeof(AncientLowerRoute),"gateSolid").GetValue(route)).enabled,"A skipped optional stop never creates a blocking collider");
                foreach(int id in new[]{3,10,30,45})
                {
                    Call(route,"Select",id);Vector2 p=AncientLowerRoute.Point(id);game.hero.TravelTo(p+Vector2.left*2);Physics2D.SyncTransforms();
                    var gate=(BoxCollider2D)Field(typeof(AncientLowerRoute),"gateSolid").GetValue(route);
                    Assert(gate.enabled&&Mathf.Abs(gate.transform.position.x-p.x-18)<.01f,"Unfinished stop "+id+" owns a forward physical gate");
                    Assert(!route.TryBlade(p-Vector2.right,Vector2.right),"An unbriefed action cannot consume the objective");
                    Call(route,"StartAction",id);var action=AncientLowerRouteData.Stops[id].Action;
                    if(action==AncientRouteAction.Blade)
                        Assert(route.TryBlade(p-Vector2.right,Vector2.right),"A real nearby blade callback resolves the yellow plate");
                    else if(action==AncientRouteAction.Relay)
                    {
                        var supply=(SpriteRenderer)Field(typeof(AncientLowerRoute),"supply").GetValue(route);
                        Assert(route.TryBlade((Vector2)supply.transform.position-Vector2.right,Vector2.right),"The local supply can create particles when the player has no energy");
                        Assert(!(bool)Field(typeof(AncientLowerRoute),"resolved").GetValue(route),"Breaking the refill does not also activate its receiver");
                        route.OnLaser(p-Vector2.right*3,p+Vector2.right*3,.5f);
                    }
                    else if(action==AncientRouteAction.Throw)
                    {
                        Assert(!route.HitThrown(p+Vector2.up*3,p+new Vector2(3,3)),"A missed thrown object leaves the gate closed");
                        Assert(route.HitThrown(p-Vector2.right*3,p+Vector2.right*3),"A swept thrown object resolves the actual receiver");
                    }
                    else if(action==AncientRouteAction.Wire)
                    {
                        game.hero.TravelTo(p);Physics2D.SyncTransforms();
                        Assert(Physics2D.OverlapPoint(p-Vector2.up*1.4f,1<<8),"Upper wire task has an actual supporting platform");
                        Assert(route.TryInteract(),"The upper wire receiver accepts its nearby interaction");
                    }
                    Assert((bool)Field(typeof(AncientLowerRoute),"resolved").GetValue(route),"Correct action marks stop "+id+" resolved");
                    Assert(AncientLowerRouteRules.Done(AncientGame.Saved("lowerActionMask"+(id/9)),id),"Action result persists before its final witness confirmation");
                    route.ResetEncounter();Call(route,"Select",id);
                    Assert((bool)Field(typeof(AncientLowerRoute),"resolved").GetValue(route),"Scene reset restores the completed action without reopening its puzzle");
                    Call(route,"Finish",id);
                    Assert(AncientLowerRouteRules.Done(route.CompletedMask(id/9),id)&&!gate.enabled,"Completion persists and opens its local gate");
                    int mission=id/9,sector=id%9/3;Vector2 checkpoint=route.Checkpoint(mission,sector,Vector2.zero);
                    Assert((checkpoint-p).sqrMagnitude<.01f,"Retry checkpoint stays at the completed local stop");
                    route.ResetEncounter();Assert(AncientLowerRouteRules.Done(route.CompletedMask(id/9),id),"Encounter reset retains earned stop progress");
                }
                Assert(Array.IndexOf(AncientGame.ProgressSaveKeys,"lowerStopMask0")>=0&&Array.IndexOf(AncientGame.ProgressSaveKeys,"lowerHeardMask7")>=0&&Array.IndexOf(AncientGame.ProgressSaveKeys,"lowerActionMask7")>=0,"New save data participates in clean restart and QA profile isolation");
            }
            finally {route.ResetEncounter();route.enabled=enabled;game.City.enabled=city;game.Campaign.enabled=campaign;game.hero.TravelTo(previous);game.View.transform.position=cameraPosition;game.View.orthographicSize=zoom;}
        }
        static void Update()
        {
            if(!SessionState.GetBool(Root+"Running",false))return;
            if(SessionState.GetBool(Root+"Finishing",false)){if(!EditorApplication.isPlayingOrWillChangePlaymode)Cleanup();return;}
            if(!EditorApplication.isPlaying)
            {if(!EditorApplication.isPlayingOrWillChangePlaymode&&Now-TimeValue("Started")>2)Finish(false,"Play Mode was cancelled.");return;}
            if(EditorApplication.isPaused)return;
            try
            {
                if(Now-TimeValue("Started")>45)throw new TimeoutException("Lower route smoke exceeded 45 seconds.");
                var game=AncientGame.Instance;if(!game||!game.hero||!game.hero.Combat||!game.GameplayInputReady||StageAge<.4)return;
                if(!AncientGame.IsQAProfile)throw new InvalidOperationException("Refusing to modify a normal gameplay profile.");
                Assert(!game.MainMenu&&!game.Paused&&!game.ArenaActive,"The temporary profile starts outside a paused scene or boss arena");
                CheckRoute(game);
                Finish(true,"Lower route component fixtures completed. Human controls, dialogue flow, rendered prompts and full traversal/timing remain unverified.");
            }
            catch(Exception error){Finish(false,error.ToString());}
        }
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
            if(equal)Append("PASS: normal save/profile options unchanged.");return equal;
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
                if(!SessionState.GetBool(Root+"Finishing",false)){SessionState.SetBool(Root+"Finishing",true);SessionState.SetBool(Root+"Passed",false);Append("CANCELLED: Play Mode ended before completion.");}
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
            bool normalIntact=VerifyNormalProfile();bool passed=SessionState.GetBool(Root+"Passed",false)&&normalIntact;string profile=SessionState.GetString(Root+"Profile","");
            if(profile.StartsWith(QAPrefix,StringComparison.Ordinal))
            {foreach(string key in AncientGame.ProgressSaveKeys)PlayerPrefs.DeleteKey(profile+key);foreach(string key in optionKeys)PlayerPrefs.DeleteKey(profile+key);PlayerPrefs.Save();}
            if(SessionState.GetString(ProfileKey,"")==profile)SessionState.EraseString(ProfileKey);
            Append("END: "+(passed?"PASS":"FAIL / CANCELLED")+". Only the temporary QA profile was cleaned up.");
            string report=SessionState.GetString(Root+"Log","");SessionState.SetBool(Root+"Running",false);SessionState.SetBool(Root+"Finishing",false);
            try
            {
                string folder=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Diagnostics");Directory.CreateDirectory(folder);
                string path=Path.Combine(folder,"lower-route-smoke-last.txt");File.WriteAllText(path,report,new UTF8Encoding(false));
                if(passed)Debug.Log("Lower route smoke PASS. Report: "+path);else Debug.LogError("Lower route smoke did not pass. Report: "+path);
            }
            catch(Exception error){Debug.LogError("Could not write lower route smoke report: "+error.Message+"\n"+report);}
        }
    }
}
#endif
