using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.PlayerCore
{
    public enum BluePlugSocketState { Idle, Ready, Blocked, Linked, Locked }

    // 판정은 콘센트의 위치와 연결 설정만 사용한다. 그림에는 물리 콜라이더가 없다.
    [DefaultExecutionOrder(180)]
    public sealed class BluePlugSocket : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] Vector2 towOffset = new Vector2(0, .95f);
        [SerializeField] Vector2 entryDirection = Vector2.right;
        [SerializeField] BluePlugSkill supported = BluePlugSkill.Tow | BluePlugSkill.Swing;
        [SerializeField] bool operational = true;
        [SerializeField, Min(0)] float rangeOverride;
        [SerializeField, Min(0)] float towSpeedOverride;
        [SerializeField, Range(-1, 1)] float towCarryOverride = -1;
        [Header("벽 고정점")]
        [SerializeField] Collider2D wallFace;
        [SerializeField] int wallOutwardSign = -1;
        [Header("표시")]
        [SerializeField, Min(.1f)] float panelScale = 1f;
        [SerializeField, Range(0, 1)] float glowIntensity = .82f;

        static readonly Color Signal = BluePlugArt.MovementColor;
        static readonly Color Unlearned = new Color(.41f, .47f, .68f);
        static readonly Color Obstructed = new Color(1f, .60f, .28f);

        Transform panel;
        BluePlugPointLight localLight;
        SpriteRenderer halo, faceEmitters, slots, indicator, tow, swing, towLock, swingLock;
        BluePlugSocketState state;
        BluePlugSkill unlocked;
        Color signalColor;
        float signalStrength, pulsePhase;
        bool lastOperational, appPaused;
        public GrappleAnchor BoundAnchor { get; private set; }

        // 벽 고정은 물리 손잡이다. 도킹·전원·견인 기술을 요구하지 않는다.
        public Collider2D WallFace => wallFace;
        public int WallOutwardSign => wallOutwardSign < 0 ? -1 : 1;
        public bool CanWallRest => isActiveAndEnabled && wallFace && wallFace.isActiveAndEnabled &&
            !wallFace.isTrigger && (!BoundAnchor || !BoundAnchor.Spent);

        public void ConfigureWallRest(Collider2D face, int outwardSign)
        {
            wallFace = face;
            wallOutwardSign = outwardSign < 0 ? -1 : 1;
        }

        public Vector2 Point => transform.position;
        public Vector2 TowPoint => transform.TransformPoint(towOffset);
        public Vector2 EntryDirection => ((Vector2)transform.TransformDirection(
            entryDirection.sqrMagnitude > .0001f ? entryDirection : Vector2.right)).normalized;
        public BluePlugSkill Supported => supported;
        // 설비 급전은 기본 투척만으로 가능하며 이동 문양의 개별 잠금은 유지한다.
        public bool PowersEquipment { get; private set; }
        public void MarkPowerOutlet() => PowersEquipment = true;
        internal void SetEquipmentPower(bool powered)
        {
            operational=powered;
            if(panel)Refresh();
        }
        public bool Operational => operational && isActiveAndEnabled &&
            (!BoundAnchor || BoundAnchor.isActiveAndEnabled && BoundAnchor.Operational && !BoundAnchor.Spent);
        public bool PatrolDriven => BoundAnchor && BoundAnchor.PatrolDriven;
        public Vector2 AnchorVelocity => PatrolDriven ? BoundAnchor.PatrolVelocity : Vector2.zero;
        public float Range(float fallback) => rangeOverride > 0 ? rangeOverride : fallback;
        public float TowSpeed(float fallback) => towSpeedOverride > 0 ? towSpeedOverride : fallback;
        public float TowCarry(float fallback) => towCarryOverride >= 0 ? towCarryOverride : fallback;

        public void BindAnchor(GrappleAnchor anchor)
        {
            BoundAnchor = anchor;
            if (!anchor) return;
            anchor.UseBluePlugVisual();
            Configure(Vector2.zero, BluePlugSkill.Tow | BluePlugSkill.Swing, Vector2.up);
            SetTravel(0, 72, .22f);
        }

        public void SetTravel(float range, float towSpeed, float carry)
        {
            rangeOverride = range;
            towSpeedOverride = towSpeed;
            towCarryOverride = carry;
        }

        public bool Reserve() => Operational && (!BoundAnchor || BoundAnchor.TryAcquire());
        public void Release(bool arrived) { if (BoundAnchor) BoundAnchor.Release(arrived); }

        public void Configure(Vector2 towOffset, BluePlugSkill supported, Vector2 entryDirection)
        {
            this.towOffset = towOffset;
            this.supported = supported;
            this.entryDirection = entryDirection.sqrMagnitude > .0001f ? entryDirection.normalized : Vector2.right;
            if (panel) Refresh();
        }

        void Awake()
        {
            var root = new GameObject("콘센트 표시");
            panel = root.transform;
            panel.SetParent(transform, false);
            panel.localScale = Vector3.one * panelScale;

            halo = BluePlugArt.AddSprite(panel, "콘센트 푸른 후광", BluePlugArt.Halo, Vector2.zero, 6, 2.15f);
            halo.sharedMaterial = BluePlugArt.GlowMaterial;
            BluePlugArt.AddSprite(panel, "콘센트 함체", BluePlugArt.Socket, Vector2.zero, 8);
            faceEmitters = BluePlugArt.AddSprite(panel, "콘센트 상하 표시등", BluePlugArt.SocketEmitter, Vector2.zero, 9);
            faceEmitters.sharedMaterial = BluePlugArt.GlowMaterial;
            slots = BluePlugArt.AddSprite(panel, "방향을 맞춘 단자 홈", BluePlugArt.SocketSlots, Vector2.zero, 9);
            indicator = BluePlugArt.AddSprite(panel, "연결 상태", BluePlugArt.Idle, new Vector2(0, .69f), 10, .68f);
            tow = BluePlugArt.AddSprite(panel, "견인 화살표", BluePlugArt.Arrow, new Vector2(-.71f, .03f), 9, .90f);
            swing = BluePlugArt.AddSprite(panel, "스윙 호", BluePlugArt.Arc, new Vector2(.71f, .03f), 9, .90f);
            towLock = BluePlugArt.AddSprite(panel, "견인 잠금 표시", BluePlugArt.Lock, new Vector2(-.71f, -.28f), 9, .43f);
            swingLock = BluePlugArt.AddSprite(panel, "스윙 잠금 표시", BluePlugArt.Lock, new Vector2(.71f, -.28f), 9, .43f);
            Refresh();
        }

        void OnDisable() => localLight?.Clear();

        void OnEnable()
        {
            if (panel) Refresh();
        }

        bool PresentationPaused
        {
            get
            {
                if (appPaused || !Application.isFocused || Time.timeScale <= 0) return true;
                var clock = BoundAnchor ? BoundAnchor.clockOwner : null;
                // 순찰·연습장은 자체 입력 시계를 사용하고, 본편 표시 풀은 본편 시계를 따른다.
                if (clock) return clock.Paused;
                var game = AncientGame.Instance;
                return game && game.Paused;
            }
        }

        void LateUpdate()
        {
            if (!panel) return;
            bool visible = !BoundAnchor || !BoundAnchor.Spent;
            if (panel.gameObject.activeSelf != visible) panel.gameObject.SetActive(visible);
            if (!visible)
            {
                localLight?.Clear();
                return;
            }
            // 외부 전원·사용 완료 변경은 새 조준 입력이 없어도 표시와 동기화한다.
            if (lastOperational != Operational) Refresh();
            bool paused = PresentationPaused;
            bool quality = AncientVisuals.HighQuality;
            bool pulse = lastOperational &&
                (state == BluePlugSocketState.Idle || state == BluePlugSocketState.Ready ||
                 state == BluePlugSocketState.Linked);
            float multiplier = 1;
            if (pulse && quality)
            {
                float period = state == BluePlugSocketState.Ready ? .95f :
                    state == BluePlugSocketState.Linked ? 1.6f : 2.4f;
                if (!paused) pulsePhase = Mathf.Repeat(pulsePhase + Time.deltaTime / period, 1);
                float depth = state == BluePlugSocketState.Ready ? .14f :
                    state == BluePlugSocketState.Linked ? .045f : .10f;
                multiplier -= depth * (.5f - .5f * Mathf.Cos(pulsePhase * Mathf.PI * 2));
            }
            ApplySignal(multiplier, !paused && quality);
        }

        void OnApplicationPause(bool paused)
        {
            appPaused = paused;
            if (paused) localLight?.Clear();
            else if (panel) Refresh();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) localLight?.Clear();
            else if (panel) Refresh();
        }

        public void SetIndicator(BluePlugSocketState next, BluePlugSkill learned)
        {
            bool open = Operational;
            bool visible = !BoundAnchor || !BoundAnchor.Spent;
            if (panel && panel.gameObject.activeSelf != visible) panel.gameObject.SetActive(visible);
            if (!visible) localLight?.Clear();
            if (state == next && unlocked == learned && open == lastOperational) return;
            if (state != next) pulsePhase = 0;
            state = next;
            unlocked = learned;
            if (panel) Refresh();
        }

        void Refresh()
        {
            lastOperational = Operational;
            slots.transform.localRotation = Quaternion.Euler(0, 0,
                Mathf.Atan2(entryDirection.y, entryDirection.x) * Mathf.Rad2Deg);
            // 타이머가 꺼지는 순간에도 이전 Ready/Linked 문양을 사용 가능 표시로 남기지 않는다.
            var display = !lastOperational &&
                (state == BluePlugSocketState.Ready || state == BluePlugSocketState.Linked)
                ? BluePlugSocketState.Idle : state;
            // 경로 검사로 선정된 콘센트만 가이드가 벌어진다. 체결 시에는 실제 걸쇠 그림으로 교체한다.
            slots.sprite = display == BluePlugSocketState.Ready ? BluePlugArt.OpenSlots :
                display == BluePlugSocketState.Linked ? BluePlugArt.ClaspedSlots : BluePlugArt.SocketSlots;
            switch (display)
            {
                case BluePlugSocketState.Ready:
                    indicator.sprite = BluePlugArt.Aim;
                    indicator.color = Signal;
                    SetSignal(Signal, .75f);
                    break;
                case BluePlugSocketState.Blocked:
                    indicator.sprite = BluePlugArt.Cross;
                    indicator.color = Obstructed;
                    SetSignal(Obstructed, .21f);
                    break;
                case BluePlugSocketState.Linked:
                    indicator.sprite = BluePlugArt.Linked;
                    indicator.color = Signal;
                    SetSignal(Signal, .96f);
                    break;
                case BluePlugSocketState.Locked:
                    indicator.sprite = BluePlugArt.Lock;
                    indicator.color = Unlearned;
                    SetSignal(Unlearned, .26f);
                    break;
                default:
                    indicator.sprite = BluePlugArt.Idle;
                    indicator.color = lastOperational ? Signal : Unlearned;
                    SetSignal(indicator.color, lastOperational ? .31f : .12f);
                    break;
            }

            ShowSkill(tow, towLock, BluePlugSkill.Tow);
            ShowSkill(swing, swingLock, BluePlugSkill.Swing);
        }

        void SetSignal(Color color, float strength)
        {
            signalColor = color;
            signalStrength = strength;
            ApplySignal(1, !PresentationPaused && AncientVisuals.HighQuality);
        }

        void ApplySignal(float multiplier, bool allowLight)
        {
            Color color = signalColor;
            float strength = signalStrength * multiplier;
            color.a = strength * glowIntensity;
            faceEmitters.color = color;
            color.a *= .80f;
            halo.color = color;
            // 선택된 연결점만 주변에 빛을 투사하고, 풀에 돌아가거나 가려지면 즉시 꺼진다.
            bool targeted = state == BluePlugSocketState.Ready || state == BluePlugSocketState.Linked;
            if (!allowLight || !targeted || !lastOperational || !panel.gameObject.activeSelf || !isActiveAndEnabled)
            {
                localLight?.Clear();
                return;
            }
            localLight ??= new BluePlugPointLight(transform);
            localLight.Set(Point, Signal, strength * glowIntensity, 1.4f * panelScale);
        }

        void ShowSkill(SpriteRenderer glyph, SpriteRenderer padlock, BluePlugSkill skill)
        {
            bool allowed = (supported & skill) != 0;
            bool learned = (unlocked & skill) != 0;
            glyph.enabled = allowed;
            glyph.color = learned ? Signal : Unlearned;
            padlock.enabled = allowed && !learned;
            padlock.color = Unlearned;
        }
    }
}
