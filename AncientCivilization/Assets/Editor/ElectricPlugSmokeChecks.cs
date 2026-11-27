#if UNITY_EDITOR
using System;
using Plugin.PlayerCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.AncientCivilization.EditorChecks
{
    public static class ElectricPlugSmokeChecks
    {
        const string Menu="PLUGIN/전투/플러그 감전·환경 검사";
        [MenuItem(Menu,true)]
        static bool Available() => EditorApplication.isPlaying && !EditorApplication.isCompiling;

        [MenuItem(Menu)]
        static void Run()
        {
            // 시험 중 현재 맵의 전선 뱀과 폭탄 시계를 앞당기지 않도록 레지스트리를 별도 씬에 격리한다.
            var scene=SceneManager.CreateScene("ElectricPlug_"+Guid.NewGuid().ToString("N"));
            var lab=new GameObject("전기 정비 시험대");
            SceneManager.MoveGameObjectToScene(lab,scene);
            lab.transform.position=new Vector2(4096,4032);
            var hero=new GameObject("시험용 경위");
            hero.transform.SetParent(lab.transform,false);
            var data=Resources.Load<BluePlugData>("Traversal/DefaultBluePlug");
            try
            {
                var electricity=ElectricPlugCombat.Attach(hero,data);
                var first=Fixture(ElectricFixtureKind.Dummy,4);
                var second=Fixture(ElectricFixtureKind.Dummy,6);
                var third=Fixture(ElectricFixtureKind.Dummy,8);
                var fourth=Fixture(ElectricFixtureKind.Dummy,10);
                Vector2 p=lab.transform.position;
                Vector2 start=p+Vector2.up;
                var wall=new GameObject("검사 셔터");wall.transform.SetParent(lab.transform,false);
                wall.transform.localPosition=new Vector2(2.5f,1);
                wall.layer=8;wall.AddComponent<BoxCollider2D>().size=new Vector2(.35f,4);
                Physics2D.SyncTransforms();
                Check(!electricity.ThrowHit(start,start+Vector2.right*4.5f,out _),"차폐된 공격");
                wall.SetActive(false);Physics2D.SyncTransforms();
                Check(electricity.ThrowHit(start,start+Vector2.right*5,out _),"고속 투척 충돌");
                Check(first.Health==150 && second.Health==165 && third.Health==165 && fourth.Health==180,"직격·전이 2회 상한");
                electricity.Tick(2);
                Check(first.Health==134,"초당 8, 2초 감전");
                electricity.Spin(first.Point,first.Point);
                electricity.Spin(first.Point,first.Point);
                Check(first.Health==124,"회전 중복 피해");
                electricity.Arrive(fourth.Point);
                Check(fourth.Health==160,"실제 견인 도착 방전");
                var water=Fixture(ElectricFixtureKind.Water,21);
                var snake=Fixture(ElectricFixtureKind.CableSnake,25);
                electricity.ThrowHit(water.Point+Vector2.left*2,water.Point,out _);
                Check(snake.StunRemaining>1.9f && snake.Health==50,"수면 전도와 전선 뱀 마비");
                var charge=Fixture(ElectricFixtureKind.BombBulb,33);
                var bystander=Fixture(ElectricFixtureKind.Dummy,34.7f);
                charge.React(ElectricPlugPulse.Spin,10);
                electricity.Tick(.5f);
                Check(!charge.Targetable && bystander.Health==125,"폭탄 퓨즈와 범위 피해");
                var holder=new GameObject("노후 콘센트");
                holder.transform.SetParent(lab.transform,false);holder.transform.localPosition=new Vector2(41,1);
                var outlet=holder.AddComponent<BluePlugSocket>();
                ElectricPlugTarget.Place(lab.transform,ElectricFixtureKind.WornOutlet,Vector2.zero,holder.name,null,holder);
                electricity.Dock(outlet);electricity.Tick(.5f);
                Check(!outlet.Operational,"소켓 과부하 차단");
                electricity.Tick(4.1f);
                Check(outlet.Operational,"소켓 냉각 후 정상");
                var panel=Fixture(ElectricFixtureKind.Glass,48);
                var wire=Fixture(ElectricFixtureKind.Filament,48.5f);
                var lamp=Fixture(ElectricFixtureKind.Lamp,49);
                electricity.Area(panel.Point,1.2f,80,SummonedBladeHit.Impact);
                Check(!panel.Targetable && !wire.Targetable && lamp.Lit,"낙하 충격·필라멘트·작업등");
                Debug.Log("플러그 전기 시험 통과 · 투척 차폐, 감전 전이, 회전, 도착, 물·뱀·폭탄·노후 소켓, 절단/점등");

                ElectricPlugTarget Fixture(ElectricFixtureKind type,float x) =>
                    ElectricPlugTarget.Place(lab.transform,type,new Vector2(x,1),type.ToString());
            }
            finally
            {
                lab.SetActive(false);
                SceneManager.UnloadSceneAsync(scene);
            }
        }

        static void Check(bool success,string test)
        {
            if(!success)throw new InvalidOperationException("전기 검사 실패 · "+test);
        }
    }
}
#endif
