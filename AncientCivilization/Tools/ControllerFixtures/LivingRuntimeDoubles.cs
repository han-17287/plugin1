// Test doubles for executing the actual LivingBoss scheduler outside Unity.
// No native physics, rendering, transforms with rotated parents or input are simulated.
using System;
using System.Collections.Generic;
namespace Plugin.AncientCivilization
{
    using UnityEngine;
    public sealed class AncientHero : Object
    {
        public Transform transform=new Transform();public float hp=100,bikeHP,ImmuneUntil;public bool Held;public int HurtCalls,AcceptedHits;public Vector2 Impulse;
        public void Hurt(float damage){HurtCalls++;if(Time.time<ImmuneUntil)return;hp-=damage;ImmuneUntil=Time.time+.65f;AcceptedHits++;}
        public void AddEncounterImpulse(Vector2 impulse){Impulse=impulse;}
        public void HoldForEncounter(bool held){Held=held;}
        public readonly AncientCombat Combat=new AncientCombat();
    }
    public sealed class AncientCombat {public readonly List<float> Emitted=new List<float>();public void EmitParticles(Vector2 point,float amount){Emitted.Add(amount);}}
    public sealed class AncientEnemy : Object
    {
        public Transform transform=new Transform();public float Health=960;public AncientLivingBoss Boss;
        public bool IsAlive=>Health>0;public float HealthRatio=>Health/960;
        public void Hit(float damage,bool breakGuard=false){Health-=Boss.FilterDamage(damage,Health,960);}
        public void BossStep(float distance){transform.position+=(Vector3)(Vector2.right*distance);}
    }
    public sealed class AncientProjectile : Object
    {
        public Vector2 Point,Velocity;public float Damage,Gravity;public bool Breakable;
        public void SetLivingProjectile(bool breakable,bool cannon=false){Breakable=breakable;}
    }
    public sealed class ProjectileFixture
    {
        readonly AncientGame game;public ProjectileFixture(AncientGame owner){game=owner;}
        public readonly List<AncientProjectile> History=new List<AncientProjectile>();
        public AncientProjectile Spawn(Vector2 point,Vector2 speed,float damage,bool scrap,int kind,float gravity)
        {var p=new AncientProjectile{Point=point,Velocity=speed,Damage=damage,Gravity=gravity};History.Add(p);game.shots.Add(p);return p;}
    }
    public sealed class AncientLivingDistrict : Object
    {
        public AncientLivingBoss Boss;public int Rockets,DoubleRockets,Missiles,ClearCalls;public bool Wreck;
        public void ClearRockets(){ClearCalls++;}
        public void LaunchRocket(AncientLivingBoss boss,Vector2 point,bool pair){Rockets++;if(pair)DoubleRockets++;}
        public void QueueMissile(Vector2 point,Vector2 target,float delay){Missiles++;}
        public void BeginAftermath(Transform rig,Vector2 point){Wreck=true;}
    }
    public sealed class StoryFixture {public void RadioBark(string speaker,string text){}}
    public static class AncientVisuals
    {
        public static bool HighQuality = true;
    }
    public sealed class AncientGame : Object
    {
        public bool ReducedFeedback;
        public static readonly Color Cyan=new Color(.2f,.8f,1),Orange=new Color(1,.4f,.1f);
        public readonly AncientHero hero=new AncientHero();public readonly AncientLivingDistrict Living=new AncientLivingDistrict();public readonly StoryFixture Story=new StoryFixture();
        public readonly List<AncientProjectile> shots=new List<AncientProjectile>();public ProjectileFixture Projectiles;public bool Paused;public int InputBlocks;
        public AncientGame(){Projectiles=new ProjectileFixture(this);}
        public LineRenderer Line(string name,Color color,float width,int order)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.sortingOrder = order;
            return line;
        }
        public void Notice(string words,float seconds=2){}public void Sound(int sound){}public void Shake(float amount){}public void Impact(Vector2 point,Color color,bool heavy){}public void Burst(Vector2 point,Color color,int count){}
        public void ClearBattleProjectiles(){shots.Clear();}public void BlockGameplayInput(){InputBlocks++;}
    }
    public static class AncientArt
    {
        public static SpriteRenderer Display(string name,Sprite sprite,Vector2 point,int order,Transform parent)
        {var s=new GameObject(name).AddComponent<SpriteRenderer>();s.transform.SetParent(parent,false);s.transform.localPosition=point;return s;}
    }
    public static class AncientLivingArt {public static Sprite Part(int id)=>new Sprite();}
}
