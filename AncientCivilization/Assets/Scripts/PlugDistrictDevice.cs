using UnityEngine;

namespace Plugin.PlayerCore
{
    // 본편과 순찰이 같은 기계를 사용한다. 해당 방의 시계와 실제 소켓의 접속/해제만 전달받는다.
    public sealed class PlugDistrictDevice : MonoBehaviour
    {
        static readonly string[] Names = { "상차 호이스트", "수선가 방화 셔터", "서고 이중 승인교", "온실 접이 차양", "첨탑 비상 중계기" };
        static readonly string[] SecondNames = { "상차장 안전 셔터", "공동 건조대", "기록 송신 중계기", "정원 정비 승강대", "비상 이중 인증교" };
        static readonly string[] Hints = {
            "좌클릭을 돌렸다 놓아 화물대에 전원 공급",
            "셔터에 전원을 넣고 위 통로로",
            "왼쪽 연결 · G 회수 후 오른쪽 승인 단자에 투척",
            "전원을 꽂으면 차양이 차례로 펼쳐집니다",
            "왼쪽 연결 · G 회수 후 고공 중계기에 투척"
        };
        static readonly Color Standby = new Color32(78, 92, 100, 255);
        static readonly Color Warning = new Color32(243, 164, 73, 255);
        static PlugDistrictSettings settings;

        PlugDistrictCircuit circuit;
        int floor;
        PlugDistrictDriveArt driveArt;
        Surface lift, gate;
        Surface[] folds;
        float liftHeight, gateOpen;
        bool placeShutter;
        SpriteRenderer[] lamps;
        DistrictWorldSign sign;

        public static string SiteLabel(int floor, bool advanced)
        {
            if ((uint)floor >= Names.Length) throw new System.ArgumentOutOfRangeException(nameof(floor));
            return advanced ? SecondNames[floor] : Names[floor];
        }

        public string Label { get; private set; }
        public int Mechanism { get; private set; }
        public int SocketCount => circuit.SocketCount;
        public bool SocketAvailable(int slot) => circuit.Available(slot);

        public Vector2 SocketPoint(int slot)
        {
            Vector2 local = slot == 0 ? new Vector2(0, 3.2f) :
                Mechanism == 4 ? new Vector2(6.5f, 6f) : new Vector2(5, 4.6f);
            return transform.TransformPoint(local);
        }

        public Vector2 TowPoint(int slot)
        {
            // 승인교 진입 발판에서는 키가 큰 본편 경위의 발까지 디딤면 위에 오도록 한다.
            float clearance = Mechanism == 2 && slot == 0 ? .61f : .32f;
            return SocketPoint(slot) + Vector2.up * clearance;
        }

        public static PlugDistrictDevice Spawn(Transform parent, int floor, Vector2 localGroundOrigin, bool advanced = false)
        {
            if ((uint)floor > 4) throw new System.ArgumentOutOfRangeException(nameof(floor));
            if (!settings)
            {
                settings = Resources.Load<PlugDistrictSettings>("Traversal/DefaultPlugDistrict");
                if (!settings) throw new System.InvalidOperationException("DefaultPlugDistrict 설비 설정이 없습니다.");
            }
            var label = SiteLabel(floor, advanced);
            var host = new GameObject(label);
            host.transform.SetParent(parent, false);
            host.transform.localPosition = localGroundOrigin;
            var device = host.AddComponent<PlugDistrictDevice>();
            device.floor = floor;
            device.Label = label;
            device.Mechanism = PlugDistrictCircuit.KindFor(floor, advanced);
            device.circuit = new PlugDistrictCircuit(device.Mechanism, settings.shutterSeconds, settings.sequenceSeconds,
                settings.relaySeconds, settings.releaseHold, settings.canopyInterval);
            device.Build();
            device.ResetDevice();
            return device;
        }

        void Build()
        {
            lamps = new SpriteRenderer[SocketCount];
            for (int i = 0; i < SocketCount; i++)
            {
                Vector2 socket = transform.InverseTransformPoint(SocketPoint(i));
                PlugDistrictArt.Part(transform, Label, floor, PlugDistrictPiece.Backplate,
                    socket + Vector2.down * .68f, new Vector2(1.24f, 1.15f), 1);
                lamps[i] = PlugDistrictArt.Part(transform, "전원 상태", Mechanism, PlugDistrictPiece.Indicator,
                    socket + new Vector2(.73f, -.66f), Vector2.one * .34f, 5);
            }
            var label = new GameObject("설비 안내");
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector2(-1, 4.48f);
            sign = label.AddComponent<DistrictWorldSign>();
            sign.text = Hints[Mechanism];
            sign.enabled = false;

            if (Mechanism != 0 && Mechanism != 1)
                PlugDistrictArt.Part(transform, "설비 구동부", Mechanism, PlugDistrictPiece.Fixture,
                    new Vector2(2.15f, 2.45f), new Vector2(1.04f, 2.6f), 0);

            switch (Mechanism)
            {
                case 0:
                    lift = MakeSurface("화물 승강대", 3, 1.05f, 3.15f, .42f, moving: true);
                    MakeSurface("상차 검수 발코니", 7.1f, 4.35f, 3.8f, .4f);
                    break;
                case 1:
                    MakeSurface("셔터 앞 복도", .75f, 2.4f, 3.1f, .34f);
                    MakeSurface("셔터 건너 복도", 5.65f, 2.4f, 4.5f, .34f);
                    gate = MakeSurface("방화 셔터", 3, 5.8f, 1.25f, 3.4f, door: true);
                    break;
                case 2:
                    MakeSurface("문서 이송대 입구", .45f, 2.65f, 2.3f, .40f);
                    folds = new[] { MakeSurface("승인교 전개판", 4.65f, 2.95f, 6.2f, .42f) };
                    MakeSurface("문서 이송대 출구", 8.6f, 3.1f, 2f, .40f);
                    break;
                case 3:
                    folds = new[] { MakeSurface("첫 차양", 2f, 1.65f, 2.1f, .30f),
                        MakeSurface("둘째 차양", 4.4f, 2.9f, 2.1f, .30f),
                        MakeSurface("셋째 차양", 6.8f, 4.15f, 2.1f, .30f) };
                    MakeSurface("온실 관측창", 8.9f, 4.55f, 1.95f, .4f);
                    break;
                case 4:
                    folds = new[] { MakeSurface("비상 착지대", 8.0f, 4.65f, 3.25f, .38f) };
                    break;
            }
            var second = SocketCount > 1 ? (Vector2)transform.InverseTransformPoint(SocketPoint(1)) : Vector2.zero;
            driveArt = PlugDistrictDriveArt.Build(transform, Mechanism, lift?.root,
                transform.InverseTransformPoint(SocketPoint(0)), second);
        }

        sealed class Surface
        {
            public Transform root;
            public SpriteRenderer art;
            public BoxCollider2D box;
            public Rigidbody2D carrier;
            public Vector2 center, size;
            public float extension;
        }

        Surface MakeSurface(string label, float x, float top, float width, float height,
            bool moving = false, bool door = false)
        {
            var host = new GameObject(label);
            host.layer = 8;
            var at = new Vector2(x, top - height * .5f);
            host.transform.SetParent(transform, false);
            host.transform.localPosition = at;
            var box = host.AddComponent<BoxCollider2D>();
            var size = new Vector2(width, height);
            box.size = size;
            if (!door)
            {
                var oneWay = host.AddComponent<PlatformEffector2D>();
                oneWay.useOneWay = true;
                oneWay.surfaceArc = 155;
                box.usedByEffector = true;
            }
            var art = PlugDistrictArt.Part(host.transform, label, door ? Mechanism : floor,
                door ? PlugDistrictPiece.Fixture : PlugDistrictPiece.Deck, Vector2.zero, size, 3);
            Rigidbody2D rb = null;
            if (moving)
            {
                rb = host.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
            return new Surface { root = host.transform, art = art,
                box = box, carrier = rb, center = at, size = size, extension = 1 };
        }

        public void Dock(int slot)
        {
            if (circuit.Dock(slot)) RefreshLamps();
        }

        public void Release(int slot) => circuit.Release(slot);

        public void ResetDevice()
        {
            circuit.Reset();
            if (lift != null)
            {
                liftHeight = lift.center.y;
                lift.root.localPosition = lift.center;
                lift.carrier.position = transform.TransformPoint(lift.center);
            }
            if (gate != null)
            {
                // 재시도 직후 몸과 겹치는 위치에 닫힌 벽을 만들지 않는다.
                gateOpen = 1;
                placeShutter = true;
                SetGate(1);
            }
            if (folds != null)
                foreach (var surface in folds) SetExtension(surface, 0);
            sign.enabled = false;
            RefreshLamps();
        }

        public void Step(float dt, Vector2 playerWorld)
        {
            if (dt <= 0 || !isActiveAndEnabled) return;
            circuit.Step(dt);
            Vector2 hero = transform.InverseTransformPoint(playerWorld);
            bool readSign = Mathf.Abs(hero.x - 1f) < 5.8f && Mathf.Abs(hero.y - 2.5f) < 4;
            if (sign.enabled != readSign) sign.enabled = readSign;
            if (lift != null)
            {
                float target = lift.center.y + (circuit.WantsSurface(0) ? 3.25f : 0);
                float next = Mathf.MoveTowards(liftHeight, target, settings.liftSpeed * dt);
                if (next != liftHeight)
                {
                    liftHeight = next;
                    lift.carrier.MovePosition(transform.TransformPoint(new Vector2(lift.center.x, next)));
                }
            }
            if (gate != null)
            {
                float goal = circuit.WantsSurface(0) ? 1 : 0;
                // 입장 직후 빈 통로에는 처음부터 닫힌 셔터를 배치한다.
                float next = placeShutter ? goal : Mathf.MoveTowards(gateOpen, goal,
                    dt / Mathf.Max(.05f, settings.unfoldSeconds * 1.55f));
                placeShutter = false;
                // 패널이 차지할 새 영역 전체가 비어 있을 때만 닫는다.
                float top = gate.center.y + gate.size.y * .5f;
                if (next < gateOpen && Mathf.Abs(hero.x - gate.center.x) < gate.size.x * .5f + .44f &&
                    hero.y + 1.13f > top - gate.size.y * (1 - next) && hero.y - 1.13f < top)
                    next = gateOpen;
                if (gateOpen != next) { gateOpen = next; SetGate(next); }
            }
            if (folds != null)
                for (int i = 0; i < folds.Length; i++) StepFold(folds[i], circuit.WantsSurface(i), hero, dt);
            RefreshLamps(dt);
        }

        void StepFold(Surface surface, bool deploy, Vector2 hero, float dt)
        {
            float next = Mathf.MoveTowards(surface.extension, deploy ? 1 : 0,
                dt / Mathf.Max(.05f, settings.unfoldSeconds));
            if (next == surface.extension) return;
            float left = surface.center.x - surface.size.x * .5f;
            float top = surface.center.y + surface.size.y * .5f;
            bool nearHeight = hero.y > top + .74f && hero.y < top + 1.21f;
            if (next < surface.extension && nearHeight && hero.x > left - .37f &&
                hero.x < left + surface.size.x * surface.extension + .37f) return;
            // 전개되는 끝단이 캐릭터 몸 안에서 생성되는 경우만 기다린다.
            if (next > surface.extension && !nearHeight && hero.y + 1.1f > top - surface.size.y && hero.y - 1.1f < top &&
                hero.x + .38f > left + surface.size.x * surface.extension &&
                hero.x - .38f < left + surface.size.x * next) return;
            SetExtension(surface, next);
        }

        static void SetExtension(Surface surface, float fraction)
        {
            surface.extension = fraction;
            var offset = new Vector2((fraction - 1) * surface.size.x * .5f, 0);
            surface.art.transform.localPosition = offset;
            surface.art.enabled = fraction > .03f;
            if (surface.art.enabled) surface.art.size = new Vector2(surface.size.x * fraction, surface.size.y);
            surface.box.enabled = fraction > .03f;
            if (surface.box.enabled)
            {
                surface.box.size = new Vector2(surface.size.x * fraction, surface.size.y);
                surface.box.offset = offset;
            }
        }

        void SetGate(float open)
        {
            float closed = 1 - open;
            var offset = Vector2.up * (gate.size.y * open * .5f);
            gate.art.transform.localPosition = offset;
            gate.art.enabled = closed > .02f;
            if (gate.art.enabled) gate.art.size = new Vector2(gate.size.x, gate.size.y * closed);
            gate.box.enabled = closed > .02f;
            if (gate.box.enabled)
            {
                gate.box.offset = offset;
                gate.box.size = new Vector2(gate.size.x, gate.size.y * closed);
            }
        }

        void RefreshLamps(float dt = 0)
        {
            driveArt?.Signal(dt, circuit.Powered && !circuit.Completed);
            bool timed = (Mechanism == 1 || Mechanism == 4 || Mechanism == 2 && !circuit.Completed);
            bool aboutToClose = timed && !(Mechanism == 4 && circuit.Linked == 1) &&
                circuit.Remaining > 0 && circuit.Remaining < 1.15f;
            Color live = aboutToClose ? Warning : BluePlugArt.MovementColor;
            if (aboutToClose) live.a = .66f + .34f * Mathf.Abs(Mathf.Sin(circuit.Remaining * 9));
            for (int i = 0; i < lamps.Length; i++)
            {
                bool on = i == 0 ? circuit.Powered || circuit.Completed : circuit.Available(i);
                lamps[i].color = on ? live : Standby;
            }
        }
    }
}
