#if UNITY_EDITOR
using System;
using Plugin.PlayerCore;
using UnityEditor;
using UnityEngine;

namespace Plugin.AncientCivilization.EditorChecks
{
    public static class PatrolSocketRouteSmokeChecks
    {
        const string Menu = "PLUGIN/맵/벽 콘센트 순찰 50방 물리 검사";
        const int Ground = 1 << 8;
        static PatrolExpedition expedition;
        static int initialRoom, nextRoom, wallSeats, frame;
        static double started;
        const string Probe = "qa.socketRoute.crossing";
        static string saveRoot;
        static int savedClassic, savedSocket, savedTow, transferStage;
        static bool restoreCurrent;

        [MenuItem(Menu, true)]
        static bool Available() => EditorApplication.isPlaying && !EditorApplication.isCompiling && !expedition;

        [MenuItem(Menu)]
        static void Run()
        {
            var active = UnityEngine.Object.FindAnyObjectByType<PatrolExpedition>();
            if (!active || !active.SocketRoute || !PatrolEntry.Preview)
            {
                Debug.LogWarning("벽 콘센트 길에서 개발자 지도(0)를 열고 방을 선택한 뒤 검사하세요.");
                return;
            }
            // 개발자 지도 미리보기에서만 방을 순회한다. 한 프레임에 모든 맵의 텍스처를 중복 생성하지 않는다.
            expedition=active; initialRoom=active.RoomId; nextRoom=1; wallSeats=0;
            started=EditorApplication.timeSinceStartup; frame=-1; transferStage=0; restoreCurrent=false;
            saveRoot="patrol."+PatrolEntry.Read("patrol.generation")+".";
            savedClassic=PatrolEntry.Read(saveRoot+"flag."+Probe);
            savedSocket=PatrolEntry.Read(saveRoot+"wallRoute.flag."+Probe);
            savedTow=PatrolEntry.Read(saveRoot+"flag."+BluePlugSkillKeys.Tow);
            EditorApplication.update+=Step;
        }

        static void Step()
        {
            if (!EditorApplication.isPlaying || !expedition)
            {
                Stop();
                return;
            }
            try
            {
                Require(EditorApplication.timeSinceStartup-started<120,"시간 초과");
                if (Time.frameCount==frame) return;
                frame=Time.frameCount;
                if (nextRoom>PatrolSocketCatalog.Count)
                {
                    CheckStreetTransfer();
                    return;
                }
                int id=nextRoom;
                expedition.Travel(id);
                var room=UnityEngine.Object.FindAnyObjectByType<PatrolRoom>();
                Require(expedition.RoomId==id && expedition.SocketRoute && room && room.Title==PatrolSocketCatalog.Name(id),
                    id+"번 벽 콘센트 길 진입");
                Physics2D.SyncTransforms();
                Require(Mathf.Abs(expedition.Player.GetComponent<Rigidbody2D>().position.x-2)<.05f,id+"번 서측 입구");
                for(float x=1.5f; x<=46.5f; x+=.75f)
                {
                    // 점검로의 굴곡까지 검사한다. 기본 점프로 복귀 가능한 깊이에 실제 충돌 바닥이 있어야 한다.
                    var hit=Physics2D.Raycast(new Vector2(x,.12f),Vector2.down,1.7f,Ground);
                    Require(hit.collider && !hit.collider.isTrigger,id+"번 바닥 끊김 x="+x);
                }
                int seats=0;
                foreach(var socket in room.BluePlugSockets)
                {
                    if(!socket.CanWallRest) continue;
                    var face=socket.WallFace;
                    Require(face.gameObject.layer==8 && !face.isTrigger && !face.usedByEffector,id+"번 벽 손잡이 설치");
                    // 실제 Unity 변환 이후 핀이 벽에 박히거나 손잡이가 공중에 뜬 경우를 잡는다.
                    Vector2 gap=socket.Point-face.ClosestPoint(socket.Point);
                    Require(gap.x*socket.WallOutwardSign>.09f && gap.x*socket.WallOutwardSign<.34f &&
                        Mathf.Abs(gap.y)<.08f,id+"번 소켓 측면 오프셋");
                    seats++;
                }
                Require(seats>0,id+"번 사용 가능한 물리 고정점");
                wallSeats+=seats;
                expedition.Travel(id,true);
                Require(Mathf.Abs(expedition.Player.GetComponent<Rigidbody2D>().position.x-46)<.05f,id+"번 동측 입구");
                nextRoom++;
            }
            catch(Exception failure)
            {
                Debug.LogError("벽 콘센트 순찰 물리 검사 · "+failure);
                Stop();
            }
        }

        static void CheckStreetTransfer()
        {
            // 교차로의 실제 전환 메서드를 서로 다른 프레임에 사용한다. 검사 키와 기술 상태는 미리보기에만 쓴다.
            int id=PatrolSocketCatalog.Count;
            if(transferStage==0)
            {
                PatrolEntry.Write(saveRoot+"flag."+Probe,0);
                expedition.SetFlag(Probe,true);
                expedition.SetFlag(BluePlugSkillKeys.Tow,savedTow==0);
                expedition.SwitchStreet();
                Require(!expedition.SocketRoute && expedition.RoomId==id,"교차로에서 일반 순찰로 이동");
                transferStage=1;
                return;
            }
            if(transferStage==1)
            {
                Require(!expedition.Flag(Probe),"기존 순찰 경로에 새 지름길 기록이 섞임");
                Require(expedition.Flag(BluePlugSkillKeys.Tow)==(savedTow==0),"교차로에서 이동 기술 소실");
                expedition.SwitchStreet();
                Require(expedition.SocketRoute && expedition.RoomId==id && expedition.Flag(Probe),"원래 경로로 복귀 및 기록 복원");
                Debug.Log($"벽 콘센트 길 50방 양방향 입구 · 바닥 · 고정점 {wallSeats}개 · 교차로 기술/저장 검사 통과");
                Stop();
            }
        }

        static void Require(bool condition,string error)
        {
            if(!condition) throw new InvalidOperationException(error);
        }

        static void Stop()
        {
            EditorApplication.update-=Step;
            var previous=expedition;
            expedition=null;
            if(!EditorApplication.isPlaying || !previous || !PatrolEntry.Preview) return;
            PatrolEntry.Write(saveRoot+"flag."+Probe,savedClassic);
            PatrolEntry.Write(saveRoot+"wallRoute.flag."+Probe,savedSocket);
            PatrolEntry.Write(saveRoot+"flag."+BluePlugSkillKeys.Tow,savedTow);
            if(previous.SocketRoute) previous.Travel(initialRoom);
            else
            {
                // 실패가 전환 프레임에 발생해도 같은 프레임 입력 차단이 해제된 뒤 원래 길로 복구한다.
                expedition=previous; restoreCurrent=true;
                EditorApplication.update+=Restore;
            }
        }

        static void Restore()
        {
            if(EditorApplication.isPlaying && expedition && PatrolEntry.Preview && restoreCurrent)
            {
                if(!expedition.SocketRoute) expedition.SwitchStreet();
                if(!expedition.SocketRoute) return;
                expedition.Travel(initialRoom);
            }
            expedition=null;
            restoreCurrent=false;
            EditorApplication.update-=Restore;
        }
    }
}
#endif
