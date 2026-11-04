namespace Plugin.AncientCivilization
{
    public static class AncientCityLayout
    {
        public const int LastFloor=4;
        public static string Name(int floor)
        {return floor==0?"하층 생산구":floor==1?"하부 생활구":floor==2?"중층 행정구":floor==3?"상층 거주구":"최상층 통제구";}
        public static int VisualSector(float x)
        {
            int room=AncientCampaignRules.RoomAt(x);if(room>=0)return room/4;
            if(x>=790)return 5;
            for(int floor=LastFloor;floor>=1;floor--)if(x>=Left(floor)-3&&x<=Right(floor)+3)return floor;
            return 0;
        }
        public static float Left(int floor){return floor==0?-5:floor==1?128:floor==2?300:floor==3?460:620;}
        public static float Right(int floor){return floor==0?118:floor==1?282:Left(floor)+126;}
        public static float Entry(int floor){return floor==0?115:Left(floor)+4;}
        public static float ArenaLeft(int floor){return floor==1?258:Left(floor)+106;}
        public static float ArenaRight(int floor){return floor==1?280:Left(floor)+124;}
        public static AncientTraversalLayout.Ledge[] Route(int floor)
        {
            float x=Left(floor);
            if(floor==3)return new[]{new AncientTraversalLayout.Ledge(x,x+14,0),
                new AncientTraversalLayout.Ledge(x+18,x+27,1.5f),new AncientTraversalLayout.Ledge(x+31,x+39,3),
                new AncientTraversalLayout.Ledge(x+43,x+52,1),new AncientTraversalLayout.Ledge(x+57,x+66,3),
                new AncientTraversalLayout.Ledge(x+70,x+79,1.5f),new AncientTraversalLayout.Ledge(x+84,x+93,3.2f),
                new AncientTraversalLayout.Ledge(x+98,x+105,1),new AncientTraversalLayout.Ledge(x+106,x+126,0)};
            if(floor==4)return new[]{new AncientTraversalLayout.Ledge(x,x+12,0),
                new AncientTraversalLayout.Ledge(x+17,x+26,1),new AncientTraversalLayout.Ledge(x+31,x+39,2),
                new AncientTraversalLayout.Ledge(x+44,x+52,1),new AncientTraversalLayout.Ledge(x+57,x+65,2.4f),
                new AncientTraversalLayout.Ledge(x+70,x+78,1),new AncientTraversalLayout.Ledge(x+83,x+91,2),
                new AncientTraversalLayout.Ledge(x+96,x+103,1),new AncientTraversalLayout.Ledge(x+106,x+126,0)};
            return new[]{new AncientTraversalLayout.Ledge(x,x+12,0),
                new AncientTraversalLayout.Ledge(x+17,x+24,1.5f),new AncientTraversalLayout.Ledge(x+29,x+36,2),
                new AncientTraversalLayout.Ledge(x+41,x+48,1.2f),new AncientTraversalLayout.Ledge(x+53,x+60,2.8f),
                new AncientTraversalLayout.Ledge(x+65,x+72,1.5f),new AncientTraversalLayout.Ledge(x+77,x+84,2.2f),
                new AncientTraversalLayout.Ledge(x+89,x+98,1),new AncientTraversalLayout.Ledge(x+103,x+126,0)};
        }
        public static bool SafeResume(int floor,float x,float y)
        {return floor>=0&&floor<=LastFloor&&x>Left(floor)&&x<Right(floor)&&y>=.5f&&y<8.5f;}
        public static bool CanLift(int stage,int points,int cleared,int current,int target)
        {
            if(current<0||current>LastFloor||target<0||target>=LastFloor||current==target)return false;
            if(target==0)return true;
            if(stage<8||points<1000)return false;
            if(target==1)return true;
            return (cleared&(1<<(target-1)))!=0;
        }
        public static bool CanInfiltrate(int cleared,int current)
        {return current==3&&(cleared&(1<<3))!=0;}
        public static bool ShutterClosed(float time,float phase){return ((time+phase)%3.6f+3.6f)%3.6f>=2;}
        public static bool ShutterWarning(float time,float phase)
        {float t=((time+phase)%3.6f+3.6f)%3.6f;return t>=1.55f&&t<2;}
        public static float VentLift(float speed,float delta)
        {return System.Math.Max(speed,System.Math.Min(12,System.Math.Max(6,speed+64*System.Math.Max(0,delta))));}
    }
}
