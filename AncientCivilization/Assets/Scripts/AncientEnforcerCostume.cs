using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 집행관 한 명의 원화·장갑 표시등·투영 서류를 발 기준의 같은 좌표계에서 굽힌다.
    internal sealed class AncientEnforcerCostume
    {
        static readonly int Spine = Shader.PropertyToID("_EnforcerBend");
        static readonly int Head = Shader.PropertyToID("_EnforcerHead");
        static readonly int ToActor = Shader.PropertyToID("_EnforcerToActor");
        static readonly int ToWorld = Shader.PropertyToID("_EnforcerToWorld");
        readonly Material cloth;

        internal AncientEnforcerCostume()
        {
            Shader shader = Resources.Load<Shader>("Shaders/EnforcerUniform");
            if (shader && shader.isSupported) cloth = new Material(shader) { name = "집행관 분절 외투와 단안 광학계" };
        }

        // 기본 4꼭짓점 스프라이트에는 허리나 무릎이 없다. 처음 불러올 때만 28px 간격으로 분할한다.
        internal static Sprite Articulate(Sprite sprite)
        {
            Rect rect = sprite.rect;
            int columns = Mathf.Max(1, Mathf.CeilToInt(rect.width / 28f));
            int rows = Mathf.Max(1, Mathf.CeilToInt(rect.height / 28f));
            int stride = columns + 1;
            var vertices = new Vector2[stride * (rows + 1)];
            var triangles = new ushort[columns * rows * 6];
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= columns; x++)
                    vertices[y * stride + x] = new Vector2(rect.width * x / columns, rect.height * y / rows);
            int t = 0;
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    int i = y * stride + x;
                    triangles[t++] = (ushort)i;
                    triangles[t++] = (ushort)(i + stride);
                    triangles[t++] = (ushort)(i + 1);
                    triangles[t++] = (ushort)(i + 1);
                    triangles[t++] = (ushort)(i + stride);
                    triangles[t++] = (ushort)(i + stride + 1);
                }
            sprite.OverrideGeometry(vertices, triangles);
            return sprite;
        }

        internal void Apply(SpriteRenderer body, SpriteRenderer optics, SpriteRenderer scan,
            Vector2 lens, AncientEnforcerBodyMotion gesture)
        {
            if (!cloth || !body) return;
            if (body.sharedMaterial != cloth) body.sharedMaterial = cloth;
            if (optics && optics.sharedMaterial != cloth) optics.sharedMaterial = cloth;
            if (scan && scan.sharedMaterial != cloth) scan.sharedMaterial = cloth;
            Transform actor = body.transform;
            Vector3 local = actor.InverseTransformPoint(lens);
            cloth.SetMatrix(ToActor, actor.worldToLocalMatrix);
            cloth.SetMatrix(ToWorld, actor.localToWorldMatrix);
            cloth.SetVector(Head, new Vector4(local.x, local.y, gesture.Facing, 0));
            cloth.SetVector(Spine, new Vector4(gesture.Spine, gesture.Chest, gesture.Hem, gesture.Arm));
        }

        internal Vector2 Anchor(SpriteRenderer body, AncientEnforcerBodyMotion gesture, Vector2 original)
        {
            if (!cloth || !body || gesture == null) return original;
            Vector3 point = body.transform.InverseTransformPoint(original);
            gesture.Bend(point.x, point.y, out float dx, out float dy);
            point.x += dx;
            point.y += dy;
            return body.transform.TransformPoint(point);
        }

        internal void Dispose()
        {
            if (cloth) Object.Destroy(cloth);
        }
    }
}
