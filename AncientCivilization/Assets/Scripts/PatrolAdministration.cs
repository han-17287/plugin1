using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom : MonoBehaviour
    {
        static readonly Color AdminSteel = new Color(.19f,.30f,.36f);
        static readonly Color AdminPaper = new Color(.75f,.77f,.63f);
        static readonly Color AdminMint = new Color(.45f,.95f,.77f);
        static readonly Color AdminAmber = new Color(.98f,.65f,.28f);
        static readonly Color AdminGlass = new Color(.4f,.82f,.93f,.55f);

        void BuildAdministration(int id)
        {
            switch (id)
            {
                case 21: AdminShutterHall(); break;
                case 22: AdminSorter(); break;
                case 23: AdminStacks(); break;
                case 24: AdminOptics(); break;
                case 25: AdminApproval(); break;
                case 26: AdminCounterweight(); break;
                case 27: AdminHandoff(); break;
                case 28: AdminReceiver(); break;
                case 29: AdminRepository(); break;
                case 30: AdminWiring(); break;
            }
        }

        bool AdminOn(float x, float top, float halfWidth)
        {
            return Grounded && Mathf.Abs(Hero.x-x) < halfWidth+.25f && Mathf.Abs(Hero.y-top-.9f) < .38f;
        }

        static void AdminTint(GameObject obj, Color color) => obj.GetComponent<SpriteRenderer>().color = color;

        void AdminDoor(GameObject leaf, bool open, float width, float height)
        {
            if (leaf.activeSelf == !open) return;
            Vector2 p = leaf.transform.localPosition;
            // 순환 셔터와 복구 바닥이 플레이어의 몸 안에서 닫히지 않도록 한다.
            if (!open && Mathf.Abs(Hero.x-p.x) < width*.5f+.38f && Mathf.Abs(Hero.y-p.y) < height*.5f+.93f) return;
            leaf.SetActive(!open);
        }

        void AdminSteps(string name, float x, float y, int count, int direction=1)
        {
            for (int i=0; i<count; i++)
                Deck(name,x+i*direction*2.05f,y+i*1.60f,2.65f);
        }

        GameObject AdminDetail(GameObject mount,string name,float dx,float dy,float width,float height,Color ink,int order=5)
        {
            var mark=Box(name,0,0,width,height,ink,false,order);
            mark.transform.SetParent(mount.transform,false);
            mark.transform.localPosition=new Vector2(dx,dy);
            return mark;
        }

        void AdminBindings(GameObject shutter,float width,float height)
        {
            for(int i=0;i<4;i++)
                AdminDetail(shutter,"겹철 띠",0,height*(i-1.5f)*.21f,width*.92f,.07f,AdminPaper);
            AdminDetail(shutter,"수동 해제봉",width*.19f,.02f,.05f,height*.35f,AdminAmber);
        }

        void AdminBeam(GameObject line, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 delta=b-a;
            line.transform.localPosition=(a+b)*.5f;
            line.transform.localScale=new Vector3(delta.magnitude,thickness,1);
            line.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }

        static float AdminBeamDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 d=b-a;
            return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.001f,d.sqrMagnitude)));
        }

        // 21 / 71. 아래 점검통로와 위 통과로가 끝의 같은 전산 단말로 이어진다.
        void AdminShutterHall()
        {
            AdminSteps("발급 대기 계단",4,1.4f,3);
            Deck("출입 심사 마루",22,4.6f,30);
            Deck("심사실 퇴장 발판",39,3.5f,4);
            Box("지하 점검 안내등",21,.4f,17,.08f,AdminMint);
            Hook("천장 이동 고리",11,8.7f);
            Hook("퇴실 고리",38,7.7f);
            Sign("황색 한 칸 뒤에 셔터가 닫힙니다",5.8f,6.8f);
            Sign("점검 통로 ▼",11.4f,2.0f);
            var gates=new GameObject[3];
            var lamps=new GameObject[3];
            for (int i=0;i<3;i++)
            {
                float x=14+7*i;
                gates[i]=Box("번호 셔터 "+(i+1),x,7.05f,.65f,4.9f,AdminSteel,true);
                AdminBindings(gates[i],.65f,4.9f);
                lamps[i]=Box("바닥 예고등 "+(i+1),x-1.05f,4.69f,1.4f,.1f,AdminMint);
                Box("셔터 구동부",x,9.65f,1.4f,.31f,AdminPaper);
            }
            Box("중간 대기 칸",24.5f,4.64f,2.1f,.08f,AdminPaper);
            Portal("전산탑 직통 통로",40.1f,.97f,30,()=>Flag("admin.towerGrid"));
            Use("점검원의 수기",21,.8f,()=>{
                SetFlag("admin.maintenanceNote");
                Say("「2번 셔터 앞에서 숨을 고르면 마지막 두 문이 같이 열린다.」");
            });
            Step(dt=>{
                for (int i=0;i<3;i++)
                {
                    float shift=i==0?0:i==1?1.55f:2.12f;
                    float beat=Mathf.Repeat(Elapsed-shift,5.3f);
                    bool open=beat<1.63f;
                    AdminDoor(gates[i],open,.65f,4.9f);
                    AdminTint(lamps[i],open?(beat>1.27f?AdminAmber:AdminMint):AdminSteel);
                }
            });
        }

        // 22 / 72. 하행 벨트는 플랫폼 자체가 이동하고, 봉인 해제는 실제 중간 고리 도착으로만 기록한다.
        void AdminSorter()
        {
            AdminSteps("서류 투입 계단",3.8f,1.6f,4);
            Deck("분류기 투입구",13,6.4f,7);
            Deck("상부 승인 수로",24.2f,7.75f,3.4f);
            Deck("반송 창고",28,1.6f,10.2f);
            Hook("출구 견인 고리",39,7.0f);
            var carrier=Deck("하행 반송 벨트",15.9f,6.35f,3.5f);
            var outerUpper=Hook("직인 원형 / 고가",20.5f,9.4f).GetComponent<GrappleAnchor>();
            var outerLower=Hook("직인 원형 / 창고",23,4.65f).GetComponent<GrappleAnchor>();
            var innerUpper=Hook("직인 마름모 / 고가",23.5f,9.55f).GetComponent<GrappleAnchor>();
            var innerLower=Hook("직인 마름모 / 창고",26.2f,4.85f).GetComponent<GrappleAnchor>();
            var seal=Hook("봉인 서랍 인출 고리",31,7.65f);
            Deck("서랍 내부",35.5f,6.15f,6.4f);
            Box("서랍 뒷판",38.75f,7.4f,.42f,2.95f,AdminSteel,true);
            Box("서랍 천장",35.65f,8.93f,6.55f,.3f,AdminSteel,true);
            var drawer=Box("봉인 서랍 문",32.33f,7.48f,.35f,2.65f,AdminPaper,true);
            AdminBindings(drawer,.35f,2.65f);
            for(int i=0;i<5;i++)
                AdminDetail(carrier,"벨트 이송 롤러",-1.28f+i*.64f,-.01f,.23f,.08f,AdminPaper);
            var stamp=Box("분류 시간 인장",19,6.72f,.5f,.5f,AdminMint);
            Sign("분류 문양 점등 중 · 위는 접수, 아래는 반송",11,8.25f);
            Sign("서랍 7호 / 견인 봉인",33.3f,10);
            Use("반송 송장 읽기",35.6f,7.05f,()=>{
                if (!Flag("admin.drawerOpen")) return;
                SetFlag("admin.drawerRead");
                Say("수취 층은 지워졌지만 발송인은 전산탑이었다.");
            });
            Step(dt=>{
                float t=Mathf.PingPong(Elapsed*.23f,1);
                Place(carrier,Mathf.Lerp(15.9f,27.0f,t),Mathf.Lerp(6.35f,1.65f,t)-.175f);
                bool circle=Mathf.Repeat(Elapsed,5.6f)<2.8f;
                outerUpper.SetOperational(circle);
                outerLower.SetOperational(circle);
                innerUpper.SetOperational(!circle);
                innerLower.SetOperational(!circle);
                stamp.transform.localRotation=Quaternion.Euler(0,0,circle?0:45);
                AdminTint(stamp,circle?AdminMint:AdminAmber);
                if (!Flag("admin.drawerOpen") && Arrived(seal))
                { SetFlag("admin.drawerOpen"); Say("봉인 와이어가 풀렸다. 서랍 안쪽에서 종이 냄새가 난다."); }
                AdminDoor(drawer,Flag("admin.drawerOpen"),.35f,2.65f);
            });
        }

        // 23 / 73. 한쪽 손잡이로 다리를 만들고, 건너편에서 반대로 돌려 기존 벽 아래 귀환문을 연다.
        void AdminStacks()
        {
            AdminSteps("도서 반납 계단",4,1.55f,3);
            Deck("서쪽 서가 회랑",11.4f,4.75f,7.8f);
            Deck("동쪽 서가 회랑",26.7f,4.75f,8.4f);
            var shelf=Box("회전 서가",16.1f,8.4f,.6f,7.3f,AdminSteel,true);
            for(int i=0;i<10;i++)
            {
                var binding=AdminDetail(shelf,"책등",-.04f,(i-4.5f)*.64f,.4f,.53f,i%3==0?AdminPaper:AdminMint);
                AdminDetail(binding,"분류띠",0,.12f,.31f,.045f,AdminSteel,6);
            }
            var latch=Box("지하 귀환문",21.8f,1.76f,.6f,3.52f,AdminSteel,true);
            var door=Box("반송실 시한문",32,6.87f,.65f,4.25f,AdminSteel,true);
            var lamp=Box("인계 시각등",30.8f,7.88f,.5f,.5f,AdminSteel);
            Deck("인계실 통로",33.2f,4.75f,6.3f);
            Hook("서류 투입구 상부",11.2f,8.4f);
            Hook("반송 발송구",37,7.2f);
            Sign("서가를 펼쳐 서쪽에서 동쪽으로",9,6.7f);
            Use("서가 펼치기",14.3f,5.72f,()=>{
                if (!Flag("admin.shelfBridge"))
                { SetFlag("admin.shelfBridge"); Say("장서함이 옆으로 누워 회랑 사이를 이었다."); }
            });
            Use("서가 반대로 돌리기",23.7f,5.7f,()=>{
                if (!Flag("admin.shelfBridge")) {Say("서쪽 손잡이로 서가를 먼저 펼친다."); return;}
                SetFlag("admin.shelfBridge",false); SetFlag("admin.shelfReturn");
                Say("반대편 경첩이 풀렸다. 아래 반납구로 돌아갈 수 있다.");
            });
            Portal("반납구 · 셔터 복도",20,1.05f,21,()=>Flag("admin.shelfReturn"));
            float since=-10;
            Use("지연 반송 스위치",25.9f,5.72f,()=>{since=Elapsed; Say("한 번 점멸한 뒤 열립니다.");});
            Step(dt=>{
                bool bridge=Flag("admin.shelfBridge");
                float angle=bridge?-90:0;
                Vector2 centre=bridge?new Vector2(19.45f,4.62f):new Vector2(16.1f,8.27f);
                shelf.transform.localRotation=Quaternion.Euler(0,0,angle);
                shelf.transform.localPosition=centre;
                AdminDoor(latch,Flag("admin.shelfReturn"),.6f,3.52f);
                float age=Elapsed-since;
                bool pass=age>=.85f && age<3.25f;
                AdminTint(lamp,pass?AdminMint:age<.85f?(age<.38f||age>.66f?AdminAmber:AdminSteel):AdminSteel);
                AdminDoor(door,pass,.65f,4.25f);
            });
        }

        // 24 / 74. 두 리프트가 엇갈릴 때 높이를 교환한다. 거울에 닿는 감시광도 실제로 같은 광원이다.
        void AdminOptics()
        {
            var leftLift=Deck("선행 검수 리프트",14,2,4);
            var rightLift=Deck("후행 검수 리프트",19,6.2f,4);
            AdminSteps("검수 대기석",3.8f,1.45f,3);
            Deck("보정 난간",23.7f,5.3f,4.8f);
            var mirror=Box("검수 거울",25,6.35f,.22f,1.65f,AdminGlass,true);
            var sensor=Box("검수 수광부",33,9.1f,.75f,.75f,AdminSteel,true);
            var door=Box("검수 차단문",35,2.65f,.7f,5.3f,AdminSteel,true);
            AdminDetail(sensor,"수광 렌즈",0,0,.43f,.43f,AdminGlass).transform.localRotation=Quaternion.Euler(0,0,45);
            Vector2 emitter=new Vector2(28,10.85f),glass=new Vector2(25,6.35f),receiver=new Vector2(33,9.1f);
            Vector2 tangent=(glass-emitter).normalized+(receiver-glass).normalized;
            float mirrorAngle=Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg-90;
            Box("감시 렌즈",emitter.x,emitter.y,.7f,.6f,AdminPaper);
            var incident=Box("감시광",0,0,1,1,AdminAmber);
            var bounce=Box("반사광",0,0,1,1,AdminMint);
            Hook("검수 회로 갈고리",22.5f,8.55f);
            Sign("렌즈가 훑고 지나가면 검수 거울을 돌리세요",8,6.2f);
            float repelAt=0;
            Use("거울을 수광부로 돌리기",23.8f,6.25f,()=>{
                SetFlag("admin.opticOpen"); Say("검수 완료. 감시 회선이 문에 전력을 보낸다.");
            });
            Step(dt=>{
                float wave=Mathf.Sin(Elapsed*1.2f);
                Place(leftLift,14,4.25f+wave*2.2f-.175f);
                Place(rightLift,19,4.25f-wave*2.2f-.175f);
                bool lit=Flag("admin.opticOpen");
                Vector2 end=lit?glass:new Vector2(18.7f+5.8f*Mathf.Sin(Elapsed*1.62f),.25f);
                AdminBeam(incident,emitter,end,.085f);
                mirror.transform.localRotation=Quaternion.Euler(0,0,lit?mirrorAngle:25);
                bounce.SetActive(lit);
                if(lit) AdminBeam(bounce,glass,receiver,.11f);
                AdminTint(sensor,lit?AdminMint:AdminSteel);
                AdminDoor(door,lit,.7f,5.3f);
                if (!lit && Elapsed>repelAt && AdminBeamDistance(Hero,emitter,end)<.42f)
                { Warp(10.1f,1.1f); repelAt=Elapsed+1.4f; Say("검수 불일치. 재검 대기선으로 이동합니다."); }
            });
        }

        // 25 / 75. 색과 문양을 함께 바꾼다. 측면 차폐판은 실제 레이어 8 지형이어서 위 직선을 가로막는다.
        void AdminApproval()
        {
            AdminSteps("인가 대기 계단",3.6f,1.55f,4);
            Deck("인가실 진입",12.6f,6.35f,5.65f);
            var circle=Deck("원형 인가 바닥",17.6f,6.35f,4.7f);
            var diamond=Deck("마름모 인가 바닥",23.1f,6.35f,4.7f);
            var dot=Box("원형 인가등",17.6f,6.4f,.5f,.1f,AdminMint);
            var kite=Box("능형 인가등",23.1f,6.42f,.28f,.28f,AdminAmber);
            kite.transform.localRotation=Quaternion.Euler(0,0,45);
            Sign("○",17.6f,7.1f); Sign("◇",23.1f,7.1f);
            Deck("하부 서명실",21,1.9f,10);
            Deck("인증 직선 발사대",26,2.5f,3.0f);
            Box("불투명 승인 차폐판",29,7.8f,.84f,4.35f,AdminSteel,true);
            var hook=Hook("차폐판 너머의 고리",32.8f,5.65f);
            Deck("차폐판 뒤",35.4f,4.12f,5.75f);
            Hook("상부 대기고리",12.8f,9.1f);
            Sign("○ 정지 · ◇ 통과 / 아래에도 승인기가 있습니다",6.5f,8.25f);
            System.Action toggle=()=>{SetFlag("admin.diamond",!Flag("admin.diamond")); Say(Flag("admin.diamond")?"◇ 승인 · ○ 투과":"○ 승인 · ◇ 투과");};
            Use("인가 문양 교체",17.2f,7.25f,toggle,2.45f);
            Use("하부 승인기",19.1f,2.85f,toggle);
            Use("미발송 결정서",22.4f,2.84f,()=>{SetFlag("admin.unsent"); Say("「승인을 기다리는 동안 주소는 또 한 층 아래가 되었다.」");});
            Step(dt=>{
                bool d=Flag("admin.diamond");
                AdminDoor(circle,d,4.7f,.35f);
                AdminDoor(diamond,!d,4.7f,.35f);
                dot.SetActive(!d); kite.SetActive(d);
                if (!Flag("admin.lowerAngle") && Arrived(hook))
                { SetFlag("admin.lowerAngle"); Say("측면 차폐판 아래로 사선이 열린다."); }
            });
        }

        // 26 / 76. 레이어 8의 실제 상자를 밀어 승강기에 싣고, 내려서는 쪽 선반에 내려놓는다.
        void AdminCounterweight()
        {
            AdminSteps("서류 하역 계단",3.3f,1.5f,4);
            Deck("기록 상자 적재대",12.95f,6.3f,7.7f);
            var lift=Deck("무게 승강기",18.75f,6.3f,4.0f);
            Deck("저층 하역 선반",23.7f,1.3f,6.2f);
            Deck("고층 반출 선반",23.7f,6.3f,6.2f);
            Deck("스캔 아래 통행로",32.4f,1.3f,9.2f);
            Deck("순찰 주행로",31.8f,5.1f,9.1f);
            Deck("재승차 발판",38.3f,3.05f,3.7f);
            Hook("스캔 뒤 복귀 고리",39.85f,7.1f);
            var upperDoor=Box("상단 기록 문",27.15f,7.62f,.46f,2.65f,AdminSteel,true);
            var lowerDoor=Box("하단 기록 문",27.15f,2.56f,.46f,2.52f,AdminSteel,true);
            bool lowLast=Flag("admin.crateLow"), highLast=Flag("admin.crateHigh");
            var crate=Box("중량 기록 상자",lowLast||highLast?23.1f:14,lowLast?1.91f:6.92f,1.25f,1.2f,AdminPaper,true);
            AdminDetail(crate,"압인 봉인끈",0,.02f,.17f,1.15f,AdminSteel);
            AdminDetail(crate,"화물 식별표",-.31f,.21f,.33f,.18f,AdminMint);
            var mass=crate.GetComponent<Rigidbody2D>();
            if (!mass) mass=crate.AddComponent<Rigidbody2D>();
            mass.bodyType=RigidbodyType2D.Dynamic; mass.mass=.65f; mass.gravityScale=2.4f;
            mass.constraints=RigidbodyConstraints2D.FreezeRotation;
            mass.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            mass.interpolation=RigidbodyInterpolation2D.Interpolate;
            var scanA=Box("상부 세로 스캐너 A",30,6.55f,.16f,3.1f,AdminGlass);
            var scanB=Box("상부 세로 스캐너 B",34,6.55f,.16f,3.1f,AdminGlass);
            Box("스캐너 하부 레일",32,4.95f,7.8f,.09f,AdminPaper);
            Sign("상자를 밀어 싣고 함께 타세요 · 빈 승강기는 올라갑니다",10,8);
            Sign("스캔 진입 / S + Space로 한 층 아래",30,8.65f);
            float top=6.3f,scanReady=0;
            System.Action reset=()=>{
                mass.position=transform.TransformPoint(new Vector2(14,7.05f)); mass.linearVelocity=Vector2.zero;
                SetFlag("admin.crateLow",false); SetFlag("admin.crateHigh",false);
                Say("기록 상자가 원래 적재대로 반송됐다.");
            };
            Use("상자 반송",11.3f,7.2f,reset);
            Use("상자 반송",24.7f,2.23f,reset);
            Step(dt=>{
                Vector2 c=transform.InverseTransformPoint(mass.position);
                bool loaded=Mathf.Abs(c.x-18.75f)<2.32f && Mathf.Abs(c.y-top-.6f)<.38f;
                bool aboard=AdminOn(18.75f,top,2) || loaded && Mathf.Abs(Hero.x-c.x)<1.05f && Mathf.Abs(Hero.y-c.y-1.5f)<.4f;
                float goal=loaded?(aboard?1.3f:top):6.3f;
                top=Mathf.MoveTowards(top,goal,dt*2.4f);
                Place(lift,18.75f,top-.175f);
                bool onShelf=c.x>21.55f && c.x<26.5f;
                bool low=onShelf && c.y<3.0f, high=onShelf && c.y>5.9f;
                if(low!=Flag("admin.crateLow")) SetFlag("admin.crateLow",low);
                if(high!=Flag("admin.crateHigh")) SetFlag("admin.crateHigh",high);
                AdminDoor(upperDoor,high,.46f,2.65f);
                AdminDoor(lowerDoor,low,.46f,2.52f);
                float dx=Mathf.Sin(Elapsed*1.45f)*1.65f;
                scanA.transform.localPosition=new Vector2(30+dx,6.55f);
                scanB.transform.localPosition=new Vector2(34-dx,6.55f);
                if (Hero.y>4.85f && Hero.y<8.2f && Elapsed>scanReady &&
                    (Mathf.Abs(Hero.x-30-dx)<.43f || Mathf.Abs(Hero.x-34+dx)<.43f))
                { Warp(Mathf.Clamp(Hero.x,29,35),2.4f); scanReady=Elapsed+1; Say("검사 보류. 아래 통로에서 재승차할 수 있습니다."); }
            });
        }

        // 27 / 77. 한복판에서 두 스캐너가 동시에 물러나는 시간은 약 1.2초다.
        void AdminHandoff()
        {
            AdminSteps("감시실 천장 사다리",4,1.5f,4);
            Deck("천장 점검로 서단",13.5f,7.25f,6.8f);
            Hook("천장 조명용 고리",20.5f,10.3f);
            Deck("천장 점검로 중앙",24,8.55f,5.5f);
            Hook("인계실 천장 고리",29,10.55f);
            Deck("천장 점검로 동단",32.4f,7.5f,5.2f);
            Deck("오른쪽 고위 배출구",40.1f,7.35f,4.4f);
            Deck("왼쪽 저위 배출구",30.4f,2.05f,3.3f);
            var beamA=Box("서측 스캔",0,0,1,1,AdminGlass);
            var beamB=Box("동측 스캔",0,0,1,1,AdminGlass);
            var handoff=Box("인계 표시등",24.1f,.22f,3.3f,.15f,AdminSteel);
            Vector2 originA=new Vector2(16,5.3f),originB=new Vector2(29.2f,5.3f);
            Box("서측 광학 헤드",originA.x,originA.y,.66f,.56f,AdminPaper);
            Box("동측 광학 헤드",originB.x,originB.y,.66f,.56f,AdminPaper);
            Box("양방향 배출문",35.3f,1.7f,1.3f,3.4f,new Color(.22f,.57f,.62f,.45f));
            Sign("두 감시선 인계 중에는 바닥 중앙이 비어 있습니다",9.7f,2.2f);
            Sign("우행 ▲   좌행 ▼",35.2f,4.2f);
            float prevX=float.NaN,repelAt=0;
            Step(dt=>{
                float t=Mathf.Repeat(Elapsed,6);
                float west=t<2.35f?Mathf.Sin(Mathf.PI*t/2.35f):0;
                float east=t>3.02f && t<5.35f?Mathf.Sin(Mathf.PI*(t-3.02f)/2.33f):0;
                Vector2 endA=new Vector2(Mathf.Lerp(13.5f,25.1f,west),.16f);
                Vector2 endB=new Vector2(Mathf.Lerp(31.65f,19.85f,east),.16f);
                AdminBeam(beamA,originA,endA,.14f); AdminBeam(beamB,originB,endB,.14f);
                AdminTint(handoff,west<.12f && east<.12f?AdminMint:AdminAmber);
                Vector2 p=Hero;
                if (p.x>16.3f && p.x<28.85f && Elapsed>repelAt &&
                    (AdminBeamDistance(p,originA,endA)<.42f || AdminBeamDistance(p,originB,endB)<.42f))
                { Warp(p.x<22.6f?15.1f:30.1f,1.05f); repelAt=Elapsed+1.05f; }
                else if (p.y<3.6f && Mathf.Abs(p.x-prevX)<1.8f)
                {
                    if (prevX<35.3f && p.x>=35.3f) {Warp(40.1f,8.45f); Say("우행 통과. 고위 배출구.");}
                    else if (prevX>35.3f && p.x<=35.3f) {Warp(30.4f,3.15f); Say("좌행 통과. 저위 배출구.");}
                }
                prevX=Hero.x;
            });
        }

        // 28 / 78. 압력판에서 내리면 리프트가 잠시 높이를 유지해 일찍/늦게 뛰는 선택이 남는다.
        void AdminReceiver()
        {
            AdminSteps("유리실 접근",4.1f,1.55f,3);
            Deck("서쪽 압력 발판",13.7f,4.75f,4.2f);
            var lift=Deck("반대편 무게 발판",19.55f,2.25f,4);
            Deck("중간 문턱",23.2f,3.95f,3.85f);
            Deck("측면 사격 난간",24.8f,7.1f,5.3f);
            Hook("압력판 상부 고리",12.45f,8.2f);
            Hook("점검용 우회 고리",39,8.25f);
            var shutter=Box("이동 차폐막",29.55f,6.7f,.67f,4.85f,AdminSteel,true);
            Box("정면 고정 차폐대",30.1f,2.55f,.65f,5.1f,AdminSteel,true);
            AdminBindings(shutter,.67f,4.85f);
            var eye=Box("유리문 레이저 수광부",35.6f,7.85f,.72f,.92f,AdminAmber,true);
            var door=Box("광학 유리문",38.4f,3.18f,.63f,6.36f,AdminGlass,true);
            var gauge=Box("하중 신호",13.7f,4.81f,1.7f,.09f,AdminSteel);
            Box("상부 차폐 가이드",29.55f,12.5f,.13f,3.4f,AdminPaper);
            Sign("발판을 누른 시간만큼 반대편이 올라옵니다",8,6.35f);
            Sign("유리문 수광부 · 난간에서 조준 후 F 해제",24.2f,9.25f);
            Use("중간 문서함",23.3f,4.84f,()=>{
                SetFlag("admin.receiverNote");
                Say("차폐막이 높아지는 순간, 건너편 수광부의 금빛 렌즈가 보인다.");
            });
            float top=2.25f,coast=0;
            Step(dt=>{
                bool weighing=AdminOn(13.7f,4.75f,2.1f);
                if (weighing) {top=Mathf.MoveTowards(top,7.05f,dt*2.25f); coast=1.4f;}
                else if(coast>0) coast-=dt;
                else top=Mathf.MoveTowards(top,2.25f,dt*1.9f);
                Place(lift,19.55f,top-.175f);
                Place(shutter,29.55f,7.6f+3.1f*Mathf.Sin(Elapsed*1.4f));
                AdminTint(gauge,Color.Lerp(AdminSteel,AdminMint,Mathf.InverseLerp(2.25f,7.05f,top)));
                if (!Flag("admin.glassOpen") && Hero.x>=21.5f && Hero.x<=27.6f && Hero.y>=7.2f &&
                    BeamHits(eye))
                {SetFlag("admin.glassOpen"); Say("수광부 1회 수신. 검수 유리문이 열렸습니다.");}
                bool open=Flag("admin.glassOpen");
                AdminTint(eye,open?AdminMint:AdminAmber); AdminDoor(door,open,.63f,6.36f);
            });
        }

        // 29 / 79. 막힌 서가 아래는 먼지가 이어지고, 개방 가능한 서가 아래에서만 바람띠가 흐른다.
        void AdminRepository()
        {
            AdminSteps("보존실 계단",3.8f,1.5f,4);
            Deck("상부 원본 서가",14.7f,6.35f,8.3f);
            Deck("첫 번째 발사대",20.25f,5.85f,3.6f);
            Deck("낮은 발사대",23.55f,2.8f,3.65f);
            var hook=Hook("빗장 건너 고정 고리",32.6f,8.35f);
            var bolt=Box("개폐 천장 빗장",27.7f,8.8f,.8f,4.45f,AdminSteel,true);
            AdminBindings(bolt,.8f,4.45f);
            Box("빗장 상부 하우징",27.7f,13.2f,1.65f,1.2f,AdminPaper);
            Deck("고리 착지 단",34.15f,6.9f,4.2f);
            Box("먼지 낀 서가",11,1.7f,1.3f,3.4f,AdminSteel,true);
            for(int i=0;i<5;i++)
                Box("서가 하부 먼지",9.7f+i*.34f,.11f+(i%2)*.04f,.27f,.13f,AdminPaper);
            var falseWall=Box("아래에서 바람이 새는 서가",36,2.1f,.8f,3.8f,AdminSteel,true);
            Box("보관소 뒷면",40.45f,2.0f,.5f,4.0f,AdminSteel,true);
            Box("보관소 천장",38.3f,4.1f,4.8f,.36f,AdminSteel,true);
            var breeze=new GameObject[3];
            for(int i=0;i<3;i++) breeze[i]=Box("서가 밑 기류",35,.24f,.57f,.055f,AdminMint);
            Sign("밀폐 보존 서류 · 통풍 상태는 바닥에서 확인",7.9f,7.9f);
            Use("들뜬 서가 밑을 조사",35.1f,.9f,()=>{
                SetFlag("admin.repository"); Say("바닥 바람을 따라 서가를 밀었다. 도면에도 없는 빈 방이다.");
            });
            Use("빈 방의 벽보",38.55f,1.0f,()=>{
                if(Flag("admin.repository")) Say("「바깥 공기가 들어오면 봉인이 살아 있다는 뜻.」");
            });
            Step(dt=>{
                Place(bolt,27.7f,8.1f+2.15f*Mathf.Sin(Elapsed*1.3f));
                bool open=Flag("admin.repository");
                AdminDoor(falseWall,open,.8f,3.8f);
                for(int i=0;i<3;i++)
                {
                    float t=Mathf.Repeat(Elapsed*.65f+i/3f,1);
                    breeze[i].transform.localPosition=new Vector2(35.8f-2.25f*t,.22f+.21f*t);
                }
                if(!Flag("admin.boltLine") && Arrived(hook))
                {SetFlag("admin.boltLine"); Say("빗장 밑을 지나는 견인각을 기록했다.");}
            });
        }

        // 30 / 80. 왕복 손잡이로 구간을 단축한 뒤 두 회선을 연결하면 층 사이 순환 동선도 열린다.
        void AdminWiring()
        {
            AdminSteps("전산탑 진입 계단",4,1.55f,3);
            Deck("배전실 난간",12.3f,4.75f,6.8f);
            var lift=Deck("급전 승강기",19,1.4f,4);
            Deck("상부 배전대",25,6.75f,6.5f);
            var bridge=Deck("전산탑 가동교",31.4f,6.75f,6.8f);
            Deck("탑측 인계대",38.25f,6.75f,6.8f);
            AdminSteps("탑 뒤 사다리",43.1f,1.4f,3,-1);
            Hook("배선 점검 고리",23.35f,10);
            Hook("탑 고리",37.8f,9.45f);
            var tempDoor=Box("임시 출입문",26,2.1f,.6f,4.2f,AdminSteel,true);
            var shortcut=Box("순찰 회선 방화문",32,2.2f,.7f,4.4f,AdminSteel,true);
            var liftCable=Box("승강기 송전선",17.6f,5.2f,.09f,7.4f,AdminSteel);
            var bridgeCable=Box("교량 송전선",31.2f,7.38f,6.2f,.1f,AdminSteel);
            AdminBindings(tempDoor,.6f,4.2f);
            AdminBindings(shortcut,.7f,4.4f);
            var handle=Box("돌아오는 길 손잡이",35,1.1f,.28f,.73f,AdminAmber);
            Sign("서쪽 배전반 / 승강기 왕복 교량",10,6.55f);
            Sign("임시증은 동행만 허용합니다",23,3.15f);
            Use("전력 배분 전환",12.7f,5.68f,()=>{
                bool bridgePower=!Flag("admin.bridgePower");
                SetFlag("admin.bridgePower",bridgePower);
                Say(bridgePower?"교량 쪽 전압 정상. 승강기는 하부에 대기합니다.":"승강기 쪽 전압 정상. 상부에서 우회 회선을 확인하세요.");
            });
            float temporaryUntil=-1;
            Use("동행 임시 출입증",22.2f,1.05f,()=>{ temporaryUntil=Elapsed+5.3f; Say("동행 1회 출입. 귀환은 탑측 손잡이를 이용하세요."); });
            Use("탑측 귀환 손잡이",35,1.05f,()=>{
                if(!Flag("admin.towerVisited")) {Say("탑측 도착 도장이 필요합니다."); return;}
                if (!Flag("admin.returnPass"))
                {SetFlag("admin.returnPass"); Say("귀환 출입증 등록. 두 방화문이 항시 개방됩니다.");}
            });
            Use("양 회선 우회 접속",37.8f,7.68f,()=>{
                if (!Flag("admin.bridgePower")) {Say("먼저 서쪽 배전반에서 교량에 전력을 배분하세요."); return;}
                if (!Flag("admin.towerGrid"))
                {
                    SetFlag("admin.towerGrid"); SetFlag("living.marketTower"); SetFlag("admin.towerLaundry");
                    Say("두 회선 연결. 셔터 복도와 시장, 야경 세탁실에 귀환등이 켜졌다.");
                }
            });
            Use("왕복 출입구 · 탑 건너편",5.5f,1.05f,()=>{
                if(Flag("admin.returnPass")) Warp(40.65f,1.15f);
                else Say("탑측에서 돌아올 때 귀환 손잡이를 돌리세요.");
            });
            Use("왕복 출입구 · 서쪽 배전실",41.25f,1.05f,()=>{
                if(Flag("admin.returnPass")) Warp(5.5f,1.15f);
                else Say("하부 귀환 손잡이에 도착 도장을 읽히세요.");
            });
            Portal("셔터 복도 직통",39.8f,7.65f,21,()=>Flag("admin.towerGrid"));
            Portal("야시장 연결선",37.9f,.98f,11,()=>Flag("living.marketTower"));
            Portal("야경 세탁실 승강기",42.3f,2.48f,40,()=>Flag("admin.towerLaundry"));
            Step(dt=>{
                if(!Flag("admin.towerVisited") && Hero.x>38.1f) SetFlag("admin.towerVisited");
                bool all=Flag("admin.towerGrid"), bridgePower=Flag("admin.bridgePower");
                float top=all||!bridgePower?Mathf.Lerp(1.4f,6.75f,Mathf.PingPong(Elapsed*.32f,1)):1.4f;
                Place(lift,19,top-.175f);
                AdminDoor(bridge,!all&&!bridgePower,6.8f,.35f);
                bool pass=Flag("admin.returnPass");
                AdminDoor(tempDoor,pass||Elapsed<temporaryUntil,.6f,4.2f);
                AdminDoor(shortcut,pass,.7f,4.4f);
                AdminTint(handle,pass?AdminMint:AdminAmber);
                AdminTint(liftCable,all||!bridgePower?AdminMint:AdminSteel);
                AdminTint(bridgeCable,all||bridgePower?AdminMint:AdminSteel);
            });
        }
    }
}
