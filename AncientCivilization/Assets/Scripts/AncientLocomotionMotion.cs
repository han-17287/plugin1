using System;

namespace Plugin.AncientCivilization
{
    // Visual state only. Support-relative travel drives the authored four-pose gait;
    // no root position, collider, jump velocity or gameplay timer is modified.
    public sealed class AncientLocomotionMotion
    {
        const float MovingSpeed=.35f,StrideMetres=2.8f,LandingDuration=.30f;
        double gait,landingAge;
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
    }
}
