using UnityEngine;
using Plugin.AncientCivilization;
namespace Plugin.PlayerCore
{
    // 기존 원화의 발 피벗을 캡슐 아래에 맞춘다. 판정 크기는 애니메이션과 분리한다.
    public sealed class DistrictPlayerPresentation : MonoBehaviour
    {
        PlayerStateMachine player;PlayerInputReader input;PlayerModeController mode;
        SpriteRenderer body;float clock;int facing=1;
        void Awake()
        {
            player=GetComponent<PlayerStateMachine>();input=GetComponent<PlayerInputReader>();mode=GetComponent<PlayerModeController>();
            body=AncientArt.Display("백 경위",AncientArt.Hero(0),Vector2.zero,12,transform);
            body.transform.localPosition=new Vector3(0,-.9f,0);
            float height=body.sprite.bounds.size.y;body.transform.localScale=Vector3.one*(1.95f/Mathf.Max(.1f,height));
        }
        void LateUpdate()
        {
            if(!player||!body)return;
            if(!input.Paused&&Application.isFocused)clock+=Time.deltaTime;
            if(Mathf.Abs(player.Velocity.x)>.2f)facing=player.Velocity.x<0?-1:1;
            int frame=player.Grounded?(Mathf.Abs(player.Velocity.x)>1?1+Mathf.FloorToInt(clock*10)%3:0):player.Velocity.y>0?2:3;
            body.sprite=AncientArt.Hero(frame);body.flipX=facing<0;
        }
        void OnGUI()
        {
            if(!input||!input.Ready||!mode)return;
            Vector2 p=input.Pointer;p.y=Screen.height-p.y;Color old=GUI.color;GUI.color=mode.ModeColor;
            GUI.DrawTexture(new Rect(p.x-6,p.y-1,12,2),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(p.x-1,p.y-6,2,12),Texture2D.whiteTexture);GUI.color=old;
        }
    }
}
