using System;
namespace Plugin.AncientCivilization
{
    // Straight pulling uses no gravity, tangential kick or acceleration away from the clicked ray.
    public static class AncientZipRules
    {
        public const float Speed=30,Range=22,Skin=.045f,Arrival=.055f,MaximumSeconds=3.5f;
        public static void Velocity(float dx,float dy,float deltaTime,out float x,out float y)
        {
            x=y=0;if(!Finite(dx)||!Finite(dy)||!Finite(deltaTime)||deltaTime<=0)return;
            float length=(float)Math.Sqrt(dx*dx+dy*dy);if(length<=Arrival)return;
            float speed=Math.Min(Speed,length/deltaTime);x=dx/length*speed;y=dy/length*speed;
        }
        public static bool Arrived(float dx,float dy)
        {return Finite(dx)&&Finite(dy)&&dx*dx+dy*dy<=Arrival*Arrival;}
        // Test the actual collision-clipped planned physics segment, not just its
        // starting gap. A moving carrier advances again before the next FixedUpdate;
        // testing only that later gap can chase it forever at carrierSpeed*deltaTime.
        public static bool ReachesThisStep(float dx,float dy,float stepX,float stepY)
        {
            if(!Finite(dx)||!Finite(dy)||!Finite(stepX)||!Finite(stepY))return false;
            double x=dx,y=dy,sx=stepX,sy=stepY,length=sx*sx+sy*sy;
            double t=length>0?Math.Max(0,Math.Min(1,(x*sx+y*sy)/length)):0;
            double remainingX=x-sx*t,remainingY=y-sy*t;
            return remainingX*remainingX+remainingY*remainingY<=(double)Arrival*Arrival;
        }
        public static float Support(float normalX,float normalY,bool bike)
        {
            if(!Finite(normalX)||!Finite(normalY))return bike?1.5f:1.1f;
            float length=(float)Math.Sqrt(normalX*normalX+normalY*normalY);if(length<.0001f)return Skin;
            float x=Math.Abs(normalX/length),y=Math.Abs(normalY/length);
            return (bike?1.425f*x+.9f*y-.085f*(normalY/length):.36f+.69f*y)+Skin;
        }
        public static float SafeDistance(float requested,float hitDistance)
        {
            if(!Finite(requested)||requested<=0||float.IsNaN(hitDistance))return 0;
            return Math.Max(0,Math.Min(requested,hitDistance-Skin));
        }
        public static bool BlocksInitialContact(float closestX,float closestY,float directionX,float directionY)
        {
            if(!Finite(closestX)||!Finite(closestY)||!Finite(directionX)||!Finite(directionY))return true;
            if(closestX*closestX+closestY*closestY<.00001f)return true;
            return closestX*directionX+closestY*directionY>.001f;
        }
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}
