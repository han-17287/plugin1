using System;
using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom : MonoBehaviour
    {
        static AudioClip livingReeds;
        AudioSource roomSpeaker;
        AncientFeedbackProfile speakerProfile;
        Vector2 speakerPoint;
        Func<bool> speakerOpen;
        float speakerRange, speakerGain, speakerTarget;
        bool speakerNear, speakerMusical, speakerQueued, speakerPaused;


        void BuildLiving(int id)
        {
            switch (id)
            {
                case 11: BuildNightMarket(); break;
                case 12: BuildLaundryAlley(); break;
                case 13: BuildSkyStop(); break;
                case 14: BuildSignArcade(); break;
                case 15: BuildRooftopGarden(); break;
                case 16: BuildMidnightTheatre(); break;
                case 17: BuildAccordionShop(); break;
                case 18: BuildScrapYard(); break;
                case 19: BuildVacantFlats(); break;
                case 20: BuildMovingKitchen(); break;
            }
        }

        void LivingWall(int style, int alternate, float x, float y, float scale)
        {
            var art = AncientArt.Display("생활부 · 골목 벽면",AncientLivingWallArt.Get(style,alternate),
                new Vector2(x,y),-11,transform);
            art.transform.localScale = new Vector3(scale,scale,1);
        }


        static readonly Color Cloth = new Color(.71f,.38f,.29f), ReedGold = new Color(.93f,.68f,.35f);
        static readonly Color Tile = new Color(.24f,.46f,.46f), LitGlass = new Color(.49f,.79f,.75f);

        void LivingNpc(string name, int sprite, float x, float y, Action speak)
        {
            AncientArt.Display(name,AncientArt.Citizen(sprite),new Vector2(x,y),5,transform);
            Use(name+"에게 말 걸기",x,y+1,speak);
        }

        void LivingWindow(string name, float x, float sill, bool lit)
        {
            Box(name+" 창틀",x,sill+1.02f,2.05f,2.05f,Tile,false,-6);
            Box(name+" 유리",x,sill+1.02f,1.76f,1.79f,lit ? ReedGold : LitGlass,false,-5);
            Box(name+" 세로살",x,sill+1.02f,.10f,1.90f,Tile,false,-4);
            Box(name+" 가로살",x,sill+1.12f,1.90f,.12f,Tile,false,-4);
        }

        void LivingCanvas(string name, float x, float y, float width)
        {
            Box(name+" 차양",x,y,width,.42f,Cloth,false,2);
            int count = Mathf.CeilToInt(width/.8f);
            for (int i=0; i<count; i++)
                Box(name+" 술",x-width*.5f+width*(i+.5f)/count,y-.27f,width/count*.91f,.27f,
                    i%2==0 ? ReedGold : Cloth,false,3);
        }

        void LivingSteps(string name, float x, float top, int count, float dx, float dy)
        {
            for (int i=0; i<count; i++) Deck(name,x+dx*i,top+dy*i,2.25f);
        }

        void LivingAccordion(float x,float y) { LivingSpeaker(x,y,null); }

        void LivingSpeaker(float x, float y, Func<bool> play)
        {
            CreateSpeaker("골목 손풍금", x, y, 10, .34f);
            speakerOpen = play;
            speakerMusical = true;
            roomSpeaker.loop = true;
            if (speakerProfile) roomSpeaker.outputAudioMixerGroup = speakerProfile.music;
        }

        void CreateSpeaker(string label, float x, float y, float range, float gain)
        {
            var owner = new GameObject(label);
            owner.transform.SetParent(transform, false);
            owner.transform.localPosition = new Vector3(x, y, 0);
            roomSpeaker = owner.AddComponent<AudioSource>();
            roomSpeaker.playOnAwake = false;
            roomSpeaker.spatialBlend = 0;
            roomSpeaker.dopplerLevel = 0;
            roomSpeaker.priority = 105;
            roomSpeaker.volume = 0;
            speakerPoint = new Vector2(x, y);
            speakerRange = range;
            speakerGain = gain;
            speakerProfile = Resources.Load<AncientFeedbackProfile>("Audio/FieldFeedback");
        }

        void TuneRadio(AudioClip clip, bool musical)
        {
            roomSpeaker.Stop();
            roomSpeaker.volume = 0;
            roomSpeaker.clip = clip;
            roomSpeaker.loop = false;
            speakerMusical = musical;
            speakerPaused = false;
            speakerQueued = clip && AncientGame.Saved("muted") == 0;
            if (speakerProfile)
                roomSpeaker.outputAudioMixerGroup = musical ? speakerProfile.music : speakerProfile.effects;
        }

        public bool WantsRoomMusic()
        {
            if (!roomSpeaker) return false;
            float strength = Mathf.Clamp01(1 - Vector2.Distance(Hero, speakerPoint) / speakerRange);
            // 경계선을 오가더라도 같은 음원이 매 프레임 멈췄다 켜지지 않게 한다.
            speakerNear = strength > (speakerNear ? .10f : .18f) && (speakerOpen == null || speakerOpen());
            speakerTarget = speakerNear ? strength * speakerGain : 0;
            bool active = roomSpeaker.loop || speakerQueued || speakerPaused || roomSpeaker.isPlaying;
            return speakerMusical && active && (speakerNear || (roomSpeaker.isPlaying && roomSpeaker.volume > 0));
        }

        public void TickSpeaker(bool paused, bool muted, bool allowsMusic)
        {
            if (!roomSpeaker) return;
            if (!paused && !speakerNear && !roomSpeaker.isPlaying)
            {
                if (speakerPaused) roomSpeaker.Stop();
                speakerQueued = speakerPaused = false;
                roomSpeaker.volume = 0;
                return;
            }
            if (paused || muted || (speakerMusical && !allowsMusic))
            {
                if (roomSpeaker.isPlaying)
                {
                    roomSpeaker.Pause();
                    speakerPaused = true;
                }
                return;
            }
            if (speakerPaused)
            {
                roomSpeaker.UnPause();
                speakerPaused = false;
            }
            else if (speakerNear && !roomSpeaker.isPlaying && (roomSpeaker.loop || speakerQueued))
            {
                if (!roomSpeaker.clip) roomSpeaker.clip = LivingReedTune();
                roomSpeaker.Play();
                speakerQueued = false;
            }
            roomSpeaker.volume = Mathf.MoveTowards(roomSpeaker.volume, speakerTarget, Time.unscaledDeltaTime * 1.5f);
            if (!speakerNear && roomSpeaker.volume <= 0 && roomSpeaker.isPlaying)
                roomSpeaker.Stop();
        }

        // 열린 골목으로 장사꾼도 옮긴다. 마지막에 연 문이 현재 정류장이 된다.
        void LivingUnlock(string key, int vendorRoom)
        {
            if (Flag(key)) return;
            SetFlag(key);
            foreach (int home in new[]{11,12,17,19,20})
                SetFlag("living.cart."+home, home==vendorRoom);
        }

        int LivingVendorHome()
        {
            if (Flag("living.cart.11")) return 11;
            if (Flag("living.cart.12")) return 12;
            if (Flag("living.cart.17")) return 17;
            if (Flag("living.cart.19")) return 19;
            return 20;
        }

        void LivingVendor(int room, float x)
        {
            var cart = new GameObject("순회 포장마차");
            cart.transform.SetParent(transform,false);
            cart.transform.localPosition=new Vector3(x,0,0);
            var cook = AncientArt.Display("복례",AncientArt.Citizen(8),new Vector2(.50f,.38f),5,cart.transform);
            cook.flipX=true;
            foreach(var part in new[]{
                Box("매대",x,1.10f,3.3f,.62f,Tile,false,6),
                Box("지붕",x,3.08f,3.7f,.36f,Cloth,false,5),
                Box("왼쪽 기둥",x-1.52f,1.95f,.13f,2.35f,ReedGold,false,5),
                Box("오른쪽 기둥",x+1.52f,1.95f,.13f,2.35f,ReedGold,false,5),
                Box("수레바퀴",x-.98f,.36f,.62f,.60f,Tile,false,5),
                Box("수레바퀴",x+.98f,.36f,.62f,.60f,Tile,false,5)})
                part.transform.SetParent(cart.transform,true);
            cart.SetActive(LivingVendorHome()==room);
            Step(dt => { bool here=LivingVendorHome()==room; if (cart.activeSelf!=here) cart.SetActive(here); });
            Use("포장마차 정류장",x,1,() =>
            {
                if (!cart.activeSelf)
                {
                    int street=LivingVendorHome();
                    Say("분필 쪽지  ‘오늘은 "+(street==11?"야시장":street==12?"빨래골목":street==17?"수리점":street==19?"폐쇄 아파트":"빨래차 차고")+" 앞에서 끓여요.’"); return;
                }
                Say(!Flag("living.marketLaundry") ? "복례  “콩국수 한 그릇. 세탁소 창이 열릴 때마다 악기 소리가 난다우.”" :
                    !Flag("living.reedsFixed") ? "복례  “악기 주인이 간판 좁은 수리점에서 기다려. 국물 식기 전에 다녀와.”" :
                    !Flag("living.towerLift") ? "복례  “찬밥은 꼭대기 불 켜질 때 나가. 아파트 문은 안에서 잠갔대.”" :
                    !Flag("living.laundryLift") ? "복례  “저 위 세탁실이랑 이 골목, 옛날엔 같은 승강기를 썼다지.”" :
                    "복례  “오늘은 돌아오셨네. 경위님 몫은 늘 냄비 바닥에 남겨요.”");
            },1.35f);
        }

        void BuildNightMarket()
        {
            LivingWall(0,0,12,0,1.3f); LivingWall(2,0,30,0,1.35f);
            LivingSteps("시장 처마",6,1.35f,3,3,1.4f);
            Deck("지붕 가판",19,5.7f,8); Deck("한 층 아래 발코니",23.6f,3.25f,3.0f);
            LivingCanvas("청과",17.8f,5.45f,6);
            Hook("시간표 모서리 고리",21,7.25f);
            Deck("셔터 권양기 지붕",29.7f,7.20f,7.7f); Hook("시장 옥상",28.8f,8.5f);
            var shutter=Box("지상 시장 셔터",32.3f,2.1f,.65f,4.2f,Tile,true);
            var rungs = new GameObject[6];
            for(int i=0;i<rungs.Length;i++) rungs[i]=Deck("접이식 사다리",27.3f,1.1f+i*1.08f,1.18f,.13f);
            Action refresh=()=>{bool open=Flag("living.marketShutter");shutter.SetActive(!open);foreach(var rung in rungs)rung.SetActive(open);};
            refresh();
            Use("옥상에서 셔터 쇠사슬 풀기",30.1f,8.1f,()=>
            {
                LivingUnlock("living.marketShutter",11);refresh();
                Say("말순  “바닥에 걸렸던 게 빠졌네! 사다리 받아요, 경위님.”");
            });
            LivingNpc("말순 상인",5,9,0,()=>Say(Flag("living.marketShutter") ?
                "말순  “사다리가 내려왔으니 밤 장사도 다시 하지.”" :
                "말순  “지붕 쪽에서 셔터 고리 좀 빼 줘요. 여기선 손이 안 닿아서.”"));
            Sign("지붕 발판에서 S+Space · 오른쪽 짧은 발코니",20,7);
            Sign("세탁소 뒷문",35,2.5f);
            Portal("세탁소 뒷문",35,1,12,()=>Flag("living.marketLaundry"));
            Deck("시장 종루",38,8.6f,5); Hook("종루",37.3f,10.15f);
            Use("종루 쪽 통행 볼트 뽑기",37,9.55f,()=>{LivingUnlock("living.marketTower",11);Say("전산탑 방향 등불이 함께 켜졌다.");});
            Portal("전산탑으로 통하는 종루문",39.5f,9.5f,30,()=>Flag("living.marketTower"));
            LivingVendor(11,42.3f);
        }

        void BuildLaundryAlley()
        {
            LivingWall(0,0,10,0,1.35f); LivingWall(0,1,31,0,1.5f);
            Deck("공동 옥상",8,4.4f,10); Hook("권취기 시작",8,6.1f);
            Deck("세탁소 비밀 창턱",26,4.95f,2.25f); LivingWindow("음악이 새는 창문",26,4.95f,true);
            Deck("지붕 본길",36.8f,5.2f,9); Hook("건너편 홈통",37,7.55f);
            var hook=Hook("긴 빨래 리프트 고리",24,6.35f);
            var laundry=new GameObject[4];
            for (int i=0; i<4; i++)
                laundry[i]=Box("바람에 마르는 빨래",22+i*.75f,7.56f,.59f,1.05f,i%2==0 ? Tile : ReedGold,false,-2);
            Box("빨래 철선",22,8.5f,17,.09f,Tile,false,-3);
            float brake=0; bool spent=false;
            Step(dt=>
            {
                float x=25.6f+Mathf.Sin(Elapsed*1.15f)*2.6f;
                Place(hook,x,6.5f);
                for(int i=0;i<laundry.Length;i++)Place(laundry[i],x-1.65f+i*.75f,7.87f+.12f*Mathf.Sin(Elapsed*3+i));
                if(Arrived(hook) && !spent) {brake=.49f;spent=true;Push(15,3.6f);}
                if(!Near(x,6.5f,3.5f))spent=false;
                if(brake<=0)return;
                brake-=dt;
                if(Axis<-.2f && Mathf.Abs(Hero.x-26)<3.45f)
                {
                    Push(Mathf.Clamp((26-Hero.x)*3.5f,-5,5),-2.8f);
                    if(Mathf.Abs(Hero.x-26)<.65f)brake=0;
                }
            });
            LivingSteps("아랫골목 건조대",18,1.45f,3,2.65f,1.24f);
            LivingWindow("세탁소 반지하",24.6f,0,true);LivingAccordion(25.8f,2.2f);
            Use("세탁소 창을 열고 안으로 들어가기",26,5.87f,()=>{Warp(24.6f,1.0f);Say("물받이 밑 반쯤 열린 창에서 손풍금 소리가 샌다.");});
            Use("세탁소 분실물 확인",24.2f,1,()=>
            {
                LivingUnlock("living.marketLaundry",12);
                if(!Flag("living.reedsFound")){SetFlag("living.reedsFound");Say("분실물 칸에서 바람 빠진 손풍금을 챙겼다. 손잡이에 ‘동네 수리점’이 새겨져 있다.");}
                else Say("세탁소 뒷문이 야시장 쪽으로 열려 있다.");
            });
            Portal("세탁소 뒷문 · 야시장",28.7f,1,11,()=>Flag("living.marketLaundry"));
            Portal("야경 세탁실 정비 승강기",34,1,40,()=>Flag("living.laundryLift"));
            Sign("긴 고리 뒤 A로 제동 / 그대로 옥상행",9,5.75f);
            LivingVendor(12,40.7f);
        }

        void BuildSkyStop()
        {
            LivingWall(1,0,16,0,1.4f);LivingWall(1,1,36,0,1.3f);
            LivingSteps("정류장 계단",5,1.45f,3,2.65f,1.45f);
            Deck("윗층 대합실",14,7.2f,7); Hook("시간표 고리",13,8.65f);
            Deck("먼저 내리는 역",26.2f,5.70f,3);LivingWindow("정류장 창문",26.2f,5.7f,true);
            Deck("종점",40.2f,4.3f,6.5f);
            Box("옛 정류소 방풍벽",34.5f,1.62f,.70f,3.24f,Tile,true,-1);
            var shuttle=Deck("매시 9분 셔틀",9,4.1f,4.7f,.70f);
            var cabin=Box("셔틀 객실",9,5.06f,3.83f,1.47f,Tile,false,-1);
            var glass=Box("셔틀 차창",9,5.28f,3.15f,.66f,LitGlass,false,0);
            Step(dt=>
            {
                // 끝 정류장 두 곳에서 잠시 정차한다. 윗층에서 내려타는 타이밍이 생긴다.
                float p=Mathf.Clamp01((Mathf.Sin(Elapsed*.33f-Mathf.PI*.5f)+.73f)/1.46f);
                float x=Mathf.Lerp(9,38,p);
                Place(shuttle,x,3.75f);Place(cabin,x,5.06f);Place(glass,x,5.28f);
            });
            LivingNpc("막차 기사",6,41.6f,4.3f,()=>Say("막차 기사  “창문엔 정차 안 합니다. 한 정거장 일찍 내릴 분은 위 대합실로요.”"));
            Use("창문 너머 대기실",26.2f,6.65f,()=>{SetFlag("living.skyWindow");Warp(30.3f,1.10f);Say("도장집 뒷계단. 막차를 탄 사람들이 벽에 별 하나씩 그려 놓았다.");});
            Sign("S+Space · 셔틀이 발밑에 왔을 때",14,8.48f);
        }

        void BuildSignArcade()
        {
            LivingWall(1,1,14,0,1.3f); LivingWall(2,1,30,0,1.5f);
            LivingSteps("간판 정비 발판",7,1.3f,3,2.9f,1.35f);
            Hook("간판 정비줄",18.7f,7);
            var surface=Deck("그림자 간판",25.2f,4.75f,6.8f);
            var panel=Box("빛을 가리는 칠보 간판",25.2f,4.12f,6.8f,1.23f,Cloth,false,4);
            var fold=Box("접힌 간판",22.15f,5.7f,.49f,3.0f,Cloth,false,4);
            var arrow=Box("간판 이정표",25.2f,4.12f,2.5f,.17f,ReedGold,false,5);
            bool open=Flag("living.signOpen");
            Action show=()=>{surface.SetActive(open); panel.SetActive(open);fold.SetActive(!open);arrow.SetActive(open);};
            show();
            Use("간판 도르래 당기기",15.8f,4.98f,()=>{open=!open;SetFlag("living.signOpen",open);show();Say(open ? "간판 아래 그늘이 생겼다. 윗면은 다음 지붕과 이어진다." : "간판을 접자 탐조등이 골목 바닥까지 닿는다.");});
            Deck("도르래 받침",16,4.12f,3.7f); Deck("노점 옥상",33,6.2f,4.45f);Hook("노점 집게",32.2f,7.95f);
            var beam=Box("노란 탐조등",25,4,1,1,new Color(1,.76f,.35f,.52f),false,1);
            Vector2 source=new Vector2(25.3f,9.2f);
            Box("순찰등",source.x,source.y,.94f,.67f,Tile,false,3);
            float grace=0;
            Step(dt=>
            {
                float groundX=25.2f+Mathf.Sin(Elapsed*1.8f)*5;
                Vector2 end=new Vector2(groundX,.22f);
                float p=(source.y-4.75f)/(source.y-end.y);
                Vector2 crossed=Vector2.Lerp(source,end,p);
                if(open && Mathf.Abs(crossed.x-25.2f)<3.4f)end=crossed;
                LivingBeam(beam,source,end,.42f);
                grace-=dt;
                if(grace<=0 && LivingLineNear(source,end,Hero,.60f))
                {Push(Hero.x<25.2f?-5.5f:5.5f,4);grace=1.2f;Say("탐조등에 걸렸다. 펼친 간판 아래로 숨자.");}
            });
            Sign("간판을 펼치면 아래는 그늘, 위는 발판",10,3);
        }

        static bool LivingLineNear(Vector2 a, Vector2 b, Vector2 p, float radius)
        {
            Vector2 ab=b-a;
            float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(.0001f,ab.sqrMagnitude));
            return (p-a-ab*t).sqrMagnitude<=radius*radius;
        }

        void LivingBeam(GameObject beam, Vector2 a, Vector2 b, float width)
        {
            Vector2 ab=b-a; Vector2 center=(a+b)*.5f;
            Place(beam,center.x,center.y);
            beam.transform.localScale=new Vector3(width,ab.magnitude,1);
            beam.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(ab.y,ab.x)*Mathf.Rad2Deg-90);
        }

        void BuildRooftopGarden()
        {
            LivingWall(0,1,11,0,1.4f); LivingWall(3,1,33,0,1.45f);
            LivingSteps("화단 계단",5.8f,1.4f,3,2.8f,1.3f);
            var west=Deck("서쪽 급수 화분",16.5f,2.45f,4.3f,.54f);
            var east=Deck("동쪽 채소 함지",31.6f,6.4f,4.3f,.54f);
            var sprouts=new GameObject[6];
            for(int i=0;i<6;i++) sprouts[i]=Box("깻잎 새순",0,0,.52f,.72f,new Color(.34f,.62f,.35f),false,-1);
            bool watered=Flag("living.westWater");float mix=watered ? 1 : 0;
            Action pour=()=>{watered=!watered;SetFlag("living.westWater",watered);Say(watered?"서쪽 물받이가 무거워지며 동쪽 온실이 올라온다.":"동쪽 물받이를 채우자 서쪽 옥상밭이 올라온다.");};
            Use("물길 바꾸기",10,1,pour);Use("옥상 급수통",10.9f,4.85f,pour);
            Deck("탄력 천막",24,2.8f,7.2f,.25f);LivingCanvas("연두 천막",24,2.55f,7.2f);
            Deck("높은 온실 창",24.2f,7.50f,3.4f);LivingWindow("온실",24.2f,7.50f,true);
            Deck("낮은 가게 지붕",32.5f,4.55f,4);Hook("온실 잡이",26.8f,9.1f);
            float lastY=Hero.y, cool=0;
            Step(dt=>
            {
                mix=Mathf.MoveTowards(mix,watered?1:0,dt*.60f);
                float wy=Mathf.Lerp(6.3f,2.4f,mix),ey=Mathf.Lerp(2.4f,6.3f,mix);
                Place(west,16.5f,wy-.27f);Place(east,31.6f,ey-.27f);
                for(int i=0;i<6;i++) Place(sprouts[i],(i<3?16.5f:31.6f)+(i%3-1)*.8f,(i<3?wy:ey)+.39f);
                cool-=dt;float dx=Hero.x-24;
                if(cool<=0 && Mathf.Abs(dx)<3.52f && Hero.y<3.91f && Hero.y>3.30f && lastY>Hero.y+.002f && Velocity.y<1)
                {
                    bool middle=Mathf.Abs(dx)<1.2f;
                    Push(middle ? Velocity.x*.4f : Mathf.Sign(dx)*8.1f,middle ? 19 : 11.5f);
                    cool=.36f;
                }
                lastY=Hero.y;
            });
            LivingNpc("온실지기",9,24.4f,7.50f,()=>Say("온실지기  “천막 한가운데엔 생선 그물을 두 겹 댔어. 옆집으로 갈 땐 가장자리만 밟아.”"));
            Sign("서쪽 물통 / 동쪽 화단",8,2.6f);
        }

        void BuildMidnightTheatre()
        {
            LivingWall(2,1,11,0,1.45f); LivingWall(3,1,36,0,1.5f);
            LivingSteps("관객석",6.1f,1,4,2.35f,.74f);
            Deck("무대",22,2.15f,11,.48f);
            Deck("조명 다리",22.6f,8.2f,8.25f);Hook("조명 사다리 고리",21,9.65f);
            Box("조명 트러스",22.6f,11.8f,11.4f,.21f,Tile,false,-2);
            for(int i=0;i<4;i++)Box("조명등",18.55f+i*2.65f,11.35f,.62f,.69f,ReedGold,false,-2);
            var curtain=Box("무대 뒤 가동 배경",28,3.52f,.8f,7,Cloth,true,3);
            var painted=Box("무대 배경의 달",28,5.20f,.59f,1.36f,ReedGold,false,4);
            bool raised=Flag("living.backdrop");float lift=raised?1:0;
            Use("조명 다리에서 막 올리기",20.5f,9.12f,()=>{raised=!raised;SetFlag("living.backdrop",raised);Say(raised?"뒤편 통로에 바깥 바람이 든다. 창으로 나가면 옥상이다.":"배경이 내려오자 작은 무대가 다시 밤이 됐다.");});
            Step(dt=>
            {
                // 막 아래에 사람이 있으면 내려오는 장치를 그 자리에서 멈춘다.
                bool occupied=!raised && Mathf.Abs(Hero.x-28)<.85f && Hero.y+1.08f>7*lift-.22f && Hero.y<7*lift+3.5f;
                if(!occupied)lift=Mathf.MoveTowards(lift,raised?1:0,dt*.67f);
                Place(curtain,28,3.52f+7*lift);Place(painted,28,5.2f+7*lift);
            });
            LivingWindow("통로 출입구",30.3f,0,false);LivingWindow("의상실 창",36.1f,5.8f,true);
            Deck("공연장 뒤 회랑",32.2f,3.3f,5.5f);Deck("의상실",36.2f,5.8f,4);
            Deck("밖의 지붕",42.3f,7.22f,6.1f);
            Use("무대 뒤 낮은 문",30.2f,1.0f,()=>{if(!raised){Say("조명 다리부터 올라가 막을 걷어야 한다.");return;}Warp(30.6f,4.34f);Say("배우 동선. 건너편 높은 창이 출구다.");});
            Use("의상실 높은 창으로 나가기",36.2f,6.75f,()=>{Warp(40.6f,8.20f);Say("연극이 끝나기 전에 옆 골목에 도착했다.");});
            LivingNpc("조명 기사",7,14.3f,0,()=>Say("조명 기사  “객석에선 줄을 못 당겨요. 조명 다리 위 감긴 줄을 봐요.”"));
        }

        void BuildAccordionShop()
        {
            LivingWall(0,0,9,0,1.25f);LivingWall(3,1,32,0,1.65f);
            LivingSteps("수리점 외부 계단",13.1f,1.35f,3,2.7f,1.32f);
            Deck("갈림 폭 좁은 발판",24,5.5f,2.6f);Hook("수리점 크레인",23.35f,7.2f);
            Deck("왼쪽 넓은 계단",19.7f,4.25f,4.0f);
            Deck("오른쪽 작은 간판",28.3f,6.48f,1.15f);
            LivingCanvas("악기점",31.8f,6.35f,4);
            Deck("주인집 창턱",34.25f,7.9f,4.2f);LivingWindow("악기 보관실",34.3f,7.9f,true);
            var gate=Box("수리점 골목문",38.2f,1.73f,.72f,3.46f,Tile,true);
            gate.SetActive(!Flag("living.reedsFixed"));
            LivingNpc("손풍금 수리공 은주",8,31.5f,0,()=>
            {
                if(Flag("living.reedsFixed")){Say("은주  “가게 불이 꺼져 있어도, 이 곡이 들리면 제가 안에 있는 거예요.”");return;}
                if(!Flag("living.reedsFound")){Say("은주  “빨래골목에서 손풍금 보셨어요? 세탁소 물받이에 두고 온 것 같아요.”");return;}
                LivingUnlock("living.reedsFixed",17);gate.SetActive(false);
                Say("은주  “우리 어머니 곡이에요. 경위님, 한 소절만 듣고 가요.”  가게 옆 골목문이 열렸다.");
            });
            LivingSpeaker(31.5f,1.55f,()=>Flag("living.reedsFixed"));
            Portal("열린 악기점 옆길 · 야시장",40.1f,1,11,()=>Flag("living.reedsFixed"));
            Use("악기 보관실에서 뒷편으로 내려가기",34.25f,8.85f,()=>{Warp(40.8f,1.15f);Say("작은 간판을 건너면 골목문 너머로 나온다.");});
            Sign("공중 갈림 · 왼쪽 계단 / 오른쪽 작은 간판",22,6.8f);
            LivingVendor(17,6.6f);
        }

        void BuildScrapYard()
        {
            LivingWall(3,0,9,0,1.45f);LivingWall(1,1,37,0,1.40f);
            Deck("고철 위 단",10.7f,3.3f,5.4f);Hook("노끈",12,5.2f);
            Deck("높은 검사 창",26,8.00f,4.4f);LivingWindow("차축 검사실",26,8,true);
            Deck("안쪽 작은 발판",33.7f,3.1f,4);LivingWindow("안쪽 골목",35,.2f,false);
            var crates=new GameObject[3];
            var stripes=new GameObject[3];
            var marked=new bool[3];
            var sparks=new GameObject[3];
            var ages=new float[3];
            float[] xs={15.4f,23.5f,30.3f};
            for(int i=0;i<crates.Length;i++)
            {
                int index=i;
                crates[i]=Box("차폐용 철제 상자",xs[i],1.14f,1.84f,2.28f,Tile,true,3);
                stripes[i]=Box("상자 테이프",xs[i],1.32f,1.8f,.24f,ReedGold,false,4);
                sparks[i]=Box("해체 입자",xs[i],1.35f,.34f,.34f,LitGlass,false,6);
                sparks[i].SetActive(false);
                Use("철상자 해체",xs[i],1.2f,()=>BreakLivingCrate(index,crates,stripes,marked,sparks));
            }
            var falling=new GameObject[3];var debris=new GameObject[3];
            for(int i=0;i<3;i++)
            {
                falling[i]=Hook("낙하 고철의 고리",17+6*i,9);
                debris[i]=Box("낙하 고철",17+6*i,8,.95f,.60f,Tile,false,-1);
            }
            var beam=Box("상자에 막히는 순찰광",25,1,1,1,new Color(1,.60f,.22f,.66f),false,1);
            Box("폐차장 감시렌즈",36,1.60f,.6f,.86f,ReedGold,false,4);
            float grace=0;
            Step(dt=>
            {
                float endX=8.30f;
                for(int i=0;i<3;i++)
                {
                    if(!marked[i] && (BeamHits(crates[i]) || (PrimaryHeld && expedition.Player.Mode==PlayerMode.Attack && !Zipping && Near(xs[i],1.2f,1.60f))))
                        BreakLivingCrate(i,crates,stripes,marked,sparks);
                    if(sparks[i].activeSelf)
                    {
                        ages[i]+=dt;
                        Place(sparks[i],xs[i]+Mathf.Sin(ages[i]*7)*.18f,1.35f+ages[i]*2);
                        if(ages[i]>.6f)sparks[i].SetActive(false);
                    }
                    if(!marked[i])endX=xs[i]+.92f;
                    float u=(Elapsed*.159f+i*.29f)%1;
                    float y=Mathf.Lerp(11.8f,1.0f,u*u);
                    float x=16.6f+i*6.35f;
                    Place(falling[i],x,y);Place(debris[i],x,y-.6f);
                }
                float h=1.50f+.44f*Mathf.Sin(Elapsed*1.7f);
                LivingBeam(beam,new Vector2(36,h),new Vector2(endX,h),.14f);
                grace-=dt;
                if(grace<=0 && Hero.x> endX && Hero.x<36 && Mathf.Abs(Hero.y-h)<.81f)
                {Push(-6.25f,4.5f);grace=1.15f;Say("순찰광 노출. 철상자를 남기면 그 뒤로 숨을 수 있다.");}
            });
            Use("높은 검사 창에서 해치 열기",26,8.95f,()=>{SetFlag("living.scrapWindow");Warp(39.1f,1.05f);Say("차축 검사실 바깥문이 열렸다.");});
            Sign("철상자 / 입자 또는 엄폐",10,2.15f);
        }

        void BreakLivingCrate(int i,GameObject[] crates,GameObject[] stripes,bool[] marked,GameObject[] sparks)
        {
            if(marked[i]) return;
            marked[i]=true;crates[i].SetActive(false);stripes[i].SetActive(false);
            AddCharge(3);
            Say("철상자의 푸른 입자 +3 · 광선이 다음 엄폐물까지 뻗는다.");
            sparks[i].SetActive(true);
        }

        void BuildVacantFlats()
        {
            LivingWall(0,0,9,0,1.5f); LivingWall(0,1,29.7f,0,1.65f);
            LivingSteps("안쪽 비상계단",7.8f,1.4f,3,2.60f,1.38f);
            Deck("바깥 임시 발코니",18.2f,5.30f,4.5f);
            Deck("안쪽 계단참",23.3f,6.76f,4.3f);
            Deck("바깥 이불 난간",28.4f,8.12f,4.5f);
            Deck("옥상 기계실",34.6f,10.1f,7.5f);
            Hook("발코니 줄걸이",18.5f,7.22f);Hook("기계실 줄걸이",34,11.60f);
            LivingWindow("계단 통풍창",23.3f,6.76f,false);LivingWindow("옥상 전기실",35,10.1f,true);
            Deck("되돌아보는 낮은 난간",31.3f,1.52f,2.6f);
            var gate=Box("폐쇄 아파트 현관",36.8f,1.75f,.68f,3.5f,Tile,true);
            var elevator=Deck("주민 승강기",39.4f,.48f,3.6f,.46f);
            var indicator=Box("승강기 상태등",37.8f,2.1f,.33f,.33f,ReedGold,false,4);
            bool power=Flag("living.towerLift");gate.SetActive(!power);indicator.SetActive(power);
            Use("엘리베이터 차단기 복구",34.6f,11.05f,()=>
            {
                power=true;LivingUnlock("living.towerLift",19);gate.SetActive(false);indicator.SetActive(true);
                Say("옥상과 현관에 초록불이 켜졌다. 1층 호출 버튼이 살아났다.");
            });
            bool top=false;float lift=.48f;
            Action call=()=>{if(!power){Say("승강기가 멎어 있다. 옥상 차단기를 확인하자.");return;}top=!top;Say(top?"승강기가 옥상으로 간다.":"승강기가 현관으로 간다.");};
            Use("엘리베이터 호출",39.4f,1.48f,call);Use("옥상 승강기 호출",39.4f,11.06f,call);
            var rear=Box("낮은 안방 문틈",25.6f,.95f,.92f,1.64f,ReedGold,false,-3);
            bool seen=Flag("living.reverseDoor");rear.SetActive(seen);
            Step(dt=>
            {
                if(power){lift=Mathf.MoveTowards(lift,top?10.12f:.48f,dt*3.0f);Place(elevator,39.4f,lift-.23f);}
                if(!seen && Axis<-.15f && Hero.x>26.3f && Hero.x<33.2f && Hero.y<3.65f)
                {
                    seen=true;SetFlag("living.reverseDoor");rear.SetActive(true);
                    Say("되돌아보니 낮은 난간 아래 문고리에 빛이 걸린다.");
                }
            });
            Use("낮은 난간 뒤 안방",25.6f,1,()=>
            {
                if(!seen){Say("문틈이 기둥 뒤에 가려 있다.");return;}
                Warp(14.1f,5.10f);Say("빈 안방의 뒤쪽 계단이 처음 계단참으로 이어진다.");
            });
            var guard=AncientArt.Display("경비원 영택",AncientArt.Citizen(5),new Vector2(7,0),5,transform);
            float guardX=power?36.7f:7, boardWait=0; bool riding=false, upstairs=false;
            Step(dt=>
            {
                if(!power)return;
                float previous=guardX;
                if(upstairs) guardX=Mathf.MoveTowards(guardX,35.65f,dt*1.6f);
                else if(!riding)
                {
                    guardX=Mathf.MoveTowards(guardX,38.75f,dt*2.8f);
                    if(guardX>=38.74f)
                    {
                        if(lift<.58f){riding=true;boardWait=1.2f;}
                        else if(lift>=10.1f && !Near(39.4f,lift+.9f,2))top=false;
                    }
                }
                else
                {
                    boardWait-=dt;
                    if(boardWait<=0 && lift<.6f && !Near(39.4f,lift+.9f,2))top=true;
                    if(lift>=10.1f){riding=false;upstairs=true;}
                }
                float feet=upstairs?10.1f:riding?lift:0;
                guard.transform.localPosition=new Vector2(guardX,feet);
                if(Mathf.Abs(previous-guardX)>.001f) guard.flipX=guardX<previous;
            });
            Use("경비원 영택",7,1,()=>Say(power?"계단 밑 의자가 비었다. 영택 씨는 물뿌리개를 들고 승강기로 갔다.":"영택  “계단이 끊긴 층은 바깥으로 나가요. 위층 발코니에서 안으로 다시 들어오면 돼.”"));
            Use("옥상 화분 확인",35.65f,11.25f,()=>Say(upstairs?"영택  “십 년 만에 이걸 타고 물 주러 왔다오.”":"옥상 화분에 물뿌리개 자리가 비어 있다."),1.4f);
            LivingVendor(19,42.5f);
        }

        void BuildMovingKitchen()
        {
            LivingWall(1,1,9.5f,0,1.4f);LivingWall(0,1,35,0,1.38f);
            Deck("포장마차 골목 처마",12.7f,3.55f,5.4f);Hook("처마 사슬",14,5.45f);
            Deck("차고 옥상",24.5f,6.15f,9);Hook("차고 지붕",23.7f,7.95f);
            Deck("뒷골목 입구 위",35.8f,4.1f,5.4f);
            var trolley=Deck("이동식 빨래차",12.3f,.92f,3.5f,.52f);
            var basket=Box("바퀴 달린 빨래 바구니",12.3f,.52f,3.35f,.76f,Tile,false,2);
            var sheet=Box("서로 묶은 빨래",12.3f,.78f,2.56f,.29f,LitGlass,false,3);
            var gate=Box("뒷골목 차고 셔터",37.8f,1.66f,.63f,3.32f,Cloth,true);
            var pad=Box("빨래차 스위치",32.6f,.075f,2.0f,.15f,ReedGold,false,2);
            float trolleyX=12.3f;bool sent=false, opened=Flag("living.laundryCart");
            gate.SetActive(!opened);
            Use("빨래차 제동 풀기",12.2f,1.55f,()=>{sent=true;Say("차가 출발했다. 타고 가거나 차만 보내고 지붕으로 가자.");});
            Step(dt=>
            {
                if(sent)
                {
                    trolleyX=Mathf.MoveTowards(trolleyX,34.6f,dt*3.3f);
                    Place(trolley,trolleyX,.66f);Place(basket,trolleyX,.52f);Place(sheet,trolleyX,.78f);
                    if(trolleyX>=32.3f && !opened)
                    {
                        opened=true;LivingUnlock("living.laundryCart",20);gate.SetActive(false);Place(pad,32.6f,.012f);
                        Say("압판이 눌려 뒷골목 차고가 열렸다. 포장마차가 새 골목으로 들어온다.");
                    }
                }
            });
            LivingVendor(20,6.8f);
            Sign("메뉴와 오늘의 정류장",7.4f,3.45f);
            Portal("장사꾼 뒤따라 야시장",40.3f,1,11,()=>Flag("living.marketShutter"));
            Portal("장사꾼 뒤따라 수리점",43.6f,1,17,()=>Flag("living.reedsFixed"));
        }

        // 세탁소, 수리점, 야경 세탁실에서 같은 숨과 악절이 되돌아온다.
        static AudioClip LivingReedTune()
        {
            if (livingReeds) return livingReeds;
            const int rate = 22050;
            const float beat = .32f;
            int[] notes = { 69,72,74,0, 76,74,72,69, 67,0,69,72, 71,67,64,0,
                            69,72,76,0, 79,76,74,72, 67,71,72,67, 69,0,0,0 };
            int[] bass = { 45,52,48,55,43,50,40,47 };
            var samples = new float[Mathf.RoundToInt(notes.Length * beat * rate)];
            // 파형은 한 번만 만든 뒤 인접 표본을 보간한다. 방에 처음 들어가는 프레임의 합성 비용을 줄인다.
            const int tableSize = 512;
            var reed = new float[tableSize+1];
            var sine = new float[tableSize+1];
            for(int i=0;i<=tableSize;i++)
            {
                float a=2*Mathf.PI*i/tableSize;
                sine[i]=Mathf.Sin(a);
                reed[i]=.50f*sine[i]+.18f*Mathf.Sin(a*2)+.11f*Mathf.Sin(a*3);
            }
            var hz = new float[128];
            for(int i=0;i<hz.Length;i++)hz[i]=440*Mathf.Pow(2,(i-69)/12f);
            for (int i=0; i<samples.Length; i++)
            {
                float t = i/(float)rate;
                int n = Mathf.Min(notes.Length-1,(int)(t/beat));
                float noteAge = t-n*beat;
                float bellows = .68f+.32f*Mathf.Sin(Mathf.PI*((t/(4*beat))%1));
                float envelope = Mathf.Clamp01(noteAge/.024f)*Mathf.Clamp01((beat-noteAge)/.067f);
                float melody = notes[n] == 0 ? 0 : LivingReed(reed,sine,hz[notes[n]]*noteAge);
                float bassAge = t-(n/4)*4*beat;
                float bassVolume = Mathf.Clamp01(bassAge/.05f)*Mathf.Clamp01((beat*2.8f-bassAge)/.14f);
                samples[i] = bellows*(melody*.60f*envelope+.23f*LivingReed(reed,sine,hz[bass[n/4]]*bassAge)*bassVolume);
            }
            livingReeds = AudioClip.Create("빗물 골목 · 손풍금",samples.Length,1,rate,false);
            livingReeds.SetData(samples,0);
            return livingReeds;
        }

        static float LivingReed(float[] reed,float[] sine,float cycles)
        {
            return LivingSample(reed,cycles)+.18f*LivingSample(sine,cycles*1.006f);
        }

        static float LivingSample(float[] table,float cycles)
        {
            float phase=(cycles-(int)cycles)*(table.Length-1);
            int i=Mathf.Min((int)phase,table.Length-2);
            return Mathf.LerpUnclamped(table[i],table[i+1],phase-i);
        }
    }
}
