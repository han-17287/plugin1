using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.PlayerCore
{
    // 플레이어와 콘센트를 잇는 그림은 물리 상태를 읽기만 한다.
    public sealed class BluePlugVisuals : MonoBehaviour
    {
        public static event System.Action<BluePlugVisuals, Vector2> DockFlash;

        const int CablePoints = 19;
        const int OrbitSubdivisions = 4;
        const int RangePoints = 72;
        static Color CableBlue => BluePlugArt.MovementColor;
        static Color Signal(float alpha)
        {
            var tint = CableBlue;
            tint.a = alpha;
            return tint;
        }

        static Material cordMaterial, filamentMaterial;
        static readonly int EnergyId = Shader.PropertyToID("_Energy");
        static readonly int GlowEnergyId = Shader.PropertyToID("_Intensity");
        static readonly int FlowId = Shader.PropertyToID("_Flow");
        static readonly int LengthId = Shader.PropertyToID("_CableLength");
        static readonly int TintId = Shader.PropertyToID("_Signal");

        [SerializeField, Min(.1f)] float plugScale = 1f;
        [SerializeField, Min(.01f)] float cableWidth = .052f;
        [SerializeField, Range(.06f, .25f)] float dockFlashSeconds = .20f;
        [SerializeField, Range(.08f, .22f)] float towPulseSeconds = .15f;

        [SerializeField, Range(.5f, 1f)] float orbitHeight = .73f;
        [SerializeField, Range(.04f, .18f)] float orbitReleaseFade = .10f;
        [SerializeField, Range(.5f, 6)] float peakEmission = 4;
        [SerializeField, Range(.1f, 3)] float lightRadius = 1.15f;

        Transform assembly, plugHead;
        SpriteRenderer actor;
        AncientGame campaign;
        bool finaleLook, clashHidden;
        SpriteRenderer headBody;
        BluePlugTravelFX effects;
        BluePlugSound sound;
        BluePlugPointLight localLight;
        BluePlugFlightTrail flightTrail;
        MaterialPropertyBlock cordProperties, glowProperties;
        readonly Vector3[] cableMesh = new Vector3[CablePoints];
        LineRenderer towPulse;
        float towStartedAt = -100f;
        LineRenderer cableEdge, cableLight, cableCore, cableStripe, windGlow, windCore, rangeGuide;
        readonly Vector3[] guideMesh = new Vector3[RangePoints];
        SpriteRenderer headHalo, filament, latchFlash, arrivalFlash, reelLight;
        BluePlugPhase previousPhase;
        public BluePlugPhase PresentedPhase => previousPhase;
        Vector3 lastGripLocal;
        float rearmStart = -100;
        bool sendLatchSparks, clicked, reducedFlash;
        Vector2 arrivalPoint;
        float arrivalTime = -100f;
        float latchTime = -100f;
        float windTrailStart;
        readonly BluePlugOrbitHistory orbit = new BluePlugOrbitHistory();
        readonly Vector3[] orbitMesh = new Vector3[BluePlugOrbitHistory.Capacity * OrbitSubdivisions + 1];
        MaterialPropertyBlock orbitProperties;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        float orbitReleasedAt = -100f;

        void Awake()
        {
            if (GetComponent<AncientHero>()) campaign = AncientGame.Instance;
            assembly = new GameObject("파란 플러그 표시").transform;
            assembly.SetParent(transform, false);
            // 피복과 청색 띠는 불투명 재질로 그려 밝은 배경이나 발광을 끈 화면에서도 남는다.
            cableEdge = MakeLine("전선 윤곽", CablePoints, cableWidth * 2.2f,
                new Color32(12, 30, 54, 255), 13);
            cableLight = MakeLine("전선 청색광", CablePoints, cableWidth * 3.2f,
                Signal(.11f), 12);
            cableLight.sharedMaterial = BluePlugArt.GlowMaterial;
            cableStripe = MakeLine("전선 절연 피복 띠", CablePoints, cableWidth * 1.24f,
                new Color32(55, 157, 223, 255), 14);
            cableCore = MakeLine("전선 중심", CablePoints, cableWidth * .56f, CableBlue, 15);
            // 견인 시작 때만 짧은 전류 한 줄이 부싱에서 손으로 흐른다.
            towPulse = MakeLine("견인 전류", 2, .08f, Color.white, 16);
            towPulse.widthCurve = AnimationCurve.EaseInOut(0f, .06f, 1f, 1f);
            towPulse.startColor = new Color32(48, 121, 232, 40);
            towPulse.endColor = new Color32(220, 251, 255, 255);
            towPulse.numCapVertices = 2;
            towPulse.enabled = false;
            BluePlugArt.LoadContacts();
            if (!cordMaterial)
            {
                var flow = OptionalGraph("BluePlugURP/SignalCord");
                if (!flow) flow = Resources.Load<Shader>("Shaders/BluePlugCord");
                if (flow && flow.isSupported) cordMaterial = new Material(flow);
            }
            if (cordMaterial) cableCore.sharedMaterial = cordMaterial;
            cordProperties = new MaterialPropertyBlock();
            glowProperties = new MaterialPropertyBlock();
            orbitProperties = new MaterialPropertyBlock();
            windGlow = MakeWindTrail("회전 궤적 후광", .14f, true, 10);
            windCore = MakeWindTrail("회전 궤적 중심", .068f, false, 11);
            rangeGuide = MakeLine("투척 사거리", RangePoints, .038f, CableBlue, 10);
            rangeGuide.loop = true;
            rangeGuide.enabled = false;

            plugHead = new GameObject("플러그 헤드").transform;
            plugHead.SetParent(assembly, false);
            plugHead.localScale = Vector3.one * plugScale;
            headHalo = BluePlugArt.AddSprite(plugHead, "플러그 후광", BluePlugArt.Halo, new Vector2(-.52f, 0), 15, 1.12f);
            headHalo.sharedMaterial = BluePlugArt.GlowMaterial;
            headBody = BluePlugArt.AddSprite(plugHead, "파란 플러그 함체", BluePlugArt.Plug, Vector2.zero, 17);
            filament = BluePlugArt.AddSprite(plugHead, "필라멘트 창", BluePlugArt.PlugEmitter, Vector2.zero, 18);
            if (!filamentMaterial)
            {
                var graph = OptionalGraph("BluePlugURP/PlugFilament");
                filamentMaterial = graph ? new Material(graph) : BluePlugArt.GlowMaterial;
            }
            filament.sharedMaterial = filamentMaterial;
            filament.color = CableBlue;
            latchFlash = BluePlugArt.AddSprite(plugHead, "잠금 섬광", BluePlugArt.Linked, Vector2.zero, 18);
            latchFlash.sharedMaterial = BluePlugArt.GlowMaterial;
            latchFlash.enabled = false;
            arrivalFlash = BluePlugArt.AddSprite(transform, "견인 도착 섬광", BluePlugArt.Linked, Vector2.zero, 19, 1.5f);
            arrivalFlash.sharedMaterial = BluePlugArt.GlowMaterial;
            arrivalFlash.enabled = false;

            reelLight = BluePlugArt.AddSprite(transform, "릴 충전등", BluePlugArt.Halo, Vector2.zero, 20, .42f);
            reelLight.sharedMaterial = BluePlugArt.GlowMaterial;
            reelLight.enabled = false;
            sound = BluePlugSound.ForPlayer(gameObject);
            localLight = new BluePlugPointLight(transform);
            flightTrail = new BluePlugFlightTrail(transform);
            if (actor) BindEffects();
            assembly.gameObject.SetActive(false);
        }

        static Shader OptionalGraph(string resource)
        {
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (!pipeline || pipeline.GetType().FullName != "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset")
                return null;
            var shader = Resources.Load<Shader>(resource);
            return shader && shader.isSupported ? shader : null;
        }

        public void SetActor(SpriteRenderer body)
        {
            actor = body;
            if (actor && assembly) BindEffects();
        }

        void BindEffects()
        {
            if (!effects) effects = gameObject.AddComponent<BluePlugTravelFX>();
            effects.Bind(actor);
            int layer = actor.sortingLayerID;
            towPulse.sortingLayerID = layer;
            cableLight.sortingLayerID = cableEdge.sortingLayerID = cableStripe.sortingLayerID = cableCore.sortingLayerID = layer;
            headHalo.sortingLayerID = headBody.sortingLayerID = filament.sortingLayerID = latchFlash.sortingLayerID = layer;
            rangeGuide.sortingLayerID = windGlow.sortingLayerID = windCore.sortingLayerID = layer;
            arrivalFlash.sortingLayerID = reelLight.sortingLayerID = layer;
            arrivalFlash.sortingOrder = reelLight.sortingOrder = actor.sortingOrder + 8;
        }

        bool FinaleActive => campaign && campaign.isActiveAndEnabled && campaign.hero && campaign.hero.gameObject == gameObject &&
            campaign.InSniperFinalPhase && campaign.boss && campaign.boss == campaign.Sniper.Owner;

        void FollowEncounter()
        {
            if (!campaign && !finaleLook) return;
            bool legacy = FinaleActive;
            if (legacy != finaleLook)
            {
                // 페이즈 경계의 전선·잔상·모터음을 새 연출이나 결전으로 넘기지 않는다.
                ResetFeedback();
                finaleLook = legacy;
                headBody.sprite = legacy ? BluePlugArt.FinalePlug : BluePlugArt.Plug;
                filament.sprite = legacy ? BluePlugArt.FinaleEmitter : BluePlugArt.PlugEmitter;
                latchFlash.sharedMaterial = BluePlugArt.GlowMaterial;
                latchFlash.sprite = arrivalFlash.sprite = legacy ? BluePlugArt.FinaleLinked : BluePlugArt.Linked;
                headHalo.transform.localPosition = new Vector2(legacy ? -.57f : -.52f, 0);
                headHalo.transform.localScale = Vector3.one * (legacy ? 1.7f : 1.12f);
                cableStripe.enabled = !legacy;
                cableEdge.widthMultiplier = legacy ? .037f * 2.15f : cableWidth * 2.2f;
                cableEdge.startColor = cableEdge.endColor = legacy ? new Color(.025f, .095f, .21f, .95f) : new Color32(12, 30, 54, 255);
                cableLight.widthMultiplier = legacy ? .037f * 5f : cableWidth * 3.2f;
                cableLight.startColor = cableLight.endColor = Signal(legacy ? .16f : .11f);
                cableCore.widthMultiplier = legacy ? .037f : cableWidth * .56f;
                StyleOrbit(windCore, false, legacy);
                StyleOrbit(windGlow, true, legacy);
                OrbitOpacity(1);
            }
            bool hiding = legacy && campaign.Sniper.Clashing;
            if (hiding && !clashHidden) ResetFeedback();
            clashHidden = hiding;
        }

        public Vector2 ProjectOrbit(Vector2 center, Vector2 tip)
        {
            Vector2 radial = tip - center;
            if (FinaleActive) return center + new Vector2(radial.x, radial.y * orbitHeight);
            // 손목의 미세한 기울기와 좌우 높이 차이만 그림에 더한다. 벽 가까이에서는 호출자가 원래 팁으로 복귀한다.
            float lift = orbitHeight + .055f * radial.normalized.x + .028f * Mathf.Sin(Time.time * 4.1f);
            return center + new Vector2(radial.x, radial.y * lift + radial.x * .034f);
        }

        public void Show(Vector2 hand, Vector2 plug, Vector2 insertionDirection, BluePlugPhase phase,
            Vector2? windCenter = null, float charge = 0, float range = 0, Vector2? windingGrip = null)
        {
            if (!assembly) return;
            FollowEncounter();
            if (clashHidden) return;
            bool visible = phase != BluePlugPhase.Ready;
            bool winding = phase == BluePlugPhase.Winding;
            bool traveling = phase == BluePlugPhase.Towing || phase == BluePlugPhase.Swinging;
            if (winding && previousPhase != phase)
            {
                ClearOrbit();
                windTrailStart = Time.time;
            }
            else if (!finaleLook && phase == BluePlugPhase.Flying && previousPhase == BluePlugPhase.Winding)
                orbitReleasedAt = Time.time;
            else if (!winding && (phase != BluePlugPhase.Flying || finaleLook))
                ClearOrbit();
            sound?.Show(phase, charge, finaleLook);
            if (effects) effects.SetTowing(phase == BluePlugPhase.Towing);
            if (phase == BluePlugPhase.Towing && previousPhase != phase && !finaleLook)
                towStartedAt = Time.time;
            else if (phase != BluePlugPhase.Towing || finaleLook)
                towStartedAt = -100f;
            if (previousPhase == BluePlugPhase.Retracting && !visible && actor) rearmStart = Time.time;
            previousPhase = phase;
            if (assembly.gameObject.activeSelf != visible) assembly.gameObject.SetActive(visible);
            rangeGuide.enabled = winding && range > 0;
            if (!visible)
            {
                towPulse.enabled = false;
                latchTime = -100f;
                clicked = sendLatchSparks = false;
                localLight.Clear();
                if (finaleLook) flightTrail.Clear();
                else flightTrail.End();
                return;
            }

            Vector2 direction = insertionDirection.sqrMagnitude > .0001f ? insertionDirection.normalized : Vector2.right;
            float heading = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (!finaleLook && phase == BluePlugPhase.Retracting)
            {
                // 핀이 아니라 부싱이 먼저 돌아온다. 도킹 각도에서 갑자기 반 바퀴 뒤집지 않는다.
                Vector2 fromHand = plug - hand;
                float current = plugHead.eulerAngles.z;
                heading = fromHand.sqrMagnitude > .02f
                    ? Mathf.MoveTowardsAngle(current, Mathf.Atan2(fromHand.y, fromHand.x) * Mathf.Rad2Deg,
                        900f * Time.deltaTime)
                    : current;
                float radians = heading * Mathf.Deg2Rad;
                direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            }
            // 소환 첫 몇 프레임에는 손 바로 옆에서 케이스가 펴진다. 투척 입력과 물리 팁은 기다리지 않는다.
            float summon = winding && !finaleLook ? Mathf.SmoothStep(.42f, 1, (Time.time - windTrailStart) / .075f) : 1;
            float headScale = plugScale * summon;
            plugHead.localScale = Vector3.one * headScale;
            plugHead.SetPositionAndRotation(plug, Quaternion.Euler(0, 0, heading));
            if (actor) lastGripLocal = transform.InverseTransformPoint(hand);
            rearmStart = -100;
            bool behind = winding && windingGrip.HasValue && windCenter.HasValue && direction.y < -.05f;
            int artOrder = actor ? actor.sortingOrder : 12;
            int order = behind ? artOrder - (finaleLook ? 6 : 9) : artOrder + 1;
            cableLight.sortingOrder = finaleLook ? order + 1 : order;
            cableEdge.sortingOrder = finaleLook ? order : order + 1;
            cableStripe.sortingOrder = order + 2;
            int headOrder = order + (finaleLook ? 3 : 4);
            cableCore.sortingOrder = headHalo.sortingOrder = headOrder - 1;
            headBody.sortingOrder = headOrder;
            filament.sortingOrder = headOrder + 1;
            latchFlash.sortingOrder = headOrder + 2;
            if (winding && finaleLook)
            {
                windGlow.sortingOrder = order;
                windCore.sortingOrder = order + 1;
                DrawFinaleArc(windCenter ?? hand, plug, direction, charge);
            }
            else if (winding)
            {
                // 실제 단자 좌표를 기록한다. 몸 앞/뒤 경계에서는 몸통과 빈 링 전체가 번갈아 깜빡이지 않게 한다.
                windGlow.sortingOrder = artOrder - 2;
                windCore.sortingOrder = artOrder - 1;
                DrawWindTrail(windCenter ?? hand, plugHead.position, heading);
            }
            if (rangeGuide.enabled) DrawRangeGuide(windCenter ?? hand, charge, range);

            // 그림의 기준점은 단자 끝이다. 전선은 플러그 뒤쪽의 케이블 부싱에 붙는다.
            Vector2 tail = plug - direction * (BluePlugArt.PlugTailOffset * headScale);
            DrawCable(hand, winding ? windingGrip : null, tail, phase, direction);
            DrawTowPulse(tail, hand, headOrder);
            flightTrail.Show(finaleLook ? tail : (Vector2)plugHead.position, phase, charge, order, headBody.sortingLayerID);
            bool seated = phase == BluePlugPhase.Connected || traveling;
            float flash = seated ? Mathf.Clamp01(1f - (Time.time - latchTime) / (finaleLook ? .16f : dockFlashSeconds)) : 0;
            float energy = winding ? Mathf.Lerp(.5f, peakEmission, charge) :
                traveling ? peakEmission * .85f : phase == BluePlugPhase.Flying ? peakEmission * .75f :
                phase == BluePlugPhase.Retracting ? .60f : Mathf.Lerp(.9f, peakEmission, flash);
            if (sendLatchSparks)
            {
                if (seated)
                {
                    if (effects) effects.Latch(plug, -direction);
                    if (actor && !reducedFlash && AncientVisuals.HighQuality) DockFlash?.Invoke(this, plug);
                }
                sendLatchSparks = false;
            }
            var glow = CableBlue;
            glow.a = winding ? Mathf.Lerp(.14f, .34f, charge) : .22f;
            if (finaleLook) glow.a = winding ? Mathf.Lerp(.38f, .78f, charge) : .52f;
            headHalo.enabled = cableLight.enabled = finaleLook || AncientVisuals.HighQuality;
            headHalo.color = glow;
            filament.GetPropertyBlock(glowProperties);
            glowProperties.SetFloat(GlowEnergyId, energy);
            filament.SetPropertyBlock(glowProperties);
            cordProperties.SetFloat(EnergyId, energy);
            cordProperties.SetFloat(FlowId, phase == BluePlugPhase.Retracting ? -1 : traveling ? 1.5f :
                phase == BluePlugPhase.Connected ? .23f : 1);
            cordProperties.SetFloat(LengthId, Vector2.Distance(hand, tail));
            cordProperties.SetColor(TintId, CableBlue);
            cableCore.SetPropertyBlock(cordProperties);
            if (actor) localLight.Set(traveling ? hand : plug, CableBlue, Mathf.Min(1.5f, energy * .38f), lightRadius);
            latchFlash.enabled = flash > 0;
            if (latchFlash.enabled)
            {
                var contact = !finaleLook && AncientVisuals.HighQuality && !reducedFlash
                    ? BluePlugArt.Contact(1f - flash) : null;
                if (contact)
                {
                    latchFlash.sharedMaterial = BluePlugArt.SolidMaterial;
                    latchFlash.sprite = contact;
                    latchFlash.color = Color.white;
                    latchFlash.transform.localScale = Vector3.one;
                }
                else
                {
                    latchFlash.sharedMaterial = BluePlugArt.GlowMaterial;
                    latchFlash.sprite = finaleLook ? BluePlugArt.FinaleLinked : BluePlugArt.Linked;
                    latchFlash.color = Signal(flash * (finaleLook ? (reducedFlash ? .30f : .91f) : (reducedFlash ? .26f : .72f)));
                    latchFlash.transform.localScale = Vector3.one * (finaleLook ? .65f + 1.75f * (1f - flash) : .54f + .78f * (1f - flash));
                }
            }
        }

        public void Click()
        {
            FollowEncounter();
            if (clicked || clashHidden) return;
            clicked = true;
            ClearOrbit();
            reducedFlash = AncientGame.Saved("reducedFeedback") != 0;
            latchTime = Time.time;
            sendLatchSparks = true;
            sound?.Click();
        }

        public void Arrival(Vector2 point)
        {
            FollowEncounter();
            if (clashHidden) return;
            if (effects) effects.Arrival(point);
            sound?.Arrival();
            reducedFlash = AncientGame.Saved("reducedFeedback") != 0;
            arrivalPoint = point;
            arrivalTime = Time.time;
        }

        void LateUpdate()
        {
            FollowEncounter();
            if (clashHidden) return;
            flightTrail?.Tick();
            if (orbitReleasedAt >= 0)
            {
                float remaining = 1f - (Time.time - orbitReleasedAt) / orbitReleaseFade;
                if (remaining <= 0) ClearOrbit();
                else OrbitOpacity(remaining);
            }
            float rearm = (Time.time - rearmStart) / .12f;
            reelLight.enabled = rearm < 1 && actor;
            if (reelLight.enabled)
            {
                reelLight.transform.position = transform.TransformPoint(lastGripLocal);
                var shade = CableBlue;
                shade.a = .7f * Mathf.Sin(rearm * Mathf.PI);
                reelLight.color = shade;
            }
            float progress = (Time.time - arrivalTime) / .18f;
            arrivalFlash.enabled = progress < 1;
            if (progress >= 1) return;
            arrivalFlash.transform.position = arrivalPoint;
            arrivalFlash.transform.localScale = Vector3.one * Mathf.Lerp(.60f, 2.1f, progress);
            arrivalFlash.color = Signal((1 - progress) * (reducedFlash ? .28f : .82f));
        }

        void OnDisable() => ResetFeedback();

        public void ResetFeedback()
        {
            ClearOrbit();
            if (assembly) assembly.gameObject.SetActive(false);
            sound?.Clear();
            localLight?.Clear();
            flightTrail?.Clear();
            if (effects) effects.Clear();
            if (reelLight) reelLight.enabled = false;
            if (towPulse) towPulse.enabled = false;
            if (latchFlash) latchFlash.enabled = false;
            towStartedAt = -100f;
            rearmStart = -100;
            previousPhase = BluePlugPhase.Ready;
            clicked = sendLatchSparks = false;
            if (arrivalFlash) arrivalFlash.enabled = false;
            latchTime = arrivalTime = -100f;
        }

        void DrawRangeGuide(Vector2 center, float charge, float range)
        {
            var shade = Signal(.20f + charge * .24f);
            rangeGuide.startColor = rangeGuide.endColor = shade;
            for (int i = 0; i < guideMesh.Length; i++)
            {
                float angle = i * (2f * Mathf.PI / RangePoints);
                guideMesh[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * range;
            }
            rangeGuide.SetPositions(guideMesh);
        }

        // 회전 중에는 오른손 매듭에 정점을 둔다. 릴리즈 프레임에는 그 매듭을 즉시 푼다.
        void DrawCable(Vector2 left, Vector2? right, Vector2 tail, BluePlugPhase phase, Vector2 plugDirection)
        {
            const int gripIndex = 5;
            float length = Vector2.Distance(left, tail);
            bool loose = phase == BluePlugPhase.Connected || phase == BluePlugPhase.Flying;
            float sag = phase == BluePlugPhase.Connected ?
                (finaleLook ? Mathf.Min(.72f, length * .092f) : Mathf.Min(.80f, length * .115f)) :
                phase == BluePlugPhase.Flying ?
                (finaleLook ? Mathf.Min(.28f, length * .055f) : Mathf.Min(.24f, length * .046f)) : 0;
            // 수직 전선도 느슨함이 보이도록 부싱 뒤쪽으로 활 모양의 여유를 준다.
            float vertical = 1f - Mathf.Abs((tail - left).normalized.x);
            float side = Mathf.Clamp(.5f - 1.5f * plugDirection.x, -1f, 1f);
            Vector2 slackDirection = finaleLook ? Vector2.down : Vector2.down + Vector2.right * (side * vertical * .74f);
            bool finaleTravel = finaleLook && (phase == BluePlugPhase.Towing || phase == BluePlugPhase.Swinging);
            Vector2 across = Vector2.Perpendicular((tail - left).normalized);
            for (int i = 0; i < cableMesh.Length; i++)
            {
                bool betweenHands = right.HasValue && i <= gripIndex;
                Vector2 start = betweenHands || !right.HasValue ? left : right.Value;
                Vector2 end = betweenHands ? right.Value : tail;
                float t = betweenHands ? i / (float)gripIndex :
                    right.HasValue ? (i - gripIndex) / (float)(CablePoints - 1 - gripIndex) : i / (CablePoints - 1f);
                float envelope = 4 * t * (1 - t);
                float segmentSag = betweenHands ? Mathf.Min(.18f, Vector2.Distance(start, end) * .22f) : sag;
                // 견인·스윙 중 중심선은 손과 고정점을 정확히 잇고, 전류만 재질 안에서 흐른다.
                Vector2 point = Vector2.Lerp(start, end, t)
                    + (loose ? slackDirection : Vector2.down) * (segmentSag * envelope);
                if (finaleTravel) point += across * (.055f * envelope * Mathf.Sin(t * 39f - Time.time * 68f));
                cableMesh[i] = point;
            }
            cableEdge.SetPositions(cableMesh);
            cableLight.SetPositions(cableMesh);
            cableStripe.SetPositions(cableMesh);
            cableCore.SetPositions(cableMesh);
        }

        void DrawTowPulse(Vector2 tail, Vector2 hand, int order)
        {
            float progress = (Time.time - towStartedAt) / towPulseSeconds;
            float length = Vector2.Distance(tail, hand);
            towPulse.enabled = towStartedAt >= 0 && progress < 1 && length > .14f &&
                AncientVisuals.HighQuality && !reducedFlash;
            if (!towPulse.enabled) return;
            // 견인 중 전선은 곧다. 펄스의 머리와 꼬리를 그 두 끝 사이로 제한한다.
            float span = Mathf.Min(.25f, .86f / length);
            float head = progress * (1f + span);
            towPulse.SetPosition(0, Vector2.Lerp(tail, hand, head - span));
            towPulse.SetPosition(1, Vector2.Lerp(tail, hand, head));
            towPulse.sortingOrder = order;
        }

        void DrawFinaleArc(Vector2 center, Vector2 tip, Vector2 tangent, float charge)
        {
            const int WindTrailPoints = 14;
            const float WindTrailAngle = 102f * Mathf.Deg2Rad;
            windGlow.positionCount = windCore.positionCount = WindTrailPoints;
            windGlow.enabled = windCore.enabled = true;
            windGlow.endColor = Signal(Mathf.Lerp(.19f, .40f, charge));
            windCore.endColor = Signal(Mathf.Lerp(.48f, .85f, charge));
            Vector2 radial = tip - center;
            float sign = radial.x * tangent.y - radial.y * tangent.x >= 0 ? 1f : -1f;
            Vector2 unitRadius = new Vector2(tangent.y, -tangent.x) * sign;
            // 벽 옆에서는 투영을 생략할 수도 있다. 실제 끝점과 회전 접선에서 화면상의 타원을 복원한다.
            float height = orbitHeight;
            if (Mathf.Abs(unitRadius.x) > .06f && Mathf.Abs(unitRadius.y) > .06f && Mathf.Abs(radial.x) > .002f)
                height = Mathf.Clamp(radial.y * unitRadius.x / (radial.x * unitRadius.y), orbitHeight, 1f);
            Vector2 planar = new Vector2(radial.x, radial.y / height);
            float length = WindTrailAngle * Mathf.Lerp(.58f, 1.2f, charge) * Mathf.Clamp01((Time.time - windTrailStart) / .09f);
            for (int i = 0; i < WindTrailPoints; i++)
            {
                float t = i / (WindTrailPoints - 1f);
                float angle = -sign * length * (1f - t);
                float cosine = Mathf.Cos(angle), sine = Mathf.Sin(angle);
                Vector2 rotated = new Vector2(planar.x * cosine - planar.y * sine,
                    (planar.x * sine + planar.y * cosine) * height);
                Vector2 heading = new Vector2(tangent.x * cosine - tangent.y * sine,
                    tangent.x * sine + tangent.y * cosine);
                Vector2 point = center + rotated - heading * (.56f * plugScale);
                windGlow.SetPosition(i, point);
                windCore.SetPosition(i, point);
            }
        }

        void ClearOrbit()
        {
            orbit.Clear();
            orbitReleasedAt = -100f;
            if (windCore) windCore.enabled = false;
            if (windGlow) windGlow.enabled = false;
        }

        void DrawWindTrail(Vector2 center, Vector2 actualTip, float heading)
        {
            // 팁이 벽에 막혀 손으로 접히면 벽 너머에 원을 보간하지 않는다.
            if ((actualTip - center).sqrMagnitude < .0025f) { ClearOrbit(); return; }
            orbit.Add(new System.Numerics.Vector2(actualTip.x, actualTip.y), heading, Time.time);
            if (orbit.Count < 2)
            {
                windGlow.enabled = windCore.enabled = false;
                return;
            }

            int points = 0;
            for (int segment = 0; segment < orbit.Count - 1; segment++)
            {
                float start = segment == 0 ? orbit.FirstFraction : 0f;
                for (int j = segment == 0 ? 0 : 1; j <= OrbitSubdivisions; j++)
                {
                    var sample = orbit.Point(segment, Mathf.Lerp(start, 1f, j / (float)OrbitSubdivisions));
                    orbitMesh[points++] = new Vector3(sample.X, sample.Y, 0);
                }
            }
            windCore.positionCount = windGlow.positionCount = points;
            windCore.SetPositions(orbitMesh);
            windGlow.SetPositions(orbitMesh);
            windGlow.enabled = AncientVisuals.HighQuality;
            windCore.enabled = true;
            OrbitOpacity(1);
        }

        void OrbitOpacity(float opacity)
        {
            orbitProperties.SetColor(ColorId, new Color(1f, 1f, 1f, opacity));
            windGlow.SetPropertyBlock(orbitProperties);
            windCore.SetPropertyBlock(orbitProperties);
        }

        LineRenderer MakeWindTrail(string label, float width, bool glow, int order)
        {
            var line = MakeLine(label, 0, width, Color.white, order);
            // 몸통·헤드를 숨겨도 이미 지나간 월드 좌표는 남겨 놓을 수 있는 독립 렌더러다.
            line.transform.SetParent(transform, false);
            StyleOrbit(line, glow, false);
            line.enabled = false;
            return line;
        }

        static void StyleOrbit(LineRenderer line, bool glow, bool legacy)
        {
            line.numCapVertices = legacy ? 0 : 2;
            line.sharedMaterial = glow || legacy ? BluePlugArt.GlowMaterial : BluePlugArt.SolidMaterial;
            line.widthMultiplier = glow ? (legacy ? .16f : .14f) : (legacy ? .052f : .068f);
            if (legacy)
            {
                var original = new Gradient();
                original.SetKeys(new[] { new GradientColorKey(CableBlue, 0), new GradientColorKey(CableBlue, 1) },
                    new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 1) });
                line.colorGradient = original;
                line.widthCurve = AnimationCurve.Linear(0, .12f, 1, 1);
                return;
            }
            var gradient = new Gradient();
            gradient.SetKeys(new[] {
                new GradientColorKey(new Color32(25, 55, 100, 255), 0),
                new GradientColorKey(new Color32(43, 143, 238, 255), .43f),
                new GradientColorKey(CableBlue, .80f),
                new GradientColorKey(new Color32(216, 246, 255, 255), 1)
            }, new[] {
                new GradientAlphaKey(0, 0),
                new GradientAlphaKey(glow ? .08f : .27f, .32f),
                new GradientAlphaKey(glow ? .25f : .86f, 1)
            });
            line.colorGradient = gradient;
            line.widthCurve = AnimationCurve.EaseInOut(0f, .08f, 1f, 1f);
        }

        LineRenderer MakeLine(string label, int points, float width, Color color, int order)
        {
            var child = new GameObject(label);
            child.transform.SetParent(assembly, false);
            var cable = child.AddComponent<LineRenderer>();
            cable.sharedMaterial = BluePlugArt.SolidMaterial;
            cable.useWorldSpace = true;
            cable.positionCount = points;
            cable.widthMultiplier = width;
            cable.startColor = cable.endColor = color;
            cable.sortingOrder = order;
            return cable;
        }

    }
}
