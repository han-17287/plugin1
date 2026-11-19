using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientHero : Component
    {
        public readonly AncientCombat Combat=new AncientCombat();
        public float CaptureEnergy;
        public int HurtCalls;
        public bool Wired;
        public Vector2 ReleasedAnchor;
        public Vector2 WireOrigin => (Vector2)transform.position+Vector2.up*.26f;
        public float SpendCapturedEnergy()
        {
            float amount=CaptureEnergy;
            CaptureEnergy=0;
            return amount;
        }
        public void Hurt(float damage) => HurtCalls++;
        public void ReleaseWire() => Wired=false;
        public void ReleaseWireAt(Vector2 point) => ReleasedAnchor=point;
    }

    public sealed class AncientCombat : Object
    {
        public bool Firing,BladeAnimating;
        public int Emissions;
        public void CancelAttacks(){}
        public void EmitParticles(Vector2 point,float amount) => Emissions++;
    }

    public sealed class AncientEnemy : Object
    {
        public Vector2 AimPoint;
        public bool IsAlive=true;
        public float Received;
        public void Hit(float damage,bool breakGuard) => Received+=damage;
    }

    public sealed class AncientGame : Component
    {
        public readonly AncientHero hero=new AncientHero();
        public readonly List<Vector2> anchors=new List<Vector2>();
        public readonly List<AncientEnemy> enemies=new List<AncientEnemy>();
        public bool Paused,won,GameplayInputReady=true;
        public int OrbitalAnchorSlot=-1;
        public void Notice(string text,float length){}
        public void Impact(Vector2 position,Color color,bool heavy){}
        public void Sound(int cue){}
        public void Burst(Vector2 position,Color color,int count){}
        public GameObject Box(string name,Vector2 position,Vector2 size,Color color,int order)
        {
            var shape=new GameObject(name);
            shape.AddComponent<SpriteRenderer>();
            return shape;
        }
    }
}
