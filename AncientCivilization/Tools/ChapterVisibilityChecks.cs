using System;
using System.Collections.Generic;
using Plugin.AncientCivilization;

class ChapterVisibilityChecks
{
    static int checks;
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);checks++;}
    static int Count(int mask){int count=0;for(int id=0;id<20;id++)if((mask&(1<<id))!=0)count++;return count;}
    struct Anchor {public int slot,floor;public float x,y;}
    static void CheckNearest(Anchor[] anchors,int capacity,int floor,float cameraX,float cameraY,float halfWidth,float halfHeight,float margin)
    {
        var selection=new AncientAnchorLightSelection(capacity);var expected=new List<Anchor>();
        foreach(var anchor in anchors)
        {
            selection.Consider(anchor.slot,anchor.floor,floor,anchor.x,anchor.y,cameraX,cameraY,halfWidth,halfHeight,margin);
            if(anchor.slot>=0&&anchor.floor>=0&&AncientChapterVisibility.PointVisible(anchor.floor,floor,anchor.x,anchor.y,cameraX,cameraY,halfWidth,halfHeight,margin))expected.Add(anchor);
        }
        // The test oracle can sort its small independent copy; the actual runtime
        // selector maintains only a fixed-size heap and never sorts all world data.
        expected.Sort((a,b)=>
        {
            double ax=(double)a.x-cameraX,ay=(double)a.y-cameraY,bx=(double)b.x-cameraX,by=(double)b.y-cameraY;
            int comparison=(ax*ax+ay*ay).CompareTo(bx*bx+by*by);
            return comparison!=0?comparison:a.slot.CompareTo(b.slot);
        });
        int wanted=Math.Min(capacity,expected.Count);var actual=new HashSet<int>();
        Check(selection.Count==wanted&&selection.Count<=selection.Capacity,"The halo selector stays bounded even when thousands of world anchors fit the camera");
        for(int i=0;i<selection.Count;i++)Check(actual.Add(selection.SlotAt(i)),"Each selected anchor owns at most one pooled halo");
        for(int i=0;i<wanted;i++)Check(actual.Contains(expected[i].slot),"A crowded viewport retains the actual nearest visible anchors with deterministic tie handling");
        Check(selection.SlotAt(-1)==-1&&selection.SlotAt(selection.Count)==-1,"Unassigned pool slots cannot reuse a stale anchor index");
        selection.Begin();Check(selection.Count==0&&selection.SlotAt(0)==-1&&selection.Capacity==capacity,"A selection refresh releases old indices without growing the pool");
    }
    static void CheckAnchorPoolSelection()
    {
        var dense=new Anchor[4096];for(int i=0;i<dense.Length;i++)dense[i]=new Anchor{slot=i,floor=0,x=i%64-31.5f,y=i/64-31.5f};
        foreach(int capacity in new[]{1,2,8,64})CheckNearest(dense,capacity,0,0,0,40,40,0);
        var random=new Random(17287);
        for(int pass=0;pass<12;pass++)
        {
            for(int i=dense.Length-1;i>0;i--){int at=random.Next(i+1);var old=dense[i];dense[i]=dense[at];dense[at]=old;}
            CheckNearest(dense,64,0,(float)random.NextDouble()*12-6,(float)random.NextDouble()*12-6,10,8,4);
        }
        var highway=new[]{
            new Anchor{slot=0,floor=5,x=30001,y=11},new Anchor{slot=1,floor=5,x=30005,y=11},
            new Anchor{slot=2,floor=0,x=30002,y=11},new Anchor{slot=3,floor=5,x=30000,y=50},
            new Anchor{slot=4,floor=5,x=-100000,y=-100000},new Anchor{slot=5,floor=5,x=30060,y=11}
        };
        CheckNearest(highway,64,5,30000,4.6f,20,9.5f,4);
        var moving=new AncientAnchorLightSelection(1);moving.Consider(7,0,0,1,7.6f,0,5,20,10,4);
        Check(moving.SlotAt(0)==7,"A nearby moving crane is represented by its gameplay anchor index");
        moving.Begin();moving.Consider(7,0,0,-100000,-100000,0,5,20,10,4);moving.Consider(9,0,0,2,7.6f,0,5,20,10,4);
        Check(moving.Count==1&&moving.SlotAt(0)==9,"Refreshing current anchor positions drops a disabled latch and admits the newly visible moving hook");
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            moving.Begin();moving.Consider(1,0,0,bad,5,0,5,20,10,4);moving.Consider(2,0,0,1,bad,0,5,20,10,4);
            Check(moving.Count==0,"Invalid world positions never occupy a pooled light");
        }
        bool rejected=false;try{new AncientAnchorLightSelection(0);}catch(ArgumentOutOfRangeException){rejected=true;}
        Check(rejected,"A halo selector cannot be constructed with an unusable empty capacity");
    }
    static void Main()
    {
        for(int floor=0;floor<5;floor++)
        {
            int chapterMask=15<<(floor*4);
            for(int id=floor*4;id<floor*4+4;id++)foreach(float halfWidth in new[]{10f,20f,40f})
            {
                float centre=(AncientChapterLayout.CaseLeft(id)+AncientChapterLayout.CaseRight(id))*.5f;
                int visible=AncientChapterVisibility.VisibleCases(floor,centre,halfWidth);
                Check((visible&(1<<id))!=0,"The physical case is visible even when another case owns the objective");
                Check((visible&~chapterMask)==0,"A camera window cannot show another floor's cases");
                Check(Count(visible)==1,"A normal camera in a case centre does not render the whole chapter");
                Check(AncientChapterVisibility.SceneryVisible(floor,id,floor,visible),"Visible physical-case scenery survives forward handoff and backtracking");
            }
            for(int id=floor*4;id<floor*4+3;id++)foreach(float halfWidth in new[]{10f,20f,40f})
            {
                float bridge=(AncientChapterLayout.CaseRight(id)+AncientChapterLayout.CaseLeft(id+1))*.5f;
                int visible=AncientChapterVisibility.VisibleCases(floor,bridge,halfWidth);
                Check((visible&(1<<id))!=0&&(visible&(1<<(id+1)))!=0,"Both connected sections are prepared before crossing their bridge");
                Check(Count(visible)==2,"A normal bridge view does not activate distant chapter sections");
                Check(AncientChapterVisibility.SceneryVisible(floor,-1,floor,visible),"Bridge and introduction geometry remains visible without an active case ID");
            }
            // Walk a full chapter forward and back. Every case actually intersecting
            // the viewport must be available before Unity's mesh frustum test.
            foreach(float direction in new[]{1f,-1f})for(int step=0;step<=600;step++)
            {
                float start=AncientChapterLayout.FloorStart(floor),end=AncientChapterLayout.FloorEnd(floor);
                float x=direction>0?start+(end-start)*step/600f:end-(end-start)*step/600f;
                int visible=AncientChapterVisibility.VisibleCases(floor,x,20);
                Check((visible&~chapterMask)==0,"Walking either direction stays within the current chapter's visibility mask");
                for(int id=floor*4;id<floor*4+4;id++)
                    if(AncientChapterLayout.CaseLeft(id)<=x+20&&AncientChapterLayout.CaseRight(id)>=x-20)
                        Check((visible&(1<<id))!=0,"No visible ceiling or floor disappears while crossing a case boundary");
            }
            for(int other=0;other<5;other++)if(other!=floor)
            {
                Check(!AncientChapterVisibility.SceneryVisible(other,-1,floor,-1),"Other chapter bridge meshes remain hidden");
                Check(!AncientChapterVisibility.PointVisible(other,floor,100,5,100,5,20,8,12),"Off-floor gimmicks cannot gain collision through camera proximity");
            }
        }
        Check(AncientChapterVisibility.VisibleCases(5,24000,20)==0,"The final chase does not activate city case meshes");
        Check(AncientChapterVisibility.SceneryVisible(5,-1,5,0),"Final chase scenery still works without case IDs");
        Check(!AncientChapterVisibility.SceneryVisible(1,4,0,1<<4),"A stale case mask cannot bypass chapter separation");
        Check(!AncientChapterVisibility.SceneryVisible(0,20,0,-1),"Invalid room IDs cannot shift into a valid mask bit");
        Check(AncientChapterVisibility.PointVisible(2,2,132,5,100,5,20,8,12),"Gimmicks prewarm just beyond the camera edge");
        Check(!AncientChapterVisibility.PointVisible(2,2,132.1f,5,100,5,20,8,12),"Distant gimmicks remain inactive");
        Check(!AncientChapterVisibility.PointVisible(2,2,100,26,100,5,20,8,12),"Distant vertical gimmicks remain inactive");
        Check(AncientChapterVisibility.PointVisible(2,2,100,5,100,5,20,8,12),"A revisited completed-section gimmick can work without an active-case equality check");
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            Check(AncientChapterVisibility.VisibleCases(0,bad,20)==0,"Invalid camera coordinates fail closed");
            Check(AncientChapterVisibility.VisibleCases(0,100,bad)==0,"Invalid viewport sizes fail closed");
            Check(!AncientChapterVisibility.PointVisible(0,0,bad,5,100,5,20,8,12),"Invalid gimmick coordinates fail closed");
        }
        Check(AncientChapterVisibility.VisibleCases(-1,100,20)==0,"An invalid chapter cannot show city meshes");
        Check(AncientChapterVisibility.VisibleCases(0,100,-1)==0,"A negative viewport cannot show city meshes");
        CheckAnchorPoolSelection();
        Console.WriteLine("PASS: "+checks+" continuous-chapter scenery, bridge, gimmick visibility and bounded nearest-anchor halo checks");
    }
}
