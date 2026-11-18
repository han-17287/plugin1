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
        string folder=args[0];var prototypes=new List<object>();
        foreach(string tier in new[]{"Easy","Normal","Hard"})
        {
            string text=File.ReadAllText(Path.Combine(folder,tier+".asset"));int count=(int)Number(text,"linkedAnchors"),gap=(int)Number(text,"gapTiles");
            float exit=TraversalLayout.ExitX(count,gap),width=TraversalLayout.Width(count,gap);
            foreach(bool precision in new[]{false,true})
            {
                var points=new List<object>{new object[]{2,1,"P"},new object[]{3,1,"C"},new object[]{6,4,"A"}};
                for(int i=1;i<=count;i++){float x,y;TraversalLayout.Anchor(i,out x,out y,gap);points.Add(new object[]{x,y,i%2==0?"M":"a"});}
                points.Add(new object[]{exit,4,"A"});points.Add(new object[]{width-3,1,"E"});
                object blocks=precision?new[]{new[]{0f,9,0,0},new[]{exit-2,width-1,0,0}}:new[]{new[]{0f,width-1,0,0}};
                prototypes.Add(new{tier,precision,width,height=15,points,blocks,hazards=precision?new[]{new[]{10f,exit-3,0}}:Array.Empty<float[]>(),amplitude=Number(text,"movingAmplitude"),period=Number(text,"movingPeriod")});
            }
        }
        Console.Write(JsonSerializer.Serialize(new{prototypes,source="C# TraversalLayout plus shipping ScriptableObject fields; not a Unity render"}));
    }
}
