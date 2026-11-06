using System;
using Plugin.AncientCivilization;
class MovementChecks
{
    static int count;
    static void Check(bool value,string text){if(!value)throw new Exception(text);count++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    static void Main()
    {
        // A tap can begin and end before the landing. No render Update runs on the landing tick.
        foreach(int physicsHz in new[]{30,50,60,120,240})foreach(bool held in new[]{false,true})
        {
            float step=1f/physicsHz,press=2,deadline=press+AncientMovementRules.BufferSeconds;
            float groundedAt=-100,land=press+step*2;
            Check(AncientMovementRules.ChooseJump(press+step,deadline,groundedAt,false,false)==AncientJumpKind.None,"A buffered press waits for the landing without an unlocked air jump");
            bool stable=AncientMovementRules.StableGround(true,0,-11,0,1);
            if(stable)groundedAt=land;
            AncientJumpKind jump=AncientMovementRules.ChooseJump(land,deadline,groundedAt,false,false);
            Check(jump==AncientJumpKind.Ground,"The physics landing consumes a valid buffer without waiting for a render frame");
            float velocity=AncientMovementRules.LaunchSpeed(jump,held);
            Check(Near(velocity,held?14:7.7f),"A release remembered before landing produces a short hop exactly once");
            groundedAt=-100;deadline=-10;
            Check(!AncientMovementRules.StableGround(true,0,velocity,0,1),"The next upward step cannot refresh ground eligibility from a lingering foot cast");
            Check(AncientMovementRules.ChooseJump(land+step,deadline,groundedAt,true,false)==AncientJumpKind.None,"A consumed press cannot also consume the air jump");
        }
        float groundTime=8,validBuffer=9;
        Check(AncientMovementRules.ChooseJump(groundTime+.119f,validBuffer,groundTime,false,false)==AncientJumpKind.Ground,"Coyote jump remains valid immediately before its deadline");
        Check(AncientMovementRules.ChooseJump(groundTime+.121f,validBuffer,groundTime,false,false)==AncientJumpKind.None,"Coyote jump expires after its deadline");
        Check(AncientMovementRules.ChooseJump(0,1,-AncientMovementRules.CoyoteSeconds,false,false)==AncientJumpKind.None,"The exact coyote boundary is exclusive");
        Check(AncientMovementRules.ChooseJump(4,4,4,false,false)==AncientJumpKind.None,"The exact press-buffer deadline is exclusive even when grounded");
        Check(AncientMovementRules.ChooseJump(4.001f,4,4,false,false)==AncientJumpKind.None,"An expired press cannot launch on a late landing");
        Check(AncientMovementRules.ChooseJump(3.999f,4,3.999f,false,false)==AncientJumpKind.Ground,"The last moment inside the press buffer still launches");
        Check(AncientMovementRules.ChooseJump(4,5,4.1f,false,false)==AncientJumpKind.None,"A future render timestamp cannot grant a coyote jump in an earlier physics tick");
        Check(AncientMovementRules.ChooseJump(10,11,-100,false,false)==AncientJumpKind.None,"Air jump requires its upgrade");
        Check(AncientMovementRules.ChooseJump(10,11,-100,true,false)==AncientJumpKind.Air,"The unlocked air jump remains available away from the ground");
        Check(AncientMovementRules.ChooseJump(10,11,-100,true,true)==AncientJumpKind.None,"An already used air jump cannot be repeated");
        Check(AncientMovementRules.ChooseJump(10,11,10,true,false)==AncientJumpKind.Ground,"Landing uses the ground jump before spending the available air jump");
        Check(Near(AncientMovementRules.LaunchSpeed(AncientJumpKind.Air,false),6.6f),"A released air-jump buffer respects the same short-hop intent");
        Check(Near(AncientMovementRules.LaunchSpeed(AncientJumpKind.Air,true),12),"A held air jump preserves its existing launch speed");
        Check(Near(AncientMovementRules.ReleaseSpeed(4),4)&&Near(AncientMovementRules.ReleaseSpeed(-20),-20),"Release never slows a falling body or an already small upward speed");
        Check(Near(AncientMovementRules.ReleaseSpeed(14),7.7f),"Releasing during ascent preserves the existing jump-cut ratio");
        Check(AncientMovementRules.StableGround(true,0,5-5,0,1),"A rising platform refreshes ground eligibility when its rider moves with it");
        Check(AncientMovementRules.StableGround(true,0,-5-(-5),0,1),"A descending platform still counts as stable support");
        Check(!AncientMovementRules.StableGround(true,0,9-5,0,1),"Jumping away from a rising platform cannot immediately rearm the ground jump");
        Check(!AncientMovementRules.StableGround(false,0,0,0,1),"Relative speed alone cannot create a ground contact");
        Check(AncientMovementRules.StableGround(true,0,.5f,0,1)&&!AncientMovementRules.StableGround(true,0,.501f,0,1),"Small support jitter is tolerated without treating an upward launch as a landing");
        foreach(float speed in new[]{5,9,17})foreach(float slope in new[]{-.1f,.1f,-.5f,.5f})
        {
            float vx=slope<0?-speed:speed,vy=Math.Abs(speed*slope);
            Check(AncientMovementRules.StableGround(true,vx,vy,-slope,1),"Walking, running and riding uphill remain supported on road ramps");
            Check(!AncientMovementRules.StableGround(true,vx,vy+14,-slope,1),"An upward jump separates from a ramp even while the foot cast still overlaps it");
        }
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            Check(!AncientMovementRules.StableGround(true,invalid,0,0,1)&&!AncientMovementRules.StableGround(true,0,invalid,0,1),"Invalid support velocities never establish a landing");
            Check(!AncientMovementRules.StableGround(true,0,0,invalid,1)&&!AncientMovementRules.StableGround(true,0,0,0,invalid),"Invalid contact normals never establish a landing");
            Check(AncientMovementRules.ChooseJump(invalid,11,10,true,false)==AncientJumpKind.None,"Invalid time never launches a buffered jump");
            Check(AncientMovementRules.ChooseJump(10,invalid,10,true,false)==AncientJumpKind.None,"Invalid buffer deadline never launches a jump");
        }
        Console.WriteLine("Movement checks passed: "+count);
    }
}
