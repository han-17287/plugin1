using System;

namespace Plugin.AncientCivilization
{
    // Visual state only. Support-relative travel drives the authored four-pose gait;
    // no root position, collider, jump velocity or gameplay timer is modified.
    public sealed class AncientLocomotionMotion
    {
        const float MovingSpeed=.35f,StrideMetres=2.8f,LandingDuration=.30f;
        double gait,landingAge;
        double fieldStride, breath;
        float brakeAge, departureAge, departureDistance;
        int plantedSide;
        // 본편의 세밀한 이동 원화는 전용 프레임 번호를 쓴다. 플러그·연출의 기존 4칸은 유지한다.
        public int FieldFrame { get; private set; }
        const float FieldStrideMetres = 188f / 110f, BrakeDuration = .11f;
        float lowestFall,landingStrength;
        bool initialized,wasGrounded,moving;
        public int Frame {get;private set;}
        public float Angle {get;private set;}
        public float OffsetY {get;private set;}
        public float ScaleX {get;private set;}
        public float ScaleY {get;private set;}

        public AncientLocomotionMotion(){Clear();}
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
        static float Clamp(float value,float low,float high){return Math.Max(low,Math.Min(high,value));}
        static float Blend(float value,float target,float rate,float seconds)
        {return target+(value-target)*(float)Math.Exp(-rate*seconds);}

        public void Clear()
        {
            gait=0;landingAge=LandingDuration;lowestFall=landingStrength=0;
            fieldStride=breath=0; departureAge=departureDistance=0; brakeAge=BrakeDuration; plantedSide=FieldFrame=0;
            initialized=wasGrounded=moving=false;
            Frame=0;Angle=OffsetY=0;ScaleX=ScaleY=1;
        }

        public void Step(bool grounded,float relativeVx,float vy,float facing,float dt)
        {
            // Presentation may be called from a pause or a zero-step native probe.
            // Freeze the entire state, including edge detection, until time resumes.
            if(!Finite(dt)||dt<=0)return;
            dt=Math.Min(dt,.1f);
            relativeVx=Finite(relativeVx)?Clamp(relativeVx,-40,40):0;
            vy=Finite(vy)?Clamp(vy,-40,40):0;
            facing=Finite(facing)&&facing<0?-1:1;
            if(!initialized){initialized=true;wasGrounded=grounded;}
            bool previouslyGrounded=wasGrounded;
            if(grounded)
            {
                if(!wasGrounded&&lowestFall<-3)
                {
                    landingStrength=Clamp((-lowestFall-3)/22,0,1);
                    landingAge=0;
                }
                lowestFall=0;
            }
            else lowestFall=Math.Min(lowestFall,vy);
            wasGrounded=grounded;
            landingAge=Math.Min(LandingDuration,landingAge+dt);

            float speed=Math.Abs(relativeVx),targetAngle,targetX=1,targetY=1;
            SelectFieldFrame(grounded, previouslyGrounded, speed, vy, dt);
            if(grounded)
            {
                if(speed>MovingSpeed)
                {
                    if(!moving)gait=0;
                    gait=(gait+speed*dt*4/StrideMetres)%4;
                    Frame=1+(int)gait;moving=true;
                }
                else {gait=0;moving=false;Frame=0;}
                targetAngle=-Clamp(relativeVx/9,-1,1)*3.5f;
                float u=(float)(landingAge/LandingDuration);
                float compression=landingStrength*.12f*(float)Math.Sin(u*Math.PI*2)*(1-u)*(1-u);
                targetX+=compression*.72f;targetY-=compression;
                // The foot pivot remains planted throughout landing compression.
                OffsetY=landingAge<LandingDuration||!moving?0:
                    .01f*(1-(float)Math.Cos(gait*Math.PI));
            }
            else
            {
                moving=false;Frame=vy>3?1:vy<-3?4:3;OffsetY=0;
                targetAngle=-Clamp(relativeVx/14,-1,1)*5+
                    (vy>3?-1:vy<-3?.7f:0)*facing;
                float extension=vy>3?.035f*Clamp(vy/14,0,1):vy<-3?.025f*Clamp(-vy/20,0,1):0;
                targetY+=extension;targetX-=extension*.5f;
            }
            Angle=Blend(Angle,Clamp(targetAngle,-6,6),12,dt);
            ScaleX=Blend(ScaleX,targetX,30,dt);ScaleY=Blend(ScaleY,targetY,30,dt);
        }

        void SelectFieldFrame(bool grounded, bool previouslyGrounded, float speed, float vy, float dt)
        {
            if (!grounded)
            {
                // 위로 무릎을 치고, 정점에서 몸을 모았다가, 밑창을 먼저 펼친다.
                FieldFrame = 24 + (int)Math.Round(Clamp((14 - vy) / 34, 0, 1) * 7);
                brakeAge = BrakeDuration;
                return;
            }
            if (speed > MovingSpeed)
            {
                if (!moving)
                {
                    fieldStride = .25;
                    departureAge = previouslyGrounded ? 0 : .075f;
                    departureDistance = 0;
                }
                if (landingStrength > .25f && landingAge < .055f)
                {
                    FieldFrame = 32 + Math.Min(3, (int)(landingAge / .055f * 4));
                    return;
                }
                // 첫 발을 지면에서 뗄 때의 좁은 보폭을 거리로 편다. 입력이나 실제 이동은 기다리지 않는다.
                if (departureAge < .075f && departureDistance < .28f)
                {
                    departureAge += dt;
                    departureDistance += speed * dt;
                    float progress = Math.Max(departureAge / .075f, departureDistance / .28f);
                    FieldFrame = 51 - Math.Min(3, (int)(progress * 4));
                    return;
                }
                fieldStride = (fieldStride + speed * dt / FieldStrideMetres) % 1;
                FieldFrame = 8 + (int)(fieldStride * 16);
                return;
            }
            if (moving && previouslyGrounded)
            {
                // 닿기·지나가기 네 자세 중 현재 발과 가장 가까운 모양에서 체중을 회수한다.
                int quarter = (int)(fieldStride * 4 + .5) % 4;
                plantedSide = quarter == 1 ? 2 : quarter == 2 ? 1 : quarter;
                brakeAge = 0;
            }
            if (landingStrength > .08f && landingAge < .18f)
            {
                FieldFrame = 32 + Math.Min(7, (int)(landingAge / .18f * 8));
                brakeAge = BrakeDuration;
                return;
            }
            if (brakeAge < BrakeDuration)
            {
                FieldFrame = 40 + 4 * plantedSide + Math.Min(3, (int)(brakeAge / BrakeDuration * 4));
                brakeAge += dt;
                return;
            }
            breath = (breath + dt / 1.92) % 1;
            FieldFrame = (int)(breath * 8);
        }
    }
}
