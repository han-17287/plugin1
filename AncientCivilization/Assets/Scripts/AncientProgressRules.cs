namespace Plugin.AncientCivilization
{
    public static class AncientProgressRules
    {
        public const int CacheCount=10;
        public const float CheckpointRecovery=20;
        public static float RecoverHealth(float current,float maximum,float amount)
        {
            if(!Finite(current)||!Finite(maximum)||!Finite(amount)||current<=0||maximum<=current||amount<=0)return current;
            return System.Math.Min(maximum,current+System.Math.Min(amount,maximum-current));
        }
        public static float RecoverAtCheckpoint(float current,float maximum,int previousCheckpoint,int newCheckpoint)
        {
            if(previousCheckpoint<0||previousCheckpoint>2||newCheckpoint<1||newCheckpoint>2||newCheckpoint<=previousCheckpoint)return current;
            return RecoverHealth(current,maximum,CheckpointRecovery);
        }
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
        public static bool HasCache(int mask,int id){return id>=0&&id<CacheCount&&(mask&(1<<id))!=0;}
        public static int ClaimCache(int mask,int id){return id<0||id>=CacheCount?mask:mask|(1<<id);}
        public static int CachesOnFloor(int mask,int floor)
        {
            if(floor<0||floor>4)return 0;
            return (HasCache(mask,floor*2)?1:0)+(HasCache(mask,floor*2+1)?1:0);
        }
        public static bool KnowsEvidence(int evidence,int stage,int cityCleared)
        {return KnowsEvidence(evidence,stage,cityCleared,0);}
        public static bool KnowsEvidence(int evidence,int stage,int cityCleared,int cases)
        {
            if(evidence>=6&&evidence<=8)return (cases&(1<<(evidence-6)))!=0;
            if(evidence==0)return stage>=1;if(evidence==1)return stage>=5;if(evidence==2)return stage>=8;
            if(evidence==3)return stage>=9;if(evidence==4)return (cityCleared&(1<<3))!=0;
            if(evidence==5)return (cityCleared&(1<<4))!=0;return false;
        }
    }
}
