using System;
using Plugin.AncientCivilization;

class BossReadabilityChecks
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    static void SimulateArea(int fps,float invulnerableUntil,bool bike)
    {
        var window=new AncientClockAreaDamage();window.Begin();
        float hp=100,bikeHP=bike?80:0,initialHP=hp,initialBikeHP=bikeHP;int accepted=0;
        bool expectedHit=invulnerableUntil<=1.3f;
        // Match the entire broad sweep, including frames after the hero's normal .65s immunity expires.
        for(int frame=0;frame<=Math.Ceiling(fps*1.3);frame++)
        {
            float time=frame/(float)fps;if(!window.CanHit)continue;
            float beforeHP=hp,beforeBike=bikeHP;
            if(time>=invulnerableUntil)
            {
                if(bike)bikeHP-=28;else hp-=28;
                invulnerableUntil=time+.65f;accepted++;
            }
            window.RecordHealth(beforeHP,hp,beforeBike,bikeHP);
        }
        Check(accepted==(expectedHit?1:0),"One clock activation deals at most one accepted hit at "+fps+" FPS");
        Check(Near((initialHP-hp)+(initialBikeHP-bikeHP),expectedHit?28:0),"A broad sweep never stacks two or three health losses");
        Check(window.CanHit!=expectedHit,"Only actual health loss consumes the sweep's damage budget");
        window.Begin();Check(window.CanHit,"The next of the three clock areas gets a fresh damage budget");
    }
    static void Main()
    {
        foreach(bool enraged in new[]{false,true})
        {
            int count=AncientBossRules.Count(AncientBossMove.FloorRoll,enraged),rows=count/2;
            Check(rows==(enraged?2:1),"The enraged ground volley adds one upper row");
            for(int i=0;i<count;i+=2)
            {
                float tellHeight=AncientBossRules.FloorRollHeight(i);
                Check(Near(tellHeight,AncientBossRules.FloorRollHeight(i+1)),"Both sides of a floor row share the telegraphed height");
                Check(Near(AncientBossRules.FloorRollVelocity(i),-AncientBossRules.FloorRollVelocity(i+1)),"Floor arrows correspond to opposing projectile directions");
                Check(AncientBossRules.FloorRollVelocity(i)<0&&AncientBossRules.FloorRollVelocity(i+1)>0,"Every floor row attacks both directions");
                Check(Near(tellHeight,i==0?.5f:1.2f),"Slab heights stay on their authored ground lanes even when the hero is airborne");
                Check(Near(Math.Abs(AncientBossRules.FloorRollVelocity(i)),8),"The readable tell does not change projectile speed");
            }
            for(int cycle=0;cycle<4;cycle++)
            {
                Check(AncientBossRules.Choose(3,cycle*3,enraged,8)==AncientBossMove.CoreLob,"The floor-three sequence begins with lobbed cores");
                Check(AncientBossRules.Choose(3,cycle*3+1,enraged,8)==AncientBossMove.FloorRoll,"Ground slabs remain the second pattern");
                Check(AncientBossRules.Choose(3,cycle*3+2,enraged,8)==AncientBossMove.CeilingDebris,"Ceiling debris remains the third pattern");
            }
        }
        Check(AncientBossRules.Windup(AncientBossMove.FloorRoll)>=.75f,"Ground lanes remain visible before the volley");
        Check(AncientBossRules.Opening(AncientBossMove.FloorRoll)>=1.25f,"Clearing ground slabs retains a punish window");
        foreach(int fps in new[]{15,30,60,120,144,240})foreach(float immunity in new[]{0f,.25f,.9f,1.5f})
        {SimulateArea(fps,immunity,false);SimulateArea(fps,immunity,true);}

        var ignored=new AncientClockAreaDamage();ignored.Begin();
        for(int i=0;i<100;i++)ignored.RecordHealth(100,100,80,80);
        Check(ignored.CanHit,"Invulnerability ignores cannot silently disarm the area before immunity expires");
        ignored.RecordHealth(100,90,80,80);ignored.RecordHealth(90,100,80,80);
        Check(!ignored.CanHit,"Healing or subsequent ignored attempts cannot rearm an already damaging sweep");
        ignored.Begin();ignored.RecordHealth(100,100,10,0);
        Check(!ignored.CanHit,"Destroying a bike also consumes this activation's damage budget");

        float radius=AncientClockRules.BroadRadius(32);
        foreach(float x in new[]{-16f,-8f,-1.16f,1.16f,8f,16f})
            Check(AncientClockRules.BroadHit(x,-5.1f,radius),"The broad clock sweep still reaches the expanded arena's exposed floor");
        foreach(float x in new[]{-1.15f,-.5f,0f,.5f,1.15f})
            Check(!AncientClockRules.BroadHit(x,-5.1f,radius),"Standing directly beneath the elevated secretary remains safe");
        Check(AncientClockRules.BroadHit(0,.5f,radius),"The safe lane does not extend above the secretary");
        Check(AncientClockRules.SpinSeconds==3,"Vector tells retain their full three-second spin");
        for(int shot=0;shot<9;shot++)Check(AncientClockRules.ThirdVectorShot(shot)==(shot%3==2),"Only every third cast adds both hand directions");
        for(int angle=0;angle<24;angle++)for(int step=0;step<=30;step++)
        {
            float target=angle*(float)Math.PI/12,h,m;
            AncientClockRules.Angles(target,step/30f,out h,out m);
            float x=AncientClockRules.HourForce*(float)Math.Cos(h)+AncientClockRules.MinuteForce*(float)Math.Cos(m);
            float y=AncientClockRules.HourForce*(float)Math.Sin(h)+AncientClockRules.MinuteForce*(float)Math.Sin(m);
            Check(x*(float)Math.Cos(target)+y*(float)Math.Sin(target)>0,"Clock resultant continues to track the target direction");
            Check(Math.Abs(x*(float)Math.Sin(target)-y*(float)Math.Cos(target))<.001f,"Hour and minute forces preserve their authored resultant");
        }
        Console.WriteLine("PASS: "+checks+" boss lane, per-activation damage and clock-pattern checks");
    }
}
