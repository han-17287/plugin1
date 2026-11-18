using System;
using Plugin.PlayerCore;
class PlayerCoreChecks
{
    static int checks;
    static void Check(bool b,string message){if(!b)throw new Exception(message);checks++;}
    static void Main()
    {
        foreach(int renderHz in new[]{30,60,144,240})foreach(int fixedHz in new[]{30,50,60,120})
        {
            var buffer=new PlayerInputBuffer();double press=10.0+1.0/renderHz,consume=Math.Ceiling(press*fixedHz)/fixedHz;
            buffer.Put(PlayerCommand.Jump,press,.15);
            Check(buffer.Take(PlayerCommand.Jump,consume),"A render-frame press survives until the next physics tick");
            Check(!buffer.Take(PlayerCommand.Jump,consume),"A buffered action runs once even with multiple physics ticks");
            buffer.Put(PlayerCommand.Dash,press,.15);Check(!buffer.Take(PlayerCommand.Dash,press+.151),"An expired dash never launches late");
            buffer.Put(PlayerCommand.Primary,press,.15);buffer.Remove(PlayerCommand.Primary);Check(!buffer.Take(PlayerCommand.Primary,press),"Mode changes can discard the old mode's primary command");
            foreach(PlayerCommand command in Enum.GetValues(typeof(PlayerCommand)))if(command!=PlayerCommand.Count)buffer.Put(command,press,.15);
            buffer.Clear();foreach(PlayerCommand command in Enum.GetValues(typeof(PlayerCommand)))if(command!=PlayerCommand.Count)Check(!buffer.Has(command,press),"Focus loss/death clears every buffered command");
        }
        foreach(float noise in new[]{0,.001f,-.01f,.099f})
        {var gate=new ScrollModeGate();Check(gate.Read(noise,1,.1f,.16f)==0,"Wheel noise cannot switch mode");}
        foreach(int hz in new[]{30,60,144,240})
        {
            var gate=new ScrollModeGate();Check(gate.Read(120,1,.1f,.16f)==1,"The first positive wheel impulse selects attack");
            for(int i=1;i<=hz;i++)Check(gate.Read(120,1+i/(double)hz,.1f,.16f)==0,"A held/continuous wheel packet cannot repeatedly change modes");
            Check(gate.Read(0,2.1,.1f,.16f)==0&&gate.Read(-120,2.2,.1f,.16f)==-1,"Returning to neutral rearms a later deliberate wheel movement");
            gate.Reset();Check(gate.Read(-1,0,.1f,.16f)==-1,"Reset does not retain a previous session's cooldown");
        }
        var scroll=new ScrollModeGate();Check(scroll.Read(1,1,.1f,.16f)==1,"Initial wheel event accepted");scroll.Read(0,1.01,.1f,.16f);
        Check(scroll.Read(-1,1.02,.1f,.16f)==0,"Opposite chatter inside the cooldown is suppressed");scroll.Read(0,1.2,.1f,.16f);Check(scroll.Read(-1,1.21,.1f,.16f)==-1,"A later intentional reverse wheel event is accepted");
        foreach(double invalid in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity})
        {var buffer=new PlayerInputBuffer();buffer.Put(PlayerCommand.Jump,invalid,.1);Check(!buffer.Has(PlayerCommand.Jump,1),"Invalid input clocks cannot create a pending action");}
        Console.WriteLine("PASS: "+checks+" player input buffer and wheel cadence checks; Unity movement/input/physics not executed");
    }
}
