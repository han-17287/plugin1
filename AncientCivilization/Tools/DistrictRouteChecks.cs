using System;
using System.Linq;
using System.Text.Json;
using System.IO;
using Plugin.PlayerCore;
class DistrictRouteChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);checks++;}
    // 직선 이동 캡슐의 보수적인 사각 외접 범위를 모든 보행면과 비교한다.
    static bool Intersects(float ax,float ay,float bx,float by,float l,float r,float bottom,float top)
    {
        float start=0,end=1;
        return Axis(ax,bx-ax,l,r,ref start,ref end)&&Axis(ay,by-ay,bottom,top,ref start,ref end)&&start<=end;
    }
    static bool Axis(float a,float delta,float min,float max,ref float start,ref float end)
    {
        if(Math.Abs(delta)<.00001f)return a>=min&&a<=max;
        float lo=(min-a)/delta,hi=(max-a)/delta;if(lo>hi)(lo,hi)=(hi,lo);start=Math.Max(start,lo);end=Math.Min(end,hi);return start<=end;
    }
    static void Main(string[] args)
    {
        var maps=Enumerable.Range(0,5).Select(DistrictRouteLayout.Create).ToArray();
        if(args.Length>0&&args[0]=="--export")
        {Console.WriteLine(JsonSerializer.Serialize(maps,new JsonSerializerOptions{IncludeFields=true}));return;}
        string diagram=File.Exists("DistrictMapPreview.html")?"DistrictMapPreview.html":"AncientCivilization/DistrictMapPreview.html";
        string html=File.ReadAllText(diagram),marker="const maps=";int begin=html.IndexOf(marker,StringComparison.Ordinal)+marker.Length,end=html.IndexOf(";let floor",begin,StringComparison.Ordinal);
        var options=new JsonSerializerOptions{IncludeFields=true};var preview=JsonSerializer.Deserialize<DistrictRouteLayout[]>(html.Substring(begin,end-begin),options);
        Check(JsonSerializer.Serialize(preview,options)==JsonSerializer.Serialize(maps,options),"Shipped diagram exactly matches canonical runtime geometry");
        foreach(var m in maps)
        {
            Check(m.width>=110&&m.width<=120,"Authored routes contain multiple screens");Check(m.stops.Length==3,"Three named grounded restart pockets");
            Check(m.gapRight-m.gapLeft>=10&&m.gapRight-m.gapLeft<=24,"One local mandatory hazard area, not an entire level");
            foreach(var d in m.decks)Check(d.right>d.left&&d.left>=0&&d.right<=m.width,"Deck bounds "+m.title+"/"+d.name);
            foreach(var s in m.stops)
            {
                Check(m.decks.Any(d=>s.x-.4f>d.left&&s.x+.4f<d.right&&Math.Abs(s.y-1-d.top)<.01f),"Restart capsule has stable floor: "+s.name);
                foreach(var d in m.decks)Check(!Intersects(s.x,s.y+.02f,s.x,s.y+.02f,d.left-.39f,d.right+.39f,(d.bridge?d.top-.65f:-5)-.93f,d.top+.93f),"Restart capsule is not inside geometry: "+s.name);
            }
            for(int i=0;i<m.hooks.Length;i++)for(int pa=-10;pa<=10;pa++)for(int pb=-10;pb<=10;pb++)
            {
                var b=m.hooks[i];var a=i==0?new DistrictHook(m.stops[0].x,m.stops[0].y):m.hooks[i-1];
                float ax=a.x,ay=a.y,bx=b.x,by=b.y;
                bool vertical=m.title.StartsWith("상층");
                if(a.kind==2){if(vertical)ay+=pa/10f;else ax+=pa/10f;}
                if(b.kind==2){if(vertical)by+=pb/10f;else bx+=pb/10f;}
                Check((bx-ax)*(bx-ax)+(by-ay)*(by-ay)<=12*12,"Two-tile range reserve: "+m.title+" link "+i);
                Check(Math.Max(ay,by)+.93f<m.ceiling,"Whole capsule fits under closed ceiling");
                foreach(var d in m.decks)
                {
                    float depth=d.bridge?.65f:d.top+5;
                    Check(!Intersects(ax,ay,bx,by,d.left-.39f,d.right+.39f,d.top-depth-.93f,d.top+.93f),"Straight capsule path blocked: "+m.title+" link "+i+" by "+d.name+" phases "+pa+","+pb);
                }
            }
            var rescue=m.Recovery;var next=m.hooks.First(h=>h.kind==0&&h.x>=rescue.x);
            Check((rescue.y+1.1f)*(rescue.y+1.1f)<=12*12,"A drained safe floor can reach its permanent recovery anchor");
            Check((next.x-rescue.x)*(next.x-rescue.x)+(next.y-rescue.y)*(next.y-rescue.y)<=12*12,"Recovery reconnects to a permanent anchor with range reserve");
            foreach(var d in m.decks)
            {
                float bottom=d.bridge?d.top-.65f:-5;
                Check(!Intersects(rescue.x,-1.1f,rescue.x,rescue.y,d.left-.39f,d.right+.39f,bottom-.93f,d.top+.93f),"Vertical rescue route is clear: "+m.title+" / "+d.name);
                Check(!Intersects(rescue.x,rescue.y,next.x,next.y,d.left-.39f,d.right+.39f,bottom-.93f,d.top+.93f),"Rescue returns to route without clipping: "+m.title+" / "+d.name);
            }
            var last=m.hooks[m.hooks.Length-1];Check(m.decks.Any(d=>d.left<last.x&&d.right>last.x&&d.top==0),"Last anchor allows safe exit landing");
        }
        Check(maps.Select(m=>string.Join(";",m.decks.Select(d=>$"{d.left},{d.right},{d.top}"))).Distinct().Count()==5,"All five districts have different topology");
        Console.WriteLine("PASS: "+checks+" authored district route range, capsule corridor, restart pocket, ceiling and topology checks. Native Unity physics not run.");
    }
}
