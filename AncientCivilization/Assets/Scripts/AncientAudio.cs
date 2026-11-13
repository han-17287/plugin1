using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Original synthesized cues and two prebuilt industrial layers. Mixing never
    // allocates samples or clips during play; warnings have their own priority bus.
    public sealed class AncientAudio : MonoBehaviour
    {
        const int PublicCueCount=12,RoadDebrisCue=12,RoadBombCue=13,CaptainTellCue=14,ParticleLaserCue=15,SampleRate=22050;
        readonly AudioClip[] clips=new AudioClip[16];
        readonly float[] nextVoice=new float[PublicCueCount];
        AncientGame game;AudioSource source,warnings,ambient,rhythm;AudioClip ambience,drivePulse;
        bool muted,voicesPaused,lastMenu,focused=true;float duckUntil;
        public bool Muted {get{return muted;}}
        void Awake()
        {
            game=GetComponent<AncientGame>();
            source=CreateSource(96,false);warnings=CreateSource(32,false);
            ambient=CreateSource(192,true);rhythm=CreateSource(160,true);
            source.volume=.22f;warnings.volume=.27f;ambient.volume=rhythm.volume=0;
            focused=Application.isFocused;muted=AncientGame.Saved("muted")!=0;ApplyMute();
        }
        AudioSource CreateSource(int priority,bool loop)
        {
            var audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;
            audioSource.playOnAwake=false;audioSource.loop=loop;audioSource.priority=priority;
            audioSource.dopplerLevel=0;return audioSource;
        }
        void Start()
        {
            // Generation happens once behind the boot/menu presentation, never on
            // the first swing, particle pickup, or warning in the road encounter.
            for(int cue=0;cue<clips.Length;cue++)clips[cue]=CreateTone(cue);
            ambience=CreateIndustrialLayer(false);drivePulse=CreateIndustrialLayer(true);
            ambient.clip=ambience;rhythm.clip=drivePulse;ambient.Play();rhythm.Play();
        }
        void ApplyMute()
        {
            bool silence=muted||!focused;
            if(source)source.mute=silence;if(warnings)warnings.mute=silence;
            if(ambient)ambient.mute=silence;if(rhythm)rhythm.mute=silence;
        }
        void OnApplicationFocus(bool value)
        {focused=value;ApplyMute();if(!focused)PauseVoices(true);}
        void PauseVoices(bool pause)
        {
            if(voicesPaused==pause)return;voicesPaused=pause;
            if(pause){source.Pause();warnings.Pause();}
            else{source.UnPause();warnings.UnPause();}
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
            if(menu&&!lastMenu){source.Stop();warnings.Stop();duckUntil=0;}
            lastMenu=menu;PauseVoices(!focused||menu||Panel||Speech||AncientBootIntro.Active);
            float bed=.038f,pulse=0;
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
            else if(game&&game.Story&&game.Story.Stage<5&&game.hero)
            {
                // The production road changes from distant machinery to an active
                // conveyor rhythm as the officer reaches the stock floor.
                float depth=Mathf.InverseLerp(AncientProductionLayout.TutorialLeft,AncientProductionLayout.StockX,game.hero.transform.position.x);
                bed=Mathf.Lerp(.026f,.043f,depth);pulse=Mathf.Lerp(.004f,.013f,depth);
            }
            if(Speech){bed*=.45f;pulse*=.16f;}
            if(Panel){bed*=.25f;pulse=0;}
            if(Time.unscaledTime<duckUntil){bed*=.42f;pulse*=.32f;}
            if(!focused||AncientBootIntro.Active){bed=0;pulse=0;}
            float fade=1-Mathf.Exp(-4*Mathf.Min(Time.unscaledDeltaTime,.1f));
            ambient.volume=Mathf.Lerp(ambient.volume,bed,fade);rhythm.volume=Mathf.Lerp(rhythm.volume,pulse,fade);
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
        public void Play(int cue)
        {
            cue=Mathf.Clamp(cue,0,PublicCueCount-1);
            if(!source||muted||!focused||(game&&game.MainMenu)||Panel||Speech||AncientBootIntro.Active||Time.unscaledTime<nextVoice[cue])return;
            bool road=game&&game.Production&&game.Production.Sequence.Phase==AncientProductionPhase.Chase;
            int sound=road&&cue==0?RoadDebrisCue:road&&cue==8?RoadBombCue:game&&game.Production&&game.Production.DeckCombat&&cue==8?CaptainTellCue:cue==4&&game&&game.hero&&game.hero.Combat&&game.hero.Combat.Firing?ParticleLaserCue:cue;
            if(!clips[sound])return;
            bool warning=cue==8||sound==RoadDebrisCue;
            nextVoice[cue]=Time.unscaledTime+(warning?.12f:cue==2?.05f:.03f);
            if(warning){duckUntil=Time.unscaledTime+(sound==CaptainTellCue?.6f:.4f);warnings.PlayOneShot(clips[sound]);}
            else source.PlayOneShot(clips[sound],cue==7?.5f:1);
        }
        void OnDestroy()
        {foreach(var clip in clips)if(clip)Destroy(clip);if(ambience)Destroy(ambience);if(drivePulse)Destroy(drivePulse);}
    }
}
