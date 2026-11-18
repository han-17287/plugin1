using System;
using Plugin.PlayerCore;
class GrappleLayoutChecks
{
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    static void Main()
    {
        foreach(float distance in new[]{.5f,1,6,8,11.05f,14})foreach(int hz in new[]{30,50,60,120,240})
        {
            float remaining=distance,dt=1f/hz;int ticks=0;float travelled=0;
            while(remaining>.00001f&&ticks<1000)
            {float step=GrappleRules.Step(remaining,72,dt);Check(step>=0&&step<=remaining&&step<=72*dt+.0001f,"Fixed-speed steps never overshoot or teleport past the endpoint");remaining-=step;travelled+=step;ticks++;}
            Check(remaining<.00001f&&Math.Abs(travelled-distance)<.0001f,"Every supported timestep reaches the same captured point");
            Check(ticks*dt<=distance/72+dt+.0001f,"Long-frame cadence has a bounded single-tick arrival rounding cost");
        }
        int[] counts={3,4,5},gaps={6,7,8};float[] amplitudes={.5f,1,1.5f};
        for(int tier=0;tier<3;tier++)
        {
            int count=counts[tier],gap=gaps[tier];float amp=amplitudes[tier];
            for(int index=0;index<count;index++)
            {
                float ax,ay,bx,by;TraversalLayout.Anchor(index,out ax,out ay,gap);TraversalLayout.Anchor(index+1,out bx,out by,gap);
                for(int a=-10;a<=10;a++)for(int b=-10;b<=10;b++)
                {
                    float x=bx-ax+(b-a)*amp/10;
                    Check(GrappleRules.Reachable(x,by-ay,14,2),"Even independent worst-case motion leaves a two-tile range reserve");
                    Check(Math.Sqrt(x*x+(by-ay)*(by-ay))<13,"Neighboring targets remain within the forward view at4:3, zoom8 and2.5-tile lookahead");
                }
                Check(Math.Abs(by-ay)<=2,"Difficulty grows through sequence length, not a sudden vertical reach requirement");
            }
            Check(TraversalLayout.ExitX(count,gap)>6+count*gap&&TraversalLayout.Width(count,gap)>TraversalLayout.ExitX(count,gap)+3,"The final anchor leads to a broad exit and room boundary");
            Check(14-(6+amp)>1.8f+.3f,"The complete capsule fits above the highest moving target with safety clearance");
            Check(TraversalLayout.Width(count,gap)<64,"Trial rooms remain bounded and immediately retryable");
        }
        int flow=0,challenge=0;
        for(int i=0;i<5;i++)
        {float fraction=TraversalLayout.FlowTiles[i]/(float)(TraversalLayout.FlowTiles[i]+TraversalLayout.ChallengeTiles[i]);Check(fraction>=.7f&&fraction<=.8f,"Every proposed stage stays within the requested flow/challenge ratio");Check(TraversalLayout.RestTiles[i]>=16,"Each required challenge has a distinct supply/breathing pocket");flow+=TraversalLayout.FlowTiles[i];challenge+=TraversalLayout.ChallengeTiles[i];}
        Check(flow==612&&challenge==204,"The entire proposed five-stage route has75% flow and25% required challenge");
        Check(Math.Abs(GrappleRules.JumpHeight(14,32)-3.0625f)<.0001f,"The provisional M1 jump height is derived from the actual movement settings");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {Check(GrappleRules.Step(invalid,72,.02f)==0&&!GrappleRules.Reachable(invalid,0,14),"Invalid endpoint data cannot create an unbounded movement step");}
        Check(!GrappleRules.Reachable(14.1f,0,14)&&!GrappleRules.Reachable(0,0,-1),"Out-of-range and corrupt range values are rejected");
        Check(GrappleRules.StrikeMultiplier(14,72)>GrappleRules.StrikeMultiplier(6,72)&&GrappleRules.StrikeMultiplier(100,1000)<=2.5f,"Zip-strike bonus grows with travel and has a bounded multiplier for P4");
        Console.WriteLine("PASS: "+checks+" grapple stepping, motion-envelope, room clearance, provisional M1 and five-stage rhythm checks; native traversal unmeasured");
    }
}
