using System;
using System.Collections.Generic;
using System.Reflection;
using Plugin.AncientCivilization;
using UnityEngine;

// Runs the shipping route/resident controllers against deterministic service doubles.
// These are lifecycle/save checks, not a Unity playthrough or a traversal-time estimate.
class LowerControllerChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    static object Get(AncientLowerRoute route,string name)=>typeof(AncientLowerRoute).GetField(name,Private).GetValue(route);
    static void Call(AncientLowerRoute route,string name)=>typeof(AncientLowerRoute).GetMethod(name,Private).Invoke(route,null);
    static void WireSet(AncientWireObject wire,string name,object value)=>typeof(AncientWireObject).GetField(name,Private).SetValue(wire,value);
    static void WireFrame(AncientWireObject wire)=>typeof(AncientWireObject).GetMethod("Update",Private).Invoke(wire,null);
    static AncientWireObject Flying(Fixture f,Vector2 p,Vector2 velocity,bool hostile=false)
    {
        f.game.Story.CurrentFloor=1;var wire=new GameObject("Swept throw fixture").AddComponent<AncientWireObject>();wire.Initialize(f.game,f.game.Living,p,false);
        var mode=typeof(AncientWireObject).GetField("mode",Private);mode.SetValue(wire,Enum.Parse(mode.FieldType,hostile?"Hostile":"Flying"));
        WireSet(wire,"velocity",velocity);WireSet(wire,"rethrown",true);return wire;
    }
    static AncientEnemy Enemy(Fixture f,Vector2 p)
    {var enemy=new GameObject("Throw target").AddComponent<AncientEnemy>();enemy.transform.position=p;f.game.enemies.Add(enemy);return enemy;}
    static void ThrownOrdering()
    {
        foreach(float dt in new[]{.1f,.5f,1f})
        {
            // Frame-sized sweeps deliberately use the shipping velocity at several long/short steps.
            float end=22*dt;var f=new Fixture();f.game.LowerRoute=null;Time.deltaTime=dt;
            var close=Enemy(f,new Vector2(end*.35f,0));var far=Enemy(f,new Vector2(end*.8f,0));
            var wire=Flying(f,Vector2.zero,Vector2.right*22);WireFrame(wire);
            Check(close.Hits==1&&far.Hits==0&&wire.Available,"Nearest enemy wins independently of the enemy list's reverse iteration order");
            Check(wire.Point.x<=close.AimPoint.x&&f.game.hero.Combat.EmissionCount==1,"The object shatters at the first contact and emits particles once");
            WireFrame(wire);Check(close.Hits==1&&f.game.hero.Combat.EmissionCount==1,"A spent object cannot apply the same impact twice");

            f=new Fixture();f.game.LowerRoute=null;Time.deltaTime=dt;Physics2D.WallX=end*.5f;
            var behind=Enemy(f,new Vector2(Physics2D.WallX+.1f,0));wire=Flying(f,Vector2.zero,Vector2.right*22);WireFrame(wire);
            Check(behind.Hits==0&&Math.Abs(wire.Point.x-Physics2D.WallX)<.001f,"A target radius protruding through a thin wall cannot receive a thrown hit");
            Check(wire.Available&&f.game.hero.Combat.EmissionCount==1,"A wall consumes the thrown object exactly once");

            f=new Fixture();f.game.LowerRoute=null;Time.deltaTime=dt;Physics2D.WallX=end*.8f;f.game.hero.transform.position=new Vector2(end*.35f,0);
            wire=Flying(f,Vector2.zero,Vector2.right*22,true);WireFrame(wire);
            Check(f.game.hero.HurtCount==1&&wire.Point.x<Physics2D.WallX,"A hostile fist contacts a player before a wall on the same frame");
            f=new Fixture();f.game.LowerRoute=null;Time.deltaTime=dt;Physics2D.WallX=end*.5f;f.game.hero.transform.position=new Vector2(Physics2D.WallX+.1f,0);
            wire=Flying(f,Vector2.zero,Vector2.right*22,true);WireFrame(wire);
            Check(f.game.hero.HurtCount==0&&Math.Abs(wire.Point.x-Physics2D.WallX)<.001f,"A hostile fist cannot damage a player whose radius overlaps a wall from behind");
        }
        foreach(bool enemyFirst in new[]{true,false})
        {
            var f=new Fixture();f.Move(45);f.Dialogue();Vector2 p=AncientLowerRoute.Point(45);Time.deltaTime=1;
            var enemy=Enemy(f,p+Vector2.right*(enemyFirst?-3:3));var wire=Flying(f,p-Vector2.right*6,Vector2.right*22);WireFrame(wire);
            Check(enemy.Hits==(enemyFirst?1:0)&&(bool)Get(f.route,"resolved")==!enemyFirst,"The actual receiver callback and enemy collision compete by first distance, not code order");
            Check(wire.Available&&f.game.hero.Combat.EmissionCount==1,"A receiver/target collision destroys and particleizes the object only once");
        }
        var blocked=new Fixture();blocked.Move(45);blocked.Dialogue();var target=AncientLowerRoute.Point(45);Physics2D.WallX=target.x-.1f;Time.deltaTime=1;
        var blockedWire=Flying(blocked,target-Vector2.right*6,Vector2.right*22);WireFrame(blockedWire);
        Check(!(bool)Get(blocked.route,"resolved")&&blockedWire.Available,"A required throw receiver cannot be activated through a thin wall");
        var paused=new Fixture();paused.game.LowerRoute=null;paused.game.MainMenu=true;var pauseWire=Flying(paused,Vector2.zero,Vector2.right*22);WireFrame(pauseWire);
        Check(pauseWire.Point.sqrMagnitude==0&&!pauseWire.Available,"A menu pause freezes an in-flight object");
    }
    static void CounterRocket()
    {
        var f=new Fixture();f.game.LowerRoute=null;Time.deltaTime=.5f;
        var boss=new AncientLivingBoss{Core=new Vector2(4,0),CanCatchRocket=true};f.game.Living.Boss=boss;
        var enemy=Enemy(f,boss.Core);enemy.IsLivingBoss=true;
        var rocket=Flying(f,Vector2.zero,Vector2.right*22);WireSet(rocket,"boss",boss);WireSet(rocket,"rocket",true);WireSet(rocket,"doubleRocket",true);WireSet(rocket,"reflections",1);
        WireFrame(rocket);
        Check(boss.Catches==1&&boss.Hits==0&&!rocket.Selectable&&!rocket.Available,"The first reflected double rocket is caught without damaging the mech or releasing particles");
        Check(f.game.hero.Combat.EmissionCount==0,"A caught fist remains an intact counterable object");
        f.game.hero.transform.position=new Vector2(0,5);Time.deltaTime=.6f;WireFrame(rocket);
        Check(!rocket.Selectable&&boss.Catches==1,"The boss holds its caught fist for the visible warning duration");
        Time.deltaTime=.11f;WireFrame(rocket);
        var speed=(Vector2)typeof(AncientWireObject).GetField("velocity",Private).GetValue(rocket);
        Check(rocket.Selectable&&speed.x<0&&Math.Abs(speed.y)<.001f,"The rethrow locks the original player position and becomes selectable again");
        Time.deltaTime=.01f;WireFrame(rocket);
        var later=(Vector2)typeof(AncientWireObject).GetField("velocity",Private).GetValue(rocket);
        Check(later.x==speed.x&&later.y==speed.y,"The rethrown double fist flies straight instead of resuming homing");
        rocket.Grab();
        Check(rocket.Orbiting&&(int)typeof(AncientWireObject).GetField("reflections",Private).GetValue(rocket)==2,"A second actual wire grab records the second reflection");
        var mode=typeof(AncientWireObject).GetField("mode",Private);mode.SetValue(rocket,Enum.Parse(mode.FieldType,"Flying"));rocket.transform.position=Vector2.zero;
        WireSet(rocket,"velocity",Vector2.right*22);Time.deltaTime=.5f;WireFrame(rocket);
        Check(boss.Catches==1&&boss.Hits==1&&rocket.Available&&f.game.hero.Combat.EmissionCount==1,"The second reflection damages the mech once, never creates an endless catch loop, and emits particles");
        foreach(bool pair in new[]{false,true})
        {
            f=new Fixture();f.game.LowerRoute=null;Time.deltaTime=.5f;boss=new AncientLivingBoss{Core=new Vector2(4,0),CanCatchRocket=!pair};f.game.Living.Boss=boss;
            enemy=Enemy(f,boss.Core);enemy.IsLivingBoss=true;rocket=Flying(f,Vector2.zero,Vector2.right*22);WireSet(rocket,"boss",boss);WireSet(rocket,"rocket",true);WireSet(rocket,"doubleRocket",pair);WireSet(rocket,"reflections",1);
            WireFrame(rocket);Check(boss.Catches==0&&boss.Hits==1&&rocket.Available,"Single rockets and a mech outside its catch window resolve as ordinary reflected hits");
        }
    }
    static void TimerCall(AncientPlaytime timer,string method,params object[] args)
    {typeof(AncientPlaytime).GetMethod(method,Private).Invoke(timer,args);}
    static void TimerFrame(AncientPlaytime timer,float seconds)
    {Time.unscaledDeltaTime=seconds;TimerCall(timer,"Update");timer.WriteSave();}
    static void PlaytimeIntegration()
    {
        var f=new Fixture();var timer=f.game.gameObject.AddComponent<AncientPlaytime>();timer.Initialize(f.game);
        TimerFrame(timer,60);Check(AncientGame.Saved("lowerTimeTotal0")==0,"The initial frame does not count load/menu time as gameplay");
        TimerFrame(timer,2);Check(AncientGame.Saved("lowerTimeTotal0")==2000,"The shipping timer counts an active unscaled frame");
        f.game.Story.DialogueOpen=true;f.game.Production.Active=true;TimerFrame(timer,3);
        Check(AncientGame.Saved("lowerTimeTotal0")==5000&&AncientGame.Saved("lowerTimeDialogue0")==3000&&AncientGame.Saved("lowerTimeBoss0")==3000,"Dialogue and production boss categories use the real controller's eligibility without double-counting total time");
        f.game.Story.DialogueOpen=false;f.game.Production.Active=false;
        foreach(int kind in new[]{0,1,2,3})
        {
            if(kind==0)f.game.skills=true;if(kind==1)f.game.Journal.Open=true;if(kind==2)f.game.Story.LiftBusy=true;if(kind==3)f.game.MainMenu=true;
            int before=AncientGame.Saved("lowerTimeTotal0");TimerFrame(timer,100);
            f.game.skills=f.game.Journal.Open=f.game.Story.LiftBusy=f.game.MainMenu=false;TimerFrame(timer,100);
            Check(AncientGame.Saved("lowerTimeTotal0")==before,"Menu/journal/lift time and the first resumed frame are excluded, even with a long suspended delta");
            TimerFrame(timer,1);Check(AncientGame.Saved("lowerTimeTotal0")==before+1000,"The clock resumes on the next uninterrupted gameplay frame");
        }
        int total=AncientGame.Saved("lowerTimeTotal0");Application.isFocused=false;TimerCall(timer,"OnApplicationFocus",false);TimerFrame(timer,200);
        Application.isFocused=true;TimerCall(timer,"OnApplicationFocus",true);TimerFrame(timer,600);TimerFrame(timer,1);
        Check(AncientGame.Saved("lowerTimeTotal0")==total+1000,"Focus loss and restoration cannot add time spent in another window");
        total=AncientGame.Saved("lowerTimeTotal0");int flushes=PlayerPrefs.Flushes;TimerCall(timer,"OnApplicationPause",true);
        Check(PlayerPrefs.Flushes==flushes+1,"Suspending the application flushes its current playtime");
        TimerFrame(timer,600);TimerCall(timer,"OnApplicationPause",false);TimerFrame(timer,600);TimerFrame(timer,1);
        Check(AncientGame.Saved("lowerTimeTotal0")==total+1000,"An OS pause without regular Update calls cannot inflate the next resumed frame");
        total=AncientGame.Saved("lowerTimeTotal0");f.game.HasSession=false;TimerFrame(timer,100);f.game.HasSession=true;TimerFrame(timer,100);TimerFrame(timer,1);
        Check(AncientGame.Saved("lowerTimeTotal0")==total+1000,"A stopped session begins a fresh measurement interval on resume");
        total=AncientGame.Saved("lowerTimeTotal0");AncientGame.IsQAProfile=true;TimerFrame(timer,100);timer.Retry();AncientGame.IsQAProfile=false;TimerFrame(timer,100);TimerFrame(timer,1);timer.Retry();timer.WriteSave();
        Check(AncientGame.Saved("lowerTimeTotal0")==total+1000&&AncientGame.Saved("lowerTimeRetry0")==1,"QA updates and retries are excluded while an actual player retry is retained");
        total=AncientGame.Saved("lowerTimeTotal0");f.game.Campaign.CompletedMask=15;f.game.Story.CurrentFloor=1;TimerFrame(timer,1);
        Check(AncientGame.Saved("lowerTimeComplete")==1,"Production first-clear timing freezes only after the four cases and ascent");
        f.game.City.Cleared=2;f.game.Story.DialogueOpen=true;f.game.ArenaActive=true;TimerFrame(timer,3);
        Check(AncientGame.Saved("lowerTimeComplete")==1&&AncientGame.Saved("lowerTimeTotal1")==4000,"The living arrest dialogue remains in first-clear time after the mech is defeated");
        f.game.Story.DialogueOpen=false;TimerFrame(timer,1);f.game.Story.CurrentFloor=0;TimerFrame(timer,50);
        Check(AncientGame.Saved("lowerTimeComplete")==3&&AncientGame.Saved("lowerTimeTotal1")==4000&&AncientGame.Saved("lowerTimeTotal0")==total,"Both finished districts remain frozen during return visits");
        var restored=new Fixture(false);var loaded=restored.game.gameObject.AddComponent<AncientPlaytime>();loaded.Initialize(restored.game);
        Check(loaded.Summary(0).Contains("완료")&&loaded.Summary(1).Contains("완료")&&AncientGame.Saved("lowerTimeRetry0")==1,"Actual runtime serialization restores completed clocks and retry counts");
        AncientGame.Prefs.Clear();AncientGame.SetSaved("questStage",9);AncientGame.SetSaved("cityFloor",1);var old=new Fixture(false);var partial=old.game.gameObject.AddComponent<AncientPlaytime>();partial.Initialize(old.game);
        Check(AncientGame.Saved("lowerTimePartial")==3&&partial.Summary(0).StartsWith("이번 버전 이후")&&partial.Summary(1).StartsWith("이번 버전 이후"),"An old save never labels new partial measurements as complete historical playtime");
    }
    sealed class Fixture
    {
        public AncientGame game;public AncientLowerRoute route;
        public Fixture(bool clear=true,int completed=0,int active=0)
        {
            if(clear)AncientGame.Prefs.Clear();AncientGame.IsQAProfile=false;Application.isFocused=true;Physics2D.Blocked=false;Physics2D.WallX=float.NaN;Time.time=1;Time.deltaTime=.1f;
            game=new GameObject("Route world fixture").AddComponent<AncientGame>();game.Campaign.CompletedMask=completed;game.Campaign.CurrentRoom=active;
            route=game.gameObject.AddComponent<AncientLowerRoute>();game.LowerRoute=route;route.Initialize(game);
        }
        public void Move(int id,bool upper=false,bool setCase=true)
        {
            if(setCase){game.Campaign.CurrentRoom=id/9;game.Campaign.CurrentSector=id%9/3;}
            game.Story.CurrentFloor=id/36;game.hero.transform.position=upper?AncientLowerRoute.Point(id):AncientLowerRoute.BasePoint(id)+Vector2.left*6;
            game.View.transform.position=(Vector3)game.hero.transform.position+new Vector3(0,3,-10);Frame();
        }
        public void Frame(){Time.time+=.1f;Call(route,"Update");Call(route,"LateUpdate");}
        public void Dialogue(){Check(route.TryInteract()&&game.Story.DialogueOpen,"Nearby ground interaction opens the real controller's conversation");game.Story.Finish();}
        public void FinishStop(int id)
        {
            Move(id);Check(route.CurrentStop==id,"The runtime selects physical stop "+id+" in order");
            var data=AncientLowerRouteData.Stops[id];int beforeCredits=game.credits;
            Check(((BoxCollider2D)Get(route,"gateSolid")).enabled==!data.Optional,"Only required stops close their actual local gate");
            Check(!route.TryBlade(AncientLowerRoute.Point(id)-Vector2.right,Vector2.right),"Unbriefed input cannot finish a new action");
            Dialogue();
            if(data.Action!=AncientRouteAction.Talk)
            {
                Frame();Vector2 p=AncientLowerRoute.Point(id);
                switch(data.Action)
                {
                    case AncientRouteAction.Blade:
                        Physics2D.Blocked=true;Check(!route.TryBlade(p-Vector2.right,Vector2.right),"Blade cannot cut a plate through terrain");Physics2D.Blocked=false;
                        Check(route.TryBlade(p-Vector2.right,Vector2.right),"A nearby unobstructed blade resolves the plate");break;
                    case AncientRouteAction.Relay:
                        var supply=(SpriteRenderer)Get(route,"supply");float energy=game.hero.Combat.Emitted;
                        Check(route.TryBlade((Vector2)supply.transform.position-Vector2.right,Vector2.right),"An empty-energy player has a working local refill");
                        Check(game.hero.Combat.Emitted==energy+32&&!route.TryBlade((Vector2)supply.transform.position-Vector2.right,Vector2.right),"The refill emits once and respects its cooldown");
                        Physics2D.Blocked=true;route.OnLaser(p-Vector2.right*3,p+Vector2.right*3,.5f);Check(!(bool)Get(route,"resolved"),"A blocked beam does not power the receiver");Physics2D.Blocked=false;
                        route.OnLaser(p-Vector2.right*3,p+Vector2.right*3,.5f);break;
                    case AncientRouteAction.Ambush:
                        Check(game.enemies.Count==2,"A local battle creates exactly two real controller-owned guards");
                        foreach(var enemy in game.enemies){Check(enemy.Kind!=3&&enemy.Floor==id/36,"Local enemies are non-bosses assigned to their actual floor");enemy.IsAlive=false;}Frame();break;
                    case AncientRouteAction.Wire:
                        Move(id,true);Check(route.TryInteract()&&!game.Story.DialogueOpen&&game.hero.ReleaseCount>0,"The upper connection resolves without pretending its confirmation dialogue already happened");break;
                    case AncientRouteAction.Throw:
                        Check(!route.HitThrown(p+Vector2.up*3,p+new Vector2(3,3)),"A missed throw leaves the objective unresolved");Check(route.HitThrown(p-Vector2.right*3,p+Vector2.right*3),"A swept throw reaches the receiver");break;
                }
                Check((bool)Get(route,"resolved")&&!AncientLowerRouteRules.Done(route.CompletedMask(id/9),id),"Action success still requires the witness's result confirmation");
                int[] progress=(int[])Get(route,"acted");
                Check(AncientLowerRouteRules.Done(progress[id/9],id)&&AncientLowerRouteRules.Done(AncientGame.Saved("lowerActionMask"+(id/9)),id),"Action success is saved before its witness confirmation");
                route.ResetEncounter();Move(id);
                Check((bool)Get(route,"resolved")&&game.enemies.Count==0,"Resetting a solved action keeps it solved and does not respawn defeated guards");
                Move(id);Dialogue();
            }
            Check(AncientLowerRouteRules.Done(route.CompletedMask(id/9),id),"Finishing the conversation saves earned progress");
            Check(game.credits==beforeCredits+(data.Optional?3:0)&&game.hero.hp<=game.hero.MaxHP,"Optional reward is exact and healing respects maximum health");
            Check(!((BoxCollider2D)Get(route,"gateSolid")).enabled,"The completed gate opens immediately");
            int credited=game.credits;float hp=game.hero.hp;Dialogue();
            Check(game.credits==credited&&game.hero.hp==hp,"Replaying a completed witness cannot duplicate rewards");
        }
    }
    static void Main()
    {
        var all=new Fixture();for(int id=0;id<72;id++)all.FinishStop(id);
        Check(all.game.credits==72,"All24 optional stops grant exactly72 TECH in total");
        for(int mission=0;mission<8;mission++)Check(all.route.CompletedMask(mission)==511&&AncientGame.Saved("lowerHeardMask"+mission)==511,"All nine earned stops and actual conversations survive per-case serialization");
        var reload=new Fixture(false,255,-1);for(int mission=0;mission<8;mission++)Check(reload.route.CompletedMask(mission)==511,"Full completion survives a fresh controller instance");
        reload.Move(0,false,false);Check(reload.route.CurrentStop==-1,"A completed revisited sector does not recreate gates or mandatory encounters");
        reload.Dialogue();Check(reload.game.credits==0,"A completed save's old witness does not award a second optional reward");

        var migrated=new Fixture(true,1,-1);
        Check(migrated.route.CompletedMask(0)==219&&AncientGame.Saved("lowerHeardMask0")==0,"An old completed case opens required stops without inventing unheard new dialogue");
        migrated.Move(0,false,false);Check(migrated.route.CurrentStop==2&&!((BoxCollider2D)Get(migrated.route,"gateSolid")).enabled,"Migrated case offers an optional revisit without a blocking gate");
        int done=migrated.route.CompletedMask(0);migrated.Dialogue();
        Check(migrated.game.Story.LastLines.Length==6&&AncientGame.Saved("lowerHeardMask0")==1&&migrated.route.CompletedMask(0)==done&&migrated.game.credits==0,"Reading an inherited witness records only the heard script and grants no new completion reward");
        var migratedAgain=new Fixture(false,1,-1);
        Check(AncientGame.Saved("lowerHeardMask0")==1,"Saving and reopening migrated completion still does not reveal the other new conversations");
        migratedAgain.Move(1,false,false);migratedAgain.Dialogue();
        Check(AncientGame.Saved("lowerHeardMask0")==3,"The same resident can give its second physical landmark's testimony on a return trip");

        var retry=new Fixture(true,2,-1);retry.Move(17,false,false);retry.Dialogue();retry.Frame();
        Check(retry.game.enemies.Count==2,"A skipped optional battle can start after its case is already complete");
        var previous=new List<AncientEnemy>(retry.game.enemies);retry.route.ResetEncounter();
        Check(retry.game.enemies.Count==0&&!AncientLowerRouteRules.Done(retry.route.CompletedMask(1),17),"Retry clears living guards without marking the encounter complete");
        foreach(var enemy in previous)Check(!enemy,"Reset destroys the old encounter's components");
        retry.Frame();Check(retry.game.enemies.Count==2&&!(bool)Get(retry.route,"resolved"),"Returning after reset respawns a live optional encounter instead of inferring victory from removed guards");

        var guards=new Fixture();AncientGame.SetSaved("lowerStopMask4",1);AncientGame.SetSaved("lowerHeardMask4",1);guards=new Fixture(false);
        guards.Move(37);guards.Dialogue();guards.game.Living.ExistingGuards=1;guards.Frame();Check(guards.game.enemies.Count==0,"A living ambush waits for nearby spotlight guards instead of stacking another pair");
        guards.game.Living.ExistingGuards=0;guards.Frame();Check(guards.game.enemies.Count==2,"The waiting ambush starts when the nearby threat is resolved");
        var gate=new Fixture();gate.Move(0);gate.game.hero.transform.position=AncientLowerRoute.Point(0)+Vector2.right*20;gate.Frame();
        Check(gate.game.hero.TravelCount==1&&gate.game.CameraSnaps==1&&!AncientLowerRouteRules.Done(gate.route.CompletedMask(0),0),"A fast gate bypass returns to earned progress without granting the objective");
        var pending=new Fixture();pending.Move(3);pending.Dialogue();var plate=AncientLowerRoute.Point(3);
        Check(pending.route.TryBlade(plate-Vector2.right,Vector2.right),"A pending plate can be resolved before leaving the game");
        var reopened=new Fixture(false);reopened.Move(3);
        Check((bool)Get(reopened.route,"resolved")&&!AncientLowerRouteRules.Done(reopened.route.CompletedMask(0),3),"A fresh controller restores action success without inventing the final witness conversation");
        Check(!reopened.route.TryBlade(plate-Vector2.right,Vector2.right),"A restored solved plate cannot be harvested again before confirmation");
        reopened.Dialogue();Check(reopened.game.Story.LastLines.Length==2&&AncientLowerRouteRules.Done(reopened.route.CompletedMask(0),3),"Restored success resumes with only the witness's two result lines");
        AncientGame.Prefs.Clear();AncientGame.SetSaved("lowerActionMask0",511);var corrupt=new Fixture(false);corrupt.Move(3);
        Check(!(bool)Get(corrupt.route,"resolved")&&AncientGame.Saved("lowerActionMask0")==0,"An unbriefed orphan action mask cannot bypass a new scene");
        Check(AncientLowerRoute.ProgressKeys.Length==24&&new HashSet<string>(AncientLowerRoute.ProgressKeys).Count==24,"Completion, heard dialogue and action results have separate unique save keys");
        ThrownOrdering();CounterRocket();PlaytimeIntegration();
        Console.WriteLine("PASS: "+checks+" shipping lower-route/resident/throw/playtime lifecycle, all72 stops, collisions, rewards, focus/pause and save checks with service doubles; native traversal/physics/input not simulated");
    }
}
