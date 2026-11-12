using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Lower-workshop restoration changes the place, not only its objective icon.
    // All pieces are collision-free, prebuilt, and read the campaign's persisted mask.
    public sealed class AncientWorkshopCircuit
    {
        sealed class Bank
        {
            public float X,Left,Right;
            public SpriteRenderer shell,lamp,window,surge;
            public readonly SpriteRenderer[] leds=new SpriteRenderer[3];
            public LineRenderer bus,drop;
        }
        readonly AncientGame game;readonly Transform root;readonly Bank[] banks=new Bank[3];
        readonly SpriteRenderer receiverFrame,receiverGlass,receiverCore;readonly LineRenderer receiverFeed;
        readonly Vector2 receiverPoint;int lastMask;float restoreAge=-1;
        static readonly Color Steel=new Color(.17f,.25f,.28f),Unpowered=new Color(.24f,.19f,.11f),Powered=new Color(.32f,.86f,.94f);
        public bool PoweredOn {get{return (lastMask&4)!=0;}}
        public bool RestorationWave {get{return restoreAge>=0&&restoreAge<3.2f;}}
        public AncientWorkshopCircuit(AncientGame owner,Transform parent,float left,Vector2[] points,int initialMask)
        {
            game=owner;lastMask=initialMask&7;receiverPoint=points[2];
            root=new GameObject("Lower workshop service power circuit").transform;root.SetParent(parent,false);
            for(int sector=0;sector<banks.Length;sector++)
            {
                float start=left+AncientFieldExpansion.SectorStart(sector),x=points[sector].x-6;
                var bank=new Bank{X=x,Left=start+(sector==0?8:0),Right=start+AncientFieldExpansion.SectorSpan-(sector==banks.Length-1?8:0)};banks[sector]=bank;
                bank.shell=Part("Workshop service lamp casing",new Vector2(x,8.35f),new Vector2(5,.38f),Steel,-3);
                bank.lamp=Part("Workshop restored service light",new Vector2(x,8.30f),new Vector2(4.5f,.1f),Unpowered,-2);
                bank.window=Part("Workshop restored machine window",new Vector2(x,4.8f),new Vector2(6.3f,3.2f),new Color(.12f,.35f,.4f,.01f),-6);
                for(int i=0;i<bank.leds.Length;i++)
                    bank.leds[i]=Part("Workshop physical circuit status "+i,new Vector2(x-.7f+i*.7f,7.92f),new Vector2(.18f,.12f),Unpowered,-2);
                bank.bus=Cable("Workshop overhead supply bus",new Vector2(bank.Left,8.85f),new Vector2(bank.Right,8.85f));
                bank.drop=Cable("Workshop lamp branch",new Vector2(x,8.85f),new Vector2(x,8.48f));
                bank.surge=Part("Reused workshop return-power pulse",new Vector2(x,8.85f),new Vector2(1.5f,.11f),Powered,-2);
            }
            receiverFrame=Part("Restored receiver persistent housing",receiverPoint,new Vector2(1.1f,1.65f),Steel,6);
            receiverGlass=Part("Restored receiver powered glass",receiverPoint,new Vector2(.68f,1.15f),Powered,7);
            receiverCore=Part("Restored receiver white contact",receiverPoint,new Vector2(.13f,.87f),new Color(.8f,1,1),8);
            receiverFeed=Cable("Workshop receiver supply riser",receiverPoint+Vector2.up*.825f,new Vector2(receiverPoint.x,8.85f));
        }
        SpriteRenderer Part(string name,Vector2 point,Vector2 size,Color color,int order)
        {
            var part=game.Box(name,point,size,color,order);part.transform.SetParent(root,true);
            var view=part.GetComponent<SpriteRenderer>();view.enabled=false;return view;
        }
        LineRenderer Cable(string name,Vector2 start,Vector2 end)
        {
            var line=game.Line(name,Steel,.045f,-3);line.transform.SetParent(root,true);
            line.SetPosition(0,start);line.SetPosition(1,end);line.enabled=false;return line;
        }
        static void Show(Renderer view,bool shown){if(view.enabled!=shown)view.enabled=shown;}
        static bool Near(float left,float right,float bottom,float top,Vector2 camera,Vector2 half)
        {return right>=camera.x-half.x-4&&left<=camera.x+half.x+4&&top>=camera.y-half.y-2&&bottom<=camera.y+half.y+2;}
        public void Update(int taskMask)
        {
            int mask=taskMask&7;
            if(mask!=lastMask)
            {
                if((lastMask&4)==0&&(mask&4)!=0)restoreAge=0;
                else if((mask&4)==0)restoreAge=-1;
                lastMask=mask;
            }
            if(restoreAge>=0&&!game.Paused&&Application.isFocused)restoreAge=Mathf.Min(3.2f,restoreAge+Time.deltaTime);
            bool shown=game.HasSession&&!game.MainMenu&&game.Story&&game.Story.CurrentFloor==0&&game.View;
            Vector2 camera=game.View?(Vector2)game.View.transform.position:Vector2.zero;
            Vector2 half=game.View?new Vector2(game.View.orthographicSize*game.View.aspect,game.View.orthographicSize):Vector2.zero;
            bool powered=PoweredOn;float flash=RestorationWave?Mathf.Exp(-restoreAge*3):0;
            Color lamp=powered?Color.Lerp(Powered,new Color(.85f,1,1),flash):Unpowered;
            Color cable=powered?new Color(.14f,.51f,.58f):lastMask==0?Steel:new Color(.47f,.34f,.13f);
            for(int sector=0;sector<banks.Length;sector++)
            {
                var bank=banks[sector];bool near=shown&&Near(bank.X-3.2f,bank.X+3.2f,3.2f,8.6f,camera,half);
                Show(bank.shell,near);Show(bank.lamp,near);Show(bank.window,near);bank.lamp.color=lamp;
                bank.window.color=powered?new Color(.14f,.55f,.64f,.10f+flash*.04f):new Color(.12f,.35f,.4f,.01f);
                for(int i=0;i<bank.leds.Length;i++)
                {Show(bank.leds[i],near);bank.leds[i].color=(lastMask&(1<<i))!=0?(powered?Powered:AncientGame.Orange):Unpowered;}
                bool wire=shown&&Near(bank.Left,bank.Right,8.8f,8.9f,camera,half);Show(bank.bus,wire);Show(bank.drop,near);
                bank.bus.startColor=bank.bus.endColor=bank.drop.startColor=bank.drop.endColor=cable;
                float pulseX=receiverPoint.x+2-restoreAge*220;
                bool pulse=RestorationWave&&shown&&pulseX>=bank.Left&&pulseX<=bank.Right&&Near(pulseX-1,pulseX+1,8.7f,9,camera,half);
                Show(bank.surge,pulse);if(pulse)bank.surge.transform.position=new Vector2(pulseX,8.85f);
            }
            bool receiver=powered&&shown&&Near(receiverPoint.x-.6f,receiverPoint.x+.6f,receiverPoint.y-.9f,receiverPoint.y+.9f,camera,half);
            Show(receiverFrame,receiver);Show(receiverGlass,receiver);Show(receiverCore,receiver);receiverGlass.color=lamp;
            Show(receiverFeed,shown&&Near(receiverPoint.x,receiverPoint.x,receiverPoint.y+.825f,8.85f,camera,half));
            receiverFeed.startColor=receiverFeed.endColor=cable;
        }
    }
}
