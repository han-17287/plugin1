using UnityEngine;
namespace Plugin.AncientCivilization
{
    // Batched silhouettes distinguish workplaces, wards, archives, gardens and sealed control rooms.
    public static class AncientFieldScenery
    {
        static void B(AncientGame game,float x,float y,float w,float h,Color c,int order=-5)
        {game.StaticBox("District field furnishing",new Vector2(x,y),new Vector2(w,h),c,order);}
        public static void Build(AncientGame game,int floor,float left,int variant,float span=144)
        {
            Color dark=new Color(.04f,.08f,.11f),steel=new Color(.2f,.3f,.34f),white=new Color(.7f,.77f,.76f);
            for(int i=0;i<Mathf.Max(1,Mathf.FloorToInt((span-24)/17)+1);i++)
            {
                float x=left+12+i*17;
                if(floor==0)
                    ProductionFurnishing(game,x,variant,i);
                else if(floor==1)
                {
                    B(game,x,3.8f,5.8f,2.8f,new Color(.3f,.4f,.39f));B(game,x,3.8f,5.3f,2.3f,dark);
                    B(game,x-1.7f,3.7f,1.2f,.6f,white);B(game,x+.8f,3.5f,2.9f,.45f,white*.8f);
                    B(game,x,2.8f,5.7f,.15f,steel);B(game,x-2.4f,4.8f,.12f,2.5f,steel);
                    B(game,x-2.4f,5.5f,.6f,.8f,new Color(.55f,.7f,.7f));
                    B(game,x+2,6.3f,.65f,.2f,new Color(.7f,.25f,.23f));B(game,x+2,6.3f,.2f,.65f,new Color(.7f,.25f,.23f));
                }
                else if(floor==2)
                {
                    B(game,x,4.5f,6,6.3f,steel);B(game,x,4.5f,5.5f,5.9f,dark);
                    for(int row=0;row<4;row++)for(int col=0;col<3;col++)
                    {
                        float y=2.25f+row*1.4f,bx=x-1.8f+col*1.8f;
                        B(game,bx,y,1.4f,.95f,new Color(.42f,.5f,.51f));B(game,bx,y+.1f,.7f,.1f,white);B(game,bx,y-.22f,.45f,.08f,dark);
                    }
                    B(game,x+2.2f,7.9f,.55f,.08f,(i+variant)%2==0?AncientGame.Cyan:AncientGame.Orange);
                }
                else if(floor==3)
                {
                    B(game,x,3.8f,6,4.7f,new Color(.45f,.46f,.39f));B(game,x,3.8f,5.4f,4.1f,dark);
                    B(game,x,2.2f,5.3f,.6f,new Color(.35f,.32f,.23f));
                    for(int branch=0;branch<3;branch++)
                    {
                        float bx=x-1.7f+branch*1.7f;B(game,bx,3.1f,.16f,1.8f,new Color(.27f,.35f,.24f));
                        B(game,bx,4.1f,1.4f,1.3f,new Color(.22f,.4f,.3f));B(game,bx-.3f,4.6f,.8f,.8f,new Color(.33f,.48f,.34f));
                    }
                    B(game,x,7,5,.18f,white*.7f);
                }
                else
                {
                    B(game,x,4.7f,5,7.6f,new Color(.11f,.13f,.2f));B(game,x,4.7f,4.4f,7,dark);
                    for(int row=0;row<6;row++)
                    {
                        B(game,x,1.9f+row*1.1f,3.8f,.7f,new Color(.18f,.23f,.29f));
                        B(game,x-1.2f,1.9f+row*1.1f,.2f,.13f,(row+i+variant)%3==0?AncientGame.Orange:AncientGame.Cyan);
                    }
                    B(game,x+2.8f,4.8f,.08f,7.5f,steel);
                }
            }
        }
        static readonly Color FactorySteel=new Color(.15f,.23f,.26f),FactoryTeal=new Color(.12f,.33f,.35f),FactoryTint=new Color(.65f,.72f,.75f,.9f);
        static void ProductionFurnishing(AncientGame game,float x,int variant,int index)
        {
            // Bottom-pivoted, original factory art stays behind actors and objectives.
            // The atlas cells fit within 6.5 by 5.6 metres at their authored 64 PPU.
            int kind=((variant%4)+4)%4;float side=index%2==0?1:-1;
            game.WorldBatch.Sprite(AncientArt.ProductionProp(kind),new Vector2(x,1.25f),Vector2.one,FactoryTint,-5);
            B(game,x,8.2f,8.5f,.24f,FactorySteel);
            B(game,x+side*4.05f,5.15f,.16f,5.7f,FactorySteel);
            B(game,x+side*3.55f,8.05f,1.15f,.12f,FactoryTeal);
        }
    }
}
