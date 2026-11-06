#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Plugin.AncientCivilization.EditorChecks
{
    // Opt-in real-Physics2D probes. Only an already isolated QA Play Mode profile may be modified.
    public static class AncientProjectileSmokeChecks
    {
        const string Menu="PLUGIN/검사/투사체 지형 충돌 (QA 플레이 중)";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static FieldInfo Field(Type type,string name)
        {var field=type.GetField(name,Private);if(field==null)throw new MissingFieldException(type.Name,name);return field;}
        static void Assert(bool result,string message){if(!result)throw new InvalidOperationException(message);Debug.Log("PASS: "+message);}
        static void Tick(object item)
        {var method=item.GetType().GetMethod("Update",Private);if(method==null)throw new MissingMethodException(item.GetType().Name,"Update");method.Invoke(item,null);}
        [MenuItem(Menu,true)]
        static bool CanRun(){return EditorApplication.isPlaying&&AncientGame.IsQAProfile&&AncientGame.Instance&&!AncientGame.Instance.Paused;}
        [MenuItem(Menu)]
        public static void Run(){if(CanRun())RunInQAProfile(AncientGame.Instance);}
        static void MoveHero(AncientHero hero,Vector2 point)
        {hero.GetComponent<Rigidbody2D>().position=point;hero.transform.position=point;Physics2D.SyncTransforms();Field(typeof(AncientHero),"invulnerableUntil").SetValue(hero,0f);}
        static AncientProjectile Shot(AncientGame game,Vector2 start,Vector2 end,int ricochets=0)
        {
            var shot=game.Projectiles.Spawn(start,(end-start)/Time.deltaTime,18,false,0,0,ricochets);
            if(!shot)throw new InvalidOperationException("The projectile pool is full.");shot.enabled=false;Field(typeof(AncientProjectile),"age").SetValue(shot,.1f);return shot;
        }
        public static void RunInQAProfile(AncientGame game)
        {
            if(!EditorApplication.isPlaying||!AncientGame.IsQAProfile||!game||game!=AncientGame.Instance||game.Paused)
                throw new InvalidOperationException("Projectile probes require an isolated QA profile in unpaused Play Mode.");
            if(game.hero.bike||Input.GetKey(KeyCode.F)||Time.deltaTime<=0)
                throw new InvalidOperationException("Dismount and leave F untouched during projectile probes.");
            var hero=game.hero;var body=hero.GetComponent<Rigidbody2D>();Vector2 original=hero.transform.position;
            if(hero.Wired||(game.Sniper&&game.Sniper.InputLocked)||(hero.Combat&&(hero.Combat.Firing||hero.Combat.Charging||hero.Combat.BladeAnimating||hero.Combat.BladeBuffered)))
                throw new InvalidOperationException("Projectile probes require an idle hero without a wire, attack, charge or encounter lock.");
            var constraints=body.constraints;bool enabled=hero.enabled;float hp=hero.hp;
            Vector2 velocity=body.linearVelocity;float gravity=body.gravityScale;
            float immunity=(float)Field(typeof(AncientHero),"invulnerableUntil").GetValue(hero),hurt=(float)Field(typeof(AncientHero),"hurtUntil").GetValue(hero);
            AncientProjectile shot=null;AncientClockSpear spear=null;var wall=new GameObject("QA projectile ordering surface");
            try
            {
                // This isolated QA probe starts with cleared idle combat; live attack effects are not restored.
                if(hero.Combat)hero.Combat.ResetCombat();
                hero.enabled=false;body.constraints=RigidbodyConstraints2D.FreezeAll;body.linearVelocity=Vector2.zero;body.gravityScale=0;hero.hp=1000;
                wall.layer=8;var solid=wall.AddComponent<BoxCollider2D>();solid.size=new Vector2(.1f,10);wall.transform.position=new Vector2(6,80);
                MoveHero(hero,new Vector2(5,80));shot=Shot(game,new Vector2(2,80),new Vector2(8,80),1);Tick(shot);
                Assert(Mathf.Abs(hero.hp-982)<.001f&&shot.Available,"A sniper round crossed before a wall damages the hero before it could ricochet");shot.enabled=true;shot=null;
                hero.hp=1000;MoveHero(hero,new Vector2(6.15f,80));shot=Shot(game,new Vector2(2,80),new Vector2(8,80));Tick(shot);
                Assert(hero.hp==1000&&shot.Available,"A thin wall protects a hero whose generous hit radius overlaps its far side");shot.enabled=true;shot=null;
                wall.transform.position=new Vector2(2.5f,80);MoveHero(hero,new Vector2(2.7f,80));shot=Shot(game,new Vector2(2,80),new Vector2(2.2f,80));Tick(shot);
                Assert(hero.hp==1000&&!shot.Available&&Mathf.Abs(shot.transform.position.x-2.2f)<.001f,"A surface beyond this frame blocks radius leakage without prematurely removing the projectile");shot.Remove();shot.enabled=true;shot=null;
                wall.transform.position=new Vector2(6,80);MoveHero(hero,new Vector2(5,83));shot=Shot(game,new Vector2(2,80),new Vector2(8,80),1);Tick(shot);
                Assert(!shot.Available&&shot.velocity.x<0,"A shot missing the hero still performs its authored wall ricochet");shot.Remove();shot.enabled=true;shot=null;
                solid.size=new Vector2(20,1);wall.transform.position=new Vector2(2,79.5f);
                spear=new GameObject("QA clock-hand ordering probe").AddComponent<AncientClockSpear>();spear.Initialize(game);spear.enabled=false;
                MoveHero(hero,new Vector2(2,81.1f));spear.Play(new Vector2(2,82.3f),Vector2.down*(2.8f/Time.deltaTime),0);Tick(spear);
                Assert(Mathf.Abs(hero.hp-982)<.001f&&spear.Available,"Clock rain crossing hero and floor in one stalled frame damages before it sticks");
                hero.hp=1000;MoveHero(hero,new Vector2(2,79.8f));spear.Play(new Vector2(2,82.3f),Vector2.down*(2.8f/Time.deltaTime),0);Tick(spear);
                Assert(hero.hp==1000&&!spear.Available&&(bool)Field(typeof(AncientClockSpear),"stuck").GetValue(spear),"The floor still blocks a clock hand and leaves it breakable as particles");
                MoveHero(hero,new Vector2(2,79.9f));spear.Play(new Vector2(2,80.5f),Vector2.down*(.2f/Time.deltaTime),0);Tick(spear);
                Assert(hero.hp==1000&&!spear.Available&&!(bool)Field(typeof(AncientClockSpear),"stuck").GetValue(spear),"An upcoming floor only occludes and cannot make a hand stick before it arrives");
            }
            finally
            {
                if(shot){shot.Remove();shot.enabled=true;}if(spear){spear.Disarm();UnityEngine.Object.Destroy(spear.gameObject);}
                wall.SetActive(false);UnityEngine.Object.Destroy(wall);MoveHero(hero,original);hero.hp=hp;hero.enabled=enabled;body.constraints=constraints;
                body.gravityScale=gravity;body.linearVelocity=velocity;
                Field(typeof(AncientHero),"invulnerableUntil").SetValue(hero,immunity);Field(typeof(AncientHero),"hurtUntil").SetValue(hero,hurt);
            }
        }
    }
}
#endif
