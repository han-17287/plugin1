using System;
namespace Plugin.AncientCivilization
{
    public static class AncientLowerRouteRules
    {
        public const int CaseCount=8,StopsPerCase=9,StopsPerSector=3,RequiredPerSector=2,StopCount=CaseCount*StopsPerCase,Mask=(1<<StopsPerCase)-1;
        public static bool Supports(int mission){return mission>=0&&mission<CaseCount;}
        static readonly int[,] guardPairs={{0,1},{1,2},{0,5},{4,1},{0,1},{5,1},{4,0},{5,2}};
        public static int GuardType(int id,int slot)
        {
            if(id<0||id>=StopCount||slot<0||slot>1)return 0;
            return guardPairs[id/StopsPerCase,slot];
        }
        public static int Id(int mission,int sector,int stop)
        {return !Supports(mission)||sector<0||sector>2||stop<0||stop>=StopsPerSector?-1:mission*StopsPerCase+sector*StopsPerSector+stop;}
        public static void Location(int id,out float x,out float y,out float feet)
        {
            id=Math.Max(0,Math.Min(StopCount-1,id));int mission=id/StopsPerCase,sector=id%StopsPerCase/StopsPerSector,block=id%StopsPerSector;
            var support=AncientFieldExpansion.BlockSegment(mission,sector,block,5);
            x=AncientChapterLayout.CaseLeft(mission)+(support.left+support.right)*.5f;feet=support.top;
            y=AncientLowerRouteData.Stops[id].Action==AncientRouteAction.Wire?7.3f:feet+1.3f;
        }
        public static int Bit(int id){return id<0||id>=StopCount?0:1<<(id%StopsPerCase);}
        public static bool Done(int mask,int id){int bit=Bit(id);return mask>=0&&bit!=0&&(mask&bit)!=0;}
        public static int Mark(int mask,int id){return (Math.Max(0,mask)|Bit(id))&Mask;}
        public static int Next(int mission,int sector,int mask)
        {for(int stop=0;stop<StopsPerSector;stop++){int id=Id(mission,sector,stop);if(id>=0&&!Done(mask,id))return id;}return -1;}
        public static int OptionalOnReturn(int mission,int sector,int mask)
        {int id=Id(mission,sector,2);return id>=0&&SectorComplete(mission,sector,mask)&&!Done(mask,id)?id:-1;}
        public static bool SectorComplete(int mission,int sector,int mask)
        {
            if(!Supports(mission))return true;
            for(int stop=0;stop<RequiredPerSector;stop++)if(!Done(mask,Id(mission,sector,stop)))return false;
            return true;
        }
        // A version upgrade preserves earned old objectives without inventing visits to optional scenes.
        public static int Restore(int mission,int saved,int completed,int tasks)
        {
            if(!Supports(mission))return 0;int result=Math.Max(0,saved)&Mask;
            for(int sector=0;sector<3;sector++)
                if((completed&(1<<mission))!=0||(tasks&(1<<sector))!=0)
                    for(int stop=0;stop<RequiredPerSector;stop++)result=Mark(result,Id(mission,sector,stop));
            return result;
        }
        public static bool SegmentHit(float ax,float ay,float bx,float by,float px,float py,float radius)
        {
            if(!Finite(ax)||!Finite(ay)||!Finite(bx)||!Finite(by)||!Finite(px)||!Finite(py)||!Finite(radius)||radius<0)return false;
            float dx=bx-ax,dy=by-ay,length=dx*dx+dy*dy;
            if(float.IsInfinity(length))return false;
            float t=length>.0001f?Math.Max(0,Math.Min(1,((px-ax)*dx+(py-ay)*dy)/length)):0;
            float x=ax+t*dx-px,y=ay+t*dy-py;return x*x+y*y<=radius*radius;
        }
        static bool Finite(float x){return !float.IsNaN(x)&&!float.IsInfinity(x);}
    }
}
