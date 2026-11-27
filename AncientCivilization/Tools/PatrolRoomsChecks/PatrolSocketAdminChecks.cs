using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        static void CheckAdminSocketRooms()
        {
            var room=OpenSocketRoom(21);
            var bridge=room.ObjectNamed("A 인출교");
            var lift=room.ObjectNamed("B 점검 승강기");
            room.Tick();
            Check(!bridge.activeSelf, "21 unpowered gap remains a gap");
            room.Seat("다리 회선 A");
            room.Tick(30);
            Check(bridge.activeSelf && lift.transform.localPosition.y<3.4f,"21 left bus opens the bridge alone");
            room.ReleaseSeat();
            room.Tick();
            Check(!bridge.activeSelf,"21 recalling disconnects the bridge");
            room.Seat("승강 회선 B");
            room.Tick(150);
            Check(!bridge.activeSelf && lift.transform.localPosition.y>8.1f,"21 east bus powers only the lift");
            room.ReleaseSeat();
            room.Tick(150);
            Check(lift.transform.localPosition.y<3.4f,"21 lift returns to the transfer deck");

            room=OpenSocketRoom(22);
            var plate=room.ObjectNamed("첫 잔류 발판");
            room.Seat("첫 축전 단자");
            room.Tick();
            room.ReleaseSeat();
            room.Tick(65);
            Check(plate.activeSelf,"22 capacitor carries a player after disconnect");
            room.Tick(85);
            Check(!plate.activeSelf,"22 capacitor expires, independently of other banks");
            room.Seat("두 번째 축전 단자");
            room.Tick();
            Check(room.ObjectNamed("둘째 잔류 발판").activeSelf && !plate.activeSelf,"22 second bank controls its own deck");

            room=OpenSocketRoom(23);
            var fireDoor=room.ObjectNamed("양면 방화문");
            room.Seat("방화문 뒷면 단자");
            room.Tick(80);
            Check(fireDoor.transform.localPosition.x>17.8f && fireDoor.transform.localPosition.y<3.42f,"23 rear face lays a physical bridge");
            room.ReleaseSeat();
            room.Seat("방화문 앞면 단자");
            room.Tick(80);
            Check(fireDoor.transform.localPosition.x<14.47f && fireDoor.transform.localPosition.y>6.8f,"23 front face restores upright wall");

            room=OpenSocketRoom(24);
            room.Point(21.25f,5.7f);
            room.TouchRest("센서 뒤편 피난 단자");
            room.Tick(155);
            Check(room.Hero.y>5,"24 socket blind spot shelters from inspection");
            room.DropRest();
            room.Tick(305);
            Check(room.Hero.y<1.2f,"24 exposed agent is redirected to safe lower lane");

            room=OpenSocketRoom(25);
            var rack=room.ObjectNamed("회전 서버랙");
            var translatingPort=room.ObjectNamed("랙 이동 단자");
            room.Seat("랙 이동 단자");
            room.Tick(140);
            Check(rack.transform.localPosition.x>19.3f && rack.transform.localPosition.y<3,"25 rack physically turns a quarter turn");
            Check(translatingPort.transform.localPosition.x>18.6f,"25 rider port rotates with the rack");

            room=OpenSocketRoom(26);
            room.Seat("기록 색인 단자");
            room.Tick(20);
            Check(room.ObjectNamed("1번 기록 서랍").activeSelf && !room.ObjectNamed("2번 기록 서랍").activeSelf,"26 first numbered drawer");
            room.Tick(145);
            Check(!room.ObjectNamed("1번 기록 서랍").activeSelf && room.ObjectNamed("2번 기록 서랍").activeSelf,"26 second drawer replaces opening drawer");
            room.ReleaseSeat();
            room.Tick();
            Check(!room.ObjectNamed("2번 기록 서랍").activeSelf,"26 removing current closes drawers");

            room=OpenSocketRoom(27);
            var lowerGlass=room.ObjectNamed("투명 격벽 아래");
            var upperGlass=room.ObjectNamed("투명 격벽 위");
            float lowerLip=lowerGlass.transform.localPosition.y+lowerGlass.GetComponent<BoxCollider2D>().size.y*.5f;
            float upperLip=upperGlass.transform.localPosition.y-upperGlass.GetComponent<BoxCollider2D>().size.y*.5f;
            Vector2 side=room.ObjectNamed("환기구 측면 단자").transform.localPosition;
            Vector2 target=room.ObjectNamed("환기구 너머 단자").transform.localPosition;
            float glassX=lowerGlass.transform.localPosition.x;
            float fromSide=Mathf.Lerp(side.y,target.y,(glassX-side.x)/(target.x-side.x));
            float fromGround=Mathf.Lerp(1.05f,target.y,(glassX-14)/(target.x-14));
            Check(fromGround>2.2f && fromGround<lowerLip-.10f, "27 low shot strikes the glass");
            Check(fromSide>lowerLip+.10f && fromSide<upperLip-.10f,"27 side angle clears both vent lips");
            room.Seat("환기구 너머 단자");
            room.Tick();
            Check(room.ObjectNamed("환기구 정비 발판").activeSelf,"27 opposite port extends vent platform");

            room=OpenSocketRoom(28);
            room.Tick(160);
            Check(room.ObjectNamed("서행 기록 승강기").transform.localPosition.y>7.08f &&
                  room.ObjectNamed("동행 기록 승강기").transform.localPosition.y<2.63f,"28 lift phases are opposite");
            room.Tick(160);
            Check(room.ObjectNamed("서행 기록 승강기").transform.localPosition.y<2.67f &&
                  room.ObjectNamed("동행 기록 승강기").transform.localPosition.y>8.7f,"28 high east stop reaches return gallery");

            room=OpenSocketRoom(29);
            var covered=room.ObjectNamed("압력실 내부 단자").GetComponent<BluePlugSocket>();
            var gate=room.ObjectNamed("압력실 통과문");
            var hood=room.ObjectNamed("압력실 소켓 덮개");
            Check(!covered.Operational,"29 socket covered before first pressure");
            room.Point(14.4f,4.3f);
            room.Tick();
            Check(covered.Operational && !gate.activeSelf && !hood.activeSelf,"29 weight opens the socket and passage");
            room.Seat("압력실 내부 단자");
            room.Point(18.3f,4.4f);
            room.Tick(70);
            Check(covered.Operational && !gate.activeSelf,"29 stepping off preserves seated plug during timed run");
            room.Point(20.6f,5.3f);
            room.Tick(65);
            Check(!gate.activeSelf,"29 timed gate waits for player's body to clear");
            room.Point(23.6f,4.5f);
            room.Tick();
            Check(gate.activeSelf && !hood.activeSelf,"29 door closes with outlet lease intact");
            room.ReleaseSeat();
            room.Tick();
            Check(!covered.Operational && hood.activeSelf,"29 disconnect closes the cover");

            room=OpenSocketRoom(30);
            const string unlocked="wallroute.30.returnLadder";
            room.UseAction("배전실 사다리 빗장 풀기");
            Check(!room.Flag(unlocked),"30 latch needs the entrance-facing perch");
            room.TouchRest("배전실 입구 위 단자");
            room.UseAction("배전실 사다리 빗장 풀기");
            Check(room.Flag(unlocked) && room.ObjectNamed("복귀 사다리 중단").activeSelf,"30 latch deploys traversable return steps");
            room=OpenSocketRoom(30,room);
            Check(room.ObjectNamed("복귀 사다리 하단").activeSelf,"30 returning to room retains ladder");
        }
    }
}
