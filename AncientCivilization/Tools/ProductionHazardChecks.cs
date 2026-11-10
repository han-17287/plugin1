using System;
using Plugin.AncientCivilization;

class ProductionHazardChecks
{
    static int checks;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0002f;}
    static float RiderY(float time,bool jump,float jumpAt=.65f,float inputDelay=0)
    {
        float t=time-jumpAt-inputDelay;
        if(!jump||t<0)return .84f;
        // The motorcycle deliberately ignores short-hop release. Its full14m/s jump
        // uses gravity32 upward and the hero's actual1.2 falling gravity multiplier.
        const float apex=14/32f,peak=.84f+14*apex-16*apex*apex;
        return t<=apex?.84f+14*t-16*t*t:Math.Max(.84f,peak-19.2f*(t-apex)*(t-apex));
    }
    struct Blast {public bool detonated,hit;public float time,x,y;}
    static Blast BombRun(float[] steps,bool jump,bool old=false,float jumpAt=.65f,float inputDelay=0)
    {
        float y=old?3.4f:AncientProductionHazardRules.BombStartHeight,velocity=old?0:AncientProductionHazardRules.BombStartVelocityY;
        float fuse=old?.9f:AncientProductionHazardRules.BombFuse,time=0;bool landed=false;
        for(int frame=0;frame<500;frame++)
        {
            float step=steps[frame%steps.Length],after,x,heroY;
            bool blast=AncientProductionHazardRules.TickBomb(ref y,ref velocity,ref fuse,ref landed,step,out after);
            if(blast)
            {
                Check(AncientProductionHazardRules.BombImpactHero(time*17,RiderY(time,jump,jumpAt,inputDelay),(time+step)*17,RiderY(time+step,jump,jumpAt,inputDelay),after,step,out x,out heroY),"A detonation samples the rider inside the actual crossed frame");
                return new Blast{detonated=true,time=time+after,x=x,y=heroY,hit=AncientProductionHazardRules.BombHit(x,heroY,18)};
            }
            time+=step;
        }
        return new Blast();
    }
    static bool DebrisRun(float[] steps,float jumpAt,float inputDelay)
    {
        float time=0;
        for(int frame=0;time<2&&frame<500;frame++)
        {
            float next=time+steps[frame%steps.Length];
            if(AncientProductionHazardRules.SweptDebris(time*17,RiderY(time,true,jumpAt,inputDelay),next*17,RiderY(next,true,jumpAt,inputDelay),18))return true;
            time=next;
        }
        return false;
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
        foreach(float origin in new[]{0f,30000f})
        {
            Check(AncientProductionHazardRules.DebrisCleared(origin-3,3.9f,origin+5,.84f,origin),"A coarse frame can safely clear the far edge high and land beyond it before drawing success feedback");
            Check(!AncientProductionHazardRules.DebrisCleared(origin-3,.84f,origin+5,3.8f,origin),"Rising after a low entrance cannot turn a real debris collision into dodge feedback");
            Check(!AncientProductionHazardRules.DebrisCleared(origin+2,3,origin+3,3,origin),"An already passed hazard cannot increase a dodge chain again");
            Check(!AncientProductionHazardRules.DebrisCleared(origin+3,3,origin-3,3,origin),"Reverse motion is not an earned forward road clearance");
            Check(AncientProductionHazardRules.BombCleared(origin,2.7f,origin)&&!AncientProductionHazardRules.BombCleared(origin,2.69f,origin),"Bomb dodge feedback uses the same exact jump-height threshold as its damage geometry");
            Check(!AncientProductionHazardRules.BombCleared(origin+3,4,origin),"A distant explosion cannot manufacture successful-jump feedback");
        }
        Check(!AncientProductionHazardRules.DebrisCleared(-3,3,1.4f,3,0),"Feedback waits until the rider exits the far side, rather than counting a stopped boundary");
        Check(AncientProductionHazardRules.DebrisCleared(1.4f,3,2,3,0),"Exiting after an exact far-edge sample produces one clearance");
        Check(!AncientProductionHazardRules.BombCleared(2.4f,3,0),"The strict side boundary is outside the blast and is not a jump dodge");
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
            int clears=0;time=0;
            for(int frame=0;frame<fps*2;frame++)
            {float next=time+1f/fps;if(AncientProductionHazardRules.DebrisCleared(time*17,RiderY(time,true),next*17,RiderY(next,true),18))clears++;time=next;}
            Check(clears==1,"A completed Space jump produces exactly one far-edge clearance at "+fps+"FPS");
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
            Check(!AncientProductionHazardRules.DebrisCleared(bad,3,3,3,0)&&!AncientProductionHazardRules.DebrisCleared(-3,3,3,bad,0)&&!AncientProductionHazardRules.BombCleared(bad,3,0)&&!AncientProductionHazardRules.BombCleared(0,bad,0),"Invalid samples cannot create a dodge cue");
            float beforeY=y,beforeVelocity=velocity,beforeFuse=fuse;bool beforeLanded=landed;
            Check(!AncientProductionHazardRules.TickBomb(ref y,ref velocity,ref fuse,ref landed,bad,out after)&&y==beforeY&&velocity==beforeVelocity&&fuse==beforeFuse&&landed==beforeLanded,"Invalid active time cannot poison the bomb state");
        }
        foreach(float step in new[]{0f,-.3f})
        {
            float before=y;Check(!AncientProductionHazardRules.TickBomb(ref y,ref velocity,ref fuse,ref landed,step,out after)&&y==before,"Paused/nonpositive steps leave bombs unchanged");
            Check(!AncientProductionHazardRules.BombImpactHero(0,0,1,1,.1f,step,out x,out heroY),"A stopped clock cannot sample a new explosion");
        }
        // A tutorial obstacle must allow a broad reaction window, rather than merely
        // one hand-picked successful press. Include one full60Hz input/physics delay.
        foreach(float[] steps in new[]{new[]{1/30f},new[]{1/60f},new[]{1/144f},new[]{1/60f,.1f,1/144f,.066f}})
        {
            int safe=0;float first=-1,last=-1;
            for(int sample=0;sample<=120;sample++)
            {
                float press=sample/120f;
                var blast=BombRun(steps,true,false,press,1/60f);
                bool clear=blast.detonated&&!blast.hit&&!DebrisRun(steps,press,1/60f);
                if(clear){safe++;if(first<0)first=press;last=press;}
                if(press>=.45f&&press<=.8f)Check(clear,"Both road hazards admit a comfortable Space press from450 to800ms after their appearance, including input delay");
            }
            Check(safe>=50&&last-first>.4f,"The shared debris/bomb jump window is over400ms across smooth and uneven render pacing");
            Check(BombRun(steps,true,false,.1f).hit&&BombRun(steps,true,false,1).hit,"Jumping immediately at spawn or after the explosion remains meaningfully different from a timely reaction");
        }
        Console.WriteLine("PASS: "+checks+" production road-hazard sweep, bomb landing/fuse, rider timing and jump-clearance checks");
    }
}
