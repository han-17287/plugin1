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
        public int BladeStep {get{return combo;}}
        public bool BladeAnimating {get{return hero&&Time.time<bladeUntil;}}
        public bool BladeBuffered {get{return bladeBufferedUntil>Time.time;}}
        public int NextBladeStep {get{return AncientBladeRules.NextStep(combo,bladeReady,Time.time);}}
        public bool Firing {get{return Time.time<laserUntil;}}
        public float ChargeProgress {get{return heldAt<0?0:Mathf.Clamp01((Time.time-heldAt-TapThreshold)/(ReadyDelay-TapThreshold));}}
        public bool Charging {get{return heldAt>=0&&Time.time-heldAt>=TapThreshold;}}
        public float ArmActivity {get{return Charging?ChargeProgress:Mathf.Clamp01((laserUntil-Time.time)/.18f);}}
        public bool CanCollect {get{return hero&&!(game.Sniper&&game.Sniper.InputLocked)&&AncientRules.CanCollectParticles(Input.GetMouseButton(1),Application.isFocused,game.Paused,game.GameplayInputReady,hero.CaptureEnergy);}}
        AncientBeamVisuals beamProfile;AncientMoteVisuals moteVisuals;
        AncientHero hero;AncientGame game;LineRenderer slashTrail,slashGlow,aim,beam,beamGlow,beamCore;
        AncientEnergyMote[] motes;int moteCursor,combo;float heldAt=-1,bladeUntil,bladeStarted,laserUntil,bladeReady,bladeBufferedUntil;bool bladeHitPending,bladeRebounded,readySignal;
        Vector2 bladeDirection,bufferedDirection,beamStart,beamEnd;
        readonly Vector3[] slashPoints=new Vector3[9];
        static readonly Color Yellow=new Color(1,.88f,.18f);
        public void Initialize(AncientHero owner)
        {
            hero=owner;game=AncientGame.Instance;
            // Each cut has sixteen authored poses. These lines only show the blade's swept light.
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
            moteVisuals=new AncientMoteVisuals();
            for(int i=0;i<motes.Length;i++)
            {
                var o=game.Box("Reusable blue energy mote",Vector2.zero,Vector2.one,Color.white,20);
                motes[i]=o.AddComponent<AncientEnergyMote>();motes[i].Initialize(this,hero,moteVisuals.Attach(o.GetComponent<SpriteRenderer>(),i));o.SetActive(false);
            }
        }
        Vector2 Origin {get{return (Vector2)hero.transform.position+new Vector2(hero.Facing*.14f,.55f);}}
        Vector2 BladeOrigin {get{return (Vector2)hero.transform.position+new Vector2((bladeDirection.x<0?-1:1)*.14f,.55f);}}
        Vector2 AimPoint()
        {
            if(!game.View)return Origin+Vector2.right*hero.Facing*10;
            return game.View.ScreenToWorldPoint(Input.mousePosition);
        }
        bool ControlsReady {get{return hero&&game&&!game.Paused&&!game.won&&game.GameplayInputReady&&!(game.Sniper&&game.Sniper.InputLocked)&&!hero.MeleeInterrupted;}}
        public void CancelInput(){heldAt=-1;readySignal=false;bladeBufferedUntil=0;if(aim)aim.enabled=false;}
        public void CancelAttacks()
        {CancelInput();CancelBlade();laserUntil=0;if(beam)beam.enabled=false;if(beamGlow)beamGlow.enabled=false;if(beamCore)beamCore.enabled=false;}
        public void CancelBlade()
        {
            bladeHitPending=bladeRebounded=false;bladeUntil=bladeBufferedUntil=bladeReady=0;combo=0;
            if(slashTrail)slashTrail.enabled=false;if(slashGlow)slashGlow.enabled=false;
            if(hero)hero.CancelBladePose();
        }
        public void HandleInput()
        {
            if(!hero)return;
            if(!ControlsReady){CancelInput();CancelBlade();return;}
            if(!hero.AttackMode)
            {CancelInput();CancelBlade();return;}
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
                // A deliberate laser hold replaces a queued tap rather than releasing both attacks.
                bladeBufferedUntil=0;
                hero.AimWeaponAt(AimPoint());Vector2 start=Origin,end=ResolveBeam(start,AimPoint());aim.enabled=true;
                if(ChargeProgress>=1&&!readySignal){readySignal=true;game.Sound(3);game.Feel.Signal("레이저 준비 완료",Yellow,.5f);}
                aim.startColor=aim.endColor=ChargeProgress>=1?Yellow:new Color(.25f,1,.7f,.45f);
                aim.SetPosition(0,start);aim.SetPosition(1,end);
            }
        }
        public void StrikeBlade()
        {
            if(!ControlsReady||!hero.AttackMode||hero.Wired||Charging||Firing)return;
            Vector2 direction=(AimPoint()-Origin).normalized;if(direction.sqrMagnitude<.1f)direction=Vector2.right*hero.Facing;
            if(Time.time<bladeReady)
            {
                if(AncientBladeRules.CanBuffer(bladeReady-Time.time))
                {bladeBufferedUntil=Time.time+AncientBladeRules.BufferLifetime;bufferedDirection=direction;}
                return;
            }
            StartBlade(direction);
        }
        void StartBlade(Vector2 direction)
        {
            combo=AncientBladeRules.NextStep(combo,bladeReady,Time.time);
            bladeStarted=Time.time;bladeReady=Time.time+AncientBladeRules.Duration(combo);bladeUntil=bladeReady;bladeHitPending=true;bladeRebounded=false;bladeBufferedUntil=0;
            bladeDirection=direction;
            hero.BladePose(bladeDirection.x<0?-1:1,combo);BladeUses++;game.Sound(8+combo);
        }
        void Update()
        {
            if(!hero)return;
            if(!ControlsReady){CancelInput();CancelBlade();return;}
            if(!hero.AttackMode){CancelInput();CancelBlade();return;}
            if(Charging)bladeBufferedUntil=0;
            // Incoming physical objects can meet any visible part of the cut, including after contact.
            // Enemy and interactive-target damage still resolves once at its authored contact pose.
            if(!hero.Wired&&Time.time<bladeUntil&&AncientBladeRules.IsActive(combo,Time.time-bladeStarted))ResolvePhysicalBreaks();
            if(bladeHitPending&&Time.time>=bladeStarted+AncientBladeRules.ImpactTime(combo))
            {
                bladeHitPending=false;
                if(hero.AttackMode&&!hero.Wired&&AncientBladeRules.IsActive(combo,Time.time-bladeStarted))ResolveBladeHit();
            }
            if(!hero.Wired&&!Charging&&!Firing&&Time.time>=bladeReady&&Time.time<bladeBufferedUntil)
            {Vector2 direction=bufferedDirection;bladeBufferedUntil=0;StartBlade(direction);}
        }
        void ResolveBladeHit()
        {
            Vector2 origin=BladeOrigin;bool rebound=(game.Sniper&&game.Sniper.TryBlade(origin,bladeDirection))||(game.Campaign&&game.Campaign.TryBladeTarget(origin,bladeDirection))||(game.Cases&&game.Cases.TryBladeRelay(origin,bladeDirection))||(game.Traversal&&game.Traversal.TryBladeTarget(origin,bladeDirection))||(game.City&&game.City.TryBladeTarget(origin,bladeDirection));
            for(int i=game.enemies.Count-1;i>=0;i--)
            {
                var enemy=game.enemies[i];if(!enemy)continue;Vector2 delta=enemy.AimPoint-origin;
                if(delta.sqrMagnitude<3*3&&Vector2.Dot(delta.normalized,bladeDirection)>-.15f&&!Physics2D.Linecast(origin,enemy.AimPoint,1<<8)){if(enemy.kind==4)rebound=true;enemy.Hit(combo==3?42:28,combo==3);}
            }
            if(rebound)ReboundOnce();
            game.Burst(origin+bladeDirection*1.5f,Yellow,6);
        }
        void ReboundOnce(){if(bladeRebounded||hero.bike||hero.IsOnGround)return;bladeRebounded=true;hero.BladeRebound();}
        void ResolvePhysicalBreaks()
        {
            Vector2 origin=BladeOrigin;bool rebound=(game.Sniper&&game.Sniper.TryBreakObjects(origin,bladeDirection))||(game.City&&game.City.TryBreakObjects(origin,bladeDirection));
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
            if(rebound)ReboundOnce();
        }
        public void EmitParticles(Vector2 position,float energy)
        {
            for(int i=0;i<3;i++)motes[moteCursor++%motes.Length].Play(position,energy/3,new Vector2((i-1)*2,2+i*.7f));
            game.Burst(position,AncientMoteVisuals.Blue,7);
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
            if(!ControlsReady||!hero.AttackMode||hero.Wired)return;
            if(hero.CaptureEnergy<=0){game.Notice("입자가 없습니다. 물체를 베고 나온 입자를 우클릭으로 수집하세요.",2);return;}
            CancelBlade();
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
            bool sweeping=Time.time<bladeUntil&&AncientBladeRules.IsActive(combo,elapsed)&&!game.Paused&&!game.won;
            slashTrail.enabled=slashGlow.enabled=sweeping;
            if(sweeping)
            {
                float progress=AncientBladeRules.SweepProgress(combo,elapsed);
                for(int i=0;i<9;i++)
                {
                    float t=Mathf.Lerp(Mathf.Max(0,progress-.45f),progress,i/8f);
                    slashPoints[i]=hero.BladeTipWorld(AncientBladeRules.TrailFrame(t));
                }
                slashTrail.SetPositions(slashPoints);slashGlow.SetPositions(slashPoints);
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
        void OnApplicationFocus(bool focus){if(!focus){CancelInput();CancelBlade();}}
        public void ResetCombat()
        {CancelInput();laserUntil=0;CancelBlade();slashTrail.enabled=slashGlow.enabled=beam.enabled=beamGlow.enabled=beamCore.enabled=aim.enabled=false;foreach(var mote in motes)mote.gameObject.SetActive(false);}
        void OnDestroy()
        {
            if(beamProfile!=null)beamProfile.Dispose();
            if(moteVisuals!=null)moteVisuals.Dispose();
            foreach(var line in new[]{slashTrail,slashGlow,aim,beam,beamGlow,beamCore})if(line)Destroy(line.gameObject);
            if(motes!=null)foreach(var mote in motes)if(mote)Destroy(mote.gameObject);
        }
    }
    public sealed class AncientEnergyMote : MonoBehaviour
    {
        AncientCombat combat;AncientHero hero;Vector2 velocity;float energy,life;AncientMoteVisuals.Mote visual;
        public void Initialize(AncientCombat owner,AncientHero player,AncientMoteVisuals.Mote presentation){combat=owner;hero=player;visual=presentation;}
        public void Play(Vector2 position,float amount,Vector2 motion)
        {transform.position=position;energy=amount;velocity=motion;life=8;visual.Play();gameObject.SetActive(true);}
        void Update()
        {
            var g=AncientGame.Instance;if(g.Paused)return;
            life-=Time.deltaTime;if(life<=0||g.won){gameObject.SetActive(false);return;}
            Vector2 delta=(Vector2)hero.transform.position-(Vector2)transform.position;
            bool collecting=combat.CanCollect&&delta.sqrMagnitude<49&&!Physics2D.Linecast(transform.position,hero.transform.position,1<<8);
            if(collecting)
            {velocity=delta.normalized*22;if(delta.sqrMagnitude<.45f){combat.Receive(energy);gameObject.SetActive(false);return;}}
            else velocity=Vector2.Lerp(velocity,Vector2.zero,Time.deltaTime*3);
            Vector2 next=(Vector2)transform.position+velocity*Time.deltaTime;
            if(velocity.sqrMagnitude>.01f)
            {
                var hit=Physics2D.Linecast(transform.position,next,1<<8);
                if(hit){next=hit.point+hit.normal*.06f;velocity=Vector2.Reflect(velocity,hit.normal)*.25f;}
            }
            transform.position=next;transform.Rotate(0,0,180*Time.deltaTime);visual.Tick(life,8-life,collecting);
        }
    }
}
