using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Small original synthesized cues; no downloaded audio or licensed music.
    public sealed class AncientAudio : MonoBehaviour
    {
        AudioSource source;
        readonly Dictionary<int,AudioClip> clips=new Dictionary<int,AudioClip>();
        void Awake(){source=gameObject.AddComponent<AudioSource>();source.spatialBlend=0;source.volume=.22f;}
        AudioClip Tone(int cue)
        {
            AudioClip clip;if(clips.TryGetValue(cue,out clip))return clip;
            const int rate=22050;float duration=cue==4?.5f:cue==5?.55f:.16f;
            var samples=new float[Mathf.CeilToInt(rate*duration)];var random=new System.Random(17287+cue);
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/rate,life=t/duration;
                float frequency=cue==0?Mathf.Lerp(650,140,life):cue==1?Mathf.Lerp(160,55,life):cue==2?Mathf.Lerp(420,1100,life):cue==3?Mathf.Lerp(260,600,life):cue==4?Mathf.Lerp(110,35,life):650;
                float tone=Mathf.Sin(t*frequency*Mathf.PI*2);
                if(cue==1||cue==4)tone=tone*.35f+((float)random.NextDouble()*2-1)*.65f;
                if(cue==5)tone=(Mathf.Sin(t*650*Mathf.PI*2)+Mathf.Sin(t*975*Mathf.PI*2))*.45f;
                samples[i]=tone*Mathf.Sin(Mathf.PI*Mathf.Min(1,life*12))*Mathf.Pow(1-life,2)*.55f;
            }
            clip=AudioClip.Create("Ancient cue "+cue,samples.Length,1,rate,false);clip.SetData(samples,0);clips[cue]=clip;return clip;
        }
        public void Play(int cue){if(source)source.PlayOneShot(Tone(cue));}
        void OnDestroy(){foreach(var clip in clips.Values)if(clip)Destroy(clip);}
    }
}
