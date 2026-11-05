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
    public static class AncientBladeSmokeChecks
    {
        const string Menu="PLUGIN/검사/블레이드 3연격과 파란 입자";
        const string Root="PLUGIN.QA.BladeSmoke.",ProfileKey="PLUGIN.QA.ActiveProfile",NormalPrefix="PLUGIN.Ancient.v2.",QAPrefix="PLUGIN.Ancient.QA.";
        const BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string[] optionKeys={"muted","graphics","frameTarget"};
        static AncientProjectile recoveryProbe;
        [Serializable] sealed class PrefSnapshot {public string[] keys;public bool[] present;public int[] values;}

        static AncientBladeSmokeChecks()
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
            Append("START: isolated blade smoke. Leave mouse, wheel and keyboard controls untouched.");
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
        static void SetAttackMode(AncientHero hero)
        {Field(typeof(AncientHero),"<AttackMode>k__BackingField").SetValue(hero,true);}
        static void TickCombat(AncientCombat combat)
        {
            var method=typeof(AncientCombat).GetMethod("Update",PrivateInstance);
            if(method==null)throw new MissingMethodException(typeof(AncientCombat).FullName,"Update");method.Invoke(combat,null);
        }
        static AncientEnergyMote[] Motes(AncientCombat combat)
        {return (AncientEnergyMote[])Field(typeof(AncientCombat),"motes").GetValue(combat);}
        static bool HasBluePixels(Texture2D texture)
        {
            // Runtime mote textures discard their CPU copy; read the displayed texture back explicitly.
            RenderTexture previous=RenderTexture.active;
            var target=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);
            Texture2D readable=null;
            try
            {
                Graphics.Blit(texture,target);RenderTexture.active=target;
                readable=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
                readable.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);readable.Apply();
                foreach(var color in readable.GetPixels32())if(color.a>80&&color.b>color.r+30&&color.g>color.r+20)return true;
                return false;
            }
            finally
            {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);if(readable)UnityEngine.Object.Destroy(readable);}
        }
        static void CheckAtlas()
        {
            var distinct=new System.Collections.Generic.HashSet<Sprite>();
            for(int step=1;step<=3;step++)for(int frame=0;frame<16;frame++)
            {
                var sprite=AncientArt.Blade(step,frame);
                Assert(sprite&&sprite.texture&&sprite.rect.width>0&&sprite.rect.height>0,"Blade "+step+"/"+frame+" loads a nonempty sprite");
                Assert(sprite==AncientArt.Blade(step,frame),"Blade "+step+"/"+frame+" reuses its cached sprite");
                Assert(sprite.texture.filterMode==FilterMode.Point,"Blade "+step+"/"+frame+" uses point sampling");distinct.Add(sprite);
            }
            Assert(distinct.Count==48,"The three cuts have 48 distinct sprite objects");
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
                if(Now-TimeValue("Started")>45)throw new TimeoutException("Blade smoke exceeded 45 seconds. Stage="+SessionState.GetInt(Root+"Stage",0));
                var game=AncientGame.Instance;if(!game||!game.hero||!game.hero.Combat)return;
                if(!AncientGame.IsQAProfile)throw new InvalidOperationException("Refusing to modify a normal gameplay profile.");
                var hero=game.hero;var combat=hero.Combat;int stage=SessionState.GetInt(Root+"Stage",0);
                if(stage==0)
                {
                    if(!game.GameplayInputReady||StageAge<.4)return;
                    Assert(!game.MainMenu&&!game.Paused,"The temporary profile starts in an interactive scene");CheckAtlas();
                    hero.TravelTo(new Vector2(2,1.4f));hero.hp=10000;
                    var body=hero.GetComponent<Rigidbody2D>();body.constraints=RigidbodyConstraints2D.FreezeAll;
                    SetAttackMode(hero);combat.StrikeBlade();
                    Assert(combat.BladeAnimating&&combat.BladeStep==1&&combat.BladeUses==1,"First tap starts the vertical cut");SetStage(1);return;
                }
                if(stage==1)
                {
                    if(StageAge<AncientBladeRules.Duration(1)-AncientBladeRules.InputBufferWindow*.5f)return;
                    combat.StrikeBlade();Assert(combat.BladeUses==1,"A late tap is queued rather than resolving a second strike immediately");SetStage(2);return;
                }
                if(stage==2)
                {
                    if(combat.BladeUses<2)return;
                    Assert(combat.BladeUses==2&&combat.BladeStep==2,"The queued tap starts the horizontal cut after recovery");SetStage(3);return;
                }
                if(stage==3)
                {
                    if(StageAge<AncientBladeRules.Duration(2)-AncientBladeRules.InputBufferWindow*.5f)return;
                    combat.StrikeBlade();combat.CancelInput();SetStage(4);return;
                }
                if(stage==4)
                {
                    if(StageAge<AncientBladeRules.InputBufferWindow+.05f)return;
                    Assert(combat.BladeUses==2,"Cancelling input discards the queued tap");combat.StrikeBlade();
                    Assert(combat.BladeStep==3,"A deliberate third tap starts the diagonal finisher");SetStage(5);return;
                }
                if(stage==5)
                {
                    if(StageAge<AncientBladeRules.Duration(3)+.04f)return;
                    combat.StrikeBlade();Assert(combat.BladeStep==1,"A completed three-cut sequence cycles to the vertical cut");SetStage(6);return;
                }
                if(stage==6)
                {
                    if(StageAge<AncientBladeRules.Duration(1)+AncientBladeRules.ComboGrace+.08f)return;
                    combat.StrikeBlade();Assert(combat.BladeStep==1,"An expired combo link starts a new vertical cut");
                    combat.CancelBlade();Assert(!combat.BladeAnimating&&combat.BladeStep==0,"Hard cancellation clears the active cut and combo chain");
                    combat.StrikeBlade();Assert(combat.BladeAnimating&&combat.BladeStep==1,"Hard cancellation does not leave a stale blade cooldown");
                    Field(typeof(AncientCombat),"heldAt").SetValue(combat,Time.time-.4f);
                    Assert(combat.Charging,"The interruption probe begins with held laser input");float hp=hero.hp;hero.Hurt(1);
                    Assert(hero.hp<hp&&!combat.BladeAnimating&&!combat.Charging,"Taking a hit discards both the blade and held laser input");SetStage(7);return;
                }
                if(stage==7)
                {
                    if(StageAge<.3)return;
                    combat.StrikeBlade();Assert(combat.BladeAnimating,"Melee becomes available after hit recovery");
                    game.skills=true;TickCombat(combat);
                    Assert(!combat.BladeAnimating&&combat.BladeStep==0,"Opening a pause menu clears the active cut");int count=combat.BladeUses;combat.StrikeBlade();
                    Assert(combat.BladeUses==count,"A direct strike request cannot bypass a pause menu");game.skills=false;SetStage(8);return;
                }
                if(stage==8)
                {
                    if(StageAge<.1||!game.GameplayInputReady)return;
                    combat.StrikeBlade();Assert(combat.BladeAnimating,"Unpaused gameplay can start a fresh cut");game.BlockGameplayInput();TickCombat(combat);
                    Assert(!combat.BladeAnimating,"The post-dialogue input guard cancels a pending cut");int count=combat.BladeUses;combat.StrikeBlade();
                    Assert(combat.BladeUses==count,"A strike request respects the post-dialogue input guard");SetStage(9);return;
                }
                if(stage==9)
                {
                    if(StageAge<.1||!game.GameplayInputReady)return;
                    combat.StrikeBlade();hero.TravelTo(new Vector2(2,1.4f));
                    Assert(!combat.BladeAnimating&&combat.BladeStep==0,"Travel clears the previous room's attack state");
                    combat.EmitParticles(new Vector2(3,2),18);int active=0;float energy=0;Texture2D sharedMoteTexture=null;
                    foreach(var mote in Motes(combat))if(mote.gameObject.activeSelf)
                    {
                        active++;var renderer=mote.GetComponent<SpriteRenderer>();var color=renderer.color;
                        if(!sharedMoteTexture){sharedMoteTexture=renderer.sprite.texture;Assert(HasBluePixels(sharedMoteTexture),"The displayed collectible texture contains blue/cyan crystal pixels");}
                        Assert(renderer.sprite.texture==sharedMoteTexture&&color.b>=color.r&&color.g>=color.r,"A live collectible reuses the blue texture without a yellow tint");
                        energy+=(float)Field(typeof(AncientEnergyMote),"energy").GetValue(mote);
                    }
                    Assert(active==3&&Mathf.Abs(energy-18)<.001f,"Particle recoloring preserves the three-mote energy total");
                    combat.ResetCombat();foreach(var mote in Motes(combat))Assert(!mote.gameObject.activeSelf,"Reset returns every energy mote to the pool");
                    SetAttackMode(hero);hero.AddCapturedEnergy(50);combat.StrikeBlade();int shots=combat.LaserShots;combat.FireLaser();
                    Assert(!combat.BladeAnimating&&combat.Firing&&combat.LaserShots==shots+1&&hero.CaptureEnergy==0,"Laser release cancels melee, consumes stored energy and starts its continuous beam");
                    SetStage(10);return;
                }
                if(stage==10)
                {
                    // Probe the scene at known animation instants without synthesizing mouse input.
                    // Actual pooled projectiles, removal and Rigidbody2D rebound still run normally.
                    hero.TravelTo(new Vector2(2,6));SetAttackMode(hero);
                    var body=hero.GetComponent<Rigidbody2D>();body.constraints=RigidbodyConstraints2D.FreezeRotation;body.gravityScale=0;
                    Physics2D.SyncTransforms();Assert(!hero.IsOnGround,"The object-break probes run in open air");
                    combat.StrikeBlade();Assert(combat.BladeAnimating&&combat.BladeStep==1,"The airborne probe starts a fresh vertical cut");
                    Field(typeof(AncientCombat),"bladeDirection").SetValue(combat,Vector2.right);
                    Field(typeof(AncientCombat),"bladeStarted").SetValue(combat,Time.time-AncientBladeRules.ImpactTime(1)-.005f);TickCombat(combat);
                    Assert(!(bool)Field(typeof(AncientCombat),"bladeHitPending").GetValue(combat),"The ordinary enemy-damage pulse resolves before the late object arrives");
                    int broken=combat.BrokenObjects;body.linearVelocity=new Vector2(7,-4);
                    Vector2 objectPoint=(Vector2)hero.transform.position+new Vector2(1.7f,.55f);
                    var late=game.Projectiles.Spawn(objectPoint,Vector2.zero,12,false,1);
                    Assert(late&&!late.Available&&game.shots.Contains(late),"A real pooled plate enters the late active swing");
                    Field(typeof(AncientCombat),"bladeStarted").SetValue(combat,Time.time-AncientBladeRules.ActiveEnd(1)+.005f);TickCombat(combat);
                    Assert(late.Available&&!game.shots.Contains(late)&&combat.BrokenObjects==broken+1,"A plate arriving after the impact pulse still breaks during the visible swing");
                    float firstLift=body.linearVelocity.y;
                    Assert(firstLift>0&&Mathf.Abs(body.linearVelocity.x-7)<.001f,"The first airborne object break provides lift while preserving horizontal speed");
                    var second=game.Projectiles.Spawn(objectPoint+Vector2.right*.15f,Vector2.zero,12,false,1);
                    Assert(second,"A second pooled plate spawns during the same cut");TickCombat(combat);
                    Assert(second.Available&&combat.BrokenObjects==broken+2,"The same visible cut can break a second physical object");
                    Assert(Mathf.Abs(body.linearVelocity.y-firstLift)<.001f&&Mathf.Abs(body.linearVelocity.x-7)<.001f,"Several objects in one cut cannot stack the airborne rebound");
                    recoveryProbe=game.Projectiles.Spawn(objectPoint,Vector2.zero,12,false,1);
                    Assert(recoveryProbe&&!recoveryProbe.Available,"A third physical plate enters during recovery");
                    Field(typeof(AncientCombat),"bladeStarted").SetValue(combat,Time.time-AncientBladeRules.ActiveEnd(1)-.005f);TickCombat(combat);
                    Assert(!recoveryProbe.Available&&combat.BrokenObjects==broken+2,"Recovery frames cannot break a newly arriving object");
                    var missedContact=new GameObject("QA missed blade contact enemy").AddComponent<AncientEnemy>();
                    missedContact.Initialize(0);missedContact.enabled=false;missedContact.transform.position=objectPoint-Vector2.up*.95f;game.enemies.Add(missedContact);
                    try
                    {
                        // A stalled render can skip from anticipation straight into recovery.
                        // A pending contact flag must expire rather than damaging a fresh target late.
                        Field(typeof(AncientCombat),"bladeHitPending").SetValue(combat,true);
                        Field(typeof(AncientCombat),"bladeStarted").SetValue(combat,Time.time-AncientBladeRules.ActiveEnd(1)-.01f);TickCombat(combat);
                        Assert(Mathf.Abs(missedContact.HealthRatio-1)<.00001f&&!(bool)Field(typeof(AncientCombat),"bladeHitPending").GetValue(combat),"Skipping the visible contact window clears pending enemy damage without an invisible recovery hit");
                    }
                    finally {game.enemies.Remove(missedContact);UnityEngine.Object.Destroy(missedContact.gameObject);}
                    SessionState.SetInt(Root+"RecoveryBroken",combat.BrokenObjects);
                    body.linearVelocity=Vector2.zero;body.constraints=RigidbodyConstraints2D.FreezeAll;SetStage(11);return;
                }
                if(stage==11)
                {
                    if(StageAge<.12)return;
                    Assert(recoveryProbe&&!recoveryProbe.Available&&game.shots.Contains(recoveryProbe)&&combat.BrokenObjects==SessionState.GetInt(Root+"RecoveryBroken",-1),"The recovery plate remains intact through subsequent scene updates");
                    recoveryProbe.Remove();recoveryProbe=null;combat.ResetCombat();
                    Finish(true,"Structural blade, late physical-object break, one-rebound-per-cut and particle checks completed. Physical hold/release, wheel switching, airborne wire transitions, frame pacing and visual alignment still need manual playtesting.");
                }
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
                string path=Path.Combine(folder,"blade-smoke-last.txt");File.WriteAllText(path,report,new UTF8Encoding(false));
                if(passed)Debug.Log("Blade smoke PASS. Report: "+path);else Debug.LogError("Blade smoke did not pass. Report: "+path);
            }
            catch(Exception error){Debug.LogError("Could not write blade smoke report: "+error.Message+"\n"+report);}
        }
    }
}
#endif
