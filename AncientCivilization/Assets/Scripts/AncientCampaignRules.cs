using System;
namespace Plugin.AncientCivilization
{
    public static class AncientCampaignRules
    {
        public const int MissionCount=20,TasksMask=7;
        public const float Origin=1600,RoomStride=144,RoomWidth=132;
        public static int Witness(int mission){return mission==2?4:mission==5?6:mission==11?14:mission==14?18:6;}
        public static int WalkRow(int citizen){return citizen==4?0:citizen==6?1:citizen==14?2:citizen==18?3:-1;}
        public static int SanitizeMask(int mask){return mask&((1<<MissionCount)-1);}
        public static bool Complete(int mask,int id){return id>=0&&id<MissionCount&&(mask&(1<<id))!=0;}
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
        public static float Left(int id){return Origin+Math.Max(0,Math.Min(MissionCount-1,id))*RoomStride;}
        public static int RoomAt(float x)
        {
            if(float.IsNaN(x)||x<Origin||x>=Origin+MissionCount*RoomStride)return -1;
            int id=(int)((x-Origin)/RoomStride);return x<=Left(id)+RoomWidth?id:-1;
        }
        public static int Objective(int mask,int index){return index<0||index>2?mask&TasksMask:(mask|(1<<index))&TasksMask;}
        public static bool UseLaterDialogue(bool progressed,bool hasJokerClue,bool heardBefore)
        {return progressed&&(!hasJokerClue||heardBefore);}
        public static bool RepeatableEnergyNode(int kind,int tasks,int node)
        {return kind==3&&node==0&&(tasks&4)==0;}
        public static bool AllTasks(int mask){return (mask&TasksMask)==TasksMask;}
    }
}
