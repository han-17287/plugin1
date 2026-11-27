using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        // 아래 작업로는 양방향으로 열려 있다. 전력 퍼즐은 그 위 점검 회랑에 놓는다.
        [SerializeField, Min(.1f)] float drawerHoldSeconds = 2.3f;
        [SerializeField, Min(.1f)] float bridgeAfterglowSeconds = 2.8f;
        [SerializeField, Min(.1f)] float pressureDoorGraceSeconds = 2.2f;
        static readonly Color CabinetDark = new Color(.14f, .22f, .28f);
        static readonly Color CabinetFace = new Color(.32f, .43f, .45f);
        static readonly Color CabinetOn = new Color(.89f, .79f, .41f);
        static readonly Color CabinetSafe = new Color(.55f, .84f, .63f);

        void BuildSocketAdmin(int id)
        {
            switch (id)
            {
                case 21: BuildSinglePowerRoom(); break;
                case 22: BuildCapacitorWalk(); break;
                case 23: BuildFoldingFireDoor(); break;
                case 24: BuildPatrolLamp(); break;
                case 25: BuildTurningRack(); break;
                case 26: BuildIndexDrawers(); break;
                case 27: BuildGlassVent(); break;
                case 28: BuildLiftTransfer(); break;
                case 29: BuildPressureLock(); break;
                case 30: BuildReturnLadder(); break;
            }
        }

        // 톱니가 있는 랙 프레임과 램프의 가는 눈금은 배경이고, 선반의 윗면만 실제 발판이다.
        void CabinetRail(string label, float x, float bottom, float top)
        {
            Box(label, x, (bottom + top) * .5f, .22f, top-bottom, CabinetDark, false, 0);
            for (float y = bottom+.22f; y < top; y += .83f)
                Box("레일 눈금", x+.13f, y, .37f, .06f, CabinetFace, false, 1);
        }

        GameObject CabinetLamp(string label, float x, float y)
        {
            Box(label+" 테두리", x, y, .75f, .28f, CabinetDark, false, 2);
            return Box(label, x, y, .56f, .12f, CabinetFace, false, 3);
        }

        void CabinetEntrance(float top)
        {
            Deck("서측 점검 계단", 4, top*.48f, 3);
            CabinetDeck("서측 조작대", 8.5f, top, 5.45f);
        }

        GameObject CabinetDeck(string name, float x, float top, float width)
        {
            var deck=Deck(name,x,top,width);
            PlugDistrictArt.Part(deck.transform,name+" 금속 마감재",2,PlugDistrictPiece.Deck,
                Vector2.zero, new Vector2(width,.35f),5);
            return deck;
        }

        void CabinetLanding(string name, float x, float y, float width)
        {
            CabinetDeck(name, x, y, width);
            Box("착지 표지", x, y-.08f, width*.44f, .07f, CabinetOn, false, 6);
        }

        void CabinetPivots(GameObject leaf, float length)
        {
            for (int i=0; i<4; ++i)
                AdminDetail(leaf, "가동 패널 보강대", 0, length*(i-1.5f)*.23f, .51f, .09f, CabinetFace);
            AdminDetail(leaf, "끝단 표시", 0, length*.44f, .49f, .13f, CabinetOn);
        }

        // 회전하려는 다음 자세에 몸이 있으면 멈춘다. 복귀 시에도 밀폐 패널에 끼이지 않는다.
        bool CabinetLeafIntersects(Vector2 hero, Vector2 centre, Vector2 axis, float length)
        {
            Vector2 side = new Vector2(axis.y, -axis.x);
            Vector2 relative = hero-centre;
            float widthMargin = .36f*Mathf.Abs(side.x) + .89f*Mathf.Abs(side.y);
            float endMargin = .36f*Mathf.Abs(axis.x) + .89f*Mathf.Abs(axis.y);
            return Mathf.Abs(Vector2.Dot(relative, side)) < .27f+widthMargin &&
                   Mathf.Abs(Vector2.Dot(relative, axis)) < length*.5f+endMargin;
        }

        static Vector2 CabinetAxis(float angle)
        {
            float radians=angle*Mathf.PI/180f;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }

        void CabinetPose(GameObject panel, Vector2 pivot, Vector2 axis, float length, float angle)
        {
            panel.transform.localPosition = pivot+axis*(length*.5f);
            panel.transform.localRotation = Quaternion.Euler(0, 0, -angle);
        }

        void BuildSinglePowerRoom()
        {
            CabinetEntrance(3.5f);
            var source=WallOutlet("다리 회선 A", 9.85f, 5.45f, -1);
            var liftSource=WallOutlet("승강 회선 B", 24.1f, 5.5f, -1);
            var bridge=CabinetDeck("A 인출교", 15.5f, 3.5f, 8.65f);
            bridge.SetActive(false);
            CabinetLanding("전원 교대 마루", 22.5f, 3.5f, 5.7f);
            var lift=CabinetDeck("B 점검 승강기", 28.4f, 3.5f, 4.7f);
            CabinetRail("승강기 구동 레일", 28.4f, 2.35f, 10.5f);
            CabinetLanding("상층 기록 회랑", 36.5f, 8.4f, 10.5f);
            var bridgeLamp=CabinetLamp("교량 전압", 10.7f, 6.9f);
            var liftLamp=CabinetLamp("승강기 전압", 25, 6.9f);
            Sign("왼쪽은 다리, 오른쪽은 승강기", 3.2f, 5.65f);
            Sign("건넌 뒤 G로 회수", 18.4f, 7.3f);
            float liftTop=3.5f;
            Step(dt=>{
                bool bridgeFed=Powered(source);
                bool liftFed=Powered(liftSource);
                AdminDoor(bridge, !bridgeFed, 8.65f, .35f);
                AdminTint(bridgeLamp, bridgeFed ? CabinetOn : CabinetDark);
                AdminTint(liftLamp, liftFed ? CabinetOn : CabinetDark);
                liftTop=Mathf.MoveTowards(liftTop, liftFed ? 8.4f : 3.5f, dt*2.6f);
                Place(lift, 28.4f, liftTop-.175f);
            });
        }

        void BuildCapacitorWalk()
        {
            CabinetEntrance(3.2f);
            var first=WallOutlet("첫 축전 단자", 9.6f, 5.25f, -1);
            var second=WallOutlet("두 번째 축전 단자", 23.25f, 6.35f, -1);
            CabinetLanding("축전기 교대 마루", 22.25f, 4.55f, 4.0f);
            CabinetLanding("축전 회랑 끝", 37.9f, 5.9f, 7.1f);
            var ports=new[]{first, second};
            var decks=new[]{CabinetDeck("첫 잔류 발판", 15.65f, 3.85f, 7.4f),
                            CabinetDeck("둘째 잔류 발판", 29.6f, 5.24f, 8.1f)};
            var lights=new GameObject[2,4];
            var remaining=new float[2];
            for(int k=0; k<2; ++k)
            {
                decks[k].SetActive(false);
                for(int i=0; i<4; ++i)
                    lights[k,i]=CabinetLamp("잔류 눈금", (k==0 ? 12.8f : 26.45f)+i*1.75f, k==0 ? 3.37f : 4.77f);
            }
            Sign("전원을 뽑으면 눈금이 하나씩 꺼진다", 3.8f, 6.0f);
            Step(dt=>{
                for (int k=0; k<2; ++k)
                {
                    remaining[k]=Powered(ports[k]) ? bridgeAfterglowSeconds : Mathf.Max(0, remaining[k]-dt);
                    AdminDoor(decks[k], remaining[k]<=0, k==0 ? 7.4f : 8.1f, .35f);
                    for (int i=0; i<4; ++i)
                        AdminTint(lights[k,i], remaining[k] > i*bridgeAfterglowSeconds*.25f ? CabinetOn : CabinetDark);
                }
            });
        }

        void BuildFoldingFireDoor()
        {
            CabinetEntrance(3.6f);
            CabinetLanding("방화문 서측 발판", 12f, 3.6f, 4.7f);
            CabinetLanding("동측 피난소", 25.1f, 3.6f, 7.85f);
            Deck("후면 수동 접근턱", 24, 1.77f, 4);
            var front=WallOutlet("방화문 앞면 단자", 13.1f, 5.55f, -1);
            var back=WallOutlet("방화문 뒷면 단자", 22.4f, 5.55f, 1);
            const float bladeLength=7;
            Vector2 pivot=new Vector2(14.45f,3.36f);
            var leaf=Box("양면 방화문", pivot.x, pivot.y+bladeLength*.5f, .52f, bladeLength, CabinetFace, true);
            CabinetPivots(leaf, bladeLength);
            var hinge=Box("방화 경첩", pivot.x, pivot.y, .71f,.71f, CabinetOn, false, 6);
            Sign("세우기", 9.7f, 6.7f);
            Sign("눕히기", 23.25f, 6.7f);
            float angle=0; bool layDown=false;
            Step(dt=>{
                if (Powered(front)) layDown=false;
                if (Powered(back)) layDown=true;
                float next=Mathf.MoveTowards(angle, layDown ? 90 : 0, 88*dt);
                if (next==angle) return;
                Vector2 axis=CabinetAxis(next);
                if (CabinetLeafIntersects(Hero, pivot+axis*(bladeLength*.5f), axis, bladeLength)) return;
                angle=next;
                CabinetPose(leaf, pivot, axis, bladeLength, angle);
                hinge.transform.localRotation=Quaternion.Euler(0,0,-angle);
            });
        }

        void BuildPatrolLamp()
        {
            CabinetEntrance(3.5f);
            Deck("감지 마루", 23.2f, 3.5f, 24.5f);
            var blind=WallOutlet("센서 뒤편 피난 단자", 21.25f, 5.75f, -1, 2.5f);
            Box("감시함 차양", 22.3f, 7.42f, 3.6f,.26f,CabinetDark);
            CabinetLanding("검사 출구", 38, 4.4f, 6.5f);
            const float start=12.8f, end=33.9f;
            var sweep=Box("순찰등 검사광", start, 6.65f, .25f, 6.3f, new Color(1,.65f,.32f,.65f));
            var head=Box("순찰등 렌즈", start, 9.85f, .82f,.32f,CabinetFace);
            Box("감시등 주행 레일", (end+start)*.5f,10.15f, end-start,.13f,CabinetDark);
            var warning=Box("바닥 순찰 예고", start,3.56f,1.3f,.09f,CabinetOn,false,6);
            Sign("바닥 불빛을 보고 센서 뒤에 붙는다",3.2f,6.2f);
            float immunity=0;
            Step(dt=>{
                float beat=Mathf.Repeat(Elapsed, 6.1f);
                bool scanning=beat>=1.05f && beat<=5.05f;
                float x=Mathf.Lerp(start, end, Mathf.Clamp01((beat-1.05f)/4f));
                float ahead=Mathf.Lerp(start, end, Mathf.Clamp01((beat-.28f)/4f));
                Place(head,x,9.85f);
                Place(sweep,x,6.65f);
                Place(warning,ahead,3.56f);
                sweep.SetActive(scanning);
                warning.SetActive(beat<5.05f);
                immunity=Mathf.Max(0,immunity-dt);
                if (scanning && immunity<=0 && Hero.y>3.5f && Hero.y<10.2f &&
                    Mathf.Abs(Hero.x-x)<.44f && !Resting(blind))
                {
                    Warp(10.5f, 1.05f);
                    Say("감시등이 대기 통로로 돌려보냈다.");
                    immunity=1;
                }
            });
        }

        void BuildTurningRack()
        {
            CabinetEntrance(3.1f);
            CabinetLanding("랙 정비 선반", 12.6f,3.1f,5);
            CabinetLanding("랙 뒤 색인실", 26.1f,3.1f,7.4f);
            var pivot=new Vector2(16,2.92f);
            const float length=6.8f;
            var rack=Box("회전 서버랙",pivot.x,pivot.y+length*.5f,.54f,length,CabinetDark,true);
            CabinetPivots(rack,length);
            var fixedPort=WallOutlet("랙 구동 단자",10.6f,4.85f,-1);
            // 축 반대쪽 단자는 랙의 앞쪽에 달려 있다. 랙을 떠나도 선택된 자세는 끝까지 간다.
            var rider=WallOutlet("랙 이동 단자",15.41f,6.22f,-1);
            Sign("바깥 단자나 랙 단자를 연결하면 오른쪽으로 돈다",3.2f,6.4f);
            float angle=0; bool engaged=false;
            Step(dt=>{
                if (Powered(fixedPort) || Powered(rider)) engaged=true;
                float next=engaged ? Mathf.MoveTowards(angle,90,35*dt) : angle;
                Vector2 axis=CabinetAxis(next);
                if (next!=angle && (Resting(rider) || !CabinetLeafIntersects(Hero,pivot+axis*(length*.5f),axis,length)))
                    angle=next;
                axis=CabinetAxis(angle);
                CabinetPose(rack,pivot,axis,length,angle);
                // 정지한 틱도 위치를 샘플링한다. 이탈할 때 마지막 회전 속도가 남지 않는다.
                // 가동 단자를 선반 위로 빼서 회전 후의 벽 고정점과 디딤면이 겹치지 않는다.
                Vector2 p=pivot+axis*3.3f+new Vector2(-.59f,1.05f*Mathf.Sin(angle*Mathf.PI/180f));
                Place(rider,p.x,p.y);
            });
        }

        void BuildIndexDrawers()
        {
            CabinetEntrance(3.35f);
            var index=WallOutlet("기록 색인 단자",11,5.4f,-1);
            CabinetLanding("색인대 맞은편",27.8f,7.6f,6.9f);
            var panels=new[]{ Deck("1번 기록 서랍",15.4f,4.55f,4.6f),
                              Deck("2번 기록 서랍",18.85f,5.9f,4.6f),
                              Deck("3번 기록 서랍",22.4f,7.2f,4.6f) };
            var lamps=new GameObject[3];
            var indexLamps=new GameObject[3];
            for(int i=0; i<3; ++i)
            {
                float x=15.4f+i*3.5f, top=4.55f+i*1.33f;
                panels[i].SetActive(false);
                Box("철제 서랍 홈",x,top-.38f,4.35f,.63f,CabinetDark,false,0);
                lamps[i]=CabinetLamp("서랍 번호등",x,top-.38f);
                indexLamps[i]=CabinetLamp("다음 기록 표시", 8.4f+i*1.36f,6.74f);
                Sign((i+1).ToString(), 8.18f+i*1.36f,7.38f);
                Sign((i+1).ToString(),x-.1f,top-.62f);
            }
            Sign("점등된 번호의 서랍이 나온다",2.8f,5.1f);
            float timer=0;
            Step(dt=>{
                if(Powered(index)) timer+=dt;
                else timer=0;
                float phase=Mathf.Repeat(timer/drawerHoldSeconds,3);
                int current=(int)phase;
                float fraction=phase-current;
                for(int i=0; i<3; ++i)
                {
                    bool lit=Powered(index) && current==i;
                    bool extended=Powered(index) && (current==i || i==(current+2)%3 && fraction<.32f && timer>=drawerHoldSeconds);
                    AdminDoor(panels[i],!extended,4.6f,.35f);
                    AdminTint(lamps[i],lit?CabinetOn:CabinetDark);
                    AdminTint(indexLamps[i],lit?CabinetOn:CabinetDark);
                }
            });
        }

        void BuildGlassVent()
        {
            CabinetEntrance(3.2f);
            var perch=WallOutlet("환기구 측면 단자",13.45f,6.35f,1);
            Deck("측면 점검턱",11.4f,4.65f,3.4f);
            // 지면 통로와 격벽 사이 틈은 2.1이다. 투척선은 유리의 실제 콜라이더에서 멈춘다.
            Box("투명 격벽 아래",20.6f,4.04f,.65f,3.88f,AdminGlass,true);
            Box("투명 격벽 위",20.6f,11.7f,.65f,6.52f,AdminGlass,true);
            for(int i=0; i<2; ++i)
                Box("환기구 모서리",20.6f,i==0 ? 5.98f : 8.44f,.96f,.09f,CabinetFace);
            var receiver=WallOutlet("환기구 너머 단자",27.3f,7.86f,-1);
            var walk=Deck("환기구 정비 발판",23.65f,6.45f,5.1f);
            walk.SetActive(false);
            CabinetLanding("격벽 건너 점검실",31.5f,6.4f,6);
            Sign("유리 위 틈으로 단자를 겨눈다",5.25f,8.4f);
            var status=CabinetLamp("격벽 검수등",28.5f,9.12f);
            Step(dt=>{
                bool pass=Powered(receiver);
                AdminDoor(walk,!pass,5.1f,.35f);
                AdminTint(status,pass ? CabinetSafe : Resting(perch) ? CabinetOn : CabinetDark);
            });
        }

        void BuildLiftTransfer()
        {
            CabinetEntrance(2.8f);
            var west=CabinetDeck("서행 기록 승강기",13.4f,2.8f,4.2f);
            var east=CabinetDeck("동행 기록 승강기",24.1f,7.4f,4.4f);
            WallOutlet("중간 환승 단자",18.45f,6.95f,1,2.5f);
            CabinetRail("서측 균형 레일",13.4f,2.15f,9.9f);
            CabinetRail("동측 균형 레일",24.1f,2.15f,10.5f);
            CabinetLanding("서측 우편 검사대",16.3f,4.7f,2.7f);
            CabinetLanding("동측 고가 발송대",31.1f,8.55f,8.5f);
            Sign("중간 단자에서 반대 칸을 기다린다",3.5f,5.25f);
            Step(dt=>{
                float t=(1-Mathf.Cos(Elapsed*.98f))*.5f;
                Place(west,13.4f,Mathf.Lerp(2.8f,7.3f,t)-.175f);
                Place(east,24.1f,Mathf.Lerp(8.95f,2.75f,t)-.175f);
            });
        }

        void BuildPressureLock()
        {
            CabinetEntrance(3.4f);
            Deck("압력판 발판",14.4f,3.4f,4.3f);
            var plate=Box("잠금실 압력판",14.4f,3.45f,2.4f,.11f,CabinetFace,false,5);
            var port=WallOutlet("압력실 내부 단자",26.2f,5.8f,-1);
            var coveredPort=port.GetComponent<GrappleAnchor>();
            coveredPort.SetOperational(false);
            CabinetLanding("잠금실 안쪽",29.5f,4.2f,7.1f);
            var hood=Box("압력실 소켓 덮개",25.12f,5.8f,.36f,2.5f,CabinetFace,true);
            var shutter=Box("압력실 통과문",20.6f,6.45f,.66f,6.5f,CabinetDark,true);
            CabinetPivots(shutter,6.5f);
            var lamp=CabinetLamp("압력 시한등",19.65f,8.55f);
            Sign("압력판을 밟고 꽂은 뒤 문이 닫히기 전에 견인",4,7.3f);
            float grace=0;
            Step(dt=>{
                bool held=AdminOn(14.4f,3.4f,1.2f);
                bool plugged=Powered(port);
                coveredPort.SetOperational(held || plugged);
                if (held) grace=pressureDoorGraceSeconds;
                else grace=Mathf.Max(0,grace-dt);
                AdminDoor(hood,held || plugged,.36f,2.5f);
                AdminDoor(shutter,grace>0,.66f,6.5f);
                AdminTint(plate,held ? CabinetOn : CabinetFace);
                AdminTint(lamp,held ? CabinetSafe : grace>0 ? CabinetOn : CabinetDark);
            });
        }

        void BuildReturnLadder()
        {
            const string unlocked="wallroute.30.returnLadder";
            CabinetEntrance(3.1f);
            // 아래로 돌아 나갔다가 오른쪽에서 왼쪽으로 되짚는 상부 덕트.
            Deck("배전실 남쪽 도보 발판",28.4f,1.6f,4.2f);
            Deck("배전실 동쪽 계단",32.1f,3.25f,4.35f);
            Deck("동쪽 돌아가는 계단",35.9f,4.95f,4.4f);
            Deck("상부 덕트 진입",32.2f,6.55f,4.5f);
            CabinetLanding("배전실 상부 복귀로",20.45f,7.35f,19.65f);
            var port=WallOutlet("배전실 입구 위 단자",10.5f,8.92f,1);
            var steps=new[]{Deck("복귀 사다리 하단",7.65f,3.85f,2.3f),
                            Deck("복귀 사다리 중단",9.35f,5.65f,2.5f)};
            for(int i=0;i<steps.Length;++i) steps[i].SetActive(Flag(unlocked));
            var rail=Box("풀려 내려온 사다리",8.88f,5.43f,.23f,4.85f,CabinetOn,false,1);
            rail.SetActive(Flag(unlocked));
            var hook=CabinetLamp("사다리 빗장",11.5f,9.9f);
            AdminTint(hook,Flag(unlocked)?CabinetSafe:CabinetFace);
            Sign("동쪽 덕트를 돌아 사다리 빗장으로",4.1f,5.5f);
            Use("배전실 사다리 빗장 풀기",11.5f,8.95f,()=>{
                if(Flag(unlocked)) {Say("사다리는 이미 내려와 있다.");return;}
                if(!Resting(port)) {Say("콘센트 자리에 붙으면 빗장에 손이 닿는다.");return;}
                SetFlag(unlocked);
                foreach(var step in steps) step.SetActive(true);
                rail.SetActive(true);
                AdminTint(hook,CabinetSafe);
                Say("입구와 상부 배전실이 사다리로 이어졌다.");
            });
        }
    }
}
