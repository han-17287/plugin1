using UnityEngine;
namespace Plugin.AncientCivilization
{
    // A continuous cross-beam light profile, generated once rather than emitted as particles.
    public sealed class AncientBeamVisuals : System.IDisposable
    {
        readonly Texture2D profile,terminalTexture;
        readonly Material material,terminalMaterial;
        readonly Sprite terminalSprite;
        readonly SpriteRenderer muzzle,contact;
        bool endpointsVisible;
        public AncientBeamVisuals(LineRenderer line,bool cannon=false)
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
            Shader shader=cannon?Resources.Load<Shader>("Shaders/ParticleGlow"):null;
            if(!shader||!shader.isSupported)shader=Shader.Find("Sprites/Default");
            material=new Material(shader);material.name="Continuous laser light profile";material.mainTexture=profile;
            line.sharedMaterial=material;line.textureMode=LineTextureMode.Stretch;
            if(!cannon)return;
            const int size=64;var glow=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=(x+.5f)/size*2-1,dy=(y+.5f)/size*2-1;
                byte alpha=(byte)Mathf.RoundToInt(Mathf.Pow(Mathf.Max(0,1-dx*dx-dy*dy),3)*255);
                glow[y*size+x]=new Color32(255,255,255,alpha);
            }
            terminalTexture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Cannon terminal light",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            terminalTexture.SetPixels32(glow);terminalTexture.Apply(false,true);
            terminalSprite=Sprite.Create(terminalTexture,new Rect(0,0,size,size),Vector2.one*.5f,size,0,SpriteMeshType.FullRect);
            terminalMaterial=new Material(shader){name="Cannon terminal additive light",mainTexture=terminalTexture};
            muzzle=Endpoint("Reusable laser muzzle light",line.sortingOrder+2);
            contact=Endpoint("Reusable laser contact light",line.sortingOrder+2);
        }
        SpriteRenderer Endpoint(string name,int order)
        {
            var point=new GameObject(name).AddComponent<SpriteRenderer>();point.sprite=terminalSprite;
            point.sharedMaterial=terminalMaterial;point.sortingOrder=order;point.enabled=false;return point;
        }
        public void ShowEndpoints(Vector2 start,Vector2 end,float bodyWidth,float opacity,bool visible)
        {
            if(!muzzle)return;
            if(visible!=endpointsVisible){endpointsVisible=visible;muzzle.enabled=contact.enabled=visible;}
            if(!visible)return;
            muzzle.transform.position=start;contact.transform.position=end;
            muzzle.transform.localScale=Vector3.one*bodyWidth*1.8f;contact.transform.localScale=Vector3.one*bodyWidth*2.2f;
            muzzle.color=new Color(.4f,.78f,1,.5f*opacity);contact.color=new Color(.55f,.86f,1,.65f*opacity);
        }
        public void HideEndpoints(){if(muzzle){endpointsVisible=false;muzzle.enabled=contact.enabled=false;}}
        public void Dispose()
        {
            if(muzzle)Object.Destroy(muzzle.gameObject);if(contact)Object.Destroy(contact.gameObject);
            if(terminalSprite)Object.Destroy(terminalSprite);if(terminalMaterial)Object.Destroy(terminalMaterial);if(terminalTexture)Object.Destroy(terminalTexture);
            if(material)Object.Destroy(material);if(profile)Object.Destroy(profile);
        }
    }
}
