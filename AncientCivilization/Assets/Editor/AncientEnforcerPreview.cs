#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.AncientCivilization.EditorChecks
{
    // 본편 입구를 쓰되 일반 세이브와 다른 임시 프로필에 진행을 기록한다.
    [InitializeOnLoad]
    public sealed class AncientEnforcerPreview : EditorWindow
    {
        const string Menu = "PLUGIN/검사/기록 집행관 바로 플레이";
        const string ActiveProfile = "PLUGIN.QA.ActiveProfile";
        const string PreviewProfile = "PLUGIN.QA.EnforcerPreview.Profile";
        const string Prefix = "PLUGIN.Ancient.QA.Enforcer.";

        static AncientEnforcerPreview() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        [MenuItem(Menu, true)]
        static bool CanPlay() => !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling &&
            SceneManager.GetActiveScene().name == "Main" && string.IsNullOrEmpty(SessionState.GetString(ActiveProfile, ""));

        [MenuItem(Menu)]
        static void Play()
        {
            if (!CanPlay()) return;
            string profile = Prefix + Guid.NewGuid().ToString("N") + ".";
            SessionState.SetString(PreviewProfile, profile);
            SessionState.SetString(ActiveProfile, profile);
            PlayerPrefs.SetInt(profile + "started", 1);
            PlayerPrefs.SetInt(profile + "questStage", 9);
            PlayerPrefs.SetInt(profile + "storyBoss", 1);
            PlayerPrefs.SetInt(profile + "cityFloor", 2);
            PlayerPrefs.SetInt(profile + "cityCleared", 1 << 1);
            PlayerPrefs.SetInt(profile + "campaignVersion", AncientCampaignRules.RouteVersion);
            PlayerPrefs.SetInt(profile + "campaignComplete", (1 << 12) - 1);
            PlayerPrefs.SetInt(profile + "retryActive", 1);
            PlayerPrefs.SetInt(profile + "retryFloor", 2);
            PlayerPrefs.SetInt(profile + "retryX", Mathf.RoundToInt((AncientCityLayout.ArenaLeft(2) + 1.7f) * 100));
            PlayerPrefs.SetInt(profile + "retryY", 130);
            PlayerPrefs.Save();
            EditorApplication.isPlaying = true;
        }

        int inspectPhase = 1;
        int requestedHealth = 50;
        static readonly string[] PhaseLabels = { "1페이즈", "2페이즈", "3페이즈" };

        [MenuItem("PLUGIN/검사/기록 집행관 패턴 검사")]
        static void OpenInspector() => GetWindow<AncientEnforcerPreview>("집행관 패턴 검사");

        void OnInspectorUpdate() => Repaint();

        void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                GUILayout.Label("Main 씬에서 임시 검사 프로필을 시작하세요.");
                GUI.enabled = CanPlay();
                if (GUILayout.Button("기록 집행관 바로 플레이")) Play();
                GUI.enabled = true;
                return;
            }
            string profile = SessionState.GetString(PreviewProfile, "");
            if (!profile.StartsWith(Prefix, StringComparison.Ordinal) ||
                SessionState.GetString(ActiveProfile, "") != profile || !AncientGame.IsQAProfile)
            {
                GUILayout.Label("기록 집행관 바로 플레이로 시작한 임시 프로필에서 사용할 수 있습니다.");
                return;
            }
            var game = AncientGame.Instance;
            var encounter = game ? game.Enforcer : null;
            if (!encounter || !encounter.PreviewReady || !game.boss || !game.hero)
            {
                GUILayout.Label("임시 프로필의 기록실 전투 중에 사용할 수 있습니다.");
                return;
            }

            int hp = Mathf.RoundToInt(game.boss.HealthRatio * 100f);
            GUILayout.Label($"현재 {encounter.Phase}페이즈 · 보스 체력 {hp}%");
            inspectPhase = GUILayout.Toolbar(inspectPhase - 1, PhaseLabels) + 1;
            for (int i = 0; i < AncientEnforcerRules.MoveCount(inspectPhase); i++)
            {
                var move = AncientEnforcerRules.Move(inspectPhase, i);
                if (GUILayout.Button(PatternName(move, inspectPhase))) encounter.PreviewPattern(inspectPhase, i);
            }
            GUILayout.Space(8f);
            GUILayout.Label($"지정할 보스 체력 {requestedHealth}%");
            requestedHealth = Mathf.RoundToInt(GUILayout.HorizontalSlider(requestedHealth, 1, 100));
            if (GUILayout.Button("체력 적용")) encounter.PreviewHealth(requestedHealth / 100f);
            if (GUILayout.Button("백 경위 입자 최대 충전")) game.hero.AddCapturedEnergy(100f);
        }

        static string PatternName(AncientEnforcerMove move, int phase)
        {
            switch (move)
            {
                case AncientEnforcerMove.Drop: return "철괴 4회와 컨테이너";
                case AncientEnforcerMove.Bombard: return "후경 미사일 폭격";
                case AncientEnforcerMove.Gun: return phase == 1 ? "사격 후 총 투척" : "다섯 갈래 사격";
                case AncientEnforcerMove.Waves: return "좌우 입자 파도";
                case AncientEnforcerMove.Blocks: return phase == 2 ? "블록 하드드랍" : "고속 하드드랍";
                case AncientEnforcerMove.Echoes: return phase == 2 ? "생활구 로봇 1기" : "생활구 로봇 2 + 2 + 1";
                case AncientEnforcerMove.Planes: return "추적 종이비행기";
                case AncientEnforcerMove.Missiles: return "미사일 난사";
                default: return "15초 흡수 배리어";
            }
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            string profile = SessionState.GetString(PreviewProfile, "");
            if (!profile.StartsWith(Prefix, StringComparison.Ordinal)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
                EditorApplication.ExecuteMenuItem("Window/General/Game");
            if (change != PlayModeStateChange.EnteredEditMode) return;
            foreach (string key in AncientGame.ProgressSaveKeys) PlayerPrefs.DeleteKey(profile + key);
            foreach (string key in AncientGame.OptionSaveKeys) PlayerPrefs.DeleteKey(profile + key);
            PlayerPrefs.Save();
            if (SessionState.GetString(ActiveProfile, "") == profile) SessionState.EraseString(ActiveProfile);
            SessionState.EraseString(PreviewProfile);
        }
    }
}
#endif
