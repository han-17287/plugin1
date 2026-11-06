using System;

namespace Plugin.AncientCivilization
{
    // Each floor contains one walkable chapter: an introduction, four case
    // sections joined by physical bridges, then the destination and exit lift.
    public static class AncientChapterLayout
    {
        public const int FloorCount=5,CasesPerFloor=4,CaseCount=FloorCount*CasesPerFloor;
        public const float FloorStride=4096,CaseStartOffset=128,CaseWidth=624,CaseStride=688,BridgeWidth=CaseStride-CaseWidth;
        static int Floor(int floor){return Math.Max(0,Math.Min(FloorCount-1,floor));}
        static int Case(int id){return Math.Max(0,Math.Min(CaseCount-1,id));}
        public static float FloorStart(int floor){return Floor(floor)*FloorStride;}
        public static float CaseLeft(int id)
        {id=Case(id);return FloorStart(id/CasesPerFloor)+CaseStartOffset+(id%CasesPerFloor)*CaseStride;}
        public static float CaseRight(int id){return CaseLeft(id)+CaseWidth;}
        public static float CaseEnd(int floor){return CaseRight(Floor(floor)*CasesPerFloor+CasesPerFloor-1);}
        public static float FloorEnd(int floor){return CaseEnd(floor)+96;}
        public static float ExitLift(int floor){return CaseEnd(floor)+76;}
        public static float OfficeX {get{return CaseEnd(4)+82;}}
        public static float SniperLeft {get{return CaseRight(18)+12;}}
        public static float SniperRight {get{return CaseRight(18)+44;}}
        public static float SecretaryLeft {get{return CaseEnd(4)+16;}}
        public static float SecretaryRight {get{return CaseEnd(4)+48;}}
        public static int FloorAt(float x)
        {
            if(float.IsNaN(x)||float.IsInfinity(x))return -1;
            for(int floor=FloorCount-1;floor>=0;floor--)
                if(x>=(floor==0?-5:FloorStart(floor))&&x<=FloorEnd(floor))return floor;
            return -1;
        }
    }
}
