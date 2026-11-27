using System;
namespace Plugin.AncientCivilization
{
    public static class AncientClockRules
    {
        public const float SpinSeconds=3,HourForce=2.4f,MinuteForce=3.4f,UnderSafeHalfWidth=1.15f;
        public const int RainWaves = 7;
        public const float RainFirstTell = .9f, RainBeat = .65f, RainAfterRelease = .30f;
        public const float RainHeight = 8.6f, SpearHitRadius = .65f;
        // 각 소절마다 모든 구역에서 동시에 내린다. 오프셋은 전투장 기준으로만 결정된다.
        static readonly float[] rainOffsets = { .125f, .625f, .375f, .875f, .5f, .25f, .75f };

        public static int RainLaneCount(float width) => Math.Max(1, (int)Math.Ceiling(width / 4.5f));
        public static float RainX(float left, float right, int lanes, int volley, int lane) =>
            left + (right - left) * ((lane + rainOffsets[volley]) / lanes);
        public static int RainHand(int volley, int lane) => (lane + volley) % 3;
        public static float RainSpeed(int hand) => 7.5f + hand;
        // 땅에 꽂힌 침을 일찍 지우지 않도록 직전 벡터 창 다섯 개의 자리도 남긴다.
        public static int SpearCapacity(int lanes) => RainWaves * lanes + 5;
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
    // A sweep is one attack even when its visual hands keep rotating for several frames.
    public struct AncientClockAreaDamage
    {
        bool spent;
        public bool CanHit {get{return !spent;}}
        public void Begin(){spent=false;}
        public void RecordHealth(float hpBefore,float hpAfter,float bikeBefore,float bikeAfter)
        {if(hpAfter<hpBefore||bikeAfter<bikeBefore)spent=true;}
    }
}
