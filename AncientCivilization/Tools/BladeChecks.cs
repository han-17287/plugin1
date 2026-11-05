using System;
using Plugin.AncientCivilization;

class BladeChecks
{
    static int count;
    static void Check(bool condition,string description){if(!condition)throw new Exception(description);count++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}

    static void Main()
    {
        Check(AncientBladeRules.StepCount==3&&AncientBladeRules.FramesPerStep==16,"Three dedicated sixteen-frame cuts");
        Check(AncientBladeRules.NextStep(0,0,0)==1,"The first tap is a vertical cut");
        Check(AncientBladeRules.NextStep(1,1,1)==2,"An immediate second tap links the horizontal cut");
        Check(AncientBladeRules.NextStep(2,1,1)==3,"The third tap links the diagonal finisher");
        Check(AncientBladeRules.NextStep(3,1,1)==1,"The fourth tap returns to vertical");
        Check(AncientBladeRules.NextStep(2,10,10.47f)==3,"A short pause preserves the intended finisher");
        Check(AncientBladeRules.NextStep(2,10,10+AncientBladeRules.ComboGrace)==3,"The combo grace boundary includes the final valid instant");
        Check(AncientBladeRules.NextStep(2,10,10.49f)==1,"An expired combo restarts instead of surprising the player");
        Check(AncientBladeRules.NextStep(2,10,9)==1,"A clock reset cannot retain an old combo");
        Check(AncientBladeRules.NextStep(-1,1,1)==1,"A cancelled sequence starts from the first cut");
        Check(AncientBladeRules.NextStep(99,1,1)==1,"Invalid saved sequence state starts safely");
        Check(!AncientBladeRules.CanBuffer(.161f),"Early input does not queue an entire extra attack");
        Check(AncientBladeRules.CanBuffer(.16f),"Input enters the buffer at its exact boundary");
        Check(AncientBladeRules.CanBuffer(.0001f),"A last-moment tap can still link");
        Check(!AncientBladeRules.CanBuffer(0),"A ready attack should start immediately, not be queued");
        Check(!AncientBladeRules.CanBuffer(-1),"Expired recovery does not create a stale buffer");
        Check(AncientBladeRules.BufferLifetime>AncientBladeRules.InputBufferWindow,"A valid queued tap survives to the end of recovery");

        for(int step=1;step<=3;step++)
        {
            float start=AncientBladeRules.ActiveStart(step),impact=AncientBladeRules.ImpactTime(step),end=AncientBladeRules.ActiveEnd(step),duration=AncientBladeRules.Duration(step);
            Check(0<start&&start<impact&&impact<end&&end<duration,"Anticipation, initial motion, visible contact and recovery remain distinct");
            Check(!AncientBladeRules.IsActive(step,start-.0001f),"Windup cannot show an early sweep trail");
            Check(AncientBladeRules.IsActive(step,start),"The visual sweep starts before the blade makes contact");
            Check(AncientBladeRules.IsActive(step,impact),"The damaging instant falls inside the visible sweep");
            Check(!AncientBladeRules.IsActive(step,end),"Recovery cannot keep damaging objects");
            Check(AncientBladeRules.AnimationFrame(step,start)==5,"The first moving blade pose is frame five");
            Check(AncientBladeRules.AnimationFrame(step,impact)>=6&&AncientBladeRules.AnimationFrame(step,impact)<=7,"Impact matches an authored contact pose after initial motion");
            Check(AncientBladeRules.AnimationFrame(step,end)==11,"The active window ends on the first recovery pose");
            Check(AncientBladeRules.AnimationFrame(step,duration)==15,"The last frame settles into recovery");
            Check(AncientBladeRules.AnimationFrame(step,-1)==0,"Negative animation age does not index before the sheet");
            Check(AncientBladeRules.AnimationFrame(step,100)==15,"Old timestamps do not index beyond the sheet");
            Check(Near(AncientBladeRules.SweepProgress(step,start),0),"The trail starts with the first moving blade pose");
            Check(AncientBladeRules.SweepProgress(step,impact)>0&&AncientBladeRules.SweepProgress(step,impact)<1,"The blade has visibly moved before its contact instant");
            Check(Near(AncientBladeRules.SweepProgress(step,end),1),"The full arc is complete when active damage ends");
            Check(Near(AncientBladeRules.SweepProgress(step,-1),0),"Trail progress clamps before the swing");
            Check(Near(AncientBladeRules.SweepProgress(step,100),1),"Trail progress clamps after the swing");
            var seen=new bool[16];int previous=-1;
            for(int tick=0;tick/120f<duration;tick++)
            {
                int frame=AncientBladeRules.AnimationFrame(step,tick/120f);
                Check(frame>=0&&frame<16,"A 120 Hz render sample stays within its own sixteen-frame cut");
                Check(frame>=previous,"Frames advance in order without reversing or wrapping");
                previous=frame;seen[frame]=true;
            }
            for(int frame=0;frame<16;frame++)Check(seen[frame],"Every authored pose can be seen at 120 Hz");
            foreach(int hz in new[]{30,60,120,240})
            {
                int crossing=(int)Math.Ceiling(impact*hz);float hitSample=crossing/(float)hz;
                Check(hitSample>=impact-.000001f&&hitSample-impact<=1f/hz+.000001f,"Impact scheduling remains bounded by one update at all supported rates");
                Check(AncientBladeRules.IsActive(step,hitSample),"Even at 30 Hz the first impact update is inside the visible sweep");
            }
            for(int sample=0;sample<=40;sample++)
            {
                float t=sample/40f;float x,y,lx,ly;
                AncientBladeRules.SweepTip(step,t,1,out x,out y);
                AncientBladeRules.SweepTip(step,t,-1,out lx,out ly);
                Check(!float.IsNaN(x)&&!float.IsNaN(y)&&!float.IsInfinity(x)&&!float.IsInfinity(y),"Authored trail coordinates remain finite");
                Check(Near(x,-lx)&&Near(y,ly),"Left-facing cuts mirror horizontally and retain the same height");
                Check(x*x+y*y<=3.1f*3.1f,"The visible blade tip remains within the existing physical-object break reach");
            }
        }

        // The three paths must read differently even without relying on their textures.
        float vx0,vy0,vx1,vy1,hx0,hy0,hx1,hy1,dx0,dy0,dx1,dy1;
        AncientBladeRules.SweepTip(1,0,1,out vx0,out vy0);AncientBladeRules.SweepTip(1,1,1,out vx1,out vy1);
        AncientBladeRules.SweepTip(2,0,1,out hx0,out hy0);AncientBladeRules.SweepTip(2,.5f,1,out hx1,out hy1);
        AncientBladeRules.SweepTip(3,0,1,out dx0,out dy0);AncientBladeRules.SweepTip(3,1,1,out dx1,out dy1);
        Check(Math.Abs(vx1-vx0)<.01f&&vy0-vy1>2.5f,"Vertical cut moves down a narrow plane");
        Check(hx1-hx0>1.5f&&Math.Abs(hy1-hy0)<.3f,"Horizontal cut spans the waist with little vertical displacement");
        Check(dx1-dx0>2.5f&&dy0-dy1>2.5f,"Diagonal finisher crosses both axes decisively");
        Check(AncientBladeRules.Duration(3)>AncientBladeRules.Duration(1),"The heavier finisher keeps a longer readable recovery");
        Check(Near(AncientBladeRules.TrailFrame(0),5)&&Near(AncientBladeRules.TrailFrame(1),10),"Atlas trail interpolation spans exactly the six active poses");
        Check(Near(AncientBladeRules.TrailFrame(-1),5)&&Near(AncientBladeRules.TrailFrame(2),10),"Atlas trail lookup clamps before and after the sweep");
        Check(Near(AncientBladeRules.TrailFrame(.5f),8),"Atlas trail lookup matches the middle active animation pose");
        Check(Near(AncientBladeRules.TrailFrame(.25f),6.5f),"Atlas trail lookup retains fractional positions for interpolation");

        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            Check(!AncientBladeRules.CanBuffer(bad),"Invalid time cannot queue input");
            Check(!AncientBladeRules.IsActive(1,bad),"Invalid elapsed time cannot resolve damage");
            Check(AncientBladeRules.NextStep(2,bad,1)==1&&AncientBladeRules.NextStep(2,1,bad)==1,"Invalid clock data clears sequence links");
            int frame=AncientBladeRules.AnimationFrame(1,bad);
            Check(frame>=0&&frame<16,"Invalid elapsed time never indexes outside the sprite sheet");
            float x,y;AncientBladeRules.SweepTip(1,bad,bad,out x,out y);
            Check(!float.IsNaN(x)&&!float.IsNaN(y)&&!float.IsInfinity(x)&&!float.IsInfinity(y),"Invalid sweep input has a safe visible position");
        }
        Check(Near(AncientBladeRules.Duration(-1),AncientBladeRules.Duration(1)),"Low invalid step clamps to the first cut");
        Check(Near(AncientBladeRules.Duration(99),AncientBladeRules.Duration(3)),"High invalid step clamps to the last cut");
        Console.WriteLine("PASS: "+count+" blade timing, sequence and sweep checks");
    }
}
