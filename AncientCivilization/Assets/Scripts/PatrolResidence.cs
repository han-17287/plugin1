using UnityEngine;
using Plugin.AncientCivilization;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom : MonoBehaviour
    {
        static readonly Color ResStone=new Color(.24f,.31f,.34f);
        static readonly Color ResIvory=new Color(.81f,.79f,.65f);
        static readonly Color ResTeal=new Color(.30f,.74f,.72f);
        static readonly Color ResWater=new Color(.12f,.54f,.68f,.56f);
        static readonly Color ResMoss=new Color(.30f,.50f,.36f);
        static readonly Color ResGlass=new Color(.38f,.62f,.67f,.65f);
        static readonly Color ResBrass=new Color(.81f,.59f,.29f);

        void BuildResidence(int id)
        {
            switch(id)
            {
                case 31: ResidenceGarden(); break;
                case 32: ResidenceCanal(); break;
                case 33: ResidenceBalconies(); break;
                case 34: ResidenceChapel(); break;
                case 35: ResidenceTower(); break;
                case 36: ResidenceCleaner(); break;
                case 37: ResidenceBelfry(); break;
                case 38: ResidenceObservatory(); break;
                case 39: ResidenceOldBridge(); break;
                case 40: ResidenceLaundry(); break;
            }
        }
        // 물결과 부력을 같은 주기로 움직여 수면을 보며 화물에 오를 순간을 읽는다.
        static float ResidenceTide(float seconds,float period)
        {
            return .5f-.5f*Mathf.Cos(2*Mathf.PI*seconds/period);
        }

        void ResidenceWater(float x,float width,float low,float high,float period)
        {
            var pool=Box("수조 · 유리 너머의 물",x,low/2,width,1,ResWater,false,-3);
            var crest=Box("수면 눈금",x,low,width,.10f,ResTeal,false,-2);
            var bodyOfWater=pool.GetComponent<SpriteRenderer>();
            Step(dt=>{
                float h=Mathf.Lerp(low,high,ResidenceTide(Elapsed,period));
                bodyOfWater.size=new Vector2(width,h);
                pool.transform.localPosition=new Vector3(x,h/2,0);
                crest.transform.localPosition=new Vector3(x,h,0);
            });
        }

        GameObject ResidenceRaft(string name,float x,float width,float low,float high,float period)
        {
            const float depth=.55f;
            var raft=Deck(name,x,low,width,depth);
            var stripe=Box("화물 흘수선",x,low-depth+.08f,width-.28f,.1f,ResIvory,false,4);
            Step(dt=>{
                float top=Mathf.Lerp(low,high,ResidenceTide(Elapsed,period));
                Place(raft,x,top-depth/2);
                stripe.transform.localPosition=new Vector3(x,top-depth+.08f,0);
            });
            return raft;
        }

        void ResidencePlanter(float x,float y,float width)
        {
            Box("정원 화단",x,y+.22f,width,.44f,ResStone,false,-1);
            for(int i=0;i<5;i++)
            {
                float across=(i/4f-.5f)*(width-.6f);
                float height=.4f+(i%3)*.17f;
                var leaf=Box("산책로 수풀",x+across,y+.45f+height/2,.3f,height,ResMoss,false,-2);
                leaf.transform.localRotation=Quaternion.Euler(0,0,18*(i%3-1));
            }
        }

        void ResidenceWindow(float x,float y,Color pane)
        {
            Box("주거동 창틀",x,y,2.9f,2.3f,ResStone,false,-5);
            Box("주거동 창",x,y,2.55f,1.93f,pane,false,-4);
            Box("창살",x,y,.09f,1.93f,ResStone,false,-3);
        }

        // 풍향을 실내의 장식에서도 읽을 수 있도록 발생 위치에 방향이 고정된 깃을 단다.
        void ResidenceVent(float x,float y,float width,float height,float vx,float vy,System.Func<bool> enabled=null)
        {
            var opening=Box("환기구",x,y,width,height,new Color(.31f,.67f,.66f,.12f),false,-6);
            int count=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(width,height)/3f),2,8);
            for(int i=0;i<count;i++)
            {
                bool vertical=Mathf.Abs(vy)>Mathf.Abs(vx);
                float along=(i+.5f)/count-.5f;
                float px=x+(vertical?0:along*width),py=y+(vertical?along*height:0);
                float angle=Mathf.Atan2(vy,vx)*Mathf.Rad2Deg;
                for(int blade=-1;blade<=1;blade+=2)
                {
                    Vector2 offset=Quaternion.Euler(0,0,angle)*new Vector3(-.18f,.10f*blade,0);
                    var vane=Box("풍향 깃",px+offset.x,py+offset.y,.47f,.07f,ResTeal,false,-2);
                    vane.transform.localRotation=Quaternion.Euler(0,0,angle-28*blade);
                }
            }
            var tint=opening.GetComponent<SpriteRenderer>();
            Step(dt=>{
                bool on=enabled==null || enabled();
                if(tint) tint.color=on?new Color(.31f,.67f,.66f,.18f):new Color(.25f,.3f,.32f,.08f);
                Vector2 h=Hero;
                if(!on || Zipping || Mathf.Abs(h.x-x)>width/2 || Mathf.Abs(h.y-y)>height/2) return;
                Vector2 v=Velocity;
                if(vy>0) v.y=Mathf.Max(v.y,vy);
                if(vy<0) v.y=Mathf.Min(v.y,vy);
                if(vx!=0) v.x=Mathf.MoveTowards(v.x,vx+Axis*4,46*dt);
                Push(v.x,v.y);
            });
        }

        void ResidenceGarden()
        {
            ResidencePlanter(6,0,6); ResidencePlanter(30,0,8);
            ResidenceVent(12,7.75f,4,15.5f,0,7.5f);
            Deck("중간 높이의 숨은 정원",17,6.7f,5.8f);
            ResidencePlanter(18,6.7f,2.6f);
            AncientArt.Display("테라스 관리인",AncientArt.Citizen(15),new Vector2(18,6.70f),5,transform);
            Use("테라스 관리인",18,7.65f,()=>{
                SetFlag("residence.gardenTerrace");
                Say("정원사  ·  꼭대기까지 갈 필요 없어요. 바람 곁에 씨앗이 쌓이거든요.");
            });
            Hook("천장 옆 정원 고리",22,11.5f);
            Deck("정원 배수교",26,8.6f,6);
            Hook("온실 동편 배관",33,10.8f);
            Deck("온실 난간",37.5f,6.9f,7);
            Hook("동쪽 계단 고리",43,5.5f);
            Sign("산책로는 아래로  ·  전망 정원은 상승류 옆으로",5,2);
        }

        void ResidenceCanal()
        {
            const float period=11;
            FactoryMachine(6,"수로 환수 펌프",39,.05f,.84f);
            ResidenceWater(24,27,.55f,6.9f,period);
            ResidenceRaft("부력 화물 · 서쪽 운반함",15.2f,4.8f,.72f,7.15f,period);
            ResidenceRaft("부력 화물 · 동쪽 짐함",34.4f,4.4f,.86f,7.30f,period);
            Deck("수문 위 다리",26,7.6f,9);
            Box("정비실 바닥",25.5f,3.63f,7,.40f,ResStone,true);
            Box("정비실 천장",25.5f,6.53f,7,.38f,ResStone,true);
            Box("정비실 끝벽",28.85f,5.08f,.36f,2.78f,ResStone,true);
            var maintenance=Box("중간 수위 정비문",22.15f,5.02f,.38f,2.7f,ResBrass,true,4);
            Box("낮은 수로의 천장",22.3f,2.71f,7.6f,.36f,ResStone,true);
            var sluice=Box("수위 연동 배수문",20.4f,1.28f,.49f,2.56f,ResSteelBlue(),true,4);
            bool hatchOpen=false;
            Step(dt=>{
                float t=ResidenceTide(Elapsed,period);
                Vector2 h=Hero;
                // 수위가 바뀌어도 문턱에 선 플레이어를 끼우거나 정비실 안에 가두지 않는다.
                bool inside=h.x>22.42f && h.x<29 && h.y>3.6f && h.y<6.5f;
                bool inDoor=Mathf.Abs(h.x-22.15f)<.73f && h.y>3.6f && h.y<6.5f;
                hatchOpen=t>=.35f && t<=.67f || inside || hatchOpen && inDoor;
                maintenance.SetActive(!hatchOpen);
                sluice.SetActive(t>.31f && !(Mathf.Abs(h.x-20.4f)<.8f && h.y<3.25f));
            });
            Use("수위 정비 일지",26,4.85f,()=>{SetFlag("residence.canalLog");Say("정비 일지  ·  둘째 눈금에서만 정비문이 열린다. 밀물 때는 화물 위로.");});
            ResidenceVent(6.8f,4.8f,3.3f,9.6f,0,7);
            ResidenceVent(12.4f,8.85f,10.4f,2.1f,9,0);
            Hook("교차풍 끝의 배관 고리",21,9.75f);
            Deck("동쪽 제방",41,5.9f,6.2f);
            Sign("낮은 물에는 수문 아래  ·  밀물에는 화물 위",10.8f,2.2f);
        }
        static Color ResSteelBlue()=>new Color(.22f,.42f,.49f);

        void ResidenceBalconies()
        {
            LivingWall(0,1,12,0,.94f); LivingWall(0,0,38,0,.86f);
            ResidenceWindow(11,4,ResGlass); ResidenceWindow(18,7.1f,ResBrass);
            ResidenceWindow(10,10.4f,new Color(.07f,.14f,.17f));
            Deck("서쪽 빈집 발코니",10.5f,2.0f,5.3f);
            Deck("거주 중인 동쪽 창",17,4.4f,4.7f);
            Deck("서쪽의 열린 창",11.8f,6.95f,5.5f);
            Hook("창문 사이의 계단 고리",17.8f,9.8f);
            Deck("연결 발코니",19.6f,8.5f,6);
            AncientArt.Display("발코니 입주민",AncientArt.Citizen(17),new Vector2(18,4.4f),2,transform);
            Use("옆집에 노크",17.5f,5.45f,()=>Say("입주민  ·  불 꺼진 창은 비었어요. 녹색 차양이 달린 집엔 발판이 접혀 있고요."));
            bool windowOpen=Flag("residence.greenWindow");
            var balcony=Deck("펴지는 차양 발판",25.7f,8.0f,5);
            balcony.SetActive(windowOpen);
            Use("녹색 차양 손잡이",20.6f,9.3f,()=>{windowOpen=!windowOpen;SetFlag("residence.greenWindow",windowOpen);balcony.SetActive(windowOpen);});
            ResidenceWater(32,8,.5f,6.50f,8);
            ResidenceRaft("창 사이 떠오르는 운반함",32,3.6f,.66f,6.60f,8);
            Box("화물 위 낮은 처마",32,8.75f,5,.50f,ResStone,true);
            Deck("중간 수위 맞은편 창턱",38,5.45f,4.8f);
            ResidenceWindow(38,6.7f,ResGlass);
            Use("창턱의 화물표",38,6.4f,()=>{SetFlag("residence.windowManifest");Say("물자 운반 기록  ·  물이 반쯤 찼을 때 창문을 열어 줄 것.");});
            Sign("처마 아래까지 차기 전에 창턱으로",28.5f,2);
        }

        void ResidenceChapel()
        {
            int face=0;
            var glass=Box("회전 색유리",12,6.3f,1.25f,5.0f,ResGlass,false,2);
            var light=Box("바닥으로 내려오는 빛",14,2.3f,.58f,4.2f,new Color(.83f,.72f,.28f,.46f),false,-2);
            var trays=new GameObject[3]; var lightMarks=new GameObject[3];
            Color[] colors={new Color(.91f,.67f,.24f),new Color(.38f,.81f,.69f),new Color(.66f,.61f,.92f)};
            // 장미창의 납살과 여섯 색유리 잎. 바닥의 유도광과 같은 세 색을 반복한다.
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;
                var border=Box("장미창 납살",12+Mathf.Cos(a)*1.72f,8.3f+Mathf.Sin(a)*1.72f,
                    1.1f,2.15f,ResStone,false,-7);
                var petal=Box("장미창 모자이크",12+Mathf.Cos(a)*1.72f,8.3f+Mathf.Sin(a)*1.72f,
                    .82f,1.82f,colors[i%3],false,-6);
                border.transform.localRotation=petal.transform.localRotation=Quaternion.Euler(0,0,i*60-90);
            }
            for(int i=0;i<3;i++)
            {
                trays[i]=Deck("빛에 나타나는 석제 계단",15.7f+4*i,2.2f+1.45f*i,4.35f);
                lightMarks[i]=Box("색유리 반사 눈금",15.7f+4*i,.1f,2.8f,.13f,colors[i],false,3);
            }
            System.Action refract=()=>{
                glass.GetComponent<SpriteRenderer>().color=colors[face];
                glass.transform.localRotation=Quaternion.Euler(0,0,7+face*13);
                light.transform.localPosition=new Vector3(14.2f+face*1.9f,3.0f,0);
                light.transform.localRotation=Quaternion.Euler(0,0,18+face*10);
                light.GetComponent<SpriteRenderer>().color=new Color(colors[face].r,colors[face].g,colors[face].b,.40f);
                for(int i=0;i<3;i++){trays[i].SetActive(i<=face);lightMarks[i].SetActive(i<=face);}
            };
            refract();
            Use("색유리 회전 손잡이",9.8f,1.2f,()=>{face=(face+1)%3;refract();});
            Deck("성가대 회랑",28,6.4f,5);
            Use("유리 장인의 서명",28.5f,7.3f,()=>{SetFlag("residence.chapelLight");Say("유리 장인의 서명  ·  오늘도 해가 계단 셋을 먼저 올랐다.");});
            ResidenceWater(36,12,.35f,2.8f,9);
            for(int i=0;i<3;i++) ResidenceRaft("세례 수로의 부표 "+(i+1),31.6f+i*4.1f,2.8f,.55f+i*.38f,2.9f+i*1.05f,9);
            Deck("수로 끝 문지방",44,4.85f,4);
            Hook("유리 아치 고리",38,8.7f);
        }
        void ResidenceTower()
        {
            FactoryMachine(1,"급수관 압력 탱크",6,.05f,.78f);
            bool emptied=Flag("residence.tankEmpty");
            float balance=emptied?1:0;
            Box("서쪽 급수탑",11,5,4.6f,10,ResStone,false,-5);
            Box("동쪽 급수탑",22.5f,5,4.6f,10,ResStone,false,-5);
            var gauge=Box("배수 탱크 수위계",11,5,2.4f,1,ResWater,false,-3);
            var liquidGauge=gauge.GetComponent<SpriteRenderer>();
            var counter=Deck("급수탑의 균형추",20.1f,2.4f,4.5f,.5f);
            var tank=Deck("반대쪽 물탱크 받침",13,7.9f,4.4f,.5f);
            Step(dt=>{
                balance=Mathf.MoveTowards(balance,emptied?1:0,dt*.32f);
                Place(counter,20.1f,2.15f+5.5f*balance);
                Place(tank,13,7.65f-5.5f*balance);
                float depth=Mathf.Lerp(7.5f,1.0f,balance);
                liquidGauge.size=new Vector2(2.4f,depth);
                gauge.transform.localPosition=new Vector3(11,1+depth/2,0);
            });
            System.Action drain=()=>{emptied=!emptied;SetFlag("residence.tankEmpty",emptied);Say(emptied?"물이 빠진다. 반대편 균형추가 올라간다.":"급수 밸브가 열렸다.");};
            Use("급수 밸브",8.6f,1.2f,drain);
            Use("균형추 위 원격 밸브",20.1f,3.25f,drain);
            Deck("탑 전망대",26.8f,8.4f,7);
            Hook("전망대 난간",25.6f,10.85f);
            ResidenceVent(32.6f,5.3f,3.6f,10.6f,0,-11);
            Hook("급강하 통로 옆 고리",38,4.1f);
            Deck("압력 점검 창",39.6f,3.2f,5);
            Use("배관공의 표식",40,4.25f,()=>{SetFlag("residence.downVent");Say("바닥의 흰 선을 넘기 전에 옆 배관을 잡는다. 배관공의 이동 수칙이다.");});
            Sign("동쪽 전망대  ·  하강 환풍구",28.5f,9.5f);
        }

        void ResidenceCleaner()
        {
            FactoryMachine(4,"외벽 와이어 권양기",12.4f,10.2f,.69f);
            Deck("청소대 탑승 계단",7,1.8f,4.4f);
            var cradle=Deck("외벽 청소 작업대",13,3.7f,5,.5f);
            var socket=Hook("청소대 바깥쪽 견인점",15,6.1f);
            var cable=Box("청소대 윈치",13,10.3f,.09f,11,ResIvory,false,-2);
            var cableStrip=cable.GetComponent<SpriteRenderer>();
            Step(dt=>{
                float phase=ResidenceTide(Elapsed,10);
                float x=13+13*phase, top=3.7f+3.2f*phase;
                Place(cradle,x,top-.25f);
                Place(socket,x+2,top+2.4f);
                float length=15.8f-top;
                cableStrip.size=new Vector2(.09f,length);
                cable.transform.localPosition=new Vector3(x,(15.8f+top)/2,0);
            });
            Deck("거꾸로 뛰어드는 창턱",16.2f,6.95f,3.2f);
            ResidenceWindow(16.2f,8.0f,ResBrass);
            Use("창틀의 쪽지",16.2f,7.95f,()=>{SetFlag("residence.washerWindow");Say("반대편으로 뛰어 이 창문까지 와 줬구나. 화분에 물은 내가 줄게.");});
            Hook("외벽 점검 레일",29,9.8f);
            var west=Deck("청소 도르래 서쪽 발판",31.0f,7.1f,3.6f,.44f);
            var east=Deck("청소 도르래 동쪽 발판",36.2f,2.65f,3.6f,.44f);
            float offset=.15f;
            Step(dt=>{
                float westTop=Mathf.Lerp(7.1f,2.35f,offset),eastTop=Mathf.Lerp(2.65f,7.4f,offset);
                if(ResidenceStanding(31,westTop,3.6f)) offset=Mathf.MoveTowards(offset,1,.39f*dt);
                else if(ResidenceStanding(36.2f,eastTop,3.6f)) offset=Mathf.MoveTowards(offset,0,.39f*dt);
                Place(west,31,Mathf.Lerp(7.1f,2.35f,offset)-.22f);
                Place(east,36.2f,Mathf.Lerp(2.65f,7.4f,offset)-.22f);
            });
            Deck("청소부 장비 창고",42,6.4f,5);
            Sign("마주 오는 받침을 향해 뛰세요",29,8.5f);
        }
        bool ResidenceStanding(float x,float top,float width)
        {
            Vector2 h=Hero;
            return Grounded && Mathf.Abs(h.x-x)<width*.5f+.15f &&
                h.y>top+.43f && h.y<top+1.32f;
        }

        static AudioClip residenceBell;
        void PrepareResidenceBell(float x,float y)
        {
            if(!residenceBell)
            {
                const int rate=22050;
                var pcm=new float[rate*9/10];
                // 두 거주층 곡이 공유하는 E와 B로 종을 조율한다. 부분음은 E4의 1·2·3배다.
                const double fundamental=329.6275569;
                double[] tones={fundamental,fundamental*2,fundamental*3}, gain={.36,.15,.055};
                for(int n=0;n<tones.Length;n++)
                {
                    double angle=2*System.Math.PI*tones[n]/rate;
                    double factor=2*System.Math.Cos(angle), prev=-System.Math.Sin(angle), cur=0;
                    for(int i=0;i<pcm.Length;i++)
                    {
                        pcm[i]+=(float)(gain[n]*cur);
                        double next=factor*cur-prev;prev=cur;cur=next;
                    }
                }
                double decay=System.Math.Exp(-6.5/rate), envelope=1;
                for(int i=0;i<pcm.Length;i++)
                {
                    double attack=System.Math.Min(i*110.0/rate,1);
                    double release=System.Math.Min((pcm.Length-1-i)*32.0/rate,1);
                    pcm[i]*=(float)(attack*envelope*release);
                    envelope*=decay;
                }
                residenceBell=AudioClip.Create("거주구 종탑",pcm.Length,1,rate,false);residenceBell.SetData(pcm,0);
            }
            CreateSpeaker("종탑 종소리",x,y,52,.22f);
        }

        void ResidenceBelfry()
        {
            var striker=Box("종탑의 종",11.2f,9.3f,1.25f,1.55f,ResBrass,false,-1);
            PrepareResidenceBell(11.2f,8.4f);
            var first=Hook("새 그림자 속 첫 고리",15.2f,8.0f);
            var second=Hook("맞은편 창의 점검 고리",30.2f,8.7f);
            var birds=new GameObject[6];
            for(int i=0;i<birds.Length;i++)
            {
                birds[i]=Box("창살에 앉은 새",15.2f,8,.54f,.16f,ResStone,false,5);
                for(int wing=-1;wing<=1;wing+=2)
                {
                    var feather=Box("새의 날개",15.2f+wing*.22f,8.12f,.44f,.095f,ResStone,false,5);
                    feather.transform.localRotation=Quaternion.Euler(0,0,wing*31);
                    feather.transform.SetParent(birds[i].transform,true);
                }
                birds[i].transform.localRotation=Quaternion.Euler(0,0,i%2==0?18:-16);
            }
            Deck("서쪽 탑 받침",10,2.2f,5);
            Deck("고리 아래 착지 창대",18.5f,4.35f,6.4f);
            // 전면 차폐벽이 첫 고리에서 반대편 고리까지의 시선을 가린다. 아래 좁은 창은 열린다.
            Box("높은 창 사이 칸막이",23,10.85f,.7f,7.4f,ResStone,true);
            Deck("오른쪽 종루 회랑",33.2f,6.8f,6);
            Hook("종루 바깥 고리",39.4f,6.15f);
            float cycle=4.7f;bool called=false, tookFirst=false, landed=false, holdHook=false;
            first.SetActive(false);second.SetActive(false);
            Step(dt=>{
                cycle+=dt;
                if(cycle>=7.1f){cycle-=7.1f;called=false;}
                bool flown=cycle<3.1f;
                if(flown&&!called){called=true;PlayRoomCue(residenceBell,false);}
                striker.transform.localRotation=Quaternion.Euler(0,0,flown?Mathf.Sin(cycle*10)*Mathf.Exp(-cycle*2)*13:0);
                if(Arrived(first)) tookFirst=true;
                if(tookFirst && ResidenceStanding(18.5f,4.35f,6.4f)) landed=true;
                // 소리가 끊겨도 이미 도착한 와이어는 지우지 않는다.
                holdHook=Zipping && (holdHook||flown);
                first.SetActive(flown||holdHook);
                second.SetActive(landed);
                float scatter=flown?Mathf.Sin(Mathf.PI*cycle/3.1f):0;
                for(int i=0;i<birds.Length;i++)
                {
                    float side=i%2==0?-1:1;
                    birds[i].transform.localPosition=new Vector3(15.2f+side*(.15f+i*.12f)+side*scatter*(2+i*.6f),8.10f+(i%3)*.17f+scatter*(2.35f+i*.1f),0);
                }
            });
            Use("종 밧줄",8.5f,1.2f,()=>{cycle=0;called=false;});
            Sign("종이 울리면 새들이 난다  ·  아래 창대를 한 번 딛어 보세요",8,3.45f);
        }

        void ResidenceObservatory()
        {
            int facing=0;
            var scope=Box("회전 망원경 경통",24,7.0f,2.4f,.38f,ResBrass,false,3);
            Box("삼각대",24,6.3f,.23f,1.1f,ResStone,false,3);
            Deck("망원경 전망 데크",24,5.65f,9);
            Hook("망원경 계단 손잡이",19,8.5f);
            string[] routes={"수로","야시장","전산탑"};
            var lamps=new GameObject[3];
            var vistas=new[]{AncientArt.DistrictBackdrop(3),AncientArt.DistrictBackdrop(1),AncientArt.DistrictBackdrop(2)};
            Box("망원경 투영 유리틀",24,10.2f,7.5f,3.55f,ResStone,false,-8);
            var view=AncientArt.Display("망원경에 비친 구역",vistas[0],new Vector2(24,10.2f),-7,transform);
            for(int i=0;i<3;i++)
            {
                float x=32+i*5.1f;
                ResidenceWindow(x,2.1f,ResGlass);
                lamps[i]=Box("방향 관측등",x,3.6f,.52f,.25f,ResIvory,false,3);
            }
            System.Action point=()=>{
                scope.transform.localRotation=Quaternion.Euler(0,0,16-facing*13);
                view.sprite=vistas[facing];
                var size=view.sprite.bounds.size;
                view.transform.localScale=new Vector3(7.16f/size.x,3.2f/size.y,1);
                for(int i=0;i<3;i++) lamps[i].GetComponent<SpriteRenderer>().color=i==facing?ResBrass:ResStone;
            };
            point();
            Use("망원경 방향 바꾸기",23.8f,6.7f,()=>{
                facing=(facing+1)%3;point();
                Say(facing==0?"수로  ·  물때 눈금 아래 청록색 정비문이 보인다.":facing==1?"야시장  ·  천막 뒤 셔터의 노란 등, 여기가 그 문 밖이다.":"전산탑  ·  배선교 밖으로 관측 통로가 이어진다.");
            });
            Portal(routes[0]+" 관측 통로",32,1.15f,32,()=>facing==0);
            Portal(routes[1]+" 관측 통로",37.1f,1.15f,11,()=>facing==1);
            Portal(routes[2]+" 관측 통로",42.2f,1.15f,30,()=>facing==2);
            ResidenceWater(12,12,.4f,4.0f,10);
            // 같은 마루가 물 위에서는 수평이 되고, 물이 빠지면 낮은 곳으로 이어지는 계단이 된다.
            for(int i=0;i<5;i++)
            {
                int n=i;float x=7+i*2.38f;
                var slat=Deck("수로 배수교의 연결 판",x,4.3f,2.46f,.36f);
                Step(dt=>{
                    float water=ResidenceTide(Elapsed,10);
                    float top=4.3f-n*.72f*(1-water);
                    Place(slat,x,top-.18f);
                    slat.transform.localRotation=Quaternion.Euler(0,0,-10*(1-water));
                    if(water<.45f && Axis>.15f && ResidenceStanding(x,top,2.46f))
                        Push(Mathf.Max(Velocity.x,6.8f),Velocity.y);
                });
            }
            Deck("서쪽 지지대",4.7f,2.0f,3);
        }
        void ResidenceOldBridge()
        {
            Deck("구름다리 서쪽 계단",5.8f,2.0f,3.9f);
            Deck("구름다리 서쪽 받침",10.4f,4.4f,4.1f);
            Deck("서쪽 건물 지붕",14.2f,7.15f,5.6f);
            Hook("서쪽 교대 고리",14.8f,9.35f);
            // 서쪽 축은 고정되어 있고 끊어진 중앙 이음판만 잠깐 사라진다.
            const float hingeX=16.8f, hingeY=7.15f;
            var planks=new GameObject[6];
            for(int i=0;i<planks.Length;i++)
                planks[i]=Deck("구름다리 경첩 마루",hingeX+(i+.5f)*2.25f,hingeY,2.29f,.34f);
            var rivet=Box("경첩 경고등",hingeX,hingeY+.2f,.35f,.35f,ResBrass,false,4);
            float sag=Flag("residence.bridgeLowered")?1:0,rumble=0;
            bool fractured=sag>0;
            Deck("추락을 받아주는 지붕",26.2f,2.8f,15);
            Deck("동쪽 수리대",34.7f,3.85f,4.4f);
            var shield=Box("이동 방풍판",34,2.7f,.6f,4.0f,ResIvory,true);
            float shieldX=34;
            var pennant=Box("바람 세기 깃발",45,4,.9f,.22f,ResTeal,false,-1);
            Step(dt=>{
                if(!fractured && ResidenceStanding(23.65f,hingeY,8.4f))
                {fractured=true;SetFlag("residence.bridgeLowered");Say("가운데 볼트가 빠졌다. 아래 건물 지붕이 바로 밑에 있다.");}
                if(fractured)
                {
                    rumble+=dt;
                    if(rumble>.42f) sag=Mathf.MoveTowards(sag,1,1.25f*dt);
                    float rad=-19.5f*Mathf.Deg2Rad*sag;
                    for(int i=0;i<planks.Length;i++)
                    {
                        GameObject plank=planks[i];float arm=(i+.5f)*2.25f;
                        bool shouldExist=i!=3 || sag<.06f || sag>.98f;
                        float x=hingeX+Mathf.Cos(rad)*arm,top=hingeY+Mathf.Sin(rad)*arm;
                        if(shouldExist&&!plank.activeSelf && Near(x,top,1.2f)) shouldExist=false;
                        if(plank.activeSelf!=shouldExist) plank.SetActive(shouldExist);
                        if(!shouldExist) continue;
                        Place(plank,x,top-.17f);
                        plank.transform.localRotation=Quaternion.Euler(0,0,rad*Mathf.Rad2Deg);
                    }
                    rivet.GetComponent<SpriteRenderer>().color=ResTeal;
                }
                shieldX=Mathf.Lerp(32,40,ResidenceTide(Elapsed,7));
                Place(shield,shieldX,2.7f);
                float strength=ResidenceTide(Elapsed,5.5f);
                pennant.transform.localRotation=Quaternion.Euler(0,0,-50+46*strength);
                bool sheltered=Hero.x>shieldX && Hero.x<shieldX+3.2f;
                if(!Zipping && Hero.x>30 && Hero.x<44 && Hero.y<5.4f && Hero.y>.5f && !sheltered)
                {
                    float gust=Mathf.Lerp(.3f,7f,strength);
                    Push(Mathf.MoveTowards(Velocity.x,Axis*8.7f+gust,28*dt),Velocity.y);
                }
            });
            Sign("방풍판 바로 뒤는 바람이 쉰다",31,5);
        }

        void ResidenceLaundry()
        {
            FactoryMachine(3,"승강기 제어반",32.2f,0,.43f);
            Deck("옥상 세탁대 계단",7,2.0f,4.4f);
            Deck("널빤지 계단",11.8f,4.45f,4.2f);
            Deck("빨랫줄 아래 창대",15.6f,6.7f,4.0f);
            Deck("세탁실 다락 지붕",18.6f,9.0f,4.1f);
            Hook("승강탑 외벽 고리",20.5f,12.8f);
            Deck("전망대 다이브 출발점",24,13.1f,6.2f);
            ResidenceWindow(13.1f,5.8f,ResBrass);
            LivingAccordion(13.1f,5.4f);
            for(int row=0;row<2;row++)
            {
                Box("세탁실 빨랫줄",13+row*3,8+row*2,10,.05f,ResIvory,false,-3);
                for(int i=0;i<4;i++)
                {
                    var sheet=Box("야간 건조 중인 셔츠",9.8f+row*3+i*2,7.45f+row*2,.89f,1.0f,i%2==0?ResTeal:ResIvory,false,-4);
                    sheet.transform.localRotation=Quaternion.Euler(0,0,4-row*6);
                }
            }
            // 낙하 축 자체는 끝까지 비워 둔다. 원하는 높이에서 좌우로만 빠져나간다.
            Deck("높은 복귀 회랑",36,10.7f,6.5f);
            Hook("전망대로 돌아가는 고리",35,12.5f);
            Portal("전망대 복귀",37.8f,11.7f,38);
            Deck("다이브 중간의 숨은 정원",24,6.15f,6.0f);
            ResidencePlanter(23,6.15f,3);
            Use("정원 쪽 빗장",25.5f,7.12f,()=>{SetFlag("residence.diveGarden");Say("화분 사이로 부유 정원의 산책로가 이어진다.");});
            Portal("숨은 정원 산책로",22.0f,7.15f,31,()=>Flag("residence.diveGarden"));
            var liftLamp=Box("정비 승강기 운행등",33.6f,3,.55f,.24f,ResBrass,false,4);
            System.Action lamp=()=>{liftLamp.GetComponent<SpriteRenderer>().color=Flag("living.laundryLift")?ResTeal:ResBrass;};
            lamp();
            Use("정비 승강기 복구",30,1.05f,()=>{
                SetFlag("living.laundryLift");SetFlag("admin.towerLaundry");lamp();
                Say("승강기 운행 재개  ·  빨래골목에서 들었던 손풍금이 아래로부터 울린다.");
            });
            Portal("생활부 빨래골목 승강기",34,1.15f,12,()=>Flag("living.laundryLift"));
            Portal("전산탑 배선교",40.5f,1.15f,30,()=>Flag("admin.towerLaundry"));
            Sign("높은 회랑 · 가운데 정원 · 바닥 승강기",23.4f,14.17f);
        }
    }
}
