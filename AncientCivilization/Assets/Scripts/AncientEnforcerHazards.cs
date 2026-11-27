using System;
using System.Collections.Generic;
using Plugin.PlayerCore;
using UnityEngine;
using Cue = Plugin.AncientCivilization.AncientBossCue;

namespace Plugin.AncientCivilization
{
    public enum AncientEnforcerShot { Bullet, Missile, Plane, Gun }

    // 모든 위험물은 집행관 전투가 소유한다. 정지·단계 전환·재도전에서 함께 정리한다.
    public sealed class AncientEnforcerHazards : IDisposable
    {
        enum Motion { None, Forming, Falling, Resting, Block, Wave, Shot, Blast, Beam, Orbit, Return, Energy }
        const int Capacity = 144;
        const float ArenaHeight = 10f;
        const float Ground = 0f;
        const float EdgeSafety = 3f;
        const float CatchRange = 14f;
        static readonly Color Sky = new Color(.38f, .79f, 1f);
        static readonly Color WhiteBlue = new Color(.9f, .98f, 1f);
        static readonly Color Warn = new Color(1f, .67f, .29f);
        static readonly Vector2[] BlockOffsets = {
            new Vector2(-1, 0), Vector2.zero, Vector2.right, Vector2.up,
            new Vector2(-1, 0), Vector2.zero, Vector2.right, new Vector2(1, 1),
            Vector2.zero, Vector2.up, Vector2.right, Vector2.one,
            new Vector2(-1, 1), Vector2.up, Vector2.zero, Vector2.right
        };

        sealed class Hazard
        {
            public GameObject root;
            public SpriteRenderer body, halo;
            public LineRenderer warning;
            public uint activationTick;
            public uint plugGeneration;
            public BluePlugVisuals plugVisual;
            public SpriteRenderer[] pieces;
            public bool fromForge;
            public Vector2 forgeSize;
            public float forgeAngle;
            public Color forgeColor;
            public bool active, interrupt, container, resolved, hit, reverse, locked;
            public Motion motion;
            public AncientEnforcerShot shot;
            public Vector2 position, origin, end, velocity, size, bossTarget;
            public float age, duration, damage, value, collectionProgress, timeSincePull, angle, floor;
            public int shape;

            public void Retire()
            {
                active = false;
                motion = Motion.None;
                if (plugVisual) plugVisual.Show(Vector2.zero, Vector2.zero, Vector2.zero, BluePlugPhase.Ready);
                root.SetActive(false);
            }
        }

        readonly AncientGame game;
        readonly Transform container;
        readonly List<Hazard> pool = new List<Hazard>(Capacity);
        readonly float left, right;
        // 한 틱에 생성된 파편은 풀의 어느 슬롯에 배정되든 다음 틱부터 움직인다.
        uint simulationTick;
        uint plugGeneration;
        const int RingSegments = 24;
        static readonly Vector2[] RingDirections = CreateRingDirections();
        readonly Vector3[] ringPositions = new Vector3[RingSegments];

        static Vector2[] CreateRingDirections()
        {
            var directions = new Vector2[RingSegments];
            for (int i = 0; i < directions.Length; i++)
            {
                float angle = i * (2f * Mathf.PI / RingSegments);
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            return directions;
        }

        Hazard held;
        int nextBlock;
        bool disposed;
        Material glowMaterial;
        Sprite pixel, glow, chargeMetal, stampedSlug;
        readonly List<Sprite> ownedSprites = new List<Sprite>(4);
        readonly List<Texture2D> ownedTextures = new List<Texture2D>(4);

        public int AllocatedSlots => pool.Count;
        public int DeniedSpawns { get; private set; }
        internal bool PlugObjectHeld => held != null && held.active && held.motion == Motion.Orbit;

        public Action Interrupt;
        public Action<float> AwardEnergy;
        public Action<float> ThrowHit;
        public Func<Vector2> ThrowTarget;

        public AncientEnforcerHazards(AncientGame world, Transform parent, float left, float right)
        {
            game = world;
            this.left = left;
            this.right = right;
            container = new GameObject("집행관 위험물").transform;
            container.SetParent(parent, false);
            // 위험물은 보스가 비행해도 월드 좌표에서 움직인다.
            container.position = Vector3.zero;
        }

        public bool AvailableInterrupt
        {
            get
            {
                foreach (var item in pool)
                    if (item.active && item.interrupt)
                        return true;
                return false;
            }
        }

        public void Clear(bool preserveEnergy = false)
        {
            held = null;
            foreach (var item in pool)
                if (item.active && (!preserveEnergy || item.motion != Motion.Energy))
                    item.Retire();
        }

        public void Dispose()
        {
            if (disposed) return;
            Clear();
            disposed = true;
            if (container) UnityEngine.Object.Destroy(container.gameObject);
            if (glowMaterial) UnityEngine.Object.Destroy(glowMaterial);
            foreach (var sprite in ownedSprites) UnityEngine.Object.Destroy(sprite);
            foreach (var texture in ownedTextures) UnityEngine.Object.Destroy(texture);
        }

        public void Drop(float x, float warning, float damage, bool container = false, bool interrupt = false, float formY = 0f, SpriteRenderer forge = null)
        {
            var item = Take(Motion.Forming);
            if (item == null) return;
            item.container = container;
            item.interrupt = interrupt;
            item.damage = damage;
            item.duration = Mathf.Max(.08f, warning);
            item.size = container ? new Vector2(2.5f, 1.45f) : interrupt ? new Vector2(1.25f, 1.1f) : new Vector2(1.35f, 1.22f);
            item.position = new Vector2(Mathf.Clamp(x, left + item.size.x * .5f, right - item.size.x * .5f),
                Mathf.Clamp(formY > 0f ? formY : 8.6f, 2f, ArenaHeight - .12f - item.size.y * .5f));
            item.floor = FloorAt(item.position.x, item.position.y);
            item.body.sprite = interrupt ? chargeMetal : AncientEnforcerVisuals.ObjectSprite(container ? 1 : 0);
            // 매달린 강철이 다시 투명해지지 않게 직전 조형물의 실제 자세를 인계한다.
            item.fromForge = forge && forge.enabled && forge.sprite == item.body.sprite;
            if (item.fromForge)
            {
                Vector3 scale = forge.transform.lossyScale;
                Vector3 bounds = forge.sprite.bounds.size;
                item.forgeSize = new Vector2(bounds.x*Mathf.Abs(scale.x), bounds.y*Mathf.Abs(scale.y));
                item.forgeAngle = Mathf.DeltaAngle(0f, forge.transform.eulerAngles.z);
                item.forgeColor = forge.color;
                forge.enabled = false;
            }
            EnsurePieces(item);
            DrawForm(item);
            if (!interrupt)
                Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Tell, item.position, Vector2.down, container ? 1.1f : .68f);
        }

        public void Blocks(float targetX, float duration, bool fast)
        {
            var item = Take(Motion.Block);
            if (item == null) return;
            item.locked = false;
            item.position = new Vector2(Mathf.Clamp(targetX, left + 2f, right - 2f), 7.8f);
            item.duration = Mathf.Max(.8f, duration);
            item.damage = fast ? 29f : 21f;
            item.shape = (nextBlock++ % 4) * 4;
            item.value = fast ? 13f : 8f;
            item.size = Vector2.one * .95f;
            EnsurePieces(item);
            DrawBlocks(item, false);
        }

        public void Wave(bool fromLeft, float duration, float damage)
        {
            var item = Take(Motion.Wave);
            if (item == null) return;
            item.reverse = !fromLeft;
            item.duration = Mathf.Max(.8f, duration);
            item.damage = damage;
            item.position = new Vector2(fromLeft ? left : right, ArenaHeight * .5f);
            item.size = new Vector2(1.7f, ArenaHeight);
            EnsurePieces(item);
            DrawWave(item);
            Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Strike, new Vector2(item.position.x, 2.3f),
                fromLeft ? Vector2.right : Vector2.left, .95f);
        }

        public void Shot(Vector2 origin, Vector2 velocity, float damage, AncientEnforcerShot kind = AncientEnforcerShot.Bullet, float lifetime = 3f, bool markLaunch = true)
        {
            var item = Take(Motion.Shot);
            if (item == null) return;
            item.position = origin;
            item.velocity = velocity;
            item.damage = damage;
            item.shot = kind;
            item.duration = Mathf.Max(.06f, lifetime);
            switch (kind)
            {
                case AncientEnforcerShot.Missile:
                    item.body.sprite = AncientEnforcerVisuals.ObjectSprite(4);
                    item.size = new Vector2(1.12f, .37f);
                    break;
                case AncientEnforcerShot.Plane:
                    item.body.sprite = AncientEnforcerVisuals.ObjectSprite(3);
                    item.size = new Vector2(.98f, .54f);
                    break;
                case AncientEnforcerShot.Gun:
                    item.body.sprite = AncientEnforcerVisuals.ObjectSprite(2);
                    item.size = new Vector2(1.48f, .68f);
                    break;
                default:
                    item.body.sprite = stampedSlug;
                    item.size = new Vector2(.48f, .12f);
                    break;
            }
            DrawBody(item, VelocityAngle(item.velocity), kind == AncientEnforcerShot.Bullet ? WhiteBlue : Color.white, 1f);
            if (markLaunch)
                Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Strike, origin, velocity, kind == AncientEnforcerShot.Bullet ? .52f : .81f);
        }

        public void Beam(Vector2 start, Vector2 end, float width, float duration, float damage)
        {
            var item = Take(Motion.Beam);
            if (item == null) return;
            item.origin = start;
            item.end = end;
            item.duration = Mathf.Max(.06f, duration);
            item.damage = damage;
            item.value = Mathf.Max(.05f, width);
            Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Strike, start, end - start, .93f);
            // 같은 선분의 5겹 광선은 집행관 시각 효과가 재생한다.
        }

        public bool TryWireTarget(Vector2 cursor, Vector2 hero, out Vector2 point)
        {
            var item = held == null ? PickWireObject(cursor, hero) : null;
            point = item == null ? Vector2.zero : item.position;
            return item != null;
        }

        public bool TryThrow(Vector2 cursor, Vector2 hero, Vector2 bossCenter)
        {
            if (held != null) return false;
            var item = PickWireObject(cursor, hero);
            return item != null && BeginThrow(item, hero, bossCenter);
        }

        internal bool TryGetPlugObject(int slot, out Vector2 point, out uint generation)
        {
            point = Vector2.zero;
            generation = 0;
            if (held != null || slot < 0 || slot >= pool.Count) return false;
            Hazard item = pool[slot];
            if (!item.active || !IsCuttableCrate(item)) return false;
            point = item.position;
            generation = item.plugGeneration;
            return true;
        }

        internal bool TryUsePlugObject(int slot, uint generation, Vector2 hero, Vector2 bossCenter)
        {
            if (!TryGetPlugObject(slot, out Vector2 point, out uint current) || current != generation ||
                (point - hero).sqrMagnitude > CatchRange * CatchRange || Physics2D.Linecast(hero, point, 1 << 8))
                return false;
            return BeginThrow(pool[slot], hero, bossCenter);
        }

        internal void CancelPlugObject()
        {
            if (!PlugObjectHeld) return;
            Hazard item = held;
            held = null;
            item.motion = Motion.Resting;
            item.position = item.origin;
            item.age = 0;
            item.duration = item.interrupt ? 4.8f : 6f;
            if (item.plugVisual) item.plugVisual.Show(Vector2.zero, Vector2.zero, Vector2.zero, BluePlugPhase.Ready);
            DrawBody(item, 0, WhiteBlue, 1);
        }

        bool BeginThrow(Hazard item, Vector2 hero, Vector2 bossCenter)
        {
            held = item;
            if (!item.plugVisual) item.plugVisual = item.root.AddComponent<BluePlugVisuals>();
            HidePieces(item);
            item.motion = Motion.Orbit;
            item.origin = item.position;
            item.bossTarget = bossCenter;
            item.age = 0f;
            item.collectionProgress = 0f;
            item.duration = AncientLivingRules.OrbitDuration;
            item.angle = Mathf.Atan2(item.position.y - hero.y, item.position.x - hero.x);
            HideTell(item);
            game.Sound(3);
            return true;
        }

        Hazard PickWireObject(Vector2 cursor, Vector2 hero)
        {
            Hazard nearest = null;
            float best = float.MaxValue;
            foreach (var item in pool)
            {
                if (!item.active || !IsCuttableCrate(item)) continue;
                float radius = Mathf.Max(1.05f, item.size.x * .6f);
                float distance = (cursor - item.position).sqrMagnitude;
                if (distance > radius * radius || distance >= best || (hero - item.position).sqrMagnitude > CatchRange * CatchRange)
                    continue;
                if (Physics2D.Linecast(hero, item.position, 1 << 8)) continue;
                nearest = item;
                best = distance;
            }
            return nearest;
        }

        internal bool UnsafeBladeGround(Vector2 point)
        {
            foreach(var item in pool)
            {
                if(!item.active)continue;
                if(item.motion==Motion.Forming || item.motion==Motion.Falling || item.motion==Motion.Block)
                {
                    float width=item.motion==Motion.Block?2f:item.container?3.25f:item.size.x*.65f;
                    if(Mathf.Abs(point.x-item.position.x)<=width && point.y<item.position.y+1f)return true;
                }
                if(item.motion==Motion.Wave && Mathf.Abs(point.x-item.position.x)<1.4f)return true;
                if(item.motion==Motion.Blast && (item.position-point).sqrMagnitude<item.size.x*item.size.x)return true;
            }
            return false;
        }

        internal void BreakObjectsInRadius(Vector2 origin,float radius)
        {
            // 파괴하면서 입자 풀 항목이 늘어날 수 있으므로 기존 개수까지만 순회한다.
            for(int index=pool.Count-1;index>=0 && !disposed;index--)
            {
                var item=pool[index];
                if(!item.active || (!IsCuttableCrate(item) && !(item.motion==Motion.Shot &&
                    (item.shot==AncientEnforcerShot.Missile || item.shot==AncientEnforcerShot.Plane))))continue;
                if((item.position-origin).sqrMagnitude<=radius*radius && !Physics2D.Linecast(origin+Vector2.up*.08f,item.position,1<<8))
                    Break(item,item.container?30f:14f);
            }
        }

        public bool TryBlade(Vector2 origin, Vector2 direction, float reach, float width)
        {
            Hazard nearest = null;
            float best = float.MaxValue;
            foreach (var item in pool)
            {
                if (!item.active) continue;
                bool destructibleShot = item.motion == Motion.Shot &&
                    (item.shot == AncientEnforcerShot.Missile || item.shot == AncientEnforcerShot.Plane);
                if (!IsCuttableCrate(item) && !destructibleShot) continue;
                Vector2 delta = item.position - origin;
                float distance = delta.sqrMagnitude;
                float bladeLength = reach + item.size.x * .35f;
                if (distance >= best || distance > bladeLength * bladeLength ||
                    Vector2.Dot(delta.normalized, direction) < -Mathf.Clamp(width / Mathf.Max(reach, .1f), .15f, .45f))
                    continue;
                if (Physics2D.Linecast(origin, item.position, 1 << 8)) continue;
                best = distance;
                nearest = item;
            }
            if (nearest == null) return false;
            Break(nearest, nearest.container ? 30f : 14f);
            return true;
        }

        public void Collect(Vector2 hero, float radius, float dt)
        {
            float collected = 0f;
            foreach (var item in pool)
            {
                if (!item.active) continue;
                bool looseEnergy = item.motion == Motion.Energy;
                if (!looseEnergy && (!item.container || item.interrupt || item.motion != Motion.Resting)) continue;
                if ((hero - item.position).sqrMagnitude > radius * radius ||
                    Physics2D.Linecast(hero, item.position, 1 << 8))
                {
                    item.collectionProgress = 0f;
                    continue;
                }
                item.collectionProgress += dt;
                item.timeSincePull = 0f;
                float pullSpeed = looseEnergy ? 8f : 3f;
                item.position = Vector2.MoveTowards(item.position, hero, (pullSpeed + item.collectionProgress * 9f) * dt);
                if (looseEnergy ? Vector2.Distance(item.position, hero) > .5f : item.collectionProgress < .8f) continue;
                Vector2 at = item.position;
                collected += looseEnergy ? item.value : 32f;
                item.Retire();
                if (!looseEnergy) game.Impact(at, Sky, false);
            }
            if (collected > 0f) AwardEnergy?.Invoke(collected);
        }

        public float AbsorbEnergy(Vector2 center, float dt)
        {
            float absorbed = 0f;
            foreach (var item in pool)
            {
                if (!item.active || item.motion != Motion.Energy) continue;
                item.collectionProgress += dt;
                item.timeSincePull = 0f;
                item.position = Vector2.MoveTowards(item.position, center, (6f + item.collectionProgress * 8f) * dt);
                if ((item.position - center).sqrMagnitude > .25f) continue;
                absorbed += item.value;
                item.Retire();
            }
            return absorbed;
        }

        public void Tick(float dt)
        {
            if (disposed || !game || !game.hero || dt <= 0f) return;
            // 부모는 보스 생명주기를 따르고, 좌표는 전투장의 월드 공간을 유지한다.
            container.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Vector2 hero = game.hero.transform.position;
            // 충돌로 물체가 추가되어도 새 물체가 같은 틱에 피해를 주지 않는다.
            simulationTick++;
            int count = pool.Count;
            for (int i = 0; i < count; i++)
            {
                var item = pool[i];
                if (!item.active || item.activationTick == simulationTick) continue;
                // 플레이어가 다른 쪽으로 이동하면 그 주변에 다음 저지 물체를 공급할 수 있다.
                if (item.interrupt && item.motion != Motion.Orbit && item.motion != Motion.Return &&
                    Mathf.Abs(item.position.x - hero.x) > 10f)
                {
                    item.Retire();
                    continue;
                }
                item.age += dt;
                item.timeSincePull += dt;
                if (item.timeSincePull > Mathf.Max(.14f, dt * 2.5f)) item.collectionProgress = 0f;
                switch (item.motion)
                {
                    case Motion.Forming:
                        DrawForm(item);
                        if (item.age >= item.duration)
                        {
                            item.motion = Motion.Falling;
                            item.age = 0f;
                            item.body.enabled = true;
                            item.velocity = Vector2.down * (item.interrupt ? 2.5f : 8f);
                            if (!item.interrupt)
                                Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Strike, item.position, Vector2.down, item.container ? 1.04f : .65f);
                            // 물체가 떨어지기 시작하는 프레임에도 바닥의 네 표지는 유지한다.
                            for (int piece=0; piece<12; piece++) item.pieces[piece].enabled = false;
                        }
                        break;
                    case Motion.Falling:
                        TickFall(item, hero, dt);
                        break;
                    case Motion.Resting:
                        if (item.age >= item.duration) item.Retire();
                        else
                        {
                            float fade = Mathf.Clamp01((item.duration - item.age) * 3f);
                            if (item.collectionProgress <= 0f)
                            {
                                float restingHeight = item.floor + item.size.y * .5f;
                                item.position.y = Mathf.MoveTowards(item.position.y, restingHeight, 4f * dt);
                            }
                            DrawBody(item, 0f, Color.white, fade);
                        }
                        break;
                    case Motion.Block:
                        TickBlock(item, hero, dt);
                        break;
                    case Motion.Wave:
                        TickWave(item, hero);
                        break;
                    case Motion.Shot:
                        TickShot(item, hero, dt);
                        break;
                    case Motion.Blast:
                        TickBlast(item, hero);
                        break;
                    case Motion.Beam:
                        // 긴 프레임이 빔의 전체 수명을 지나도 첫 판정은 수행한다.
                        if (item.age - dt < item.duration)
                            TryAreaDamage(item, ContainsSegment(item.origin, item.end, hero, item.value * .5f + .3f));
                        if (item.active && item.age >= item.duration) item.Retire();
                        break;
                    case Motion.Orbit:
                        TickOrbit(item, hero);
                        break;
                    case Motion.Return:
                        TickReturn(item, dt);
                        break;
                    case Motion.Energy:
                        TickEnergy(item, dt);
                        break;
                }
                // 피해로 인한 씬 종료 또는 사망에 따른 보스 삭제 이후에는 공격을 진행하지 않는다.
                if (disposed || !container || !game.hero) break;
                hero = game.hero.transform.position;
            }
        }

        void TickFall(Hazard item, Vector2 hero, float dt)
        {
            float bottom = item.floor + item.size.y * .5f;
            float previousY = item.position.y;
            item.velocity.y = Mathf.Max(item.interrupt ? -5.5f : -26f, item.velocity.y - (item.interrupt ? 8f : 33f) * dt);
            item.position.y = Mathf.Max(bottom, previousY + item.velocity.y * dt);
            if (item.damage > 0f)
            {
                bool intersects = Mathf.Abs(hero.x - item.position.x) <= item.size.x * .5f + .36f &&
                    hero.y >= item.position.y - item.size.y * .5f - .65f &&
                    hero.y <= previousY + item.size.y * .5f + .64f;
                TryAreaDamage(item, intersects);
                if (!item.active) return;
            }
            DrawBody(item, 0f, Color.white, 1f);
            if (item.position.y > bottom + .01f) return;
            Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Contact, new Vector2(item.position.x, item.floor + .13f),
                Vector2.up, item.container ? 1.25f : item.interrupt ? .47f : .8f);
            HideTell(item);
            // 착지 표지만 지워서 베거나 잡을 수 있는 물체의 모서리는 착지 프레임에도 남긴다.
            for (int marker=12; marker<16; marker++) item.pieces[marker].enabled = false;
            if (item.interrupt || item.container)
            {
                item.motion = Motion.Resting;
                item.age = 0f;
                item.duration = item.interrupt ? 4.8f : 6f;
                if (item.container)
                {
                    var at = new Vector2(item.position.x, item.floor + .16f);
                    // 낙하와 파동은 같은 공격의 피해를 공유한다.
                    if (!item.hit && Mathf.Abs(hero.x - at.x) <= 3.25f && hero.y < at.y + 1.7f)
                        TryAreaDamage(item, true);
                    if (item.active) RadiusOutline(item, at, 3.25f, .45f, Sky, .12f);
                }
            }
            else item.Retire();
        }

        void TickBlock(Hazard item, Vector2 hero, float dt)
        {
            if (!item.locked)
            {
                if (item.age >= item.duration - .42f)
                {
                    item.locked = true;
                    item.floor = FloorAt(item.position.x, item.position.y);
                    Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Tell, item.position, Vector2.down, .8f);
                }
                else
                {
                    float ramp = Mathf.Clamp01(item.age / item.duration);
                    item.position.x = Mathf.MoveTowards(item.position.x, Mathf.Clamp(hero.x, left + 2f, right - 2f),
                        Mathf.Lerp(1.4f, item.value, ramp) * dt);
                }
            }
            bool dropped = item.age >= item.duration;
            if (item.age >= item.duration + .7f)
            {
                item.Retire();
                return;
            }
            DrawBlocks(item, dropped);
            if (!dropped || item.resolved) return;
            item.resolved = true;
            game.Sound(4);
            Cue.Play(game, Cue.Style.Enforcer, Cue.Beat.Contact, new Vector2(item.position.x, item.floor + .09f), Vector2.up, 1.18f);
            // 하드드랍은 그 블록이 지나간 열을 공격한다. 잠근 뒤에는 추적하지 않는다.
            Rect column = BlockColumn(item);
            TryAreaDamage(item, column.Contains(hero));
        }

        static Rect BlockColumn(Hazard item)
        {
            float minX = BlockOffsets[item.shape].x;
            float maxX = minX;
            float maxY = BlockOffsets[item.shape].y;
            for (int i = 1; i < 4; i++)
            {
                Vector2 offset = BlockOffsets[item.shape + i];
                minX = Mathf.Min(minX, offset.x);
                maxX = Mathf.Max(maxX, offset.x);
                maxY = Mathf.Max(maxY, offset.y);
            }
            float horizontalReach = item.size.x * .5f + .36f;
            float top = item.position.y - 2.1f + maxY + item.size.y * .5f + .64f;
            return Rect.MinMaxRect(item.position.x + minX - horizontalReach, item.floor,
                item.position.x + maxX + horizontalReach, top);
        }

        void TickWave(Hazard item, Vector2 hero)
        {
            float previousX = item.position.x;
            DrawWave(item);
            bool safe = item.reverse ? hero.x <= left + EdgeSafety : hero.x >= right - EdgeSafety;
            if (item.age >= .72f && !safe)
            {
                float minimum = Mathf.Min(previousX, item.position.x) - .88f;
                float maximum = Mathf.Max(previousX, item.position.x) + .88f;
                TryAreaDamage(item, hero.x >= minimum && hero.x <= maximum && hero.y < ArenaHeight);
            }
            if (item.active && item.age >= item.duration) item.Retire();
        }

        void TickShot(Hazard item, Vector2 hero, float dt)
        {
            if (item.age >= item.duration)
            {
                FinishShot(item);
                return;
            }
            if (item.shot == AncientEnforcerShot.Plane)
            {
                Vector2 toward = hero - item.position;
                float speed = item.velocity.magnitude;
                float angle = Mathf.MoveTowardsAngle(VelocityAngle(item.velocity), VelocityAngle(toward), 2.65f * Mathf.Rad2Deg * dt);
                item.velocity = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * speed;
            }
            if (item.shot == AncientEnforcerShot.Gun) item.velocity += Vector2.down * 9f * dt;
            Vector2 next = item.position + item.velocity * dt;
            Vector2 delta = next - item.position;
            Vector2 target = hero - item.position;
            float hitRadius = .53f + item.size.y * .42f;
            var wall = Physics2D.Linecast(item.position, next + delta.normalized * hitRadius, 1 << 8);
            bool heroHit = AncientProjectileRules.CircleEntry(delta.x, delta.y, target.x, target.y, hitRadius, out float entry);
            if (wall && heroHit)
            {
                Vector2 fromSurface = hero - wall.point;
                heroHit = AncientProjectileRules.BeforeSurface(entry, wall.distance / Mathf.Max(.001f, delta.magnitude),
                    fromSurface.x, fromSurface.y, wall.normal.x, wall.normal.y);
            }
            if (heroHit)
            {
                item.position += delta * entry;
                float damage = item.damage;
                bool gunInFlight = item.shot == AncientEnforcerShot.Gun;
                FinishShot(item);
                if (!gunInFlight) game.hero.Hurt(damage);
                return;
            }
            if (wall && wall.distance <= delta.magnitude)
            {
                item.position = wall.point + wall.normal * .08f;
                FinishShot(item);
                return;
            }
            item.position = next;
            DrawBody(item, item.shot == AncientEnforcerShot.Gun ? item.age * 400f : VelocityAngle(item.velocity), WhiteBlue, 1f);
        }

        void FinishShot(Hazard item)
        {
            Vector2 at = item.position;
            bool gunInFlight = item.shot == AncientEnforcerShot.Gun;
            float damage = item.damage;
            item.Retire();
            if (gunInFlight)
            {
                var blast = Take(Motion.Blast);
                if (blast == null) return;
                blast.position = at;
                blast.duration = .65f;
                blast.value = 2.8f;
                blast.damage = damage;
                DrawBlast(blast);
            }
            else game.Impact(at, Sky, false);
        }

        void TickBlast(Hazard item, Vector2 hero)
        {
            if (item.age > item.duration + .48f)
            {
                item.Retire();
                return;
            }
            DrawBlast(item);
            if (item.age < item.duration) return;
            if (!item.resolved)
            {
                item.resolved = true;
                game.Sound(4);
                game.Impact(item.position, Sky, true);
                game.Shake(.12f);
            }
            TryAreaDamage(item, Vector2.Distance(hero, item.position) <= item.value + .35f);
        }

        void TickOrbit(Hazard item, Vector2 hero)
        {
            float t = Mathf.Clamp01(item.age / item.duration);
            float angle = item.angle + t * Mathf.PI * 2f;
            float orbitRadius = Mathf.Lerp(1.1f, 1.9f, t);
            Vector2 orbit = hero + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .58f) * orbitRadius;
            orbit.x = Mathf.Clamp(orbit.x, left + .5f, right - .5f);
            orbit.y = Mathf.Clamp(orbit.y, .55f, ArenaHeight - .6f);
            Vector2 hand = game.hero.WireOrigin;
            Vector2 next = Vector2.Lerp(item.origin, orbit, Mathf.Clamp01(t * 4f));
            var wall = Physics2D.Linecast(hand, next, 1 << 8);
            item.position = wall ? wall.point + wall.normal * .18f : next;
            DrawBody(item, t * -360f, WhiteBlue, 1f);
            Vector2 radial = item.position - hand;
            if (item.plugVisual) item.plugVisual.Show(hand, item.position, new Vector2(-radial.y, radial.x), BluePlugPhase.Winding);
            if (t < 1f) return;
            if (item.plugVisual) item.plugVisual.Show(Vector2.zero, Vector2.zero, Vector2.zero, BluePlugPhase.Ready);
            item.motion = Motion.Return;
            item.age = 0f;
            item.duration = 1.55f;
            held = null;
            HideTell(item);
            game.Sound(5);
        }

        void TickReturn(Hazard item, float dt)
        {
            Vector2 previous = item.position;
            Vector2 target = ThrowTarget == null ? item.bossTarget : ThrowTarget();
            Vector2 next = Vector2.MoveTowards(previous, target, 26f * dt);
            var wall = Physics2D.Linecast(previous, next, 1 << 8);
            if (wall)
            {
                item.position = wall.point;
                // 지형에 닿은 투척물로는 충전 저지를 발동시키지 않는다.
                item.interrupt = false;
                Break(item, item.container ? 28f : 16f);
                return;
            }
            item.position = next;
            DrawBody(item, item.age * 480f, WhiteBlue, 1f);
            bool hitTarget = (next - target).sqrMagnitude <= .32f * .32f;
            if (!hitTarget && item.age < item.duration) return;
            bool stopCharge = hitTarget && item.interrupt;
            bool heavy = item.container;
            item.interrupt = stopCharge;
            Break(item, heavy ? 24f : 14f);
            if (!stopCharge && hitTarget && !disposed) ThrowHit?.Invoke(heavy ? 58f : 45f);
        }

        void Break(Hazard item, float energy)
        {
            bool stopCharge = item.interrupt;
            Vector2 point = item.position;
            item.Retire();
            game.Impact(point, Sky, true);
            if (stopCharge)
            {
                Interrupt?.Invoke();
                return;
            }
            EmitEnergy(point, energy);
        }

        void EmitEnergy(Vector2 origin, float amount)
        {
            const int count = 5;
            for (int i = 0; i < count; i++)
            {
                var mote = Take(Motion.Energy);
                if (mote == null) return;
                mote.position = origin;
                mote.size = new Vector2(.24f, .085f);
                mote.duration = 7f;
                mote.value = amount / count;
                mote.velocity = new Vector2((i - 2) * 1.4f, 2.6f + i % 3 * .6f);
                mote.floor = FloorAt(origin.x, origin.y) + .15f;
                Place(mote.body, mote.position, mote.size, i * 21f, Sky);
            }
        }

        void TickEnergy(Hazard item, float dt)
        {
            if (item.age >= item.duration)
            {
                item.Retire();
                return;
            }
            if (item.collectionProgress <= 0f)
            {
                item.velocity.y -= 9f * dt;
                item.position += item.velocity * dt;
                if (item.position.y < item.floor)
                {
                    item.position.y = item.floor;
                    item.velocity = new Vector2(item.velocity.x * .32f, Mathf.Max(0f, -item.velocity.y * .28f));
                }
                item.position.x = Mathf.Clamp(item.position.x, left + .2f, right - .2f);
            }
            DrawBody(item, 25f + item.age * 128f, Sky, Mathf.Clamp01((item.duration - item.age) * 2f));
        }

        float FloorAt(float x, float above)
        {
            var support = Physics2D.Raycast(new Vector2(x, above), Vector2.down, ArenaHeight + 2f, 1 << 8);
            return support ? support.point.y : Ground;
        }

        static bool IsCuttableCrate(Hazard item)
        {
            return (item.container || item.interrupt) && (item.motion == Motion.Falling || item.motion == Motion.Resting);
        }

        void TryAreaDamage(Hazard item, bool inside)
        {
            if (item.hit || !inside || item.damage <= 0f) return;
            item.hit = true;
            game.hero.Hurt(item.damage);
        }

        static bool ContainsSegment(Vector2 start, Vector2 end, Vector2 point, float radius)
        {
            return AncientLivingRules.BeamHit(start.x, start.y, end.x, end.y, point.x, point.y, radius);
        }

        static float VelocityAngle(Vector2 velocity)
        {
            return Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        }

        Hazard Take(Motion motion)
        {
            if (disposed || !container) return null;
            WarmArt();
            Hazard item = null;
            foreach (var candidate in pool)
                if (!candidate.active)
                {
                    item = candidate;
                    break;
                }
            if (item == null)
            {
                // 전조와 필수 저지 물체가 탄막 상한 때문에 생략되지 않게 한다.
                if (pool.Count == Capacity)
                {
                    if (motion == Motion.Shot)
                    {
                        DeniedSpawns++;
                        return null;
                    }
                    foreach (var candidate in pool)
                        if (candidate.motion == Motion.Shot && candidate.shot == AncientEnforcerShot.Bullet)
                        {
                            item = candidate;
                            break;
                        }
                    if (item == null)
                    {
                        DeniedSpawns++;
                        return null;
                    }
                    item.Retire();
                }
                else
                {
                    item = CreateActor();
                    pool.Add(item);
                }
            }
            item.motion = motion;
            item.activationTick = simulationTick;
            item.plugGeneration = ++plugGeneration;
            item.active = true;
            item.interrupt = item.container = item.resolved = item.hit = item.reverse = item.locked = false;
            item.age = item.damage = item.value = item.collectionProgress = item.timeSincePull = item.angle = item.floor = 0f;
            item.duration = 1f;
            item.position = item.origin = item.end = item.velocity = item.bossTarget = Vector2.zero;
            item.size = Vector2.one;
            item.body.sprite = pixel;
            item.body.enabled = false;
            item.halo.enabled = false;
            HideTell(item);
            HidePieces(item);
            item.root.SetActive(true);
            return item;
        }

        Hazard CreateActor()
        {
            var root = new GameObject("집행관 공격 물체");
            root.transform.SetParent(container, false);
            SpriteRenderer halo = Renderer(root.transform, "발광 번짐", glow, 13);
            if (glowMaterial) halo.sharedMaterial = glowMaterial;
            return new Hazard
            {
                root = root,
                body = Renderer(root.transform, "물체 코어", pixel, 14),
                halo = halo
            };
        }

        static SpriteRenderer Renderer(Transform parent, string label, Sprite sprite, int order)
        {
            var renderer = new GameObject(label).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        void EnsurePieces(Hazard item)
        {
            if (item.pieces != null) return;
            item.pieces = new SpriteRenderer[16];
            for (int i = 0; i < item.pieces.Length; i++)
            {
                item.pieces[i] = Renderer(item.root.transform, "하늘색 직사각형 입자", pixel, 15);
                item.pieces[i].enabled = false;
            }
        }

        static void HidePieces(Hazard item)
        {
            if (item.pieces == null) return;
            foreach (var piece in item.pieces) piece.enabled = false;
        }

        static void HideTell(Hazard item)
        {
            if (item.warning) item.warning.enabled = false;
        }

        static void Place(SpriteRenderer sprite, Vector2 at, Vector2 size, float degrees, Color color)
        {
            sprite.enabled = true;
            sprite.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, degrees));
            Vector3 artSize = sprite.sprite.bounds.size;
            sprite.transform.localScale = new Vector3(size.x / artSize.x, size.y / artSize.y, 1f);
            sprite.color = color;
        }

        void DrawBody(Hazard item, float degrees, Color color, float opacity)
        {
            color.a *= opacity;
            float pulse = .83f + .17f * Mathf.Sin(item.age * 24f);
            Place(item.body, item.position, item.size, degrees, color);
            bool smallShot = item.motion == Motion.Shot && item.shot == AncientEnforcerShot.Bullet;
            // 탄막에서는 금속탄 하나하나가 읽히도록 본체보다 큰 원형 잔광을 줄인다.
            Vector2 fringe = smallShot ? new Vector2(.24f, .17f) : new Vector2(.78f, .65f);
            Place(item.halo, item.position, item.size + fringe, degrees, Tint(Sky, pulse * opacity * (smallShot ? .24f : .46f)));
            if (item.motion == Motion.Falling) DrawLandingSeal(item, 1f);
            if (item.interrupt && IsCuttableCrate(item))
                DrawReleaseClamps(item, opacity);
            if (item.warning && item.motion == Motion.Resting && item.container && item.age < .25f)
            {
                float t = Mathf.Clamp01(item.age / .25f);
                RadiusOutline(item, new Vector2(item.position.x, item.floor + .17f), Mathf.Lerp(.85f, 3.25f, t), .45f, Sky, .14f * (1f - t));
            }
            else if (item.motion == Motion.Resting) HideTell(item);
        }

        // 네 개의 열린 모서리는 충전 저지용 물체가 실제로 베기·견인 가능한 동안만 표시한다.
        static void DrawReleaseClamps(Hazard item, float opacity)
        {
            float gap = .08f + .05f * Mathf.Cos(item.age * 6f);
            Vector2 half = item.size * .5f + Vector2.one * gap;
            const float notch = .24f;
            Color color = Tint(WhiteBlue, .95f * opacity);
            for (int i = 0; i < 4; i++)
            {
                float x = (i & 1) == 0 ? -1f : 1f;
                float y = i < 2 ? -1f : 1f;
                Vector2 corner = item.position + new Vector2(x * half.x, y * half.y);
                Place(item.pieces[i * 2], corner - new Vector2(x * notch * .5f, 0f), new Vector2(notch, .055f), 0f, color);
                Place(item.pieces[i * 2 + 1], corner - new Vector2(0f, y * notch * .5f), new Vector2(.055f, notch), 0f, color);
            }
        }

        void DrawForm(Hazard item)
        {
            float t = Mathf.Clamp01(item.age / item.duration);
            float build = item.fromForge ? Mathf.Lerp(.8f,1f,t) : t;
            float radius = Mathf.Lerp(item.container ? 2.25f : 1.55f, .06f, Mathf.SmoothStep(0f, 1f, build));
            // 사각 파편 열두 장이 회전한 뒤 금속 표면의 4×3 격자에 물리듯 모인다.
            float assemble = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.48f, .92f, build));
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 2.39996f + item.age * Mathf.Lerp(6f, 20f, t);
                float spread = radius * Mathf.Lerp(.64f, 1.15f, (i % 5) * .25f);
                Vector2 orbit = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .56f) * spread;
                Vector2 cell = new Vector2(((i % 4) - 1.5f) * item.size.x * .22f, ((i / 4) - 1) * item.size.y * .26f);
                Vector2 p = item.position + Vector2.Lerp(orbit,cell,assemble);
                float finalAngle = (i * 2.39996f + .92f * item.duration * Mathf.Lerp(6f,20f,.92f)) * Mathf.Rad2Deg + 90f;
                float rotation = Mathf.Lerp(angle * Mathf.Rad2Deg + 90f, Mathf.Round(finalAngle/180f)*180f, assemble);
                Place(item.pieces[i], p, new Vector2(.26f + i % 4 * .045f, .075f), rotation,
                    Tint(i % 4 == 0 ? WhiteBlue : Sky, .88f - .63f*assemble));
            }
            float settle = Mathf.SmoothStep(0f,1f,t);
            Vector2 shellSize = item.fromForge ? Vector2.Lerp(item.forgeSize,item.size,settle) : item.size * Mathf.Lerp(.72f,1f,t);
            float shellAngle = item.fromForge ? Mathf.Lerp(item.forgeAngle,0f,settle) : 0f;
            Color shellColor = item.fromForge ? Color.Lerp(item.forgeColor,Color.white,settle)
                : Tint(Color.white,Mathf.SmoothStep(0f,1f,(t-.28f)/.72f));
            Place(item.body,item.position,shellSize,shellAngle,shellColor);
            Place(item.halo, item.position, item.size * (1.65f + radius * .32f), 0f, Tint(Sky, .47f));
            DrawLandingSeal(item, t);
        }

        void DrawLandingSeal(Hazard item, float progress)
        {
            // 표지의 발은 낙하 중 접촉할 수 있는 플레이어 중심의 한계에 고정한다.
            // 컨테이너의 외곽 파동(3.25)과 직접 낙하 열은 동시에 따로 읽힌다.
            float reach = item.size.x*.5f + (item.damage > 0f ? .36f : 0f);
            float radius = item.container ? 3.25f : reach;
            Vector2 foot = new Vector2(item.position.x, item.floor + .09f);
            Color color = item.interrupt ? Sky : Warn;
            RadiusOutline(item, foot, radius, item.container ? .16f : .11f, Tint(color,.74f), .045f + progress*.025f);
            float height = Mathf.Lerp(.30f,.52f,progress);
            for (int side=0; side<2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                Vector2 edge = foot + Vector2.right * (reach*sign);
                Place(item.pieces[12+side*2], edge + Vector2.up*(height*.5f), new Vector2(.065f,height),0f,color);
                Place(item.pieces[13+side*2], edge - Vector2.right*(sign*.13f), new Vector2(.26f,.065f),0f,color);
            }
        }

        void DrawBlocks(Hazard item, bool dropped)
        {
            float y = dropped ? item.floor + .49f : item.position.y - Mathf.Clamp01(item.age / item.duration) * 2.1f;
            Vector2 at = new Vector2(item.position.x, y);
            Color blockColor = item.locked ? WhiteBlue : Sky;
            for (int i = 0; i < 4; i++)
            {
                Vector2 point = at + BlockOffsets[item.shape + i];
                Place(item.pieces[i * 2], point, item.size, 0f, Tint(Sky, .93f));
                Place(item.pieces[i * 2 + 1], point + new Vector2(0f, .23f), new Vector2(.8f, .23f), 0f, blockColor);
            }
            Place(item.halo, at + Vector2.up * .38f, new Vector2(4f, 3.3f), 0f, Tint(Sky, .4f));
            if (dropped)
            {
                HideTell(item);
                return;
            }
            LineRenderer tell = Tell(item, 4, true);
            Rect column = BlockColumn(item);
            tell.startColor = tell.endColor = Tint(item.locked ? Warn : Sky, item.locked ? .9f : .48f);
            tell.startWidth = tell.endWidth = item.locked ? .07f : .035f;
            tell.SetPosition(0, new Vector2(column.xMin, column.yMin));
            tell.SetPosition(1, new Vector2(column.xMax, column.yMin));
            tell.SetPosition(2, new Vector2(column.xMax, column.yMax));
            tell.SetPosition(3, new Vector2(column.xMin, column.yMax));
        }

        void DrawWave(Hazard item)
        {
            float begin = item.reverse ? right : left;
            float stop = item.reverse ? left + EdgeSafety : right - EdgeSafety;
            float t = Mathf.Clamp01((item.age - .72f) / Mathf.Max(.08f, item.duration - .72f));
            item.position.x = Mathf.Lerp(begin, stop, t);
            float intensity = item.age < .72f ? .25f + .16f * Mathf.Sin(item.age * 18f) : 1f;
            float direction = item.reverse ? -1f : 1f;
            float front = item.position.x + direction * .88f;
            front = item.reverse ? Mathf.Max(stop,front) : Mathf.Min(stop,front);
            Place(item.halo, item.position, new Vector2(2.55f, ArenaHeight), 0f, Tint(Sky, .32f * intensity));
            Place(item.body, new Vector2(front,ArenaHeight*.5f), new Vector2(.06f,ArenaHeight), 0f,
                Tint(WhiteBlue,.72f*intensity));
            for (int i = 0; i < item.pieces.Length; i++)
            {
                // 위험 전면 뒤에 직사각형 띠가 물결처럼 붙는다. 끝의 안전 발판은 덮지 않는다.
                float crest = .26f + .17f*Mathf.Sin(i*.68f - item.age*9f);
                float tip = item.position.x + direction*(.88f-crest);
                tip = item.reverse ? Mathf.Max(stop,tip) : Mathf.Min(stop,tip);
                float tail = item.position.x - direction*(.23f + .16f*(i%4));
                Vector2 pos = new Vector2((tip+tail)*.5f, .26f + i*.61f);
                Place(item.pieces[i], pos, new Vector2(Mathf.Abs(tip-tail), .1f + (i%3)*.09f), 0f,
                    Tint(i % 3 == 0 ? WhiteBlue : Sky, intensity));
            }
            float edge = item.reverse ? left : right;
            Segment(item, new Vector2(edge, .12f), new Vector2(stop, .12f), new Color(.56f, 1f, .88f), .14f);
        }

        void DrawBlast(Hazard item)
        {
            float t = item.age - item.duration;
            bool fired = t >= 0f;
            float progress = Mathf.Clamp01(t / .48f);
            RadiusOutline(item, item.position, item.value, 1f, fired ? Sky : Warn, fired ? .17f * (1f - progress) + .03f : .065f);
            if (fired)
                Place(item.halo, item.position, Vector2.one * item.value * 2.45f, 0f, Tint(Sky, .75f * (1f - progress)));
        }

        void Segment(Hazard item, Vector2 start, Vector2 end, Color color, float width)
        {
            SetLine(Tell(item, 2, false), start, end, color, width);
        }

        static void SetLine(LineRenderer line, Vector2 start, Vector2 end, Color color, float width)
        {
            line.enabled = true;
            line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = width;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        void RadiusOutline(Hazard item, Vector2 at, float radius, float heightRatio, Color color, float width)
        {
            var ring = Tell(item, RingSegments, true);
            ring.startColor = ring.endColor = color;
            ring.startWidth = ring.endWidth = Mathf.Max(.01f, width);
            for (int i = 0; i < RingSegments; i++)
            {
                Vector2 axis = RingDirections[i];
                ringPositions[i] = new Vector3(at.x + axis.x * radius, at.y + axis.y * radius * heightRatio, 0f);
            }
            ring.SetPositions(ringPositions);
        }

        LineRenderer Tell(Hazard item, int points, bool loop)
        {
            if (!item.warning)
            {
                item.warning = game.Line("공격 범위 예고", Warn, .05f, 15);
                item.warning.transform.SetParent(item.root.transform, true);
            }
            item.warning.enabled = true;
            if (item.warning.loop != loop) item.warning.loop = loop;
            if (item.warning.positionCount != points) item.warning.positionCount = points;
            return item.warning;
        }

        static Color Tint(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        void WarmArt()
        {
            if (pixel) return;
            Shader shader = Resources.Load<Shader>("Shaders/ParticleGlow");
            if (shader && shader.isSupported) glowMaterial = new Material(shader);
            pixel = BuildSprite("하늘색 입자 패널", 1, 1, (x, y) => Color.white);
            glow = BuildSprite("입자 광륜", 24, 24, (x, y) =>
            {
                float dx = (x - 11.5f) / 11.5f;
                float dy = (y - 11.5f) / 11.5f;
                float intensity = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                return Tint(Color.white, intensity * intensity);
            }, true);
            // 후미 절개·금속 윗면·앞쪽 두 눈금이 있는 압인탄. 모든 탄이 같은 스프라이트를 공유한다.
            stampedSlug = BuildSprite("집행관 압인탄", 24, 8, (x,y) =>
            {
                if (x < 2 && (y < 2 || y > 5) || x > 20 && Mathf.Abs(y - 3.5f) > (24 - x)*.75f)
                    return Color.clear;
                if (y == 0 || y == 7 || x == 0) return new Color(.10f,.17f,.24f);
                if (x == 4 || x == 16) return new Color(.18f,.28f,.37f);
                if (y == 5 || x >= 19) return WhiteBlue;
                return y < 3 ? new Color(.28f,.43f,.53f) : Sky;
            });
            const int width = 20;
            const int height = 18;
            chargeMetal = BuildSprite("충전 저지용 낙하물", width, height, (x, y) =>
            {
                if ((x == 0 || x == width - 1) && (y < 2 || y >= height - 2)) return Color.clear;
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1) return new Color(.06f, .14f, .21f);
                if (x <= 2 || x >= width - 3 || y <= 2 || y >= height - 3) return new Color(.39f, .56f, .65f);
                if (x == width / 2 || y == height / 2) return WhiteBlue;
                if ((x == 4 || x == width - 5) && (y == 4 || y == height - 5)) return Sky;
                return y > height / 2 ? new Color(.34f, .51f, .61f) : new Color(.19f, .31f, .4f);
            });
        }

        Sprite BuildSprite(string name, int width, int height, Func<int, int, Color> pixelColor, bool soft = false)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = soft ? FilterMode.Bilinear : FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = pixelColor(x, y);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(.5f, .5f), width, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            ownedTextures.Add(texture);
            ownedSprites.Add(sprite);
            return sprite;
        }
    }
}
