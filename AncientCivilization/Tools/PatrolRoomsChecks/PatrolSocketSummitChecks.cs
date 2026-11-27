using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        static void CheckSummitSocketRooms()
        {
            CheckNeonRelayAirConnection();
            CheckNeonRelayAirtime();
            CheckSummitSwingLandings();
            CheckSummitBlackout();
            CheckSummitSolidShutters();
            CheckTowerRouteBonus();

            var shaft = OpenSocketRoom(43);
            shaft.UseAction("보급실 예비 전지");
            int first = shaft.Charge;
            var revisited = OpenSocketRoom(43, shaft);
            revisited.UseAction("보급실 예비 전지");
            Check(first == 3 && revisited.Charge == first, "낙하 갱도의 선택 보급품은 돌아왔을 때 중복 지급하지 않는다");
        }

        static void CheckNeonRelayAirConnection()
        {
            var r = OpenSocketRoom(42);
            var light = r.ObjectNamed("중계 완주등").GetComponent<SpriteRenderer>();
            var start = r.ObjectNamed("도약 벽 콘센트").GetComponent<BluePlugSocket>();
            // 손으로만 벽을 잡고 두 번째 단자로 갈 때는 회수 순서를 거치지 않았다.
            r.TouchRest("도약 벽 콘센트"); r.Tick(); r.DropRest();
            r.Point(12, 5, 9, 4);
            r.bluePlug.ThrowSequence++;
            r.Seat("중계 수신 콘센트"); r.Tick();
            r.Arrive("중계 수신 콘센트"); r.Tick();
            Check(light.color.r != SummitSun.r, "출발 단자에 꽂지 않고 건너면 릴레이 기록을 주지 않는다");

            r.Seat("도약 벽 콘센트");
            r.TouchRest("도약 벽 콘센트");
            r.Tick();
            r.ReleaseSeat();
            r.bluePlug.LastManualRecall = start; r.bluePlug.ManualRecallSequence++; r.bluePlug.WallJumpAtManualRecall = r.bluePlug.WallJumpSequence;
            r.Tick();
            r.DropRest(); r.Point(12, 5, 9, 4);
            r.bluePlug.ThrowSequence++;
            r.Seat("중계 수신 콘센트"); r.Tick(); r.Arrive("중계 수신 콘센트"); r.Tick();
            Check(light.color.r != SummitSun.r, "도약 전에 소켓에서 회수하면 공중 회수 기록이 아니다");

            r.Seat("도약 벽 콘센트"); r.TouchRest("도약 벽 콘센트"); r.Tick();
            r.DropRest(); r.bluePlug.LastWallJump = start; r.bluePlug.WallJumpSequence++;
            r.Point(12, 5, 9, 4); r.ReleaseSeat();
            r.bluePlug.LastManualRecall=start; r.bluePlug.ManualRecallSequence++; r.bluePlug.WallJumpAtManualRecall = r.bluePlug.WallJumpSequence; r.Tick();
            r.Grounded = true; r.Point(13, .92f); r.Tick();
            r.Grounded = false; r.bluePlug.ThrowSequence++;
            r.Seat("중계 수신 콘센트"); r.Tick(); r.Arrive("중계 수신 콘센트"); r.Tick();
            Check(light.color.r != SummitSun.r, "하부 착지 뒤의 단순 연결은 공중 릴레이 완주가 아니다");

            r.Seat("도약 벽 콘센트"); r.TouchRest("도약 벽 콘센트"); r.Tick();
            r.DropRest(); r.ReleaseSeat();
            r.bluePlug.LastManualRecall = start; r.bluePlug.ManualRecallSequence++; r.bluePlug.WallJumpAtManualRecall = r.bluePlug.WallJumpSequence; r.Tick();
            r.bluePlug.ThrowSequence++; r.Seat("중계 수신 콘센트"); r.Tick();
            r.Arrive("중계 수신 콘센트"); r.Tick();
            Check(light.color.r != SummitSun.r, "S로 벽을 놓고 낙하한 경우에는 도약 완주등이 켜지지 않는다");

            r.ReleaseSeat();
            r.Seat("도약 벽 콘센트"); r.TouchRest("도약 벽 콘센트"); r.Tick();
            r.ReleaseSeat();
            r.bluePlug.LastManualRecall = start; r.bluePlug.ManualRecallSequence++;
            r.bluePlug.WallJumpAtManualRecall = r.bluePlug.WallJumpSequence;
            r.DropRest(); r.bluePlug.LastWallJump = start; r.bluePlug.WallJumpSequence++;
            r.Point(12, 5, 9, 4); r.Tick();
            r.bluePlug.ThrowSequence++; r.Seat("중계 수신 콘센트"); r.Tick();
            r.Arrive("중계 수신 콘센트"); r.Tick();
            Check(light.color.r != SummitSun.r, "같은 물리 틱 사이에도 G를 먼저 누른 뒤 도약하면 기록하지 않는다");

            r.ReleaseSeat();
            r.Seat("도약 벽 콘센트");
            r.TouchRest("도약 벽 콘센트");
            r.Tick();
            r.DropRest();
            r.bluePlug.LastWallJump = start; r.bluePlug.WallJumpSequence++;
            r.ReleaseSeat();
            r.bluePlug.LastManualRecall = start; r.bluePlug.ManualRecallSequence++; r.bluePlug.WallJumpAtManualRecall = r.bluePlug.WallJumpSequence;
            r.Point(13, 6, 9, 2);
            r.Tick();
            r.bluePlug.ThrowSequence++;
            r.Seat("중계 수신 콘센트");
            r.Tick();
            Check(light.color.r == SummitBeacon.r, "공중에서 걸어 둔 두 번째 회로의 대기등");
            r.ReleaseSeat();
            r.Arrive("중계 수신 콘센트");
            r.Grounded = true;
            r.Tick();
            Check(light.color.r == SummitSun.r, "같은 틱의 견인 도착과 접지에서 완료 신호를 보존한다");
        }

        static void CheckNeonRelayAirtime()
        {
            var r = OpenSocketRoom(42);
            var start = r.ObjectNamed("도약 벽 콘센트").GetComponent<BluePlugSocket>();
            var target = r.ObjectNamed("중계 수신 콘센트").GetComponent<BluePlugSocket>();
            var p = start.Point + new Vector2(.23f, 0);
            var move = SocketMovementTuning();
            var plug = SocketMovementTuning("DefaultBluePlug.asset");
            float vy = move["wallJumpY"];
            const float dt = .02f;
            // 점프 뒤 2틱, 회수 최대 2틱, 재투척 대기 0.12초, 마우스 2틱,
            // 느긋하게 두 바퀴를 돌리고 놓는 경우까지 공중 연계가 가능한지 검사한다.
            int recallTicks = Mathf.CeilToInt(1.6f / (plug["retractSpeed"] * dt));
            int cooldownTicks = Mathf.CeilToInt(plug["recastDelay"] / dt);
            int windTicks = Mathf.CeilToInt(2f / (plug["windTurnsPerSecond"] * dt));
            int releaseTick = 2 + recallTicks + cooldownTicks + 2 + windTicks;
            int activateTick = -1;
            for (int tick = 1; tick <= 65; tick++)
            {
                var previous = p;
                vy -= move["gravity"] * (vy < 0 ? move["fallMultiplier"] : 1) * dt;
                p += new Vector2(tick * dt <= move["wallInputLock"] ? move["wallJumpX"] : move["runSpeed"], vy) * dt;
                // 실제 플레이어의 정점 체공 단축 보정은 사용하지 않는다.
                Check(r.ClearAt(p, .36f, .89f, true), "릴레이 재투척·재조준 대기 동안 캡슐이 솔리드에 닿지 않는다");
                foreach (var item in r.objects)
                {
                    var collider = item.Object.GetComponent<BoxCollider2D>();
                    if (!collider || !collider.usedByEffector) continue;
                    var c = (Vector2)collider.transform.position;
                    float top = c.y + collider.size.y * .5f;
                    bool overlaps = Mathf.Abs(p.x - c.x) < collider.size.x * .5f + .36f;
                    Check(!(overlaps && previous.y - .89f >= top && p.y - .89f <= top),
                        "수신 전 점프가 단방향 발판 위에 먼저 떨어지지 않는다");
                }
                if (tick == releaseTick)
                {
                    Vector2 hand = p + new Vector2(0, plug["handHeight"]);
                    float distance = Vector2.Distance(hand, target.Point);
                    Check(distance < plug["range"], "최소 충전 투척 사거리");
                    for (int sample=0;sample<=24;sample++)
                        Check(r.ClearAt(Vector2.Lerp(hand, target.Point, sample / 24f), .1f, .1f, true),
                            "플러그 비행 유효선");
                    // 궤도 바깥쪽에서 놓는 거리 오차 1타일, 도킹 후 새 C 입력 1틱을 더한다.
                    activateTick = tick + (int)System.Math.Ceiling((distance + 1) / (plug["throwSpeed"] * dt)) + 1;
                }
                if (tick != activateTick) continue;
                Vector2 to = target.Point + new Vector2(-.255f, 0);
                for (int sample=0;sample<=30;sample++)
                    Check(r.ClearAt(Vector2.Lerp(p, to, sample / 30f), .36f, .89f, true),
                        "새 C 입력 직후 완전한 캡슐이 통과할 수 있는 견인 경로");
                Check(to.y - .9f > 6.35f, "견인 도착점에서 수신 갑판에 착지할 수 있다");
                return;
            }
            Check(false, "네온 릴레이 공중 연결의 제한 시간");
        }

        static void CheckSummitSwingLandings()
        {
            var move = SocketMovementTuning();
            var plug = SocketMovementTuning("DefaultBluePlug.asset");
            const float dt = .02f, hw = .36f, hh = .9f;

            bool HitsDeck(PatrolRoom room, Vector2 p, Vector2 next, BoxCollider2D wanted, out bool reached)
            {
                reached = false;
                if (next.y >= p.y) return false;
                foreach (var item in room.objects)
                {
                    var c = item.Object.GetComponent<BoxCollider2D>();
                    if (!c || !c.usedByEffector || !item.Object.activeSelf) continue;
                    var center = (Vector2)c.transform.position;
                    float level = center.y + c.size.y * .5f + hh;
                    if (p.y < level || next.y > level) continue;
                    float x = Mathf.Lerp(p.x, next.x, (level - p.y) / (next.y - p.y));
                    if (Mathf.Abs(x - center.x) >= c.size.x * .5f + hw) continue;
                    reached = c == wanted && Mathf.Abs(x - center.x) < c.size.x * .5f - hw;
                    return true;
                }
                return false;
            }

            bool Lands(PatrolRoom room, Vector2 p, Vector2 v, BoxCollider2D deck)
            {
                for (int tick=0; tick<110; tick++)
                {
                    float run = move["runSpeed"];
                    float error = Mathf.Clamp01(Mathf.Abs(run-v.x)/run);
                    float gain = .35f + .65f * error * error * (3 - 2 * error);
                    if (tick*dt >= .18f || v.x <= run) v.x = Mathf.MoveTowards(v.x, run, move["airAcceleration"] * gain * dt);
                    v.y = Mathf.Max(-move["maxFallSpeed"], v.y - move["gravity"] * (v.y<0?move["fallMultiplier"]:1) * dt);
                    var next = p + v * dt;
                    if (!room.ClearAt(next, hw, hh, true)) return false;
                    if (HitsDeck(room, p, next, deck, out bool reached)) return reached;
                    p=next;
                }
                return false;
            }

            // 실제 단자 위치와 각도 적분을 사용해, V를 뗄 수 있는 연속된 여유 프레임 수를 구한다.
            int ReleaseWindow(int id, string socketName, Vector2 p, float vx, string destination, out int first)
            {
                var r=OpenSocketRoom(id);
                var pivot=r.ObjectNamed(socketName).GetComponent<BluePlugSocket>().Point;
                var deck=r.ObjectNamed(destination).GetComponent<BoxCollider2D>();
                Vector2 radial=p-pivot;
                float angle=Mathf.Atan2(radial.y, radial.x), radius=radial.magnitude;
                float speed=vx * -Mathf.Sin(angle);
                int streak=0, longest=0; first=-1;
                for(int i=0;i<95;i++)
                {
                    Vector2 tangent=new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
                    speed=BluePlugRules.SwingSpeed(speed,tangent.y,tangent.x,plug["swingGravity"],plug["swingPump"],plug["swingSpeedLimit"],dt);
                    angle+=BluePlugRules.SwingAngleStep(speed,radius,dt);
                    Vector2 next=pivot+new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))*radius;
                    if(!r.ClearAt(next,hw,hh,true) || HitsDeck(r,p,next,null,out _)) break;
                    for(int j=1;j<=14;j++)
                        if(!r.ClearAt(Vector2.Lerp(next,pivot,j/14f), .007f,.007f,true)) return longest;
                    p=next;
                    var release=new Vector2(-Mathf.Sin(angle),Mathf.Cos(angle))*speed;
                    if(release.y>0 && release.x>0 && Lands(r,p,release,deck))
                    {
                        if(first<0) first=i;
                        streak++;
                        longest=Mathf.Max(longest,streak);
                    }
                    else streak=0;
                }
                return longest;
            }

            Check(ReleaseWindow(41,"두 지붕 사이 하부 스윙 단자",new Vector2(14.3f,6.77f),8,"마지막 옥상",out _)>=3,
                "41 · 긴 스윙에서 중간 지붕을 건너뛰고 마지막 옥상으로 착지");
            var billboardStart=new Vector2(24.8f,3.8f);
            int low=ReleaseWindow(48,"광고판 옆면 스윙 단자",billboardStart,9,"광고판 뒷길 발판",out int early);
            int high=ReleaseWindow(48,"광고판 옆면 스윙 단자",billboardStart,9,"상부 송신탑 난간",out int late);
            Check(low>=3 && high>=3 && late>=early+3,
                "48 · 같은 소켓에서 이른 이탈은 뒷길, 늦은 이탈은 상부 송신탑");
            Check(ReleaseWindow(50,"종주 스윙 단자",new Vector2(26.5f,9.4f),9,"종주 옥상 착지대",out _)>=3,
                "50 · 견인 후 지붕 가장자리와 천장에 막히지 않는 마지막 스윙");
        }

        static void CheckSummitBlackout()
        {
            var r = OpenSocketRoom(49);
            var cart = r.ObjectNamed("비상 전동 발판");
            var rest = r.ObjectNamed("무전원 벽 고정점");
            r.Tick(80, .05f);
            float x = cart.transform.localPosition.x;
            r.TouchRest("무전원 벽 고정점");
            r.Tick(32, .05f);
            Check(x == cart.transform.localPosition.x && r.Resting(rest), "정전 중 발판만 멈추고 벽 고정은 유지한다");
            r.Tick(25, .05f);
            Check(Mathf.Abs(x - cart.transform.localPosition.x) > .005f, "비상 전력 복귀 뒤 발판의 직전 운동 위상에서 재개한다");
        }

        static void CheckSummitSolidShutters()
        {
            var aerial = OpenSocketRoom(44);
            var near = aerial.ObjectNamed("근거리 안테나 차폐판");
            var far = aerial.ObjectNamed("장거리 안테나 차폐판");
            aerial.Tick(130, .02f);
            Check(near.GetComponent<BoxCollider2D>() && far.GetComponent<BoxCollider2D>() &&
                near.transform.localPosition.y > 10.7f && far.transform.localPosition.y < 10.8f,
                "안테나가 한쪽 창을 열고 반대쪽 실제 투척 경로에 차폐판을 내린다");

            var r = OpenSocketRoom(47);
            var fence = r.ObjectNamed("열림 난간 차단선");
            r.Tick(138);
            Check(!fence.activeSelf, "예고가 끝난 난간 회로가 열린다");
            r.Point(18.7f, 7.9f);
            r.Tick(75);
            Check(!fence.activeSelf, "난간 안에 머무는 동안 닫히지 않는다");
            r.Point(23, 8);
            r.Tick();
            Check(fence.activeSelf, "벗어난 뒤 난간이 닫힌다");
        }

        static void CheckTowerRouteBonus()
        {
            var r = OpenSocketRoom(50);
            var stair = r.ObjectNamed("전망대 전개 계단");
            r.Point(40, 8.6f);
            r.Tick();
            Check(!stair.activeSelf, "무정차 연계 없이 옥상에 서면 보너스 계단은 접혀 있다");
            r.TouchRest("종주 출발 고정점");
            r.Point(9.3f, 5.9f);
            r.Tick();
            r.TouchRest("종주 정비차 측면");
            r.Point(13, 6.5f);
            r.Tick();
            r.DropRest();
            r.Arrive("종주 송전탑 견인 단자");
            r.Point(25, 9.4f);
            r.Tick();
            r.Seat("종주 스윙 단자");
            r.Tick();
            r.Point(40, 8.6f); r.Grounded = true;
            r.Tick();
            Check(!stair.activeSelf, "스윙 단자에 꽂기만 하면 종주가 완료되지 않는다");
            r.Grounded = false;
            r.bluePlug.Phase = BluePlugPhase.Swinging;
            r.Tick();
            r.ReleaseSeat();
            r.bluePlug.Phase = BluePlugPhase.Ready;
            r.Grounded = true;
            r.Tick();
            Check(stair.activeSelf && !r.ObjectNamed("전망대 입구 덮개").activeSelf,
                "이동 차체를 경유해 견인과 실제 스윙 후 착지하면 최고 전망대로 연결된다");
        }
    }
}
