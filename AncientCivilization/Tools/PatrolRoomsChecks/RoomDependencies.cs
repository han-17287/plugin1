using System;
using UnityEngine;

namespace UnityEngine
{
    public sealed class SerializeField : Attribute { }

    public static partial class Mathf
    {
        public static int Min(int a,int b) => Math.Min(a,b);
        public static float Repeat(float t,float length) => t-MathF.Floor(t/length)*length;
        public static float PingPong(float t,float length) => length-MathF.Abs(Repeat(t,2*length)-length);
        public static float LerpUnclamped(float a,float b,float t) => a+(b-a)*t;
        public static float MoveTowardsAngle(float a,float b,float step)
        {
            float delta=Repeat(b-a+180,360)-180;
            return MoveTowards(a,a+delta,step);
        }
        public static float Atan(float a) => MathF.Atan(a);
        public static float Exp(float a) => MathF.Exp(a);
        public static float Pow(float a,float b) => MathF.Pow(a,b);
        public static int RoundToInt(float a) => (int)MathF.Round(a);
    }

    public partial struct Quaternion
    {
        public static Vector3 operator *(Quaternion q,Vector3 v)
        {
            float r=q.ZDegrees*Mathf.Deg2Rad, c=MathF.Cos(r), s=MathF.Sin(r);
            return new Vector3(c*v.x-s*v.y,s*v.x+c*v.y,v.z);
        }
    }

    public partial class Transform
    {
        public Vector3 TransformPoint(Vector3 v) => position+v;
        public Vector3 InverseTransformPoint(Vector3 v) => v-position;
    }

    public struct Bounds { public Vector3 size; }
    public partial class Sprite { public Bounds bounds = new Bounds {size=new Vector3(6,3,1)}; }
    public partial class SpriteRenderer { public Vector2 size; }
    public enum RigidbodyConstraints2D { FreezeRotation }
    public enum CollisionDetectionMode2D { Continuous }

    public partial class Rigidbody2D
    {
        public float gravityScale,mass;
        public RigidbodyConstraints2D constraints;
        public CollisionDetectionMode2D collisionDetectionMode;
    }

    public sealed class AudioClip : Object
    {
        public int frequency; public float[] Pcm;
        public static AudioClip Create(string name,int length,int channels,int rate,bool stream) => new AudioClip { frequency=rate };
        public void SetData(float[] samples,int offset) { Pcm=samples; }
    }

    public sealed class AudioSource : Component
    {
        public object outputAudioMixerGroup;
        public bool playOnAwake,loop,isPlaying;
        public int priority;
        public float volume,dopplerLevel,spatialBlend;
        public AudioClip clip;
        public void Play() { isPlaying=clip; }
        public void Pause() { isPlaying=false; }
        public void Stop() { isPlaying=false; }
        public void UnPause() { isPlaying=clip; }
    }

    public static class Resources { public static T Load<T>(string path) where T:Object,new() => new T(); }
}

namespace Plugin.AncientCivilization
{
    public sealed class AncientFeedbackProfile : UnityEngine.Object { public object music=new object(),effects=new object(); }
    public static class AncientGame { public static int Saved(string key) => 0; }
    public static class AncientCityLayout { public static string Name(int floor) => "층 "+floor; }
    public static class AncientLivingWallArt {public static Sprite Get(int style,int variation) => new Sprite();}
    public static class AncientArt
    {
        public static Sprite Citizen(int style)=>new Sprite();
        public static Sprite DistrictBackdrop(int floor)=>new Sprite();
        public static Sprite ProductionProp(int id)=>new Sprite();
        public static Sprite ProductionWorker(int id)=>new Sprite();
        public static Sprite Rooftop(float width,bool tall)=>new Sprite();
        public static SpriteRenderer Display(string label,Sprite sprite,Vector2 pos,int order,Transform parent)
        {
            var renderer = new GameObject(label).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent,false);
            renderer.transform.localPosition=pos;
            renderer.sprite=sprite;
            return renderer;
        }
    }
}

namespace Plugin.PlayerCore
{
    public sealed class BluePlugSocket : Component
    {
        Vector2 towOffset;
        public BoxCollider2D WallFace { get; private set; }
        public int WallOutwardSign { get; private set; }
        public bool CanWallRest => gameObject.activeSelf && WallFace && WallFace.enabled;
        public void ConfigureWallRest(BoxCollider2D face, int outwardSign)
        {
            WallFace = face;
            WallOutwardSign = outwardSign < 0 ? -1 : 1;
        }
        public Vector2 Point => transform.position;
        public Vector2 TowPoint => Point + towOffset;
        public bool PowersEquipment { get; private set; }
        public void MarkPowerOutlet() => PowersEquipment = true;
        public BluePlugSkill Supported { get; private set; }
        public Vector2 EntryDirection { get; private set; }
        public GrappleAnchor BoundAnchor { get; private set; }
        public bool Operational => gameObject.activeSelf && (!BoundAnchor || BoundAnchor.Available);

        public void BindAnchor(GrappleAnchor anchor)
        {
            BoundAnchor=anchor;
            Configure(Vector2.zero,BluePlugSkill.Tow | BluePlugSkill.Swing,Vector2.up);
        }

        public void Configure(Vector2 towOffset,BluePlugSkill supported,Vector2 entryDirection)
        {
            this.towOffset=towOffset;
            Supported=supported;
            EntryDirection=entryDirection.normalized;
        }
    }

    public sealed class GrappleAnchor : Component
    {
        public bool Available {get; private set;}=true;
        public void SetOperational(bool value) { Available=value; }
    }
    public enum PlayerMode { Hook, Attack }
    public sealed class PatrolTestPlayer
    {
        public PlayerMode Mode=PlayerMode.Hook;
        public float LastLift;
        public void ApplyEnvironmentalLift(float speed) { LastLift=speed; }
    }
    public sealed class PatrolTestExpedition : UnityEngine.Object
    {
        public bool Paused;
        public readonly PatrolTestPlayer Player = new PatrolTestPlayer();
    }
}
