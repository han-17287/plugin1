using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plugin.AncientCivilization
{
    // 판정과 별개로 조형·배리어·컷신에 쓰이는 2D 표현을 소유한다.
    public sealed class AncientEnforcerVisuals : IDisposable
    {
        public static readonly Color ParticleColor = new Color(.40f, .80f, 1f);
        static readonly Color Light = new Color(.72f, .94f, 1f);
        static readonly Color WhiteHot = new Color(.89f, .98f, 1f);
        const int ParticleCount = 64;
        const int RingSegments = 64;
        readonly GameObject root;
        readonly SpriteRenderer[] particleBodies = new SpriteRenderer[ParticleCount];
        readonly SpriteRenderer[] particleLights = new SpriteRenderer[ParticleCount];
        readonly SpriteRenderer focusLight, introLight, barrierLight;
        readonly LineRenderer barrierBorder, barrierCharge, beamAura, beamBody, beamCore;
        readonly AncientBeamVisuals beamProfile;
        readonly Material additive;
        readonly Sprite softSprite;
        readonly Texture2D softTexture;
        GUIStyle titleStyle;
        bool vortexVisible, disposed;

        public AncientEnforcerVisuals(AncientGame game, Transform parent)
        {
            root = new GameObject("기록 집행관 조형 효과");
            root.transform.SetParent(parent, false);

            var shader = Resources.Load<Shader>("Shaders/ParticleGlow");
            if (!shader || !shader.isSupported) shader = Shader.Find("Sprites/Default");
            additive = new Material(shader) { name = "Enforcer additive light" };
            softTexture = CreateSoftTexture();
            softSprite = Sprite.Create(softTexture, new Rect(0, 0, 40, 40), Vector2.one * .5f, 40);

            focusLight = LightSprite("조형 중심의 푸른 광원", 12);
            introLight = LightSprite("등장 컷신의 역광", 7);
            barrierLight = LightSprite("입자 배리어 광원", 11);
            for (int i = 0; i < ParticleCount; i++)
            {
                var item = game.Box("하늘색 직사각형 입자", Vector2.zero, Vector2.one, ParticleColor, 17);
                item.transform.SetParent(root.transform, false);
                particleBodies[i] = item.GetComponent<SpriteRenderer>();
                particleBodies[i].enabled = false;
                particleLights[i] = LightSprite("직사각형 입자의 빛 번짐", 16);
            }

            barrierBorder = Line(game, "입자 배리어 경계", 14);
            barrierBorder.positionCount = 7;
            barrierCharge = Line(game, "입자 배리어 충전 표시", 15);
            barrierCharge.positionCount = RingSegments + 1;

            beamAura = Line(game, "집행관 레이저 빛 번짐", 18);
            beamBody = Line(game, "집행관 레이저 하늘색 본체", 19);
            beamCore = Line(game, "집행관 레이저 백색 중심", 20);
            beamProfile = new AncientBeamVisuals(beamAura, true);
        }

        SpriteRenderer LightSprite(string name, int order)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(root.transform, false);
            renderer.sprite = softSprite;
            renderer.sharedMaterial = additive;
            renderer.sortingOrder = order;
            renderer.enabled = false;
            return renderer;
        }

        LineRenderer Line(AncientGame game, string name, int order)
        {
            var line = game.Line(name, ParticleColor, .05f, order);
            line.transform.SetParent(root.transform, false);
            line.useWorldSpace = true;
            line.numCapVertices = 5;
            line.numCornerVertices = 3;
            line.enabled = false;
            return line;
        }

        static Texture2D CreateSoftTexture()
        {
            const int size = 40;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size * 2f - 1f;
                float dy = (y + .5f) / size * 2f - 1f;
                float alpha = Mathf.Pow(Mathf.Max(0, 1f - dx * dx - dy * dy), 2.8f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Enforcer soft emissive profile",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        static Color Opacity(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        static void Place(SpriteRenderer renderer, Vector2 point, Vector2 size, float rotation, Color color)
        {
            renderer.transform.position = point;
            renderer.transform.localScale = new Vector3(size.x, size.y, 1);
            renderer.transform.localRotation = Quaternion.Euler(0, 0, rotation);
            renderer.color = color;
            renderer.enabled = true;
        }

        // 빛 번짐의 크기를 별도로 조절하여 실제 직사각형 외곽이 흐려지지 않게 한다.
        public void Vortex(Vector2 center, float progress, float age, float radius = 2f)
        {
            progress = Mathf.Clamp01(progress);
            float collapse = Mathf.SmoothStep(1.18f, .13f, progress);
            float brightness = Mathf.SmoothStep(.25f, 1f, progress);
            int count = AncientVisuals.HighQuality ? ParticleCount : ParticleCount / 2;

            for (int i = 0; i < count; i++)
            {
                float strand = i % 4 * Mathf.PI * .5f;
                float position = i / (float)count;
                float angle = strand + position * 7.4f + age * 5.1f + progress * 2.8f;
                float reach = radius * collapse * (.36f + position * .93f);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 point = center + new Vector2(direction.x, direction.y * .71f) * reach;
                float flicker = .75f + .25f * Mathf.Sin(age * 16f + i * 2.1f);
                float length = (.135f + (i % 6) * .034f) * (1f - progress * .36f);
                float size = .048f + (i % 3) * .013f;
                float rotation = angle * Mathf.Rad2Deg + 67f;
                Color tint = Color.Lerp(ParticleColor, WhiteHot, brightness * .72f + (i % 3) * .07f);
                Place(particleBodies[i], point, new Vector2(length, size), rotation, Opacity(tint, flicker));
                Place(particleLights[i], point, new Vector2(length * 2.9f, size * 5f), rotation,
                    Opacity(ParticleColor, .34f * flicker));
            }

            for (int i = count; i < ParticleCount; i++)
            {
                particleBodies[i].enabled = false;
                particleLights[i].enabled = false;
            }

            float disc = radius * Mathf.Lerp(1.15f, .64f, progress);
            Place(focusLight, center, Vector2.one * disc, 0, Opacity(Light, .16f + brightness * .34f));
            vortexVisible = true;
        }

        public void ClearVortex()
        {
            if (!vortexVisible) return;
            vortexVisible = false;
            focusLight.enabled = false;
            for (int i = 0; i < ParticleCount; i++)
            {
                particleBodies[i].enabled = false;
                particleLights[i].enabled = false;
            }
        }

        public void Intro(Vector2 focus, float weight)
        {
            weight = Mathf.Clamp01(weight);
            Place(introLight, focus + Vector2.up * .6f, new Vector2(10.5f, 8.5f), 0,
                Opacity(ParticleColor, weight * .34f));
        }

        public void ClearIntro()
        {
            introLight.enabled = false;
        }

        public void DrawIntroOverlay(float age, float duration)
        {
            if (Event.current.type != EventType.Repaint || duration <= 0f) return;
            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / .35f));
            float fadeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((duration - age) / .38f));
            float envelope = fadeIn * fadeOut;
            if (envelope <= .001f) return;

            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            GUI.depth = -20;
            float barHeight = Screen.height * .082f * envelope;
            GUI.color = Opacity(new Color(.012f, .026f, .052f), envelope * .95f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, barHeight), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - barHeight, Screen.width, barHeight), Texture2D.whiteTexture);

            float titleOpacity = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.62f, .78f, age / duration)) * fadeOut;
            if (titleOpacity > .001f)
            {
                if (titleStyle == null)
                    titleStyle = new GUIStyle(GUI.skin.label) { font = AncientKoreanUI.Font, alignment = TextAnchor.MiddleCenter };
                titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * .031f), 16, 32);
                titleStyle.normal.textColor = WhiteHot;
                GUI.color = Opacity(Color.white, titleOpacity);
                GUI.Label(new Rect(0, Screen.height - barHeight, Screen.width, barHeight), "기록 집행관", titleStyle);
            }

            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        public void Barrier(Vector2 focus, float progress, float age)
        {
            progress = Mathf.Clamp01(progress);
            float shimmer = .79f + Mathf.Sin(age * (4f + progress * 9f)) * .21f;
            float radius = 2.18f + progress * .28f;
            for (int i = 0; i <= 6; i++)
            {
                float angle = Mathf.PI * (i / 3f + .5f);
                barrierBorder.SetPosition(i, focus + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            barrierBorder.enabled = true;
            barrierBorder.startColor = barrierBorder.endColor = Opacity(ParticleColor, shimmer * .75f);
            barrierBorder.widthMultiplier = .055f + progress * .065f;

            for (int i = 0; i <= RingSegments; i++)
            {
                float angle = Mathf.PI * (.5f - 2f * i / RingSegments * progress);
                barrierCharge.SetPosition(i, focus + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius + .24f));
            }

            barrierCharge.enabled = progress > .002f;
            barrierCharge.startColor = barrierCharge.endColor = Light;
            barrierCharge.widthMultiplier = .085f;
            Place(barrierLight, focus, Vector2.one * radius * 3.6f, 0,
                Opacity(ParticleColor, shimmer * (.14f + .2f * progress)));
        }

        public void ClearBarrier()
        {
            barrierBorder.enabled = false;
            barrierCharge.enabled = false;
            barrierLight.enabled = false;
        }

        public void Beam(Vector2 from, Vector2 to, float width, float intensity = 1f)
        {
            intensity = Mathf.Clamp01(intensity);
            if (intensity <= .001f || width <= 0f || (to - from).sqrMagnitude < .0001f)
            {
                ClearBeam();
                return;
            }

            SetBeamLine(beamAura, from, to, width * 2.85f, Opacity(ParticleColor, .56f * intensity));
            SetBeamLine(beamBody, from, to, width, Opacity(Light, .87f * intensity));
            SetBeamLine(beamCore, from, to, width * .35f, Opacity(WhiteHot, intensity));
            beamProfile.ShowEndpoints(from, to, width, intensity, true);
        }

        static void SetBeamLine(LineRenderer line, Vector2 from, Vector2 to, float width, Color color)
        {
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startColor = line.endColor = color;
            line.widthMultiplier = width;
            line.enabled = true;
        }

        public void ClearBeam()
        {
            beamAura.enabled = false;
            beamBody.enabled = false;
            beamCore.enabled = false;
            beamProfile.HideEndpoints();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            beamProfile.Dispose();
            if (root) Object.Destroy(root);
            Object.Destroy(softSprite);
            Object.Destroy(softTexture);
            Object.Destroy(additive);
        }

        // 투명 원화의 실제 발 위치에 피벗을 두어 넓은 코트와 팔 동작에도 몸의 중심을 유지한다.
        static readonly Rect[] PoseCells = {
            new Rect(168, 3, 185, 459), new Rect(619, 7, 238, 455), new Rect(1027, 13, 414, 450),
            new Rect(83, 570, 306, 434), new Rect(571, 480, 300, 504), new Rect(1024, 691, 405, 315)
        };
        static readonly Vector2[] PoseFeet = {
            new Vector2(256, 456), new Vector2(761, 456), new Vector2(1247, 457),
            new Vector2(254, 997), new Vector2(768, 977), new Vector2(1300, 1001)
        };
        static readonly float[] PosePixelsPerUnit = { 131, 130, 129, 137, 145, 131 };
        static readonly Sprite[] Poses = new Sprite[6];
        static readonly Sprite[] Objects = new Sprite[5];
        static readonly Color32 Ink = new Color32(11, 19, 29, 255);
        static readonly Color32 Navy = new Color32(29, 47, 65, 255);
        static readonly Color32 Seam = new Color32(55, 81, 96, 255);
        static readonly Color32 Steel = new Color32(101, 131, 142, 255);
        static readonly Color32 Edge = new Color32(170, 194, 196, 255);
        static readonly Color32 Blue = new Color32(71, 175, 231, 255);
        static readonly Color32 Ice = new Color32(155, 230, 255, 255);
        static readonly Color32 Pearl = new Color32(223, 251, 255, 255);

        public static Sprite PoseSprite(int pose)
        {
            pose = Mathf.Clamp(pose, 0, Poses.Length - 1);
            if (Poses[pose]) return Poses[pose];
            Rect cell = PoseCells[pose];
            Vector2 root = PoseFeet[pose];
            Vector2 pivot = new Vector2((root.x - cell.x) / cell.width, (cell.yMax - root.y) / cell.height);
            return Poses[pose] = AncientArt.Slice("RecordEnforcer", "기록 집행관 자세 " + pose, cell, PosePixelsPerUnit[pose], pivot, false);
        }

        // 컷신과 패턴에 쓰이는 물체는 같은 강철·서류 팔레트를 공유한다.
        public static Sprite ObjectSprite(int shape)
        {
            shape = Mathf.Clamp(shape, 0, Objects.Length - 1);
            if (Objects[shape]) return Objects[shape];
            var sprite = new PixelCanvas(48, 32);

            switch (shape)
            {
                case 0:
                    sprite.Polygon(Ink, 1, 12, 7, 23, 37, 28, 47, 19, 44, 9, 10, 4);
                    sprite.Polygon(Seam, 4, 12, 10, 20, 38, 23, 44, 18, 42, 11, 10, 6);
                    sprite.Polygon(Steel, 7, 21, 36, 26, 44, 20, 12, 15);
                    sprite.Polygon(Edge, 9, 21, 36, 26, 39, 23, 13, 19);
                    sprite.Stroke(11, 13, 38, 17, 2, Navy);
                    sprite.Stroke(12, 9, 41, 14, 1, Blue);
                    break;
                case 1:
                    sprite.Polygon(Ink, 2, 4, 2, 25, 37, 25, 46, 29, 46, 9, 39, 3);
                    sprite.Rect(5, 6, 31, 17, Steel);
                    sprite.Polygon(Seam, 38, 5, 38, 24, 44, 27, 44, 10);
                    for (int x = 8; x < 35; x += 4)
                    {
                        sprite.Rect(x, 7, 1, 14, Edge);
                        sprite.Rect(x + 1, 7, 1, 14, Seam);
                    }
                    sprite.Rect(5, 20, 31, 2, Navy);
                    sprite.Rect(5, 7, 31, 2, Navy);
                    sprite.Rect(24, 11, 9, 6, Ink);
                    sprite.Rect(25, 12, 7, 4, Blue);
                    sprite.Rect(26, 14, 4, 1, Ice);
                    sprite.Stroke(39, 8, 39, 20, 1, Edge);
                    break;
                case 2:
                    sprite.Polygon(Ink, 1, 13, 1, 22, 9, 22, 15, 19, 15, 25, 32, 25, 36, 22, 47, 22, 47, 16, 34, 16,
                        29, 14, 27, 4, 21, 4, 20, 13, 17, 13, 14, 4, 8, 4, 9, 13);
                    sprite.Polygon(Steel, 3, 15, 3, 20, 9, 20, 14, 17, 17, 21, 31, 22, 34, 19, 44, 20, 46, 18, 33, 17,
                        26, 15, 25, 6, 23, 6, 22, 15, 14, 15);
                    sprite.Rect(17, 21, 12, 2, Edge);
                    sprite.Rect(27, 17, 7, 2, Blue);
                    sprite.Rect(30, 17, 3, 1, Ice);
                    sprite.Rect(10, 6, 3, 7, Seam);
                    break;
                case 3:
                    sprite.Polygon(Ink, 2, 25, 46, 17, 21, 5, 18, 11, 10, 13);
                    sprite.Polygon(Ice, 5, 23, 43, 17, 19, 14, 11, 15);
                    sprite.Polygon(Blue, 19, 14, 43, 17, 22, 7);
                    sprite.Polygon(Pearl, 8, 22, 43, 17, 20, 17);
                    sprite.Stroke(19, 14, 35, 16, 1, Pearl);
                    break;
                case 4:
                    sprite.Polygon(Ink, 4, 7, 5, 22, 15, 19, 34, 21, 46, 15, 34, 8, 16, 10, 8, 6);
                    sprite.Polygon(Seam, 6, 11, 7, 19, 16, 17, 34, 19, 43, 15, 34, 10, 16, 12, 8, 8);
                    sprite.Polygon(Edge, 16, 16, 34, 19, 42, 15, 33, 13, 17, 13);
                    sprite.Rect(14, 13, 4, 4, Blue);
                    sprite.Rect(6, 13, 6, 4, Blue);
                    sprite.Rect(7, 14, 3, 2, Pearl);
                    sprite.Rect(30, 15, 4, 1, Ice);
                    break;
            }

            return Objects[shape] = sprite.Make("집행관 조형 물체 " + shape, Vector2.one * .5f, 16f);
        }

        sealed class PixelCanvas
        {
            readonly int width, height;
            readonly Color32[] pixels;

            public PixelCanvas(int width, int height)
            {
                this.width = width;
                this.height = height;
                pixels = new Color32[width * height];
            }

            public void Rect(int x, int y, int w, int h, Color32 color)
            {
                for (int row = Mathf.Max(0, y); row < Mathf.Min(height, y + h); row++)
                for (int col = Mathf.Max(0, x); col < Mathf.Min(width, x + w); col++)
                    pixels[row * width + col] = color;
            }

            public void Stroke(int x0, int y0, int x1, int y1, int size, Color32 color)
            {
                int steps = Mathf.Max(1, Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)));
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Rect(Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)) - size / 2,
                        Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)) - size / 2, size, size, color);
                }
            }

            public void Polygon(Color32 color, params int[] points)
            {
                int minX = width, minY = height, maxX = 0, maxY = 0;
                for (int index = 0; index < points.Length; index += 2)
                {
                    minX = Mathf.Min(minX, points[index]);
                    maxX = Mathf.Max(maxX, points[index]);
                    minY = Mathf.Min(minY, points[index + 1]);
                    maxY = Mathf.Max(maxY, points[index + 1]);
                }

                for (int y = Mathf.Max(0, minY); y < Mathf.Min(height, maxY); y++)
                for (int x = Mathf.Max(0, minX); x < Mathf.Min(width, maxX); x++)
                {
                    float px = x + .5f, py = y + .5f;
                    bool inside = false;
                    for (int i = 0, previous = points.Length - 2; i < points.Length; previous = i, i += 2)
                    {
                        int ax = points[previous], ay = points[previous + 1], bx = points[i], by = points[i + 1];
                        if ((ay > py) != (by > py) && px < (bx - ax) * (py - ay) / (by - ay) + ax)
                            inside = !inside;
                    }

                    if (inside) pixels[y * width + x] = color;
                }
            }

            public Sprite Make(string name, Vector2 pivot, float pixelsPerUnit)
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = name,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                return Sprite.Create(texture, new Rect(0, 0, width, height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
            }
        }
    }
}
