using System;
using System.Collections.Generic;
using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 렌더러가 실제로 받은 점과 실제 Hurt 호출을 관측한다.
    public enum AncientEquipmentCue { Lens, Counter, Contact, Sweep, Forge, ShortCircuit, JokerCharge }
    // GPU에 올리는 재질만 대체한다. 선 좌표와 표시 상태는 실제 패턴 코드가 갱신한다.
    public sealed class AncientBeamVisuals : IDisposable
    {
        public AncientBeamVisuals(LineRenderer glow, bool cannon, bool createEndpoints) {}
        public void StyleBody(LineRenderer body, LineRenderer core, bool filament) {}
        public void Dispose() {}
    }
    public static class AncientVisuals { public static bool HighQuality = true; }
    public class AncientAudio : Component { public int Stops; public void StopJokerCharge() { Stops++; } }
    public class AncientHero : Component
    {
        public int Hits; public Action OnDamage;
        public void Hurt(float amount) { Hits++; OnDamage?.Invoke(); }
    }
    public class AncientGame : Component
    {
        public AncientHero hero;
        public bool ReducedFeedback;
        public readonly Dictionary<string,LineRenderer> Lines = new Dictionary<string,LineRenderer>();
        public readonly List<AncientEquipmentCue> Sounds = new List<AncientEquipmentCue>();
        public LineRenderer Line(string name, Color color, float width, int order)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            Lines.Add(name,line); return line;
        }
        public void Sound(AncientEquipmentCue cue) { Sounds.Add(cue); }
        public void Burst(Vector2 where, Color color, int count) {}
    }
    public class AncientJokerArena
    {
        public readonly List<int> Signals = new List<int>();
        public float EscapeHeight => 1.08f;
        public Vector2 MirrorPoint(int index) => new Vector2(index == 0 ? 1.35f : 28.65f,5.55f);
        public void SetMirrors(int first, int count) { Signals.Add(count); }
    }
}

class JokerAttackChecks
{
    static int checks;
    static void Check(bool pass,string message) { if (!pass) throw new Exception(message); checks++; }
    sealed class Stage
    {
        public readonly AncientGame Game = new GameObject("game").AddComponent<AncientGame>();
        public readonly AncientJokerArena Arena = new AncientJokerArena();
        public readonly AncientJokerAttacks Attack;
        public LineRenderer Body => Game.Lines["판정"];
        public Stage()
        {
            Game.hero = new GameObject("hero").AddComponent<AncientHero>();
            Game.gameObject.AddComponent<AncientAudio>();
            Attack = new AncientJokerAttacks(Game, Game.transform, 0, 30);
            Attack.SetArena(Arena);
        }
        public void Tick(float dt, Vector2 hero) { Game.hero.transform.position=hero; Attack.Tick(dt,hero); }
        public int Hits => Game.hero.Hits;
    }
    static readonly Vector2 Muzzle = new Vector2(24,3.8f), Hero = new Vector2(12,1.08f);

    static void Mirrors()
    {
        var t=new Stage();
        t.Attack.Mirror(Muzzle,Hero,2);
        Check(t.Body.positionCount==2 && t.Arena.Signals[0]==1,"Only first mirror is lit initially");
        Check(t.Game.Lines["광학 표식 0"].positionCount==2 && !t.Game.Lines["광학 표식 1"].enabled,
            "First mirror exposes only its illuminated incoming facet");
        t.Tick(.8f,Hero);
        Check(t.Body.positionCount==4 && t.Arena.Signals[1]==2,"Second lamp reveals the second physical reflection");
        Check(Vector2.Distance(t.Body.GetPosition(1),t.Arena.MirrorPoint(0))<.001f &&
              Vector2.Distance(t.Body.GetPosition(2),t.Arena.MirrorPoint(1))<.001f,"Line touches lit geometry in order");
        t.Tick(.78f,Hero);
        var endpoint=t.Body.GetPosition(3);
        Vector2 escaped = new Vector2(26,1.08f);
        t.Attack.Aim(new Vector2(3,7),escaped);
        t.Tick(.49f,escaped);
        Check(!t.Attack.StartedFiring && Vector2.Distance(endpoint,t.Body.GetPosition(3))<.001f &&
            Vector2.Distance(Muzzle,t.Body.GetPosition(0))<.001f,"Locked geometry remains still when both parties move");
        t.Tick(.04f,escaped);
        Check(t.Attack.StartedFiring && t.Hits==0,"Escaped old angle before trigger");
        t.Tick(.04f,Hero);
        Check(t.Hits==1,"Returning to actual final reflected segment hits");
        t.Tick(.07f,Hero);
        Check(t.Hits==1,"Reflected ray cannot hit multiple times in one shot");
        t.Tick(.20f,Hero);
        Check(!t.Attack.Busy && !t.Body.enabled && t.Arena.Signals[2]==0,"Ray and lamps cleared on completion");

        t.Attack.Mirror(Muzzle,Hero,1);
        t.Tick(8,Hero);
        Check(t.Attack.Telegraphing && !t.Attack.StartedFiring && t.Hits==1,"A hitch still displays frozen warning before impact");
        t.Tick(4,Hero);
        Check(t.Hits==2,"A hitch across whole active window resolves the shot exactly once");

        var lanes = new Stage();
        int mask=lanes.Attack.MirrorSafeLanes(Muzzle,Hero);
        Check(mask!=0,"At least one planned refuge is available");
        for (int lane=0; lane<3; lane++)
        {
            if((mask & (1<<lane))==0) continue;
            var s=new Stage(); Vector2 refuge=new Vector2(5+10*lane,1.08f);
            s.Attack.Mirror(Muzzle,Hero,2,lane);
            for(int i=0;i<140;i++) s.Tick(.017f,refuge);
            Check(s.Hits==0,"Protected refuge stays free while reticle follows a moving player");
        }
    }

    static void Duel()
    {
        var s = new Stage(); Vector2 origin=new Vector2(12,1.1f);
        Vector2 inReach=origin+new Vector2(1.4f,0);
        s.Game.hero.transform.position=inReach;
        s.Attack.Club(origin,1);
        s.Tick(.81f,inReach);
        Check(s.Hits==1,"First slash sweeps the body even at low frame rate");
        s.Tick(.63f,inReach);
        Check(s.Hits==1 && s.Attack.Telegraphing,"Offbeat gap between attacks");
        s.Tick(.28f,inReach); Check(s.Hits==2,"Second slash has independent hit window");
        s.Attack.Club(origin,-1);
        s.Tick(2,inReach); Check(s.Hits==2,"Behind the slashing arm is safe");

        var ring=new Stage(); ring.Attack.Ring(new Vector2(10,.65f));
        Vector2 inRing = new Vector2(13,1.08f);
        ring.Tick(.45f,inRing); Check(ring.Hits==0,"Swap ring windup is harmless");
        ring.Tick(2,inRing); Check(ring.Hits==1,"Ring swept across target between frames");
        ring.Attack.Ring(new Vector2(10,.65f));
        ring.Tick(.45f,new Vector2(13,3.15f)); ring.Tick(2,new Vector2(13,3.15f));
        Check(ring.Hits==1,"Jump clears shallow ring");
    }
    static void Laser()
    {
        foreach(bool down in new[]{false,true})
        {
            var s=new Stage();
            s.Attack.Laser(down);
            Vector2 exposed = new Vector2(15,4.6f);
            float start=s.Body.GetPosition(0).y;
            Check((start>5)==down && s.Game.Sounds.Contains(AncientEquipmentCue.JokerCharge),"Start height and unique low warning");
            s.Tick(1.21f,exposed); Check(s.Hits==0,"Thick laser warning harmless");
            s.Tick(.2f,exposed); float moved=s.Body.GetPosition(0).y;
            Check((down?moved<start:moved>start) && s.Body.positionCount==2 && s.Body.startWidth>.8f,"Straight wide beam sweeps in announced direction");
            s.Tick(2,exposed);
            Check(s.Hits==1 && !s.Attack.Busy,"Normal sweep damages once and ends automatically");
            s.Attack.Laser(down); s.Tick(1.2f,Hero); s.Tick(2,Hero);
            Check(s.Hits==1,"Lower lane remains reachable and safe");
        }
        var local=new Stage();
        local.Attack.Laser(false,2); var rightRefuge = new Vector2(25,5); local.Tick(1.21f,rightRefuge); local.Tick(2,rightRefuge);
        Check(local.Hits==0,"Planned edge refuge falls beyond truncated beam");
        local.Attack.Laser(true,2);
        var top=local.Game.Lines["광학 표식 0"]; var bottom=local.Game.Lines["광학 표식 1"];
        Check(Math.Abs(top.GetPosition(0).y-bottom.GetPosition(0).y-local.Body.startWidth)<.001f &&
            Math.Abs(top.GetPosition(1).x-local.Body.GetPosition(1).x)<.001f,"Warning rails mark the real beam width and stop before the refuge");
        local.Tick(1.4f,rightRefuge);
        for(int i=0; i<top.positionCount; i++)
            Check(Math.Abs(top.GetPosition(i).y-local.Body.GetPosition(0).y)<local.Body.startWidth*.5f,
                "Live plasma filament lies inside damaging beam");
        local.Attack.Clear();
        Check(!top.enabled && !bottom.enabled,"Interrupted sweep clears live filaments");
        local.Attack.Laser(true); local.Tick(0,Hero); Check(local.Attack.Progress==0,"Paused warning does not advance");
        local.Attack.Dispose(); local.Tick(15,Hero);
        Check(!local.Attack.Busy && !local.Body.enabled && local.Hits==0 &&
            local.Game.gameObject.GetComponent<AncientAudio>().Stops>0,"Interrupt clears lines, stops charge and cancels damage");
        var life=new Stage();
        life.Game.hero.OnDamage=()=>life.Attack.Dispose(); life.Attack.Laser(false); life.Tick(1.2f,new Vector2(15,4)); life.Tick(2,new Vector2(15,4));
        Check(!life.Attack.Busy && life.Hits==1,"Respawn can tear down attack synchronously from Hurt");
    }
    static void Caustics()
    {
        foreach (int lane in new[] {-1,0,2})
        foreach (bool right in new[] {false,true})
        {
            var s = new Stage();
            s.Attack.SweepFromRight = () => right;
            s.Attack.Laser(false,lane);
            var rays = new[] {s.Game.Lines["굴절광 0"], s.Game.Lines["굴절광 1"]};
            Check(!rays[0].enabled && !rays[1].enabled,"No caustics in the warning corridor");
            s.Tick(1.22f,Hero);
            bool shown=false;
            for (int frame=0; frame<92; frame++)
            {
                s.Tick(1f/60,Hero);
                foreach (var ray in rays)
                {
                    if (!ray.enabled) continue;
                    shown=true;
                    var a=s.Body.GetPosition(0); var b=s.Body.GetPosition(1);
                    Check((ray.GetPosition(3).x-ray.GetPosition(0).x)*(right?-1:1)>0,"Caustic travels away from the real firing port");
                    for(int i=0;i<4;i++)
                    {
                        var p=ray.GetPosition(i); float rim=ray.startWidth*.5f;
                        Check(p.x-rim>=a.x && p.x+rim<=b.x && Math.Abs(p.y-a.y)+rim<s.Body.startWidth*.5f,
                            "Entire tapered strip stays inside the active beam and outside protected lanes");
                    }
                }
            }
            s.Tick(.10f,Hero);
            Check(shown && !s.Attack.Busy && !rays[0].enabled && !rays[1].enabled,"Normal attack cleanup hides both reused strips");
        }

        var quality=new Stage();
        quality.Attack.Laser(true); quality.Tick(1.66f,Hero);
        var ribbon=quality.Game.Lines["굴절광 0"];
        Check(ribbon.enabled,"Full detail draws travelling light during sweep");
        quality.Game.ReducedFeedback=true;quality.Tick(.01f,Hero);
        Check(!ribbon.enabled && quality.Body.enabled,"F5 mid-shot hides only extra light");
        quality.Game.ReducedFeedback=false;AncientVisuals.HighQuality=false;quality.Tick(.01f,Hero);
        Check(!ribbon.enabled && quality.Body.enabled,"Low detail retains original active beam");
        AncientVisuals.HighQuality=true;quality.Tick(.01f,Hero);
        Check(ribbon.enabled,"Detail restored during beam honors current settings");
        quality.Attack.Dispose();
        Check(!ribbon.enabled,"Disposal hides the ordinary sweep strip");
    }

    static void Main() { Caustics(); Mirrors(); Duel(); Laser(); Console.WriteLine($"{checks} Joker attack geometry and timing checks passed."); }
}
