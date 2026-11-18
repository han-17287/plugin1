#if UNITY_EDITOR
using System;
using Plugin.PlayerCore;
using UnityEditor;
using UnityEngine;
namespace Plugin.AncientCivilization.EditorChecks
{
    // 실제 연습 씬 Play에서 세 앵커의 비행/도착을 기다리는 선택 검사다.
    public static class GrappleRoomSmokeChecks
    {
        static PlayerStateMachine player;static PlayerGrapple grapple;static TraversalRoom room;static GrappleAnchor target;
        static Vector2 original,from,captured;static PlayerMode originalMode;static int stage;static float started;
        [MenuItem("PLUGIN/P2/안전 흐름 방 앵커 실물 검사")]
        static void Run()
        {
            if(!EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="TraversalRhythmPractice")
            {Debug.LogError("TraversalRhythmPractice에서 안전 흐름 방을 Play한 뒤 실행하세요.");return;}
            if(player){Debug.LogError("이전 검사가 실행 중입니다.");return;}
            room=UnityEngine.Object.FindAnyObjectByType<TraversalRoom>();
            if(!room||room.precision){Debug.LogError("안전 흐름 방으로 전환하고 검사하세요.");return;}
            player=UnityEngine.Object.FindAnyObjectByType<PlayerStateMachine>();grapple=player.GetComponent<PlayerGrapple>();
            original=player.transform.position;var mode=player.GetComponent<PlayerModeController>();originalMode=mode.Mode;
            try
            {
                mode.Set(PlayerMode.Hook);player.Respawn(StartPoint());Physics2D.SyncTransforms();
                Require(grapple.TryStartAt(room.Anchors[0]),"고정 앵커 발사 요청");
                player.ReceiveHurt(new Vector2(-2,4),.2f);Require(!grapple.Active&&!room.Anchors[0].Leased&&player.State==PlayerStateId.Hurt,"피격이 돌진 예약을 해제하고 상태 우선순위를 유지");
                player.Die();player.FinishExternal(Vector2.up,true);Require(player.State==PlayerStateId.Dead,"늦은 도착 콜백이 사망을 취소하지 않음");
                stage=0;StartLeg();EditorApplication.update+=Poll;
            }
            catch(Exception e){Finish(false,e.Message);}
        }
        static void StartLeg()
        {
            grapple.ResetForRetry();room.ResetForRetry();
            target=room.Anchors[0];
            foreach(var item in room.Anchors)if(stage==1&&item is OneUseGrappleAnchor||stage==2&&item is MovingGrappleAnchor){target=item;break;}
            Vector2 spawn=stage==0?StartPoint():target.Point+Vector2.left*4+Vector2.up*2;
            player.Respawn(spawn);Physics2D.SyncTransforms();from=spawn;captured=target.Point;
            Require(grapple.TryStartAt(target),"검사 대상 앵커 발사");started=Time.realtimeSinceStartup;
        }
        static void Poll()
        {
            if(!EditorApplication.isPlaying||!player){Finish(false,"Play가 끝났습니다.");return;}
            try
            {
                if(Time.realtimeSinceStartup-started>5)throw new Exception("돌진 완료 시간 초과. Game 뷰 포커스와 입력 설정을 확인하세요.");
                if(grapple.Active)
                {
                    Vector2 delta=player.GetComponent<Rigidbody2D>().position-from,line=captured-from;
                    float perpendicular=Mathf.Abs(delta.x*line.y-delta.y*line.x)/Mathf.Max(.01f,line.magnitude);
                    Require(perpendicular<.15f,"목표까지 직선 경로 유지");
                    if(stage==2)Require((target.Point-captured).sqrMagnitude<.0001f,"움직이는 앵커 예약 동안 목표 고정");
                    return;
                }
                Require(grapple.ZipStrikeReady&&player.ArrivalJumpAvailable,"실제 도착 후 공격 창/공중 점프 준비");
                Require(player.RemainingDashes==player.data.airDashes,"실제 도착 후 대시 초기화");
                if(stage==1)Require(target.Spent&&!target.Available,"도착 성공 때 일회용 앵커 소모");
                if(++stage<3){StartLeg();return;}Finish(true,"고정·일회용·이동 앵커의 실제 직선 이동, 예약 해제, 도착 보상 확인");
            }
            catch(Exception e){Finish(false,e.Message);}
        }
        static void Finish(bool ok,string message)
        {
            EditorApplication.update-=Poll;
            if(grapple)grapple.ResetForRetry();if(room)room.ResetForRetry();
            if(player){player.Respawn(original);player.GetComponent<PlayerModeController>().Set(originalMode);}
            player=null;grapple=null;room=null;target=null;
            if(ok)Debug.Log("PASS: "+message);else Debug.LogError("P2 실물 검사 실패: "+message);
        }
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static Vector2 StartPoint(){if(room.District==null)return room.Checkpoint;var s=room.District.stops[0];return new Vector2(s.x,s.y+.02f);}
    }
}
#endif
