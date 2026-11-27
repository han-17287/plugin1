using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine
{
    public partial class MonoBehaviour { public bool isActiveAndEnabled => !gameObject || gameObject.activeSelf; }
    public partial class BoxCollider2D { public Vector2 offset; }
    public sealed class PlatformEffector2D : Component { public bool useOneWay; public float surfaceArc; }
    public class ScriptableObject : Object { }
    public sealed class CreateAssetMenuAttribute : Attribute { public string menuName; }
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string label) { } }
    public sealed class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public struct Color32
    {
        readonly Color color;
        public Color32(byte r, byte g, byte b, byte a) { color = new Color(r/255f,g/255f,b/255f,a/255f); }
        public static implicit operator Color(Color32 value) => value.color;
    }
}

namespace Plugin.PlayerCore
{
    // 실제 순찰 방·전력 회로·물리 표면을 구성한다. 렌더링과 입력 신호만 대역으로 전달한다.
    public sealed class PlayerBluePlug : Component
    {
        public event Action<BluePlugSocket> SeatedSocketChanged;
        public event Action LinkReset;
        public BluePlugSocket SeatedSocket { get; private set; }
        public BluePlugPhase Phase;
        public BluePlugSocket LastWallJump {get; set;}
        public uint WallJumpSequence {get; set;}
        public uint WallJumpAtManualRecall {get; set;}
        public uint ManualRecallSequence {get; set;}
        public BluePlugSocket LastManualRecall {get; set;}
        public uint ThrowSequence {get; set;}
        public BluePlugSocket RestingSocket { get; set; }
        public Func<Vector2,bool> BladePower;
        public bool SuppliesAt(Vector2 point) => BladePower?.Invoke(point) == true;
        public bool WallResting => RestingSocket;
        public void Report(BluePlugSocket socket)
        {
            SeatedSocket = socket;
            Phase = socket ? BluePlugPhase.Connected : BluePlugPhase.Ready;
            SeatedSocketChanged?.Invoke(socket);
        }
        public void ResetLink() { Report(null); LinkReset?.Invoke(); }
    }

    public sealed class DistrictWorldSign : Component { public string text; public bool enabled; }
    internal static class BluePlugArt { internal static readonly Color MovementColor = Color.white; }
    internal sealed class PlugDistrictDriveArt
    {
        internal static PlugDistrictDriveArt Build(Transform owner, int mechanism, Transform lift, Vector2 a, Vector2 b) => null;
        internal void Signal(float dt, bool sending) { }
    }
    internal enum PlugDistrictPiece { Backplate, Fixture, Deck, Indicator }
    internal static class PlugDistrictArt
    {
        internal static readonly Dictionary<string, BoxCollider2D> Surfaces = new Dictionary<string, BoxCollider2D>();
        internal static SpriteRenderer Part(Transform parent, string label, int floor, PlugDistrictPiece piece,
            Vector2 pos, Vector2 size, int order)
        {
            var child = new GameObject(label);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = pos;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.size = size;
            var surface = parent.gameObject.GetComponent<BoxCollider2D>();
            if (surface) Surfaces[label] = surface;
            return renderer;
        }
    }

    public sealed partial class PatrolRoom
    {
        static void CheckDistrictPlugRooms()
        {
            int[] expectedRooms = { 1, 9, 11, 16, 21, 27, 31, 35, 41, 47 };
            int[] expectedKinds = { 0, 1, 1, 3, 2, 4, 3, 0, 4, 2 };
            for (int id = 1; id <= 50; ++id)
            {
                var room = Open(id);
                int site = Array.IndexOf(expectedRooms, id);
                Check((room.plugWorks != null) == (site >= 0), "Only authored patrol rooms build plug equipment: " + id);
                string mapLabel = PatrolCatalog.Device(id);
                Check(PatrolCatalog.Devices.Any(original => mapLabel == (site < 0 ? original : original + " · " + room.plugWorks.Label)),
                    "Room card includes its original obstacle and the installed machine's exact name: " + id);
                Check(mapLabel.Length * 15 <= 382 - 13 - 5, "Second line fits the room card even at full-width character advances: " + id);
                if (site < 0) continue;
                int kind = expectedKinds[site];
                Check(room.plugWorks.Mechanism == kind, "Two different machines per floor: " + id);
                room.Tick(36); // 셔터가 안전한 초기 열린 상태에서 닫힐 때까지 진행한다.
                var input = room.bluePlug;
                var ports = room.machineSockets;
                bool pair = kind == 2 || kind == 4;
                Check(ports.Length == (pair ? 2 : 1) && ports.All(p => p.PowersEquipment && room.BluePlugSockets.Contains(p)) &&
                    ports[0].Operational, "Station enters normal targeting before movement skills unlock: " + id);
                Check(!pair || !ports[1].Operational, "Standby return terminal starts offline: " + id);

                foreach (var port in ports)
                {
                    var point = port.Point;
                    Check(point.x > 0 && point.x < 48 && point.y < 15, "Socket inside the authored room: " + id);
                    Check(room.HasGroundApproach(point), "There is an unobstructed throw from the low route: " + id);
                    Check(room.ClearAt(point, .10f, .10f, false), "Plug head centered outside existing geometry: " + id);
                    // 순찰 경위 캡슐(0.72 × 1.80)에 플러그 이동 skin 0.035를 더한 보수적 검사.
                    Check(room.ClearAt(port.TowPoint, .395f, .935f, false), "Tow arrival fits above work decks and room geometry: " + id);
                }
                if (id == 1)
                    Check(room.ClearThrow(new Vector2(2, 1.25f), ports[0].Point),
                        "Untrained player can power the first hoist from the arrival apron");

                var parts = PlugDistrictArt.Surfaces;
                var active = parts[kind switch {
                    0 => "화물 승강대", 1 => "방화 셔터", 2 => "승인교 전개판", 3 => "셋째 차양", _ => "비상 착지대" }];
                float initialY = active.transform.position.y;
                if (kind == 1)
                    Check(active.enabled && active.size.y > 3, "Hanging shutter initially blocks its upper corridor: " + id);
                if (kind >= 2)
                    Check(!active.enabled, "Retracted decks have no collider: " + id);

                input.Report(ports[0]); room.Tick(80);
                if (kind == 0)
                    Check(active.transform.position.y > initialY + 3 && active.enabled,
                        "Connected hoist physically raises its standing surface: " + id);
                if (kind == 1)
                    Check(!active.enabled, "Powered shutter removes its upper-corridor collision: " + id);
                if (kind == 3)
                    Check(parts["첫 차양"].enabled && parts["둘째 차양"].enabled && active.enabled && active.size.x > 2,
                        "All three usable platforms unfold: " + id);

                input.Report(null);
                if (pair)
                {
                    Check(ports[1].Operational && !active.enabled, "Real B port opens while the destination stays folded: " + id);
                    input.Report(ports[1]); room.Tick(24);
                    Check(active.enabled && active.size.x > 3, "Second physical socket deploys the destination: " + id);
                }
                input.ResetLink(); room.Tick(36);
                Check((kind != 0 || Math.Abs(active.transform.position.y - initialY) < .001f) &&
                    (kind != 1 || active.enabled && active.size.y > 3) && (kind < 2 || !active.enabled) &&
                    (!pair || !ports[1].Operational), "Hard reset restores geometry and returns all ports to standby: " + id);
                if (kind == 0)
                {
                    input.BladePower = p => Vector2.Distance(p,ports[0].Point) < .1f;
                    room.Tick(80);
                    Check(!input.SeatedSocket && active.transform.position.y > initialY+3,
                        "A planted blade independently powers the physical cargo hoist: "+id);
                    input.BladePower = null;
                    room.Tick(400);
                    Check(Math.Abs(active.transform.position.y-initialY)<.001f,
                        "Removing the planted blade releases the physical cargo hoist: "+id);
                }
                room.OnDisable();
                input.Report(ports[0]); room.Tick(80);
                Check((kind != 0 || Math.Abs(active.transform.position.y - initialY) < .001f) &&
                    (kind != 1 || active.enabled) && (kind < 2 || !active.enabled) && (!pair || !ports[1].Operational),
                    "Inactive room stops consuming future player's connections: " + id);
            }
        }

        bool HasGroundApproach(Vector2 target)
        {
            for (int dx = -4; dx <= 4; dx++)
            {
                var hand = new Vector2(target.x + dx, 1.25f);
                if (hand.x > .4f && hand.x < 47.6f && ClearAt(hand, .3f, .7f, true) && ClearThrow(hand, target)) return true;
            }
            return false;
        }

        bool ClearThrow(Vector2 hand, Vector2 target)
        {
            if (Vector2.Distance(hand, target) > 16) return false;
            // 아래에서 던진 플러그는 일방향 발판을 통과한다. 단단한 벽·닫힌 셔터는 팁 반경까지 검사한다.
            int steps = Mathf.CeilToInt(Vector2.Distance(hand, target) / .075f);
            for (int step = 0; step <= steps; step++)
                if (!ClearAt(Vector2.Lerp(hand, target, (float)step / steps), .1f, .1f, true)) return false;
            return true;
        }

        bool ClearAt(Vector2 point, float halfWidth, float halfHeight, bool skipOneWay)
        {
            var roomSolids = objects.Select(o => o.Object.GetComponent<BoxCollider2D>());
            foreach (var solid in roomSolids.Concat(PlugDistrictArt.Surfaces.Values))
            {
                if (!solid || !solid.enabled || !solid.gameObject.activeSelf || skipOneWay && solid.usedByEffector) continue;
                var delta = point - (Vector2)solid.transform.position - solid.offset;
                if (Mathf.Abs(delta.x) < solid.size.x * .5f + halfWidth &&
                    Mathf.Abs(delta.y) < solid.size.y * .5f + halfHeight) return false;
            }
            return true;
        }
    }
}
