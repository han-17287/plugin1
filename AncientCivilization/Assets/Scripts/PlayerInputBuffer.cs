using System;
namespace Plugin.PlayerCore
{
    public enum PlayerCommand { Jump,Dash,Drop,Primary,Summon,Detach,LaserPress,LaserRelease,Overload,Count }
    // 프레임 입력을 물리 틱까지 보존한다. 한 번 소비한 명령은 다시 실행하지 않는다.
    public sealed class PlayerInputBuffer
    {
        readonly double[] deadlines=new double[(int)PlayerCommand.Count];
        public PlayerInputBuffer(){Clear();}
        public void Put(PlayerCommand command,double now,double lifetime)
        {if(Valid(now)&&Valid(lifetime)&&lifetime>=0)deadlines[(int)command]=now+lifetime;}
        public bool Has(PlayerCommand command,double now){return Valid(now)&&now<=deadlines[(int)command];}
        public bool Take(PlayerCommand command,double now)
        {if(!Has(command,now))return false;deadlines[(int)command]=double.NegativeInfinity;return true;}
        public void Remove(PlayerCommand command){deadlines[(int)command]=double.NegativeInfinity;}
        public void Clear(){for(int i=0;i<deadlines.Length;i++)deadlines[i]=double.NegativeInfinity;}
        static bool Valid(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
    }
}
