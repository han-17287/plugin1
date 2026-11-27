using System;
namespace Plugin.PlayerCore
{
    public enum TraversalFloor { Production,Living,Administration,Residence,Top }
    // 각 층의 기하와 리듬을 런타임·내보내기·검사가 공유한다.
    public static class FloorTraversalLayout
    {
        public const int FloorCount=5;
        public static readonly string[] Names={"생산구","생활부","행정구","상층 거주구","최상층"};
        public static readonly string[] Hints={"안전한 바닥에서 세 콘센트로 먼저 연습하세요.","탐조등에 계속 머무르면 경보. 견인으로 빠져나오거나 위쪽 콘센트로 우회하세요.","보안 셔터가 열리길 기다리거나 상부 콘센트로 우회하세요.","보호 발판 위 환기류로 올라가세요. 견인 중에는 바람의 영향을 받지 않습니다.","불이 꺼진 콘센트는 잠깐 사용할 수 없습니다. 다시 켜지길 기다리거나 위쪽 길로 우회하세요."};
        public static readonly string[] Resources={"Production","Living","Administration","Residence","Top"};
        public static readonly string[,] Stages={
            {"교대 통로","운송 벨트","검수 회랑","장비 검수실","코어 재고 관리실"},
            {"주거 골목","진료소 앞길","폐차 적치장","감시 외벽","로봇 격납고"},
            {"민원 접수로","차량 조회실","감사 차단구","원본 대조실","증인 안전회랑"},
            {"조경 조명로","공용 분기실","계약 전시실","주소 검문로","외벽 접근 정원"},
            {"무기명 정비로","미승인 우회로","삭제 기록층","저격수 전방교","직할 승인선"}};
        public static readonly int[,] Flow={ {96,144,180,120,72},{120,144,168,144,96},{120,144,168,120,96},{144,168,144,168,120},{144,168,180,120,96} };
        public static readonly int[,] Rest={ {16,16,20,20,24},{20,20,20,20,24},{20,20,20,24,24},{20,20,24,24,24},{24,24,24,24,28} };
        public static int Challenge(int floor,int stage){return Flow[floor,stage]/3;}
        public static void Anchor(TraversalFloor floor,int index,int count,int gap,out float x,out float y)
        {
            x=6+index*gap;
            if(index==0||index==count+1){y=4;return;}
            switch(floor)
            {
                case TraversalFloor.Living:y=index%2==0?7:5;break;
                case TraversalFloor.Administration:y=index%3==0?5:7;break;
                case TraversalFloor.Residence:y=index%2==0?9:6;break;
                case TraversalFloor.Top:y=index%2==0?8:5;break;
                default:TraversalLayout.Anchor(index,out x,out y,gap);break;
            }
        }
        public static float GateX(int count,int gap){return 6+Math.Min(2,count)*gap+gap*.5f;}
        public static float WaitLeft(float gate){return gate-5;}
        public static float WaitRight(float gate){return gate;}
        public static bool Open(float age,float period,float duration,float phase=0)
        {
            if(!GrappleRules.Finite(age)||!GrappleRules.Finite(period)||!GrappleRules.Finite(duration)||!GrappleRules.Finite(phase)||period<=0||duration<=0)return false;
            float local=(age+phase)%period;if(local<0)local+=period;return local<Math.Min(duration,period);
        }
        public static bool ShutterOverlap(float playerX,float playerY,float gateX)
        {return Math.Abs(playerX-gateX)<.36f+.375f+.03f&&playerY-.9f<8&&playerY+.9f>0;}
        public static bool ScanOverlap(float playerX,float playerY,float beamX,float width)
        {return Math.Abs(playerX-beamX)<.36f+Math.Max(0,width)*.5f&&playerY-.9f<8&&playerY+.9f>0;}
    }
}
