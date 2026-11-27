using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        void BuildSocketSummit(int id)
        {
            switch (id)
            {
                case 41: BuildTwinSocketRoofs(); break;
                case 42: BuildNeonRelay(); break;
                case 43: BuildDropShaft(); break;
                case 44: BuildRelayAntenna(); break;
                case 45: BuildMaintenanceOvertake(); break;
                case 46: BuildOpposingWindows(); break;
                case 47: BuildOpeningRail(); break;
                case 48: BuildBillboardPassage(); break;
                case 49: BuildBlackoutCrossing(); break;
                case 50: BuildTowerTraverse(); break;
            }
        }

        // 아래 정비 통로에서 도약만으로 올라올 수 있게 첫 두 단의 높이 차는 2타일 이내로 둔다.
        void SummitSocketEntry()
        {
            SummitRoof("서쪽 비상 계단", 4, 1.75f, 4.5f);
            SummitRoof("서쪽 계단참", 7.2f, 3.65f, 3.7f);
        }

        SpriteRenderer SummitPilot(string label, float x, float y)
        {
            return Box(label, x, y, .64f, .20f, SummitIron, false, 5).GetComponent<SpriteRenderer>();
        }

        void BuildTwinSocketRoofs()
        {
            SummitSocketEntry();
            SummitRoof("첫 옥상", 11, 5.7f, 5.6f);
            SummitRoof("가운데 짧은 옥상", 22.4f, 7.5f, 3.9f);
            SummitRoof("마지막 옥상", 33.5f, 6.3f, 10);
            SummitRoof("동쪽 소방 계단", 40.7f, 3, 4.2f);
            WallOutlet("첫 옥상 벽 콘센트", 11.8f, 7.05f, 1);
            WallOutlet("중간 옥상 견인 단자", 23.8f, 10.65f, -1);
            WallOutlet("끝 옥상 견인 단자", 34, 10.55f, -1);
            // 아래쪽 팔 끝에는 가벼운 매달림식 소켓을 달아 긴 스윙의 양쪽 반원을 비운다.
            Hook("두 지붕 사이 하부 스윙 단자", 20.6f, 9.8f);
            SummitGirder("하부 현수팔", 20.6f, 11.3f, 2.6f, 90);
            Sign("위 단자는 짧게 두 번 · 아래 단자는 긴 스윙", 6.6f, 5.65f);
        }

        void BuildNeonRelay()
        {
            SummitSocketEntry();
            SummitRoof("네온 릴레이 수신 갑판", 20, 6.35f, 9f);
            SummitRoof("송신부 동쪽 옥상", 30.4f, 6.1f, 8.2f);
            SummitRoof("하부 정비로 디딤판", 23.3f, 1.9f, 6.2f);
            SummitRoof("출구 계단", 38, 3.7f, 4.9f);
            var launch = WallOutlet("도약 벽 콘센트", 8, 5.6f, 1);
            var receiver = WallOutlet("중계 수신 콘센트", 16.9f, 7.65f, -1);
            var left = SummitPilot("중계 송신등", 8, 6.85f);
            var right = SummitPilot("중계 완주등", 16.9f, 8.9f);
            // G 회수·실제 투척 입력의 일련번호는 아주 짧은 되감기가 한 물리 틱 안에 끝나도 보존된다.
            // 벽을 맨손으로 잡거나 첫 단자에서 먼저 뽑고 나서 도약한 경우에는 기록하지 않는다.
            int leg = 0;
            uint recall = bluePlug.ManualRecallSequence, thrown = bluePlug.ThrowSequence;
            uint jumped = bluePlug.WallJumpSequence;
            bool lit = false;
            Step(dt =>
            {
                if (Resting(launch))
                {
                    leg = PluggedInto(launch) ? 1 : 0;
                    jumped = bluePlug.WallJumpSequence;
                    recall = bluePlug.ManualRecallSequence;
                    thrown = bluePlug.ThrowSequence;
                }
                else if (leg == 1 && !Grounded && bluePlug.ManualRecallSequence != recall)
                {
                    bool jumpedFromLaunch = bluePlug.WallJumpSequence != jumped &&
                        bluePlug.WallJumpAtManualRecall == bluePlug.WallJumpSequence &&
                        bluePlug.LastWallJump && bluePlug.LastWallJump.gameObject == launch;
                    leg = jumpedFromLaunch && bluePlug.LastManualRecall &&
                        bluePlug.LastManualRecall.gameObject == launch ? 2 : 0;
                }
                if (leg == 2 && !Grounded && bluePlug.ThrowSequence != thrown) leg = 3;
                if (leg == 3 && !Grounded && PluggedInto(receiver)) leg = 4;
                // 도착으로 줄이 풀리는 틱에 수신 갑판 접지도 동시에 들어올 수 있다.
                if (leg == 4 && Arrived(receiver))
                {
                    if (!lit) Say("네온 중계 성공. 공중에서 두 번째 회로를 닫았다.");
                    lit = true;
                    leg = 0;
                }
                if (!Resting(launch) && Grounded && !Zipping) leg = 0;
                left.color = leg > 0 ? SummitBeacon : SummitIron;
                right.color = lit ? SummitSun : leg == 4 ? SummitBeacon : SummitIron;
            });
            Sign("벽 도약 · G 회수 · 공중 투척 · C 견인", 4.3f, 4.9f);
        }

        void BuildDropShaft()
        {
            // 계단은 왼쪽 갱도, 세 착지선은 오른쪽 갱도에 두어 아래 포구도 선택할 수 있다.
            SummitRoof("승강 계단 첫 단", 4.8f, 2.05f, 4.4f);
            SummitRoof("승강 계단 둘째 단", 9.25f, 4.35f, 4.7f);
            SummitRoof("승강 계단 셋째 단", 13.5f, 6.6f, 5);
            SummitRoof("승강 계단 넷째 단", 17.6f, 8.85f, 4.4f);
            SummitRoof("승강 계단 다섯째 단", 14, 10.95f, 4.9f);
            SummitRoof("낙하 지붕", 19.7f, 12.8f, 7);
            SummitRoof("상단 보급실", 33.7f, 9.6f, 7.3f);
            SummitRoof("중간 송전로", 33.6f, 5.8f, 11.4f);
            SummitRoof("하단 복귀로", 34.2f, 2.2f, 16.8f);
            WallOutlet("상단 보급실 정박 단자", 30.9f, 11.1f, -1);
            WallOutlet("중간 직행 정박 단자", 28.8f, 7.35f, -1);
            WallOutlet("하단 복귀 정박 단자", 26.8f, 3.65f, -1);
            Use("보급실 예비 전지", 34.5f, 10.5f, () =>
            {
                if (Flag("patrol.socket.shaft.supply")) { Say("전지를 수령한 보급함이다."); return; }
                SetFlag("patrol.socket.shaft.supply");
                AddCharge(3);
                Say("보급실 예비 전지에서 입자를 확보했다.");
            });
            Sign("상단 보급실 · 중간 직행로 · 하단 복귀로", 16.2f, 14);
        }

        void BuildRelayAntenna()
        {
            SummitSocketEntry();
            SummitRoof("송신실 서쪽 지붕", 10.6f, 5.5f, 5.3f);
            SummitRoof("근거리 안테나 정비선", 20.2f, 6.1f, 5.1f);
            SummitRoof("장거리 안테나 정비선", 28.5f, 8.8f, 6.2f);
            SummitRoof("안테나 하부 계단", 35, 4.2f, 6);
            WallOutlet("근거리 노출 단자", 19.2f, 7.85f, -1);
            WallOutlet("장거리 노출 단자", 27.6f, 10.55f, -1);
            var nearCover = Box("근거리 안테나 차폐판", 18.5f, 7.85f, .52f, 2.45f, SummitIron, true);
            var farCover = Box("장거리 안테나 차폐판", 26.9f, 13.65f, .52f, 2.45f, SummitIron, true);
            var mast = SummitGirder("중계 안테나 방위침", 23, 13, 4.8f, 0);
            var a = SummitPilot("근거리 수신등", 19.2f, 9.2f);
            var b = SummitPilot("장거리 수신등", 27.6f, 11.9f);
            Step(dt =>
            {
                float theta = Elapsed * 1.22f;
                float lift = (1 - Mathf.Cos(theta)) * .5f;
                // 가려진 단자도 실제 벽 뒤에 있다. 안전 여유 안에 경위가 있으면 엄폐판을 기다리게 한다.
                MoveSummitCover(nearCover, 18.5f, 7.85f + 3.1f * lift, 1.225f, dt);
                MoveSummitCover(farCover, 26.9f, 10.55f + 3.1f * (1 - lift), 1.225f, dt);
                mast.transform.localRotation = Quaternion.Euler(0, 0, Elapsed * 70);
                a.color = nearCover.transform.localPosition.y > 9.45f ? SummitBeacon : SummitIron;
                b.color = farCover.transform.localPosition.y > 12.15f ? SummitBeacon : SummitIron;
            });
            Sign("안테나가 돌면 밝은 쪽 수신창이 열린다", 6, 5);
        }

        void MoveSummitCover(GameObject cover, float x, float targetY, float halfHeight, float dt)
        {
            float y = cover.transform.localPosition.y;
            float next = Mathf.MoveTowards(y, targetY, 6 * dt);
            if (Mathf.Abs(Hero.x - x) < .74f && Mathf.Abs(Hero.y - next) < halfHeight + .98f &&
                Mathf.Abs(Hero.y - next) < Mathf.Abs(Hero.y - y)) return;
            Place(cover, x, next);
        }

        void BuildMaintenanceOvertake()
        {
            SummitSocketEntry();
            SummitRoof("정비차 승강장", 10, 4.75f, 5.4f);
            SummitRoof("굽은 선로 추월 선반", 24, 7.5f, 5.4f);
            SummitRoof("고가 선로 도착대", 39, 6.8f, 7);
            var car = Deck("고가 정비차 후방 발판", 9.3f, 5, 3.4f);
            var nose = Deck("고가 정비차 앞 발판", 13.3f, 5.8f, 3.25f);
            var side = WallOutlet("정비차 측면 승차 단자", 9.7f, 4.3f, -1);
            var turn = WallOutlet("굽은 선로 벽 콘센트", 23.2f, 9.2f, -1);
            var lamp = SummitPilot("추월 접속등", 24.2f, 8.75f);
            bool rode = false, cut = false, succeeded = false;
            Step(dt =>
            {
                float t = (1 - Mathf.Cos(Elapsed * .53f)) * .5f;
                float x = 9.3f + 24 * t, y = 5 + Mathf.Sin(t * Mathf.PI) * 1.4f;
                Place(car, x, y - .175f);
                Place(nose, x + 4, y + .8f - .175f);
                Place(side, x + .4f, y - .7f);
                if (Resting(side) || Grounded && Near(x, y + .9f, 1.45f)) rode = true;
                if (rode && (Resting(turn) || Arrived(turn))) cut = true;
                if (!succeeded && cut && Grounded && Mathf.Abs(Hero.x - (x + 4)) < 1.7f &&
                    Mathf.Abs(Hero.y - (y + 1.7f)) < .45f)
                {
                    succeeded = true;
                    Say("정비차가 굽이를 도는 사이 앞 발판에 올라탔다.");
                }
                if (Grounded && Hero.y < 1.7f) rode = cut = false;
                lamp.color = succeeded ? SummitSun : cut ? SummitBeacon : SummitSilver;
            });
            Sign("차체 뒤에서 출발 · 굽이 안쪽으로 추월", 7, 6.1f);
        }

        void BuildOpposingWindows()
        {
            SummitSocketEntry();
            SummitRoof("서편 외창", 12.1f, 5.6f, 5.4f);
            SummitRoof("동편 외창", 24, 5.95f, 5.9f);
            SummitRoof("동편 내부 통로", 34, 4.5f, 9);
            WallOutlet("서편 바람 대기 단자", 13.4f, 7.1f, 1);
            WallOutlet("동편 바람 대기 단자", 23.2f, 7.5f, -1);
            var sock = SummitGirder("건물 사이 바람자루", 18.5f, 10, 2.6f, 0);
            var west = SummitPilot("서풍등", 13.3f, 8.6f);
            var east = SummitPilot("동풍등", 23.2f, 9);
            Step(dt =>
            {
                float wind = Mathf.Clamp(Mathf.Sin(Elapsed * 1.12f) * 2.6f, -1, 1);
                sock.transform.localRotation = Quaternion.Euler(0, 0, wind * 33);
                west.color = wind > .45f ? SummitSun : SummitIron;
                east.color = wind < -.45f ? SummitSun : SummitIron;
                if (!Grounded && !RestingAnywhere && !Zipping &&
                    Hero.x > 13.6f && Hero.x < 23 && Hero.y > 3.6f)
                    Push(Mathf.Clamp(Velocity.x + wind * 23 * dt, -20, 20), Velocity.y);
            });
            Sign("바람등이 켜진 창틀에서 도약 · 중앙에서 대시", 9, 6.65f);
        }

        void BuildOpeningRail()
        {
            SummitSocketEntry();
            SummitRoof("열림 난간 출발대", 10.6f, 5.7f, 5.5f);
            SummitRoof("점검 난간 동쪽", 27.7f, 7.45f, 7.6f);
            SummitRoof("스윙 하차대", 24.8f, 3.25f, 5.5f);
            SummitRoof("수신 옥상 출구", 37.2f, 5, 7.4f);
            WallOutlet("난간 출발 단자", 12.15f, 7, 1);
            WallOutlet("난간 너머 장거리 단자", 26.7f, 9.5f, -1);
            var shutter = Box("열림 난간 차단선", 18.7f, 7.9f, .74f, 4.3f, SummitSilver, true);
            var pilot = SummitPilot("난간 개방등", 18.75f, 10.45f);
            Hook("난간 하부 우회 단자", 18.8f, 5.25f);
            Step(dt =>
            {
                float beat = Mathf.Repeat(Elapsed, 4.3f);
                bool open = beat >= 2.65f && beat < 3.95f;
                // 열린 틈에 몸이 들어왔으면 먼저 빠져나가게 한다.
                bool occupied = Mathf.Abs(Hero.x - 18.7f) < .81f && Mathf.Abs(Hero.y - 7.9f) < 3.12f;
                if (shutter.activeSelf == (open || occupied)) shutter.SetActive(!(open || occupied));
                pilot.color = open ? SummitBeacon : beat >= 2.12f && beat < 2.65f ? SummitSun : SummitIron;
            });
            Sign("금색 예고 뒤 난간 개방 · 아래 소켓은 상시 가동", 6, 4.85f);
        }

        void BuildBillboardPassage()
        {
            SummitSocketEntry();
            SummitRoof("광고판 정면 보도", 13.3f, 5, 8.8f);
            Box("광고판 막힌 정면", 18.5f, 8.5f, 1.2f, 7, SummitIron, true);
            Box("광고판 정면 등", 18.5f, 10.3f, .45f, 2.2f, SummitSun, false, 4);
            SummitRoof("광고판 하단 정비턱", 18.7f, 2.2f, 5.7f);
            WallOutlet("광고판 뒷면 벽 단자", 20.4f, 4.1f, 1);
            SummitGirder("광고판 뒷면 현수팔", 21.7f, 11.5f, 6.5f, 15);
            WallOutlet("광고판 옆면 스윙 단자", 24.6f, 10.5f, 1);
            SummitRoof("현수팔 아래 발진대", 24.4f, 2.85f, 3.5f);
            SummitRoof("광고판 뒷길 발판", 31.8f, 4.35f, 6.4f);
            SummitRoof("상부 송신탑 난간", 33.5f, 6.5f, 5.8f);
            SummitRoof("출구 점검단", 40, 3.1f, 5.4f);
            Sign("광고판 아래를 지나 오른쪽 면으로", 8, 6.3f);
            Sign("올라가는 호에서 늦게 놓으면 송신탑", 21, 5.6f);
        }

        void BuildBlackoutCrossing()
        {
            SummitSocketEntry();
            SummitRoof("정전 서쪽 비상 발판", 11, 5.45f, 5.9f);
            SummitRoof("정전 중앙 대기 선반", 23.8f, 6.05f, 3.8f);
            SummitRoof("정전 동쪽 비상 발판", 36.5f, 6.7f, 7.4f);
            var safe = WallOutlet("무전원 벽 고정점", 23.4f, 7.75f, -1);
            WallOutlet("동쪽 비상 벽 고정점", 35.5f, 8.55f, -1);
            var car = Deck("비상 전동 발판", 13, 5.7f, 4.2f);
            var panel = SummitPilot("비상 전원 계기", 23.4f, 9.3f);
            var strip = Box("비상 선로 조명", 24, 11.65f, 20, .22f, SummitSun, false, -2)
                .GetComponent<SpriteRenderer>();
            float motor = 0;
            Step(dt =>
            {
                float beat = Mathf.Repeat(Elapsed, 7);
                bool on = beat < 3.9f || beat >= 6.25f;
                if (on)
                {
                    motor += dt;
                    float phase = (1 - Mathf.Cos(motor * .77f)) * .5f;
                    Place(car, 13 + 20 * phase, 5.7f + phase * .8f - .175f);
                }
                panel.color = on ? SummitSun : Resting(safe) || beat > 5.85f ? SummitBeacon : SummitSilver;
                strip.color = on ? SummitSun : SummitIron;
            });
            Sign("정전 중에는 발판만 정지 · 벽의 단자는 유지", 7.8f, 6.45f);
        }

        void BuildTowerTraverse()
        {
            SummitSocketEntry();
            SummitRoof("타워 벽 출발참", 8.4f, 4.35f, 3.4f);
            var start = WallOutlet("종주 출발 고정점", 9, 5.9f, 1);
            var car = Deck("종주 정비차", 13, 5.45f, 3.4f);
            var ride = WallOutlet("종주 정비차 측면", 12.4f, 6.6f, -1);
            var tow = Hook("종주 송전탑 견인 단자", 25.3f, 9.45f);
            SummitGirder("송전탑 견인팔", 25.3f, 10.95f, 2.5f, 90);
            var pivot = Hook("종주 스윙 단자", 30.8f, 12.05f);
            SummitRoof("송전탑 정차 참", 24.1f, 6.9f, 3.8f);
            SummitRoof("중간 이탈 보수 갑판", 31, 3.2f, 6);
            SummitRoof("종주 옥상 착지대", 40.1f, 7.7f, 6.3f);
            SummitRoof("최상층 전망대", 44.1f, 12.5f, 4.2f);
            var hatch = Box("전망대 입구 덮개", 42.7f, 12.2f, .45f, 3.2f, SummitIron, true);
            var stairs = Deck("전망대 전개 계단", 41.1f, 9.85f, 2.8f);
            var upperStair = Deck("전망대 상부 전개 계단", 39.6f, 11.45f, 2.6f);
            stairs.SetActive(false);
            upperStair.SetActive(false);
            var lamp = SummitPilot("종주 기록등", 42.2f, 12.95f);
            bool clear = false;
            int leg = 0;
            float still = 0;
            Step(dt =>
            {
                float wave = (1 - Mathf.Cos(Elapsed * .94f)) * .5f;
                float x = 13 + 6.4f * wave, y = 5.45f + .6f * wave;
                Place(car, x, y - .175f);
                Place(ride, x - .6f, y + 1.15f);
                if (clear) return;
                bool onCar = Resting(ride) || Grounded && Mathf.Abs(Hero.x - x) < 1.8f &&
                    Mathf.Abs(Hero.y - y - .9f) < .45f;
                if (Resting(start) || Arrived(start))
                {
                    leg = 1;
                    still = 0;
                }
                if (leg == 1 && onCar) leg = 2;
                if (leg == 2 && Arrived(tow)) leg = 3;
                if (leg == 3 && SocketSwinging && PluggedInto(pivot)) leg = 4;
                if (leg == 4 && !SocketSwinging && Grounded &&
                    Hero.x >= 36.9f && Hero.x <= 43.25f && Mathf.Abs(Hero.y - 8.6f) < .5f)
                {
                    clear = true;
                    hatch.SetActive(false);
                    stairs.SetActive(true);
                    upperStair.SetActive(true);
                    lamp.color = SummitSun;
                    Say("외벽 무정차 종주. 옥상에서 가장 높은 전망대로 올라갈 수 있다.");
                    return;
                }
                // 차의 실제 운동과 공중에서 한 회전 감아 던지는 시간을 인정한다.
                // 중간 갑판에서 오래 쉬거나 아래 길로 내려오면 보너스 주행만 해제한다.
                bool paused = !onCar && Hero.x > 10 &&
                    (RestingAnywhere || Grounded && Mathf.Abs(Velocity.x) < 1);
                still = paused ? still + dt : 0;
                if (Hero.y < 2 || still > .9f) leg = 0;
                lamp.color = leg == 0 ? SummitIron : Color.Lerp(SummitSilver, SummitBeacon, leg / 4f);
            });
            Sign("고정점 · 정비차 · C 견인 · V 스윙 · 옥상 착지", 4.4f, 5);
        }
    }
}
