#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Plugin.PlayerCore;
namespace Plugin.AncientCivilization.EditorChecks
{
    // 실제 Play에서 선택한 지역의 전체 앵커 경로를 물리 틱으로 따라간다.
    public static class DistrictRouteSmokeChecks
    {
        static TraversalRoom room;static PlayerStateMachine player;static PlayerGrapple grapple;
        static Vector2 original,from,endpoint;static PlayerMode originalMode;static int index;static float deadline;
        [MenuItem("PLUGIN/맵/현재 지역 전체 앵커 경로 실물 검사")]
        static void Run()
        {
            if(!EditorApplication.isPlaying||player){Debug.LogError("지역 맵을 Play한 뒤, 다른 검사가 끝나면 실행하세요.");return;}
            room=UnityEngine.Object.FindAnyObjectByType<TraversalRoom>();
            if(!room||room.District==null){Debug.LogError("메인 메뉴에서 새 지역 맵 둘러보기를 여세요.");return;}
            var actor=UnityEngine.Object.FindAnyObjectByType<PlayerStateMachine>();
            if(!actor||actor.GetComponent<PlayerInputReader>().Paused){Debug.LogError("지역 설정을 닫고 일시정지를 풀어 주세요.");return;}
            player=actor;grapple=player.GetComponent<PlayerGrapple>();original=player.transform.position;originalMode=player.Mode;
            try
            {
                grapple.ResetForRetry();room.ResetForRetry();player.GetComponent<PlayerModeController>().Set(PlayerMode.Hook);
                var start=room.District.stops[0];player.Respawn(new Vector2(start.x,start.y+.02f));Physics2D.SyncTransforms();
                index=0;Launch();EditorApplication.update+=Poll;
            }
            catch(Exception error){Finish(false,error.Message);}
        }
        static void Launch()
        {
            from=player.GetComponent<Rigidbody2D>().position;endpoint=room.Anchors[index].Point;
            if(!grapple.TryStartAt(room.Anchors[index]))throw new Exception("발사 거부 / 링크 "+index);
            deadline=Time.realtimeSinceStartup+5;
        }
        static void Poll()
        {
            if(!EditorApplication.isPlaying||!room||!player||!grapple){Finish(false,"Play 또는 검사 대상이 해제되었습니다.");return;}
            try
            {
                if(Time.realtimeSinceStartup>deadline)throw new Exception("진행 시간 초과. Game 뷰에 포커스를 두세요.");
                if(grapple.Active)
                {
                    Vector2 delta=player.GetComponent<Rigidbody2D>().position-from,line=endpoint-from;
                    if(Mathf.Abs(delta.x*line.y-delta.y*line.x)/Mathf.Max(.01f,line.magnitude)>.15f)throw new Exception("직선 경로 이탈 / 링크 "+index);
                    return;
                }
                if(!grapple.ZipStrikeReady)throw new Exception("실제 도착 실패 / 링크 "+index);
                if(++index<room.District.hooks.Length){Launch();return;}
                Finish(true,"선택한 지역의 전체 앵커 연결을 실제 Rigidbody2D로 통과했습니다. 걷기·선택 경로·전투·FPS 검사는 별도입니다.");
            }
            catch(Exception error){Finish(false,error.Message);}
        }
        static void Finish(bool ok,string detail)
        {
            EditorApplication.update-=Poll;
            if(grapple)grapple.ResetForRetry();if(room)room.ResetForRetry();
            if(player){player.Respawn(original);player.GetComponent<PlayerModeController>().Set(originalMode);}
            player=null;room=null;grapple=null;
            if(ok)Debug.Log("PASS: "+detail);else Debug.LogError("지역 경로 검사: "+detail);
        }
    }
}
#endif
