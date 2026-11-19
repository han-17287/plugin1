using UnityEngine;

namespace Plugin.AncientCivilization
{
    [RequireComponent(typeof(Camera))]
    public sealed class AncientCameraFX : MonoBehaviour
    {
        Material grade;
        static readonly int BloomStrength=Shader.PropertyToID("_BloomStrength"),DamageAmount=Shader.PropertyToID("_DamageAmount"),LowHealth=Shader.PropertyToID("_LowHealth");
        void Start()
        {
            var shader=Resources.Load<Shader>("Shaders/Presentation");
            if(shader&&shader.isSupported)grade=new Material(shader);
        }
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            var game=AncientGame.Instance;
            if(!grade||(!AncientVisuals.HighQuality&&(!game||(game.DamageFlash<.001f&&game.HealthWarning<=0)))){Graphics.Blit(source,destination);return;}
            grade.SetFloat(BloomStrength,AncientVisuals.HighQuality ? 0.25f : 0f);
            grade.SetFloat(DamageAmount,game?game.DamageFlash:0);
            grade.SetFloat(LowHealth,game?game.HealthWarning:0);
            Graphics.Blit(source,destination,grade);
        }
        void OnDestroy(){if(grade)Destroy(grade);}
    }
}
