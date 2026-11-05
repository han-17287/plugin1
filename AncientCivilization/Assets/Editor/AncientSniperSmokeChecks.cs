#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Plugin.AncientCivilization.EditorChecks
{
    // Opt-in actual-Unity smoke checks. No normal profile is loaded or written.
    // Run from the menu with controls untouched; full playability/timing still needs a human playthrough.
    [InitializeOnLoad]
    public static class AncientSniperSmokeChecks
    {
        const string Menu="PLUGIN/검사/저격수 4페이즈";
        const string Root="PLUGIN.QA.SniperSmoke.",ProfileKey="PLUGIN.QA.ActiveProfile",NormalPrefix="PLUGIN.Ancient.v2.",QAPrefix="PLUGIN.Ancient.QA.";
        const double TimeoutSeconds=60;
        static readonly string[] optionKeys={"muted","graphics","frameTarget"};
        static AncientSniperEncounter retiredEncounter;
        [Serializable] sealed class PrefSnapshot {public string[] keys;public bool[] present;public int[] values;}

        static AncientSniperSmokeChecks()
        {
            EditorApplication.update+=Update;
            EditorApplication.playModeStateChanged+=PlayModeChanged;
            Application.logMessageReceived+=LogReceived;
        }
        [MenuItem(Menu,true)]
        static bool CanRun(){return !EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!SessionState.GetBool(Root+"Running",false);}
        [MenuItem(Menu)]
        public static void Run()
        {
            if(!CanRun())return;
            string profile=QAPrefix+Guid.NewGuid().ToString("N")+".";
            SnapshotNormalProfile();
            SessionState.SetBool(Root+"Running",true);SessionState.SetBool(Root+"Finishing",false);SessionState.SetBool(Root+"Passed",false);
            SessionState.SetString(Root+"Profile",profile);SessionState.SetString(ProfileKey,profile);SessionState.SetString(Root+"Log","");
            SetTime("Started",Now);SetStage(0);
            Set(profile,"questStage",9);Set(profile,"cityFloor",4);Set(profile,"storyBoss",1);Set(profile,"storyCores",7);
            Set(profile,"residencePoints",5000);Set(profile,"started",1);Set(profile,"cityCleared",14);
            Set(profile,"campaignVersion",1);Set(profile,"campaignComplete",(1<<19)-1);
            Set(profile,"retryActive",1);Set(profile,"retryFloor",4);Set(profile,"retryX",72800);Set(profile,"retryY",130);
            PlayerPrefs.Save();Append("START: isolated sniper smoke. Leave Space/mouse/game controls untouched during automatic checks.");
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
        static int CloneCount(AncientGame game)
        {int count=0;foreach(var e in game.enemies)if(e&&e.IsAlive&&e.IsSniperClone)count++;return count;}
        static void AssertTurretVisibleAt(int index,float expectedX)
        {
            var root=GameObject.Find("Emerging compressed-matter turret "+index);
            Assert(root&&root.activeInHierarchy,"Turret "+index+" is active in the arena");
            Assert(Mathf.Abs(root.transform.position.x-expectedX)<.01f,"Turret "+index+" root matches its logical interaction position");
            var body=root.transform.Find("Deployable armored turret");
            var core=root.transform.Find("Recoverable turret power core");
            var barrel=root.transform.Find("Compressed matter launcher");
            Assert(body&&core&&barrel,"Turret "+index+" has a body, core and barrel");
            Assert(body.localPosition.sqrMagnitude<.0001f&&Mathf.Abs(core.localPosition.x)<.01f&&Mathf.Abs(barrel.localPosition.x)<.01f,
                "Turret "+index+" art uses offsets from the root, without doubling its world position");
            var renderer=body.GetComponent<SpriteRenderer>();
            Assert(renderer&&renderer.enabled&&renderer.bounds.min.x<=expectedX&&renderer.bounds.max.x>=expectedX,
                "Turret "+index+" visible body bounds cover its logical position");
            Assert(Mathf.Abs(barrel.position.x-expectedX)<.01f&&Mathf.Abs(barrel.position.y-(root.transform.position.y+1.4f))<.01f,
                "Turret "+index+" barrel appears at the projectile origin");
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
                if(Now-TimeValue("Started")>TimeoutSeconds)throw new TimeoutException("Smoke check exceeded 60 seconds. Stage="+SessionState.GetInt(Root+"Stage",0));
                var game=AncientGame.Instance;if(!game||!game.hero)return;
                if(!AncientGame.IsQAProfile)throw new InvalidOperationException("Refusing to modify a normal gameplay profile.");
                game.hero.hp=10000; // Isolated test only: prevent incidental AI hits from interrupting structural checks.
                var encounter=game.Sniper;int stage=SessionState.GetInt(Root+"Stage",0);
                float firstTurret=AncientCityLayout.ArenaLeft(4)+5,secondTurret=AncientCityLayout.ArenaRight(4)-5;
                if(stage==0)
                {
                    if(!encounter||!encounter.Owner||StageAge<.4)return;
                    Assert(AncientGame.IsQAProfile,"Runtime uses the isolated QA profile");
                    Assert(!game.MainMenu&&encounter.Phase==1,"Phase 1 starts in the actual scene without menu interaction");
                    Assert(CloneCount(game)==0,"Phase 1 has no energy clones");
                    encounter.Owner.Hit(100000,true);SetStage(1);return;
                }
                if(stage==1)
                {
                    if(!encounter||encounter.Phase!=2||StageAge<.2)return;
                    Assert(CloneCount(game)==1,"Phase 2 creates exactly one live clone");encounter.Owner.Hit(100000,true);SetStage(2);return;
                }
                if(stage==2)
                {
                    if(!encounter||encounter.Phase!=3||StageAge<1.3)return;
                    Assert(CloneCount(game)==3,"Phase 3 creates exactly three live clones");
                    AssertTurretVisibleAt(0,firstTurret);AssertTurretVisibleAt(1,secondTurret);
                    foreach(var e in game.enemies)if(e&&e.IsSniperClone)
                    {
                        var art=e.GetComponentInChildren<SpriteRenderer>();
                        Assert(art&&art.sharedMaterial&&art.sharedMaterial.shader.name=="PLUGIN/AncientEnergyClone","Clone uses the full-body energy shader");
                    }
                    foreach(var e in game.enemies.ToArray())if(e&&e.IsSniperClone&&(e.name.EndsWith(" 2",StringComparison.Ordinal)||e.name.EndsWith(" 3",StringComparison.Ordinal)))e.Hit(1000,true);
                    Assert(CloneCount(game)==1,"Defeating turret operators leaves the independent clone alive");
                    game.hero.TravelTo(new Vector2(firstTurret,1.3f));SetStage(3);return;
                }
                if(stage==3)
                {
                    if(!encounter||!game.GameplayInputReady||StageAge<.2)return;
                    Assert(encounter.TryInteract(),"An unoccupied turret can be mounted");
                    Assert(encounter.MountedIndex==0&&encounter.InputLocked,"Mount locks ordinary player movement");SetStage(4);return;
                }
                if(stage==4)
                {
                    if(!encounter||!game.GameplayInputReady||StageAge<.2)return;
                    Assert(encounter.TryInteract(),"The mounted turret can be dismounted");
                    Assert(encounter.MountedIndex<0&&!encounter.InputLocked,"Dismount restores ordinary controls");
                    encounter.Owner.Hit(100000,true);SetStage(5);return;
                }
                if(stage==5)
                {
                    if(!encounter||encounter.Phase!=4||StageAge<.2)return;
                    Assert(CloneCount(game)==0,"Final phase removes all clones");
                    AssertTurretVisibleAt(0,firstTurret);AssertTurretVisibleAt(1,secondTurret);
                    float health=encounter.Owner.HealthRatio;encounter.Owner.Hit(100000,true);
                    Assert(Mathf.Abs(encounter.Owner.HealthRatio-health)<.00001f,"Charging sniper is immune to ordinary damage");
                    for(int i=0;i<2;i++){encounter.TryBlade(new Vector2(firstTurret,1),Vector2.right);encounter.TryBlade(new Vector2(secondTurret,1),Vector2.right);}
                    Assert(encounter.TurretsBroken==2,"Both final-phase turrets can be broken");
                    game.hero.AddCapturedEnergy(100);
                    Assert(encounter.Clashing&&encounter.InputLocked&&game.hero.CaptureEnergy<.01f,"Full collected energy starts automatic clash and consumes the charge");
                    SetStage(6);return;
                }
                if(stage==6)
                {
                    if(!encounter)throw new InvalidOperationException("Encounter ended before the no-input clash failure check. Keep controls untouched.");
                    if(encounter.Clashing||StageAge<8.2)return;
                    Assert(encounter.Phase==4&&encounter.TurretsBroken==0&&!encounter.InputLocked,"No-input clash failure restores both turrets and collection controls");
                    retiredEncounter=encounter;game.hero.RetryFromCheckpoint();SetStage(7);return;
                }
                if(stage==7)
                {
                    if(!encounter||encounter==retiredEncounter||StageAge<.6)return;
                    Assert(encounter.Phase==1&&CloneCount(game)==0&&!encounter.InputLocked,"Combat checkpoint retry creates a fresh phase-1 encounter");
                    Assert(game.hero.GetComponent<Rigidbody2D>().simulated,"Retry restores player physics simulation");
                    Assert(game.hero.transform.position.x<firstTurret-2&&game.hero.transform.position.x>=AncientCityLayout.ArenaLeft(4),"Retry remains at the checkpoint, not the old turret seat");
                    Finish(true,"Structural sniper smoke checks completed. Shot timing, rendering, terrain dodge and successful physical-input clash still require manual playtesting.");
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
                string path=Path.Combine(folder,"sniper-smoke-last.txt");File.WriteAllText(path,report,new UTF8Encoding(false));
                if(passed)Debug.Log("Sniper smoke PASS. Report: "+path);else Debug.LogError("Sniper smoke did not pass. Report: "+path);
            }
            catch(Exception error){Debug.LogError("Could not write sniper smoke report: "+error.Message+"\n"+report);}
        }
    }
}
#endif
