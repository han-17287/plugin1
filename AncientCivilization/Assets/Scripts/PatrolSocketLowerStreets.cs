using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        void SocketLaundryFork()
        {
            // 정비소 시험에서 검증한 출발대·창턱 간격을 야시장의 독립 평행 골목에도 유지한다.
            LivingSteps("빨래골목 출발 계단",10.2f,1.10f,3,1.95f,1.075f);
            Deck("빨래골목 투척대",15.55f,4.30f,2.20f);
            var port=WallOutlet("빨래골목 공용 벽 소켓",23.5f,9.25f,-1,.80f);
            port.GetComponent<BluePlugSocket>().Configure(new Vector2(-1.03f,1.10f),BluePlugSkill.Tow|BluePlugSkill.Swing,Vector2.right);
            Deck("견인 정비 옥상",26.5f,9.15f,10.2f);
            Deck("지붕 동쪽 귀환로",37.4f,5.20f,9.8f);
            Deck("스윙 수선집 창턱",31.5f,3.50f,5.8f);
            LivingCanvas("처마 밑 수선집",31.5f,6.07f,5);
            LivingWindow("수선집 노란 창문",31.5f,3.50f,true);
            Box("빨래 끈",11.6f,7.65f,10.85f,.08f,Tile,false,-3);
            for(int i=0;i<5;i++) Box("널어 놓은 작업복",7.4f+i*1.95f,7.25f,.79f,.8f,i%2==0?Cloth:LitGlass,false,-2);
            Sign("같은 소켓  ·  C 옥상  /  V 처마 밑 창문",14,6.4f);
            Sign("기술 없이 아래 시장길로",20.1f,1.8f);
            Use("수선집 안쪽에서 골목으로",31.5f,4.4f,()=>{ Warp(34.3f,1.02f); Say("재봉틀 뒤편 계단이 시장길로 이어진다."); });
        }

        void SocketBounceAwning()
        {
            WallOutlet("차양 옆 벽 소켓",17.2f,4.45f,1);
            LivingSteps("골목 짧은 계단",8,1.15f,2,2.5f,1.08f);
            var canopy=Deck("펼친 천 차양",20.2f,2.35f,4.9f,.23f);
            var valance=Box("차양 무늬",20.2f,2.28f,4.75f,.16f,Cloth,false,4);
            Deck("차양 위 다락 창",26,6.45f,5.7f);
            var cord=Box("차양 당김끈",17.6f,3.32f,.09f,1.95f,ReedGold,false,-1);
            float nextBounce=0;
            Sign("천이 펴질 때 위에서 내려앉기",12.4f,5.4f);
            Step(dt=>{
                float t=Mathf.Repeat(Elapsed,4.2f);
                bool occupied=Mathf.Abs(Hero.x-20.2f)<2.7f && Mathf.Abs(Hero.y-3.25f)<.26f;
                bool open=t>1.0f && t<3.15f || canopy.activeSelf && occupied;
                // 펼치는 순간 플레이어 내부에 발판이 생기지 않게 한다.
                if(!canopy.activeSelf && Mathf.Abs(Hero.x-20.2f)<2.68f && Mathf.Abs(Hero.y-2.25f)<.90f) open=false;
                canopy.SetActive(open); valance.SetActive(open); Place(cord,17.6f,open?3.45f:3.85f);
                if(open && occupied && Velocity.y<=.20f && Elapsed>=nextBounce && !RestingAnywhere && !Zipping)
                { Push(Velocity.x,17.7f); nextBounce=Elapsed+.44f; }
            });
        }

        void SocketSignBack()
        {
            var port=WallOutlet("간판 뒷면 확인 소켓",18.4f,4.30f,-1);
            var fascia=Box("간판 헤드라인",22.8f,5.50f,7.15f,2.80f,Cloth,false,7);
            var sill=Deck("간판 뒤 숨은 발판",23.4f,3.96f,3.85f);
            var back=Box("간판 뒤 열린 틈",24.5f,4.88f,1.40f,1.76f,Tile,false,-2);
            sill.SetActive(false); back.SetActive(false);
            bool revealed=false;
            Sign("간판 정면에서는 글자만 보인다",11.6f,3.85f);
            Use("간판 뒤편 통로",24.2f,4.93f,()=>{
                if(!revealed){Say("간판 옆의 작업 소켓에서 뒷면을 살펴볼 수 있다.");return;}
                Warp(36.9f,2.72f); Say("간판 지주 사이의 뒷길이 동쪽 골목으로 이어졌다.");
            },1.08f);
            Deck("간판 뒷길 출구",36.8f,1.76f,3.85f);
            Step(dt=>{
                if(!revealed && Resting(port))
                {
                    revealed=true; fascia.SetActive(false); sill.SetActive(true); back.SetActive(true);
                    Say("간판 뒤로 얇은 발판과 틈이 보인다.");
                }
            });
        }

        void SocketWaterTank()
        {
            var port=WallOutlet("물탱크 배수 전원",12.85f,2.8f,-1);
            var water=Box("골목 저수조 수면",23.2f,3.23f,11.0f,.18f,LitGlass,false,-2);
            var floatDeck=Deck("물탱크 부표",23.2f,3.48f,3.25f);
            var valve=Box("배수 통로 수밀문",27.7f,1.42f,.59f,2.84f,Tile,true,3);
            Deck("물탱크 옆 점검대",16,1.75f,4.4f);
            Deck("물탱크 위층 창",28.4f,5.08f,4.7f);
            LivingWindow("물탱크 위층",28.4f,5.08f,true);
            // 높은 우회 발판을 유지하므로 수밀문이 닫혀도 기본 점프로 통행할 수 있다.
            Deck("수밀문 우회 턱",25.2f,1.60f,2.45f); Deck("수밀문 접근 계단",30.05f,1.35f,2.20f);
            Sign("꽂으면 물을 빼고  /  뽑으면 부표가 뜬다",12.8f,4.3f);
            float level=1;
            Step(dt=>{
                level=Mathf.MoveTowards(level,Powered(port)?0:1,dt*.64f);
                float surface=Mathf.Lerp(.34f,3.23f,level);
                Place(water,23.2f,surface); Place(floatDeck,23.2f,surface+.080f);
                bool closed=level>.14f;
                if(!valve.activeSelf && Mathf.Abs(Hero.x-27.7f)<.77f && Hero.y<3.79f)closed=false;
                valve.SetActive(closed);
            });
        }

        void SocketLaundryCart()
        {
            var port=WallOutlet("빨래 운반 카트 측면",11.75f,3.30f,1);
            var cart=Deck("빨래 운반차",10.4f,3.0f,2.1f,.50f);
            var laundry=Box("운반차 포대",10.43f,3.53f,1.38f,1.0f,Cloth,false,1);
            var door=Box("이동 가판 사이 뒷문",23.25f,2.02f,1.62f,1.96f,ReedGold,false,-2);
            var grille=Box("가게 뒷문 가리개",23.25f,2.02f,1.75f,2.02f,Tile,false,-1);
            float until=-1;
            Deck("가게 하역 문턱",23.25f,1.12f,2.5f); Deck("가게 동쪽 창",37.75f,2.3f,3.75f);
            Sign("정류장 사이에서 카트 아래 노란 문을 찾기",9.7f,5.3f);
            Use("카트 아래 가게 뒷문",23.25f,2.03f,()=>{
                if(Elapsed>until){Say("양쪽 정류장 사이로 가판이 지나야 가게 문이 드러난다.");return;}
                Warp(37.75f,3.24f); Say("수거 가게를 가로질러 동쪽 창으로 나왔다.");
            },1.15f);
            Step(dt=>{
                float x=10.4f+22.8f*Mathf.SmoothStep(0,1,Mathf.PingPong(Elapsed*.16f,1));
                Place(cart,x,2.75f); Place(port,x+1.35f,3.3f); Place(laundry,x,3.53f);
                if(Mathf.Abs(x-23.25f)<1.6f)until=Elapsed+1.15f;
                door.SetActive(Elapsed<=until); grille.SetActive(Elapsed>until);
            });
        }

        void SocketSlidingWindow()
        {
            WallOutlet("미닫이 대기 소켓",18.8f,4.8f,1);
            LivingSteps("창문 앞 계단",8.7f,1.18f,3,2.25f,1.10f);
            Deck("미닫이 창틀 아래",24.1f,2.75f,6.4f);
            Deck("창가 지름길",27.6f,5.20f,6.4f);
            LivingWindow("미닫이 유리창",23.85f,4.83f,false);
            var shutter=Box("미닫이 창문 판",23.85f,5.72f,.49f,2.18f,Tile,true,5);
            var lamp=Box("창문 길 안내등",22.9f,7.35f,.52f,.14f,ReedGold,false,5);
            float gateY=5.72f;
            Sign("창틀 아래는 안전  /  열린 틈으로 옆점프",12.25f,5.8f);
            Step(dt=>{
                float t=Mathf.Repeat(Elapsed,4.2f);
                bool open=t>1.05f && t<3.05f;
                float target=open?9.20f:5.72f;
                float next=Mathf.MoveTowards(gateY,target,dt*6);
                if(next>=gateY || Mathf.Abs(Hero.x-23.85f)>.70f || Hero.y+.9f<next-1.09f || Hero.y-.9f>gateY+1.09f) gateY=next;
                Place(shutter,23.85f,gateY); lamp.SetActive(gateY>=8.92f);
            });
        }

        void SocketFanMarket()
        {
            var port=WallOutlet("환기팬 역전 전원",13,2.72f,-1);
            Deck("환기 덕트 점검턱",19.5f,1.40f,5.2f);
            Deck("시장 위 조리 공방",26.9f,6.38f,6.25f);
            var blade=Box("시장 환기팬 날개",20.04f,1.37f,2.12f,.25f,ReedGold,false,3);
            var stream=Box("환기팬 바람결",20.04f,3.15f,.19f,2.60f,LitGlass,false,-1);
            Sign("평소에는 시장길  /  전원 연결 시 위 공방",11.3f,4.2f);
            float angle=0;
            Step(dt=>{
                bool up=Powered(port);
                angle=Mathf.Repeat(angle+(up?380:-180)*dt,360);
                blade.transform.localRotation=Quaternion.Euler(0,0,angle);
                Place(stream,20.04f,up?4.5f+.6f*Mathf.Sin(Elapsed*5):2.35f);
                if(up && !RestingAnywhere && !Zipping && Hero.x>17.6f && Hero.x<23.35f && Hero.y<7.5f)
                    Push(Velocity.x,Mathf.MoveTowards(Velocity.y,14.8f,dt*80));
            });
        }

        void SocketBasketLift()
        {
            WallOutlet("배달꾼 옆점프 소켓",15.75f,4.1f,1);
            var basket=Deck("배달 도르래 바구니",20.6f,3.15f,3.2f,.52f);
            var weight=Deck("도르래 반대 짐칸",25.4f,1.28f,3.2f,.52f);
            var wheel=Box("배달 도르래 축",23.0f,7.1f,.62f,.62f,ReedGold,false,0);
            Box("도르래 가로대",23,7.05f,5.02f,.17f,Tile,false,-1);
            Deck("배달 물품 선반",29.8f,5.85f,4.15f);
            float load=0,hold=0;
            Sign("바구니를 밟으면 반대 짐칸이 올라온다",12.1f,5.5f);
            Step(dt=>{
                float basketTop=Mathf.Lerp(3.15f,1.50f,load), weightTop=Mathf.Lerp(1.28f,4.15f,load);
                bool riding=Grounded && Mathf.Abs(Hero.x-20.6f)<1.64f && Mathf.Abs(Hero.y-(basketTop+.9f))<.24f;
                bool onOther=Grounded && Mathf.Abs(Hero.x-25.4f)<1.64f && Mathf.Abs(Hero.y-(weightTop+.9f))<.24f;
                if(riding)hold=2.15f; else hold=Mathf.Max(0,hold-dt);
                if(!onOther || riding) load=Mathf.MoveTowards(load,riding||hold>0?1:0,dt*.95f);
                Place(basket,20.6f,Mathf.Lerp(3.15f,1.50f,load)-.26f);
                Place(weight,25.4f,Mathf.Lerp(1.28f,4.15f,load)-.26f);
                wheel.transform.localRotation=Quaternion.Euler(0,0,load*220);
            });
        }

        void SocketArcadeShutter()
        {
            const string key="socket.living.arcadeLatch";
            bool unlocked=Flag(key);
            var shutter=Box("아케이드 지상 셔터",23.6f,1.78f,.86f,3.56f,Tile,true,4);
            shutter.SetActive(!unlocked);
            WallOutlet("셔터 위 정비 소켓",23.12f,4.74f,-1,1.12f);
            LivingSteps("아케이드 셔터 점검 계단",14.2f,1.05f,3,2.25f,1.10f);
            LivingSteps("아케이드 동쪽 계단",32.7f,1.05f,3,-2.25f,1.10f);
            Deck("셔터 위 횡단 발판",24.3f,4.32f,4.85f);
            var latch=Box("반대편 해제 레버",26.45f,1.48f,.48f,.70f,unlocked?LitGlass:ReedGold,false,4);
            Sign("셔터 너머에서 걸쇠를 풀면 지상 직통",17.25f,5.25f);
            Use("반대편 아케이드 걸쇠",26.45f,1.35f,()=>{
                if(!unlocked){unlocked=true;SetFlag(key);shutter.SetActive(false);latch.GetComponent<SpriteRenderer>().color=LitGlass;}
                Say("걸쇠가 열린 채 고정됐다. 다음 순찰에도 이 골목을 바로 통과할 수 있다.");
            },1.15f);
        }

        void SocketGutter()
        {
            var port=WallOutlet("처마 아래 빗물 대피 소켓",19.4f,4.15f,1);
            LivingSteps("처마 점검 계단",8.8f,1.05f,3,2.25f,1.04f);
            Deck("빗물받이 위 배수관",25.8f,4.82f,5.4f);
            LivingCanvas("빗물 막는 함석 처마",18.0f,6.85f,4.7f);
            var fall=Box("빗물받이 분출",22.5f,5.25f,.83f,4.78f,LitGlass,false,5);
            var drip=Box("분출 전 떨어지는 물방울",22.5f,7.60f,.18f,.41f,LitGlass,false,5);
            Use("배수관 속 순찰 표식 회수",26.4f,5.8f,()=>LowerSocketFind("socket.gutter.token","배수관 안쪽에서 오래된 순찰 표식을 찾았다."),1.25f);
            Sign("빗물이 멎으면 배수관으로 옆점프",12,5.6f);
            Step(dt=>{
                float t=Mathf.Repeat(Elapsed,3.70f);
                bool spray=t>1.05f && t<2.25f;
                fall.SetActive(spray); drip.SetActive(t<=1.05f);
                if(spray && !Resting(port) && Mathf.Abs(Hero.x-22.5f)<.62f && Hero.y>2.95f && Hero.y<7.90f)
                    ReturnToInspectionLane(21,"처마 밑 골목에 착지했다.");
            });
        }
    }
}
