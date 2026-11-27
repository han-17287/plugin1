using System;
using Plugin.PlayerCore;

static class PlugDistrictCircuitChecks
{
    static int checks;

    static void Require(bool condition, string detail)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(detail);
    }

    static PlugDistrictCircuit Make(int kind) => new PlugDistrictCircuit(kind, 5, 6, 5, 4, .28f);

    static void RunClock(PlugDistrictCircuit circuit, float seconds, int hz)
    {
        int steps = (int)Math.Round(seconds * hz);
        for (int i = 0; i < steps; i++) circuit.Step(1f / hz);
    }

    static void Main()
    {
        var second = new[] { 1, 3, 4, 0, 2 };
        foreach (int hz in new[] { 30, 50, 60, 120 })
        {
            for (int floor = 0; floor < 5; floor++)
            {
                Require(PlugDistrictCircuit.KindFor(floor, true) == second[floor] &&
                    PlugDistrictCircuit.KindFor(floor, false) == floor && floor != second[floor],
                    "한 층에서 서로 다른 두 작동 원리를 만나야 한다.");
                foreach (bool advanced in new[] { false, true })
                {
                    int kind = PlugDistrictCircuit.KindFor(floor, advanced);
                    var c = Make(kind);
                    Require(c.Available(0) && !c.Available(1) && !c.Available(-1) && !c.Available(2),
                        "출발 시 A만 꽂을 수 있다.");
                    Require(!c.Dock(1) && !c.Dock(-1) && !c.Dock(2), "비활성 단자는 작동하지 않는다.");
                    Require(c.Dock(0) && !c.Dock(0), "매 물리 프레임에 전원 타이머가 다시 시작되면 안 된다.");
                    float time = c.Remaining;
                    c.Step(0); c.Step(-1); c.Step(float.NaN); c.Step(float.PositiveInfinity);
                    Require(c.Remaining == time, "정지 시계나 잘못된 델타가 전원을 소모하면 안 된다.");
                    c.Release(1);
                    Require(c.Linked == 0, "다른 단자 해제가 현재 연결을 끊었다.");
                    switch (kind)
                    {
                        case 0:
                            RunClock(c, 8, hz);
                            Require(c.WantsSurface(0), "플러그를 유지하는 동안 화물대가 내려갔다.");
                            c.Release(0);
                            RunClock(c, 3.8f, hz);
                            Require(c.WantsSurface(0), "회수 직후 화물대로 뛸 여유가 없다.");
                            RunClock(c, .5f, hz);
                            Require(!c.WantsSurface(0), "승강기가 복귀하지 않았다.");
                            break;
                        case 1:
                            RunClock(c, 4.75f, hz);
                            Require(c.WantsSurface(0), "제한 시간 이전에 셔터가 닫혔다.");
                            RunClock(c, .5f, hz);
                            Require(!c.WantsSurface(0) && !c.Dock(0), "꽂은 채로는 셔터 타이머를 반복할 수 없다.");
                            c.Release(0);
                            Require(c.Dock(0) && c.WantsSurface(0), "셔터를 다시 열 수 있어야 한다.");
                            break;
                        case 2: case 4:
                            Require(c.Available(1) && !c.Dock(1), "A를 회수한 뒤에 B로 갈아꽂는다.");
                            c.Release(0);
                            RunClock(c, kind == 2 ? 6.2f : 5.2f, hz);
                            Require(!c.Available(1) && !c.Dock(1) && !c.WantsSurface(0), "시간 초과 뒤 B 단자가 살아 있다.");
                            Require(c.Dock(0), "A에서 다시 시작할 수 있어야 한다.");
                            c.Release(0);
                            RunClock(c, 2, hz);
                            Require(c.Dock(1) && c.WantsSurface(0), "두 번째 단자에 꽂으면 통로가 열린다.");
                            RunClock(c, 10, hz);
                            Require(c.Available(1) && c.WantsSurface(0), "이미 꽂힌 중계 단자가 꺼졌다.");
                            c.Release(1);
                            RunClock(c, 4.3f, hz);
                            Require(c.WantsSurface(0) == (kind == 2), "승인교 유지 또는 임시 착지대 만료가 틀리다.");
                            break;
                        case 3:
                            Require(c.WantsSurface(0) && !c.WantsSurface(1), "차양의 시작 순서가 틀리다.");
                            RunClock(c, .36f, hz);
                            Require(c.WantsSurface(1) && !c.WantsSurface(2), "중간 차양이 건너뛰어졌다.");
                            RunClock(c, .4f, hz);
                            Require(c.WantsSurface(2), "마지막 차양이 안 열렸다.");
                            c.Release(0);
                            RunClock(c, 4.3f, hz);
                            Require(!c.WantsSurface(0) && !c.WantsSurface(2), "차양 복귀 실패");
                            break;
                    }
                    c.Reset(); c.Reset();
                    Require(c.Linked == -1 && !c.WantsSurface(0) && !c.Completed && c.Dock(0),
                        "다른 방/재시도에 이전 전력이 남았다.");
                }
            }
        }
        Console.WriteLine("PASS: " + checks + " district plug circuit, clock and reset checks");
    }
}
