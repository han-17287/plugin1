using System;
namespace Plugin.AncientCivilization
{
    // Combat timing and the final clash are pure rules, independent of frame rate and Unity input.
    public static class AncientSniperRules
    {
        public const float AimSeconds=2f,ReloadSeconds=2.4f,ClashSeconds=8f;
        public const int MagazineSize=5,ClashRequiredTaps=8;
        public const float ClashStart=.4f,ClashDrainPerSecond=.065f,ClashTapGain=.055f;
        public static int Phase(float healthRatio)
        {
            if(float.IsNaN(healthRatio))return 1;
            return healthRatio>.75f?1:healthRatio>.5f?2:healthRatio>.25f?3:4;
        }
        // Caller must limit a damage burst at phase thresholds so a large laser cannot skip an encounter.
        public static float HealthFloor(int phase)
        {return phase<=1?.75f:phase==2?.5f:phase==3?.25f:0;}
        public static int FloorCell(float x,float left,int cellCount,float cellWidth)
        {
            if(cellCount<=0||cellWidth<=0||float.IsNaN(cellWidth)||float.IsInfinity(cellWidth)||float.IsNaN(x)||float.IsInfinity(x)||float.IsNaN(left)||float.IsInfinity(left))return -1;
            double cell=Math.Floor(((double)x-left)/cellWidth);
            return cell<0?0:cell>=cellCount?cellCount-1:(int)cell;
        }
    }
    public enum AncientSniperClashResult { Running,Won,Lost }
    public struct AncientSniperClash
    {
        public float Progress {get;private set;}
        public float Elapsed {get;private set;}
        public int SpaceTaps {get;private set;}
        public int MouseTaps {get;private set;}
        public AncientSniperClashResult Result {get;private set;}
        bool spaceWasHeld,mouseWasHeld;
        // Reset is required for a fresh attempt, including every retry after recollecting particles.
        public void Reset()
        {Progress=AncientSniperRules.ClashStart;Elapsed=0;SpaceTaps=MouseTaps=0;Result=AncientSniperClashResult.Running;spaceWasHeld=mouseWasHeld=false;}
        // Use this overload with GetKey/GetMouseButton. A held button contributes once, never per frame.
        public void Poll(float seconds,bool spaceHeld,bool mouseHeld)
        {
            if(!ValidTime(seconds)||Result!=AncientSniperClashResult.Running)return;
            bool spacePressed=spaceHeld&&!spaceWasHeld,mousePressed=mouseHeld&&!mouseWasHeld;
            spaceWasHeld=spaceHeld;mouseWasHeld=mouseHeld;
            Tick(seconds,spacePressed,mousePressed);
        }
        // Use this overload only with fresh press edges (GetKeyDown/GetMouseButtonDown).
        public void Tick(float seconds,bool spacePressed,bool mousePressed)
        {
            if(Result!=AncientSniperClashResult.Running||!ValidTime(seconds))return;
            float step=Math.Min(seconds,Math.Max(0,AncientSniperRules.ClashSeconds-Elapsed));
            Progress=Math.Max(0,Progress-AncientSniperRules.ClashDrainPerSecond*step);Elapsed+=step;
            // Inputs sampled at or after the deadline cannot rescue an expired attempt.
            if(Elapsed>=AncientSniperRules.ClashSeconds){Result=AncientSniperClashResult.Lost;return;}
            if(spacePressed){SpaceTaps++;Progress+=AncientSniperRules.ClashTapGain;}
            if(mousePressed){MouseTaps++;Progress+=AncientSniperRules.ClashTapGain;}
            Progress=Math.Min(1,Progress);
            if(Progress>=1&&SpaceTaps>=AncientSniperRules.ClashRequiredTaps&&MouseTaps>=AncientSniperRules.ClashRequiredTaps)
                Result=AncientSniperClashResult.Won;
        }
        static bool ValidTime(float seconds)
        {return seconds>0&&!float.IsNaN(seconds)&&!float.IsInfinity(seconds);}
    }
}
