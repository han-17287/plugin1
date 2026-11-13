// Test doubles for executing the actual LivingBoss scheduler outside Unity.
// No native physics, rendering, transforms with rotated parents or input are simulated.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value){return value!=null;} }
    public class Component : Object {public Transform transform=new Transform();public GameObject gameObject;}
    public class MonoBehaviour : Component {}
    public class GameObject : Object
    {
        public Transform transform=new Transform();public GameObject(string name){}
        public T AddComponent<T>() where T:Component,new(){return new T{gameObject=this,transform=transform};}
    }
    public class Transform : Object
    {
        Transform parent;Vector3 local;
        public Vector3 localPosition {get{return local;}set{local=value;}}
        public Vector3 position {get{return parent?parent.position+local:local;}set{local=parent?value-parent.position:value;}}
        public Vector3 localScale=Vector3.one;public Quaternion localRotation;
        public void SetParent(Transform value,bool worldPositionStays=true){Vector3 p=position;parent=value;if(worldPositionStays)position=p;}
    }
    public struct Vector2
    {
        public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}
        public static Vector2 zero=>new Vector2();public static Vector2 one=>new Vector2(1,1);public static Vector2 up=>new Vector2(0,1);public static Vector2 down=>new Vector2(0,-1);public static Vector2 left=>new Vector2(-1,0);public static Vector2 right=>new Vector2(1,0);
        public float sqrMagnitude=>x*x+y*y;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector2 normalized=>magnitude>1e-6f?this/magnitude:zero;
        public static Vector2 operator+(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);
        public static Vector2 operator*(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);public static Vector2 operator/(Vector2 a,float b)=>new Vector2(a.x/b,a.y/b);
        public static float Distance(Vector2 a,Vector2 b)=>(a-b).magnitude;
        public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>a+(b-a)*Mathf.Clamp01(t);
        public static implicit operator Vector3(Vector2 a)=>new Vector3(a.x,a.y,0);
        public static implicit operator Vector2(Vector3 a)=>new Vector2(a.x,a.y);
    }
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);
        public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
    }
    public struct Quaternion {public static Quaternion Euler(float x,float y,float z)=>new Quaternion();}
    public struct Color
    {
        public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static Color white=>new Color(1,1,1);
        public static Color Lerp(Color a,Color b,float t)=>new Color(Mathf.Lerp(a.r,b.r,t),Mathf.Lerp(a.g,b.g,t),Mathf.Lerp(a.b,b.b,t),Mathf.Lerp(a.a,b.a,t));
    }
    public static class Mathf
    {
        public const float PI=(float)Math.PI,Deg2Rad=PI/180;
        public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(b,x));public static float Clamp01(float x)=>Clamp(x,0,1);
        public static float Abs(float x)=>Math.Abs(x);public static float Sign(float x)=>x<0?-1:1;
        public static float Sin(float x)=>(float)Math.Sin(x);public static float Cos(float x)=>(float)Math.Cos(x);
        public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
        public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return Lerp(a,b,t*t*(3-2*t));}
        public static float MoveTowards(float a,float b,float step)=>Abs(b-a)<=step?b:a+Sign(b-a)*step;
    }
    public static class Time {public static float time,deltaTime;}
    public static class Physics2D {public static bool Blocked;public static bool Linecast(Vector2 a,Vector2 b,int mask)=>Blocked;}
    public class Sprite : Object {}
    public class SpriteRenderer : Component {public bool enabled=true;public Color color=Color.white;}
    public class LineRenderer : Component
    {
        readonly Vector3[] points=new Vector3[64];public int positionCount;public bool enabled,loop;public Color startColor,endColor;public float startWidth,endWidth;
        public void SetPosition(int index,Vector3 point){points[index]=point;}public Vector3 GetPosition(int index)=>points[index];
    }
}
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
    public sealed class AncientGame : Object
    {
        public static readonly Color Cyan=new Color(.2f,.8f,1),Orange=new Color(1,.4f,.1f);
        public readonly AncientHero hero=new AncientHero();public readonly AncientLivingDistrict Living=new AncientLivingDistrict();public readonly StoryFixture Story=new StoryFixture();
        public readonly List<AncientProjectile> shots=new List<AncientProjectile>();public ProjectileFixture Projectiles;public bool Paused;public int InputBlocks;
        public AncientGame(){Projectiles=new ProjectileFixture(this);}
        public LineRenderer Line(string name,Color color,float width,int order){return new GameObject(name).AddComponent<LineRenderer>();}
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
