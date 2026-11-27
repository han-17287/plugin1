#if UNITY_EDITOR
using System;
using Plugin.PlayerCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.AncientCivilization.EditorChecks
{
    public static class PlugDistrictSmokeChecks
    {
        const string Menu = "PLUGIN/맵/다섯 층 플러그 설비 물리 검사";
        const float Tick = .02f;
        const int SolidLayer = 1 << 8;

        [MenuItem(Menu, true)]
        static bool Available() => EditorApplication.isPlaying && !EditorApplication.isCompiling;

        [MenuItem(Menu)]
        static void Run()
        {
            var settings = Resources.Load<PlugDistrictSettings>("Traversal/DefaultPlugDistrict");
            Require(settings, "설비 설정을 읽지 못했습니다.");
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.CreateScene("회로 검사 " + Guid.NewGuid().ToString("N"),
                new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            GameObject root = null;
            try
            {
                // 임시 씬에서 생성까지 마친다. 게임의 기본 물리 월드는 수동 시뮬레이션하지 않는다.
                Require(SceneManager.SetActiveScene(scene), "검사용 로컬 씬을 열지 못했습니다.");
                root = new GameObject("설비 검사 전용");
                var physics = scene.GetPhysicsScene2D();
                Require(physics.IsValid() && !physics.Equals(Physics2D.defaultPhysicsScene), "독립된 2D 물리 월드가 필요합니다.");
                var trial = new Trial(root.transform, physics, settings);
                trial.CheckApproaches();
                trial.CheckLift();
                trial.CheckShutter();
                trial.CheckBridge();
                trial.CheckCanopies();
                trial.CheckRelay();
                Debug.Log("다섯 층 10개 현장 플러그 접근 검사 통과 · 화물대 착지, 셔터 끼임 방지, 승인교, 연속 차양, 중계기 만료");
            }
            finally
            {
                // 검사 실패도 호출자에게 전달하고 원래 게임의 활성 씬과 물리 상태를 복구한다.
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (root) UnityEngine.Object.DestroyImmediate(root);
                if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
            }
        }

        sealed class Trial
        {
            readonly Transform root;
            readonly PhysicsScene2D physics;
            readonly PlugDistrictSettings settings;
            readonly Rigidbody2D body;
            readonly BoxCollider2D playerBox;
            PlugDistrictDevice device;

            public Trial(Transform root, PhysicsScene2D physics, PlugDistrictSettings settings)
            {
                this.root = root;
                this.physics = physics;
                this.settings = settings;
                var actor = new GameObject("검사용 인물 판정");
                actor.transform.SetParent(root, false);
                body = actor.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                playerBox = actor.AddComponent<BoxCollider2D>();
                playerBox.size = new Vector2(.72f, 1.8f);
            }

            void StartFloor(int floor, bool advanced = false)
            {
                if (device) UnityEngine.Object.DestroyImmediate(device.gameObject);
                float x = AncientCampaignPlugLayout.OriginX(floor, advanced ? 1 : 0);
                device = PlugDistrictDevice.Spawn(root, floor, new Vector2(x, 0), advanced);
                MoveProbe(-4, 1);
                Require(device.gameObject.scene == root.gameObject.scene, "다른 씬에 설비가 생성됐습니다.");
                foreach (var collider in device.GetComponentsInChildren<BoxCollider2D>())
                    Require(collider.gameObject.scene == root.gameObject.scene, "설비 판정이 게임 씬으로 새어 나갔습니다.");
            }

            Vector2 At(float x, float y) => device.transform.TransformPoint(new Vector2(x, y));

            void MoveProbe(float x, float y)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.linearVelocity = Vector2.zero;
                body.position = At(x, y);
            }

            void Advance(float seconds)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(seconds / Tick));
                for (int i = 0; i < steps; i++)
                {
                    if (body.bodyType == RigidbodyType2D.Dynamic) body.AddForce(Vector2.down * (9.81f * body.mass));
                    device.Step(Tick, body.position);
                    Require(physics.Simulate(Tick), "검사용 월드의 물리 틱이 실행되지 않았습니다.");
                }
            }

            BoxCollider2D Surface(string name)
            {
                var part = device.transform.Find(name);
                Require(part && part.TryGetComponent<BoxCollider2D>(out _), name + " 판정이 없습니다.");
                return part.GetComponent<BoxCollider2D>();
            }

            // 단방향 발판도 독립 물리 월드에서 대상 레이어에 등록됐는지 실제 위치를 조회한다.
            bool Contains(BoxCollider2D surface, float x, float y) =>
                physics.OverlapPoint(At(x, y), SolidLayer) == surface;

            readonly RaycastHit2D[] hits = new RaycastHit2D[32];
            static readonly Vector2 MainCapsule = new Vector2(.72f, 2.1f);

            void RequireNoWall(int count, string action)
            {
                Require(count < hits.Length, device.Label + " " + action + " 검사 버퍼 초과");
                for (int i = 0; i < count; i++)
                {
                    Collider2D collider = hits[i].collider;
                    // 본편의 위쪽 견인/투척 및 수평 케이블은 단방향 디딤판의 아래쪽으로 통과한다.
                    if (collider.usedByEffector && collider.TryGetComponent<PlatformEffector2D>(out var effector)
                        && effector.useOneWay) continue;
                    throw new InvalidOperationException(device.Label + " " + action + " 차단: " + collider.name);
                }
            }

            public void CheckApproaches()
            {
                var filter = new ContactFilter2D { useTriggers = false };
                filter.SetLayerMask(SolidLayer);
                for (int floor = 0; floor < AncientChapterLayout.FloorCount; floor++)
                for (int site = 0; site < AncientCampaignPlugLayout.SitesPerFloor; site++)
                {
                    StartFloor(floor, site == 1);
                    Advance(settings.unfoldSeconds * 1.55f + .1f);
                    Vector2 centre = At(-3.5f, 1.08f), hand = centre + new Vector2(.14f, .78f);
                    RequireNoWall(physics.CapsuleCast(centre, MainCapsule, CapsuleDirection2D.Vertical, 0,
                        Vector2.right, 14, filter, hits), "하단 보행");
                    for (int slot = 0; slot < device.SocketCount; slot++)
                    {
                        Require(device.SocketAvailable(slot), device.Label + " 정상 순서에서 소켓이 잠겼습니다.");
                        Vector2 tip = device.SocketPoint(slot) - hand;
                        Vector2 cable = device.SocketPoint(slot) - centre;
                        Vector2 tow = device.TowPoint(slot) - centre;
                        RequireNoWall(physics.CircleCast(hand, .1f, tip.normalized, tip.magnitude, filter, hits), "투척");
                        RequireNoWall(physics.Raycast(centre, cable.normalized, cable.magnitude, filter, hits), "케이블");
                        device.Dock(slot);
                        Advance(settings.unfoldSeconds + 2 * settings.canopyInterval + .12f);
                        RequireNoWall(physics.CapsuleCast(centre, MainCapsule, CapsuleDirection2D.Vertical, 0,
                            tow.normalized, tow.magnitude, filter, hits), "견인");
                        Require(!physics.OverlapCapsule(device.TowPoint(slot), MainCapsule,
                            CapsuleDirection2D.Vertical, 0, SolidLayer), device.Label + " 견인 도착 지점이 지형과 겹칩니다.");
                        device.Release(slot);
                    }
                }
            }

            public void CheckLift()
            {
                StartFloor(0);
                var lift = Surface("화물 승강대");
                Advance(.08f);
                Require(Contains(lift, 3, .85f), "생산구 승강대가 바닥 쪽에 없습니다.");
                device.Dock(0);
                Advance(3.25f / Mathf.Max(.25f, settings.liftSpeed) + .24f);
                Require(Contains(lift, 3, 4.10f) && !Contains(lift, 3, .85f), "상차대 판정이 위쪽 정차점까지 이동하지 않았습니다.");
                // 동적 상자를 실제로 떨어뜨려 플랫폼 이펙터를 통과하지 않고 착지하는지 확인한다.
                body.position = At(3, 5.7f);
                body.bodyType = RigidbodyType2D.Dynamic;
                body.linearVelocity = Vector2.down * 3;
                Advance(.5f);
                Require(playerBox.IsTouching(lift) && Mathf.Abs(playerBox.bounds.min.y - lift.bounds.max.y) < .09f,
                    "움직인 상차대에 착지할 수 없습니다.");
                MoveProbe(-4, 1);
                device.Release(0);
                Advance(settings.releaseHold + 3.25f / Mathf.Max(.25f, settings.liftSpeed) + .24f);
                Require(Contains(lift, 3, .85f), "회수 후 상차대가 출발점으로 복귀하지 않았습니다.");
            }

            public void CheckShutter()
            {
                StartFloor(1);
                var shutter = Surface("방화 셔터");
                float travel = settings.unfoldSeconds * 1.55f + .12f;
                Advance(travel);
                Require(Contains(shutter, 3, 3.75f) && !Contains(shutter, 3, 1f), "셔터와 하단 우회로의 초기 판정이 다릅니다.");
                device.Dock(0);
                Advance(travel);
                Require(!Contains(shutter, 3, 3.75f), "전원을 넣어도 상층 통로가 열리지 않았습니다.");
                MoveProbe(3, 3.75f);
                device.Release(0);
                Advance(settings.shutterSeconds + travel);
                Require(!Contains(shutter, 3, 3.75f) && (!shutter.enabled || !playerBox.Distance(shutter).isOverlapped),
                    "셔터가 인물을 감싼 상태로 닫혔습니다.");
                MoveProbe(-4, 1);
                Advance(travel);
                Require(Contains(shutter, 3, 3.75f), "통로를 비워도 셔터가 닫히지 않았습니다.");
            }

            public void CheckBridge()
            {
                StartFloor(2);
                var deck = Surface("승인교 전개판");
                device.Dock(1);
                Advance(settings.unfoldSeconds + .08f);
                Require(!deck.enabled && !device.SocketAvailable(1), "행정구 B 단독 입력으로 다리가 열렸습니다.");
                device.Dock(0);
                device.Release(0);
                Advance(settings.sequenceSeconds + .1f);
                Require(!device.SocketAvailable(1) && !deck.enabled, "승인 제한 시간 뒤에도 단자가 열려 있습니다.");
                device.Dock(0);
                device.Release(0);
                Require(device.SocketAvailable(1), "A 접속 뒤 B 승인 단자가 열리지 않았습니다.");
                device.Dock(1);
                device.Release(1);
                Advance(settings.sequenceSeconds + settings.unfoldSeconds + .15f);
                Require(Contains(deck, 2, 2.77f) && Contains(deck, 7.30f, 2.77f), "승인 후 다리 양쪽에 디딤면이 없습니다.");
                device.ResetDevice();
                Advance(.04f);
                Require(!deck.enabled && !device.SocketAvailable(1), "재시도 후 승인교가 초기화되지 않았습니다.");
            }

            public void CheckCanopies()
            {
                StartFloor(3);
                var pieces = new[] { Surface("첫 차양"), Surface("둘째 차양"), Surface("셋째 차양") };
                var firstTick = new[] { -1, -1, -1 };
                device.Dock(0);
                int end = Mathf.CeilToInt((2 * settings.canopyInterval + settings.unfoldSeconds + .1f) / Tick);
                for (int i = 0; i < end; i++)
                {
                    Advance(Tick);
                    for (int k = 0; k < pieces.Length; k++)
                        if (pieces[k].enabled && firstTick[k] < 0) firstTick[k] = i;
                }
                Require(firstTick[0] >= 0 && firstTick[0] < firstTick[1] && firstTick[1] < firstTick[2],
                    "세 차양의 물리 디딤면이 앞에서부터 차례로 펼쳐져야 합니다.");
                Require(Contains(pieces[0], 2, 1.52f) && Contains(pieces[1], 4.4f, 2.75f) && Contains(pieces[2], 6.8f, 4.00f),
                    "전개된 차양의 위치에서 발판 판정을 찾지 못했습니다.");
                MoveProbe(4.4f, 3.81f);
                device.Release(0);
                Advance(settings.releaseHold + settings.unfoldSeconds + .1f);
                Require(!pieces[0].enabled && Contains(pieces[1], 4.4f, 2.75f) && !pieces[2].enabled,
                    "인물이 서 있는 가운데 차양만 접힘을 기다려야 합니다.");
                MoveProbe(-4, 1);
                Advance(settings.unfoldSeconds + .1f);
                Require(!pieces[1].enabled, "인물이 떠난 가운데 차양이 접히지 않습니다.");
            }

            public void CheckRelay()
            {
                StartFloor(4);
                var deck = Surface("비상 착지대");
                Require(!deck.enabled && !device.SocketAvailable(1), "중계기가 전원 없이 켜져 있습니다.");
                device.Dock(0);
                device.Release(0);
                Require(device.SocketAvailable(1), "A에서 고공 단자로 전력이 전달되지 않았습니다.");
                Advance(settings.relaySeconds + .09f);
                Require(!device.SocketAvailable(1) && !deck.enabled, "사용하지 않은 중계기가 만료되지 않았습니다.");
                device.Dock(0);
                device.Release(0);
                device.Dock(1);
                Advance(settings.relaySeconds + settings.releaseHold + settings.unfoldSeconds + .15f);
                Require(device.SocketAvailable(1) && Contains(deck, 8.1f, 4.47f), "연결 중에 중계기나 착지대가 꺼졌습니다.");
                device.Release(1);
                Advance(settings.releaseHold + settings.unfoldSeconds + .15f);
                Require(!device.SocketAvailable(1) && !deck.enabled, "접속을 해제한 중계기와 착지대가 꺼지지 않았습니다.");
            }
        }

        static void Require(bool result, string detail)
        {
            if (!result) throw new InvalidOperationException(detail);
        }
    }
}
#endif
