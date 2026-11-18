using System;
namespace Plugin.PlayerCore
{
    // 단위는 타일(월드 1). 렌더러·검증·배치도가 같은 원본을 읽는다.
    public struct DistrictDeck
    {
        public string name;public float left,right,top;public bool bridge;
        public DistrictDeck(string n,float l,float r,float y,bool b=false){name=n;left=l;right=r;top=y;bridge=b;}
    }
    public struct DistrictHook
    {
        public float x,y;public int kind;
        public DistrictHook(float px,float py,int type=0){x=px;y=py;kind=type;}
    }
    public struct DistrictStop
    {
        public string name;public float x,y;
        public DistrictStop(string n,float px,float py){name=n;x=px;y=py;}
    }
    public sealed class DistrictRouteLayout
    {
        public string title,description;public float width,ceiling,gapLeft,gapRight,mechanismX;
        public DistrictDeck[] decks;public DistrictHook[] hooks;public DistrictStop[] stops;
        public DistrictHook Recovery=>new DistrictHook(gapRight-1,7);
        public static DistrictRouteLayout Create(int floor)
        {
            switch(floor)
            {
                case 0:return new DistrictRouteLayout {
                    title="생산구 · 제3 하역선",description="교대문 / 컨베이어 정비교 / 코어 검수동",width=112,ceiling=18,gapLeft=48,gapRight=66,mechanismX=57,
                    decks=new[]{D("교대문 진입로",0,18,0),D("하역 도크",18,30,2),D("검수 앞마당",30,48,0),D("도크 건너편",66,80,2),D("코어 검수동",80,112,0),D("정비 계단참",36,43,5,true),D("컨베이어 정비교",51,59,7,true),D("크레인 승강대",68,76,6,true)},
                    hooks=new[]{H(8,5),H(18,6,1),H(28,7),H(38,8),H(47,9,1),H(56,10,2),H(65,10,1),H(74,9),H(84,6),H(94,5),H(104,5)},
                    stops=new[]{S("교대문",3,1),S("컨베이어 정비소",39,1),S("검수동 입구",85,1)}};
                case 1:return new DistrictRouteLayout {
                    title="생활부 · 불 꺼지지 않는 골목",description="공동 주택 / 진료소 앞뜰 / 감시 외벽",width=120,ceiling=20,gapLeft=51,gapRight=69,mechanismX=60,
                    decks=new[]{D("주거 골목",0,18,0),D("공동 현관",18,30,1),D("진료소 앞뜰",30,51,0),D("폐차장 출입로",69,83,1),D("감시동 현관",83,96,3),D("조직 본거지 방면",96,120,0),D("빨래 건조 발코니",22,29,7,true),D("진료소 외벽 점검대",46,52,5,true),D("배관 보행교",58,65,7,true),D("폐차장 캐노피",73,80,7,true)},
                    hooks=new[]{H(8,5),H(18,9),H(28,10),H(38,6),H(48,8,1),H(57,10,2),H(66,10,1),H(76,10),H(86,8),H(96,6),H(106,5),H(115,5)},
                    stops=new[]{S("주거동 입구",3,1),S("진료소 쉼터",39,1),S("감시동 뒤편",101,1)}};
                case 2:return new DistrictRouteLayout {
                    title="행정구 · 기록 반출 통로",description="접수 회랑 / 원본 운송 셔터 / 감사 기록실",width=114,ceiling=18,gapLeft=54,gapRight=64,mechanismX=57,
                    decks=new[]{D("민원 접수 회랑",0,22,0),D("운송대 진입 턱",22,34,2),D("원본 대조실",34,54,0),D("셔터 대기 섬",54,60,1),D("감사동 운송로",64,82,0),D("기록실 단상",82,94,2),D("증인 안전 회랑",94,114,0),D("열람실 위 정비교",28,35,7,true),D("셔터 위 우회 덕트",53,62,10,true),D("감사동 점검대",73,80,6,true)},
                    hooks=new[]{H(8,5),H(18,6),H(28,9),H(38,10),H(47,10),H(56,13,2),H(65,12),H(75,9,1),H(85,6),H(95,6),H(105,5)},
                    stops=new[]{S("접수 회랑",3,1),S("원본 대조실",41,1),S("감사 기록실",99,1)}};
                case 3:return new DistrictRouteLayout {
                    title="상층 거주구 · 공용 정원",description="관리동 / 환기 온실 / 계약 전시관",width=116,ceiling=24,gapLeft=47,gapRight=66,mechanismX=56,
                    decks=new[]{D("정원 관리로",0,20,0),D("낮은 테라스",20,32,2),D("온실 앞 정원",32,47,0),D("환기구 보호 격자",53,59,2,true),D("정원 상단 테라스",66,80,8),D("전시관 앞뜰",80,95,3),D("계약 전시관",95,116,0),D("온실 외벽 정비대",39,46,6,true),D("고층 화단 보행교",66,75,13,true)},
                    hooks=new[]{H(8,5),H(18,6),H(28,7),H(38,9),H(47,11,1),H(56,12,2),H(65,17),H(75,17),H(84,11),H(94,7),H(104,5)},
                    stops=new[]{S("정원 관리동",3,1),S("온실 관리소",37,1),S("전시관 앞뜰",99,1)}};
                case 4:return new DistrictRouteLayout {
                    title="최상층 · 무기명 정비로",description="폐쇄 승강구 / 점검 외벽 / 직할 통제동",width=118,ceiling=22,gapLeft=49,gapRight=72,mechanismX=60,
                    decks=new[]{D("폐쇄 승강구",0,18,0),D("배선 검사대",18,30,3),D("미승인 우회로",30,49,0),D("통제동 정비 현관",72,85,5),D("무기명 복도",85,98,2),D("직할 승인선",98,118,0),D("외벽 접속대",42,49,6,true),D("감시 장치 정비교",54,61,9,true),D("상부 통신대",65,72,12,true)},
                    hooks=new[]{H(8,5),H(18,7),H(28,8),H(38,6),H(47,10,1),H(56,12,2),H(65,15,2),H(75,15),H(84,10),H(94,7),H(105,5)},
                    stops=new[]{S("폐쇄 승강구",3,1),S("외벽 대기소",37,1),S("통제동 뒷문",103,1)}};
                default:throw new ArgumentOutOfRangeException(nameof(floor));
            }
        }
        static DistrictDeck D(string n,float l,float r,float y,bool b=false)=>new DistrictDeck(n,l,r,y,b);
        static DistrictHook H(float x,float y,int k=0)=>new DistrictHook(x,y,k);
        static DistrictStop S(string n,float x,float y)=>new DistrictStop(n,x,y);
    }
}
