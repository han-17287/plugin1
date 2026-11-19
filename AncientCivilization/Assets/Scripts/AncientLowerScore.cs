namespace Plugin.AncientCivilization
{
    public static class AncientLowerScore
    {
        public static string Resolve(AncientGame game)
        {
            if (!game || !game.Story || !game.hero) return null;
            int floor = game.Story.CurrentFloor;
            if (floor != 0 && floor != 1) return null;
            float x = game.hero.transform.position.x;

            if (floor == 0)
            {
                if (game.Production && game.Production.Active)
                    return Pursuit(game.Production.Sequence);
                // 검거 대화를 다시 불러와도 피니시의 음악이 그대로 이어진다.
                if (game.Story.Stage == 7) return "cargo-verdict";
                if (x < AncientChapterLayout.CaseLeft(0))
                {
                    int section = AncientProductionLayout.SectionAt(x);
                    if (section == 1) return "shift-current";
                    if (section == 2)
                        return game.ProductionMap && game.ProductionMap.UpperInspectionRoute ? "heat-riser" : "shift-current";
                    if (section == 3) return "prism-circuit";
                    return "morning-inventory";
                }
            }
            else if (game.Living)
            {
                var boss = game.Living.Boss;
                if (boss && boss.Alive)
                    return boss.Phase >= 3 ? "iron-suit-3" : boss.Phase == 2 ? "iron-suit-2" : "iron-suit-1";
                if (game.Living.WaveActive) return "warehouse-crew";
            }

            return Field(floor, x, game.City ? game.City.Cleared : 0);
        }

        static string Pursuit(AncientProductionSequence sequence)
        {
            switch (sequence.Phase)
            {
                case AncientProductionPhase.Breach: return "breach-alarm";
                case AncientProductionPhase.Boarding:
                case AncientProductionPhase.Deck: return "roof-captain";
                case AncientProductionPhase.Knockback:
                    return sequence.Round == 0 ? "redline-remount" : "cargo-verdict";
                case AncientProductionPhase.Finisher: return "cargo-verdict";
                default: return sequence.Round == 0 ? "truck-pursuit" : "redline-remount";
            }
        }

        static string Field(int floor, float x, int cleared)
        {
            int firstCase = floor * AncientChapterLayout.CasesPerFloor;
            if (x < AncientChapterLayout.CaseLeft(firstCase)) return "alley-trail";
            // 다음 사건이 미리 활성화되더라도 플레이어가 다리를 건너야 새 지역 곡으로 바뀐다.
            for (int i = 0; i < AncientChapterLayout.CasesPerFloor; i++)
            {
                int mission = firstCase + i;
                float left = AncientChapterLayout.CaseLeft(mission);
                if (x < left) return "service-lantern";
                if (x > AncientChapterLayout.CaseRight(mission)) continue;
                int sector = AncientFieldExpansion.SectorAt(mission, x - left);
                if (AncientFieldExpansion.IsJumpSector(mission, sector)) return "filament";
                switch (mission)
                {
                    case 0: return sector == 1 ? "morning-inventory" : "shift-current";
                    case 1: return "prism-circuit";
                    case 2: return sector == 2 ? "main-theme" : "shift-current";
                    case 3: return "shift-current";
                    case 4: return "alley-trail";
                    case 5: return "salvage-orbit";
                    case 6: return "spotlight-grid";
                    default: return "warehouse-crew";
                }
            }
            return floor == 1 && (cleared & (1 << floor)) == 0 ? "sealed-gate" : "service-lantern";
        }
    }
}
