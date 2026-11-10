using System;
using Plugin.AncientCivilization;

class PulseChecks
{
    sealed class EqualTarget
    {
        public override bool Equals(object other){return other is EqualTarget;}
        public override int GetHashCode(){return 1;}
    }
    static int checks;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    static void Main()
    {
        var pulse=new AncientLaserPulse<object>(4);var actor=new object();var lateActor=new object();
        Check(!pulse.IsActive(0)&&!pulse.TryHit(actor,0),"An idle pulse cannot damage an actor");
        Check(pulse.Begin(10,AncientBeamRules.Duration,3,4,80),"A released shot starts immediately");
        int firstId=pulse.Id;
        Check(firstId>0&&Near(pulse.DirectionX,.6f)&&Near(pulse.DirectionY,.8f)&&pulse.Energy==80,"Release locks normalized direction and captured energy");
        Check(pulse.TryHit(actor,10)&&!pulse.TryHit(actor,10)&&!pulse.TryHit(actor,10.1f),"Release and render/physics updates cannot deal duplicate damage");
        Check(pulse.TryHit(lateActor,10.2f),"An actor entering the active beam after release is hit once");
        Check(!pulse.TryHit(null,10.2f),"Missing or removed actors never occupy a ledger entry");
        Check(pulse.IsActive(10.3f)&&!pulse.IsActive(10+AncientBeamRules.Duration),"The pulse remains active until its exact release-relative expiry");
        Check(!pulse.TryHit(new object(),10.5f),"Expired light cannot damage a late arrival");
        Check(pulse.Begin(20,AncientBeamRules.Duration,-2,0,120),"The same emitter can release another pulse");
        Check(pulse.Id!=firstId&&pulse.Energy==100&&pulse.DirectionX==-1,"A new pulse owns a new identity and bounded charge");
        Check(pulse.TryHit(actor,20),"A surviving or pooled actor can be hit again by the next pulse");
        pulse.Cancel();
        Check(!pulse.IsActive(20)&&!pulse.TryHit(lateActor,20),"Mode changes, damage, dialogue and focus cancellation immediately disarm the pulse");
        Check(pulse.Begin(21,.38f,1,0,0)&&pulse.TryHit(actor,21),"Cancellation clears references without disabling future shots");
        // No target eviction: crowded scenes fail closed instead of repeatedly damaging an old entry.
        Check(pulse.TryHit(new object(),21)&&pulse.TryHit(new object(),21)&&pulse.TryHit(new object(),21),"The bounded ledger admits its full capacity");
        Check(!pulse.TryHit(new object(),21)&&!pulse.TryHit(actor,21),"Capacity pressure never recycles previous hits within one pulse");
        var firstEqual=new EqualTarget();var secondEqual=new EqualTarget();
        pulse.Begin(22,.38f,1,0,50);
        Check(firstEqual.Equals(secondEqual)&&pulse.TryHit(firstEqual,22)&&pulse.TryHit(secondEqual,22),"Different actor references remain separate even if equality and hash values match");
        foreach(int fps in new[]{20,30,60,120,144,240})
        {
            pulse.Begin(0,AncientBeamRules.Duration,1,0,50);int damage=0,lateDamage=0;
            for(int frame=0;frame<=fps;frame++)
            {
                float now=(float)frame/fps;
                // Simulate both Update and a physics callback at the same instant.
                if(pulse.TryHit(actor,now))damage+=110;
                if(pulse.TryHit(actor,now))damage+=110;
                if(now>=.15f&&pulse.TryHit(lateActor,now))lateDamage+=110;
            }
            Check(damage==110&&lateDamage==110,"Damage is independent of render and physics callback frequency at "+fps+"fps");
        }
        pulse.Begin(5,.38f,1,0,50);pulse.TryHit(actor,5);
        for(int i=0;i<100;i++)Check(pulse.IsActive(5.2f)&&!pulse.TryHit(actor,5.2f),"A paused game clock freezes the pulse without repeated damage");
        Check(pulse.IsActive(5.3f)&&!pulse.IsActive(5.38f),"Unpausing resumes the remaining pulse lifetime");
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            Check(!pulse.Begin(bad,.38f,1,0,50),"Invalid release times fail closed");
            Check(!pulse.Begin(0,bad,1,0,50),"Invalid durations fail closed");
            Check(!pulse.Begin(0,.38f,bad,0,50),"Invalid directions fail closed");
            Check(!pulse.Begin(0,.38f,1,0,bad),"Invalid captured energy fails closed");
        }
        Check(!pulse.Begin(0,0,1,0,50)&&!pulse.Begin(0,.38f,0,0,50),"Zero lifetime or direction cannot start an invisible attack");
        pulse.Begin(5,.38f,1,0,50);
        Check(!pulse.IsActive(4)&&!pulse.TryHit(actor,5),"A rewound/reset game clock cannot revive an old scene's pulse");
        Console.WriteLine("PASS: "+checks+" pulse lifetime, locked release, bounded hit-ledger and frame-independence checks");
    }
}
