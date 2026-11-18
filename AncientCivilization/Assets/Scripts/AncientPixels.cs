using System;

namespace Plugin.AncientCivilization
{
    // Runtime native-pixel presentation: opaque palette pixels and transparent space only.
    public static class AncientPixels
    {
        static readonly uint[] palette={
            0x070C15FFu,0x111923FFu,0x253241FFu,0x425264FFu,0x6A7884FFu,
            0x514C38FFu,0x958B74FFu,0xC2A068FFu,0x3E1B1BFFu,0x79352EFFu,0xB4563BFFu,
            0x123B36FFu,0x226553FFu,0x34A578FFu,0x6AD8A0FFu,0xAEF5CAFFu,
            0xBDB98AFFu,0xF2D64BFFu,0xFFFFB4FFu,0x6E5B4DFFu,0xB18B69FFu,0xEAD2A0FFu,
            0x302C43FFu,0x62516EFFu,0x9B7EAAFFu,0xD9CBD7FFu,0xE7E1D4FFu,
            0xF8FAFFFFu,0xDDE8F2FFu,0x2051A0FFu,0x279AE8FFu,0x87E1FFFFu
        };
        public static uint Quantize(uint rgba)
        {
            if((rgba&255)<110)return 0;
            int r=(int)(rgba>>24),g=(int)((rgba>>16)&255),b=(int)((rgba>>8)&255),best=int.MaxValue;uint result=0;
            foreach(uint color in palette)
            {
                int dr=r-(int)(color>>24),dg=g-(int)((color>>16)&255),db=b-(int)((color>>8)&255);
                int distance=dr*dr+dg*dg+db*db;
                if(distance<best){best=distance;result=color;}
            }
            return result;
        }
        // Extract one connected actor from an atlas where a long sword overlaps a neighbour's columns.
        // This runs once at load; it preserves weapon pixels rather than clipping to a nominal cell.
        public static uint[] Isolate(uint[] source,int width,int height,int seedX,int seedY)
        {
            if(source==null||source.Length!=width*height)throw new ArgumentException("Invalid component buffer");
            var result=new uint[source.Length];int seed=-1,best=int.MaxValue;
            for(int y=System.Math.Max(0,seedY-40);y<System.Math.Min(height,seedY+41);y++)
                for(int x=System.Math.Max(0,seedX-40);x<System.Math.Min(width,seedX+41);x++)
                {
                    if((source[y*width+x]&255)<110)continue;
                    int d=(x-seedX)*(x-seedX)+(y-seedY)*(y-seedY);
                    if(d<best){best=d;seed=y*width+x;}
                }
            if(seed<0)return result;
            var queue=new int[source.Length];int read=0,write=0;queue[write++]=seed;result[seed]=source[seed];
            while(read<write)
            {
                int at=queue[read++],px=at%width,py=at/width;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    int x=px+dx,y=py+dy;if(x<0||y<0||x>=width||y>=height)continue;
                    int next=y*width+x;if(result[next]!=0||(source[next]&255)<110)continue;
                    result[next]=source[next];queue[write++]=next;
                }
            }
            return result;
        }
        public static uint[] Grid(uint[] source,int width,int height,int step,int regionWidth,int regionHeight)
        {
            if(source==null||source.Length!=width*height||width<1||height<1)throw new ArgumentException("Invalid pixel buffer");
            if(step<1)throw new ArgumentOutOfRangeException("step");
            regionWidth=Math.Max(0,Math.Min(width,regionWidth));regionHeight=Math.Max(0,Math.Min(height,regionHeight));
            var result=(uint[])source.Clone();
            for(int y=0;y<regionHeight;y+=step)for(int x=0;x<regionWidth;x+=step)
            {
                int sx=Math.Min(x+step/2,regionWidth-1),sy=Math.Min(y+step/2,regionHeight-1);
                uint color=Quantize(source[sy*width+sx]);
                for(int py=y;py<Math.Min(y+step,regionHeight);py++)for(int px=x;px<Math.Min(x+step,regionWidth);px++)result[py*width+px]=color;
            }
            return result;
        }
    }
}
