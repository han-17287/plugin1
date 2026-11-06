using System;
namespace Plugin.AncientCivilization
{
    public static class AncientFrameRules
    {
        // Active-play stalls remain in the measurement; filtering >1 second would hide freezes.
        public static bool ValidSample(float seconds)
        {return seconds>0&&!float.IsNaN(seconds)&&!float.IsInfinity(seconds);}
        public static int P95Index(int count)
        {return count<=0?-1:(int)Math.Ceiling(count*.95)-1;}
    }
}
