#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.AncientCivilization.EditorChecks
{
    // 본편 입구를 쓰되 일반 세이브와 다른 임시 프로필에 진행을 기록한다.
    [InitializeOnLoad]
    public static class AncientEnforcerPreview
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

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            string profile = SessionState.GetString(PreviewProfile, "");
            if (!profile.StartsWith(Prefix, StringComparison.Ordinal)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
                EditorApplication.ExecuteMenuItem("Window/General/Game");
            if (change != PlayModeStateChange.EnteredEditMode) return;
            foreach (string key in AncientGame.ProgressSaveKeys) PlayerPrefs.DeleteKey(profile + key);
            foreach (string key in new[] { "muted", "graphics", "frameTarget" }) PlayerPrefs.DeleteKey(profile + key);
            PlayerPrefs.Save();
            if (SessionState.GetString(ActiveProfile, "") == profile) SessionState.EraseString(ActiveProfile);
            SessionState.EraseString(PreviewProfile);
        }
    }
}
#endif
