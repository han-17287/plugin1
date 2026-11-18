#if UNITY_EDITOR
using System;
using Plugin.PlayerCore;
using UnityEditor;
using UnityEngine;
namespace Plugin.AncientCivilization.EditorChecks
{
    // 연습 씬 Play 중 실행하는 검사다. 실제 키 입력과 조작감은 별도로 확인한다.
    public static class PlayerCoreSmokeChecks
    {
        [MenuItem("PLUGIN/P1/연습 씬 구성과 상태 검사")]
        static void Run()
        {
            if(!EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="PlayerMovementPractice")
            {Debug.LogError("PlayerMovementPractice 씬을 열고 Play한 뒤 실행하세요.");return;}
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerStateMachine>();
            if(!player)throw new InvalidOperationException("플레이어가 없습니다.");
            var body=player.GetComponent<Rigidbody2D>();var shape=player.GetComponent<CapsuleCollider2D>();var input=player.GetComponent<PlayerInputReader>();var mode=player.GetComponent<PlayerModeController>();
            Require(input.Ready,"신 Input System 입력 에셋 로드");Require(body.interpolation==RigidbodyInterpolation2D.Interpolate,"보간 설정");
            Require(body.collisionDetectionMode==CollisionDetectionMode2D.Continuous,"연속 충돌 설정");Require(shape.sharedMaterial&&shape.sharedMaterial.friction==0,"벽 걸림 방지 마찰 재질");
            Require(!UnityEngine.Object.FindAnyObjectByType<AncientGame>(),"기존 게임 자동 생성 차단");
            var point=(Vector2)player.transform.position;float scale=Time.timeScale;var original=mode.Mode;
            try
            {
                mode.Set(PlayerMode.Attack);Require(player.Mode==PlayerMode.Attack,"공격 모드 전달");mode.Set(PlayerMode.Hook);Require(player.Mode==PlayerMode.Hook,"갈고리 모드 전달");
                player.ReceiveHurt(new Vector2(-5,7),.3f);Require(player.State==PlayerStateId.Hurt&&player.Velocity.y==7,"피격 상태와 넉백");
                player.Die();Require(player.State==PlayerStateId.Dead&&player.Velocity==Vector2.zero,"사망 정지");
                Require(!player.BeginExternal(PlayerStateId.Attack),"사망 중 공격 진입 금지");
                player.Respawn(point);Require(player.State==PlayerStateId.Fall&&player.RemainingDashes==player.data.airDashes,"재배치 시 대시 초기화");
                Require(Time.timeScale==scale,"전역 시간 배율 유지");
            }
            finally{mode.Set(original);player.Respawn(point);}
            Debug.Log("PASS: P1 실물 구성/상태 검사. 점프·벽·하강·대시 충돌과 실제 입력은 체크리스트로 확인하세요.");
        }
        static void Require(bool valid,string name){if(!valid)throw new InvalidOperationException("P1 실패: "+name);}
    }
}
#endif
