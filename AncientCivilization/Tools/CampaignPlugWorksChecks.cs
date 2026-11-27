using System;
using Plugin.AncientCivilization;
using Plugin.PlayerCore;
using UnityEngine;

static class CampaignPlugWorksChecks
{
    static int checks;
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    static void Main()
    {
        var game = new GameObject("main").AddComponent<AncientGame>();
        var works = new AncientCampaignPlugWorks(game);
        for (int floor = 0; floor < AncientChapterLayout.FloorCount; floor++)
        {
            for (int site = 0; site < AncientCampaignPlugLayout.SitesPerFloor; site++)
            {
                float x = AncientCampaignPlugLayout.OriginX(floor, site);
                if (site == 1)
                {
                    float bridge = AncientChapterLayout.CaseRight(floor * 4);
                    Check(x - 2 > bridge + 26 && x + 10 < bridge + AncientChapterLayout.BridgeWidth - 2,
                        "복도 단말, 다리 위 주민과 다음 사건 봉쇄벽 사이에 설비와 착지가 들어간다.");
                }
                else if (floor == 0)
                    Check(x > -234 && x + 10 < -185, "첫 승강기는 기본 이동 수업 전에 걸어서 만난다.");
                else
                    Check(x > AncientChapterLayout.FloorStart(floor) + 26 && x + 10 < AncientChapterLayout.CaseLeft(floor * 4),
                        "첫 설비는 해당 층 도착역을 지나 첫 사건 전에 배치된다.");
                game.hero.transform.position = new Vector2(x, 1);
                works.Step(floor, .02f);
                Check(works.TryPort(0, out var port) && Math.Abs(port.point.x - x) < .001f, "현재 설비의 실제 소켓 위치");
                var device = (PlugDistrictDevice)port.owner;
                int[] secondKinds = { 1, 3, 4, 0, 2 };
                Check(device.Mechanism == (site == 0 ? floor : secondKinds[floor]), "층마다 입구와 사건 다리는 다른 종류의 설비를 제공한다.");
                Check(works.Refresh(port, out _) && !string.IsNullOrEmpty(works.Hint(port)), "활성 소켓과 안내");
                Check(!works.TryPort(1, out _), "전원이 없는 출력은 후보에서 사라진다.");
                var start = game.hero.transform.position;
                works.Dock(port);
                Check(device.Connected == 0 && game.hero.transform.position.x == start.x &&
                    game.hero.transform.position.y == start.y, "체결 신호는 장치만 움직이고 백 경위의 위치를 보존한다.");
                float age = device.Time;
                game.Paused = true;
                works.Step(floor, 2);
                Check(device.Time == age && device.Connected == 0, "대화 중 전원 타이머와 꽂힌 상태 보존");
                game.Paused = false;
                works.Release();
                Check(device.Connected == -1, "회수·이탈은 현재 접점을 해제한다.");
                if (works.Count == 2)
                {
                    Check(works.TryPort(1, out var second), "A 체결 및 회수 후 시간창에서 B를 선택할 수 있다.");
                    works.Dock(second);
                    Check(device.Connected == 1 && device.Circuit.Completed, "B에 실제 도킹해야 승인교 또는 착지대가 열린다.");
                    works.Release();
                }
                works.Step(floor,.02f,p=>true);
                Check(device.Connected==0,"설치된 손잡이가 닿는 첫 소켓에 전원을 공급한다.");
                works.Step(floor,.02f,p=>false);
                Check(device.Connected==-1,"블레이드 장착 또는 방전 시 공급 접점을 해제한다.");
                works.Reset();
                Check(!works.Refresh(port, out _) && !works.TryPort(-1, out _), "재시도는 낡은 접점 핸들을 무효화한다.");
                works.Dock(port);
                Check(device.Connected == -1, "과거의 도킹 신호를 새 회차에 재사용하지 않는다.");
                game.hero.transform.position = new Vector2(x + 80, 1);
                works.Step(floor, .02f);
                Check(!device.gameObject.activeSelf && works.Count == 0, "거리 밖의 표시·충돌·선택을 해제한다.");
            }
        }
        game.hero.transform.position = new Vector2(AncientCampaignPlugLayout.OriginX(4, 0), 1);
        game.City.ArenaActive = true;
        works.Step(4, .02f);
        Check(works.Count == 0, "보스전에서는 탐험 설비를 만들지 않는다.");
        game.City.ArenaActive = false;
        game.Production.Active = true;
        works.Step(4, .02f);
        Check(works.Count == 0, "트럭 사건에서는 탐험 설비를 만들지 않는다.");
        Check(AncientCampaignPlugLayout.NearbySite(5, 0) == -1 && AncientCampaignPlugLayout.NearbySite(0, float.NaN) == -1,
            "추격 전용 구역과 잘못된 좌표를 제외한다.");
        Console.WriteLine($"PASS: {checks} campaign plug placement, pause, actor lifecycle and port callbacks with device/physics doubles.");
    }
}

namespace Plugin.AncientCivilization
{
    public class AncientGame : Component
    {
        public bool HasSession = true, MainMenu, won, Paused;
        public readonly Component hero = new Component();
        public readonly Context Story = new Context(), City = new Context(), Production = new Context(), Finale = new Context();
        public class Context : Component { public bool LiftBusy, ArenaActive, Active; }
    }
    public class AncientBluePlugPorts
    {
        public enum Source { DistrictDevice }
        public readonly struct Port
        {
            internal readonly int slot;
            internal readonly uint generation;
            internal readonly Component owner;
            internal readonly Vector2 point;
            public Port(Source source, int slot, uint generation, Component owner, Vector2 point, Vector2 tow, Vector2 down, BluePlugSkill skills)
            { this.slot = slot; this.generation = generation; this.owner = owner; this.point = point; }
        }
    }
}

namespace Plugin.PlayerCore
{
    public enum BluePlugSkill { Tow = 1, Swing = 2 }
    public class PlugDistrictDevice : Component
    {
        public PlugDistrictCircuit Circuit;
        public int Connected => Circuit.Linked;
        public int Mechanism;
        public string Label => "장치";
        public float Time;
        public int SocketCount => Circuit.SocketCount;
        public bool SocketAvailable(int i) => Circuit.Available(i);
        public Vector2 SocketPoint(int i) => (Vector2)transform.position + Vector2.up * 3;
        public Vector2 TowPoint(int i) => SocketPoint(i) + Vector2.down;
        public void Step(float dt, Vector2 player) { Time += dt; Circuit.Step(dt); }
        public void Dock(int i) => Circuit.Dock(i);
        public void Release(int i) => Circuit.Release(i);
        public void ResetDevice() { Time = 0; Circuit.Reset(); }
        public static PlugDistrictDevice Spawn(Transform parent, int floor, Vector2 origin, bool advanced)
        {
            var item = new GameObject("device fixture").AddComponent<PlugDistrictDevice>();
            item.Mechanism = PlugDistrictCircuit.KindFor(floor, advanced);
            item.Circuit = new PlugDistrictCircuit(item.Mechanism, 5, 6, 5, 1.2f, .16f);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = origin;
            return item;
        }
    }
}
