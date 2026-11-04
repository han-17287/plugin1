using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientCombat : MonoBehaviour
    {
        public const float TapThreshold=AncientRules.TapThreshold,ReadyDelay=AncientRules.ReadyDelay;
        public int BladeUses {get;private set;}
        public int BrokenObjects {get;private set;}
        public int CollectedParticles {get;private set;}
        public int LaserShots {get;private set;}
        public float ChargeProgress {get{return heldAt<0?0:Mathf.Clamp01((Time.time-heldAt-TapThreshold)/(ReadyDelay-TapThreshold));}}
        public bool Charging {get{return heldAt>=0&&Time.time-heldAt>=TapThreshold;}}
        public float ArmActivity {get{return Charging?ChargeProgress:Mathf.Clamp01((laserUntil-Time.time)/.18f);}}
        public bool CanCollect {get{return hero&&AncientRules.CanCollectParticles(Input.GetMouseButton(1),Application.isFocused,game.Paused,game.GameplayInputReady,hero.CaptureEnergy);}}
        AncientHero hero;AncientGame game;LineRenderer blade,bladeGlow,bladeSpine,bladeEdge,aim,beam,beamGlow;
        AncientEnergyMote[] motes;int moteCursor,combo;float heldAt=-1,bladeUntil,laserUntil,bladeReady,comboUntil;
        Vector2 bladeDirection,beamStart,beamEnd;
        static readonly Color Yellow=new Color(1,.88f,.18f);
        public void Initialize(AncientHero owner)
        {
            hero=owner;game=AncientGame.Instance;
            blade=game.Line("Yellow filament blade",new Color(1,.75f,.08f),.36f,22);
            bladeGlow=game.Line("Yellow bulb blade glow",new Color(1,.8f,.05f,.28f),.6f,21);
            bladeSpine=game.Line("Graphite sword spine",new Color(.13f,.16f,.19f),.1f,23);
            bladeEdge=game.Line("Incandescent sword bevel",new Color(1,1,.65f),.045f,24);
            foreach(var layer in new[]{blade,bladeGlow,bladeSpine,bladeEdge})
            {
                layer.positionCount=5;layer.numCapVertices=0;layer.numCornerVertices=0;
                layer.widthCurve=new AnimationCurve(new Keyframe(0,.55f),new Keyframe(.12f,1),new Keyframe(.78f,.8f),new Keyframe(1,0));
            }
            aim=game.Line("Laser aiming guide",new Color(.4f,1,.8f,.4f),.025f,21);
            beam=game.Line("Captured particle laser",new Color(.8f,1,.9f),.1f,22);
            beamGlow=game.Line("Laser energy sheath",new Color(.15f,1,.7f,.35f),.4f,21);
            blade.enabled=bladeGlow.enabled=bladeSpine.enabled=bladeEdge.enabled=aim.enabled=beam.enabled=beamGlow.enabled=false;
            motes=new AncientEnergyMote[48];
            for(int i=0;i<motes.Length;i++)
            {
                var o=game.Box("Reusable captured energy mote",Vector2.zero,new Vector2(.1f,.1f),Yellow,20);
                motes[i]=o.AddComponent<AncientEnergyMote>();motes[i].Initialize(this,hero);o.SetActive(false);
            }
        }
        Vector2 Origin {get{return (Vector2)hero.transform.position+new Vector2(hero.Facing*.5f,.35f);}}
        Vector2 AimPoint()
        {
            if(!game.View)return Origin+Vector2.right*hero.Facing*10;
            return game.View.ScreenToWorldPoint(Input.mousePosition);
        }
        public void CancelInput(){heldAt=-1;if(aim)aim.enabled=false;}
        public void HandleInput()
        {
            if(!hero)return;
            if(game.Paused||game.won||!game.GameplayInputReady||!hero.AttackMode)
            {heldAt=-1;aim.enabled=false;return;}
            if(Input.GetMouseButtonDown(0)){heldAt=Time.time;}
            if(Input.GetMouseButtonUp(0)&&heldAt>=0)
            {
                float held=Time.time-heldAt;heldAt=-1;aim.enabled=false;
                var action=AncientRules.ResolveLeftRelease(held,hero.CaptureEnergy);
                if(action==LeftReleaseAction.Blade)StrikeBlade();
                else if(action==LeftReleaseAction.Laser||action==LeftReleaseAction.Empty)FireLaser();
                else game.Notice("Laser cancelled / hold until the aiming guide turns gold.",1.2f);
            }
            if(Charging)
            {
                Vector2 start=Origin,end=ResolveBeam(start,AimPoint());aim.enabled=true;
                aim.startColor=aim.endColor=ChargeProgress>=1?Yellow:new Color(.25f,1,.7f,.45f);
                aim.SetPosition(0,start);aim.SetPosition(1,end);
            }
        }
        public void StrikeBlade()
        {
            if(Time.time<bladeReady)return;
            bladeReady=Time.time+.28f;bladeUntil=Time.time+.2f;
            combo=Time.time>comboUntil?1:combo%3+1;comboUntil=Time.time+.8f;
            bladeDirection=(AimPoint()-Origin).normalized;if(bladeDirection.sqrMagnitude<.1f)bladeDirection=Vector2.right*hero.Facing;
            hero.BladePose(bladeDirection.x<0?-1:1,combo);BladeUses++;game.Sound(0);
            Vector2 origin=Origin;
            for(int i=game.shots.Count-1;i>=0;i--)
            {
                var shot=game.shots[i];if(!shot||shot.friendly)continue;
                Vector2 delta=(Vector2)shot.transform.position-origin;
                if(delta.sqrMagnitude<3.1f*3.1f&&Vector2.Dot(delta.normalized,bladeDirection)>-.15f&&!Physics2D.Linecast(origin,shot.transform.position,1<<8))
                {EmitParticles(shot.transform.position,Mathf.Clamp(shot.damage,8,25));shot.Remove();BrokenObjects++;game.Story.OnTraining(5);}
            }
            for(int i=game.enemies.Count-1;i>=0;i--)
            {
                var enemy=game.enemies[i];if(!enemy)continue;Vector2 delta=enemy.AimPoint-origin;
                if(delta.sqrMagnitude<3*3&&Vector2.Dot(delta.normalized,bladeDirection)>-.15f&&!Physics2D.Linecast(origin,enemy.AimPoint,1<<8))enemy.Hit(combo==3?42:28,combo==3);
            }
            game.Burst(origin+bladeDirection*1.5f,Yellow,6);
        }
        public void EmitParticles(Vector2 position,float energy)
        {
            for(int i=0;i<3;i++)motes[moteCursor++%motes.Length].Play(position,energy/3,new Vector2((i-1)*2,2+i*.7f));
            game.Burst(position,Yellow,7);
        }
        public bool TryCollectionTarget(out Vector2 point)
        {
            point=Vector2.zero;if(!CanCollect)return false;float best=49;bool found=false;
            foreach(var mote in motes)
            {
                if(!mote.gameObject.activeSelf)continue;
                float distance=((Vector2)mote.transform.position-(Vector2)hero.transform.position).sqrMagnitude;
                if(distance>=best||Physics2D.Linecast(hero.transform.position,mote.transform.position,1<<8))continue;
                best=distance;point=mote.transform.position;found=true;
            }
            return found;
        }
        public void Receive(float energy)
        {hero.AddCapturedEnergy(energy);CollectedParticles++;game.Story.OnTraining(6);game.Sound(2);}
        Vector2 ResolveBeam(Vector2 origin,Vector2 target)
        {
            Vector2 delta=target-origin;float range=Mathf.Clamp(delta.magnitude,.5f,24);
            Vector2 direction=delta.sqrMagnitude>.001f?delta.normalized:Vector2.right*hero.Facing;
            var hit=Physics2D.Raycast(origin,direction,range,1<<8);return hit?hit.point:origin+direction*range;
        }
        public void FireLaser()
        {
            if(hero.CaptureEnergy<=0){game.Notice("No captured particles. Break a thrown object with a tap, then collect its particles.",2);return;}
            float energy=hero.SpendCapturedEnergy(),power=energy/100f;
            beamStart=Origin;beamEnd=ResolveBeam(beamStart,AimPoint());Vector2 delta=beamEnd-beamStart;
            float radius=.25f+power*.4f,damage=30+energy*1.6f;
            for(int i=game.enemies.Count-1;i>=0;i--)
            {
                var enemy=game.enemies[i];if(!enemy)continue;
                float t=delta.sqrMagnitude>.001f?Mathf.Clamp01(Vector2.Dot(enemy.AimPoint-beamStart,delta)/delta.sqrMagnitude):0;
                if(Vector2.Distance(enemy.AimPoint,beamStart+delta*t)<radius+.6f)enemy.Hit(damage,true);
            }
            if(game.Story.Truck)game.Story.Truck.HitByLaser(beamStart,beamEnd,damage);
            laserUntil=Time.time+.18f;beam.startWidth=beam.endWidth=.08f+power*.3f;
            beamGlow.startWidth=beamGlow.endWidth=.3f+power*.7f;
            LaserShots++;game.Story.OnTraining(7);game.Sound(4);game.Shake(.08f+power*.1f);game.Burst(beamEnd,new Color(.3f,1,.65f),10+Mathf.RoundToInt(power*20));
        }
        void LateUpdate()
        {
            if(!hero)return;bool slashing=Time.time<bladeUntil&&!game.won;
            blade.enabled=bladeGlow.enabled=bladeSpine.enabled=bladeEdge.enabled=slashing;
            if(slashing)
            {
                float phase=Mathf.Clamp01((bladeUntil-Time.time)/.2f),angle=Mathf.Atan2(bladeDirection.y,bladeDirection.x)+Mathf.Lerp(.8f,-.8f,phase);
                Vector2 tip=Origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*2.6f;
                Vector2 normal=Vector2.Perpendicular(tip-Origin).normalized;
                for(int i=0;i<5;i++)
                {
                    float t=i/4f;Vector2 point=Vector2.Lerp(Origin,tip,t);
                    blade.SetPosition(i,point);bladeGlow.SetPosition(i,point);bladeSpine.SetPosition(i,point);
                    bladeEdge.SetPosition(i,point+normal*(.12f*(1-t)));
                }
            }
            bool firing=Time.time<laserUntil;beam.enabled=beamGlow.enabled=firing;
            if(firing){beam.SetPosition(0,beamStart);beam.SetPosition(1,beamEnd);beamGlow.SetPosition(0,beamStart);beamGlow.SetPosition(1,beamEnd);}
        }
        void OnApplicationFocus(bool focus){if(!focus)CancelInput();}
        public void ResetCombat()
        {heldAt=-1;bladeUntil=laserUntil=0;blade.enabled=bladeGlow.enabled=bladeSpine.enabled=bladeEdge.enabled=beam.enabled=beamGlow.enabled=aim.enabled=false;foreach(var mote in motes)mote.gameObject.SetActive(false);}
        void OnDestroy()
        {
            foreach(var line in new[]{blade,bladeGlow,bladeSpine,bladeEdge,aim,beam,beamGlow})if(line)Destroy(line.gameObject);
            if(motes!=null)foreach(var mote in motes)if(mote)Destroy(mote.gameObject);
        }
    }
    public sealed class AncientEnergyMote : MonoBehaviour
    {
        AncientCombat combat;AncientHero hero;Vector2 velocity;float energy,life;SpriteRenderer art;
        public void Initialize(AncientCombat owner,AncientHero player){combat=owner;hero=player;art=GetComponent<SpriteRenderer>();}
        public void Play(Vector2 position,float amount,Vector2 motion)
        {transform.position=position;energy=amount;velocity=motion;life=8;art.color=new Color(1,.9f,.2f);gameObject.SetActive(true);}
        void Update()
        {
            var g=AncientGame.Instance;if(g.Paused)return;
            life-=Time.deltaTime;if(life<=0||g.won){gameObject.SetActive(false);return;}
            Vector2 delta=(Vector2)hero.transform.position-(Vector2)transform.position;
            if(combat.CanCollect&&delta.sqrMagnitude<49&&!Physics2D.Linecast(transform.position,hero.transform.position,1<<8))
            {velocity=delta.normalized*22;if(delta.sqrMagnitude<.45f){combat.Receive(energy);gameObject.SetActive(false);return;}}
            else velocity=Vector2.Lerp(velocity,Vector2.zero,Time.deltaTime*3);
            Vector2 next=(Vector2)transform.position+velocity*Time.deltaTime;
            if(velocity.sqrMagnitude>.01f)
            {
                var hit=Physics2D.Linecast(transform.position,next,1<<8);
                if(hit){next=hit.point+hit.normal*.06f;velocity=Vector2.Reflect(velocity,hit.normal)*.25f;}
            }
            transform.position=next;transform.Rotate(0,0,180*Time.deltaTime);
        }
    }
}
