using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientJokerArena
    {
        const float LandingY = 2.85f, RailY = 5.95f, DeckDepth = .22f;
        const float FoldWarning = .52f, FoldInterval = .31f, FoldSeconds = .24f, FoldHold = 1.65f;
        static readonly Vector2 Unavailable = new Vector2(-100000, -100000);
        static readonly Color LampIdle = new Color(.32f,.61f,.66f);
        static readonly Color LampWarn = new Color(1f,.65f,.26f);
        static readonly Color LampSafe = new Color(.67f,1f,.84f);
        static readonly float[] PanelX = { .215f, .287f, .458f, .542f, .713f, .785f };
        static readonly float[] PanelY = { 3.42f, 3.57f, 4.25f, 4.25f, 3.57f, 3.42f };

        enum Prop { Rail, Mirror, LitMirror, Shutter, OpenShutter, Deck, Shadow, Ring }
        static readonly Rect[] Frames = {
            new Rect(0,0,96,48), new Rect(96,0,32,56), new Rect(128,0,32,56),
            new Rect(160,0,48,70), new Rect(208,0,48,70), new Rect(0,76,96,22),
            new Rect(104,76,52,18), new Rect(168,76,32,32)
        };
        static readonly Sprite[] Sprites = new Sprite[Frames.Length];

        sealed class Deck
        {
            public Rigidbody2D body;
            public BoxCollider2D solid;
            public Transform tread;
            public SpriteRenderer lamp;
            public Vector2 home;
            public float width;
            public int foldIndex = -1;
        }

        readonly AncientGame game;
        readonly GameObject scenery;
        readonly float left, right;
        readonly int ringSlot;
        readonly SpriteRenderer[] mirrors = new SpriteRenderer[2], shutters = new SpriteRenderer[2];
        readonly Deck[] landings = new Deck[2], panels = new Deck[PanelX.Length];
        readonly SpriteRenderer shadow, ring;
        int phase, markedSide = -1, groundFrame = -1, groundMask;
        bool disposed, ringLive, rewinding, folding;
        float rewindTime, foldTime, restoreAt;

        public float EscapeHeight => 1.08f;

        public AncientJokerArena(AncientGame owner, Transform parent, float left, float right)
        {
            game = owner;
            this.left = left;
            this.right = right;
            scenery = new GameObject("조커 · 철골 난간과 옥상");
            // 전투 컴포넌트가 보스에 붙어 있어도 철골과 착지점의 월드 좌표는 고정한다.
            scenery.transform.SetParent(parent, false);
            scenery.transform.position = Vector3.zero;
            if (game.JokerAnchorSlot < 0)
            {
                game.JokerAnchorSlot = game.anchors.Count;
                game.anchors.Add(Unavailable);
            }
            ringSlot = game.JokerAnchorSlot;
            game.anchors[ringSlot] = Unavailable;

            const int bays = 8;
            float span = (right-left)/bays;
            for (int i = 0; i < bays; i++)
            {
                var section = Art("후경 철교 트러스",Prop.Rail,
                    new Vector2(left+(i+.5f)*span,RailY+.25f),-1);
                section.transform.localScale = new Vector3(span/(96f/24),1,1);
            }
            for (int side = 0; side < 2; side++)
            {
                mirrors[side] = Art("도탄 거울",Prop.Mirror,MirrorPoint(side),4);
                mirrors[side].flipX = side == 1;
                shutters[side] = Art("진입 셔터",Prop.Shutter,LandingPoint(side)+Vector2.up*1.44f,2);
                landings[side] = MakeDeck("셔터 앞 옥상",LandingPoint(side),3.3f);
            }
            for (int i = 0; i < panels.Length; i++)
            {
                Vector2 point = new Vector2(Mathf.Lerp(left,right,PanelX[i]),PanelY[i]);
                panels[i] = MakeDeck("옥상 가동 발판",point,i==2 || i==3 ? 2.17f : 1.70f);
            }
            shadow = Art("진짜 모자 그림자",Prop.Shadow,Vector2.zero,9);
            ring = Art("공중 낚아채기 · 와이어 고리",Prop.Ring,Vector2.zero,14);
            ring.gameObject.SetActive(false);
            shadow.enabled = false;
            SetPhase(1);
        }

        static Sprite GetSprite(Prop prop)
        {
            int i = (int)prop;
            if (!Sprites[i])
                Sprites[i] = AncientArt.Slice("JokerRooftops","joker-rooftop-"+prop,
                    Frames[i],24,Vector2.one*.5f,false);
            return Sprites[i];
        }

        SpriteRenderer Art(string name, Prop prop, Vector2 point, int order, Transform parent = null)
        {
            return AncientArt.Display(name,GetSprite(prop),point,order,parent ? parent : scenery.transform);
        }

        Deck MakeDeck(string name, Vector2 top, float width)
        {
            var platform = new GameObject(name);
            platform.layer = 8;
            platform.transform.SetParent(scenery.transform,false);
            Vector2 home = top-Vector2.up*(DeckDepth*.5f);
            platform.transform.position = home;
            var body = platform.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var solid = platform.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(width,DeckDepth);
            // 아래 통로에서 뛰어오를 때는 통과하고, 내려올 때에는 발판에 착지한다.
            var effector = platform.AddComponent<PlatformEffector2D>();
            solid.usedByEffector = true;
            effector.useOneWay = true;
            effector.surfaceArc = 130;

            var hinge = new GameObject("힌지");
            hinge.transform.SetParent(platform.transform,false);
            hinge.transform.localPosition = new Vector2(-width*.5f,DeckDepth*.5f);
            var art = Art("금속 디딤판",Prop.Deck,new Vector2(width*.5f,-.23f),6,hinge.transform);
            art.transform.localScale = new Vector3(width/(96f/24),.46f/(22f/24),1);
            var lamp = game.Box("힌지 상태등",Vector2.zero,new Vector2(width*.42f,.065f),LampIdle,7);
            lamp.transform.SetParent(hinge.transform,false);
            lamp.transform.localPosition = new Vector2(width*.5f,-.085f);
            return new Deck { body=body, solid=solid, tread=hinge.transform,
                lamp=lamp.GetComponent<SpriteRenderer>(), home=home, width=width };
        }

        public Vector2 MirrorPoint(int side) => new Vector2(side == 0 ? left+1.35f : right-1.35f,5.55f);
        public Vector2 RailPoint(int side) => new Vector2(side == 0 ? left+4.75f : right-4.75f,RailY);
        public Vector2 LandingPoint(int side) => new Vector2(side == 0 ? left+3.85f : right-3.85f,LandingY);

        public void SetMirrors(int first, int litCount)
        {
            if (disposed) return;
            for (int side = 0; side < 2; side++)
                mirrors[side].sprite = GetSprite(litCount >= 2 || litCount == 1 && side == first ? Prop.LitMirror : Prop.Mirror);
        }

        public void MarkReturn(int side)
        {
            if (disposed || phase < 2 || phase > 5 || side < 0 || side > 1) return;
            if (markedSide >= 0 && markedSide != side) shutters[markedSide].sprite = GetSprite(Prop.Shutter);
            markedSide = side;
            shutters[side].sprite = GetSprite(Prop.OpenShutter);
            shadow.transform.position = LandingPoint(side) + Vector2.up*.12f;
            shadow.transform.localScale = new Vector3(side == 0 ? .85f : -.85f,.65f,1);
            shadow.enabled = true;
        }

        public void SetRing(Vector2 point)
        {
            if (disposed || phase != 4) return;
            if (!ringLive) ring.gameObject.SetActive(true);
            ringLive = true;
            ring.transform.position = point;
            game.anchors[ringSlot] = point;
        }

        public bool TryAttach(Vector2 selected, out Transform moving)
        {
            moving = null;
            if (disposed || !ringLive || (selected-(Vector2)ring.transform.position).sqrMagnitude > .18f*.18f) return false;
            moving = ring.transform;
            return true;
        }

        public void ClearReturn()
        {
            if (disposed) return;
            if (markedSide >= 0) shutters[markedSide].sprite = GetSprite(Prop.Shutter);
            markedSide = -1;
            shadow.enabled = false;
            CloseRing();
        }

        public void CloseRing()
        {
            if (disposed || !ringLive) return;
            // 같은 프레임에 새 보스가 생성되어도 이전 보스가 새 소켓을 지우지 않는다.
            if (game && (!game.Sniper || game.Sniper.Arena == null || ReferenceEquals(game.Sniper.Arena,this)))
            {
                if (game.hero) game.hero.ReleaseWireAt(ring.transform.position);
                game.anchors[ringSlot] = Unavailable;
            }
            ringLive = false;
            ring.gameObject.SetActive(false);
        }

        bool Occupied(Deck deck)
        {
            if (!game.hero) return false;
            Vector2 p = game.hero.transform.position;
            var centre = deck.body.position;
            return Mathf.Abs(p.x-centre.x) < deck.width*.5f+.38f &&
                Mathf.Abs(p.y-centre.y) < 1.05f+DeckDepth*.5f-.015f;
        }

        void SetDeck(Deck deck, bool active)
        {
            deck.body.gameObject.SetActive(active);
            if (active) deck.body.position = deck.home;
            deck.solid.enabled = active && !Occupied(deck);
            deck.tread.localRotation = Quaternion.identity;
            deck.lamp.color = LampIdle;
            deck.foldIndex = -1;
        }

        public void SetPhase(int next)
        {
            if (disposed) return;
            ResetPattern();
            phase = next;
            rewinding = false;
            groundFrame = -1;
            bool shuttersShown = next >= 2 && next <= 5;
            for (int i = 0; i < 2; i++)
            {
                mirrors[i].enabled = next < 6;
                shutters[i].enabled = shuttersShown;
                SetDeck(landings[i],shuttersShown && next != 3);
            }
            for (int i = 0; i < panels.Length; i++)
                SetDeck(panels[i],next == 3 && (i == 1 || i == 4) || next == 4 || next == 5);
        }

        public void BeginRewind()
        {
            if (disposed || phase != 3 || rewinding) return;
            rewinding = true;
            rewindTime = 0;
            panels[1].lamp.color = panels[4].lamp.color = LampWarn;
        }

        public void FixedTick(float dt)
        {
            if (disposed || phase != 3 || game.Paused || !rewinding || dt <= 0) return;
            rewindTime += dt;
            // 가감속까지 대칭인 왕복. MovePosition의 실제 물리 속도를 플레이어가 물려받는다.
            const float warning = .42f, period = 3.1f;
            float t = Mathf.Clamp01((rewindTime-warning)/period);
            float displacement = (1-Mathf.Cos(t*2*Mathf.PI))*1.16f;
            for (int i = 1; i <= 4; i += 3)
            {
                var roof = panels[i];
                roof.body.MovePosition(roof.home+Vector2.right*(i == 1 ? displacement : -displacement));
            }
            if (t >= 1)
            {
                rewinding = false;
                panels[1].lamp.color = panels[4].lamp.color = LampIdle;
            }
        }

        int Lane(float x)
        {
            float u = Mathf.InverseLerp(left,right,x);
            return u < 1f/3 ? 0 : u < 2f/3 ? 1 : 2;
        }

        bool GroundRoute(int lane)
        {
            float width = (right-left)/3;
            float start = left+lane*width+.28f, end = start+width-.56f;
            // 복구되지 않은 수류탄 구멍(한 칸 1.5)을 건너뛰지 않는 간격으로 실제 바닥을 확인한다.
            int pieces = Mathf.CeilToInt((end-start)/.75f);
            for (int i = 0; i <= pieces; i++)
                if (!Physics2D.Raycast(new Vector2(Mathf.Lerp(start,end,i/(float)pieces),.15f),Vector2.down,.42f,1<<8)) return false;
            return true;
        }

        int GroundLanes()
        {
            if (groundFrame == Time.frameCount) return groundMask;
            groundFrame = Time.frameCount;
            groundMask = 0;
            for (int lane = 0; lane < 3; lane++)
                if (GroundRoute(lane)) groundMask |= 1<<lane;
            return groundMask;
        }

        public int SupportedLanes
        {
            get
            {
                if (disposed) return 0;
                int mask = GroundLanes();
                for (int lane = 0; lane < 3; lane++)
                {
                    if ((mask & (1<<lane)) != 0) continue;
                    for (int i = lane*2; i < lane*2+2; i++)
                        if (panels[i].solid.enabled) mask |= 1<<lane;
                }
                return mask;
            }
        }

        public bool CanFold(int fromSide, float heroX)
        {
            if (disposed || phase != 5 || folding || fromSide < 0 || fromSide > 1 ||
                heroX < left-1f || heroX > right+1f) return false;
            return GroundLanes() == 7;
        }

        public void BeginFold(int fromSide)
        {
            if (game.hero) BeginFold(fromSide,Lane(game.hero.transform.position.x));
        }

        public void BeginFold(int fromSide, int protectedLane)
        {
            if (!game.hero || protectedLane < 0 || protectedLane > 2 ||
                !CanFold(fromSide,game.hero.transform.position.x)) return;
            folding = true;
            foldTime = 0;
            int order = 0;
            for (int step = 0; step < panels.Length; step++)
            {
                int i = fromSide == 0 ? step : panels.Length-1-step;
                var deck = panels[i];
                // 각 레인에 두 발판이 있다. 회피 경로의 두 발판은 접히지 않는다.
                if (i/2 == protectedLane) { deck.foldIndex = -1; deck.lamp.color = LampSafe; }
                else deck.foldIndex = order++;
            }
            restoreAt = FoldWarning+FoldInterval*(order-1)+FoldSeconds+FoldHold;
        }

        public void Tick()
        {
            if (disposed || game.Paused) return;
            if (folding) foldTime += Time.deltaTime;
            for (int i = 0; i < panels.Length; i++)
            {
                Deck deck = panels[i];
                if (!deck.body.gameObject.activeSelf) continue;
                if (folding && deck.foldIndex >= 0)
                {
                    float warnAt = FoldInterval*deck.foldIndex;
                    float close = Mathf.Clamp01((foldTime-warnAt-FoldWarning)/FoldSeconds);
                    float reopen = Mathf.Clamp01((foldTime-restoreAt)/FoldSeconds);
                    float angle = close*(1-reopen);
                    deck.tread.localRotation = Quaternion.Euler(0,0,85f*angle);
                    deck.lamp.color = foldTime >= warnAt && reopen < 1 ? LampWarn : LampIdle;
                    if (angle > .01f) { deck.solid.enabled = false; continue; }
                }
                if (!deck.solid.enabled && !Occupied(deck)) deck.solid.enabled = true;
            }
            for (int i = 0; i < landings.Length; i++)
            {
                var landing = landings[i];
                if (landing.body.gameObject.activeSelf && !landing.solid.enabled && !Occupied(landing))
                    landing.solid.enabled = true;
            }
            if (folding && foldTime >= restoreAt+FoldSeconds) EndFolds();
        }

        void EndFolds()
        {
            folding = false;
            foreach (Deck deck in panels)
            {
                deck.foldIndex = -1;
                deck.tread.localRotation = Quaternion.identity;
                deck.lamp.color = LampIdle;
                if (deck.body.gameObject.activeSelf && !deck.solid.enabled && !Occupied(deck)) deck.solid.enabled = true;
            }
        }

        public void ResetPattern()
        {
            if (disposed) return;
            ClearReturn();
            SetMirrors(0,0);
            if (folding) EndFolds();
        }

        public void Dispose()
        {
            if (disposed) return;
            ClearReturn();
            disposed = true;
            if (scenery)
            {
                scenery.SetActive(false);
                Object.Destroy(scenery);
            }
        }
    }
}
