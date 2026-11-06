using System;
namespace Plugin.AncientCivilization
{
    public static class AncientClockRules
    {
        public const float SpinSeconds=3,HourForce=2.4f,MinuteForce=3.4f,UnderSafeHalfWidth=1.15f;
        public static void Angles(float targetRadians,float progress,out float hour,out float minute)
        {
            double u=Math.Max(0,Math.Min(1,progress)),spread=(50+1080*Math.Pow(1-u,3))*Math.PI/180;
            double x=(HourForce+MinuteForce)*Math.Cos(spread),y=(HourForce-MinuteForce)*Math.Sin(spread);
            double basis=targetRadians-Math.Atan2(y,x);
            hour=(float)(basis+spread);minute=(float)(basis-spread);
        }
        public static bool BroadHit(float relativeX,float relativeY,float radius)
        {return relativeX*relativeX+relativeY*relativeY<=radius*radius&&!(Math.Abs(relativeX)<=UnderSafeHalfWidth&&relativeY<-.75f);}
        public static float BroadRadius(float arenaWidth)
        {
            if(float.IsNaN(arenaWidth)||float.IsInfinity(arenaWidth)||arenaWidth<=0)return 11;
            return Math.Max(11,(float)Math.Sqrt(arenaWidth*arenaWidth*.25f+100));
        }
        public static bool ThirdVectorShot(int index){return index%3==2;}
    }
}
