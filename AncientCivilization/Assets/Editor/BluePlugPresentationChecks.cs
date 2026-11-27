#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Plugin.PlayerCore;
using UnityEditor;
using UnityEngine;

namespace Plugin.AncientCivilization.EditorChecks
{
    public static class BluePlugPresentationChecks
    {
        const string Menu = "PLUGIN/P2/플러그 양손·전선 연출 검사";
        const string CaptureMenu = "PLUGIN/P2/실제 플레이 플러그 촬영 시작·종료";
        const float PoseStep = 1f / 60f;
        const float Tolerance = .002f;

        static BluePlugVisuals captureOwner;
        static Coroutine recording;
        static LaundryLanding landing;

        [MenuItem(CaptureMenu, true)]
        static bool CanCapture() => EditorApplication.isPlaying && !EditorApplication.isCompiling;

        [MenuItem(CaptureMenu)]
        static void ToggleCapture()
        {
            landing?.Dispose();
            landing = null;
            var patrol = UnityEngine.Object.FindAnyObjectByType<PatrolExpedition>();
            var game = AncientGame.Instance;
            var hero = game ? game.hero : null;
            BluePlugVisuals owner = patrol && patrol.Player ? patrol.Player.GetComponent<BluePlugVisuals>() :
                hero ? hero.GetComponent<BluePlugVisuals>() : null;
            if (captureOwner && recording != null)
            {
                bool same = captureOwner == owner && captureOwner.isActiveAndEnabled;
                captureOwner.StopCoroutine(recording);
                captureOwner = null;
                recording = null;
                if (same)
                {
                    Debug.Log("플러그 촬영을 마쳤습니다.");
                    return;
                }
            }
            if (!owner || !owner.isActiveAndEnabled || (!patrol && game.MainMenu))
            {
                Debug.LogWarning("본편이나 빨래골목에 들어가 Game 뷰를 연 후 촬영을 시작하세요.");
                return;
            }
            // 실제 카메라가 그린 프레임만 저장한다. 조작·시간·스킬·카메라는 바꾸지 않는다.
            string directory = Path.Combine(Application.persistentDataPath, "PlugCaptures",
                DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(directory);
            captureOwner = owner;
            if (patrol && patrol.BluePlug) landing = new LaundryLanding(patrol);
            recording = owner.StartCoroutine(CaptureGameplay(owner, patrol, directory));
            Debug.Log("플러그 촬영 중. Game 뷰에서 플레이하면 상태별 첫 화면이 여기에 저장됩니다. " + directory);
        }

        static IEnumerator CaptureGameplay(BluePlugVisuals owner, PatrolExpedition patrol, string directory)
        {
            var endOfFrame = new WaitForEndOfFrame();
            var saved = new HashSet<string>();
            var before = BluePlugPhase.Ready;
            float phaseSince = Time.time;
            while (owner && owner.isActiveAndEnabled)
            {
                yield return endOfFrame;
                if (!owner || !owner.isActiveAndEnabled) yield break;
                if (patrol ? patrol.Paused : AncientGame.Instance && AncientGame.Instance.Paused) continue;
                var phase = owner.PresentedPhase;
                if (phase != before) phaseSince = Time.time;
                var playerPlug = patrol ? patrol.BluePlug : null;
                string label = null;
                if (playerPlug && playerPlug.WallResting)
                    label = playerPlug.RestingSocket.WallOutwardSign < 0 ? "wall-on-right" : "wall-on-left";
                else if (phase != BluePlugPhase.Ready)
                {
                    label = phase.ToString();
                    if (Time.time - phaseSince >= .065f &&
                        (phase == BluePlugPhase.Flying || phase == BluePlugPhase.Towing || phase == BluePlugPhase.Swinging))
                        label += "-in-motion";
                }
                else if (before == BluePlugPhase.Swinging || before == BluePlugPhase.Towing)
                    label = before == BluePlugPhase.Swinging ? "swing-exit" : "tow-exit";
                before = phase;
                string arrival = landing?.Observe(phase);
                if (label == null && arrival == null) continue;
                string route = patrol ? (patrol.SocketRoute ? "socket-" : "patrol-") + patrol.RoomId : "campaign";
                string key = label == null ? null : route + "-" + label;
                string landingKey = arrival == null ? null : route + "-" + arrival;
                if (key != null && saved.Contains(key)) key = null;
                if (landingKey != null && saved.Contains(landingKey)) landingKey = null;
                if (key == null && landingKey == null) continue;
                var image = ScreenCapture.CaptureScreenshotAsTexture();
                try
                {
                    if (image)
                    {
                        byte[] png = image.EncodeToPNG();
                        Save(key, png);
                        Save(landingKey, png);
                    }
                }
                finally { if (image) UnityEngine.Object.Destroy(image); }
            }

            void Save(string key, byte[] png)
            {
                if (key == null) return;
                string path = Path.Combine(directory, key + ".png");
                File.WriteAllBytes(path, png);
                saved.Add(key);
                Debug.Log("실제 플레이 프레임 저장 " + path);
            }
        }

        // 실제 공용 소켓에서 한 번의 이동을 시작한 경우만 추적한다. 바닥 이름만으로 경로 성공을 판정하지 않는다.
        sealed class LaundryLanding : IDisposable
        {
            readonly PatrolExpedition patrol;
            readonly PlayerBluePlug plug;
            readonly Rigidbody2D body;
            readonly RaycastHit2D[] supports = new RaycastHit2D[12];
            readonly ContactFilter2D filter;
            BluePlugSocket outlet;
            Collider2D deck;
            BluePlugPhase travel;
            int arrivalBefore;

            internal LaundryLanding(PatrolExpedition expedition)
            {
                patrol = expedition;
                plug = patrol.BluePlug;
                body = patrol.Player.GetComponent<Rigidbody2D>();
                filter = new ContactFilter2D { useLayerMask = true, layerMask = patrol.Player.data.solidLayers };
                plug.LinkReset += Reset;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }

            internal string Observe(BluePlugPhase phase)
            {
                bool wallStreet = patrol.SocketRoute && patrol.RoomId == 11;
                if (!wallStreet && (patrol.SocketRoute || patrol.RoomId != 12)) { Reset(); return null; }
                if (phase == BluePlugPhase.Towing || phase == BluePlugPhase.Swinging)
                {
                    if (outlet && travel == phase) return null;
                    Reset();
                    var source = plug.SeatedSocket;
                    if (!source || source.name != (wallStreet ? "빨래골목 공용 벽 소켓" : "파란 플러그 전용 콘센트")) return null;
                    bool tow = phase == BluePlugPhase.Towing;
                    string platform = wallStreet ? (tow ? "견인 정비 옥상" : "스윙 수선집 창턱") :
                        (tow ? "견인 도착 · 세탁소 옥상" : "스윙 도착 · 수선집 창턱");
                    var room = source.GetComponentInParent<PatrolRoom>();
                    var target = room ? room.transform.Find(platform) : null;
                    if (!target || !target.TryGetComponent(out deck)) return null;
                    travel = phase;
                    outlet = source;
                    arrivalBefore = plug.ArrivalSequence;
                    return null;
                }
                if (!outlet || !deck) return null;
                if (phase != BluePlugPhase.Ready || travel == BluePlugPhase.Towing &&
                    (plug.LastArrival != outlet || plug.ArrivalSequence == arrivalBefore)) { Reset(); return null; }
                if (!patrol.Player.Grounded) return null;
                // 플레이어의 기존 지면 검사와 같은 몸체·거리·법선 기준으로 현재 지지면을 읽기만 한다.
                var data = patrol.Player.data;
                int count = body.Cast(Vector2.down, filter, supports, data.groundProbe);
                bool grounded = false, onDeck = false;
                for (int i = 0; i < count; i++)
                {
                    var hit = supports[i];
                    if (hit.normal.y < data.groundNormal) continue;
                    grounded = true;
                    onDeck |= hit.collider == deck;
                }
                if (!grounded) return null;
                string result = onDeck ? travel == BluePlugPhase.Towing ? "tow-rooftop-landing" : "swing-window-landing" : null;
                Reset();
                return result;
            }

            void Reset() { outlet = null; deck = null; travel = BluePlugPhase.Ready; }
            void OnPlayModeChanged(PlayModeStateChange change)
            {
                if (change == PlayModeStateChange.ExitingPlayMode) Dispose();
            }
            public void Dispose()
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                if (plug) plug.LinkReset -= Reset;
                Reset();
            }
        }

        [MenuItem(Menu, true)]
        static bool CanRun() => EditorApplication.isPlaying && !EditorApplication.isCompiling;

        [MenuItem(Menu)]
        static void Run()
        {
            var fixture = new GameObject("임시 플러그 연출 검사") { hideFlags = HideFlags.HideAndDontSave };
            fixture.transform.position = new Vector3(-128, -128, 0);
            try
            {
                var body = AncientArt.Display("검사용 백 경위", AncientArt.Hero(0), Vector2.zero, 12, fixture.transform);
                body.transform.localPosition = Vector3.down;
                var motion = new BluePlugCharacterMotion(body);
                var visuals = fixture.AddComponent<BluePlugVisuals>();
                visuals.SetActor(body);

                var assembly = fixture.transform.Find("파란 플러그 표시");
                Require(assembly, "런타임 플러그 표시가 생성되지 않았습니다.");
                var cable = assembly.Find("전선 중심").GetComponent<LineRenderer>();
                var guide = assembly.Find("투척 사거리").GetComponent<LineRenderer>();
                var reel = body.transform.Find("왼손 전선 감개").GetComponent<SpriteRenderer>();

                CheckTowGhosts(body, motion, fixture.GetComponent<BluePlugTravelFX>());
                CheckPlantedFeet(body, motion);
                CheckFacing(1, body, motion, visuals, cable, guide, reel);
                CheckFacing(-1, body, motion, visuals, cable, guide, reel);
                CheckBracingAndTravel(1, body, motion, visuals, cable);
                CheckBracingAndTravel(-1, body, motion, visuals, cable);
                CheckSocketFace(fixture.transform);
                CheckSeatingFeedback(visuals, assembly);
                Debug.Log("플러그 연출 검사 통과. 좌우 벽 자세, 견인·스윙 실루엣, 손·전선 연결과 이탈 정리를 확인했습니다.");
            }
            catch (Exception error) { Debug.LogException(error); }
            finally { UnityEngine.Object.DestroyImmediate(fixture); }
        }

        static void CheckTowGhosts(SpriteRenderer body, BluePlugCharacterMotion motion, BluePlugTravelFX effects)
        {
            Require(effects && effects.AfterimageCapacity == 6, "견인 잔상 풀 크기를 확인하세요.");
            var method = typeof(BluePlugTravelFX).GetMethod("CompleteBody",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Require(method != null, "실제 잔상 프레임 선택 경로가 없습니다.");
            var sheet = Resources.Load<Texture2D>("Traversal/BluePlugOfficer");
            motion.Clear();
            for (int frame = 0; frame < 2; frame++)
            {
                for (int i = 0; i < (frame == 0 ? 1 : 7); i++)
                    motion.Apply(BluePlugPhase.Towing, Vector2.right, Vector2.right * 15, 1, false, PoseStep);
                Sprite source = body.sprite;
                var args = new object[] { source };
                var complete = (Sprite)method.Invoke(effects, args);
                Require(source.name == "견인 " + frame && complete && complete != source && complete.texture == sheet &&
                    complete.name == "견인 전신 잔상 " + frame && complete.pivot == source.pivot &&
                    complete.rect.size == source.rect.size && complete.pixelsPerUnit == source.pixelsPerUnit &&
                    complete == (Sprite)method.Invoke(effects, args), "견인 잔상은 미리 합성한 동일 피벗 전신을 재사용해야 합니다.");
            }
            motion.Clear();
            motion.Apply(BluePlugPhase.Towing, Vector2.right, Vector2.right * 15, 1, false, PoseStep, useFinalePose:true);
            Require((Sprite)method.Invoke(effects, new object[] { body.sprite }) == AncientArt.Swing(0),
                "기존 분리 몸통의 전신 원화 대응이 바뀌었습니다.");
            motion.Clear();
        }

        static void CheckPlantedFeet(SpriteRenderer body, BluePlugCharacterMotion motion)
        {
            var gait = new AncientLocomotionMotion();
            var sheet = Resources.Load<Texture2D>("Traversal/BluePlugOfficer");
            var finale = Resources.Load<Texture2D>("Traversal/BluePlugGroundBody");
            Require(sheet && finale, "일반 플레이와 조커 마지막 페이즈의 별도 원화를 확인하세요.");
            foreach (float facing in new[] { -1f, 1f })
            {
                motion.Clear();
                gait.Clear();
                int frames = 0;
                for (int i = 0; i < 240; ++i)
                {
                    float speed = i < 120 ? 6.5f * facing : 0f;
                    gait.Step(true, speed, 0, facing, PoseStep);
                    float a = i * PoseStep * Mathf.PI * 8;
                    Vector2 radial = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    // 후반부는 움직이는 운반대 위에 가만히 선 상태다. 세계 속도는 있어도 몸은 정지 보폭이다.
                    motion.Apply(BluePlugPhase.Winding, radial, Vector2.right * (speed + 5),
                        facing, true, PoseStep, gait);
                    Require(body.sprite.texture == sheet && body.sprite.name == "플러그 보행 " + gait.Frame &&
                        body.sprite.pixelsPerUnit == 110 && body.sprite.rect.width == 256,
                        "플러그 회전 중 보폭이 실제 지면 기준 걸음과 어긋났습니다.");
                    Require(Mathf.Abs(body.transform.localPosition.y + 1) < .14f,
                        "반복 재생 중 플러그 지상 자세의 발 높이가 누적됐습니다.");
                    frames |= 1 << gait.Frame;
                }
                Require(frames == 31, "정지와 달리기의 다섯 발 자세가 모두 나와야 합니다.");

                // 마지막 거리에서 팁이 손 뒤로 넘어가도 몸통은 진행 방향을 유지한다.
                motion.Apply(BluePlugPhase.Towing, Vector2.right * facing, Vector2.right * 24 * facing, facing, false, PoseStep, gait);
                motion.Apply(BluePlugPhase.Towing, Vector2.left * facing * .10f, Vector2.right * 15 * facing, facing, false, PoseStep, gait);
                Require(body.flipX == (facing < 0), "도착 직전 소켓을 지나며 견인 자세가 반대로 뒤집혔습니다.");
                motion.Apply(BluePlugPhase.Ready, Vector2.zero, Vector2.right * facing, facing, true, PoseStep, gait);
                Require(body.sprite.texture == sheet && body.sprite.name == "충격 흡수 0" &&
                    Mathf.Abs(body.transform.localPosition.y + 1) < .14f,
                    "착지 첫 프레임에서 무릎을 접고 지면에 발을 받쳐야 합니다.");
                bool rose = false;
                for (int i = 0; i < 40; i++)
                {
                    bool posed = motion.Apply(BluePlugPhase.Ready, Vector2.zero, Vector2.zero, facing, true, PoseStep, gait);
                    if (posed && body.sprite.texture == sheet && body.sprite.name == "충격 흡수 1") rose = true;
                }
                Require(rose && !motion.Presenting, "착지 뒤 몸을 펴고 일반 보행에 조작권을 돌려야 합니다.");
                motion.Clear();
                motion.Apply(BluePlugPhase.Towing, Vector2.right * facing, Vector2.right * 18 * facing,
                    facing, false, PoseStep, gait, useFinalePose: true);
                Require(body.sprite && body.sprite.name == "플러그 전용 몸통 0" &&
                    body.sprite.texture != sheet && Mathf.Abs(body.sprite.pixelsPerUnit - 27.5f) < Tolerance,
                    "조커 마지막 페이즈의 견인에 평상시 신규 몸체가 표시됐습니다.");
                motion.Apply(BluePlugPhase.Ready, Vector2.zero, Vector2.zero, facing, true,
                    PoseStep, gait, useFinalePose: true);
                Require(body.sprite.texture == finale, "보호된 마지막 페이즈의 복귀 원화를 확인하세요.");
            }
        }

        static void CheckFacing(float facing, SpriteRenderer body, BluePlugCharacterMotion motion,
            BluePlugVisuals visuals, LineRenderer cable, LineRenderer guide, SpriteRenderer reel)
        {
            motion.Clear();
            body.sprite = AncientArt.Hero(0);
            body.transform.localPosition = Vector3.down;
            body.transform.localScale = Vector3.one;
            body.transform.localRotation = Quaternion.identity;
            body.flipX = facing < 0;

            var radial = new Vector2(.8f * facing, .6f);
            Vector2 origin = visuals.transform.position;
            Vector2 centre = origin + Vector2.up * .35f;
            Vector2 rawTip = centre + radial;
            Vector2 tip = visuals.ProjectOrbit(centre, rawTip);
            Require(Mathf.Abs(rawTip.x - tip.x) < Tolerance && tip.y > centre.y && tip.y < rawTip.y,
                "회전 궤적이 세로축으로만 투영되어야 합니다.");

            Require(motion.Apply(BluePlugPhase.Winding, radial, Vector2.zero, facing, true, PoseStep),
                "회전 중에는 양손 자세가 적용되어야 합니다.");
            var right = motion.RightGripSocket.GetComponent<SpriteRenderer>();
            var left = motion.LeftGripSocket.GetComponent<SpriteRenderer>();
            var closedHand = right.sprite;
            Require(left.enabled && right.enabled && reel.enabled && body.flipX == (facing < 0),
                "회전 중에는 양손과 릴이 움직임 방향에 맞게 보여야 합니다.");

            visuals.Show(motion.LeftGripWorld, tip, Vector2.Perpendicular(radial), BluePlugPhase.Winding,
                centre, .8f, 16, motion.RightGripWorld);
            Require(cable.gameObject.activeInHierarchy && guide.enabled && Near(cable.GetPosition(0), motion.LeftGripWorld) &&
                HasVertex(cable, motion.RightGripWorld) && !Near(motion.LeftGripWorld, motion.RightGripWorld),
                "회전 전선에 왼손 시작점과 오른손 안내점이 모두 있어야 합니다.");

            Vector2 destination = origin + new Vector2(facing * 8, 3);
            Vector2 direction = (destination - centre).normalized;
            motion.Apply(BluePlugPhase.Flying, direction, Vector2.zero, facing, true, PoseStep);
            visuals.Show(motion.LeftGripWorld, destination, direction, BluePlugPhase.Flying);
            Require(right.enabled && right.sprite != closedHand && !guide.enabled && Near(cable.GetPosition(0), motion.LeftGripWorld),
                "투척 순간에는 오른손을 열고 사거리 표시를 끄면서 왼손 연결을 유지해야 합니다.");
            var released = new Vector3[cable.positionCount];
            cable.GetPositions(released);
            visuals.Show(motion.LeftGripWorld, destination, direction, BluePlugPhase.Flying,
                windingGrip: destination + Vector2.up * 6);
            for (int i = 0; i < released.Length; i++)
                Require(Vector3.Distance(released[i], cable.GetPosition(i)) < Tolerance, "비행 중 오른손 매듭이 남았습니다.");

            motion.Clear();
            body.sprite = AncientArt.Hero(2);
            body.transform.localPosition += new Vector3(facing * .5f, .18f, 0);
            motion.PresentPassive();
            visuals.Show(motion.LeftGripWorld, destination, direction, BluePlugPhase.Connected);
            Require(!motion.Presenting && reel.enabled && Near(reel.transform.position, motion.LeftGripWorld) &&
                Near(cable.GetPosition(0), motion.LeftGripWorld), "연결 상태에서는 보행 자세의 왼손에 릴과 전선이 있어야 합니다.");

            motion.Apply(BluePlugPhase.Towing, direction, direction * 12, facing, false, PoseStep);
            visuals.Show(motion.LeftGripWorld, destination, direction, BluePlugPhase.Towing);
            Require(cable.gameObject.activeInHierarchy && Near(cable.GetPosition(0), motion.LeftGripWorld),
                "견인에도 왼손의 전선 시작점을 유지해야 합니다.");
            visuals.Show(Vector2.zero, Vector2.zero, Vector2.right, BluePlugPhase.Ready);
            Require(!cable.gameObject.activeInHierarchy, "견인이 끝난 프레임에 전선이 사라져야 합니다.");

            visuals.ResetFeedback();
            motion.Clear();
            Require(!left.enabled && !right.enabled && !reel.enabled && !cable.gameObject.activeInHierarchy,
                "초기화 후 검사 캐릭터에 손, 릴, 전선이 남았습니다.");
        }

        static bool HasVertex(LineRenderer line, Vector2 point)
        {
            for (int i = 0; i < line.positionCount; i++)
                if (Near(line.GetPosition(i), point)) return true;
            return false;
        }

        static void CheckBracingAndTravel(float facing, SpriteRenderer body, BluePlugCharacterMotion motion,
            BluePlugVisuals visuals, LineRenderer cable)
        {
            motion.Clear();
            var root = visuals.transform;
            Vector3 rootPosition = root.position;
            Quaternion rootRotation = root.rotation;
            Vector2 socket = (Vector2)root.position + new Vector2(6 * facing, 4);
            Vector2 entry = Vector2.right * facing;
            Vector2 nearGrip = (Vector2)root.position + new Vector2(.45f * facing, .32f);
            Require(!motion.Apply(BluePlugPhase.Connected, entry, Vector2.zero, facing, false, PoseStep),
                "멀리서 꽂기만 해도 벽 고정 자세가 나왔습니다.");
            Require(motion.Apply(BluePlugPhase.Connected, entry, Vector2.zero, facing, false,
                PoseStep, wallGripPoint: nearGrip) && body.flipX == (facing < 0),
                "벽 접점이 생겼을 때 실제 벽 방향으로 고정 자세를 취해야 합니다.");
            var wallBody = Resources.Load<Texture2D>("Traversal/BluePlugOfficer");
            // 원화 피벗은 x=128, 밑창 끝은 x=188이다. 반전 후에도 밑창이 실제 벽 표면에 닿아야 한다.
            float sole = facing * (188 - body.sprite.pivot.x) / body.sprite.pixelsPerUnit;
            Require(wallBody && body.sprite.texture == wallBody && body.sprite.name == "두 무릎을 접고 벽 손잡이에 기대기" &&
                Mathf.Abs(body.transform.TransformPoint(new Vector3(sole, 0)).x - nearGrip.x) < Tolerance,
                "벽 고정용 접힌 무릎과 밑창이 소켓 벽면을 벗어났습니다.");
            Require(!motion.Apply(BluePlugPhase.Connected, entry, Vector2.zero, facing, false, PoseStep),
                "접점이 사라진 뒤에도 벽 고정 자세가 남았습니다.");
            visuals.Show(motion.LeftGripWorld, socket, entry, BluePlugPhase.Connected);
            Require(CableSag(cable) > .08f, "힘이 걸리지 않은 전선이 두 끝점 사이에서 처져야 합니다.");
            var head = root.Find("파란 플러그 표시/플러그 헤드");
            var shell = head.Find("파란 플러그 함체").GetComponent<SpriteRenderer>();
            var jacket = root.Find("파란 플러그 표시/전선 윤곽").GetComponent<LineRenderer>();
            Require(shell.sprite && shell.enabled && shell.color.a >= .99f && jacket.enabled &&
                jacket.startColor.a >= .99f && shell.sortingLayerID == body.sortingLayerID &&
                shell.sortingOrder > jacket.sortingOrder && jacket.sortingOrder > body.sortingOrder &&
                shell.sharedMaterial && jacket.sharedMaterial && shell.gameObject.activeInHierarchy && jacket.gameObject.activeInHierarchy,
                "발광을 꺼도 몸 앞에 불투명한 플러그와 피복이 남아야 합니다.");
            Vector2 tail = cable.GetPosition(cable.positionCount - 1);
            Require(Near(head.position, socket) && Vector2.Dot(((Vector2)head.position - tail).normalized, entry) > .98f &&
                Vector2.Distance(tail, socket) > .35f, "전선은 삽입점에서 단자 반대편에 있는 꼬리로 이어져야 합니다.");

            Vector2 toSocket = socket - (Vector2)root.position;
            for (int i = 0; i < 12; i++)
                motion.Apply(BluePlugPhase.Towing, toSocket, toSocket.normalized * 14, facing, false, PoseStep);
            var towSprite = body.sprite;
            visuals.Show(motion.LeftGripWorld, socket, entry, BluePlugPhase.Towing);
            Require(Mathf.Abs(CableSag(cable)) < .018f, "견인 중에는 전선이 팽팽해야 합니다.");
            for (int i = 0; i < 12; i++)
                motion.Apply(BluePlugPhase.Swinging, Vector2.up * 4, Vector2.right * facing * 7, facing, false, PoseStep);
            visuals.Show(motion.LeftGripWorld, socket, entry, BluePlugPhase.Swinging);
            Require(body.sprite && body.sprite != towSprite && Mathf.Abs(CableSag(cable)) < .018f,
                "스윙은 견인과 다른 몸 실루엣으로 줄의 회전축을 유지해야 합니다.");
            Require(root.position == rootPosition && root.rotation == rootRotation, "자세 연출이 플레이어 루트를 변경했습니다.");
            visuals.ResetFeedback();
            motion.Clear();
        }

        static void CheckSeatingFeedback(BluePlugVisuals visuals, Transform assembly)
        {
            var pins = assembly.Find("플러그 헤드/잠금 섬광").GetComponent<SpriteRenderer>();
            var pulse = assembly.Find("견인 전류").GetComponent<LineRenderer>();
            Vector2 hand = visuals.transform.position;
            Vector2 socket = hand + new Vector2(4, 2);
            visuals.Show(hand, socket, Vector2.right, BluePlugPhase.Retracting);
            Require(!pins.enabled && !pulse.enabled, "빗나간 플러그에 체결 신호가 켜졌습니다.");
            visuals.Click();
            visuals.Show(hand, socket, Vector2.right, BluePlugPhase.Connected);
            bool full = AncientVisuals.HighQuality && AncientGame.Saved("reducedFeedback") == 0;
            var atlas = Resources.Load<Texture2D>("Traversal/BluePlugContact");
            Require(atlas && atlas.width == 6 * atlas.height && pins.enabled && !pulse.enabled,
                "도킹 셀이나 체결 신호가 없거나 첫 체결에서 견인 전류가 시작됐습니다.");
            Require((pins.sprite.texture == atlas) == full, "도킹 효과가 현재 피드백 설정과 다릅니다.");
            visuals.Show(hand, socket, Vector2.right, BluePlugPhase.Towing);
            Require(pulse.enabled == full, "견인 입력 뒤 전류가 설정대로 켜지지 않았습니다.");
            visuals.Show(hand, socket, Vector2.right, BluePlugPhase.Swinging);
            Require(!pulse.enabled, "스윙에 견인 전류가 남았습니다.");
            visuals.ResetFeedback();
            Require(!pins.enabled && !pulse.enabled, "재시작 뒤 단자 방전이나 견인 전류가 남았습니다.");
        }

        static float CableSag(LineRenderer cable)
        {
            int last = cable.positionCount - 1, middle = last / 2;
            Vector3 straight = Vector3.Lerp(cable.GetPosition(0), cable.GetPosition(last), middle / (float)last);
            return straight.y - cable.GetPosition(middle).y;
        }

        static void CheckSocketFace(Transform fixture)
        {
            var socketRoot = new GameObject("검사용 콘센트");
            socketRoot.transform.SetParent(fixture, false);
            var outlet = socketRoot.AddComponent<BluePlugSocket>();
            var face = socketRoot.transform.Find("콘센트 표시/방향을 맞춘 단자 홈").GetComponent<SpriteRenderer>();
            var light = socketRoot.transform.Find("콘센트 표시/콘센트 상하 표시등").GetComponent<SpriteRenderer>();
            var halo = socketRoot.transform.Find("콘센트 표시/콘센트 푸른 후광").GetComponent<SpriteRenderer>();
            // 발광 계층을 꺼도 단자 홈 자체의 모양만으로 세 상태를 구별할 수 있는지 본다.
            light.enabled = halo.enabled = false;
            const BluePlugSkill learned = BluePlugSkill.Tow | BluePlugSkill.Swing;
            outlet.SetIndicator(BluePlugSocketState.Idle, learned);
            var idle = face.sprite;
            outlet.SetIndicator(BluePlugSocketState.Ready, learned);
            var aiming = face.sprite;
            outlet.SetIndicator(BluePlugSocketState.Linked, learned);
            var seated = face.sprite;
            Require(face.enabled && idle && aiming && seated && idle != aiming && idle != seated && aiming != seated,
                "대기·조준·체결에 각각 다른 콘센트 홈이 보여야 합니다.");
            outlet.SetIndicator(BluePlugSocketState.Blocked, learned);
            Require(face.sprite == idle, "벽 뒤 콘센트에 조준용으로 열린 홈이 남았습니다.");
        }

        static bool Near(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < Tolerance * Tolerance;

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
