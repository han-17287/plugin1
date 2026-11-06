using System;
using Plugin.AncientCivilization;
class ProductionLayoutChecks
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    static void Main()
    {
        Check(AncientProductionLayout.TutorialLeft<AncientProductionLayout.TutorialSpawn-6,"The opening spawn has a safe margin from its retaining wall");
        Check(AncientProductionLayout.StockX>AncientProductionLayout.SectionStarts[4]&&AncientProductionLayout.StockX<AncientProductionLayout.BreachX-6,"The stock console stands in the inventory hall before the breach wall");
        Check(AncientProductionLayout.TutorialRight<=AncientChapterLayout.CaseLeft(0),"The production route leaves the established continuous chapter entrance intact");
        Check(AncientProductionLayout.ChaseStart>AncientFinaleRules.Finish+256,"The introductory chase cannot overlap the final chase");
        Check(AncientProductionLayout.ChaseEnd>AncientProductionLayout.ChaseStart+1200,"The auto-drive lane has a long uninterrupted roadway");
        Check(AncientChapterLayout.FloorAt(AncientProductionLayout.ChaseStart)<0,"The chase lane does not accidentally become a city investigation case");
        float shortHop=AncientMovementRules.LaunchSpeed(AncientJumpKind.Ground,false),shortHeight=shortHop*shortHop/(2*32);
        Check(AncientProductionLayout.JumpCrateHeight+.2f<shortHeight,"A short Space tap can clear the first jump cargo with a useful height margin");
        float aboveCargoTime=2*(float)Math.Sqrt(shortHop*shortHop-2*32*AncientProductionLayout.JumpCrateHeight)/32;
        Check(AncientProductionLayout.JumpCrateWidth+.72f+.2f<9*aboveCargoTime,"A short running hop clears the first cargo with body width and timing margin; walking can safely land on top");
        for(float x=AncientProductionLayout.TutorialLeft;x<=AncientProductionLayout.TutorialRight;x+=.1f)
        {
            float top=AncientProductionLayout.GroundHeight(x);
            Check(AncientProductionLayout.InTutorial(x),"Every authored ground sample stays inside the novice route");
            Check(top>=0&&top<=.8f,"The mandatory road never creates a death pit or a tall wall");
            Check(top+2.1f+3.1f<AncientProductionLayout.Ceiling,"The normal road leaves clearance for a full jump and the player's body");
        }
        foreach(var ramp in AncientProductionLayout.GentleRamps)
        {
            Check(ramp.Right>ramp.Left&&Math.Abs((ramp.High-ramp.Low)/(ramp.Right-ramp.Left))<=.101f,"The novice road ramps stay at a gentle one-in-ten slope");
            Check(Near(ramp.Height(ramp.Left),ramp.Low)&&Near(ramp.Height(ramp.Right),ramp.High),"Every ramp meets its declared roadway heights");
            for(int step=0;step<=40;step++)
            {
                float x=ramp.Left+(ramp.Right-ramp.Left)*step/40;
                Check(Near(AncientProductionLayout.GroundHeight(x),ramp.Height(x)),"The route height agrees with each actual ramp profile");
            }
        }
        for(int i=0;i<AncientProductionLayout.WireShortcut.Length;i++)
        {
            var ledge=AncientProductionLayout.WireShortcut[i];float hook=AncientProductionLayout.WireHookX[i];
            Check(ledge.Right-ledge.Left>=6,"A novice wire landing has generous standing room");
            Check(hook<ledge.Left-.405f&&hook>ledge.Left-.7f,"Each marked hook sits before the landing edge so a straight zip does not aim through the platform");
            Check(ledge.Top-AncientProductionLayout.CatwalkThickness>2.1f+.3f,"Service gratings leave the ground route clear for the full-height player");
            Check(ledge.Top>=3.2f&&ledge.Top+2.1f<AncientProductionLayout.WireHookY,"The optional route is distinct without trapping the player against the hook");
            Check(AncientProductionLayout.GroundHeight(hook)>=0,"Missing an optional wire landing drops onto the safe ground route");
            Check(AncientProductionLayout.WireHookY-1.1f<AncientZipRules.Range,"A beginner can select the hook directly from the street");
            if(i>0)
            {
                float dx=hook-AncientProductionLayout.WireHookX[i-1],dy=AncientProductionLayout.WireHookY-(AncientProductionLayout.WireShortcut[i-1].Top+1.1f);
                Check(dx*dx+dy*dy<AncientZipRules.Range*AncientZipRules.Range,"The next shortcut hook remains inside the real wire range");
            }
        }
        Check(AncientProductionLayout.WireHookY+1.05f+.045f<AncientProductionLayout.Ceiling,"A nearly horizontal hook chain leaves capsule clearance under the real roof");
        Check(Near(AncientProductionLayout.GroundHeight(AncientProductionLayout.StockX),0),"The inventory interaction point has a flat unobstructed standing height");
        Check(AncientProductionLayout.TunnelCeiling>1.3f+8+1.05f,"The tunnel roof leaves room for the requested high backflip silhouette");
        for(int section=0;section<AncientProductionLayout.SectionStarts.Length;section++)
        {
            Check(AncientProductionLayout.SectionAt(AncientProductionLayout.SectionStarts[section])==section,"Each authored landmark begins at its own readable threshold");
            Check(!string.IsNullOrEmpty(AncientProductionLayout.SectionNames[section]),"Every production stage has a named place");
        }
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            Check(!AncientProductionLayout.InTutorial(bad)&&!AncientProductionLayout.InChaseLane(bad),"Invalid coordinates never enter a playable route");
        Check(!AncientProductionLayout.InTutorial(AncientProductionLayout.TutorialRight+1)&&!AncientProductionLayout.InTutorial(AncientProductionLayout.TutorialLeft-1),"The map region does not silently claim neighboring investigation space");
        Console.WriteLine("PASS: "+checks+" production tutorial, safe landing, wire reach and chase separation checks");
    }
}
