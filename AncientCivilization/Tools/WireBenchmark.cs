using System;
using System.Diagnostics;
using Plugin.AncientCivilization;
class WireBenchmark
{
    static float checksum;
    static void Main()
    {
        const int frames=20000,count=192;var normal=new float[count];var depth=new float[count];
        for(int i=0;i<1000;i++)AncientWireMath.Fill(22,.6f,0,count,normal,depth);
        var watch=Stopwatch.StartNew();
        for(int frame=0;frame<frames;frame++)for(int strand=0;strand<2;strand++)
        {
            for(int i=0;i<count;i++)AncientRules.WireHelixOffset(22,frame*.00005f,i/(count-1f),strand,out normal[i],out depth[i]);
            checksum+=normal[count/2]+depth[count/2];
        }
        watch.Stop();double reference=watch.Elapsed.TotalMilliseconds;
        long before=GC.GetAllocatedBytesForCurrentThread();watch.Restart();
        for(int frame=0;frame<frames;frame++)for(int strand=0;strand<2;strand++)
        {AncientWireMath.Fill(22,frame*.00005f,strand,count,normal,depth);checksum+=normal[count/2]+depth[count/2];}
        watch.Stop();long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Console.WriteLine("Pure .NET wire math only; not a Unity FPS or GPU benchmark.");
        Console.WriteLine("Frames: "+frames+", two strands, "+count+" samples each");
        Console.WriteLine("Reference trig: "+reference.ToString("F2")+"ms");
        Console.WriteLine("Recurrence: "+watch.Elapsed.TotalMilliseconds.ToString("F2")+"ms");
        Console.WriteLine("Recurrence managed allocations: "+allocated+" bytes; checksum "+checksum);
        if(allocated!=0)throw new Exception("Wire generation allocated in the steady-state loop");
    }
}
