using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 전체 입자 풀이 픽셀 스프라이트 두 장과 발광 머티리얼 하나를 공유한다.
    public sealed class AncientMoteVisuals : System.IDisposable
    {
        public static readonly Color Blue=new Color(.12f,.55f,1);
        readonly Texture2D coreTexture,haloTexture;
        readonly Sprite coreSprite,haloSprite;
        readonly Material haloMaterial;
        bool disposed;

        public AncientMoteVisuals()
        {
            coreTexture=BuildTexture("Collectible blue particle core",16,false);
            haloTexture=BuildTexture("Collectible blue particle halo",32,true);
            coreSprite=Sprite.Create(coreTexture,new Rect(0,0,16,16),Vector2.one*.5f,16,0,SpriteMeshType.FullRect);
            haloSprite=Sprite.Create(haloTexture,new Rect(0,0,32,32),Vector2.one*.5f,32,0,SpriteMeshType.FullRect);
            coreSprite.name="Blue particle crystal";haloSprite.name="Blue particle pixel halo";
            Shader shader = Resources.Load<Shader>("Shaders/ParticleGlow");
            if (shader && shader.isSupported)
                haloMaterial = new Material(shader) { name = "Shared collectible particle light", mainTexture = haloTexture };
        }

        static Texture2D BuildTexture(string label,int size,bool halo)
        {
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.name=label;texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color32[size*size];float center=(size-1)*.5f;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float distance=Mathf.Abs(x-center)+Mathf.Abs(y-center);
                Color32 color=new Color32(0,0,0,0);
                if(halo)
                {
                    // Discrete diamond bands keep the glow consistent with the pixel character art.
                    if(distance<=5)color=new Color32(99,209,255,105);
                    else if(distance<=8)color=new Color32(55,171,255,70);
                    else if(distance<=11)color=new Color32(36,133,255,36);
                    else if(distance<=14)color=new Color32(17,91,255,14);
                    else if(distance<=16)color=new Color32(10,63,255,6);
                }
                else if(distance<=1.5f)color=new Color32(214,249,255,255);
                else if(distance<=4)color=new Color32(77,190,255,255);
                else if(distance<=7)color=new Color32(31,116,255,245);
                pixels[y*size+x]=color;
            }
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }

        public Mote Attach(SpriteRenderer core,int variant)
        {
            core.sprite=coreSprite;core.color=Color.white;core.transform.localScale=Vector3.one*.14f;
            var child=new GameObject("Pooled blue particle halo");child.transform.SetParent(core.transform,false);
            child.transform.localScale=Vector3.one*2;
            var halo=child.AddComponent<SpriteRenderer>();halo.sprite=haloSprite;
            if (haloMaterial) halo.sharedMaterial = haloMaterial;
            halo.sortingLayerID=core.sortingLayerID;halo.sortingOrder=core.sortingOrder-1;halo.color=Color.white;
            return new Mote(core,halo,variant);
        }

        public void Dispose()
        {
            if(disposed)return;disposed=true;
            if(coreSprite)Object.Destroy(coreSprite);if(haloSprite)Object.Destroy(haloSprite);
            if(coreTexture)Object.Destroy(coreTexture);if(haloTexture)Object.Destroy(haloTexture);
            if (haloMaterial) Object.Destroy(haloMaterial);
        }

        public sealed class Mote
        {
            readonly SpriteRenderer core,halo;
            readonly float phase;
            public Mote(SpriteRenderer coreRenderer,SpriteRenderer haloRenderer,int variant)
            {core=coreRenderer;halo=haloRenderer;phase=variant*1.618f;}
            public void Play()
            {
                core.color=halo.color=Color.white;core.transform.localScale=Vector3.one*.14f;
                core.transform.localRotation=Quaternion.Euler(0,0,phase*45);
                halo.transform.localScale=Vector3.one*2;
            }
            public void Tick(float remainingLife,float age,bool collecting)
            {
                float fade=Mathf.Clamp01(remainingLife/.75f);
                float pulse=.88f+Mathf.Sin(age*7+phase)*.12f;
                core.color=new Color(1,1,1,fade);
                halo.color=new Color(1,1,1,fade*(collecting?1:pulse));
                core.transform.localScale=Vector3.one*(collecting?.15f:.14f);
                halo.transform.localScale=Vector3.one*((collecting ? 2.65f : 2.15f) + pulse*.2f);
            }
        }
    }
}
