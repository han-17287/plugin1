namespace Plugin.AncientCivilization
{
    // Pure rules shared by gameplay and the standalone regression checks.
    public enum LeftReleaseAction { Blade, Cancel, Laser, Empty }
    public static class AncientRules
    {
        public const float BladeDuration=.4f,BladeHitTime=.15f;
        public static int BladeAnimationFrame(float elapsed)
        {
            if(elapsed<.035f)return 0;if(elapsed<.09f)return 1;if(elapsed<.15f)return 2;
            if(elapsed<.185f)return 3;if(elapsed<.22f)return 4;if(elapsed<.275f)return 5;
            if(elapsed<.345f)return 6;return 7;
        }
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
            double radius=.14*System.Math.Max(0,System.Math.Min(1,t*length/.2))*System.Math.Max(0,System.Math.Min(1,(1-t)*length/.2));
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
        public static float CraneOffset(float time,int index)
        {return (float)System.Math.Sin(time*.9+index*1.7)*2.7f;}
        public static float BladeReboundSpeed(float current)
        {return System.Math.Max(current,System.Math.Min(9,System.Math.Max(6,current+4)));}
        public static bool WireImpactOnStep(float dx,float dy,float stepX,float stepY,bool blocked)
        {
            if(blocked)return false;
            float square=stepX*stepX+stepY*stepY;
            float t=square>.0001f?System.Math.Max(0,System.Math.Min(1,(dx*stepX+dy*stepY)/square)):0;
            float x=dx-stepX*t,y=dy-stepY*t;return x*x+y*y<1.1f*1.1f;
        }
        public static float WireHitDamage(bool boss){return boss?3:12;}
        public static float WireImpactDamage(bool boss){return boss?4:18;}
        public static float WirePushDistance(bool boss){return boss?.18f:.65f;}
        public static bool IgnoreWireContact(bool sameTarget,bool pulling,float protectedUntil,float now)
        {return sameTarget&&(pulling||now<protectedUntil);}
        public static bool CanSelectLiftFloor(int stage,int points,int currentFloor,int targetFloor)
        {
            if(currentFloor<0||currentFloor>1||targetFloor<0||targetFloor>1||currentFloor==targetFloor)return false;
            return targetFloor==0||stage>=8&&points>=1000;
        }
        public static bool CanEnterUpperCity(int stage,int points,bool alreadyUpper)
        {return stage>=8&&points>=1000&&!alreadyUpper;}
    }
}
