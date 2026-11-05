using System;

namespace Plugin.AncientCivilization
{
    // Animation timing is independent of the render rate. Damage still resolves once per cut.
    public static class AncientBladeRules
    {
        public const int StepCount=3,FramesPerStep=16;
        public const float InputBufferWindow=.16f,BufferLifetime=.22f,ComboGrace=.48f;

        public static int NormalizeStep(int step){return Math.Max(1,Math.Min(StepCount,step));}
        public static float Duration(int step){return NormalizeStep(step)==1?.36f:NormalizeStep(step)==2?.34f:.44f;}
        public static float ActiveStart(int step){return NormalizeStep(step)==1?.11f:NormalizeStep(step)==2?.10f:.14f;}
        public static float ImpactTime(int step){return NormalizeStep(step)==1?.14f:NormalizeStep(step)==2?.135f:.19f;}
        public static float ActiveEnd(int step){return NormalizeStep(step)==1?.21f:NormalizeStep(step)==2?.205f:.27f;}

        public static int AnimationFrame(int step,float elapsed)
        {
            if(float.IsNaN(elapsed)||elapsed<=0)return 0;
            float start=ActiveStart(step),end=ActiveEnd(step),duration=Duration(step);
            if(elapsed>=duration)return FramesPerStep-1;
            if(elapsed<start)return SpanFrame(elapsed,0,start,0,5);
            if(elapsed<end)return SpanFrame(elapsed,start,end,5,6);
            return SpanFrame(elapsed,end,duration,11,5);
        }

        public static bool IsActive(int step,float elapsed)
        {return Finite(elapsed)&&elapsed>=ActiveStart(step)&&elapsed<ActiveEnd(step);}

        public static float SweepProgress(int step,float elapsed)
        {
            if(float.IsNaN(elapsed))return 0;
            return Clamp01((elapsed-ActiveStart(step))/(ActiveEnd(step)-ActiveStart(step)));
        }

        // Measured atlas blade tips can be interpolated between the six active poses.
        public static float TrailFrame(float progress)
        {return Math.Min(10,5+6*(float.IsNaN(progress)?0:Clamp01(progress)));}

        public static int NextStep(int previous,float lastEnd,float now)
        {
            // Pause, damage, mode changes and an expired link must not replay a stale finisher.
            if(previous<1||previous>StepCount||!Finite(lastEnd)||!Finite(now)||now<lastEnd||now>lastEnd+ComboGrace)return 1;
            return previous%StepCount+1;
        }

        public static bool CanBuffer(float remaining)
        {return Finite(remaining)&&remaining>0&&remaining<=InputBufferWindow;}

        static int SpanFrame(float value,float begin,float end,int first,int length)
        {return first+Math.Min(length-1,(int)((value-begin)/(end-begin)*length));}
        static float Clamp01(float value){return Math.Max(0,Math.Min(1,value));}
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}
