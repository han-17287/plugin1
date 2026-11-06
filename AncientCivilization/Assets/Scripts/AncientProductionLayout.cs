using System;

namespace Plugin.AncientCivilization
{
    // Authored opening: a safe work route, a clearly optional wire shortcut, then the stock hall.
    // The transport tunnel lives outside the five-city and finale coordinate ranges.
    public static class AncientProductionLayout
    {
        public const float TutorialLeft=-244,TutorialSpawn=-234,TutorialRight=118,StockX=92;
        public const float ChaseStart=30000,ChaseEnd=31600,RoadY=0,Ceiling=10,TunnelCeiling=11;
        public const float BreachX=104,ConveyorLeft=-175,ConveyorRight=-159;
        public const float JumpCrateX=-185,JumpCrateWidth=1.4f,JumpCrateHeight=.55f;
        public struct Span
        {
            public readonly float Left,Right,Top;
            public Span(float left,float right,float top){Left=left;Right=right;Top=top;}
        }
        public struct Ramp
        {
            public readonly float Left,Right,Low,High;
            public Ramp(float left,float right,float low,float high){Left=left;Right=right;Low=low;High=high;}
            public float Height(float x){return Low+(High-Low)*Math.Max(0,Math.Min(1,(x-Left)/(Right-Left)));}
        }
        public static readonly Ramp[] GentleRamps={new Ramp(-158,-150,0,.8f),new Ramp(-136,-128,.8f,0)};
        public static readonly Span[] RaisedRoad={new Span(-150,-136,.8f)};
        public static readonly Span[] WireShortcut={new Span(-120,-112,3.2f),new Span(-100,-92,4.2f),new Span(-80,-72,4.2f),new Span(-60,-52,3.2f)};
        public static readonly float[] WireHookX={-120.55f,-100.55f,-80.55f,-60.55f};
        public const float WireHookY=8.8f,CatwalkThickness=.18f;
        public static readonly float[] SwingHookX={-111,-91,-71};
        public const float SwingHookY=7.5f;
        public static readonly float[] TrainingEmitterX={-42,-18,7,28,56,86};
        public const float TrainingEmitterY=1.6f,TrainingEmitterRange=16;
        public static readonly float[] SectionStarts={TutorialLeft,-194,-128,-52,38};
        public static readonly string[] SectionNames={"교대 통로","운송 벨트","검수 회랑","장비 검수실","코어 재고 관리실"};
        public static float GroundHeight(float x)
        {
            foreach(var ramp in GentleRamps)if(x>=ramp.Left&&x<=ramp.Right)return ramp.Height(x);
            foreach(var span in RaisedRoad)if(x>=span.Left&&x<=span.Right)return span.Top;
            if(Math.Abs(x-JumpCrateX)<=JumpCrateWidth*.5f)return JumpCrateHeight;
            return RoadY;
        }
        public static int SectionAt(float x)
        {
            if(float.IsNaN(x)||float.IsInfinity(x)||x<TutorialLeft||x>TutorialRight)return -1;
            for(int i=SectionStarts.Length-1;i>=0;i--)if(x>=SectionStarts[i])return i;
            return -1;
        }
        public static bool InTutorial(float x){return SectionAt(x)>=0;}
        public static float TutorialCheckpointX(int stage)
        {return stage<=1?TutorialSpawn:stage==2?-159:stage==3?-48:StockX-3;}
        public static float TutorialCheckpointY(int stage)
        {return GroundHeight(TutorialCheckpointX(stage))+1.3f;}
        // These are representative visible racks, not a replacement for the full 120-unit stock ledger.
        public static bool StockCorePresent(int rack,int row,int slot)
        {return rack>=0&&rack<4&&row>=0&&row<3&&slot>=0&&slot<3&&!(rack==3&&row==2);}
        public static bool InChaseLane(float x)
        {return !float.IsNaN(x)&&!float.IsInfinity(x)&&x>=ChaseStart&&x<=ChaseEnd;}
        static float CameraGap(float gap)
        {return float.IsNaN(gap)||float.IsInfinity(gap)?22:Math.Max(0,Math.Min(32,gap));}
        public static float RoadCameraLead(float truckGap)
        {return Math.Max(7,Math.Min(11.5f,CameraGap(truckGap)*.5f+.5f));}
        public static float RoadCameraZoom(float truckGap,float aspect)
        {
            float gap=CameraGap(truckGap),lead=RoadCameraLead(gap);
            if(float.IsNaN(aspect)||float.IsInfinity(aspect))aspect=16/9f;
            // Include the whole rider, the forward road hazards and the whole truck.
            // The extra right margin covers exponential follow lag and one20ms
            // fixed physics step; framing the target alone clips a moving truck.
            float lag=17/7f+17*.02f;
            float halfSpan=Math.Max(lead+2.4f,gap+4.1f-lead+lag);
            return Math.Max(7.8f,halfSpan/Math.Max(.5f,aspect));
        }
        public static float DeckCameraZoom(float heroOffset,float aspect)
        {
            if(float.IsNaN(heroOffset)||float.IsInfinity(heroOffset))heroOffset=0;
            if(float.IsNaN(aspect)||float.IsInfinity(aspect))aspect=16/9f;
            // Focus is halfway between the rider and the truck. Include the captain's
            // extended throw pose and the follow lag at full wire speed in either direction.
            return Math.Max(6.5f,(Math.Abs(heroOffset)*.5f+8.2f)/Math.Max(.5f,aspect));
        }
    }
}
