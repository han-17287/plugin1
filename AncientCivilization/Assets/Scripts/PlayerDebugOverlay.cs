using UnityEngine;
namespace Plugin.PlayerCore
{
    public sealed class PlayerDebugOverlay : MonoBehaviour
    {
        public PlayerStateMachine target;public PlayerInputReader input;public SpriteRenderer bulb;
        [SerializeField] bool visible=true;
        GUIStyle style;PlayerModeController mode;Camera view;
        void Awake(){mode=GetComponent<PlayerModeController>();view=Camera.main;}
        void LateUpdate(){if(bulb&&mode)bulb.color=mode.ModeColor;}
        void OnGUI()
        {
            if(!visible||!target||!input)return;
            if(style==null)style=new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Fonts/NeoDunggeunmo"),fontSize=17,wordWrap=true};
            GUI.Box(new Rect(10,10,470,132),"");
            GUI.Label(new Rect(20,18,450,120),"상태: "+target.State+" / 모드: "+target.Mode+"\n속도: "+target.Velocity.ToString("F2")+" / 지면: "+target.Grounded+"\n남은 대시: "+target.RemainingDashes+" / 수집 홀드: "+input.CollectHeld+"\n입력: "+(input.Ready?"Input System":"설정 필요")+" / 일시정지: "+input.Paused,style);
            if(view)
            {
                Color old=GUI.color;GUI.color=mode.ModeColor;Vector2 p=input.Pointer;p.y=Screen.height-p.y;
                GUI.DrawTexture(new Rect(p.x-7,p.y-1,14,2),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(p.x-1,p.y-7,2,14),Texture2D.whiteTexture);GUI.color=old;
            }
        }
    }
}
