using System;
using Plugin.AncientCivilization;
class ZipChecks
{
    static int count;
    static void Check(bool value,string text){if(!value)throw new Exception(text);count++;}
    static bool Near(float a,float b,float error=.0002f){return Math.Abs(a-b)<=error;}
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
            float player=0,target=22,dt=1f/hz,time=0;bool caught=false;
            for(int frame=0;frame<hz*3;frame++)
            {
                float vx,vy;AncientZipRules.Velocity(target-player,0,dt,out vx,out vy);player+=vx*dt;time+=dt;
                if(AncientZipRules.Arrived(target-player,0)){caught=true;break;}target+=18*dt;
            }
            Check(caught&&time>1.25f&&time<2,"A moving truck can be caught without the former premature1.25-second release");
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
        }
        Console.WriteLine("PASS: "+count+" straight-wire movement and collision-budget checks");
    }
}
