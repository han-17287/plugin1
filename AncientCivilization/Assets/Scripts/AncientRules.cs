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
        public static bool CanCollectParticles(bool rightHeld,bool focused,bool paused,bool ready,float energy)
        {return rightHeld&&focused&&!paused&&ready&&energy<100;}
        // Radius fades to zero at the emitter and anchor; opposite phases preserve the straight axis.
        public static void WireHelixOffset(float length,float age,float t,int strand,out float normal,out float depth)
        {
            t=System.Math.Max(0,System.Math.Min(1,t));
            double radius=.105*System.Math.Max(0,System.Math.Min(1,t*length/.2))*System.Math.Max(0,System.Math.Min(1,(1-t)*length/.2));
            double angle=t*length*System.Math.PI*2/1.15-age*7+strand*System.Math.PI;
            normal=(float)(System.Math.Sin(angle)*radius);depth=(float)(System.Math.Cos(angle)*radius);
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
        public static bool CanSelectLiftFloor(int stage,int points,int currentFloor,int targetFloor)
        {
            if(currentFloor<0||currentFloor>1||targetFloor<0||targetFloor>1||currentFloor==targetFloor)return false;
            return targetFloor==0||stage>=8&&points>=1000;
        }
        public static bool CanEnterUpperCity(int stage,int points,bool alreadyUpper)
        {return stage>=8&&points>=1000&&!alreadyUpper;}
    }
}
