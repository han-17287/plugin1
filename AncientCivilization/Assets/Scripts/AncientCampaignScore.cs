namespace Plugin.AncientCivilization
{
    public static class AncientCampaignScore
    {
        public static string Resolve(AncientGame game)
        {
            if (!game || AncientBootIntro.Active) return null;
            if (game.MainMenu) return game.HasSession ? "ignition-title" : "main-theme";
            if (!game.HasSession || !game.Story || !game.hero) return null;

            if (game.Finale)
            {
                switch (game.Finale.Phase)
                {
                    case 1:
                        // 노트를 읽는 동안에는 게임의 60초 작전 시계도 멈춰 있다.
                        return game.Finale.CountdownRemaining >= 60f && game.Story.DialogueOpen
                            ? "vacant-office" : "sixty-to-recovery";
                    case 2: return "descent-convoy";
                    case 3:
                    case 4: return "core-ignition";
                    case 5: return "first-ending";
                }
            }

            if (game.Story.LiftBusy) return "lift-between";
            int floor = game.Story.CurrentFloor;
            if (floor < 2) return AncientLowerScore.Resolve(game);

            var boss = game.boss;
            if (boss && boss.IsDistrictBoss && boss.IsAlive && boss.DistrictFloor == floor)
            {
                if (boss.IsSecretary)
                    return game.Story.DialogueOpen ? "clock-ante" : "clock-secretary";
                if (floor == 2)
                {
                    if (!game.Enforcer || game.Enforcer.InputLocked) return "sealed-gate";
                    return game.Enforcer.Phase >= 3 ? "enforcer-3" :
                           game.Enforcer.Phase == 2 ? "enforcer-2" : "enforcer-1";
                }
                if (floor == 3)
                {
                    // 보안 병기의 분노 진입과 마지막 체력 구간에서 편곡만 전환한다.
                    if (boss.HealthRatio <= .25f) return "residence-warden-3";
                    return boss.Enraged ? "residence-warden-2" : "residence-warden-1";
                }
                if (floor == 4)
                {
                    int phase = game.Sniper && game.Sniper.Owner == boss ? game.Sniper.Phase : 1;
                    switch (phase)
                    {
                        case 4: return "joker-beam";
                        case 3: return "joker-turrets";
                        case 2: return "joker-double";
                        default: return "joker-sightline";
                    }
                }
            }

            float x = game.hero.transform.position.x;
            bool cleared = game.City && (game.City.Cleared & (1 << floor)) != 0;
            if (!cleared && x >= AncientCityLayout.ArenaLeft(floor) - 12f &&
                x <= AncientCityLayout.ArenaRight(floor))
                return "sealed-gate";

            switch (floor)
            {
                case 2:
                    if (InJumpSection(9, x)) return "shutter-index";
                    if (InRoom(10, x)) return "ledger-imprint";
                    if (InRoom(11, x)) return "witness-crossing";
                    return "numbered-facades";
                case 3:
                    return InJumpSection(15, x)
                        ? "greenhouse-fault" : "glass-terrace";
                case 4:
                    if (game.City && game.City.SecretaryCleared && x >= AncientChapterLayout.SecretaryRight)
                        return "vacant-office";
                    if (cleared && game.City && !game.City.SecretaryCleared &&
                        x >= AncientChapterLayout.CaseRight(19) - 12f && x <= AncientChapterLayout.SecretaryRight)
                        return "clock-ante";
                    return "summit-trespass";
                default:
                    return null;
            }
        }

        // 사건을 마치고 되돌아갈 때도 실제로 밟고 있는 방과 발판의 곡을 고른다.
        static bool InRoom(int room, float x)
        {
            return x >= AncientChapterLayout.CaseLeft(room) && x <= AncientChapterLayout.CaseRight(room);
        }

        static bool InJumpSection(int room, float x)
        {
            return InRoom(room, x) && AncientFieldExpansion.IsJumpSector(room,
                AncientFieldExpansion.SectorAt(room, x - AncientChapterLayout.CaseLeft(room)));
        }
    }
}
