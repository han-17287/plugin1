using System;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 전조에서 정한 세계 좌표를 공격이 끝날 때까지 보존한다. 빛의 중심선만 판정한다.
    public sealed class AncientOrbitalHazards : IDisposable
    {
        const float Roof = 7.6f;
        const float Floor = .04f;
        const float CargoContactTime = .58f;
        static readonly Color Tell = new Color(.98f, .7f, .22f);
        static readonly Color Live = new Color(.48f, .89f, 1f);
        static readonly Color Lens = new Color(1f, .42f, .39f);
        static readonly Color Steel = new Color(.48f, .62f, .74f);

        readonly AncientGame game;
        readonly Transform root;
        readonly float left, right;
        readonly LineRenderer[] strokes = new LineRenderer[48];
        int used, visible, hitMask, nextHit;
        float nextDamage, previous;
        Vector2 origin, mark, previousHero, probe, previousCargo, sampledCargo;
        int side, fakeFacing;
        OrbitalMove move;
        bool armed, paint, test, disposed;

        public Vector2 CargoPosition { get; set; }
        public Vector2 ImpactPoint => new Vector2(mark.x, Floor);
        public Vector2 RushPosition { get; private set; }

        public AncientOrbitalHazards(AncientGame game, Transform parent, float left, float right)
        {
            this.game = game;
            this.left = left;
            this.right = right;
            root = new GameObject("궤도 봉쇄기 · 사격선").transform;
            root.SetParent(parent, false);
        }

        public static bool ShutterFiring(float progress) => progress >= .12f && progress < .76f;

        public static float TellSeconds(OrbitalMove move)
        {
            switch (move)
            {
                case OrbitalMove.ShutterFeint: return 1.60f;
                case OrbitalMove.ChainDrop: case OrbitalMove.CargoRush: return 1.12f;
                case OrbitalMove.PursuitStamp: case OrbitalMove.CrossBeam: return 1f;
                case OrbitalMove.ReturnBlade: case OrbitalMove.SplitShutter: return .92f;
                case OrbitalMove.ReverseRail: case OrbitalMove.BladeFerry: return .70f;
                default: return .84f;
            }
        }

        public static float ActiveSeconds(OrbitalMove move)
        {
            switch (move)
            {
                case OrbitalMove.ReturnBlade: return 2.55f;
                case OrbitalMove.ChainDrop: return 2.05f;
                case OrbitalMove.RisingBeam: case OrbitalMove.ClosingSweep: return 1.58f;
                case OrbitalMove.PursuitStamp: case OrbitalMove.CargoRush: return 1.45f;
                case OrbitalMove.ShutterFeint: case OrbitalMove.CoreTrap: return .94f;
                case OrbitalMove.SupplyLeak: return 2.10f;
                case OrbitalMove.BladeFerry: return 1.80f;
                case OrbitalMove.ReverseRail: return .85f;
                default: return 1.28f;
            }
        }

        public void Telegraph(OrbitalMove attack, Vector2 source, Vector2 target, int attackSide)
        {
            Clear();
            move = attack;
            origin = source;
            mark = new Vector2(Mathf.Clamp(target.x, left + .75f, right - .75f), target.y);
            side = attackSide < 0 ? -1 : 1;
            fakeFacing = mark.x >= origin.x ? 1 : -1;
            RushPosition = origin;
            CargoPosition = previousCargo = sampledCargo = source;
            previousHero = game.hero ? (Vector2)game.hero.transform.position : mark;
            armed = true;
            TickTell(0);
        }

        public void TickTell(float progress)
        {
            if (!armed || disposed) return;
            BeginFrame();
            float t = Mathf.Clamp01(progress);
            float x = mark.x;
            Vector2 ground = ImpactPoint;
            Vector2 cargo = CargoPosition;
            Vector2 inlet = RailEnd(side), outlet = RailEnd(-side);
            switch (move)
            {
                case OrbitalMove.GroundWave:
                    Arrow(new Vector2(origin.x, .63f), -1, 1.65f);
                    Arrow(new Vector2(origin.x, .63f), 1, 1.65f);
                    break;
                case OrbitalMove.LandingStamp: case OrbitalMove.PursuitStamp:
                    Footprint(x, move == OrbitalMove.PursuitStamp ? 1.25f : .92f, Tell);
                    Stroke(ground + Vector2.up * 1.8f, ground + Vector2.up * 6.2f, .055f, Tell);
                    if (move == OrbitalMove.PursuitStamp)
                        Stroke(origin, new Vector2(x, Roof - .9f), .055f, Tell);
                    break;
                case OrbitalMove.ReturnBlade:
                    Stroke(inlet + Up(1.05f), outlet + Up(1.05f), .055f, Tell);
                    Arrow(outlet + Up(1.65f), side, 1.15f);
                    Saw(inlet + Up(1.05f), .56f, t * 95f, Tell);
                    break;
                case OrbitalMove.AnchorLock:
                    CrossMark(mark, .42f, Tell);
                    break;
                case OrbitalMove.CargoSweep:
                    SweepUnder(cargo, 1, Tell, false);
                    break;
                case OrbitalMove.DropCargo: case OrbitalMove.ChainDrop:
                    if (move == OrbitalMove.DropCargo) Footprint(x, 1.2f, Tell);
                    else for (int i = 0; i < 4; i++) Footprint(DropX(i), .85f, i == 0 ? Tell : Steel);
                    break;
                case OrbitalMove.RisingBeam:
                    RisingBar(0, Tell, false);
                    Stroke(new Vector2(x, .55f), new Vector2(x, 5.45f), .055f, Tell);
                    break;
                case OrbitalMove.CrossBeam:
                    CrossBeams(Tell, false);
                    break;
                case OrbitalMove.ShutterFeint:
                    Feint(t < .55f ? fakeFacing : -fakeFacing, t < .55f ? Tell : t < .75f ? Steel : Lens,
                        t >= .75f, false, t < .55f || t >= .75f);
                    break;
                case OrbitalMove.BladeFerry: case OrbitalMove.ReverseRail:
                    Arrow(cargo + Up(.75f), side, 2f);
                    break;
                case OrbitalMove.CoreTrap: case OrbitalMove.SupplyLeak:
                    Footprint(x, move == OrbitalMove.CoreTrap ? .70f : 2.3f, Tell);
                    break;
                case OrbitalMove.SplitShutter:
                    DrawShutter(Tell, false);
                    break;
                case OrbitalMove.ClosingSweep:
                    Arrow(inlet + Up(.8f), -side, 2.3f);
                    Arrow(outlet + Up(.8f), side, 2.3f);
                    break;
                case OrbitalMove.CargoRush:
                    Stroke(origin, RushCross, .07f, Tell);
                    Stroke(RushCross, RushExit, .07f, Tell);
                    Footprint(x, .96f, Tell);
                    break;
            }
            EndFrame();
            previousCargo = cargo;
            previousHero = game.hero ? (Vector2)game.hero.transform.position : mark;
        }

        public void TickActive(float progress)
        {
            if (!armed || disposed || !game.hero || game.Paused) return;
            float t = Mathf.Clamp01(progress);
            Vector2 hero = game.hero.transform.position;
            nextDamage = 0; nextHit = 0;
            // 낮은 프레임에도 지난 프레임과 현재 위치 사이의 회전날·낙하물 궤적을 검사한다.
            float elapsedFraction = Mathf.Max(0, t - previous);
            int steps = Mathf.Max(1, Mathf.CeilToInt(elapsedFraction * 100f));
            BeginFrame();
            test = true;
            for (int i = 1; i <= steps; i++)
            {
                float fraction = (float)i / steps;
                paint = i == steps;
                probe = Vector2.Lerp(previousHero, hero, fraction);
                sampledCargo = Vector2.Lerp(previousCargo, CargoPosition, fraction);
                DrawActive(Mathf.Lerp(previous, t, fraction));
            }
            EndFrame();
            previous = t; previousHero = hero; previousCargo = CargoPosition;
            if (nextDamage > 0)
            {
                hitMask |= nextHit;
                game.hero.Hurt(nextDamage);
            }
        }

        void DrawActive(float t)
        {
            float x = mark.x, centre = (left + right) * .5f;
            switch (move)
            {
                case OrbitalMove.GroundWave:
                    Wave(Mathf.Lerp(origin.x, left, t), -1);
                    Wave(Mathf.Lerp(origin.x, right, t), 1);
                    break;
                case OrbitalMove.LandingStamp:
                    Ram(x, Mathf.Lerp(6f, .28f, Mathf.Clamp01(t / .53f) * Mathf.Clamp01(t / .53f)),
                        1.7f, t < .76f, 18);
                    break;
                case OrbitalMove.ReturnBlade:
                    float journey = t <= .5f ? Mathf.Clamp01(t / .47f) : 1f - Mathf.Clamp01((t - .53f) / .47f);
                    Vector2 saw = Vector2.Lerp(RailEnd(side), RailEnd(-side), journey) + Up(1.05f);
                    if (paint) Saw(saw, .56f, -side * t * 1080f, Live);
                    Ball(saw, .54f, 17, t < .52f ? 0 : 1);
                    break;
                case OrbitalMove.AnchorLock:
                    // 실제 잠금 표식과 와이어 차단은 앵커의 현재 위치에서 아레나가 처리한다.
                    break;
                case OrbitalMove.CargoSweep:
                    SweepUnder(sampledCargo, Mathf.Clamp01(t * 3.5f), Live, t < .87f);
                    break;
                case OrbitalMove.DropCargo:
                    // 물리 화물이 직접 타격한 뒤 발판이 되므로 같은 위치에서 추가 피해를 주지 않는다.
                    break;
                case OrbitalMove.RisingBeam:
                    RisingBar(Mathf.SmoothStep(0, 1, t), Live, true);
                    break;
                case OrbitalMove.CrossBeam:
                    if (t < .76f) CrossBeams(Live, true);
                    break;
                case OrbitalMove.ShutterFeint:
                    if (t < .76f) Feint(-fakeFacing, ShutterFiring(t) ? Live : Lens, true, ShutterFiring(t), true);
                    break;
                case OrbitalMove.BladeFerry:
                    if (paint) Arrow(sampledCargo + Up(.7f), side, 1.4f);
                    break;
                case OrbitalMove.CoreTrap:
                    if (t < .79f) Ram(x, .09f, 1.22f, true, 17, 1.1f);
                    break;
                case OrbitalMove.SupplyLeak:
                    float half = 2.3f * Mathf.Clamp01(.35f + t * 3.8f);
                    Slash(new Vector2(x - half, .24f), new Vector2(x + half, .24f), .47f, Lens, 15);
                    if (paint) Footprint(x, half, Live);
                    break;
                case OrbitalMove.ReverseRail:
                    if (paint) Arrow(sampledCargo + Up(.7f), side, 1.8f);
                    break;
                case OrbitalMove.ChainDrop:
                    for (int i = 0; i < 4; i++)
                    {
                        float u = (t - i * .17f) / .43f;
                        if (u < 0) { if (paint) Footprint(DropX(i), .85f, Steel); continue; }
                        if (u > 1) continue;
                        float f = Mathf.Clamp01(u / .76f);
                        Ram(DropX(i), Mathf.Lerp(6.5f, .15f, f * f), 1.6f, true, 16, .85f, i);
                    }
                    break;
                case OrbitalMove.SplitShutter:
                    DrawShutter(t < .38f ? Lens : Steel, t < .38f);
                    break;
                case OrbitalMove.PursuitStamp:
                    Vector2 apex = new Vector2(x, Roof - .9f);
                    if (t < .37f) RushPosition = Vector2.Lerp(origin, apex, Mathf.SmoothStep(0, 1, t / .37f));
                    else
                    {
                        float drop = Mathf.Clamp01((t - .37f) / .27f);
                        RushPosition = Vector2.Lerp(apex, new Vector2(x, 1.1f), drop * drop);
                        if (t < .80f) Ram(x, RushPosition.y - .97f, 2.18f, true, 22, 1.45f);
                    }
                    break;
                case OrbitalMove.ClosingSweep:
                    float sweep = Mathf.SmoothStep(0, 1, t);
                    Wave(Mathf.Lerp(left, centre - 1.65f, sweep), 1);
                    Wave(Mathf.Lerp(right, centre + 1.65f, sweep), -1);
                    break;
                case OrbitalMove.CargoRush:
                    float cross = CargoContactTime / ActiveSeconds(OrbitalMove.CargoRush);
                    RushPosition = t < cross ? Vector2.Lerp(origin, RushCross, t / cross)
                        : Vector2.Lerp(RushCross, RushExit, Mathf.Clamp01((t - cross) / (.9f - cross)));
                    if (t < .90f)
                    {
                        int dir = RushExit.x > RushCross.x ? 1 : -1;
                        Slash(RushPosition - Vector2.right * dir * .92f,
                            RushPosition + Vector2.right * dir * .92f, 1.12f, Live, 23);
                    }
                    break;
            }
        }

        Vector2 RushCross => new Vector2(mark.x, 2.35f);
        Vector2 RushExit => new Vector2(mark.x > origin.x ? right - 1 : mark.x < origin.x ? left + 1 :
            side < 0 ? right - 1 : left + 1, 2.35f);
        Vector2 RailEnd(int edge) => new Vector2(edge < 0 ? left + .65f : right - .65f, Floor);
        static Vector2 Up(float amount) => new Vector2(0, amount);
        float DropX(int i) => Mathf.Lerp(left + 1.35f, right - 1.35f, (side < 0 ? i : 3 - i) / 3f);

        void Wave(float x, int direction)
        {
            Vector2 foot = new Vector2(x - direction * .25f, .16f);
            Slash(foot, new Vector2(x, .92f), .44f, Live, 16);
            if (paint) Stroke(foot, foot - Vector2.right * direction * .82f, .20f, Live);
        }

        void SweepUnder(Vector2 cargo, float extent, Color color, bool active)
        {
            Vector2 mouth = new Vector2(cargo.x, cargo.y - .75f);
            float x = Mathf.Clamp(mouth.x - side * 7.4f * extent, left, right);
            Slash(mouth, new Vector2(x, mouth.y), active ? .44f : .07f, color, active ? 18 : 0);
        }

        void RisingBar(float t, Color color, bool active)
        {
            float y = Mathf.Lerp(.18f, 6.35f, t);
            Slash(new Vector2(Mathf.Max(left, mark.x - 4), y),
                new Vector2(Mathf.Min(right, mark.x + 4), y), active ? .48f : .09f, color, active ? 17 : 0);
        }

        void CrossBeams(Color color, bool active)
        {
            float a = Mathf.Clamp(mark.x - 5f, left, right), b = Mathf.Clamp(mark.x + 5f, left, right);
            Slash(new Vector2(a, .24f), new Vector2(b, 5.65f), active ? .43f : .07f, color, active ? 17 : 0);
            Slash(new Vector2(a, 5.65f), new Vector2(b, .24f), active ? .43f : .07f, color, active ? 17 : 0);
        }

        void Feint(int direction, Color color, bool open, bool active, bool showRay)
        {
            // 끝 자세까지 읽을 수 있도록 셔터가 열린 순간부터 실제 방향의 렌즈를 드러낸다.
            Vector2 eye = origin + Vector2.right * direction * .64f;
            if (paint)
            {
                float spacing = open ? .32f : .08f;
                Stroke(eye + Up(spacing), eye + Up(spacing) + Vector2.right * direction * .44f, .11f, color);
                Stroke(eye - Up(spacing), eye - Up(spacing) + Vector2.right * direction * .44f, .11f, color);
            }
            if (showRay)
                Slash(eye, new Vector2(direction > 0 ? right : left, eye.y), active ? .60f : .07f,
                    color, active ? 20 : 0);
        }

        void Ram(float x, float bottom, float width, bool dangerous, float damage, float height = .92f, int hit = 0)
        {
            Vector2 low = new Vector2(x, bottom), high = low + Up(height);
            Slash(low, high, width, dangerous ? Live : Steel, dangerous ? damage : 0, hit);
            if (paint) Stroke(low - Vector2.right * width * .5f, low + Vector2.right * width * .5f, .08f, Lens);
        }

        void DrawShutter(Color color, bool dangerous)
        {
            float x = (left + right) * .5f;
            Slash(new Vector2(x, 2.65f), new Vector2(x, 5.15f), dangerous ? .75f : .08f,
                color, dangerous ? 16 : 0);
            if (paint)
            {
                Arrow(new Vector2(x, 5.7f), -side, 1.05f);
                Arrow(new Vector2(x, 1.95f), -side, 1.05f);
            }
        }

        void BeginFrame() { used = 0; paint = true; test = false; }
        void EndFrame()
        {
            for (int i = used; i < visible; i++) strokes[i].enabled = false;
            visible = used;
        }

        // 장식 테두리에는 피해가 없고, 그려진 몸통과 피해 검사가 동일한 선분과 폭을 사용한다.
        void Slash(Vector2 a, Vector2 b, float width, Color color, float damage, int hit = 0)
        {
            if (paint) Stroke(a, b, width, color);
            if (!test || damage <= 0 || (hitMask & (1 << hit)) != 0) return;
            Vector2 axis = b - a, p = probe - a;
            if (AncientBeamRules.IntersectsBox(axis.x, axis.y, p.x, p.y, width * .5f, .29f, .83f))
                RecordHit(hit, damage);
        }

        void Ball(Vector2 center, float radius, float damage, int hit)
        {
            if (!test || (hitMask & (1 << hit)) != 0) return;
            Vector2 d = probe - center;
            float y = Mathf.Max(0, Mathf.Abs(d.y) - .58f);
            if (d.x * d.x + y * y <= (radius + .27f) * (radius + .27f)) RecordHit(hit, damage);
        }

        void RecordHit(int hit, float damage)
        {
            nextHit |= 1 << hit;
            nextDamage = Mathf.Max(nextDamage, damage);
        }

        void Stroke(Vector2 a, Vector2 b, float width, Color color)
        {
            if (used >= strokes.Length) return;
            var line = strokes[used];
            if (!line)
            {
                line = game.Line("궤도 경로", color, width, 18);
                line.transform.SetParent(root, false);
                line.useWorldSpace = true;
                line.alignment = LineAlignment.View;
                line.textureMode = LineTextureMode.Stretch;
                strokes[used] = line;
            }
            used++;
            line.enabled = true;
            line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = width;
            line.SetPosition(0, a);
            line.SetPosition(1, b);
        }

        void Footprint(float x, float half, Color color)
        {
            Vector2 a = new Vector2(x - half, .10f), b = new Vector2(x + half, .10f);
            Stroke(a, b, .09f, color);
            Stroke(a, a + Up(.3f), .08f, color);
            Stroke(b, b + Up(.3f), .08f, color);
        }

        void Arrow(Vector2 start, int direction, float length)
        {
            Vector2 end = start + Vector2.right * (direction * length);
            Stroke(start, end, .09f, Tell);
            Stroke(end, end + new Vector2(-direction * .35f, .23f), .09f, Tell);
            Stroke(end, end + new Vector2(-direction * .35f, -.23f), .09f, Tell);
        }

        void CrossMark(Vector2 at, float radius, Color color)
        {
            Vector2 d = Vector2.one * radius, e = new Vector2(radius, -radius);
            Stroke(at - d, at + d, .08f, color);
            Stroke(at - e, at + e, .08f, color);
        }

        void Saw(Vector2 at, float radius, float angle, Color color)
        {
            Vector2 previousTip = at + Unit(angle) * radius;
            for (int i = 1; i <= 8; i++)
            {
                Vector2 notch = at + Unit(angle + i * 45f - 20f) * (radius * .57f);
                Vector2 tip = at + Unit(angle + i * 45f) * radius;
                Stroke(previousTip, notch, .11f, color);
                Stroke(notch, tip, .11f, color);
                previousTip = tip;
            }
            CrossMark(at, .14f, Lens);
        }

        static Vector2 Unit(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        public void Clear()
        {
            armed = false;
            previous = 0; hitMask = 0; used = 0;
            EndFrame();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Clear();
            if (root) UnityEngine.Object.Destroy(root.gameObject);
        }
    }
}
