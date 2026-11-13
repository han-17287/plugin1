using System;
using Plugin.AncientCivilization;
class SwingChecks
{
    static int count;
    static void Check(bool condition,string description){if(!condition)throw new Exception(description);count++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    static void Main()
    {
        Check(AncientSwingRules.AttachedFrame(0,0,20,9)==0,"Attachment starts with the reach pose");
        Check(AncientSwingRules.AttachedFrame(.0799f,0,20,9)==0,"Reach holds through its interval");
        Check(AncientSwingRules.AttachedFrame(.08f,0,20,9)==1,"Shoulder tension follows attachment");
        Check(AncientSwingRules.AttachedFrame(.1799f,0,20,9)==1,"Tension holds until the arc starts");
        Check(AncientSwingRules.AttachedFrame(.18f,20,15,-5)==2,"Descending velocity selects the extended pose");
        Check(AncientSwingRules.AttachedFrame(.4f,30,15,0)==3,"Fast bottom pass tucks knees");
        Check(AncientSwingRules.AttachedFrame(.5f,40,16,8)==4,"Rising arc extends the forward leg");
        Check(AncientSwingRules.AttachedFrame(.7f,50,16,8)==5,"High rising arc tucks the body");
        Check(AncientSwingRules.AttachedFrame(.9f,90,12,-8)==2,"Descending movement takes priority over a stale high arc");
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            Check(AncientSwingRules.AttachedFrame(bad,20,15,2)==1,"Invalid animation age has a safe tension pose");
            Check(AncientSwingRules.AttachedFrame(.3f,bad,15,2)==1,"Invalid arc has a safe tension pose");
            Check(AncientSwingRules.AttachedFrame(.3f,20,bad,2)==1,"Invalid velocity has a safe tension pose");
            Check(AncientSwingRules.ReleaseFrame(bad)==-1,"Invalid release cannot leave an animation stuck");
            Check(Near(AncientSwingRules.AlignAngle(bad,1,1,1),0),"Invalid direction cannot rotate the actor");
            Check(Near(AncientSwingRules.ReleaseAngle(30,bad),0),"Invalid release time resets visual tilt");
        }
        Check(AncientSwingRules.AttachedFrame(-1,0,0,0)==1,"Negative attachment time is safe");
        Check(AncientSwingRules.ReleaseFrame(-.1f)==-1,"No release pose before actual release");
        Check(AncientSwingRules.ReleaseFrame(0)==6,"Release starts with the extended follow-through");
        Check(AncientSwingRules.ReleaseFrame(.1399f)==6,"Initial release pose stays within its interval");
        Check(AncientSwingRules.ReleaseFrame(.14f)==7,"Free-launch pose follows release");
        Check(AncientSwingRules.ReleaseFrame(.3399f)==7,"Launch follow-through ends smoothly");
        Check(AncientSwingRules.ReleaseFrame(.34f)==-1,"Release clears exactly at its duration");
        Check(AncientSwingRules.ReleaseFrame(100)==-1,"Old release timestamps never replay a pose");
        Check(AncientSwingRules.Facing(.5f,-1)==-1,"Slow near-zero velocity keeps established facing");
        Check(AncientSwingRules.Facing(-.5f,1)==1,"Small reverse jitter cannot flip the actor");
        Check(AncientSwingRules.Facing(-2,1)==-1,"Clear leftward travel faces left");
        Check(AncientSwingRules.Facing(2,-1)==1,"Clear rightward travel faces right");
        Check(AncientSwingRules.Facing(0,0)==1,"An absent fallback has a stable default");
        Check(Near(AncientSwingRules.AlignAngle(0,0,1,1),0),"No rope length means no aiming angle");
        Check(Near(AncientSwingRules.AlignAngle(1,1,0,0),0),"No emitter direction means no aiming angle");
        Check(Near(AncientSwingRules.AlignAngle(1,1,2,2),0),"Already aligned arm stays unrotated");
        Check(Near(AncientSwingRules.AlignAngle(0,1,1,0),60),"Steep alignment stays within tilt limit");
        for(int x=1;x<=8;x++)for(int y=1;y<=8;y++)
        {
            float right=AncientSwingRules.AlignAngle(x,y,1,2),left=AncientSwingRules.AlignAngle(-x,y,-1,2);
            Check(Near(right,-left),"Mirrored rope and arm mirror the body angle");
            Check(Math.Abs(right)<=60,"Presentation rotation cannot exceed the visual limit");
            Check(Near(right,AncientSwingRules.AlignAngle(x*12,y*12,3,6)),"Direction alignment is independent of rope length and emitter scale");
        }
        foreach(float begin in new[]{-60f,-25f,0f,25f,60f})
        {
            float previous=Math.Abs(begin);
            for(int i=0;i<=34;i++)
            {
                float time=i*.01f,value=AncientSwingRules.ReleaseAngle(begin,time);
                Check(Math.Abs(value)<=previous+.0001f,"Release settles monotonically without overshoot");
                Check(Near(value,-AncientSwingRules.ReleaseAngle(-begin,time)),"Release motion mirrors left and right");
                previous=Math.Abs(value);
            }
            Check(Near(AncientSwingRules.ReleaseAngle(begin,.34f),0),"Release settles to neutral at the duration");
            Check(Near(AncientSwingRules.ReleaseAngle(begin,2),0),"Release remains neutral after its duration");
        }
        Console.WriteLine("PASS: "+count+" swing presentation rule checks");
    }
}
