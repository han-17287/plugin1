using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace Plugin.AncientCivilization
{
    // LANCER(0)의 기존 투구·갑옷·망토·칼 픽셀을 그대로 쓰는 표시 전용 변형이다.
    // 두 UV 영역은 원화 한 장을 겹치지 않게 나눈 것이다. 같은 위치에 놓으면 원본과 일치한다.
    internal sealed class AncientGuardSkin : IDisposable
    {
        const int Width = 435, Height = 327;
        const int MotionMargin = 72;
        const float SourceLeft = 48, SourceBottom = 484, FootX = 265.5f;
        const float ElbowX = 322, ElbowY = 290;
        static MeshLayout shared;
        readonly SpriteRenderer art;
        readonly Sprite original;
        Sprite moving;
        bool expandedBounds;
        NativeArray<Vector3> positions;

        public AncientGuardSkin(SpriteRenderer art)
        {
            this.art = art;
            original = art.sprite;
        }

        struct Vertex
        {
            public Vector2 source;
            public float upper, foot, farLeg, cape, hinge;
            public bool arm;
            public Vertex(float x, float y, bool arm)
            {
                source = new Vector2(x, y);
                upper = 1 - Ramp(y, 210, 400);
                foot = Ramp(y, 332, 469);
                farLeg = 1 - Ramp(x, 227, 312);
                cape = Ramp(233 - x, 0, 155) * Ramp(y, 242, 292) * (1 - Ramp(y, 399, 441));
                hinge = Ramp(Vector2.Distance(source, new Vector2(ElbowX, ElbowY)), 15, 55);
                this.arm = arm;
            }
        }

        sealed class MeshLayout
        {
            public readonly Vertex[] points;
            public readonly Vector2[] pixels, uv;
            public readonly ushort[] triangles;

            public MeshLayout()
            {
                var verts = new List<Vertex>(1100);
                var rest = new List<Vector2>(1100);
                var coords = new List<Vector2>(1100);
                var indices = new List<ushort>(6000);
                // 머리·다리·망토는 한 면, 손잡이와 칼날은 단단한 두 번째 면이다.
                Grid(48, 157, 483, 484, 32, 24, false);
                Grid(310, 254, 483, 359, 20, 14, true);
                points = verts.ToArray(); pixels = rest.ToArray(); uv = coords.ToArray(); triangles = indices.ToArray();

                void Grid(float left, float top, float right, float bottom, int cols, int rows, bool arm)
                {
                    int start = verts.Count;
                    for (int iy = 0; iy <= rows; iy++)
                    for (int ix = 0; ix <= cols; ix++)
                    {
                        float x = Mathf.Lerp(left, right, (float)ix / cols);
                        float y = Mathf.Lerp(bottom, top, (float)iy / rows);
                        float px = x - SourceLeft, py = SourceBottom - y;
                        verts.Add(new Vertex(x, y, arm));
                        rest.Add(new Vector2(px, py));
                        coords.Add(new Vector2((px + (arm ? Width : 0)) / (Width * 2), py / Height));
                        if (ix == cols || iy == rows) continue;
                        ushort a = (ushort)(start + iy * (cols + 1) + ix);
                        ushort b = (ushort)(a + cols + 1);
                        indices.Add(a); indices.Add(b); indices.Add((ushort)(a + 1));
                        indices.Add((ushort)(a + 1)); indices.Add(b); indices.Add((ushort)(b + 1));
                    }
                }
            }
        }

        static float Ramp(float value, float lo, float hi)
        {
            float t = Mathf.Clamp01((value - lo) / (hi - lo));
            return t * t * (3 - 2 * t);
        }

        internal struct Pose
        {
            public float stride, backLift, frontLift, walk, lean, sink, cloak, sin, cos, elbowShift;

            public static Pose Sample(float gait, float stride01, bool aiming, float windup, float strikeAge, float kick, bool staggered)
            {
                var p = new Pose();
                float radians = stride01 * Mathf.PI * 2;
                p.walk = Mathf.Clamp01(gait);
                p.stride = Mathf.Sin(radians) * p.walk;
                float step = Mathf.Cos(radians) * p.walk;
                p.backLift = Mathf.Max(0, step); p.frontLift = Mathf.Max(0, -step);
                p.lean = 7 * p.walk; p.sink = -4 * Mathf.Abs(p.stride);
                p.cloak = -9 * p.walk - 5 * p.stride;
                float angle = -4 * p.walk + 10 * p.stride;
                if (staggered || kick > 0 && !aiming && strikeAge >= .33f)
                {
                    float fold = staggered ? .48f + .52f * kick : kick;
                    p.lean -= 19 * fold; p.sink += 8 * fold; angle += 19 * fold;
                }
                else if (aiming)
                {
                    // 첫 0.08초에 원래 칼을 세우고, 판정 직전에 앞쪽으로 베기 시작한다.
                    float draw = Ramp(windup, 0, .14f);
                    float turn = Ramp(windup, .82f, 1);
                    angle = Mathf.Lerp((47 + 22 * windup) * draw, -13, turn);
                    p.lean += draw * Mathf.Lerp(-13, 11, turn);
                    p.sink += 5 * draw;
                    p.cloak += draw * Mathf.Lerp(8, -9, turn);
                    p.elbowShift = draw * Mathf.Lerp(-5, 6, turn);
                }
                else if (strikeAge >= 0 && strikeAge < .33f)
                {
                    float settle = 1 - Ramp(strikeAge, .04f, .33f);
                    angle = -13 * settle - 8 * Mathf.Sin(Ramp(strikeAge, 0, .13f) * Mathf.PI);
                    p.lean += 11 * settle; p.sink += 5 * settle;
                    p.cloak -= 9 * settle; p.elbowShift = 6 * settle;
                }
                p.sin = Mathf.Sin(angle * Mathf.Deg2Rad); p.cos = Mathf.Cos(angle * Mathf.Deg2Rad);
                return p;
            }
        }

        static Vector2 Deform(Vertex v, Pose pose)
        {
            Vector2 at = v.source;
            float far = v.farLeg;
            at.x += pose.lean * v.upper + pose.cloak * v.cape;
            at.y += pose.sink * (1 - v.foot) - Mathf.Abs(pose.cloak) * .20f * v.cape;
            at.x += v.foot * ((far * 2 - 1) * (pose.walk * 14 + pose.stride * 26));
            at.y -= v.foot * (far * pose.backLift + (1 - far) * pose.frontLift) * 21;
            if (v.arm)
            {
                // 칼날과 손잡이는 하나의 강체 회전이다. 경첩 바로 옆에서만 부드럽게 보간한다.
                float dx = v.source.x - ElbowX, dy = v.source.y - ElbowY;
                float armX = ElbowX + pose.cos * dx + pose.sin * dy + pose.lean * .55f + pose.elbowShift;
                float armY = ElbowY - pose.sin * dx + pose.cos * dy + pose.sink;
                at = Vector2.Lerp(at, new Vector2(armX, armY), v.hinge);
            }
            return at;
        }

        bool Prepare()
        {
            if (moving) return true;
            var texture = Resources.Load<Texture2D>("Art/FieldGuardSkin");
            if (!texture || texture.width != Width * 2 || texture.height != Height) return false;
            shared ??= new MeshLayout();
            moving = Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(.5f, 0),
                original.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            moving.name = "LANCER · 원화 변형";
            moving.OverrideGeometry(shared.pixels, shared.triangles);
            using (var uv = new NativeArray<Vector2>(shared.uv, Allocator.Temp))
                moving.SetVertexAttribute(VertexAttribute.TexCoord0, uv);
            positions = new NativeArray<Vector3>(shared.points.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            return true;
        }

        public void Show(Pose pose)
        {
            if (!Prepare()) return;
            float units = 1 / original.pixelsPerUnit;
            for (int i = 0; i < shared.points.Length; i++)
            {
                Vector2 point = Deform(shared.points[i], pose);
                positions[i] = new Vector3((point.x - FootX) * units, (SourceBottom - point.y) * units, 0);
            }
            moving.SetVertexAttribute(VertexAttribute.Position, positions);
            if (art.sprite != moving)
            {
                art.sprite = moving;
                // 원화의 테두리 밖까지 뻗는 칼과 망토가 카메라 가장자리에서 먼저 사라지지 않게 한다.
                art.localBounds = new Bounds(new Vector3(0, Height * units * .5f, 0),
                    new Vector3((Width + 2 * MotionMargin) * units, (Height + 2 * MotionMargin) * units, .1f));
                expandedBounds = true;
            }
        }

        public void Rest()
        {
            if (!art) return;
            if (art.sprite != original) art.sprite = original;
            if (!expandedBounds) return;
            art.ResetLocalBounds();
            expandedBounds = false;
        }

        public void Dispose()
        {
            Rest();
            if (positions.IsCreated) positions.Dispose();
            if (moving) UnityEngine.Object.Destroy(moving);
        }
    }
}
