using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Unscaled clock: the menu keeps simulation paused throughout the studio sequence.
    public sealed class AncientBootIntro : MonoBehaviour
    {
        static bool shown;
        public static bool Active {get;private set;}
        const float Lead=.3f,Slot=3.1f,Fade=.55f;
        float started;Texture2D logos;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession(){shown=false;Active=false;}
        void Awake()
        {
            var game=GetComponent<AncientGame>();
            if(shown||!game.MainMenu){enabled=false;return;}
            shown=true;Active=true;started=Time.unscaledTime;
            logos=Resources.Load<Texture2D>("Art/StudioLogos");
            if(logos){logos.filterMode=FilterMode.Bilinear;logos.wrapMode=TextureWrapMode.Clamp;}
        }
        void Update()
        {
            float elapsed=Time.unscaledTime-started;
            if(elapsed>=Lead+Slot*2+.2f||(elapsed>.4f&&(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Escape))))
            {Active=false;enabled=false;}
        }
        void OnGUI()
        {
            if(!Active)return;
            int depth=GUI.depth;Color color=GUI.color;Matrix4x4 matrix=GUI.matrix;
            GUI.depth=-10000;GUI.matrix=Matrix4x4.identity;GUI.color=Color.black;
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            float elapsed=Time.unscaledTime-started-Lead;
            if(logos&&elapsed>=0&&elapsed<Slot*2)
            {
                int index=elapsed<Slot?0:1;float local=elapsed-index*Slot;
                float alpha=Mathf.Clamp01(Mathf.Min(local/Fade,(Slot-local)/Fade));
                // Source regions include black gutters, so the two marks never bleed into one another.
                Rect crop=index==0?new Rect(90,270,720,440):new Rect(910,240,500,510);
                float fit=Mathf.Min(Screen.width*.68f/crop.width,Screen.height*.58f/crop.height);
                Rect target=new Rect((Screen.width-crop.width*fit)*.5f,(Screen.height-crop.height*fit)*.5f,crop.width*fit,crop.height*fit);
                GUI.color=new Color(1,1,1,alpha);
                GUI.DrawTextureWithTexCoords(target,logos,new Rect(crop.x/logos.width,1-crop.yMax/logos.height,crop.width/logos.width,crop.height/logos.height));
            }
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
        void OnDestroy(){if(enabled)Active=false;}
    }
}
