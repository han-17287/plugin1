using System;

namespace Plugin.AncientCivilization
{
    public static class AncientProductionHazardRules
    {
        public const float DebrisHalfWidth=1.4f,DebrisMaxY=1.85f,BombHalfWidth=2.4f,BombMaxY=2.7f;
        public const float BombStartHeight=1.7f,BombStartVelocityY=-2,BombGravity=16,BombFuse=.75f;
        public const float BombFloorHeight=.38f,SpawnLead=18,Drive=17;
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
        public static bool SweptDebris(float prevX,float prevY,float nextX,float nextY,float hazardX)
        {
            if(!Finite(prevX)||!Finite(prevY)||!Finite(nextX)||!Finite(nextY)||!Finite(hazardX))return false;
            double x=(double)prevX-hazardX,dx=(double)nextX-prevX,first=0,last=1;
            if(dx==0)
            {if(Math.Abs(x)>=DebrisHalfWidth)return false;}
            else
            {
                double a=(-DebrisHalfWidth-x)/dx,b=(DebrisHalfWidth-x)/dx;
                first=Math.Max(0,Math.Min(a,b));last=Math.Min(1,Math.Max(a,b));
                // A single touch at the side is not entry into the open danger box.
                if(first>=last)return false;
            }
            double dy=(double)nextY-prevY;
            return prevY+dy*(dy>=0?first:last)<DebrisMaxY;
        }
        public static bool BombHit(float heroX,float heroY,float bombX)
        {
            return Finite(heroX)&&Finite(heroY)&&Finite(bombX)&&
                Math.Abs((double)heroX-bombX)<BombHalfWidth&&heroY<BombMaxY;
        }
        // Returns a detonation in this step, with the actual seconds from the step's start.
        // The fuse starts at ground contact, not at the beginning of its landing frame.
        public static bool TickBomb(ref float y,ref float velocityY,ref float remainingFuse,ref bool landed,float step,out float detonationAfter)
        {
            detonationAfter=0;
            if(!Finite(y)||!Finite(velocityY)||!Finite(remainingFuse)||!Finite(step)||step<=0)return false;
            double flight=0;
            if(!landed&&y>BombFloorHeight)
            {
                double v=velocityY,height=(double)y-BombFloorHeight;
                double landing=(v+Math.Sqrt(v*v+2*BombGravity*height))/BombGravity;
                if(landing>step)
                {
                    y=(float)(y+v*step-.5*BombGravity*step*step);velocityY=(float)(v-BombGravity*step);return false;
                }
                flight=Math.Max(0,landing);
            }
            y=BombFloorHeight;velocityY=0;landed=true;
            double grounded=step-flight,fuse=Math.Max(0,remainingFuse);
            remainingFuse=(float)Math.Max(0,fuse-grounded);
            if(fuse>grounded)return false;
            detonationAfter=(float)Math.Min(step,flight+fuse);return true;
        }
        // Interpolates the hero at a countdown crossing; callers still apply BombHit at that position.
        public static bool BombImpactHero(float prevX,float prevY,float nextX,float nextY,float remainingFuse,float step,out float x,out float y)
        {
            x=y=0;
            if(!Finite(prevX)||!Finite(prevY)||!Finite(nextX)||!Finite(nextY)||!Finite(remainingFuse)||!Finite(step)||step<=0||remainingFuse>step)return false;
            double t=Math.Max(0,remainingFuse)/(double)step;
            x=(float)(prevX+((double)nextX-prevX)*t);y=(float)(prevY+((double)nextY-prevY)*t);return true;
        }
    }
}
