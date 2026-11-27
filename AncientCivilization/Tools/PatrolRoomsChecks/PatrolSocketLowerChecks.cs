using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        // 지정된 소켓, 출발대, 도착 발판을 읽어 실제 진자 규칙과 캡슐 크기로 양쪽 동선을 추적한다.
        void CheckLaundrySocketLanding(BluePlugSocket port)
        {
            const float dt=1f/60, halfWidth=.36f, halfHeight=.9f;
            Vector2 start=new Vector2(17.25f,5.14f);
            Vector2 target=port.TowPoint, heading=(target-start).normalized;
            for(float at=0;at<(target-start).magnitude;at+=.08f)
                Check(ClearAt(start+heading*at,halfWidth,halfHeight,true),"11 · 견인 중 측면 벽 통과 금지");
            void LandOn(string label,Vector2 pos,Vector2 velocity,int axis)
            {
                var deck=ObjectNamed(label).GetComponent<BoxCollider2D>();
                float y=deck.transform.position.y+.5f*deck.size.y+halfHeight;
                for(int i=0;i<100;i++)
                {
                    float error=Mathf.Clamp01(Mathf.Abs(axis*9-velocity.x)/9);
                    float rate=axis==0?.45f+.55f*error:.35f+.65f*error*error*(3-2*error);
                    velocity.x=Mathf.MoveTowards(velocity.x,axis*9,32*rate*dt);
                    velocity.y=Mathf.Max(-24,velocity.y-(velocity.y<0?52.8f:32)*dt);
                    Vector2 next=pos+velocity*dt;
                    if(velocity.y<0 && pos.y>=y && next.y<=y)
                    {
                        float x=Mathf.Lerp(pos.x,next.x,(y-pos.y)/(next.y-pos.y));
                        Check(Mathf.Abs(x-deck.transform.position.x)<deck.size.x*.5f-halfWidth,"11 · 캡슐 전체 착지: "+label);
                        return;
                    }
                    Check(ClearAt(next,halfWidth,halfHeight,true),"11 · 이탈 궤적에 솔리드 간섭 없음: "+label);
                    pos=next;
                }
                throw new InvalidOperationException("착지 실패: "+label);
            }
            LandOn("견인 정비 옥상",target,heading*(72f*.22f),0);
            Vector2 p=port.Point, r=start-p, atSwing=start;
            float a=Mathf.Atan2(r.y,r.x), speed=Vector2.Dot(new Vector2(8,-1),new Vector2(-Mathf.Sin(a),Mathf.Cos(a)));
            for(int step=0;step<120 && atSwing.x<28.7f;step++)
            {
                Vector2 tangent=new Vector2(-Mathf.Sin(a),Mathf.Cos(a));
                speed=BluePlugRules.SwingSpeed(speed,tangent.y,tangent.x,32,15,22,dt);
                a+=BluePlugRules.SwingAngleStep(speed,r.magnitude,dt);
                atSwing=p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r.magnitude;
                Check(ClearAt(atSwing,halfWidth,halfHeight,true),"11 · 수선집 아래 호의 솔리드 여유");
            }
            Check(atSwing.x>=28.7f,"11 · 수선집 앞에서 V 이탈");
            LandOn("스윙 수선집 창턱",atSwing,new Vector2(-Mathf.Sin(a),Mathf.Cos(a))*speed,-1);
        }

        static Dictionary<string,float> SocketMovementTuning(string name="DefaultMovement.asset")
        {
            var movement=new Dictionary<string,float>();
            foreach(var line in File.ReadLines(Path.Combine(AppContext.BaseDirectory,name)))
            {
                var field=line.Trim().Split(':');
                if(field.Length==2 && float.TryParse(field[1],NumberStyles.Float,CultureInfo.InvariantCulture,out float n))
                    movement[field[0]]=n;
            }
            return movement;
        }

        static void CheckPressWallJump()
        {
            var movement=SocketMovementTuning();
            // 녹색이 켜진 직후, 중간, 다음 예고 직전. 미해금 상태에서 정식 벽점프 수치를 사용한다.
            const float dt=.02f, hw=.36f, hh=.90f;
            foreach(var hold in new[]{(ticks:143,height:-.25f),(ticks:183,height:0f),(ticks:226,height:.25f)})
            {
                var r=OpenSocketRoom(1);
                var socket=r.ObjectNamed("프레스 사각 고정점").GetComponent<BluePlugSocket>();
                var face=socket.WallFace;
                float edge=face.transform.position.x+face.offset.x+face.size.x*.5f;
                Vector2 pos=new Vector2(edge+hw+movement["skin"],socket.Point.y+hold.height);
                r.Point(pos.x,pos.y); r.TouchRest("프레스 사각 고정점");r.Tick(hold.ticks);
                Check((r.Hero-pos).sqrMagnitude<.00001f,"1 · 닫힘과 재개방을 거친 뒤에도 같은 자리에서 버팀");
                var goal=r.ObjectNamed("프레스 맞은편 반출대").GetComponent<BoxCollider2D>();
                r.DropRest();
                var v=new Vector2(socket.WallOutwardSign*movement["wallJumpX"],movement["wallJumpY"]);
                float apex=0, age=0; bool landed=false;
                for(int frame=0;frame<90 && !landed;frame++)
                {
                    age+=dt;
                    if(age>=movement["wallInputLock"])
                    {
                        float error=Mathf.Clamp01(Mathf.Abs(movement["runSpeed"]-v.x)/movement["runSpeed"]);
                        float rate=.35f+.65f*error*error*(3-2*error);
                        v.x=Mathf.MoveTowards(v.x,movement["runSpeed"],movement["airAcceleration"]*rate*dt);
                    }
                    float gravity=v.y<0?movement["fallMultiplier"]:1;
                    if(Mathf.Abs(v.y)<movement["apexSpeed"] && apex<movement["apexDuration"])
                    { apex+=dt; gravity=movement["apexGravityMultiplier"]; }
                    v.y=Mathf.Max(-movement["maxFallSpeed"],v.y-movement["gravity"]*gravity*dt);
                    var next=pos+v*dt;
                    r.Point(next.x,next.y,v.x,v.y);r.Tick();
                    Check((r.Hero-next).sqrMagnitude<.00001f && r.ClearAt(next,hw,hh,true),
                        "1 · 초록 전조 뒤 캡슐이 움직이는 헤드와 벽을 피한다");
                    if(v.y<0)
                    {
                        foreach(var entry in r.objects)
                        {
                            var surface=entry.Object.GetComponent<BoxCollider2D>();
                            if(!surface || !surface.usedByEffector || !surface.enabled || !entry.Object.activeSelf)continue;
                            var center=(Vector2)surface.transform.position+surface.offset;
                            float top=center.y+surface.size.y*.5f;
                            if(pos.y-hh<top || next.y-hh>top)continue;
                            float u=(pos.y-hh-top)/(pos.y-next.y), x=Mathf.Lerp(pos.x,next.x,u);
                            if(Mathf.Abs(x-center.x)>surface.size.x*.5f+hw)continue;
                            Check(surface==goal && Mathf.Abs(x-center.x)<=surface.size.x*.5f-hw,
                                "1 · 하강 중 처음 만난 실제 일방향 디딤면에 캡슐 전체 착지");
                            landed=true;break;
                        }
                    }
                    pos=next;
                }
                Check(landed,"1 · 기술 없이 압축기 대기점에서 맞은편 발판 도착");
            }
        }

        void CheckLowerReturnFloor(string label=null)
        {
            bool supported=false;
            foreach(var entry in objects)
            {
                if(entry.Label!=(label??"하부 점검로 바닥"))continue;
                var shape=entry.Object.GetComponent<BoxCollider2D>();
                if(!shape || !shape.enabled || !shape.gameObject.activeSelf)continue;
                Vector2 center=(Vector2)shape.transform.position+shape.offset;
                float gap=Hero.y-.90f-center.y-shape.size.y*.5f;
                if(Mathf.Abs(Hero.x-center.x)<=shape.size.x*.5f-.36f && gap>=-.001f && gap<.18f)
                    supported=true;
            }
            Check(supported && ClearAt(Hero,.36f,.90f,false),"복귀 지점에 하단 점검로 또는 지정 발판이 있고 몸이 끼지 않는다");
        }

        static void CheckLowerSocketRooms()
        {
            CheckPressWallJump();
            var r=OpenSocketRoom(1);
            r.Point(19.85f,5.15f); r.TouchRest("프레스 사각 고정점"); r.Tick(67);
            Check(r.Hero.x==19.85f && r.ObjectNamed("왕복 프레스 헤드").transform.position.y<6,"1 · 사각지대에서 프레스가 닫힐 때까지 대기");
            r.DropRest(); r.Point(23.4f,5.36f); r.Tick();
            Check(r.Hero.y<1.5f,"1 · 닫힌 프레스는 하단 안전로로 복귀");
            r.CheckLowerReturnFloor();
            r.Tick(110);
            Check(r.ObjectNamed("왕복 프레스 헤드").transform.position.y>10,"1 · 열린 박자에 맞은편으로 벽점프할 공백");

            r=OpenSocketRoom(2); r.Point(13,1.95f);r.Tick(8);
            Check(r.Velocity.x < -4,"2 · 아래 역이송"); r.Point(26,3.95f);r.Tick(8);
            Check(r.Velocity.x>5,"2 · 상단 순이송");

            r=OpenSocketRoom(3);
            var plug=r.ObjectNamed("철판 뒤 입고 콘센트").GetComponent<BluePlugSocket>();
            var hand=new Vector2(13,4.75f);
            Check(!r.ClearThrow(hand,plug.Point),"3 · 철판이 닫히면 실제 투척이 차단된다");
            r.Tick(125);
            Check(r.ClearThrow(hand,plug.Point),"3 · 판이 지나가면 같은 각도에서 투척할 수 있다");

            r=OpenSocketRoom(4);
            float start=r.ObjectNamed("승강 집게 측면").transform.position.y; r.Tick(160);
            Check(r.ObjectNamed("승강 집게 측면").transform.position.y>start+4.8f,"4 · 집게와 소켓이 위 하차점까지 함께 이동");

            r.UseAction("분류대 하역 출구"); r.CheckLowerReturnFloor("하역 검수교"); Check(r.Hero.y<4 && r.Hero.x>36,"4 · 중단 하역장으로 하차");
            r.UseAction("상층 선적 출구"); r.CheckLowerReturnFloor("상층 선적교 출구"); Check(r.Hero.y>7 && r.Hero.x>38,"4 · 상단 선적교로 하차");

            r=OpenSocketRoom(5); r.Point(20,5); r.TouchRest("용광로 피풍 고정점"); r.Tick(20);
            Check(r.Velocity.y==0,"5 · 벽 정지점은 역풍을 막는다"); r.DropRest();r.Tick(12);
            Check(r.Velocity.y>8 && r.Velocity.x<0,"5 · 밖에서는 역풍과 상승력이 함께 작동");

            r=OpenSocketRoom(6); r.Seat("선별기 자석 전원"); r.Tick(85);
            Check(r.ObjectNamed("자석에 끌리는 철상자 3").transform.position.y>4.15f,"6 · 도킹한 채 상자 계단 유지");
            r.ReleaseSeat();r.Tick(85);
            Check(r.ObjectNamed("자석에 끌리는 철상자 3").transform.position.y<.5f,"6 · 회수하면 계단 해체");

            r=OpenSocketRoom(7); r.Point(22,4.45f);r.Tick(20);
            Check(r.Hero.y<1.5f,"7 · 노출된 용접 불꽃에서 점검로 복귀");
            r.CheckLowerReturnFloor();
            bool shielded=false;
            for(int i=0;i<340;i++)
            {
                r.Tick();float x=r.ObjectNamed("주행 용접 차광판").transform.position.x;
                float t=Mathf.Repeat(r.Elapsed+.35f,2.35f);
                if(Math.Abs(x-22)<.8f && t>.65f && t<1.50f)
                {
                    r.Point(22,4.45f);r.Tick();
                    shielded=r.Hero.y>4 && !r.ObjectNamed("용접 스파크").activeSelf;break;
                }
            }
            Check(shielded,"7 · 차광판이 아크와 겹치면 점검교 안전");

            r=OpenSocketRoom(8);r.Point(20,3.22f);
            r.Tick(40);Check(r.Velocity.y==0,"8 · 분출 전 일반 점프 높이");r.Tick(72);
            Check(r.Velocity.y>16,"8 · 분출 순간 상승");

            r=OpenSocketRoom(9);var drum=r.ObjectNamed("드럼 테두리 콘센트");
            r.Tick(116);Check(drum.transform.position.y>8.80f,"9 · 드럼 상단 이탈");
            r.UseAction("숨은 부품 선반 확인"); r.UseAction("숨은 부품 선반 확인");
            Check(r.Charge==1,"9 · 숨은 선반 보상은 한 번만");

            r=OpenSocketRoom(10); r.Tick(28);
            var scrap=r.ObjectNamed("굴러 떨어지는 폐품 1").transform.position;
            r.Point(scrap.x,scrap.y);r.Tick();
            Check(r.Hero.y<1.5f,"10 · 폐품에 스치면 계단이 있는 하단으로");

            r.CheckLowerReturnFloor();

            r=OpenSocketRoom(11);
            plug=r.ObjectNamed("빨래골목 공용 벽 소켓").GetComponent<BluePlugSocket>();
            Check(r.BluePlugSockets.Length==1 && plug.Supported==(BluePlugSkill.Tow|BluePlugSkill.Swing),"11 · 두 경로에 실제 공용 소켓 하나");
            Check(r.ClearThrow(new Vector2(16.47f,5.48f),plug.Point) && r.ClearAt(plug.TowPoint,.395f,.935f,false),
                "11 · 출발대에서 투척 가능하며 옥상 도착 시 몸통 여유");
            Check(r.ObjectNamed("견인 정비 옥상").transform.position.y>r.ObjectNamed("스윙 수선집 창턱").transform.position.y+5f,
                "11 · 아래 처마 창과 옥상 간 높이 차이");

            r.CheckLaundrySocketLanding(plug);
            r.UseAction("수선집 안쪽에서 골목으로");r.CheckLowerReturnFloor();

            r=OpenSocketRoom(12);r.Tick(15); Check(!r.ObjectNamed("펼친 천 차양").activeSelf,"12 · 접힌 천은 충돌 없음");
            r.Tick(50);r.Point(20.2f,3.25f,3,-1);r.Tick();Check(r.Velocity.y>17,"12 · 펴진 차양에 낙하한 뒤 반동");

            r=OpenSocketRoom(13);
            Check(!r.ObjectNamed("간판 뒤 숨은 발판").activeSelf,"13 · 정면에서는 뒤 통로가 안 보임");
            r.Seat("간판 뒷면 확인 소켓");r.Tick();
            Check(!r.ObjectNamed("간판 뒤 숨은 발판").activeSelf,"13 · 원격 투척만으로 통로 미발견");
            r.TouchRest("간판 뒷면 확인 소켓");r.Tick();
            Check(r.ObjectNamed("간판 뒤 숨은 발판").activeSelf,"13 · 옆에 붙어 실제 뒤편 발판 발견");

            r.UseAction("간판 뒤편 통로");r.CheckLowerReturnFloor("간판 뒷길 출구");

            r=OpenSocketRoom(14);r.Seat("물탱크 배수 전원");r.Tick(85);
            Check(!r.ObjectNamed("배수 통로 수밀문").activeSelf && r.ObjectNamed("물탱크 부표").transform.position.y<1,"14 · 배수 완료 시 아래 길 열림");
            r.ReleaseSeat();r.Point(27.7f,1.1f);r.Tick(85);
            Check(!r.ObjectNamed("배수 통로 수밀문").activeSelf && r.ObjectNamed("물탱크 부표").transform.position.y>3.2f,"14 · 재급수 중 문 아래 플레이어 보호");
            r.Point(35,1.1f);r.Tick();Check(r.ObjectNamed("배수 통로 수밀문").activeSelf,"14 · 통과 후 수밀문 복원");

            r=OpenSocketRoom(15);r.Tick();
            Check(!r.ObjectNamed("이동 가판 사이 뒷문").activeSelf,"15 · 정류장에서는 가게 뒷문이 가려짐");
            bool visible=false;for(int i=0;i<270;i++){r.Tick();if(r.ObjectNamed("이동 가판 사이 뒷문").activeSelf){visible=true;break;}}
            Check(visible,"15 · 정류장 사이에서 하차 창구 개방");r.UseAction("카트 아래 가게 뒷문");
            r.CheckLowerReturnFloor("가게 동쪽 창");
            Check(r.Hero.x>36,"15 · 상점 뒷문으로 지름길 이동");

            r=OpenSocketRoom(16);r.Tick(95);
            Check(r.ObjectNamed("미닫이 창문 판").transform.position.y>9,"16 · 실제 창문 개방");
            r.Point(23.85f,6.24f);r.Tick(105);
            Check(r.ObjectNamed("미닫이 창문 판").transform.position.y>8,"16 · 닫히는 창문에 몸 끼임 방지");

            r=OpenSocketRoom(17);r.Point(20.2f,3.3f);r.Tick(8);Check(r.Velocity.y==0,"17 · 평소엔 시장길");
            r.Seat("환기팬 역전 전원");r.Tick(12);Check(r.Velocity.y>12,"17 · 연결 시 상단길 상승류");

            r=OpenSocketRoom(18);
            for(int i=0;i<70;i++)
            {
                float y=r.ObjectNamed("배달 도르래 바구니").transform.position.y+.26f+.9f;
                r.Point(20.6f,y);r.Grounded=true;r.Tick();
            }
            Check(r.ObjectNamed("도르래 반대 짐칸").transform.position.y>3.85f,"18 · 바구니를 누르면 반대편 짐칸 상승");
            r.Point(2,1);r.Tick(30);Check(r.ObjectNamed("도르래 반대 짐칸").transform.position.y>3.85f,"18 · 하차한 뒤에도 환승할 시간");

            r=OpenSocketRoom(19);Check(r.ObjectNamed("아케이드 지상 셔터").activeSelf,"19 · 처음에는 지상길 닫힘");
            r.UseAction("반대편 아케이드 걸쇠");r=OpenSocketRoom(19,r);
            Check(!r.ObjectNamed("아케이드 지상 셔터").activeSelf,"19 · 방 재진입 때도 지상 지름길 유지");

            r=OpenSocketRoom(20);r.Point(22.5f,4.65f);r.Tick(65);
            Check(r.Hero.y<1.4f && r.ObjectNamed("빗물받이 분출").activeSelf,"20 · 빗물에 휩쓸려 하단 안착");
            r.CheckLowerReturnFloor();
            r.UseAction("배수관 속 순찰 표식 회수");r.UseAction("배수관 속 순찰 표식 회수");
            Check(r.Charge==1,"20 · 배수관 숨은 표식은 한 번만 지급");
        }
    }
}
