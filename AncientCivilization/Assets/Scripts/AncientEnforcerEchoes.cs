using System;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 생활구 로봇의 공격 기록만 재현한다. 원본 적·보상·저장 상태에는 접근하지 않는다.
    public sealed class AncientEnforcerEchoes : IDisposable
    {
        enum Sequence { None, Single, Barrage, Crossfire, XAndFall }
        enum ShotStyle { Bullet, Fist, Missile, Shell }

        const float RoofHeight = 10f;
        const float XWidth = 1.8f;
        const float XStart = 2.15f;
        const float XEnd = 2.9f;
        const float FallWarningStart = 3.25f;
        const float FallStart = 4.45f;
        const float FallImpact = 4.89f;
        const float FallRadius = 4.1f;
        const int ShellCount = 4;
        const int CrossMissileCount = 12;

        static readonly Color Sky = new Color(.42f, .83f, 1f);
        static readonly Color Ice = new Color(.83f, .97f, 1f);
        static readonly Color Warning = new Color(1f, .68f, .34f);
        static readonly Vector3[] PartOffsets = {
            new Vector3(0, 3.23f), new Vector3(-1.92f, 2.95f), new Vector3(1.92f, 2.95f),
            new Vector3(-2.25f, 1.82f), new Vector3(2.25f, 1.82f), new Vector3(-1.1f, .66f),
            new Vector3(1.1f, .66f), new Vector3(0, 2.58f)
        };
        static readonly int[] PartIds = {0, 1, 1, 2, 2, 3, 3, 7};
        static readonly Sprite[] EchoParts = new Sprite[8];
        static Sprite softSprite;

        sealed class Echo
        {
            public Transform root;
            public readonly SpriteRenderer[] parts = new SpriteRenderer[PartIds.Length];
            public readonly SpriteRenderer[] pixels = new SpriteRenderer[16];
            public SpriteRenderer light, launcher;
        }

        sealed class Shot
        {
            public Transform root;
            public SpriteRenderer core, glow, fist;
            public LineRenderer warning;
            public Vector2 position, velocity, target;
            public float age, life, launchDelay, damage, radius, impactRadius, turn;
            public ShotStyle style;
            public bool active;
        }

        readonly AncientGame game;
        readonly Transform root;
        readonly Material glowMaterial;
        readonly float left, right, floor, center;
        readonly Echo[] echoes = new Echo[2];
        readonly Shot[] shots = new Shot[32];
        readonly LineRenderer[,] xLines = new LineRenderer[2, 3];
        readonly LineRenderer[] fallMarks = new LineRenderer[3];
        readonly LineRenderer[] firingGuides = new LineRenderer[2];
        readonly SpriteRenderer[] impactPixels = new SpriteRenderer[28];
        readonly Vector2[] shellTargets = new Vector2[ShellCount];
        readonly Vector2[] xOrigins = new Vector2[2], xEnds = new Vector2[2];
        Sequence sequence;
        float age, burstDirection;
        int rounds, shells, missiles;
        bool rocketLaunched, xResolved, fallResolved, disposed;

        public bool Active { get { return sequence != Sequence.None && root && root.gameObject.activeInHierarchy; } }
        public bool Telegraphing
        {
            get
            {
                if (!Active) return false;
                switch (sequence)
                {
                    case Sequence.Single: return age < 1.05f || age >= 3.18f && age < 4.52f;
                    case Sequence.Barrage: return age < 1.16f;
                    case Sequence.Crossfire: return age < 1.62f;
                    default: return age < XStart || age >= FallWarningStart && age < FallImpact;
                }
            }
        }

        public AncientEnforcerEchoes(AncientGame game, Transform parent, float left, float right, float floorY)
        {
            this.game = game;
            this.left = left;
            this.right = right;
            floor = floorY;
            center = (left + right) * .5f;
            root = new GameObject("Enforcer reconstructed robot attacks").transform;
            root.SetParent(parent, false);
            Shader glowShader = Resources.Load<Shader>("Shaders/ParticleGlow");
            if (glowShader && glowShader.isSupported) glowMaterial = new Material(glowShader);
            for (int i = 0; i < echoes.Length; i++) echoes[i] = BuildEcho(i);
            for (int i = 0; i < shots.Length; i++) shots[i] = BuildShot(i);
            for (int i = 0; i < 2; i++)
            {
                firingGuides[i] = Line("Shooting direction", 17);
                for (int layer = 0; layer < 3; layer++) xLines[i, layer] = Line("Cross beam", 16 + layer);
            }
            for (int i = 0; i < fallMarks.Length; i++) fallMarks[i] = Line("Center impact boundary", 16 + i);
            for (int i = 0; i < impactPixels.Length; i++) impactPixels[i] = Rectangle("Impact fragment", root, 22);
            xOrigins[0] = new Vector2(left, floor + .58f);
            xEnds[0] = new Vector2(right, floor + 8.92f);
            xOrigins[1] = new Vector2(left, floor + 8.92f);
            xEnds[1] = new Vector2(right, floor + .58f);
            Clear();
        }

        public void Start(bool finalPhase)
        {
            if (disposed) return;
            Enter(finalPhase ? Sequence.Barrage : Sequence.Single);
        }

        void Enter(Sequence next)
        {
            Clear();
            if (!root || next == Sequence.None) return;
            sequence = next;
            root.gameObject.SetActive(true);
            burstDirection = game.hero.transform.position.x < center ? -1f : 1f;
            if (next == Sequence.Barrage)
            {
                // 첫 절반은 현재 경위 쪽, 뒷 절반은 남은 공간을 조준한다. 네 포격의 착탄 시각을 분리한다.
                float target = game.hero.transform.position.x;
                float spacing = (right - left) * .09f;
                for (int i = 0; i < ShellCount; i++)
                    shellTargets[i] = new Vector2(Mathf.Clamp(target + (i - 1.5f) * spacing * -burstDirection, left + 2f, right - 2f), floor + .13f);
            }
        }

        public void Tick(float dt)
        {
            if (!Active || !game || !game.hero || dt <= 0 || game.Paused) return;
            age += dt;
            Vector2 player = game.hero.transform.position;
            HideMarks();
            for (int i = 0; i < shots.Length && Active; i++) if (shots[i].active) StepShot(shots[i], player, dt);
            if (!Active) return;
            switch (sequence)
            {
                case Sequence.Single: TickSingle(player); break;
                case Sequence.Barrage: TickBarrage(player); break;
                case Sequence.Crossfire: TickCrossfire(player); break;
                case Sequence.XAndFall: TickXAndFall(player); break;
            }
        }

        void TickSingle(Vector2 player)
        {
            const float fireStart = 1.05f;
            const float interval = .196f;
            const float rocketTime = 4.52f;
            const float end = 7.3f;
            Vector2 feet = SideFeet(-burstDirection);
            Pose(echoes[0], feet, 1, 1f, Fade(end), Mathf.Clamp01((age - 3.18f) / (rocketTime - 3.18f)), rocketLaunched);
            if (age < fireStart)
                Segment(firingGuides[0], feet + new Vector2(0, 2.44f), new Vector2(player.x, floor + 1.45f), .05f, Warning);
            while (rounds < AncientLivingRules.BulletCount(1) && age >= fireStart + rounds * interval)
                FireBullet(feet, player, rounds++);
            if (age >= 3.18f && age < rocketTime) RocketWarning(feet, player);
            if (age >= rocketTime && !rocketLaunched) FireRockets(feet, player);
            if (age >= end) Clear();
        }

        void TickBarrage(Vector2 player)
        {
            const float end = 6.55f;
            Vector2 front = SideFeet(-burstDirection);
            Vector2 back = new Vector2(Mathf.Lerp(center, SideFeet(burstDirection).x, .72f), floor + 2.6f);
            Pose(echoes[0], front, 1, .99f, Fade(end), 0, false);
            Pose(echoes[1], back, 2, .62f, Fade(end), 0, false);
            if (age < 1.16f) Segment(firingGuides[0], front + new Vector2(0, 2.44f), player, .05f, Warning);
            while (rounds < AncientLivingRules.BulletCount(1) * 2 && age >= 1.16f + rounds * .132f)
                FireBullet(front, player, rounds++);
            while (shells < ShellCount && age >= 1.0f + shells * .72f)
            {
                Vector2 target = shellTargets[shells++];
                Launch(ShotStyle.Shell, new Vector2(target.x, floor + RoofHeight - .52f), target, 11.8f, .86f, 22, 1.85f);
                Flash(back + Vector2.up * 2.2f, false);
            }
            if (age >= end) Enter(Sequence.Crossfire);
        }

        void TickCrossfire(Vector2 player)
        {
            const float rocketTime = 1.62f;
            const float end = 7.35f;
            Vector2 front = SideFeet(burstDirection);
            Vector2 back = new Vector2(Mathf.Lerp(center, SideFeet(-burstDirection).x, .75f), floor + 2.55f);
            Pose(echoes[0], front, 3, 1f, Fade(end), Mathf.Clamp01(age / rocketTime), rocketLaunched);
            Pose(echoes[1], back, 4, .71f, Fade(end), 0, false);
            if (age < rocketTime) RocketWarning(front, player);
            if (age >= rocketTime && !rocketLaunched) FireRockets(front, player);
            while (missiles < CrossMissileCount && age >= 1.43f + missiles * .33f)
            {
                Vector2 edge = MissileOrigin(missiles++);
                Launch(ShotStyle.Missile, edge, player, 15f, .7f, 15);
                Flash(back + Vector2.up * 2.2f, false);
            }
            if (age >= end) Enter(Sequence.XAndFall);
        }

        void TickXAndFall(Vector2 player)
        {
            const float end = 5.52f;
            float fall = Mathf.Clamp01((age - FallStart) / (FallImpact - FallStart));
            Vector2 feet = new Vector2(center, floor + Mathf.Lerp(5.4f, .08f, fall * fall));
            Pose(echoes[0], feet, 5, Mathf.Lerp(.73f, 1.07f, fall), Fade(end) * (1f - Mathf.InverseLerp(FallImpact, FallImpact + .28f, age)), 0f, false);
            if (age < XEnd) ShowCrossBeam(player);
            else if (age < FallImpact) foreach (SpriteRenderer pixel in impactPixels) pixel.enabled = false;
            if (!Active) return;
            if (age >= FallWarningStart && age < FallImpact) ShowFallWarning();
            if (age >= FallImpact && !fallResolved)
            {
                fallResolved = true;
                game.Sound(3);
                game.Shake(.26f);
                Flash(new Vector2(center, floor + .25f), true);
                Vector2 impact = new Vector2(center, floor + .07f);
                if (Mathf.Abs(player.x - center) <= FallRadius + .25f && player.y < floor + 3.4f && !Physics2D.Linecast(impact, player, 1 << 8))
                    game.hero.Hurt(27f);
            }
            if (!Active) return;
            if (age >= FallImpact) ImpactBurst();
            if (age >= end) Clear();
        }

        Vector2 SideFeet(float side)
        {
            return new Vector2(side < 0f ? left + 3.4f : right - 3.4f, floor + .07f);
        }

        Vector2 MissileOrigin(int index)
        {
            int row = index / 3;
            switch (index % 3)
            {
                case 0: return new Vector2(left + .48f, floor + 2.0f + row * 1.5f);
                case 1: return new Vector2(right - .48f, floor + 2.8f + (3 - row) * 1.42f);
                default: return new Vector2(Mathf.Lerp(left + 3, right - 3, row / 3f), floor + RoofHeight - .5f);
            }
        }

        float Fade(float end)
        {
            return Mathf.SmoothStep(0, 1, age / .62f) * Mathf.SmoothStep(0, 1, (end - age) / .44f);
        }

        void FireBullet(Vector2 feet, Vector2 target, int index)
        {
            float face = target.x < feet.x ? -1f : 1f;
            Vector2 nozzle = feet + new Vector2(face * 1.1f, 2.34f + (index % 3) * .17f);
            float spread = (index % 5 - 2) * 3.0f;
            Vector2 aim = nozzle + Rotate((target - nozzle).normalized, spread) * 12f;
            Launch(ShotStyle.Bullet, nozzle, aim, 13.5f, 0, 9.5f);
            if (index % 3 == 0) Flash(nozzle, false);
        }

        void RocketWarning(Vector2 feet, Vector2 player)
        {
            for (int i = 0; i < firingGuides.Length; i++)
                Segment(firingGuides[i], feet + new Vector2(i == 0 ? -2.05f : 2.05f, 3f), player, .07f, new Color(1f, .69f, .43f, .73f));
        }

        void FireRockets(Vector2 feet, Vector2 player)
        {
            rocketLaunched = true;
            for (int i = 0; i < 2; i++)
            {
                Vector2 origin = feet + new Vector2(i == 0 ? -2.34f : 2.34f, 3f);
                Launch(ShotStyle.Fist, origin, player + new Vector2(i == 0 ? -.6f : .6f, 0), 11.4f + i, 0, 19f);
                Flash(origin, true);
            }
            game.Sound(3);
        }

        void Launch(ShotStyle style, Vector2 origin, Vector2 target, float speed, float delay, float damage, float blastRadius = 0)
        {
            foreach (Shot shot in shots)
            {
                if (shot.active) continue;
                Vector2 direction = target - origin;
                if (direction.sqrMagnitude < .0001f) direction = Vector2.down;
                shot.style = style;
                shot.position = origin;
                shot.target = target;
                shot.velocity = direction.normalized * speed;
                shot.age = 0f;
                shot.launchDelay = delay;
                shot.damage = damage;
                shot.impactRadius = blastRadius;
                shot.turn = style == ShotStyle.Fist ? 85f : 0f;
                switch (style)
                {
                    case ShotStyle.Bullet: shot.life = 2.1f; shot.radius = .11f; break;
                    case ShotStyle.Fist: shot.life = 2.55f; shot.radius = .45f; break;
                    case ShotStyle.Shell: shot.life = 1.15f; shot.radius = .27f; break;
                    default: shot.life = 2.65f; shot.radius = .18f; break;
                }
                shot.core.enabled = style != ShotStyle.Fist;
                shot.fist.enabled = style == ShotStyle.Fist;
                Vector2 dimensions = style == ShotStyle.Bullet ? new Vector2(.35f, .15f) : new Vector2(.94f, .27f);
                shot.core.transform.localScale = new Vector3(dimensions.x, dimensions.y, 1);
                shot.core.color = style == ShotStyle.Shell ? Sky : Ice;
                shot.glow.transform.localScale = new Vector3(dimensions.x * 2.3f, dimensions.y * 3.3f, 1);
                shot.root.gameObject.SetActive(true);
                shot.active = true;
                SetShotPose(shot);
                return;
            }
        }

        void StepShot(Shot shot, Vector2 player, float dt)
        {
            float oldAge = shot.age;
            shot.age += dt;
            shot.warning.enabled = false;
            if (shot.age <= shot.launchDelay)
            {
                if (shot.style == ShotStyle.Shell)
                    Segment(shot.warning, shot.target + Vector2.left * shot.impactRadius, shot.target + Vector2.right * shot.impactRadius, .11f, Warning);
                else
                    Segment(shot.warning, shot.position, shot.target, .065f, new Color(1f, .68f, .34f, .68f));
                shot.glow.color = new Color(Sky.r, Sky.g, Sky.b, .42f + .2f * Mathf.Sin(shot.age * 22));
                return;
            }
            float step = Mathf.Min(dt, shot.age - shot.launchDelay);
            if (oldAge <= shot.launchDelay && shot.launchDelay > 0f) Flash(shot.position, false);
            if (shot.turn > 0 && shot.age < 1.3f)
            {
                Vector2 toPlayer = player - shot.position;
                float angle = Vector2.SignedAngle(shot.velocity, toPlayer);
                shot.velocity = Rotate(shot.velocity, Mathf.Clamp(angle, -shot.turn * step, shot.turn * step));
            }
            Vector2 next = shot.position + shot.velocity * step;
            Vector2 delta = next - shot.position;
            RaycastHit2D wall = Physics2D.Linecast(shot.position, next, 1 << 8);
            if (ShotHitsPlayer(shot, delta, player, wall))
            {
                Vector2 impact = shot.position;
                float damage = shot.damage;
                Retire(shot);
                Flash(impact, false);
                game.hero.Hurt(damage);
                return;
            }
            if (shot.style == ShotStyle.Shell && (next.y <= shot.target.y || wall))
            {
                Vector2 impact = wall ? wall.point + wall.normal * .04f : shot.target;
                float damage = shot.damage, radius = shot.impactRadius;
                Retire(shot);
                Flash(impact, true);
                if (Vector2.Distance(player, impact) < radius && !Physics2D.Linecast(impact, player, 1 << 8)) game.hero.Hurt(damage);
                return;
            }
            if (wall || shot.age - shot.launchDelay >= shot.life)
            {
                if (wall && shot.style != ShotStyle.Bullet) Flash(wall.point, false);
                Retire(shot);
                return;
            }
            shot.position = next;
            SetShotPose(shot);
        }

        bool ShotHitsPlayer(Shot shot, Vector2 delta, Vector2 player, RaycastHit2D wall)
        {
            Vector2 difference = player - shot.position;
            float hitTime;
            bool hit = game.hero.bike
                ? AncientProjectileRules.BoxEntry(delta.x, delta.y, difference.x - 1.44f - shot.radius, difference.y - .82f - shot.radius,
                    difference.x + 1.44f + shot.radius, difference.y + .98f + shot.radius, out hitTime)
                : AncientProjectileRules.CircleEntry(delta.x, delta.y, difference.x, difference.y, .60f + shot.radius, out hitTime);
            if (!hit) return false;
            if (!wall) return true;
            Vector2 fromWall = player - wall.point;
            return AncientProjectileRules.BeforeSurface(hitTime, wall.fraction, fromWall.x, fromWall.y, wall.normal.x, wall.normal.y);
        }

        static void Retire(Shot shot)
        {
            shot.active = false;
            shot.warning.enabled = false;
            if (shot.root) shot.root.gameObject.SetActive(false);
        }

        void SetShotPose(Shot shot)
        {
            shot.root.position = shot.position;
            shot.root.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shot.velocity.y, shot.velocity.x) * Mathf.Rad2Deg);
            shot.glow.color = new Color(Sky.r, Sky.g, Sky.b, .6f);
        }

        void ShowCrossBeam(Vector2 player)
        {
            bool firing = age >= XStart;
            if (firing && !xResolved)
            {
                xResolved = true;
                game.Sound(3);
                game.Shake(.17f);
            }
            bool hit = false;
            for (int i = 0; i < 2; i++)
            {
                Vector2 a = xOrigins[i], b = xEnds[i];
                if (!firing)
                {
                    float opacity = .12f + .08f * Mathf.Sin(age * 10);
                    Segment(xLines[i, 0], a, b, XWidth, new Color(1f, .53f, .25f, opacity));
                    Segment(xLines[i, 2], a, b, .07f, new Color(1f, .67f, .34f, .89f));
                    continue;
                }
                float pulse = .75f + .25f * Mathf.Sin(age * 31);
                Segment(xLines[i, 0], a, b, XWidth * 1.54f, new Color(.18f, .61f, 1, .20f * pulse));
                Segment(xLines[i, 1], a, b, XWidth, new Color(.30f, .77f, 1, .65f));
                Segment(xLines[i, 2], a, b, .31f + pulse * .08f, Ice);
                ScatterBeamPixels(i, a, b);
                hit |= AncientLivingRules.BeamHit(a.x, a.y, b.x, b.y, player.x, player.y, XWidth * .5f + .36f);
            }
            if (hit && beamCanHit)
            {
                float previousHP = game.hero.bike ? game.hero.bikeHP : game.hero.hp;
                game.hero.Hurt(24f);
                beamCanHit = Active && (game.hero.bike ? game.hero.bikeHP : game.hero.hp) >= previousHP;
            }
        }

        bool beamCanHit;

        void ScatterBeamPixels(int beam, Vector2 start, Vector2 end)
        {
            int half = impactPixels.Length / 2;
            Vector2 tangent = (end - start).normalized;
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            float tilt = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            for (int i = 0; i < half; i++)
            {
                float t = Mathf.Repeat(i / (float)half + (age - XStart) * (.48f + i % 4 * .08f), 1f);
                SpriteRenderer pixel = impactPixels[beam * half + i];
                pixel.enabled = true;
                pixel.transform.position = Vector2.Lerp(start, end, t) + normal * Mathf.Sin(age * 37f + i * 2.4f) * .82f;
                pixel.transform.rotation = Quaternion.Euler(0, 0, tilt);
                pixel.transform.localScale = new Vector3(.23f + (i % 4) * .10f, .085f, 1);
                pixel.color = new Color(.59f, .9f, 1, .54f + .25f * Mathf.Sin(age * 14f + i));
            }
        }

        void ShowFallWarning()
        {
            float pulse = .45f + .48f * Mathf.Clamp01((age - FallWarningStart) / (FallImpact - FallWarningStart));
            Color warm = new Color(Warning.r, Warning.g, Warning.b, pulse);
            Vector2 leftEdge = new Vector2(center - FallRadius, floor + .13f);
            Vector2 rightEdge = new Vector2(center + FallRadius, floor + .13f);
            Segment(fallMarks[0], leftEdge, rightEdge, .18f, warm);
            Segment(fallMarks[1], leftEdge, leftEdge + Vector2.up * 2.7f, .07f, warm);
            Segment(fallMarks[2], rightEdge, rightEdge + Vector2.up * 2.7f, .07f, warm);
        }

        void ImpactBurst()
        {
            float t = Mathf.Clamp01((age - FallImpact) / .55f);
            for (int i = 0; i < impactPixels.Length; i++)
            {
                SpriteRenderer pixel = impactPixels[i];
                float direction = i < impactPixels.Length / 2 ? -1 : 1;
                float spread = (i % 14) / 13f;
                pixel.enabled = t < 1f;
                pixel.transform.position = new Vector2(center + direction * (1 + 4 * spread) * t, floor + .22f + Mathf.Sin(t * Mathf.PI) * (1.2f + spread * 2f));
                pixel.transform.localScale = new Vector3(.22f + (i % 3) * .14f, .095f, 1);
                pixel.transform.rotation = Quaternion.Euler(0, 0, direction * (i % 5) * 12);
                pixel.color = new Color(.52f, .86f, 1f, 1 - t);
            }
            float reach = Mathf.Lerp(.55f, FallRadius, t);
            Segment(fallMarks[0], new Vector2(center - reach, floor + .2f), new Vector2(center + reach, floor + .2f), .34f * (1 - t), new Color(.51f, .86f, 1f, .67f * (1 - t)));
        }

        void Flash(Vector2 point, bool heavy)
        {
            game.Impact(point, Sky, heavy);
        }

        Echo BuildEcho(int index)
        {
            var echo = new Echo { root = new GameObject("Reconstructed mech " + index).transform };
            echo.root.SetParent(root, false);
            for (int i = 0; i < echo.parts.Length; i++)
                echo.parts[i] = AncientArt.Display("Reconstructed armour", EchoSprite(PartIds[i]), PartOffsets[i], 8 + (i == 7 ? 3 : 0), echo.root);
            echo.light = Light("Echo core light", echo.root, 9);
            echo.light.transform.localPosition = new Vector3(0, 2.72f);
            echo.light.transform.localScale = new Vector3(2.6f, 2.2f, 1f);
            echo.launcher = AncientArt.Display("Rear artillery launcher", EchoSprite(4), new Vector2(0, 4.5f), 10, echo.root);
            for (int i = 0; i < echo.pixels.Length; i++) echo.pixels[i] = Rectangle("Pale blue data shard", echo.root, 12);
            return echo;
        }

        void Pose(Echo echo, Vector2 feet, int serial, float scale, float alpha, float raising, bool fistsAway)
        {
            echo.root.gameObject.SetActive(alpha > .001f);
            echo.root.position = feet;
            echo.root.localScale = Vector3.one * scale;
            for (int i = 0; i < echo.parts.Length; i++)
            {
                Vector3 offset = PartOffsets[i];
                if (i == 3 || i == 4) offset.y += raising * 1.2f;
                if (i <= 4 || i == 7) offset.y += Mathf.Sin(age * 3.9f + serial) * .085f;
                echo.parts[i].transform.localPosition = offset;
                echo.parts[i].enabled = !fistsAway || (i != 3 && i != 4);
                echo.parts[i].color = new Color(.84f + (serial % 3) * .07f, .92f, 1f, alpha * (scale < .8f ? .77f : .95f));
            }
            echo.light.color = new Color(.38f, .78f, 1f, alpha * (.45f + .14f * Mathf.Sin(age * 10f)));
            echo.launcher.enabled = serial == 2 || serial == 4;
            echo.launcher.transform.localRotation = Quaternion.Euler(0, 0, serial == 2 ? 71f : -21f);
            echo.launcher.color = new Color(.69f, .89f, 1f, alpha * .8f);
            for (int i = 0; i < echo.pixels.Length; i++)
            {
                float angle = i * 2.39996f + age * 2.9f;
                float radius = (2.45f + i % 4 * .13f) * (1f + .4f * (1f - alpha));
                SpriteRenderer pixel = echo.pixels[i];
                pixel.enabled = true;
                pixel.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 2.72f + Mathf.Sin(angle) * radius * .57f);
                pixel.transform.localScale = new Vector3(.14f + (i % 4) * .06f, .06f, 1f);
                pixel.transform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 0 : 90);
                pixel.color = new Color(.57f, .88f, 1f, alpha * (.66f + .27f * Mathf.Sin(age * 8 + i)));
            }
        }

        Shot BuildShot(int index)
        {
            var shot = new Shot { root = new GameObject("Echo attack particle " + index).transform };
            shot.root.SetParent(root, false);
            shot.core = Rectangle("Rectangular particle", shot.root, 13);
            shot.glow = Light("Particle light spread", shot.root, 12);
            shot.fist = AncientArt.Display("Rocket fist", EchoSprite(2), Vector2.zero, 14, shot.root);
            shot.fist.transform.localScale = Vector3.one * .61f;
            // 경고선은 회전하는 투사체 밖에서 월드 좌표를 사용한다.
            shot.warning = Line("Echo attack aim warning", 15);
            return shot;
        }

        SpriteRenderer Light(string name, Transform parent, int order)
        {
            if (!softSprite)
            {
                const int width = 32;
                var texture = new Texture2D(width, width, TextureFormat.RGBA32, false)
                {
                    name = "Echo bloom ramp", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color32[width * width];
                for (int y = 0; y < width; y++)
                for (int x = 0; x < width; x++)
                {
                    float dx = (x + .5f) / width * 2 - 1, dy = (y + .5f) / width * 2 - 1;
                    float alpha = Mathf.Clamp01(1f - (dx * dx + dy * dy));
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)(alpha * alpha * 255));
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                softSprite = Sprite.Create(texture, new Rect(0, 0, width, width), Vector2.one * .5f, width);
            }
            SpriteRenderer sprite = AncientArt.Display(name, softSprite, Vector2.zero, order, parent);
            if (glowMaterial) sprite.sharedMaterial = glowMaterial;
            return sprite;
        }

        SpriteRenderer Rectangle(string name, Transform parent, int order)
        {
            GameObject rectangle = game.Box(name, Vector2.zero, Vector2.one, Sky, order);
            rectangle.transform.SetParent(parent, false);
            var renderer = rectangle.GetComponent<SpriteRenderer>();
            if (glowMaterial) renderer.sharedMaterial = glowMaterial;
            return renderer;
        }

        LineRenderer Line(string name, int order)
        {
            LineRenderer line = game.Line(name, Sky, .06f, order);
            line.transform.SetParent(root, false);
            line.useWorldSpace = true;
            line.enabled = false;
            return line;
        }

        static void Segment(LineRenderer line, Vector2 a, Vector2 b, float width, Color color)
        {
            line.SetPosition(0, a);
            line.SetPosition(1, b);
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color;
            line.enabled = true;
        }

        void HideMarks()
        {
            foreach (LineRenderer line in xLines) line.enabled = false;
            foreach (LineRenderer line in fallMarks) line.enabled = false;
            foreach (LineRenderer line in firingGuides) line.enabled = false;
        }

        static Sprite EchoSprite(int id)
        {
            if (EchoParts[id]) return EchoParts[id];
            Sprite original = AncientLivingArt.Part(id);
            Texture2D source = original.texture;
            var pixels = source.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                if (p.a == 0) continue;
                int luma = (p.r * 3 + p.g * 5 + p.b * 2) / 10;
                if (luma < 27) pixels[i] = new Color32(13, 38, 66, p.a);
                else if (luma < 86) pixels[i] = new Color32(36, 116, 160, p.a);
                else if (luma < 159) pixels[i] = new Color32(80, 177, 226, p.a);
                else pixels[i] = new Color32(164, 228, 255, p.a);
            }
            var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                name = "Sky blue mech echo part " + id, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            EchoParts[id] = Sprite.Create(texture, original.rect, Vector2.one * .5f, original.pixelsPerUnit);
            return EchoParts[id];
        }

        static Vector2 Rotate(Vector2 input, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians), sine = Mathf.Sin(radians);
            return new Vector2(input.x * cosine - input.y * sine, input.x * sine + input.y * cosine);
        }

        public void Clear()
        {
            sequence = Sequence.None;
            age = 0;
            rounds = shells = missiles = 0;
            rocketLaunched = xResolved = fallResolved = false;
            beamCanHit = true;
            if (!root) return;
            foreach (Shot shot in shots) Retire(shot);
            foreach (Echo echo in echoes) echo.root.gameObject.SetActive(false);
            foreach (SpriteRenderer pixel in impactPixels) pixel.enabled = false;
            HideMarks();
            root.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Clear();
            if (root) UnityEngine.Object.Destroy(root.gameObject);
            if (glowMaterial) UnityEngine.Object.Destroy(glowMaterial);
        }
    }
}
