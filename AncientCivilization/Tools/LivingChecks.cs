using System;
using Plugin.AncientCivilization;
static class LivingChecks
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static void Main()
    {
        var first=new AncientLivingRules();
        for(int cycle=0;cycle<500;cycle++)
        {
            Check(first.Next()==AncientLivingAttack.Bullets,"phase 1 bullet opening");Check(first.Next()==AncientLivingAttack.Leap,"jump after bullets");Check(first.Next()==AncientLivingAttack.Rocket,"counterable fist");Check(first.Next()==AncientLivingAttack.Missiles,"collectible missiles");
            Check(!first.AdvancePhase(.9f)&&first.Phase==1,"cycle count cannot advance phase");
        }
        Check(!first.AdvancePhase(float.NaN),"invalid health cannot change phase");
        Check(first.FilterDamage(10000,960,960,true)>307&&first.FilterDamage(10000,960,960,true)<308,"burst stops at first health gate");
        Check(first.AdvancePhase(.68f)&&first.Phase==2,"second phase at authored HP gate");
        Check(!first.AdvancePhase(.68f),"same HP cannot skip next phase");
        for(int cycle=0;cycle<100;cycle++)
        {Check(first.Next()==AncientLivingAttack.Cannon,"background cannon first");Check(first.Next()==AncientLivingAttack.Bullets,"double bullets second");Check(first.Next()==AncientLivingAttack.Grab,"barrier grab third");}
        Check(AncientLivingRules.BulletCount(2)==AncientLivingRules.BulletCount(1)*2,"exact doubled bullet count");
        Check(AncientLivingRules.RocketCharge==3,"both fist tells use 3 seconds");
        foreach(var kind in new[]{AncientLivingHit.Laser,AncientLivingHit.Throw,AncientLivingHit.Blade})
        {
            first.RaiseBarrier();int count=kind==AncientLivingHit.Laser?1:kind==AncientLivingHit.Throw?10:7;
            for(int i=1;i<=count;i++)Check(first.HitBarrier(kind)==(i==count),"exact shield activation count "+kind+" "+i);
            Check(!first.HitBarrier(kind),"broken shield cannot retrigger stun");
        }
        for(int throws=0;throws<=10;throws++)for(int cuts=0;cuts<=7;cuts++)
        {
            first.RaiseBarrier();for(int i=0;i<throws;i++)first.HitBarrier(AncientLivingHit.Throw);for(int i=0;i<cuts;i++)first.HitBarrier(AncientLivingHit.Blade);
            Check(first.Barrier==Math.Max(0,70-throws*7-cuts*10),"mixed shield attacks accumulate proportionally");
        }
        first.RaiseBarrier();for(int i=0;i<100;i++)first.HitBarrier(AncientLivingHit.Wire);
        Check(first.Barrier==70,"ordinary zip cannot bypass shield");Check(first.FilterDamage(100,500,960,true)==0,"shield blocks body damage");
        Check(first.AdvancePhase(.34f)&&first.Phase==3&&first.Barrier==0,"third health gate clears shield");
        for(int cycle=0;cycle<200;cycle++)
        {
            Check(first.Next()==AncientLivingAttack.DoubleRocket,"phase 3 double rockets");Check(first.Next()==AncientLivingAttack.XBlast,"phase 3 X");Check(first.Next()==AncientLivingAttack.Suction,"phase 3 suction");
            bool success=cycle%2==0;first.ResolveSuction(success);
            if(!success){Check(first.Next()==AncientLivingAttack.DoubleRocket,"absorbed extra fists");Check(first.Next()==AncientLivingAttack.XBlast,"absorbed extra X");}
            Check(first.Next()==AncientLivingAttack.CrossMissiles,"both branches reach missiles, never loop back to suction");
        }
        Check(first.FilterDamage(float.NaN,100,960,true)==0,"bad damage ignored");Check(first.FilterDamage(50,100,960,false)==0,"background/transition cannot take damage");
        Check(first.FilterDamage(500,100,960,true)==100,"final phase can be defeated");Check(!first.AdvancePhase(0),"no phase 4");
        Check(AncientLivingRules.InSpotlight(0,-8,0,-1,12,.98f),"lit centre detected");Check(!AncientLivingRules.InSpotlight(0,8,0,-1,12,.98f),"behind lamp not detected");Check(!AncientLivingRules.InSpotlight(0,-13,0,-1,12,.98f),"range bounded");
        for(int x=-15;x<=15;x++)for(int y=0;y<=9;y++)
        {
            bool a=AncientLivingRules.BeamHit(-15,.4f,15,8.8f,x,y,2.25f),b=AncientLivingRules.BeamHit(-15,8.8f,15,.4f,x,y,2.25f);
            if(x==0&&y==1)Check(!a&&!b,"standing below X centre is genuinely safe");
            if(x==0&&y==5)Check(a&&b,"X centre deals damage");
            Check(a==AncientLivingRules.BeamHit(15,8.8f,-15,.4f,x,y,2.25f),"swept collision independent of endpoint ordering");
        }
        Check(!AncientLivingRules.BeamHit(0,0,1,0,2,0,.1f),"hits bounded to travelled segment");
        for(int i=4;i<=7;i++)Check(AncientCampaignData.Missions[i].Kind==0,"living trails do not require obsolete switch/escort tasks");
        Check(AncientCampaignData.Missions[5].Tasks[0].Contains("투척"),"wire lesson has explicit objective");
        Console.WriteLine("PASS: "+checks+" living trail, boss HP transitions, barrier, branching and hit geometry checks");
    }
}
