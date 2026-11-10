using System;

namespace Plugin.AncientCivilization
{
    // A pulse owns its release parameters and a bounded, allocation-free hit ledger.
    // Reference identity also works for destroyed Unity objects and reused pool slots.
    public sealed class AncientLaserPulse<T> where T:class
    {
        readonly T[] targets;
        int count,sequence;
        bool running;
        float began;
        public int Id {get;private set;}
        public float Until {get;private set;}
        public float DirectionX {get;private set;}
        public float DirectionY {get;private set;}
        public float Energy {get;private set;}
        public AncientLaserPulse(int capacity=128)
        {if(capacity<1)throw new ArgumentOutOfRangeException("capacity");targets=new T[capacity];}
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
        public bool Begin(float now,float duration,float directionX,float directionY,float energy)
        {
            Cancel();
            if(!Finite(now)||!Finite(duration)||duration<=0||!Finite(directionX)||!Finite(directionY)||!Finite(energy))return false;
            double length=Math.Sqrt((double)directionX*directionX+(double)directionY*directionY);
            if(length<.0001||!Finite(now+duration))return false;
            DirectionX=(float)(directionX/length);DirectionY=(float)(directionY/length);Energy=Math.Max(0,Math.Min(100,energy));
            began=now;Until=now+duration;sequence=sequence==int.MaxValue?1:sequence+1;Id=sequence;running=true;return true;
        }
        public bool IsActive(float now)
        {
            if(!running)return false;
            if(!Finite(now)||now<began||now>=Until){Cancel();return false;}
            return true;
        }
        public bool TryHit(T target,float now)
        {
            if(!IsActive(now)||ReferenceEquals(target,null))return false;
            for(int i=0;i<count;i++)if(ReferenceEquals(targets[i],target))return false;
            // Never evict a previous hit: even an overfull scene cannot multiply damage.
            if(count==targets.Length)return false;
            targets[count++]=target;return true;
        }
        public void Cancel()
        {for(int i=0;i<count;i++)targets[i]=null;count=0;running=false;Until=0;}
    }
}
