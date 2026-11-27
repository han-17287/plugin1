using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.PlayerCore
{
    [DisallowMultipleComponent]
    public sealed class BluePlugSound : MonoBehaviour
    {
        enum Cue { Spin, PayOut, Travel, Rewind, Full, Throw, Dock, Land, Rearm, SwingAir }
        const int Rate = 24000;
        static readonly float[] Durations = { .25f, .30f, .40f, .28f, .105f, .104f, .092f, .126f, .064f, .58f };
        static AudioClip[] bank;
        static int bankUsers;

        readonly AudioSource[] beds = new AudioSource[2];
        readonly AudioSource[] hits = new AudioSource[2];
        AncientFeedbackProfile profile;
        AncientAudio campaignAudio;
        PlayerInputReader practiceInput;
        Rigidbody2D playerBody;
        [SerializeField, Min(1f)] float swingAirFullSpeed = 18f;
        BluePlugPhase previous = BluePlugPhase.Ready;
        int bed = -1, nextHit, checkedFrame = -1;
        float bedGain;
        bool fullPlayed, docked, arrived, audible, acquired, appPaused;

        public static BluePlugSound ForPlayer(GameObject owner)
        {
            // 소품과 적도 같은 플러그 그림을 쓴다. 이 경우 음원/재생기를 만들지 않는다.
            if (!owner || !(owner.GetComponent<AncientHero>() || owner.GetComponent<PlayerStateMachine>())) return null;
            var existing = owner.GetComponent<BluePlugSound>();
            return existing ? existing : owner.AddComponent<BluePlugSound>();
        }

        void Awake()
        {
            practiceInput = GetComponent<PlayerInputReader>();
            playerBody = GetComponent<Rigidbody2D>();
            var game = AncientGame.Instance;
            if (game) campaignAudio = game.GetComponent<AncientAudio>();
            profile = game && game.Feedback ? game.Feedback : Resources.Load<AncientFeedbackProfile>("Audio/FieldFeedback");
            if (bank == null)
            {
                bank = new AudioClip[Durations.Length];
                for (int i = 0; i < bank.Length; ++i) bank[i] = Build((Cue)i);
            }
            ++bankUsers;
            acquired = true;
            for (int i = 0; i < beds.Length; ++i) beds[i] = Source(true);
            for (int i = 0; i < hits.Length; ++i) hits[i] = Source(false);
        }

        AudioSource Source(bool loop)
        {
            var voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0;
            voice.dopplerLevel = 0;
            voice.priority = loop ? 142 : 108;
            voice.loop = loop;
            voice.volume = 0;
            if (profile) voice.outputAudioMixerGroup = profile.effects;
            return voice;
        }

        bool CanSound()
        {
            if (!isActiveAndEnabled || appPaused || !Application.isFocused || AudioListener.pause || Time.timeScale <= 0) return false;
            var game = AncientGame.Instance;
            if ((game && game.Paused) || (practiceInput && practiceInput.Paused)) return false;
            if (campaignAudio) return !campaignAudio.Muted;
            // 연습 씬에는 캠페인 믹서 관리자가 없다. F7 저장값은 프레임당 한 번만 읽는다.
            if (checkedFrame != Time.frameCount)
            {
                checkedFrame = Time.frameCount;
                audible = AncientGame.Saved("muted") == 0;
            }
            return audible;
        }

        float Level => profile ? profile.effectsVolume : .22f;

        public void Show(BluePlugPhase phase, float charge, bool finale = false)
        {
            if (!acquired) return;
            BluePlugPhase old = previous;
            previous = phase;
            if (phase == BluePlugPhase.Ready && old == phase) return;
            // 회수 프레임에서 다음 1회의 소리를 준비한다. 낮은 FPS로 첫 회전 그림을 건너뛰어도 딸깍 소리가 난다.
            if (phase == BluePlugPhase.Ready || phase == BluePlugPhase.Winding && old != phase)
                fullPlayed = docked = arrived = false;
            charge = Mathf.Clamp01(charge);
            bool full = phase == BluePlugPhase.Winding && charge >= .999f && !fullPlayed;
            if (full) fullPlayed = true;
            if (!CanSound()) { Cut(); return; }
            if (full) Play(Cue.Full, .74f);
            if (phase == BluePlugPhase.Flying && old != phase) Play(Cue.Throw, 1f);
            if (phase == BluePlugPhase.Ready && old == BluePlugPhase.Retracting) Play(Cue.Rearm, .57f);
            switch (phase)
            {
                case BluePlugPhase.Winding:
                    Loop(Cue.Spin, Mathf.Lerp(.30f, .62f, charge), Mathf.Lerp(1f, 1.65f, charge));
                    break;
                case BluePlugPhase.Flying: Loop(Cue.PayOut, .39f, 1.04f); break;
                case BluePlugPhase.Towing: Loop(Cue.Travel, .60f, 1.15f); break;
                case BluePlugPhase.Swinging when finale:
                    Loop(Cue.Travel, .33f, .83f);
                    break;
                case BluePlugPhase.Swinging:
                    // 최저점에서 커지고 정점에서 낮아지는 실제 이동 속도의 풍절음이다.
                    float speed = playerBody ? Mathf.Clamp01(playerBody.linearVelocity.magnitude / swingAirFullSpeed) : 0f;
                    Loop(Cue.SwingAir, Mathf.Lerp(.04f, .76f, speed), Mathf.Lerp(.67f, 1.52f, speed));
                    break;
                case BluePlugPhase.Retracting: Loop(Cue.Rewind, .42f, 1.04f); break;
                default: bed = -1; break;
            }
        }

        // 둘 다 실제 물리 판정에서 호출한다. 그림 갱신 순서가 바뀌어도 중복되지 않는다.
        public void Click()
        {
            if (!acquired || docked) return;
            docked = true;
            if (CanSound()) Play(Cue.Dock, 1.16f);
        }

        public void Arrival()
        {
            if (!acquired || arrived) return;
            arrived = true;
            if (CanSound()) Play(Cue.Land, 1.1f);
        }

        void Loop(Cue cue, float gain, float pitch)
        {
            var clip = bank[(int)cue];
            if (bed < 0 || beds[bed].clip != clip || !beds[bed].isPlaying)
            {
                int other = bed >= 0 ? 1 - bed : beds[0].volume <= beds[1].volume ? 0 : 1;
                bed = other;
                var voice = beds[bed];
                voice.Stop();
                voice.volume = 0;
                voice.clip = clip;
                voice.pitch = pitch;
                voice.Play();
            }
            beds[bed].pitch = pitch;
            bedGain = gain * Level;
        }

        void Play(Cue cue, float gain)
        {
            var voice = hits[nextHit];
            nextHit = 1 - nextHit;
            voice.Stop();
            voice.clip = bank[(int)cue];
            voice.volume = Level * gain;
            voice.Play();
        }

        void LateUpdate()
        {
            if (!acquired || !(beds[0].isPlaying || beds[1].isPlaying || hits[0].isPlaying || hits[1].isPlaying)) return;
            if (!CanSound()) { Cut(); return; }
            for (int i = 0; i < beds.Length; ++i)
            {
                var voice = beds[i];
                if (!voice.isPlaying) continue;
                float target = i == bed ? bedGain : 0f;
                voice.volume = Mathf.MoveTowards(voice.volume, target, Time.unscaledDeltaTime * 12f);
                if (i != bed && voice.volume == 0) voice.Stop();
            }
        }

        void Cut()
        {
            bed = -1;
            for (int i = 0; i < beds.Length; ++i)
            {
                if (beds[i]) { beds[i].Stop(); beds[i].volume = 0; }
                if (hits[i]) hits[i].Stop();
            }
        }

        public void Clear()
        {
            previous = BluePlugPhase.Ready;
            fullPlayed = docked = arrived = false;
            Cut();
        }

        void OnApplicationPause(bool paused) { appPaused = paused; if (paused) Cut(); }
        void OnApplicationFocus(bool focused) { checkedFrame = -1; if (!focused) Cut(); }
        void OnDisable() { Clear(); }
        void OnDestroy()
        {
            if (!acquired) return;
            foreach (var voice in beds) if (voice) Destroy(voice);
            foreach (var voice in hits) if (voice) Destroy(voice);
            if (--bankUsers != 0) return;
            foreach (var clip in bank) if (clip) Destroy(clip);
            bank = null;
        }

        static AudioClip Build(Cue cue)
        {
            int length = Mathf.RoundToInt(Durations[(int)cue] * Rate);
            var samples = new float[length];
            uint state = (uint)(5501 + 103 * (int)cue);
            float smoothNoise = 0f;
            if (cue == Cue.SwingAir) WindNoise(samples, state);
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    float t = i / (float)Rate;
                    float circle = 2f * Mathf.PI * i / length;
                    // 루프는 정수 주기의 모터·래칫만 쓴다. 양 끝의 주파수와 파형이 그대로 이어진다.
                    if (cue <= Cue.Rewind) samples[i] = Motor(cue, circle);
                    else
                    {
                        state = unchecked(1664525u * state + 1013904223u);
                        float noise = (state >> 16) * (2f / 65535f) - 1f;
                        smoothNoise += .18f * (noise - smoothNoise);
                        samples[i] = Strike(cue, t, noise, smoothNoise) * Mathf.Clamp01((length - 1 - i) / 96f);
                    }
                }
            }
            var clip = AudioClip.Create("BluePlug." + cue, length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static void WindNoise(float[] samples, uint state)
        {
            const int seam = Rate / 30;
            var raw = new float[samples.Length + seam];
            float low = 0f, high = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                state = unchecked(1664525u * state + 1013904223u);
                float noise = (state >> 16) * (2f / 65535f) - 1f;
                high += .39f * (noise - high);
                low += .075f * (high - low);
                raw[i] = (high - low) * 1.65f;
            }
            // 다음 반복의 첫 샘플을 끝점 다음 실제 노이즈에 이어 클릭 없는 공기층을 만든다.
            for (int i = 0; i < samples.Length; i++)
                samples[i] = i < seam ? Mathf.Lerp(raw[samples.Length + i], raw[i], i / (float)seam) : raw[i];
        }

        static float Motor(Cue cue, float a)
        {
            if (cue == Cue.Spin)
                return (.10f + .44f * Mathf.Pow(.5f - .5f * Mathf.Cos(a), 3))
                    * (Mathf.Sin(42*a) + .38f*Mathf.Sin(89*a + .9f*Mathf.Sin(2*a)) + .14f*Mathf.Sin(147*a));
            if (cue == Cue.PayOut)
                return .17f*Mathf.Sin(37*a) + .17f*Mathf.Sin(144*a + 1.65f*Mathf.Sin(8*a))
                    + .08f*Mathf.Sin(227*a + 1.1f*Mathf.Sin(12*a));
            if (cue == Cue.Travel)
                return .28f*Mathf.Sin(48*a + .8f*Mathf.Sin(a)) + .16f*Mathf.Sin(99*a)
                    + .12f*Mathf.Sin(201*a + 1.5f*Mathf.Sin(7*a));
            return .20f*Mathf.Sin(49*a + 1.15f*Mathf.Sin(6*a))
                + (.12f + .11f*Mathf.Sin(7*a))*Mathf.Sin(161*a + 1.6f*Mathf.Sin(13*a));
        }

        static float Envelope(float time, float decay)
        {
            return time < 0f ? 0f : Mathf.Clamp01(time * 900f) * Mathf.Exp(-time * decay);
        }

        static float Tone(float frequency, float t) => Mathf.Sin(2f * Mathf.PI * frequency * t);

        static float Strike(Cue cue, float t, float noise, float soft)
        {
            switch (cue)
            {
                case Cue.Full:
                    return .40f * Tone(1090, t) * Envelope(t, 52) + .39f * Tone(1635, t-.033f) * Envelope(t-.033f, 55);
                case Cue.Throw:
                    return (.42f*Tone(345f - 1250f*t,t) + .50f*soft + .12f*noise) * Envelope(t, 40);
                case Cue.Dock:
                    return (.34f*Tone(1760,t) + .38f*noise) * Envelope(t, 177)
                        + .50f*Tone(660,t-.016f)*Envelope(t-.016f,91) + .21f*(noise-soft)*Envelope(t-.024f,86);
                case Cue.Land:
                    return (.68f*Tone(136f - 310f*t,t) + .26f*soft) * Envelope(t,32)
                        + .26f*Tone(475,t-.012f)*Envelope(t-.012f, 76);
                default:
                    return (.33f*Tone(780,t)+.21f*noise)*Envelope(t,155)
                        + .29f*Tone(530,t-.016f)*Envelope(t-.016f,100);
            }
        }
    }
}
