using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        int room;
        Color Accent => Color.white;

        static PatrolRoom OpenSocketRoom(int id, PatrolRoom previous = null)
        {
            var r = new PatrolRoom { room=id, socketRoute=true };
            if (previous != null)
            {
                foreach (var entry in previous.flags) r.flags.Add(entry.Key, entry.Value);
                r.Charge = previous.Charge;
            }
            // 실제 방과 같은 크기의 바닥과 천장, 플레이어는 시작 시 두 이동 기술 모두 없다.
            r.BuildSocketPaving(id);
            r.Box("밀폐 천장",24,16.6f,52,1.2f,Color.white,true);
            PlugDistrictArt.Surfaces.Clear();
            r.BuildSocketStreet(id);
            if (!r.socketRoute) throw new InvalidOperationException("벽 콘센트 경로가 비활성 상태입니다.");
            return r;
        }

        void Seat(string label)
        {
            var socket = ObjectNamed(label).GetComponent<BluePlugSocket>();
            Check(socket && socket.Operational, "투척이 실제로 가능한 단자: " + label);
            bluePlug.Report(socket);
        }
        void ReleaseSeat() => bluePlug.Report(null);
        void TouchRest(string label)
        {
            var socket = ObjectNamed(label).GetComponent<BluePlugSocket>();
            Check(socket && socket.CanWallRest, "물리 벽에 연결된 고정점: " + label);
            bluePlug.RestingSocket = socket;
            Grounded = false;
        }
        void DropRest() => bluePlug.RestingSocket = null;

        static void CheckSocketRoutes()
        {
            int[] prototypes = { 1,11,21,31,42 };
            var names = new HashSet<string>();
            int total = 0;
            for (int id = 1; id <= PatrolSocketCatalog.Count; id++)
            {
                string name=PatrolSocketCatalog.Name(id);
                Check(!string.IsNullOrWhiteSpace(name) && names.Add(name), "다른 방과 구분되는 동선 이름: " + id);
                Check(!string.IsNullOrWhiteSpace(PatrolSocketCatalog.Hint(id)), "현장에 표시할 경로 설명: " + id);
                Check(PatrolSocketCatalog.IsPrototype(id)==prototypes.Contains(id), "최초 비교 방 지정: " + id);
                var r=OpenSocketRoom(id);
                var rests=r.BluePlugSockets.Where(s=>s.CanWallRest).ToArray();
                Check(rests.Length>0, "실제로 버틸 수 있는 벽 콘센트: " + id);
                Check(r.BluePlugSockets.Distinct().Count()==r.BluePlugSockets.Length, "방 안 중복 소켓 등록: "+id);
                total+=rests.Length;
                bool lowRouteOpen=false;
                for (int sample=0;sample<4;sample++)
                {
                    if (sample>0) r.Tick(67);
                    if (!lowRouteOpen) lowRouteOpen = new WalkingEnvelope(r, 1).Traversable() && new WalkingEnvelope(r, -1).Traversable();
                    foreach (var s in rests)
                    {
                        if (!s.gameObject.activeSelf || !s.WallFace.enabled) continue;
                        var face=s.WallFace;
                        // 소켓 측면에 캡슐이 설 수 있고 투척 팁이 솔리드 속에 묻히지 않는다.
                        var delta=s.Point-(Vector2)face.transform.position-face.offset;
                        float clearance=delta.x*s.WallOutwardSign-face.size.x*.5f;
                        Check(face.gameObject.layer==8 && !face.usedByEffector && face.size.y>=.5f, "솔리드 세로 면: "+id);
                        Check(clearance>=.09f && clearance<=.4f && Math.Abs(delta.y)<face.size.y*.51f,
                            "소켓과 고정 면 위치·방향: "+id+" "+s.Point.x+","+s.Point.y);
                    }
                    Check(r.ClearAt(new Vector2(2,1.1f),.36f,.89f,true) &&
                        r.ClearAt(new Vector2(46,1.1f),.36f,.89f,true), "양쪽 입구의 캡슐 여유: "+id);
                }
                Check(lowRouteOpen, "전원·견인·스윙 없이 보행·점프로 왕복할 수 있는 시간대: "+id);
            }
            Check(names.Count==50, "다섯 층 × 열 개 경로");
            var factory=OpenSocketRoom(1);
            Check(!factory.Flag(BluePlugSkillKeys.Tow) && !factory.Flag(BluePlugSkillKeys.Swing),"입장 때 이동 기술을 자동 해금하지 않는다");
            factory.UseAction("직선 견인 학습 · 첫 정비소");
            var living=OpenSocketRoom(11, factory);
            Check(living.Flag(BluePlugSkillKeys.Tow) && !living.Flag(BluePlugSkillKeys.Swing),"생산층에서 배운 견인을 생활층까지 보존");
            living.UseAction("진자 스윙 학습 · 교정대");
            Check(living.Flag(BluePlugSkillKeys.Swing),"생활층 샛길에서 스윙 해금");
            var newArrival=OpenSocketRoom(11);
            newArrival.UseAction("진자 스윙 학습 · 교정대");
            Check(!newArrival.Flag(BluePlugSkillKeys.Swing),"견인 선행 없이 스윙 단계를 건너뛸 수 없다");
            newArrival.UseAction("직선 견인 학습 · 첫 정비소");
            newArrival.UseAction("진자 스윙 학습 · 교정대");
            Check(newArrival.Flag(BluePlugSkillKeys.Tow) && newArrival.Flag(BluePlugSkillKeys.Swing),"11번부터 온 플레이어도 두 가지 경로를 열 수 있다");
            CheckLowerSocketRooms();
            CheckAdminSocketRooms();
            CheckResidenceSocketRooms();
            CheckSummitSocketRooms();
            Console.WriteLine($"PASS: socket routes 1–50 built from runtime room scripts ({total} physical wall rest positions).");
        }
    }
}
