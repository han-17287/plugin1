using System.Collections.Generic;
using Plugin.PlayerCore;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 본편의 배치 좌표와 전투 장치가 전용 콘센트의 수명과 위치를 관리한다.
    // 영웅의 LateUpdate에서 이번 프레임의 조준/체결을 결정한 뒤 풀의 패널을 갱신한다.
    [DefaultExecutionOrder(40)]
    public sealed class AncientBluePlugPorts : MonoBehaviour
    {
        internal enum Source { None, Map, LivingObject, EnforcerObject, FinaleTruck, DistrictDevice, BladeHandle }

        public readonly struct Port
        {
            internal readonly Source source;
            internal readonly int slot;
            internal readonly uint generation;
            internal readonly Component owner;
            public readonly Vector2 Point, Anchor, Normal, EntryDirection;
            public readonly BluePlugSkill Supported;
            public readonly bool IsBoarding;
            public bool IsValid => source != Source.None;
            public bool IsProp => source == Source.LivingObject || source == Source.EnforcerObject;
            public bool IsObject => IsProp;
            internal bool IsBlade => source == Source.BladeHandle;
            internal bool IsDevice => source == Source.DistrictDevice;

            internal Port(Source kind, int slot, uint generation, Component owner, Vector2 point,
                Vector2 anchor, Vector2 normal, BluePlugSkill supported, bool boarding = false)
            {
                source = kind;
                this.slot = slot;
                this.generation = generation;
                this.owner = owner;
                Point = point;
                Anchor = anchor;
                Normal = normal;
                EntryDirection = normal.sqrMagnitude > .1f ? -normal : Vector2.up;
                Supported = supported;
                IsBoarding = boarding;
            }

            internal bool SameSocket(Port other) => source == other.source && slot == other.slot &&
                generation == other.generation && owner == other.owner;
        }

        sealed class SocketView
        {
            public BluePlugSocket socket;
            public Port previous;
        }

        const int Solid = 1 << 8;
        const int ViewLimit = 40;
        const float TipClearance = .19f;
        const BluePlugSkill Travel = BluePlugSkill.Tow | BluePlugSkill.Swing;
        readonly List<SocketView> views = new List<SocketView>(ViewLimit);
        AncientGame game;
        AncientCampaignPlugWorks works;
        Port highlighted;
        BluePlugSocketState state;
        BluePlugSkill learned = Travel;
        int activeViews;
        public bool PropInUse => game && (game.Enforcer && game.Enforcer.PlugObjectHeld ||
            game.Living && game.Living.PlugObjectHeld);

        public static AncientBluePlugPorts For(AncientGame game)
        {
            var ports = game.GetComponent<AncientBluePlugPorts>();
            if (!ports) ports = game.gameObject.AddComponent<AncientBluePlugPorts>();
            ports.game = game;
            ports.works ??= new AncientCampaignPlugWorks(game);
            return ports;
        }

        int Sector => game.Production && game.Production.InChaseLane || game.Finale && game.Finale.Active
            ? 5 : game.Story ? game.Story.CurrentFloor : AncientCityLayout.VisualSector(game.hero.transform.position.x);

        internal System.Func<Vector2,bool> BladeSupplies { private get; set; }
        internal void StepDevices(float dt)
        {
            if (game && game.hero) works?.Step(Sector, dt, BladeSupplies);
        }

        internal void DockDevice(Port port) { if (port.IsDevice) works.Dock(port); }
        internal void ReleaseDevice() => works?.Release();
        internal void ResetDevices() => works?.Reset();
        internal string DeviceHint(Port port) => port.IsDevice ? works?.Hint(port) : null;

        static bool Authored(Vector2 point) => GrappleRules.Finite(point.x) && GrappleRules.Finite(point.y) &&
            Mathf.Abs(point.x) < 100000 && Mathf.Abs(point.y) < 100000;

        public void Highlight(Port port, BluePlugSocketState status, BluePlugSkill skills)
        {
            highlighted = port;
            state = status;
            learned = skills;
        }

        public bool TryPick(Vector2 aim, Vector2 origin, bool boarding, out Port result,
            float range = AncientZipRules.Range)
        {
            result = default;
            if (!game || !game.hero || !Authored(aim)) return false;
            int sector = Sector;
            float best = float.PositiveInfinity;
            float snap = game.View ? Mathf.Clamp(game.View.orthographicSize * .16f, .70f, 1.45f) : 1.2f;
            if (!boarding && TryBlade(out var handle))
            {
                Consider(handle, aim, origin, snap, range, ref best, ref result);
                if (result.IsValid) return true;
            }
            if (!boarding && works != null)
                for (int i = 0; i < works.Count; i++)
                    if (works.TryPort(i, out Port device))
                        Consider(device, aim, origin, snap, range, ref best, ref result);
            for (int slot = 0; slot < game.anchors.Count; slot++)
            {
                Vector2 anchor = game.anchors[slot];
                float radius = boarding ? 2.1f : snap;
                if (!Authored(anchor) || (anchor - aim).sqrMagnitude > radius * radius + .75f ||
                    AncientCityLayout.VisualSector(anchor.x) != sector || !TryMap(slot, out Port port) ||
                    boarding && !port.IsBoarding) continue;
                Consider(port, aim, origin, radius, range, ref best, ref result);
            }
            if (!boarding)
            {
                if (TryFinale(out Port finale)) Consider(finale, aim, origin, 2.1f, range, ref best, ref result);
                var living = game.Living;
                if (living && living.PlugObjectsAvailable)
                    foreach (var item in living.PlugObjects)
                        if (item && item.Selectable && (item.Point - aim).sqrMagnitude <= 4f && TryLiving(item, out Port prop))
                            Consider(prop, aim, origin, 1.65f, range, ref best, ref result);
                var enforcer = game.Enforcer;
                if (enforcer)
                    for (int slot = 0; slot < enforcer.PlugObjectSlots; slot++)
                        if (TryEnforcer(enforcer, slot, out Port prop)) Consider(prop, aim, origin, 1.65f, range, ref best, ref result);
            }
            return result.IsValid;
        }

        void Consider(Port port, Vector2 aim, Vector2 origin, float radius, float range, ref float best, ref Port result)
        {
            float score = (port.Point - aim).sqrMagnitude;
            float reach = (port.Point - origin).sqrMagnitude;
            if (score > radius * radius || score >= best || reach > range * range ||
                reach < .08f || !game.hero.CampaignTipClear(origin, port.Point - origin)) return;
            best = score;
            result = port;
        }

        public bool TryRefresh(Port port, out Port refreshed)
        {
            refreshed = default;
            if (!game || !game.hero) return false;
            switch (port.source)
            {
                case Source.BladeHandle:
                    return TryBlade(out refreshed) && refreshed.generation == port.generation;
                case Source.DistrictDevice:
                    return works != null && works.Refresh(port, out refreshed);
                case Source.Map:
                    return TryMap(port.slot, out refreshed) && port.IsBoarding == refreshed.IsBoarding &&
                        AncientCityLayout.VisualSector(refreshed.Anchor.x) == Sector;
                case Source.FinaleTruck:
                    return TryFinale(out refreshed) && refreshed.owner == port.owner;
                case Source.LivingObject:
                    return TryLiving(port.owner as AncientWireObject, out refreshed) && refreshed.generation == port.generation;
                case Source.EnforcerObject:
                    var enforcer = port.owner as AncientEnforcerEncounter;
                    return game.Enforcer == enforcer && TryEnforcer(enforcer, port.slot, out refreshed) &&
                        refreshed.generation == port.generation;
                default: return false;
            }
        }

        bool TryBlade(out Port port)
        {
            port = default;
            var blade = game.hero.Loadout;
            if (!blade || !blade.Handle) return false;
            var handle = blade.Handle;
            port = new Port(Source.BladeHandle, 0, blade.HandleGeneration, blade, handle.Point, handle.TowPoint,
                -handle.EntryDirection, BluePlugSkill.Tow);
            return true;
        }

        public bool TryUse(Port port, Vector2 origin)
        {
            if (!TryRefresh(port, out Port current)) return false;
            if (current.source == Source.LivingObject)
                return game.Living.TryUsePlugObject((AncientWireObject)current.owner, origin);
            if (current.source == Source.EnforcerObject)
                return ((AncientEnforcerEncounter)current.owner).TryUsePlugObject(current.slot, current.generation, origin);
            return false;
        }

        public void CancelPropLink()
        {
            if (!game) return;
            if (game.Living) game.Living.CancelPlugObject();
            if (game.Enforcer) game.Enforcer.CancelPlugObject();
        }

        bool TryMap(int slot, out Port port)
        {
            port = default;
            if (slot < 0 || slot >= game.anchors.Count) return false;
            Vector2 anchor = game.anchors[slot];
            if (!Authored(anchor) || !FindTip(anchor, out Vector2 tip, out Vector2 normal)) return false;
            bool boarding = game.Production && game.Production.IsBoardingPlugSlot(slot);
            if (boarding && !game.Production.CanBoardWire(anchor) &&
                game.Production.Sequence.Phase != AncientProductionPhase.Boarding) return false;
            port = new Port(Source.Map, slot, 0, null, tip, anchor, normal,
                boarding ? BluePlugSkill.Tow : Travel, boarding);
            return true;
        }

        bool TryLiving(AncientWireObject item, out Port port)
        {
            port = default;
            if (!game.Living || !game.Living.PlugObjectsAvailable || !item || !item.Selectable) return false;
            Vector2 anchor = item.Point;
            if (!FindTip(anchor + Vector2.up * .16f, out Vector2 tip, out _)) return false;
            port = new Port(Source.LivingObject, 0, item.PlugGeneration, item, tip, anchor, Vector2.zero, BluePlugSkill.Tow);
            return true;
        }

        static bool TryEnforcer(AncientEnforcerEncounter enforcer, int slot, out Port port)
        {
            port = default;
            if (!enforcer || !enforcer.TryGetPlugObject(slot, out Vector2 anchor, out uint generation) ||
                !FindTip(anchor + Vector2.up * .16f, out Vector2 tip, out _)) return false;
            port = new Port(Source.EnforcerObject, slot, generation, enforcer, tip, anchor, Vector2.zero, BluePlugSkill.Tow);
            return true;
        }

        bool TryFinale(out Port port)
        {
            port = default;
            if (!game.Finale || !game.Finale.TryPlugAnchor(out Vector2 anchor, out Transform carrier) ||
                !FindTip(anchor, out Vector2 tip, out Vector2 normal)) return false;
            port = new Port(Source.FinaleTruck, 0, 0, carrier, tip, anchor, normal, BluePlugSkill.Tow);
            return true;
        }

        static bool FindTip(Vector2 anchor, out Vector2 tip, out Vector2 normal)
        {
            tip = anchor;
            normal = Vector2.zero;
            if (!Authored(anchor)) return false;
            var support = Physics2D.OverlapCircle(anchor, TipClearance, Solid);
            if (!support) return true;
            Vector2 face = support.ClosestPoint(anchor);
            Vector2 outward = anchor - face;
            if (outward.sqrMagnitude < .00001f)
            {
                Bounds bounds = support.bounds;
                float left = Mathf.Abs(anchor.x - bounds.min.x), right = Mathf.Abs(bounds.max.x - anchor.x);
                float bottom = Mathf.Abs(anchor.y - bounds.min.y), top = Mathf.Abs(bounds.max.y - anchor.y);
                float nearestX = Mathf.Min(left, right), nearestY = Mathf.Min(bottom, top);
                normal = nearestY <= nearestX ? (bottom <= top ? Vector2.down : Vector2.up) :
                    (left <= right ? Vector2.left : Vector2.right);
                face = anchor + normal * Mathf.Min(nearestX, nearestY);
            }
            else normal = outward.normalized;
            tip = face + normal * TipClearance;
            return !Physics2D.OverlapCircle(tip, .12f, Solid);
        }

        void LateUpdate()
        {
            int used = 0;
            if (game && game.hero && game.View && !game.MainMenu)
            {
                Vector2 camera = game.View.transform.position;
                float height = game.View.orthographicSize + 1.2f;
                float width = game.View.orthographicSize * game.View.aspect + 1.2f;
                int sector = Sector;
                if (TryBlade(out var bladePort))
                    game.hero.Loadout.Handle.SetIndicator(bladePort.SameSocket(highlighted) ? state : BluePlugSocketState.Idle, BluePlugSkill.Tow);
                if (!highlighted.IsBlade && TryRefresh(highlighted, out Port current) && InView(current.Point, camera, width, height))
                    Draw(current, ref used);
                if (TryFinale(out Port finale) && InView(finale.Point, camera, width, height) && !finale.SameSocket(highlighted))
                    Draw(finale, ref used);
                if (works != null)
                    for (int i = 0; i < works.Count && used < ViewLimit; i++)
                        if (works.TryPort(i, out Port device) && !device.SameSocket(highlighted) &&
                            InView(device.Point, camera, width, height)) Draw(device, ref used);
                for (int slot = 0; slot < game.anchors.Count && used < ViewLimit; slot++)
                {
                    Vector2 anchor = game.anchors[slot];
                    if (!InView(anchor, camera, width, height) || AncientCityLayout.VisualSector(anchor.x) != sector ||
                        !TryMap(slot, out Port port) || port.SameSocket(highlighted)) continue;
                    Draw(port, ref used);
                }
                var living = game.Living;
                if (living && living.PlugObjectsAvailable)
                    foreach (var item in living.PlugObjects)
                    {
                        if (used == ViewLimit) break;
                        if (item && InView(item.Point, camera, width, height) && TryLiving(item, out Port prop) &&
                            !prop.SameSocket(highlighted)) Draw(prop, ref used);
                    }
                var enforcer = game.Enforcer;
                if (enforcer)
                    for (int slot = 0; slot < enforcer.PlugObjectSlots && used < ViewLimit; slot++)
                        if (TryEnforcer(enforcer, slot, out Port prop) && InView(prop.Point, camera, width, height) &&
                            !prop.SameSocket(highlighted)) Draw(prop, ref used);
            }
            for (int i = used; i < activeViews; i++) views[i].socket.gameObject.SetActive(false);
            activeViews = used;
        }

        static bool InView(Vector2 point, Vector2 camera, float width, float height) =>
            Mathf.Abs(point.x - camera.x) <= width && Mathf.Abs(point.y - camera.y) <= height;

        void Draw(Port port, ref int used)
        {
            if (used >= ViewLimit) return;
            if (used == views.Count)
            {
                var holder = new GameObject("본편 파란 콘센트");
                holder.transform.SetParent(transform, false);
                views.Add(new SocketView { socket = holder.AddComponent<BluePlugSocket>() });
            }
            SocketView view = views[used++];
            view.socket.gameObject.SetActive(true);
            view.socket.transform.position = port.Point;
            view.socket.transform.localScale = Vector3.one * (port.IsProp ? .56f : .73f);
            if (!port.SameSocket(view.previous) || port.Supported != view.previous.Supported ||
                port.EntryDirection != view.previous.EntryDirection)
                view.socket.Configure(Vector2.zero, port.Supported, port.EntryDirection);
            view.previous = port;
            view.socket.SetIndicator(port.SameSocket(highlighted) ? state : BluePlugSocketState.Idle, learned);
        }
    }
}
