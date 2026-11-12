using UnityEngine;

namespace Plugin.AncientCivilization
{
    [RequireComponent(typeof(Camera))]
    public sealed class AncientCameraFX : MonoBehaviour
    {
        Material grade;
        static readonly int BloomStrength=Shader.PropertyToID("_BloomStrength"),DamageAmount=Shader.PropertyToID("_DamageAmount");
        void Start()
        {
            var shader=Resources.Load<Shader>("Shaders/Presentation");
            if(shader&&shader.isSupported)grade=new Material(shader);
        }
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(!grade||(!AncientVisuals.HighQuality&&(!AncientGame.Instance||AncientGame.Instance.DamageFlash<.001f))){Graphics.Blit(source,destination);return;}
            grade.SetFloat(BloomStrength,AncientVisuals.HighQuality ? 0.25f : 0f);
            grade.SetFloat(DamageAmount,AncientGame.Instance?AncientGame.Instance.DamageFlash:0);
            Graphics.Blit(source,destination,grade);
        }
        void OnDestroy(){if(grade)Destroy(grade);}
    }
}
