using System;
using Plugin.AncientCivilization;

class BladeChecks
{
    static int count;
    static void Check(bool condition,string description){if(!condition)throw new Exception(description);count++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    static void FeedbackChecks()
    {
        foreach(float facing in new[]{-1f,1f})
        {
            foreach(float aim in new[]{-.2f,-.08f,0,.08f,.2f})
                Check(AncientCombatFeedbackRules.BladeFacing(aim,facing)==facing,"A nearly vertical cut preserves the body facing instead of flipping across the arm offset");
            Check(AncientCombatFeedbackRules.BladeFacing(-.21f,facing)==-1&&AncientCombatFeedbackRules.BladeFacing(.21f,facing)==1,"A deliberate horizontal aim turns both the body and blade toward the attack");
            foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
                Check(AncientCombatFeedbackRules.BladeFacing(bad,facing)==facing,"An invalid aim never flips the character");
        }
        Check(AncientCombatFeedbackRules.BladeFacing(0,0)==1,"An invalid initial facing uses a stable right-facing fallback");
        foreach(float energy in new[]{0f,-1,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            Check(!AncientCombatFeedbackRules.LaserReady(energy,1),"An empty or invalid gauge never signals that a laser can fire");
        foreach(float progress in new[]{0f,.5f,.9999f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            Check(!AncientCombatFeedbackRules.LaserReady(100,progress),"A partial or invalid charge never announces a ready shot");
        Check(AncientCombatFeedbackRules.LaserReady(.01f,1)&&AncientCombatFeedbackRules.LaserReady(100,1),"A completed hold with actual energy signals readiness at every supported energy level");
        Check(!AncientCombatFeedbackRules.LaserReady(0,1)&&AncientCombatFeedbackRules.LaserReady(6,1),"Collecting particles during an already completed hold changes empty guidance to a ready shot");

        foreach(var view in new[]{new[]{640f,480},new[]{1280f,720},new[]{1920f,1080},new[]{3840f,2160},new[]{720f,1280}})
        foreach(float scale in new[]{.85f,1,1.5f,2})
        foreach(float text in new[]{160f,240,360})
        foreach(float xRatio in new[]{0f,.02f,.5f,.98f,1})
        foreach(float yRatio in new[]{0f,.02f,.5f,.98f,1})
        {
            float x=view[0]*xRatio,y=view[1]*yRatio;
            var box=AncientCombatFeedbackRules.ChargeLabel(view[0],view[1],x,y,text,18*scale,scale);
            Check(box.Width>0&&box.Height>0&&box.X>=0&&box.Y>=0&&box.X+box.Width<=view[0]+.001f&&box.Y+box.Height<=view[1]+.001f,"Measured Korean charge guidance remains inside the viewport at edges and all supported resolutions");
            Check(x<box.X||x>box.X+box.Width||y<box.Y||y>box.Y+box.Height,"The charge label never covers the aiming cursor");
        }
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1f,0})
        {
            var box=AncientCombatFeedbackRules.ChargeLabel(bad,bad,bad,bad,bad,bad,bad);
            Check(Finite(box.X)&&Finite(box.Y)&&Finite(box.Width)&&Finite(box.Height)&&box.X>=0&&box.Y>=0&&box.Width>0&&box.Height>0,"Transient invalid viewport samples never produce corrupt GUI coordinates");
        }
    }
    static void ScheduledImpactRun(int step,float[] samples,float cancelAt=-1)
    {
        bool pending=true;int hits=0;float previous=-1,accepted=-1;
        foreach(float elapsed in samples)
        {
            if(cancelAt>=0&&elapsed>=cancelAt)pending=false;
            if(AncientBladeRules.ConsumeScheduledImpact(step,elapsed,ref pending))
            {hits++;accepted=elapsed;Check(previous<AncientBladeRules.ImpactTime(step),"A scheduled cut resolves on the first available update after contact, never a later retry");}
            Check(hits<=1,"A cut cannot apply enemy/interactive damage twice across animation and recovery updates");previous=elapsed;
        }
        bool canceledBeforeContact=cancelAt>=0&&cancelAt<AncientBladeRules.ImpactTime(step);
        Check(hits==(canceledBeforeContact?0:1),"A valid cut survives irregular pacing, while a canceled cut never revives");
        if(hits!=0)Check(accepted>=AncientBladeRules.ImpactTime(step),"Hitch recovery cannot move contact into anticipation");
    }

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

            float before=impact-.01f,after=end+.02f;
            ScheduledImpactRun(step,new[]{0f,before,after,duration+.1f,duration+1});
            ScheduledImpactRun(step,new[]{0f,duration+.2f,duration+.5f,duration+1});
            ScheduledImpactRun(step,new[]{0f,before,after,duration+1},impact-.005f);
            foreach(float[] frames in new[]{new[]{1/30f},new[]{1/60f},new[]{1/144f},new[]{.013f,.09f,.14f,.033f,.3f}})
            {
                var samples=new System.Collections.Generic.List<float>();float elapsed=0;int frame=0;
                while(elapsed<1.4f){samples.Add(elapsed);elapsed+=frames[frame++%frames.Length];}
                ScheduledImpactRun(step,samples.ToArray());
            }

        }

        Check(AncientBladeRules.Duration(3)>AncientBladeRules.Duration(1),"The heavier finisher keeps a longer readable recovery");
        Check(Near(AncientBladeRules.TrailFrame(0),5)&&Near(AncientBladeRules.TrailFrame(1),10),"Atlas trail interpolation spans exactly the six active poses");
        Check(Near(AncientBladeRules.TrailFrame(-1),5)&&Near(AncientBladeRules.TrailFrame(2),10),"Atlas trail lookup clamps before and after the sweep");
        Check(Near(AncientBladeRules.TrailFrame(.5f),8),"Atlas trail lookup matches the middle active animation pose");
        Check(Near(AncientBladeRules.TrailFrame(.25f),6.5f),"Atlas trail lookup retains fractional positions for interpolation");

        bool overdue=true;
        Check(AncientBladeRules.ConsumeScheduledImpact(1,.43f,ref overdue)&&!overdue,"A new tap can flush the previous cut's overdue contact before replacing its timestamp");
        overdue=true;
        Check(!AncientBladeRules.ConsumeScheduledImpact(2,0,ref overdue)&&overdue,"The replacement cut retains its own anticipation instead of inheriting old damage");
        Check(AncientBladeRules.ConsumeScheduledImpact(2,.24f,ref overdue)&&!AncientBladeRules.ConsumeScheduledImpact(2,.6f,ref overdue),"Flushing an old cut never duplicates the next cut's contact");

        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            bool pending=true;Check(!AncientBladeRules.ConsumeScheduledImpact(1,bad,ref pending)&&pending,"Invalid clock samples cannot consume or resolve an accepted cut");
            Check(!AncientBladeRules.CanBuffer(bad),"Invalid time cannot queue input");
            Check(!AncientBladeRules.IsActive(1,bad),"Invalid elapsed time cannot resolve damage");
            Check(AncientBladeRules.NextStep(2,bad,1)==1&&AncientBladeRules.NextStep(2,1,bad)==1,"Invalid clock data clears sequence links");
            int frame=AncientBladeRules.AnimationFrame(1,bad);
            Check(frame>=0&&frame<16,"Invalid elapsed time never indexes outside the sprite sheet");

        }
        Check(Near(AncientBladeRules.Duration(-1),AncientBladeRules.Duration(1)),"Low invalid step clamps to the first cut");
        Check(Near(AncientBladeRules.Duration(99),AncientBladeRules.Duration(3)),"High invalid step clamps to the last cut");
        FeedbackChecks();
        Console.WriteLine("PASS: "+count+" blade timing, scheduled contact, facing, laser readiness and measured feedback placement checks");
    }
}
