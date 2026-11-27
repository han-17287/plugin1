using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 전조선이나 충돌 판정을 소유하지 않는다. 실제 전투 이벤트가 들어온 순간의 접점만 그린다.
    public sealed class AncientBossCue : MonoBehaviour
    {
        public enum Style { Captain, Mech, Enforcer, Orbital, Joker, Clock, Convoy, Guardian }
        public enum Beat { Tell, Strike, Contact }

        const int Frames = 8, Beats = 3, Styles = 8, Capacity = 36;
        const int CellWidth = 112, CellHeight = 80;
        [SerializeField, Min(.05f)] float tellSeconds = .31f;
        [SerializeField, Min(.05f)] float strikeSeconds = .155f;
        [SerializeField, Min(.05f)] float contactSeconds = .23f;

        struct Slot
        {
            public SpriteRenderer view;
            public Style style;
            public int first;
            public float age, duration;
            public Transform support;
            public Vector3 localPoint, localDirection;
            public bool attached;
        }

        AncientGame game;
        Texture2D atlas;
        BossFlourishMesh flourishes;
        Transform root;
        readonly Slot[] slots = new Slot[Capacity];
        readonly Sprite[] frames = new Sprite[Styles * Beats * Frames];
        int cursor;

        public static void Play(AncientGame game, Style style, Beat beat, Vector2 point, Vector2 direction,
            float worldScale = 1f, Transform support = null)
        {
            if (!game || game.won || game.MainMenu || game.InSniperFinalPhase || worldScale <= 0) return;
            Prewarm(game);
            var cues = game.BossCues;
            if (cues.atlas) cues.Show(style, beat, point, direction, worldScale, support);
        }

        internal static void Prewarm(AncientGame host)
        {
            if (host.BossCues) return;
            var cues = host.gameObject.AddComponent<AncientBossCue>();
            host.BossCues = cues;
            cues.Initialize(host);
        }

        void Initialize(AncientGame host)
        {
            game = host;
            atlas = Resources.Load<Texture2D>("Art/BossCueAtlas");
            if (!atlas) { enabled = false; return; }
            // 전투에 들어가기 전 한 번 잘라 둔다. 발사·명중 프레임에는 풀의 참조만 교체한다.
            for (int row = 0; row < Styles*Beats; row++)
                for (int i = 0; i < Frames; i++)
                    frames[row*Frames+i] = Sprite.Create(atlas,
                        new Rect(i*CellWidth, (Styles*Beats-row-1)*CellHeight, CellWidth, CellHeight),
                        new Vector2(40f/CellWidth, .5f), 47f, 0, SpriteMeshType.FullRect);
            root = new GameObject("보스 공격 문양").transform;
            root.SetParent(host.transform, false);
            for (int i = 0; i < slots.Length; i++)
            {
                var view = new GameObject("접점").AddComponent<SpriteRenderer>();
                view.transform.SetParent(root, false);
                view.sortingOrder = 16;
                view.enabled = false;
                slots[i].view = view;
            }
            flourishes = new BossFlourishMesh(root);
            enabled = false;
        }

        void Show(Style style, Beat beat, Vector2 point, Vector2 direction, float size, Transform support)
        {
            int row = (int)style * Beats + (int)beat;
            int first = row * Frames;
            int index = cursor;
            for (int i = 0; i < slots.Length; i++)
            {
                index = (cursor+i) % Capacity;
                if (!slots[index].view.enabled) break;
                if (i == slots.Length-1) index = cursor;
            }
            cursor = (index+1) % Capacity;
            ref Slot slot = ref slots[index];
            slot.style = style;
            slot.first = first;
            slot.age = 0;
            slot.duration = beat == Beat.Tell ? tellSeconds : beat == Beat.Strike ? strikeSeconds : contactSeconds;
            Vector3 axis = direction.sqrMagnitude > .00001f ? direction.normalized : Vector2.right;
            slot.attached = support;
            slot.support = support;
            slot.localPoint = support ? support.InverseTransformPoint(point) : (Vector3)point;
            slot.localDirection = support ? support.InverseTransformDirection(axis) : axis;
            bool reduced = game.ReducedFeedback || !AncientVisuals.HighQuality;
            float scale = size * (reduced ? .80f : 1f);
            slot.view.transform.localScale = Vector3.one * scale;
            slot.view.color = new Color(1, 1, 1, reduced ? .64f : .96f);
            slot.view.sprite = frames[first];
            Place(ref slot);
            slot.view.enabled = true;
            if (!reduced) flourishes.Spawn(style,beat,point,axis,size,support,slot.duration);
            enabled = true;
        }

        static void Place(ref Slot slot)
        {
            Vector3 point = slot.support ? slot.support.TransformPoint(slot.localPoint) : slot.localPoint;
            Vector3 axis = slot.support ? slot.support.TransformDirection(slot.localDirection) : slot.localDirection;
            slot.view.transform.SetPositionAndRotation(point, Quaternion.Euler(0, 0, Mathf.Atan2(axis.y, axis.x)*Mathf.Rad2Deg));
        }

        void LateUpdate()
        {
            if (!game || game.MainMenu || game.won || game.InSniperFinalPhase)
            { HideAll(); return; }
            bool rolling = game.Finale && game.Finale.AdvancingCinematicCues;
            float dt = game.Paused ? 0f : Time.deltaTime;
            bool active = false;
            for (int i = 0; i < slots.Length; i++)
            {
                ref Slot s = ref slots[i];
                if (!s.view || !s.view.enabled) continue;
                s.age += s.style == Style.Convoy && rolling ? Mathf.Min(Time.unscaledDeltaTime, .25f) : dt;
                if (s.age >= s.duration || (s.attached && (!s.support || !s.support.gameObject.activeInHierarchy)))
                { s.view.enabled = false; continue; }
                if (s.attached) Place(ref s);
                s.view.sprite = frames[s.first + Mathf.Min(Frames-1,(int)(Frames*s.age/s.duration))];
                active = true;
            }
            if (game.ReducedFeedback || !AncientVisuals.HighQuality) flourishes.Hide();
            else active |= flourishes.Tick(dt, rolling ? Mathf.Min(Time.unscaledDeltaTime,.25f) : dt);
            enabled = active;
        }

        internal static void DrawFlourish(BossFlourishBrush b, Style style, Beat beat, float t, bool primaryLight = true)
        {
            // 한 발사구에서 여러 갈래가 나가도 중심의 빛은 한 번만 겹친다.
            if (primaryLight)
            {
                Color tint;
                switch (style)
                {
                    case Style.Captain: tint = new Color(1,.38f,.10f); break;
                    case Style.Mech: tint = new Color(1,.65f,.15f); break;
                    case Style.Enforcer: tint = new Color(.24f,.79f,1); break;
                    case Style.Orbital: tint = new Color(.48f,.38f,1); break;
                    case Style.Joker: tint = new Color(1,.30f,.46f); break;
                    case Style.Clock: tint = new Color(1,.72f,.28f); break;
                    case Style.Convoy: tint = new Color(.86f,.46f,1); break;
                    default: tint = new Color(.84f,.70f,.43f); break;
                }
                bool fired = beat == Beat.Strike;
                float energy = beat == Beat.Tell ? .11f*(1-t) : .35f*(1-t)*(1-t);
                tint.a = energy;
                b.Halo(Vector2.zero, fired ? .9f+.42f*t : .7f+.34f*t, .54f+.23f*t, tint);
            }
            switch (style)
            {
                case Style.Clock: case Style.Enforcer: AuthorityBossFlourishes.Draw(b,style,beat,t); break;
                case Style.Joker: case Style.Orbital: PrismBossFlourishes.Draw(b,style,beat,t); break;
                default: FoundryBossFlourishes.Draw(b,style,beat,t); break;
            }
        }

        public static void Clear(AncientGame game, Style style)
        {
            if (!game || !game.BossCues) return;
            var cues = game.BossCues;
            cues.flourishes?.Clear(style);
            for (int i = 0; i < Capacity; i++)
                if (cues.slots[i].style == style && cues.slots[i].view) cues.slots[i].view.enabled = false;
        }
        public static void ClearAll(AncientGame game) { if (game && game.BossCues) game.BossCues.HideAll(); }
        void HideAll() { enabled = false; }
        void OnDisable()
        {
            flourishes?.Hide();
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].view) slots[i].view.enabled = false;
        }
        void OnDestroy()
        {
            flourishes?.Dispose();
            if (root) Destroy(root.gameObject);
            for (int i = 0; i < frames.Length; i++) if (frames[i]) Destroy(frames[i]);
        }
    }
}
