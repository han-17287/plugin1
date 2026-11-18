using System;
using Plugin.PlayerCore;
class FloorTraversalChecks
{
    static int checks;
    static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);checks++;}
    static void Main()
    {
        int[] counts={3,4,5},gaps={6,7,8};float[] amps={.5f,1,1.5f};
        for(int floor=0;floor<5;floor++)for(int tier=0;tier<3;tier++)
        {
            var kind=(TraversalFloor)floor;int count=counts[tier],gap=gaps[tier];float amp=amps[tier];
            for(int i=0;i<=count;i++)
            {
                float ax,ay,bx,by;FloorTraversalLayout.Anchor(kind,i,count,gap,out ax,out ay);FloorTraversalLayout.Anchor(kind,i+1,count,gap,out bx,out by);
                for(int phaseA=-10;phaseA<=10;phaseA++)for(int phaseB=-10;phaseB<=10;phaseB++)
                {
                    float a=i>0&&i<=count&&i%2==0?amp*phaseA/10:0,b=i+1<=count&&(i+1)%2==0?amp*phaseB/10:0;
                    float dx=bx-ax,dy=by-ay;if(kind==TraversalFloor.Residence)dy+=b-a;else dx+=b-a;
                    Check(GrappleRules.Reachable(dx,dy,14,2),"Every floor/tier/moving phase retains two tiles of grapple range reserve");
                    Check(Math.Abs(dx)<13,"Next anchors fit the planned4:3 forward framing");
                    float top=Math.Max(ay+(kind==TraversalFloor.Residence?a:0),by+(kind==TraversalFloor.Residence?b:0));
                    Check(top+.9f+.03f<14,"The complete capsule fits under each floor's closed roof");
                }
            }
            if(kind==TraversalFloor.Administration)
            {float gate=FloorTraversalLayout.GateX(count,gap);Check(gate-5>10&&gate+1<TraversalLayout.ExitX(count,gap)-2,"The wait island and shutter stay within the test corridor");float stop=gate-.375f-.36f-.03f;Check(stop-.36f>=FloorTraversalLayout.WaitLeft(gate)&&stop+.36f<=FloorTraversalLayout.WaitRight(gate),"A complete stopped capsule is supported directly before the closed shutter");Check(!FloorTraversalLayout.ShutterOverlap(gate,13.07f,gate),"The upper ceiling route remains above the security shutter");}
        }
        for(int floor=0;floor<5;floor++)for(int stage=0;stage<5;stage++)
        {float ratio=FloorTraversalLayout.Flow[floor,stage]/(float)(FloorTraversalLayout.Flow[floor,stage]+FloorTraversalLayout.Challenge(floor,stage));Check(ratio>=.7f&&ratio<=.8f,"Each of25 planned sections retains the requested flow/challenge rhythm");Check(FloorTraversalLayout.Rest[floor,stage]>=16,"Every required challenge has a separate breathing pocket");}
        foreach(int hz in new[]{30,50,60,120,240})
        {
            Check(FloorTraversalLayout.Open(0,3,1.4f),"Retry starts with an open timing window");
            Check(FloorTraversalLayout.Open(1.39f,3,1.4f)&&!FloorTraversalLayout.Open(1.41f,3,1.4f),"Shutter boundaries have explicit opening and closing phases");
            for(int i=0;i<hz*10;i++){float t=i/(float)hz;Check(FloorTraversalLayout.Open(t,3,1.4f)==FloorTraversalLayout.Open(t+30,3,1.4f)||Math.Abs(t%3-1.4f)<.001f||t%3<.001f,"Timing repeats without frame-count dependence away from float boundaries");}
        }
        Check(FloorTraversalLayout.ShutterOverlap(0,2,0)&&!FloorTraversalLayout.ShutterOverlap(2,2,0),"A shutter can defer closing only while the capsule intersects its volume");
        Check(FloorTraversalLayout.ScanOverlap(0,1.1f,0,1.4f)&&!FloorTraversalLayout.ScanOverlap(3,1.1f,0,1.4f),"Searchlight exposure uses the visible beam width plus capsule radius");
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})Check(!FloorTraversalLayout.Open(bad,3,1),"Invalid timing does not activate an anchor");
        Console.WriteLine("PASS: "+checks+" five-floor link/capsule/camera,25-stage rhythm and mechanism timing rules; native physics not measured");
    }
}
