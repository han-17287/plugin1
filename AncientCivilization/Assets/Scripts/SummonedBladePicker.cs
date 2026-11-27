using System;
using UnityEngine;

namespace Plugin.PlayerCore
{
    // 다음 틱에 움직이는 발판도 같은 국소 접점을 유지한다.
    internal struct SummonedBladeLanding
    {
        internal Collider2D support;
        internal bool confinedDeck;
        Vector2 localPoint, localNormal;
        internal Vector2 Point => support.transform.TransformPoint(localPoint);
        internal Vector2 Normal => ((Vector2)support.transform.TransformDirection(localNormal)).normalized;

        internal SummonedBladeLanding(RaycastHit2D hit)
        {
            confinedDeck = false;
            support = hit.collider;
            localPoint = support.transform.InverseTransformPoint(hit.point);
            localNormal = support.transform.InverseTransformDirection(hit.normal);
        }
    }

    internal sealed class SummonedBladePicker
    {
        readonly SummonedBladeData data;
        readonly Camera view;
        readonly Func<Vector2, bool> dangerous;
        readonly RaycastHit2D[] rays = new RaycastHit2D[48], sweep = new RaycastHit2D[32];
        readonly Collider2D[] overlaps = new Collider2D[24];
        readonly ContactFilter2D solid;
        readonly ContactFilter2D danger;
        internal bool FixedForDebug, ConfinedDeckFallback;
        internal Vector2 DebugWorld;

        internal SummonedBladePicker(SummonedBladeData data, Camera view, Func<Vector2, bool> dangerous)
        {
            this.data = data;
            this.view = view;
            this.dangerous = dangerous;
            solid = new ContactFilter2D {useLayerMask = true, layerMask = data.groundMask, useTriggers = false};
            danger = new ContactFilter2D {useLayerMask = true, layerMask = data.dangerMask, useTriggers = true};
        }

        internal bool TryPick(Vector2 actor, Vector2? last, out SummonedBladeLanding result)
        {
            result = default;
            if (!view || !view.orthographic) return false;
            float z = Mathf.Abs(view.transform.position.z);
            Vector2 left = view.ViewportToWorldPoint(new Vector3(0, 0, z));
            Vector2 right = view.ViewportToWorldPoint(new Vector3(1, 1, z));
            float min = Mathf.Max(left.x + data.groundMargin, actor.x - data.maxDistance);
            float max = Mathf.Min(right.x - data.groundMargin, actor.x + data.maxDistance);
            if (max <= min) return false;
            if (FixedForDebug)
                return Column(DebugWorld.x, right.y, left.y, actor, last, false, out result, ConfinedDeckFallback);
            int attempts = Mathf.Clamp(data.positionAttempts, 4, 64);
            float total = 0;
            for (int i = 0; i < attempts; i++)
            {
                float x = UnityEngine.Random.Range(min, max);
                if (!Column(x, right.y, left.y, actor, last, false, out var item)) continue;
                // 경사가 완만하고 화면 중앙 가까운 자리에 높은 가중치를 둔다.
                float centre = 1 - Mathf.Abs(2 * (x - min) / (max - min) - 1);
                float weight = .5f + centre + item.Normal.y;
                total += weight;
                if (UnityEngine.Random.value * total < weight) result = item;
            }
            if (result.support) return true;
            // 무작위 탐색이 놓친 좁은 지형은 같은 검사로 일정 간격에서 재탐색한다.
            for (int i = 0; i <= attempts; i++)
                if (Column(Mathf.Lerp(min, max, i / (float)attempts), right.y, left.y, actor, last, false, out result))
                    return true;
            // 짧은 트럭 적재함에서는 통상 범위를 모두 검사한 뒤에만 가까운 설치점을 허용한다.
            if (ConfinedDeckFallback)
                for (int i = 0; i <= attempts; i++)
                    if (Column(Mathf.Lerp(min, max, i / (float)attempts), right.y, left.y, actor, last, false, out result, true))
                        return true;
            return false;
        }

        internal bool TryPlantAtFeet(Vector2 actor, out SummonedBladeLanding result)
        {
            // 경위 바로 아래 바닥만 사용한다. 절벽 위에서 E를 누르면 손에 계속 든다.
            return Column(actor.x, actor.y + .2f, actor.y - 6, actor, null, true, out result);
        }

        bool Column(float x, float top, float bottom, Vector2 actor, Vector2? last, bool atFeet, out SummonedBladeLanding result, bool confinedDeck = false)
        {
            result = default;
            int count = Physics2D.Raycast(new Vector2(x, top), Vector2.down, solid, rays, top - bottom);
            if (count == rays.Length) return false;
            float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = rays[i];
                if (!hit.collider || hit.distance < .015f || hit.normal.y < .67f) continue;
                if (last.HasValue && (hit.point - last.Value).sqrMagnitude < data.previousClearance * data.previousClearance) continue;
                var candidate = new SummonedBladeLanding(hit) { confinedDeck = confinedDeck };
                if (!Valid(candidate, actor, !atFeet, !atFeet)) continue;
                if (!atFeet && DropHeight(candidate) < .25f) continue;
                float d = Mathf.Abs(candidate.Point.y - (FixedForDebug && !atFeet ? DebugWorld.y : actor.y - 1));
                if (d >= best) continue;
                best = d;
                result = candidate;
            }
            return result.support;
        }

        internal bool Valid(SummonedBladeLanding landing, Vector2 actor, bool requireView, bool requireRange)
        {
            if (!landing.support || !landing.support.isActiveAndEnabled || !view) return false;
            Vector2 p = landing.Point, normal = landing.Normal, handle = p + normal * data.handleHeight;
            if (normal.y < .67f) return false;
            if (requireView)
            {
                Vector3 v = view.WorldToViewportPoint(p), h = view.WorldToViewportPoint(handle);
                if (v.z <= 0 || v.x < .025f || v.x > .975f || v.y < .025f || v.y > .975f || h.y > .975f) return false;
            }
            float r = Vector2.Distance(actor, p);
            if (requireRange && (r < (landing.confinedDeck ? Mathf.Min(data.minDistance, data.deckMinDistance) : data.minDistance) || r > data.maxDistance || Vector2.Distance(actor, handle) > data.plugRange)) return false;
            // 보스전 판정에는 실제 바닥점만 넘긴다. 적재함 위의 손잡이는 바닥 높이 밖에 있다.
            if (dangerous != null && dangerous(p)) return false;
            if (Physics2D.OverlapCircle(p + normal * .4f, .63f, danger, overlaps) > 0) return false;
            if (Physics2D.OverlapCapsule(handle, new Vector2(.78f, 2.15f), CapsuleDirection2D.Vertical, 0, solid, overlaps) > 0)
                return false;
            if (!requireRange) return true;
            // 플러그 팁과 경위 몸이 지나는 두 경로를 각각 검사한다. 단방향 발판은 아래에서 진입할 수 있다.
            Vector2 hand = actor + Vector2.up * .30f;
            return Clear(hand, handle, false) && Clear(actor, handle, true);
        }

        bool Clear(Vector2 from, Vector2 to, bool body)
        {
            Vector2 path = to - from;
            float length = path.magnitude;
            if (length < .05f) return true;
            Vector2 direction = path / length;
            // 몸통의 바닥 여유를 두어 출발 발판의 접촉을 장애물로 잘못 읽지 않는다.
            int count = body
                ? Physics2D.CapsuleCast(from + Vector2.up * .10f, new Vector2(.72f, 1.74f), CapsuleDirection2D.Vertical,
                    0, direction, solid, sweep, length)
                : Physics2D.CircleCast(from, .10f, direction, solid, sweep, length);
            if (count == sweep.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var hit = sweep[i];
                if (!hit.collider) continue;
                if (hit.collider.usedByEffector && from.y - (body ? .77f : .1f) < hit.collider.bounds.max.y - .05f) continue;
                if (hit.distance <= .02f && (hit.collider.bounds.max.y < from.y - .5f || Vector2.Dot(hit.normal, direction) > -.05f))
                    continue;
                return false;
            }
            return true;
        }

        internal float DropHeight(SummonedBladeLanding landing)
        {
            Vector2 handle = landing.Point + landing.Normal * data.handleHeight;
            // 짧은 화면이나 낮은 천장에서도 칼이 나타나는 첫 프레임부터 손잡이가 보인다.
            float visibleTop = view.ViewportToWorldPoint(new Vector3(.5f, .975f,
                Mathf.Abs(view.transform.position.z))).y;
            float height = Mathf.Clamp(visibleTop - handle.y - .25f, 0, data.fallHeight);
            if (height <= 0) return 0;
            int count = Physics2D.CircleCast(handle, .23f, Vector2.up, solid, sweep, height);
            if (count == sweep.Length) return 0;
            for (int i = 0; i < count; i++)
                if (sweep[i].collider) height = Mathf.Min(height, Mathf.Max(0, sweep[i].distance - .12f));
            return height;
        }
    }
}
