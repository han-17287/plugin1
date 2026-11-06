using System;
using Plugin.AncientCivilization;
class ZipChecks
{
    static int count;
    static void Check(bool value,string text){if(!value)throw new Exception(text);count++;}
    static bool Near(float a,float b,float error=.0002f){return Math.Abs(a-b)<=error;}
    static bool SegmentCircleOracle(float dx,float dy,float stepX,float stepY)
    {
        // Independent quadratic intersection oracle rather than the runtime's
        // closest-point projection. All inputs are the actual finite float values.
        double a=(double)stepX*stepX+(double)stepY*stepY,c=(double)dx*dx+(double)dy*dy-(double)AncientZipRules.Arrival*AncientZipRules.Arrival;
        if(c<=0)return true;if(a<=0)return false;
        double b=-2*((double)dx*stepX+(double)dy*stepY),d=b*b-4*a*c;
        if(d<0)return false;double root=Math.Sqrt(d);
        return (-b-root)/(2*a)<=1&&(-b+root)/(2*a)>=0;
    }
    static bool OldMovingArrival(float speed,int hz)
    {
        float x=0,y=.84f,target=12.5f,goalY=6-AncientZipRules.Support(0,-1,false),dt=1f/hz;
        for(int frame=0;frame<hz*4;frame++)
        {
            if(AncientZipRules.Arrived(target-x,goalY-y))return true;
            float vx,vy;AncientZipRules.Velocity(target-x,goalY-y,dt,out vx,out vy);
            // Both bodies advance between physics callbacks. The next callback
            // observes the carrier's NEW position, unlike the former test loop.
            x+=vx*dt;y+=vy*dt;target+=speed*dt;
        }
        return false;
    }
    static void MovingTrace(float gap,float carrier,float origin,float[] frames)
    {
        float x=origin,y=.84f,target=origin+gap-1.5f,goalY=6-AncientZipRules.Support(0,-1,false),time=0,lastGoal=target;
        bool finishPending=false;int frame=0;
        while(frame<2000&&time<AncientZipRules.MaximumSeconds)
        {
            float dt=frames[frame++%frames.Length];
            if(finishPending)
            {
                Check(Math.Sqrt((double)(x-lastGoal)*(x-lastGoal)+(double)(y-goalY)*(y-goalY))<=AncientZipRules.Arrival+.004f,
                    "A predicted finish is committed only after the actual planned segment has physically advanced to its old goal");
                float offset=target-x;
                for(int coast=0;coast<30;coast++){x+=carrier*dt;target+=carrier*dt;}
                Check(Near(target-x,offset,.004f),"Inheriting the carrier's horizontal speed after release preserves the roof-relative landing gap");
                Check(time<2,"Every authored8-to14m boarding gap reaches the latch without the age timeout");return;
            }
            float dx=target-x,dy=goalY-y,vx,vy;AncientZipRules.Velocity(dx,dy,dt,out vx,out vy);
            float stepX=vx*dt,stepY=vy*dt;
            finishPending=AncientZipRules.ReachesThisStep(dx,dy,stepX,stepY);lastGoal=target;
            Check(finishPending==SegmentCircleOracle(dx,dy,stepX,stepY),"The planned-step finish agrees with an independent segment/circle intersection oracle");
            x+=stepX;y+=stepY;target+=carrier*dt;time+=dt;
        }
        throw new Exception("A valid moving support failed to finish: gap="+gap+" carrier="+carrier+" origin="+origin);
    }
    static void Main()
    {
        foreach(int hz in new[]{30,50,60,120,240})foreach(var point in new[]{new[]{22f,0f},new[]{0f,10f},new[]{-12f,8f},new[]{12f,-8f},new[]{.12f,.04f}})
        {
            float x=0,y=0,t=0,dt=1f/hz;int frames=0;
            while(!AncientZipRules.Arrived(point[0]-x,point[1]-y)&&frames++<hz*4)
            {
                float dx=point[0]-x,dy=point[1]-y,vx,vy;AncientZipRules.Velocity(dx,dy,dt,out vx,out vy);
                Check(Math.Abs(dx*vy-dy*vx)<.001f,"Every frame pulls directly toward the target without a tangent kick");
                Check(vx*vx+vy*vy<=AncientZipRules.Speed*AncientZipRules.Speed+.01f,"Straight pull remains within its intended speed");
                Check((vx*dt)*(vx*dt)+(vy*dt)*(vy*dt)<=dx*dx+dy*dy+.0001f,"A short final physics step never overshoots its goal");
                x+=vx*dt;y+=vy*dt;t+=dt;
            }
            Check(AncientZipRules.Arrived(point[0]-x,point[1]-y),"Every selected stationary point is reached at all physics rates");
            float expected=(float)Math.Sqrt(point[0]*point[0]+point[1]*point[1])/AncientZipRules.Speed;
            Check(Math.Abs(t-expected)<=dt+AncientZipRules.Arrival/AncientZipRules.Speed+.001f,"Travel time stays independent of physics update rate");
        }
        foreach(int hz in new[]{30,50,60,120,240})
        {
            Check(!OldMovingArrival(17,hz),"The regression reproduces the former endless carrierSpeed*deltaTime gap at "+hz+"Hz");
            foreach(float carrier in new[]{-17f,-7f,0f,7f,17f})foreach(float origin in new[]{0f,30000f,31570f})
                for(int gap=8;gap<=14;gap++)MovingTrace(gap,carrier,origin,new[]{1f/hz});
        }
        foreach(float carrier in new[]{0f,7f,17f})for(int gap=8;gap<=14;gap++)MovingTrace(gap,carrier,30000,new[]{1/60f,.1f,1/144f,.2f});
        Check(AncientZipRules.ReachesThisStep(1,0,1,0)&&AncientZipRules.ReachesThisStep(1,0,2,0),"Exact arrival and a forward segment through the arrival circle finish once");
        Check(!AncientZipRules.ReachesThisStep(1,0,-2,0)&&!AncientZipRules.ReachesThisStep(1,0,0,2),"Backward and perpendicular movement cannot claim a forward target");
        Check(!AncientZipRules.ReachesThisStep(1,0,.9f,0)&&AncientZipRules.ReachesThisStep(1,0,.96f,0),"A collision-clipped short step finishes only when its actual endpoint reaches the arrival tolerance");
        Check(!AncientZipRules.ReachesThisStep(1,.2f,2,0)&&AncientZipRules.ReachesThisStep(1,.01f,2,0),"Crossing the target's projected direction still respects lateral clearance");
        Check(!AncientZipRules.ReachesThisStep(.1f,0,0,0)&&AncientZipRules.ReachesThisStep(.02f,0,0,0),"A zero step only finishes an already reached target");
        var random=new Random(17287);
        for(int sample=0;sample<2000;sample++)
        {
            float sx=(float)(random.NextDouble()*40-20),sy=(float)(random.NextDouble()*40-20),t=(float)(random.NextDouble()*1.8-.4);
            float dx=sx*t+(float)(random.NextDouble()*.2-.1),dy=sy*t+(float)(random.NextDouble()*.2-.1);
            Check(AncientZipRules.ReachesThisStep(dx,dy,sx,sy)==SegmentCircleOracle(dx,dy,sx,sy),"Finite oblique, partial and overshooting steps match the independent intersection oracle");
        }
        float ax,ay;AncientZipRules.Velocity(0,10,.02f,out ax,out ay);
        Check(Near(ax,0)&&Near(ay,30),"A ceiling click immediately pulls vertically without side drift");
        AncientZipRules.Velocity(-10,0,.02f,out ax,out ay);
        Check(Near(ax,-30)&&Near(ay,0),"A wall click immediately pulls horizontally without an upward hop");
        Check(AncientZipRules.Support(1,0,false)<.5f,"Wall arrival uses capsule width instead of stopping1.8metres short");
        Check(AncientZipRules.Support(0,1,false)>1.05f&&AncientZipRules.Support(0,1,false)<1.2f,"Ceiling arrival preserves full head clearance");
        Check(Near(AncientZipRules.Support(1,0,false),AncientZipRules.Support(-1,0,false)),"Surface stopping clearance mirrors horizontally");
        Check(Near(AncientZipRules.Support(0,1,false),AncientZipRules.Support(0,-1,false)),"Head and foot arrival use the same capsule clearance");
        Check(AncientZipRules.Support(1,0,true)>AncientZipRules.Support(1,0,false),"Mounted pulling respects the motorcycle's wider collider");
        Check(Near(AncientZipRules.Support(0,-1,true)-AncientZipRules.Support(0,1,true),.17f),"Motorcycle ceiling/floor offsets preserve its raised collider centre without a stopping hop");
        Check(!AncientZipRules.BlocksInitialContact(0,-1,0,1),"An existing floor contact cannot block an upward departure");
        Check(!AncientZipRules.BlocksInitialContact(.4f,0,0,1),"A side-wall contact permits an upward slide");
        Check(AncientZipRules.BlocksInitialContact(.4f,0,1,0),"A wall in front of the body blocks a forward step");
        Check(AncientZipRules.BlocksInitialContact(0,0,1,0),"A body already inside solid geometry is stopped conservatively");
        foreach(float step in new[]{.01f,.25f,.6f,1f})foreach(float hit in new[]{0f,.02f,.2f,.8f,2f})
        {
            float safe=AncientZipRules.SafeDistance(step,hit);
            Check(safe>=0&&safe<=step,"Collision clipping never adds distance or moves backward");
            Check(safe<=Math.Max(0,hit-AncientZipRules.Skin)+.00001f,"Collision clipping keeps a skin gap before the first obstruction");
        }
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            AncientZipRules.Velocity(bad,1,.02f,out ax,out ay);Check(ax==0&&ay==0,"Invalid target coordinates never inject a bad velocity");
            AncientZipRules.Velocity(1,1,bad,out ax,out ay);Check(ax==0&&ay==0,"Invalid time step stops safely");
            Check(!AncientZipRules.Arrived(bad,0),"Invalid target coordinates cannot count as a successful arrival");
            Check(!AncientZipRules.ReachesThisStep(bad,0,1,0)&&!AncientZipRules.ReachesThisStep(1,bad,1,0)&&!AncientZipRules.ReachesThisStep(1,0,bad,0)&&!AncientZipRules.ReachesThisStep(1,0,1,bad),"Invalid target or planned-step coordinates cannot schedule a wire completion");
        }
        Console.WriteLine("PASS: "+count+" straight-wire movement, planned-step arrival, simultaneous moving-support trace and collision-budget checks");
    }
}
