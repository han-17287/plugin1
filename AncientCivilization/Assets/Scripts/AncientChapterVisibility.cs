using System;

namespace Plugin.AncientCivilization
{
    // Visibility follows the physical camera, not the case currently owning objectives.
    // Prewarming the next section also keeps bridge crossings and backtracking seamless.
    public static class AncientChapterVisibility
    {
        public const float SceneryMargin=40,GimmickMargin=12;
        public static int VisibleCases(int floor,float cameraX,float halfWidth)
        {
            if(floor<0||floor>=AncientChapterLayout.FloorCount||!Finite(cameraX)||!Finite(halfWidth)||halfWidth<0)return 0;
            float left=cameraX-halfWidth-SceneryMargin,right=cameraX+halfWidth+SceneryMargin;int mask=0;
            for(int id=floor*AncientChapterLayout.CasesPerFloor;id<(floor+1)*AncientChapterLayout.CasesPerFloor;id++)
                if(AncientChapterLayout.CaseLeft(id)<=right&&AncientChapterLayout.CaseRight(id)>=left)mask|=1<<id;
            return mask;
        }
        public static bool SceneryVisible(int objectFloor,int room,int currentFloor,int visibleCases)
        {
            if(objectFloor<0)return true;
            if(objectFloor!=currentFloor)return false;
            return room<0||room<AncientChapterLayout.CaseCount&&room/AncientChapterLayout.CasesPerFloor==currentFloor&&(visibleCases&(1<<room))!=0;
        }
        public static bool PointVisible(int objectFloor,int currentFloor,float x,float y,float cameraX,float cameraY,float halfWidth,float halfHeight,float margin)
        {
            if(objectFloor!=currentFloor||!Finite(x)||!Finite(y)||!Finite(cameraX)||!Finite(cameraY)||
                !Finite(halfWidth)||!Finite(halfHeight)||!Finite(margin)||halfWidth<0||halfHeight<0||margin<0)return false;
            return Math.Abs(x-cameraX)<=halfWidth+margin&&Math.Abs(y-cameraY)<=halfHeight+margin;
        }
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}
