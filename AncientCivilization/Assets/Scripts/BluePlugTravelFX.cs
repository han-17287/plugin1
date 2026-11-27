using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.PlayerCore
{
    // 실제 자세를 고정 크기 풀에 복사한다. 이동·충돌·입력 판정에는 관여하지 않는다.
    [DefaultExecutionOrder(120)]
    public sealed class BluePlugTravelFX : MonoBehaviour
    {
        const int GhostCapacity = 6, SparkCapacity = 16;
        static readonly Color GhostBlue = BluePlugArt.MovementColor;
        static readonly Color SparkBlue = BluePlugArt.MovementColor;
        [Header("견인 잔상")]
        [SerializeField, Range(.06f, .30f)] float afterimageLife = .15f;
        [SerializeField, Range(.016f, .08f)] float afterimageInterval = .028f;
        [SerializeField, Range(.04f, .4f)] float afterimageDistance = .12f;

        struct Ghost
        {
            public SpriteRenderer Art;
            public float Age, Alpha;
        }

        struct Spark
        {
            public SpriteRenderer Art;
            public Vector2 Velocity;
            public float Age, Life, Alpha, Length, Width;
        }

        readonly Ghost[] ghosts = new Ghost[GhostCapacity];
        readonly Spark[] sparks = new Spark[SparkCapacity];
        readonly Sprite[] fullBodyFrames = new Sprite[8];
        Transform pool;
        SpriteRenderer actor;
        Camera view;
        PlayerInputReader input;
        AncientGame main;
        Sprite sparkSprite;
        Texture2D sparkTexture;
        Material silhouetteMaterial;
        int ghostCursor, sparkCursor, burstSequence;
        bool towing, haveSample, highQuality = true, reduced;
        float sampleAge;
        Vector3 lastSample;

        public int ActiveAfterimages { get; private set; }
        public int ActiveSparks { get; private set; }
        public int AfterimageCapacity => GhostCapacity;
        public int ImpactCapacity => SparkCapacity;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            main = GetComponent<AncientHero>() ? AncientGame.Instance : null;
            pool = new GameObject("파란 플러그 견인 효과 풀").transform;
            // 월드 좌표에 남겨 플레이어가 이동하거나 방을 옮겨도 잔상이 함께 끌려가지 않게 한다.
            pool.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            sparkTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            { name = "플러그 불꽃 픽셀", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            sparkTexture.SetPixels32(new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255),
                new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) });
            sparkTexture.Apply(false, true);
            sparkSprite = Sprite.Create(sparkTexture, new Rect(0, 0, 2, 2), Vector2.one * .5f, 2);
            var silhouetteShader = Resources.Load<Shader>("Shaders/BluePlugSilhouette");
            if (silhouetteShader && silhouetteShader.isSupported)
                silhouetteMaterial = new Material(silhouetteShader) { name = "플러그 잔상 알파 실루엣" };
            for (int i = 0; i < ghosts.Length; i++)
            {
                ghosts[i].Art = MakeSprite("견인 본체 잔상 " + i);
                if (silhouetteMaterial) ghosts[i].Art.sharedMaterial = silhouetteMaterial;
            }
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i].Art = MakeSprite("플러그 접점 불꽃 " + i);
                sparks[i].Art.sprite = sparkSprite;
            }
            RefreshPreferences();
        }

        SpriteRenderer MakeSprite(string label)
        {
            var child = new GameObject(label);
            child.transform.SetParent(pool, false);
            var image = child.AddComponent<SpriteRenderer>();
            image.sharedMaterial = BluePlugArt.SolidMaterial;
            image.enabled = false;
            return image;
        }

        public void Bind(SpriteRenderer bodyArt, Camera cameraView = null)
        {
            if (actor != bodyArt) Clear();
            actor = bodyArt;
            view = cameraView ? cameraView : Camera.main;
            // 분리된 팔을 지운 몸통 프레임에는 같은 힙 피벗의 완전한 원화를 대신 복사한다.
            // 초기 연결 때 준비해 실제 견인 중에는 스프라이트를 생성하지 않는다.
            if (actor && !fullBodyFrames[0])
                for (int i = 0; i < fullBodyFrames.Length; i++) fullBodyFrames[i] = AncientArt.Swing(i);
            RefreshPreferences();
        }

        public void SetTowing(bool active)
        {
            if (towing == active) return;
            towing = active;
            sampleAge = afterimageInterval;
            haveSample = false;
        }

        public void Latch(Vector2 point, Vector2 normal = default)
        { Emit(point, normal, false); }

        public void Arrival(Vector2 point, Vector2 incoming = default)
        { Emit(point, -incoming, true); }

        bool Paused => !Application.isFocused || input && input.Paused || main && main.Paused;

        // 외부 설정 창에서 옵션을 바꾸면 호출한다. 일반 F5/F6 입력은 아래에서 감지한다.
        public void RefreshPreferences()
        {
            bool wasReduced = reduced;
            highQuality = AncientGame.Saved("graphics", 1) != 0;
            reduced = AncientGame.Saved("reducedFeedback") != 0;
            // 효과를 줄이는 옵션은 이미 화면에 남은 잔상과 불꽃에도 즉시 적용한다.
            if (!highQuality || reduced && !wasReduced) HideSamples();
        }

        bool NearView(Vector2 point)
        {
            if (!view) return true;
            Vector3 screen = view.WorldToViewportPoint(point);
            return screen.z > 0 && screen.x > -.12f && screen.x < 1.12f && screen.y > -.12f && screen.y < 1.12f;
        }

        void Emit(Vector2 point, Vector2 outward, bool arrival)
        {
            if (!pool || !isActiveAndEnabled || Paused) return;
            if (!highQuality || !NearView(point)) return;
            int count = reduced ? 2 : arrival ? 8 : 6;
            float heading = outward.sqrMagnitude > .0001f ? Mathf.Atan2(outward.y, outward.x) : 0;
            bool directional = outward.sqrMagnitude > .0001f;
            int sequence = ++burstSequence;
            for (int i = 0; i < count; i++)
            {
                int index = sparkCursor++ % sparks.Length;
                var spark = sparks[index];
                // 장식용 각도는 고정 수열로 구해 게임 판정에서 쓰는 난수 순서를 건드리지 않는다.
                float mix = ((i * 7 + sequence * 3) % 13) / 12f;
                float angle = heading + (directional ? Mathf.Lerp(-1.25f, 1.25f, i / (float)(count - 1))
                    : i * (2 * Mathf.PI / count) + sequence * .47f);
                float speed = reduced ? 2.1f + mix : (arrival ? 5.2f : 3.8f) + mix * 3.7f;
                spark.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                spark.Age = 0;
                spark.Life = reduced ? .12f : .13f + mix * .08f;
                spark.Alpha = reduced ? .24f : .72f;
                spark.Length = reduced ? .10f : .14f + mix * .18f;
                spark.Width = reduced ? .022f : .028f;
                var art = spark.Art;
                art.transform.SetPositionAndRotation(point, Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg));
                art.transform.localScale = new Vector3(spark.Length, spark.Width, 1);
                art.sortingLayerID = actor ? actor.sortingLayerID : 0;
                art.sortingOrder = actor ? actor.sortingOrder + 3 : 20;
                art.color = new Color(SparkBlue.r, SparkBlue.g, SparkBlue.b, spark.Alpha);
                art.enabled = true;
                sparks[index] = spark;
            }
            CountVisible();
        }

        void LateUpdate()
        {
            // Saved()는 저장 접두사를 붙인 키를 만든다. 지속 이동 중에는 매 프레임 읽지 않는다.
            if (Input.GetKeyDown(KeyCode.F5) || Input.GetKeyDown(KeyCode.F6)) RefreshPreferences();
            if (!highQuality) { HideSamples(); return; }
            if (!actor || !actor.enabled || !actor.gameObject.activeInHierarchy) HideGhosts();
            if (Paused) return;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            for (int i = 0; i < ghosts.Length; i++)
            {
                var sample = ghosts[i];
                if (!sample.Art.enabled) continue;
                sample.Age += dt;
                float fade = Mathf.Clamp01(1 - sample.Age / Mathf.Max(.01f, afterimageLife));
                sample.Art.enabled = fade > 0;
                sample.Art.color = new Color(GhostBlue.r, GhostBlue.g, GhostBlue.b, sample.Alpha * fade * fade);
                ghosts[i] = sample;
            }
            for (int i = 0; i < sparks.Length; i++)
            {
                var spark = sparks[i];
                if (!spark.Art.enabled) continue;
                spark.Age += dt;
                float fade = Mathf.Clamp01(1 - spark.Age / spark.Life);
                spark.Art.enabled = fade > 0;
                spark.Art.transform.position += (Vector3)(spark.Velocity * dt);
                spark.Velocity *= Mathf.Max(0, 1 - 5.5f * dt);
                spark.Art.transform.localScale = new Vector3(spark.Length * (.35f + fade * .65f), spark.Width, 1);
                spark.Art.color = new Color(SparkBlue.r, SparkBlue.g, SparkBlue.b, spark.Alpha * fade);
                sparks[i] = spark;
            }
            if (towing && actor && actor.enabled && actor.gameObject.activeInHierarchy && actor.sprite && NearView(actor.transform.position))
            {
                sampleAge += dt;
                Vector3 point = actor.transform.position;
                float distance = haveSample ? (point - lastSample).sqrMagnitude : 0;
                if (haveSample && distance > 64) HideGhosts();
                if (sampleAge >= afterimageInterval && (!haveSample || distance >= afterimageDistance * afterimageDistance)) SampleBody(point);
            }
            else haveSample = false;
            CountVisible();
        }

        void SampleBody(Vector3 point)
        {
            int index = ghostCursor++ % ghosts.Length;
            var sample = ghosts[index];
            var image = sample.Art;
            image.sprite = CompleteBody(actor.sprite);
            image.flipX = actor.flipX;
            image.flipY = actor.flipY;
            image.drawMode = actor.drawMode;
            image.size = actor.size;
            image.sortingLayerID = actor.sortingLayerID;
            image.sortingOrder = actor.sortingOrder - 2;
            image.transform.SetPositionAndRotation(point, actor.transform.rotation);
            image.transform.localScale = actor.transform.lossyScale;
            sample.Age = 0;
            sample.Alpha = (reduced ? .10f : .24f) * actor.color.a;
            image.color = new Color(GhostBlue.r, GhostBlue.g, GhostBlue.b, sample.Alpha);
            image.enabled = true;
            ghosts[index] = sample;
            sampleAge = 0;
            lastSample = point;
            haveSample = true;
        }

        Sprite CompleteBody(Sprite source)
        {
            if (!source) return null;
            var detailed = BluePlugPoseAtlas.FullTow(source);
            if (detailed) return detailed;
            // 두 원화는 같은 시트 좌표의 힙과 월드 픽셀 밀도를 사용하므로 추가 위치 보정이 필요 없다.
            for (int i = 0; i < fullBodyFrames.Length; i++)
                if (source == BluePlugMotionArt.Body(i) && fullBodyFrames[i]) return fullBodyFrames[i];
            if (fullBodyFrames[0] && source == BluePlugMotionArt.TravelBody(0)) return fullBodyFrames[0];
            if (fullBodyFrames[2] && source == BluePlugMotionArt.TravelBody(1)) return fullBodyFrames[2];
            return source;
        }

        void HideGhosts()
        {
            for (int i = 0; i < ghosts.Length; i++) if (ghosts[i].Art) ghosts[i].Art.enabled = false;
            ActiveAfterimages = 0;
            haveSample = false;
        }

        void HideSamples()
        {
            HideGhosts();
            for (int i = 0; i < sparks.Length; i++) if (sparks[i].Art) sparks[i].Art.enabled = false;
            ActiveSparks = 0;
        }

        void CountVisible()
        {
            int bodyCount = 0, sparkCount = 0;
            for (int i = 0; i < ghosts.Length; i++) if (ghosts[i].Art.enabled) bodyCount++;
            for (int i = 0; i < sparks.Length; i++) if (sparks[i].Art.enabled) sparkCount++;
            ActiveAfterimages = bodyCount;
            ActiveSparks = sparkCount;
        }

        public void Clear()
        { towing = false; sampleAge = 0; HideSamples(); }

        void OnEnable() { RefreshPreferences(); }

        void OnDisable() { Clear(); }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) Clear();
            else RefreshPreferences();
        }

        void OnDestroy()
        {
            if (pool) Destroy(pool.gameObject);
            if (sparkSprite) Destroy(sparkSprite);
            if (sparkTexture) Destroy(sparkTexture);
            if (silhouetteMaterial) Destroy(silhouetteMaterial);
        }
    }
}
