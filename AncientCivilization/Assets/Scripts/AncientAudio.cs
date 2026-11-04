using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Short original synthesized cues, with integrated phase and voice rate limiting.
    public sealed class AncientAudio : MonoBehaviour
    {
        AudioSource source,ambient;AudioClip ambience;bool muted;
        readonly Dictionary<int,AudioClip> clips=new Dictionary<int,AudioClip>();
        readonly float[] nextVoice=new float[9];
        public bool Muted {get{return muted;}}
        void Awake()
        {
            source=gameObject.AddComponent<AudioSource>();source.spatialBlend=0;
            ambient=gameObject.AddComponent<AudioSource>();ambient.spatialBlend=0;ambient.loop=true;
            muted=AncientGame.Saved("muted")!=0;ApplyVolume();
        }
        void Start()
        {
            const int rate=22050;var data=new float[rate*8];
            float[] notes={110,130.8128f,164.8138f,146.8324f};
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)rate;float edge=Mathf.Min(1,Mathf.Min(t,8-t)/.06f);
                float hum=Mathf.Sin(t*55*Mathf.PI*2)*.22f+Mathf.Sin(t*82.5f*Mathf.PI*2)*.12f;
                float pulse=Mathf.Pow(Mathf.Sin(t*Mathf.PI),8)*.18f;
                float motif=Mathf.Sin(t*notes[(int)(t/2)%notes.Length]*Mathf.PI*2)*pulse;
                data[i]=(hum+motif)*edge;
            }
            ambience=AudioClip.Create("Buried city ambience",data.Length,1,rate,false);ambience.SetData(data,0);
            ambient.clip=ambience;ambient.Play();
            // Prewarm once while the menu is loading, avoiding synthesis on the first combat hit.
            for(int cue=0;cue<nextVoice.Length;cue++)Tone(cue);
        }
        void ApplyVolume(){source.volume=muted?0:.22f;ambient.volume=muted?0:.055f;}
        void Update()
        {
            if(!Input.GetKeyDown(KeyCode.F7))return;
            muted=!muted;ApplyVolume();AncientGame.SetSaved("muted",muted?1:0);PlayerPrefs.Save();
            if(AncientGame.Instance)AncientGame.Instance.Notice(muted?"AUDIO / MUTED":"AUDIO / ON");
        }
        AudioClip Tone(int cue)
        {
            AudioClip clip;if(clips.TryGetValue(cue,out clip))return clip;
            const int rate=22050;
            float duration=cue==4?.55f:cue==5?.65f:cue==7?.09f:cue==8?.45f:.18f;
            var samples=new float[Mathf.CeilToInt(rate*duration)];var random=new System.Random(17287+cue);
            float phase=0,filtered=0;
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/rate,life=t/duration;
                float frequency=cue==0?Mathf.Lerp(600,160,life):cue==1?Mathf.Lerp(140,55,life):cue==2?Mathf.Lerp(380,1050,life):cue==3?Mathf.Lerp(300,720,life):cue==4?Mathf.Lerp(95,30,life):cue==6?Mathf.Lerp(220,70,life):cue==7?70:cue==8?((int)(t*8)%2==0?330:440):650;
                phase+=frequency*Mathf.PI*2/rate;
                float noise=(float)random.NextDouble()*2-1;filtered=Mathf.Lerp(filtered,noise,cue==4?.15f:.4f);
                float tone=Mathf.Sin(phase);
                if(cue==0)tone=tone*.15f+noise*.65f;
                if(cue==1||cue==4||cue==6||cue==7)tone=tone*.45f+filtered*.65f;
                if(cue==5)
                {
                    float note=t<.18f?523.25f:t<.36f?659.25f:783.99f;
                    tone=(Mathf.Sin(t*note*Mathf.PI*2)+Mathf.Sin(t*note*2*Mathf.PI*2)*.3f)*.65f;
                }
                float attack=Mathf.Clamp01(t/.006f);
                samples[i]=Mathf.Clamp(tone*attack*Mathf.Pow(1-life,1.7f)*.55f,-.8f,.8f);
            }
            clip=AudioClip.Create("Ancient cue "+cue,samples.Length,1,rate,false);clip.SetData(samples,0);clips[cue]=clip;return clip;
        }
        public void Play(int cue)
        {
            cue=Mathf.Clamp(cue,0,nextVoice.Length-1);if(!source||Time.unscaledTime<nextVoice[cue])return;
            nextVoice[cue]=Time.unscaledTime+(cue==2?.05f:.03f);
            source.PlayOneShot(Tone(cue),cue==7?.5f:1);
        }
        void OnDestroy(){foreach(var clip in clips.Values)if(clip)Destroy(clip);if(ambience)Destroy(ambience);}
    }
}
