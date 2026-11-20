using System;
using UnityEngine;
using Plugin.AncientCivilization;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom : MonoBehaviour
    {
        static readonly Color SummitIron = new Color(.18f,.26f,.36f);
        static readonly Color SummitSilver = new Color(.55f,.72f,.80f);
        static readonly Color SummitSun = new Color(1f,.69f,.32f);
        static readonly Color SummitBeacon = new Color(.32f,.90f,.97f);

        void BuildSummit(int id)
        {
            switch (id)
            {
                case 41: SummitRailCar(); break;          // 98번 정비차 질주가 이 옥상에서 이어진다.
                case 42: SummitAwning(); break;           // 94번 난간 아래 낙하
                case 43: SummitFoldingFacade(); break;    // 97번 접히는 다리 릴레이
                case 44: SummitSolarPanels(); break;      // 92번 중간 지붕 생략
                case 45: SummitAntenna(); break;          // 91번 네 번의 견인
                case 46: SummitCloudLift(); break;        // 96번 돌아오는 고리
                case 47: SummitCableExchange(); break;    // 93번 움직이는 고리 교차
                case 48: SummitWindmill(); break;         // 95번 코너 브레이크
                case 49: SummitStation(); break;          // 99번 무착지 도전선
                case 50: SummitObservation(); break;      // 100번 세 갈래 도약
            }
        }

        // 실제 옥상 도트를 난간 길이에 맞추고, 밟는 면은 동일 좌표의 일방향 충돌체에 둔다.
        void SummitRoof(string name,float x,float top,float width)
        {
            Deck(name,x,top,width);
            var sprite = AncientArt.Rooftop(Mathf.Min(width,6),false);
            if (!sprite) return;
            var art = AncientArt.Display(name + " · 외장 패널",sprite,new Vector2(x,top-.06f),-1,transform);
            art.transform.localScale = new Vector3(width/sprite.bounds.size.x,1.3f,1);
        }

        GameObject SummitGirder(string name,float x,float y,float length,float angle)
        {
            var part = Box(name,x,y,length,.12f,SummitSilver,false,-2);
            part.transform.localRotation = Quaternion.Euler(0,0,angle);
            return part;
        }

        bool SummitOccupiesGate(float x,float y,float halfHeight)
        {
            return Mathf.Abs(Hero.x-x)<.82f && Mathf.Abs(Hero.y-y)<halfHeight+.96f;
        }

        void SummitFastAccess()
        {
            SummitRoof("출입 발코니",4,2.2f,5);
            SummitRoof("층계참",8,4.3f,3);
        }

        void SummitRailCar()
        {
            SummitFastAccess();
            SummitRoof("서쪽 정비동 옥상",10,6,8);
            SummitRoof("맞은편 옥상",36,7.2f,9);
            SummitRoof("케이블 점검로",24,2.4f,15);
            SummitGirder("무인차 철골 레일",24,9.8f,41,0);
            for (int i=0;i<7;i++) SummitGirder("대각 보강재",6+i*6,8.8f,4,i%2==0?32:-32);
            var chassis=Box("뒤편 레일 정비차",5,10.25f,4,1.05f,SummitIron,false,-1);
            var cab=Box("정비차 관제창",5,10.8f,2.2f,.68f,SummitBeacon,false,0);
            var bogieA=Box("서쪽 차륜",5,9.85f,.58f,.57f,SummitSilver,false,0);
            var bogieB=Box("동쪽 차륜",5,9.85f,.58f,.57f,SummitSilver,false,0);
            var bridge=Deck("정비차 전개교",22,6.8f,9);
            var hooks=new GameObject[5];
            for (int i=0;i<hooks.Length;i++) hooks[i]=Hook("순차 전개 전방 고리 "+(i+1),10+i*6,8.3f+(i%2)*1.1f);
            float carX=6, idle=0;
            bool running=false;
            Step(dt=>{
                if(!running && Hero.x>4 && Hero.x<14) {running=true; idle=0;}
                if(running) carX=Mathf.Min(43,carX+dt*7.9f);
                if(carX>=43) {idle+=dt; if(idle>3 && Hero.x<13){running=false;carX=6;idle=0;}}
                Place(chassis,carX,10.25f); Place(cab,carX+.36f,10.81f);
                Place(bogieA,carX-1.35f,9.85f); Place(bogieB,carX+1.35f,9.85f);
                // 화면 앞쪽으로 내려오는 점검교. 이미 올라탄 동안에는 접지 않는다.
                bool aboard=bridge.activeSelf && Mathf.Abs(Hero.x-bridge.transform.localPosition.x)<4.9f && Hero.y>6 && Hero.y<9;
                bridge.SetActive((carX>=17 && carX<=34)||aboard);
                if(bridge.activeSelf) Place(bridge,Mathf.Clamp(carX,18,30),6.63f);
                for(int i=0;i<hooks.Length;i++)
                    hooks[i].SetActive((i==0||carX>=8+i*5.45f) && (carX<26+i*6||Zipping));
            });
            Hook("점검로 복귀 고리",39,5.2f);
            Sign("앞차의 흰 창이 켜진 고리로 이어진다",7,6.8f);
        }

        void SummitAwning()
        {
            SummitFastAccess();
            SummitRoof("풍향 난간",11,6.4f,8); SummitRoof("역풍 끝 절벽",34,6.4f,10);
            SummitRoof("방풍 점검로",22,1.5f,16);
            Hook("바깥 장거리 도약 고리",21.5f,9.7f); Hook("순풍 수신 고리",32.2f,9.3f);
            Hook("내벽 세로 홈",13,4.4f); Hook("바람막이 아래 고리",22,4.6f); Hook("천막 안쪽 고리",29.5f,5.7f);
            var pennants=new GameObject[4];
            for(int i=0;i<pennants.Length;i++)
            {
                float x=11+i*6;
                Box("천막 계류 기둥",x,7.7f,.13f,3.7f,SummitSilver,false,-2);
                pennants[i]=Box("직조 바람 자루",x,9.1f,2.8f,.45f,i%2==0?SummitSun:SummitBeacon,false,-1);
            }
            Step(dt=>{
                float wind=Mathf.Sin(Elapsed*1.18f);
                for(int i=0;i<pennants.Length;i++)
                {
                    float flutter=wind+.12f*Mathf.Sin(Elapsed*7+i);
                    Place(pennants[i],11+i*6+1.2f*wind,9+.12f*Mathf.Abs(wind));
                    pennants[i].transform.localRotation=Quaternion.Euler(0,0,flutter*18);
                }
                if(!Zipping && !Grounded && Hero.x>12 && Hero.x<32 && Hero.y>5.85f)
                    Push(Mathf.Clamp(Velocity.x+wind*dt*32,-16,19),Velocity.y);
            });
            Sign("북서풍 / 낮은 난간 안쪽은 무풍",13,5.1f);
        }

        void SummitFoldingFacade()
        {
            SummitFastAccess(); SummitRoof("외벽 출입턱",9,5.1f,7); SummitRoof("조립동 반대편",39,5.1f,8);
            SummitRoof("난간 아래 케이블 점검로",24,1.8f,22);
            var leaves=new GameObject[4]; var lamps=new SpriteRenderer[4]; var angles=new float[4];var timers=new float[4];
            bool reverse=false;
            for(int i=0;i<leaves.Length;i++)
            {
                float hinge=12.5f+i*5.4f;
                leaves[i]=Box("외벽 양면 날개 "+(i+1),hinge+2.5f,5,5,.38f,SummitSilver,true);
                lamps[i]=Box("힌지 경고등",hinge,4.7f,.37f,.37f,SummitBeacon,false,3).GetComponent<SpriteRenderer>();
                Hook("벽 상단 정비 고리 "+(i+1),hinge+.9f,10.7f);
            }
            Action reset=()=>{reverse=!reverse;for(int i=0;i<timers.Length;i++)timers[i]=0;
                Say(reverse?"서쪽 귀환 설정. 날개가 반대편 경첩으로 접힌다.":"동쪽 통과 설정. 밟은 날개부터 접힌다.");};
            Use("경첩 방향 전환",37,5.9f,reset); Use("케이블 경첩 제어",20,2.6f,reset);
            Step(dt=>{
                for(int i=0;i<leaves.Length;i++)
                {
                    float left=12.5f+i*5.4f;
                    if(timers[i]<=0 && Grounded && Hero.x>left && Hero.x<left+5 && Mathf.Abs(Hero.y-6.15f)<.72f) timers[i]=9;
                    if(timers[i]>0)timers[i]=Mathf.Max(0,timers[i]-dt);
                    float target=timers[i]>0 && timers[i]<8.50f?88:0;
                    angles[i]=Mathf.MoveTowards(angles[i],target,dt*110);
                    float rad=angles[i]*Mathf.Deg2Rad,side=reverse?-1:1,hinge=reverse?left+5:left;
                    Place(leaves[i],hinge+side*2.5f*Mathf.Cos(rad),5+2.5f*Mathf.Sin(rad));
                    leaves[i].transform.localRotation=Quaternion.Euler(0,0,side*angles[i]);
                    lamps[i].color=timers[i]>0?SummitSun:SummitBeacon;
                }
            });
            Sign("경고등이 켜지면 다음 날개 끝에서 점프",8,6.3f);
        }

        void SummitSolarPanels()
        {
            SummitFastAccess(); SummitRoof("서측 전지판 기단",9,6,7);
            SummitRoof("중앙 전지판 기단",23,6.2f,3.2f); SummitRoof("동편 인버터 옥상",37,6.1f,10);
            Hook("첫 집광탑 견인 고리",15.5f,8.6f); Hook("중앙 감속 고리",23.8f,8.1f); Hook("반대편 옥상 고리",31.7f,8.2f);
            var panels=new GameObject[3];var shades=new GameObject[3];var lamps=new SpriteRenderer[3];
            for(int i=0;i<3;i++)
            {
                float x=16+i*7;
                panels[i]=Box("회전 전지판",x,5.6f,5.1f,.31f,new Color(.25f,.43f,.65f),true);
                shades[i]=Box("움직이는 패널 그늘",x,1.7f,6.2f,3.2f,new Color(.10f,.17f,.28f,.48f),false,-1);
                lamps[i]=Box("일사량 경고",x,.22f,1.1f,.12f,SummitBeacon,false,3).GetComponent<SpriteRenderer>();
            }
            float glare=0;
            Step(dt=>{
                bool shade=false;
                for(int i=0;i<3;i++)
                {
                    float x=16+i*7,tilt=Mathf.Sin(Elapsed*1.18f+i*.47f),shadowX=x-tilt*3;
                    panels[i].transform.localRotation=Quaternion.Euler(0,0,tilt*28);
                    Place(shades[i],shadowX,1.7f);
                    if(Mathf.Abs(Hero.x-shadowX)<3.15f&&Hero.y<3.7f)shade=true;
                }
                bool exposed=Hero.x>12 && Hero.x<33 && Hero.y<4.6f && !shade;
                glare=Mathf.Clamp(glare+dt*(exposed?1:-1.9f),0,2.7f);
                for(int i=0;i<lamps.Length;i++)lamps[i].color=Color.Lerp(SummitBeacon,SummitSun,glare/2.6f);
                if(glare>=2.6f){glare=0;Warp(Hero.x<23?10:35,1.15f);Say("일사 경보. 점검 그늘에서 차광기가 복구됐다.");}
            });
            Sign("상단 배선로 / 첫 견인 관성으로 중앙 기단 생략 가능",8,7.1f);
        }

        void SummitAntenna()
        {
            SummitFastAccess(); SummitRoof("송신탑 하부 데크",12,4.6f,7);
            SummitRoof("중계탑 중간 참",27,7.1f,5); SummitRoof("뒤쪽 방송실 창턱",19.5f,10.8f,5);
            SummitRoof("주파수 점검창",34,10.6f,9);
            for(int i=0;i<3;i++)
            {
                float x=13+i*9;
                Box("송신탑 기둥",x,6.6f,.30f,13.2f,SummitIron,false,-2);
                for(int j=0;j<4;j++) SummitGirder("안테나 횡빔",x,4+j*2.7f,2.6f-.38f*j,0);
            }
            var sockets=new[]{Hook("① 낮은 급전 고리",10.5f,5.4f),Hook("② 먼 수신 고리",22.3f,6.2f),
                Hook("③ 위쪽 첨탑 고리",27.7f,12.6f),Hook("④ 뒤편 창문 고리",19.6f,12.6f)};
            int next=0;
            for(int i=1;i<4;i++)sockets[i].SetActive(false);
            Step(dt=>{
                if(next>=sockets.Length||!Arrived(sockets[next]))return;
                next++;
                if(next<sockets.Length)sockets[next].SetActive(true);
                else{SetFlag("summit.broadcast.window");Say("반대편 창턱에 옛 방송국 표찰이 걸려 있다.");}
            });
            Portal("뒤돌아 본 방송실 창문",19.5f,11.8f,49,()=>Flag("summit.broadcast.window"));
            Sign("수신 확인 뒤 다음 흰 고리 점등",9,2.7f);
        }

        void SummitCloudLift()
        {
            SummitRoof("구름 승강장",10,2.2f,7); SummitRoof("기압계 측실",23,5.5f,8.6f);
            SummitRoof("상부 조율실",25,9.2f,9); SummitRoof("꼭대기 승강장",19.2f,12.4f,6.9f);
            SummitRoof("맞은편 통신 첨탑",37,9.3f,7); SummitRoof("하부 계단",33,4,6);
            Box("승강기 안내 레일",14.9f,7,.22f,14,SummitSilver,false,-2);
            var lift=Deck("순환식 구름 승강기",15.2f,1.1f,4.3f);
            var lowDoor=Box("기압계 층 방풍문",19.1f,7.05f,.42f,3.0f,SummitIron,true);
            var highDoor=Box("조율실 층 방풍문",20.6f,10.75f,.42f,3,SummitIron,true);
            var returning=Hook("구름 사이로 돌아오는 정비 고리",23,9.6f);
            Hook("원격 기둥 고리",36,11.8f);
            Use("측실 압력계 검침",25,6.5f,()=>{SetFlag("summit.pressure");Say("창문 밖 구름이 층계참과 같은 높이다.");});
            float cycle=0;
            Step(dt=>{
                cycle=Mathf.Repeat(cycle+dt,14);
                float phase=cycle/14*Mathf.PI*2,top=1.1f+(1-Mathf.Cos(phase))*5.66f;
                Place(lift,15.2f,top-.175f);
                lowDoor.SetActive(Mathf.Abs(top-5.5f)>1.6f&&!SummitOccupiesGate(19.1f,7.05f,1.5f));
                highDoor.SetActive(Mathf.Abs(top-9.2f)>1.55f&&!Near(20.6f,10.75f,1.5f));
                float sweep=(1-Mathf.Cos(Elapsed*1.18f))*.5f;
                Place(returning,16.4f+11.2f*sweep,9.4f+sweep*1.65f);
            });
            Sign("운전 간격 14초 / 2층 기압계 · 3층 조율실",8,2.9f);
        }

        void SummitCableExchange()
        {
            SummitFastAccess(); SummitRoof("외벽 출발 베이",10,5.8f,8);
            SummitRoof("교차로 동부 베이",38,6.2f,10); SummitRoof("실패 회수 난간",24,2,19);
            SummitGirder("북쪽 현수 레일",16,9.5f,17,12);
            SummitGirder("남쪽 현수 레일",31,10.5f,17,-12);
            var west=Hook("서측 이동 고리",10,7.5f);var east=Hook("동측 이동 고리",37,8.9f);
            var junction=Box("환승 접점 표시",24,10.2f,.6f,.30f,SummitSilver,false,4).GetComponent<SpriteRenderer>();
            Hook("아래 점검 난간 복귀 고리",39,5.8f);
            Step(dt=>{
                float wave=(1-Mathf.Cos(Elapsed*1.31f))*.5f;
                float ax=9.6f+12.7f*wave,bx=37.4f-12.3f*wave;
                Place(west,ax,7.45f+wave*2.65f);Place(east,bx,8.55f+wave*2.65f);
                junction.color=bx-ax<6?SummitSun:SummitSilver;
            });
            Sign("황색 접점 / 반대 레일 전달 가능",10,6.75f);
        }

        void SummitWindmill()
        {
            SummitFastAccess(); SummitRoof("풍력 통제교",19.5f,4.1f,17);
            SummitRoof("좁은 코너 정비턱",31.8f,6.65f,2.7f);SummitRoof("귀환 빗장 손잡이",27.5f,8.6f,4);
            SummitRoof("날개 끝 상부 출구",39,6.7f,7); SummitRoof("회전문 하부 배수로",32,1.55f,9);
            Hook("긴 견인",22.8f,7.6f); Hook("코너 진입",30.85f,8.45f); Hook("직행 관성 고리",39.7f,10.65f);
            Hook("뒤로 꺾는 고리",26.7f,10.25f);
            var vaneA=Box("통제교 날개 A",17.9f,6.1f,5.25f,.46f,SummitSilver,false,2);
            var vaneB=Box("통제교 날개 B",17.9f,6.1f,5.25f,.46f,SummitSun,false,1);
            Box("날개 중심축",17.9f,6.1f,.72f,.74f,SummitIron,false,4);
            var upper=Box("상부 회전문",29,6.7f,.60f,4.3f,SummitIron,true);
            var lower=Box("하부 회전문",29,2.05f,.60f,3.8f,SummitIron,true);
            bool east=false,wasTouching=false;float bearing=0;
            Action spin=()=>{east=!east;Say(east?"상부 회전문이 열렸다.":"아래 배수로가 열렸다.");};
            Use("풍차 날개 밀기",18,4.9f,spin);
            Use("양방향 빗장 해제",27.5f,9.6f,()=>{SetFlag("summit.windmill.open");Say("귀환 걸쇠를 풀었다. 통제교 두 경로가 열렸다.");});
            Step(dt=>{
                bool touching=Near(17.9f,5.1f,1.6f)&&Mathf.Abs(Velocity.x)>5;
                if(touching&&!wasTouching&&!Flag("summit.windmill.open"))spin();
                wasTouching=touching;
                bool latched=Flag("summit.windmill.open");
                upper.SetActive(!latched&&!east&&!SummitOccupiesGate(29,6.7f,2.15f));
                lower.SetActive(!latched&&east&&!SummitOccupiesGate(29,2.05f,1.9f));
                bearing=Mathf.MoveTowardsAngle(bearing,east?135:45,100*dt);
                vaneA.transform.localRotation=Quaternion.Euler(0,0,bearing);
                vaneB.transform.localRotation=Quaternion.Euler(0,0,bearing+90);
            });
            Sign("동쪽으로 계속 견인 / 코너 난간은 뒤쪽 빗장실로",23,5.2f);
        }

        void SummitStation()
        {
            SummitFastAccess(); SummitRoof("방송국 도전선 발진대",9,5.7f,6);
            SummitRoof("비상 케이블 통로",24,2,21);SummitRoof("방송국 수신 데크",39,6.2f,8);
            SummitRoof("방송 조정실",29.5f,4.5f,6);
            float[] xs={7,12,19,25.5f,32,39,44.2f};
            float[] ys={5,7.9f,10,8.4f,10.95f,8.7f,9.1f};
            for(int i=0;i<xs.Length;i++)Hook("방송 파형 고리 "+(i+1),xs[i],ys[i]);
            Box("송신 주파수 눈금",29.4f,5.9f,4,.5f,SummitIron,false,0);
            var needle=Box("조율 바늘",27.9f,6,.12f,.72f,SummitSun,false,1);
            var radio=new GameObject("방송실 수신기").AddComponent<AudioSource>();radio.transform.SetParent(transform,false);
            radio.playOnAwake=false;radio.loop=false;radio.spatialBlend=0;radio.volume=.28f;
            var audioProfile=Resources.Load<AncientFeedbackProfile>("Audio/FieldFeedback");
            if(audioProfile)radio.outputAudioMixerGroup=audioProfile.effects;
            var clips=new[]{LivingReedTune(),Resources.Load<AudioClip>("Audio/MetalFall"),Resources.Load<AudioClip>("Audio/ArchiveGlass")};
            string[] discoveries={"living.marketLaundry","prod.tools.cache","admin.repository"};
            string[] hints={"물 흐르는 소리 틈으로 손풍금이 샌다. 열린 세탁소 창을 찾아봐.","공구 회전대의 뒤쪽을 지날 때만 빈 철통이 울린다.","잠긴 서고에선 유리 울림이 짧고, 바람 새는 빈 방에선 길어."};
            int channel=-1;
            Use("무전 수신 주파수 맞추기",29.4f,5.4f,()=>{
                bool unheard=!Flag(discoveries[0])||!Flag(discoveries[1])||!Flag(discoveries[2]);
                for(int i=0;i<clips.Length;i++)
                {
                    channel=(channel+1)%clips.Length;
                    if(!unheard||!Flag(discoveries[channel]))break;
                }
                Place(needle,27.9f+1.4f*channel,6);Say(hints[channel]);
                radio.Stop();radio.clip=clips[channel];if(radio.clip && AncientGame.Saved("muted")==0)radio.Play();
            },2.25f);
            Box("무착지 결승 수신선",46,8.8f,.12f,3.7f,SummitBeacon,false,2);
            bool launched=false,completed=false;
            Step(dt=>{
                if(radio.isPlaying)
                    radio.volume=AncientGame.Saved("muted")!=0?0:.28f*Mathf.Clamp01(1-Vector2.Distance(Hero,new Vector2(29.4f,5.4f))/12f);
                if(!TrialActive){launched=completed=false;return;}
                if(completed)return;
                if(!launched)
                {
                    if(Hero.x>10){completed=true;return;}
                    if(!Grounded && (Velocity.y>1.1f || Zipping))launched=true;
                    return;
                }
                if(Grounded){completed=true;launched=false;Say("하부 케이블 통로로 계속 완주할 수 있다.");}
                else if(Hero.x>=46){completed=true;launched=false;SetFlag("summit.airline");Say("방송국에 무착지 신호 도달. 비행선 기록을 남겼다.");}
            });
            Sign("상부 무착지 기록선 / 아래 통로에도 수신기가 있다",8,6.5f);
        }

        void SummitObservation()
        {
            SummitFastAccess(); SummitRoof("해 뜨는 관측교 출발대",13,6.4f,9);
            SummitRoof("상공 전망대",30,11.25f,10); SummitRoof("옆 방송실 출입구",38.8f,7.75f,8);
            SummitRoof("귀환 승강기",25.3f,2.25f,8); SummitRoof("도시 연결 보행교",36,3.55f,9);
            Hook("관측교 공통 발사 고리",17.5f,8.8f);Hook("상공 수신 고리",25.1f,13);
            Hook("방송실 중계 고리",28.8f,8.7f); Hook("승강기 하부 고리",21.7f,4.6f);
            for(int i=0;i<5;i++)
            {
                float x=10+i*6, height=2.6f+i%3*1.25f;
                Box("아래로 내려다보이는 층의 실루엣",x,height*.5f,4.8f,height,
                    Color.Lerp(SummitIron,SummitSilver,i*.085f),false,-3);
                for(int j=0;j<3;j++)Box("건너편 창의 아침빛",x-1.55f+j*1.4f,height-.6f,.58f,.18f,SummitSun,false,-2);
            }
            for(int district=0;district<4;district++)
            {
                var vista=AncientArt.DistrictBackdrop(district);
                if(!vista)continue;
                // 전망창마다 아래로 보이는 도시의 실제 원경 도트를 사용한다.
                float x=10.2f+district*8f,y=1.8f+district*.27f;
                var window=AncientArt.Display("전망창 · "+AncientCityLayout.Name(district),vista,new Vector2(x,y),-2,transform);
                window.transform.localScale=new Vector3(6.7f/vista.bounds.size.x,2.7f/vista.bounds.size.y,1);
                window.color=new Color(.74f,.82f,.91f,.8f);
                Box("황동 전망창 난간",x,y+1.4f,6.9f,.09f,SummitSun,false,-1);
            }
            // 개별 문은 각 층의 실제 순찰 공간을 양 끝으로 가진다.
            Portal("생산구 · 귀환 화물 승강기",22.5f,3.2f,1);
            Portal("생활부 · 야시장 연결문",27.5f,3.2f,11);
            Portal("행정구 · 전산탑 배선교",34,4.5f,30);
            Portal("거주구 · 무인 전망대",30,12.2f,38);
            Portal("최상층 · 방송실",38.8f,8.7f,49);
            Portal("최상층 · 서쪽 쌍둥이 옥상",42.8f,1.1f,41);
            Sign("위 · 전망대     옆 · 방송실     아래 · 각 층 귀환",13,7.45f);
        }
    }
}
