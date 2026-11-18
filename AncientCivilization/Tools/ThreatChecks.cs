using System;
using Plugin.AncientCivilization;
class ThreatChecks
{
    static int count;
    static void Check(bool value,string message){if(!value)throw new Exception(message);count++;}
    static void Main()
    {
        Check(AncientThreatRules.BodyContactAllowed(false,false,false),"A normal active enemy still has body contact damage");
        Check(!AncientThreatRules.BodyContactAllowed(false,false,true),"The stagger earned by wire impact or a finisher is safe to punish");
        foreach(bool stagger in new[]{false,true})
        {
            Check(AncientThreatRules.BodyContactAllowed(true,false,stagger),"Boss contact does not inherit ordinary-enemy stagger protection");
            Check(!AncientThreatRules.BodyContactAllowed(true,true,stagger),"The existing boss recovery opening remains safe");
            Check(!AncientThreatRules.BodyContactAllowed(false,true,stagger),"A recovery opening never becomes contact damage");
        }
        foreach(float x in new[]{-.06f,0f,.5f,1f,1.06f})foreach(float y in new[]{-.12f,0f,.5f,1f,1.12f})
            Check(AncientThreatRules.CanStartAttack(false,true,x,y,10),"Visible actors and the small reveal margin can begin their tells");
        foreach(float x in new[]{-.061f,-.5f,1.061f,1.5f})
            Check(!AncientThreatRules.CanStartAttack(false,true,x,.5f,10),"Unseen actors cannot start a new ordinary attack");
        foreach(float y in new[]{-.121f,-.5f,1.121f,1.5f})
            Check(!AncientThreatRules.CanStartAttack(false,true,.5f,y,10),"Offscreen vertical threats cannot start a new ordinary attack");
        foreach(float depth in new[]{-10f,-.01f,0f})
            Check(!AncientThreatRules.CanStartAttack(false,true,.5f,.5f,depth),"Actors behind the camera cannot attack from an invisible tell");
        Check(!AncientThreatRules.CanStartAttack(false,false,.5f,.5f,10),"Missing camera data does not authorize unseen ordinary attacks");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            Check(!AncientThreatRules.CanStartAttack(false,true,invalid,.5f,10),"Invalid horizontal projection fails closed");
            Check(!AncientThreatRules.CanStartAttack(false,true,.5f,invalid,10),"Invalid vertical projection fails closed");
            Check(!AncientThreatRules.CanStartAttack(false,true,.5f,.5f,invalid),"Invalid depth fails closed");
            Check(AncientThreatRules.CanStartAttack(true,false,invalid,invalid,invalid),"Existing arena boss patterns ignore camera gating");
        }
        // A patrol crossing the screen gets exactly one entry/exit interval, without an
        // arbitrary world-space distance becoming permission to shoot from offscreen.
        int changes=0;bool previous=false;
        for(int step=-100;step<=200;step++)
        {
            float x=step/100f;bool current=AncientThreatRules.CanStartAttack(false,true,x,.5f,10);
            if(current!=previous)changes++;previous=current;
            Check(current==(x>=-AncientThreatRules.HorizontalMargin&&x<=1+AncientThreatRules.HorizontalMargin),"Attack visibility follows a continuous horizontal screen crossing");
        }
        Check(changes==2,"A moving patrol enters and leaves one continuous attack-visibility window");
        Console.WriteLine("PASS: "+count+" combat threat visibility and stagger safety checks");
    }
}
