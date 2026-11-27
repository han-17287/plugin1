using System;
using System.Collections.Generic;

namespace Plugin.PlayerCore
{
    public enum ElectricPlugPulse { Spin, Throw, Chain, Arrival, Burn, Blast, Blade, Beam, Impact }

    // 최초 접촉까지의 진행률을 구한다. 고속 투척도 한 물리 틱 사이의 표적을 건너뛰지 않는다.
    public static class ElectricPlugRules
    {
        public static bool SegmentHit(float dx, float dy, float tx, float ty, float radius, out float t)
        {
            t = 0;
            if (radius < 0) return false;
            float c = tx * tx + ty * ty - radius * radius;
            if (c <= 0) return true;
            float length2 = dx * dx + dy * dy, dot = dx * tx + dy * ty;
            if (length2 <= .000001f || dot <= 0) return false;
            float discriminant = dot * dot - length2 * c;
            if (discriminant < 0) return false;
            t = (dot - (float)Math.Sqrt(discriminant)) / length2;
            return t >= 0 && t <= 1;
        }
    }

    internal sealed class ElectricPlugClock<T> where T : class
    {
        struct Shock
        {
            public T target;
            public float until, paidThrough, next, rate, interval;
        }
        struct Gate { public T target; public float until; }
        readonly List<Gate> spin = new List<Gate>(12);
        readonly List<Gate> links = new List<Gate>(12);
        readonly List<Shock> shocks = new List<Shock>(12);
        uint revision;
        public float Time { get; private set; }

        public float ShockRemaining(T target)
        {
            for(int i=0;i<shocks.Count;i++)
                if(ReferenceEquals(shocks[i].target,target))return Math.Max(0,shocks[i].until-Time);
            return 0;
        }

        public bool Spin(T target,float interval) => Admit(spin,target,interval);
        public bool Chain(T target,float interval) => Admit(links,target,interval);
        bool Admit(List<Gate> gates,T target,float interval)
        {
            for (int i = gates.Count - 1; i >= 0; --i)
            {
                if (gates[i].until <= Time) gates.RemoveAt(i);
                else if (ReferenceEquals(gates[i].target,target)) return false;
            }
            gates.Add(new Gate { target=target, until=Time+Math.Max(.01f,interval) });
            return true;
        }

        public void ShockTarget(T target,float seconds,float rate,float interval)
        {
            if (seconds <= 0 || rate <= 0) return;
            interval=Math.Max(.05f,interval);
            for (int i=0; i<shocks.Count; ++i)
            {
                var item=shocks[i];
                if (!ReferenceEquals(item.target,target)) continue;
                // 재감전은 시간을 연장한다. 이미 진행 중인 반 틱의 피해도 유지한다.
                item.until=Time+seconds;
                shocks[i]=item;
                return;
            }
            shocks.Add(new Shock {target=target, until=Time+seconds, paidThrough=Time,
                next=Time+interval, rate=rate, interval=interval});
        }

        public void Tick(float dt,Action<T,float> apply)
        {
            if (dt<=0 || float.IsInfinity(dt) || float.IsNaN(dt)) return;
            Time+=dt;
            uint original=revision;
            for (int i=shocks.Count-1;i>=0;--i)
            {
                var s=shocks[i];
                if (Time+.00001f < Math.Min(s.next,s.until)) continue;
                float end=Math.Min(Time,s.until);
                float ticks=(float)Math.Floor((end-s.paidThrough+.00001f)/s.interval);
                float through=Time+.00001f>=s.until?s.until:Math.Min(s.until,s.paidThrough+ticks*s.interval);
                float damage=(through-s.paidThrough)*s.rate;
                s.paidThrough=through;
                s.next=through+s.interval;
                if (through>=s.until) shocks.RemoveAt(i);
                else shocks[i]=s;
                if (damage>0) apply(s.target,damage);
                if (original!=revision) break;
            }
        }

        public void Clear() { revision++; spin.Clear();links.Clear();shocks.Clear();Time=0; }
    }
}
