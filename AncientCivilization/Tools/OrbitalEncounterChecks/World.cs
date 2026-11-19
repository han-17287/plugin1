using System;
using UnityEngine;

namespace UnityEngine
{
    public static class Application { public static bool isFocused = true; }
    public static class Random
    {
        static readonly System.Random Generator = new System.Random(797);
        public static float value => (float)Generator.NextDouble();
    }
}

namespace Plugin.AncientCivilization
{
    // 물리 엔진 대신 고정된 전장과 생존 가능한 플레이어를 제공한다.
    // 공격 선택, 전조, 판정 시계, 반격 창, 체력 경계 및 퇴장은 게임의 코드를 실행한다.
    public sealed class TestHero : MonoBehaviour
    {
        public bool IsOnGround => true;
        public float HorizontalSpeed => 0;
        public void Hurt(float damage) {}
    }

    public sealed class TestJournal : UnityEngine.Object { public bool Open => false; }
    public enum AncientEquipmentCue { Forge, Lens, MetalFall, Sweep, ShortCircuit, Contact }

    public sealed class AncientGame : MonoBehaviour
    {
        public AncientOrbitalEncounter Orbital;
        public readonly TestHero hero = new TestHero();
        public bool Paused, MainMenu, skills;
        public TestJournal Journal { get; } = new TestJournal();
        public static Color Cyan => Color.white;
        public void Sound(AncientEquipmentCue cue) {}
        public void Impact(Vector2 point, Color color, bool heavy) {}
        public void Notice(string message, float seconds) {}
        public void Burst(Vector2 point, Color color, int count) {}
        public LineRenderer Line(string name, Color color, float width, int order)
        {
            return new GameObject(name).AddComponent<LineRenderer>();
        }
    }

    public sealed class AncientEnemy : MonoBehaviour
    {
        public const float MaxHealth = 900;
        public float Health = MaxHealth;
        public float HealthRatio => Health / MaxHealth;
        public Vector2 AimPoint => (Vector2)transform.position + Vector2.up * 1.65f;
    }

    public sealed class AncientOrbitalRig
    {
        public static bool FollowupLamp { get; private set; }
        public static bool LensOpen { get; private set; }
        public AncientOrbitalRig(Transform owner) {}
        public void Pose(int frame, int facing, bool open, bool followup, bool flashLens, float hitFlash)
        {
            FollowupLamp = followup;
            LensOpen = flashLens;
        }
        public void Opacity(float opacity) {}
    }

    public sealed class AncientOrbitalArena
    {
        OrbitalMove preparedMove;
        Vector2 advertisedTarget;
        int preparedSide;
        public int Announcements { get; private set; }
        public int Executions { get; private set; }
        public bool Released { get; private set; }
        public float CentreX => 16;
        public Vector2 CargoPoint => new Vector2(10, 4);
        public int RailDirection => 1;
        public Vector2 DispenserPoint => new Vector2(28, .8f);
        public Vector2 DropPoint => new Vector2(17, .5f);
        public Vector2 LatchPoint => new Vector2(10, .38f);
        public AncientOrbitalArena(AncientGame game, Transform parent, float left, float right) {}
        public void Tick(float elapsed) {}
        public bool IsAnchorReachable(int side, Vector2 hero) => true;
        public Vector2 FixedAnchorPoint(int side) => new Vector2(side < 0 ? 2 : 30, 6.65f);
        public void OpenCounter() {}
        public bool LockHit(Vector2 point) => Math.Abs(point.x - LatchPoint.x) < 1.65f;

        public void PrepareMove(OrbitalMove move, int side, Vector2 target)
        {
            if (Announcements != Executions) throw new InvalidOperationException("Uncommitted tell overwritten");
            Announcements++;
            preparedMove = move;
            preparedSide = side;
            advertisedTarget = move == OrbitalMove.DropCargo || move == OrbitalMove.CargoRush ? DropPoint : target;
        }

        public void BeginMove(OrbitalMove move, int side, Vector2 target)
        {
            if (preparedMove != move || side != preparedSide || Vector2.Distance(advertisedTarget, target) > .001f)
                throw new InvalidOperationException("Published move, direction or position changed after tell");
            Executions++;
        }

        public void Dispose() { Released = true; }
    }
}
