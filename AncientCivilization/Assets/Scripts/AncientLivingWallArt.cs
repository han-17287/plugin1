using UnityEngine;
namespace Plugin.AncientCivilization
{
    // Eight original rear-wall furnishings share a fixed pixel grid and palette.
    // They are scenery, with no collision or interaction silhouette in the road.
    public static class AncientLivingWallArt
    {
        const int W=120,H=128;
        static readonly Sprite[] cache=new Sprite[8];
        static readonly Color32 ink=new Color32(13,24,33,255),shadow=new Color32(22,36,44,255),steel=new Color32(45,65,72,255),edge=new Color32(69,91,96,255),paper=new Color32(135,148,137,255);
        static readonly Color32 rust=new Color32(102,63,46,255),warm=new Color32(159,116,60,255),teal=new Color32(47,100,102,255),blue=new Color32(35,105,147,255),glow=new Color32(71,153,172,255);
        static Color32[] pixels;
        public static Sprite Get(int districtCase,int variant)
        {
            int id=Mathf.Clamp(districtCase,0,3)*2+(variant&1);if(cache[id])return cache[id];pixels=new Color32[W*H];
            switch(id/2)
            {
                case 0:Homes((id&1)!=0);break;
                case 1:Scrapyard((id&1)!=0);break;
                case 2:Security((id&1)!=0);break;
                default:Warehouse((id&1)!=0);break;
            }
            var texture=new Texture2D(W,H,TextureFormat.RGBA32,false){name="Living district wall "+id,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply();pixels=null;
            return cache[id]=Sprite.Create(texture,new Rect(0,0,W,H),new Vector2(.5f,0),16);
        }
        static void R(int x,int y,int w,int h,Color32 color)
        {for(int py=Mathf.Max(0,y);py<Mathf.Min(H,y+h);py++)for(int px=Mathf.Max(0,x);px<Mathf.Min(W,x+w);px++)pixels[py*W+px]=color;}
        static void Bolt(int x,int y){R(x,y,3,3,ink);R(x,y+1,2,2,edge);}
        static void Panel(int x,int y,int w,int h)
        {R(x,y,w,h,ink);R(x+2,y+2,w-4,h-4,steel);R(x+2,y+h-4,w-4,2,edge);Bolt(x+3,y+3);Bolt(x+w-6,y+3);}
        static void Pipe(int x,int bottom,int top)
        {R(x,bottom,7,top-bottom,ink);R(x+2,bottom,3,top-bottom,steel);for(int y=bottom+8;y<top-4;y+=24){R(x-1,y,9,4,ink);R(x+1,y+1,5,2,edge);}}
        static void Window(int x,int y,int w,int h,bool lit)
        {
            R(x,y,w,h,ink);R(x+2,y+2,w-4,h-4,lit?warm:teal);
            R(x+w/2-1,y+2,2,h-4,steel);R(x+2,y+h/2-1,w-4,2,steel);R(x-2,y-2,w+4,3,edge);
            if(lit){R(x+3,y+3,3,h-6,rust);R(x+w-6,y+3,3,h-6,rust);}
        }
        static void Grille(int x,int y,int w,int h)
        {R(x,y,w,h,ink);for(int yy=y+3;yy<y+h-2;yy+=5){R(x+2,yy,w-4,2,steel);R(x+2,yy,w-4,1,edge);}}
        static void Homes(bool clinic)
        {
            R(12,0,98,111,ink);R(15,0,92,107,shadow);R(15,104,92,3,edge);
            for(int y=10;y<101;y+=20)R(15,y,92,1,steel);
            Pipe(8,0,120);Pipe(105,0,119);R(10,115,101,4,ink);R(12,117,97,1,edge);
            Window(23,73,30,25,true);Window(66,73,30,25,!clinic);
            R(20,63,82,3,ink);R(20,64,82,1,steel);
            // Laundry and folded awnings identify lived-in spaces, not a row of crates.
            if(!clinic)
            {
                R(24,48,13,15,teal);R(21,57,19,5,teal);R(28,60,5,3,shadow);R(26,49,2,12,edge);
                R(47,46,10,17,paper);R(59,46,9,17,paper);R(47,58,21,5,paper);R(56,46,3,12,shadow);
                R(79,49,16,13,rust);R(76,57,22,5,rust);R(85,60,5,3,shadow);
            }
            else
            {
                Panel(25,49,68,16);R(30,53,10,8,teal);R(33,51,4,12,paper);R(29,55,12,4,paper);
                for(int x=46;x<86;x+=7)R(x,55,4,3,edge);
            }
            Panel(25,0,31,42);R(30,4,21,31,ink);Grille(33,18,15,14);R(46,10,2,4,paper);
            Panel(65,3,31,35);Window(68,17,25,16,clinic);R(69,7,23,6,shadow);
            R(20,0,80,3,steel);R(17,0,5,10,ink);R(98,0,5,13,ink);
            R(71,38,4,6,paper);R(77,38,10,4,rust);R(33,42,13,2,warm);
        }
        static void Scrapyard(bool press)
        {
            R(15,0,93,105,ink);R(18,0,87,102,shadow);
            R(13,106,97,5,steel);R(12,111,98,3,edge);R(22,91,77,4,rust);
            for(int i=0;i<4;i++){R(20+i*24,0,4,106,steel);Bolt(20+i*24,96);}
            if(press)
            {
                Panel(30,9,61,61);R(36,17,49,45,ink);R(41,20,39,20,rust);
                for(int x=44;x<78;x+=9){R(x,23,3,12,steel);R(x,32,6,2,edge);}
                R(37,52,47,9,steel);R(39,59,43,2,edge);Pipe(56,63,99);
                R(30,74,20,10,ink);R(33,77,5,4,warm);R(41,77,6,4,teal);
                R(25,6,71,4,rust);for(int x=28;x<93;x+=10)R(x,7,4,2,warm);
            }
            else
            {
                for(int row=0;row<3;row++)
                {
                    int x=26+(row&1)*7,y=7+row*25;
                    R(x,y,65,19,ink);R(x+3,y+4,59,12,row==1?steel:rust);R(x+10,y+10,35,4,shadow);
                    R(x+6,y,10,9,ink);R(x+47,y,10,9,ink);R(x+8,y+2,6,4,edge);R(x+49,y+2,6,4,edge);
                    R(x+2,y+14,61,2,edge);
                }
                R(39,91,3,18,edge);R(39,88,12,3,edge);R(48,88,3,7,edge);
            }
            Grille(92,69,10,22);R(15,0,93,4,steel);R(6,0,10,18,ink);R(8,2,6,12,rust);
        }
        static void Security(bool relay)
        {
            Panel(15,0,94,114);R(23,6,78,99,shadow);R(19,7,3,99,ink);R(101,7,3,99,ink);
            for(int y=14;y<100;y+=14){R(23,y,78,3,steel);R(23,y+2,78,1,edge);}
            R(60,7,3,97,ink);Panel(21,107,82,12);
            for(int x=26;x<100;x+=10){R(x,110,5,5,rust);R(x+2,112,3,3,warm);}
            Pipe(7,0,120);R(108,0,7,120,ink);R(111,0,2,120,steel);
            if(relay)
            {
                Panel(37,31,49,55);Grille(43,37,37,18);R(43,62,37,17,ink);R(47,66,15,8,teal);
                for(int y=66;y<74;y+=4)R(66,y,10,2,paper);Pipe(56,0,31);
            }
            else
            {
                Panel(27,67,26,18);R(32,72,16,8,ink);R(36,75,8,3,warm);
                R(79,44,10,4,ink);R(81,44,3,3,edge);R(30,21,9,12,rust);R(32,23,5,8,paper);
            }
            R(13,0,98,5,ink);R(17,1,90,2,steel);
        }
        static void Warehouse(bool conduit)
        {
            R(12,0,99,110,ink);R(16,3,91,103,shadow);R(11,108,101,5,steel);R(11,112,101,2,edge);
            if(conduit)
            {
                Panel(29,9,67,91);Grille(36,17,53,16);Grille(36,72,53,19);
                for(int i=0;i<3;i++)
                {int x=37+i*19;R(x,39,14,28,ink);R(x+3,42,8,22,blue);R(x+6,44,3,18,glow);R(x+2,39,10,3,steel);R(x+2,64,10,3,steel);}
                Pipe(17,0,107);Pipe(101,0,107);R(20,102,84,4,steel);
            }
            else
            {
                for(int row=0;row<3;row++)for(int col=0;col<3;col++)
                {
                    int x=23+col*27,y=8+row*31;Panel(x,y,23,26);R(x+5,y+5,13,16,ink);R(x+9,y+8,5,11,blue);R(x+11,y+9,2,9,glow);R(x+4,y+22,15,2,paper);
                }
                for(int y=5;y<102;y+=31){R(19,y,87,3,steel);R(19,y+2,87,1,edge);}
                R(18,0,4,107,edge);R(102,0,4,107,edge);
            }
            Panel(37,115,50,11);for(int x=43;x<83;x+=7)R(x,118,4,3,teal);
            R(12,0,99,4,steel);
        }
    }
}
