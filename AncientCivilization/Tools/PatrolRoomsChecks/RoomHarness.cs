using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Plugin.PlayerCore
{
    // Executes the shipped floor builders and their callbacks. Input, time, scene navigation
    // and physics contacts are supplied by the scenario; no Unity player or renderer runs.
    public sealed partial class PatrolRoom
    {
        sealed class Interaction
        {
            public string Label;
            public Vector2 Position;
            public Action Action;
            public int Destination;
            public Func<bool> Open;
        }

        readonly PatrolTestExpedition expedition=new PatrolTestExpedition();
        readonly List<(string Label,GameObject Object)> objects=new List<(string,GameObject)>();
        readonly List<Interaction> interactions=new List<Interaction>();
        readonly List<Action<float>> callbacks=new List<Action<float>>();
        readonly Dictionary<string,bool> flags=new Dictionary<string,bool>();
        GameObject arrivalTarget,beamTarget;
        string message="";
        Vector2 Hero=new Vector2(2,.9f),Velocity;
        float Elapsed,Axis;
        bool Grounded=true,Zipping,JumpHeld,PrimaryHeld,CollectHeld,TrialActive;
        int Charge;

        static PatrolRoom Open(int id,PatrolRoom previous=null)
        {
            var r=new PatrolRoom();
            if(previous!=null)
            {
                foreach(var pair in previous.flags) r.flags.Add(pair.Key,pair.Value);
                r.Charge=previous.Charge;
            }
            switch ((id-1)/10)
            {
                case 0: r.BuildProduction(id); break;
                case 1: r.BuildLiving(id); break;
                case 2: r.BuildAdministration(id); break;
                case 3: r.BuildResidence(id); break;
                case 4: r.BuildSummit(id); break;
                default: throw new ArgumentOutOfRangeException(nameof(id));
            }
            return r;
        }

        GameObject ObjectNamed(string name) => objects.Last(o=>o.Label==name).Object;
        void Point(float x,float y,float vx=0,float vy=0)
        { Hero=new Vector2(x,y); Velocity=new Vector2(vx,vy); }
        void UseAction(string label)
        {
            var handle=interactions.Single(i=>i.Action!=null && i.Label==label);
            Hero=handle.Position;
            handle.Action();
        }
        bool PortalOpen(int destination) => interactions.Any(i=>i.Destination==destination && (i.Open==null || i.Open()));
        void Arrive(string name) { arrivalTarget=ObjectNamed(name); }
        void Shot(string name) { beamTarget=ObjectNamed(name); }

        void Tick(int count=1,float dt=.02f)
        {
            for(int i=0;i<count;i++)
            {
                Elapsed+=dt;
                Time.unscaledDeltaTime=dt;
                foreach(var callback in callbacks) callback(dt);
                WantsRoomMusic();
                TickSpeaker(expedition.Paused, false, true);
                arrivalTarget=beamTarget=null;
            }
        }

        void Step(Action<float> callback) => callbacks.Add(callback);
        bool Near(float x,float y,float distance) => (Hero-new Vector2(x,y)).sqrMagnitude<=distance*distance;
        void Push(float x,float y) => Velocity=new Vector2(x,y);
        void Warp(float x,float y) => Point(x,y);
        void Place(GameObject obj,float x,float y) => obj.transform.localPosition=new Vector2(x,y);
        bool Arrived(GameObject target) => target==arrivalTarget;
        bool BeamHits(GameObject target) => target==beamTarget && target.activeSelf;
        void Say(string text) => message=text;
        bool Flag(string key) => flags.TryGetValue(key,out var value) && value;
        void SetFlag(string key,bool value=true) => flags[key]=value;
        void AddCharge(int amount) { Charge=Math.Min(99,Charge+amount); }
        bool SpendCharge(int amount)
        {
            if(amount<0 || amount>Charge) return false;
            Charge-=amount;
            return true;
        }

        GameObject Create(string label,float x,float y)
        {
            var obj=new GameObject(label);
            obj.transform.SetParent(transform,false);
            Place(obj,x,y);
            objects.Add((label,obj));
            return obj;
        }
        GameObject Box(string label,float x,float y,float w,float h,Color color,bool solid=false,int order=2)
        {
            var obj=Create(label,x,y);
            var renderer=obj.AddComponent<SpriteRenderer>();
            renderer.size=new Vector2(w,h);
            renderer.color=color;
            if(solid) obj.AddComponent<BoxCollider2D>().size=renderer.size;
            return obj;
        }
        GameObject Deck(string label,float x,float top,float width,float depth=.35f)
        {
            var deck=Box(label,x,top-depth/2,width,depth,Color.white,true);
            deck.GetComponent<BoxCollider2D>().usedByEffector=true;
            return deck;
        }
        GameObject Hook(string label,float x,float y)
        {
            var hook=Create(label,x,y);hook.AddComponent<GrappleAnchor>();return hook;
        }
        void Sign(string label,float x,float y) { Create(label,x,y); }
        void Use(string label,float x,float y,Action callback,float radius=1.8f) =>
            interactions.Add(new Interaction {Label=label,Position=new Vector2(x,y),Action=callback});
        void Portal(string label,float x,float y,int destination,Func<bool> open=null) =>
            interactions.Add(new Interaction {Label=label,Position=new Vector2(x,y),Destination=destination,Open=open});
    }
}
