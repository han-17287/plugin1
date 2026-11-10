using System;

namespace Plugin.AncientCivilization
{
    // Logical GUI pixels, top-left origin. No Unity objects or per-layout heap allocations.
    public static class AncientSpeechLayout
    {
        public const float MaximumWidth=400,MaximumHeight=190,Margin=16,ActorGap=18;
        public struct Point
        {
            public float X,Y;
            public Point(float x,float y){X=x;Y=y;}
        }
        public struct Box
        {
            public float X,Y,Width,Height;
            public Box(float x,float y,float width,float height){X=x;Y=y;Width=width;Height=height;}
            public float Right {get{return X+Width;}}
            public float Bottom {get{return Y+Height;}}
        }
        public struct Placement
        {
            public Box Bubble;
            public Point TailA,TailB,TailTip,ActorPoint;
            public bool Below,ClearsActor,TailVisible;
        }
        enum Side { Above,Below,Left,Right,Clamped }

        public static Placement Compute(float viewWidth,float viewHeight,Point head,float requestedWidth,float requestedHeight,Box actor)
        {
            float vw=Finite(viewWidth)?Clamp(viewWidth,1,32768):1280;
            float vh=Finite(viewHeight)?Clamp(viewHeight,1,32768):720;
            float margin=Math.Min(Margin,Math.Min(vw,vh)*.04f);
            head=new Point(Clamp(Finite(head.X)?head.X:vw*.5f,margin,vw-margin),Clamp(Finite(head.Y)?head.Y:vh*.5f,margin,vh-margin));
            actor=SanitizeActor(actor,head,vw,vh);
            bool hasActor=actor.Width>0&&actor.Height>0;
            float width=Clamp(Finite(requestedWidth)?requestedWidth:300,Math.Min(120,vw-margin*2),Math.Min(MaximumWidth,vw-margin*2));
            float height=Clamp(Finite(requestedHeight)?requestedHeight:140,Math.Min(56,vh-margin*2),Math.Min(MaximumHeight,vh-margin*2));
            float top=hasActor?Math.Min(head.Y,actor.Y):head.Y,bottom=hasActor?Math.Max(head.Y,actor.Bottom):head.Y;
            float left=hasActor?Math.Min(head.X,actor.X):head.X,right=hasActor?Math.Max(head.X,actor.Right):head.X;
            float x=Clamp(head.X-width*.5f,margin,vw-margin-width),y=Clamp(head.Y-height*.35f,margin,vh-margin-height);
            Box box=new Box(x,top-ActorGap-height,width,height);Side side=Side.Above;
            if(!Fits(box,vw,vh,margin)||Overlaps(box,actor))
            {
                box=new Box(x,bottom+ActorGap,width,height);side=Side.Below;
                if(!Fits(box,vw,vh,margin)||Overlaps(box,actor))
                {
                    bool preferRight=vw-right>=left;
                    box=new Box(preferRight?right+ActorGap:left-ActorGap-width,y,width,height);side=preferRight?Side.Right:Side.Left;
                    if(!Fits(box,vw,vh,margin)||Overlaps(box,actor))
                    {
                        box=new Box(preferRight?left-ActorGap-width:right+ActorGap,y,width,height);side=preferRight?Side.Left:Side.Right;
                        if(!Fits(box,vw,vh,margin)||Overlaps(box,actor))
                        {
                            float above=top-margin-ActorGap,below=vh-margin-bottom-ActorGap;
                            if(Math.Max(above,below)>=72)
                            {
                                side=above>=below?Side.Above:Side.Below;height=Math.Min(height,Math.Max(above,below));
                                box=new Box(x,side==Side.Above?top-ActorGap-height:bottom+ActorGap,width,height);
                            }
                            else
                            {
                                float before=left-margin-ActorGap,after=vw-margin-right-ActorGap;
                                if(Math.Max(before,after)>=120)
                                {
                                    side=after>=before?Side.Right:Side.Left;width=Math.Min(width,Math.Max(before,after));
                                    box=new Box(side==Side.Right?right+ActorGap:left-ActorGap-width,y,width,height);
                                }
                                else
                                {
                                    // An actor can occupy the entire viewport. Keep the box bounded and
                                    // expose the compromise so presentation can restage instead of hiding it.
                                    side=Side.Clamped;
                                    Box aboveBox=Clamped(new Box(x,top-ActorGap-height,width,height),vw,vh,margin);
                                    Box belowBox=Clamped(new Box(x,bottom+ActorGap,width,height),vw,vh,margin);
                                    box=OverlapArea(aboveBox,actor)<=OverlapArea(belowBox,actor)?aboveBox:belowBox;
                                }
                            }
                        }
                    }
                }
            }
            box=Clamped(box,vw,vh,margin);
            Point target=head;
            if(hasActor)
            {
                if(side==Side.Above)target=new Point(Clamp(head.X,actor.X,actor.Right),top);
                else if(side==Side.Below)target=new Point(Clamp(head.X,actor.X,actor.Right),bottom);
                else if(side==Side.Left)target=new Point(left,Clamp(head.Y,actor.Y,actor.Bottom));
                else if(side==Side.Right)target=new Point(right,Clamp(head.Y,actor.Y,actor.Bottom));
            }
            target=new Point(Clamp(target.X,1,Math.Max(1,vw-1)),Clamp(target.Y,1,Math.Max(1,vh-1)));
            var result=new Placement{Bubble=box,Below=side==Side.Below,ClearsActor=!Overlaps(box,actor),ActorPoint=target};
            Tail(box,target,vw,vh,ref result);
            return result;
        }

        static Box SanitizeActor(Box actor,Point head,float vw,float vh)
        {
            float x=Finite(actor.X)?Clamp(actor.X,-vw*2,vw*3):head.X,y=Finite(actor.Y)?Clamp(actor.Y,-vh*2,vh*3):head.Y;
            float w=Finite(actor.Width)?Clamp(actor.Width,0,vw*4):0,h=Finite(actor.Height)?Clamp(actor.Height,0,vh*4):0;
            return new Box(x,y,w,h);
        }
        static void Tail(Box box,Point target,float vw,float vh,ref Placement result)
        {
            if(target.X>=box.X&&target.X<=box.Right&&target.Y>=box.Y&&target.Y<=box.Bottom)
            {result.TailA=result.TailB=result.TailTip=target;return;}
            float dx=target.X-Clamp(target.X,box.X,box.Right),dy=target.Y-Clamp(target.Y,box.Y,box.Bottom);
            bool vertical=Math.Abs(dy)>=Math.Abs(dx);
            Point a,b,centre;
            if(vertical)
            {
                float inset=Math.Min(14,box.Width*.2f),half=Math.Min(8,(box.Width-inset*2)*.25f);
                float x=Clamp(target.X,box.X+inset,box.Right-inset),y=target.Y<box.Y?box.Y:box.Bottom;
                centre=new Point(x,y);a=new Point(x-half,y);b=new Point(x+half,y);
            }
            else
            {
                float inset=Math.Min(14,box.Height*.2f),half=Math.Min(8,(box.Height-inset*2)*.25f);
                float y=Clamp(target.Y,box.Y+inset,box.Bottom-inset),x=target.X<box.X?box.X:box.Right;
                centre=new Point(x,y);a=new Point(x,y-half);b=new Point(x,y+half);
            }
            float tx=target.X-centre.X,ty=target.Y-centre.Y,distance=(float)Math.Sqrt(tx*tx+ty*ty);
            float length=Math.Min(30,distance);
            Point tip=distance>.001f?new Point(centre.X+tx/distance*length,centre.Y+ty/distance*length):centre;
            tip.X=Clamp(tip.X,0,vw);tip.Y=Clamp(tip.Y,0,vh);
            result.TailA=a;result.TailB=b;result.TailTip=tip;result.TailVisible=distance>2;
        }
        static Box Clamped(Box box,float vw,float vh,float margin)
        {box.X=Clamp(box.X,margin,vw-margin-box.Width);box.Y=Clamp(box.Y,margin,vh-margin-box.Height);return box;}
        static bool Fits(Box box,float vw,float vh,float margin)
        {return box.X>=margin&&box.Y>=margin&&box.Right<=vw-margin&&box.Bottom<=vh-margin;}
        static bool Overlaps(Box a,Box b){return b.Width>0&&b.Height>0&&OverlapArea(a,b)>.001f;}
        static float OverlapArea(Box a,Box b)
        {return Math.Max(0,Math.Min(a.Right,b.Right)-Math.Max(a.X,b.X))*Math.Max(0,Math.Min(a.Bottom,b.Bottom)-Math.Max(a.Y,b.Y));}
        static float Clamp(float value,float min,float max){return Math.Max(min,Math.Min(max,value));}
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}
