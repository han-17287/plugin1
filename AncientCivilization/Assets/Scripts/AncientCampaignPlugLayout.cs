using System;

namespace Plugin.AncientCivilization
{
    // 각 층의 입구와 첫 사건 뒤 휴식 다리에 설비를 둔다. 주요 전투와 사건 봉쇄벽은 다리 바깥에 있다.
    public static class AncientCampaignPlugLayout
    {
        public const int SitesPerFloor = 2;
        public const float ActivationRadius = 38;

        public static float OriginX(int floor, int site)
        {
            if (site == 1) return AncientChapterLayout.CaseRight(floor * AncientChapterLayout.CasesPerFloor) + 39;
            // 생산구의 첫 승강기는 점프 연습 상자보다 앞에 두어 이동 기술 해금 전에도 전원을 연결해 보게 한다.
            if (floor == 0) return -211;
            return AncientChapterLayout.FloorStart(floor) + 62;
        }

        public static int NearbySite(int floor, float heroX)
        {
            if ((uint)floor >= AncientChapterLayout.FloorCount) return -1;
            for (int site = 0; site < SitesPerFloor; site++)
                if (Math.Abs(heroX - (OriginX(floor, site) + 4)) <= ActivationRadius)
                    return site;
            return -1;
        }
    }
}
