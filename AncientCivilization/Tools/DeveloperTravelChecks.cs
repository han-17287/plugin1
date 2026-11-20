using System;
using System.Collections.Generic;
using System.Linq;
using Plugin.AncientCivilization;
using UnityEngine.SceneManagement;

// Unity 외부에서도 실제 목적지·캠페인 조건·저장소 전환을 실행한다. 씬 로드만 예약한다.
namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) {}
    }
    public static class Application
    {
        public static bool CanStreamedLevelBeLoaded(string name) => name == "Main";
    }
    public static class Debug { public static void LogException(Exception error) {} }
}
namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single }
    public struct Scene { public string name; }
    public static class SceneManager
    {
        public static string Current = "Main";
        public static bool Fail;
        public static Scene GetActiveScene() => new Scene { name = Current };
        public static void LoadScene(string name, LoadSceneMode mode)
        {
            if (Fail) throw new InvalidOperationException();
        }
    }
}
namespace Plugin.AncientCivilization
{
    public sealed class TestHero
    {
        public bool Combat = true;
        public static implicit operator bool(TestHero hero) => hero != null;
    }
    public sealed class AncientGame
    {
        public TestHero hero = new TestHero();
        public int DirtyScore;
        public void Save() { Normal["score"] = DirtyScore; }
        public bool HasSession, MainMenu;
        public static readonly string[] OptionSaveKeys = {"frameTarget", "muted"};
        public static readonly string[] ProgressSaveKeys = {"questStage", "campaignComplete"};
        public static readonly Dictionary<string, int> Normal = new Dictionary<string, int> {["frameTarget"] = 144, ["questStage"] = 1};
        public static int Saved(string key, int fallback = 0) => AncientDeveloperTravel.IsVisiting
            ? AncientDeveloperTravel.Read(key, fallback)
            : Normal.TryGetValue(key, out int value) ? value : fallback;
        public static implicit operator bool(AncientGame game) => game != null;
    }
}

class DeveloperTravelChecks
{
    static int passed;
    static void Check(bool condition, string detail)
    {
        if (!condition) throw new Exception(detail);
        passed++;
    }
    static AncientDeveloperDestination At(string id) => AncientDeveloperTravel.Destinations.Single(d => d.Id == id);
    static int Resume(Dictionary<string, int> p)
    {
        int active = p["campaignActive"];
        int chapters = AncientCampaignRules.RestoreChapters(p.GetValueOrDefault("campaignChapterStarted"), p["campaignComplete"], active);
        return AncientCampaignRules.ResumeCase(p["campaignComplete"], chapters, active, p["cityFloor"], p["questStage"], p["cityCleared"]);
    }
    static void Main()
    {
        var destinations = AncientDeveloperTravel.Destinations;
        Check(destinations.Select(d => d.Id).Distinct().Count() == destinations.Count, "중복 목적지");
        Check(destinations.Count(d => d.Group == AncientDeveloperGroup.Floor) == 5, "층 개수");
        foreach (var row in destinations)
        {
            var profile = row.BuildProfile();
            if (profile["questStage"] >= 8)
                Check(AncientCityLayout.SafeResume(row.Floor, profile["retryX"] / 100f, profile["retryY"] / 100f), row.Id + " 체크포인트");
            if (row.Group == AncientDeveloperGroup.Floor) Check(Resume(profile) == -1, row.Id + " 층 입구");
        }
        for (int id = 0; id < AncientCampaignRules.MissionCount; id++)
        {
            var p = At("case-" + id).BuildProfile();
            Check(Resume(p) == id && !AncientCampaignRules.OrbitalBlocksCase(id, p["orbitalMidWon"] != 0), "사건 진입 " + id);
            Check(At("case-" + id).Name == AncientCampaignData.Missions[id].Title, "사건 이름 " + id);
        }
        foreach (string id in new[]{"living-wave","living-chief","enforcer","orbital-first","orbital-return","joker","secretary","office","convoy"})
        {
            var d = At(id);
            var p = d.BuildProfile();
            int readyCase = Resume(p);
            Check(readyCase == -1 || AncientCampaignRules.OrbitalBlocksCase(readyCase, p["orbitalMidWon"] != 0), id + " 조사 자동 복구 충돌");
            if (id == "orbital-first")
                Check(p["orbitalMidWon"] == 0 && AncientCampaignRules.Complete(p["campaignComplete"], 13) &&
                    d.EntryX > AncientCityLayout.OrbitalLeft + 1 && d.EntryX < AncientCityLayout.OrbitalRight, id);
            else if (id == "office" || id == "convoy")
                Check(p["secretaryCleared"] == 1 && p["finalePhase"] == (id == "convoy" ? 2 : 0), id);
            else if (id == "secretary")
                Check(AncientCampaignRules.FloorCount(p["campaignComplete"], 4) == 4 && (p["cityCleared"] & 16) != 0 &&
                    p["secretaryCleared"] == 0 && d.EntryX > AncientCityLayout.BossArenaLeft(4, true) + 1, id);
            else
                Check(AncientCampaignRules.ReadyForBoss(p["campaignComplete"], d.Floor) && (p["cityCleared"] & (1 << d.Floor)) == 0 &&
                    d.EntryX > AncientCityLayout.ArenaLeft(d.Floor) + 1, id);
        }
        Check(At("living-wave").BuildProfile()["livingGuardsCleared"] == 0 && At("living-chief").BuildProfile()["livingGuardsCleared"] == 1, "경비대 생략 선택");
        Check(At("orbital-return").BuildProfile()["orbitalMidWon"] == 1, "2차 검사대");
        var game = new AncientGame { HasSession = true, DirtyScore = 115 };
        SceneManager.Current = "Other";
        Check(!AncientDeveloperTravel.Go(game, At("joker")), "다른 씬 차단");
        SceneManager.Current = "Main";
        SceneManager.Fail = true;
        Check(!AncientDeveloperTravel.Go(game, At("joker")) && !AncientDeveloperTravel.IsVisiting, "씬 오류 복구");
        SceneManager.Fail = false;
        Check(AncientDeveloperTravel.Go(game, At("joker")), "이동 시작");
        Check(!AncientDeveloperTravel.IsVisiting && !AncientDeveloperTravel.Go(game, At("secretary")), "중복 이동·이전 씬 저장 보호");
        AncientDeveloperTravel.EnterScene();
        Check(AncientDeveloperTravel.ConsumeQuickStart() && !AncientDeveloperTravel.ConsumeQuickStart(), "자동 진입 1회");
        Check(AncientGame.Saved("frameTarget") == 144 && AncientGame.Saved("muted", 7) == 7, "원래 옵션과 기본값");
        AncientDeveloperTravel.Write("questStage", 77);
        Check(AncientDeveloperTravel.Go(game, At("deck-2")) && AncientGame.Saved("questStage") == 77, "이동 예약 후 이전 연습 상태 보존");
        AncientDeveloperTravel.EnterScene();
        Check(AncientGame.Saved("productionRound") == 1 && AncientDeveloperTravel.ConsumeProductionArrival(out bool finisher) && !finisher, "지붕전 바로 입장");
        Check(!AncientDeveloperTravel.ConsumeProductionArrival(out _), "재시도 시 정규 추격");
        Check(AncientDeveloperTravel.Go(game, At("production-finish")), "피니시 이동");
        AncientDeveloperTravel.EnterScene();
        Check(AncientDeveloperTravel.ConsumeProductionArrival(out finisher) && finisher, "세 번째 탑승");
        AncientDeveloperTravel.ClearSandboxProgress();
        Check(AncientGame.Saved("questStage") == 0 && AncientGame.Saved("frameTarget") == 144 && AncientGame.Normal["questStage"] == 1, "새 게임 보호");
        Check(AncientDeveloperTravel.Return(game) && AncientDeveloperTravel.IsVisiting, "복귀 예약");
        AncientDeveloperTravel.EnterScene();
        Check(!AncientDeveloperTravel.IsVisiting && AncientDeveloperTravel.ConsumeQuickStart() && AncientGame.Saved("questStage") == 1, "원래 저장 복귀");
        Check(AncientGame.Saved("score") == 115, "체크포인트 이후의 일반 점수 보존");
        game.MainMenu = true;
        game.DirtyScore = 7;
        Check(AncientDeveloperTravel.Go(game, At("enforcer")), "타이틀에서 연습");
        AncientDeveloperTravel.EnterScene();
        Check(AncientDeveloperTravel.Return(game), "타이틀로 복귀");
        AncientDeveloperTravel.EnterScene();
        Check(!AncientDeveloperTravel.ConsumeQuickStart() && AncientGame.Saved("score") == 115, "원래 메인 화면 복원");
        AncientGame.Normal["firstRunCleared"] = 1;
        Check(AncientDeveloperTravel.Go(game, At("floor-0")), "튜토리얼 재방문");
        AncientDeveloperTravel.EnterScene();
        Check(AncientGame.Saved("questStage") == 0 && AncientGame.Saved("firstRunCleared") == 0, "새 튜토리얼 시작과 완료 플래그 격리");
        Check(AncientDeveloperTravel.Return(game), "튜토리얼에서 복귀");
        AncientDeveloperTravel.EnterScene();
        Check(AncientGame.Saved("firstRunCleared") == 1, "일반 1회차 완료 보존");
        SceneManager.Current="PatrolExpedition";
        Check(AncientDeveloperTravel.GoFromPatrol(At("joker"),true),"순찰옥상에서 조커 직행");
        Check(!AncientDeveloperTravel.GoFromPatrol(At("enforcer"),true),"순찰 메뉴 중복 이동 방지");
        SceneManager.Current="Main"; AncientDeveloperTravel.EnterScene();
        Check(AncientDeveloperTravel.IsVisiting && AncientGame.Saved("cityFloor")==4 && AncientGame.Saved("secretaryCleared")==0,"조커 연습 구역의 전투 조건");
        Check(AncientDeveloperTravel.Return(game),"조커 연습 종료");
        AncientDeveloperTravel.EnterScene();
        Check(AncientDeveloperTravel.ConsumeQuickStart() && AncientGame.Saved("firstRunCleared")==1,"순찰에서 진입한 원래 캠페인 보존");
        Console.WriteLine(destinations.Count + " destinations, " + passed + " developer travel checks passed.");
    }
}
