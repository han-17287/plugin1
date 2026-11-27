using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 실제 발 좌표는 항상 보스 컨트롤러의 것이다. 중립 시간에 튄 거리만 그림으로 이어 준다.
    internal struct AncientJokerLaneTransition
    {
        Vector2 from;
        float oldScale, start, span;
        public bool Active => span > 0;

        public void Begin(Vector2 previousFoot, Vector2 newFoot, float previousScale, float now, float neutralSeconds)
        {
            // 하트 복귀처럼 이미 몸이 호를 따라가는 동작에는 두 번째 보간을 붙이지 않는다.
            span = 0;
            if ((previousFoot - newFoot).sqrMagnitude < 1.6f * 1.6f || neutralSeconds < .065f) return;
            from = previousFoot;
            oldScale = previousScale;
            start = now;
            span = Mathf.Min(.24f, neutralSeconds - .024f);
        }

        public void Sample(Vector2 realFoot, float realScale, float now, bool neutral, out Vector2 foot, out float scale)
        {
            foot = realFoot;
            scale = realScale;
            if (!Active) return;
            if (!neutral || now >= start + span)
            {
                span = 0;
                return;
            }
            float p = Mathf.Clamp01((now - start) / span);
            float easing = Mathf.SmoothStep(0, 1, p);
            foot = Vector2.Lerp(from, realFoot, easing) + Vector2.up * (.44f * 4 * p * (1 - p));
            scale = Mathf.Lerp(oldScale, realScale, easing);
        }
    }
}
