using UnityEngine;

namespace Plugin.AncientCivilization
{
    // A moving, nonblocking radio bubble. Ordinary conversations and menus take precedence.
    public sealed class AncientRouteRadio : MonoBehaviour
    {
        AncientGame game;AncientSpeechBubble bubble;
        readonly AncientRouteRadioPlayback playback=new AncientRouteRadioPlayback();
        float pageWidth=-1,pageHeight=-1;
        static readonly Color Accent=new Color(.1f,.4f,.5f);
        public void Initialize(AncientGame owner){game=owner;}
        public void Show(string speaker,string text){playback.Show(speaker,text);}
        bool CanRun
        {
            get{return game&&game.HasSession&&game.hero&&game.View&&!game.MainMenu&&!game.Paused&&!game.won&&
                !(game.Journal&&game.Journal.Open)&&!(game.Finale&&game.Finale.Cinematic)&&!(game.Story&&game.Story.DialogueOpen);}
        }
        void Update(){playback.Tick(Time.unscaledDeltaTime,CanRun);}
        void OnGUI()
        {
            if(!CanRun||!playback.Active||Event.current.type!=EventType.Repaint)return;
            if(bubble==null)bubble=new AncientSpeechBubble();
            Vector2 player=game.hero.transform.position;
            var frame=bubble.Measure(game,playback.Speaker,playback.Text,playback.PageStart,
                player+Vector2.up*1.15f,player-Vector2.up*1.05f,.9f,interactive:false,radio:true);
            if(!playback.PageReady||Mathf.Abs(pageWidth-frame.Body.width)>.5f||Mathf.Abs(pageHeight-frame.Body.height)>.5f)
            {
                playback.SetPageEnd(bubble.PageEnd(playback.Text,playback.PageStart,frame));
                pageWidth=frame.Body.width;pageHeight=frame.Body.height;
            }
            bubble.Draw(frame,playback.Speaker,playback.VisibleText,Accent,playback.Complete,ready:false,displayDepth:-20);
        }
        void OnDestroy(){if(bubble!=null)bubble.Dispose();}
    }
}
