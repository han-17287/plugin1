using System;
using System.Text;
using Plugin.AncientCivilization;
class RouteRadioChecks
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static string Drain(AncientRouteRadioPlayback p,int chunk=9)
    {
        var joined=new StringBuilder();bool recorded=false,validPrefixes=true;int guard=0;
        while(p.Active&&guard++<20000)
        {
            if(!p.PageReady){p.SetPageEnd(Math.Min(p.Text.Length,p.PageStart+chunk));recorded=false;}
            p.Tick(.02f,true);
            string shown=p.VisibleText;
            validPrefixes&=shown.Length==0||!char.IsHighSurrogate(shown[shown.Length-1]);
            if(p.Complete&&!recorded){joined.Append(shown);recorded=true;}
        }
        Check(validPrefixes,"A visible typewriter prefix never ends on half a surrogate pair");
        Check(!p.Active,"Every queued radio message eventually finishes");return joined.ToString();
    }
    static void Main()
    {
        const string line="문 앞의 경비를 확인해라.\n파란 입자를 수집하고 😃 다음 통로로 이동해라!";
        for(int chunk=1;chunk<=17;chunk++)
        {var p=new AncientRouteRadioPlayback();p.Show("통신관",line);Check(p.Speaker=="사령관 · 무전","Commander uses a radio identity");Check(Drain(p,chunk)==line,"Automatic pages preserve the complete Korean message, whitespace and emoji");}
        var paused=new AncientRouteRadioPlayback();paused.Show("통신관","가나다라마바사");paused.SetPageEnd(paused.Text.Length);paused.Tick(.12f,true);
        string before=paused.VisibleText;paused.Tick(200,false);Check(paused.VisibleText==before,"Hidden or paused time does not reveal text");
        paused.Tick(.04f,true);Check(paused.VisibleText.Length<=before.Length+2,"Resuming keeps the remaining letter delay instead of flooding the page");
        var hold=new AncientRouteRadioPlayback();hold.Show("통신관","가");hold.SetPageEnd(1);hold.Tick(.04f,true);hold.Tick(1,true);hold.Tick(300,false);
        Check(hold.Active,"A paused reading hold cannot expire");hold.Tick(.4f,true);Check(hold.Active,"The remaining hold survives resume");hold.Tick(.2f,true);Check(!hold.Active,"A fully visible message exits after the remaining1.5-second active hold");
        var layout=new AncientRouteRadioPlayback();layout.Show("통신관","가나다");layout.Tick(100,true);layout.SetPageEnd(3);layout.Tick(.04f,true);
        Check(layout.VisibleText=="가","Time before first layout does not skip the opening letters");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1,0})
        {before=layout.VisibleText;layout.Tick(invalid,true);Check(layout.VisibleText==before,"Invalid elapsed input does not change playback");}
        var queue=new AncientRouteRadioPlayback();for(int i=0;i<9;i++)queue.Show("통신관","[무전"+i+"]");string all=Drain(queue,5);
        int previous=-1;
        for(int i=0;i<9;i++)
        {string token="[무전"+i+"]";int at=all.IndexOf(token,StringComparison.Ordinal);Check(at>previous,"Overflow preserves incoming message order");Check(at==all.LastIndexOf(token,StringComparison.Ordinal),"Overflow neither drops nor repeats a message");previous=at;}
        foreach(string name in new[]{"백명","백명 경위","백명 · 무전"})
        {var p=new AncientRouteRadioPlayback();p.Show(name,"백명 경위, 이동한다.");Check(p.Speaker=="백 경위 · 무전"&&!p.Text.Contains("백명"),"Canonical full name stays hidden on live radio");}
        foreach(int hz in new[]{15,30,60,120})
        {
            var p=new AncientRouteRadioPlayback();p.Show("사령관","가나다라마바사아자차카타");float seconds=0;
            while(p.Active&&seconds<5){if(!p.PageReady)p.SetPageEnd(p.Text.Length);p.Tick(1f/hz,true);seconds+=1f/hz;}
            Check(!p.Active&&Math.Abs(seconds-1.98f)<.16f,"Radio typing and reading delay have consistent duration across frame rates");
        }
        Console.WriteLine("PASS: "+checks+" live-radio queue, typewriter, paging and active-clock checks");
    }
}
