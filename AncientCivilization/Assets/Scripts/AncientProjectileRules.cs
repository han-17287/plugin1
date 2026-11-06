using System;
namespace Plugin.AncientCivilization
{
    // First contact along one render-frame sweep. Terrain and targets share this time interval.
    public static class AncientProjectileRules
    {
        public static bool CircleEntry(float dx,float dy,float targetX,float targetY,float radius,out float entry)
        {
            entry=0;
            if(!Finite(dx)||!Finite(dy)||!Finite(targetX)||!Finite(targetY)||!Finite(radius)||radius<=0)return false;
            double c=(double)targetX*targetX+(double)targetY*targetY-(double)radius*radius;
            if(c<0)return true;
            double a=(double)dx*dx+(double)dy*dy;if(a<=1e-12)return false;
            double projection=(double)targetX*dx+(double)targetY*dy;
            double discriminant=projection*projection-a*c;if(discriminant<=0)return false;
            double first=(projection-Math.Sqrt(discriminant))/a;
            if(first<0||first>=1)return false;entry=(float)first;return true;
        }
        public static bool BoxEntry(float dx,float dy,float minX,float minY,float maxX,float maxY,out float entry)
        {
            entry=0;
            if(!Finite(dx)||!Finite(dy)||!Finite(minX)||!Finite(minY)||!Finite(maxX)||!Finite(maxY)||minX>maxX||minY>maxY)return false;
            double first=0,last=1;
            if(!Axis(dx,minX,maxX,ref first,ref last)||!Axis(dy,minY,maxY,ref first,ref last))return false;
            entry=(float)first;return true;
        }
        static bool Axis(float direction,float minimum,float maximum,ref double first,ref double last)
        {
            if(Math.Abs(direction)<1e-12)return minimum<=0&&maximum>=0;
            double a=minimum/(double)direction,b=maximum/(double)direction;
            first=Math.Max(first,Math.Min(a,b));last=Math.Min(last,Math.Max(a,b));return first<=last;
        }
        public static float BoxOcclusionReach(float directionX,float directionY,float halfX,float halfY,float offsetX,float offsetY)
        {
            if(!Finite(directionX)||!Finite(directionY)||!Finite(halfX)||!Finite(halfY)||!Finite(offsetX)||!Finite(offsetY)||halfX<0||halfY<0)return 0;
            return halfX*Math.Abs(directionX)+halfY*Math.Abs(directionY)+Math.Abs(offsetX*directionX+offsetY*directionY);
        }
        public static bool BeforeSurface(float entry,float surfaceFraction,float surfaceToTargetX,float surfaceToTargetY,float normalX,float normalY)
        {
            if(!Finite(entry)||!Finite(surfaceFraction)||!Finite(surfaceToTargetX)||!Finite(surfaceToTargetY)||!Finite(normalX)||!Finite(normalY))return false;
            // A generous target radius must not reach through a thin wall from its far side.
            return entry>=0&&entry<surfaceFraction&&(double)surfaceToTargetX*normalX+(double)surfaceToTargetY*normalY>=0;
        }
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}
