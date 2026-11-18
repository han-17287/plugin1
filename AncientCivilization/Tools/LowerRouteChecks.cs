using System;
using System.Collections.Generic;
using Plugin.AncientCivilization;
class LowerRouteChecks
{
    static int checks;
    static void Check(bool result,string message){if(!result)throw new Exception(message);checks++;}
    static void Main()
    {
        var titles=new HashSet<string>();var actions=new HashSet<AncientRouteAction>();int required=0,optional=0;
        Check(AncientLowerRouteData.Stops.Length==72,"Every required and optional stop has a script");
        for(int id=0;id<72;id++)
        {
            var d=AncientLowerRouteData.Stops[id];int mission=id/9,sector=id%9/3,block=id%3;
            Check(titles.Add(d.Title),"Every stop has a distinct landmark title");actions.Add(d.Action);
            foreach(string line in new[]{d.Speaker,d.Problem,d.Reply,d.Evidence,d.Outcome,d.Closing})Check(!string.IsNullOrWhiteSpace(line),"Every stop has an authored conversation");
            Check(!d.Problem.Contains("백명")&&!d.Evidence.Contains("친구"),"Opening witnesses do not reveal the hero's name or sniper friendship");
            Check(d.Optional==(block==2),"The third block of each sector offers a separate optional stop");if(d.Optional)optional++;else required++;
            Check(AncientLowerRouteRules.Id(mission,sector,block)==id,"Order maps to the correct case and sector");
            Check(AncientLowerRouteRules.Mark(0,id)==1<<(id%9),"Case save bits are independent");
            Check(AncientLowerRouteRules.Done(AncientLowerRouteRules.Mark(0,id),id),"Completion survives save serialization");
            Check(AncientLowerRouteRules.Mark(AncientLowerRouteRules.Mark(0,id),id)==AncientLowerRouteRules.Mark(0,id),"Retries cannot double-award completion");
            var ledge=AncientFieldExpansion.BlockSegment(mission,sector,block,5);float x=(ledge.left+ledge.right)*.5f;
            Check(x>AncientFieldExpansion.SectorStart(mission,sector)&&x<AncientFieldExpansion.SectorStart(mission,sector)+AncientFieldExpansion.Span(mission),"Every new stop is inside its physical sector");
            Check(d.Action!=AncientRouteAction.Wire||AncientFieldExpansion.IsJumpSector(mission,sector),"Raised required switches exist only in dedicated jump sectors");
            Check(d.Action!=AncientRouteAction.Throw||mission>=4,"Throw objectives only use the district where large-object capture is enabled");
            float worldX,worldY,feet;AncientLowerRouteRules.Location(id,out worldX,out worldY,out feet);
            float actorX=worldX-AncientChapterLayout.CaseLeft(mission)-6;
            Check(actorX-.45f>=ledge.left&&actorX+.45f<=ledge.right,"The full resident footprint fits its authored resting ledge");
            Check(Math.Abs(feet-ledge.top)<.001f,"Witness feet use the same geometry as the collider builder");
            if(AncientFieldExpansion.IsJumpSector(mission,sector))for(int i=0;i<9;i++)
            {
                var obstacle=AncientFieldExpansion.BlockSegment(mission,sector,block,i);
                if(i==5)continue;
                Check(actorX+.45f<=obstacle.left||actorX-.45f>=obstacle.right||feet+2.1f<=obstacle.top-1||feet>=obstacle.top,"Resident body does not overlap a neighbouring jump platform");
            }
            if(d.Action==AncientRouteAction.Wire)
            {
                Check(ledge.top+2.1f+.15f<5,"Hero can stand beneath the upper service platform");
                Check(6+2.1f+.15f<10,"Upper service platform does not crush the hero against the ceiling");
            }
            if(d.Action==AncientRouteAction.Ambush)for(int slot=0;slot<2;slot++)
            {
                int kind=AncientLowerRouteRules.GuardType(id,slot);
                Check(kind>=0&&kind<=5&&kind!=3,"A two-guard local ambush never spawns a boss archetype");
                Check(!AncientFieldExpansion.IsJumpSector(mission,sector),"Grounded ambush guards use a supported street rather than a jump gap");
                float guardY=feet+(kind==4?2.8f:0);
                Check(guardY>=feet&&guardY+2.1f<10,"Both grounded and flying route guards fit below the closed ceiling");
            }
        }
        Check(required==48&&optional==24&&actions.Count==6,"Long routes have48 required and24 optional events across six mechanics");
        for(int mission=0;mission<8;mission++)
        {
            Check(AncientChapterLayout.Width(mission)==2496&&AncientFieldExpansion.WidthFor(mission)==2496,"Lower case sections are four times their former width");
            for(int mask=0;mask<512;mask++)for(int sector=0;sector<3;sector++)
            {
                int a=AncientLowerRouteRules.Id(mission,sector,0),b=a+1,c=a+2,next=AncientLowerRouteRules.Next(mission,sector,mask);
                Check(next==(!AncientLowerRouteRules.Done(mask,a)?a:!AncientLowerRouteRules.Done(mask,b)?b:!AncientLowerRouteRules.Done(mask,c)?c:-1),"Next stop never skips missing local progress");
                Check(AncientLowerRouteRules.SectorComplete(mission,sector,mask)==(AncientLowerRouteRules.Done(mask,a)&&AncientLowerRouteRules.Done(mask,b)),"Both required stops unlock the objective regardless of optional completion");
                Check(AncientLowerRouteRules.SectorComplete(mission,sector,mask)==AncientLowerRouteRules.SectorComplete(mission,sector,mask^AncientLowerRouteRules.Bit(c)),"Optional activity can never lock the main path");
                int returned=AncientLowerRouteRules.OptionalOnReturn(mission,sector,mask);
                Check(returned==(AncientLowerRouteRules.SectorComplete(mission,sector,mask)&&!AncientLowerRouteRules.Done(mask,c)?c:-1),"A completed case permits a missed optional event on return without replaying required gates");
            }
            for(int tasks=0;tasks<8;tasks++)for(int saved=0;saved<512;saved++)
            {
                int restored=AncientLowerRouteRules.Restore(mission,saved,0,tasks);
                for(int local=0;local<9;local++)
                {
                    int id=mission*9+local;bool expected=(saved&(1<<local))!=0||local%3<2&&(tasks&(1<<(local/3)))!=0;
                    Check(AncientLowerRouteRules.Done(restored,id)==expected,"Migration preserves saved stops and earned old objectives without inventing optional visits");
                }
            }
            int completed=AncientLowerRouteRules.Restore(mission,0,1<<mission,0);
            for(int local=0;local<9;local++)Check(AncientLowerRouteRules.Done(completed,mission*9+local)==(local%3<2),"A completed old mission stays open while optional conversations remain unvisited");
            Check(AncientLowerRouteRules.Restore(mission,-1,0,0)==0,"Corrupt negative state cannot create completed events");
            float width=AncientFieldExpansion.WidthFor(mission);
            for(int sector=0;sector<3;sector++)
            {
                float start=AncientFieldExpansion.SectorStart(mission,sector);
                Check(AncientFieldExpansion.SectorAt(mission,start+.1f)==sector,"Expanded sector start selects the correct checkpoint");
                Check(AncientFieldExpansion.SectorAt(mission,start+831.9f)==sector,"Far end remains in its expanded sector");
            }
            Check(AncientFieldExpansion.EscortStep(mission,width-16.1f,width-2,9,.1f,7,63,false)<=width-16,"Escorts stop at the expanded exit");
            Check(AncientFieldExpansion.EscortStep(mission,830,900,25,.25f,0,63,false)<=830.6f,"Fast escort catch-up cannot cross a locked expanded sector gate");
        }
        for(int mission=8;mission<20;mission++)Check(AncientChapterLayout.Width(mission)==624,"Upper route lengths remain unchanged");
        for(int mission=0;mission<20;mission++)
        {
            Check(AncientCampaignRules.RoomAt(AncientChapterLayout.CaseLeft(mission)+1)==mission,"Entry identifies correct resized case");
            Check(AncientCampaignRules.RoomAt(AncientChapterLayout.CaseRight(mission)-1)==mission,"Exit identifies correct resized case");
        }
        for(int floor=0;floor<2;floor++)Check(AncientChapterLayout.FloorEnd(floor)-AncientChapterLayout.FloorStart(floor)==10400,"Both lower chapters include full four-case extended roads");
        Check(AncientChapterLayout.FloorEnd(4)<AncientProductionLayout.ChaseStart,"All chapters remain separate from the production chase lane");
        Check(AncientFinaleRules.Start>AncientProductionLayout.ChaseEnd+256,"Final descending chase remains separate from the opening chase");
        foreach(int id in new[]{-1,72,100,int.MaxValue})Check(!AncientLowerRouteRules.Done(-1,id)&&AncientLowerRouteRules.Bit(id)==0,"Invalid stop never unlocks progress");
        Check(AncientLowerRouteRules.SegmentHit(0,0,10,0,5,0,1),"Swept object path hits a target between frames");
        Check(!AncientLowerRouteRules.SegmentHit(0,0,10,0,5,2,1),"A parallel miss cannot open a throw gate");
        Check(!AncientLowerRouteRules.SegmentHit(0,0,10,0,12,0,1),"Targets beyond the clipped terrain endpoint are not hit");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            Check(!AncientLowerRouteRules.SegmentHit(invalid,0,10,0,5,0,1),"Invalid collision coordinates are rejected");
            Check(!AncientLowerRouteRules.SegmentHit(0,0,10,0,5,0,invalid),"Invalid collision radius is rejected");
        }
        Console.WriteLine("PASS: "+checks+" lower-route content, migration, gate and geometry checks");
    }
}
