using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        void InstallElectricFixtures(int id)
        {
            switch(id)
            {
                case 1:
                    ElectricPlugTarget.Place(transform,ElectricFixtureKind.Dummy,new Vector2(8,.74f),"검사 표적");
                    ElectricPlugTarget.Place(transform,ElectricFixtureKind.BombBulb,new Vector2(10.15f,.73f),"폐기용 폭탄 전구");
                    Sign("검사 표적 · 전구 옆에서 방전",8.7f,2.25f);
                    break;
                case 11:
                    ElectricPlugTarget.Place(transform,ElectricFixtureKind.Water,new Vector2(26,.21f),"누수된 물받이");
                    ElectricPlugTarget.Place(transform,ElectricFixtureKind.CableSnake,new Vector2(29,.50f),"전선 뱀");
                    break;
                case 21:
                    var grate=Box("유지보수 샛길 유리",40,2.25f,.22f,1.7f,new Color(.56f,.83f,.87f,.47f),true);
                    ElectricPlugTarget.Place(transform,ElectricFixtureKind.Glass,new Vector2(39.74f,2.25f),"유리 보호 덮개",
                        broken=>grate.SetActive(!broken));
                    break;
                case 31:
                    var shelf=Deck("전등이 비추는 점검 발판",39,2.8f,3.2f);
                    ElectricPlugTarget.Place(transform,ElectricFixtureKind.Lamp,new Vector2(37.6f,1.45f),"수선공의 점검등",
                        on=>shelf.SetActive(on));
                    break;
                case 42:
                    var socket=Hook("노후 정비 콘센트",39.6f,3.25f);
                    ElectricPlugTarget.Place(transform,ElectricFixtureKind.WornOutlet,Vector2.zero,socket.name,null,socket);
                    break;
            }
        }
    }

    public sealed partial class TraversalRoom
    {
        // 생산구 연습장 진입로. 공용 입구의 경로는 항상 열어 두고 오른쪽에서만 장치가 서로 전도된다.
        public void InstallElectricBench()
        {
            if(Floor!=TraversalFloor.Production)return;
            ElectricPlugTarget.Place(transform,ElectricFixtureKind.Dummy,new Vector2(5.85f,.74f),"절연 훈련 표적");
            ElectricPlugTarget.Place(transform,ElectricFixtureKind.BombBulb,new Vector2(8.35f,.74f),"폭탄 전구");
            ElectricPlugTarget.Place(transform,ElectricFixtureKind.Water,new Vector2(10.7f,.20f),"전도 실험 수조");
            ElectricPlugTarget.Place(transform,ElectricFixtureKind.CableSnake,new Vector2(14.1f,.52f),"전선 뱀");
            Spawn("FixedAnchor",new Vector2(15.1f,3.75f));
            var worn=plugSockets[plugSockets.Count-1];
            ElectricPlugTarget.Place(transform,ElectricFixtureKind.WornOutlet,Vector2.zero,"과부하 소켓",null,worn.gameObject);
            var lamp=ElectricPlugTarget.Place(transform,ElectricFixtureKind.Lamp,new Vector2(34.9f,1.45f),"작업등");
            var screen=Box("절연막",new Vector2(33.5f,2.3f),new Vector2(.28f,1.4f),new Color(.28f,.25f,.19f),8);
            screen.AddComponent<BoxCollider2D>();
            ElectricPlugTarget.Place(transform,ElectricFixtureKind.Filament,new Vector2(33.15f,2.55f),"절단 필라멘트",
                cut=>screen.SetActive(!cut));
            Sign("접촉 표적 · 폭탄 전구 · 물 전도",6.1f,3.2f);
            Sign("절단 필라멘트 / "+lamp.name,32.8f,4.0f);
        }

        public void ResetElectricBench()
        {
            var list=ElectricPlugTarget.Registered;
            for(int i=0;i<list.Count;i++)
            {
                var f=list[i];
                if(f && f.transform.IsChildOf(transform))f.ResetFixture();
            }
        }
    }
}
