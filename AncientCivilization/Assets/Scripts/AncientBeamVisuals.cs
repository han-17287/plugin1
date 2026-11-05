using UnityEngine;
namespace Plugin.AncientCivilization
{
    // A continuous cross-beam light profile, generated once rather than emitted as particles.
    public sealed class AncientBeamVisuals : System.IDisposable
    {
        readonly Texture2D profile;
        readonly Material material;
        public AncientBeamVisuals(LineRenderer line)
        {
            const int height=64;var colors=new Color32[height*2];
            for(int y=0;y<height;y++)
            {
                float radial=Mathf.Abs(y/(height-1f)*2-1);
                byte alpha=(byte)Mathf.RoundToInt(Mathf.Pow(1-radial,2)*255);
                colors[y*2]=colors[y*2+1]=new Color32(255,255,255,alpha);
            }
            profile=new Texture2D(2,height,TextureFormat.RGBA32,false);
            profile.name="Continuous laser soft light";profile.filterMode=FilterMode.Bilinear;profile.wrapMode=TextureWrapMode.Clamp;
            profile.SetPixels32(colors);profile.Apply(false,true);
            material=new Material(Shader.Find("Sprites/Default"));material.name="Continuous laser light profile";material.mainTexture=profile;
            line.sharedMaterial=material;line.textureMode=LineTextureMode.Stretch;
        }
        public void Dispose(){if(material)Object.Destroy(material);if(profile)Object.Destroy(profile);}
    }
}
