#if UNITY_EDITOR
using System;
using Plugin.PlayerCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.AncientCivilization.EditorChecks
{
    public static class PatrolExpeditionSmokeChecks
    {
        static PatrolExpedition patrol;
        static int roomIndex, phase, frame, storedFlag, oldBadge;
        static string savedPrefix;
        static double timeout;
        static bool running, verifyBossTransfer;

        // 개발자 미리보기에서만 지형과 입력 경계를 검사한다. 마지막에는 본편으로 복귀한다.
        [MenuItem("PLUGIN/맵/순찰 50구역 검사 후 본편 복귀")]
        static void Run()
        {
            if (!EditorApplication.isPlaying || running) return;
            patrol = UnityEngine.Object.FindAnyObjectByType<PatrolExpedition>();
            if (!patrol || patrol.SocketRoute || !PatrolEntry.Preview)
            {
                Debug.LogWarning("도시 자유 순찰에서 개발자 지도로 방을 선택한 뒤 실행하세요.");
                patrol = null;
                return;
            }
            running = true;
            verifyBossTransfer = !AncientDeveloperTravel.IsVisiting;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            savedPrefix = "patrol." + PatrolEntry.Read("patrol.generation") + ".";
            storedFlag = AncientGame.Saved(savedPrefix + "flag.qa.preview", -1);
            patrol.SetFlag("qa.preview", true);
            oldBadge = PatrolEntry.Read(savedPrefix + "room.1.badge");
            roomIndex = 1; phase = 0; frame = -1;
            timeout = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Require(bool condition, string detail)
        { if (!condition) throw new InvalidOperationException(detail); }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) { End(false, "Play가 종료됐습니다."); return; }
            try
            {
                Require(EditorApplication.timeSinceStartup < timeout, "시간 초과 · Game 뷰와 일시정지를 확인하세요.");
                if (frame == Time.frameCount) return;
                frame = Time.frameCount;
                if (phase >= 4) { CheckSceneTransfers(); return; }
                Require(patrol && patrol.InputReader.Ready, "Input System 또는 순찰 씬에 연결되지 않았습니다.");
                if (phase == 0)
                {
                    patrol.Travel(roomIndex, true);
                    Require(Mathf.Abs(patrol.Player.GetComponent<Rigidbody2D>().position.x - 46) < .05f, "역방향 입구 " + roomIndex);
                    var backFloor = Physics2D.Raycast(new Vector2(46, .76f), Vector2.down, 1.5f, 1 << 8);
                    Require(backFloor.collider && !backFloor.collider.isTrigger, "돌아오는 입구 발판 " + roomIndex);
                    patrol.Travel(roomIndex);
                    var room = UnityEngine.Object.FindAnyObjectByType<PatrolRoom>();
                    Require(room && room.Title == PatrolRoom.NameOf(roomIndex) && patrol.RoomId == roomIndex, "구역 이름 또는 로드 " + roomIndex);
                    var floor = Physics2D.Raycast(new Vector2(2, .76f), Vector2.down, 1.5f, 1 << 8);
                    Require(floor.collider && !floor.collider.isTrigger, "시작 지점 발판 " + roomIndex);
                    Require(Mathf.Abs(patrol.Player.GetComponent<Rigidbody2D>().position.x - 2) < .05f && !patrol.TrialActive, "정방향 입구 " + roomIndex);
                    if (++roomIndex > PatrolCatalog.Count) phase = 1;
                    return;
                }
                if (phase == 1)
                {
                    patrol.Travel(1);
                    patrol.Player.Respawn(new Vector2(47.55f, 1.1f));
                    patrol.Player.transform.position = new Vector2(47.55f, 1.1f);
                    Physics2D.SyncTransforms();
                    phase = 2; return;
                }
                if (phase == 2)
                {
                    if (patrol.RoomId != 2) return;
                    Require(PatrolEntry.Read(savedPrefix + "room.1.passed") == 1, "정방향 완주 기록");
                    patrol.Travel(1);
                    Require(patrol.BeginTrial() && patrol.TrialActive, "완주한 길 시험 주행 시작");
                    phase = 3; return;
                }
                patrol.Travel(2);
                Require(!patrol.TrialActive && PatrolEntry.Read(savedPrefix + "room.1.badge") == oldBadge, "중단한 시험에 배지를 지급했습니다.");
                Require(patrol.Flag("qa.preview") && AncientGame.Saved(savedPrefix + "flag.qa.preview", -1) == storedFlag, "미리보기 저장 분리");
                phase = 4;
                patrol.ReturnToCampaign();
            }
            catch (Exception ex) { End(false, ex.ToString()); }
        }

        static void CheckSceneTransfers()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (phase == 5)
            {
                if (scene != "PatrolExpedition") return;
                AncientDeveloperDestination joker = null;
                foreach (var entry in AncientDeveloperTravel.Destinations)
                    if (entry.Id == "joker") { joker = entry; break; }
                Require(joker != null && PatrolEntry.TravelCampaign(joker), "순찰에서 조커전으로 전환");
                phase = 6;
                return;
            }
            if (scene != "Main" || !AncientGame.Instance) return;
            if (phase == 6)
            {
                if (!AncientDeveloperTravel.IsVisiting || !AncientGame.Instance.Story ||
                    AncientGame.Instance.Story.CurrentFloor != 4) return;
                var encounter = AncientGame.Instance.Sniper;
                if (!encounter || !encounter.Owner) return;
                Transform actor = encounter.Owner.transform.Find(encounter.Owner.Role);
                if (!actor) return;
                var body = actor.GetComponent<SpriteRenderer>();
                var atlas = Resources.Load<Texture2D>("Art/JokerRifleMotion");
                var transitions = Resources.Load<Texture2D>("Art/JokerRifleTransitions");
                var articulated = Resources.Load<Texture2D>("Art/JokerRifleArticulation");
                Require(encounter.Phase == 1 && atlas && transitions && articulated && body && body.enabled && body.sprite &&
                    (body.sprite.texture == atlas || body.sprite.texture == transitions || body.sprite.texture == articulated),
                    "순찰에서 보스 바로가기로 진입한 Main의 실제 조커 원화");
                Require(AncientDeveloperTravel.Return(AncientGame.Instance), "조커 임시 세션에서 복귀");
                phase = 7;
                return;
            }
            if (phase == 7 && AncientDeveloperTravel.IsVisiting) return;
            Require(!PatrolEntry.Preview && AncientGame.Saved(savedPrefix + "flag.qa.preview", -1) == storedFlag,
                "복귀 후 미리보기 값이 본편에 남았습니다.");
            if (phase == 4 && verifyBossTransfer)
            {
                Require(PatrolEntry.Request(1, true), "조커전 이동 검사에 사용할 순찰 미리보기 시작");
                phase = 5;
                return;
            }
            End(true, "실제 방 50개 양방향 입장, 시간 계측, 미완주 보상 차단, 임시 저장 분리와 본편 복귀" +
                (verifyBossTransfer ? ", 순찰에서 조커전 직접 이동" : ""));
        }

        static void End(bool ok, string detail)
        {
            EditorApplication.update -= Tick;
            running = false;
            patrol = null;
            if (ok) Debug.Log("PASS · " + detail);
            else Debug.LogError("순찰 검사 · " + detail);
        }
    }
}
#endif
