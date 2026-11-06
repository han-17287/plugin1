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
        static void Invoke(object owner,string method)
        {var call=owner.GetType().GetMethod(method,PrivateInstance);if(call==null)throw new MissingMethodException(owner.GetType().Name,method);call.Invoke(owner,null);}
        static void MoveProbeHero(AncientHero hero,Vector2 point)
        {hero.GetComponent<Rigidbody2D>().position=point;hero.transform.position=point;Physics2D.SyncTransforms();Field(typeof(AncientHero),"groundSampleTick").SetValue(hero,-1f);}
        static void FireRight(AncientGame game)
        {
            // The existing no-camera fallback gives a deterministic release direction without mouse synthesis.
            var field=Field(typeof(AncientGame),"view");var camera=field.GetValue(game);
            game.hero.AimWeaponAt((Vector2)game.hero.transform.position+Vector2.right*10);
            try {field.SetValue(game,null);game.hero.Combat.FireLaser();}
            finally {field.SetValue(game,camera);}
        }
        static AncientEnemy LaserProbeEnemy(AncientGame game,Vector2 aimPoint)
        {
            var enemy=new GameObject("QA surviving laser target").AddComponent<AncientEnemy>();
            enemy.Initialize(0);enemy.enabled=false;enemy.transform.position=aimPoint-Vector2.up*.95f;
            Field(typeof(AncientEnemy),"hp").SetValue(enemy,1000f);Field(typeof(AncientEnemy),"maxHP").SetValue(enemy,1000f);game.enemies.Add(enemy);return enemy;
        }
        static float MoteEnergy(AncientCombat combat)
        {float total=0;foreach(var mote in Motes(combat))if(mote.gameObject.activeSelf)total+=(float)Field(typeof(AncientEnergyMote),"energy").GetValue(mote);return total;}
        static void CheckScheduledBladeHitches(AncientGame game)
        {
            var hero=game.hero;var combat=hero.Combat;var body=hero.GetComponent<Rigidbody2D>();Vector2 original=hero.transform.position;
            AncientEnemy enemy=null;var constraints=body.constraints;float gravity=body.gravityScale;
            try
            {
                combat.ResetCombat();MoveProbeHero(hero,new Vector2(2,80));body.constraints=RigidbodyConstraints2D.FreezeAll;SetAttackMode(hero);
                enemy=LaserProbeEnemy(game,(Vector2)hero.transform.position+new Vector2(1.84f,.55f));
                combat.StrikeBlade();Field(typeof(AncientCombat),"bladeDirection").SetValue(combat,Vector2.right);
                Field(typeof(AncientCombat),"bladeStarted").SetValue(combat,Time.time-.13f);TickCombat(combat);
                Assert(Mathf.Abs(enemy.HealthRatio-1)<.00001f&&(bool)Field(typeof(AncientCombat),"bladeHitPending").GetValue(combat),"A real blade keeps its accepted contact pending immediately before the first impact");
                Field(typeof(AncientCombat),"bladeStarted").SetValue(combat,Time.time-.23f);TickCombat(combat);
                Assert(Mathf.Abs(enemy.HealthRatio-.972f)<.00001f&&!(bool)Field(typeof(AncientCombat),"bladeHitPending").GetValue(combat),"A real Combat.Update crossing .13 to .23 resolves the first cut once even after the visible active window");
                TickCombat(combat);TickCombat(combat);
                Assert(Mathf.Abs(enemy.HealthRatio-.972f)<.00001f,"Later recovery updates cannot repeat that scheduled enemy contact");

                combat.ResetCombat();Field(typeof(AncientEnemy),"hp").SetValue(enemy,1000f);combat.StrikeBlade();
                Field(typeof(AncientCombat),"bladeDirection").SetValue(combat,Vector2.right);
                Field(typeof(AncientCombat),"bladeStarted").SetValue(combat,Time.time-.43f);
                Field(typeof(AncientCombat),"bladeReady").SetValue(combat,Time.time-.07f);
                Field(typeof(AncientCombat),"bladeUntil").SetValue(combat,Time.time-.07f);
                int uses=combat.BladeUses;combat.StrikeBlade();
                Assert(Mathf.Abs(enemy.HealthRatio-.972f)<.00001f&&combat.BladeUses==uses+1&&combat.BladeStep==2&&(bool)Field(typeof(AncientCombat),"bladeHitPending").GetValue(combat),"A following real tap flushes the old overdue hit before creating the second cut's independent anticipation");
                TickCombat(combat);Assert(Mathf.Abs(enemy.HealthRatio-.972f)<.00001f,"The fresh second cut cannot inherit an immediate duplicate contact");
                Field(typeof(AncientCombat),"bladeDirection").SetValue(combat,Vector2.right);
                Field(typeof(AncientCombat),"bladeStarted").SetValue(combat,Time.time-.24f);TickCombat(combat);TickCombat(combat);
                Assert(Mathf.Abs(enemy.HealthRatio-.944f)<.00001f,"Both accepted cuts apply exactly one contact across a late-input handoff");

                combat.ResetCombat();Field(typeof(AncientEnemy),"hp").SetValue(enemy,1000f);combat.StrikeBlade();game.skills=true;TickCombat(combat);game.skills=false;
                Field(typeof(AncientCombat),"bladeStarted").SetValue(combat,Time.time-.23f);TickCombat(combat);
                Assert(Mathf.Abs(enemy.HealthRatio-1)<.00001f&&!(bool)Field(typeof(AncientCombat),"bladeHitPending").GetValue(combat),"A cut canceled by the actual pause path stays canceled when its old impact time passes");
            }
            finally
            {
                game.skills=false;combat.ResetCombat();if(enemy){game.enemies.Remove(enemy);UnityEngine.Object.Destroy(enemy.gameObject);}
                body.constraints=constraints;body.gravityScale=gravity;MoveProbeHero(hero,original);SetAttackMode(hero);
            }
        }
        static void CheckLaserPulse(AncientGame game)
        {
            var hero=game.hero;var combat=hero.Combat;var body=hero.GetComponent<Rigidbody2D>();var original=(Vector2)hero.transform.position;
            AncientEnemy late=null,replacement=null;var wall=new GameObject("QA moving laser obstruction");
            try
            {
                combat.ResetCombat();hero.SpendCapturedEnergy();MoveProbeHero(hero,new Vector2(2,80));body.constraints=RigidbodyConstraints2D.FreezeAll;SetAttackMode(hero);
                Vector2 initialOrigin=(Vector2)hero.transform.position+new Vector2(.14f,.55f);
                wall.layer=8;var solid=wall.AddComponent<BoxCollider2D>();solid.size=new Vector2(1,8);wall.transform.position=initialOrigin+Vector2.right*9;Physics2D.SyncTransforms();
                late=LaserProbeEnemy(game,initialOrigin+new Vector2(5,8));hero.AddCapturedEnergy(50);FireRight(game);
                Assert(combat.Firing&&hero.CaptureEnergy==0,"A pulse consumes its release energy exactly once");
                Vector2 start=(Vector2)Field(typeof(AncientCombat),"beamStart").GetValue(combat),end=(Vector2)Field(typeof(AncientCombat),"beamEnd").GetValue(combat);
                Assert(Mathf.Abs(end.x-solid.bounds.min.x)<.03f&&Mathf.Abs(end.y-start.y)<.001f,"The released thick beam clips to the actual solid face");
                MoveProbeHero(hero,(Vector2)hero.transform.position+new Vector2(2,1));hero.AddCapturedEnergy(30);
                Vector2 movedOrigin=(Vector2)hero.transform.position+new Vector2(.14f,.55f);late.transform.position=movedOrigin+Vector2.right*5-Vector2.up*.95f;
                Invoke(combat,"LateUpdate");start=(Vector2)Field(typeof(AncientCombat),"beamStart").GetValue(combat);end=(Vector2)Field(typeof(AncientCombat),"beamEnd").GetValue(combat);
                Assert((start-movedOrigin).sqrMagnitude<.00001f&&Mathf.Abs(end.y-start.y)<.001f,"The emitter follows movement while the release direction stays fixed with the real camera restored");
                Assert(Mathf.Abs(late.HealthRatio-.89f)<.0001f&&hero.CaptureEnergy==30,"A late entering actor takes the original 50-energy damage without spending newly collected energy");
                Invoke(combat,"LateUpdate");Invoke(combat,"LateUpdate");
                Assert(Mathf.Abs(late.HealthRatio-.89f)<.0001f,"Repeated visible-beam updates cannot damage the same surviving actor twice");
                wall.transform.position=movedOrigin+Vector2.right*4;Physics2D.SyncTransforms();Invoke(combat,"LateUpdate");end=(Vector2)Field(typeof(AncientCombat),"beamEnd").GetValue(combat);
                Assert(Mathf.Abs(end.x-solid.bounds.min.x)<.03f,"A moving obstruction clips the already active beam on the next render update");
                // Dead actors are removed from the world list. A new Unity object is a fresh ledger identity.
                Field(typeof(AncientEnemy),"dead").SetValue(late,true);game.enemies.Remove(late);
                replacement=LaserProbeEnemy(game,movedOrigin+Vector2.right*2);Invoke(combat,"LateUpdate");
                Assert(Mathf.Abs(replacement.HealthRatio-.89f)<.0001f,"Removing a dead actor does not suppress a new object's hit within the same pulse");
                Field(typeof(AncientHero),"<AttackMode>k__BackingField").SetValue(hero,false);TickCombat(combat);
                Assert(!combat.Firing&&!((LineRenderer)Field(typeof(AncientCombat),"beam").GetValue(combat)).enabled,"Switching to wire mode immediately cancels the player pulse");
                SetAttackMode(hero);hero.SpendCapturedEnergy();hero.AddCapturedEnergy(50);FireRight(game);
                Assert(Mathf.Abs(replacement.HealthRatio-.78f)<.0001f,"A surviving target can be hit again by the next pulse");
                game.skills=true;Invoke(combat,"LateUpdate");Assert(combat.Firing,"An ordinary pause menu preserves the current pulse");game.skills=false;
                var focus=typeof(AncientCombat).GetMethod("OnApplicationFocus",PrivateInstance);focus.Invoke(combat,new object[]{false});
                Assert(!combat.Firing,"Losing application focus cancels the live player pulse");
                hero.AddCapturedEnergy(50);FireRight(game);Field(typeof(AncientHero),"invulnerableUntil").SetValue(hero,0f);hero.Hurt(1);TickCombat(combat);
                Assert(!combat.Firing,"Real hurt recovery cancels the pulse before its next damage or rendering update");
                Field(typeof(AncientHero),"hurtUntil").SetValue(hero,0f);
                wall.transform.position=movedOrigin;Physics2D.SyncTransforms();hero.SpendCapturedEnergy();hero.AddCapturedEnergy(50);int shots=combat.LaserShots;FireRight(game);
                Assert(!combat.Firing&&combat.LaserShots==shots&&hero.CaptureEnergy==50,"An obstructed muzzle neither spends energy nor starts a pulse");
            }
            finally
            {
                game.skills=false;combat.ResetCombat();if(late){game.enemies.Remove(late);UnityEngine.Object.Destroy(late.gameObject);}if(replacement){game.enemies.Remove(replacement);UnityEngine.Object.Destroy(replacement.gameObject);}
                wall.SetActive(false);UnityEngine.Object.Destroy(wall);MoveProbeHero(hero,original);SetAttackMode(hero);
            }
        }
        static void CheckCaptureEnergy(AncientGame game)
        {
            var hero=game.hero;var combat=hero.Combat;
            combat.ResetCombat();hero.particles=0;Field(typeof(AncientHero),"absorbAt").SetValue(hero,Time.time-.05f);
            var perfect=game.Projectiles.Spawn((Vector2)hero.transform.position+Vector2.right*5,Vector2.zero,20,false,1);hero.AbsorbShot(perfect);
            Assert(perfect.Available&&Mathf.Abs(MoteEnergy(combat)-30)<.001f&&Mathf.Abs(hero.particles-18)<.001f,"Perfect F decomposition produces 50 percent more actual blue energy and the matching legacy-resource bonus");
            combat.ResetCombat();hero.particles=0;Field(typeof(AncientHero),"absorbAt").SetValue(hero,Time.time-1);
            var ordinary=game.Projectiles.Spawn((Vector2)hero.transform.position+Vector2.right*5,Vector2.zero,20,false,1);hero.AbsorbShot(ordinary);
            Assert(ordinary.Available&&Mathf.Abs(MoteEnergy(combat)-20)<.001f&&Mathf.Abs(hero.particles-12)<.001f,"An ordinary F decomposition retains its original collectible energy and resource amount");combat.ResetCombat();
        }
        static void CheckTrainingObjectReuse(AncientGame game)
        {
            var hero=game.hero;var combat=hero.Combat;Vector2 original=hero.transform.position;
            float health=hero.hp,energy=hero.CaptureEnergy;
            FieldInfo immunity=Field(typeof(AncientHero),"invulnerableUntil"),hurt=Field(typeof(AncientHero),"hurtUntil");
            object savedImmunity=immunity.GetValue(hero),savedHurt=hurt.GetValue(hero);AncientProjectile practice=null;
            try
            {
                // Use the real pooled projectile contact path in an empty part of the scene.
                // A missed lesson must not consume health or reward a contact as if it were a capture.
                combat.ResetCombat();MoveProbeHero(hero,new Vector2(2,80));immunity.SetValue(hero,0f);hurt.SetValue(hero,0f);
                practice=game.Projectiles.Spawn(hero.transform.position,Vector2.zero,17,false,1);
                Assert(practice&&!practice.Available,"A training-contact probe uses an actual pooled physical object");
                practice.TrainingObject=true;Field(typeof(AncientProjectile),"age").SetValue(practice,1f);Invoke(practice,"Update");
                Assert(practice.Available&&!game.shots.Contains(practice)&&hero.hp==health&&hero.CaptureEnergy==energy,"Missing a practice object removes it without damage or free collected energy");
                // Replay the same pool slot, rather than creating a fresh component: immunity must not leak.
                practice.Play(hero.transform.position,Vector2.zero,17,false,1,0);
                Assert(!practice.TrainingObject&&!practice.Available,"Reusing the practice pool slot resets its training-only protection");
                Field(typeof(AncientProjectile),"age").SetValue(practice,1f);Invoke(practice,"Update");
                Assert(practice.Available&&Mathf.Abs(hero.hp-(health-17))<.001f,"The reused enemy object still damages the officer through its ordinary contact path");
            }
            finally
            {
                if(practice)practice.Remove();combat.ResetCombat();hero.hp=health;
                Field(typeof(AncientHero),"<CaptureEnergy>k__BackingField").SetValue(hero,energy);
                immunity.SetValue(hero,savedImmunity);hurt.SetValue(hero,savedHurt);MoveProbeHero(hero,original);
            }
        }
        static void CheckPulseCallbacks(AncientGame game)
        {
            var transport=new GameObject("QA pulse cargo truck").AddComponent<AncientTruck>();AncientFinalTruck finalTransport=null;AncientSniperEncounter encounter=null;
            try
            {
                transport.Initialize(game,8);transport.enabled=false;transport.transform.position=new Vector2(8,80);
                Vector2 start=new Vector2(2,81.4f),end=new Vector2(20,81.4f);var health=Field(typeof(AncientTruck),"health");
                transport.HitByLaser(start,end,20,.5f,71);transport.HitByLaser(start,end,20,.5f,71);
                Assert(Mathf.Abs((float)health.GetValue(transport)-160)<.001f,"A real truck receives one damage event for repeated callbacks from one pulse");
                transport.HitByLaser(start,end,20,.5f,72);Assert(Mathf.Abs((float)health.GetValue(transport)-140)<.001f,"The next pulse can damage the truck again");
                transport.HitByLaser(start+Vector2.up*10,end+Vector2.up*10,20,.5f,73);transport.HitByLaser(start,end,20,.5f,73);
                Assert(Mathf.Abs((float)health.GetValue(transport)-120)<.001f,"A missed callback does not reserve the truck's pulse identity");
                transport.HitByLaser(start,end,20,.5f);transport.HitByLaser(start,end,20,.5f);
                Assert(Mathf.Abs((float)health.GetValue(transport)-80)<.001f,"Legacy direct truck calls without a pulse ID retain their original per-call behavior");
                finalTransport=new GameObject("QA final transport collider").AddComponent<AncientFinalTruck>();finalTransport.Initialize(game,game.Finale,8);finalTransport.enabled=false;
                finalTransport.transform.position=new Vector2(8,80);Physics2D.SyncTransforms();var suppression=Field(typeof(AncientFinalTruck),"suppressedUntil");
                finalTransport.HitBeam(start,end,20,.5f,91);Assert((float)suppression.GetValue(finalTransport)>Time.time,"The final truck's real kinematic collider accepts a pulse through its visible body");
                suppression.SetValue(finalTransport,-1f);finalTransport.HitBeam(start,end,20,.5f,91);
                Assert((float)suppression.GetValue(finalTransport)==-1,"A duplicate final-truck callback cannot refresh suppression or repeat its effect");
                finalTransport.HitBeam(start,end,20,.5f,92);Assert((float)suppression.GetValue(finalTransport)>Time.time,"A new pulse can suppress the final truck again");
                finalTransport.gameObject.SetActive(false);Physics2D.SyncTransforms();
                encounter=new GameObject("QA pulse turret encounter").AddComponent<AncientSniperEncounter>();
                Field(typeof(AncientSniperEncounter),"game").SetValue(encounter,game);Field(typeof(AncientSniperEncounter),"phase").SetValue(encounter,4);
                var turretType=typeof(AncientSniperEncounter).GetNestedType("Turret",BindingFlags.NonPublic);var turrets=(Array)Field(typeof(AncientSniperEncounter),"turrets").GetValue(encounter);
                for(int i=0;i<2;i++)
                {
                    var turret=Activator.CreateInstance(turretType,true);turrets.SetValue(turret,i);turretType.GetField("root").SetValue(turret,new GameObject("QA turret root"));
                    turretType.GetField("position").SetValue(turret,new Vector2(i==0?8:100,80));turretType.GetField("hp").SetValue(turret,1000f);
                }
                var objects=(Array)Field(typeof(AncientSniperEncounter),"objects").GetValue(encounter);var objectType=typeof(AncientSniperEncounter).GetNestedType("ObjectShot",BindingFlags.NonPublic);
                for(int i=0;i<objects.Length;i++)objects.SetValue(Activator.CreateInstance(objectType,true),i);
                var hp=turretType.GetField("hp");encounter.OnLaser(start,end,.5f,81);encounter.OnLaser(start,end,.5f,81);
                Assert(Mathf.Abs((float)hp.GetValue(turrets.GetValue(0))-900)<.001f,"A physical turret callback applies its laser damage once per pulse");
                encounter.OnLaser(start,end,.5f,82);Assert(Mathf.Abs((float)hp.GetValue(turrets.GetValue(0))-800)<.001f,"A later pulse can damage the surviving turret again");
            }
            finally {UnityEngine.Object.Destroy(transport.gameObject);if(finalTransport)UnityEngine.Object.Destroy(finalTransport.gameObject);if(encounter){Field(typeof(AncientSniperEncounter),"game").SetValue(encounter,null);UnityEngine.Object.Destroy(encounter.gameObject);}game.hero.Combat.ResetCombat();}
        }
        static void CheckTutorialLaser(AncientGame game)
        {
            var story=game.Story;var hero=game.hero;var combat=hero.Combat;var original=(Vector2)hero.transform.position;
            Assert(!story.DialogueOpen&&!combat.Firing,"The tutorial handoff probe begins outside dialogue with an idle arm");
            var stage=Field(typeof(AncientStory),"<Stage>k__BackingField");var flags=Field(typeof(AncientStory),"flags");
            int savedStage=story.Stage,savedFlags=(int)flags.GetValue(story),savedInput=(int)Field(typeof(AncientGame),"inputReadyFrame").GetValue(game);float savedEnergy=hero.CaptureEnergy;
            string[] fields={"choiceOpen","choiceAction","lineIndex","revealedChars","pageStart","pageEnd","pageReady","pageWidth","pageHeight","nextLetterAt","showFull","visibleChars","shownText","bubbleNameLine","afterDialogue","dialogueFocusReady","nextTrainingShot","reportPending","coachedStep","nextTrainingHint","trainingObject"};
            var values=new object[fields.Length];for(int i=0;i<fields.Length;i++)values[i]=Field(typeof(AncientStory),fields[i]).GetValue(story);
            var dialogue=(System.Collections.IList)Field(typeof(AncientStory),"lines").GetValue(story);
            try
            {
                stage.SetValue(story,3);flags.SetValue(story,224);combat.ResetCombat();MoveProbeHero(hero,new Vector2(2,80));SetAttackMode(hero);
                combat.StrikeBlade();Assert(combat.BladeAnimating,"The handoff probe starts a real active blade cut");Invoke(story,"Update");
                Assert(story.Stage==3&&!story.DialogueOpen,"Completed training cannot interrupt an active blade cut");combat.CancelBlade();
                Field(typeof(AncientCombat),"heldAt").SetValue(combat,Time.time-.4f);Assert(combat.Charging,"The handoff probe enters the existing held laser charge state");Invoke(story,"Update");
                Assert(story.Stage==3&&!story.DialogueOpen,"Completed training waits for an active charge to resolve");combat.CancelInput();
                hero.SpendCapturedEnergy();hero.AddCapturedEnergy(50);FireRight(game);Assert(combat.Firing,"The completed tutorial has a real released laser pulse");int projectiles=game.shots.Count;
                Invoke(story,"Update");
                Assert(story.Stage==3&&!story.DialogueOpen&&combat.Firing,"Finishing weapon practice does not interrupt the visible pulse or begin the stock report");
                Assert(game.shots.Count==projectiles,"Completed objectives stop the training projectile feeder during the final pulse");
                // Expire the actual release deadline instead of cancelling the pulse or sleeping the Editor.
                var pulse=Field(typeof(AncientCombat),"laserPulse").GetValue(combat);Field(pulse.GetType(),"<Until>k__BackingField").SetValue(pulse,Time.time-.01f);
                Field(typeof(AncientCombat),"laserUntil").SetValue(combat,Time.time-.01f);Invoke(story,"Update");
                Assert(story.Stage==3&&!story.DialogueOpen&&!combat.Firing,"Expired practice effects leave the inventory objective active until the player reaches its terminal");
                MoveProbeHero(hero,new Vector2(AncientProductionLayout.StockX,1.06f));Assert(hero.IsOnGround,"The final practice handoff reaches the actual stock-room floor before interaction");story.TryProductionStock();
                Assert(story.Stage==4&&story.DialogueOpen,"An explicit stock-terminal interaction starts the inventory discrepancy report after practice");
            }
            finally
            {
                combat.ResetCombat();dialogue.Clear();stage.SetValue(story,savedStage);flags.SetValue(story,savedFlags);
                for(int i=0;i<fields.Length;i++)Field(typeof(AncientStory),fields[i]).SetValue(story,values[i]);
                Field(typeof(AncientGame),"inputReadyFrame").SetValue(game,savedInput);Field(typeof(AncientHero),"<CaptureEnergy>k__BackingField").SetValue(hero,savedEnergy);MoveProbeHero(hero,original);Invoke(story,"Persist");
            }
        }
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
                    Assert(!(bool)Field(typeof(AncientCombat),"bladeHitPending").GetValue(combat),"The already consumed enemy contact remains consumed during the physical-object recovery probe");
                    SessionState.SetInt(Root+"RecoveryBroken",combat.BrokenObjects);
                    body.linearVelocity=Vector2.zero;body.constraints=RigidbodyConstraints2D.FreezeAll;SetStage(11);return;
                }
                if(stage==11)
                {
                    if(StageAge<.12)return;
                    Assert(recoveryProbe&&!recoveryProbe.Available&&game.shots.Contains(recoveryProbe)&&combat.BrokenObjects==SessionState.GetInt(Root+"RecoveryBroken",-1),"The recovery plate remains intact through subsequent scene updates");
                    recoveryProbe.Remove();recoveryProbe=null;combat.ResetCombat();
                    CheckScheduledBladeHitches(game);CheckLaserPulse(game);CheckCaptureEnergy(game);CheckTrainingObjectReuse(game);CheckPulseCallbacks(game);CheckTutorialLaser(game);
                    // Conversation is last because it deliberately acquires the gameplay input guard.
                    MoveProbeHero(hero,new Vector2(2,80));hero.SpendCapturedEnergy();hero.AddCapturedEnergy(50);FireRight(game);Assert(combat.Firing,"The dialogue cancellation probe starts with a live pulse");
                    game.Story.Conversation(null,0,new[]{"QA 통신관"},new[]{"레이저 중단 확인."});Assert(game.Story.DialogueOpen&&!combat.Firing,"Opening a real conversation cancels the arm's active pulse");
                    Finish(true,"Structural blade, active moving laser, once-per-pulse actor/truck/turret damage, interruption and actual blue-energy checks completed. Physical hold/release, wheel switching, airborne wire transitions, frame pacing and visual alignment still need manual playtesting.");
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
