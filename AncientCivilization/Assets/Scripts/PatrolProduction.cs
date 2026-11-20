using UnityEngine;
using Plugin.AncientCivilization;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom : MonoBehaviour
    {
        // 전 구역의 시설은 생산구에서 쓰는 실제 기계 도트를 공유한다.
        void FactoryMachine(int prop, string label, float x, float y, float scale)
        {
            var art = AncientArt.Display(label, AncientArt.ProductionProp(prop),
                new Vector2(x, y), -2, transform);
            art.transform.localScale = new Vector3(scale, scale, 1);
        }

        void BuildProduction(int id)
        {
            switch (id)
            {
                case 1: BuildFreightDock(); break;
                case 2: BuildReversingBelt(); break;
                case 3: BuildSteamSteps(); break;
                case 4: BuildPressRoof(); break;
                case 5: BuildMagneticAssembly(); break;
                case 6: BuildBulbStore(); break;
                case 7: BuildToolCarousel(); break;
                case 8: BuildRejectChute(); break;
                case 9: BuildEmptyFreightTrain(); break;
                case 10: BuildNightServiceBay(); break;
            }
        }
        static readonly Color BeltSteel = new Color(.18f, .28f, .32f);
        static readonly Color SignalGold = new Color(1f, .76f, .30f);
        static readonly Color SignalGreen = new Color(.35f, .95f, .74f);

        void FactoryStairs(string name, float x, float y, int count, float rise = 1.2f, float stride = 1.65f)
        {
            for (int i = 0; i < count; i++)
                Deck(name + " " + (i + 1), x + i * stride, y + i * rise, 1.95f);
        }

        GameObject FactoryLamp(string name, float x, float y, Color color)
        {
            return Box(name, x, y, .32f, .32f, color, false, 4);
        }

        void FactoryWorker(int sprite, string name, float x, float y, System.Action speak)
        {
            var actor = AncientArt.Display(name, AncientArt.ProductionWorker(sprite),
                new Vector2(x, y), 5, transform);
            actor.transform.localScale = Vector3.one * .86f;
            Use(name, x, y + .85f, speak);
        }

        bool FactoryOnBelt(float a, float b, float top)
        {
            return !Zipping && Grounded && Hero.x >= a && Hero.x <= b && Mathf.Abs(Hero.y - top - .90f) < .32f;
        }

        void FactoryBelt(string label, float a, float b, float top, System.Func<float> speed)
        {
            Deck(label, (a + b) * .5f, top, b - a, .48f);
            int n = Mathf.Max(2, Mathf.FloorToInt((b - a) / 1.55f));
            var cleats = new GameObject[n];
            for (int i = 0; i < n; i++)
                cleats[i] = Box(label + " · 이송 리브", a, top - .14f, .40f, .12f, SignalGold, false, 5);
            float advance = 0;
            Step(dt => {
                float flow = speed();
                advance = Mathf.Repeat(advance + flow * dt, b - a);
                for (int i = 0; i < n; i++)
                    Place(cleats[i], a + Mathf.Repeat(advance + i * (b - a) / n, b - a), top - .14f);
                if (FactoryOnBelt(a, b, top))
                    Push(Mathf.MoveTowards(Velocity.x, Axis * 9f + flow, 72f * dt), Velocity.y);
            });
        }

        void BuildFreightDock()
        {
            FactoryMachine(4, "선박용 상차 호이스트", 13, 2, 1.15f);
            FactoryMachine(5, "도크 입고 화물", 35, 0, .9f);
            Deck("출항 검수대", 5, 2.2f, 5);
            FactoryBelt("낙하 화물 회수 벨트", 8, 35, .28f, () => 4.4f);
            const int count = 3;
            var hooks = new GameObject[count];
            float[] xs = { 10, 18, 26 };
            float[] ys = { 4.1f, 5.7f, 6.3f };
            for (int i = 0; i < count; i++) hooks[i] = Hook("천장 크레인 · 화물 고리 " + (i + 1), xs[i], ys[i]);
            Deck("도약판 아래 정비 발판", 26.5f, 3.1f, 3.4f);
            Deck("상부 반출구", 36.5f, 7.1f, 5.4f);
            FactoryStairs("벨트 하역 계단", 34.5f, 1.4f, 4);
            Sign("화물 고리 끝에서 점프  오른쪽 위", 22, 7.9f);
            FactoryWorker(0, "도크 수신호원", 6.5f, 0, () => Say("놓치면 벨트를 타세요. 세 번째 고리에서 발을 차면 저 반출구까지 한 번에 갑니다."));
            Portal("야간 정비소 직통문", 3.5f, 3.1f, 10, () => Flag("prod.dock.service"));
            bool linked = Flag("prod.dock.service");
            var serviceLamp = FactoryLamp("정비소 회신등", 3.5f, 4, SignalGreen);
            serviceLamp.SetActive(linked);
            SpriteRenderer courier = linked ? AncientArt.Display("직통문을 이용하는 교대조",
                AncientArt.ProductionWorker(1),new Vector2(3.5f,2.2f),5,transform) : null;
            float launchUntil = -1, launchedAt = -10;
            bool consumed = false, wasJump = false;
            Step(dt => {
                for (int i = 0; i < count; i++)
                {
                    float t = Elapsed * .86f + i * 1.7f;
                    Place(hooks[i], xs[i] + 1.35f * Mathf.Sin(t), ys[i] + .35f * Mathf.Cos(t));
                }
                bool arrival = Arrived(hooks[2]);
                if (arrival && !consumed) { consumed = true; launchUntil = Elapsed + .37f; }
                if (!arrival) consumed = false;
                if (Elapsed <= launchUntil && JumpHeld && !wasJump)
                {
                    launchedAt = Elapsed;
                    Push(17f, 14.6f);
                    launchUntil = -1;
                }
                float flight = Elapsed - launchedAt;
                if (flight < .65f && !Zipping && Axis >= -.1f)
                {
                    // 짧게 누른 점프도 고리 왼쪽 끝에서 반출대 앞 모서리까지 도달한다.
                    float vy = flight < .22f ? Mathf.Max(Velocity.y,14.6f-32f*flight) : Velocity.y;
                    Push(17f,vy);
                }
                if (courier)
                {
                    float walk=Mathf.PingPong(Elapsed*.8f,2.9f);
                    courier.transform.localPosition=new Vector2(3.3f+walk,2.2f);
                    courier.flipX=Mathf.Repeat(Elapsed*.8f,5.8f)>2.9f;
                }
                wasJump = JumpHeld;
            });
        }

        void BuildReversingBelt()
        {
            FactoryMachine(6, "유압 벨트 구동부", 8, 0, 1);
            bool reversed = Flag("prod.belt.reverse");
            FactoryBelt("양방향 이송 벨트", 7, 21, .36f, () => reversed ? 5.7f : -4.7f);
            var arrow = Box("벨트 방향 지시등", 7, 2.15f, .9f, .16f, SignalGold, false, 5);
            Use("역회전 레버", 5.1f, 1.15f, () => {
                reversed = !reversed; SetFlag("prod.belt.reverse", reversed);
                Say(reversed ? "반출 방향. 벨트 끝에서 뛰면 창을 건널 수 있다." : "반입 방향. 화물이 하역장으로 돌아간다.");
            });
            float gapY = 3.9f, clock = 0;
            var lower = Box("가동 격벽 하부", 24, 0, 1.22f, 6f, BeltSteel, true, 3);
            var upper = Box("가동 격벽 상부", 24, 10, 1.22f, 15f, BeltSteel, true, 3);
            Hook("열쇠구멍 너머 반출 고리", 30.5f, 4.2f);
            Deck("검사창 착지턱", 31, 2.9f, 5.2f);
            Deck("레버 점검 선반", 17, 2.1f, 3.2f);
            FactoryStairs("격벽 상부 우회 계단", 13, 3.4f, 4);
            Hook("상단 정비용 고리", 23, 12.1f);
            Deck("정비교 진입대", 21.6f, 10.1f, 3.2f);
            // 고정된 고리 쪽으로 조준하면 두 격벽 사이의 틈을 통과할 때만 직선 견인이 성립한다.
            Step(dt => {
                bool crossing = Mathf.Abs(Hero.x-24f) < 1.08f && Mathf.Abs(Hero.y-gapY) < 1.38f;
                if (!crossing) clock += dt;
                gapY = 3.75f + 1.72f * Mathf.Sin(clock * 1.08f);
                Place(lower, 24, gapY - 1.35f - 3);
                Place(upper, 24, gapY + 1.35f + 7.5f);
                Place(arrow, reversed ? 7.65f : 6.35f, 2.15f);
            });
            Sign("반출 검사창  •  초록 고리와 틈을 겹쳐 볼 것", 18, 6.9f);
        }

        void BuildSteamSteps()
        {
            FactoryMachine(1, "교번식 압력 탱크", 24.5f, 0, 1);
            bool steam = false;
            float age = 0;
            Use("증기 분배 밸브", 4.4f, 1.2f, () => {
                steam = !steam; age = 0;
                Say(steam ? "배관 가압. 흰 분출구는 올려 보내고 주황 배관은 옆으로 분다." : "밸브를 잠갔다. 운반대는 계속 운행한다.");
            });
            var slow = Deck("저속 광석 운반대", 9, 1.7f, 5.2f);
            var fast = Deck("급행 공구 운반대", 16, 3.3f, 4);
            Deck("급행 하차 부두", 29, 3.8f, 4.6f);
            Deck("상층 증기 계단", 37.5f, 7.9f, 5.5f);
            FactoryStairs("수동 점검 계단", 33, 1.3f, 4);
            var plumes = new GameObject[2];
            plumes[0] = Box("흡기탑 상승 증기", 28.7f, 3.7f, 1.8f, 6.5f, new Color(.67f,.89f,.91f,.38f));
            plumes[1] = Box("안전변의 횡분출", 34, 2.5f, 3.8f, 1.2f, new Color(.96f,.58f,.28f,.49f));
            var hot = FactoryLamp("황색 배관 예고등", 32.1f, 3.4f, SignalGold);
            Step(dt => {
                float t = Elapsed;
                Place(slow, 11 + 7f * Mathf.Sin(t * .47f), 1.52f);
                Place(fast, 21 + 9f * Mathf.Sin(t * .98f - .8f), 3.12f);
                if (steam) age += dt;
                float beat = Mathf.Repeat(age, 4.4f);
                bool rising = steam && beat < 1.9f, venting = steam && beat >= 2.24f && beat < 4.1f;
                plumes[0].SetActive(rising); plumes[1].SetActive(venting);
                hot.SetActive(steam && beat >= 1.9f && beat < 4.1f);
                if (Zipping) return;
                if (rising && Mathf.Abs(Hero.x - 28.7f) < 1.4f && Hero.y < 8f)
                    Push(Velocity.x, Mathf.Max(Velocity.y, 11.8f));
                if (venting && Mathf.Abs(Hero.x - 34) < 2.2f && Mathf.Abs(Hero.y - 2.5f) < 1.35f)
                    Push(Mathf.Min(Velocity.x, -7.8f), Mathf.Max(Velocity.y, 2));
            });
            FactoryWorker(1, "증기반 차장", 41, 0, () => Say("두 운반대가 만날 때 갈아타 봐요. 급행이 먼저 증기탑 앞에 섭니다."));
        }

        void BuildPressRoof()
        {
            FactoryMachine(0, "판재 프레스", 23.5f, 0, 1.28f);
            var head = Box("프레스 가동 금형", 23, 4.5f, 5.8f, 1.65f, BeltSteel, true, 4);
            var light = FactoryLamp("압착 전 경고등", 20, 5.6f, SignalGold);
            var hooks = new GameObject[] {
                Hook("순차 고리 1", 10.4f, 4.8f), Hook("순차 고리 2", 17.6f, 7.1f), Hook("순차 고리 3", 25.7f, 8.15f) };
            int stage = 0;
            void SelectRing(int index)
            {
                stage = index;
                for (int i = 0; i < hooks.Length; i++) hooks[i].SetActive(i == index);
            }
            SelectRing(0);
            Deck("하부 재시작 점검대", 15, 1.05f, 5);
            Deck("상부 검사대", 31, 6.1f, 6.2f);
            Hook("점검대 차단기 고리", 37, 5.1f);
            Use("고리 점검 회로 재가동", 15, 1.85f, () => { SelectRing(0); Say("첫 고리에 다시 불이 들어왔다."); });
            FactoryStairs("프레스 윗면 진입턱", 18.2f, 1.7f, 2);
            float clock = 0; bool dipped = false;
            Step(dt => {
                float angle = clock * 1.35f;
                float top = 3.6f + 2.3f * Mathf.Cos(angle);
                // 금형이 사람이 있는 하부 통로로 내려오면 안전변이 멈춘다.
                bool blocked = Mathf.Sin(angle) > 0 && Mathf.Abs(Hero.x-23)<3.25f &&
                    Hero.y < top-.75f && top-.825f-Hero.y < 1.08f;
                if (!blocked) clock += dt;
                Place(head, 23, top);
                light.SetActive(blocked || Mathf.Sin(angle) > .33f);
                if (stage < hooks.Length && Arrived(hooks[stage])) SelectRing(stage + 1);
                bool resetZone = Hero.x>=12.4f && Hero.x<=17.6f && Hero.y<2.6f && Grounded;
                if (resetZone && !dipped && stage>0) SelectRing(0);
                dipped = resetZone;
            });
            Sign("열린 금형은 아래로  /  닫힌 금형은 지붕으로", 18.4f, 4.45f);
        }

        void BuildMagneticAssembly()
        {
            FactoryMachine(3, "극성 반전 제어반", 5.2f, 0, .76f);
            bool magnet = Flag("prod.magnet.stairs");
            float morph = magnet ? 1 : 0;
            var plates = new GameObject[5];
            var free = new Vector2[5];
            var fixedToWall = new Vector2[5];
            for (int i = 0; i < plates.Length; i++)
            {
                free[i] = new Vector2(8.1f+i*2.12f, .48f + (i%3)*.22f);
                fixedToWall[i] = new Vector2(9.5f+i*2.13f, 1.18f+i*1.04f);
                plates[i] = Deck("자성 철판 " + (i+1), free[i].x, free[i].y, 2.85f);
            }
            var grate = Box("하부 통풍구 자석 덮개", 18.9f, 1.35f, 1.15f, 2.7f, BeltSteel, true, 5);
            Deck("통풍구 천장", 16.6f, 2.8f, 5.5f, .45f);
            var switchHead = FactoryLamp("극성 스위치", 6f, 1.3f, SignalGold);
            bool StrikeNearSwitch() => Near(6,1.3f,2.5f) && PrimaryHeld && expedition.Player.Mode == PlayerMode.Attack && !Zipping;
            void Flip()
            {
                magnet=!magnet; SetFlag("prod.magnet.stairs",magnet);
                Say(magnet?"철판이 전자석을 따라 벽에 정렬된다.":"철판이 바닥으로 풀린다. 하부 통풍구가 열렸다.");
            }
            Use("자석 스위치", 6, 1.3f, Flip);
            Deck("조립선 상부 이송대", 22.6f, 5.45f, 5.2f);
            FactoryBelt("양향 도약 벨트", 24.6f, 33.5f, 5.45f, () => 5.5f);
            Deck("장거리 도약 · 고철 분류실", 40.5f, 5.75f, 4.6f);
            Deck("역방향 도약 · 품질 검사실", 22.8f, 2.35f, 3.6f);
            FactoryWorker(2, "품질 검사원", 22, 2.35f, () => Say("벨트를 거슬러 뛰면 이 검사실에 내려와요. 앞쪽 분류대는 좀 더 멀죠."));
            Sign("장거리 분류선 ▶  /  ◀ 품질 검사", 30, 7.2f);
            bool previousStrike = false, onLaunch = false;
            float assisted = 0, speed = 0;
            Step(dt => {
                bool hit = StrikeNearSwitch() || BeamHits(switchHead);
                if (hit && !previousStrike) Flip();
                previousStrike = hit;
                morph = Mathf.MoveTowards(morph,magnet?1:0,dt*1.4f);
                for (int i = 0; i < plates.Length; i++)
                {
                    Vector2 p = Vector2.Lerp(free[i],fixedToWall[i],Mathf.SmoothStep(0,1,morph));
                    Place(plates[i],p.x,p.y-.175f);
                }
                if (!magnet || !Near(18.9f,1.35f,2)) grate.SetActive(magnet);
                bool standing = FactoryOnBelt(24.6f,33.5f,5.45f);
                if (onLaunch && !standing && Velocity.y>1 && !Zipping)
                { speed = Axis<-.2f ? -6.5f : 15.5f; assisted=.32f; }
                if (assisted>0 && !Zipping)
                { assisted-=dt; Push(speed,Velocity.y); }
                onLaunch = standing;
            });
        }

        void BuildBulbStore()
        {
            FactoryMachine(2, "절연 부품 창고", 4.3f, 0, .88f);
            var bulbs = new GameObject[3];
            Vector2[] from = {new Vector2(6,1.8f),new Vector2(9.35f,2.25f),new Vector2(12.8f,1.9f)};
            Color glass = new Color(.75f,.93f,.97f);
            for (int i=0;i<bulbs.Length;i++)
            {
                Vector2 p=from[i]; bulbs[i]=Box("폐전구 "+(i+1),p.x,p.y,.64f,.84f,glass,false,4);
                Box("전구 소켓",p.x,p.y+.54f,.54f,.27f,BeltSteel);
            }
            // 한 전구에서 세 입자가 튄 뒤 멈춘다. 우클릭으로 재접근하면 다시 수집 가능하다.
            const int perBulb=3;
            var sparks=new GameObject[bulbs.Length*perBulb];
            var motePosition=new Vector2[sparks.Length];
            var moteVelocity=new Vector2[sparks.Length];
            bool[] cracked = new bool[bulbs.Length], taken = new bool[sparks.Length];
            for (int j=0;j<sparks.Length;j++)
            { sparks[j]=Box("청색 유리 입자",0,0,.17f,.17f,new Color(.32f,.79f,1),false,7); sparks[j].SetActive(false); }
            bool powered=Flag("prod.store.lift");
            var lift=Deck("창고 전동 리프트",18, .40f,3.4f,.44f);
            float height=.40f;
            Use("리프트에 입자 3개 공급",16.25f,1.0f,()=>{
                if(powered){Say("전동 리프트는 두 정거장을 오갑니다.");return;}
                if(!SpendCharge(3)){Say("입자 세 개가 필요하다. 전구를 깨고 우클릭으로 모으거나 정비 사다리를 이용하자.");return;}
                powered=true; SetFlag("prod.store.lift"); Say("전력이 돌아왔다. 위 선반까지 운행한다.");
            });
            for(int i=0;i<6;i++) Deck("정비 사다리 디딤발", 13.8f+(i%2)*1.42f,1.22f+i*1.09f,1.8f);
            Deck("상부 재고량 검사대",21.8f,7.25f,5.5f);
            var slope=Box("오일 경사판",30,5.6f,11.5f,.55f,BeltSteel,true,3);
            slope.transform.localRotation=Quaternion.Euler(0,0,-14f);
            Deck("제동 시 진입하는 정렬 창구",30.2f,3.24f,4.6f);
            var middleGate=Box("중간 검사창 문",31,4.45f,.52f,2.3f,BeltSteel,true,5);
            Deck("활주 끝 반출구",39.1f,3.7f,5.4f);
            Use("정렬 창구 기록 확인",29.2f,4.1f,()=>Say("야간 교대 기록. 유리 분진이 끼면 감속 패드부터 닦는다."));
            Sign("오일 경사  ▼   A 제동 · D 활주",29,8.2f);
            bool braking=false, sliding=false;
            float slideSpeed=0, coast=0, gateOpen=0;
            Step(dt=>{
                for(int i=0;i<bulbs.Length;i++)
                {
                    if(cracked[i])continue;
                    Vector2 p=from[i];
                    bool cutting=PrimaryHeld && expedition.Player.Mode==PlayerMode.Attack && !Zipping && Near(p.x,p.y,2.2f);
                    if(!cutting&&!BeamHits(bulbs[i]))continue;
                    cracked[i]=true; bulbs[i].SetActive(false);
                    for(int k=0;k<perBulb;k++)
                    {
                        int n=i*perBulb+k; sparks[n].SetActive(true);
                        motePosition[n]=p;
                        moteVelocity[n]=new Vector2((k-1)*2.35f,2.3f+k*.43f);
                    }
                }
                for(int j=0;j<sparks.Length;j++)
                {
                    if(!cracked[j/perBulb]||taken[j])continue;
                    Vector2 p=motePosition[j], d=Hero-p;
                    if(CollectHeld&&d.sqrMagnitude<8*8)
                    {
                        moteVelocity[j]=Vector2.MoveTowards(moteVelocity[j],d.normalized*11,36*dt);
                        if(d.sqrMagnitude<.72f*.72f){taken[j]=true;sparks[j].SetActive(false);AddCharge(1);continue;}
                    }
                    else
                    {
                        moteVelocity[j].y-=12*dt;
                        if(p.y<=.26f&&moteVelocity[j].y<0)moteVelocity[j]=Vector2.zero;
                    }
                    p+=moteVelocity[j]*dt; p.y=Mathf.Max(.24f,p.y);
                    motePosition[j]=p; Place(sparks[j],p.x,p.y);
                }
                float destination=powered ? .4f+6.85f*(.5f-.5f*Mathf.Cos(Elapsed*.7f)) : .4f;
                height=Mathf.MoveTowards(height,destination,dt*4); Place(lift,18,height-.22f);
                float deckY=5.6f-(Hero.x-30f)*.249f+.275f;
                bool landed=Grounded&&!Zipping&&Hero.x>=24.5f&&Hero.x<=35.5f&&Mathf.Abs(Hero.y-.90f-deckY)<.5f;
                if(landed&&!sliding){slideSpeed=Mathf.Abs(Velocity.x)>.8f?Velocity.x:4; braking=false;}
                if(landed)
                {
                    if(Axis*Mathf.Sign(slideSpeed)<-.25f)
                    {
                        slideSpeed=Mathf.MoveTowards(slideSpeed,0,dt*25);braking=true;
                        if(Mathf.Abs(slideSpeed)<2f&&Hero.x>26&&Hero.x<32){gateOpen=1.4f;}
                    }
                    else slideSpeed=Mathf.MoveTowards(slideSpeed,braking&&Mathf.Abs(Axis)<.2f?0:9.6f,dt*11);
                    Push(slideSpeed,Velocity.y);
                    if(gateOpen>0&&Mathf.Abs(slideSpeed)<1.1f&&Hero.x>27&&Hero.x<31)
                    {Warp(29.4f,4.22f); gateOpen=2f;}
                }
                if(sliding&&!landed&&!braking&&Hero.x>34&&slideSpeed>2)coast=.31f;
                if(coast>0&&!Zipping){coast-=dt;Push(Mathf.Max(Velocity.x,10.5f),Velocity.y);}
                sliding=landed;
                gateOpen=Mathf.Max(0,gateOpen-dt);
                if(gateOpen>0||!Near(31,4.45f,1.6f))middleGate.SetActive(gateOpen<=0);
            });
        }

        void BuildToolCarousel()
        {
            FactoryMachine(0,"원형 조립 공구대",19,0,1.15f);
            var cradle=Deck("회전대 탑승 바스켓",12,2.2f,3.15f,.44f);
            var hub=Box("회전대 축",19,3,.7f,.7f,SignalGold,false,3);
            var spoke=Box("회전대 지지암",15,3,6,.23f,BeltSteel,false,1);
            Deck("공구함 뒷방",10.1f,5.75f,5.2f);
            var cabinet=Box("공구함의 잠금쇠",9.8f,6.6f,.52f,.42f,SignalGold,false,4);
            bool claimed=Flag("prod.tools.cache");cabinet.SetActive(!claimed);
            Use("뒷면 공구함 열기",9.8f,6.65f,()=>{
                if(!claimed){claimed=true;SetFlag("prod.tools.cache");cabinet.SetActive(false);AddCharge(2);Say("공구함 뒤에서 낡은 정비 쪽지와 입자 두 개를 찾았다.");}
                else Say("공구함 쪽지에는 회전대의 점검 주기가 남아 있다.");
            });
            Deck("안쪽 문 착지대",27,3.5f,4.5f);
            // 낮은 보 사이에서 짧게 두 번 넘고, 보가 끝난 지점에서 길게 뛰어 높은 턱에 오른다.
            Box("검척 터널 천장",33.1f,3.67f,7.6f,.64f,BeltSteel,true);
            Box("측정턱 1",31.2f,.36f,.62f,.72f,BeltSteel,true);
            Box("측정턱 2",34.6f,.40f,.75f,.80f,BeltSteel,true);
            Box("높이 선별 출고턱",40,1.35f,3.5f,2.7f,BeltSteel,true);
            Sign("검척 터널  •  짧게 두 번, 끝에서 길게",30,4.9f);
            Sign("공구함은 안쪽 문 반대편",14,3.8f);
            Step(dt=>{
                float angle=Elapsed*.70f + 2.75f;
                Vector2 p=new Vector2(19+6.7f*Mathf.Cos(angle),3.5f+2.45f*Mathf.Sin(angle));
                Place(cradle,p.x,p.y-.22f);
                Vector2 root=new Vector2(19,3.5f), delta=p-root;
                Place(spoke,(root.x+p.x)*.5f,(root.y+p.y)*.5f);
                spoke.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                hub.transform.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
            });
        }

        void BuildRejectChute()
        {
            FactoryMachine(4,"반품 분류 호이스트",11,1,1);
            FactoryStairs("분류통 계단",4.5f,1.24f,5,1.28f,1.72f);
            Deck("불량품 투입구",13.8f,7.35f,4.2f);
            var chute=Box("미끄럼 배출통",21,4.26f,13f,.5f,BeltSteel,true);
            chute.transform.localRotation=Quaternion.Euler(0,0,-25f);
            var crates=new GameObject[3];
            for(int i=0;i<crates.Length;i++)
                crates[i]=Box("불량품 상자 "+(i+1),0,0,1.05f,.86f,new Color(.58f,.43f,.28f),false,4);
            Hook("분류통 샛길 손잡이",23.7f,7.05f);
            Deck("가로채기 검수 통로",27.7f,6.1f,5.2f);
            var bumper=Hook("완충 범퍼 고리",34.5f,5.7f);
            var face=Box("좌상향 압축 범퍼",35.05f,5.18f,1.55f,.37f,SignalGold,true,6);
            face.transform.localRotation=Quaternion.Euler(0,0,43);
            Deck("반동 포집대",27.4f,9.6f,5.2f);
            Sign("왼쪽 위 완충 범퍼   •   접촉 뒤에만 방향 전환",30,8);
            FactoryWorker(2,"반품 검수원",27,9.6f,()=>Say("불량품은 아래로 흘리고, 걸러낼 상자는 이쪽에서 받아요. 방금처럼요."));
            Deck("정상 반송 통로",31.2f,1.15f,6.2f);
            bool launched=false;
            float bounce=0;
            Step(dt=>{
                for(int i=0;i<crates.Length;i++)
                {
                    float rate=Mathf.Repeat(Elapsed*.175f+i/3f,1);
                    float x=Mathf.Lerp(15.1f,27.2f,rate);
                    Place(crates[i],x,4.26f-(x-21)*.466f+.68f);
                    if(!Zipping&&Mathf.Abs(x-Hero.x)<.73f&&Mathf.Abs((4.26f-(x-21)*.466f+1f)-Hero.y)<.8f)
                        Push(Mathf.Max(Velocity.x,4.7f),Velocity.y);
                }
                if(!Zipping&&Grounded&&Hero.x>15&&Hero.x<27&&
                    Mathf.Abs(Hero.y-(4.26f-(Hero.x-21)*.466f+1.16f))<.55f)
                    Push(Mathf.MoveTowards(Velocity.x,Axis<-.5f?1.2f:8,22*dt),Velocity.y);
                bool arrived=Arrived(bumper);
                if(arrived&&!launched)
                { launched=true; bounce=.34f; Push(-12.4f,16.6f); }
                if(!arrived)launched=false;
                if(bounce>0&&!Zipping){bounce-=dt; Push(-12.4f,Velocity.y);}
            });
        }

        void BuildEmptyFreightTrain()
        {
            float[] cars={10,20,30};
            Color carriage=new Color(.17f,.29f,.32f);
            FactoryMachine(5,"정차한 화차의 수하물",40.6f,0,.62f);
            for(int i=0;i<cars.Length;i++)
            {
                float x=cars[i];
                Box("빈 객차 외판",x,2,7.5f,4f,carriage,false,-1);
                Deck("객차 지붕 "+(i+1),x,4.35f,7.6f);
                Box("창틀",x-1.7f,2.7f,2.3f,.95f,new Color(.3f,.52f,.55f),false,1);
                Box("차체 식별선",x,1.75f,7.30f,.1f,SignalGold,false,0);
                Box("연결기",x+4.1f,.94f,.85f,.28f,BeltSteel,false,0);
                for(int wheel=0;wheel<2;wheel++)
                {
                    float wx=x+(wheel==0?-2.5f:2.5f);
                    var axle=Box("차축",wx,.38f,.69f,.69f,BeltSteel,false,1);
                    axle.transform.localRotation=Quaternion.Euler(0,0,45);
                    Box("차축 중심",wx,.38f,.22f,.22f,SignalGold,false,2);
                }
                Box("차내 수송 상자",x-1,.44f,1.1f,.88f,BeltSteel,true);
            }
            Hook("기중기 탑승 고리",7,6.05f);
            Hook("지붕 사이 비상 고리",24,6.5f);
            Deck("최후미 반환 승강장",42.75f,4.35f,5.5f);
            var bridge=Box("반환 연결교",36.9f,6.6f,6.2f,.45f,BeltSteel,true,4);
            bool linked=Flag("prod.train.bridge"), fast=Flag("prod.train.fast"), stopped=Flag("prod.train.mid");
            float swing = linked ? 1 : 0;
            void RestoreBridge()
            {
                linked=true; SetFlag("prod.train.bridge"); Say("연결핀이 잠겼다. 마지막 객차에서 승강장으로 양방향 통행이 가능하다.");
            }
            Use("연결핀 레버",32,1.15f,RestoreBridge);
            Sign("차내 연결핀 ▼  /  지붕 직통 ▶",23.6f,5.6f);
            var plates=new GameObject[3];
            float[] xs={9.8f,15.2f,20.8f};
            for(int i=0;i<plates.Length;i++)
                plates[i]=Box("속도 계전판 "+(i+1),xs[i],.14f,1.2f,.25f,SignalGold,false,4);
            var gate=Box("직통 검사구 셔터",25.8f,1.46f,.7f,2.9f,BeltSteel,true);
            var middle=Box("중간 검수실 문",13.2f,5.7f,.75f,2.94f,BeltSteel,true);
            Use("첫 객차 검수용 승강구",10.1f,1.05f,()=>{
                if (!stopped) {Say("주황 압력판 한가운데서 멈춰야 승강구가 열린다.");return;}
                Warp(14.6f,5.36f); Say("검수실 창으로 나오니 두 객차의 지붕이 보인다.");
            });
            Deck("검수실 창문 발코니",14.6f,4.4f,2.3f);
            var lamps=new[] {FactoryLamp("검수실 개방등",13.2f,7.35f,SignalGreen),FactoryLamp("직통구 개방등",26,3.4f,SignalGreen)};
            var bridgeLamps=new[] {FactoryLamp("후미 연결 완료",33.6f,5.05f,SignalGreen),FactoryLamp("승강장 연결 완료",40.2f,5.05f,SignalGreen)};
            int nextPlate=0;
            float stopTime=0, deadline=0;
            bool standingStart=false;
            var plateLamps=new SpriteRenderer[plates.Length];
            for(int i=0;i<plates.Length;i++)plateLamps[i]=plates[i].GetComponent<SpriteRenderer>();
            Step(dt=>{
                swing=Mathf.MoveTowards(swing,linked?1:0,dt*.84f);
                float angle=Mathf.Lerp(83,0,Mathf.SmoothStep(0,1,swing))*Mathf.Deg2Rad;
                bridge.transform.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
                Place(bridge,33.6f+3.1f*Mathf.Cos(angle),4.12f+3.1f*Mathf.Sin(angle));
                foreach(var lamp in bridgeLamps)lamp.SetActive(linked);
                bool heel=Grounded&&Hero.y<2.15f, atFirst=heel&&Mathf.Abs(Hero.x-xs[0])<.85f;
                if(atFirst && Mathf.Abs(Velocity.x)<.85f)
                {
                    stopTime+=dt;
                    if(stopTime>.42f&&!stopped)
                    {stopped=true;SetFlag("prod.train.mid");Say("첫 계전판의 주황등이 멎었다. 중간 검수실 개방.");}
                }
                else stopTime=0;
                if(nextPlate>0&&(Elapsed>deadline||Velocity.x<-.5f))nextPlate=0;
                if(atFirst&&!standingStart&&Velocity.x>=5.4f){nextPlate=1;deadline=Elapsed+2.15f;}
                standingStart=atFirst;
                if(nextPlate>0&&heel&&Mathf.Abs(Hero.x-xs[nextPlate])<.8f&&Velocity.x>=4.6f)
                {
                    nextPlate++;
                    if(nextPlate==xs.Length)
                    {
                        if(!fast)Say("세 계전판이 차례로 울렸다. 반출 직통구 개방.");
                        fast=true;SetFlag("prod.train.fast"); nextPlate=0;
                    }
                }
                for(int i=0;i<3;i++)
                    plateLamps[i].color=(i<nextPlate||fast)?SignalGreen:SignalGold;
                gate.SetActive(!fast);middle.SetActive(!stopped);lamps[0].SetActive(stopped);lamps[1].SetActive(fast);
            });
            FactoryWorker(0,"차량 검사원",39.5f,0,()=>Say(Flag("prod.train.bridge")?"이제 마지막 칸에서 돌아오는 사람도 건너옵니다.":"지붕으로만 가면 연결 레버를 지나칩니다. 세 번째 객차 안을 보세요."));
        }

        void BuildNightServiceBay()
        {
            FactoryMachine(4,"야간 점검용 주행 크레인",14,3,1.22f);
            FactoryMachine(2,"정비조 탈의실",6,0,.84f);
            var cradle=Deck("가동 크레인 바닥",15,3.55f,4,.54f);
            var hook=Hook("크레인 발판 손잡이",15,4.95f);
            Deck("고층 수리대",23.3f,6.15f,4.6f);
            var lockDoor=Box("수리대 방호문",21.3f,7.52f,.72f,2.75f,BeltSteel,true,4);
            var green=FactoryLamp("크레인 정지 확인등",21.2f,9.2f,SignalGreen);
            float stopUntil=-1, phase=.4f;
            bool repaired=Flag("prod.dock.service");
            FactoryWorker(1,"야간 정비사",6.6f,0,()=>{
                stopUntil=Elapsed+13f;
                Say("백 경위, 13초간 크레인 정지! 정비 발판을 올라 수리대에서 도크 쪽 잠금핀을 풀어요.");
            });
            FactoryStairs("비상 정비 발판",14.6f,1.15f,4,1.26f,1.6f);
            Use("도크 쪽 잠금핀",23.3f,7.15f,()=>{
                if(Elapsed>stopUntil&&!repaired){Say("가동 중에는 잠금핀을 만질 수 없다.");return;}
                repaired=true;SetFlag("prod.dock.service"); Say("도크에서 녹색 응답이 돌아왔다. 양쪽 문을 사용할 수 있다.");
            });
            Portal("하역 도크 정비문",25.1f,7.12f,1,()=>repaired);
            var returnLamp=FactoryLamp("도크 연결 표시등",25.2f,8.75f,SignalGreen);
            FactoryStairs("배출갱 올라가는 계단",26,6.8f,4,1.30f,1.65f);
            Deck("수직 배출구 개구부",32.3f,10.85f,3.1f);
            Box("배출갱 외판",33.4f,5.4f,.54f,10.6f,BeltSteel,true);
            var side=Hook("낙하 중 옆으로 환승하는 고리",40.3f,6.95f);
            Deck("환승 정비 회랑",41.7f,5.55f,5.8f);
            Deck("배출구 안전 매트",36.7f,.32f,5.6f,.42f);
            Sign("옆 고리 ▶ 정비 회랑   /   ▼ 안전 매트",35.7f,11.7f);
            bool sawTransfer=false;
            Step(dt=>{
                bool halt=Elapsed<stopUntil;
                if(!halt)phase+=dt;
                float x=14.5f+4.5f*Mathf.Sin(phase*.78f);
                Place(cradle,x,3.35f); Place(hook,x,4.95f);
                green.SetActive(halt);returnLamp.SetActive(repaired);
                if(halt||!Near(21.3f,7.52f,1.85f))lockDoor.SetActive(!halt&&!repaired);
                if(Arrived(side)&&!sawTransfer){sawTransfer=true;Say("달아오른 배출관 옆으로 착지했다. 정비 회랑은 출구와 곧장 이어진다.");}
            });
        }
    }
}
