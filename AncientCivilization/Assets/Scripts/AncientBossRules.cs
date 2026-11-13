using System;
namespace Plugin.AncientCivilization
{
    public enum AncientBossMove { Slam,ArcCrate,Rush,SpreadThrow,StampBurst,FanVolley,CoreLob,FloorRoll,CeilingDebris,SniperBurst,BankShot }
    public static class AncientBossRules
    {
        public static AncientBossMove Choose(int floor,int turn,bool phaseTwo,float distance)
        {
            int beat=Math.Abs(turn%3);
            if(floor==0)return beat==0&&distance<7?AncientBossMove.Slam:AncientBossMove.ArcCrate;
            if(floor==1)return beat==0?AncientBossMove.Rush:beat==1?AncientBossMove.ArcCrate:AncientBossMove.SpreadThrow;
            if(floor==2)return beat==0?AncientBossMove.StampBurst:beat==1&&distance<6?AncientBossMove.Slam:AncientBossMove.FanVolley;
            if(floor==3)return beat==0?AncientBossMove.CoreLob:beat==1?AncientBossMove.FloorRoll:AncientBossMove.CeilingDebris;
            return turn%2==0?AncientBossMove.SniperBurst:AncientBossMove.BankShot;
        }
        public static bool Physical(AncientBossMove move){return move==AncientBossMove.Slam||move==AncientBossMove.Rush;}
        public static float Windup(AncientBossMove move)
        {return move==AncientBossMove.BankShot||move==AncientBossMove.SniperBurst?1.15f:move==AncientBossMove.CeilingDebris?1.1f:move==AncientBossMove.Rush?.85f:.75f;}
        public static float Opening(AncientBossMove move)
        {return move==AncientBossMove.BankShot||move==AncientBossMove.SniperBurst?1.65f:move==AncientBossMove.CoreLob||move==AncientBossMove.CeilingDebris?1.5f:1.25f;}
        public static int Count(AncientBossMove move,bool phaseTwo)
        {
            if(move==AncientBossMove.StampBurst)return phaseTwo?4:3;
            if(move==AncientBossMove.FanVolley||move==AncientBossMove.SpreadThrow)return phaseTwo?7:5;
            if(move==AncientBossMove.CoreLob)return phaseTwo?3:2;
            if(move==AncientBossMove.SniperBurst||move==AncientBossMove.CeilingDebris)return 3;
            if(move==AncientBossMove.FloorRoll)return phaseTwo?4:2;
            return 1;
        }
        public static float FloorRollHeight(int projectileIndex){return .5f+Math.Max(0,projectileIndex)/2*.7f;}
        public static float FloorRollVelocity(int projectileIndex){return projectileIndex%2==0?-8:8;}
        public static float BankHeight(float originX,float originY,float targetX,float targetY,float wallX)
        {
            float mirror=2*wallX-targetX,denominator=mirror-originX;
            return Math.Abs(denominator)<.001f?originY:originY+(targetY-originY)*(wallX-originX)/denominator;
        }
    }
}
