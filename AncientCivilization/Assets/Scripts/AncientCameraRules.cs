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
        // 빨래골목의 기본 줌에서 다음 디딤면과 발밑을 동시에 남기는 시선 오프셋이다.
        public static float LaundryLeadX(float vx, float facing, bool hasDock, float toDock)
        {
            float neutral = facing < 0 ? -1.7f : 1.7f;
            float momentum = Math.Clamp(Speed(vx) * .30f, -4.4f, 4.4f);
            float weight = Unit(Math.Abs(Speed(vx)) / 8);
            float lead = neutral + (momentum - neutral) * weight;
            return hasDock ? lead * .35f + Math.Clamp(Speed(toDock) * .62f, -4.4f, 4.4f) * .65f : lead;
        }

        public static float LaundryLeadY(float vy, bool hasDock, float toDock) =>
            hasDock ? 1.2f + Math.Clamp(Speed(toDock) * .35f, -1.6f, 1.6f) :
            1.7f + Math.Clamp(Speed(vy) * .14f, -2.1f, 1.3f);

        public static float EaseLook(float current, float target, float dt) =>
            current + (target - current) * (1f - (float)Math.Exp(-7f * Math.Clamp(Speed(dt), 0, .1f)));

        public static float LaundryFrameAxis(float current, float actor, float look, float dt,
            float halfExtent, float headroom, float footroom, float minCentre, float maxCentre)
        {
            float target = Math.Clamp(actor + look, minCentre, maxCentre);
            float smooth = 1 - (float)Math.Exp(-8 * Math.Max(0, dt));
            float frame = current + (target - current) * smooth;
            float lower = actor - Math.Max(.1f, halfExtent - headroom);
            float upper = actor + Math.Max(.1f, halfExtent - footroom);
            return Math.Clamp(Math.Clamp(frame, lower, upper), minCentre, maxCentre);
        }

        public static float TravelZoom(bool wired,bool mounted,bool grounded,float horizontalSpeed)
        {
            if(wired)return 7.5f;
            if(mounted)return 6.8f;
            return grounded?5.8f:5.8f+1.7f*Unit((Math.Abs(Speed(horizontalSpeed))-10)/14);
        }
    }
}
