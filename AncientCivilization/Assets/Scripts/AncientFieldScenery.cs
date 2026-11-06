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
        static readonly Color FactoryDark=new Color(.035f,.07f,.09f),FactorySteel=new Color(.15f,.23f,.26f),FactoryIvory=new Color(.48f,.56f,.55f),FactoryTeal=new Color(.12f,.33f,.35f),FactoryAmber=new Color(.58f,.4f,.2f);
        static void ProductionFurnishing(AncientGame game,float x,int variant,int index)
        {
            // These are wall-mounted background fixtures: no colliders, anchor sprites or
            // foreground warning colours. One shared sorting layer keeps the meshes batched.
            int kind=((variant%4)+4)%4;float side=index%2==0?1:-1;
            B(game,x,8.2f,8.5f,.24f,FactorySteel);
            if(kind==0)
            {
                // Separate weighing cells and numbered storage columns for the night load.
                B(game,x,4.65f,8.6f,6.25f,FactorySteel);B(game,x,4.65f,8.05f,5.7f,FactoryDark);
                B(game,x+side*3.1f,4.65f,.2f,5.6f,FactoryIvory);
                for(int row=0;row<3;row++)
                {
                    float y=2.65f+row*1.8f;
                    B(game,x,y-.62f,7.65f,.17f,FactorySteel);
                    for(int cell=0;cell<2;cell++)
                    {
                        float bx=x-1.7f+cell*3.2f;
                        B(game,bx,y,2.25f,1.05f,FactorySteel);
                        B(game,bx,y+.35f,1.9f,.18f,FactoryIvory);
                        B(game,bx+side*.65f,y,.16f,.62f,FactoryTeal);
                    }
                }
                B(game,x,1.45f,7.8f,.34f,FactorySteel);B(game,x+side*2.4f,1.45f,1.25f,.18f,FactoryDark);
            }
            else if(kind==1)
            {
                // Pressure vessels, a common supply pipe and a recessed distribution gauge.
                B(game,x,4.6f,8.4f,6.4f,FactoryDark);
                for(int vessel=0;vessel<2;vessel++)
                {
                    float bx=x-2.7f+vessel*5.4f;
                    B(game,bx,4.65f,1.9f,5.65f,FactorySteel);
                    B(game,bx,6.9f,1.55f,.55f,FactoryIvory);
                    B(game,bx+side*.48f,4.7f,.18f,3.6f,FactoryTeal);
                    B(game,bx,2.5f,2.2f,.28f,FactorySteel);
                }
                B(game,x,3.7f,6.6f,.42f,FactorySteel);B(game,x,5.1f,2.65f,2.3f,FactoryIvory);
                B(game,x,5.1f,2.15f,1.8f,FactoryDark);B(game,x,5.25f,1.55f,.16f,FactoryTeal);
                B(game,x+side*.3f,4.85f,.65f,.18f,FactoryAmber);
                B(game,x+side*1.45f,7.4f,.3f,1.35f,FactorySteel);
            }
            else if(kind==2)
            {
                // Work lockers and a preserved evacuation diagram remain after the closure.
                for(int locker=0;locker<3;locker++)
                {
                    float bx=x-2.65f+locker*2.65f;
                    B(game,bx,4.35f,2.3f,5.45f,FactorySteel);
                    B(game,bx,4.35f,1.98f,5.08f,FactoryDark);
                    B(game,bx,5.9f,1.8f,1.35f,FactoryIvory);
                    B(game,bx+side*.57f,3.8f,.1f,.58f,FactoryIvory);
                    B(game,bx-side*.55f,6.1f,.22f,.13f,FactoryAmber);
                }
                B(game,x,1.25f,7.95f,.32f,FactorySteel);
                B(game,x+side*5.65f,6.1f,2.7f,2.05f,FactoryIvory);
                B(game,x+side*5.65f,6.1f,2.3f,1.65f,FactoryDark);
                B(game,x+side*5.65f,6.15f,1.7f,.16f,FactoryTeal);
                B(game,x+side*5.15f,5.85f,.16f,.65f,FactoryTeal);
            }
            else
            {
                // Local relay electronics and paper-copy drawers lead toward the dispatch.
                B(game,x-side*1.8f,4.45f,4.5f,5.8f,FactorySteel);
                B(game,x-side*1.8f,5.2f,3.95f,2.5f,FactoryIvory);
                B(game,x-side*1.8f,5.2f,3.35f,1.95f,FactoryDark);
                for(int row=0;row<3;row++)
                {
                    B(game,x-side*1.9f,5.75f-row*.48f,2.45f-row*.35f,.1f,row==0?FactoryTeal:FactorySteel);
                    float y=2.6f+row*1.4f;
                    B(game,x+side*2.6f,y,2.5f,1.12f,FactorySteel);
                    B(game,x+side*2.6f,y+.18f,1.5f,.16f,FactoryIvory);
                }
                B(game,x-side*1.8f,3.2f,3.5f,.35f,FactoryDark);
                B(game,x-side*1.65f,2.5f,1.5f,.5f,FactoryIvory);
                B(game,x+side*2.6f,7.2f,2.7f,.3f,FactoryIvory);
                B(game,x+.4f*side,7.1f,.16f,1.7f,FactorySteel);
            }
        }
    }
}
