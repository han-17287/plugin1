using System;
using Plugin.AncientCivilization;
using Layout=Plugin.AncientCivilization.AncientSpeechLayout;

class SpeechLayoutChecks
{
    static int checks;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.02f;}
    static bool Finite(float x){return !float.IsNaN(x)&&!float.IsInfinity(x);}
    static float Area(Layout.Box a,Layout.Box b)
    {return Math.Max(0,Math.Min(a.Right,b.Right)-Math.Max(a.X,b.X))*Math.Max(0,Math.Min(a.Bottom,b.Bottom)-Math.Max(a.Y,b.Y));}
    static bool Inside(Layout.Point p,float w,float h){return Finite(p.X)&&Finite(p.Y)&&p.X>=-.01f&&p.Y>=-.01f&&p.X<=w+.01f&&p.Y<=h+.01f;}
    static void CheckBounded(Layout.Placement p,float w,float h,Layout.Box actor)
    {
        var b=p.Bubble;
        Check(Finite(b.X)&&Finite(b.Y)&&Finite(b.Width)&&Finite(b.Height),"Speech geometry stays finite");
        Check(b.Width>0&&b.Height>0&&b.X>=-.01f&&b.Y>=-.01f&&b.Right<=w+.01f&&b.Bottom<=h+.01f,"The complete bubble stays in its viewport");
        Check(b.Width<=Layout.MaximumWidth+.01f&&b.Height<=Layout.MaximumHeight+.01f,"Speech never expands into a full-screen dialogue panel");
        Check(p.ClearsActor==(Area(b,actor)<=.001f),"Actor-clearance flag agrees with actual overlap");
        if(p.TailVisible)
        {
            Check(Inside(p.TailA,w,h)&&Inside(p.TailB,w,h)&&Inside(p.TailTip,w,h),"The full speech tail remains inside the viewport");
            bool horizontal=Near(p.TailA.Y,p.TailB.Y)&&(Near(p.TailA.Y,b.Y)||Near(p.TailA.Y,b.Bottom));
            bool vertical=Near(p.TailA.X,p.TailB.X)&&(Near(p.TailA.X,b.X)||Near(p.TailA.X,b.Right));
            Check(horizontal||vertical,"The tail grows from a real bubble edge");
            float cx=(p.TailA.X+p.TailB.X)*.5f,cy=(p.TailA.Y+p.TailB.Y)*.5f;
            float dx=p.TailTip.X-cx,dy=p.TailTip.Y-cy;
            Check(dx*dx+dy*dy<=30.01f*30.01f,"An offscreen actor cannot create a long tail across the scene");
            Check(dx*(p.ActorPoint.X-cx)+dy*(p.ActorPoint.Y-cy)>0,"The tail points toward its speaker");
            Check(p.TailTip.X<=b.X||p.TailTip.X>=b.Right||p.TailTip.Y<=b.Y||p.TailTip.Y>=b.Bottom,"A visible tail points outside the bubble");
        }
    }
    static void Main()
    {
        var actor=new Layout.Box(620,360,40,180);
        var above=Layout.Compute(1280,720,new Layout.Point(640,360),380,140,actor);
        Check(above.ClearsActor&&!above.Below&&above.Bubble.Bottom<actor.Y,"Ordinary conversation sits above the speaker and leaves the actor visible");
        Check(above.TailVisible&&Near(above.TailTip.X,640)&&Near(above.TailTip.Y,360),"A nearby speaker receives an accurately attached speech tail");
        CheckBounded(above,1280,720,actor);
        actor=new Layout.Box(620,10,40,160);
        var below=Layout.Compute(1280,720,new Layout.Point(640,10),380,140,actor);
        Check(below.Below&&below.ClearsActor&&below.Bubble.Y>actor.Bottom,"A speaker near the ceiling gets a bubble below their body");
        Check(below.TailTip.Y>=actor.Bottom,"A below-speaker tail does not cross the character's body");CheckBounded(below,1280,720,actor);
        actor=new Layout.Box(600,80,80,600);
        var side=Layout.Compute(1280,720,new Layout.Point(640,80),400,190,actor);
        Check(side.ClearsActor&&(side.Bubble.Right<actor.X||side.Bubble.X>actor.Right),"A tall character can use free space to the side when vertical space is insufficient");CheckBounded(side,1280,720,actor);
        actor=new Layout.Box(320,120,640,560);
        var paged=Layout.Compute(1280,720,new Layout.Point(640,120),400,190,actor);
        Check(paged.ClearsActor&&paged.Bubble.Height<190&&paged.Bubble.Bottom<actor.Y,"A broad actor can keep clear by shortening the bubble for pagination");CheckBounded(paged,1280,720,actor);
        actor=new Layout.Box(140,80,28,80);
        var tiny=Layout.Compute(320,240,new Layout.Point(154,80),400,190,actor);
        Check(tiny.ClearsActor&&tiny.Bubble.Width<200,"A small window uses narrow side space without covering its speaker");CheckBounded(tiny,320,240,actor);
        actor=new Layout.Box(0,0,1280,720);
        var impossible=Layout.Compute(1280,720,new Layout.Point(640,10),400,190,actor);
        Check(!impossible.ClearsActor,"An actor filling the screen exposes the need for presentation restaging");CheckBounded(impossible,1280,720,actor);
        var left=Layout.Compute(1280,720,new Layout.Point(35,420),360,150,new Layout.Box(15,420,40,150));
        var right=Layout.Compute(1280,720,new Layout.Point(1245,420),360,150,new Layout.Box(1225,420,40,150));
        Check(Near(left.Bubble.X,1280-right.Bubble.Right)&&Near(left.Bubble.Y,right.Bubble.Y),"Left and right edge speakers keep symmetric readable bubbles");
        Check(Near(left.TailTip.X,1280-right.TailTip.X),"Tail attachment mirrors with the edge speaker");
        var random=new Random(17287);
        int[,] sizes={{320,240},{640,480},{720,1280},{1024,768},{1280,720},{1920,1080},{2560,720},{3840,2160},{5120,1440}};
        for(int size=0;size<sizes.GetLength(0);size++)for(int sample=0;sample<45;sample++)
        {
            float w=sizes[size,0],h=sizes[size,1],x=(float)random.NextDouble()*w,y=(float)random.NextDouble()*h;
            float aw=24+(float)random.NextDouble()*120,ah=70+(float)random.NextDouble()*230;
            actor=new Layout.Box(x-aw*.5f,y,aw,ah);
            float requestedW=150+(float)random.NextDouble()*400,requestedH=70+(float)random.NextDouble()*180;
            var p=Layout.Compute(w,h,new Layout.Point(x,y),requestedW,requestedH,actor);CheckBounded(p,w,h,actor);
            float margin=Math.Min(16,Math.Min(w,h)*.04f),height=Math.Min(190,requestedH);
            if(y-Layout.ActorGap-height>=margin)
                Check(p.ClearsActor&&p.Bubble.Bottom<=actor.Y,"Available space above a normal actor is used before covering them");
        }
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            var p=Layout.Compute(bad,bad,new Layout.Point(bad,bad),bad,bad,new Layout.Box(bad,bad,bad,bad));
            Check(Finite(p.Bubble.X)&&Finite(p.Bubble.Y)&&Finite(p.Bubble.Width)&&Finite(p.Bubble.Height),"Invalid projection data cannot create NaN or infinite draw rectangles");
        }
        for(int i=0;i<10;i++)Layout.Compute(1280,720,new Layout.Point(640,360),380,140,new Layout.Box(620,360,40,180));
        long before=GC.GetAllocatedBytesForCurrentThread();float sum=0;
        for(int i=0;i<10000;i++)sum+=Layout.Compute(1280,720,new Layout.Point(640,360),380,140,new Layout.Box(620,360,40,180)).Bubble.X;
        long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Check(sum>0&&allocated==0,"Repeated geometry calculation allocates no managed memory after warmup");
        Console.WriteLine("PASS: "+checks+" speech-bubble geometry checks; standalone .NET rules only");
    }
}
