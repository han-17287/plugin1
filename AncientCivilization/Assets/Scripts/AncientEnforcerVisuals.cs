using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plugin.AncientCivilization
{
    // 판정과 별개로 조형·배리어·컷신에 쓰이는 2D 표현을 소유한다.
    public sealed partial class AncientEnforcerVisuals : IDisposable
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
            beamProfile.StyleBody(beamBody, beamCore);
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
            line.startWidth = line.endWidth = width;
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

        // 도감 원화의 왼쪽 위를 기준으로 자른다. 장비와 건틀릿이 벌어져도 두 발 사이의 피벗을 유지한다.
        static readonly Rect[] PoseCells = {
            new Rect(140, 4, 258, 501), new Rect(647, 5, 257, 500), new Rect(505, 515, 464, 502),
            new Rect(135, 517, 328, 499), new Rect(1057, 4, 339, 501), new Rect(1102, 587, 299, 429)
        };
        static readonly Vector2[] PoseFeet = {
            new Vector2(256, 499), new Vector2(768, 499), new Vector2(768, 1011),
            new Vector2(256, 1010), new Vector2(1280, 499), new Vector2(1280, 1010)
        };
        static readonly float[] PosePixelsPerUnit = { 195, 195, 195, 195, 195, 195 };
        // 원화 좌상단 픽셀 좌표. 몸체의 뒤집기와 후방 축척을 그대로 따르는 컷신 기준점이다.
        static readonly Vector2[] LensPixels = {
            new Vector2(263, 43), new Vector2(775, 43), new Vector2(778, 555),
            new Vector2(264, 555), new Vector2(1290, 42), new Vector2(1347, 625)
        };
        static readonly Vector2[] DossierPixels = {
            new Vector2(363.1f, 175.1f), new Vector2(867.5f, 47.6f), new Vector2(635.7f, 689.3f),
            new Vector2(427.8f, 556.1f), new Vector2(1085.7f, 57.8f), new Vector2(1365.8f, 687.5f)
        };
        // 문서의 가로/세로 축을 따르는 읽기 선. 투영된 대각선 테두리 밖으로 새지 않는다.
        static readonly Vector4[] DossierAxes = {
            new Vector4(.90f, -.17f, -.14f, .79f), new Vector4(.97f, -.09f, -.12f, .90f),
            new Vector4(.91f, -.17f, -.16f, .83f), new Vector4(.90f, -.12f, -.15f, .78f),
            new Vector4(.85f, -.17f, .02f, .78f), new Vector4(.93f, -.15f, -.14f, .82f)
        };
        static Sprite scanLine;

        public static Vector2 LensWorld(SpriteRenderer body, int pose) =>
            SourcePoint(body, pose, LensPixels, AncientEnforcerBridgeAtlas.Lenses);
        public static Vector2 DossierWorld(SpriteRenderer body, int pose) =>
            SourcePoint(body, pose, DossierPixels, AncientEnforcerBridgeAtlas.Dossiers);

        static Vector2 SourcePoint(SpriteRenderer body, int pose, Vector2[] points, Vector2[] bridgePoints)
        {
            if (!body) return Vector2.zero;
            pose = Mathf.Clamp(pose, 0, points.Length - 1);
            int bridge = BridgeFrameOf(body.sprite);
            Vector2 offset = bridge >= 0
                ? (bridgePoints[bridge] - AncientEnforcerBridgeAtlas.Feet[bridge]) / BridgePixelsPerUnit
                : (points[pose] - PoseFeet[pose]) / PosePixelsPerUnit[pose];
            return body.transform.TransformPoint(new Vector3(body.flipX ? -offset.x : offset.x,
                body.flipY ? offset.y : -offset.y, 0));
        }

        static readonly Sprite[] Poses = new Sprite[6];
        // 발광 부분만 조밀하게 패킹하고 본체 원화 좌표로 되돌린다.
        static readonly Rect[] OpticsCells = {
            new Rect(5, 5, 73, 189), new Rect(86, 5, 46, 89), new Rect(140, 5, 140, 63),
            new Rect(288, 5, 115, 83), new Rect(5, 202, 147, 91), new Rect(160, 202, 145, 168)
        };
        static readonly Vector2[] OpticsSourceCenters = {
            new Vector2(232.5f, 133.5f), new Vector2(791, 83.5f), new Vector2(841, 582.5f),
            new Vector2(314.5f, 592.5f), new Vector2(1222.5f, 83.5f), new Vector2(1280.5f, 705)
        };
        static readonly Sprite[] Optics = new Sprite[6];
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
            Vector2 foot = PoseFeet[pose];
            Vector2 pivot = new Vector2((foot.x - cell.x) / cell.width, (cell.yMax - foot.y) / cell.height);
            return Poses[pose] = AncientEnforcerCostume.Articulate(AncientArt.Slice("RecordEnforcer",
                "기록 집행관 자세 " + pose, cell, PosePixelsPerUnit[pose], pivot, false));
        }

        // 렌즈와 압인 장갑의 표시등만 별도 피벗 정합 레이어로 맥동한다.
        // 컷신/후방 사격의 뒤집기, 축척, 투명도는 몸체와 함께 변한다.
        public static void PresentOptics(SpriteRenderer body, ref SpriteRenderer lights, ref SpriteRenderer scan, int pose)
        {
            if (!lights)
            {
                lights = new GameObject("단안 렌즈와 입자 압인기 표시등").AddComponent<SpriteRenderer>();
                lights.transform.SetParent(body.transform, false);
                scan = new GameObject("홀로그램 기록 읽기 선").AddComponent<SpriteRenderer>();
                scan.transform.SetParent(body.transform, false);
                if (!scanLine) scanLine = AncientEnforcerCostume.Articulate(AncientArt.Slice("RecordEnforcerOptics",
                    "집행관 기록 읽기 선", new Rect(408, 224, 64, 8), 64, new Vector2(.5f, .5f), false));
                scan.sprite = scanLine;
            }

            pose = Mathf.Clamp(pose, 0, Optics.Length - 1);
            int bridge = BridgeFrameOf(body.sprite);
            float pixelsPerUnit = PosePixelsPerUnit[pose];
            Vector2 foot = PoseFeet[pose], lightsCenter = OpticsSourceCenters[pose], dossier = DossierPixels[pose];
            Vector4 axes = DossierAxes[pose];
            if (bridge >= 0)
            {
                pixelsPerUnit = BridgePixelsPerUnit;
                foot = AncientEnforcerBridgeAtlas.Feet[bridge];
                lightsCenter = AncientEnforcerBridgeAtlas.OpticCenters[bridge];
                dossier = AncientEnforcerBridgeAtlas.Dossiers[bridge];
                axes = AncientEnforcerBridgeAtlas.DossierAxes[bridge];
                lights.sprite = BridgeOpticsSprite(bridge);
            }
            else
            {
                if (!Optics[pose]) Optics[pose] = AncientEnforcerCostume.Articulate(AncientArt.Slice("RecordEnforcerOptics",
                    "집행관 광학 장비 " + pose, OpticsCells[pose], pixelsPerUnit, new Vector2(.5f, .5f), false));
                lights.sprite = Optics[pose];
            }
            Vector2 offset = (lightsCenter - foot) / pixelsPerUnit;
            lights.transform.localPosition = new Vector3(body.flipX ? -offset.x : offset.x, body.flipY ? offset.y : -offset.y);
            lights.enabled = body.enabled && body.color.a > .005f;
            lights.flipX = body.flipX;
            lights.flipY = body.flipY;
            lights.sortingLayerID = body.sortingLayerID;
            lights.sortingOrder = body.sortingOrder + 1;
            float wave = .5f + .5f * Mathf.Sin(Time.time * 4.25f);
            lights.color = new Color(1f, 1f, 1f, body.color.a * (.12f + .76f * wave * wave));

            float step = Mathf.Repeat(Time.time * .48f, 1f);
            Vector2 center = dossier + new Vector2(axes.z, axes.w) * Mathf.Lerp(-18f, 16f, step);
            Vector2 scanOffset = (center - foot) / pixelsPerUnit;
            float flipX = body.flipX ? -1 : 1;
            float flipY = body.flipY ? 1 : -1;
            scan.transform.localPosition = new Vector3(flipX * scanOffset.x, flipY * scanOffset.y, 0);
            Vector2 along = new Vector2(axes.x * flipX, axes.y * flipY);
            scan.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg);
            scan.transform.localScale = new Vector3(42f * along.magnitude / pixelsPerUnit,
                25f / pixelsPerUnit, 1);
            float visible = Mathf.Clamp01(Mathf.Min(step, 1f - step) * 9f);
            scan.color = new Color(1, 1, 1, visible * body.color.a * .63f);
            scan.enabled = lights.enabled;
            scan.sortingLayerID = lights.sortingLayerID;
            scan.sortingOrder = lights.sortingOrder;
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
