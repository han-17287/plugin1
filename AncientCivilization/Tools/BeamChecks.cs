using System;
using Plugin.AncientCivilization;

class BeamChecks
{
    static int count;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);count++;}
    static bool Near(float a,float b,float epsilon=.0001f){return Math.Abs(a-b)<epsilon;}
    static void Main()
    {
        float previous=0;
        for(int energy=0;energy<=100;energy++)
        {
            float body=AncientBeamRules.BodyWidth(energy),core=AncientBeamRules.CoreWidth(energy),halo=AncientBeamRules.SheathWidth(energy);
            Check(core<body&&body<halo,"The white core, blue collision body and soft light remain visibly separate layers");
            Check(body>=previous,"Captured energy cannot make the real laser body narrower");previous=body;
            Check(body>=(.30f+energy*.0055f)*2.5f,"The cannon body is at least2.5times the former beam at every charge level");
        }
        Check(Near(AncientBeamRules.BodyWidth(-10),AncientBeamRules.BodyWidth(0)),"Negative energy cannot create an inverted beam");
        Check(Near(AncientBeamRules.BodyWidth(200),AncientBeamRules.BodyWidth(100)),"Overfull energy cannot exceed the intended body width");
        Check(AncientBeamRules.Range==24,"Mouse direction fires a full24metre ray until terrain clips it");
        Check(AncientBeamRules.Duration>AncientBeamRules.FadeSeconds,"The beam holds its complete body before the final fade");
        foreach(float age in new[]{0f,.03f,.1f,.2f,AncientBeamRules.Duration})
        {
            float remaining=AncientBeamRules.Duration-age,opacity=AncientBeamRules.Opacity(remaining);
            Check(opacity>=0&&opacity<=1,"The beam fade remains bounded");
            if(remaining>=AncientBeamRules.FadeSeconds)Check(opacity==1,"The ray appears at full strength immediately without a traveling head");
        }
        Check(AncientBeamRules.Opacity(0)==0&&AncientBeamRules.Opacity(-1)==0,"Expired laser light does not remain visible");

        float radius=AncientBeamRules.BodyWidth(100)*.5f;
        foreach(float angle in new[]{0f,.3f,1.2f,2.5f,3.14f,4.7f,6.0f})
        {
            float ux=(float)Math.Cos(angle),uy=(float)Math.Sin(angle);
            foreach(var sample in new[]{new[]{5f,0f,1f},new[]{5f,radius+.59f,1f},new[]{5f,radius+.61f,0f},
                new[]{-.4f,radius+.4f,1f},new[]{-.5f,radius+.5f,0f},new[]{-1f,0f,0f},
                new[]{11f,0f,0f},new[]{10.4f,radius+.4f,1f},new[]{10.5f,radius+.5f,0f}})
            {
                float x=sample[0]*ux-sample[1]*uy,y=sample[0]*uy+sample[1]*ux;
                Check(AncientBeamRules.IntersectsCircle(10*ux,10*uy,x,y,radius,.6f)==(sample[2]>0),"Thick laser collision follows its flat face and rounded target across every firing angle");
            }
        }
        Check(!AncientBeamRules.IntersectsCircle(0,0,0,0,radius,1),"A muzzle inside terrain cannot damage through a zero-length ray");
        Check(!AncientBeamRules.IntersectsBox(0,0,0,0,radius,3,1.4f),"A blocked shot cannot suppress a truck through its endpoint glow");
        Check(AncientBeamRules.IntersectsBox(10,0,5,radius+1.39f,radius,3,1.4f),"A truck touched by the blue body's side is a hit");
        Check(!AncientBeamRules.IntersectsBox(10,0,5,radius+1.41f,radius,3,1.4f),"The cosmetic sheath does not count as truck collision");
        Check(!AncientBeamRules.IntersectsBox(10,0,13.02f,0,radius,3,1.4f),"A box wholly beyond the clipped endpoint is not hit");
        Check(!AncientBeamRules.IntersectsBox(10,0,-3.02f,0,radius,3,1.4f),"A box wholly behind the emitter is not hit");
        foreach(float width in new[]{0f,.5f,1.1f})for(int xi=-20;xi<=70;xi++)for(int yi=-20;yi<=20;yi++)
        {
            float x=xi*.2f,y=yi*.2f;bool expected=x+.51f>=0&&x-.51f<=10&&y+.31f>=-width&&y-.31f<=width;
            Check(AncientBeamRules.IntersectsBox(10,0,x,y,width,.51f,.31f)==expected,"Vehicle collision matches the visible flat-ended beam rectangle");
        }
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            Check(!AncientBeamRules.IntersectsCircle(bad,0,5,0,radius,.6f),"Invalid beam coordinates fail closed");
            Check(!AncientBeamRules.IntersectsCircle(10,0,bad,0,radius,.6f),"Invalid target coordinates fail closed");
            Check(!AncientBeamRules.IntersectsBox(10,0,5,0,bad,3,1),"Invalid body widths fail closed");
            Check(AncientBeamRules.Opacity(bad)==0,"Invalid fade times cannot leave a bright beam");
        }
        Console.WriteLine("PASS: "+count+" thick-laser geometry, charge and presentation checks");
    }
}
