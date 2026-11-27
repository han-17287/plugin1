using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plugin.PlayerCore
{
    public enum ElectricFixtureKind { Dummy, BombBulb, CableSnake, Water, WornOutlet, Glass, Filament, Lamp }

    public sealed class ElectricPlugTarget : MonoBehaviour,IElectricReactive
    {
        static readonly List<ElectricPlugTarget> active=new List<ElectricPlugTarget>();
        public static IReadOnlyList<ElectricPlugTarget> Registered => active;
        [SerializeField] ElectricFixtureKind kind;
        [SerializeField, Min(.1f)] float radius=.55f;
        [SerializeField, Min(.1f)] float snakeStun=2f;
        [SerializeField, Min(.05f)] float fuse=.45f;
        [SerializeField, Min(.1f)] float blastRadius=2.8f, blastDamage=55;
        [SerializeField, Min(.1f)] float repairTime=4f;
        public ElectricFixtureKind Kind=>kind;
        public float Radius=>radius;
        public Vector2 Point=>transform.position;
        public bool Targetable=>isActiveAndEnabled && art && returning<=0 && !broken;
        public bool CanShock=>kind==ElectricFixtureKind.CableSnake || kind==ElectricFixtureKind.Dummy;
        public bool Conductive=>kind!=ElectricFixtureKind.Glass && kind!=ElectricFixtureKind.Filament;
        public float StunRemaining=>stun;
        public float Health=>health;
        public bool Lit=>lit;
        float health,stun,timer,returning,activeAge,contactReady;
        bool armed,broken,lit;
        SpriteRenderer art,signal;
        Vector3 home;
        BluePlugSocket outlet;
        Action<bool> onPower;
        static Texture2D sheet;
        static readonly Sprite[] frames=new Sprite[8];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() => active.Clear();
        void OnEnable(){if(!active.Contains(this))active.Add(this);}
        void OnDisable()=>active.Remove(this);

        public static ElectricPlugTarget Place(Transform parent,ElectricFixtureKind kind,Vector2 local,string label,
            Action<bool> onPower=null,GameObject socketHolder=null)
        {
            var item=socketHolder ? socketHolder : new GameObject(label);
            if(!socketHolder){item.transform.SetParent(parent,false);item.transform.localPosition=local;}
            var f=item.AddComponent<ElectricPlugTarget>();
            f.kind=kind;f.onPower=onPower;f.home=item.transform.localPosition;
            f.outlet=item.GetComponent<BluePlugSocket>();
            f.radius=kind==ElectricFixtureKind.Water ? .96f : kind==ElectricFixtureKind.CableSnake ? .67f : .55f;
            int i=(int)kind;
            if(!sheet)sheet=Resources.Load<Texture2D>("Traversal/ElectricFixtures");
            if(!frames[i] && sheet)frames[i]=Sprite.Create(sheet,new Rect(i*32,0,32,32),Vector2.one*.5f,22,0,SpriteMeshType.FullRect);
            f.art=BluePlugArt.AddSprite(item.transform,"전기 장치 본체",frames[i],Vector2.zero,8);
            if(kind==ElectricFixtureKind.WornOutlet)f.art.transform.localPosition=Vector2.down*.5f;
            if(kind==ElectricFixtureKind.Water)f.art.transform.localScale=new Vector3(1.65f,.78f,1);
            f.signal=BluePlugArt.AddSprite(item.transform,"발광 신호",BluePlugArt.Halo,Vector2.zero,9,.9f);
            f.signal.sharedMaterial=BluePlugArt.GlowMaterial;
            f.ResetFixture();
            return f;
        }

        public void ResetFixture()
        {
            health=kind==ElectricFixtureKind.Dummy?180:65;
            stun=timer=returning=activeAge=contactReady=0;
            armed=broken=lit=false;
            if(art){art.enabled=true;art.color=Color.white;}
            if(signal)signal.enabled=false;
            if(kind==ElectricFixtureKind.CableSnake)transform.localPosition=home;
            if(outlet)outlet.SetEquipmentPower(true);
            onPower?.Invoke(false);
        }

        public void React(ElectricPlugPulse pulse,float damage)
        {
            if(!Targetable)return;
            bool mechanicalCut=pulse==ElectricPlugPulse.Blade || pulse==ElectricPlugPulse.Beam ||
                pulse==ElectricPlugPulse.Impact || pulse==ElectricPlugPulse.Blast;
            switch(kind)
            {
                case ElectricFixtureKind.BombBulb:
                case ElectricFixtureKind.WornOutlet:
                    if(!armed){armed=true;timer=fuse;}
                    Flash(new Color(1,.23f,.18f),timer);
                    break;
                case ElectricFixtureKind.Dummy:
                case ElectricFixtureKind.CableSnake:
                    health=Mathf.Max(0,health-damage);
                    if(kind==ElectricFixtureKind.CableSnake && !mechanicalCut && pulse!=ElectricPlugPulse.Burn)
                        stun=Mathf.Max(stun,snakeStun);
                    Flash(new Color(.72f,.81f,1),.16f);
                    if(health<=0)GoDark();
                    break;
                case ElectricFixtureKind.Water:
                    Flash(new Color(.68f,.74f,1),.8f);
                    break;
                case ElectricFixtureKind.Filament:
                case ElectricFixtureKind.Glass:
                    if(mechanicalCut){broken=true;art.enabled=signal.enabled=false;onPower?.Invoke(true);}
                    break;
                case ElectricFixtureKind.Lamp:
                    lit=true;onPower?.Invoke(true);Flash(new Color(1,.83f,.30f),1);break;
            }
        }

        void Flash(Color colour,float seconds)
        {
            signal.enabled=true;signal.color=colour;
            timer=Mathf.Max(timer,seconds);
        }

        void GoDark()
        {
            returning=repairTime;armed=false;timer=stun=0;
            art.enabled=kind==ElectricFixtureKind.WornOutlet;
            if(art.enabled)art.color=new Color(.34f,.28f,.31f);
            signal.enabled=false;
            if(outlet)outlet.SetEquipmentPower(false);
        }

        internal void Advance(ElectricPlugCombat owner,float dt)
        {
            if(!art || broken)return;
            if(returning>0)
            {
                returning-=dt;
                if(returning<=0)ResetFixture();
                return;
            }
            timer=Mathf.Max(0,timer-dt);
            if(armed)
            {
                float brightness=.45f+.5f*Mathf.Abs(Mathf.Sin(timer*31));
                signal.color=new Color(1,brightness*.48f,.13f,brightness);
                if(timer<=0)
                {
                    GoDark();
                    owner.FixtureBurst(Point,blastRadius,kind==ElectricFixtureKind.BombBulb?blastDamage:blastDamage*.45f);
                }
                return;
            }
            if(timer<=0)signal.enabled=lit;
            if(kind==ElectricFixtureKind.CableSnake)
            {
                contactReady=Mathf.Max(0,contactReady-dt);
                stun=Mathf.Max(0,stun-dt);
                if(stun>0){art.color=new Color(.57f,.67f,1);return;}
                art.color=Color.white;
                activeAge+=dt;
                transform.localPosition=home+Vector3.right*Mathf.Sin(activeAge*2.2f)*1.15f;
                art.flipX=Mathf.Cos(activeAge*2.2f)<0;
                if(contactReady<=0 && (Point-(Vector2)owner.transform.position).sqrMagnitude<.70f)
                {
                    owner.CableContact(Point);
                    contactReady=1.1f;
                }
            }
        }
    }
}
