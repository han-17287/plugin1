using UnityEngine;
namespace Plugin.PlayerCore
{
    public sealed class TraversalPracticeHazard : MonoBehaviour
    {
        void OnTriggerStay2D(Collider2D other)
        {
            var player=other.GetComponent<PlayerStateMachine>();if(!player)return;
            var input=player.GetComponent<PlayerInputReader>();if(input.Paused||!input.Ready||!Application.isFocused)return;
            var plug=player.GetComponent<PlayerBluePlug>();
            // 연습장의 위험 레이어에서만 직선 견인 중 재시도를 유예한다.
            if(gameObject.layer==10&&plug&&plug.HazardInvulnerable)return;
            var lab=GetComponentInParent<TraversalRhythmPractice>();if(lab)lab.Retry();
        }
    }
}
