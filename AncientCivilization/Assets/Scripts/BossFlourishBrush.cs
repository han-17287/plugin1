using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 월드 접점을 축으로 작은 도형을 그린다. 모든 보스가 같은 꼭짓점 버퍼를 재사용한다.
    internal sealed class BossFlourishBrush
    {
        internal readonly List<Vector3> Positions = new List<Vector3>(8192);
        internal readonly List<Color32> Tints = new List<Color32>(8192);
        internal readonly List<int> Indices = new List<int>(12288);
        Vector3 origin, xAxis, yAxis;
        float opacity;

        internal void BeginFrame()
        {
            Positions.Clear(); Tints.Clear(); Indices.Clear();
        }

        internal void BeginStamp(Vector3 point, Vector3 right, Vector3 up, float alpha)
        { origin = point; xAxis = right; yAxis = up; opacity = Mathf.Clamp01(alpha); }

        void Vertex(Vector2 point, Color32 color)
        { Positions.Add(origin + xAxis*point.x + yAxis*point.y); Tints.Add(color); }

        Color32 Tint(Color color) { color.a *= opacity; return color; }

        internal void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            if (color.a*opacity < .004f) return;
            int i = Positions.Count;
            var tint = Tint(color);
            Vertex(a,tint); Vertex(b,tint); Vertex(c,tint);
            Indices.Add(i); Indices.Add(i+1); Indices.Add(i+2);
        }

        void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int i = Positions.Count;
            var tint = Tint(color);
            Vertex(a,tint); Vertex(b,tint); Vertex(c,tint); Vertex(d,tint);
            Indices.Add(i); Indices.Add(i+1); Indices.Add(i+2);
            Indices.Add(i); Indices.Add(i+2); Indices.Add(i+3);
        }

        internal void Stroke(Vector2 a, Vector2 end, float width, Color color)
        {
            var delta = end-a;
            if (width <= 0 || delta.sqrMagnitude < .0000001f || color.a*opacity < .004f) return;
            var across = new Vector2(-delta.y, delta.x) * (width*.5f / delta.magnitude);
            Quad(a+across, end+across, end-across, a-across, color);
        }

        internal void Splinter(Vector2 tail, Vector2 tip, float halfWidth, Color color)
        {
            var delta = tip-tail;
            if (halfWidth <= 0 || delta.sqrMagnitude < .0000001f || color.a*opacity < .004f) return;
            var across = new Vector2(-delta.y, delta.x) * (halfWidth / delta.magnitude);
            var shoulder = tail+delta*.29f;
            Quad(tail, shoulder+across, tip, shoulder-across, color);
        }

        // 연속된 부채꼴 단면을 공유해 굵은 호의 이음새가 벌어지지 않게 한다.
        internal void Arc(Vector2 center, float radius, float startDegrees, float sweepDegrees,
            float width, Color color, int segments = 12)
        {
            if (radius <= 0 || width <= 0 || Mathf.Abs(sweepDegrees)<.01f || color.a*opacity < .004f) return;
            segments = Mathf.Clamp(segments, 2, 48);
            float step = sweepDegrees * Mathf.Deg2Rad / segments;
            float cs = Mathf.Cos(step), sn = Mathf.Sin(step), start = startDegrees * Mathf.Deg2Rad;
            var axis = new Vector2(Mathf.Cos(start), Mathf.Sin(start));
            var tint = Tint(color);
            float inner = Mathf.Max(0,radius-width*.5f), outer = radius+width*.5f;
            int i = Positions.Count;
            for (int j = 0; j <= segments; j++, i+=2)
            {
                Vertex(center+axis*inner,tint); Vertex(center+axis*outer,tint);
                if (j < segments)
                {
                    Indices.Add(i); Indices.Add(i+1); Indices.Add(i+3);
                    Indices.Add(i); Indices.Add(i+3); Indices.Add(i+2);
                }
                axis = new Vector2(axis.x*cs-axis.y*sn,axis.x*sn+axis.y*cs);
            }
        }

        internal void Halo(Vector2 center, float radiusX, float radiusY, Color color)
        {
            if (color.a*opacity < .004f || radiusX <= 0 || radiusY <= 0) return;
            const int sides = 16;
            int first = Positions.Count;
            Color32 centre = Tint(color), edge = centre;
            edge.a = 0;
            Vertex(center,centre);
            var axis = Vector2.right;
            for (int j = 0; j < sides; j++)
            {
                Vertex(center + new Vector2(axis.x*radiusX,axis.y*radiusY),edge);
                Indices.Add(first); Indices.Add(first+j+1); Indices.Add(first+1+(j+1)%sides);
                axis = new Vector2(axis.x*.9238795f-axis.y*.3826834f,axis.x*.3826834f+axis.y*.9238795f);
            }
        }
    }
}
