using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.PlayerCore
{
    // 순찰·훈련장에서 같은 발사구와 착탄광을 쓴다. 타격 판정과 점수는 여기서 처리하지 않는다.
    public sealed class FilamentStrikeFX : MonoBehaviour
    {
        const int ArcPoints = 19, SparkCount = 8;
        const float SparkLife = .14f;
        static readonly Color Amber = new Color(1f,.60f,.14f);
        GameObject root;
        Material stroke;
        LineRenderer beam, aura, centre, arc, arcLight, sight, chargeHalo;
        const int HaloPoints = 16;
        readonly Vector3[] haloPath = new Vector3[HaloPoints];
        readonly Vector2[] haloUnit = new Vector2[HaloPoints];
        readonly LineRenderer[] sparks = new LineRenderer[SparkCount];
        readonly Vector2[] sparkDirections = new Vector2[SparkCount];
        readonly Vector3[] arcPath = new Vector3[ArcPoints];
        readonly Vector3[] gripPath = new Vector3[ArcPoints];
        FilamentCutInk ink;
        int bladeStep;
        AncientBeamVisuals profile;
        float sparkUntil, beamUntil, beamDuration, beamWidth, power;
        bool soundedSweep, surfaceTouched, combatContact;
        Vector2 beamStart, beamEnd, strikePoint;
        AudioSource bladeVoice;
        AudioClip swish, scrape, contact;
        AncientFeedbackProfile mix;

        void CreateRoot()
        {
            if (root) return;
            root = new GameObject("필라멘트 전투 효과");
            root.transform.SetParent(transform, false);
            var shader = Resources.Load<Shader>("Shaders/ParticleGlow");
            if (!shader || !shader.isSupported) shader = Shader.Find("Sprites/Default");
            stroke = new Material(shader) { name = "필라멘트 획 공유광" };
        }

        LineRenderer Stroke(string label, int order, int count)
        {
            var child = new GameObject(label);
            child.transform.SetParent(root.transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = stroke;
            line.sortingOrder = order;
            line.positionCount = count;
            line.numCapVertices = 2;
            line.enabled = false;
            return line;
        }

        public void Aim(Vector2 start, Vector2 end, float fill)
        {
            Vector2 ray = end - start;
            if (ray.sqrMagnitude < .0001f) { StopAim(); return; }
            if (!sight)
            {
                CreateRoot();
                sight = Stroke("레이저 사전 조준선", 16, 2);
                chargeHalo = Stroke("발사구 축전 고리", 22, HaloPoints);
                chargeHalo.loop = true;
                for (int i=0; i<HaloPoints; i++)
                {
                    float phase = 2 * Mathf.PI * i / HaloPoints;
                    haloUnit[i] = new Vector2(Mathf.Cos(phase), Mathf.Sin(phase));
                }
            }
            fill = Mathf.Clamp01(fill);
            var tint = AncientBeamVisuals.FilamentInk(fill);
            tint.a = Mathf.Lerp(.24f,.50f,fill);
            sight.startColor = sight.endColor = tint;
            Segment(sight,start,end,.018f+fill*.016f,.014f);
            // 발사 직전 고리가 휘감기는 폭을 줄여 완충을 색과 크기로 함께 표시한다.
            Vector2 along = ray.normalized, across = new Vector2(-along.y,along.x);
            float radius = Mathf.Lerp(.32f,.13f,fill);
            for (int i=0; i<HaloPoints; i++)
                haloPath[i] = start + along*(.16f+haloUnit[i].x*radius*.48f) + across*haloUnit[i].y*radius;
            chargeHalo.SetPositions(haloPath);
            tint.a = .64f + .26f*fill;
            chargeHalo.startColor=chargeHalo.endColor=tint;
            chargeHalo.startWidth=chargeHalo.endWidth=.026f+.023f*fill;
            chargeHalo.enabled=true;
        }

        public void StopAim()
        {
            if (sight) sight.enabled=chargeHalo.enabled=false;
        }

        public void Laser(Vector2 from, Vector2 to, float width, float normalizedEnergy, float duration)
        {
            StopAim();
            if ((to-from).sqrMagnitude < .0001f || width <= 0 || duration <= 0) return;
            if (!beam)
            {
                CreateRoot();
                aura = Stroke("빔 외광", 21, 2);
                beam = Stroke("필라멘트 빔", 22, 2);
                centre = Stroke("백열 코어", 23, 2);
                profile = new AncientBeamVisuals(aura, true);
                profile.StyleBody(beam, centre, true);
            }
            beamStart=from; beamEnd=to; beamWidth=width;
            power=Mathf.Clamp01(normalizedEnergy);
            beamDuration=duration; beamUntil=Time.time+duration;
            Segment(beam,from,to,width,width);
            Segment(aura,from,to,width*1.82f,width*1.65f);
            Segment(centre,from,to,width*.30f,width*.22f);
            PaintBeam(1);
        }

        void PaintBeam(float opacity)
        {
            profile.UpdateBody(beam,aura,centre,opacity,Time.time,power);
            bool detailed=AncientVisuals.HighQuality;
            aura.enabled=detailed;
            profile.ShowEndpoints(beamStart,beamEnd,beamWidth,opacity,detailed,power);
        }

        public void BeginBlade(int step)
        {
            bladeStep = step;
            CutInk();  // 첫 활성 프레임 전에 시트와 메시를 준비한다.
            soundedSweep = surfaceTouched = combatContact = false;
            HideSweep();
        }

        public void HideSweep()
        {
            if (arc) arc.enabled = arcLight.enabled = false;
            ink?.HideRibbon();
        }

        public void SwingSound()
        {
            if (soundedSweep) return;
            soundedSweep = true;
            BladeSound(0);
        }

        public void BladeSweep(SpriteRenderer actor, int step, int frame, LayerMask solids)
        {
            SwingSound();
            if (!arc)
            {
                CreateRoot();
                arcLight = Stroke("칼끝의 잔광",20,ArcPoints);
                arc = Stroke("실제 칼끝의 이동선",21,ArcPoints);
                arc.startColor = new Color(1,.62f,.2f,0);
                arc.endColor = new Color(1,.92f,.67f,.72f);
                arc.startWidth = .008f; arc.endWidth = .055f;
                arcLight.startColor = new Color(1,.42f,.1f,0);
                arcLight.endColor = new Color(1,.69f,.22f,.16f);
                arcLight.startWidth = .016f; arcLight.endWidth = .10f;
            }
            AncientBladeMotion.Sweep(actor,step,frame,arcPath,gripPath);
            CutInk().Sweep(arcPath, gripPath, step, AncientVisuals.HighQuality);
            arc.SetPositions(arcPath); arcLight.SetPositions(arcPath);
            arc.enabled = true; arcLight.enabled = AncientVisuals.HighQuality;
            if (surfaceTouched || combatContact) return;
            Vector2 hilt = AncientBladeMotion.World(actor, AncientBladeAtlas.HiltLocal(step,frame));
            var hit = Physics2D.Linecast(hilt, arcPath[arcPath.Length-1], solids);
            if (!hit || hit.fraction <= .001f) return;
            surfaceTouched = true;
            BladeSound(1);
            CutInk().Contact(hit.point, hit.normal, FilamentCutInk.Wall);
        }

        public void BladeContact(Vector2 point, Vector2 away, bool sound)
        {
            if (combatContact) return;
            combatContact = true;
            CutInk().Contact(point, away, bladeStep == 3 ? FilamentCutInk.Finisher : FilamentCutInk.Hit);
            if (sound) BladeSound(2);
        }

        FilamentCutInk CutInk()
        {
            if (ink == null)
            {
                CreateRoot();
                ink = new FilamentCutInk(root.transform, stroke, ArcPoints);
            }
            return ink;
        }

        void BladeSound(int cue)
        {
            if (!Application.isFocused || AudioListener.pause || Time.timeScale <= 0 || AncientGame.Saved("muted") != 0) return;
            if (!bladeVoice)
            {
                bladeVoice = gameObject.AddComponent<AudioSource>();
                bladeVoice.playOnAwake = false; bladeVoice.spatialBlend = 0; bladeVoice.priority = 116;
                mix = Resources.Load<AncientFeedbackProfile>("Audio/FieldFeedback");
                if (mix) bladeVoice.outputAudioMixerGroup = mix.effects;
                swish = Resources.Load<AudioClip>("Audio/BladeSwish");
                scrape = Resources.Load<AudioClip>("Audio/BladeWall");
                contact = Resources.Load<AudioClip>("Audio/Contact");
            }
            AudioClip clip = cue == 0 ? swish : cue == 1 ? scrape : contact;
            if (clip) bladeVoice.PlayOneShot(clip, (mix ? mix.effectsVolume : .22f) * (cue == 1 ? .58f : .85f));
        }

        public void Spark(Vector2 point, Vector2 away, bool small = false)
        {
            CreateRoot();
            float heading=away.sqrMagnitude>.001f ? Mathf.Atan2(away.y,away.x) : 0;
            strikePoint=point;
            int visible=small ? 3 : AncientVisuals.HighQuality?SparkCount:3;
            for (int i=0;i<SparkCount;i++)
            {
                if(i>=visible) { if(sparks[i]) sparks[i].enabled=false; continue; }
                if(!sparks[i]) sparks[i]=Stroke("절삭 불티",24,2);
                float angle=heading-.96f+1.92f*i/Mathf.Max(1,visible-1);
                sparkDirections[i]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(.48f+(i*5%7)*.105f)*(small ? .45f : 1f);
                sparks[i].enabled=true;
            }
            sparkUntil=Time.time+SparkLife;
            PaintSpark(0);
        }

        static void Segment(LineRenderer line,Vector2 from,Vector2 to,float start,float end)
        {
            line.SetPosition(0,from); line.SetPosition(1,to);
            line.startWidth=start; line.endWidth=end; line.enabled=true;
        }

        void PaintSpark(float phase)
        {
            for(int i=0;i<sparks.Length;i++)
            {
                var ray=sparks[i]; if(!ray || !ray.enabled) continue;
                var d=sparkDirections[i];
                Segment(ray,strikePoint+d*phase,strikePoint+d*(phase+.15f*(1-phase)),.034f,.012f);
                var tint=Color.Lerp(Color.white,Amber,phase); tint.a=1-phase;
                ray.startColor=ray.endColor=tint;
            }
        }

        void LateUpdate()
        {
            float now=Time.time;
            ink?.Tick(now);
            if(beam && beam.enabled)
            {
                if(now<beamUntil) PaintBeam(Mathf.Clamp01((beamUntil-now)/Mathf.Min(.10f,beamDuration)));
                else { beam.enabled=aura.enabled=centre.enabled=false; profile.HideEndpoints(); }
            }
            if(sparks[0] && sparks[0].enabled)
            {
                if(now<sparkUntil) PaintSpark(1-(sparkUntil-now)/SparkLife);
                else foreach(var spark in sparks) if(spark) spark.enabled=false;
            }
        }

        public void Clear()
        {
            StopAim();
            if(beam) { beam.enabled=aura.enabled=centre.enabled=false; profile.HideEndpoints(); }
            HideSweep();
            ink?.HideContact();
            if (bladeVoice) bladeVoice.Stop();
            foreach(var spark in sparks) if(spark) spark.enabled=false;
        }

        void OnDisable() => Clear();
        void OnDestroy() { ink?.Dispose(); profile?.Dispose(); if(stroke) Destroy(stroke); }
    }
}
