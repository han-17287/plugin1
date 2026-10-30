using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Rendering-only decoration: no colliders, damage or gameplay random-number consumption.
    public sealed class AncientVisuals : MonoBehaviour
    {
        public static bool HighQuality=true;
        public static AncientVisuals Instance;
        Material glowMaterial,rimMaterial;Texture2D softTexture;Sprite softSprite;
        readonly List<SpriteRenderer> lights=new List<SpriteRenderer>();
        readonly List<Color> colours=new List<Color>();
        LineRenderer[] rain;Vector2[] rainPositions;float[] rainSpeeds;
        SpriteRenderer[] haze;SpriteRenderer armGlow;
        AncientGame game;System.Random random=new System.Random(47287);
        float Next(float min,float max){return min+(float)random.NextDouble()*(max-min);}
        void Awake()
        {
            Instance=this;HighQuality=AncientGame.Saved("graphics",1)!=0;
            var glow=Resources.Load<Shader>("Shaders/ParticleGlow");if(glow&&glow.isSupported)glowMaterial=new Material(glow);
            var rim=Resources.Load<Shader>("Shaders/CharacterRim");if(rim&&rim.isSupported)rimMaterial=new Material(rim);
            softTexture=new Texture2D(64,64,TextureFormat.RGBA32,false);softTexture.filterMode=FilterMode.Bilinear;softTexture.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[64*64];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                float r=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;
                pixels[y*64+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),2.8f));
            }
            softTexture.SetPixels(pixels);softTexture.Apply();softSprite=Sprite.Create(softTexture,new Rect(0,0,64,64),Vector2.one*.5f,64);
        }
        void Start()
        {
            game=AncientGame.Instance;
            foreach(var anchor in game.anchors)Glow(anchor,AncientGame.Cyan,2.3f,4);
            Glow(new Vector2(46,1.65f),AncientGame.Cyan,3,4);Glow(new Vector2(86,.65f),AncientGame.Cyan,3,4);
            Glow(new Vector2(115,2.5f),AncientGame.Cyan,5,-1);
            armGlow=Glow(new Vector2(.4f,.1f),AncientGame.Cyan,1.2f,11,game.hero.transform);
            for(int i=0;i<20;i++)
            {
                float x=Next(0,118);float top=x>43&&x<58?1:0;
                // Lamps only on solid roof sections, not across gaps.
                if((x>20&&x<22)||(x>38&&x<43)||(x>58&&x<63)||(x>80&&x<85))continue;
                Glow(new Vector2(x,top-.45f),AncientGame.Orange,2.6f,3);
            }
            rain=new LineRenderer[72];rainPositions=new Vector2[rain.Length];rainSpeeds=new float[rain.Length];
            for(int i=0;i<rain.Length;i++)
            {
                rain[i]=game.Line("Distant rain",new Color(.4f,.65f,.8f,Next(.08f,.21f)),Next(.008f,.018f),-6);
                rainPositions[i]=new Vector2(Next(-26,26),Next(-15,15));rainSpeeds[i]=Next(8,16);
            }
            haze=new SpriteRenderer[3];
            for(int i=0;i<haze.Length;i++)
            {
                haze[i]=AncientArt.Display("Low drifting haze",softSprite,Vector2.zero,-12);
                haze[i].color=new Color(.28f,.48f,.6f,.085f);haze[i].transform.localScale=new Vector3(27,4,1);
            }
            for(int i=0;i<6;i++)
            {
                float x=8+i*20;var cable=game.Line("Foreground hanging cable",new Color(.018f,.03f,.05f,.8f),.065f,23);
                cable.positionCount=20;
                for(int n=0;n<20;n++){float t=n/19f;cable.SetPosition(n,new Vector2(x+t*11,14-Mathf.Sin(t*Mathf.PI)*2));}
            }
        }
        public void Rim(SpriteRenderer renderer){if(rimMaterial)renderer.sharedMaterial=rimMaterial;}
        public SpriteRenderer Glow(Vector2 p,Color colour,float size,int order,Transform parent=null)
        {
            var glow=AncientArt.Display("Local particle light",softSprite,p,order,parent);
            glow.transform.localScale=Vector3.one*size;glow.color=new Color(colour.r,colour.g,colour.b,.48f);
            if(glowMaterial)glow.sharedMaterial=glowMaterial;
            lights.Add(glow);colours.Add(glow.color);return glow;
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.F6))
            {HighQuality=!HighQuality;AncientGame.SetSaved("graphics",HighQuality?1:0);PlayerPrefs.Save();game.Notice(HighQuality?"VISUALS / Enhanced":"VISUALS / Light");}
        }
        void LateUpdate()
        {
            if(!game||!game.hero||!Camera.main)return;
            Vector2 camera=Camera.main.transform.position;
            if(armGlow)armGlow.transform.localPosition=new Vector2(game.hero.Facing*.4f,.15f);
            for(int i=0;i<lights.Count;i++)if(lights[i])
            {
                lights[i].enabled=HighQuality;
                Color c=colours[i];c.a*=.93f+Mathf.Sin(Time.time*3+i*1.7f)*.07f;lights[i].color=c;
            }
            for(int i=0;i<rain.Length;i++)
            {
                rain[i].enabled=HighQuality;
                if(!HighQuality)continue;
                rainPositions[i]+=new Vector2(-1,-rainSpeeds[i])*Time.deltaTime;
                if(rainPositions[i].y<-15){rainPositions[i].y=15;rainPositions[i].x=Next(-26,26);}
                Vector2 p=camera+rainPositions[i];rain[i].SetPosition(0,p);rain[i].SetPosition(1,p+new Vector2(.07f,.4f));
            }
            for(int i=0;i<haze.Length;i++)
            {
                haze[i].enabled=HighQuality;
                haze[i].transform.position=new Vector2(camera.x-13+i*13+Mathf.Sin(Time.time*.13f+i)*4,camera.y-5+i*1.6f);
            }
        }
        void OnDestroy()
        {
            if(rain!=null)foreach(var line in rain)if(line)Destroy(line.gameObject);
            foreach(var light in lights)if(light)Destroy(light.gameObject);
            if(haze!=null)foreach(var fog in haze)if(fog)Destroy(fog.gameObject);
            if(glowMaterial)Destroy(glowMaterial);if(rimMaterial)Destroy(rimMaterial);
            if(softSprite)Destroy(softSprite);if(softTexture)Destroy(softTexture);
        }
    }
}
