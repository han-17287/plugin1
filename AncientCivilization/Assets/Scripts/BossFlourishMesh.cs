using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 짧은 접점 원화와 별도로 열기·파편의 여운만 그린다. 한 씬에서 메시는 하나다.
    internal sealed class BossFlourishMesh
    {
        const int Capacity = 10;
        const float SharedPortRadius = .24f;
        struct Stamp
        {
            public AncientBossCue.Style style;
            public AncientBossCue.Beat beat;
            public bool active, firstFrame, attached, primaryLight;
            public Transform support;
            public Vector3 point, direction;
            public float size, age, lifetime, crowdAlpha;
            public Vector3 WorldPoint => attached ? support.TransformPoint(point) : point;
            public Vector3 WorldAxis => attached ? support.TransformDirection(direction) : direction;
            public bool SupportVisible => !attached || (support && support.gameObject.activeInHierarchy);
        }

        readonly Stamp[] stamps = new Stamp[Capacity];
        readonly BossFlourishBrush brush = new BossFlourishBrush();
        readonly Mesh mesh;
        readonly Material material;
        readonly MeshRenderer display;
        readonly Transform space;
        internal BossFlourishMesh(Transform root)
        {
            space = new GameObject("보스 문양 · 잔광과 파편").transform;
            space.SetParent(root,false);
            mesh = new Mesh { name = "보스 잔광" };
            mesh.MarkDynamic();
            space.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            display = space.gameObject.AddComponent<MeshRenderer>();
            var shader = Resources.Load<Shader>("Shaders/BossFlourish");
            if (!shader || !shader.isSupported) shader = Shader.Find("Sprites/Default");
            material = new Material(shader);
            display.sharedMaterial = material;
            display.sortingOrder = 13;
            display.enabled = false;
        }

        internal void Spawn(AncientBossCue.Style style, AncientBossCue.Beat beat, Vector3 worldPoint,
            Vector3 direction, float size, Transform support, float baseDuration)
        {
            if (support && !support.gameObject.activeInHierarchy) return;
            int free = -1, replacement = -1, overlaps = 0;
            float mostSpent = -1f;
            for (int i=0; i<Capacity; i++)
            {
                ref Stamp old = ref stamps[i];
                if (!old.active || !old.SupportVisible)
                { if (free<0) free=i; continue; }
                if (old.style==style && old.beat==beat && old.age<.05f &&
                    (old.WorldPoint-worldPoint).sqrMagnitude < SharedPortRadius*SharedPortRadius)
                {
                    if (Vector3.Dot(old.WorldAxis,direction)>.978f) return;
                    overlaps++;
                }
                // 이펙트가 몰리면 이미 끝나가는 파편부터 양보한다. 실제 위험 표시는 별도 렌더러에 남는다.
                float spent = old.age/old.lifetime;
                if (beat!=AncientBossCue.Beat.Contact && old.beat==AncientBossCue.Beat.Contact && spent>mostSpent)
                { mostSpent=spent; replacement=i; }
            }
            int slot = free>=0 ? free : replacement;
            if (slot<0) return;
            ref Stamp item = ref stamps[slot];
            item.active = item.firstFrame = true;
            item.style = style; item.beat = beat; item.age = 0;
            item.support = support; item.attached = support;
            item.point = support ? support.InverseTransformPoint(worldPoint) : worldPoint;
            item.direction = support ? support.InverseTransformDirection(direction) : direction;
            item.size = Mathf.Clamp(size,.3f,1.75f);
            item.lifetime = baseDuration + (beat == AncientBossCue.Beat.Strike ? .115f : beat == AncientBossCue.Beat.Contact ? .11f : 0f);
            item.primaryLight = overlaps==0;
            item.crowdAlpha = overlaps==0 ? 1f : .64f;
        }

        internal bool Tick(float dt,float convoyDt)
        {
            bool live = false;
            for (int i=0; i<Capacity; i++)
            {
                ref Stamp s = ref stamps[i];
                if (!s.active) continue;
                if (!s.firstFrame) s.age += s.style==AncientBossCue.Style.Convoy ? convoyDt : dt;
                s.firstFrame = false;
                s.active = s.SupportVisible && s.age<s.lifetime;
                live |= s.active;
            }
            if (live) Redraw();
            else display.enabled = false;
            return live;
        }

        void Redraw()
        {
            brush.BeginFrame();
            var local = space.worldToLocalMatrix;
            for (int i=0; i<Capacity; i++)
            {
                ref Stamp s = ref stamps[i];
                if (!s.active || !s.SupportVisible) continue;
                float t = Mathf.Clamp01(s.age/s.lifetime);
                Vector3 x = s.WorldAxis*s.size, y = new Vector3(-x.y,x.x,0);
                float tail = 1f - Mathf.SmoothStep(0,1,Mathf.InverseLerp(.56f,1f,t));
                brush.BeginStamp(local.MultiplyPoint3x4(s.WorldPoint),local.MultiplyVector(x),local.MultiplyVector(y),.94f*tail*s.crowdAlpha);
                AncientBossCue.DrawFlourish(brush,s.style,s.beat,t,s.primaryLight);
            }
            display.enabled = brush.Indices.Count>0;
            if (!display.enabled) return;
            mesh.Clear();
            mesh.SetVertices(brush.Positions);
            mesh.SetColors(brush.Tints);
            mesh.SetTriangles(brush.Indices,0,true);
        }

        internal void Clear(AncientBossCue.Style style)
        {
            bool changed = false;
            for (int i=0; i<Capacity; i++)
                if (stamps[i].active && stamps[i].style==style) { stamps[i].active=false; changed=true; }
            if (changed && display.enabled) Redraw();
        }
        internal void Hide()
        {
            for (int i=0; i<Capacity; i++) stamps[i].active=false;
            display.enabled=false;
        }
        internal void Dispose()
        {
            Object.Destroy(mesh);
            Object.Destroy(material);
        }
    }
}
