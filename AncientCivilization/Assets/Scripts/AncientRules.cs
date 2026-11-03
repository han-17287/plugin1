namespace Plugin.AncientCivilization
{
    // Pure rules shared by gameplay and the standalone regression checks.
    public enum LeftReleaseAction { Blade, Cancel, Laser, Empty }
    public static class AncientRules
    {
        public const float TapThreshold=.22f,ReadyDelay=.85f;
        public static LeftReleaseAction ResolveLeftRelease(float held,float energy)
        {
            if(held<0)return LeftReleaseAction.Cancel;
            if(held<TapThreshold)return LeftReleaseAction.Blade;
            if(held<ReadyDelay)return LeftReleaseAction.Cancel;
            return energy>0?LeftReleaseAction.Laser:LeftReleaseAction.Empty;
        }
        public static bool TutorialReady(int stage,int flags)
        {
            int mask=stage==1?7:stage==2?24:stage==3?224:0;
            return mask!=0&&(flags&mask)==mask;
        }
        public static bool CanStartMiniBoss(int stage,bool truckStopped,int cores,float x)
        {return stage==5&&truckStopped&&cores>=3&&x>98;}
        public static int PointsAfterArrest(int current,int stage)
        {return stage==6?current+1000:current;}
        public static bool CanEnterUpperCity(int stage,int points,bool alreadyUpper)
        {return stage>=8&&points>=1000&&!alreadyUpper;}
    }
}
