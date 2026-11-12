using System;
namespace Plugin.AncientCivilization
{
    // Presentation only. These rules do not change the rope, collider or launch velocity.
    public static class AncientSwingRules
    {
        public const float ReleaseDuration=.34f,MaximumTilt=60;
        public static int AttachedFrame(float age,float arcDegrees,float velocityX,float velocityY)
        {
            if(!Finite(age)||!Finite(arcDegrees)||!Finite(velocityX)||!Finite(velocityY)||age<0)return 1;
            if(age<.08f)return 0;
            if(age<.18f)return 1;
            if(velocityY<-2)return 2;
            if(Math.Abs(velocityY)<3.5f&&Math.Abs(velocityX)>3)return 3;
            return arcDegrees<50?4:5;
        }
        public static int ReleaseFrame(float elapsed)
        {
            if(!Finite(elapsed)||elapsed<0||elapsed>=ReleaseDuration)return -1;
            return elapsed<.14f?6:7;
        }
        public static float Facing(float velocityX,float fallback)
        {
            if(Finite(velocityX)&&Math.Abs(velocityX)>1)return velocityX<0?-1:1;
            return Finite(fallback)&&fallback<0?-1:1;
        }
        public static float AlignAngle(float ropeX,float ropeY,float emitterX,float emitterY)
        {
            if(!Finite(ropeX)||!Finite(ropeY)||!Finite(emitterX)||!Finite(emitterY))return 0;
            double rx=ropeX,ry=ropeY,ex=emitterX,ey=emitterY;
            if(rx*rx+ry*ry<.00000001||ex*ex+ey*ey<.00000001)return 0;
            double delta=(Math.Atan2(ry,rx)-Math.Atan2(ey,ex))*180/Math.PI;
            if(delta>180)delta-=360;
            else if(delta<-180)delta+=360;
            return (float)Math.Max(-MaximumTilt,Math.Min(MaximumTilt,delta));
        }
        public static float ReleaseAngle(float lastAngle,float elapsed)
        {
            if(!Finite(lastAngle)||!Finite(elapsed))return 0;
            float u=Math.Max(0,Math.Min(1,elapsed/ReleaseDuration));
            return lastAngle*(1-u*u*(3-2*u));
        }
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}
