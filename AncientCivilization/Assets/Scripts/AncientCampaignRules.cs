using System;
namespace Plugin.AncientCivilization
{
    public static class AncientCampaignRules
    {
        public const int MissionCount=20,TasksMask=7,RouteVersion=2;
        public const float Origin=128,RoomStride=AncientFieldExpansion.Stride,RoomWidth=AncientFieldExpansion.Width;
        public static int Witness(int mission){return mission==2?4:mission==5?6:mission==11?14:mission==14?18:6;}
        public static int WalkRow(int citizen){return citizen==4?0:citizen==6?1:citizen==14?2:citizen==18?3:-1;}
        public static int SanitizeMask(int mask){return mask&((1<<MissionCount)-1);}
        public static bool Complete(int mask,int id){return id>=0&&id<MissionCount&&(mask&(1<<id))!=0;}
        public static int RestoreChapters(int saved,int completed,int previousActive)
        {
            int result=saved&31;
            for(int floor=0;floor<5;floor++)if(FloorCount(completed,floor)>0)result|=1<<floor;
            if(previousActive>=0&&previousActive<MissionCount)result|=1<<(previousActive/4);
            return result;
        }
        public static int ForwardCase(int completed,int current,int stage,int cleared)
        {
            if(current<0||current>=MissionCount||current%4==3)return -1;
            int next=current+1;return CanStart(completed,next,next/4,stage,cleared)?next:-1;
        }
        public static int ResumeCase(int completed,int chapters,int preferred,int floor,int stage,int cleared)
        {
            if(floor<0||floor>4)return -1;
            if((chapters&(1<<floor))==0&&(preferred<0||preferred>=MissionCount||preferred/4!=floor))return -1;
            int next=Next(completed,floor);return next>=0&&CanStart(completed,next,floor,stage,cleared)?next:-1;
        }
        public static int Claim(int mask,int id){return id<0||id>=MissionCount?SanitizeMask(mask):SanitizeMask(mask)|(1<<id);}
        public static int FloorCount(int mask,int floor)
        {if(floor<0||floor>4)return 0;int count=0;for(int i=floor*4;i<floor*4+4;i++)if(Complete(mask,i))count++;return count;}
        public static int Next(int mask,int floor)
        {if(floor<0||floor>4)return -1;for(int i=floor*4;i<floor*4+4;i++)if(!Complete(mask,i))return i;return -1;}
        public static bool ReadyForBoss(int mask,int floor)
        {if(floor<1||floor>4)return false;int required=floor==4?3:4;for(int i=0;i<required;i++)if(!Complete(mask,floor*4+i))return false;return true;}
        public static bool CanStart(int mask,int id,int floor,int stage,int cleared)
        {
            if(id<0||id>=MissionCount||id/4!=floor||Next(mask,floor)!=id)return false;
            if(stage<(floor==0?8:9))return false;
            return id!=19||(cleared&(1<<4))!=0;
        }
        public static float Left(int id){return AncientChapterLayout.CaseLeft(id);}
        public static int RoomAt(float x)
        {
            int floor=AncientChapterLayout.FloorAt(x);if(floor<0)return -1;
            float local=x-AncientChapterLayout.CaseLeft(floor*4);if(local<0)return -1;
            int index=(int)(local/RoomStride);if(index>3)return -1;
            int id=floor*4+index;return x<=Left(id)+RoomWidth?id:-1;
        }
        public static int Objective(int mask,int index){return index<0||index>2?mask&TasksMask:(mask|(1<<index))&TasksMask;}
        public static bool UseLaterDialogue(bool progressed,bool hasJokerClue,bool heardBefore)
        {return progressed&&(!hasJokerClue||heardBefore);}
        public static bool RepeatableEnergyNode(int kind,int tasks,int node)
        {return kind==3&&node==0&&(tasks&4)==0;}
        public static bool AllTasks(int mask){return (mask&TasksMask)==TasksMask;}
    }
}
