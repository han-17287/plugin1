using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.PlayerCore
{
    // 본편과 순찰에서 같은 실제 장착 상태를 읽는다. 새 게임 진행 상태를 저장하지 않는다.
    public sealed class FilamentLoadoutGauge
    {
        static readonly Color Ink = new Color(.039f, .052f, .073f, .94f);
        static readonly Color Socket = new Color(.36f, .80f, 1f);
        static readonly Color Filament = new Color(1f, .75f, .31f);
        static readonly Color Dim = new Color(.43f, .47f, .53f);
        int previousState = -1, previousTime = -1, previousCost = -1;
        bool previousReady;
        string heading = "";

        public void Draw(Rect slot, SummonedBlade blade, int charge, string plugHint, GUIStyle label)
        {
            if (!blade || !blade.Data) return;
            var state = blade.State;
            int seconds = Mathf.CeilToInt(blade.Remaining);
            int cost = blade.Data.summonCost;
            bool ready = charge >= cost;
            if ((int)state != previousState || seconds != previousTime || cost != previousCost || ready != previousReady)
            {
                previousState = (int)state; previousTime = seconds; previousCost = cost; previousReady = ready;
                switch (state)
                {
                    case SummonedBladeState.Summoning: heading = "낙하 예고   " + seconds + "초"; break;
                    case SummonedBladeState.Falling: heading = "블레이드 낙하 중"; break;
                    case SummonedBladeState.Stuck: heading = "블레이드 설치   " + seconds + "초"; break;
                    case SummonedBladeState.Equipped: heading = "전구 블레이드 장착"; break;
                    case SummonedBladeState.Cooldown: heading = "전구 재생   " + seconds + "초"; break;
                    default: heading = "Q  블레이드 소환   ·   입자 " + cost + (ready ? "" : " 필요"); break;
                }
            }
            bool warm = state != SummonedBladeState.None && state != SummonedBladeState.Cooldown;
            Color accent = warm ? Filament : state == SummonedBladeState.Cooldown ? Dim : Socket;
            AncientHUD.Panel(slot, Ink);
            AncientHUD.Panel(new Rect(slot.x,slot.y,2,slot.height),accent);
            float x = slot.x + 9, y = slot.y + 10;
            // 두 핀의 모양과 문구로 색각과 관계없이 소켓을 알아볼 수 있게 한다.
            AncientHUD.Panel(new Rect(x+2,y,2,6),accent);
            AncientHUD.Panel(new Rect(x+8,y,2,6),accent);
            AncientHUD.Panel(new Rect(x,y+5,12,5),accent);
            AncientHUD.Panel(new Rect(x+5,y+10,2,4),accent);
            GUI.Label(new Rect(x+18,slot.y+3,slot.width-32,24), heading, label);
            string detail = state == SummonedBladeState.Equipped ? "좌클릭 베기 · F 충전 레이저 · E 분리 · R 과부하"
                : state == SummonedBladeState.Falling || state == SummonedBladeState.Stuck ? "좌클릭 회전 후 손잡이에 던져 장착"
                : state == SummonedBladeState.Summoning ? "바닥 예고를 따라 손잡이 조준"
                : plugHint;
            GUI.Label(new Rect(x,slot.y+29,slot.width-17,slot.height-30), detail, label);
        }
    }
}
