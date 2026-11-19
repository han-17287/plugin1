using System;

namespace Plugin.AncientCivilization
{
    // The bright blue body defines collision; the wider additive sheath is light only.
    public static class AncientBeamRules
    {
        public const float Range=24,Duration=.38f,FadeSeconds=.12f,CastDepth=.02f,MinimumLength=.025f;
        public static float Power(float energy)
        {return Finite(energy)?Math.Max(0,Math.Min(1,energy/100)):0;}
        public static float BodyWidth(float energy){return 1+Power(energy)*1.2f;}
        public static float CoreWidth(float energy){return .28f+Power(energy)*.42f;}
        public static float SheathWidth(float energy){return 2.1f+Power(energy)*1.9f;}
        public static float Opacity(float remaining)
        {return Finite(remaining)?Math.Max(0,Math.Min(1,remaining/FadeSeconds)):0;}

        // A flat-ended beam rectangle against a target circle. A wide beam must not
        // gain an equally wide invisible capsule behind its emitter or beyond a wall.
        public static bool IntersectsCircle(float dx,float dy,float targetX,float targetY,float halfWidth,float targetRadius)
        {
            if(!Valid(dx,dy,targetX,targetY,halfWidth,targetRadius))return false;
            float length=(float)Math.Sqrt(dx*dx+dy*dy);if(length<MinimumLength)return false;
            float along=(targetX*dx+targetY*dy)/length,across=Math.Abs(targetX*dy-targetY*dx)/length;
            float endGap=Math.Max(0,Math.Max(-along,along-length));
            float edgeGap=Math.Max(0,across-halfWidth);
            return endGap*endGap+edgeGap*edgeGap<=targetRadius*targetRadius;
        }

        // Separating-axis test for the flat beam and an axis-aligned vehicle box.
        public static bool IntersectsBox(float dx,float dy,float centerX,float centerY,float halfWidth,float boxHalfX,float boxHalfY)
        {
            if(!Valid(dx,dy,centerX,centerY,halfWidth,boxHalfX)||!Finite(boxHalfY)||boxHalfY<0)return false;
            float length=(float)Math.Sqrt(dx*dx+dy*dy);if(length<MinimumLength)return false;
            float ux=dx/length,uy=dy/length,vx=-uy,vy=ux;
            float x=centerX-dx*.5f,y=centerY-dy*.5f,halfLength=length*.5f;
            if(Math.Abs(x*ux+y*uy)>halfLength+boxHalfX*Math.Abs(ux)+boxHalfY*Math.Abs(uy))return false;
            if(Math.Abs(x*vx+y*vy)>halfWidth+boxHalfX*Math.Abs(vx)+boxHalfY*Math.Abs(vy))return false;
            if(Math.Abs(x)>halfLength*Math.Abs(ux)+halfWidth*Math.Abs(vx)+boxHalfX)return false;
            return Math.Abs(y)<=halfLength*Math.Abs(uy)+halfWidth*Math.Abs(vy)+boxHalfY;
        }

        static bool Valid(float dx,float dy,float x,float y,float halfWidth,float radius)
        {return Finite(dx)&&Finite(dy)&&Finite(x)&&Finite(y)&&Finite(halfWidth)&&Finite(radius)&&halfWidth>=0&&radius>=0;}
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}
