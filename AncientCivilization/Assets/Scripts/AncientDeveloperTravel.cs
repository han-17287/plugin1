using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.AncientCivilization
{
    public enum AncientDeveloperGroup { Floor, Area, Boss }

    public sealed class AncientDeveloperDestination
    {
        public readonly string Id, Name;
        public readonly int Floor;
        public readonly AncientDeveloperGroup Group;
        internal readonly int Stage, CompletedCases, ClearedFloors, Mission, ProductionRound, FinalePhase;
        internal readonly bool GuardWaveCleared, OrbitalWithdrawn, SecretaryCleared, StartOnDeck, StartFinisher;
        internal readonly float EntryX;

        internal AncientDeveloperDestination(string id, string name, int floor, AncientDeveloperGroup group,
            float entryX, int completedCases, int stage = 9, int mission = -1, int round = 0,
            bool guards = false, bool deck = false, bool finisher = false, int finale = 0)
        {
            Id = id;
            Name = name;
            Floor = floor;
            Group = group;
            Stage = stage;
            EntryX = entryX;
            CompletedCases = completedCases;
            Mission = mission;
            ProductionRound = round;
            StartOnDeck = deck;
            StartFinisher = finisher;
            FinalePhase = finale;
            // 19번 사건은 조커가 물러난 뒤에 시작한다. 14번 사건 입구는 검사대 동쪽이다.
            ClearedFloors = ((1 << floor) - 1) & ~1;
            if (mission == 19 || completedCases == AncientCampaignRules.MissionCount) ClearedFloors |= 1 << 4;
            OrbitalWithdrawn = completedCases > 14 || mission == 14;
            GuardWaveCleared = guards || floor > 1;
            SecretaryCleared = id == "office" || finale != 0;
        }

        internal Dictionary<string, int> BuildProfile()
        {
            var data = new Dictionary<string, int>
            {
                ["started"] = 1, ["questStage"] = Stage, ["cityFloor"] = Floor,
                ["upperCity"] = Floor > 0 ? 1 : 0, ["campaignVersion"] = AncientCampaignRules.RouteVersion,
                ["campaignComplete"] = (1 << CompletedCases) - 1, ["campaignActive"] = Mission,
                ["cityCleared"] = ClearedFloors, ["orbitalMidWon"] = OrbitalWithdrawn ? 1 : 0,
                ["livingGuardsCleared"] = GuardWaveCleared ? 1 : 0, ["secretaryCleared"] = SecretaryCleared ? 1 : 0,
                ["productionRound"] = ProductionRound, ["productionVersion"] = 1, ["finalePhase"] = FinalePhase
            };
            if (Stage >= 7)
            {
                data["storyCores"] = 7;
                data["storyBoss"] = 1;
                data["residencePoints"] = Math.Max(1, Floor) * 1000;
            }
            // 검수 회랑은 이동 과제 후 진입한다. 재고 보고부터는 검수 과제도 완료돼 있다.
            if (Stage >= 2) data["questFlags"] = Stage == 2 ? 7 : Stage == 3 ? 31 : 255;
            if (Mission >= 0) data["campaignChapterStarted"] = 1 << Floor;
            if (Stage >= 8)
            {
                data["retryActive"] = 1;
                data["retryFloor"] = Floor;
                data["retryX"] = (int)Math.Round(EntryX * 100.0);
                data["retryY"] = 130;
            }
            return data;
        }
    }

    public static class AncientDeveloperTravel
    {
        public static IReadOnlyList<AncientDeveloperDestination> Destinations { get; } = BuildCatalog().AsReadOnly();
        static Dictionary<string, int> active, pending;
        static AncientDeveloperDestination pendingTarget, productionArrival;
        static bool loading, quickStart, returnToSession;
        public static bool IsVisiting => active != null;

        static List<AncientDeveloperDestination> BuildCatalog()
        {
            const AncientDeveloperGroup floor = AncientDeveloperGroup.Floor, area = AncientDeveloperGroup.Area, boss = AncientDeveloperGroup.Boss;
            var rows = new List<AncientDeveloperDestination>();
            for (int f = 0; f <= AncientCityLayout.LastFloor; f++)
            {
                rows.Add(new AncientDeveloperDestination("floor-" + f, AncientCityLayout.Name(f), f, floor,
                    AncientCityLayout.Entry(f), f * 4, f == 0 ? 0 : 9));
                foreach (var mission in AncientCampaignData.Missions)
                    if (mission.Floor == f)
                        rows.Add(new AncientDeveloperDestination("case-" + mission.Id, mission.Title, f, area,
                            AncientCampaignRules.Left(mission.Id) + 6, mission.Id, f == 0 ? 8 : 9, mission.Id));
            }
            rows.Add(new AncientDeveloperDestination("training-wire", "검수 회랑 · 와이어 훈련", 0, area, 0, 0, 2));
            rows.Add(new AncientDeveloperDestination("training-blade", "장비 검수 · 입자 훈련", 0, area, 0, 0, 3));
            rows.Add(new AncientDeveloperDestination("stock", "코어 재고 보고 · 격벽 돌파", 0, area, 0, 0, 4));
            rows.Add(new AncientDeveloperDestination("chase-1", "코어 수송로 · 첫 번째 오토바이 추격", 0, boss, 0, 0, 5));
            rows.Add(new AncientDeveloperDestination("deck-1", "운송대장 · 첫 번째 트럭 지붕전", 0, boss, 0, 0, 5, deck: true));
            rows.Add(new AncientDeveloperDestination("chase-2", "코어 수송로 · 두 번째 오토바이 추격", 0, boss, 0, 0, 5, round: 1));
            rows.Add(new AncientDeveloperDestination("deck-2", "운송대장 · 두 번째 트럭 지붕전", 0, boss, 0, 0, 5, round: 1, deck: true));
            rows.Add(new AncientDeveloperDestination("production-finish", "세 번째 탑승 · 화물칸 절단", 0, area, 0, 0, 5, round: 1, finisher: true));
            rows.Add(new AncientDeveloperDestination("arrest", "운송대장 검거 · 코어 인계", 0, area, 0, 0, 7));
            rows.Add(new AncientDeveloperDestination("living-wave", "조직 본거지 · 경비대", 1, boss, AncientCityLayout.ArenaLeft(1) + 2, 8));
            rows.Add(new AncientDeveloperDestination("living-chief", "조직 두목 · 거대 로봇 슈트", 1, boss, AncientCityLayout.ArenaLeft(1) + 2, 8, guards: true));
            rows.Add(new AncientDeveloperDestination("enforcer", "기록 집행관", 2, boss, AncientCityLayout.ArenaLeft(2) + 2, 12));
            rows.Add(new AncientDeveloperDestination("orbital-first", "궤도 봉쇄기 · 화물 검사대", 3, boss, AncientCityLayout.OrbitalLeft + 2, 14));
            rows.Add(new AncientDeveloperDestination("orbital-return", "코어 보안병기 · 외벽 재교전", 3, boss, AncientCityLayout.ArenaLeft(3) + 2, 16));
            rows.Add(new AncientDeveloperDestination("joker", "조커 · 최상층 저격수", 4, boss, AncientCityLayout.ArenaLeft(4) + 2, 19));
            rows.Add(new AncientDeveloperDestination("secretary", "비서실장 · 시계 전투", 4, boss, AncientCityLayout.BossArenaLeft(4, true) + 2, 20));
            rows.Add(new AncientDeveloperDestination("office", "회장 집무실 · 고대 문명 회수 계획", 4, area, AncientChapterLayout.OfficeX - 1, 20));
            rows.Add(new AncientDeveloperDestination("convoy", "회장 호송 트럭 · 최하층 추격", 4, boss, AncientChapterLayout.OfficeX, 20, finale: 2));
            return rows;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            active = pending = null;
            productionArrival = pendingTarget = null;
            loading = quickStart = returnToSession = false;
        }

        public static int Read(string key, int fallback) => active.TryGetValue(key, out int value) ? value : fallback;
        public static void Write(string key, int value) => active[key] = value;

        public static void ClearSandboxProgress()
        {
            if (!IsVisiting) return;
            foreach (var key in AncientGame.ProgressSaveKeys) active.Remove(key);
            productionArrival = null;
        }

        public static bool Go(AncientGame game, AncientDeveloperDestination destination)
        {
            if (!game || destination == null || loading || !CanReloadMain()) return false;
            if (!IsVisiting)
            {
                returnToSession = game.HasSession && !game.MainMenu;
                // 일반 플레이에서 마지막 체크포인트 이후 획득한 보상과 플레이 시간을 기록한다.
                if (returnToSession && game.hero && game.hero.Combat) game.Save();
            }
            return Reload(PracticeProfile(destination), destination);
        }

        public static bool GoFromPatrol(AncientDeveloperDestination destination,bool resumeOrdinaryGame)
        {
            if(destination==null || loading || SceneManager.GetActiveScene().name!="PatrolExpedition" ||
                !Application.CanStreamedLevelBeLoaded("Main")) return false;
            if(!IsVisiting) returnToSession=resumeOrdinaryGame;
            return Reload(PracticeProfile(destination),destination);
        }

        static Dictionary<string,int> PracticeProfile(AncientDeveloperDestination destination)
        {
            var data = destination.BuildProfile();
            // 옵션의 미설정 값도 보존해야 각 하위 시스템이 고유한 기본값을 선택한다.
            foreach (var key in AncientGame.OptionSaveKeys)
            {
                int option = AncientGame.Saved(key, int.MinValue);
                if (option != int.MinValue) data[key] = option;
            }
            return data;
        }

        public static bool Return(AncientGame game) => game && IsVisiting && !loading && CanReloadMain() && Reload(null, null);

        static bool CanReloadMain() => SceneManager.GetActiveScene().name == "Main" && Application.CanStreamedLevelBeLoaded("Main");

        static bool Reload(Dictionary<string, int> next, AncientDeveloperDestination destination)
        {
            loading = true;
            pending = next;
            pendingTarget = destination;
            try { SceneManager.LoadScene("Main", LoadSceneMode.Single); }
            catch (Exception error)
            {
                loading = false;
                pending = null;
                pendingTarget = null;
                Debug.LogException(error);
                return false;
            }
            return true;
        }

        // 이전 씬의 종료 콜백까지 이전 저장소를 사용하고, 새 게임의 첫 읽기 전에 전환한다.
        public static void EnterScene()
        {
            if (!loading) return;
            active = pending;
            quickStart = IsVisiting || returnToSession;
            productionArrival = pendingTarget;
            pending = null;
            pendingTarget = null;
            loading = false;
        }

        public static bool ConsumeQuickStart()
        {
            bool value = quickStart;
            quickStart = false;
            return value;
        }

        internal static bool ConsumeProductionArrival(out bool finisher)
        {
            finisher = productionArrival != null && productionArrival.StartFinisher;
            bool useDeck = finisher || (productionArrival != null && productionArrival.StartOnDeck);
            productionArrival = null;
            return useDeck;
        }
    }
}
