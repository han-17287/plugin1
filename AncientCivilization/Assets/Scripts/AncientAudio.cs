using UnityEngine;
using UnityEngine.Audio;

namespace Plugin.AncientCivilization
{
    // Original synthesized cues and two prebuilt industrial layers. Mixing never
    // allocates samples or clips during play; warnings have their own priority bus.
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
        AudioSource ambient, rhythm, phaseTwo, phaseThree, ui;
        readonly AudioLowPassFilter[] scoreFilters = new AudioLowPassFilter[4];
        float musicCutoff = 22000f;
        AudioClip ambience, drivePulse;
        bool muted, voicesPaused, lastMenu, focused=true;
        float duckUntil, nextHeart, lastPickup = -1f, pickupPitch = 1;
        int effectCursor;
        uint variation = 8191;
        public bool Muted => muted;

        void Awake()
        {
            game = GetComponent<AncientGame>();
            profile = game.Feedback;
            for (int i = 0; i < effects.Length; i++) effects[i] = CreateSource(gameObject, 96, false, profile.effects);
            for (int i = 0; i < warnings.Length; i++) warnings[i] = CreateSource(gameObject, 32, false, profile.effects);
            ui = CreateSource(gameObject, 48, false, profile.ui);
            ambient = CreateScoreTrack("공장 환기", 0, 192);
            rhythm = CreateScoreTrack("기계 박자", 1, 160);
            phaseTwo = CreateScoreTrack("기록실 저음", 2, 160);
            phaseThree = CreateScoreTrack("기록실 유리", 3, 160);
            ambient.volume = rhythm.volume = phaseTwo.volume = phaseThree.volume = 0;
            focused = Application.isFocused;
            muted = AncientGame.Saved("muted") != 0;
            ApplyMute();
        }

        AudioSource CreateScoreTrack(string label, int index, int priority)
        {
            var track = new GameObject(label);
            track.transform.SetParent(transform, false);
            var voice = CreateSource(track, priority, true, profile.music);
            var filter = track.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = musicCutoff;
            filter.enabled = false;
            scoreFilters[index] = filter;
            return voice;
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
            ambience=CreateIndustrialLayer(false);drivePulse=CreateIndustrialLayer(true);
            ambient.clip = ambience;
            rhythm.clip = drivePulse;
            phaseTwo.clip = Resources.Load<AudioClip>("Audio/ArchiveBass");
            phaseThree.clip = Resources.Load<AudioClip>("Audio/ArchiveGlass");
            for (int i = 0; i < equipment.Length; i++)
                equipment[i] = Resources.Load<AudioClip>("Audio/" + ((AncientEquipmentCue)i).ToString());
            // 같은 DSP 시각에 시작해 페이즈가 바뀌어도 네 루프의 박자가 포개진다.
            double downbeat = AudioSettings.dspTime + .10;
            ambient.PlayScheduled(downbeat);
            rhythm.PlayScheduled(downbeat);
            if (phaseTwo.clip) phaseTwo.PlayScheduled(downbeat);
            if (phaseThree.clip) phaseThree.PlayScheduled(downbeat);
        }
        void ApplyMute()
        {
            bool silence=muted||!focused;
            foreach (var voice in effects) voice.mute = silence;
            foreach (var voice in warnings) voice.mute = silence;
            ui.mute = ambient.mute = rhythm.mute = phaseTwo.mute = phaseThree.mute = silence;
        }
        void OnApplicationFocus(bool value)
        {focused=value;ApplyMute();if(!focused)PauseVoices(true);}
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
            float bed=.038f,pulse=0;
            int enforcerPhase = !menu && game && game.Enforcer && !game.Enforcer.InputLocked ? game.Enforcer.Phase : 0;
            if(menu)bed=.019f;
            else if(game&&game.Production&&game.Production.Active)
            {
                switch(game.Production.Sequence.Phase)
                {
                    case AncientProductionPhase.Chase:bed=.032f;pulse=.065f;break;
                    case AncientProductionPhase.Approach:bed=.031f;pulse=.018f;break;
                    case AncientProductionPhase.Boarding:bed=.028f;pulse=.045f;break;
                    case AncientProductionPhase.Deck:bed=.027f;pulse=.052f;break;
                    case AncientProductionPhase.Breach:bed=.023f;pulse=.025f;break;
                    case AncientProductionPhase.Knockback:bed=.026f;pulse=.048f;break;
                    case AncientProductionPhase.Finisher:
                        bed=.029f;pulse=game.Production.Sequence.Elapsed<7.65f?.058f:.008f;break;
                }
            }
            else if (enforcerPhase > 0)
            {
                bed = .032f;
                pulse = game.Enforcer.Recovering ? .021f : .061f;
            }
            else if(game&&game.Story&&game.Story.Stage<5&&game.hero)
            {
                // The production road changes from distant machinery to an active
                // conveyor rhythm as the officer reaches the stock floor.
                float depth=Mathf.InverseLerp(AncientProductionLayout.TutorialLeft,AncientProductionLayout.StockX,game.hero.transform.position.x);
                bed=Mathf.Lerp(.026f,.043f,depth);pulse=Mathf.Lerp(.004f,.013f,depth);
            }
            if(Speech){bed*=.45f;pulse*=.16f;}
            if(Panel){bed*=.25f;pulse=0;}
            float duck = Time.unscaledTime < duckUntil ? profile.duckMusicTo : 1f;
            bed *= duck;
            pulse *= duck;
            if(!focused||AncientBootIntro.Active){bed=0;pulse=0;}
            float fade=1-Mathf.Exp(-4*Mathf.Min(Time.unscaledDeltaTime,.1f));
            ambient.volume=Mathf.Lerp(ambient.volume,bed,fade);rhythm.volume=Mathf.Lerp(rhythm.volume,pulse,fade);
            float layers = !focused || Speech || Panel || AncientBootIntro.Active ? 0 : duck;
            phaseTwo.volume = Mathf.Lerp(phaseTwo.volume, enforcerPhase >= 2 ? profile.phaseTwoVolume * layers : 0, fade);
            phaseThree.volume = Mathf.Lerp(phaseThree.volume, enforcerPhase >= 3 ? profile.phaseThreeVolume * layers : 0, fade);
            bool low = game && game.LowHealth && !game.Paused;
            float targetCutoff = low ? 1800 : 22000;
            if (musicCutoff != targetCutoff)
            {
                musicCutoff = Mathf.Lerp(musicCutoff, targetCutoff, fade);
                if (Mathf.Abs(musicCutoff - targetCutoff) < 10) musicCutoff = targetCutoff;
                foreach (var filter in scoreFilters)
                {
                    filter.enabled = musicCutoff < 22000;
                    filter.cutoffFrequency = musicCutoff;
                }
            }
            if (low && Time.unscaledTime >= nextHeart)
            {
                PlayEquipment(AncientEquipmentCue.Heartbeat);
                nextHeart = Time.unscaledTime + profile.heartbeatInterval;
            }
        }
        static AudioClip CreateIndustrialLayer(bool driving)
        {
            const int seconds=16;var data=new float[SampleRate*seconds];var random=new System.Random(driving?1728702:1728701);
            float phase=0,air=0,metal=0;
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)SampleRate,edge=Mathf.Clamp01(Mathf.Min(t,seconds-t)/.12f);
                float noise=(float)random.NextDouble()*2-1;air=Mathf.Lerp(air,noise,.018f);metal=Mathf.Lerp(metal,noise,.28f);
                float sample;
                if(driving)
                {
                    // A restrained four-bar mechanical pulse, with a quieter fourth
                    // bar. It recedes completely in the safe truck boarding window.
                    float beat=Mathf.Repeat(t,.5f);int step=(int)(t*2);float bar=step/8==3?.55f:1;
                    float kick=Mathf.Sin(beat*(72-24*Mathf.Clamp01(beat/.12f))*Mathf.PI*2)*Mathf.Exp(-beat*32)*.42f;
                    float tick=metal*Mathf.Exp(-Mathf.Repeat(t,.25f)*85)*.22f;
                    float hit=(step%4==2?metal*Mathf.Exp(-beat*40)*.28f:0);
                    sample=(kick+tick+hit)*bar;
                }
                else
                {
                    // Airflow, resonant pipes and occasional distant metal taps
                    // leave breathing gaps instead of a continuously loud drone.
                    phase+=(54+.7f*Mathf.Sin(t*.41f))*Mathf.PI*2/SampleRate;
                    float breath=.35f+.3f*Mathf.Sin(t*.63f)+.18f*Mathf.Sin(t*1.13f+.5f);
                    float pipe=(Mathf.Sin(phase)*.1f+Mathf.Sin(phase*1.5f)*.045f)*breath;
                    float knock=Mathf.Repeat(t+1.3f,3.7f);
                    float distant=Mathf.Sin(knock*370*Mathf.PI*2)*Mathf.Exp(-knock*25)*.15f;
                    sample=air*.7f+pipe+distant;
                }
                data[i]=Mathf.Clamp(sample*edge,-.8f,.8f);
            }
            var clip=AudioClip.Create(driving?"Original production mechanical pulse":"Original production airflow and pipes",data.Length,1,SampleRate,false);clip.SetData(data,0);return clip;
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
        {foreach(var clip in clips)if(clip)Destroy(clip);if(ambience)Destroy(ambience);if(drivePulse)Destroy(drivePulse);}
    }
}
