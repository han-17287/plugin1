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
    public static class AncientMovementSmokeChecks
    {
        const string Menu="PLUGIN/검사/이동 점프 버퍼와 와이어";
        const string Root="PLUGIN.QA.MovementSmoke.",ProfileKey="PLUGIN.QA.ActiveProfile",NormalPrefix="PLUGIN.Ancient.v2.",QAPrefix="PLUGIN.Ancient.QA.";
        const BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string[] optionKeys={"muted","graphics","frameTarget"};
        [Serializable] sealed class PrefSnapshot {public string[] keys;public bool[] present;public int[] values;}

        static AncientMovementSmokeChecks()
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
            PlayerPrefs.SetInt(profile+"campaignVersion",1);PlayerPrefs.SetInt(profile+"cityFloor",0);PlayerPrefs.Save();
            Append("START: isolated movement smoke. Leave mouse, wheel and keyboard controls untouched.");
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
        static T Read<T>(AncientHero hero,string name){return (T)Field(typeof(AncientHero),name).GetValue(hero);}
        static void Set(AncientHero hero,string name,object value){Field(typeof(AncientHero),name).SetValue(hero,value);}
        static void Tick(AncientHero hero,string name)
        {
            var method=typeof(AncientHero).GetMethod(name,PrivateInstance);
            if(method==null)throw new MissingMethodException(typeof(AncientHero).FullName,name);method.Invoke(hero,null);
        }
        static bool Near(float a,float b){return Mathf.Abs(a-b)<.001f;}
        static void Prepare(AncientHero hero,Vector2 position,Vector2 velocity)
        {
            hero.TravelTo(position);hero.doubleJump=false;Set(hero,"axis",0f);
            var body=hero.GetComponent<Rigidbody2D>();body.linearVelocity=velocity;body.gravityScale=1;
            Physics2D.SyncTransforms();Set(hero,"groundSampleTick",-1f);
        }
        static void Queue(AncientHero hero,bool held)
        {Set(hero,"jumpBufferedUntil",Time.time+AncientMovementRules.BufferSeconds);Set(hero,"jumpHeld",held);Set(hero,"cutJump",false);}
        static void Attach(AncientHero hero,Vector2 point,AncientEnemy target,Vector2 normal)
        {
            var method=typeof(AncientHero).GetMethod("AttachWire",PrivateInstance);
            if(method==null)throw new MissingMethodException(typeof(AncientHero).FullName,"AttachWire");
            method.Invoke(hero,new object[]{point,target,normal});
        }
        static void CheckWireAttachments(AncientGame game,GameObject support)
        {
            var hero=game.hero;var body=hero.GetComponent<Rigidbody2D>();
            var cranes=Field(typeof(AncientTraversal),"cranes");var previous=cranes.GetValue(game.Traversal);
            bool zip=hero.ZipMode;int score=game.score,credits=game.credits,kills=game.kills,pickups=game.pickups.Count;
            GameObject deadTarget=null,lethalTarget=null;
            try
            {
                Field(typeof(AncientHero),"<ZipMode>k__BackingField").SetValue(hero,true);
                support.transform.rotation=Quaternion.identity;support.GetComponent<BoxCollider2D>().size=new Vector2(8,.3f);
                var platform=support.GetComponent<Rigidbody2D>();platform.bodyType=RigidbodyType2D.Kinematic;
                var crane=support.AddComponent<AncientMovingAnchor>();crane.enabled=false;cranes.SetValue(game.Traversal,new[]{crane});
                Vector2 local=new Vector2(.1f,.15f),clicked=(Vector2)support.transform.position+local;
                Prepare(hero,(Vector2)support.transform.position+Vector2.up*5,Vector2.zero);Attach(hero,clicked,null,Vector2.up);
                Assert(hero.Wired&&Read<Transform>(hero,"wireMovingAnchor")==support.transform&&(Read<Vector2>(hero,"wireMovingLocal")-local).sqrMagnitude<.00001f,"A registered moving crane retains the selected nonzero surface offset");
                support.transform.position+=new Vector3(2,1,0);Physics2D.SyncTransforms();Tick(hero,"FixedUpdate");
                Assert((Read<Vector2>(hero,"anchor")-(Vector2)support.transform.TransformPoint(local)).sqrMagnitude<.00001f,"The next physics step follows the clicked point as the actual moving support changes position");
                cranes.SetValue(game.Traversal,previous);platform.bodyType=RigidbodyType2D.Static;
                Prepare(hero,(Vector2)support.transform.position+Vector2.up*5,Vector2.zero);
                Attach(hero,(Vector2)support.transform.position+local,null,Vector2.up);Tick(hero,"FixedUpdate");
                Assert(hero.Wired&&!Read<Transform>(hero,"wireMovingAnchor")&&object.ReferenceEquals(Read<AncientEnemy>(hero,"wireEnemy"),null),"Ordinary static terrain keeps its wire when no enemy target reference exists");
                Prepare(hero,new Vector2(2,80),new Vector2(18,7));
                deadTarget=new GameObject("Movement QA destroyed wire enemy");deadTarget.transform.position=new Vector2(8,80);
                var enemy=deadTarget.AddComponent<AncientEnemy>();enemy.Initialize(0);enemy.enabled=false;
                Set(hero,"wireEnemy",enemy);Set(hero,"wired",true);Set(hero,"wireZip",true);Set(hero,"anchor",enemy.AimPoint);Set(hero,"wireAttachedAt",Time.time);
                // Synchronous destruction is restricted to this temporary Editor fixture.
                UnityEngine.Object.DestroyImmediate(deadTarget);
                Assert(!enemy&&!object.ReferenceEquals(enemy,null),"The real destroyed enemy exercises Unity's fake-null reference semantics");
                Tick(hero,"FixedUpdate");
                Assert(!hero.Wired&&object.ReferenceEquals(Read<AncientEnemy>(hero,"wireEnemy"),null)&&Near(body.linearVelocity.x,18)&&Near(body.linearVelocity.y,7),"Destroying the target releases its wire without a ghost destination or lost air momentum");
                Prepare(hero,new Vector2(2,80),new Vector2(4,1));
                lethalTarget=new GameObject("Movement QA lethal wire enemy");lethalTarget.transform.position=new Vector2(8,80);
                var fragile=lethalTarget.AddComponent<AncientEnemy>();fragile.Initialize(0);fragile.enabled=false;Field(typeof(AncientEnemy),"hp").SetValue(fragile,1f);game.enemies.Add(fragile);
                Attach(hero,fragile.AimPoint,fragile,Vector2.zero);
                Assert(!fragile.IsAlive&&!hero.Wired&&object.ReferenceEquals(Read<AncientEnemy>(hero,"wireEnemy"),null)&&Near(body.linearVelocity.x,4)&&Near(body.linearVelocity.y,1),"A lethal initial wire hit releases synchronously before assigning zip velocity toward a dead target");
            }
            finally
            {
                cranes.SetValue(game.Traversal,previous);Field(typeof(AncientHero),"<ZipMode>k__BackingField").SetValue(hero,zip);
                game.score=score;game.credits=credits;game.kills=kills;
                for(int i=game.pickups.Count-1;i>=pickups;i--){var pickup=game.pickups[i];game.pickups.RemoveAt(i);if(pickup)UnityEngine.Object.Destroy(pickup.gameObject);}
                if(deadTarget)UnityEngine.Object.Destroy(deadTarget);if(lethalTarget){var enemy=lethalTarget.GetComponent<AncientEnemy>();game.enemies.Remove(enemy);UnityEngine.Object.Destroy(lethalTarget);}
            }
        }
        static void CheckCompletedZip(AncientGame game,GameObject support)
        {
            var hero=game.hero;var body=hero.GetComponent<Rigidbody2D>();var platform=support.GetComponent<Rigidbody2D>();var collider=support.GetComponent<BoxCollider2D>();
            var simulation=Physics2D.simulationMode;Vector3 oldPosition=support.transform.position;Vector2 size=collider.size,platformVelocity=platform.linearVelocity;var kind=platform.bodyType;
            var cranes=Field(typeof(AncientTraversal),"cranes");object registered=cranes.GetValue(game.Traversal);GameObject freeCrane=null;
            try
            {
                Physics2D.simulationMode=SimulationMode2D.Script;float dt=Time.fixedDeltaTime;
                Field(typeof(AncientHero),"<ZipMode>k__BackingField").SetValue(hero,true);
                Prepare(hero,new Vector2(1.6f,83.905f),Vector2.zero);Attach(hero,new Vector2(2,85),null,Vector2.down);
                Vector2 before=body.position;Tick(hero,"FixedUpdate");
                Assert(hero.Wired&&Read<bool>(hero,"wireFinishPending")&&body.position==before&&body.linearVelocity.x>0,"A static final zip step plans completion without releasing or teleporting before physics moves");
                Physics2D.SyncTransforms();Physics2D.Simulate(dt);Tick(hero,"FixedUpdate");
                Assert(!hero.Wired&&!Read<bool>(hero,"wireFinishPending")&&Near(body.position.x,2)&&Near(body.linearVelocity.x,0)&&Near(body.linearVelocity.y,0),"The completed static zip stops at its physical destination without carrying its final fast step into an unwanted drift");

                platform.bodyType=RigidbodyType2D.Kinematic;collider.size=new Vector2(8,.3f);support.transform.position=new Vector2(2,84);platform.linearVelocity=new Vector2(7,0);Physics2D.SyncTransforms();
                Prepare(hero,new Vector2(1.7f,85.245f),Vector2.zero);Attach(hero,new Vector2(2.1f,84.15f),null,Vector2.up);Tick(hero,"FixedUpdate");
                Assert(hero.Wired&&Read<bool>(hero,"wireFinishPending")&&Read<Transform>(hero,"wireMovingAnchor")==support.transform,"The final moving-roof step stays attached while targeting the actual selected collider point");
                Physics2D.SyncTransforms();Physics2D.Simulate(dt);Tick(hero,"FixedUpdate");
                Assert(!hero.Wired&&!Read<bool>(hero,"wireFinishPending")&&Near(body.linearVelocity.x,7)&&Near(Read<float>(hero,"airborneSurface"),7),"Moving-roof completion uses the completed step and inherits the carrier's seven-unit speed even after its latch has moved ahead");

                platform.linearVelocity=Vector2.zero;platform.bodyType=RigidbodyType2D.Static;support.transform.position=oldPosition;Physics2D.SyncTransforms();
                freeCrane=new GameObject("Movement QA transform-only crane");freeCrane.transform.position=new Vector2(2,85);
                var crane=freeCrane.AddComponent<AncientMovingAnchor>();crane.enabled=false;cranes.SetValue(game.Traversal,new[]{crane});
                Prepare(hero,new Vector2(1.6f,83.905f),Vector2.zero);Attach(hero,new Vector2(2,85),null,Vector2.down);Tick(hero,"FixedUpdate");
                Assert(hero.Wired&&Read<bool>(hero,"wireFinishPending"),"A transform-only crane can plan a real final wire step without a rigidbody");
                Physics2D.SyncTransforms();Physics2D.Simulate(dt);freeCrane.transform.position+=Vector3.right*(3*dt);Physics2D.SyncTransforms();Tick(hero,"FixedUpdate");
                Assert(!hero.Wired&&Mathf.Abs(body.linearVelocity.x-3)<.01f&&Mathf.Abs(Read<float>(hero,"airborneSurface")-3)<.01f,"A completed transform-crane zip preserves measured horizontal carrier motion instead of a stale zip velocity");

                Prepare(hero,new Vector2(1.6f,83.905f),Vector2.zero);Attach(hero,new Vector2(2,85),null,Vector2.down);Tick(hero,"FixedUpdate");
                Assert(Read<bool>(hero,"wireFinishPending"),"The interruption probe begins with a pending physical finish");
                hero.TravelTo(new Vector2(2,80));Assert(!hero.Wired&&!Read<bool>(hero,"wireFinishPending"),"Travel or retry clears a pending wire finish before another destination can inherit it");
            }
            finally
            {
                cranes.SetValue(game.Traversal,registered);hero.TravelTo(new Vector2(2,80));if(freeCrane)UnityEngine.Object.Destroy(freeCrane);
                platform.bodyType=kind;if(kind!=RigidbodyType2D.Static)platform.linearVelocity=platformVelocity;collider.size=size;support.transform.position=oldPosition;Physics2D.SyncTransforms();Physics2D.simulationMode=simulation;
            }
        }
        static void CheckRealHero(AncientGame game)
        {
            var hero=game.hero;var body=hero.GetComponent<Rigidbody2D>();
            bool enabled=hero.enabled,skills=game.skills,doubleJump=hero.doubleJump;float hp=hero.hp,gravity=body.gravityScale;
            int inputReady=(int)Field(typeof(AncientGame),"inputReadyFrame").GetValue(game);
            Vector2 position=body.position,velocity=body.linearVelocity;GameObject support=null;
            try
            {
                hero.enabled=false;hero.hp=10000;
                // Keep probes above the tutorial ceiling solid (y10..42).
                support=new GameObject("Movement QA support");support.layer=8;support.transform.position=new Vector2(2,69.75f);
                var platform=support.AddComponent<Rigidbody2D>();platform.bodyType=RigidbodyType2D.Kinematic;
                support.AddComponent<BoxCollider2D>().size=new Vector2(8,.5f);
                Prepare(hero,new Vector2(2,71.1f),new Vector2(0,-8));
                Set(hero,"airJumpUsed",true);Queue(hero,false);Tick(hero,"FixedUpdate");
                Assert(Near(body.linearVelocity.y,7.7f),"A released press buffered before landing creates one short hop in the real Hero physics path");
                Assert(!Read<bool>(hero,"airJumpUsed")&&Read<float>(hero,"jumpBufferedUntil")<Time.time,"Landing refreshes the air jump and consumes the queued press");
                Queue(hero,true);Tick(hero,"FixedUpdate");
                Assert(Read<float>(hero,"lastGrounded")<0&&Read<float>(hero,"jumpBufferedUntil")>Time.time,"An upward capsule near the floor cannot rearm a second ground jump");
                Prepare(hero,new Vector2(2,71.1f),new Vector2(0,-8));Queue(hero,true);Tick(hero,"FixedUpdate");
                Assert(Near(body.linearVelocity.y,14),"A held landing buffer preserves the full ground-jump speed");
                Prepare(hero,new Vector2(2,80),new Vector2(0,-2));hero.doubleJump=true;Queue(hero,false);Tick(hero,"FixedUpdate");
                Assert(Near(body.linearVelocity.y,6.6f)&&Read<bool>(hero,"airJumpUsed"),"The real air-jump path respects a released buffered press");
                Queue(hero,true);Tick(hero,"FixedUpdate");
                Assert(Read<float>(hero,"jumpBufferedUntil")>Time.time&&Near(body.linearVelocity.y,6.6f),"An already spent air jump cannot consume another press");
                Prepare(hero,new Vector2(2,71.1f),new Vector2(0,5));platform.linearVelocity=Vector2.up*5;Queue(hero,true);Tick(hero,"FixedUpdate");
                Assert(Near(body.linearVelocity.y,14),"A rider moving with a rising support can still use a ground jump");
                platform.linearVelocity=Vector2.zero;support.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan(.1f)*Mathf.Rad2Deg);
                Prepare(hero,new Vector2(2,71.1f),new Vector2(9,.9f));Queue(hero,true);Tick(hero,"FixedUpdate");
                Assert(Read<Vector2>(hero,"groundNormal").x<-.05f,"The foot probe retains the actual uphill ramp normal instead of an initial-overlap normal");
                Assert(Near(body.linearVelocity.y,14),"Running uphill on a physical ramp preserves ground-jump eligibility");
                Prepare(hero,new Vector2(2,80),new Vector2(0,-2));Set(hero,"lastGrounded",Time.time-.06f);Queue(hero,true);Tick(hero,"FixedUpdate");
                Assert(Near(body.linearVelocity.y,14),"A recent ledge departure can use coyote time in the real physics path");
                Prepare(hero,new Vector2(2,80),new Vector2(0,-2));Queue(hero,true);Set(hero,"cutJump",true);game.skills=true;Tick(hero,"Update");
                Assert(Read<float>(hero,"jumpBufferedUntil")<Time.time&&!Read<bool>(hero,"jumpHeld")&&!Read<bool>(hero,"cutJump"),"Opening a pause discards the queued jump and release flags");
                game.skills=false;Tick(hero,"FixedUpdate");Assert(Near(body.linearVelocity.y,-2),"Unpausing cannot replay a queued jump");
                Prepare(hero,new Vector2(2,80),Vector2.zero);Set(hero,"wired",true);Set(hero,"wireZip",true);
                Set(hero,"anchor",new Vector2(2,85));Set(hero,"wireSurfaceNormal",Vector2.down);Set(hero,"wireAttachedAt",Time.time);Tick(hero,"FixedUpdate");
                Assert(hero.Wired&&Near(body.linearVelocity.x,0)&&Near(body.linearVelocity.y,AncientZipRules.Speed),"The existing active zip still pulls immediately in a straight line without gravity or a tangent kick");
                Set(hero,"wireZip",false);body.linearVelocity=new Vector2(18,7);var release=typeof(AncientHero).GetMethod("StopWire",PrivateInstance);release.Invoke(hero,new object[]{false});
                Assert(!hero.Wired&&Near(body.linearVelocity.x,18)&&Near(body.linearVelocity.y,7),"Releasing a wire without launch preserves the actual Rigidbody momentum");
                Tick(hero,"FixedUpdate");Assert(Near(body.linearVelocity.x,18),"The post-wire air-control path carries released horizontal momentum");
                CheckWireAttachments(game,support);CheckCompletedZip(game,support);
                Prepare(hero,new Vector2(2,80),Vector2.zero);
                Field(typeof(AncientHero),"<ZipMode>k__BackingField").SetValue(hero,true);Attach(hero,new Vector2(2,46),null,Vector2.down);
                Field(typeof(AncientHero),"<ZipMode>k__BackingField").SetValue(hero,false);Attach(hero,new Vector2(4,46),null,Vector2.down);
                Assert(hero.Wired&&!hero.ZipMode&&!Read<bool>(hero,"wireZip"),"Replacing a zip with a middle-click swing preserves the requested swing mode through wire release");
                Vector2 previewVelocity=body.linearVelocity,previewPosition=body.position;Tick(hero,"Update");
                Assert(hero.Wired&&!hero.ZipMode&&(body.linearVelocity-previewVelocity).sqrMagnitude<.00001f&&body.position==previewPosition,"A normal Hero Update keeps an active swing preview without changing its physical movement");
                release.Invoke(hero,new object[]{false});Tick(hero,"Update");
                Assert(!hero.Wired&&hero.ZipMode&&(body.linearVelocity-previewVelocity).sqrMagnitude<.00001f&&body.position==previewPosition,"After swing release the normal Hero Update restores the primary left-click zip preview without changing momentum");
                Queue(hero,true);Set(hero,"cutJump",true);hero.TravelTo(new Vector2(2,80));
                Assert(Read<float>(hero,"jumpBufferedUntil")<Time.time&&!Read<bool>(hero,"jumpHeld")&&!Read<bool>(hero,"cutJump"),"Travel clears queued jump input before arriving in another area");
                Queue(hero,true);game.BlockGameplayInput();Tick(hero,"Update");
                Assert(Read<float>(hero,"jumpBufferedUntil")<Time.time&&!Read<bool>(hero,"jumpHeld"),"The post-dialogue input guard cannot retain the Space press that closed dialogue");
            }
            finally
            {
                game.skills=skills;Field(typeof(AncientGame),"inputReadyFrame").SetValue(game,inputReady);
                hero.doubleJump=doubleJump;hero.hp=hp;hero.TravelTo(position);body.linearVelocity=velocity;body.gravityScale=gravity;hero.enabled=enabled;
                if(support){support.SetActive(false);UnityEngine.Object.Destroy(support);}
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
                if(Now-TimeValue("Started")>45)throw new TimeoutException("Movement smoke exceeded 45 seconds.");
                var game=AncientGame.Instance;if(!game||!game.hero||!game.hero.Combat||!game.GameplayInputReady||StageAge<.4)return;
                if(!AncientGame.IsQAProfile)throw new InvalidOperationException("Refusing to modify a normal gameplay profile.");
                Assert(!game.MainMenu&&!game.Paused&&!game.ArenaActive,"The temporary profile starts outside a paused scene or boss arena");
                CheckRealHero(game);
                Finish(true,"Real Hero reflection fixtures completed. No physical input was synthesized. Keyboard timing, actual ramp contact simulation, moving-platform travel, frame pacing and visual alignment still need a native playthrough.");
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
                string path=Path.Combine(folder,"movement-smoke-last.txt");File.WriteAllText(path,report,new UTF8Encoding(false));
                if(passed)Debug.Log("Movement smoke PASS. Report: "+path);else Debug.LogError("Movement smoke did not pass. Report: "+path);
            }
            catch(Exception error){Debug.LogError("Could not write movement smoke report: "+error.Message+"\n"+report);}
        }
    }
}
#endif
