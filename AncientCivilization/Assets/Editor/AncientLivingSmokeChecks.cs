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
    public static class AncientLivingSmokeChecks
    {
        const string Menu="PLUGIN/검사/생활부 와이어와 거대 로봇";
        const string Root="PLUGIN.QA.LivingSmoke.",ProfileKey="PLUGIN.QA.ActiveProfile",NormalPrefix="PLUGIN.Ancient.v2.",QAPrefix="PLUGIN.Ancient.QA.";
        const BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string[] optionKeys={"muted","graphics","frameTarget"};
        [Serializable] sealed class PrefSnapshot {public string[] keys;public bool[] present;public int[] values;}

        static AncientLivingSmokeChecks()
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
            PlayerPrefs.SetInt(profile+"questStage",9);PlayerPrefs.SetInt(profile+"storyBoss",1);PlayerPrefs.SetInt(profile+"started",1);
            PlayerPrefs.SetInt(profile+"campaignVersion",1);PlayerPrefs.SetInt(profile+"cityFloor",1);PlayerPrefs.Save();
            Append("START: isolated living smoke. Leave mouse, wheel and keyboard controls untouched.");
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
        static void CheckLiving(AncientGame game)
        {
            GameObject fixture=null;bool cityEnabled=game.City.enabled,campaignEnabled=game.Campaign.enabled;Vector2 position=game.hero.transform.position;
            try
            {
                game.City.enabled=false;game.Campaign.enabled=false;
                fixture=new GameObject("Living QA mech fixture");fixture.transform.position=new Vector2(AncientCityLayout.ArenaRight(1)-5,0);
                var enemy=fixture.AddComponent<AncientEnemy>();enemy.Initialize(3);enemy.ConfigureDistrict(1,true);enemy.enabled=false;game.enemies.Add(enemy);
                typeof(AncientEnemy).GetMethod("Start",PrivateInstance).Invoke(enemy,null);
                var mech=fixture.GetComponent<AncientLivingBoss>();Assert(mech&&game.Living.Boss==mech,"District boss starts the actual living controller");
                var rules=(AncientLivingRules)Field(typeof(AncientLivingBoss),"rules").GetValue(mech);
                var state=Field(typeof(AncientLivingBoss),"state");state.SetValue(mech,Enum.Parse(state.FieldType,"Active"));
                rules.RaiseBarrier();float hp=enemy.HealthRatio;
                for(int i=0;i<6;i++)mech.ReceiveHit(AncientLivingHit.Blade,28);
                Assert(mech.Barrier==10&&enemy.HealthRatio==hp,"Six actual blade hits damage only the barrier");
                mech.ReceiveHit(AncientLivingHit.Blade,28);Assert(mech.Barrier==0&&mech.Recovering&&enemy.HealthRatio==hp,"Seventh hit stuns without also damaging the body");
                state.SetValue(mech,Enum.Parse(state.FieldType,"Active"));rules.RaiseBarrier();
                for(int i=0;i<9;i++)mech.ReceiveHit(AncientLivingHit.Throw,52);
                Assert(mech.Barrier==7,"Nine thrown objects leave one throw of barrier strength");mech.ReceiveHit(AncientLivingHit.Throw,52);Assert(mech.Barrier==0,"Tenth throw breaks it");
                state.SetValue(mech,Enum.Parse(state.FieldType,"Active"));rules.RaiseBarrier();mech.ReceiveHit(AncientLivingHit.Laser,190);Assert(mech.Barrier==0&&mech.Recovering,"One charged laser breaks the actual barrier");
                Field(typeof(AncientEnemy),"hp").SetValue(enemy,960*AncientLivingRules.SecondPhase);mech.Tick();Assert(mech.Phase==2,"Runtime advances at the first health gate");
                hp=enemy.HealthRatio;enemy.Hit(9000);Assert(enemy.HealthRatio==hp,"Transition rejects incoming damage");
                state.SetValue(mech,Enum.Parse(state.FieldType,"Active"));Field(typeof(AncientEnemy),"hp").SetValue(enemy,960*AncientLivingRules.ThirdPhase);mech.Tick();Assert(mech.Phase==3,"Runtime advances at the second health gate");
                state.SetValue(mech,Enum.Parse(state.FieldType,"Active"));Field(typeof(AncientLivingBoss),"move").SetValue(mech,AncientLivingAttack.Suction);
                Assert(mech.Sucking,"Suction exposes the actual core state");mech.ReceiveHit(AncientLivingHit.Laser,10);
                Assert(!mech.Sucking&&mech.Recovering&&rules.Next()==AncientLivingAttack.CrossMissiles,"Laser overload immediately cancels suction and selects missiles");
                var objects=(System.Collections.Generic.List<AncientWireObject>)Field(typeof(AncientLivingDistrict),"objects").GetValue(game.Living);
                var crate=objects[0];game.hero.TravelTo(crate.Point+new Vector2(-3,.3f));Physics2D.SyncTransforms();
                Assert(game.Living.TryThrow(crate.Point)&&crate.Orbiting&&game.Living.InputLocked,"Real wire target selects a large prop and starts its orbit");
                Assert(!game.hero.Combat.CanCollect,"Orbit lock prevents simultaneous collection");
                game.hero.HoldForEncounter(true);Field(typeof(AncientLivingBoss),"grabbed").SetValue(mech,true);
                game.Living.ResetForTravel();Assert(!game.Living.InputLocked&&game.hero.GetComponent<Rigidbody2D>().simulated,"Travel/retry releases both captured player and orbiting object");
                var shot=game.Projectiles.Spawn(new Vector2(0,60),Vector2.right,12);shot.SetLivingProjectile(false);Assert(!shot.Breakable,"Living bullets are not collectible or blade-breakable");
                shot.Remove();shot.Play(new Vector2(0,60),Vector2.right,12,false,0,0);Assert(shot.Breakable,"Reusing a projectile resets the unbreakable flag");shot.Remove();
                Assert(AncientGame.ProgressSaveKeys.Length>0&&Array.IndexOf(AncientGame.ProgressSaveKeys,"livingGuardsCleared")>=0,"Guard checkpoint participates in profile cleanup and reset");
            }
            finally
            {
                game.Living.ResetForTravel();game.City.enabled=cityEnabled;game.Campaign.enabled=campaignEnabled;game.hero.TravelTo(position);
                if(fixture){game.enemies.Remove(fixture.GetComponent<AncientEnemy>());UnityEngine.Object.DestroyImmediate(fixture);}
            }
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
                if(Now-TimeValue("Started")>45)throw new TimeoutException("Living smoke exceeded 45 seconds.");
                var game=AncientGame.Instance;if(!game||!game.hero||!game.hero.Combat||!game.GameplayInputReady||StageAge<.4)return;
                if(!AncientGame.IsQAProfile)throw new InvalidOperationException("Refusing to modify a normal gameplay profile.");
                Assert(!game.MainMenu&&!game.Paused&&!game.ArenaActive,"The temporary profile starts outside a paused scene or boss arena");
                CheckLiving(game);
                Finish(true,"Living component fixtures completed. Physical inputs, real traversal, boss pattern timing, rendered safe zones and a complete playthrough still need manual verification.");
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
                string path=Path.Combine(folder,"living-smoke-last.txt");File.WriteAllText(path,report,new UTF8Encoding(false));
                if(passed)Debug.Log("Living smoke PASS. Report: "+path);else Debug.LogError("Living smoke did not pass. Report: "+path);
            }
            catch(Exception error){Debug.LogError("Could not write living smoke report: "+error.Message+"\n"+report);}
        }
    }
}
#endif
