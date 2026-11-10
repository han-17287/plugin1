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
        static void FinishDialogue(AncientGame game,int maximum)
        {
            var story=game.Story;var lines=(System.Collections.IList)Field(typeof(AncientStory),"lines").GetValue(story);
            for(int i=0;i<maximum&&story.DialogueOpen;i++)
            {
                int index=(int)Field(typeof(AncientStory),"lineIndex").GetValue(story);
                Assert(index>=0&&index<lines.Count,"The directed conversation has a valid current spoken line");
                var line=lines[index];string words=(string)line.GetType().GetField("text").GetValue(line);
                Field(typeof(AncientStory),"pageReady").SetValue(story,true);Field(typeof(AncientStory),"pageStart").SetValue(story,0);
                Field(typeof(AncientStory),"pageEnd").SetValue(story,words.Length);Field(typeof(AncientStory),"revealedChars").SetValue(story,words.Length);Field(typeof(AncientStory),"showFull").SetValue(story,true);
                Field(typeof(AncientGame),"inputReadyFrame").SetValue(game,Time.frameCount-1);Call(story,"Advance");
            }
            Assert(!story.DialogueOpen,"The actual dialogue-advance callback releases the completed conversation");
        }
        static void CheckOptionalWorkers(AncientGame game)
        {
            Assert(game.ProductionResidents,"The opening scene creates its optional production workers");
            Physics2D.SyncTransforms();
            string[] names={"교대 작업자","압연기 조작원","재고 관리원"};float[] positions={-211,-145,66};
            var sprites=new System.Collections.Generic.HashSet<Sprite>();
            for(int i=0;i<names.Length;i++)
            {
                Transform actor;float head;
                Assert(game.ProductionResidents.TryDialogueActor(names[i],out actor,out head)&&actor,"Each anonymous worker has a real in-world speech anchor: "+names[i]);
                Assert(Mathf.Abs(actor.position.x-positions[i])<.001f&&Mathf.Abs(actor.position.y-AncientProductionLayout.GroundHeight(positions[i]))<.001f,"The worker's feet meet the actual road or press foundation");
                var art=actor.GetComponentInChildren<SpriteRenderer>();
                Assert(art&&art.sprite==AncientArt.ProductionWorker(i)&&art.sprite.texture.filterMode==FilterMode.Point,"The worker role selects its own original pixel sprite: "+names[i]);sprites.Add(art.sprite);
                var floor=Physics2D.Raycast((Vector2)actor.position+Vector2.up*.4f,Vector2.down,.6f,1<<8);
                Assert(floor.collider&&Mathf.Abs(art.bounds.min.y-floor.point.y)<.02f&&Mathf.Abs(head-(art.bounds.max.y-actor.position.y))<.01f,"The displayed worker's feet meet the physical floor and speech height follows the actual sprite bounds");
                Assert(actor.GetComponentsInChildren<Collider2D>().Length==0,"The worker and their work props cannot obstruct a fast traversal route");
            }
            Assert(sprites.Count==3,"The shift worker, press technician and stock clerk use three distinct original worker sprites");
            Transform unrelated;float unrelatedHead;Assert(!game.ProductionResidents.TryDialogueActor("생산소 작업반장",out unrelated,out unrelatedHead),"Anonymous production workers do not replace the named production foreman's identity");
            Vector2 original=game.hero.transform.position;int stage=game.Story.Stage,cores=game.Cores,flags=(int)Field(typeof(AncientStory),"flags").GetValue(game.Story),credits=game.credits,points=game.Story.ResidencePoints;
            int input=(int)Field(typeof(AncientGame),"inputReadyFrame").GetValue(game);
            try
            {
                for(int i=0;i<names.Length;i++)
                {
                    MoveHero(game.hero,new Vector2(positions[i],AncientProductionLayout.GroundHeight(positions[i])+1.06f));Field(typeof(AncientGame),"inputReadyFrame").SetValue(game,Time.frameCount-1);
                    Assert(game.ProductionResidents.TryInteract()&&game.Story.DialogueOpen,"An actual optional worker interaction opens the Korean in-game conversation: "+names[i]);
                    var lines=(System.Collections.IList)Field(typeof(AncientStory),"lines").GetValue(game.Story);var first=lines[0];
                    Assert((string)first.GetType().GetField("speaker").GetValue(first)==names[i],"The conversation keeps the selected worker's own role and speaker identity");FinishDialogue(game,6);
                }
                Assert(game.Story.Stage==stage&&game.Cores==cores&&(int)Field(typeof(AncientStory),"flags").GetValue(game.Story)==flags&&game.credits==credits&&game.Story.ResidencePoints==points&&!game.Production.Active,"The three optional workplace conversations cannot advance inventory, tutorial flags, rewards or the mandatory pursuit");
            }
            finally {MoveHero(game.hero,original);Field(typeof(AncientGame),"inputReadyFrame").SetValue(game,input);}
        }
        static void CheckTrainingFeeder(AncientGame game)
        {
            var story=game.Story;var hero=game.hero;var combat=hero.Combat;Vector2 original=hero.transform.position;
            string[] fields={"flags","coachedStep","nextTrainingHint","nextTrainingShot","trainingObject","inspectionReadyPrompted"};
            var saved=new object[fields.Length];for(int i=0;i<fields.Length;i++)saved[i]=Field(typeof(AncientStory),fields[i]).GetValue(story);
            float health=hero.hp,energy=hero.CaptureEnergy;AncientProjectile practice=null;
            try
            {
                combat.ResetCombat();MoveHero(hero,new Vector2(-39,1.3f));
                Field(typeof(AncientStory),"flags").SetValue(story,0);Field(typeof(AncientStory),"coachedStep").SetValue(story,0);
                Field(typeof(AncientStory),"nextTrainingHint").SetValue(story,Time.time+18);Field(typeof(AncientStory),"nextTrainingShot").SetValue(story,Time.time-1);
                Call(story,"Update");practice=(AncientProjectile)Field(typeof(AncientStory),"trainingObject").GetValue(story);
                Assert(practice&&!practice.Available&&practice.TrainingObject&&game.shots.Contains(practice),"The actual weapon lesson supplies a harmless pooled housing from a visible training emitter");
                Assert(Mathf.Abs(practice.transform.position.x+41.65f)<.02f&&Mathf.Abs(practice.transform.position.y-AncientProductionLayout.TrainingEmitterY)<.001f,"The lesson uses the nearby authored emitter instead of spawning a threat beside the moving officer");
                practice.transform.position=hero.transform.position;Field(typeof(AncientProjectile),"age").SetValue(practice,1f);Call(practice,"Update");
                Assert(practice.Available&&hero.hp==health&&hero.CaptureEnergy==energy,"A real missed tutorial housing disappears on contact without health loss or an automatic capture reward");
                Field(typeof(AncientStory),"nextTrainingShot").SetValue(story,Time.time-1);
                Field(typeof(AncientCombat),"heldAt").SetValue(combat,Time.time-.3f);Assert(combat.Charging,"The supply-pause probe starts an actual held laser charge");Call(story,"Update");
                Assert(game.shots.Count==0,"A live laser-aiming hold cannot receive another training object");combat.CancelInput();
                MoveHero(hero,new Vector2(-70,1.3f));Call(story,"Update");
                Assert(game.shots.Count==0,"Leaving the authored emitter range cannot create a housing at the player's new location");
                MoveHero(hero,new Vector2(-39,1.3f));Field(typeof(AncientStory),"flags").SetValue(story,1<<5);Call(story,"Update");
                Assert(game.shots.Count==0&&(float)Field(typeof(AncientStory),"nextTrainingShot").GetValue(story)-Time.time>=5.9f,"After a successful first break, the collection lesson gets a quiet window before another housing is offered");
            }
            finally
            {
                if(practice)practice.Remove();combat.ResetCombat();hero.hp=health;
                Field(typeof(AncientHero),"<CaptureEnergy>k__BackingField").SetValue(hero,energy);
                for(int i=0;i<fields.Length;i++)Field(typeof(AncientStory),fields[i]).SetValue(story,saved[i]);
                story.ClearProductionRadio();MoveHero(hero,original);Call(story,"Persist");
            }
        }
        static void CheckStockApproach(AncientGame game)
        {
            var hero=game.hero;var story=game.Story;int stage=story.Stage,flags=(int)Field(typeof(AncientStory),"flags").GetValue(story);
            int cores=game.Cores,points=story.ResidencePoints,credits=game.credits,score=game.score;
            hero.TravelTo(new Vector2(AncientProductionLayout.StockX,2.5f));Physics2D.SyncTransforms();Field(typeof(AncientHero),"groundSampleTick").SetValue(hero,-1f);
            Assert(!hero.IsOnGround&&story.TryProductionStock()&&!story.DialogueOpen&&story.Stage==stage,"An actual airborne stock-terminal request is handled without starting the report or breach");
            hero.TravelTo(new Vector2(AncientProductionLayout.StockX,1.06f));hero.SetProductionAttackMode(false);Physics2D.SyncTransforms();Field(typeof(AncientHero),"groundSampleTick").SetValue(hero,-1f);
            Assert(hero.IsOnGround,"The wire-safety probe starts with the actual capsule resting at the stock-room floor");
            Call(hero,"AttachWire",new Vector2(AncientProductionLayout.StockX+1.5f,10),null,Vector2.down);
            Assert(hero.Wired&&hero.IsOnGround&&story.TryProductionStock()&&!story.DialogueOpen&&story.Stage==stage,"A live ceiling wire blocks the stock report even during its still-grounded launch frame");
            Assert((int)Field(typeof(AncientStory),"flags").GetValue(story)==flags&&game.Cores==cores&&story.ResidencePoints==points&&game.credits==credits&&game.score==score&&!game.Production.Active,"Rejected stock requests change no tutorial flags, rewards, inventory or pursuit phase");
            hero.TravelTo(new Vector2(AncientProductionLayout.StockX,1.06f));Physics2D.SyncTransforms();Field(typeof(AncientHero),"groundSampleTick").SetValue(hero,-1f);
            Assert(!hero.Wired&&hero.IsOnGround,"Releasing the wire and reaching the real floor leaves the stock terminal ready for a grounded request");
        }
        static bool InCamera(Camera camera,Vector3 point)
        {Vector3 view=camera.WorldToViewportPoint(point);return view.z>0&&view.x>.001f&&view.x<.999f&&view.y>.001f&&view.y<.999f;}
        static bool InCamera(Camera camera,Bounds bounds)
        {
            return InCamera(camera,new Vector3(bounds.min.x,bounds.min.y,bounds.center.z))&&InCamera(camera,new Vector3(bounds.min.x,bounds.max.y,bounds.center.z))&&
                InCamera(camera,new Vector3(bounds.max.x,bounds.min.y,bounds.center.z))&&InCamera(camera,new Vector3(bounds.max.x,bounds.max.y,bounds.center.z));
        }
        static Bounds TruckBounds(AncientProductionPursuit director)
        {
            Bounds bounds=((BoxCollider2D)Field(typeof(AncientProductionPursuit),"deck").GetValue(director)).bounds;
            bounds.Encapsulate(((SpriteRenderer)Field(typeof(AncientProductionPursuit),"truckArt").GetValue(director)).bounds);bounds.Encapsulate(((SpriteRenderer)Field(typeof(AncientEnemy),"art").GetValue(director.Commander)).bounds);return bounds;
        }
        static void CheckTruckArtwork(AncientGame game)
        {
            var director=game.Production;var art=(SpriteRenderer)Field(typeof(AncientProductionPursuit),"truckArt").GetValue(director);var deck=(BoxCollider2D)Field(typeof(AncientProductionPursuit),"deck").GetValue(director);Physics2D.SyncTransforms();
            Assert(art.sprite==AncientArt.ProductionTruck()&&art.sprite!=AncientArt.Truck()&&art.sprite.name=="production-pursuit-empty-truck","The actual pursuit vehicle uses the original empty chassis rather than the legacy truck with baked cargo");
            // The authored native-grid rectangle has three clear pixels on every side.
            float padding=3/art.sprite.pixelsPerUnit;Bounds visible=art.bounds;
            Assert(Mathf.Abs(visible.min.y+padding-AncientProductionLayout.RoadY)<.002f&&Mathf.Abs(visible.max.y-padding-deck.bounds.max.y)<.002f,"The displayed opaque wheels rest on the road and the opaque cargo roof meets the real walkable roof");
            var roof=(SpriteRenderer)Field(typeof(AncientProductionPursuit),"truckRoofArt").GetValue(director);
            Assert(Mathf.Abs(roof.bounds.max.y-deck.bounds.max.y)<.002f,"The walkable roof plate's visible top matches the collider instead of covering the officer's boots");
            Assert(visible.min.x+padding<deck.bounds.min.x&&visible.max.x-padding>deck.bounds.max.x&&visible.size.x-deck.bounds.size.x<.4f,"The original chassis covers the whole physical deck with only its small authored bumper overhang");
            CheckCargoArtwork(director,false);
        }
        static void CheckCargoArtwork(AncientProductionPursuit director,bool detached)
        {
            var cargo=(Transform)Field(typeof(AncientProductionPursuit),"cargo").GetValue(director);var cores=(SpriteRenderer[])Field(typeof(AncientProductionPursuit),"coreArt").GetValue(director);
            var distinct=new System.Collections.Generic.HashSet<SpriteRenderer>();bool shown=true;
            foreach(var renderer in cores)
            {
                shown&=renderer&&renderer.enabled&&renderer.gameObject.activeInHierarchy&&renderer.transform.parent==cargo&&renderer.color==AncientGame.Cyan&&renderer.bounds.size.x>.4f&&renderer.bounds.size.y>.8f;
                if(renderer)distinct.Add(renderer);
            }
            Assert(cores.Length==3&&distinct.Count==3&&shown&&(!detached||cargo.parent==null),"The "+(detached?"detached":"mounted")+" cargo contains exactly three separate visible cyan energy cores"+(detached?" under an independent world root":""));
        }
        static void CheckDeckCamera(AncientGame game)
        {
            var director=game.Production;var hero=game.hero;var body=hero.GetComponent<Rigidbody2D>();var view=game.View;
            var truck=(Rigidbody2D)Field(typeof(AncientProductionPursuit),"truck").GetValue(director);Vector2 position=body.position,velocity=body.linearVelocity;
            float aspect=view.aspect,zoom=view.orthographicSize;Vector3 camera=view.transform.position,follow=(Vector3)Field(typeof(AncientGame),"cameraFollow").GetValue(game);
            try
            {
                foreach(float ratio in new[]{4/3f,16/10f,16/9f,21/9f})foreach(float offset in new[]{-36f,-18f,18f,36f})
                {
                    MoveHero(hero,new Vector2(truck.position.x+offset,8.7f));view.aspect=ratio;game.SnapCameraToHero();
                    Bounds actor=hero.GetComponent<CapsuleCollider2D>().bounds;var art=(SpriteRenderer)Field(typeof(AncientHero),"art").GetValue(hero);actor.Encapsulate(art.bounds);
                    Assert(InCamera(view,actor)&&InCamera(view,TruckBounds(director)),"The actual deck camera retains the ceiling-height officer and complete truck at offset "+offset+" / aspect "+ratio);
                }
            }
            finally
            {
                MoveHero(hero,position);body.linearVelocity=velocity;view.aspect=aspect;view.orthographicSize=zoom;view.transform.position=camera;Field(typeof(AncientGame),"cameraFollow").SetValue(game,follow);
            }
        }
        static void CheckRoadCamera(AncientGame game,string context)
        {
            var director=game.Production;var view=game.View;var hero=game.hero;
            float aspect=view.aspect,zoom=view.orthographicSize;Vector3 position=view.transform.position,follow=(Vector3)Field(typeof(AncientGame),"cameraFollow").GetValue(game);
            int cursor=(int)Field(typeof(AncientProductionPursuit),"hazardCursor").GetValue(director),volley=(int)Field(typeof(AncientProductionPursuit),"volley").GetValue(director);
            bool road=director.Sequence.Phase==AncientProductionPhase.Chase;
            try
            {
                Assert(LiveHazards(director)==0,"The "+context+" camera probe begins without altering an existing live road hazard");
                if(road)for(int i=0;i<3;i++)Call(director,"SpawnHazard");Physics2D.SyncTransforms();
                Bounds rider=hero.GetComponent<BoxCollider2D>().bounds;
                foreach(var art in hero.GetComponentsInChildren<SpriteRenderer>())if(art.enabled&&art.gameObject.activeInHierarchy)rider.Encapsulate(art.bounds);
                Bounds vehicle=TruckBounds(director);
                foreach(float ratio in new[]{4/3f,16/10f,16/9f,21/9f})
                {
                    view.aspect=ratio;game.SnapCameraToHero();Vector2 focus;float targetZoom;
                    Assert(director.TryCamera(out focus,out targetZoom)&&((Vector2)view.transform.position-focus).sqrMagnitude<.00001f&&Near(view.orthographicSize,targetZoom),"The "+context+" snap applies the actual production focus and zoom immediately at aspect "+ratio);
                    Assert(InCamera(view,rider)&&InCamera(view,vehicle),"The actual camera projection contains the complete rider and transport truck at "+context+" / aspect "+ratio);
                    bool hazardsVisible=true;var pool=(Array)Field(typeof(AncientProductionPursuit),"hazards").GetValue(director);
                    foreach(var hazard in pool)if(hazard!=null&&(bool)hazard.GetType().GetField("live").GetValue(hazard))
                    {
                        var art=(SpriteRenderer)hazard.GetType().GetField("art").GetValue(hazard);var footprint=(LineRenderer)hazard.GetType().GetField("footprint").GetValue(hazard);
                        hazardsVisible&=InCamera(view,art.bounds);for(int i=0;i<footprint.positionCount;i++)hazardsVisible&=InCamera(view,footprint.GetPosition(i));
                    }
                    if(road)Assert(hazardsVisible,"The actual forward debris, bomb and full danger footprints remain inside the "+context+" camera at aspect "+ratio);
                }
            }
            finally
            {
                if(road)Call(director,"ClearHazards");Field(typeof(AncientProductionPursuit),"hazardCursor").SetValue(director,cursor);Field(typeof(AncientProductionPursuit),"volley").SetValue(director,volley);
                view.aspect=aspect;view.orthographicSize=zoom;view.transform.position=position;Field(typeof(AncientGame),"cameraFollow").SetValue(game,follow);
            }
        }
        static void CheckAutomaticBike(AncientGame game)
        {
            var hero=game.hero;var body=hero.GetComponent<Rigidbody2D>();var director=game.Production;bool enabled=hero.enabled;
            try
            {
                hero.enabled=false;MoveHero(hero,new Vector2(AncientProductionLayout.ChaseStart+6,.84f));
                Field(typeof(AncientHero),"axis").SetValue(hero,-1f);body.linearVelocity=Vector2.zero;Call(hero,"FixedUpdate");
                Assert(hero.bike&&body.simulated&&hero.GetComponent<BoxCollider2D>().enabled&&!hero.GetComponent<CapsuleCollider2D>().enabled,"The initial automatic mount uses the real motorcycle body and collider");
                Assert(!hero.AttackMode&&hero.ZipMode,"The actual automatic mount starts in straight-wire mode without retaining a prior attack or swing mode");
                Assert(Near(body.linearVelocity.x,director.DriveSpeed)&&body.linearVelocity.x>0,"Bike physics advances without a held forward key and overrides a stale reverse axis");
                Field(typeof(AncientHero),"jumpBufferedUntil").SetValue(hero,Time.time+AncientMovementRules.BufferSeconds);
                Field(typeof(AncientHero),"jumpHeld").SetValue(hero,true);Field(typeof(AncientHero),"cutJump").SetValue(hero,false);Call(hero,"FixedUpdate");
                Assert(Near(body.linearVelocity.x,director.DriveSpeed)&&Near(body.linearVelocity.y,14),"The actual buffered Space jump clears the road without cancelling automatic forward movement");
                MoveHero(hero,new Vector2(AncientProductionLayout.ChaseStart+6,.84f));body.linearVelocity=new Vector2(director.DriveSpeed,0);
            }
            finally {hero.enabled=enabled;}
        }
        static void CheckRoadFeedback(AncientGame game)
        {
            var director=game.Production;var hero=game.hero;var body=hero.GetComponent<Rigidbody2D>();
            var type=typeof(AncientProductionPursuit);var pool=(Array)Field(type,"hazards").GetValue(director);
            Vector2 position=body.position,velocity=body.linearVelocity,previous=(Vector2)Field(type,"roadHeroPrevious").GetValue(director);
            float health=hero.bikeHP,invulnerable=(float)Field(typeof(AncientHero),"invulnerableUntil").GetValue(hero);
            string[] names={"hazardCursor","volley","roadDodgeChain","roadDodgeCues","roadCoachBomb","roadCoachUntil","roadTitle"};
            var saved=new object[names.Length];for(int i=0;i<names.Length;i++)saved[i]=Field(type,names[i]).GetValue(director);
            try
            {
                Assert(LiveHazards(director)==0,"Road feedback begins without replacing an active hazard");
                Field(type,"roadDodgeChain").SetValue(director,0);Field(type,"roadDodgeCues").SetValue(director,0);Field(type,"roadCoachUntil").SetValue(director,0f);
                float x=position.x+6;
                for(int probe=0;probe<4;probe++)
                {
                    Field(type,"hazardCursor").SetValue(director,0);Field(type,"volley").SetValue(director,0);Call(director,"SpawnHazard");
                    var hazard=pool.GetValue(0);var root=(GameObject)hazard.GetType().GetField("root").GetValue(hazard);root.transform.position=new Vector2(x,AncientProductionHazardRules.BombFloorHeight);
                    if(probe==1)
                    {
                        Call(director,"ClearHazards");
                        Assert((int)Field(type,"roadDodgeChain").GetValue(director)==1,"Clearing a road pool cannot fabricate a successful jump");continue;
                    }
                    float y=probe==0?3:.84f;MoveHero(hero,new Vector2(x+3,y));Field(type,"roadHeroPrevious").SetValue(director,new Vector2(x-3,y));
                    Field(typeof(AncientHero),"invulnerableUntil").SetValue(hero,probe==3?Time.time-1:Time.time+5);
                    Call(director,"TickHazards",.1f);
                    if(probe==0)
                    {
                        Assert((int)Field(type,"roadDodgeChain").GetValue(director)==1&&LiveHazards(director)==0,"An actual swept high clearance consumes the live debris and records one jump");
                        Call(director,"TickHazards",.1f);
                        Assert((int)Field(type,"roadDodgeChain").GetValue(director)==1,"The same consumed road obstacle cannot count twice");
                    }
                    else if(probe==2)
                        Assert((int)Field(type,"roadDodgeChain").GetValue(director)==0&&Near(hero.bikeHP,health)&&(float)Field(type,"roadCoachUntil").GetValue(director)==0,"A physical collision breaks the clearance streak during invulnerability without issuing damage coaching");
                    else
                        Assert(Near(hero.bikeHP,health-14)&&(float)Field(type,"roadCoachUntil").GetValue(director)>Time.time,"A real debris hit retains its fourteen damage and opens the short contextual jump reminder");
                }
            }
            finally
            {
                Call(director,"ClearHazards");for(int i=0;i<names.Length;i++)Field(type,names[i]).SetValue(director,saved[i]);
                hero.bikeHP=health;Field(typeof(AncientHero),"invulnerableUntil").SetValue(hero,invulnerable);MoveHero(hero,position);body.linearVelocity=velocity;Field(type,"roadHeroPrevious").SetValue(director,previous);
            }
        }
        static void CheckMissedBoarding(AncientGame game)
        {
            var director=game.Production;var hero=game.hero;var sequence=director.Sequence;
            var truck=(Rigidbody2D)Field(typeof(AncientProductionPursuit),"truck").GetValue(director);
            float health=director.Commander.HealthRatio;int round=sequence.Round,mounts=sequence.MountCount;
            MoveHero(hero,new Vector2(truck.position.x-8,.84f));
            for(int i=0;i<3;i++)Step(director);
            Field(typeof(AncientHero),"wireReady").SetValue(hero,Time.time-1);Field(typeof(AncientGame),"inputReadyFrame").SetValue(game,Time.frameCount-1);
            Assert(hero.TryProductionBoard(director.BoardingHook),"The recovery probe starts with an accepted real wire-boarding action");
            // Deliberately leave physics unadvanced: this wire never reaches the roof.
            for(int i=0;i<18&&sequence.Phase==AncientProductionPhase.Boarding;i++)Step(director);
            Assert(sequence.Phase==AncientProductionPhase.Approach&&hero.bike&&!hero.Wired&&!sequence.AttackersEnabled,"An uncompleted real wire automatically returns to a safe motorcycle ceasefire");
            Assert(sequence.Round==round&&sequence.MountCount==mounts&&Near(director.Commander.HealthRatio,health)&&(int)Field(typeof(AncientProductionPursuit),"boardingFailures").GetValue(director)==1,"A missed boarding records guidance without resetting captain health, round or mount count");
            for(int i=0;i<3;i++)Step(director);Call(director,"LateUpdate");
            var latch=(LineRenderer)Field(typeof(AncientProductionPursuit),"hook").GetValue(director);
            bool precise=latch.enabled;for(int i=0;i<latch.positionCount;i++)precise&=((Vector2)latch.GetPosition(i)-director.BoardingHook).magnitude<.5f;
            Assert(precise&&director.CanBoardWire(director.BoardingHook),"The ordinary mast deployment reopens boarding with its blinking cue entirely inside the actual half-metre latch target");
        }
        static void BoardStationaryRoof(AncientGame game)
        {
            var director=game.Production;var hero=game.hero;var body=hero.GetComponent<Rigidbody2D>();
            var truck=(Rigidbody2D)Field(typeof(AncientProductionPursuit),"truck").GetValue(director);
            Vector2 original=truck.position,velocity=truck.linearVelocity;float truckX=(float)Field(typeof(AncientProductionPursuit),"truckX").GetValue(director),speed=(float)Field(typeof(AncientProductionPursuit),"kinematicSpeed").GetValue(director);
            float health=director.Commander.HealthRatio;
            try
            {
                Vector2 stop=new Vector2(AncientProductionLayout.ChaseEnd-24,0);Call(director,"PlaceTruck",stop);
                Field(typeof(AncientProductionPursuit),"truckX").SetValue(director,stop.x);Field(typeof(AncientProductionPursuit),"kinematicSpeed").SetValue(director,0f);truck.linearVelocity=Vector2.zero;
                Call(director,"ReturnToBike",false);body.linearVelocity=Vector2.zero;Field(typeof(AncientHero),"axis").SetValue(hero,0f);
                for(int i=0;i<3;i++)Step(director,.25f);Call(director,"LateUpdate");
                Field(typeof(AncientHero),"wireReady").SetValue(hero,Time.time-1);Field(typeof(AncientGame),"inputReadyFrame").SetValue(game,Time.frameCount-1);
                Assert(hero.bike&&Near(director.DriveSpeed,0)&&Near(truck.position.x,stop.x),"The tunnel-end probe starts on a stopped motorcycle beside the actually clamped truck");
                Assert(hero.TryProductionBoard(director.BoardingHook),"The deployed stopped-truck latch accepts a real wire board after the mast's ceasefire wait");
                float dt=Time.fixedDeltaTime;int frames=0;bool released=false;
                for(;frames<150&&director.Sequence.Phase==AncientProductionPhase.Boarding;frames++)
                {
                    Call(director,"FixedUpdate");Call(hero,"FixedUpdate");Physics2D.SyncTransforms();Physics2D.Simulate(dt);Step(director,dt);
                    if(!hero.Wired)
                    {released=true;Assert(Mathf.Abs(body.linearVelocity.x)<.01f,"A released wire inherits zero stopped-truck speed instead of receiving a fallback road-driving velocity");}
                }
                Assert(released&&director.DeckCombat&&frames*dt<2&&Mathf.Abs(hero.transform.position.x-truck.position.x)<3.1f&&hero.IsOnGround,"Actual capsule physics boards the stationary tunnel-end roof without drifting off, timing out or teleporting");
                Assert(Near(truck.position.x,stop.x)&&Near(director.Commander.HealthRatio,health),"The stationary boarding probe does not move the clamped truck or grant boss damage");
            }
            finally
            {
                if(director.Sequence.AbortBoarding())Call(director,"ApplyPhase");
                Call(director,"PlaceTruck",original);Field(typeof(AncientProductionPursuit),"truckX").SetValue(director,truckX);Field(typeof(AncientProductionPursuit),"kinematicSpeed").SetValue(director,speed);truck.linearVelocity=velocity;
                Call(director,"ReturnToBike",false);Physics2D.SyncTransforms();
            }
        }
        static void BoardRoof(AncientGame game)
        {
            var director=game.Production;var hero=game.hero;var body=hero.GetComponent<Rigidbody2D>();
            bool enabled=hero.enabled,directorEnabled=director.enabled;var simulation=Physics2D.simulationMode;
            try
            {
                hero.enabled=director.enabled=false;Physics2D.simulationMode=SimulationMode2D.Script;
                float dt=Time.fixedDeltaTime;
                BoardStationaryRoof(game);
                // Every real boarding distance is probed. Physics2D.Simulate does not
                // advance Time.time here, so none can succeed via the 3.5s age fallback.
                for(int gap=8;gap<=14;gap++)
                {
                    if(director.DeckCombat)
                    {Assert(director.Sequence.AbortBoarding(),"A completed boarding probe can reopen its own ceasefire without awarding damage");Call(director,"ApplyPhase");}
                    Assert(director.Sequence.Phase==AncientProductionPhase.Approach,"The "+gap+"m probe begins in the actual safe boarding phase");
                    Call(director,"ReturnToBike",false);
                    var truck=(Rigidbody2D)Field(typeof(AncientProductionPursuit),"truck").GetValue(director);
                    MoveHero(hero,new Vector2(truck.position.x-gap,.84f));body.linearVelocity=new Vector2(director.DriveSpeed,0);
                    Field(typeof(AncientHero),"axis").SetValue(hero,0f);
                    for(int i=0;i<3;i++)Step(director,.25f);
                    Call(director,"LateUpdate");var mast=(SpriteRenderer)Field(typeof(AncientProductionPursuit),"boardingMast").GetValue(director);var cap=(SpriteRenderer)Field(typeof(AncientProductionPursuit),"boardingMastCap").GetValue(director);
                    Assert(mast.enabled&&cap.enabled&&((Vector2)cap.transform.position-director.BoardingHook).sqrMagnitude<.00001f&&mast.GetComponentsInChildren<Collider2D>().Length==0,"The deployed latch has a visible mast up to its real wire point without adding a collision blocker");
                    Field(typeof(AncientHero),"wireReady").SetValue(hero,Time.time-1);
                    Field(typeof(AncientGame),"inputReadyFrame").SetValue(game,Time.frameCount-1);
                    Vector2 hook=director.BoardingHook;
                    Assert(hero.TryProductionBoard(hook)&&director.Sequence.Phase==AncientProductionPhase.Boarding,"The real Hero action accepts the deployed moving latch at "+gap+"m");
                    Assert(hero.Wired&&!hero.bike,"The actual action dismounts the motorcycle and starts its physical straight wire");
                    Assert((Vector2)Field(typeof(AncientHero),"wireSurfaceNormal").GetValue(hero)==Vector2.down,"Boarding pulls below the visible latch with capsule clearance");
                    int frames=0;bool sawPending=false;
                    for(;frames<150&&director.Sequence.Phase==AncientProductionPhase.Boarding;frames++)
                    {
                        Call(director,"FixedUpdate");Call(hero,"FixedUpdate");
                        if((bool)Field(typeof(AncientHero),"wireFinishPending").GetValue(hero))
                        {sawPending=true;Assert(hero.Wired,"The final planned zip step keeps its wire until physics has actually moved the capsule");}
                        Physics2D.SyncTransforms();Physics2D.Simulate(dt);Step(director,dt);
                    }
                    Assert(sawPending&&director.DeckCombat&&!hero.bike&&hero.AttackMode&&!hero.Wired,"Actual capsule casts, moving-truck physics and completed-step release deliver the "+gap+"m probe to deck combat");
                    Assert(frames*dt<2&&!(bool)Field(typeof(AncientHero),"wireFinishPending").GetValue(hero),"The "+gap+"m boarding finishes before any age fallback without retaining a stale finish request");
                    Assert(Mathf.Abs(hero.transform.position.x-director.Commander.transform.position.x)<5.5f&&hero.transform.position.y>3.4f,"The moving roof remains under the physically boarded officer");
                }
            }
            finally {Physics2D.simulationMode=simulation;hero.enabled=enabled;director.enabled=directorEnabled;}
        }
        static void CheckMovingRoofJump(AncientGame game)
        {
            var hero=game.hero;var director=game.Production;var body=hero.GetComponent<Rigidbody2D>();
            var truck=(Rigidbody2D)Field(typeof(AncientProductionPursuit),"truck").GetValue(director);
            bool heroEnabled=hero.enabled,directorEnabled=director.enabled,running=hero.running;var simulation=Physics2D.simulationMode;
            try
            {
                hero.enabled=director.enabled=false;hero.running=false;Physics2D.simulationMode=SimulationMode2D.Script;
                float dt=Time.fixedDeltaTime;
                foreach(float axis in new[]{1f,-1f})
                {
                    hero.TravelTo(new Vector2(truck.position.x-1.5f,3.76f));Field(typeof(AncientHero),"axis").SetValue(hero,0f);body.linearVelocity=new Vector2(7,0);
                    for(int i=0;i<12;i++)
                    {Call(director,"FixedUpdate");Call(hero,"FixedUpdate");Physics2D.SyncTransforms();Physics2D.Simulate(dt);Step(director,dt);}
                    Assert(hero.StandingOn(truck)&&Mathf.Abs(body.linearVelocity.x-7)<.15f,"The actual capsule first settles on the seven-unit-per-second moving roof");
                    Field(typeof(AncientHero),"axis").SetValue(hero,axis);Field(typeof(AncientHero),"jumpBufferedUntil").SetValue(hero,Time.time+AncientMovementRules.BufferSeconds);Field(typeof(AncientHero),"jumpHeld").SetValue(hero,true);
                    for(int i=0;i<14;i++)
                    {Call(director,"FixedUpdate");Call(hero,"FixedUpdate");Physics2D.SyncTransforms();Physics2D.Simulate(dt);Step(director,dt);}
                    Assert(!hero.IsOnGround&&body.linearVelocity.y>0,"A real buffered roof jump remains airborne during its ascent");
                    Assert(Mathf.Abs(body.linearVelocity.x-(7+axis*5))<.15f,"Air control preserves the takeoff support velocity in the "+(axis>0?"forward":"backward")+" direction");
                }
            }
            finally
            {
                hero.running=running;hero.TravelTo(new Vector2(truck.position.x-1.5f,3.76f));body.linearVelocity=new Vector2(7,0);Field(typeof(AncientHero),"axis").SetValue(hero,0f);
                Physics2D.SyncTransforms();Physics2D.simulationMode=simulation;hero.enabled=heroEnabled;director.enabled=directorEnabled;
            }
        }
        static void CheckCaptainPresentation(AncientGame game)
        {
            var director=game.Production;var captain=director.Commander;var hero=game.hero;var body=hero.GetComponent<Rigidbody2D>();
            var art=(SpriteRenderer)Field(typeof(AncientEnemy),"art").GetValue(captain);var tell=(LineRenderer)Field(typeof(AncientProductionPursuit),"tell").GetValue(director);var zone=(LineRenderer)Field(typeof(AncientProductionPursuit),"strikeZone").GetValue(director);
            string[] fields={"captainPattern","captainLastAttack","captainWindup","captainAim","captainFacing","captainTimer","captainRecoverySeconds","captainActionUntil"};
            var saved=new object[fields.Length];for(int i=0;i<fields.Length;i++)saved[i]=Field(typeof(AncientProductionPursuit),fields[i]).GetValue(director);
            Vector2 position=body.position,velocity=body.linearVelocity;Sprite sprite=art.sprite;bool flipped=art.flipX,tellShown=tell.enabled,zoneShown=zone.enabled;
            Color tint=art.color;Vector3 scale=art.transform.localScale;Quaternion rotation=art.transform.localRotation;object facing=Field(typeof(AncientEnemy),"facing").GetValue(captain);
            var oldShots=new System.Collections.Generic.HashSet<AncientProjectile>(game.shots);var tellPoints=new Vector3[tell.positionCount];tell.GetPositions(tellPoints);var zonePoints=new Vector3[zone.positionCount];zone.GetPositions(zonePoints);
            try
            {
                MoveHero(hero,new Vector2(captain.transform.position.x+1,3.76f));
                Field(typeof(AncientProductionPursuit),"captainPattern").SetValue(director,0);Field(typeof(AncientProductionPursuit),"captainWindup").SetValue(director,true);Field(typeof(AncientProductionPursuit),"captainFacing").SetValue(director,1f);Field(typeof(AncientProductionPursuit),"captainTimer").SetValue(director,.5f);
                Field(typeof(AncientProductionPursuit),"captainAim").SetValue(director,(Vector2)hero.transform.position-(Vector2)captain.transform.parent.position);director.TickCommander(captain);
                Assert(art.sprite==AncientArt.ProductionCaptain(1)&&!art.flipX,"The actual transport captain selects the original melee-anticipation sprite and locked facing");
                MoveHero(hero,new Vector2(captain.transform.position.x-2,3.76f));Field(typeof(AncientProductionPursuit),"captainTimer").SetValue(director,0f);director.TickCommander(captain);director.TickCommander(captain);
                Assert(art.sprite==AncientArt.ProductionCaptain(2)&&!art.flipX,"Resolving the real melee pattern selects its follow-through and keeps facing when the officer crosses behind");
                Field(typeof(AncientProductionPursuit),"captainActionUntil").SetValue(director,Time.time-1);director.TickCommander(captain);
                Assert(art.sprite==AncientArt.ProductionCaptain(5)&&art.flipX,"After its action window, the actual captain returns to recovery and can face the officer again");
                Transform speaker;float head;
                Assert(director.TryDialogueActor("운송대장",out speaker,out head)&&speaker==captain.transform&&Mathf.Abs(head-(art.bounds.max.y-captain.transform.position.y))<.01f,"The captain's dialogue anchor follows the displayed original pose bounds rather than a fixed old portrait height");
                Field(typeof(AncientProductionPursuit),"captainPattern").SetValue(director,1);Field(typeof(AncientProductionPursuit),"captainWindup").SetValue(director,true);Field(typeof(AncientProductionPursuit),"captainFacing").SetValue(director,-1f);Field(typeof(AncientProductionPursuit),"captainTimer").SetValue(director,.5f);
                Field(typeof(AncientProductionPursuit),"captainAim").SetValue(director,(Vector2)hero.transform.position-(Vector2)captain.transform.parent.position);director.TickCommander(captain);
                Assert(art.sprite==AncientArt.ProductionCaptain(3)&&art.flipX&&tell.enabled,"The physical plate pattern uses its distinct original throwing-anticipation pose and real trajectory tell");
                Vector2 muzzle=tell.GetPosition(0);int shots=game.shots.Count;
                Field(typeof(AncientProductionPursuit),"captainTimer").SetValue(director,0f);director.TickCommander(captain);
                Assert(game.shots.Count==shots+1&&((Vector2)game.shots[game.shots.Count-1].transform.position-muzzle).sqrMagnitude<.00001f,"The actual thrown plate starts exactly at the displayed arm-side trajectory origin");
                MoveHero(hero,new Vector2(captain.transform.position.x+1,3.76f));director.TickCommander(captain);
                Assert(art.sprite==AncientArt.ProductionCaptain(4)&&art.flipX&&!tell.enabled&&!zone.enabled,"Throw follow-through keeps its facing and clears the resolved attack tell after the officer crosses sides");
            }
            finally
            {
                foreach(var shot in game.shots.ToArray())if(shot&&!oldShots.Contains(shot))shot.Remove();
                for(int i=0;i<fields.Length;i++)Field(typeof(AncientProductionPursuit),fields[i]).SetValue(director,saved[i]);
                MoveHero(hero,position);body.linearVelocity=velocity;art.sprite=sprite;art.flipX=flipped;art.color=tint;art.transform.localScale=scale;art.transform.localRotation=rotation;Field(typeof(AncientEnemy),"facing").SetValue(captain,facing);
                tell.positionCount=tellPoints.Length;tell.SetPositions(tellPoints);tell.enabled=tellShown;zone.positionCount=zonePoints.Length;zone.SetPositions(zonePoints);zone.enabled=zoneShown;
            }
        }
        static void CheckRun(AncientGame game)
        {
            var director=game.Production;var sequence=director.Sequence;var commander=director.Commander;
            Assert(sequence.Phase==AncientProductionPhase.Chase&&sequence.Round==0&&sequence.MountCount==1&&game.Story.Stage==5,"The actual interrupted stock report reaches the first playable motorcycle chase");
            Assert(game.hero.transform.position.x>=AncientProductionLayout.ChaseStart&&game.hero.transform.position.x<AncientProductionLayout.ChaseEnd,"The playable chase starts on its dedicated tunnel instead of the stock-room coordinates");
            Assert(!game.ArenaActive,"The moving commander cannot activate the old stationary guardian clamp");
            director.TickCommander(commander);Assert(((SpriteRenderer)Field(typeof(AncientEnemy),"art").GetValue(commander)).sprite==AncientArt.ProductionCaptain(0),"The road phase displays the original transport captain's idle pose");
            CheckTruckArtwork(game);
            CheckRoadCamera(game,"chase entry");
            CheckAutomaticBike(game);
            CheckRoadFeedback(game);
            float initial=commander.HealthRatio;commander.Hit(100000,true);
            Assert(Near(commander.HealthRatio,initial),"The captain cannot lose deck-phase health from an early road attack");
            float elapsed=sequence.Elapsed;game.skills=true;Call(director,"Update");game.skills=false;
            Assert(sequence.Elapsed==elapsed,"A real paused director Update cannot advance its chase clock");
            Advance(director,AncientProductionPhase.Approach);
            Assert(!sequence.AttackersEnabled&&director.AutoDrive,"Timed approach stops attacks while automatic riding continues");
            Assert(LiveHazards(director)==0&&game.shots.Count==0,"The ceasefire removes actual road hazards and flying attack objects");
            CheckRoadCamera(game,"ceasefire approach");
            Step(director,1);Step(director,1);
            Assert(sequence.Phase==AncientProductionPhase.Approach,"The wire-boarding opportunity stays open instead of timing out");
            CheckMissedBoarding(game);
            BoardRoof(game);CheckMovingRoofJump(game);CheckDeckCamera(game);CheckCaptainPresentation(game);commander.Hit(100000,true);
            Assert(sequence.Phase==AncientProductionPhase.Knockback&&Near(commander.HealthRatio,AncientProductionRules.FirstHealthGate)&&commander.IsAlive,"An actual captain hit stops at the first health gate and begins the knockback transition");
            Advance(director,AncientProductionPhase.Chase);
            Assert(sequence.Round==1&&sequence.MountCount==2&&game.hero.bike,"The first knockback automatically returns to the second bike mount");
            Assert(AncientGame.Saved("productionRound")==1,"The second chase checkpoint is persisted at its actual phase entry");
            Assert(director.Retry()&&sequence.Round==1&&sequence.MountCount==2&&Near(commander.HealthRatio,AncientProductionRules.FirstHealthGate),"Retry resumes the second round and its captain-health checkpoint instead of restarting inventory or round one");
            CheckRoadCamera(game,"second-round retry");
            Advance(director,AncientProductionPhase.Approach);BoardRoof(game);commander.Hit(100000,true);
            Assert(sequence.Phase==AncientProductionPhase.Knockback&&Near(commander.HealthRatio,AncientProductionRules.SecondHealthGate)&&commander.IsAlive,"The second real deck hit clamps at its health gate without killing the captain");
            Advance(director,AncientProductionPhase.Finisher);
            Assert(sequence.MountCount==3&&director.Cinematic,"The third bike mount starts the directed finisher instead of a third combat loop");
            for(int i=0;i<20&&sequence.Elapsed<3;i++)Step(director);
            Assert(director.CargoSecured&&!director.TruckDestroyed&&game.Cores==0,"The blade separates and secures the visible cargo before the truck can explode or award recovery");
            CheckCargoArtwork(director,true);
            for(int i=0;i<80&&sequence.Elapsed<7.35f;i++)Step(director,.1f);
            var beam=(LineRenderer)Field(typeof(AncientProductionPursuit),"beam").GetValue(director);
            Assert(beam.enabled&&beam.startWidth>=2&&!director.TruckDestroyed,"The backflip finisher shows a real thick continuous beam before the impact explosion");
            for(int i=0;i<12&&sequence.Elapsed<8;i++)Step(director,.1f);
            Assert(director.Active&&director.TruckDestroyed&&director.CargoSecured&&game.Cores==0,"The explosion can be paused before arrest without awarding recovery early");
            Assert(director.Retry()&&sequence.Round==1&&game.Cores==0&&!director.TruckDestroyed&&!director.CargoSecured,"Retrying after the finisher explosion restores the second playable round without awarding or duplicating cores");
            var truckArt=(SpriteRenderer)Field(typeof(AncientProductionPursuit),"truckArt").GetValue(director);
            var roofArt=(SpriteRenderer)Field(typeof(AncientProductionPursuit),"truckRoofArt").GetValue(director);
            Assert(truckArt.enabled&&roofArt.enabled,"The late-finisher retry restores both the cargo vehicle and the actual walkable roof renderer");
            var cargo=(Transform)Field(typeof(AncientProductionPursuit),"cargo").GetValue(director);
            foreach(var renderer in cargo.GetComponentsInChildren<SpriteRenderer>())
                Assert(renderer.enabled,"The retry preserves every visible recovered-cargo renderer: "+renderer.name);
            Assert(!beam.enabled&&!((LineRenderer)Field(typeof(AncientProductionPursuit),"cut").GetValue(director)).enabled,"Retry cannot leave a finisher beam or cargo-cut light over the playable chase");
            Advance(director,AncientProductionPhase.Approach);BoardRoof(game);commander.Hit(100000,true);Advance(director,AncientProductionPhase.Finisher);
            Advance(director,AncientProductionPhase.Secured);
            Assert(director.TruckDestroyed&&director.CargoSecured&&game.Cores==3,"The final truck explosion preserves all three safely separated cores");
            Assert(game.Story.Stage==7&&game.Story.DialogueOpen&&game.bossDefeated&&commander.IsAlive,"The real completion callback opens arrest dialogue with the captain alive");
            var captainArt=commander.GetComponentInChildren<SpriteRenderer>();
            Assert(captainArt&&captainArt.enabled,"The arrested captain stays visible after the vehicle's renderers are hidden");
            int points=game.Story.ResidencePoints;game.Story.ProductionCleared();
            Assert(points==1000&&game.Story.ResidencePoints==points,"Arrest awards the first promotion points once even if completion is called again");
            float beforeHandoff=game.hero.transform.position.x;int handoffCredits=game.credits,handoffScore=game.score;FinishDialogue(game,12);
            var body=game.hero.GetComponent<Rigidbody2D>();
            Assert(game.Story.Stage==8&&director.Cinematic&&Mathf.Abs(game.hero.transform.position.x-beforeHandoff)<.001f&&!body.simulated,"Finishing the actual arrest conversation begins the held handoff before moving the officer across the map");
            float elapsedHandoff=(float)Field(typeof(AncientProductionPursuit),"handoffElapsed").GetValue(director);
            game.skills=true;Call(director,"Update");game.skills=false;
            Assert(Near((float)Field(typeof(AncientProductionPursuit),"handoffElapsed").GetValue(director),elapsedHandoff),"Opening a pause menu freezes the actual arrest-handoff fade");
            Call(director,"TickHandoff",.25f);Call(director,"TickHandoff",.25f);
            Assert(director.Cinematic&&!director.InChaseLane&&Near(game.hero.transform.position.x,115)&&!body.simulated,"The corridor transfer occurs at the fully black handoff midpoint while player physics stays held");
            Call(director,"TickHandoff",.25f);Call(director,"TickHandoff",.25f);
            Assert(!director.Cinematic&&body.simulated&&Near(game.hero.transform.position.x,115)&&game.Cores==3,"The completed handoff restores play on the continuing production road with all recovered cores");
            Assert(game.Campaign.Active&&game.Campaign.CurrentRoom==0&&game.Campaign.ChapterStarted(0)&&!game.Story.DialogueOpen,"The actual arrest handoff opens the forward investigation without a walk back to the chief or a second briefing");
            Assert((AncientGame.Saved("campaignChapterStarted")&1)!=0&&AncientGame.Saved("campaignActive")==0&&game.credits==handoffCredits&&game.score==handoffScore&&game.Story.ResidencePoints==points,"The forward assignment is persisted without granting a duplicate arrest reward");
            Vector2 settled=body.position,motion=body.linearVelocity;int enemies=game.enemies.Count;
            Assert(!game.Campaign.TryContinueProductionRoute()&&body.position==settled&&body.linearVelocity==motion&&game.enemies.Count==enemies,"Repeating the handoff continuation cannot reset movement or respawn the first encounter");
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
                    if(Time.time<.5f)return;
                    Assert(game.Story.Stage==3&&!game.Story.DialogueOpen&&!game.Production.Active,"Completed weapon practice leaves the stock objective available without starting the chase");
                    CheckOptionalWorkers(game);CheckTrainingFeeder(game);
                    CheckStockApproach(game);game.Story.TryProductionStock();
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
