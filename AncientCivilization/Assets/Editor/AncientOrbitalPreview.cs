#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.AncientCivilization.EditorChecks
{
    [InitializeOnLoad]
    public static class AncientOrbitalPreview
    {
        const string MidMenu = "PLUGIN/검사/궤도 봉쇄기 1차전 바로 플레이";
        const string EndMenu = "PLUGIN/검사/코어 보안 병기 재대결 바로 플레이";
        const string TestMenu = "PLUGIN/검사/현재 궤도 봉쇄기 전투 검증";
        const string ActiveProfile = "PLUGIN.QA.ActiveProfile";
        const string PreviewProfile = "PLUGIN.QA.OrbitalPreview.Profile";
        const string EndFight = "PLUGIN.QA.OrbitalPreview.Rematch";
        const string Prefix = "PLUGIN.Ancient.QA.Orbital.";

        enum Step { Off, Enter, Retry, Attack, Victory }
        static Step step;
        static double deadline;
        static int hitPhase;
        static AncientEnemy firstOwner;

        static AncientOrbitalPreview()
        {
            EditorApplication.playModeStateChanged += OnMode;
            EditorApplication.update += Check;
        }

        [MenuItem(MidMenu,true), MenuItem(EndMenu,true)]
        static bool CanStart() => !EditorApplication.isPlayingOrWillChangePlaymode &&
            !EditorApplication.isCompiling && SceneManager.GetActiveScene().name == "Main" &&
            string.IsNullOrEmpty(SessionState.GetString(ActiveProfile,""));

        [MenuItem(MidMenu)] static void FirstFight() => Start(false);
        [MenuItem(EndMenu)] static void Rematch() => Start(true);

        static void Start(bool rematch)
        {
            if (!CanStart()) return;
            string profile = Prefix + Guid.NewGuid().ToString("N") + ".";
            SessionState.SetString(PreviewProfile,profile);
            SessionState.SetString(ActiveProfile,profile);
            SessionState.SetBool(EndFight,rematch);
            void Set(string key, int value) => PlayerPrefs.SetInt(profile + key,value);

            // 실제 사건 완료·재시도 위치로 진입한다. 검사 코드가 보스를 직접 생성하지 않는다.
            Set("started",1); Set("questStage",9); Set("storyBoss",1); Set("residencePoints",3000);
            Set("cityFloor",3); Set("cityCleared",(1 << 1) | (1 << 2));
            Set("campaignVersion",AncientCampaignRules.RouteVersion);
            Set("campaignChapterStarted",1 << 3);
            Set("campaignActive",-1);
            Set("campaignComplete",(1 << (rematch ? 16 : 14)) - 1);
            Set("orbitalMidWon",rematch ? 1 : 0);
            float entry = rematch ? AncientCityLayout.ArenaLeft(3) : AncientCityLayout.OrbitalLeft;
            Set("retryActive",1); Set("retryFloor",3);
            Set("retryX",Mathf.RoundToInt((entry + 2) * 100)); Set("retryY",130);
            PlayerPrefs.Save();
            EditorApplication.isPlaying = true;
        }

        static bool OwnProfile()
        {
            string profile = SessionState.GetString(PreviewProfile,"");
            return profile.StartsWith(Prefix,StringComparison.Ordinal) &&
                profile == SessionState.GetString(ActiveProfile,"") && AncientGame.IsQAProfile;
        }

        static void OnMode(PlayModeStateChange mode)
        {
            string profile = SessionState.GetString(PreviewProfile,"");
            if (!profile.StartsWith(Prefix,StringComparison.Ordinal)) return;
            if (mode == PlayModeStateChange.EnteredPlayMode)
                EditorApplication.ExecuteMenuItem("Window/General/Game");
            if (mode != PlayModeStateChange.EnteredEditMode) return;
            step = Step.Off;
            firstOwner = null;
            foreach (string key in AncientGame.ProgressSaveKeys) PlayerPrefs.DeleteKey(profile + key);
            foreach (string key in AncientGame.OptionSaveKeys) PlayerPrefs.DeleteKey(profile + key);
            PlayerPrefs.Save();
            if (SessionState.GetString(ActiveProfile,"") == profile) SessionState.EraseString(ActiveProfile);
            SessionState.EraseString(PreviewProfile);
        }

        [MenuItem(TestMenu,true)]
        static bool CanCheck() => EditorApplication.isPlaying && OwnProfile() && step == Step.Off &&
            AncientGame.Instance && AncientGame.Instance.boss;

        [MenuItem(TestMenu)]
        static void RunChecks()
        {
            if (!CanCheck()) return;
            firstOwner = null;
            hitPhase = 0;
            step = Step.Enter;
            deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            Debug.Log("궤도 봉쇄기 검사 시작. 검사 중에는 조작하지 마세요.");
        }

        static void Require(bool condition, string problem)
        {
            if (!condition) throw new InvalidOperationException(problem);
        }

        static void Check()
        {
            if (step == Step.Off || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
            try
            {
                Require(OwnProfile(),"전용 검사 프로필이 종료됐습니다.");
                Require(EditorApplication.timeSinceStartup < deadline,"공격, 재시도 또는 결과 저장을 기다리다가 시간이 초과됐습니다.");
                var game = AncientGame.Instance;
                if (!game || !game.City || !game.hero) return;
                bool rematch = SessionState.GetBool(EndFight,false);
                if (step == Step.Victory)
                {
                    Require(game.City.OrbitalMidWon && AncientGame.Saved("orbitalMidWon") == 1,
                        "중간전 생존 퇴각이 저장되지 않았습니다.");
                    Require(((game.City.Cleared & (1 << 3)) != 0) == rematch,
                        "상층 통과 여부가 처치한 전투 종류와 다릅니다.");
                    Require(AncientCityLayout.CanInfiltrate(game.City.Cleared,3) == rematch,
                        "샛길이 잘못된 전투에서 열렸습니다.");
                    if (!rematch)
                    {
                        Require(game.Campaign.CurrentRoom == 14,"퇴각 후 다음 상층 사건이 이어지지 않았습니다.");
                    }
                    Debug.Log(rematch ? "통과 · 기존 재대결과 최상층 샛길 해금" :
                        "통과 · 중간전 재시도, 여섯 체력 단계, 생존 퇴각 저장, 최상층 샛길 봉쇄");
                    step = Step.Off;
                    firstOwner = null;
                    return;
                }
                if (game.Paused) return;
                // 피격과 함정 동작은 허용하면서 자동 검사 도중 다른 원인의 사망만 방지한다.
                game.hero.hp = game.hero.MaxHP;
                var boss = game.boss;
                if (!boss) return;
                var encounter = boss.GetComponent<AncientOrbitalEncounter>();
                Require(boss.IsDistrictBoss && boss.DistrictFloor == 3 && boss.IsOrbitalFirst == !rematch,
                    "평상시 상층 진행 경로가 다른 보스를 생성했습니다.");
                if (rematch)
                {
                    Require(game.City.OrbitalMidWon && (game.City.Cleared & (1 << 3)) == 0,
                        "재대결 입장 조건이 잘못됐습니다.");
                    boss.Hit(100000f,true);
                    step = Step.Victory;
                    return;
                }
                Require(encounter && !game.City.OrbitalMidWon, "중간전 컨트롤러 또는 진행 상태가 잘못됐습니다.");
                if (step == Step.Enter && encounter.Recovering)
                {
                    float hp = boss.HealthRatio;
                    boss.Hit(4f,true);
                    Require(boss.HealthRatio < hp,"열린 코어에 실제 피해가 들어가지 않았습니다.");
                    firstOwner = boss;
                    game.hero.RetryFromCheckpoint();
                    Require(game.hero.transform.position.x < AncientCityLayout.OrbitalLeft && !game.City.OrbitalMidWon,
                        "재시도 지점이 전투장 출구 쪽이거나 승리 기록이 생겼습니다.");
                    // 실제 재시도 지점은 서쪽 문 밖에 있다. 다시 경계를 건너 일반 스폰 조건을 검사한다.
                    game.hero.TravelTo(new Vector2(AncientCityLayout.OrbitalLeft + 2,1.3f));
                    step = Step.Retry;
                    return;
                }
                if (step == Step.Retry)
                {
                    if (boss == firstOwner) return;
                    Require(encounter.Phase == 1 && boss.HealthRatio > .999f,
                        "중간전 재시도 시 체력과 페이즈가 복원되지 않았습니다.");
                    step = Step.Attack;
                }
                if (step != Step.Attack || !encounter.Recovering || encounter.Phase == hitPhase) return;
                Require(encounter.Phase == hitPhase + 1, "강한 일격이 체력 단계를 건너뛰었습니다.");
                hitPhase = encounter.Phase;
                int killsBeforeHit = game.kills;
                boss.Hit(100000f,true);
                if (hitPhase == 6)
                {
                    // 프레임 지연으로 퇴장 연출이 끝나기 전에 실제 생존 및 전투 대상 해제를 확인한다.
                    Require(boss && boss.HealthRatio > 0 && !game.enemies.Contains(boss) &&
                        game.kills == killsBeforeHit,"퇴각한 병기가 사망 처리되거나 전투 대상으로 남았습니다.");
                    step = Step.Victory;
                }
            }
            catch (Exception ex)
            {
                step = Step.Off;
                firstOwner = null;
                Debug.LogError("궤도 봉쇄기 검사 실패 · " + ex.Message);
            }
        }
    }
}
#endif
