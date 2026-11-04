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
                bounded&=n0*n0+z0*z0<=.14001f*.14001f;
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
        Check(AncientCityLayout.LastFloor==4,"Campaign stops at the top city; no ancient-civilization map exists");
        Check(!AncientCityLayout.CanLift(7,1000,0,0,1),"Initial promotion dialogue still gates first upper floor");
        Check(AncientCityLayout.CanLift(8,1000,0,0,1),"Existing saves can enter the living district");
        Check(!AncientCityLayout.CanLift(9,100000,0,1,2),"Points cannot skip the living-district boss");
        Check(AncientCityLayout.CanLift(9,2000,2,1,2),"Clearing living district unlocks administrative district");
        Check(!AncientCityLayout.CanLift(9,2000,2,1,3),"Administrative boss cannot be skipped");
        Check(AncientCityLayout.CanLift(9,3000,6,2,3),"Administrative case unlocks residential district");
        for(int mask=0;mask<32;mask++)
        {
            Check(!AncientCityLayout.CanLift(99,999999,mask,3,4),"Top floor is never unlocked by promotion");
            Check(!AncientCityLayout.CanLift(99,999999,mask,4,5),"There is no ancient destination in first campaign");
        }
        Check(AncientCityLayout.CanInfiltrate(14,3),"Residential boss opens illegal side passage");
        Check(!AncientCityLayout.CanInfiltrate(6,3),"Side passage waits for residential encounter completion");
        Check(!AncientCityLayout.CanInfiltrate(14,2),"Side passage is physically in residential district");
        Check(AncientCityLayout.CanLift(9,3000,14,3,0),"Unlocked city floors can return home");
        Check(!AncientCityLayout.CanLift(9,3000,14,2,2),"Current destination remains disabled");
        Check(!AncientCityLayout.CanLift(9,3000,14,-1,0),"Invalid floor save is rejected by travel rules");
        for(int floor=2;floor<=4;floor++)
        {
            var route=AncientCityLayout.Route(floor);
            Check(route[0].left<=AncientCityLayout.Entry(floor)&&route[0].right>=AncientCityLayout.Entry(floor),"New district entry rests on safe ground");
            Check(route[route.Length-1].left<=AncientCityLayout.ArenaLeft(floor),"Boss approach joins final stable floor");
            Check(route[route.Length-1].right>=AncientCityLayout.ArenaRight(floor),"Boss arena has full solid floor");
            for(int i=0;i<route.Length;i++)
            {
                Check(route[i].right-route[i].left>=7,"New district basic route has broad landings");
                Check(route[i].top+2.1f<10,"New district platforms fit under continuous ceiling");
                if(i>0)Check(route[i].left-route[i-1].right<=5,"New district basic route uses short wire gaps");
            }
        }
        Check(!AncientCityLayout.ShutterClosed(1.8f,0)&&AncientCityLayout.ShutterWarning(1.8f,0),"Shutter warns before becoming solid");
        Check(AncientCityLayout.ShutterClosed(2.1f,0),"Shutter closes after warning");
        Check(!AncientCityLayout.ShutterClosed(3.7f,0),"Shutter reopens every cycle");
        Check(AncientCityLayout.VentLift(-15,.016f)>=6,"Updraft arrests a fall and launches upwards");
        Check(AncientCityLayout.VentLift(14,.016f)==14,"Updraft preserves faster existing ascent");
        Check(AncientCityLayout.VentLift(11,1)==12,"Updraft acceleration remains capped");
        Check(AncientRules.BladeAnimationFrame(0)==0,"Slash starts with anticipation");
        Check(AncientRules.BladeAnimationFrame(.149f)==2,"Windup ends before the damage frame");
        Check(AncientRules.BladeAnimationFrame(AncientRules.BladeHitTime)==3,"Blade damage begins on the actual sweep");
        Check(AncientRules.BladeAnimationFrame(.399f)==7,"Slash includes recovery rather than popping to idle");
        bool ordered=true;int prior=0,seenFrames=0;
        for(int i=0;i<400;i++)
        {
            int frame=AncientRules.BladeAnimationFrame(i*.001f);
            ordered&=frame>=prior&&frame>=0&&frame<8;seenFrames|=1<<frame;prior=frame;
        }
        Check(ordered&&seenFrames==255,"Slash plays every frame once in forward order");
        var islands=new uint[120*20];
        for(int y=3;y<15;y++)for(int x=2;x<20;x++)islands[y*120+x]=0xFFFF00FF;
        for(int y=3;y<15;y++)for(int x=80;x<100;x++)islands[y*120+x]=0x00FFFFFF;
        var actor=AncientPixels.Isolate(islands,120,20,8,8);
        Check(actor[8*120+8]==0xFFFF00FF&&actor[8*120+88]==0,"Sprite extraction keeps one actor without neighbouring art");
        Check(islands[8*120+88]==0x00FFFFFF,"Sprite extraction leaves source atlas intact");
        var diagonal=new uint[100];for(int i=0;i<10;i++)diagonal[i*10+i]=0xFFFF00FF;
        var selected=AncientPixels.Isolate(diagonal,10,10,0,0);
        Check(selected[99]!=0,"Diagonal sword pixels stay attached through eight-neighbour extraction");
        var empty=AncientPixels.Isolate(new uint[100],10,10,5,5);
        Check(Array.TrueForAll(empty,c=>c==0),"An empty sprite region stays transparent");
        Check(AncientWireMath.Samples(2,true)==24,"Short wires use a small geometry budget");
        Check(AncientWireMath.Samples(5,true)==48,"Medium wires use an intermediate geometry budget");
        Check(AncientWireMath.Samples(22,false)==96,"Light mode caps helix vertices");
        Check(AncientWireMath.Samples(22,true)==192,"Long high-quality wires retain smooth helices");
        foreach(float length in new[]{0f,.1f,2f,6f,22f})foreach(float age in new[]{0f,.5f,1.35f})
        {
            var normal=new float[192];var depth=new float[192];bool equivalent=true;
            AncientWireMath.Fill(length,age,0,192,normal,depth);
            for(int i=0;i<192;i++)
            {
                float n,z;AncientRules.WireHelixOffset(length,age,i/191f,0,out n,out z);
                equivalent&=Math.Abs(n-normal[i])<.00001f&&Math.Abs(z-depth[i])<.00001f;
            }
            Check(equivalent,"Optimized wire recurrence preserves the agreed DNA geometry");
        }
        Check(AncientCombatRules.Damage(20,false,false)==20,"Ordinary enemies keep full incoming damage");
        Check(AncientCombatRules.Damage(20,true,false)==12,"Boss armor reduces damage outside its opening");
        Check(AncientCombatRules.Damage(20,true,true)==25,"Boss opening rewards a timed attack");
        Check(AncientCombatRules.Damage(0,true,true)==0,"Zero damage stays zero");
        Check(AncientCombatRules.Damage(-10,true,true)==0,"Negative input cannot heal an enemy");
        Check(AncientCombatRules.Recovery(4,1,false)>=1.5f,"Sniper reload has a readable punish window");
        for(int district=0;district<=4;district++)for(int pattern=0;pattern<2;pattern++)
            Check(AncientCombatRules.Recovery(district,pattern,true)>=.7f,"Enrage never removes all reaction time");
        Check(AncientCombatRules.PerfectBreak(1,-1),"Close incoming object can earn a precise-break reward");
        Check(!AncientCombatRules.PerfectBreak(4,-1),"Distant object break is ordinary");
        Check(!AncientCombatRules.PerfectBreak(1,1),"A departing object does not award a precise parry");
        for(int floor=0;floor<=4;floor++)
        {
            Check(AncientCityLayout.SafeResume(floor,AncientCityLayout.Entry(floor),1.3f),"Saved floor entry is a valid retry position");
            Check(AncientCityLayout.SafeResume(floor,AncientCityLayout.ArenaLeft(floor)+1.5f,1.3f),"Boss checkpoint stays inside its own floor");
        }
        Check(!AncientCityLayout.SafeResume(5,700,1.3f),"Retry cannot enter a second-run ancient map");
        Check(!AncientCityLayout.SafeResume(2,110,1.3f),"Retry cannot restore coordinates on another floor");
        Check(!AncientCityLayout.SafeResume(2,310,-5),"Retry never restores the player below safe ground");
        int cacheMask=0;
        for(int id=0;id<AncientProgressRules.CacheCount;id++)
        {
            Check(!AncientProgressRules.HasCache(cacheMask,id),"New exploration reward is initially available");
            cacheMask=AncientProgressRules.ClaimCache(cacheMask,id);
            Check(AncientProgressRules.HasCache(cacheMask,id),"Exploration reward is persisted");
            Check(AncientProgressRules.ClaimCache(cacheMask,id)==cacheMask,"Repeated collection cannot award the same cache twice");
        }
        for(int floor=0;floor<=4;floor++)Check(AncientProgressRules.CachesOnFloor(cacheMask,floor)==2,"Each floor has two optional rewards");
        Check(AncientProgressRules.ClaimCache(cacheMask,99)==cacheMask,"Invalid cache cannot corrupt progression mask");
        Check(!AncientProgressRules.KnowsEvidence(1,4,0),"Theft evidence is not revealed before its dialogue ends");
        Check(AncientProgressRules.KnowsEvidence(1,5,0),"Confirmed theft evidence appears in journal");
        Check(!AncientProgressRules.KnowsEvidence(4,9,0),"Top-floor access clue is not spoiled early");
        Check(AncientProgressRules.KnowsEvidence(4,9,8),"Denied promotion clue follows the residential encounter");
        Check(AncientProgressRules.KnowsEvidence(5,9,16),"Friend recognition note follows the sniper encounter");
        Check(AncientFinaleRules.Road(800)==60,"Final chase starts at the top cargo road");
        Check(AncientFinaleRules.Road(1400)==0,"Final chase reaches the lowest city floor");
        Check(AncientFinaleRules.Road(1500)==0,"There is no playable ancient road below the endpoint");
        for(int section=0;section<5;section++)
        {
            float x=AncientFinaleRules.Checkpoint(section);
            Check(AncientFinaleRules.Zone(x)==section,"Every chase checkpoint restores its matching section");
            Check(AncientFinaleRules.Road(x)>=0&&AncientFinaleRules.Road(x)<=60,"Checkpoint remains on the city descent");
        }
        bool continuous=true,clearance=true;
        for(float x=800;x<1400;x+=12)
        {
            float y0=AncientFinaleRules.Road(x),y1=AncientFinaleRules.Road(x+12);
            continuous&=Math.Abs((y0-y1)-1.2f)<.00002f;
            clearance&=y1+10-y0>2.1f;
        }
        Check(continuous,"Descending road segments meet without height gaps");
        Check(clearance,"Stepped cargo ceiling leaves full character headroom");
        Check(AncientFinaleRules.TruckSpeed(800,900)==5,"Distant truck slows so the player can catch up");
        Check(AncientFinaleRules.TruckSpeed(1000,900)==15,"Leading player cannot accelerate the truck without bound");
        Check(!AncientFinaleRules.CanBoardEnd(900,900,54),"Support-shot climax cannot start halfway down");
        Check(!AncientFinaleRules.CanBoardEnd(1393,1393,1.75f),"Running beside the truck is not boarding it");
        Check(AncientFinaleRules.CanBoardEnd(1393,1393,4.25f),"Standing on the final truck roof triggers the climax");
        Check(!AncientProgressRules.KnowsEvidence(6,9,31,0),"Optional clinic evidence stays unknown until earned");
        Check(AncientProgressRules.KnowsEvidence(6,9,31,1),"Clinic job records its discovered evidence");
        Check(!AncientProgressRules.KnowsEvidence(7,9,31,1),"Clinic job cannot unlock the administrative evidence");
        Check(AncientProgressRules.KnowsEvidence(7,9,31,2),"Original/snapshot comparison records the second field case");
        Check(AncientProgressRules.KnowsEvidence(8,9,31,4),"Guarded terminal records its own field evidence");
        Check(AncientFinaleRules.RestorePhase(0)==0,"Unstarted finale stays unstarted");
        Check(AncientFinaleRules.RestorePhase(1)==1,"Operation countdown can resume");
        Check(AncientFinaleRules.RestorePhase(2)==2,"Chase restores to its road checkpoint");
        Check(AncientFinaleRules.RestorePhase(3)==2,"Interrupted ambush restores to playable chase, not a frozen movie");
        Check(AncientFinaleRules.RestorePhase(4)==2,"Interrupted explosion restores to playable chase");
        Check(AncientFinaleRules.RestorePhase(5)==5,"Completed first-run end card stays completed");
        Console.WriteLine("PASS: "+checks+" combat and quest rule checks");
    }
}
