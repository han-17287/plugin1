// Narrow scene/service doubles for running the shipping lower-route controller.
// Collision tests use a single explicit vertical-wall double, not Unity physics.
// No native input, rendering, frame order or player traversal is simulated.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object
    {
        public bool destroyed;public static implicit operator bool(Object x)=>x!=null&&!x.destroyed;
        public static void Destroy(Object x){if(x==null)return;x.destroyed=true;if(x is GameObject go)foreach(var c in go.components)c.destroyed=true;}
    }
    public class Component:Object {public Transform transform=new Transform();public GameObject gameObject;}
    public class MonoBehaviour:Component {public bool enabled=true;}
    public class GameObject:Object
    {
        public Transform transform=new Transform();public int layer;public bool active=true;public readonly List<Component> components=new List<Component>();
        public GameObject(string name){}
        public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this,transform=transform};components.Add(c);return c;}
        public T GetComponent<T>() where T:Component {foreach(var c in components)if(c is T t)return t;return null;}
        public void SetActive(bool value){active=value;}
    }
    public class Transform:Object
    {
        Transform parent;Vector3 local;public Vector3 localScale=Vector3.one;public Quaternion rotation;
        public Vector3 position {get=>parent?parent.position+local:local;set=>local=parent?value-parent.position:value;}
        public void SetParent(Transform p,bool worldPositionStays=true){var before=position;parent=p;if(worldPositionStays)position=before;}
    }
    public struct Vector2
    {
        public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}
        public static Vector2 zero=>new Vector2();public static Vector2 up=>new Vector2(0,1);public static Vector2 right=>new Vector2(1,0);public static Vector2 left=>new Vector2(-1,0);
        public float sqrMagnitude=>x*x+y*y;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector2 normalized=>sqrMagnitude>0?this/(float)Math.Sqrt(sqrMagnitude):zero;
        public static Vector2 operator+(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);public static Vector2 operator*(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);public static Vector2 operator/(Vector2 a,float b)=>new Vector2(a.x/b,a.y/b);
        public static implicit operator Vector3(Vector2 a)=>new Vector3(a.x,a.y,0);public static implicit operator Vector2(Vector3 a)=>new Vector2(a.x,a.y);
        public static float Dot(Vector2 a,Vector2 b)=>a.x*b.x+a.y*b.y;
        public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>a+(b-a)*Math.Clamp(t,0,1);
        public static Vector2 MoveTowards(Vector2 a,Vector2 b,float distance)=>(b-a).magnitude<=distance?b:a+(b-a).normalized*distance;
    }
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 one=>new Vector3(1,1,1);
        public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
    }
    public struct Color {public Color(float r,float g,float b,float a=1){}public static Color white=>new Color(1,1,1);}
    public struct Quaternion {public static Quaternion identity=>new Quaternion();public static Quaternion Euler(float x,float y,float z)=>new Quaternion();}
    public static class Mathf
    {
        public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Abs(float x)=>Math.Abs(x);public static float Clamp(float x,float a,float b)=>Math.Clamp(x,a,b);
        public const float PI=(float)Math.PI,Rad2Deg=180/PI;
        public static float Atan2(float a,float b)=>(float)Math.Atan2(a,b);public static float Sin(float x)=>(float)Math.Sin(x);public static float Cos(float x)=>(float)Math.Cos(x);
        public static float Clamp01(float x)=>Math.Clamp(x,0,1);public static float Sign(float x)=>x>=0?1:-1;
        public static int RoundToInt(float x)=>(int)Math.Round(x);
    }
    public class Sprite:Object {public int Id;public Sprite(int id=0){Id=id;}}
    public class SpriteRenderer:Component {public bool enabled=true,flipX;public Sprite sprite;public Color color;}
    public class LineRenderer:Component {public bool enabled;public int positionCount;public void SetPosition(int index,Vector3 p){}}
    public class BoxCollider2D:Component {public bool enabled=true;}
    public class Camera:Component {public float orthographicSize=5.8f,aspect=16/9f;public Vector3 WorldToScreenPoint(Vector3 p)=>p;public Vector3 ScreenToWorldPoint(Vector3 p)=>p;}
    public struct RaycastHit2D {public bool hit;public float fraction;public Vector2 point,normal;public static implicit operator bool(RaycastHit2D value)=>value.hit;}
    public static class Physics2D
    {
        public static bool Blocked;public static float WallX=float.NaN;
        public static RaycastHit2D Linecast(Vector2 a,Vector2 b,int mask)
        {
            float t=Blocked?0:float.IsNaN(WallX)||Math.Abs(b.x-a.x)<.00001f?-1:(WallX-a.x)/(b.x-a.x);
            return t>=0&&t<=1?new RaycastHit2D{hit=true,fraction=t,point=Vector2.Lerp(a,b,t),normal=Vector2.right*(b.x>a.x?-1:1)}:new RaycastHit2D();
        }
    }
    public static class Time {public static float time,deltaTime=.1f,unscaledDeltaTime=.1f;}
    public static class Application {public static bool isFocused=true;}
    public static class PlayerPrefs {public static int Flushes;public static void Save(){Flushes++;}}
    public static class Input {public static Vector3 mousePosition;}
    public struct Rect
    {
        public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
        public static Rect MinMaxRect(float x,float y,float right,float top)=>new Rect(x,y,right-x,top-y);public bool Overlaps(Rect r)=>false;
    }
    public class Font:Object{}public enum TextAnchor{MiddleCenter}
    public class GUIContent {public string text;}
    public class GUIStyle {public Font font;public int fontSize;public TextAnchor alignment;public bool wordWrap;public GUIStyle(){}public GUIStyle(GUIStyle s){}public float CalcHeight(GUIContent content,float width)=>fontSize*2;}
    public class GUISkin {public GUIStyle label=new GUIStyle();}
    public class Texture2D {public static readonly Texture2D whiteTexture=new Texture2D();}
    public static class GUI {public static GUISkin skin=new GUISkin();public static Color color;public static bool enabled=true;public static bool Button(Rect r,string text,GUIStyle s)=>false;public static void Label(Rect r,string text,GUIStyle s){}public static void DrawTexture(Rect r,Texture2D t){}}
    public static class Screen {public static int width=1280,height=720;}
    public enum EventType {Repaint,Layout}
    public class Event {public EventType type=EventType.Repaint;public static Event current=new Event();}
}
namespace Plugin.AncientCivilization
{
    using UnityEngine;
    public sealed class AncientGame:MonoBehaviour
    {
        public static readonly Dictionary<string,int> Prefs=new Dictionary<string,int>();public static readonly Color Cyan=new Color(.2f,.8f,1);
        public static int Saved(string key,int fallback=0)=>Prefs.TryGetValue(key,out int value)?value:fallback;public static void SetSaved(string key,int value){Prefs[key]=value;}
        public AncientHero hero=new AncientHero();public RouteStoryFixture Story=new RouteStoryFixture();public RouteCampaignFixture Campaign=new RouteCampaignFixture();public AncientLivingDistrict Living=new AncientLivingDistrict();public AncientLowerRoute LowerRoute;
        public Camera View=new Camera();public bool HasSession=true,MainMenu,GameplayInputReady=true,skills,ArenaActive;public static bool IsQAProfile;public bool Paused=>Story.DialogueOpen||MainMenu;
        public RouteCityFixture City=new RouteCityFixture();public RouteJournalFixture Journal=new RouteJournalFixture();public RouteProductionFixture Production=new RouteProductionFixture();
        public int credits,SaveCount,CameraSnaps;public readonly List<AncientEnemy> enemies=new List<AncientEnemy>();
        public GameObject Box(string name,Vector2 p,Vector2 size,Color color,int order){var o=new GameObject(name);o.transform.position=p;o.AddComponent<SpriteRenderer>();return o;}
        public LineRenderer Line(string name,Color color,float width,int order)=>new GameObject(name).AddComponent<LineRenderer>();
        public void StaticBox(string name,Vector2 point,Vector2 size,Color color,int order){}public void Platform(float a,float b,float height){}
        public void Save(){SaveCount++;}public void Sound(int id){}public void Notice(string text,float duration=2){}public void Impact(Vector2 p,Color c,bool heavy){}public void SnapCameraToHero(){CameraSnaps++;View.transform.position=(Vector3)hero.transform.position;}
    }
    public sealed class AncientHero:Object
    {
        public Transform transform=new Transform();public bool bike,IsOnGround=true;public float hp=30,MaxHP=100;public int TravelCount,ReleaseCount,HurtCount,Facing=1;public RouteCombatFixture Combat=new RouteCombatFixture();
        public Vector2 WireOrigin=>(Vector2)transform.position+Vector2.up;public void Hurt(float amount){hp-=amount;HurtCount++;}public void CinematicPose(bool bike,int face){}
        public void TravelTo(Vector2 p){transform.position=p;TravelCount++;}public void ReleaseWire(){ReleaseCount++;}
    }
    public sealed class RouteCombatFixture {public float Emitted;public int EmissionCount;public void EmitParticles(Vector2 p,float amount){Emitted+=amount;EmissionCount++;}public void CancelAttacks(){}}
    public sealed class RouteStoryFixture:Object
    {
        public int CurrentFloor;public bool DialogueOpen,LiftBusy;public Action Pending;public string[] LastLines,LastSpeakers;
        public void Conversation(Action done,int art,string[] people,string[] lines){Pending=done;LastLines=lines;LastSpeakers=people;DialogueOpen=true;}
        public void Finish(){var done=Pending;Pending=null;DialogueOpen=false;done?.Invoke();}
    }
    public sealed class RouteCampaignFixture:Object
    {public int CurrentRoom,CurrentSector,CompletedMask;public readonly int[] Tasks=new int[20];public int RecordedTasks(int id)=>Tasks[id];}
    public sealed class RouteCityFixture:Object {public int Cleared;}
    public sealed class RouteJournalFixture:Object {public bool Open;}
    public sealed class RouteProductionFixture:Object {public bool Active;}
    public sealed class AncientLivingDistrict:Object
    {public int ExistingGuards,RouteObjects,Throws,Released;public AncientLivingBoss Boss;public void AddRouteObject(Vector2 p,bool car){RouteObjects++;}public int NearbyGuards(float x)=>ExistingGuards;public void ReleaseObject(AncientWireObject obj){Released++;}public void RecordThrow(){Throws++;}}
    public sealed class AncientLivingBoss:Object
    {public Vector2 Core;public bool Alive=true,CanCatchRocket;public int Hits,Catches;public void ReceiveHit(AncientLivingHit hit,float amount){Hits++;}public void ShowCatch(){Catches++;}}
    public sealed class AncientEnemy:MonoBehaviour
    {public bool IsAlive=true,IsLivingBoss;public int Kind,Floor,Hits;public float Damage;public Vector2 AimPoint=>transform.position;public void Hit(float amount,bool heavy){Hits++;Damage+=amount;}public void Initialize(int kind){Kind=kind;}public void ConfigureDistrict(int floor,bool boss){Floor=floor;}}
    public static class AncientLivingArt {public static Sprite Part(int id)=>new Sprite(id);}
    public static class AncientRouteDeviceArt {public static Sprite Get(AncientRouteAction action)=>new Sprite((int)action);}
    public static class AncientArt
    {
        public static Sprite MetalProjectile(int i)=>new Sprite(i);public static Sprite World(int i)=>new Sprite(i);
        public static SpriteRenderer Display(string name,Sprite sprite,Vector2 p,int order,Transform parent)
        {var view=new GameObject(name).AddComponent<SpriteRenderer>();view.sprite=sprite;view.transform.SetParent(parent,false);view.transform.position=p;return view;}
    }
    public static class AncientCitizenArt {public static Sprite ForCitizen(int id)=>new Sprite(id);}
    public static class AncientKoreanUI {public static Font Font=new Font();}
    public static class AncientHUD {public static GUIStyle Button=new GUIStyle();}
}
