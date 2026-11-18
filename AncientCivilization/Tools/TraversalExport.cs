using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Plugin.PlayerCore;
class TraversalExport
{
    static float Number(string text,string key)=>float.Parse(Regex.Match(text,@"^  "+key+@": ([\d.]+)$",RegexOptions.Multiline).Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture);
    static void Main(string[] args)
    {
        string folder=args[0];var prototypes=new List<object>();var floors=new List<object>();
        for(int floor=0;floor<5;floor++)
        {
            string profile=File.ReadAllText(Path.Combine(folder,FloorTraversalLayout.Resources[floor]+".asset"));var stages=new List<object>();
            for(int s=0;s<5;s++)stages.Add(new{name=FloorTraversalLayout.Stages[floor,s],flow=FloorTraversalLayout.Flow[floor,s],challenge=FloorTraversalLayout.Challenge(floor,s),rest=FloorTraversalLayout.Rest[floor,s]});
            floors.Add(new{id=floor,name=FloorTraversalLayout.Names[floor],hint=FloorTraversalLayout.Hints[floor],stages});
            foreach(string tier in new[]{"Easy","Normal","Hard"})
            {
                string text=File.ReadAllText(Path.Combine(folder,tier+".asset"));int count=(int)Number(text,"linkedAnchors"),gap=(int)Number(text,"gapTiles");
                float exit=TraversalLayout.ExitX(count,gap),width=TraversalLayout.Width(count,gap);
                foreach(bool precision in new[]{false,true})
                {
                    var points=new List<object>{new object[]{2,1,"P"},new object[]{3,1,"C"},new object[]{6,4,"A"}};var gimmicks=new List<object>();
                    for(int i=1;i<=count;i++){float x,y;FloorTraversalLayout.Anchor((TraversalFloor)floor,i,count,gap,out x,out y);points.Add(new object[]{x,y,i%2==0?"M":"a"});if(floor==4&&i%2==0)gimmicks.Add(new{kind="timed",x,y,period=Number(profile,"socketPeriod"),window=Number(profile,"socketOpen")});}
                    points.Add(new object[]{exit,4,"A"});points.Add(new object[]{width-3,1,"E"});
                    var blocks=new List<float[]>();if(precision){blocks.Add(new[]{0f,9,0,0});blocks.Add(new[]{exit-2,width-1,0,0});}else blocks.Add(new[]{0f,width-1,0,0});
                    if(floor==1)for(int i=1;i<=count;i+=2)gimmicks.Add(new{kind="scan",x=6+i*gap,y=4,width=Number(profile,"scanWidth"),period=Number(profile,"scanPeriod"),window=Number(profile,"scanHold")});
                    if(floor==2){float x=FloorTraversalLayout.GateX(count,gap);gimmicks.Add(new{kind="gate",x,y=4,period=Number(profile,"gatePeriod"),window=Number(profile,"gateOpen")});blocks.Add(new[]{FloorTraversalLayout.WaitLeft(x),FloorTraversalLayout.WaitRight(x)-1,2,2});}
                    if(floor==3){float x=6+2*gap;gimmicks.Add(new{kind="vent",x,y=6.3f,lift=Number(profile,"ventLift")});blocks.Add(new[]{x-1.5f,x+.5f,1.6f,1.6f});}
                    prototypes.Add(new{floor,tier,precision,width,height=15,points,blocks,gimmicks,hazards=precision?new[]{new[]{10f,exit-3,0}}:Array.Empty<float[]>(),amplitude=Number(text,"movingAmplitude"),period=Number(text,"movingPeriod"),verticalMotion=floor==3});
                }
            }
        }
        Console.Write(JsonSerializer.Serialize(new{prototypes,floors,source="C# floor layout plus shipping profile fields; not a Unity render"}));
    }
}
