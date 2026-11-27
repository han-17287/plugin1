using Plugin.AncientCivilization;
using UnityEngine;
using UnityEngine.Rendering;

namespace Plugin.PlayerCore
{
    // 발사 후 렌더한 플러그 단자 위치만 기록한다. 플레이어나 부모의 이동은 잔상을 추가하지 않는다.
    internal sealed class BluePlugFlightTrail
    {
        const float SampleSpacing = .07f;
        readonly Transform parent;
        TrailRenderer trail;
        BluePlugPhase previous = BluePlugPhase.Ready;
        Vector2 lastTip;
        float releaseCharge, expires;
        bool highQuality, reduced;

        internal BluePlugFlightTrail(Transform parent) { this.parent = parent; }

        internal void Show(Vector2 point, BluePlugPhase phase, float charge, int order, int sortingLayer)
        {
            var old = previous;
            previous = phase;
            if (phase == BluePlugPhase.Winding)
            {
                lastTip = point;
                releaseCharge = Mathf.Clamp01(charge);
            }
            if (phase == BluePlugPhase.Flying && old != phase)
            {
                highQuality = AncientGame.Saved("graphics", 1) != 0;
                reduced = AncientGame.Saved("reducedFeedback") != 0;
                releaseCharge = old == BluePlugPhase.Winding
                    ? Mathf.Max(releaseCharge, Mathf.Clamp01(charge)) : Mathf.Clamp01(charge);
            }

            if (!AncientVisuals.HighQuality || !highQuality)
            {
                Hide();
                return;
            }
            if (phase == BluePlugPhase.Flying)
            {
                if (!trail) Create();
                if (old != phase || !trail.enabled)
                {
                    trail.enabled = true;
                    trail.Clear();
                    trail.time = Mathf.Lerp(.055f, .095f, releaseCharge) * (reduced ? .72f : 1f);
                    trail.widthMultiplier = Mathf.Lerp(.075f, .14f, releaseCharge) * (reduced ? .65f : 1f);
                    var tint = BluePlugArt.MovementColor;
                    tint.a = reduced ? .20f : .56f;
                    trail.startColor = tint;
                    tint.a = 0f;
                    trail.endColor = tint;
                    // 첫 비행 샘플에 직전 회전의 끝점을 이어 투척 시작 프레임의 빈틈을 막는다.
                    if (old != BluePlugPhase.Winding) lastTip = point;
                    trail.AddPosition(lastTip);
                }
                trail.sortingOrder = order;
                trail.sortingLayerID = sortingLayer;
                AddSample(point, SampleSpacing);
                return;
            }

            if (!trail || !trail.enabled) return;
            if (old == BluePlugPhase.Flying)
            {
                // 체결 프레임의 실제 종점만 더한다. 리셋 좌표나 움직이는 소켓으로 선을 늘리지 않는다.
                if (phase == BluePlugPhase.Connected) AddSample(point, .001f);
                expires = Time.time + trail.time;
            }
            Tick();
        }

        // 투척이 끝나면 마지막으로 본 위치에서 짧게 소멸한다. 숨긴 헤드나 다음 방으로 점을 잇지 않는다.
        internal void End()
        {
            if (previous == BluePlugPhase.Flying && trail && trail.enabled) expires = Time.time + trail.time;
            previous = BluePlugPhase.Ready;
            Tick();
        }

        internal void Tick()
        {
            if (previous != BluePlugPhase.Flying && trail && trail.enabled && Time.time >= expires) Hide();
        }

        void Create()
        {
            var holder = new GameObject("플러그 비행 잔광");
            holder.transform.SetParent(parent, false);
            trail = holder.AddComponent<TrailRenderer>();
            // 엔진 자동 방출을 끄고 Show에서 직접 점을 찍어, 발사와 도킹 좌표를 정확히 따른다.
            trail.emitting = false;
            trail.enabled = false;
            trail.autodestruct = false;
            trail.sharedMaterial = BluePlugArt.GlowMaterial;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.lightProbeUsage = LightProbeUsage.Off;
            trail.reflectionProbeUsage = ReflectionProbeUsage.Off;
            trail.generateLightingData = false;
            trail.minVertexDistance = SampleSpacing;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.numCapVertices = 0;
            trail.numCornerVertices = 0;
        }

        void AddSample(Vector2 point, float spacing)
        {
            if ((point - lastTip).sqrMagnitude < spacing * spacing) return;
            trail.AddPosition(point);
            lastTip = point;
        }

        void Hide()
        {
            if (!trail || !trail.enabled) return;
            trail.Clear();
            trail.enabled = false;
        }

        internal void Clear()
        {
            previous = BluePlugPhase.Ready;
            releaseCharge = 0f;
            Hide();
        }
    }
}
