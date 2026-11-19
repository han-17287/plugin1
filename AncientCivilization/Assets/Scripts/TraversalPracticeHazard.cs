using UnityEngine;
namespace Plugin.PlayerCore
{
    public sealed class TraversalPracticeHazard : MonoBehaviour
    {
        void OnTriggerStay2D(Collider2D other)
        {
            var player=other.GetComponent<PlayerStateMachine>();if(!player)return;
            var input=player.GetComponent<PlayerInputReader>();if(input.Paused||!input.Ready||!Application.isFocused)return;
            var grapple=player.GetComponent<PlayerGrapple>();
            // 위험 레이어만 돌진 무적이다. 일반 적/보스 공격에는 이 검사를 쓰지 않는다.
            if(grapple&&grapple.data&&(grapple.data.hazards.value&(1<<gameObject.layer))!=0&&grapple.HazardInvulnerable)return;
            var lab=GetComponentInParent<TraversalRhythmPractice>();if(lab)lab.Retry();
        }
    }
}
