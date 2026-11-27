using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 백 경위의 발밑에서만 재생한다. 이동하는 짐칸의 먼지는 바닥과 함께 움직인다.
    public sealed class AncientFootDust : MonoBehaviour
    {
        const int Frames = 6;
        const int Capacity = 4;
        const int CellWidth = 96;
        const int CellHeight = 56;
        static readonly Sprite[] frames = new Sprite[Frames];
        readonly Plume[] pool = new Plume[Capacity];
        Transform worldRoot;
        AncientGame game;
        int cursor;
        int lastPlant = -1;
        float nextStep;

        struct Plume
        {
            public SpriteRenderer view;
            public Transform support;
            public bool followsSupport;
            public Vector3 localAnchor;
            public float start, duration, opacity;
        }

        public void Initialize(AncientGame host)
        {
            game = host;
            worldRoot = new GameObject("백 경위 · 바닥 먼지").transform;
            worldRoot.SetParent(host.transform, false);
            if (!frames[0])
            {
                var atlas = Resources.Load<Texture2D>("Art/FoundryFootDust");
                for (int i = 0; i < Frames; i++)
                    frames[i] = Sprite.Create(atlas, new Rect(i * CellWidth, 0, CellWidth, CellHeight),
                        new Vector2(4f / CellWidth, 6f / CellHeight), 66, 0, SpriteMeshType.FullRect);
            }
            for (int i = 0; i < Capacity; i++)
            {
                var view = AncientArt.Display("바닥 먼지", frames[0], Vector2.zero, 9, worldRoot);
                view.enabled = false;
                pool[i].view = view;
            }
            enabled = false;
        }

        public void Land(Vector2 point, Vector2 normal, Rigidbody2D support, float fallSpeed)
        {
            float size = Mathf.Lerp(.58f, 1.12f, Mathf.InverseLerp(7, 23, fallSpeed));
            float alpha = game.ReducedFeedback ? .44f : .85f;
            Kick(point, normal, support, -1, size, .28f, alpha);
            Kick(point, normal, support, 1, size * .91f, .25f, alpha);
            nextStep = Time.time + .16f;
        }

        public void TrackBoot(int frame, float relativeSpeed, Vector2 point, Vector2 normal, Rigidbody2D support)
        {
            // 달리는 16장 가운데 발이 바닥을 짚는 두 구간. 정지·공중 자세에서는 궤적을 남기지 않는다.
            int plant = frame >= 8 && frame < 12 ? 0 : frame >= 16 && frame < 20 ? 1 : -1;
            bool planted = plant >= 0 && plant != lastPlant;
            lastPlant = plant;
            if (!planted || Mathf.Abs(relativeSpeed) <= 7 || Time.time < nextStep ||
                !AncientVisuals.HighQuality || game.ReducedFeedback) return;
            float outward = -Mathf.Sign(relativeSpeed);
            Vector2 tangent = new Vector2(normal.y, -normal.x);
            Kick(point + tangent * (.12f * outward), normal, support, outward, .42f, .18f, .57f);
            nextStep = Time.time + .14f;
        }

        void Kick(Vector2 point, Vector2 normal, Rigidbody2D support, float outward, float size, float duration, float opacity)
        {
            ref Plume p = ref pool[cursor];
            cursor = (cursor + 1) % Capacity;
            p.start = Time.time;
            p.duration = duration;
            p.opacity = opacity;
            p.support = support ? support.transform : null;
            p.followsSupport = support;
            Vector3 origin = point + normal * .017f;
            p.localAnchor = p.support ? p.support.InverseTransformPoint(origin) : origin;
            p.view.transform.SetPositionAndRotation(origin, Quaternion.Euler(0, 0, Mathf.Atan2(-normal.x, normal.y) * Mathf.Rad2Deg));
            p.view.transform.localScale = Vector3.one * size;
            p.view.flipX = outward < 0;
            p.view.color = new Color(1, 1, 1, opacity);
            p.view.sprite = frames[0];
            p.view.enabled = enabled = true;
        }

        void LateUpdate()
        {
            if (!game || game.InSniperFinalPhase || game.won) { Clear(); return; }
            if (game.Paused) return;
            bool live = false;
            for (int i = 0; i < Capacity; i++)
            {
                ref Plume p = ref pool[i];
                if (!p.view.enabled) continue;
                float t = (Time.time - p.start) / p.duration;
                if (t >= 1 || (p.followsSupport && !p.support)) { p.view.enabled = false; continue; }
                if (p.support) p.view.transform.position = p.support.TransformPoint(p.localAnchor);
                p.view.sprite = frames[Mathf.Clamp((int)(t * Frames), 0, Frames - 1)];
                p.view.color = new Color(1, 1, 1, p.opacity * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.4f, 1, t))));
                live = true;
            }
            enabled = live;
        }

        public void Clear()
        {
            nextStep = 0;
            lastPlant = -1;
            enabled = false;
        }

        void OnDisable()
        {
            for (int i = 0; i < Capacity; i++)
                if (pool[i].view) pool[i].view.enabled = false;
        }

        void OnDestroy() { if (worldRoot) Destroy(worldRoot.gameObject); }
    }
}
