using System;
using System.Collections.Generic;

// 실제 플러그 컨트롤러에 키 에지, 벽·천장 평면, 수동 물리 적분을 공급한다.
namespace UnityEngine
{
    public class Object
    {
        public static implicit operator bool(Object value) => value != null;
        public static bool operator !(Object value) => value == null;
    }

    public class GameObject
    {
        readonly Dictionary<Type, Component> components = new();
        public Transform transform = new Transform();
        public T AddComponent<T>() where T : Component, new()
        {
            var item = Add<T>();
            typeof(T).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.Invoke(item,null);
            typeof(T).GetMethod("OnEnable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.Invoke(item,null);
            return item;
        }
        public T Add<T>() where T : Component, new()
        {
            var item = new T { gameObject = this };
            components[typeof(T)] = item;
            return item;
        }
        public bool TryGet<T>(out T value) where T : Component
        {
            value = components.TryGetValue(typeof(T), out Component item) ? (T)item : null;
            return value != null;
        }
    }

    public sealed class Transform { public Vector2 position; }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component => gameObject.TryGet<T>(out var item) ? item : null;
        public bool TryGetComponent<T>(out T item) where T : Component => gameObject.TryGet(out item);
    }
    public class MonoBehaviour : Component
    {
        public bool enabled = true;
        public bool isActiveAndEnabled => enabled;
        protected static void Destroy(Object value) { }
    }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : new() => new T(); }

    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.Class)] public sealed class RequireComponent : Attribute { public RequireComponent(Type first, Type second) { } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) { } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute : Attribute { public string menuName; }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string name) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute { public MinAttribute(float min) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => default;
        public static Vector2 up => new(0, 1);
        public static Vector2 right => new(1, 0);
        public float sqrMagnitude => x * x + y * y;
        public float magnitude => MathF.Sqrt(sqrMagnitude);
        public Vector2 normalized => magnitude > .00001f ? this / magnitude : zero;
        public static Vector2 operator -(Vector2 a) => new(-a.x,-a.y);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float b) => new(a.x * b, a.y * b);
        public static Vector2 operator /(Vector2 a, float b) => new(a.x / b, a.y / b);
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a + (b - a) * t;
    }
    public static class Mathf
    {
        public const float PI = MathF.PI;
        public static int FloorToInt(float x) => (int)MathF.Floor(x);
        public static int Max(int a,int b) => Math.Max(a,b);
        public static int Min(int a,int b) => Math.Min(a,b);
        public static int Clamp(int a,int b,int c) => Math.Clamp(a,b,c);
        public static float Sign(float x) => x>=0 ? 1 : -1;
        public const float Rad2Deg = 180 / MathF.PI;
        public const float Deg2Rad = MathF.PI / 180;
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
        public static float Clamp01(float value) => Clamp(value, 0, 1);
        public static float Abs(float value) => MathF.Abs(value);
        public static float Max(float a, float b) => MathF.Max(a, b);
        public static float Min(float a, float b) => MathF.Min(a, b);
        public static float Cos(float value) => MathF.Cos(value);
        public static float Sin(float value) => MathF.Sin(value);
        public static float Atan2(float y, float x) => MathF.Atan2(y, x);
        public static float LerpAngle(float from, float to, float t)
        {
            float delta = to - from;
            delta -= MathF.Floor(delta / 360) * 360;
            return from + (delta > 180 ? delta - 360 : delta) * Clamp01(t);
        }
    }
    public struct LayerMask { public static implicit operator LayerMask(int value) => default; }
    public struct Bounds { public Vector2 min, max; }
    public class Collider2D : Component { public bool usedByEffector; public Bounds bounds; }
    public class CapsuleCollider2D : Collider2D { }
    public class PlatformEffector2D : Component { public bool useOneWay; }
    public struct ContactFilter2D { public bool useTriggers; public void SetLayerMask(LayerMask mask) { } }
    public struct RaycastHit2D { public Vector2 point; public static implicit operator bool(RaycastHit2D h)=>h.collider; public Collider2D collider; public Vector2 normal; public float distance; }
    public class Rigidbody2D : Component
    {
        public Vector2 position, linearVelocity;
        public int Cast(Vector2 direction, ContactFilter2D filter, RaycastHit2D[] hits, float distance) =>
            Physics2D.Cast(position, .36f, direction, hits, distance);
    }
    public static class Physics2D
    {
        public static RaycastHit2D Raycast(Vector2 origin,Vector2 dir,float length,LayerMask solid)=>default;
        public static float WallX = float.NaN;
        public static float WallTop = float.PositiveInfinity;
        public static float CeilingY = float.NaN;
        public static readonly Collider2D Wall = new GameObject().Add<Collider2D>();
        static readonly Collider2D Ceiling = new GameObject().Add<Collider2D>();
        public static int CircleCast(Vector2 origin, float radius, Vector2 direction, ContactFilter2D filter,
            RaycastHit2D[] hits, float distance) => Cast(origin, radius, direction, hits, distance);
        public static int Raycast(Vector2 origin, Vector2 direction, ContactFilter2D filter,
            RaycastHit2D[] hits, float distance) => Cast(origin, 0, direction, hits, distance);
        public static int Cast(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] hits, float distance)
        {
            int count = 0;
            if (direction.y > .00001f && CeilingY >= origin.y &&
                CeilingY - origin.y <= radius + distance * direction.y)
                hits[count++] = new RaycastHit2D { collider = Ceiling, normal = new Vector2(0, -1),
                    distance = MathF.Max(0, (CeilingY - radius - origin.y) / direction.y) };
            if (!float.IsNaN(WallX) && MathF.Abs(direction.x) >= .00001f)
            {
                float side = MathF.Sign(direction.x);
                float separation = (WallX - origin.x) * side;
                float travel = MathF.Max(0, (separation - radius) / MathF.Abs(direction.x));
                if (separation >= 0 && travel <= distance && origin.y + travel * direction.y - radius <= WallTop)
                    hits[count++] = new RaycastHit2D { collider = Wall, normal = new Vector2(-side, 0),
                        distance = travel };
            }
            return count;
        }
    }
    public static class Resources { public static T Load<T>(string path) where T : Object => null; }
    public class Camera : Object { public Vector2 ScreenToWorldPoint(Vector2 point) => point; }
    public static class Time { public static float time => frameCount * fixedDeltaTime; public static float deltaTime => fixedDeltaTime; public static int frameCount; public static double timeAsDouble => frameCount; public const float fixedDeltaTime = 1f / 60; }
    public static class Application { public static bool isFocused = true; }
}

namespace UnityEngine.InputSystem
{
    public sealed class Key
    {
        public bool isPressed, wasPressedThisFrame;
        public void Sample(bool pressed)
        {
            wasPressedThisFrame = pressed && !isPressed;
            isPressed = pressed;
        }
    }
    public sealed class Keyboard
    {
        public static Keyboard current;
        public readonly Key gKey = new(), cKey = new(), vKey = new();
    }
}

namespace Plugin.PlayerCore
{
    using UnityEngine;

    public enum PlayerStateId { Idle, Run, Jump, Fall, WallSlide, WallJump, Dash, GrappleAim, GrappleDash, Attack, Throw, LaserCharge, LaserFire, Hurt, Dead, WallRest }

    public sealed class SummonedBlade : MonoBehaviour
    {
        public BluePlugSocket Handle;
        public SummonedBladeData Data = new();
        Func<int,bool> spend;
        public void Configure(Camera view,Func<int> balance,Func<int,bool> spend,Action<int> grant,
            Action<Vector2,float,float,SummonedBladeHit> area,Action<float> shield,Action<string> notice,Action<int> refund=null)
            => this.spend=spend;
        public void Tick(float dt){}
        public bool TrySummon()
        {
            if (State!=SummonedBladeState.None || !spend(Data.summonCost)) return false;
            State=SummonedBladeState.Summoning;return true;
        }
        public bool TryDetach(){if (!Equipped) return false; State=SummonedBladeState.Stuck;return true;}
        public bool TryOverload(){if (!Equipped) return false; State=SummonedBladeState.Cooldown;return true;}
        public void ResetEncounter()=>State=SummonedBladeState.None;
        public SummonedBladeCarry LeaveRoom()
        {
            var carry=new SummonedBladeCarry{equipped=Equipped,cooldown=State==SummonedBladeState.Cooldown?12:0};
            ResetEncounter();return carry;
        }
        public void EnterRoom(SummonedBladeCarry carry)=>State=carry.equipped ? SummonedBladeState.Equipped : carry.cooldown>0 ? SummonedBladeState.Cooldown : SummonedBladeState.None;
        public void SetEquippedPose(Vector2 left,Vector2 right,Vector2 tip,bool show){}
        public uint HandleGeneration;
        public SummonedBladeState State;
        public bool Equipped => State==SummonedBladeState.Equipped;
        public bool Catchable => State==SummonedBladeState.Falling || State==SummonedBladeState.Stuck;
        public bool Perfect; public float Distance;
        public bool Supplies(Vector2 point) => State==SummonedBladeState.Stuck && Handle && Vector2.Distance(point,Handle.Point)<2;
        public bool TryEquip(float distance,bool falling)
        {
            if (!Catchable) return false;
            Distance=distance; Perfect=falling; State=SummonedBladeState.Equipped;
            return true;
        }
    }
    public sealed class FilamentStrikeFX : MonoBehaviour
    {
        public void Clear(){} public void Aim(Vector2 from,Vector2 to,float fill){} public void StopAim(){}
        public void Laser(Vector2 from,Vector2 to,float width,float power,float duration){}
        public int Starts, Swishes, Contacts;
        public void BeginBlade(int step){Starts++;}
        public void SwingSound(){Swishes++;}
        public void BladeContact(Vector2 p,Vector2 d,bool sound){Contacts++;} public void Spark(Vector2 p,Vector2 d){}
    }
    public sealed class ElectricPlugCombat : MonoBehaviour
    {
        public static ElectricPlugCombat Attach(GameObject owner,BluePlugData data) => owner.Add<ElectricPlugCombat>();
        public int Hits; public float LastDamage; public Vector2 LastBladeContact; public bool MeleeLands=true;
        public bool Melee(Vector2 p,float radius,float damage){Hits++;LastDamage=damage;LastBladeContact=p;return MeleeLands;}
        public void Area(Vector2 p,float r,float d,SummonedBladeHit kind){}
        public void DamageBeam(Vector2 start,Vector2 end,float radius,float damage){LastDamage=damage;}
        public void Spin(Vector2 hand,Vector2 tip) {}
        public void Tick(float dt) {}
        public void Dock(BluePlugSocket socket) {}
        public void Arrive(Vector2 point) {}
        public void ResetCombat() {}
        public bool TryAim(Vector2 hand,Vector2 aim,float reach,float radius,out Vector2 target) {target=aim;return false;}
        public bool ThrowHit(Vector2 from,Vector2 to,out Vector2 point){point=to;return false;}
    }
    public enum PlayerMode { Hook, Attack }
    public class PlayerModeController : MonoBehaviour
    {
        public PlayerMode Mode { get; private set; }
        public event Action<PlayerMode> Changed;
        public void Set(PlayerMode next)
        {
            if (Mode == next) return;
            Mode = next;
            Changed?.Invoke(next);
        }
    }
    public class PlayerInputReader : MonoBehaviour
    {
        public bool Ready = true, Paused, PrimaryHeld, PrimaryPressed, PrimaryReleased, CollectHeld;
        public Vector2 Move, Pointer;
        public readonly PlayerInputBuffer Buffer = new();
    }
    public class PlayerStateMachine : MonoBehaviour
    {
        public PlayerStateId State { get; private set; }
        public sealed class Tuning { public LayerMask solidLayers; }
        public Tuning data=new(); public void Protect(float duration) {}
        public float Facing = 1;
        public int VelocityWrites, Exits;
        public bool ResetAirActions;
        Rigidbody2D Body => GetComponent<Rigidbody2D>();
        public event Action<float> ExternalStep;
        public event Action<PlayerStateId, PlayerStateId> StateChanged;
        public event Action Respawned;
        void Change(PlayerStateId next)
        {
            var previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }
        public bool BeginWallRest(int outwardSign)
        {
            if (State > PlayerStateId.WallJump) return false;
            Facing = -outwardSign;
            Change(PlayerStateId.WallRest);
            SetExternalVelocity(Vector2.zero);
            return true;
        }
        public void LeaveWallRest(int jumpSign, Vector2 velocity)
        {
            if (State != PlayerStateId.WallRest) return;
            if (jumpSign == 0) FinishExternal(velocity, false);
            else
            {
                SetExternalVelocity(velocity + new Vector2(10 * jumpSign, 13));
                Change(PlayerStateId.WallJump);
            }
        }
        public bool TryExternalDash()
        {
            if (State != PlayerStateId.WallRest) return false;
            Change(PlayerStateId.Dash);
            return true;
        }
        public bool BeginExternal(PlayerStateId next)
        {
            if (State is PlayerStateId.Hurt or PlayerStateId.Dead) return false;
            Change(next);
            return true;
        }
        public void SetExternalVelocity(Vector2 velocity) { VelocityWrites++; Body.linearVelocity = velocity; }
        public void FinishExternal(Vector2 launch, bool reset)
        {
            Exits++;
            ResetAirActions = reset;
            SetExternalVelocity(launch);
            Change(launch.y > 0 ? PlayerStateId.Jump : PlayerStateId.Fall);
        }
        public void Hurt(Vector2 knockback) { Body.linearVelocity = knockback; Change(PlayerStateId.Hurt); }
        public void Respawn(Vector2 position)
        {
            Body.position = position;
            Body.linearVelocity = Vector2.zero;
            Change(PlayerStateId.Fall);
            Respawned?.Invoke();
        }
        public void Tick()
        {
            if (State == PlayerStateId.GrappleDash || State == PlayerStateId.WallRest) ExternalStep?.Invoke(Time.fixedDeltaTime);
            Body.position += Body.linearVelocity * Time.fixedDeltaTime;
        }
    }
    public enum BluePlugSocketState { Idle, Ready, Blocked, Linked, Locked }
    public class BluePlugSocket : MonoBehaviour
    {
        public bool Operational => isActiveAndEnabled;
        public Collider2D WallFace;
        public int WallOutwardSign = -1;
        public bool CanWallRest => WallFace && isActiveAndEnabled;
        public bool PowersEquipment, PatrolDriven, Reserved, LastReleaseArrived;
        public int Releases;
        public float RangeOverride, TowSpeedOverride, TowCarryOverride = -1;
        public Vector2 Point, TowPoint, AnchorVelocity, EntryDirection = Vector2.right;
        public BluePlugSkill Supported = BluePlugSkill.Tow | BluePlugSkill.Swing;
        public BluePlugSocketState Indicator;
        public void SetIndicator(BluePlugSocketState state, BluePlugSkill unlocked) => Indicator = state;
        public float Range(float fallback) => RangeOverride > 0 ? RangeOverride : fallback;
        public float TowSpeed(float fallback) => TowSpeedOverride > 0 ? TowSpeedOverride : fallback;
        public float TowCarry(float fallback) => TowCarryOverride >= 0 ? TowCarryOverride : fallback;
        public bool Reserve()
        {
            if (!Operational || Reserved) return false;
            Reserved = true;
            return true;
        }
        public void Release(bool arrived)
        {
            if (!Reserved) throw new InvalidOperationException("Release without a reservation");
            Reserved = false;
            Releases++;
            LastReleaseArrived = arrived;
        }
        public void Move(Vector2 displacement)
        {
            Point += displacement;
            TowPoint += displacement;
        }
    }
    public class BluePlugVisuals : MonoBehaviour
    {
        public int Clicks, Arrivals, FeedbackResets;
        public BluePlugPhase VisiblePhase;
        public float Charge, Range, OrbitHeight = 1;
        public Vector2 Radial, Hand, PhysicalTip, DrawnTip, PinDirection;
        public Vector2? WindCenter, WindingGrip;
        public Vector2 ProjectOrbit(Vector2 center, Vector2 tip)
        {
            PhysicalTip = tip;
            return new Vector2(tip.x, center.y + (tip.y - center.y) * OrbitHeight);
        }
        public void Show(Vector2 hand, Vector2 plug, Vector2 direction, BluePlugPhase phase, Vector2? windCenter = null,
            float charge = 0, float range = 0, Vector2? windingGrip = null)
        {
            Radial = plug - (windCenter ?? hand);
            DrawnTip = plug;
            PinDirection = direction;
            Hand = hand;
            WindCenter = windCenter;
            WindingGrip = windingGrip;
            VisiblePhase = phase;
            Charge = charge;
            Range = range;
        }
        public void Click() => Clicks++;
        public void Arrival(Vector2 point) => Arrivals++;
        public void ResetFeedback() => FeedbackResets++;
    }
}
