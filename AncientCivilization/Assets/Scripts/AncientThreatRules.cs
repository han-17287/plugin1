namespace Plugin.AncientCivilization
{
    // Ordinary enemies must expose their next tell, and earned stagger is safe to punish.
    public static class AncientThreatRules
    {
        public const float HorizontalMargin=.06f,VerticalMargin=.12f;
        public static bool BodyContactAllowed(bool boss,bool recovering,bool staggered)
        {return !recovering&&(boss||!staggered);}
        public static bool CanStartAttack(bool boss,bool hasCamera,float viewportX,float viewportY,float depth)
        {
            if(boss)return true;
            return hasCamera&&Finite(viewportX)&&Finite(viewportY)&&Finite(depth)&&depth>0&&
                viewportX>=-HorizontalMargin&&viewportX<=1+HorizontalMargin&&
                viewportY>=-VerticalMargin&&viewportY<=1+VerticalMargin;
        }
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}
