using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Unity.Collections;
using UnityEngine;
using Plugin.AncientCivilization;

static class GuardSkinChecks
{
    static int checks;
    static void Require(bool ok,string message) { if(!ok)throw new Exception(message);checks++; }
    static Vector2 Pixels(Sprite s,int i) => new Vector2(217.5f+s.positions[i].x*185,s.positions[i].y*185);
    static int Near(Sprite s,float x,float y,bool arm) => Enumerable.Range(0,s.rest.Length)
        .Where(i=>(s.uv[i].x>=.5f)==arm)
        .OrderBy(i=>Vector2.Distance(s.rest[i],new Vector2(x,y))).First();
    static void Main(string[] args)
    {
        Time.time = 4; Time.deltaTime = 1f / 60f;
        var original=new Sprite();
        var body=new SpriteRenderer{sprite=original};
        var motion=new AncientFieldFoeMotion(body,false);
        motion.Present(4,1,false,0,false);
        Require(body.sprite==original && Allocations.Live==0,"정지 중 원래 LANCER 스프라이트를 직접 표시해야 합니다.");
        var skin=new AncientGuardSkin(body);
        var neutral=AncientGuardSkin.Pose.Sample(0,0,false,0,10,0,false);
        skin.Show(neutral);
        var mesh=body.sprite;
        Require(mesh!=original && mesh.rest.Length==mesh.uv.Length,"두 원화 UV 영역의 정점 수가 맞지 않습니다.");
        for(int i=0;i<mesh.rest.Length;i++)
            Require(Vector2.Distance(Pixels(mesh,i),mesh.rest[i])<.001f,"중립 변형의 원본 발/형태가 어긋났습니다.");
        int baseHinge=Near(mesh,274,194,false), armHinge=Near(mesh,274,194,true);
        int bladeA=Near(mesh,344,175,true),bladeB=Near(mesh,420,195,true);
        int foot=Near(mesh,85,2,false);
        var bindGap=mesh.rest[baseHinge]-mesh.rest[armHinge];
        float bladeSpan=Vector2.Distance(mesh.rest[bladeA],mesh.rest[bladeB]);
        foreach(var sample in new[]{("neutral",neutral),
            ("walk",AncientGuardSkin.Pose.Sample(1,.2f,false,0,10,0,false)),
            ("wind",AncientGuardSkin.Pose.Sample(0,0,true,.68f,10,0,false)),
            ("active",AncientGuardSkin.Pose.Sample(0,0,false,0,.045f,0,false)),
            ("settle",AncientGuardSkin.Pose.Sample(0,0,false,0,.22f,0,false)),
            ("hurt",AncientGuardSkin.Pose.Sample(0,0,false,0,10,.85f,true))})
        {
            skin.Show(sample.Item2);
            Require(body.sprite==mesh&&Allocations.Live==1,"반복 재생 중 메시 또는 버퍼가 새로 생성됐습니다.");
            Require(mesh.positions.All(body.localBounds.Contains),"변형 정점이 렌더러 가시 범위를 벗어났습니다.");
            Require(Math.Abs(Vector2.Distance(Pixels(mesh,bladeA),Pixels(mesh,bladeB))-bladeSpan)<.03f,
                "칼날과 손잡이가 다른 배율로 늘어났습니다.");
            Require(((Pixels(mesh,baseHinge)-Pixels(mesh,armHinge))-bindGap).magnitude<1.5f,"팔꿈치 이음새가 벌어졌습니다.");
            if(sample.Item1=="wind") Require(Vector2.Distance(mesh.rest[foot],Pixels(mesh,foot))<.5f,"예고 중 디딘 발이 미끄러졌습니다.");
            if(args.Length==2 && args[0]=="--dump")
            {
                Directory.CreateDirectory(args[1]);
                File.WriteAllText(Path.Combine(args[1],sample.Item1+".json"),JsonSerializer.Serialize(new{
                    positions=Enumerable.Range(0,mesh.rest.Length).Select(i=>new[]{48+Pixels(mesh,i).x,484-Pixels(mesh,i).y}),
                    uv=mesh.uv.Select(p=>new[]{p.x*870,(1-p.y)*327}), triangles=mesh.triangles}));
            }
        }
        var second=new SpriteRenderer{sprite=original};
        var otherSkin=new AncientGuardSkin(second);
        otherSkin.Show(neutral);
        Require(second.sprite!=mesh && second.sprite.positions!=mesh.positions,"두 LANCER가 쓰기 가능한 변형 버퍼를 공유합니다.");
        skin.Rest(); Require(body.sprite==original,"정지가 원본 스프라이트로 복귀하지 않았습니다.");
        Require(!body.localBounds.Contains(new Vector3(0,1,0)),"정지 후 변형 전용 가시 범위가 남았습니다.");
        otherSkin.Dispose();skin.Dispose();motion.Dispose();
        Require(Allocations.Live==0,"장면 퇴장 후 영구 버퍼가 남았습니다.");
        Console.WriteLine($"PASS {checks} original LANCER mesh/UV, foot, elbow, rigid blade, idle, instance and cleanup checks (rendering doubles).");
    }
}
