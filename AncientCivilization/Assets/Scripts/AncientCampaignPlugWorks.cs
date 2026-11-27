using Plugin.PlayerCore;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 본편 진행을 소유한 장소에서만 장치를 시뮬레이션한다. 방문한 설비만 생성하고, 현장을 벗어나면 입력과 충돌을 함께 해제한다.
    internal sealed class AncientCampaignPlugWorks
    {
        readonly AncientGame game;
        readonly PlugDistrictDevice[] cached = new PlugDistrictDevice[AncientChapterLayout.FloorCount * AncientCampaignPlugLayout.SitesPerFloor];
        PlugDistrictDevice active;
        int floor = -1, site = -1, linked = -1, powered = -1;
        uint revision;
        string primaryHint, secondaryHint;

        internal AncientCampaignPlugWorks(AncientGame game) { this.game = game; }
        internal int Count => active ? active.SocketCount : 0;

        internal void Step(int sector, float dt, System.Func<Vector2,bool> bladeSupplies = null)
        {
            bool exploring = game.HasSession && !game.MainMenu && !game.won && game.hero &&
                game.Story && !game.Story.LiftBusy && !(game.City && game.City.ArenaActive) &&
                !(game.Production && game.Production.Active) && !(game.Finale && game.Finale.Active);
            int nearby = exploring ? AncientCampaignPlugLayout.NearbySite(sector, game.hero.transform.position.x) : -1;
            if (nearby != site || (nearby >= 0 && sector != floor)) Enter(sector, nearby);
            if (active && !game.Paused)
            {
                int wanted=linked;
                if (wanted < 0 && bladeSupplies != null)
                    for (int i=0;i<active.SocketCount;i++)
                        if (active.SocketAvailable(i) && bladeSupplies(active.SocketPoint(i))) { wanted=i; break; }
                SetPower(wanted);
                active.Step(dt, game.hero.transform.position);
            }
        }

        void Enter(int sector, int nearby)
        {
            if (active)
            {
                Reset();
                active.gameObject.SetActive(false);
                active = null;
            }
            site = nearby;
            floor = nearby < 0 ? -1 : sector;
            if (nearby < 0) return;
            var origin = new Vector2(AncientCampaignPlugLayout.OriginX(floor, site), 0);
            int key = floor * AncientCampaignPlugLayout.SitesPerFloor + site;
            if (!cached[key]) cached[key] = PlugDistrictDevice.Spawn(game.transform, floor, origin, advanced: site == 1);
            active = cached[key];
            active.gameObject.SetActive(true);
            active.ResetDevice();
            primaryHint = active.Label + " · " + Instruction(active.Mechanism, false);
            secondaryHint = active.SocketCount == 2 ? active.Label + " · " + Instruction(active.Mechanism, true) : null;
            revision++;
        }

        internal bool TryPort(int index, out AncientBluePlugPorts.Port port)
        {
            port = default;
            if (!active || (uint)index >= active.SocketCount || !active.SocketAvailable(index)) return false;
            port = new AncientBluePlugPorts.Port(AncientBluePlugPorts.Source.DistrictDevice, index, revision, active,
                active.SocketPoint(index), active.TowPoint(index), Vector2.down, BluePlugSkill.Tow | BluePlugSkill.Swing);
            return true;
        }

        internal bool Refresh(AncientBluePlugPorts.Port old, out AncientBluePlugPorts.Port port)
        {
            port = default;
            return active && old.owner == active && old.generation == revision && TryPort(old.slot, out port);
        }

        internal string Hint(AncientBluePlugPorts.Port port)
        {
            if (!Refresh(port, out _)) return null;
            return port.slot == 0 ? primaryHint : secondaryHint;
        }

        static string Instruction(int mechanism, bool secondary)
        {
            switch (mechanism)
            {
                case 0: return "꽂으면 발판이 올라간다";
                case 1: return "전원을 꽂고 위 통로로";
                case 2: return secondary ? "승인 단자에 꽂으면 다리 전개" : "A 연결, G로 회수 후 B 연결";
                case 3: return "펼쳐지는 계단으로 올라가기";
                default: return secondary ? "상단 소켓 · C 견인 또는 V 스윙" : "전원 연결, G로 회수 후 상단 소켓 연결";
            }
        }

        internal void Dock(AncientBluePlugPorts.Port port)
        {
            if (!Refresh(port, out _)) return;
            linked = port.slot;
            SetPower(linked);
        }

        internal void Release()
        {
            if (linked >= 0) SetPower(-1);
            linked = -1;
        }

        void SetPower(int next)
        {
            if (powered==next || !active) return;
            if (powered>=0) active.Release(powered);
            powered=next;
            if (powered>=0) active.Dock(powered);
        }

        internal void Reset()
        {
            Release();
            SetPower(-1);
            if (active) active.ResetDevice();
            revision++;
        }
    }
}
