using System;
using Plugin.AncientCivilization;
class RulesChecks
{
    static int checks;
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;}
    static void Main()
    {
        Check(AncientRules.ResolveLeftRelease(.1f,0)==LeftReleaseAction.Blade,"Tap is a blade even without energy");
        Check(AncientRules.ResolveLeftRelease(.219f,100)==LeftReleaseAction.Blade,"Tap boundary");
        Check(AncientRules.ResolveLeftRelease(.22f,100)==LeftReleaseAction.Cancel,"Charging does not accidentally melee");
        Check(AncientRules.ResolveLeftRelease(.849f,100)==LeftReleaseAction.Cancel,"Early release never fires");
        Check(AncientRules.ResolveLeftRelease(.85f,1)==LeftReleaseAction.Laser,"Ready release fires");
        Check(AncientRules.ResolveLeftRelease(4,0)==LeftReleaseAction.Empty,"No energy cannot fire");
        Check(AncientRules.ResolveLeftRelease(-1,100)==LeftReleaseAction.Cancel,"Cancelled press cannot fire");
        Check(!AncientRules.TutorialReady(1,0),"Patrol starts incomplete");
        Check(!AncientRules.TutorialReady(1,6),"Patrol requires movement");
        Check(!AncientRules.TutorialReady(1,5),"Patrol requires jumping");
        Check(!AncientRules.TutorialReady(1,3),"Patrol requires Ctrl running");
        Check(AncientRules.TutorialReady(1,7),"All patrol tasks complete");
        Check(!AncientRules.TutorialReady(2,7),"Patrol tasks cannot skip equipment tutorial");
        Check(!AncientRules.TutorialReady(2,8),"Wire alone cannot skip motorcycle task");
        Check(!AncientRules.TutorialReady(2,16),"Motorcycle alone cannot skip wire task");
        Check(AncientRules.TutorialReady(2,24),"Equipment tasks complete");
        Check(!AncientRules.TutorialReady(3,192),"Blade drill requires destroying a projectile");
        Check(!AncientRules.TutorialReady(3,160),"Blade drill requires collecting particles");
        Check(!AncientRules.TutorialReady(3,96),"Blade drill requires firing a laser");
        Check(AncientRules.TutorialReady(3,224),"All blade drill tasks complete");
        Check(!AncientRules.TutorialReady(0,255)&&!AncientRules.TutorialReady(4,255),"Only tutorial stages can complete tutorial goals");
        for(int stage=0;stage<10;stage++)
        {
            Check(!AncientRules.CanStartMiniBoss(stage,false,3,110),"Moving truck cannot start boss");
            Check(!AncientRules.CanStartMiniBoss(stage,true,2,110),"Missing core cannot start boss");
            Check(!AncientRules.CanStartMiniBoss(stage,true,3,98),"Boss requires arrival at checkpoint");
            Check(AncientRules.CanStartMiniBoss(stage,true,3,110)==(stage==5),"Boss gated behind quest sequence");
        }
        Check(AncientRules.PointsAfterArrest(200,6)==1200,"Arrest promotion reward");
        Check(AncientRules.PointsAfterArrest(1200,7)==1200,"Re-entering arrest dialogue cannot repeat reward");
        Check(!AncientRules.CanEnterUpperCity(6,1000,false),"Combat completion alone cannot skip dialogue");
        Check(!AncientRules.CanEnterUpperCity(8,999,false),"Upper city requires enough points");
        Check(AncientRules.CanEnterUpperCity(8,1000,false),"Promotion authorizes upper city");
        Check(!AncientRules.CanEnterUpperCity(9,1000,true),"Already arrived cannot enter lift again");
        Console.WriteLine("PASS: "+checks+" combat and quest rule checks");
    }
}
