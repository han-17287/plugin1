using System;
using System.Reflection;
using Plugin.AncientCivilization;
using UnityEngine;

// Executes the shipping boss controller, with narrowly scoped Unity/service doubles.
// Verifies scheduling and damage state; does not establish native input/physics/rendering.
class LivingControllerChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int checks;
    static void Check(bool result,string message){if(!result)throw new Exception(message);checks++;}
    static FieldInfo Field(string name)=>typeof(AncientLivingBoss).GetField(name,Private)??throw new Exception(name);
    static void Put(AncientLivingBoss boss,string name,object value){Field(name).SetValue(boss,value);}
    static object Get(AncientLivingBoss boss,string name)=>Field(name).GetValue(boss);
    static void Call(AncientLivingBoss boss,string name){typeof(AncientLivingBoss).GetMethod(name,Private).Invoke(boss,null);}
    static string State(AncientLivingBoss boss)=>Get(boss,"state").ToString();
    sealed class Fixture
    {
        public AncientGame game=new AncientGame();public AncientEnemy owner=new AncientEnemy();public AncientLivingBoss boss;
        public Fixture(int phase)
        {
            Time.time=0;Time.deltaTime=0;Physics2D.Blocked=false;
            owner.transform.position=new Vector2(AncientCityLayout.ArenaLeft(1)+16,0);
            game.hero.transform.position=new Vector2(AncientCityLayout.ArenaLeft(1)+4,1.05f);
            boss=new GameObject("Actual runtime boss fixture").AddComponent<AncientLivingBoss>();boss.transform.position=owner.transform.position;
            boss.Initialize(owner,game);owner.Boss=boss;
            var rules=(AncientLivingRules)Get(boss,"rules");if(phase>=2)rules.AdvancePhase(.68f);if(phase>=3)rules.AdvancePhase(.34f);
            owner.Health=phase==1?960:phase==2?600:300;
        }
        public void Tick(float dt){Time.deltaTime=dt;Time.time+=dt;boss.Tick();game.shots.Clear();}
        public void Attack(AncientLivingAttack move)
        {Put(boss,"move",move);Put(boss,"aim",(Vector2)game.hero.transform.position);Put(boss,"from",(Vector2)owner.transform.position);Put(boss,"emitted",0);Put(boss,"shotAt",0f);Call(boss,"BeginAttack");}
        public void Finish(int hz)
        {for(int i=0;i<hz*12&&State(boss)=="Active";i++)Tick(1f/hz);Check(State(boss)=="Recovery","An actual runtime attack reaches its recovery window");}
    }
    static void Main()
    {
        foreach(int hz in new[]{15,30,60,120,144,240})
        {
            foreach(int phase in new[]{1,2})
            {
                var f=new Fixture(phase);f.Attack(AncientLivingAttack.Bullets);f.Finish(hz);
                Check(f.game.Projectiles.History.Count==(phase==1?10:20),"Runtime emits the authored bullet count at "+hz+"Hz");
                foreach(var shot in f.game.Projectiles.History)Check(!shot.Breakable&&shot.Damage==12,"Every living bullet is unbreakable with its actual damage");
            }
            var single=new Fixture(1);single.Attack(AncientLivingAttack.Rocket);single.Finish(hz);Check(single.game.Living.Rockets==1&&single.game.Living.DoubleRockets==0,"Single rocket is launched once");
            var pair=new Fixture(3);pair.Attack(AncientLivingAttack.DoubleRocket);pair.Finish(hz);Check(pair.game.Living.Rockets==2&&pair.game.Living.DoubleRockets==2,"Double rocket launches both counterable fists exactly once");
            foreach(var attack in new[]{AncientLivingAttack.Missiles,AncientLivingAttack.CrossMissiles})
            {
                var f=new Fixture(3);f.Attack(attack);f.Finish(hz);
                Check(f.game.Living.Missiles==(attack==AncientLivingAttack.Missiles?8:16),"Actual missile scheduler finishes all cues before recovery at "+hz+"Hz");
            }
            var cannon=new Fixture(2);cannon.Attack(AncientLivingAttack.Cannon);cannon.Finish(hz);
            Check(cannon.game.Projectiles.History.Count==12,"Six launch shells and six falling cannonballs are scheduled once");
            int falling=0;foreach(var shot in cannon.game.Projectiles.History){Check(!shot.Breakable&&shot.Damage==23,"Cannonballs remain unbreakable");if(shot.Gravity>0)falling++;}
            Check(falling==6,"Exactly six cannonballs use falling gravity");
            foreach(float immunity in new[]{0f,.25f,.7f,1f})
            {
                var f=new Fixture(3);f.game.hero.transform.position=new Vector2(AncientCityLayout.ArenaLeft(1)+16,4.6f);f.game.hero.ImmuneUntil=immunity;
                f.Attack(AncientLivingAttack.XBlast);f.Finish(hz);
                Check(f.game.hero.AcceptedHits==(immunity>.8f?0:1),"Actual overlapping X arms consume at most one accepted hit, including early immunity");
            }
            var safe=new Fixture(3);safe.game.hero.transform.position=new Vector2(AncientCityLayout.ArenaLeft(1)+16,1.05f);safe.Attack(AncientLivingAttack.XBlast);safe.Finish(hz);
            Check(safe.game.hero.hp==100,"The drawn X leaves the authored centre-ground refuge safe");
            var grab=new Fixture(2);grab.game.hero.transform.position=(Vector2)grab.owner.transform.position+new Vector2(3,1.05f);grab.Attack(AncientLivingAttack.Grab);grab.Finish(hz);
            Check(grab.game.hero.AcceptedHits==1&&grab.game.hero.hp==62&&!grab.game.hero.Held&&!grab.boss.InputLocked,"A successful grab punches once and always releases the player");
            var cover=new Fixture(2);cover.game.hero.transform.position=(Vector2)cover.owner.transform.position+new Vector2(3,1.05f);Physics2D.Blocked=true;cover.Attack(AncientLivingAttack.Grab);cover.Finish(hz);
            Check(cover.game.hero.hp==100&&!cover.game.hero.Held,"An obstructed grab never captures through terrain");Physics2D.Blocked=false;
            foreach(int phase in new[]{1,3})
            {
                var charge=new Fixture(phase);var sequence=(AncientLivingRules)Get(charge.boss,"rules");
                if(phase==1){sequence.Next();sequence.Next();}Call(charge.boss,"StartAttack");
                float firedAt=0;for(int i=0;i<hz*4;i++)
                {charge.Tick(1f/hz);if(charge.game.Living.Rockets>0){firedAt=Time.time;break;}}
                Check(firedAt>=3-.002f&&firedAt<=3+1f/hz+.002f,"Both runtime rocket patterns retain their full three-second windup");
            }
            var returnToFront=new Fixture(2);Call(returnToFront.boss,"StartAttack");
            for(int i=0;i<hz*2&&State(returnToFront.boss)=="Windup";i++)returnToFront.Tick(1f/hz);
            Check(returnToFront.boss.FilterDamage(20,600,960)==0,"A cannon boss that moved into the background cannot take body damage");
            returnToFront.Finish(hz);Check(!returnToFront.boss.Recovering,"Gold attack-opportunity feedback stays hidden while the body is still in the background");
            for(int i=0;i<hz&&State(returnToFront.boss)=="Recovery"&&!returnToFront.boss.Recovering;i++)returnToFront.Tick(1f/hz);
            Check(returnToFront.boss.Recovering&&returnToFront.boss.FilterDamage(20,600,960)>0,"The attack-opportunity signal appears only when the returning body can actually be hit");
        }
        var pause=new Fixture(1);pause.Attack(AncientLivingAttack.Bullets);pause.game.Paused=true;for(int i=0;i<300;i++)pause.Tick(.02f);
        Check((float)Get(pause.boss,"age")==0&&pause.game.Projectiles.History.Count==0,"Pausing freezes the actual scheduler without releasing delayed bullets");
        pause.game.Paused=false;pause.Finish(60);Check(pause.game.Projectiles.History.Count==10,"Resuming continues the same complete attack");
        var overload=new Fixture(3);overload.Attack(AncientLivingAttack.Suction);overload.boss.ReceiveHit(AncientLivingHit.Laser,10,10);
        Check(!overload.boss.Sucking&&overload.boss.Recovering,"A laser reaching an open core immediately cancels suction");
        for(int i=0;i<200&&State(overload.boss)=="Stunned";i++)overload.Tick(1/60f);
        Check((AncientLivingAttack)Get(overload.boss,"move")==AncientLivingAttack.CrossMissiles,"Overload resumes with the promised missile attack");
        var shield=new Fixture(2);var sr=(AncientLivingRules)Get(shield.boss,"rules");shield.Attack(AncientLivingAttack.Grab);sr.RaiseBarrier();float hp=shield.owner.Health;
        shield.boss.ReceiveHit(AncientLivingHit.Laser,94,40);Check(shield.boss.Barrier==42&&shield.owner.Health==hp,"Actual partial-energy beam damages only the barrier");
        shield.boss.ReceiveHit(AncientLivingHit.Laser,190,100);Check(shield.boss.Barrier==0&&shield.owner.Health==hp&&shield.boss.Recovering,"Full beam breaks the remaining barrier without also taking body health");
        var release=new Fixture(2);release.Attack(AncientLivingAttack.Grab);Put(release.boss,"grabbed",true);release.game.hero.Held=true;release.owner.Health=960*.34f;release.Tick(.02f);
        Check(release.boss.Phase==3&&!release.game.hero.Held&&!release.boss.InputLocked&&State(release.boss)=="Transition","A health-gated phase transition releases an active grab");
        var stopped=new Fixture(2);Put(stopped.boss,"grabbed",true);stopped.game.hero.Held=true;stopped.boss.StopBattle();
        Check(!stopped.game.hero.Held&&!stopped.boss.InputLocked&&!stopped.game.Living.Boss,"Ending or abandoning combat clears both capture and district ownership");
        Console.WriteLine("PASS: "+checks+" shipping living-controller scheduling/damage-state checks with service doubles; native Unity physics/rendering/input not simulated");
    }
}
