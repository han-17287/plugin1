using System;
using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        static void CheckResidenceSocketRooms()
        {
            CheckBalconyWallJump();
            var rail = OpenSocketRoom(32);
            var cart = rail.ObjectNamed("매달린 화분차");
            var cartSocket = rail.ObjectNamed("화분차 옆 소켓");
            var initial = (Vector2)cartSocket.transform.position;
            rail.Tick(200);
            Check(cartSocket.transform.position.x > initial.x + 10, "화분차의 고정점이 실제로 궤도를 이동");
            Check(Math.Abs(cartSocket.transform.position.x - cart.transform.position.x - 1.85f) < .005,
                "화분차와 벽 고정점의 상대 좌표 유지");
            Check(rail.ObjectNamed("온실의 낮은 가지").GetComponent<BoxCollider2D>() &&
                rail.ObjectNamed("나뭇가지 밑 점검대").GetComponent<BoxCollider2D>().usedByEffector,
                "가지 충돌 뒤에 내려설 수 있는 실제 중간 디딤면");

            var glass = OpenSocketRoom(33);
            glass.Tick(56);
            Check(glass.ObjectNamed("온실 루버 1").activeSelf && !glass.ObjectNamed("온실 루버 2").activeSelf,
                "첫 루버만 먼저 닫혀 디딤판이 됨");
            glass.Tick(65);
            Check(glass.ObjectNamed("온실 루버 2").activeSelf && glass.ObjectNamed("온실 루버 3").activeSelf,
                "유리 날개가 차례로 닫힘");
            glass.Tick(164);
            Check(!glass.ObjectNamed("온실 루버 1").activeSelf && !glass.ObjectNamed("온실 루버 2").activeSelf,
                "주기가 지나면 닫힌 루버의 판정 해제");

            var fountain = OpenSocketRoom(34);
            fountain.Point(16.5f, 1);
            fountain.Tick(22);
            Check(fountain.Velocity.y == 0, "전원 없는 분수는 경위를 들어 올리지 않음");
            fountain.Seat("중정 분수 밸브");
            fountain.Tick(22);
            Check(fountain.Velocity.y >= 8 && fountain.expedition.Player.LastLift >= 8,
                "낮은 분수 분출이 물리 바디와 이동 상태 머신에 같은 틱에 전달됨");
            fountain.Point(23, 1);
            fountain.Tick();
            Check(fountain.Velocity.y == 0, "두 번째 분수는 아직 대기");
            fountain.Tick(85);
            Check(fountain.Velocity.y >= 11, "높은 분수의 지연 분출과 서로 다른 속도");
            fountain.ReleaseSeat();
            fountain.Point(23, 1);
            fountain.Tick();
            Check(fountain.Velocity.y == 0, "분수 회수 후 다음 프레임에 추력 중단");

            var cradles = OpenSocketRoom(35);
            var west = cradles.ObjectNamed("청소대 서쪽");
            var east = cradles.ObjectNamed("청소대 동쪽");
            float initialGap = east.transform.position.y - west.transform.position.y;
            cradles.Tick(138);
            Check(Math.Abs(east.transform.position.y - west.transform.position.y - initialGap) > 2.2,
                "독립 주기의 두 청소대 사이 환승 높이가 달라짐");

            var library = OpenSocketRoom(36);
            const string shelf = "책장 옆면 황동 소켓";
            var door = library.ObjectNamed("독서실 뒤 가림문");
            library.Seat(shelf);
            library.Tick(240);
            Check(door.activeSelf && library.ObjectNamed(shelf).transform.position.x > 26,
                "멀리서 책장을 돌릴 수 있지만 독서실은 그대로 닫힘");
            library.ReleaseSeat();
            library.Tick(240);
            library.TouchRest(shelf);
            library.Seat(shelf);
            library.Tick(240);
            Check(!door.activeSelf, "책장에 붙어 반 바퀴 탄 뒤 독서실 열림");
            library.UseAction("뒤쪽 독서대");
            Check(library.Flag("socket.res.reading"), "독서실 숨은 기록 수집 가능");

            var wing = OpenSocketRoom(37);
            var shutter = wing.ObjectNamed("차음 회랑 셔터");
            wing.TouchRest("차음재 벽 소켓");
            wing.Tick();
            Check(!shutter.activeSelf, "차음면에 붙으면 윗길 개방");
            wing.DropRest();
            wing.Point(22.8f,5);
            wing.Grounded=false;
            wing.Tick();
            Check(!shutter.activeSelf, "공중 이탈로는 소음 센서가 울리지 않음");
            wing.Point(22.8f,.95f);
            wing.Grounded=true;
            wing.Tick();
            Check(shutter.activeSelf, "감지 바닥에 착지하면 윗길 폐쇄");

            var terrace=OpenSocketRoom(38);
            var floor=terrace.ObjectNamed("전원이 꺼진 테라스");
            var panel=terrace.ObjectNamed("전원이 켜진 테라스 벽");
            terrace.Seat("테라스 경첩 소켓");
            terrace.Tick(50);
            Check(panel.activeSelf && !floor.activeSelf, "전원이 켜져 수평 발판이 수직 벽으로 바뀜");
            terrace.ReleaseSeat();
            terrace.Tick(50);
            Check(!panel.activeSelf && floor.activeSelf, "회수 후 벽이 다시 디딜 수 있는 발판이 됨");

            var skylight=OpenSocketRoom(39);
            skylight.Grounded = true;
            skylight.Axis = .5f;
            skylight.Point(21.5f,7.45f);
            skylight.Tick();
            Check(skylight.Velocity.x==0, "닫힌 채광창에는 미끄럼 가속 없음");
            skylight.Seat("채광창 개폐기");
            skylight.Tick(50);
            skylight.Point(21.5f,6.55f-4.15f*(float)Math.Tan(21*Math.PI/180)+.9f);
            skylight.Tick();
            Check(skylight.Velocity.x>8, "열린 각도에 실제로 올라선 플레이어만 가속");

            var sluice=OpenSocketRoom(40);
            var leaf=sluice.ObjectNamed("떠내려가는 넓은 잎");
            Check(sluice.ObjectNamed("정원 수문 날개").activeSelf, "수문 닫힘으로 시작");
            sluice.Tick(120);
            Check(!sluice.ObjectNamed("정원 수문 날개").activeSelf && leaf.activeSelf, "수문 개방 후 상류에서 잎 진입");
            float upper=leaf.transform.position.x;
            sluice.Tick(195);
            Check(leaf.transform.position.x>upper+10, "잎에 물리 발판이 붙어 하류로 이동");
            sluice.Grounded=true;
            sluice.Point(34,3.58f);
            sluice.Tick(180);
            Check(leaf.activeSelf && Math.Abs(leaf.transform.position.x-34)<.01,
                "탑승자를 상류로 순간이동시키지 않고 끝에서 대기");
            sluice.Point(40,.95f);
            sluice.Tick(100);
            Check(leaf.transform.position.x<20, "하차한 뒤 다음 물길에서 잎 공급");
        }

        static void CheckBalconyWallJump()
        {
            var room=OpenSocketRoom(31);
            var anchor=room.ObjectNamed("발코니 끝 고정점").GetComponent<BluePlugSocket>();
            var upper=room.ObjectNamed("정문 발코니").GetComponent<BoxCollider2D>();
            var lower=room.ObjectNamed("처마 밑 정비문 창턱").GetComponent<BoxCollider2D>();
            float upperTop=upper.transform.position.y+upper.size.y/2;
            float lowerTop=lower.transform.position.y+lower.size.y/2;
            Check(anchor.WallOutwardSign==1 && upperTop-lowerTop>3.6f, "같은 동쪽 벽면에서 높이가 다른 두 목적지");
            var movement=SocketMovementTuning();
            var plug=SocketMovementTuning("DefaultBluePlug.asset");
            const float dt=.02f, hw=.36f, hh=.9f;
            var face=anchor.WallFace;
            float contactX=face.transform.position.x+face.offset.x+face.size.x/2+hw+movement["skin"];
            float gravity=movement["gravity"], fall=gravity*movement["fallMultiplier"];
            float run=movement["runSpeed"], steer=movement["airAcceleration"];
            float DriveRight(float vx)
            {
                float error=Mathf.Clamp01(Mathf.Abs(run-vx)/run);
                return Mathf.MoveTowards(vx,run,steer*(.35f+.65f*error*error*(3-2*error))*dt);
            }
            // 버튼을 계속 누른 벽점프. 오름과 정점에서 다른 중력, 실제 손잡이 면과 벽점프 입력 잠금을 사용한다.
            float x=contactX, y=anchor.Point.y-hh, vy=movement["wallJumpY"], wallVx=movement["wallJumpX"];
            float hang=0;
            bool landed=false;
            for (int i=0;i<100;i++)
            {
                float oldY=y;
                float g=vy<0?fall:gravity;
                if(Math.Abs(vy)<movement["apexSpeed"] && hang<movement["apexDuration"])
                {
                    g=gravity*movement["apexGravityMultiplier"]; hang+=dt;
                }
                vy=Mathf.Max(-movement["maxFallSpeed"],vy-g*dt);
                if(i*dt>=movement["wallInputLock"]) wallVx=DriveRight(wallVx);
                y+=vy*dt; x+=wallVx*dt;
                Check(room.ClearAt(new Vector2(x,y+hh),hw,hh,true),"정문 벽점프 도중 솔리드에 닿지 않음");
                if(vy<0 && oldY>=upperTop && y<=upperTop)
                {
                    landed=Math.Abs(x-upper.transform.position.x)<upper.size.x/2-hw;
                    break;
                }
            }
            Check(landed, "콘센트에서 기본 벽점프로 정문 난간에 닿음");

            // 같은 벽에 꽂은 채 아래 입력으로 내려와 줄 최소 길이에 닿는 시점에 V를 새로 누른다.
            Vector2 position=new Vector2(contactX,anchor.Point.y), velocity=Vector2.zero;
            for(int i=0;i<60 && (position-anchor.Point).magnitude<plug["minimumRope"];i++)
            {
                if(i*dt>.25f) velocity.x=DriveRight(velocity.x);
                velocity.y=Mathf.Max(-movement["maxFallSpeed"],velocity.y-(velocity.y<0?fall:gravity)*dt);
                position+=velocity*dt;
                Check(room.ClearAt(position,hw,hh,true),"스윙 줄을 확보할 때까지 캡슐이 자유 낙하");
            }
            Vector2 radial=position-anchor.Point;
            float radius=radial.magnitude, angle=Mathf.Atan2(radial.y,radial.x);
            Check(radius>=plug["minimumRope"] && radius<=plug["maximumRope"],"실제 길이에서 스윙 시작");
            float speed=Vector2.Dot(new Vector2(-radial.y,radial.x)/radius,velocity);
            int inARow=0, best=0;
            for(int frame=0;frame<52;frame++)
            {
                float tangentX=-Mathf.Sin(angle), tangentY=Mathf.Cos(angle);
                speed=BluePlugRules.SwingSpeed(speed,tangentY,tangentX,plug["swingGravity"],plug["swingPump"],plug["swingSpeedLimit"],dt);
                angle+=BluePlugRules.SwingAngleStep(speed,radius,dt);
                float rx=Mathf.Cos(angle), ry=Mathf.Sin(angle);
                float px=anchor.Point.x+rx*radius, py=anchor.Point.y+ry*radius;
                if(!room.ClearAt(new Vector2(px,py),hw,hh,true)) break;
                float vx=-ry*speed, vertical=rx*speed;
                bool landsOnSill=false;
                for(int step=0;step<42;step++)
                {
                    float oldFeet=py-hh;
                    vx=DriveRight(vx);
                    vertical=Mathf.Max(-movement["maxFallSpeed"],vertical-(vertical<0?fall:gravity)*dt);
                    px+=vx*dt; py+=vertical*dt;
                    if(vertical<0 && oldFeet>=lowerTop && py-hh<=lowerTop)
                    {
                        landsOnSill=Math.Abs(px-lower.transform.position.x)<lower.size.x/2-hw;
                        break;
                    }
                    if(!room.ClearAt(new Vector2(px,py),hw,hh,true)) break;
                }
                inARow=landsOnSill?inARow+1:0;
                best=Math.Max(best,inARow);
            }
            Check(best>=6,"실제 진자 규칙으로 같은 벽에서 이탈해 낮은 창턱에 착지할 연속 6프레임 이상");
            room.UseAction("정문 우편함");
            room.UseAction("발코니 밑 정비문");
            room.UseAction("정문 우편함");
            Check(room.Flag("socket.res.balconyFront") && room.Flag("socket.res.balconyService") && room.Charge==2,
                "서로 다른 두 발코니에서 보상을 한 번씩만 받음");
        }
    }
}
