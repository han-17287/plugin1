using System;
namespace Plugin.AncientCivilization
{
    public static class AncientCombatRules
    {
        public static float Damage(float raw,bool boss,bool vulnerable)
        {return raw<=0?0:raw*(boss?(vulnerable?1.25f:.6f):1);}
        public static float Recovery(int district,int pattern,bool enraged)
        {
            float duration=district==4?1.6f:pattern==0?.9f:district==2?1.05f:1.15f;
            return enraged?Math.Max(.7f,duration*.85f):duration;
        }
        public static bool PerfectBreak(float distanceSquared,float approachDot)
        {return distanceSquared<1.4f*1.4f&&approachDot<-.2f;}
    }
}
