using UnityEngine;

namespace Plugin.AncientCivilization
{
    [RequireComponent(typeof(Camera))]
    public sealed class AncientCameraFX : MonoBehaviour
    {
        Material grade;
        void Start()
        {
            var shader=Resources.Load<Shader>("Shaders/Presentation");
            if(shader&&shader.isSupported)grade=new Material(shader);
        }
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(!grade){Graphics.Blit(source,destination);return;}
            grade.SetFloat("_BloomStrength",AncientVisuals.HighQuality ? 0.32f : 0f);
            Graphics.Blit(source,destination,grade);
        }
        void OnDestroy(){if(grade)Destroy(grade);}
    }
}
