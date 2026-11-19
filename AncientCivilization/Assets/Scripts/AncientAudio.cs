using UnityEngine;
using UnityEngine.Audio;

namespace Plugin.AncientCivilization
{
    // 효과음과 경고음의 재생 우선순위를 보존하면서 실제 캠페인 음악을 전환한다.
    public sealed class AncientAudio : MonoBehaviour
    {
        const int PublicCueCount=12,RoadDebrisCue=12,RoadBombCue=13,CaptainTellCue=14,ParticleLaserCue=15,SampleRate=22050;
        readonly AudioClip[] clips=new AudioClip[16];
        readonly float[] nextVoice=new float[PublicCueCount];
        readonly AudioClip[] equipment = new AudioClip[(int)AncientEquipmentCue.UI + 1];
        readonly float[] nextEquipment = new float[(int)AncientEquipmentCue.UI + 1];
        // 일반 효과가 공격 전조나 UI 음성을 빼앗지 않도록 재생 자리를 분리한다.
        readonly AudioSource[] effects = new AudioSource[6], warnings = new AudioSource[3];
        readonly float[] warningStarted = new float[3];
        readonly int[] warningRank = new int[3];
        AncientGame game;
        AncientFeedbackProfile profile;
        AudioSource ui;
        AncientScorePlayer score;
        float musicVolume;
        bool muted, voicesPaused, lastMenu, focused=true;
        float duckUntil, nextHeart, lastPickup = -1f, pickupPitch = 1;
        int effectCursor;
        uint variation = 8191;
        public bool Muted => muted;
        public string CurrentScore => score?.CurrentScore;

        void Awake()
        {
            game = GetComponent<AncientGame>();
            profile = game.Feedback;
            for (int i = 0; i < effects.Length; i++) effects[i] = CreateSource(gameObject, 96, false, profile.effects);
            for (int i = 0; i < warnings.Length; i++) warnings[i] = CreateSource(gameObject, 32, false, profile.effects);
            ui = CreateSource(gameObject, 48, false, profile.ui);
            score = new AncientScorePlayer(transform, profile.music);
            musicVolume = profile.scoreVolume;
            focused = Application.isFocused;
            muted = AncientGame.Saved("muted") != 0;
            ApplyMute();
        }

        static AudioSource CreateSource(GameObject host, int priority, bool loop, AudioMixerGroup group)
        {
            var voice = host.AddComponent<AudioSource>();
            voice.outputAudioMixerGroup = group;
            voice.spatialBlend = 0;
            voice.playOnAwake = false;
            voice.loop = loop;
            voice.priority = priority;
            voice.dopplerLevel = 0;
            return voice;
        }
        void Start()
        {
            // Generation happens once behind the boot/menu presentation, never on
            // the first swing, particle pickup, or warning in the road encounter.
            for(int cue=0;cue<clips.Length;cue++)clips[cue]=CreateTone(cue);
            for (int i = 0; i < equipment.Length; i++)
                equipment[i] = Resources.Load<AudioClip>("Audio/" + ((AncientEquipmentCue)i).ToString());
        }
        void ApplyMute()
        {
            bool silence=muted||!focused;
            foreach (var voice in effects) voice.mute = silence;
            foreach (var voice in warnings) voice.mute = silence;
            ui.mute = silence;
            score.SetMuted(silence);
        }
        void OnApplicationFocus(bool value)
        {
            focused = value;
            ApplyMute();
            if (!focused) { PauseVoices(true); score.Suspend(); }
        }
        void OnDisable() { score?.Suspend(); }
        void PauseVoices(bool pause)
        {
            if(voicesPaused==pause)return;voicesPaused=pause;
            foreach (var voice in effects) { if (pause) voice.Pause(); else voice.UnPause(); }
            foreach (var voice in warnings) { if (pause) voice.Pause(); else voice.UnPause(); }
        }
        bool Speech {get{return game&&game.Story&&(game.Story.DialogueOpen||game.Story.LiftBusy);}}
        bool Panel {get{return game&&(game.skills||(game.Journal&&game.Journal.Open));}}
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.F7))
            {
                muted=!muted;ApplyMute();AncientGame.SetSaved("muted",muted?1:0);PlayerPrefs.Save();
                if(game)game.Notice(muted?"소리 / 음소거":"소리 / 켜짐");
            }
            bool menu=game&&game.MainMenu;
            if (menu && !lastMenu)
            {
                foreach (var voice in effects) voice.Stop();
                foreach (var voice in warnings) voice.Stop();
                duckUntil = 0;
            }
            lastMenu=menu;PauseVoices(!focused||menu||Panel||Speech||AncientBootIntro.Active);
            bool low = game && game.LowHealth && !game.Paused;
            if (low && Time.unscaledTime >= nextHeart)
            {
                PlayEquipment(AncientEquipmentCue.Heartbeat);
                nextHeart = Time.unscaledTime + profile.heartbeatInterval;
            }
        }
        void LateUpdate()
        {
            bool boot = AncientBootIntro.Active;
            string route = boot ? null : AncientCampaignScore.Resolve(game);
            bool countdown = route == "sixty-to-recovery";
            float elapsed = countdown && game.Finale ? 60f - game.Finale.CountdownRemaining : 0f;
            bool pause = !focused || (!game.MainMenu && Panel) || (countdown && game.Paused);
            float target = game.Story && game.Story.DialogueOpen ? profile.dialogueScoreVolume : profile.scoreVolume;
            if (Time.unscaledTime < duckUntil) target *= profile.duckMusicTo;
            float smoothing = 1f - Mathf.Exp(-10f * Mathf.Min(Time.unscaledDeltaTime, .1f));
            musicVolume = Mathf.Lerp(musicVolume, target, smoothing);
            score.Tick(route, pause, boot || game.MainMenu, musicVolume, game.LowHealth && !game.Paused, elapsed);
        }

        public void PlayScoreEvent(string slug)
        {
            if (isActiveAndEnabled && !game.MainMenu && !AncientBootIntro.Active)
                score.PlayEvent(slug);
        }

        static AudioClip CreateTone(int cue)
        {
            float duration=cue==4?.55f:cue==5?.65f:cue==7?.09f:cue==8?.45f:cue==9?.16f:cue==10?.13f:cue==11?.22f:cue==RoadDebrisCue?.24f:cue==RoadBombCue?.38f:cue==CaptainTellCue?.52f:cue==ParticleLaserCue?.42f:.18f;
            var samples=new float[Mathf.CeilToInt(SampleRate*duration)];var random=new System.Random(17287+cue);
            float phase=0,filtered=0;
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)SampleRate,life=t/duration;
                float frequency=cue==0?Mathf.Lerp(600,160,life):cue==1?Mathf.Lerp(140,55,life):cue==2?Mathf.Lerp(380,1050,life):cue==3?Mathf.Lerp(300,720,life):cue==4?Mathf.Lerp(95,30,life):cue==6?Mathf.Lerp(220,70,life):cue==7?70:cue==8?((int)(t*8)%2==0?330:440):cue==9?Mathf.Lerp(880,190,life):cue==10?Mathf.Lerp(1050,330,life):cue==11?Mathf.Lerp(560,85,life):cue==RoadDebrisCue?740:cue==RoadBombCue?((int)(t/.1f)%2==0?620:920):cue==CaptainTellCue?Mathf.Lerp(230,480,life):650;
                phase+=frequency*Mathf.PI*2/SampleRate;
                float noise=(float)random.NextDouble()*2-1;filtered=Mathf.Lerp(filtered,noise,cue==4?.15f:.4f);
                float tone=Mathf.Sin(phase),envelope=Mathf.Clamp01(t/.006f)*Mathf.Pow(1-life,1.7f);
                if(cue==0)tone=tone*.15f+noise*.65f;
                if(cue>=9&&cue<=11)tone=tone*.15f+filtered*(cue==11?.8f:.65f);
                if(cue==1||cue==4||cue==6||cue==7)tone=tone*.45f+filtered*.65f;
                if(cue==5)
                {
                    float note=t<.18f?523.25f:t<.36f?659.25f:783.99f;
                    tone=(Mathf.Sin(t*note*Mathf.PI*2)+Mathf.Sin(t*note*2*Mathf.PI*2)*.3f)*.65f;
                }
                if(cue==RoadDebrisCue||cue==RoadBombCue)
                {
                    float period=cue==RoadDebrisCue?.12f:.1f,local=Mathf.Repeat(t,period);
                    envelope=Mathf.Clamp01(local/.005f)*Mathf.Clamp01((period*.7f-local)/.025f)*(1-life*.3f);
                    tone=tone*.55f+Mathf.Sin(phase*2)*.12f;
                }
                else if(cue==CaptainTellCue)
                {envelope=Mathf.Clamp01(t/.015f)*Mathf.Clamp01((duration-t)/.04f);tone=tone*.42f+Mathf.Sin(phase*.5f)*.2f;}
                else if(cue==ParticleLaserCue)
                {
                    envelope=Mathf.Clamp01(t/.008f)*Mathf.Clamp01((duration-t)/.09f);
                    tone=(Mathf.Sin(phase)*.24f+Mathf.Sin(phase*2.01f)*.13f+filtered*.45f)*(1-.3f*life);
                }
                samples[i]=Mathf.Clamp(tone*envelope*.55f,-.8f,.8f);
            }
            var clip=AudioClip.Create("Ancient cue "+cue,samples.Length,1,SampleRate,false);clip.SetData(samples,0);return clip;
        }
        bool Blocked => muted || !focused || (game && game.MainMenu) || Panel || Speech || AncientBootIntro.Active;

        void StartVoice(AudioClip clip, int rank, float volume, float pitch = 1, float pan = 0)
        {
            if (!clip) return;
            AudioSource voice;
            if (rank > 0)
            {
                int slot = 0;
                for (int i = 0; i < warnings.Length; i++)
                {
                    if (!warnings[i].isPlaying) { slot = i; break; }
                    if (warningRank[i] < warningRank[slot] ||
                        warningRank[i] == warningRank[slot] && warningStarted[i] < warningStarted[slot]) slot = i;
                }
                if (warnings[slot].isPlaying && warningRank[slot] > rank) return;
                voice = warnings[slot];
                warningRank[slot] = rank;
                warningStarted[slot] = Time.unscaledTime;
            }
            else
            {
                for (int i = 0; i < effects.Length; i++)
                {
                    if (!effects[(effectCursor + i) % effects.Length].isPlaying)
                    { effectCursor = (effectCursor + i) % effects.Length; break; }
                }
                voice = effects[effectCursor];
                effectCursor = (effectCursor + 1) % effects.Length;
            }
            voice.clip = clip;
            voice.pitch = pitch;
            voice.volume = volume;
            voice.panStereo = pan;
            voice.Play();
        }

        // 음정 변화가 보스 패턴용 UnityEngine.Random 상태를 바꾸지 않게 분리한다.
        float VaryPitch()
        {
            variation = unchecked(variation * 1664525u + 1013904223u);
            return 1f + ((variation >> 16) / 32767.5f - 1f) * profile.pitchVariation;
        }

        public void PlayEquipment(AncientEquipmentCue cue, float pan = 0)
        {
            int index = (int)cue;
            if (index < 0 || index >= equipment.Length || !equipment[index] || Time.unscaledTime < nextEquipment[index]) return;
            if (cue == AncientEquipmentCue.UI)
            {
                if (muted || !focused || AncientBootIntro.Active) return;
                nextEquipment[index] = Time.unscaledTime + .06f;
                ui.clip = equipment[index];
                ui.volume = profile.uiVolume;
                ui.Play();
                return;
            }
            if (Blocked) return;
            bool contact = cue == AncientEquipmentCue.Contact || cue == AncientEquipmentCue.Shatter;
            int rank = cue == AncientEquipmentCue.Counter || cue == AncientEquipmentCue.Overload ? 2 :
                cue == AncientEquipmentCue.Forge || cue == AncientEquipmentCue.Sweep || cue == AncientEquipmentCue.Lens ? 1 : 0;
            nextEquipment[index] = Time.unscaledTime + (contact ? .065f : .10f);
            if (rank > 0 || cue == AncientEquipmentCue.Shatter || cue == AncientEquipmentCue.MetalFall)
                duckUntil = Mathf.Max(duckUntil, Time.unscaledTime + profile.duckSeconds);
            StartVoice(equipment[index], rank, rank > 0 ? profile.warningVolume : profile.effectsVolume,
                contact ? VaryPitch() : 1, Mathf.Clamp(pan, -1, 1));
        }

        public void PlayPickup()
        {
            if (Blocked || !clips[2] || Time.unscaledTime < nextVoice[2]) return;
            float now = Time.unscaledTime;
            pickupPitch = now - lastPickup <= profile.pickupChainTimeout ? Mathf.Min(1.55f, pickupPitch + .075f) : 1f;
            lastPickup = now;
            nextVoice[2] = now + profile.pickupInterval;
            StartVoice(clips[2], 0, profile.effectsVolume, pickupPitch);
        }

        public void PlayLaser(float power)
        {
            if (Blocked) return;
            StartVoice(clips[ParticleLaserCue], 0, profile.effectsVolume * Mathf.Lerp(.82f, 1.2f, power),
                Mathf.Lerp(1.26f, .76f, power));
            if (power > .7f) PlayEquipment(AncientEquipmentCue.Shatter);
        }

        public void Play(int cue)
        {
            cue=Mathf.Clamp(cue,0,PublicCueCount-1);
            if (Blocked || Time.unscaledTime < nextVoice[cue]) return;
            bool road = game && game.Production && game.Production.Sequence.Phase == AncientProductionPhase.Chase;
            int sound = road && cue == 0 ? RoadDebrisCue : road && cue == 8 ? RoadBombCue :
                game && game.Production && game.Production.DeckCombat && cue == 8 ? CaptainTellCue : cue;
            if (!clips[sound]) return;
            bool warning = cue == 8 || sound == RoadDebrisCue;
            nextVoice[cue] = Time.unscaledTime + (warning ? .12f : cue == 2 ? .05f : .03f);
            if (warning) duckUntil = Mathf.Max(duckUntil, Time.unscaledTime + (sound == CaptainTellCue ? .6f : .4f));
            StartVoice(clips[sound], warning ? 1 : 0, warning ? profile.warningVolume :
                profile.effectsVolume * (cue == 7 ? .5f : 1f), cue >= 9 || cue == 7 ? VaryPitch() : 1);
        }
        void OnDestroy()
        {
            score?.Dispose();
            foreach (var clip in clips) if (clip) Destroy(clip);
        }
    }
}
