using UnityEngine;
namespace Plugin.AncientCivilization
{
    public sealed class AncientWireObject : MonoBehaviour
    {
        enum Mode { Rest,Orbit,Flying,Hostile,Caught,Spent }
        AncientGame game;AncientLivingDistrict district;AncientLivingBoss boss;SpriteRenderer art;
        LineRenderer core,helix,guide;Mode mode;Vector2 home,velocity,locked;float age,respawn,orbitStart;bool rocket,car,doubleRocket,rethrown;int reflections;
        public bool Available {get{return mode==Mode.Spent;}}
        public bool Orbiting {get{return mode==Mode.Orbit;}}
        public bool Selectable {get{return mode==Mode.Rest||mode==Mode.Hostile;}}
        public Vector2 Point {get{return transform.position;}}
        public void Initialize(AncientGame world,AncientLivingDistrict area,Vector2 point,bool vehicle,bool rocketSlot=false)
        {
            game=world;district=area;home=point;car=vehicle;rocket=rocketSlot;transform.position=point;
            art=AncientArt.Display(rocket?"Wire counter rocket fist":car?"Throwable abandoned car":"Throwable cargo crate",AncientLivingArt.Part(rocket?2:car?4:5),Vector2.zero,9,transform);
            core=game.Line("Thrown object straight wire core",Color.white,.025f,18);core.transform.SetParent(transform,true);
            helix=game.Line("Thrown object double helix",AncientGame.Cyan,.045f,19);helix.positionCount=32;helix.transform.SetParent(transform,true);
            guide=game.Line("Throw release direction",new Color(.3f,.85f,1,.5f),.035f,17);guide.transform.SetParent(transform,true);
            core.enabled=helix.enabled=guide.enabled=false;mode=rocket?Mode.Spent:Mode.Rest;art.enabled=!rocket;
        }
        public void LaunchRocket(AncientLivingBoss source,Vector2 point,bool pair)
        {boss=source;doubleRocket=pair;rethrown=false;reflections=0;age=0;mode=Mode.Hostile;transform.position=point;velocity=((Vector2)game.hero.transform.position-point).normalized*8;art.enabled=true;art.color=Color.white;transform.rotation=Quaternion.identity;}
        public void ResetObject()
        {if(Orbiting)district.ReleaseObject(this);mode=rocket?Mode.Spent:Mode.Rest;transform.position=home;transform.rotation=Quaternion.identity;art.enabled=!rocket;core.enabled=helix.enabled=guide.enabled=false;age=0;}
        public void Grab()
        {
            if(!Selectable)return;mode=Mode.Orbit;age=0;orbitStart=Mathf.Atan2(Point.y-game.hero.transform.position.y-1.8f,Point.x-game.hero.transform.position.x);
            if(rocket)reflections++;game.hero.ReleaseWire();game.hero.Combat.CancelAttacks();game.Sound(3);
        }
        public void BreakByBlade()
        {if(mode==Mode.Rest)Shatter();}
        void Shatter()
        {
            if(Orbiting)district.ReleaseObject(this);mode=Mode.Spent;respawn=Time.time+6;art.enabled=false;core.enabled=helix.enabled=guide.enabled=false;
            game.hero.Combat.EmitParticles(Point,rocket?26:car?32:18);game.Impact(Point,AncientGame.Cyan,true);game.Sound(6);
        }
        void Update()
        {
            if(!game||!game.hero||game.Paused)return;
            if(game.Story.CurrentFloor!=1){art.enabled=false;core.enabled=helix.enabled=guide.enabled=false;return;}
            if(mode==Mode.Spent){if(!rocket&&Time.time>=respawn)ResetObject();return;}
            art.enabled=Mathf.Abs(Point.x-game.hero.transform.position.x)<65;
            if(mode==Mode.Rest)return;
            float dt=Time.deltaTime;age+=dt;
            if(mode==Mode.Orbit)
            {
                float t=Mathf.Clamp01(age/AncientLivingRules.OrbitDuration),a=orbitStart+t*Mathf.PI*2;
                Vector2 centre=(Vector2)game.hero.transform.position+Vector2.up*1.8f,next=centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*1.9f;
                var wall=Physics2D.Linecast(centre,next,1<<8);transform.position=wall?wall.point+wall.normal*.3f:next;
                transform.rotation=Quaternion.Euler(0,0,t*360);DrawWire();
                Vector2 aim=(Vector2)game.View.ScreenToWorldPoint(Input.mousePosition)-Point;if(aim.sqrMagnitude<.01f)aim=Vector2.right*game.hero.Facing;
                guide.enabled=true;guide.SetPosition(0,Point);guide.SetPosition(1,Point+aim.normalized*4);
                if(t>=1)
                {velocity=aim.normalized*(rocket?26:22);mode=Mode.Flying;age=0;core.enabled=helix.enabled=guide.enabled=false;district.ReleaseObject(this);district.RecordThrow();game.Sound(4);}
                return;
            }
            if(mode==Mode.Caught)
            {
                if(!boss||!boss.Alive){Shatter();return;}
                transform.position=boss.Core+Vector2.right*Mathf.Sign(locked.x-boss.Core.x)*2.4f;
                guide.enabled=true;guide.SetPosition(0,Point);guide.SetPosition(1,locked);
                if(age>=.7f){velocity=(locked-Point).normalized*25;rethrown=true;mode=Mode.Hostile;age=0;guide.enabled=false;}
                return;
            }
            Vector2 previous=Point;
            if(mode==Mode.Hostile&&!rethrown)
            {Vector2 desired=((Vector2)game.hero.transform.position-previous).normalized*10;velocity=Vector2.MoveTowards(velocity,desired,10*dt);}
            Vector2 step=velocity*dt,end=previous+step;var terrain=Physics2D.Linecast(previous,end,1<<8);
            float first=terrain?terrain.fraction:1;
            if(mode==Mode.Flying)
            {
                AncientEnemy nearest=null;float routeContact=0;bool routeFirst=game.LowerRoute&&game.LowerRoute.TryThrownContact(previous,end,out routeContact);
                if(routeFirst&&routeContact<first)first=routeContact;else routeFirst=false;
                for(int i=game.enemies.Count-1;i>=0;i--)
                {
                    var enemy=game.enemies[i];if(!enemy||!enemy.IsAlive)continue;
                    float radius=enemy.IsLivingBoss?2.1f:car?1.25f:.8f,contact;Vector2 target=enemy.AimPoint,offset=target-previous;
                    if(!AncientProjectileRules.CircleEntry(step.x,step.y,offset.x,offset.y,radius,out contact)||contact>=first||Physics2D.Linecast(previous,target,1<<8))continue;
                    first=contact;nearest=enemy;routeFirst=false;
                }
                if(routeFirst&&game.LowerRoute.HitThrown(previous,end))
                {transform.position=Vector2.Lerp(previous,end,first);Shatter();return;}
                if(nearest)
                {
                    transform.position=Vector2.Lerp(previous,end,first);
                    if(nearest.IsLivingBoss&&boss&&doubleRocket&&reflections==1&&boss.CanCatchRocket)
                    {mode=Mode.Caught;age=0;locked=game.hero.transform.position;boss.ShowCatch();return;}
                    if(nearest.IsLivingBoss&&game.Living.Boss)game.Living.Boss.ReceiveHit(AncientLivingHit.Throw,rocket?68:car?52:36);
                    else nearest.Hit(car?65:42,true);
                    Shatter();return;
                }
            }
            else
            {
                Vector2 player=game.hero.transform.position,offset=player-previous;float contact;
                if(AncientProjectileRules.CircleEntry(step.x,step.y,offset.x,offset.y,.9f,out contact)&&contact<first&&!Physics2D.Linecast(previous,player,1<<8))
                {game.hero.Hurt(24);transform.position=Vector2.Lerp(previous,end,contact);Shatter();return;}
            }
            transform.position=terrain?terrain.point:end;transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg);
            if(terrain||age>7)Shatter();
        }
        void LateUpdate(){if(Orbiting&&game&&!game.Paused)game.hero.CinematicPose(false,game.hero.Facing);}
        void DrawWire()
        {
            Vector2 start=game.hero.WireOrigin,end=Point,d=end-start,normal=new Vector2(-d.y,d.x).normalized;
            core.enabled=helix.enabled=true;core.SetPosition(0,start);core.SetPosition(1,end);
            for(int i=0;i<32;i++)
            {bool back=i>=16;float t=(back?31-i:i)/15f;Vector2 p=Vector2.Lerp(start,end,t)+normal*Mathf.Sin(t*d.magnitude*8+Time.time*18)*(back?-.1f:.1f);helix.SetPosition(i,p);}
        }
    }
}
