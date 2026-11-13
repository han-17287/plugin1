using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Encodings.Web;
using Plugin.AncientCivilization;
class LowerRouteExport
{
    static object Ledge(AncientTraversalLayout.Ledge ledge,float offset,float depth=1){return new{x=offset+ledge.left,right=offset+ledge.right,top=ledge.top,depth};}
    static void Main()
    {
        var cases=new List<object>();
        for(int id=0;id<8;id++)
        {
            float left=AncientChapterLayout.CaseLeft(id);var sectors=new List<object>();
            for(int sector=0;sector<3;sector++)
            {
                var platforms=new List<object>();bool jump=AncientFieldExpansion.IsJumpSector(id,sector);
                if(!jump)platforms.Add(Ledge(AncientFieldExpansion.GroundRoad(id,sector),left));
                else for(int block=0;block<AncientFieldExpansion.Blocks(id);block++)
                {
                    platforms.Add(Ledge(AncientFieldExpansion.BlockLanding(id,sector,block,false),left));platforms.Add(Ledge(AncientFieldExpansion.BlockLanding(id,sector,block,true),left));
                    for(int i=0;i<9;i++)
                    {platforms.Add(Ledge(AncientFieldExpansion.BlockSegment(id,sector,block,i),left));if(i%3==1)platforms.Add(Ledge(AncientFieldExpansion.BlockExpertSegment(id,sector,block,i),left,AncientFieldExpansion.ExpertLedgeDepth));}
                }
                var stops=new List<object>();
                for(int block=0;block<3;block++)
                {
                    int stopId=AncientLowerRouteRules.Id(id,sector,block);float x,y,feet;AncientLowerRouteRules.Location(stopId,out x,out y,out feet);var data=AncientLowerRouteData.Stops[stopId];
                    string[] lines=data.Action==AncientRouteAction.Talk?new[]{data.Problem,data.Reply,data.Evidence,AncientLowerRouteData.FollowUp(stopId),data.Outcome,data.Closing}:new[]{data.Problem,data.Reply,data.Evidence,data.Outcome,data.Closing};
                    string[] voices=data.Action==AncientRouteAction.Talk?new[]{data.Speaker,"백 경위",data.Speaker,"백 경위",data.Speaker,"백 경위"}:new[]{data.Speaker,"백 경위",data.Speaker,data.Speaker,"백 경위"};
                    stops.Add(new{id=stopId,x,y,feet,gateX=x+18,actorX=x-6,action=data.Action.ToString(),optional=data.Optional,title=data.Title,speaker=data.Speaker,lines,voices});
                    if(data.Action==AncientRouteAction.Wire)platforms.Add(new{x=x-2.3f,right=x+2.3f,top=6f,depth=1f});
                }
                var node=AncientFieldExpansion.Segment(id,sector,5);
                sectors.Add(new{id=sector,name=AncientFieldExpansion.Names[id][sector],left=left+AncientFieldExpansion.SectorStart(id,sector),width=AncientFieldExpansion.Span(id),jump,platforms,stops,objective=new{x=left+(node.left+node.right)*.5f,y=node.top+1.3f}});
            }
            cases.Add(new{id,floor=id/4,title=AncientCampaignData.Missions[id].Title,left,right=AncientChapterLayout.CaseRight(id),sectors});
        }
        Console.Write(JsonSerializer.Serialize(new{version="0.25.0",source="Actual C# layout/data export. Not a Unity render or playthrough.",targetMinutes=150,cases},new JsonSerializerOptions{WriteIndented=true,Encoder=JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));
    }
}
