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
        Texture2D bodyProfile;
        Material bodyMaterial;
        bool endpointsVisible;
        public AncientBeamVisuals(LineRenderer line,bool cannon=false,bool createEndpoints=true)
        {
            const int height=64;var colors=new Color32[height*2];
            for(int y=0;y<height;y++)
            {
                float radial=Mathf.Abs(y/(height-1f)*2-1);
                float light = cannon ? .72f * Mathf.Pow(1 - radial, 1.6f) + .28f * (1 - radial) : Mathf.Pow(1 - radial, 2);
                byte alpha=(byte)Mathf.RoundToInt(light*255);
                colors[y*2]=colors[y*2+1]=new Color32(255,255,255,alpha);
            }
            profile=new Texture2D(2,height,TextureFormat.RGBA32,false);
            profile.name="Continuous laser soft light";profile.filterMode=FilterMode.Bilinear;profile.wrapMode=TextureWrapMode.Clamp;
            profile.SetPixels32(colors);profile.Apply(false,true);
            Shader shader=cannon?Resources.Load<Shader>("Shaders/ParticleGlow"):null;
            if(!shader||!shader.isSupported)shader=Shader.Find("Sprites/Default");
            material=new Material(shader);material.name="Continuous laser light profile";material.mainTexture=profile;
            line.sharedMaterial=material;line.textureMode=LineTextureMode.Stretch;
            if(!cannon||!createEndpoints)return;
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
        public void StyleBody(LineRenderer body, LineRenderer core)
        {
            // 충돌 폭에 맞춘 청색 몸통 위에 좁은 발광 중심을 겹친다.
            // 양 끝은 둥근 발광 스프라이트로 마감하며 충돌 거리는 바꾸지 않는다.
            if (!bodyMaterial)
            {
                const int width = 16;
                const int height = 64;
                var pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float radial = Mathf.Abs(y / (height - 1f) * 2 - 1);
                    float edge = Mathf.SmoothStep(0, 1, Mathf.Clamp01((1 - radial) * 8f));
                    float length = x / (width - 1f);
                    float face = .85f + .15f * Mathf.SmoothStep(0, 1, Mathf.Min(length, 1 - length) * 8f);
                    byte green = (byte)Mathf.RoundToInt(Mathf.Lerp(168, 255, 1 - radial));
                    byte red = (byte)Mathf.RoundToInt(Mathf.Lerp(45, 255, Mathf.Pow(1 - radial, 1.7f)));
                    pixels[y * width + x] = new Color32(red, green, 255, (byte)Mathf.RoundToInt(255 * edge * face));
                }
                bodyProfile = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = "Blue laser body and soft edge",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                bodyProfile.SetPixels32(pixels);
                bodyProfile.Apply(false, true);
                bodyMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = bodyProfile };
            }
            body.sharedMaterial = bodyMaterial;
            body.textureMode = LineTextureMode.Stretch;
            core.sharedMaterial = material;
            core.textureMode = LineTextureMode.Stretch;
        }
        public void UpdateBody(LineRenderer body, LineRenderer glow, LineRenderer core, float opacity, float time)
        {
            float shimmer = .94f + .06f * Mathf.Sin(time * 62f);
            body.startColor = body.endColor = new Color(.68f, .91f, 1, opacity);
            glow.startColor = glow.endColor = new Color(.12f, .62f, 1, .76f * opacity * shimmer);
            core.startColor = core.endColor = new Color(.85f, .98f, 1, opacity * shimmer);
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
            muzzle.transform.localScale=Vector3.one*bodyWidth*2.05f;contact.transform.localScale=Vector3.one*bodyWidth*2.4f;
            muzzle.color=new Color(.42f,.84f,1,.72f*opacity);contact.color=new Color(.6f,.91f,1,.82f*opacity);
        }
        public void HideEndpoints(){if(muzzle){endpointsVisible=false;muzzle.enabled=contact.enabled=false;}}
        public void Dispose()
        {
            if(muzzle)Object.Destroy(muzzle.gameObject);if(contact)Object.Destroy(contact.gameObject);
            if(terminalSprite)Object.Destroy(terminalSprite);if(terminalMaterial)Object.Destroy(terminalMaterial);if(terminalTexture)Object.Destroy(terminalTexture);
            if(material)Object.Destroy(material);if(profile)Object.Destroy(profile);
            if(bodyMaterial)Object.Destroy(bodyMaterial);if(bodyProfile)Object.Destroy(bodyProfile);
        }
    }
}
