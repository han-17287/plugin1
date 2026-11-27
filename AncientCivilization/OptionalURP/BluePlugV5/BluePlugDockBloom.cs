using Plugin.AncientCivilization;
using Plugin.PlayerCore;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Plugin.OptionalURP
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class BluePlugDockBloom : MonoBehaviour
    {
        [Tooltip("비워 두면 이 카메라 뷰포트 안에서 발생한 모든 플레이어 체결을 받는다.")]
        [SerializeField] BluePlugVisuals watchedPlayer;
        [SerializeField, Range(.04f, .20f)] float duration = .10f;
        [SerializeField, Range(0f, .7f)] float peakWeight = .30f;
        [SerializeField, Range(0f, 5f)] float bloomIntensity = 1.8f;
        [SerializeField] float volumePriority = 20;

        Camera owner;
        UniversalAdditionalCameraData cameraSettings;
        Volume overlay;
        VolumeProfile transientProfile;
        float pulseEnd;

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            owner = GetComponent<Camera>();
            if (!TryGetComponent(out cameraSettings))
            {
                Debug.LogWarning("플러그 체결 Bloom에는 URP 카메라가 필요합니다.", this);
                enabled = false;
                return;
            }

            var holder = new GameObject("플러그 체결 Bloom");
            holder.transform.SetParent(transform, false);
            holder.layer = gameObject.layer;
            transientProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            transientProfile.hideFlags = HideFlags.DontSave;
            var bloom = transientProfile.Add<Bloom>();
            // 씬 프로필의 임계값, 틴트, 산란도는 유지하고 강도만 보간한다.
            bloom.intensity.Override(bloomIntensity);
            overlay = holder.AddComponent<Volume>();
            overlay.priority = volumePriority;
            overlay.isGlobal = true;
            overlay.weight = 0;
            overlay.sharedProfile = transientProfile;
            BluePlugVisuals.DockFlash += HandleDock;
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
            RenderPipelineManager.endCameraRendering += AfterCamera;
        }

        void HandleDock(BluePlugVisuals sender, Vector2 socket)
        {
            if ((watchedPlayer && sender != watchedPlayer) || !cameraSettings.renderPostProcessing) return;
            Vector3 view = owner.WorldToViewportPoint(new Vector3(socket.x, socket.y, 0));
            if (view.z <= 0 || view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1) return;
            pulseEnd = Time.unscaledTime + duration;
        }

        void BeforeCamera(ScriptableRenderContext context, Camera rendering)
        {
            if (!overlay) return;
            overlay.weight = 0;
            if (owner != rendering || Time.unscaledTime >= pulseEnd) return;
            if (!AncientVisuals.HighQuality || AncientGame.Saved("reducedFeedback") != 0)
            {
                pulseEnd = 0;
                return;
            }
            // URP는 beginCameraRendering 뒤에 이 카메라의 볼륨 스택을 업데이트한다.
            float remaining = Mathf.Clamp01((pulseEnd - Time.unscaledTime) / duration);
            overlay.weight = peakWeight * Mathf.SmoothStep(0, 1, remaining);
        }

        void AfterCamera(ScriptableRenderContext context, Camera rendering)
        {
            if (overlay) overlay.weight = 0;
        }

        void OnDisable()
        {
            BluePlugVisuals.DockFlash -= HandleDock;
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            RenderPipelineManager.endCameraRendering -= AfterCamera;
            pulseEnd = 0;
            if (overlay)
            {
                overlay.weight = 0;
                overlay.sharedProfile = null;
                Destroy(overlay.gameObject);
            }
            if (transientProfile)
            {
                foreach (var component in transientProfile.components)
                    if (component) Destroy(component);
                transientProfile.components.Clear();
                Destroy(transientProfile);
            }
        }
    }
}
