using System;

namespace Plugin.AncientCivilization
{
    // Look ahead into fast carried movement while retaining intentional facing
    // at walking/running speed. Presentation only: no velocity or collision changes.
    public static class AncientCameraRules
    {
        static float Speed(float value){return float.IsNaN(value)||float.IsInfinity(value)?0:value;}
        static float Unit(float value){return Math.Max(0,Math.Min(1,value));}
        public static float LookAhead(float horizontalSpeed,float facing)
        {
            float velocity=Speed(horizontalSpeed),speed=Math.Abs(velocity);
            float look=float.IsNaN(facing)||float.IsInfinity(facing)?1:facing<0?-1:1;
            float magnitude=2.5f+1.5f*Unit(speed/25),carry=Unit((speed-9)/6);
            float travel=velocity<0?-1:1;
            return (look+(travel-look)*carry)*magnitude;
        }
        public static float TravelZoom(bool wired,bool mounted,bool grounded,float horizontalSpeed)
        {
            if(wired)return 7.5f;
            if(mounted)return 6.8f;
            return grounded?5.8f:5.8f+1.7f*Unit((Math.Abs(Speed(horizontalSpeed))-10)/14);
        }
    }
}
