using System;
namespace Plugin.PlayerCore
{
    // 타일=유닛인 저작 데이터와 돌진 계산이 함께 사용하는 순수 수학 규칙이다.
    public static class GrappleRules
    {
        public static float Step(float remaining,float speed,float dt)
        {if(!Finite(remaining)||!Finite(speed)||!Finite(dt)||remaining<=0||speed<=0||dt<=0)return 0;return Math.Min(remaining,speed*dt);}
        public static bool Reachable(float dx,float dy,float range,float margin=0)
        {return Finite(dx)&&Finite(dy)&&Finite(range)&&Finite(margin)&&range>margin&&margin>=0&&dx*dx+dy*dy<=(range-margin)*(range-margin);}
        // 쏜 뒤에는 방향을 고정한다. 지연 중 이동과 실제 비행 시간을 분리해 직선 교점을 계산한다.
        public static bool TryIntercept(float dx,float dy,float vx,float vy,float delay,float speed,out float flight)
        {
            flight=0;
            if (!Finite(dx)||!Finite(dy)||!Finite(vx)||!Finite(vy)||!Finite(delay)||!Finite(speed)||speed<=0||delay<0) return false;
            double x=dx+(double)vx*delay, y=dy+(double)vy*delay;
            double a=(double)speed*speed-(double)vx*vx-(double)vy*vy;
            double b=x*vx+y*vy, c=x*x+y*y;
            double discriminant=b*b+a*c;
            if (c<=0 || discriminant<0 || a<=0 && b>=0) return false;
            double root=Math.Sqrt(discriminant);
            // 다가오는 고리는 두 교점 중 빠른 쪽을 고른다. 근을 뺄 때의 정밀도 손실도 피한다.
            double t=b<0 ? c/(root-b) : (b+root)/a;
            flight=(float)t;
            return Finite(flight) && flight>0;
        }
        public static float StrikeMultiplier(float distance,float speed)
        {return Finite(distance)&&Finite(speed)?Math.Min(2.5f,1+Math.Max(0,distance)*.035f+Math.Max(0,speed)*.004f):1;}
        public static float JumpHeight(float launch,float gravity){return launch>0&&gravity>0?launch*launch/(2*gravity):0;}
        public static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
    }
}
