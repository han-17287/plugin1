#if UNITY_EDITOR
using System;
using Plugin.PlayerCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.AncientCivilization.EditorChecks
{
    [InitializeOnLoad]
    public static class AncientStoryStreetSmokeChecks
    {
        const string Menu = "PLUGIN/검사/본편 콘센트 도로 왕복과 이어하기";
        const string RunKey = "PLUGIN.QA.StoryStreet.";
        const string ProfileKey = "PLUGIN.QA.ActiveProfile";
        static AncientGame original;
        static double deadline;
        static int lastFrame, waitFrame;
        static bool relaunch;

        static AncientStoryStreetSmokeChecks()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += PlayChanged;
        }
        static bool Running => SessionState.GetBool(RunKey + "running",false);
        static int Phase
        {
            get => SessionState.GetInt(RunKey + "phase",0);
            set => SessionState.SetInt(RunKey + "phase",value);
        }
        [MenuItem(Menu,true)]
        static bool Available() => !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling &&
            !Running && SessionState.GetString(ProfileKey,"") == "" && SceneManager.GetActiveScene().name == "Main";

        [MenuItem(Menu)]
        public static void Run()
        {
            if (!Available()) return;
            string profile = "PLUGIN.Ancient.QA.Street" + Guid.NewGuid().ToString("N") + ".";
            SessionState.SetString(ProfileKey,profile);
            SessionState.SetString(RunKey + "profile",profile);
            SessionState.SetBool(RunKey + "running",true);
            SessionState.SetBool(RunKey + "restart",false);
            Phase = 0;
            PlayerPrefs.SetInt(profile + "questStage",1);
            PlayerPrefs.SetInt(profile + "started",1);
            PlayerPrefs.Save();
            EditorApplication.isPlaying = true;
        }
        static void PlayChanged(PlayModeStateChange state)
        {
            if (!Running) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                deadline = EditorApplication.timeSinceStartup + 120;
                lastFrame = -1;
                EditorApplication.ExecuteMenuItem("Window/General/Game");
            }
            if (state != PlayModeStateChange.EnteredEditMode) return;
            original = null;
            if (SessionState.GetBool(RunKey + "restart",false))
            {
                // 실제 Play 세션을 재시작해 정적 필드와 씬 메모리까지 비운다.
                SessionState.SetBool(RunKey + "restart",false);
                relaunch = true;
                return;
            }
            string profile = SessionState.GetString(RunKey + "profile","");
            if (profile.StartsWith("PLUGIN.Ancient.QA.Street",StringComparison.Ordinal))
            {
                foreach (var key in AncientGame.ProgressSaveKeys) PlayerPrefs.DeleteKey(profile + key);
                foreach (var key in AncientGame.OptionSaveKeys) PlayerPrefs.DeleteKey(profile + key);
                string root = profile + "storySocket.0.";
                foreach (string key in new[]{"hp","captured","particles","equipped","cooldownMs"})
                { PlayerPrefs.DeleteKey(root + key); PlayerPrefs.DeleteKey(root + "field." + key); }
                foreach (string key in new[]{"field.valid","room","west","opening","complete","wallRoute.lastRoom"})
                    PlayerPrefs.DeleteKey(root + key);
                PlayerPrefs.Save();
            }
            if (SessionState.GetString(ProfileKey,"") == profile) SessionState.EraseString(ProfileKey);
            SessionState.SetBool(RunKey + "running",false);
        }
        static void Check(bool ok,string detail)
        {
            if (!ok) throw new InvalidOperationException(detail);
        }
        static void Place(Component actor, float x)
        {
            var body = actor.GetComponent<Rigidbody2D>();
            body.position = new Vector2(x,1.12f);
            body.linearVelocity = Vector2.zero;
            actor.transform.position = body.position;
            Physics2D.SyncTransforms();
        }
        static void Tick()
        {
            if (Running && relaunch && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                relaunch = false;
                EditorApplication.isPlaying = true;
                return;
            }
            if (!Running || !EditorApplication.isPlaying || EditorApplication.isPaused ||
                EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying ||
                SessionState.GetBool(RunKey + "restart",false)) return;
            try
            {
                if (deadline == 0) return;
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("본편 도로 전환을 기다리는 중 시간 초과. 단계 " + Phase);
                if (Time.frameCount == lastFrame) return;
                lastFrame = Time.frameCount;
                var game = AncientGame.Instance;
                if (!game || !game.hero || !game.hero.Loadout) return;
                Check(AncientGame.IsQAProfile && SessionState.GetString(ProfileKey,"") == SessionState.GetString(RunKey+"profile",""),
                    "격리된 검사 저장을 확인할 수 없습니다.");
                float seam = AncientStoryPassageRules.Seam(0,true);
                var patrol = PatrolEntry.InStory ? UnityEngine.Object.FindAnyObjectByType<PatrolExpedition>() : null;
                switch (Phase)
                {
                    case 0:
                        if (game.Paused || !game.GameplayInputReady) return;
                        original = game;
                        game.credits = 17;
                        game.hero.hp = 74.25f;
                        game.hero.particles = 30.5f;
                        game.hero.SpendCapturedEnergy(); game.hero.AddCapturedEnergy(9.25f);
                        game.hero.Loadout.EnterRoom(new SummonedBladeCarry {equipped=true});
                        Place(game.hero,seam - .45f);
                        waitFrame = Time.frameCount + 2; Phase = 1;
                        break;
                    case 1:
                    case 4:
                        if (!game.GameplayInputReady || Time.frameCount < waitFrame) return;
                        Place(game.hero,seam + .35f);
                        Phase = Phase == 1 ? 2 : 5;
                        break;
                    case 2:
                        if (!patrol) return;
                        Check(game == original && !game.isActiveAndEnabled && patrol.RoomId == 1 && patrol.SocketRoute, "도보로 첫 실제 방 진입");
                        Check(patrol.Player.GetComponent<SummonedBlade>().Equipped && patrol.Charge == 39, "장비와 두 입자 저장고 인계");
                        Check(patrol.SpendCharge(5),"도로에서 본편 입자 소비"); patrol.AddCharge(2);
                        Place(patrol.Player,.45f); Phase = 3;
                        break;
                    case 3:
                        if (PatrolEntry.InStory || !game.GameplayInputReady) return;
                        Check(game == original && game.isActiveAndEnabled && SceneManager.GetActiveScene() == game.gameObject.scene &&
                            game.hero.transform.position.x < seam && game.hero.Loadout.Equipped, "서쪽 퇴장 후 원래 본편·장비 복원");
                        Place(game.hero,seam - .4f);
                        waitFrame = Time.frameCount + 2; Phase = 4;
                        break;
                    case 5:
                        if (!patrol || patrol.RoomId != 1) return;
                        Place(patrol.Player,47.55f); Phase = 6;
                        break;
                    case 6:
                        if (!patrol || patrol.RoomId != 2) return;
                        Check(patrol.Player.GetComponent<SummonedBlade>().Equipped,"동쪽 이음에서 장착 유지");
                        Check(AncientGame.Saved(AncientStorySocketPassage.SavePrefix() + "room") == 2,"두 번째 방 자동 저장");
                        Phase = 7;
                        SessionState.SetBool(RunKey + "restart",true);
                        EditorApplication.isPlaying = false;
                        break;
                    case 7:
                        if (!patrol) return;
                        Check(patrol.RoomId == 2 && game.credits == 17 && game.Story.Stage == 1 &&
                            Mathf.Abs(game.hero.hp - 74.25f) < .02f && Mathf.Abs(game.hero.particles - 27.5f) < .02f &&
                            Mathf.Abs(game.hero.CaptureEnergy - 9.25f) < .02f, "새 세션의 정확한 방·체력·재화·입자");
                        var blade = patrol.Player.GetComponent<SummonedBlade>();
                        Check(blade.Equipped,"새 세션의 장비");
                        blade.LeaveRoom(); blade.EnterRoom(new SummonedBladeCarry {cooldown=40});
                        original = game; Phase = 8;
                        break;
                    case 8:
                        if (patrol) { Place(patrol.Player,47.55f); return; }
                        string prefix = AncientStorySocketPassage.SavePrefix();
                        Check(game == original && game.isActiveAndEnabled && game.hero.transform.position.x > seam &&
                            game.Story.Stage == 1 && game.hero.Loadout.State == SummonedBladeState.Cooldown &&
                            game.hero.Loadout.Remaining > 0 && AncientGame.Saved(prefix + "room") == 0 &&
                            AncientGame.Saved(prefix + "field.cooldownMs") > 0,"다섯 방 끝의 본편 도로·재생 시간·저장 정리");
                        Debug.Log("PASS: 본편 도보 진입·역방향 복귀·실제 Play 재시작·동쪽 복귀·장비·재화·입자 보존");
                        Phase = 9;
                        EditorApplication.isPlaying = false;
                        break;
                }
            }
            catch (Exception error)
            {
                Phase = 9; SessionState.SetBool(RunKey + "restart",false);
                Debug.LogError("FAIL: 본편 콘센트 도로 검사 " + error);
                EditorApplication.isPlaying = false;
            }
        }
    }
}
#endif
