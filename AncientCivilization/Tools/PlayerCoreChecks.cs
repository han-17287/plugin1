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
        foreach (var command in new[] {PlayerCommand.Summon, PlayerCommand.Detach, PlayerCommand.Overload})
        {
            var buffer = new PlayerInputBuffer();
            buffer.Put(command, 1, .15);
            Check(buffer.Take(command, 1.14) && !buffer.Take(command, 1.14), "Q/E/R are single independent edges");
            Check(!buffer.Has(PlayerCommand.Primary, 1.14), "Loadout actions cannot inject a primary attack");
        }
        foreach(double invalid in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity})
        {var buffer=new PlayerInputBuffer();buffer.Put(PlayerCommand.Jump,invalid,.1);Check(!buffer.Has(PlayerCommand.Jump,1),"Invalid input clocks cannot create a pending action");}
        Console.WriteLine("PASS: "+checks+" player input buffer and loadout key checks; Unity movement/input/physics not executed");
    }
}
