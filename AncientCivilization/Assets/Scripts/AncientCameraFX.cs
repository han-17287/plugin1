using UnityEngine;

namespace Plugin.AncientCivilization
{
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent]
    public sealed class AncientCameraFX : MonoBehaviour
    {
        [Header("전구와 소켓의 국소 발광")]
        [SerializeField, Range(0, 2)] float bloomThreshold = .82f;
        [SerializeField, Range(0, 1)] float bloomStrength = .32f;
        [SerializeField, Range(1, 6)] float bloomRadius = 2.8f;
        [SerializeField, Range(0, .6f)] float dockBoost = .28f;
        const float DockFade = .095f;
        Vector2 dockUV;
        float dockUntil;
        Material grade;
        Camera view;
        static readonly int Strength = Shader.PropertyToID("_BloomStrength");
        static readonly int Threshold = Shader.PropertyToID("_BloomThreshold");
        static readonly int Radius = Shader.PropertyToID("_BloomRadius");
        static readonly int Damage = Shader.PropertyToID("_DamageAmount");
        static readonly int Health = Shader.PropertyToID("_LowHealth");

        void Awake()
        {
            view = GetComponent<Camera>();
            view.allowHDR = true;
        }

        void OnEnable() => Plugin.PlayerCore.BluePlugVisuals.DockFlash += Latch;
        void OnDisable()
        {
            Plugin.PlayerCore.BluePlugVisuals.DockFlash -= Latch;
            dockUntil = 0;
        }

        void Latch(Plugin.PlayerCore.BluePlugVisuals plug, Vector2 world)
        {
            if (!view || !view.enabled || plug.gameObject.scene != gameObject.scene) return;
            Vector3 p = view.WorldToViewportPoint(world);
            if (p.z <= 0 || p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1) return;
            dockUV = p;
            dockUntil = Time.unscaledTime + DockFade;
        }

        static readonly int Dock = Shader.PropertyToID("_DockUVPulse");
        void Start()
        {
            var shader = Resources.Load<Shader>("Shaders/Presentation");
            if (shader && shader.isSupported) grade = new Material(shader);
        }

        void LateUpdate()
        {
            // 낮은 품질에서는 HDR 카메라 버퍼의 대역폭도 절약한다.
            if (view.allowHDR != AncientVisuals.HighQuality) view.allowHDR = AncientVisuals.HighQuality;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            var game = AncientGame.Instance;
            bool ownsHero = game && game.isActiveAndEnabled && game.View == view;
            float damage = ownsHero ? game.DamageFlash : 0;
            float lowHealth = ownsHero ? game.HealthWarning : 0;
            if (!grade || (!AncientVisuals.HighQuality && damage < .001f && lowHealth <= 0))
            {
                Graphics.Blit(source, destination);
                return;
            }
            grade.SetFloat(Strength, AncientVisuals.HighQuality ? bloomStrength : 0);
            grade.SetFloat(Threshold, bloomThreshold);
            grade.SetFloat(Radius, bloomRadius);
            float flash = AncientVisuals.HighQuality ? Mathf.Clamp01((dockUntil-Time.unscaledTime)/DockFade) * dockBoost : 0;
            grade.SetVector(Dock, new Vector4(dockUV.x,dockUV.y,flash,view.aspect));
            grade.SetFloat(Damage, damage);
            grade.SetFloat(Health, lowHealth);
            Graphics.Blit(source, destination, grade);
        }

        void OnDestroy() { if (grade) Destroy(grade); }
    }
}
