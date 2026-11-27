using UnityEngine;
using Beat = Plugin.AncientCivilization.AncientBossCue.Beat;
using Style = Plugin.AncientCivilization.AncientBossCue.Style;

namespace Plugin.AncientCivilization
{
    // 원점은 기존 판정의 접점, 오른쪽은 기존 공격 방향이다. 별도 판정이나 타이머를 만들지 않는다.
    internal static class FoundryBossFlourishes
    {
        static readonly Color Charcoal = new Color(.18f, .16f, .18f);
        static readonly Color Rust = new Color(.72f, .27f, .12f);
        static readonly Color Copper = new Color(1f, .62f, .26f);
        static readonly Color Heat = new Color(1f, .95f, .72f);
        static readonly Color Iron = new Color(.24f, .34f, .39f);
        static readonly Color Alloy = new Color(.54f, .72f, .73f);
        static readonly Color Bronze = new Color(.50f, .35f, .20f);
        static readonly Color Brass = new Color(.88f, .73f, .43f);
        static readonly Color Plum = new Color(.43f, .29f, .61f);
        static readonly Color Lilac = new Color(.77f, .65f, .99f);
        static readonly Color Rose = new Color(1f, .72f, .67f);

        internal static void Draw(BossFlourishBrush b, Style style, Beat beat, float t)
        {
            float alpha = Mathf.Clamp01((1f-t) * (beat == Beat.Tell ? 4.5f : 2.1f));
            if (alpha <= 0f) return;
            switch (style)
            {
                case Style.Captain: Captain(b, beat, t, alpha); break;
                case Style.Mech: Mech(b, beat, t, alpha); break;
                case Style.Guardian: Guardian(b, beat, t, alpha); break;
                case Style.Convoy: Convoy(b, beat, t, alpha); break;
            }
        }

        static Color Ink(Color c, float alpha)
        {
            c.a = alpha;
            return c;
        }

        static Vector2 Axis(float degrees)
        {
            float angle = degrees*Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        static Vector2 Across(Vector2 dir) => new Vector2(-dir.y, dir.x);
        static float Release(float t) => 1f - (1f-t)*(1f-t);

        // 앞면과 그늘면이 만나는 능선으로 유리 조각과 피스톤의 회전을 보여 준다.
        static void Facet(BossFlourishBrush b, Vector2 centre, Vector2 axis, float length, float width,
            Color light, Color dark, float alpha)
        {
            Vector2 normal = Across(axis)*width;
            Vector2 head = centre+axis*length*.60f, tail = centre-axis*length*.40f;
            Vector2 ridge = centre-axis*length*.13f;
            b.Triangle(tail, ridge+normal, head, Ink(light,alpha));
            b.Triangle(tail, head, ridge-normal*.78f, Ink(dark,alpha));
        }

        static void Staple(BossFlourishBrush b, Vector2 pivot, Vector2 axis, float size, float alpha)
        {
            Vector2 elbow = Across(axis)*size*.52f;
            Vector2 bottom = pivot-axis*size*.52f;
            Vector2 top = pivot+axis*size*.46f;
            b.Stroke(top-elbow, bottom-elbow, .075f, Ink(Rust,alpha));
            b.Stroke(bottom-elbow, bottom+elbow, .08f, Ink(Copper,alpha));
            b.Stroke(bottom+elbow, top+elbow, .065f, Ink(Heat,alpha));
        }

        static void Captain(BossFlourishBrush b, Beat beat, float t, float a)
        {
            float u = Release(t);
            if (beat == Beat.Tell)
            {
                for (int side=-1; side<=1; side+=2)
                {
                    float shut = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t*1.6f-(side>0?.18f:0f)));
                    Vector2 joint = new Vector2(-.62f+.30f*shut, side*(.86f-.49f*shut));
                    Vector2 jaw = Axis(-side*(37f-29f*shut));
                    Facet(b,joint,jaw,.94f,.16f,Copper,Rust,a);
                    Vector2 lip = joint+jaw*.49f;
                    b.Stroke(lip, lip+new Vector2(.02f,-side*.21f), .09f, Ink(Heat,a));
                    for (int i=0;i<3;i++)
                    {
                        Vector2 tooth = joint-jaw*(.29f-i*.14f);
                        b.Stroke(tooth, tooth+Across(jaw)*(.12f*side), .044f, Ink(Charcoal,a));
                    }
                }
                b.Splinter(new Vector2(.08f,0),new Vector2(.34f+.35f*u,0),.064f,Ink(Heat,a));
                return;
            }
            if (beat == Beat.Strike)
            {
                // 감긴 강철 띠가 열리고 뒤쪽부터 지워진다. 골은 띠에 붙어 함께 휜다.
                float begin = t*t*.79f, end = Mathf.Lerp(.46f,1f,Mathf.Clamp01(t*3f));
                Vector2 lastTop=default, lastBase=default;
                for (int i=0;i<=9;i++)
                {
                    float s = Mathf.Lerp(begin,end,i/9f);
                    float wave = Mathf.Sin(s*Mathf.PI);
                    Vector2 rim = new Vector2(-.57f+3f*s, .08f+.68f*wave*wave-.43f*s);
                    Vector2 inner = rim+new Vector2(-.11f*wave,-(.035f+.32f*wave)*(1-.45f*t));
                    if (i>0)
                    {
                        b.Triangle(lastTop,lastBase,rim,Ink(Rust,a*.90f));
                        b.Triangle(lastBase,inner,rim,Ink(Copper,a*.90f));
                        b.Stroke(lastTop,rim,.055f,Ink(Heat,a));
                        if (s>.23f && s<.90f)
                        {
                            Vector2 bow=new Vector2(-.12f*wave,.10f+(.12f+.18f*t)*wave);
                            b.Stroke(lastTop+bow,rim+bow,.027f+.016f*wave,Ink(Copper,a*.78f));
                            if (i<7) b.Stroke(lastBase-bow*.71f,inner-bow*.71f,.033f,Ink(Heat,a*.68f));
                        }
                        if (i%2==1) b.Stroke(inner,Vector2.Lerp(rim,lastTop,.62f),.052f,Ink(Charcoal,a*.9f));
                    }
                    lastTop=rim; lastBase=inner;
                }
                Staple(b,new Vector2(-.38f-.54f*u,.22f+.80f*u),Axis(50f+135f*t),.38f,a);
                Staple(b,new Vector2(-.22f-.68f*u,-.30f-.65f*u),Axis(-62f-92f*t),.27f,a*.82f);
                return;
            }
            for (int i=0;i<5;i++)
            {
                float side=i%2==0?1f:-1f;
                Vector2 dir=Axis(side*(26f+32f*i));
                float radius=(.16f+(1.23f+.18f*(i%3))*u);
                Vector2 centre=dir*radius+new Vector2(.26f*u,-.28f*t*t);
                Staple(b,centre,Axis(side*(21f+32f*i+125f*t)),.27f+.055f*(i%3),a);
                b.Splinter(centre-dir*(.39f-.19f*t),centre,.036f,Ink(Heat,a*.85f));
            }
            Vector2 seam=new Vector2(.59f+1.32f*u,.13f-.36f*t);
            b.Splinter(new Vector2(-.22f,0),seam,.12f*(1f-t),Ink(Copper,a));
            b.Splinter(new Vector2(-.31f,.04f),seam*.78f,.046f*(1f-t),Ink(Heat,a));
            b.Splinter(new Vector2(.14f,.12f+.32f*u),new Vector2(.42f+.61f*u,.16f+.32f*u),
                .054f,Ink(Copper,a*.85f));
        }

        static void Mech(BossFlourishBrush b, Beat beat, float t, float a)
        {
            float u=Release(t);
            if (beat==Beat.Tell)
            {
                for (int side=-1;side<=1;side+=2)
                {
                    Vector2 rail=Axis(-side*29f), rear=new Vector2(-.87f,side*.68f);
                    Vector2 sleeve=rear+rail*(.26f+.07f*u);
                    Vector2 rod=rear+rail*(.69f+.35f*u);
                    b.Stroke(rear,rod,.16f,Ink(Charcoal,a));
                    b.Stroke(sleeve,rod,.082f,Ink(Heat,a));
                    Facet(b,sleeve,rail,.74f,.22f,Alloy,Iron,a);
                    b.Stroke(rod-Across(rail)*.16f,rod+Across(rail)*.16f,.105f,Ink(Copper,a));
                    for (int i=0;i<3;i++)
                    {
                        Vector2 collar=sleeve+rail*(i*.12f-.14f);
                        b.Stroke(collar+Across(rail)*.10f,collar-Across(rail)*.08f,.044f,Ink(Charcoal,a));
                    }
                }
                return;
            }
            if (beat==Beat.Strike)
            {
                // 네 방향의 안전밸브가 먼저 벌어지고 황백색 증기가 입구를 따라 밀고 나온다.
                for (int i=0;i<4;i++)
                {
                    bool forward=i<2;float side=i%2==0?1f:-1f;
                    Vector2 jet=Axis(side*(forward?27f+7f*u:144f-14f*u));
                    Vector2 throat=jet*(.16f+.21f*u);
                    float reach=(forward?2.3f:1.10f)*(.52f+.48f*u);
                    Vector2 tip=jet*reach;
                    b.Splinter(throat-jet*.13f,tip,.32f*(1f-.46f*t),Ink(Iron,a*.54f));
                    b.Splinter(throat,tip,.22f*(1f-.55f*t),Ink(forward?Copper:Alloy,a*.87f));
                    b.Splinter(throat+jet*.13f,tip-jet*.12f,.070f*(1f-.60f*t),Ink(Heat,a));
                    Vector2 plate=jet*(.39f+.47f*u)+Across(jet)*(.13f*side);
                    b.Stroke(plate-Across(jet)*.19f,plate+Across(jet)*.19f,.12f,Ink(Iron,a));
                    b.Stroke(plate,plate+Across(jet)*.18f,.042f,Ink(Heat,a));
                    if (forward)
                    {
                        for (int cell=0;cell<3;cell++)
                        {
                            Vector2 centre=jet*(.59f+.37f*cell+.33f*u);
                            float width=.13f-.025f*cell;
                            Vector2 left=centre-jet*.10f, right=centre+jet*.14f;
                            b.Stroke(left,centre+Across(jet)*width,.033f,Ink(Heat,a*.75f));
                            b.Stroke(centre+Across(jet)*width,right,.033f,Ink(Heat,a*.75f));
                        }
                    }
                }
                for(int side=-1;side<=1;side+=2)
                    b.Arc(new Vector2(-.24f,0),.28f+.79f*u,side*108f-18f,36f,.065f,Ink(Alloy,a*.78f),4);
                return;
            }
            for (int i=0;i<6;i++)
            {
                Vector2 dir=Axis(18f+i*60f);
                Vector2 centre=dir*(.35f+u*(1.04f+.31f*(i%2)));
                centre.y-=.38f*t*t;
                Facet(b,centre,Axis(35f+i*60f+t*(i%2==0?118f:-88f)),.55f-.14f*t,.16f,Alloy,Iron,a);
                b.Splinter(dir*(.12f+.38f*u),centre,.074f*(1f-t),Ink(Copper,a));
            }
            for (int side=-1;side<=1;side+=2)
                b.Arc(Vector2.zero,.29f+.88f*u,side*90f-33f,66f,.09f*(1f-.65f*t),Ink(Heat,a),5);
        }

        static void Guardian(BossFlourishBrush b, Beat beat, float t, float a)
        {
            float u=Release(t);
            if (beat==Beat.Tell)
            {
                float radius=1.02f-.37f*u, turn=24f*(1f-u);
                for (int i=0;i<6;i++)
                {
                    Vector2 q=Axis(60f*i+turn), next=Axis(60f*(i+1)+turn);
                    Vector2 p=q*radius, end=next*radius;
                    b.Stroke(p,Vector2.Lerp(p,end,.79f),i%2==0?.112f:.06f,Ink(i%2==0?Brass:Bronze,a));
                    if(i%2==0)
                    {
                        Vector2 tooth=q*(radius+.16f);
                        b.Triangle(p,tooth,Vector2.Lerp(p,end,.31f),Ink(Heat,a));
                    }
                }
                return;
            }
            if (beat==Beat.Strike)
            {
                // 묵직한 잠금쇠가 반대로 열리면서 앞쪽 래칫 세 개가 차례로 빠진다.
                for(int lug=0;lug<3;lug++)
                {
                    Vector2 axis=Axis(88f+lug*90f-17f*u), bend=Axis(136f+lug*90f-17f*u);
                    Vector2 jaw=new Vector2(-.21f-.32f*u,0)+axis*(.55f+.30f*u);
                    b.Stroke(jaw-axis*.23f,jaw,.102f,Ink(Bronze,a));
                    b.Stroke(jaw,jaw+bend*.40f,.08f,Ink(Brass,a));
                }
                for (int i=0;i<3;i++)
                {
                    float cycle=Mathf.Clamp01(t*1.8f-i*.17f);
                    if(i>0 && cycle<=0) continue;
                    float x=-.08f+.29f*i+Release(cycle)*(.82f+i*.18f);
                    float opening=(1.06f-.20f*i)*(1f-.36f*cycle);
                    Vector2 nose=new Vector2(x+.54f,0);
                    for(int side=-1;side<=1;side+=2)
                    {
                        Vector2 flank=new Vector2(x-.22f,side*opening);
                        Vector2 heel=flank+new Vector2(-.35f,0);
                        b.Triangle(heel,flank,nose,Ink(i==0?Bronze:Brass,a*(1f-.14f*i)));
                        b.Stroke(flank,nose,.072f,Ink(i==2?Heat:Brass,a));
                    }
                }
                return;
            }
            // 큰 너트에서 마주 보는 세 쌍의 면이 서로 다른 속도로 떨어져 나간다.
            for (int i=0;i<6;i++)
            {
                Vector2 radial=Axis(i*60f+30f);
                Vector2 face=Axis(i*60f+120f+(i%2==0?1f:-1f)*u*31f);
                Vector2 side=Across(face);
                Vector2 centre=radial*(.35f+u*(1.44f-.2f*(i%2)));
                Vector2 left=centre-face*.31f, right=centre+face*.31f;
                Vector2 ridge=centre-side*.18f+face*.09f;
                b.Triangle(left,right,ridge,Ink(Brass,a));
                b.Triangle(left+side*.12f,right,left,Ink(Bronze,a));
                b.Stroke(left,ridge,.047f,Ink(Heat,a));
                b.Splinter(radial*(.14f+.28f*u),centre-side*.07f,.054f*(1f-t),Ink(Brass,a*.8f));
                if (i%2==0)
                {
                    Vector2 pin=radial*(.53f+.40f*u)+face*(.22f+.37f*u);
                    Facet(b,pin,face,.28f,.079f,Heat,Bronze,a);
                }
            }
        }

        // 프리즘의 옆면에만 맺히는 두 가닥의 타원 집광선이다.
        static void Caustic(BossFlourishBrush b, float u, float alpha)
        {
            for(int side=-1;side<=1;side+=2)
            {
                Vector2 previous=default;
                for(int i=0;i<=7;i++)
                {
                    float f=i/7f;
                    Vector2 dir=Axis((39f+102f*f+18f*u)*side);
                    Vector2 p=new Vector2(-.17f+dir.x*(.62f+.44f*u),dir.y*(.47f+.67f*u));
                    if(i>0)
                    {
                        float shade=Mathf.Sin((i-.5f)/7f*Mathf.PI);
                        b.Stroke(previous,p,.022f+.035f*shade,Ink(side>0?Rose:Lilac,alpha*shade*.82f));
                    }
                    previous=p;
                }
            }
        }

        static void Convoy(BossFlourishBrush b, Beat beat, float t, float a)
        {
            float u=Release(t);
            if (beat==Beat.Tell)
            {
                // 세 렌즈 날이 초점에 모이되 가운데에는 관통하는 빛 한 줄을 남긴다.
                for(int i=0;i<3;i++)
                {
                    float ang=i*120f+19f+u*31f;
                    Vector2 outward=Axis(ang);
                    Vector2 centre=outward*(.82f-.42f*u);
                    Facet(b,centre,Axis(ang+71f-21f*u),.79f,.16f,i==1?Rose:Lilac,Plum,a);
                    b.Stroke(centre+Across(outward)*.22f,centre+outward*.22f,.039f,Ink(Heat,a));
                }
                b.Splinter(new Vector2(-.36f,0),new Vector2(.42f+.24f*u,0),.052f,Ink(Rose,a));
                return;
            }
            if (beat==Beat.Strike)
            {
                Caustic(b,u,a);
                b.Splinter(new Vector2(-.76f,0),new Vector2(1.29f+.80f*u,0),.55f*(1f-.48f*t),Ink(Plum,a*.41f));
                b.Splinter(new Vector2(-.49f,0),new Vector2(1.11f+.93f*u,0),.30f*(1f-.52f*t),Ink(Rose,a*.36f));
                for(int side=-1;side<=1;side+=2)
                {
                    Vector2 origin=new Vector2(-.28f,side*.07f);
                    Vector2 knee=new Vector2(.31f+.28f*u,side*(.19f+.27f*u));
                    Vector2 tip=new Vector2(1.3f+.98f*u,side*(.13f+.67f*u));
                    Vector2 recess=new Vector2(.63f+.27f*u,side*(.11f+.09f*u));
                    b.Triangle(origin,knee,recess,Ink(Plum,a*.76f));
                    b.Triangle(knee,tip,recess,Ink(side>0?Lilac:Rose,a*.76f));
                    b.Stroke(knee,tip,.049f,Ink(side>0?Heat:Lilac,a));
                    Vector2 ghost=Across(Axis(side*14f))*side*(.14f+.23f*u);
                    b.Splinter(knee+ghost,tip+ghost,.057f,Ink(side>0?Rose:Lilac,a*.70f));
                    b.Splinter(origin,new Vector2(-.66f-.42f*u,side*(.27f+.36f*u)),.08f,Ink(Lilac,a*.85f));
                }
                b.Splinter(new Vector2(-.44f,0),new Vector2(.93f+1.29f*u,0),.128f*(1f-.62f*t),Ink(Heat,a));
                return;
            }
            Caustic(b,u,a*.65f);
            for(int i=0;i<7;i++)
            {
                float ang=13f+i*(360f/7f);
                Vector2 radial=Axis(ang);
                Vector2 centre=radial*(.19f+u*(1.31f+.12f*(i%3)));
                centre.y-=.22f*t*t;
                Vector2 axis=Axis(ang+(i%2==0?1f:-1f)*(18f+83f*t));
                float span=.65f+.13f*(i%2);
                Facet(b,centre,axis,span,.13f+.05f*(i%3),i%2==0?Rose:Lilac,Plum,a);
                b.Stroke(centre-axis*.34f+Across(axis)*.018f,centre+Across(axis)*.11f,.039f,Ink(Heat,a));
                if (i%2==0) b.Splinter(radial*.16f,centre-radial*.19f,.05f*(1f-t),Ink(Lilac,a*.65f));
            }
            b.Arc(Vector2.zero,.33f+.69f*u,-21f-31f*t,72f,.06f*(1f-.65f*t),Ink(Rose,a),6);
        }
    }
}
