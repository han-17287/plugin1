using System;

namespace Plugin.AncientCivilization
{
    // Input-facing and screen-space feedback only: no damage, timing or movement tuning.
    public static class AncientCombatFeedbackRules
    {
        public struct Box
        {
            public float X,Y,Width,Height;
            public Box(float x,float y,float width,float height){X=x;Y=y;Width=width;Height=height;}
        }
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
        public static float BladeFacing(float horizontal,float previous)
        {
            float facing=Finite(previous)&&previous<0?-1:1;
            return !Finite(horizontal)||Math.Abs(horizontal)<=.2f?facing:horizontal<0?-1:1;
        }
        public static bool LaserReady(float energy,float progress)
        {return Finite(energy)&&Finite(progress)&&energy>0&&progress>=1;}
        public static Box ChargeLabel(float viewWidth,float viewHeight,float mouseX,float mouseY,float textWidth,float textHeight,float scale)
        {
            float width=Finite(viewWidth)?Math.Max(1,viewWidth):1280,height=Finite(viewHeight)?Math.Max(1,viewHeight):720;
            scale=Finite(scale)?Math.Max(.75f,Math.Min(2,scale)):1;
            float margin=Math.Min(10*scale,Math.Min(width,height)*.04f),gap=18*scale;
            float w=Math.Min(width-margin*2,Math.Max(80*scale,(Finite(textWidth)?textWidth:180)+16*scale));
            float h=Math.Min(height-margin*2,Math.Max(24*scale,(Finite(textHeight)?textHeight:18)+10*scale));
            float mx=Finite(mouseX)?Math.Max(0,Math.Min(width,mouseX)):width*.5f;
            float my=Finite(mouseY)?Math.Max(0,Math.Min(height,mouseY)):height*.5f;
            float x=mx+gap,y=my+gap;
            if(x+w>width-margin)x=mx-gap-w;
            if(y+h>height-margin)y=my-gap-h;
            return new Box(Math.Max(margin,Math.Min(width-margin-w,x)),Math.Max(margin,Math.Min(height-margin-h,y)),w,h);
        }
    }
}
