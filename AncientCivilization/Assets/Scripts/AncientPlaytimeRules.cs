using System;
namespace Plugin.AncientCivilization
{
    // Measured active session time, never a target timer or a condition for unlocking content.
    public sealed class AncientPlaytimeRules
    {
        readonly double[] total=new double[2],dialogue=new double[2],boss=new double[2];readonly int[] retries=new int[2];
        public int CompletedMask {get;private set;}
        static bool Floor(int floor){return floor>=0&&floor<2;}
        static int Milliseconds(double seconds){return (int)Math.Min(int.MaxValue,Math.Max(0,Math.Round(seconds*1000)));}
        public int TotalMilliseconds(int floor){return Floor(floor)?Milliseconds(total[floor]):0;}
        public int DialogueMilliseconds(int floor){return Floor(floor)?Milliseconds(dialogue[floor]):0;}
        public int BossMilliseconds(int floor){return Floor(floor)?Milliseconds(boss[floor]):0;}
        public int Retries(int floor){return Floor(floor)?retries[floor]:0;}
        public bool Finished(int floor){return Floor(floor)&&(CompletedMask&(1<<floor))!=0;}
        public void Restore(int floor,int totalMs,int dialogueMs,int bossMs,int retryCount,bool completed)
        {
            if(!Floor(floor))return;total[floor]=Math.Max(0,totalMs)/1000.0;
            dialogue[floor]=Math.Min(total[floor],Math.Max(0,dialogueMs)/1000.0);boss[floor]=Math.Min(total[floor],Math.Max(0,bossMs)/1000.0);
            retries[floor]=Math.Max(0,retryCount);if(completed)CompletedMask|=1<<floor;else CompletedMask&=~(1<<floor);
        }
        public void Step(int floor,double seconds,bool active,bool inDialogue,bool inBoss)
        {
            if(!Floor(floor)||!active||Finished(floor)||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;
            total[floor]=Math.Min(int.MaxValue/1000.0,total[floor]+seconds);
            if(inDialogue)dialogue[floor]=Math.Min(total[floor],dialogue[floor]+seconds);
            if(inBoss)boss[floor]=Math.Min(total[floor],boss[floor]+seconds);
        }
        public void Retry(int floor){if(Floor(floor)&&!Finished(floor)&&retries[floor]<int.MaxValue)retries[floor]++;}
        public void Complete(int floor){if(Floor(floor))CompletedMask|=1<<floor;}
    }
}
