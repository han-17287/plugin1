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
    public static class AncientProductionSmokeChecks
    {
        const string Menu="PLUGIN/검사/생산구 재고와 추격 보스";
        const string Root="PLUGIN.QA.ProductionSmoke.",ProfileKey="PLUGIN.QA.ActiveProfile",NormalPrefix="PLUGIN.Ancient.v2.",QAPrefix="PLUGIN.Ancient.QA.";
        const BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string[] optionKeys={"muted","graphics","frameTarget"};
        [Serializable] sealed class PrefSnapshot {public string[] keys;public bool[] present;public int[] values;}

        static AncientProductionSmokeChecks()
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
            PlayerPrefs.SetInt(profile+"questStage",3);PlayerPrefs.SetInt(profile+"questFlags",224);PlayerPrefs.SetInt(profile+"storyBoss",0);PlayerPrefs.SetInt(profile+"started",1);
            PlayerPrefs.SetInt(profile+"campaignVersion",AncientCampaignRules.RouteVersion);PlayerPrefs.SetInt(profile+"cityFloor",0);
            PlayerPrefs.SetInt(profile+"productionVersion",1);PlayerPrefs.SetInt(profile+"productionRound",0);PlayerPrefs.Save();
            Append("START: isolated production pursuit smoke. Leave physical controls untouched. Directed state and positions test wiring, not a human chase playthrough.");
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
        static void Call(object target,string name,params object[] arguments)
        {
            var method=target.GetType().GetMethod(name,PrivateInstance);
            if(method==null)throw new MissingMethodException(target.GetType().Name,name);method.Invoke(target,arguments);
        }
        static bool Near(float left,float right){return Mathf.Abs(left-right)<.001f;}
        static int LiveHazards(AncientProductionPursuit director)
        {
            int count=0;var pool=(Array)Field(typeof(AncientProductionPursuit),"hazards").GetValue(director);
            foreach(var hazard in pool)if(hazard!=null&&(bool)hazard.GetType().GetField("live").GetValue(hazard))count++;
            return count;
        }
        static void Step(AncientProductionPursuit director,float seconds=.25f){Call(director,"Step",seconds);}
        static void Advance(AncientProductionPursuit director,AncientProductionPhase phase)
        {
            for(int i=0;i<160&&director.Sequence.Phase!=phase;i++)Step(director);
            Assert(director.Sequence.Phase==phase,"The actual director reaches "+phase+" without skipping a player-controlled phase");
        }
        static void MoveHero(AncientHero hero,Vector2 point)
        {hero.CinematicMove(point);Physics2D.SyncTransforms();Field(typeof(AncientHero),"groundSampleTick").SetValue(hero,-1f);}
        static void CheckAutomaticBike(AncientGame game)
        {
            var hero=game.hero;var body=hero.GetComponent<Rigidbody2D>();var director=game.Production;bool enabled=hero.enabled;
            try
            {
                hero.enabled=false;MoveHero(hero,new Vector2(AncientProductionLayout.ChaseStart+6,.84f));
                Field(typeof(AncientHero),"axis").SetValue(hero,-1f);body.linearVelocity=Vector2.zero;Call(hero,"FixedUpdate");
                Assert(hero.bike&&body.simulated&&hero.GetComponent<BoxCollider2D>().enabled&&!hero.GetComponent<CapsuleCollider2D>().enabled,"The initial automatic mount uses the real motorcycle body and collider");
                Assert(Near(body.linearVelocity.x,director.DriveSpeed)&&body.linearVelocity.x>0,"Bike physics advances without a held forward key and overrides a stale reverse axis");
                Field(typeof(AncientHero),"jumpBufferedUntil").SetValue(hero,Time.time+AncientMovementRules.BufferSeconds);
                Field(typeof(AncientHero),"jumpHeld").SetValue(hero,true);Field(typeof(AncientHero),"cutJump").SetValue(hero,false);Call(hero,"FixedUpdate");
                Assert(Near(body.linearVelocity.x,director.DriveSpeed)&&Near(body.linearVelocity.y,14),"The actual buffered Space jump clears the road without cancelling automatic forward movement");
                MoveHero(hero,new Vector2(AncientProductionLayout.ChaseStart+6,.84f));body.linearVelocity=new Vector2(director.DriveSpeed,0);
            }
            finally {hero.enabled=enabled;}
        }
        static void BoardRoof(AncientGame game)
        {
            var director=game.Production;var hero=game.hero;var body=hero.GetComponent<Rigidbody2D>();
            // Fast-forwarding the director does not advance Unity's input clock. Discard only
            // the previous round's elapsed cooldown before making the next directed request.
            Field(typeof(AncientHero),"wireReady").SetValue(hero,Time.time-1);
            Field(typeof(AncientGame),"inputReadyFrame").SetValue(game,Time.frameCount-1);
            Vector2 hook=director.BoardingHook;MoveHero(hero,new Vector2(hook.x-6.5f,.84f));body.linearVelocity=new Vector2(director.DriveSpeed,0);
            Assert(hero.TryProductionBoard(hook)&&director.Sequence.Phase==AncientProductionPhase.Boarding,"The real Hero boarding action accepts the moving roof latch only during the ceasefire window");
            Assert(hero.Wired&&!hero.bike,"The actual action dismounts the motorcycle and starts its physical straight wire");
            bool enabled=hero.enabled,directorEnabled=director.enabled;var simulation=Physics2D.simulationMode;
            try
            {
                hero.enabled=director.enabled=false;Physics2D.simulationMode=SimulationMode2D.Script;
                float dt=Time.fixedDeltaTime;
                for(int i=0;i<260&&director.Sequence.Phase==AncientProductionPhase.Boarding;i++)
                {
                    Call(director,"FixedUpdate");Call(hero,"FixedUpdate");Physics2D.SyncTransforms();Physics2D.Simulate(dt);Step(director,dt);
                }
                Assert(director.DeckCombat&&!hero.bike&&hero.AttackMode,"Actual capsule casts, moving truck physics and wire release deliver the hero to deck combat without a roof teleport");
                Assert(Mathf.Abs(hero.transform.position.x-director.Commander.transform.position.x)<5.5f&&hero.transform.position.y>3.4f,"The moving roof remains under the hero after physical boarding");
            }
            finally {Physics2D.simulationMode=simulation;hero.enabled=enabled;director.enabled=directorEnabled;}
        }
        static void CheckRun(AncientGame game)
        {
            var director=game.Production;var sequence=director.Sequence;var commander=director.Commander;
            Assert(sequence.Phase==AncientProductionPhase.Chase&&sequence.Round==0&&sequence.MountCount==1&&game.Story.Stage==5,"The actual interrupted stock report reaches the first playable motorcycle chase");
            Assert(game.hero.transform.position.x>=AncientProductionLayout.ChaseStart&&game.hero.transform.position.x<AncientProductionLayout.ChaseEnd,"The playable chase starts on its dedicated tunnel instead of the stock-room coordinates");
            Assert(!game.ArenaActive,"The moving commander cannot activate the old stationary guardian clamp");
            CheckAutomaticBike(game);
            float initial=commander.HealthRatio;commander.Hit(100000,true);
            Assert(Near(commander.HealthRatio,initial),"The captain cannot lose deck-phase health from an early road attack");
            float elapsed=sequence.Elapsed;game.skills=true;Call(director,"Update");game.skills=false;
            Assert(sequence.Elapsed==elapsed,"A real paused director Update cannot advance its chase clock");
            Advance(director,AncientProductionPhase.Approach);
            Assert(!sequence.AttackersEnabled&&director.AutoDrive,"Timed approach stops attacks while automatic riding continues");
            Assert(LiveHazards(director)==0&&game.shots.Count==0,"The ceasefire removes actual road hazards and flying attack objects");
            Step(director,1);Step(director,1);
            Assert(sequence.Phase==AncientProductionPhase.Approach,"The wire-boarding opportunity stays open instead of timing out");
            BoardRoof(game);commander.Hit(100000,true);
            Assert(sequence.Phase==AncientProductionPhase.Knockback&&Near(commander.HealthRatio,AncientProductionRules.FirstHealthGate)&&commander.IsAlive,"An actual captain hit stops at the first health gate and begins the knockback transition");
            Advance(director,AncientProductionPhase.Chase);
            Assert(sequence.Round==1&&sequence.MountCount==2&&game.hero.bike,"The first knockback automatically returns to the second bike mount");
            Assert(AncientGame.Saved("productionRound")==1,"The second chase checkpoint is persisted at its actual phase entry");
            Assert(director.Retry()&&sequence.Round==1&&sequence.MountCount==2&&Near(commander.HealthRatio,AncientProductionRules.FirstHealthGate),"Retry resumes the second round and its captain-health checkpoint instead of restarting inventory or round one");
            Advance(director,AncientProductionPhase.Approach);BoardRoof(game);commander.Hit(100000,true);
            Assert(sequence.Phase==AncientProductionPhase.Knockback&&Near(commander.HealthRatio,AncientProductionRules.SecondHealthGate)&&commander.IsAlive,"The second real deck hit clamps at its health gate without killing the captain");
            Advance(director,AncientProductionPhase.Finisher);
            Assert(sequence.MountCount==3&&director.Cinematic,"The third bike mount starts the directed finisher instead of a third combat loop");
            for(int i=0;i<20&&sequence.Elapsed<3;i++)Step(director);
            Assert(director.CargoSecured&&!director.TruckDestroyed&&game.Cores==0,"The blade separates and secures the visible cargo before the truck can explode or award recovery");
            for(int i=0;i<80&&sequence.Elapsed<7.35f;i++)Step(director,.1f);
            var beam=(LineRenderer)Field(typeof(AncientProductionPursuit),"beam").GetValue(director);
            Assert(beam.enabled&&beam.startWidth>=2&&!director.TruckDestroyed,"The backflip finisher shows a real thick continuous beam before the impact explosion");
            Advance(director,AncientProductionPhase.Secured);
            Assert(director.TruckDestroyed&&director.CargoSecured&&game.Cores==3,"The final truck explosion preserves all three safely separated cores");
            Assert(game.Story.Stage==7&&game.Story.DialogueOpen&&game.bossDefeated&&commander.IsAlive,"The real completion callback opens arrest dialogue with the captain alive");
            var captainArt=commander.GetComponentInChildren<SpriteRenderer>();
            Assert(captainArt&&captainArt.enabled,"The arrested captain stays visible after the vehicle's renderers are hidden");
            int points=game.Story.ResidencePoints;game.Story.ProductionCleared();
            Assert(points==1000&&game.Story.ResidencePoints==points,"Arrest awards the first promotion points once even if completion is called again");
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
                if(Now-TimeValue("Started")>90)throw new TimeoutException("Production smoke exceeded 90 seconds. Keep the Game view focused.");
                var game=AncientGame.Instance;if(!game||!game.hero||!game.hero.Combat||!game.Production||!game.GameplayInputReady||StageAge<.3)return;
                if(!AncientGame.IsQAProfile)throw new InvalidOperationException("Refusing to modify a normal gameplay profile.");
                if(game.MainMenu||game.skills||game.Story.LiftBusy||!Application.isFocused)return;
                int stage=SessionState.GetInt(Root+"Stage",0);
                if(stage==0)
                {
                    Assert(game.Story.Stage==3&&!game.Story.DialogueOpen&&!game.Production.Active,"Completed weapon practice leaves the stock objective available without starting the chase");
                    MoveHero(game.hero,new Vector2(AncientProductionLayout.StockX,1.3f));game.Story.TryProductionStock();
                    Assert(game.Story.Stage==4&&game.Story.DialogueOpen&&!game.Production.Active,"An actual stock-terminal request opens the discrepancy report before the breach");
                    SetStage(1);return;
                }
                if(stage==1)
                {
                    // Represents a revealed third report page, without synthesizing keyboard input.
                    Field(typeof(AncientStory),"lineIndex").SetValue(game.Story,2);Field(typeof(AncientStory),"pageReady").SetValue(game.Story,true);
                    Field(typeof(AncientStory),"showFull").SetValue(game.Story,true);Field(typeof(AncientStory),"revealedChars").SetValue(game.Story,14);
                    Call(game.Story,"Update");
                    Assert(game.Story.Stage==5&&!game.Story.DialogueOpen&&game.Production.Sequence.Phase==AncientProductionPhase.Breach,"The actual report interruption clears dialogue and starts the wall-breach director once");
                    SetStage(2);return;
                }
                if(!game.Production.Commander||game.Story.DialogueOpen)return;
                Advance(game.Production,AncientProductionPhase.Chase);CheckRun(game);
                Finish(true,"Real stock-report interruption, production director and collision wiring completed. Directed clocks, synthetic press buffers and manual Physics2D simulation verify structural wiring, not physical keyboard/mouse timing, live chase dodging, complete deck footing or cinematic visual quality; these still need a native human playthrough.");
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
                string path=Path.Combine(folder,"production-smoke-last.txt");File.WriteAllText(path,report,new UTF8Encoding(false));
                if(passed)Debug.Log("Production smoke PASS. Report: "+path);else Debug.LogError("Production smoke did not pass. Report: "+path);
            }
            catch(Exception error){Debug.LogError("Could not write movement smoke report: "+error.Message+"\n"+report);}
        }
    }
}
#endif
