using System;
using Plugin.AncientCivilization;

class ProjectileOrderingChecks
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    static bool Hit(float dx,float dy,float x,float y,float radius,bool terrain,float fraction,float hitX,float hitY,float nx,float ny)
    {
        float entry;
        return AncientProjectileRules.CircleEntry(dx,dy,x,y,radius,out entry)&&
            (!terrain||AncientProjectileRules.BeforeSurface(entry,fraction,x-hitX,y-hitY,nx,ny));
    }
    static bool Travel(int fps,bool oldOrdering,float heroX,float wallX)
    {
        float current=0,dt=1f/fps;
        for(int frame=1;frame<fps*2;frame++)
        {
            float next=current+25*dt;bool wall=next>=wallX,queriedWall=oldOrdering?wall:next+.75f>=wallX;
            float fraction=queriedWall?(wallX-current)/(next-current):1;
            if(oldOrdering&&wall)return false;
            if(frame*dt>.05f&&Hit(next-current,0,heroX-current,0,.75f,queriedWall,fraction,wallX-current,0,-1,0))return true;
            if(wall)return false;current=next;
        }
        return false;
    }
    static void Main()
    {
        float entry;
        Check(AncientProjectileRules.CircleEntry(5,0,3,0,.75f,out entry)&&Near(entry,.45f),"Circle sweep returns first contact, not the closest centre point");
        Check(Hit(5,0,1.5f,0,.75f,true,.6f,3,0,-1,0),"A hero crossed before a wall is hit during the same frame");
        Check(!Hit(5,0,3.2f,0,.75f,true,.6f,3,0,-1,0),"A hero immediately behind a thin wall stays protected despite an overlapping generous hit radius");
        Check(!Hit(5,0,4.5f,0,.75f,true,.6f,3,0,-1,0),"A farther target cannot be hit through the wall");
        Check(!AncientProjectileRules.BeforeSurface(.6f,.6f,-1,0,-1,0),"Terrain wins exact contact-time ties");
        Check(!AncientProjectileRules.BeforeSurface(0,0,-1,0,-1,0),"A projectile starting in terrain cannot damage out through it");
        Check(!Hit(.2f,0,.7f,0,.75f,true,2.25f,.45f,0,-1,0),"A surface just beyond this frame still occludes a radius overlapping its far side");
        Check(Hit(.2f,0,.3f,0,.75f,true,3,.6f,0,-1,0),"A surface beyond both the frame and hero does not suppress an exposed point overlap");
        Check(!Hit(0,0,.7f,0,.75f,true,float.MaxValue,.45f,0,-1,0),"The zero-step overlap check also respects a wall between projectile and hero");
        Check(Hit(0,0,.3f,0,.75f,true,float.MaxValue,.6f,0,-1,0),"Zero-step overlap before a more distant surface remains an ordinary hit");
        Check(Hit(0,-2.3f,0,-.9f,.65f,true,2/2.3f,0,-2,0,1),"A falling clock hand hits a hero above the floor before sticking");
        Check(!Hit(0,-2.3f,0,-2.15f,.65f,true,2/2.3f,0,-2,0,1),"The floor protects a target below it even inside the radius cap");
        Check(!Hit(5,0,2,1,.75f,true,.8f,4,0,-1,0),"A clear miss still reaches terrain and can bounce");
        Check(AncientProjectileRules.CircleEntry(0,0,.2f,0,.75f,out entry)&&entry==0,"Stationary overlap retains the existing point hit");
        Check(!AncientProjectileRules.CircleEntry(0,0,.75f,0,.75f,out entry),"A stationary point exactly at the radius does not create a new hit");
        Check(!AncientProjectileRules.CircleEntry(5,0,2,.75f,.75f,out entry),"A tangent keeps the previous strict-radius miss");
        foreach(int fps in new[]{15,30,60,120,144,240})
        {
            Check(Travel(fps,false,2.75f,3.1f),"A 25m/s sniper round hits the exposed target before a nearby wall at "+fps+" FPS");
            Check(!Travel(fps,false,3.3f,3.1f),"The same nearby wall protects its far side at "+fps+" FPS");
        }
        Check(!Travel(15,true,2.75f,3.1f)&&Travel(60,true,2.75f,3.1f),"The regression reproduces the old frame-rate-dependent lost sniper hit");
        foreach(float dt in new[]{1/240f,1/60f,1/15f,.2f,.3f})
        {
            float current=2.3f;bool found=false;
            for(int frame=0;frame<240;frame++)
            {
                float next=current-9.5f*dt;bool floor=next<=0;float fraction=floor?current/(current-next):1;
                if(Hit(0,next-current,0,1.1f-current,.65f,floor,fraction,0,-current,0,1)){found=true;break;}
                if(floor)break;current=next;
            }
            Check(found,"Clock rain cannot skip the hero during normal frames or a 300ms render stall");
        }
        Check(AncientProjectileRules.BoxEntry(10,0,3,-1,6,1,out entry)&&Near(entry,.3f),"Motorcycle bounds retain their earliest near-face hit");
        Check(AncientProjectileRules.BoxEntry(0,0,-1,-1,1,1,out entry)&&entry==0,"A projectile already inside the bike box hits immediately");
        Check(!AncientProjectileRules.BoxEntry(0,0,1,-1,2,1,out entry),"Stationary points outside the bike remain misses");
        Check(!AncientProjectileRules.BoxEntry(10,0,3,1,6,2,out entry),"A parallel shot above the bike box does not hit");
        Check(AncientProjectileRules.BoxEntry(10,0,3,-1,6,1,out entry)&&AncientProjectileRules.BeforeSurface(entry,.7f,-2,0,-1,0),"Bike first contact before a wall resolves before terrain");
        Check(!AncientProjectileRules.BeforeSurface(.3f,.2f,-2,0,-1,0),"A wall reached before the bike still blocks the shot");
        for(int angle=0;angle<72;angle++)
        {
            float dx=(float)Math.Cos(angle*Math.PI/36),dy=(float)Math.Sin(angle*Math.PI/36);
            float reach=AncientProjectileRules.BoxOcclusionReach(dx,dy,1.495f,.97f,0,.085f);
            foreach(float x in new[]{-1.495f,1.495f})foreach(float y in new[]{-.885f,1.055f})
                Check(Math.Abs(x*dx+y*dy)<=reach+.00001f,"Bike occlusion reach conservatively covers every corner at every firing angle");
            Check(reach>=.97f,"A bike never falls back to a smaller pedestrian-circle occlusion reach");
        }
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            Check(!AncientProjectileRules.CircleEntry(bad,0,2,0,.75f,out entry),"Invalid travel does not invent a hero hit");
            Check(!AncientProjectileRules.BoxEntry(10,0,bad,0,3,1,out entry),"Invalid bike bounds do not invent a hit");
            Check(!AncientProjectileRules.BeforeSurface(.2f,bad,0,0,-1,0),"Invalid terrain fractions fail closed");
        }
        // Independent closest-point oracle verifies ordinary circle sweeps keep their old hit shape.
        foreach(float dx in new[]{-5f,-.1f,0f,.1f,5f})foreach(float dy in new[]{-5f,0f,5f})
        for(int xi=-10;xi<=10;xi++)for(int yi=-10;yi<=10;yi++)
        {
            float x=xi*.41f,y=yi*.37f;double a=(double)dx*dx+(double)dy*dy;
            double t=a>0?Math.Max(0,Math.Min(1,(x*(double)dx+y*(double)dy)/a)):0;
            double ex=x-dx*t,ey=y-dy*t;bool expected=ex*ex+ey*ey<.75*.75;
            Check(AncientProjectileRules.CircleEntry(dx,dy,x,y,.75f,out entry)==expected,"First-contact calculation retains the existing .75m swept hero hit shape");
        }
        Console.WriteLine("PASS: "+checks+" projectile ordering, terrain occlusion and frame-pacing checks");
    }
}
