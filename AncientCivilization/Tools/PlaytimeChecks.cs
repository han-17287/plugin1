using System;
using Plugin.AncientCivilization;
class PlaytimeChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);checks++;}
    static void Main()
    {
        foreach(int hz in new[]{15,30,60,120,144,240})
        {
            var timer=new AncientPlaytimeRules();
            for(int frame=0;frame<hz*180;frame++)timer.Step(0,1.0/hz,true,frame<hz*30,frame>=hz*120);
            Check(Math.Abs(timer.TotalMilliseconds(0)-180000)<=1,"Total measurement is frame-rate independent");
            Check(Math.Abs(timer.DialogueMilliseconds(0)-30000)<=1,"Paused simulation dialogue still counts when explicitly active");
            Check(Math.Abs(timer.BossMilliseconds(0)-60000)<=1,"Boss time is a subset of total elapsed time");
            timer.Step(0,120,false,true,true);Check(timer.TotalMilliseconds(0)==180000,"Menus, focus loss and journal reading do not inflate playtime");
            timer.Retry(0);timer.Retry(0);Check(timer.Retries(0)==2,"Each restart is recorded separately");
            var restored=new AncientPlaytimeRules();restored.Restore(0,timer.TotalMilliseconds(0),timer.DialogueMilliseconds(0),timer.BossMilliseconds(0),timer.Retries(0),false);
            restored.Step(0,60,true,false,false);Check(restored.TotalMilliseconds(0)==240000&&restored.Retries(0)==2,"Reload continues the recorded attempt instead of replacing it");
            restored.Complete(0);restored.Step(0,1000,true,true,true);restored.Retry(0);
            Check(restored.TotalMilliseconds(0)==240000&&restored.Retries(0)==2,"Returning to a completed floor never changes its first-clear measurement");
            restored.Step(1,75,true,true,false);Check(restored.TotalMilliseconds(1)==75000&&restored.TotalMilliseconds(0)==240000,"The second floor has an independent clock");
        }
        var invalid=new AncientPlaytimeRules();invalid.Restore(0,-10,2000,-30,-1,false);
        Check(invalid.TotalMilliseconds(0)==0&&invalid.DialogueMilliseconds(0)==0&&invalid.BossMilliseconds(0)==0&&invalid.Retries(0)==0,"Corrupt saved times cannot create negative or oversized categories");
        foreach(double bad in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity,-1,0})invalid.Step(0,bad,true,true,true);
        Check(invalid.TotalMilliseconds(0)==0,"Invalid deltas never poison the clock");
        invalid.Step(0,double.MaxValue,true,true,true);Check(invalid.TotalMilliseconds(0)==int.MaxValue,"Long sessions saturate safely for integer persistence");
        invalid.Complete(9);invalid.Step(9,100,true,true,true);invalid.Retry(9);Check(invalid.CompletedMask==0&&invalid.TotalMilliseconds(9)==0,"Untracked floors cannot change lower-floor measurements");
        Console.WriteLine("PASS: "+checks+" active playtime, pause, retry and persistence checks");
    }
}
