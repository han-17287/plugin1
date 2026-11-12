using System;
namespace Plugin.AncientCivilization
{
    public enum AncientLivingAttack { Bullets,Leap,Rocket,Missiles,Cannon,Grab,DoubleRocket,XBlast,Suction,CrossMissiles }
    public enum AncientLivingHit { Wire,Blade,Throw,Laser }
    // Shared by the runtime and portable checks. Times are gameplay seconds, not wall-clock deadlines.
    public sealed class AncientLivingRules
    {
        public const float SecondPhase=.68f,ThirdPhase=.34f,RocketCharge=3,OrbitDuration=.62f;
        public int Phase {get;private set;}=1;
        public int Barrier {get;private set;}
        int turn;bool extraPair;
        public bool AdvancePhase(float health)
        {
            if(float.IsNaN(health))return false;
            if(Phase==1&&health<=SecondPhase+.00001f||Phase==2&&health<=ThirdPhase+.00001f)
            {Phase++;turn=0;Barrier=0;extraPair=false;return true;}return false;
        }
        public float FilterDamage(float damage,float hp,float maximum,bool vulnerable)
        {
            if(!vulnerable||Barrier>0||float.IsNaN(damage)||float.IsInfinity(damage)||damage<=0)return 0;
            float floor=Phase==1?maximum*SecondPhase:Phase==2?maximum*ThirdPhase:0;
            return Math.Max(0,Math.Min(damage,hp-floor));
        }
        public void RaiseBarrier(){Barrier=70;}
        public bool HitBarrier(AncientLivingHit kind)
        {
            if(Barrier<=0)return false;
            int hit=kind==AncientLivingHit.Laser?70:kind==AncientLivingHit.Throw?7:kind==AncientLivingHit.Blade?10:0;
            Barrier=Math.Max(0,Barrier-hit);return Barrier==0;
        }
        public AncientLivingAttack Next()
        {
            if(Phase==1){int n=turn++%4;return n==0?AncientLivingAttack.Bullets:n==1?AncientLivingAttack.Leap:n==2?AncientLivingAttack.Rocket:AncientLivingAttack.Missiles;}
            if(Phase==2){int n=turn++%3;return n==0?AncientLivingAttack.Cannon:n==1?AncientLivingAttack.Bullets:AncientLivingAttack.Grab;}
            if(extraPair)
            {
                if(turn==0){turn=1;return AncientLivingAttack.DoubleRocket;}
                if(turn==1){turn=3;extraPair=false;return AncientLivingAttack.XBlast;}
            }
            int next=turn;turn=(turn+1)%4;
            return next==0?AncientLivingAttack.DoubleRocket:next==1?AncientLivingAttack.XBlast:next==2?AncientLivingAttack.Suction:AncientLivingAttack.CrossMissiles;
        }
        public void ResolveSuction(bool overloaded){extraPair=!overloaded;turn=overloaded?3:0;}
        public static int BulletCount(int phase){return phase>=2?20:10;}
        public static bool InSpotlight(float dx,float dy,float directionX,float directionY,float range,float cosine)
        {float d=dx*dx+dy*dy;return d>.0001f&&d<=range*range&&(dx*directionX+dy*directionY)/Math.Sqrt(d)>=cosine;}
        public static bool BeamHit(float sx,float sy,float ex,float ey,float px,float py,float radius)
        {float dx=ex-sx,dy=ey-sy,q=dx*dx+dy*dy,t=q<.00001f?0:Math.Max(0,Math.Min(1,((px-sx)*dx+(py-sy)*dy)/q));float x=sx+dx*t-px,y=sy+dy*t-py;return x*x+y*y<=radius*radius;}
    }
}
