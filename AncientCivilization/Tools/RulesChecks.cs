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
        Check(!AncientRules.CanCollectParticles(false,true,false,true,0),"Left click without RMB cannot collect particles");
        Check(AncientRules.CanCollectParticles(true,true,false,true,0),"RMB collects loose particles");
        Check(!AncientRules.CanCollectParticles(true,false,false,true,0),"Unfocused window cannot collect");
        Check(!AncientRules.CanCollectParticles(true,true,true,true,0),"Dialogue and pause block collection");
        Check(!AncientRules.CanCollectParticles(true,true,false,false,0),"Menu-close input guard blocks collection");
        Check(!AncientRules.CanCollectParticles(true,true,false,true,100),"Full reservoir cannot collect");
        Check(AncientPixels.Quantize(0xFFFFFFFFu)==AncientPixels.Quantize(AncientPixels.Quantize(0xFFFFFFFFu)),"Palette quantization is stable");
        Check(AncientPixels.Quantize(0xFFFFFF30u)==0,"Soft background alpha becomes transparent");
        Check((AncientPixels.Quantize(0x7193B2FFu)&255)==255,"Visible pixels are opaque without soft edges");
        var source=new uint[16*16];for(int i=0;i<source.Length;i++)source[i]=((uint)i<<24)|0x005070FFu;
        var grid=AncientPixels.Grid(source,16,16,8,16,16);bool block=true;
        for(int y=0;y<8;y++)for(int x=0;x<8;x++)block&=grid[y*16+x]==grid[0];
        Check(block,"Native pixel blocks are identical 8x8 squares");
        var partial=AncientPixels.Grid(source,16,16,8,5,7);bool untouched=true;
        for(int y=0;y<16;y++)for(int x=0;x<16;x++)if(x>=5||y>=7)untouched&=partial[y*16+x]==source[y*16+x];
        Check(untouched,"Pixel processing preserves motorcycle parts outside the rider region");
        Check(source[0]==0x005070FFu,"Pixel processing never mutates source resources");
        foreach(float length in new[]{0f,.1f,2f,14f,30f})
        {
            bool centred=true,bounded=true,anchored=true;
            for(int i=0;i<=100;i++)
            {
                float t=i/100f,n0,z0,n1,z1;
                AncientRules.WireHelixOffset(length,1.7f,t,0,out n0,out z0);
                AncientRules.WireHelixOffset(length,1.7f,t,1,out n1,out z1);
                centred&=Math.Abs(n0+n1)<.00001&&Math.Abs(z0+z1)<.00001;
                bounded&=n0*n0+z0*z0<=.10501f*.10501f;
                if(i==0||i==100)anchored&=Math.Abs(n0)+Math.Abs(z0)<.00001;
            }
            Check(centred,"Double helix remains centred on straight core at length "+length);
            Check(bounded,"Helix stays within wire envelope at length "+length);
            Check(anchored,"Helix converges at emitter and anchor at length "+length);
        }
        Check(!AncientRules.CanSelectLiftFloor(7,1000,0,1),"Promotion dialogue must finish before upper floor unlocks");
        Check(!AncientRules.CanSelectLiftFloor(8,999,0,1),"Upper floor requires civic points");
        Check(AncientRules.CanSelectLiftFloor(8,1000,0,1),"Promotion unlocks elevator destination without moving player");
        Check(AncientRules.CanSelectLiftFloor(9,1000,1,0),"Upper city can return to lower city");
        Check(AncientRules.CanSelectLiftFloor(9,1000,0,1),"Return trip does not revoke upper city authorization");
        Check(AncientRules.CanSelectLiftFloor(9,0,1,0),"Returning home does not require spending points");
        Check(!AncientRules.CanSelectLiftFloor(9,1000,1,1),"Cannot travel to current floor");
        Check(!AncientRules.CanSelectLiftFloor(9,1000,0,0),"Lower city is marked as current floor");
        Check(!AncientRules.CanSelectLiftFloor(9,1000,0,2),"Unimplemented destination stays unavailable");
        Check(!AncientRules.CanSelectLiftFloor(9,1000,-1,0),"Invalid source floor is rejected");
        Check(AncientRules.WireHitDamage(false)==12,"Wire hit deals normal enemy damage");
        Check(AncientRules.WireHitDamage(true)==3,"Boss resists wire hit damage");
        Check(AncientRules.WireImpactDamage(false)==18,"Enemy collision has impact damage");
        Check(AncientRules.WireImpactDamage(true)==4,"Boss resists collision damage");
        Check(AncientRules.WirePushDistance(false)>AncientRules.WirePushDistance(true),"Boss knockback is smaller");
        Check(AncientRules.IgnoreWireContact(true,true,0,5),"Pulling into target prevents its body-contact damage");
        Check(AncientRules.IgnoreWireContact(true,false,5.3f,5.1f),"Impact gives brief target-contact protection");
        Check(!AncientRules.IgnoreWireContact(true,false,5.3f,5.3f),"Contact protection expires");
        Check(!AncientRules.IgnoreWireContact(false,true,6,5),"Other enemies are not covered by wire protection");
        Check(AncientRules.WireImpactOnStep(2,0,4,0,false),"Fast wire pull hits a target crossed within a physics step");
        Check(!AncientRules.WireImpactOnStep(2,0,4,0,true),"Wall blocks wire impact");
        Check(!AncientRules.WireImpactOnStep(2,2,4,0,false),"Wire near miss does not deal collision damage");
        Check(!AncientRules.WireImpactOnStep(4,0,1,0,false),"Distant target is not hit before arrival");
        Check(AncientRules.WireImpactOnStep(.5f,0,0,0,false),"Close-range wire hit resolves without division by zero");
        Check(AncientRules.BladeReboundSpeed(-20)==6,"Blade rebound arrests a fall with a small upward impulse");
        Check(AncientRules.BladeReboundSpeed(3)==7,"Blade rebound adds lift during an ordinary ascent");
        Check(AncientRules.BladeReboundSpeed(8)==9,"Repeated rebound upward speed is capped");
        Check(AncientRules.BladeReboundSpeed(14)==14,"Rebound never slows an existing fast jump");
        Check(AncientTraversalLayout.MainRoute[0].left<=132&&AncientTraversalLayout.MainRoute[0].right>=136,"Elevator and course entrance stand on safe ground");
        Check(AncientTraversalLayout.MainRoute[AncientTraversalLayout.MainRoute.Length-1].right>AncientTraversalLayout.Finish,"Finish is on a wide landing");
        foreach(var ledge in AncientTraversalLayout.MainRoute)
        {
            Check(ledge.right-ledge.left>=4,"Beginner route has forgiving landing width");
            Check(ledge.top+2.1f<AncientTraversalLayout.Ceiling,"Beginner ledge has full player headroom");
        }
        for(int i=1;i<AncientTraversalLayout.MainRoute.Length;i++)
        {
            var before=AncientTraversalLayout.MainRoute[i-1];var next=AncientTraversalLayout.MainRoute[i];
            Check(next.left-before.right<=6,"Beginner route gap remains within short wire range");
            Check(next.top-before.top<3.0f,"Beginner route avoids oversized vertical steps");
        }
        foreach(var ledge in AncientTraversalLayout.FastRoute)
        {
            Check(ledge.top+2.1f<AncientTraversalLayout.Ceiling,"Fast route leaves clearance under continuous ceiling");
            Check(ledge.top>5,"Fast route is separated from beginner platforms");
        }
        Check(AncientTraversalLayout.TargetX.Length==AncientTraversalLayout.TargetY.Length,"All rebound targets have complete coordinates");
        for(int i=0;i<AncientTraversalLayout.TargetX.Length;i++)
            Check(AncientTraversalLayout.TargetY[i]+.6f<AncientTraversalLayout.Ceiling,"Rebound target floats below ceiling");
        foreach(var ledge in AncientTraversalLayout.LowerRoute)
        {
            Check(ledge.right-ledge.left>=4,"Factory main route provides broad landings");
            Check(ledge.top+2.1f<10,"Factory roof allows full character headroom");
            float road=ledge.left>=40&&ledge.left<=58?1:0;
            Check(ledge.top-1-road>2.1f,"Factory roofs leave the chase road clear below");
        }
        for(int i=1;i<AncientTraversalLayout.LowerRoute.Length;i++)
            Check(AncientTraversalLayout.LowerRoute[i].left-AncientTraversalLayout.LowerRoute[i-1].right<=7,"Factory route has short wire gaps");
        for(int crane=0;crane<3;crane++)
        {
            bool bounded=true,moving=false;float previous=AncientRules.CraneOffset(0,crane);
            for(int i=0;i<500;i++)
            {
                float offset=AncientRules.CraneOffset(i*.03f,crane);
                bounded&=Math.Abs(offset)<=2.70001f;moving|=Math.Abs(offset-previous)>.1f;
            }
            Check(bounded,"Crane stays inside its rail span");Check(moving,"Crane anchor changes position over time");
        }
        Console.WriteLine("PASS: "+checks+" combat and quest rule checks");
    }
}
