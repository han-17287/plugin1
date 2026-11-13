using System;

namespace Plugin.AncientCivilization
{
    // Truck-local body coordinates for the directed third-mount recovery. The only
    // intentional velocity impulse is the vertical jump at TakeoffSeconds.
    public static class AncientProductionMotion
    {
        public const float AlongsideSeconds=1.5f,TakeoffSeconds=3.2f,HoverSeconds=6.3f,LandingSeconds=8.45f;
        public const float LaserSeconds=7.1f,TruckStopSeconds=7.65f,CargoReleaseSeconds=2.65f,CargoSlideSeconds=1.9f;
        public const float EndSeconds=AncientProductionRules.FinisherSeconds,TruckSpeed=12,SaddleY=.84f;
        static float Unit(float elapsed,float begin,float end)
        {return float.IsNaN(elapsed)?0:Math.Max(0,Math.Min(1,(elapsed-begin)/(end-begin)));}
        static float Smooth(float value){return value*value*(3-2*value);}
        static float Lerp(float a,float b,float t){return a+(b-a)*t;}
        static float Time(float elapsed)
        {return float.IsNaN(elapsed)?0:Math.Max(0,Math.Min(EndSeconds,elapsed));}
        public static float TruckTravel(float elapsed)
        {return Math.Min(Time(elapsed),TruckStopSeconds)*TruckSpeed;}
        public static float BikeOffsetX(float elapsed)
        {
            float u=Unit(elapsed,0,AlongsideSeconds);
            return Lerp(-22,-.6f,Smooth(u))+7.5f*u*(1-u)*(1-u);
        }
        // This derivative is the mounted bike's WORLD speed, including the truck's
        // movement. Once dismounted, its rider presentation and engine are hidden.
        public static float BikeSpeed(float elapsed)
        {
            float time=Time(elapsed);if(time>=TakeoffSeconds)return 0;
            float u=Unit(time,0,AlongsideSeconds);
            return TruckSpeed+(21.4f*(6*u-6*u*u)+7.5f*(3*u*u-4*u+1))/AlongsideSeconds;
        }
        public static float HeroOffsetX(float elapsed)
        {
            float time=Time(elapsed);
            if(time<AlongsideSeconds)return BikeOffsetX(time);
            if(time<TakeoffSeconds)return -.6f;
            if(time<HoverSeconds)return Lerp(-.6f,-5.5f,Smooth(Unit(time,TakeoffSeconds,HoverSeconds)));
            if(time<LandingSeconds)return -5.5f;
            return Lerp(-5.5f,-8,Smooth(Unit(time,LandingSeconds,EndSeconds)));
        }
        public static float HeroY(float elapsed)
        {
            float time=Time(elapsed);
            if(time<TakeoffSeconds)return SaddleY;
            if(time<HoverSeconds)
            {
                float u=Unit(time,TakeoffSeconds,HoverSeconds);
                // Hermite: launch at12m/s from the saddle, then arrive at the laser
                // pose with zero vertical speed while staying below the tunnel roof.
                return Lerp(SaddleY,7.2f,Smooth(u))+37.2f*u*(1-u)*(1-u);
            }
            if(time<LandingSeconds)return 7.2f;
            return Lerp(7.2f,1.3f,Smooth(Unit(time,LandingSeconds,EndSeconds)));
        }
        public static float CargoOffsetX(float initial,float elapsedSinceRelease)
        {return Lerp(float.IsNaN(initial)||float.IsInfinity(initial)?-.5f:initial,-11,Smooth(Unit(elapsedSinceRelease,0,CargoSlideSeconds)));}
        public static float CargoY(float initial,float elapsedSinceRelease)
        {return Lerp(float.IsNaN(initial)||float.IsInfinity(initial)?1.45f:initial,1,Smooth(Unit(elapsedSinceRelease,0,CargoSlideSeconds)));}
    }
}
