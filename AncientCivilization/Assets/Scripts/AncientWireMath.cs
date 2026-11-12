using System;
namespace Plugin.AncientCivilization
{
    public static class AncientWireMath
    {
        public static int Samples(float length,bool high)
        {return length<3?24:length<6?48:length<12||!high?96:192;}
        // Angle recurrence avoids per-vertex sine/cosine calls; output buffers belong to the caller.
        public static void Fill(float length,float age,int strand,int count,float[] normal,float[] depth)
        {
            if(count<2||normal==null||depth==null||normal.Length<count||depth.Length<count)throw new ArgumentException("Wire buffers too small");
            double angle=-age*7+strand*Math.PI;
            double step=length*Math.PI*2/(1.15*(count-1));
            double sn=Math.Sin(angle),cs=Math.Cos(angle),ds=Math.Sin(step),dc=Math.Cos(step);
            for(int i=0;i<count;i++)
            {
                double t=i/(double)(count-1);
                double radius=.14*Math.Max(0,Math.Min(1,t*length/.2))*Math.Max(0,Math.Min(1,(1-t)*length/.2));
                normal[i]=(float)(sn*radius);depth[i]=(float)(cs*radius);
                double next=sn*dc+cs*ds;cs=cs*dc-sn*ds;sn=next;
            }
        }
    }
}
