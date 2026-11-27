#if UNITY_EDITOR
using System;
using Plugin.PlayerCore;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plugin.AncientCivilization.EditorChecks
{
    public static class SummonedBladeSmokeChecks
    {
        const string Menu = "PLUGIN/전투/소환 블레이드 바닥과 장착 검사";

        [MenuItem(Menu, true)]
        static bool Available() => EditorApplication.isPlaying && !EditorApplication.isCompiling;

        [MenuItem(Menu)]
        static void Run()
        {
            // 본편과 같은 2D 물리 월드에서 멀리 떨어진 시험 바닥을 만들고, 가상 입자 지갑으로 실행한다.
            Vector2 origin = new Vector2(1024, 8192);
            var scene = new GameObject("검사 종료 시 회수할 블레이드 시험대");
            try
            {
                var floor = Part(scene.transform, "기준 바닥", origin + Vector2.down * .5f);
                floor.layer = 8;
                floor.AddComponent<BoxCollider2D>().size = new Vector2(42,1);
                var cam = Part(scene.transform, "검사용 카메라", origin + Vector2.up*3).AddComponent<Camera>();
                cam.transform.position += Vector3.back*10; cam.enabled=false;
                cam.orthographic=true; cam.orthographicSize=6; cam.aspect=16f/9;
                var actor = Part(scene.transform, "경위 몸통", origin+Vector2.up);
                var blade = actor.AddComponent<SummonedBlade>();
                int energy=70, hits=0, bursts=0, zips=0;
                float field=0, shield=0;
                blade.Configure(cam,()=>energy,n=>{if(energy<n)return false;energy-=n;return true;},
                    n=>energy+=n,(where,radius,damage,kind)=>{
                        if(kind==SummonedBladeHit.Impact)hits++;
                        if(kind==SummonedBladeHit.Zip)zips++;
                        if(kind==SummonedBladeHit.Overload)bursts++;
                        if(kind==SummonedBladeHit.Field)field+=damage;
                    },t=>shield=t, _=>{ });
                blade.FixedLandingForDebug = origin+Vector2.right*6;
                var wall = Part(scene.transform,"차폐문",origin+new Vector2(3,2.2f));
                wall.layer=8;wall.AddComponent<BoxCollider2D>().size=new Vector2(.55f,4.4f);
                Physics2D.SyncTransforms();
                Require(!blade.TrySummon() && energy==70,"차폐 경로에서 비용이 나감");
                wall.SetActive(false);
                var danger=Part(scene.transform,"위험 바닥",origin+new Vector2(6,.2f));
                danger.layer=10;danger.AddComponent<CircleCollider2D>().radius=.6f;
                Physics2D.SyncTransforms();
                Require(!blade.TrySummon() && energy==70,"위험 지형 승인");
                danger.SetActive(false); Physics2D.SyncTransforms();
                Require(blade.TrySummon() && energy==50 && !blade.TrySummon(),"소환 비용/단일 설치");
                ForState(blade,SummonedBladeState.Falling);
                var port=blade.Handle;
                ForState(blade,SummonedBladeState.Stuck);
                Require(blade.Handle==port && hits==1 && (port.Point-origin).y>1.39f,"낙하 충격과 안전 도착점");
                Vector2 old=port.Point;
                floor.transform.position+=Vector3.right;
                Physics2D.SyncTransforms();blade.Tick(.02f);
                Require(Mathf.Abs(port.Point.x-old.x-1)<.04f,"이동한 지지면과 소켓 불일치");
                actor.transform.position=port.Point;
                Require(blade.TryEquip(7) && zips==1 && !blade.Handle && shield==0,"견인 도착 장착");
                Require(blade.TryDetach() && blade.Supplies(actor.transform.position),"E 설치와 전원 공급");
                for (int i=0;i<151;i++) blade.Tick(.1f);
                Require(Mathf.Abs(field-120)<.1f && blade.State==SummonedBladeState.Stuck,"15초 장판 피해");
                Require(blade.TryEquip(.5f) && blade.TryOverload() && energy==0 && bursts==1 && !blade.TryOverload(),"입자 전부 사용한 방출");
                ForState(blade,SummonedBladeState.None);
                energy=30; floor.transform.position-=Vector3.right; actor.transform.position=origin+Vector2.up;
                Physics2D.SyncTransforms();
                blade.FixedLandingForDebug=origin+Vector2.left*6;
                Require(blade.TrySummon(),"두 번째 소환");
                ForState(blade,SummonedBladeState.Falling);
                actor.transform.position=blade.Handle.Point;
                Require(blade.TryEquip(5,true) && shield>=.5f && energy==18 && hits==1,"퍼펙트 장착 보상/낙하 충격 중복");
                blade.ResetEncounter();
                Require(!blade.Catchable && !blade.Equipped && !blade.Handle,"재시작 후 연결 정리");
                energy=25; actor.transform.position=origin+Vector2.up;
                Require(blade.TrySummon() && blade.ReservedParticles==20,"낙하 중 예약 입자");
                ForState(blade,SummonedBladeState.Falling);
                var pending=blade.LeaveRoom();
                Require(!pending.equipped && pending.cooldown==0 && energy==25 && blade.ReservedParticles==0,
                    "방 경계에서 중단된 소환 비용 보존");
                blade.LeaveRoom();
                Require(energy==25,"이중 환불 방지");
                blade.EnterRoom(new SummonedBladeCarry {equipped=true});
                Require(blade.Equipped && zips==3 && energy==25,"통과 시 장착 유지와 장착 일격 중복 방지");
                Require(blade.TryDetach(),"이동 후 설치 가능");
                var installed=blade.LeaveRoom();
                blade.EnterRoom(installed);
                Require(blade.State==SummonedBladeState.Cooldown && !blade.Handle && blade.Remaining==12,
                    "설치 후 이동은 쿨다운과 함께 입장");
                Debug.Log("소환 지면·벽·위험·움직이는 받침·장판·완전 장착·과부하 검사 통과");
            }
            finally
            {
                scene.SetActive(false);
                Object.Destroy(scene);
            }
        }

        static GameObject Part(Transform root,string label,Vector2 point)
        {
            var part=new GameObject(label);
            part.transform.SetParent(root,false); part.transform.position=point;
            return part;
        }
        static void ForState(SummonedBlade blade,SummonedBladeState expected)
        {
            for(int i=0;i<1800 && blade.State!=expected;i++) blade.Tick(.02f);
            Require(blade.State==expected,"전이 실패 "+expected);
        }
        static void Require(bool ok,string step) {if(!ok)throw new InvalidOperationException(step);}
    }
}
#endif
