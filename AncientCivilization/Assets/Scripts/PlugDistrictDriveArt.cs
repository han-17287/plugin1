using UnityEngine;

namespace Plugin.PlayerCore
{
    // 이동 장치의 실제 부품 사이만 잇는다. 배관과 권양선은 충돌 또는 조준 대상이 아니다.
    public sealed class PlugDistrictDriveArt : MonoBehaviour
    {
        static readonly Color Casing = new Color32(30, 40, 47, 255);
        static readonly Color Steel = new Color32(96, 110, 110, 255);
        const float CurrentSpeed = 4.8f;
        const float PulseHalfLength = .13f;

        Transform hoistHook;
        LineRenderer rope, pulse;
        Vector2[] path;
        float[] distances;
        float length, traveled, lastHeight = float.NaN;
        bool powered;

        internal static PlugDistrictDriveArt Build(Transform owner, int mechanism, Transform lift,
            Vector2 socketA, Vector2 socketB)
        {
            if (mechanism != 0 && mechanism != 2 && mechanism != 4) return null;
            var go = new GameObject("전력 설비 연결부");
            go.transform.SetParent(owner, false);
            var art = go.AddComponent<PlugDistrictDriveArt>();
            if (mechanism == 0) art.BuildHoist(lift, socketA);
            else
            {
                art.BuildConduit(socketA, socketB);
                // 전류는 설비가 전달하는 게임 시계로만 갱신한다.
                art.enabled = false;
            }
            return art;
        }

        void BuildHoist(Transform lift, Vector2 socketA)
        {
            float x = lift.localPosition.x;
            float beamY = 6.32f;
            var beam = new[] { (Vector3)(socketA + new Vector2(-.21f, -.62f)),
                new Vector3(-.21f, beamY), new Vector3(x + .38f, beamY) };
            Stroke("권양기 지주", transform, beam, .15f, Casing, -1);
            Stroke("상부 철골", transform, beam, .073f, Steel, 0);
            Stroke("철골 버팀대", transform, new[] { new Vector3(-.21f, beamY - .78f),
                new Vector3(.71f, beamY) }, .08f, Steel, -1);
            PlugDistrictArt.Part(transform, "고정 권양기", 0, PlugDistrictPiece.Backplate,
                new Vector2(x, beamY - .05f), new Vector2(.85f, .69f), 1);

            // 훅과 삼각 고정줄은 승강대 자체의 자식이어서 보간 중에도 갑판에서 떨어지지 않는다.
            var hook = PlugDistrictArt.Part(lift, "화물대 견인 훅", 0, PlugDistrictPiece.Fixture,
                new Vector2(0, 1.04f), new Vector2(.57f, 1.14f), 1);
            hoistHook = hook.transform;
            Stroke("화물대 양쪽 고정줄", lift, new[] { new Vector3(-1.08f, .17f),
                new Vector3(.04f, .54f), new Vector3(1.08f, .17f) }, .055f, Steel, 0);
            var guide = new[] { new Vector3(x - .06f, beamY - .18f),
                new Vector3(x - .06f, beamY - .18f), new Vector3(x + .06f, beamY - .18f),
                new Vector3(x + .06f, beamY - .18f) };
            rope = Stroke("승강대 감김 강선", transform, guide, .044f, Steel, 0);
            UpdateHoist();
        }

        void LateUpdate() => UpdateHoist();

        void UpdateHoist()
        {
            if (!hoistHook) return;
            Vector2 tip = transform.InverseTransformPoint(hoistHook.position);
            tip.y += .55f;
            if (Mathf.Abs(tip.y - lastHeight) < .0005f) return;
            lastHeight = tip.y;
            rope.SetPosition(0, tip + Vector2.left * .06f);
            rope.SetPosition(3, tip + Vector2.right * .06f);
        }

        void BuildConduit(Vector2 a, Vector2 b)
        {
            // 실제 두 인서트 아래 전원함에서 출발해 벽면 엘보를 거쳐 상대 전원함으로 간다.
            a.y -= .68f;
            b.y -= .68f;
            float elbow = Mathf.Lerp(a.x, b.x, .62f);
            path = new[] { a, new Vector2(elbow - .20f, a.y), new Vector2(elbow, a.y + .20f),
                new Vector2(elbow, b.y - .20f), new Vector2(elbow + .20f, b.y), b };
            var geometry = new Vector3[path.Length];
            distances = new float[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                geometry[i] = path[i];
                if (i > 0) length += Vector2.Distance(path[i - 1], path[i]);
                distances[i] = length;
            }
            Stroke("송전관 피복", transform, geometry, .125f, Casing, -1);
            Stroke("송전관 도체", transform, geometry, .055f, Steel, 0);
            pulse = Stroke("다음 단자로 흐르는 전류", transform, new Vector3[3], .072f,
                new Color32(92, 203, 255, 183), 1);
            pulse.sharedMaterial = BluePlugArt.GlowMaterial;
            pulse.enabled = false;
        }

        internal void Signal(float dt, bool sending)
        {
            if (!pulse) return;
            if (!sending)
            {
                powered = false;
                pulse.enabled = false;
                return;
            }
            if (!powered) traveled = 0;
            powered = true;
            traveled = Mathf.Repeat(traveled + Mathf.Max(dt, 0) * CurrentSpeed, length + .85f);
            pulse.enabled = traveled < length + PulseHalfLength;
            if (!pulse.enabled) return;
            pulse.SetPosition(0, Along(traveled - PulseHalfLength));
            pulse.SetPosition(1, Along(traveled));
            pulse.SetPosition(2, Along(traveled + PulseHalfLength));
        }

        Vector2 Along(float at)
        {
            at = Mathf.Clamp(at, 0, length);
            for (int i = 1; i < distances.Length; i++)
                if (at <= distances[i])
                    return Vector2.Lerp(path[i - 1], path[i], (at - distances[i - 1]) / (distances[i] - distances[i - 1]));
            return path[path.Length - 1];
        }

        static LineRenderer Stroke(string label, Transform parent, Vector3[] points, float width, Color tint, int order)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.widthMultiplier = width;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.startColor = line.endColor = tint;
            line.sharedMaterial = BluePlugArt.SolidMaterial;
            line.sortingOrder = order;
            return line;
        }
    }
}
