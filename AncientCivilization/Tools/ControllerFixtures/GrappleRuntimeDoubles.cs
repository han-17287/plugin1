// 한 개의 수직 벽과 수동 물리 적분만 제공한다. Unity 물리나 입력을 흉내 낸 완주 시험이 아니다.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object {public static implicit operator bool(Object o)=>o!=null;public static bool operator !(Object o)=>o==null;}
    public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();}
    public class Behaviour:Component {public bool enabled=true;public bool isActiveAndEnabled=>enabled&&gameObject.activeSelf;}
    public class MonoBehaviour:Behaviour {protected static void Destroy(Object o){} }
    public class ScriptableObject:Object{}
    [AttributeUsage(AttributeTargets.Class)]public sealed class RequireComponent:Attribute{public RequireComponent(Type t){}}
    [AttributeUsage(AttributeTargets.Class)]public sealed class CreateAssetMenuAttribute:Attribute{public string menuName;}
    [AttributeUsage(AttributeTargets.Field)]public sealed class HeaderAttribute:Attribute{public HeaderAttribute(string s){}}
    [AttributeUsage(AttributeTargets.Field)]public sealed class MinAttribute:Attribute{public MinAttribute(float f){}}
    [AttributeUsage(AttributeTargets.Field)]public sealed class RangeAttribute:Attribute{public RangeAttribute(float a,float b){}}
    public enum RuntimeInitializeLoadType{SubsystemRegistration}
    [AttributeUsage(AttributeTargets.Method)]public sealed class RuntimeInitializeOnLoadMethodAttribute:Attribute{public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}
    public class GameObject:Object
    {readonly Dictionary<Type,Component> parts=new Dictionary<Type,Component>();public readonly Transform transform=new Transform();public bool activeSelf=true;public GameObject(string title){transform.owner=this;}public T AddComponent<T>()where T:Component,new(){var t=new T{gameObject=this};parts[typeof(T)]=t;return t;}public T GetComponent<T>()where T:Component{foreach(var part in parts.Values)if(part is T value)return value;return null;}}
    public class Transform:Object{public GameObject owner;public Vector3 position;public void SetParent(Transform p,bool world){}}
    public struct Vector2
    {
        public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();public static Vector2 one=>new Vector2(1,1);public static Vector2 up=>new Vector2(0,1);public static Vector2 right=>new Vector2(1,0);public static Vector2 left=>new Vector2(-1,0);
        public float sqrMagnitude=>x*x+y*y;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector2 normalized=>magnitude>.00001f?this/magnitude:zero;
        public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);public static Vector2 operator *(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);public static Vector2 operator /(Vector2 a,float b)=>new Vector2(a.x/b,a.y/b);
        public static float Dot(Vector2 a,Vector2 b)=>a.x*b.x+a.y*b.y;public static float Distance(Vector2 a,Vector2 b)=>(a-b).magnitude;public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>a+(b-a)*t;public static implicit operator Vector3(Vector2 a)=>new Vector3(a.x,a.y,0);public static implicit operator Vector2(Vector3 a)=>new Vector2(a.x,a.y);
    }
    public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
    public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1);public static Color cyan=>new Color(0,1,1);}
    public struct LayerMask{public int value;public static implicit operator LayerMask(int n)=>new LayerMask{value=n};public static implicit operator int(LayerMask n)=>n.value;}
    public static class Mathf{public const float PI=(float)Math.PI;public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Abs(float n)=>Math.Abs(n);public static float Sin(float n)=>(float)Math.Sin(n);public static float Cos(float n)=>(float)Math.Cos(n);}
    public class Shader:Object{public static Shader Find(string name)=>new Shader();}public class Material:Object{public Material(Shader shader){}}
    public class LineRenderer:Behaviour{public Material sharedMaterial;public bool useWorldSpace,loop;public int positionCount,sortingOrder;public float widthMultiplier;public Color startColor,endColor;public void SetPosition(int index,Vector3 point){}}
    public struct Bounds{public Vector3 extents;}
    public class Collider2D:Component{public Bounds bounds=>new Bounds{extents=new Vector3(.36f,.9f,0)};}
    public class CapsuleCollider2D:Collider2D{}
    public class Rigidbody2D:Component{public Vector2 position{get=>transform.position;set=>transform.position=value;}public int Cast(Vector2 direction,ContactFilter2D filter,RaycastHit2D[] hits,float distance)=>Physics2D.Cast(position,.36f,direction,hits,distance);}
    public struct ContactFilter2D{public bool useTriggers;public void SetLayerMask(LayerMask mask){}}
    public struct RaycastHit2D{public Collider2D collider;public Vector2 point,normal;public float distance;public static implicit operator bool(RaycastHit2D hit)=>hit.collider;public static bool operator !(RaycastHit2D hit)=>!hit.collider;}
    public static class Physics2D
    {
        public static float WallX=float.NaN;public static readonly Collider2D Wall=new GameObject("Wall").AddComponent<Collider2D>();
        public static bool Linecast(Vector2 a,Vector2 b,LayerMask mask)=>!float.IsNaN(WallX)&&(a.x-WallX)*(b.x-WallX)<=0;
        public static RaycastHit2D Raycast(Vector2 from,Vector2 direction,float range,LayerMask mask){var hits=new RaycastHit2D[1];return Cast(from,0,direction,hits,range)>0?hits[0]:default;}
        public static int CircleCast(Vector2 from,float radius,Vector2 direction,ContactFilter2D filter,RaycastHit2D[] hits,float distance)=>Cast(from,radius,direction,hits,distance);
        public static int Cast(Vector2 from,float radius,Vector2 direction,RaycastHit2D[] hits,float distance)
        {if(float.IsNaN(WallX)||Math.Abs(direction.x)<.0001f)return 0;float sign=direction.x>0?1:-1,t=(WallX-from.x-sign*radius)/direction.x;if(t<0||t>distance)return 0;hits[0]=new RaycastHit2D{collider=Wall,normal=new Vector2(-sign,0),distance=t,point=from+direction*t};return 1;}
    }
    public static class Resources{public static T Load<T>(string name)where T:Object=>null;}
    public class Camera:Object{public static Camera main=new Camera();public Vector3 ScreenToWorldPoint(Vector2 p)=>p;}
    public static class Application{public static bool isFocused=true;}
    public static class Time{public static float time,deltaTime=.02f,fixedDeltaTime=.02f;public static double timeAsDouble=>time;}
    public static class Debug{public static void LogError(string message,Object context){throw new Exception(message);}}
}
namespace Plugin.PlayerCore
{
    using UnityEngine;
    public enum PlayerMode{Hook,Attack}
    public enum PlayerStateId{Idle,Run,Jump,Fall,WallSlide,WallJump,Dash,GrappleAim,GrappleDash,Attack,Throw,LaserCharge,LaserFire,Hurt,Dead}
    public class PlayerModeController:MonoBehaviour{public PlayerMode Mode;public event Action<PlayerMode> Changed;public void Set(PlayerMode mode){Mode=mode;Changed?.Invoke(mode);}}
    public class PlayerInputReader:MonoBehaviour{public bool Ready=true,Paused;public Vector2 Pointer;public readonly PlayerInputBuffer Buffer=new PlayerInputBuffer();}
    public class PlayerStateMachine:MonoBehaviour
    {
        public PlayerStateId State=PlayerStateId.Idle;public Vector2 Velocity;public bool ArrivalJumpAvailable;public int Dashes;public float RetainedDistance;
        public event Func<PlayerCommand,PlayerStateId,bool> ActionRequested;public event Action<float> ExternalStep;public event Action<PlayerStateId,PlayerStateId> StateChanged;
        void Change(PlayerStateId next){var previous=State;State=next;StateChanged?.Invoke(previous,next);}
        public bool BeginExternal(PlayerStateId state){if(State==PlayerStateId.Dead||State==PlayerStateId.Hurt)return false;Change(state);return true;}
        public void SetExternalVelocity(Vector2 speed){Velocity=speed;}
        public void FinishExternal(Vector2 launch,bool reset){if(State==PlayerStateId.Dead||State==PlayerStateId.Hurt)return;Velocity=launch;if(reset){ArrivalJumpAvailable=true;Dashes=1;}Change(launch.y>0?PlayerStateId.Jump:PlayerStateId.Fall);}
        public bool TryExternalDash(){if(Dashes<=0)return false;Dashes--;Change(PlayerStateId.Dash);Velocity=Vector2.right*22;return true;}
        public void Hurt(){Change(PlayerStateId.Hurt);}public void Die(){Change(PlayerStateId.Dead);}
        public void Tick(float dt){var input=GetComponent<PlayerInputReader>();if(input.Paused||!Application.isFocused||!input.Ready)return;Time.time+=dt;Time.deltaTime=Time.fixedDeltaTime=dt;ExternalStep?.Invoke(dt);GetComponent<Rigidbody2D>().position+=Velocity*dt;}
        public bool Primary(){if(ActionRequested==null)return false;foreach(Func<PlayerCommand,PlayerStateId,bool> f in ActionRequested.GetInvocationList())if(f(PlayerCommand.Primary,GetComponent<PlayerModeController>().Mode==PlayerMode.Hook?PlayerStateId.GrappleAim:PlayerStateId.Attack))return true;return false;}
    }
}
