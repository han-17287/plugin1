using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Collections
{
    public enum Allocator { Temp, Persistent }
    public enum NativeArrayOptions { UninitializedMemory }
    public static class Allocations { public static int Live; }
    public struct NativeArray<T> : IDisposable where T : struct
    {
        internal T[] data;
        public bool IsCreated => data != null;
        public NativeArray(int count, Allocator allocator, NativeArrayOptions options)
        { data = new T[count]; Allocations.Live++; }
        public NativeArray(T[] items, Allocator allocator) { data = items; Allocations.Live++; }
        public T this[int i] { get => data[i]; set => data[i] = value; }
        public void Dispose() { data = null; Allocations.Live--; }
    }
}

namespace UnityEngine
{
    public struct Bounds
    {
        readonly Vector3 center, size;
        public Bounds(Vector3 center, Vector3 size) { this.center = center; this.size = size; }
        public bool Contains(Vector3 p) => Math.Abs(p.x-center.x)<=size.x*.5f &&
            Math.Abs(p.y-center.y)<=size.y*.5f && Math.Abs(p.z-center.z)<=size.z*.5f;
    }
    public partial class SpriteRenderer
    {
        public Bounds localBounds { get; set; }
        public void ResetLocalBounds() { localBounds = default; }
    }
    public struct Rect { public float x, y, width, height; public Rect(float x, float y, float w, float h)
        { this.x=x; this.y=y; width=w; height=h; } }
    public enum SpriteMeshType { FullRect }
    public class Texture2D : Object { public int width = 870, height = 327; }
    public static class Resources
    {
        static readonly Texture2D native = new Texture2D();
        public static T Load<T>(string path) where T : Object => path == "Art/FieldGuardSkin" ? native as T : null;
    }
    public partial class Sprite
    {
        public string name;
        public float pixelsPerUnit = 185;
        public Vector2[] rest, uv;
        public Vector3[] positions;
        public ushort[] triangles;
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float ppu, uint extrude, SpriteMeshType type)
        {
            if (rect.width!=435 || rect.height!=327 || pivot.x!=.5f || pivot.y!=0 || ppu!=185)
                throw new InvalidOperationException("원본 픽셀 밀도 또는 발 고정점이 바뀌었습니다.");
            return new Sprite { pixelsPerUnit = ppu };
        }
        public void OverrideGeometry(Vector2[] p, ushort[] t) { rest=p; triangles=t; }
    }
    public static partial class Mathf
    {
        public static float Repeat(float t, float length) => t-MathF.Floor(t/length)*length;
        public static int Min(int a, int b) => Math.Min(a,b);
    }
}

namespace UnityEngine.Rendering { public enum VertexAttribute { Position, TexCoord0 } }
namespace UnityEngine.U2D
{
    public static class SpriteDataAccessExtensions
    {
        public static void SetVertexAttribute<T>(this Sprite s,VertexAttribute attr,Unity.Collections.NativeArray<T> a) where T:struct
        {
            if (attr==VertexAttribute.Position) s.positions=(Vector3[])(object)a.data;
            else s.uv=(Vector2[])(object)a.data;
        }
    }
}
namespace Plugin.AncientCivilization
{
    internal static class AncientArt
    {
        public static Sprite Slice(string sheet,string key,Rect rect,float ppu,Vector2 pivot,bool trim)
        { throw new InvalidOperationException("LANCER가 저해상도 다른 액터 시트를 사용했습니다."); }
    }
}
