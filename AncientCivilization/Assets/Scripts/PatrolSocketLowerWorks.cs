using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        void BuildSocketLower(int id)
        {
            if (id <= 10) FactoryMachine((id - 1) % 6, "검수로 · 설비 외장", 11, 0, 1.04f);
            else
            {
                LivingWall((id - 11) % 3, 0, 10, 0, 1.22f);
                LivingWall((id - 10) % 3, 1, 34, 0, 1.36f);
            }
            switch (id)
            {
                case 1: SocketPress(); break;
                case 2: SocketBelts(); break;
                case 3: SocketCoveredFreight(); break;
                case 4: SocketLiftClaw(); break;
                case 5: SocketFurnace(); break;
                case 6: SocketMagnet(); break;
                case 7: SocketWelding(); break;
                case 8: SocketAirTank(); break;
                case 9: SocketDrum(); break;
                case 10: SocketRejectShaft(); break;
                case 11: SocketLaundryFork(); break;
                case 12: SocketBounceAwning(); break;
                case 13: SocketSignBack(); break;
                case 14: SocketWaterTank(); break;
                case 15: SocketLaundryCart(); break;
                case 16: SocketSlidingWindow(); break;
                case 17: SocketFanMarket(); break;
                case 18: SocketBasketLift(); break;
                case 19: SocketArcadeShutter(); break;
                case 20: SocketGutter(); break;
            }
        }

        // 위험 지대는 상단 설비에 한정한다. 피격 시 같은 방의 점검로에 다시 세운다.
        void ReturnToInspectionLane(float x, string caption)
        {
            Warp(x, 1.02f);
            Say(caption);
        }

        void SocketPress()
        {
            FactoryStairs("프레스 옆 검수 계단", 6, 1.15f, 3, 1.10f, 2);
            Deck("프레스 원자재 받침", 15.25f, 3.35f, 7.8f);
            WallOutlet("프레스 사각 고정점", 19.3f, 5.1f, 1);
            Deck("프레스 맞은편 반출대", 26.1f, 4.75f, 6.6f);
            var ram = Box("왕복 프레스 헤드", 23.4f, 10.55f, 3.55f, 1.00f, BeltSteel, true, 5);
            var stripe = Box("프레스 예고 띠", 23.4f, 10.18f, 3.35f, .16f, SignalGold, false, 6);
            var rail = Box("프레스 피스톤", 23.4f, 12.3f, .64f, 5.0f, BeltSteel, false, 1);
            var lamp = FactoryLamp("프레스 열린 박자", 20.3f, 7.2f, SignalGreen).GetComponent<SpriteRenderer>();
            Sign("옆면에서 기다렸다가 녹색에 점프", 15.7f, 6.9f);
            Step(dt => {
                float t = Mathf.Repeat(Elapsed, 4.6f);
                float down = t < .58f ? 0 : t < 1.06f ? Mathf.SmoothStep(0, 1, (t-.58f)/.48f)
                    : t < 2.15f ? 1 : 1-Mathf.SmoothStep(0, 1, (t-2.15f)/.64f);
                float y = 10.55f - 5.14f*down;
                if (down > .12f && t < 2.15f && Mathf.Abs(Hero.x-23.4f)<2.05f &&
                    Hero.y > 3.45f && Hero.y + .88f >= y - .50f && Hero.y < y + .66f)
                    ReturnToInspectionLane(18, "프레스 아래쪽 점검로에 착지했다.");
                Place(ram,23.4f,y); Place(stripe,23.4f,y-.37f); Place(rail,23.4f,y+2.65f);
                lamp.color = t > 2.79f ? SignalGreen : SignalGold;
            });
        }

        void SocketBelts()
        {
            FactoryBelt("입구 방향 역이송 벨트", 7, 21, 1.05f, () => -4.4f);
            WallOutlet("역주행 환승 콘센트", 20.3f, 3.55f, 1);
            FactoryBelt("출구 방향 가속 벨트", 23, 34.5f, 3.05f, () => 5.4f);
            Deck("속도 이월 반출대", 39.7f, 4.35f, 4.85f);
            Sign("아래 벨트는 왼쪽  /  위 벨트는 오른쪽", 12.6f, 3.85f);
        }

        void SocketCoveredFreight()
        {
            FactoryStairs("화물 조준대", 7, 1, 3, 1.20f, 2.1f);
            WallOutlet("철판 뒤 입고 콘센트", 25.8f, 6.15f, -1);
            Deck("입고 콘센트 하차대", 27.6f, 4.80f, 7);
            var panel = Box("입고 차폐 철판", 22.95f, 6.15f, 6.5f, 4.7f, BeltSteel, true, 12);
            var band = Box("철판 진행 띠",22.95f,7.15f,6.38f,.20f,SignalGold,false,13);
            Sign("철판이 지나가는 틈에 플러그를 던진다", 12, 5.8f);
            Step(dt => {
                // 정면에서 막는 동안 실제 지형 캐스트에도 걸린다. 도킹 후 되돌아오는 판도 견인을 막는다.
                float y = 6.15f + 5.6f*Mathf.SmoothStep(0,1,Mathf.PingPong(Elapsed*.40f,1));
                if (y >= panel.transform.localPosition.y || Mathf.Abs(Hero.x-22.95f)>3.64f || Mathf.Abs(Hero.y-y)>3.28f)
                { Place(panel,22.95f,y); Place(band,22.95f,y+1); }
            });
        }

        void SocketLiftClaw()
        {
            var port = WallOutlet("승강 집게 측면", 18, 2.30f, 1);
            var cabin = Box("집게 슬라이더",17.02f,2.35f,1.10f,2.06f,BeltSteel,false,1);
            Box("집게 수직 레일",17.02f,6.2f,.23f,10.6f,BeltSteel,false,-2);
            Deck("집게 분류 선반", 23.6f, 4.60f, 6.0f);
            Deck("집게 상층 출하 선반", 23.6f, 9.00f, 6.0f);
            Deck("분류대 복귀 다리", 30.4f, 3.1f, 4.1f);
            Deck("하역 검수교", 37.75f, 2.15f, 4.60f);
            Deck("상층 선적교 출구", 39.8f, 6.15f, 5.05f);
            Use("분류대 하역 출구", 24.75f, 5.60f, () => {
                Warp(37.75f,3.12f); Say("분류품 통로에서 하역 검수교로 나왔다.");
            },1.2f);
            Use("상층 선적 출구", 24.75f, 10.0f, () => {
                Warp(39.8f,7.11f); Say("상층 선적교를 통해 동쪽 옥상에 올랐다.");
            },1.2f);
            Sign("중간은 분류대  /  위는 출하 선반", 12, 4.2f);
            Step(dt => {
                float y=2.30f+6.6f*Mathf.SmoothStep(0,1,Mathf.PingPong(Elapsed*.24f,1));
                Place(port,18,y); Place(cabin,17.02f,y+.05f);
            });
        }

        void SocketFurnace()
        {
            FactoryStairs("열풍 내화 계단",7,1.1f,3);
            var port = WallOutlet("용광로 피풍 고정점",15.8f,4.8f,1);
            Deck("내화 통로",23.4f,3.65f,6.7f); Deck("용광로 상단 검사창",26.1f,8.7f,5.6f);
            var vents = new GameObject[4];
            for(int i=0;i<vents.Length;i++) vents[i]=Box("용광로 바람결",18.9f+i*1.25f,4.5f,.12f,2.3f,SignalGold,false,-1);
            Sign("벽에서 역풍을 견딘다  /  상승 기류에 뛰면 위쪽 검사창",12,6.25f);
            Step(dt => {
                float t=Mathf.Repeat(Elapsed,4.4f);
                bool strong=t<2.3f;
                for(int i=0;i<vents.Length;i++)
                    Place(vents[i],18.8f+i*1.22f+.48f*Mathf.Sin(Elapsed*6+i),4.35f+(strong?1.15f:.22f)*Mathf.Sin(Elapsed*5+i));
                if(strong && !Zipping && !Resting(port) && Hero.x>16.35f && Hero.x<23.7f && Hero.y>2.3f && Hero.y<9.4f)
                    Push(Mathf.Max(-3.6f,Velocity.x-8.5f*dt),Mathf.MoveTowards(Velocity.y,13.7f,62f*dt));
            });
        }

        void SocketMagnet()
        {
            var port=WallOutlet("선별기 자석 전원",12.3f,2.85f,-1);
            var crates=new GameObject[3];
            float[] from={10,24.8f,35}, to={15.8f,19.0f,22.2f}, levels={1.4f,2.95f,4.5f};
            for(int i=0;i<3;i++) crates[i]=Deck("자석에 끌리는 철상자 "+(i+1),from[i],.55f,2.6f,.49f);
            Deck("선별기 부품 검사대",27.2f,6,5.2f);
            Sign("전원을 남겨 둔 채 상자 계단으로",9.8f,4.25f);
            float assembly=0;
            Step(dt=>{
                assembly=Mathf.MoveTowards(assembly,Powered(port)?1:0,dt*.62f);
                float v=Mathf.SmoothStep(0,1,assembly);
                for(int i=0;i<3;i++) Place(crates[i], Mathf.Lerp(from[i],to[i],v), Mathf.Lerp(.55f,levels[i],v)-.245f);
            });
        }

        void SocketWelding()
        {
            FactoryStairs("용접 접근 계단",5.3f,1.05f,3);
            WallOutlet("용접 차광 대기점",13.1f,4.25f,1);
            WallOutlet("용접 반출 콘센트",30.0f,5.6f,-1);
            Deck("용접 검수교",21,3.4f,15.8f); Deck("반출 측면대",31,4.25f,5.5f);
            var shield=Box("주행 용접 차광판",15.4f,5.25f,3.75f,3.00f,BeltSteel,false,-1);
            var arc=Box("용접 스파크",22,5.4f,.18f,3.45f,SignalGold,false,6);
            var lamp=FactoryLamp("용접 예고등",18.5f,6.6f,SignalGold).GetComponent<SpriteRenderer>();
            Sign("불꽃을 가리는 차광판을 따라간다",10.7f,6.45f);
            Step(dt=>{
                float sx=15.4f+12.6f*Mathf.PingPong(Elapsed*.28f,1);
                float t=Mathf.Repeat(Elapsed+.35f,2.35f);
                bool firing=t>=.55f && t<1.70f;
                Place(shield,sx,5.25f); arc.SetActive(firing && Mathf.Abs(sx-22)>1.74f);
                lamp.color=t<.55f?SignalGold:firing?SignalGold:SignalGreen;
                if(firing && Mathf.Abs(Hero.x-22)<.70f && Hero.y>3.15f && Hero.y<7.25f && Mathf.Abs(sx-Hero.x)>1.74f)
                    ReturnToInspectionLane(20,"차광판 뒤에 붙으면 불꽃이 지나간다.");
            });
        }

        void SocketAirTank()
        {
            var port=WallOutlet("압축기 대기 콘센트",17.1f,3.45f,1);
            Deck("압축공기 분출판",19.7f,2.16f,5.6f); Deck("통풍실 높은 창",26.7f,7.55f,6.5f);
            var jet=Box("압축 공기 분출",19.85f,4.85f,3.2f,4.85f,new Color(.67f,.8f,.82f,.45f),false,-2);
            var lamp=FactoryLamp("분출 압력계",17.15f,5.10f,SignalGold).GetComponent<SpriteRenderer>();
            Sign("압력계가 찰 때 벽에서 오른쪽으로 도약",12,5.15f);
            Step(dt=>{
                float t=Mathf.Repeat(Elapsed,3.05f); bool blowing=t>2.10f && t<2.82f;
                lamp.color=Color.Lerp(BeltSteel,SignalGold,Mathf.Clamp01(t/2.1f)); jet.SetActive(blowing);
                if(blowing && !Resting(port) && !Zipping && Hero.x>17.0f && Hero.x<22 && Hero.y>2.1f && Hero.y<7.15f)
                    Push(Velocity.x,Mathf.Max(Velocity.y,16.5f));
            });
        }

        void SocketDrum()
        {
            const float cx=22, cy=5.1f, radius=3.85f;
            var port=WallOutlet("드럼 테두리 콘센트",cx-radius,cy,-1);
            var spoke=Box("회전 드럼 지름대",cx,cy,radius*1.92f,.36f,BeltSteel,false,-1);
            Box("회전 드럼 축",cx,cy,1.15f,1.15f,SignalGold,false,1);
            Deck("드럼 위 채취 선반",15.85f,7.9f,5.1f);
            Deck("드럼 안쪽 부품 선반",26.8f,3.15f,3.0f);
            Use("숨은 부품 선반 확인",26.8f,4.15f,()=>LowerSocketFind("socket.drum.part","가려져 있던 예비 축핀을 회수했다."));
            Sign("위를 향하면 출하 선반  /  반 바퀴 더 돌면 축핀",12.5f,4);
            Step(dt=>{
                float angle=Mathf.PI-Elapsed*.68f;
                Place(port,cx+radius*Mathf.Cos(angle),cy+radius*Mathf.Sin(angle));
                spoke.transform.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
            });
        }

        void SocketRejectShaft()
        {
            FactoryStairs("폐품 슈트 우회 계단",6.8f,1.1f,6,1.12f,1.63f);
            Deck("슈트 상단 작업대",17,8.05f,4.2f);
            WallOutlet("슈트 왼쪽 상단",19.9f,8.7f,1);
            WallOutlet("슈트 오른쪽 중단",26.8f,6.4f,-1);
            WallOutlet("슈트 왼쪽 하단",20.1f,4.4f,1);
            Deck("슈트 하단 환승 발판",30.2f,3.35f,4.4f);
            var rubble=new GameObject[3];
            for(int i=0;i<3;i++) rubble[i]=Box("굴러 떨어지는 폐품 "+(i+1),23,14,.76f,.68f,SignalGold,false,5);
            Sign("폐품이 지나면 반대편 점검 소켓으로",13,9.45f);
            Step(dt=>{
                for(int i=0;i<3;i++)
                {
                    float descent=Mathf.Repeat(Elapsed*.21f+i/3f,1);
                    float y=13.9f-11.0f*descent, x=23.35f+3.12f*Mathf.Sin((descent*2.1f+i*.21f)*Mathf.PI*2);
                    Place(rubble[i],x,y); rubble[i].transform.localRotation=Quaternion.Euler(0,0,(Elapsed+i)*130f);
                    if(Mathf.Abs(Hero.x-x)<.62f && Mathf.Abs(Hero.y-y)<1.04f)
                        ReturnToInspectionLane(18.8f,"안전망에 떨어졌다. 왼쪽 계단에서 다시 올라갈 수 있다.");
                }
            });
        }

        void LowerSocketFind(string flag, string message)
        {
            if(Flag(flag)) { Say("이미 확인한 곳이다."); return; }
            SetFlag(flag); AddCharge(1); Say(message);
        }
    }
}
