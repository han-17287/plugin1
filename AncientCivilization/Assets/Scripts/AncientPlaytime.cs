using UnityEngine;
namespace Plugin.AncientCivilization
{
    public sealed class AncientPlaytime : MonoBehaviour
    {
        AncientGame game;readonly AncientPlaytimeRules measured=new AncientPlaytimeRules();int partialMask;float sinceSave;bool previousActive,suspended;
        public static readonly string[] ProgressKeys={"lowerTimeVersion","lowerTimeComplete","lowerTimePartial","lowerTimeTotal0","lowerTimeDialogue0","lowerTimeBoss0","lowerTimeRetry0","lowerTimeTotal1","lowerTimeDialogue1","lowerTimeBoss1","lowerTimeRetry1"};
        public void Initialize(AncientGame owner)
        {
            game=owner;int complete=AncientGame.Saved(ProgressKeys[1]);partialMask=AncientGame.Saved(ProgressKeys[2])&3;
            if(AncientGame.Saved(ProgressKeys[0])==0)
            {
                if(AncientGame.Saved("questStage")>0||AncientGame.Saved("storyCheckpoint")>0)partialMask|=1;
                if(AncientGame.Saved("cityFloor")>0||(AncientGame.Saved("campaignComplete")&240)!=0)partialMask|=2;
            }
            for(int f=0;f<2;f++)
            {int at=3+f*4;measured.Restore(f,AncientGame.Saved(ProgressKeys[at]),AncientGame.Saved(ProgressKeys[at+1]),AncientGame.Saved(ProgressKeys[at+2]),AncientGame.Saved(ProgressKeys[at+3]),(complete&(1<<f))!=0);}
            WriteSave();
        }
        public void WriteSave()
        {
            AncientGame.SetSaved(ProgressKeys[0],1);AncientGame.SetSaved(ProgressKeys[1],measured.CompletedMask);AncientGame.SetSaved(ProgressKeys[2],partialMask);
            for(int f=0;f<2;f++)
            {int at=3+f*4;AncientGame.SetSaved(ProgressKeys[at],measured.TotalMilliseconds(f));AncientGame.SetSaved(ProgressKeys[at+1],measured.DialogueMilliseconds(f));AncientGame.SetSaved(ProgressKeys[at+2],measured.BossMilliseconds(f));AncientGame.SetSaved(ProgressKeys[at+3],measured.Retries(f));}
        }
        public void Retry(){if(game&&game.Story&&!AncientGame.IsQAProfile)measured.Retry(game.Story.CurrentFloor);}
        static string Minutes(int ms){return (ms/60000f).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+"분";}
        public string Summary(int floor)
        {
            if(floor<0||floor>1)return "";
            return ((partialMask&(1<<floor))!=0?"이번 버전 이후 ":"플레이 기록 ")+Minutes(measured.TotalMilliseconds(floor))+" · 대화 "+Minutes(measured.DialogueMilliseconds(floor))+" · 보스 "+Minutes(measured.BossMilliseconds(floor))+" · 재도전 "+measured.Retries(floor)+(measured.Finished(floor)?" · 완료":"");
        }
        void Update()
        {
            if(!game||!game.Story||!game.HasSession||AncientGame.IsQAProfile){previousActive=false;return;}
            int floor=game.Story.CurrentFloor,previousComplete=measured.CompletedMask;
            if(floor>0&&AncientCampaignRules.FloorCount(game.Campaign.CompletedMask,0)==4)measured.Complete(0);
            if((game.City.Cleared&2)!=0&&!game.Story.DialogueOpen)measured.Complete(1);
            if(previousComplete!=measured.CompletedMask){WriteSave();PlayerPrefs.Save();}
            bool eligible=!suspended&&!game.MainMenu&&!game.skills&&Application.isFocused&&!(game.Journal&&game.Journal.Open)&&!game.Story.LiftBusy;
            // The first resumed frame can contain time spent suspended or in a
            // menu. Count only a delta whose previous frame was also eligible.
            bool active=eligible&&previousActive;previousActive=eligible;
            measured.Step(floor,Time.unscaledDeltaTime,active,game.Story.DialogueOpen,game.ArenaActive||game.Production&&game.Production.Active);
            if(active&&floor>=0&&floor<2&&!measured.Finished(floor))
            {sinceSave+=Time.unscaledDeltaTime;if(sinceSave>=30){sinceSave=0;WriteSave();PlayerPrefs.Save();}}
        }
        void OnApplicationFocus(bool focused){previousActive=false;}
        void OnApplicationPause(bool paused)
        {suspended=paused;previousActive=false;if(paused&&game&&game.HasSession){WriteSave();PlayerPrefs.Save();}}
    }
}
