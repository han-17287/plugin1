using System;

namespace Plugin.AncientCivilization
{
    // 층마다 실제 이야기 지점 두 곳에 다섯 방짜리 도로를 삽입한다.
    public static class AncientStoryPassageRules
    {
        public const int RoomsPerPassage = 5;
        public const int PassageCount = AncientChapterLayout.FloorCount * 2;
        public static int Group(int id) => id >= 1 && id <= PassageCount * RoomsPerPassage ? (id - 1) / RoomsPerPassage : -1;
        public static int First(int group) => group * RoomsPerPassage + 1;
        public static int Last(int group) => First(group) + RoomsPerPassage - 1;
        public static bool Opening(int floor, int stage) => floor == 0 && stage >= 1 && stage <= 3;
        public static bool Eligible(int floor, int stage) =>
            floor >= 0 && floor < AncientChapterLayout.FloorCount && (Opening(floor, stage) || stage >= 8);
        public static float Seam(int group, bool opening)
        {
            int floor = group / 2;
            if (opening && floor == 0) return group == 0 ? -221f : -125f;
            return (group & 1) == 0 ? AncientChapterLayout.FloorStart(floor) + 120f :
                AncientChapterLayout.CaseRight(floor * AncientChapterLayout.CasesPerFloor) + 22f;
        }
        public static int Crossed(float previous, float current, int floor, int stage)
        {
            // 층 이동·체크포인트 복귀로 뛴 거리를 도보 횡단으로 해석하지 않는다.
            if (!Eligible(floor, stage) || Math.Abs(current - previous) > 16f) return -1;
            for (int group = floor * 2; group < floor * 2 + 2; group++)
            {
                float seam = Seam(group, Opening(floor, stage));
                if ((previous < seam && current >= seam) || (previous > seam && current <= seam)) return group;
            }
            return -1;
        }
        public static bool ResumeValid(int id, int floor, int stage, bool opening)
        {
            int group = Group(id);
            return group >= 0 && group / 2 == floor && Eligible(floor, stage) &&
                opening == Opening(floor, stage);
        }
        public static bool Leaves(int id, bool east) => east ? id == Last(Group(id)) : id == First(Group(id));
    }
}
