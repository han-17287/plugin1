using System;
using System.Collections.Generic;
using Plugin.AncientCivilization;
using Plugin.PlayerCore;
using Route = Plugin.AncientCivilization.AncientStoryPassageRules;

static class StorySocketPassageChecks
{
    static int checks;
    static void Check(bool success, string message)
    {
        if (!success) throw new Exception(message);
        checks++;
    }
    public static void Run()
    {
        var visited = new HashSet<string>();
        for (int group = 0; group < Route.PassageCount; group++)
        {
            int floor = group / 2, stage = floor == 0 ? group + 1 : 8;
            bool opening = Route.Opening(floor, stage);
            float seam = Route.Seam(group, opening);
            if (opening)
            {
                Check(AncientProductionLayout.GroundHeight(seam - 1.75f) == 0 && AncientProductionLayout.GroundHeight(seam + 1.75f) == 0, "생산층 안전 복귀 바닥");
                Check(seam > AncientProductionLayout.TutorialSpawn && seam < AncientProductionLayout.StockX, "생산장 재고 확인 전");
            }
            else
            {
                int firstCase = floor * AncientChapterLayout.CasesPerFloor;
                Check((group & 1) == 0 ?
                    seam > AncientChapterLayout.FloorStart(floor) && seam + 2 < AncientChapterLayout.CaseLeft(firstCase) :
                    seam - 2 > AncientChapterLayout.CaseRight(firstCase) && seam + 2 < AncientChapterLayout.CaseLeft(firstCase + 1),
                    "기존 임무·보스·봉쇄문을 넘기지 않는 거리");
            }
            Check(Route.Crossed(seam - 1, seam, floor, stage) == group, "동쪽 도보 진입");
            Check(Route.Crossed(seam + 1, seam, floor, stage) == group, "서쪽 도보 진입");
            Check(Route.Crossed(seam - 100, seam + 1, floor, stage) == -1, "빠른이동은 도보 판정에서 제외");
            Check(Route.Crossed(seam - 3, seam - 1, floor, stage) == -1, "이전 저장 위치 인근 정지");
            int first = Route.First(group), last = Route.Last(group);
            for (int room = first; room <= last; room++)
            {
                Check(visited.Add(PatrolSocketCatalog.Name(room)), "중복 방");
                Check(Route.Group(room) == group && Route.ResumeValid(room, floor, stage, opening), "각 방 저장 복원");
                Check(Route.Leaves(room, true) == (room == last) && Route.Leaves(room, false) == (room == first), "역방향 되돌아가기");
                Check(!Route.ResumeValid(room, (floor + 1) % 5, stage, opening), "다른 층의 오래된 대기 저장");
            }
            // 동쪽 출구에서 계속 걸으면 다시 열리지 않고, 되돌아 건너면 같은 길에 재진입한다.
            Check(Route.Crossed(seam + 1.75f, seam + 2f, floor, stage) == -1 &&
                Route.Crossed(seam + .1f, seam - .1f, floor, stage) == group, "완주 후 양방향 재방문");
        }
        Check(visited.Count == 50, "실제 소켓 구간 50개");
        for (int stage = 4; stage <= 7; stage++)
            Check(Route.Crossed(-222, -220, 0, stage) == -1, "재고 신고·추격·검거 중 진입 금지");
        Check(!Route.ResumeValid(0, 0, 1, true) && !Route.ResumeValid(51, 4, 8, false), "손상된 방 번호");
        Check(!Route.ResumeValid(1, 0, 8, true), "생산층 도로 이관");
        Console.WriteLine("PASS " + checks + " story traversal checks, 50 authored rooms, 10 bidirectional street connections");
    }
}
