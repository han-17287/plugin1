using System;
namespace Plugin.AncientCivilization
{
    public static class AncientFinaleRules
    {
        public const float Start=800,Finish=1400,Section=120;
        public static int RestorePhase(int saved){saved=Math.Max(0,Math.Min(5,saved));return saved==3||saved==4?2:saved;}
        public static float Road(float x){return Math.Max(0,60-(x-Start)*.1f);}
        public static int Zone(float x){return Math.Max(0,Math.Min(4,(int)((x-Start)/Section)));}
        public static float Checkpoint(int zone){return Start+Math.Max(0,Math.Min(4,zone))*Section+4;}
        public static float TruckSpeed(float playerX,float truckX)
        {return Math.Max(5,Math.Min(15,10+(playerX+12-truckX)*.35f));}
        public static bool CanBoardEnd(float truckX,float playerX,float playerY,bool onRoof)
        {return onRoof&&truckX>=Finish-8&&Math.Abs(playerX-truckX)<2.4f&&playerY>Road(truckX)+2.8f&&playerY<Road(truckX)+5.2f;}
    }
}
