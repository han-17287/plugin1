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
        public bool Firing {get{return Time.time<laserUntil;}}
        public float ChargeProgress {get{return heldAt<0?0:Mathf.Clamp01((Time.time-heldAt-TapThreshold)/(ReadyDelay-TapThreshold));}}
        public bool Charging {get{return heldAt>=0&&Time.time-heldAt>=TapThreshold;}}
        public float ArmActivity {get{return Charging?ChargeProgress:Mathf.Clamp01((laserUntil-Time.time)/.18f);}}
        public bool CanCollect {get{return hero&&AncientRules.CanCollectParticles(Input.GetMouseButton(1),Application.isFocused,game.Paused,game.GameplayInputReady,hero.CaptureEnergy);}}
        AncientBeamVisuals beamProfile;
        AncientHero hero;AncientGame game;LineRenderer slashTrail,slashGlow,aim,beam,beamGlow,beamCore;
        AncientEnergyMote[] motes;int moteCursor,combo;float heldAt=-1,bladeUntil,bladeStarted,laserUntil,bladeReady,comboUntil,bladeBufferedUntil;bool bladeHitPending,readySignal;
        Vector2 bladeDirection,beamStart,beamEnd;
        static readonly Color Yellow=new Color(1,.88f,.18f);
        public void Initialize(AncientHero owner)
        {
            hero=owner;game=AncientGame.Instance;
            // The physical blade is in the eight animation frames; these lines only show its swept arc.
            slashTrail=game.Line("Blade swept light",new Color(1,.9f,.25f,.45f),.07f,23);
            slashGlow=game.Line("Blade swept light glow",new Color(1,.8f,.1f,.12f),.24f,22);
            slashTrail.positionCount=slashGlow.positionCount=9;
            aim=game.Line("Laser aiming guide",new Color(.4f,.9f,1,.35f),.02f,21);
            beam=game.Line("Instant continuous laser beam",new Color(.55f,.9f,1,.95f),.18f,23);
            beamGlow=game.Line("Continuous laser sheath",new Color(.1f,.6f,1,.45f),.8f,22);
            beamProfile=new AncientBeamVisuals(beamGlow);
            beamCore=game.Line("White hot laser core",new Color(1,1,.95f),.07f,24);
            slashTrail.enabled=slashGlow.enabled=aim.enabled=beam.enabled=beamGlow.enabled=beamCore.enabled=false;
            motes=new AncientEnergyMote[48];
            for(int i=0;i<motes.Length;i++)
            {
                var o=game.Box("Reusable captured energy mote",Vector2.zero,new Vector2(.1f,.1f),Yellow,20);
                motes[i]=o.AddComponent<AncientEnergyMote>();motes[i].Initialize(this,hero);o.SetActive(false);
            }
        }
        Vector2 Origin {get{return (Vector2)hero.transform.position+new Vector2(hero.Facing*.14f,.55f);}}
        Vector2 AimPoint()
        {
            if(!game.View)return Origin+Vector2.right*hero.Facing*10;
            return game.View.ScreenToWorldPoint(Input.mousePosition);
        }
        public void CancelInput(){heldAt=-1;if(aim)aim.enabled=false;}
        public void CancelBlade(){bladeHitPending=false;bladeUntil=bladeBufferedUntil=0;if(hero)hero.CancelBladePose();}
        public void HandleInput()
        {
            if(!hero)return;
            if(game.Paused||game.won||!game.GameplayInputReady)
            {heldAt=-1;aim.enabled=false;return;}
            if(!hero.AttackMode)
            {heldAt=-1;aim.enabled=false;bladeHitPending=false;bladeUntil=bladeBufferedUntil=0;hero.CancelBladePose();return;}
            if(Input.GetMouseButtonDown(0)){heldAt=Time.time;readySignal=false;}
            if(Input.GetMouseButtonUp(0)&&heldAt>=0)
            {
                float held=Time.time-heldAt;heldAt=-1;aim.enabled=false;
                var action=AncientRules.ResolveLeftRelease(held,hero.CaptureEnergy);
                if(action==LeftReleaseAction.Blade)StrikeBlade();
                else if(action==LeftReleaseAction.Laser||action==LeftReleaseAction.Empty)FireLaser();
                else game.Notice("레이저 취소 / 조준선이 금색이 될 때까지 누르세요.",1.2f);
            }
            if(Charging)
            {
                hero.AimWeaponAt(AimPoint());Vector2 start=Origin,end=ResolveBeam(start,AimPoint());aim.enabled=true;
                if(ChargeProgress>=1&&!readySignal){readySignal=true;game.Sound(3);game.Feel.Signal("레이저 준비 완료",Yellow,.5f);}
                aim.startColor=aim.endColor=ChargeProgress>=1?Yellow:new Color(.25f,1,.7f,.45f);
                aim.SetPosition(0,start);aim.SetPosition(1,end);
            }
        }
        public void StrikeBlade()
        {
            if(Time.time<bladeReady){if(bladeReady-Time.time<.15f)bladeBufferedUntil=Time.time+.2f;return;}
            bladeStarted=Time.time;bladeReady=Time.time+AncientRules.BladeDuration;bladeUntil=bladeReady;bladeHitPending=true;
            combo=Time.time>comboUntil?1:combo%3+1;comboUntil=Time.time+.8f;
            bladeDirection=(AimPoint()-Origin).normalized;if(bladeDirection.sqrMagnitude<.1f)bladeDirection=Vector2.right*hero.Facing;
            hero.BladePose(bladeDirection.x<0?-1:1,combo);BladeUses++;game.Sound(0);
        }
        void Update()
        {
            if(!hero||game.Paused||game.won)return;
            if(bladeHitPending&&Time.time>=bladeStarted+AncientRules.BladeHitTime)
            {
                bladeHitPending=false;
                if(hero.AttackMode&&!hero.Wired)ResolveBladeHit();
            }
            if(hero.AttackMode&&Time.time>=bladeReady&&Time.time<bladeBufferedUntil)
            {bladeBufferedUntil=0;StrikeBlade();}
        }
        void ResolveBladeHit()
        {
            Vector2 origin=Origin;bool rebound=(game.Sniper&&game.Sniper.TryBlade(origin,bladeDirection))||(game.Campaign&&game.Campaign.TryBladeTarget(origin,bladeDirection))||(game.Cases&&game.Cases.TryBladeRelay(origin,bladeDirection))||(game.Traversal&&game.Traversal.TryBladeTarget(origin,bladeDirection))||(game.City&&game.City.TryBladeTarget(origin,bladeDirection));
            for(int i=game.shots.Count-1;i>=0;i--)
            {
                var shot=game.shots[i];if(!shot)continue;
                Vector2 delta=(Vector2)shot.transform.position-origin;
                if(delta.sqrMagnitude<3.1f*3.1f&&Vector2.Dot(delta.normalized,bladeDirection)>-.15f&&!Physics2D.Linecast(origin,shot.transform.position,1<<8))
                {
                    rebound=true;
                    bool perfect=AncientCombatRules.PerfectBreak(delta.sqrMagnitude,Vector2.Dot(delta.normalized,shot.velocity.normalized));
                    EmitParticles(shot.transform.position,Mathf.Clamp(shot.damage,8,25)*(perfect?1.25f:1));
                    if(perfect){game.Feel.Signal("정밀 파괴 · 에너지 +25%",Yellow,.7f);game.Sound(2);}
                    shot.Remove();BrokenObjects++;game.Story.OnTraining(5);
                }
            }
            for(int i=game.enemies.Count-1;i>=0;i--)
            {
                var enemy=game.enemies[i];if(!enemy)continue;Vector2 delta=enemy.AimPoint-origin;
                if(delta.sqrMagnitude<3*3&&Vector2.Dot(delta.normalized,bladeDirection)>-.15f&&!Physics2D.Linecast(origin,enemy.AimPoint,1<<8)){if(enemy.kind==4)rebound=true;enemy.Hit(combo==3?42:28,combo==3);}
            }
            if(rebound)hero.BladeRebound();
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
            if(hero.CaptureEnergy<=0){game.Notice("입자가 없습니다. 물체를 베고 나온 입자를 우클릭으로 수집하세요.",2);return;}
            float energy=hero.SpendCapturedEnergy(),power=energy/100f;
            hero.AimWeaponAt(AimPoint());beamStart=Origin;beamEnd=ResolveBeam(beamStart,AimPoint());Vector2 delta=beamEnd-beamStart;
            float radius=.25f+power*.4f,damage=30+energy*1.6f;
            for(int i=game.enemies.Count-1;i>=0;i--)
            {
                var enemy=game.enemies[i];if(!enemy)continue;
                float t=delta.sqrMagnitude>.001f?Mathf.Clamp01(Vector2.Dot(enemy.AimPoint-beamStart,delta)/delta.sqrMagnitude):0;
                if(Vector2.Distance(enemy.AimPoint,beamStart+delta*t)<radius+.6f&&!Physics2D.Linecast(beamStart,enemy.AimPoint,1<<8))enemy.Hit(damage,true);
            }
            if(game.Sniper)game.Sniper.OnLaser(beamStart,beamEnd);
            if(game.Campaign)game.Campaign.OnLaser(beamStart,beamEnd);
            if(game.Story.Truck)game.Story.Truck.HitByLaser(beamStart,beamEnd,damage);
            if(game.Finale)game.Finale.HitTruck(beamStart,beamEnd,damage);
            laserUntil=Time.time+.32f;beam.startWidth=beam.endWidth=.30f+power*.55f;
            beamCore.startWidth=beamCore.endWidth=.10f+power*.20f;
            beamGlow.startWidth=beamGlow.endWidth=.80f+power*.90f;
            LaserShots++;game.Story.OnTraining(7);game.Sound(4);game.Shake(.08f+power*.1f);game.Impact(beamEnd,new Color(.55f,.9f,1),power>.6f);
        }
        void LateUpdate()
        {
            if(!hero)return;
            float elapsed=Time.time-bladeStarted;
            bool sweeping=Time.time<bladeUntil&&elapsed>=AncientRules.BladeHitTime&&elapsed<.275f&&!game.won;
            slashTrail.enabled=slashGlow.enabled=sweeping;
            if(sweeping)
            {
                float progress=Mathf.Clamp01((elapsed-AncientRules.BladeHitTime)/.125f);
                float facing=bladeDirection.x<0?-1:1;
                for(int i=0;i<9;i++)
                {
                    float t=Mathf.Lerp(Mathf.Max(0,progress-.45f),progress,i/8f);
                    float angle=Mathf.Lerp(1.1f,-.95f,t);
                    Vector2 point=Origin+new Vector2(Mathf.Cos(angle)*facing,Mathf.Sin(angle))*2.2f;
                    slashTrail.SetPosition(i,point);slashGlow.SetPosition(i,point);
                }
            }
            bool firing=Firing&&!game.won;beam.enabled=beamGlow.enabled=beamCore.enabled=firing;
            if(firing)
            {
                // All three beam layers span the entire ray immediately. No flying projectile or particle trail.
                beam.SetPosition(0,beamStart);beam.SetPosition(1,beamEnd);
                beamGlow.SetPosition(0,beamStart);beamGlow.SetPosition(1,beamEnd);
                beamCore.SetPosition(0,beamStart);beamCore.SetPosition(1,beamEnd);
                float fade=Mathf.Clamp01((laserUntil-Time.time)/.1f);
                beam.startColor=beam.endColor=new Color(.45f,.92f,1,fade);
                beamGlow.startColor=beamGlow.endColor=new Color(.1f,.65f,1,.55f*fade);
                beamCore.startColor=beamCore.endColor=new Color(1,1,1,fade);
            }
        }
        void OnApplicationFocus(bool focus){if(!focus)CancelInput();}
        public void ResetCombat()
        {heldAt=-1;bladeReady=comboUntil=laserUntil=0;combo=0;readySignal=false;CancelBlade();slashTrail.enabled=slashGlow.enabled=beam.enabled=beamGlow.enabled=beamCore.enabled=aim.enabled=false;foreach(var mote in motes)mote.gameObject.SetActive(false);}
        void OnDestroy()
        {
            if(beamProfile!=null)beamProfile.Dispose();
            foreach(var line in new[]{slashTrail,slashGlow,aim,beam,beamGlow,beamCore})if(line)Destroy(line.gameObject);
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
