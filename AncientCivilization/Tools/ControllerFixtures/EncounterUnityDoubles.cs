// Shared math and presentation doubles for portable encounter checks.
// Positions and line output are captured; there is no native renderer or physics scene.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object { public static void Destroy(Object obj){} public static implicit operator bool(Object value){return value!=null;} }
    public class Component : Object {public Transform transform=new Transform();public GameObject gameObject;}
    public class MonoBehaviour : Component {}
    public class GameObject : Object
    {
        readonly Dictionary<Type,Component> parts=new Dictionary<Type,Component>();
        public Transform transform=new Transform();public GameObject(string name){transform.gameObject=this;}
        public int layer;
        public bool activeSelf=true;
        public void SetActive(bool value){activeSelf=value;}
        public T AddComponent<T>() where T:Component,new()
        {
            var part=new T{gameObject=this,transform=transform};
            parts[typeof(T)]=part;
            return part;
        }
        public T GetComponent<T>() where T:Component => parts.TryGetValue(typeof(T),out var part)?(T)part:null;
    }
    public class Transform : Object
    {
        public GameObject gameObject;Transform parent;Vector3 local;
        public Vector3 localPosition {get{return local;}set{local=value;}}
        public Vector3 position {get{return parent?parent.position+local:local;}set{local=parent?value-parent.position:value;}}
        public Vector3 localScale=Vector3.one;public Quaternion localRotation,rotation;
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
        public static float Dot(Vector2 a,Vector2 b)=>a.x*b.x+a.y*b.y;
        public static Vector2 MoveTowards(Vector2 a,Vector2 b,float step)=>(b-a).magnitude<=step?b:a+(b-a).normalized*step;
        public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>a+(b-a)*Mathf.Clamp01(t);
        public static implicit operator Vector3(Vector2 a)=>new Vector3(a.x,a.y,0);
        public static implicit operator Vector2(Vector3 a)=>new Vector2(a.x,a.y);
    }
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);
        public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
    }
    public struct Quaternion {public static Quaternion identity=>new Quaternion(); public static Quaternion Euler(float x,float y,float z)=>new Quaternion();}
    public struct Color
    {
        public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static Color white=>new Color(1,1,1);
        public static Color Lerp(Color a,Color b,float t)=>new Color(Mathf.Lerp(a.r,b.r,t),Mathf.Lerp(a.g,b.g,t),Mathf.Lerp(a.b,b.b,t),Mathf.Lerp(a.a,b.a,t));
    }
    public static class Mathf
    {
        public const float PI=(float)Math.PI,Deg2Rad=PI/180,Rad2Deg=180/PI;
        public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static int CeilToInt(float f)=>(int)Math.Ceiling(f);
        public static int FloorToInt(float f)=>(int)Math.Floor(f);
        public static int Clamp(int value,int min,int max)=>Math.Max(min,Math.Min(max,value));
        public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(b,x));public static float Clamp01(float x)=>Clamp(x,0,1);
        public static float InverseLerp(float a,float b,float value)=>a==b?0:Clamp01((value-a)/(b-a));
        public static float Abs(float x)=>Math.Abs(x);public static float Sign(float x)=>x<0?-1:1;
        public static float Sin(float x)=>(float)Math.Sin(x);public static float Cos(float x)=>(float)Math.Cos(x);
        public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
        public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return Lerp(a,b,t*t*(3-2*t));}
        public static float MoveTowards(float a,float b,float step)=>Abs(b-a)<=step?b:a+Sign(b-a)*step;
    }
    public static class Time {public static int frameCount; public static float time,deltaTime,unscaledDeltaTime,fixedDeltaTime=.02f;}
    public struct RaycastHit2D
    {
        public bool blocked;public Vector2 point;public Rigidbody2D rigidbody;
        public static implicit operator bool(RaycastHit2D value)=>value.blocked;
    }
    public static class Physics2D
    {
        public static bool Blocked;public static Vector2 HitPoint;
        public static Func<Vector2,Vector2,float,int,bool> RayHit;
        public static RaycastHit2D Raycast(Vector2 origin,Vector2 direction,float distance,int mask)=>new RaycastHit2D{blocked=RayHit!=null&&RayHit(origin,direction,distance,mask)};
        public static RaycastHit2D Linecast(Vector2 a,Vector2 b,int mask)=>new RaycastHit2D{blocked=Blocked,point=HitPoint};
    }
    public class BoxCollider2D:Component {public bool usedByEffector;public bool enabled=true;public Vector2 size;}
    public enum RigidbodyType2D {Kinematic}
    public enum RigidbodyInterpolation2D {Interpolate}
    public class Rigidbody2D:Component
    {
        public RigidbodyType2D bodyType;public RigidbodyInterpolation2D interpolation;
        public Vector2 linearVelocity;public Vector2 position {get=>transform.position;set=>transform.position=value;}
        public void MovePosition(Vector2 point)=>transform.position=point;
    }
    public enum KeyCode {R}
    public static class Input {public static bool Recall;public static bool GetKeyDown(KeyCode key)=>Recall;}
    public class Sprite : Object {}
    public class SpriteRenderer : Component {public Sprite sprite;public bool flipX;public bool enabled=true;public Color color=Color.white;}
    public enum LineAlignment { View }
    public enum LineTextureMode { Stretch }
    public class LineRenderer : Component
    {
        public bool useWorldSpace; public LineTextureMode textureMode; public LineAlignment alignment;
        readonly Vector3[] points=new Vector3[64];public int positionCount;public bool enabled,loop;public Color startColor,endColor;public float startWidth,endWidth;
        public void SetPosition(int index,Vector3 point){points[index]=point;}public Vector3 GetPosition(int index)=>points[index];
    }
}
