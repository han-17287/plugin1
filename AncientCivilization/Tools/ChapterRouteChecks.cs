using System;
using Plugin.AncientCivilization;

class ChapterRouteChecks
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    static void Main()
    {
        Check(AncientChapterLayout.CaseCount==20&&AncientChapterLayout.CasesPerFloor==4,"The existing twenty cases form four sections of each of five chapters");
        Check(AncientChapterLayout.CaseWidth==AncientFieldExpansion.Width,"Chapter geometry preserves the authored field section width");
        Check(AncientChapterLayout.CaseStride==AncientFieldExpansion.Stride,"Campaign placement and chapter bridge stride agree");
        Check(AncientChapterLayout.BridgeWidth==64,"Cases are separated by physical64-unit connectors rather than portals");
        for(int floor=0;floor<AncientChapterLayout.FloorCount;floor++)
        {
            int first=floor*4,last=first+3;
            float left=AncientCityLayout.Left(floor),right=AncientCityLayout.Right(floor),entry=AncientCityLayout.Entry(floor);
            Check(left<entry&&entry<AncientChapterLayout.CaseLeft(first),"A chapter elevator arrival is before its first case");
            Check(AncientCityLayout.SafeResume(floor,entry,1.3f),"Every elevator arrival is a safe same-floor resume point");
            var introduction=AncientCityLayout.StreetRoad(floor);
            Check(Near(introduction.left,left)&&Near(introduction.right,AncientChapterLayout.CaseLeft(first))&&introduction.top==0,"The introductory street stops at the first case instead of filling the chapter's jump pits");
            Check(Near(AncientChapterLayout.CaseLeft(first)-AncientChapterLayout.FloorStart(floor),128),"Every chapter has its introductory128-unit approach");
            Check(AncientChapterLayout.CaseRight(last)==AncientChapterLayout.CaseEnd(floor),"The destination follows exactly four complete case sections");
            Check(AncientChapterLayout.CaseEnd(floor)<AncientChapterLayout.ExitLift(floor)&&AncientChapterLayout.ExitLift(floor)<right,"The onward lift is beyond all four case sections but inside the chapter");
            Check(AncientCityLayout.SafeResume(floor,AncientChapterLayout.ExitLift(floor),1.3f),"Onward elevator checkpoints remain valid after finishing a chapter");
            Check(!AncientCityLayout.SafeResume(floor,left,1.3f)&&!AncientCityLayout.SafeResume(floor,right,1.3f),"A resume point cannot be placed inside either chapter boundary wall");
            if(floor<4)Check(right<AncientCityLayout.Left(floor+1),"Different floor maps cannot overlap their physical colliders");
            if(floor>0)Check(AncientCityLayout.ArenaRight(floor)-AncientCityLayout.ArenaLeft(floor)==32,"District and sniper arenas retain an explicit32-unit combat span");
            if(floor>0&&floor<4)Check(AncientCityLayout.ArenaLeft(floor)>AncientChapterLayout.CaseEnd(floor)&&AncientCityLayout.ArenaRight(floor)<AncientChapterLayout.ExitLift(floor),"Ordinary chapter bosses stand after the case route and before the onward lift");
            for(int mission=first;mission<=last;mission++)
            {
                float a=AncientChapterLayout.CaseLeft(mission),b=AncientChapterLayout.CaseRight(mission);
                Check(a>=left&&b<right&&b-a==624,"Every full case section fits inside its own chapter");
                for(float x=a;x<=b;x+=8)
                {
                    Check(AncientChapterLayout.FloorAt(x)==floor&&AncientCityLayout.VisualSector(x)==floor,"Every point on a case route renders as its own floor");
                    Check(AncientCityLayout.SafeResume(floor,x,1.3f),"Checkpoints along the long route are accepted on their real floor");
                    for(int other=0;other<5;other++)if(other!=floor)Check(!AncientCityLayout.SafeResume(other,x,1.3f),"A saved route position cannot restore into a different chapter");
                }
                if(mission<last)
                {
                    float next=AncientChapterLayout.CaseLeft(mission+1);
                    Check(next-b==64,"Every same-floor case handoff has a64-unit walkable bridge");
                    for(float x=b;x<=next;x+=4)Check(AncientChapterLayout.FloorAt(x)==floor&&AncientCityLayout.VisualSector(x)==floor,"Bridge scenery does not become cargo-road or another-floor scenery");
                }
            }
            foreach(float y in new[]{.5f,1.3f,4f,8.49f})Check(AncientCityLayout.SafeResume(floor,entry,y),"Finite supported resume heights stay valid");
            foreach(float y in new[]{-.1f,.49f,8.5f,100f})Check(!AncientCityLayout.SafeResume(floor,entry,y),"Unsafe floor and ceiling resume heights are rejected");
        }
        Check(AncientChapterLayout.CaseRight(18)<AncientChapterLayout.SniperLeft&&AncientChapterLayout.SniperRight<AncientChapterLayout.CaseLeft(19),"The sniper encounter fits between third and fourth top-floor cases");
        Check(AncientChapterLayout.CaseRight(19)<AncientChapterLayout.SecretaryLeft,"The secretary is reached only after the fourth top-floor case");
        Check(AncientChapterLayout.SecretaryRight<AncientChapterLayout.OfficeX&&AncientChapterLayout.OfficeX<AncientChapterLayout.FloorEnd(4),"The empty chairman office lies beyond the secretary, within the last chapter");
        Check(AncientCityLayout.BossArenaLeft(4,false)==AncientChapterLayout.SniperLeft&&AncientCityLayout.BossArenaRight(4,false)==AncientChapterLayout.SniperRight,"The sniper keeps the interstitial arena contract");
        Check(AncientCityLayout.BossArenaLeft(4,true)==AncientChapterLayout.SecretaryLeft&&AncientCityLayout.BossArenaRight(4,true)==AncientChapterLayout.SecretaryRight,"The secretary receives a distinct arena instead of teleporting backward to the sniper");
        for(int floor=0;floor<4;floor++)Check(AncientCityLayout.BossArenaLeft(floor,true)==AncientCityLayout.ArenaLeft(floor)&&AncientCityLayout.BossArenaRight(floor,true)==AncientCityLayout.ArenaRight(floor),"Secretary flags never displace another floor's boss arena");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            Check(AncientChapterLayout.FloorAt(invalid)==-1&&AncientCityLayout.VisualSector(invalid)==0,"Invalid world positions cannot select a chapter or cargo route");
            Check(!AncientCityLayout.SafeResume(0,invalid,1.3f)&&!AncientCityLayout.SafeResume(4,AncientCityLayout.Entry(4),invalid),"Invalid save coordinates are rejected");
        }
        foreach(int invalid in new[]{int.MinValue,-1,5,int.MaxValue})
        {
            int floor=invalid<0?0:4;
            Check(AncientChapterLayout.FloorStart(invalid)==AncientChapterLayout.FloorStart(floor)&&AncientChapterLayout.FloorEnd(invalid)==AncientChapterLayout.FloorEnd(floor),"Out-of-range floor layout requests clamp before multiplication");
            Check(!AncientCityLayout.SafeResume(invalid,AncientCityLayout.Entry(floor),1.3f),"A clamped layout helper does not authorize an invalid save floor");
        }
        Check(AncientChapterLayout.CaseLeft(int.MinValue)==AncientChapterLayout.CaseLeft(0)&&AncientChapterLayout.CaseRight(int.MaxValue)==AncientChapterLayout.CaseRight(19),"Case requests clamp before division and multiplication");
        Check(AncientFinaleRules.Start>AncientChapterLayout.FloorEnd(4),"The descending finale road cannot overlap any extended city chapter");
        Check(AncientCityLayout.VisualSector(AncientFinaleRules.Start)==5,"The relocated finale road uses the cargo visual sector");
        Console.WriteLine("PASS: "+checks+" continuous chapter layout, arena and save-boundary checks");
    }
}
