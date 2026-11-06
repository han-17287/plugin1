using System;
using Plugin.AncientCivilization;

class ProductionHazardChecks
{
    static int checks;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0002f;}
    static float RiderY(float time,bool jump)
    {
        float t=time-.65f;
        return !jump||t<0?.84f:Math.Max(.84f,.84f+14*t-16*t*t);
    }
    struct Blast {public bool detonated,hit;public float time,x,y;}
    static Blast BombRun(float[] steps,bool jump,bool old=false)
    {
        float y=old?3.4f:AncientProductionHazardRules.BombStartHeight,velocity=old?0:AncientProductionHazardRules.BombStartVelocityY;
        float fuse=old?.9f:AncientProductionHazardRules.BombFuse,time=0;bool landed=false;
        for(int frame=0;frame<500;frame++)
        {
            float step=steps[frame%steps.Length],after,x,heroY;
            bool blast=AncientProductionHazardRules.TickBomb(ref y,ref velocity,ref fuse,ref landed,step,out after);
            if(blast)
            {
                Check(AncientProductionHazardRules.BombImpactHero(time*17,RiderY(time,jump),(time+step)*17,RiderY(time+step,jump),after,step,out x,out heroY),"A detonation samples the rider inside the actual crossed frame");
                return new Blast{detonated=true,time=time+after,x=x,y=heroY,hit=AncientProductionHazardRules.BombHit(x,heroY,18)};
            }
            time+=step;
        }
        return new Blast();
    }
    static void Main()
    {
        Check(AncientProductionHazardRules.SweptDebris(-4,.84f,4,.84f,0),"A rider cannot tunnel through a debris box during a long render frame");
        Check(AncientProductionHazardRules.SweptDebris(4,.84f,-4,.84f,0),"Reverse recovery motion uses the same swept collision shape");
        Check(!AncientProductionHazardRules.SweptDebris(-4,3,4,3,0),"A jump that stays above the whole obstacle is safe");
        Check(AncientProductionHazardRules.SweptDebris(-3,.84f,3,3,0),"A jump started after entering the hazard cannot erase the earlier ground contact");
        Check(AncientProductionHazardRules.SweptDebris(-3,3,3,.84f,0),"Landing inside the debris path still counts even if the previous sample was high");
        Check(!AncientProductionHazardRules.SweptDebris(-3,.84f,3,4.7f,0),"A rising segment that clears the threshold before the near face is safe");
        Check(!AncientProductionHazardRules.SweptDebris(-3,3,3,3.5f,0),"Crossing the horizontal interval alone does not create a midair hit");
        Check(AncientProductionHazardRules.SweptDebris(0,.84f,0,.84f,0),"Stationary overlap remains dangerous");
        Check(!AncientProductionHazardRules.SweptDebris(1.4f,.84f,1.4f,.84f,0),"A side boundary touch retains the strict authored width");
        Check(!AncientProductionHazardRules.SweptDebris(-3,1.85f,3,1.85f,0),"The exact upper clearance boundary is safe");
        Check(!AncientProductionHazardRules.SweptDebris(-3,.84f,-1.4f,.84f,0),"Stopping at the near side without entering is safe");
        Check(AncientProductionHazardRules.SweptDebris(-1.4f,.84f,0,.84f,0),"Continuing inside from a previous side touch is a real hit");
        Check(!AncientProductionHazardRules.SweptDebris(-5,.84f,-2,.84f,0),"A segment that has not reached the obstacle cannot hit it early");
        Check(AncientProductionHazardRules.SweptDebris(29996,.84f,30004,.84f,30000),"Swept debris remains reliable at the authored production highway coordinates");
        Check(AncientProductionHazardRules.BombHit(0,.84f,0)&&!AncientProductionHazardRules.BombHit(0,2.7f,0),"The grounded rider is threatened but the exact bomb jump-clearance height is safe");
        Check(!AncientProductionHazardRules.BombHit(2.4f,.84f,0)&&AncientProductionHazardRules.BombHit(2.39f,.84f,0),"Bomb width uses the same strict border at either frame rate");
        foreach(int fps in new[]{15,30,60,120,240})
        {
            var ground=BombRun(new[]{1f/fps},false);var jump=BombRun(new[]{1f/fps},true);
            Check(ground.detonated&&Near(ground.time,1.05f)&&Math.Abs(ground.x-18)<.2f&&ground.hit,"The bomb threatens a rider travelling17m/s toward its18m spawn lead at "+fps+"FPS");
            Check(jump.detonated&&Near(jump.time,1.05f)&&jump.y>2.7f&&!jump.hit,"A timely Space jump avoids that same blast at "+fps+"FPS");
            bool hit=false,jumpHit=false;float time=0;
            for(int frame=0;frame<fps*2;frame++)
            {
                float next=time+1f/fps;
                hit|=AncientProductionHazardRules.SweptDebris(time*17,.84f,next*17,.84f,18);
                jumpHit|=AncientProductionHazardRules.SweptDebris(time*17,RiderY(time,true),next*17,RiderY(next,true),18);time=next;
            }
            Check(hit&&!jumpHit,"Debris collision remains a grounded hit and a clear jump at "+fps+"FPS");
        }
        foreach(float step in new[]{.2f,.3f})
        {
            var ground=BombRun(new[]{step},false);var jump=BombRun(new[]{step},true);
            Check(ground.detonated&&ground.hit&&Near(ground.time,1.05f),"A long frame cannot move the explosion to the frame end and miss a ground rider");
            Check(jump.detonated&&!jump.hit&&jump.y>2.7f,"Detonation-time interpolation still respects a timely jump during a long frame");
            bool hit=false,jumpHit=false;float time=0;
            for(int frame=0;frame<10;frame++)
            {
                float next=time+step;hit|=AncientProductionHazardRules.SweptDebris(time*17,.84f,next*17,.84f,18);
                jumpHit|=AncientProductionHazardRules.SweptDebris(time*17,RiderY(time,true),next*17,RiderY(next,true),18);time=next;
            }
            Check(hit&&!jumpHit,"Long-frame debris sweeps neither tunnel nor hit a completely cleared jump");
        }
        var uneven=BombRun(new[]{1/60f,.2f,1/60f,.3f},false);
        Check(uneven.hit&&Near(uneven.time,1.05f),"Alternating smooth frames and hitches does not alter authored bomb timing");
        var previous=BombRun(new[]{1/60f},false,true);
        Check(previous.detonated&&!previous.hit&&previous.x>24,"The regression reproduces the previous too-high/long-fuse bomb falling behind the rider before its blast");
        float y=1.7f,velocity=-2,fuse=.75f,after;bool landed=false;
        Check(!AncientProductionHazardRules.TickBomb(ref y,ref velocity,ref fuse,ref landed,.2f,out after)&&!landed&&Near(y,.98f)&&Near(fuse,.75f),"Airborne time does not spend the ground fuse");
        Check(!AncientProductionHazardRules.TickBomb(ref y,ref velocity,ref fuse,ref landed,.2f,out after)&&landed&&Near(y,.38f)&&Near(fuse,.65f),"Only the post-contact part of the landing frame spends fuse time");
        float x,heroY;
        Check(AncientProductionHazardRules.BombImpactHero(0,.84f,5.1f,3,.15f,.3f,out x,out heroY)&&Near(x,2.55f)&&Near(heroY,1.92f),"Blast sampling interpolates both horizontal position and jump height");
        Check(!AncientProductionHazardRules.BombImpactHero(0,0,5,5,.31f,.3f,out x,out heroY),"A countdown outside this step cannot detonate early");
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            Check(!AncientProductionHazardRules.SweptDebris(bad,0,3,0,0)&&!AncientProductionHazardRules.SweptDebris(0,bad,3,0,0)&&!AncientProductionHazardRules.SweptDebris(0,0,bad,0,0)&&!AncientProductionHazardRules.SweptDebris(0,0,3,bad,0)&&!AncientProductionHazardRules.SweptDebris(0,0,3,0,bad),"Invalid geometry cannot create a phantom debris collision");
            Check(!AncientProductionHazardRules.BombHit(bad,0,0)&&!AncientProductionHazardRules.BombHit(0,bad,0)&&!AncientProductionHazardRules.BombHit(0,0,bad),"Invalid blast geometry cannot hit the rider");
            float beforeY=y,beforeVelocity=velocity,beforeFuse=fuse;bool beforeLanded=landed;
            Check(!AncientProductionHazardRules.TickBomb(ref y,ref velocity,ref fuse,ref landed,bad,out after)&&y==beforeY&&velocity==beforeVelocity&&fuse==beforeFuse&&landed==beforeLanded,"Invalid active time cannot poison the bomb state");
        }
        foreach(float step in new[]{0f,-.3f})
        {
            float before=y;Check(!AncientProductionHazardRules.TickBomb(ref y,ref velocity,ref fuse,ref landed,step,out after)&&y==before,"Paused/nonpositive steps leave bombs unchanged");
            Check(!AncientProductionHazardRules.BombImpactHero(0,0,1,1,.1f,step,out x,out heroY),"A stopped clock cannot sample a new explosion");
        }
        Console.WriteLine("PASS: "+checks+" production road-hazard sweep, bomb landing/fuse, rider timing and jump-clearance checks");
    }
}
