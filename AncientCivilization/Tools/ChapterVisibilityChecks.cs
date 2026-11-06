using System;
using Plugin.AncientCivilization;

class ChapterVisibilityChecks
{
    static int checks;
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);checks++;}
    static int Count(int mask){int count=0;for(int id=0;id<20;id++)if((mask&(1<<id))!=0)count++;return count;}
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
        Console.WriteLine("PASS: "+checks+" continuous-chapter scenery, bridge and gimmick visibility checks");
    }
}
