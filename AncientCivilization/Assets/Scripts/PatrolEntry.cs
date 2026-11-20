using System.Collections.Generic;
using Plugin.AncientCivilization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.PlayerCore
{
    public static class PatrolEntry
    {
        const string Scene = "PatrolExpedition";
        static int requested = 1;
        static bool resumeCampaign;
        static Dictionary<string, int> preview;
        public static bool Preview => preview != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            requested = 1; resumeCampaign = false; preview = null;
        }

        public static int Read(string key, int fallback=0)
        {
            if (!Preview) return AncientGame.Saved(key, fallback);
            return preview.TryGetValue(key, out int value) ? value : fallback;
        }
        public static void Write(string key, int value)
        {
            if (Preview) preview[key] = value;
            else AncientGame.SetSaved(key, value);
        }
        public static void BeginPreview()
        {
            if (!Preview) preview = new Dictionary<string,int>();
        }
        public static bool Request(int roomId, bool developer=false)
        {
            if (roomId < 1 || roomId > PatrolCatalog.Count || !Application.CanStreamedLevelBeLoaded(Scene)) return false;
            var game = AncientGame.Instance;
            resumeCampaign = game && game.HasSession && !game.MainMenu;
            if (resumeCampaign) game.Save();
            requested = roomId;
            preview = null;
            if (developer) BeginPreview();
            SceneManager.LoadScene(Scene);
            return true;
        }
        public static bool TravelCampaign(AncientDeveloperDestination destination)
        {
            return AncientDeveloperTravel.GoFromPatrol(destination,resumeCampaign);
        }
        public static int ConsumeRoom() {int id=requested; requested=1; return id;}
        public static void Return()
        {
            // 복귀 중 옛 방의 종료 콜백도 같은 임시 저장소를 본다.
            SceneManager.LoadScene("Main");
        }
        public static bool ConsumeReturn()
        {
            preview = null;
            bool resume = resumeCampaign; resumeCampaign = false; return resume;
        }
    }
}
