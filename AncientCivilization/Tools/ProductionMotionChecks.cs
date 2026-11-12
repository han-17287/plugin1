using System;
using Plugin.AncientCivilization;

class ProductionMotionChecks
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static bool Near(float a,float b,float epsilon=.0001f){return Math.Abs(a-b)<=epsilon;}
    static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    static float WorldX(float time){return AncientProductionMotion.TruckTravel(time)+AncientProductionMotion.HeroOffsetX(time);}
    static float BikeWorldX(float time){return AncientProductionMotion.TruckTravel(time)+AncientProductionMotion.BikeOffsetX(time);}
    static void Continuous(float time,bool verticalImpulse=false)
    {
        const float epsilon=1/2400f;
        float x=AncientProductionMotion.HeroOffsetX(time),y=AncientProductionMotion.HeroY(time);
        float xl=AncientProductionMotion.HeroOffsetX(time-epsilon),xr=AncientProductionMotion.HeroOffsetX(time+epsilon);
        float yl=AncientProductionMotion.HeroY(time-epsilon),yr=AncientProductionMotion.HeroY(time+epsilon);
        Check(Math.Abs(xl-x)<.002f&&Math.Abs(xr-x)<.002f&&Math.Abs(yl-y)<.006f&&Math.Abs(yr-y)<.006f,
            "The body cannot pop between the mounted, airborne, firing and landing poses at "+time);
        Check(Math.Abs((x-xl)/epsilon-(xr-x)/epsilon)<.035f,"Horizontal velocity is continuous across the authored pose handoff at "+time);
        if(verticalImpulse)
        {
            Check(Math.Abs((y-yl)/epsilon)<.01f&&Near((yr-y)/epsilon,12,.035f),"Takeoff preserves its intentional12m/s vertical launch from a stationary saddle");
        }
        else Check(Math.Abs((y-yl)/epsilon-(yr-y)/epsilon)<.035f,"Vertical velocity eases continuously into or out of the firing pose at "+time);
    }
    static void Replay(float[] steps)
    {
        float time=0,previousX=WorldX(0),previousY=AncientProductionMotion.HeroY(0),release=-1;
        int frame=0;bool laserSeen=false,airborne=false,landing=false;
        while(time<AncientProductionMotion.EndSeconds&&frame<10000)
        {
            float step=Math.Min(AncientProductionRules.MaxTickSeconds,steps[frame++%steps.Length]);
            float next=Math.Min(AncientProductionMotion.EndSeconds,time+step),x=WorldX(next),y=AncientProductionMotion.HeroY(next);
            Check(Finite(x)&&Finite(y)&&y+1.05f<AncientProductionLayout.TunnelCeiling,"Each sampled cinematic pose and the officer's actual capsule head fit below the physical tunnel roof");
            float dx=x-previousX,dy=y-previousY;
            Check(Math.Sqrt(dx*dx+dy*dy)<=40*(next-time)+.002f,"A coarse or boundary-crossing frame moves along a bounded path without a teleport");
            if(next>AncientProductionMotion.CargoReleaseSeconds&&release<0)release=next;
            if(next>=AncientProductionMotion.TakeoffSeconds&&next<AncientProductionMotion.HoverSeconds)airborne=true;
            if(next>=AncientProductionMotion.LandingSeconds)landing=true;
            if(next>=AncientProductionMotion.LaserSeconds&&next<AncientProductionMotion.LandingSeconds)
            {
                laserSeen=true;Check(release>=0,"The finishing beam cannot begin before the core cradle has been released");
                float cargoX=AncientProductionMotion.CargoOffsetX(-.5f,next-release);
                float cargoY=AncientProductionMotion.CargoY(1.45f,next-release);
                Check(Near(cargoX,-11)&&Near(cargoY,1),"Even late release on a hitch completes the cargo slide before the first laser frame");
                // Cargo cradle half-width1.9 and the beam sheath half-width2.1 are
                // the actual authored renderer sizes. A horizontal separation alone
                // proves clearance from every point of the directed beam segment.
                float leftBeam=AncientProductionMotion.HeroOffsetX(next)+.3f;
                Check(leftBeam-(cargoX+1.9f)>2.1f,"The separated cores stay clear of the full bright laser sheath, rather than only its centerline");
                Check(AncientProductionMotion.HeroY(next)+.55f+2.1f<AncientProductionLayout.TunnelCeiling,"The beam emitter and its full sheath stay below the tunnel roof");
            }
            previousX=x;previousY=y;time=next;
        }
        Check(time==AncientProductionMotion.EndSeconds&&airborne&&laserSeen&&landing,"Smooth and uneven pacing both include takeoff, laser and landing before the scene ends");
        Check(Near(AncientProductionMotion.HeroOffsetX(time),-8)&&Near(previousY,1.3f),"Every frame profile reaches the real post-explosion arrest landing point");
    }
    static void Main()
    {
        Check(Near(AncientProductionMotion.HeroOffsetX(0),-22)&&Near(AncientProductionMotion.HeroY(0),.84f),"The third mount starts at the same22m following gap and saddle height as the incoming motorcycle");
        Check(Near(AncientProductionMotion.HeroOffsetX(3.2f),-.6f)&&Near(AncientProductionMotion.HeroY(3.2f),.84f),"The backflip starts exactly where the mounted blade cut left the rider");
        Check(Near(AncientProductionMotion.HeroOffsetX(6.3f),-5.5f)&&Near(AncientProductionMotion.HeroY(6.3f),7.2f),"The officer reaches the authored firing position without a separate reposition");
        foreach(float boundary in new[]{1.5f,3.2f,6.3f,8.45f,10.5f})Continuous(boundary,boundary==3.2f);
        float highest=0;
        for(int i=0;i<=10500;i++)
        {
            float time=i/1000f,x=AncientProductionMotion.HeroOffsetX(time),y=AncientProductionMotion.HeroY(time);
            highest=Math.Max(highest,y);
            Check(Finite(x)&&Finite(y)&&x>=-22.0001f&&x<=-.5999f&&y>=.8399f,"The authored body path stays finite and inside its intended truck-relative envelope");
            Check(y+1.05f<AncientProductionLayout.TunnelCeiling-.5f,"The whole officer has at least half a meter of roof clearance throughout the flip");
            Check(y+1.05f<5.2f+9.5f&&y-1.05f>5.2f-9.5f,"The complete body remains in the authored finishing camera's vertical view");
        }
        Check(highest>8.5f&&highest<9,"The backflip still reads as a high leap while keeping the roof clearance");
        for(int i=1;i<3200;i++)
        {
            float time=i/1000f;const float h=.0005f;
            float derivative=(BikeWorldX(time+h)-BikeWorldX(time-h))/(2*h),reported=AncientProductionMotion.BikeSpeed(time);
            Check(Finite(reported)&&reported>=11.99f&&reported<34,"The visible motorcycle engine/wheel speed never reverses or spikes beyond the authored acceleration");
            Check(Near(derivative,reported,.035f),"Motorcycle wheel and engine speed agree with the actual position derivative before dismount");
        }
        Check(Near(AncientProductionMotion.BikeSpeed(0),17)&&Near(AncientProductionMotion.BikeSpeed(1.5f),12)&&AncientProductionMotion.BikeSpeed(3.2f)==0,"The visual motorcycle matches incoming17m/s, settles alongside at12, and releases its rider at takeoff");
        float oldX=-.5f,oldY=1.45f;
        for(int i=0;i<=1900;i++)
        {
            float elapsed=i/1000f,x=AncientProductionMotion.CargoOffsetX(-.5f,elapsed),y=AncientProductionMotion.CargoY(1.45f,elapsed);
            Check(x<=oldX+.00001f&&y<=oldY+.00001f&&y>=.9999f,"The released core cradle slides smoothly backward and down without reversing or dipping through the road");
            if(i>0)Check((oldX-x)/.001f<9&&Math.Abs(oldY-y)/.001f<1,"Cargo separation has bounded velocity instead of an instantaneous11m relocation");
            oldX=x;oldY=y;
        }
        Check(Near(oldX,-11)&&Near(oldY,1),"The cradle is fully safe after its1.9second slide");
        foreach(float time in new[]{0f,1.9f})
        {
            const float h=.001f;
            Check(Math.Abs(AncientProductionMotion.CargoOffsetX(-.5f,time+h)-AncientProductionMotion.CargoOffsetX(-.5f,time-h))/(2*h)<.02f,"The cargo begins and ends separation without a horizontal velocity snap");
        }
        foreach(float[] steps in new[]{new[]{1/30f},new[]{1/60f},new[]{1/144f},new[]{1/144f,.7f,1/30f,.2f,1/60f}})Replay(steps);
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,float.MaxValue,-float.MaxValue})
            Check(Finite(AncientProductionMotion.TruckTravel(bad))&&Finite(AncientProductionMotion.BikeOffsetX(bad))&&Finite(AncientProductionMotion.BikeSpeed(bad))&&Finite(AncientProductionMotion.HeroOffsetX(bad))&&Finite(AncientProductionMotion.HeroY(bad))&&Finite(AncientProductionMotion.CargoOffsetX(-.5f,bad))&&Finite(AncientProductionMotion.CargoY(1.45f,bad)),"Corrupt or unbounded scene time cannot poison positions, wheel speed, or cargo geometry");
        Console.WriteLine("PASS: "+checks+" production cinematic continuity, intentional takeoff impulse, roof/camera clearance, cargo/laser separation and motorcycle derivative checks");
    }
}
