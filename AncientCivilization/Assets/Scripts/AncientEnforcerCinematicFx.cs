using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plugin.AncientCivilization
{
    // 컷신이 전달한 시간으로만 움직이는 효과. 정지·건너뛰기 뒤에는 Clear로 정리한다.
    public sealed class AncientEnforcerCinematicFx : IDisposable
    {
        static readonly Color Cyan = new Color(.38f, .79f, 1f);
        static readonly Color Ice = new Color(.72f, .94f, 1f);
        static readonly Color Pale = new Color(.86f, .96f, 1f);
        static readonly Color Dust = new Color(.31f, .44f, .52f);
        static readonly Color Fragment = new Color(.64f, .73f, .74f);
        const float Tau = Mathf.PI * 2f;
        const int SurfaceSegments = 48;

        readonly GameObject root;
        readonly Material additive;
        readonly Sprite haloSprite, shaftSprite, streakSprite;
        readonly SpriteRenderer shaft, sideShaft, floorPool;
        readonly SpriteRenderer convergence, flareH, flareV;
        readonly SpriteRenderer fallCorona, groundShadow;
        readonly SpriteRenderer hitLight, revealLight, revealRim;
        readonly SpriteRenderer[] airDust, ribbons, fallTrails, debris, smoke;
        readonly LineRenderer waveHalo, waveCore;

        bool disposed;

        public AncientEnforcerCinematicFx(AncientGame world, Transform parent)
        {
            root = new GameObject("집행관 등장 컷신 광원");
            root.transform.SetParent(parent, false);
            Shader shader = Resources.Load<Shader>("Shaders/ParticleGlow");
            if (!shader || !shader.isSupported) shader = Shader.Find("Sprites/Default");
            additive = new Material(shader) { name = "Enforcer cinematic additive light" };

            haloSprite = LightTexture("Cinematic diffusion", 48, 48, Radial);
            shaftSprite = LightTexture("Projected ceiling shaft", 64, 64, Projection);
            streakSprite = LightTexture("Rectangular condensation trace", 64, 20, RectangularTrace);
            shaft = Create("천장 투광", shaftSprite, 6);
            sideShaft = Create("천장 투광 가장자리", shaftSprite, 5);
            floorPool = Create("투광이 닿는 바닥", haloSprite, 6);
            airDust = CreatePool("빛 속 먼지", haloSprite, 12, 20);

            convergence = Create("조형 중심 광원", haloSprite, 15);
            flareH = Create("조형 섬광 가로", streakSprite, 19);
            flareV = Create("조형 섬광 세로", streakSprite, 19);
            ribbons = CreatePool("낱장 입자 꼬리", streakSprite, 18, 72);
            fallCorona = Create("쇳덩이 낙하 빛", haloSprite, 14);
            groundShadow = Create("쇳덩이 낙하점 광원", haloSprite, 7);
            fallTrails = CreatePool("낙하 속도선", streakSprite, 17, 12);

            hitLight = Create("착지 섬광", haloSprite, 15);
            waveHalo = Wave(world, "바닥 충격파 바깥광", 13);
            waveCore = Wave(world, "바닥 충격파 중심", 14);
            debris = CreatePool("충격 파편", streakSprite, 16, 30);
            smoke = CreatePool("충격 먼지", haloSprite, 12, 18);

            revealLight = Create("집행관 실루엣 역광", haloSprite, 6);
            revealRim = Create("집행관 등장 수직 반사광", streakSprite, 13);
        }

        SpriteRenderer Create(string label, Sprite sprite, int order)
        {
            var renderer = new GameObject(label).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(root.transform, false);
            renderer.sprite = sprite;
            renderer.sharedMaterial = additive;
            renderer.sortingOrder = order;
            renderer.enabled = false;
            return renderer;
        }

        SpriteRenderer[] CreatePool(string label, Sprite sprite, int order, int count)
        {
            var pool = new SpriteRenderer[count];
            for (int index = 0; index < count; index++) pool[index] = Create(label, sprite, order);
            return pool;
        }

        LineRenderer Wave(AncientGame world, string label, int order)
        {
            var wave = world.Line(label, Ice, .06f, order);
            wave.transform.SetParent(root.transform, false);
            wave.sharedMaterial = additive;
            wave.useWorldSpace = true;
            wave.loop = true;
            wave.positionCount = SurfaceSegments;
            wave.enabled = false;
            return wave;
        }

        static Sprite LightTexture(string name, int width, int height, Func<float, float, float> profile)
        {
            var pixels = new Color32[width * height];
            for (int row = 0; row < height; row++)
            for (int column = 0; column < width; column++)
            {
                float u = (column + .5f) / width;
                float v = (row + .5f) / height;
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(profile(u, v)) * 255f);
                pixels[row * width + column] = new Color32(255, 255, 255, alpha);
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * .5f, width, 0, SpriteMeshType.FullRect);
        }

        static float Radial(float u, float v)
        {
            float x = 2f * u - 1f, y = 2f * v - 1f;
            return Mathf.Pow(Mathf.Max(0f, 1f - x * x - y * y), 3.1f);
        }

        static float Projection(float u, float v)
        {
            // 광원의 좁은 시작점에서 바닥으로 점차 퍼진다.
            float halfWidth = Mathf.Lerp(.48f, .09f, v);
            float edge = 1f - Mathf.SmoothStep(.32f, 1f, Mathf.InverseLerp(0f, halfWidth, Mathf.Abs(u - .5f)));
            float softStart = Ease(v / .13f) * Ease((1f - v) / .052f);
            return edge * softStart * Mathf.Lerp(.27f, 1f, v);
        }

        static float RectangularTrace(float u, float v)
        {
            float side = Mathf.Abs(v * 2f - 1f);
            float softEdge = Mathf.Pow(Mathf.Max(0f, 1f - side), 1.6f);
            float trail = Mathf.Pow(u, 1.6f) * Ease((1f - u) / .1f) * softEdge;
            float face = (1f - Ease((side - .34f) / .13f)) *
                Ease((u - .58f) / .04f) * (1f - Ease((u - .94f) / .03f));
            return Mathf.Max(.5f * trail, face);
        }

        static float Ease(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));

        static Color Tint(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        static float Fraction(float value) => value - Mathf.Floor(value);

        static void Place(SpriteRenderer renderer, Vector2 point, Vector2 scale, float angle, Color color)
        {
            renderer.transform.position = point;
            renderer.transform.localRotation = Quaternion.Euler(0, 0, angle);
            renderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            renderer.color = color;
            renderer.enabled = color.a > .002f;
        }

        static void Hide(SpriteRenderer[] renderers)
        {
            foreach (var renderer in renderers) renderer.enabled = false;
        }

        public void Atmosphere(Vector2 lamp, float floorY, float age, float intensity)
        {
            float lightHeight = Mathf.Max(.2f, lamp.y - floorY);
            float strength = Mathf.Clamp01(intensity);
            Vector2 basePoint = new Vector2(lamp.x, floorY + lightHeight * .5f);
            Place(shaft, basePoint, new Vector2(lightHeight * .83f, lightHeight), 0f, Tint(Ice, strength * .22f));
            Place(sideShaft, basePoint + new Vector2(1.9f, -.15f), new Vector2(lightHeight * .62f, lightHeight), -8f,
                Tint(Cyan, strength * .08f));
            Place(floorPool, new Vector2(lamp.x, floorY + .08f), new Vector2(lightHeight * 1.08f, .45f), 0f,
                Tint(Ice, strength * .29f));

            for (int index = 0; index < airDust.Length; index++)
            {
                float seed = index * .61803398f;
                float v = Fraction(seed * 2.4f + age * (.046f + index % 4 * .007f));
                float w = Mathf.Lerp(2.3f, .42f, v);
                float x = lamp.x + (Fraction(seed) * 2f - 1f) * w + Mathf.Sin(age * .74f + seed * 7f) * .21f;
                float size = .048f + index % 4 * .02f;
                float alpha = Mathf.Sin(v * Mathf.PI) * strength * .36f;
                Place(airDust[index], new Vector2(x, floorY + v * lightHeight), Vector2.one * size, 0f, Tint(Pale, alpha));
            }
        }

        static Vector2 RibbonPosition(int index, float progress, float age, float radius)
        {
            float thread = index % 4 * Mathf.PI * .5f;
            float seed = Fraction(index * .61803398f);
            float angle = thread + seed * 2.3f + age * (2.7f + index % 4 * .23f) + Ease(progress) * 4.9f;
            float reach = radius * Mathf.Lerp(.67f + seed * .61f, .10f + seed * .12f, Ease(progress));
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .73f) * reach;
        }

        public void Condense(Vector2 center, float progress, float age, float radius = 3.2f)
        {
            progress = Mathf.Clamp01(progress);
            int maximum = AncientVisuals.HighQuality ? ribbons.Length : ribbons.Length / 2;
            int shown = Mathf.Min(maximum, 6 + Mathf.RoundToInt(Ease(progress * 1.5f) * (maximum - 6)));

            for (int index = 0; index < shown; index++)
            {
                var offset = RibbonPosition(index, progress, age, radius);
                var previous = RibbonPosition(index, Mathf.Max(0f, progress - .045f), age - .027f, radius);
                Vector2 movement = offset - previous;
                float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
                float length = Mathf.Clamp(movement.magnitude * 2.55f, .23f, .8f);
                float density = Ease((progress + .13f) / .54f);
                float opacity = (.42f + density * .54f) * (1f - Ease((progress - .95f) * 20f) * .66f);
                Color hue = Color.Lerp(Cyan, Pale, index % 5 == 0 ? .74f : progress * .33f);
                Place(ribbons[index], center + offset, new Vector2(length, .23f + index % 4 * .064f), angle,
                    Tint(hue, opacity));
            }
            for (int i = shown; i < ribbons.Length; i++) ribbons[i].enabled = false;

            float peak = Ease((progress - .74f) / .18f) * (1f - Ease((progress - .91f) / .09f));
            float body = radius * Mathf.Lerp(.75f, .29f, progress);
            Place(convergence, center, Vector2.one * (body + peak * 2.1f), 0f, Tint(Ice, .15f + progress * .25f + peak * .28f));
            Place(flareH, center + Vector2.left * .15f, new Vector2(.6f + 3.1f * peak, .12f), 0f,
                Tint(Pale, peak * .65f));
            Place(flareV, center, new Vector2(.3f + 1.8f * peak, .09f), 90f, Tint(Pale, peak * .42f));
        }

        public void ClearCondensation()
        {
            convergence.enabled = flareH.enabled = flareV.enabled = false;
            Hide(ribbons);
        }

        public void Falling(Vector2 position, float fallProgress)
        {
            float fall = Mathf.Clamp01(fallProgress);
            float velocity = Ease(fall * 1.38f);
            Place(fallCorona, position + Vector2.up * .34f, new Vector2(2.65f, 3.2f + velocity * 3.1f), 0f,
                Tint(Cyan, velocity * .43f));
            Place(groundShadow, new Vector2(position.x, .16f), new Vector2(1.35f + fall * 3.3f, .38f), 0f,
                Tint(Ice, velocity * .64f));

            for (int i = 0; i < fallTrails.Length; i++)
            {
                float s = Fraction(i * .618034f);
                float x = (s * 2f - 1f) * 1.2f;
                float y = (.6f + i % 5 * .33f) * (velocity + .36f);
                float width = .39f + velocity * (.38f + i % 3 * .27f);
                Place(fallTrails[i], position + new Vector2(x, y), new Vector2(width, .13f), -90f,
                    Tint(i % 5 == 0 ? Pale : Cyan, velocity * (.43f - (i % 3) * .09f)));
            }
        }

        public void ClearFall()
        {
            fallCorona.enabled = groundShadow.enabled = false;
            Hide(fallTrails);
        }

        public void Impact(Vector2 ground, float age)
        {
            if (age < 0f || age > 1.25f)
            {
                ClearImpact();
                return;
            }

            float burst = 1f - Ease(age / .23f);
            float flashSize = 1.3f + Ease(age / .18f) * 4.6f;
            Place(hitLight, ground + Vector2.up * .26f, new Vector2(flashSize * 1.6f, flashSize * .77f), 0f,
                Tint(Pale, .92f * burst));

            float expansion = Ease(age / .65f);
            float radius = .62f + 8.2f * expansion;
            float waveFade = 1f - Ease((age - .11f) / .47f);
            DrawWave(waveHalo, ground, radius, (.9f - expansion * .37f) * .22f, Tint(Cyan, waveFade * .37f));
            DrawWave(waveCore, ground, radius, (.9f - expansion * .37f) * .064f, Tint(Pale, waveFade * .85f));

            for (int index = 0; index < debris.Length; index++)
            {
                float seed = Fraction(index * .618034f);
                float direction = index % 2 == 0 ? -1f : 1f;
                float vx = direction * Mathf.Lerp(2f, 11f, seed);
                float vy = Mathf.Lerp(4f, 10f, Fraction(seed * 3.7f));
                float height = .08f + vy * age - 12.6f * age * age;
                var point = ground + new Vector2(vx * age, height);
                float fade = (1f - Ease(age / .98f)) * Ease((height + .03f) / .11f);
                float spin = direction * (38f + index * 13f + age * 365f);
                Color tint = index % 4 == 0 ? Fragment : index % 3 == 0 ? Pale : Cyan;
                Place(debris[index], point, new Vector2(.16f + seed * .36f, .28f + seed * .27f), spin,
                    Tint(tint, fade * .91f));
            }

            for (int index = 0; index < smoke.Length; index++)
            {
                float seed = Fraction(index * .75487767f);
                float delay = seed * .09f;
                float smokeClock = Mathf.Max(0f, age - delay);
                float drift = (seed * 2f - 1f) * (1f + smokeClock * 6.2f);
                float height = .23f + smokeClock * (.3f + (index % 4) * .24f);
                float radiusScale = Mathf.Lerp(.48f, 2.62f, Ease(smokeClock / .92f));
                float alpha = Ease(smokeClock / .12f) * (1f - Ease(smokeClock / 1.13f)) * .35f;
                Place(smoke[index], ground + new Vector2(drift, height), new Vector2(radiusScale, radiusScale * .68f),
                    0f, Tint(Color.Lerp(Dust, Ice, seed * .35f), alpha));
            }
        }

        static void DrawWave(LineRenderer line, Vector2 center, float radius, float width, Color color)
        {
            line.enabled = color.a > .002f;
            if (!line.enabled) return;
            line.widthMultiplier = width;
            line.startColor = line.endColor = color;
            for (int index = 0; index < SurfaceSegments; index++)
            {
                float angle = index / (float)SurfaceSegments * Tau;
                line.SetPosition(index, center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * .073f));
            }
        }

        public void ClearImpact()
        {
            hitLight.enabled = waveHalo.enabled = waveCore.enabled = false;
            Hide(debris);
            Hide(smoke);
        }

        public void Reveal(Vector2 position, float age, float intensity)
        {
            float light = Mathf.Clamp01(intensity);
            float breath = .90f + Mathf.Sin(age * 3.4f) * .10f;
            Place(revealLight, position + new Vector2(.34f, .24f), new Vector2(7.8f, 6.1f), 0f,
                Tint(Ice, light * breath * .35f));
            Place(revealRim, position + new Vector2(-.53f, .23f), new Vector2(2.8f, 1.28f), 89f,
                Tint(Cyan, light * breath * .22f));
        }

        public void Clear()
        {
            shaft.enabled = sideShaft.enabled = floorPool.enabled = false;
            revealLight.enabled = revealRim.enabled = false;
            Hide(airDust);
            ClearCondensation();
            ClearFall();
            ClearImpact();
        }

        static void DestroySprite(Sprite sprite)
        {
            if (!sprite) return;
            Object.Destroy(sprite.texture);
            Object.Destroy(sprite);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (root)
            {
                root.SetActive(false);
                Object.Destroy(root);
            }
            DestroySprite(haloSprite);
            DestroySprite(shaftSprite);
            DestroySprite(streakSprite);
            Object.Destroy(additive);
        }
    }
}
