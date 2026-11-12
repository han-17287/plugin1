using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Modular presentation follows the hero's one Rigidbody2D; wheels do not add competing physics bodies.
    public sealed class AncientMotorcycle : MonoBehaviour
    {
        AncientHero hero;AncientGame game;AncientAudio audioSettings;
        Transform model,frame;
        SpriteRenderer chassis,rider,rear,front;
        TrailRenderer exhaust,exhaustCore;TrailRenderer[] skidTrails;Material skidMaterial;float nextSkidAt;LineRenderer headlight;
        Sprite normalBody,damagedBody;Material effects;AudioSource engine;AudioClip engineClip;
        MaterialPropertyBlock armProperties;
        static readonly int ArmCharge=Shader.PropertyToID("_ArmCharge"),ArmPulse=Shader.PropertyToID("_ArmPulse");
        float lastSpeed,wheelAngle,compression,sparkAt,flashUntil;
        float landingAge=1,landingStrength,airborneAge,fallingSpeed,riderLean,roadPhase;
        bool previousGrounded,lastProductionBlade,wasRunning;
        public void Initialize(AncientHero owner)
        {
            hero=owner;game=AncientGame.Instance;audioSettings=game.GetComponent<AncientAudio>();
            model=new GameObject("Modular particle motorcycle").transform;model.SetParent(transform,false);
            frame=new GameObject("Suspended chassis").transform;frame.SetParent(model,false);
            normalBody=AncientArt.Motorcycle(0);damagedBody=AncientArt.Motorcycle(3);
            chassis=AncientArt.Display("Reactor chassis",normalBody,new Vector2(0,-.18f),9,frame);
            rear=AncientArt.Display("Rear wheel",AncientArt.Motorcycle(1),new Vector2(-1.15f,-.45f),10,model);
            front=AncientArt.Display("Front wheel",AncientArt.Motorcycle(1),new Vector2(1.16f,-.45f),10,model);
            rider=AncientArt.Display("Mounted rider",AncientArt.Hero(7),new Vector2(-.05f,-.42f),11,frame);
            if(AncientVisuals.Instance)
            {
                AncientVisuals.Instance.Rim(chassis);AncientVisuals.Instance.Rim(rider);
                AncientVisuals.Instance.Glow(new Vector2(.26f,-.17f),AncientGame.Cyan,1.65f,12,frame);
                AncientVisuals.Instance.Glow(new Vector2(1.08f,.12f),new Color(.6f,.9f,1),1.8f,12,frame);
                AncientVisuals.Instance.Glow(new Vector2(-1.12f,.05f),AncientGame.Orange,.75f,12,frame);
            }
            var shader=Resources.Load<Shader>("Shaders/ParticleGlow");if(!shader||!shader.isSupported)shader=Shader.Find("Sprites/Default");
            effects=new Material(shader);
            exhaust=MakeTrail("Particle exhaust",.17f,.19f);exhaustCore=MakeTrail("Exhaust core",.06f,.10f);
            skidMaterial=new Material(Shader.Find("Sprites/Default"));
            skidTrails=new TrailRenderer[2];
            for(int i=0;i<2;i++)
            {
                var tire=new GameObject("Pooled tire skid trail");tire.transform.SetParent(model,false);
                tire.transform.localPosition=new Vector2(i==0?-1.15f:1.16f,-.88f);
                var trace=tire.AddComponent<TrailRenderer>();trace.sharedMaterial=skidMaterial;trace.sortingOrder=4;
                trace.startWidth=.1f;trace.endWidth=.035f;trace.time=1.1f;trace.minVertexDistance=.08f;
                trace.startColor=new Color(.17f,.2f,.22f,.7f);trace.endColor=new Color(.17f,.2f,.22f,0);trace.emitting=false;
                skidTrails[i]=trace;
            }
            headlight=game.Line("Motorcycle headlight",new Color(.6f,.9f,1,.09f),.06f,5);
            headlight.material=effects;headlight.endWidth=1.15f;headlight.endColor=new Color(.5f,.8f,1,0);
            headlight.enabled=false;
            engine=gameObject.AddComponent<AudioSource>();engine.loop=true;engine.spatialBlend=0;engine.volume=0;
            const int rate=22050;var data=new float[rate];
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)rate;
                data[i]=(Mathf.Sin(t*55*Mathf.PI*2)*.32f+Mathf.Sin(t*110*Mathf.PI*2)*.16f+Mathf.Sin(t*220*Mathf.PI*2)*.08f)*(.8f+.2f*Mathf.Sin(t*4*Mathf.PI*2));
            }
            engineClip=AudioClip.Create("Particle motor",rate,1,rate,false);engineClip.SetData(data,0);engine.clip=engineClip;engine.Play();
            model.gameObject.SetActive(false);
        }
        TrailRenderer MakeTrail(string name,float width,float lifetime)
        {
            var o=new GameObject(name);o.transform.SetParent(frame,false);o.transform.localPosition=new Vector2(-1.2f,-.18f);
            var trail=o.AddComponent<TrailRenderer>();trail.material=effects;trail.sortingOrder=7;
            trail.startColor=new Color(.25f,.95f,1,.7f);trail.endColor=new Color(.05f,.6f,1,0);
            trail.startWidth=width;trail.endWidth=0;trail.time=lifetime;trail.minVertexDistance=.04f;trail.emitting=false;return trail;
        }
        public void SetArmPower(float charge,float activity)
        {
            if(!rider||!hero.bike)return;
            if(game.Production&&game.Production.CinematicBikeBladeActive){charge=1;activity=1;}
            if(armProperties==null)armProperties=new MaterialPropertyBlock();
            armProperties.SetFloat(ArmCharge,charge);armProperties.SetFloat(ArmPulse,activity);
            rider.SetPropertyBlock(armProperties);
        }
        public void Mount()
        {
            wheelAngle=0;compression=0;lastSpeed=hero.HorizontalSpeed;
            landingAge=1;landingStrength=airborneAge=fallingSpeed=riderLean=roadPhase=0;wasRunning=false;
            frame.localPosition=Vector3.zero;frame.localRotation=Quaternion.identity;previousGrounded=false;
            rider.transform.localPosition=new Vector2(-.05f,-.42f);rider.transform.localRotation=Quaternion.identity;
            exhaust.Clear();exhaustCore.Clear();foreach(var trace in skidTrails){trace.emitting=false;trace.Clear();}model.gameObject.SetActive(true);
        }
        public void Park()
        {
            exhaust.emitting=exhaustCore.emitting=false;exhaust.Clear();exhaustCore.Clear();
            foreach(var trace in skidTrails){trace.emitting=false;trace.Clear();}
            model.gameObject.SetActive(false);headlight.enabled=false;
        }
        public void Damage()
        {flashUntil=Time.time+.13f;game.Burst((Vector2)transform.position+Vector2.down*.2f,AncientGame.Orange,10);game.Sound(1);}
        public void Detonate()
        {
            float facing=Mathf.Abs(hero.HorizontalSpeed)>1?Mathf.Sign(hero.HorizontalSpeed):hero.Facing;
            Debris(rear,new Vector2(-facing*5,8),.9f);Debris(front,new Vector2(facing*7,10),.9f);
            Debris(chassis,new Vector2(-facing*2,7),.48f);
            game.Impact(transform.position,AncientGame.Orange,true);game.Impact(transform.position,AncientGame.Cyan,false);
            exhaust.emitting=exhaustCore.emitting=false;exhaust.Clear();exhaustCore.Clear();
            model.gameObject.SetActive(false);headlight.enabled=false;
        }
        void Debris(SpriteRenderer source,Vector2 speed,float scale)
        {
            var piece=AncientArt.Display("Dispersing bike component",source.sprite,source.transform.position,6);
            piece.transform.rotation=source.transform.rotation;piece.transform.localScale=source.transform.lossyScale*scale;
            var fragment=piece.gameObject.AddComponent<AncientBikeDebris>();fragment.Begin(speed,hero.Facing*220);
        }
        void LateUpdate()
        {
            if(!hero||!model)return;
            bool externalPause=game.MainMenu||game.skills||(game.Story&&(game.Story.DialogueOpen||game.Story.LiftBusy))||(game.Journal&&game.Journal.Open)||!Application.isFocused;
            bool cinematic=hero.bike&&game.Production&&game.Production.Cinematic&&!externalPause;
            float visualDelta=cinematic?Time.unscaledDeltaTime:Time.deltaTime;
            float signedSpeed=cinematic?game.Production.CinematicBikeSpeed:hero.HorizontalSpeed;
            float speed=Mathf.Abs(signedSpeed);float skid=hero.BikeSkid;bool running=hero.bike&&!externalPause&&(!game.Paused||cinematic)&&!game.won;
            bool productionBlade=hero.bike&&game.Production&&game.Production.CinematicBikeBladeActive;
            if(productionBlade!=lastProductionBlade)
            {lastProductionBlade=productionBlade;SetArmPower(hero.CaptureEnergy/100f,productionBlade?1:hero.Combat?hero.Combat.ArmActivity:0);}
            float volume=running&&audioSettings&&!audioSettings.Muted ? 0.065f : 0f;
            engine.volume=!running||!audioSettings||audioSettings.Muted?0:Mathf.MoveTowards(engine.volume,volume,Time.unscaledDeltaTime*.3f);
            // Park/Detonate already clear the presentation; walking need not repeat native renderer setters.
            if(!hero.bike){previousGrounded=wasRunning=false;return;}
            // Preserve the exact suspended pose while a menu, dialogue or focus loss
            // stops play. The first resumed frame must not invent an acceleration jolt.
            if(!running)
            {
                wasRunning=false;exhaust.emitting=exhaustCore.emitting=false;headlight.enabled=false;
                foreach(var trace in skidTrails)trace.emitting=false;
                return;
            }
            if(!wasRunning){lastSpeed=signedSpeed;wasRunning=true;}
            visualDelta=Mathf.Max(0,visualDelta);
            engine.pitch=Mathf.Lerp(engine.pitch,.75f+Mathf.Clamp01(speed/17)*1.15f+skid*.2f,1-Mathf.Exp(-6*visualDelta));
            float facing=speed>1.5f?Mathf.Sign(signedSpeed):hero.Facing;
            model.localScale=new Vector3(facing*(1-skid*.12f),1,1);
            float acceleration=(signedSpeed-lastSpeed)/Mathf.Max(.001f,visualDelta);lastSpeed=signedSpeed;
            bool grounded=cinematic||hero.BikeGrounded;
            if(grounded)
            {
                if(!previousGrounded&&airborneAge>.06f&&fallingSpeed<-2)
                {landingStrength=Mathf.Lerp(.035f,.12f,Mathf.Clamp01((-fallingSpeed-2)/20));landingAge=0;}
                airborneAge=fallingSpeed=0;
                roadPhase=Mathf.Repeat(roadPhase+(12+speed)*visualDelta,Mathf.PI*2);
            }
            else{airborneAge+=visualDelta;fallingSpeed=Mathf.Min(fallingSpeed,hero.VerticalSpeed);}
            previousGrounded=grounded;
            // An analytic damped spring starts at the current ride height, dips,
            // then makes one small rebound. It stays stable across frame rates/hitches.
            compression=landingStrength*Mathf.Exp(-landingAge*14)*Mathf.Sin(landingAge*22);
            landingAge=Mathf.Min(1,landingAge+visualDelta);
            float bounce=grounded?Mathf.Sin(roadPhase)*.009f*Mathf.Clamp01(speed/10):0;
            frame.localPosition=new Vector2(-skid*.16f,-compression+bounce-skid*.06f);
            float pitch=-skid*7+Mathf.Clamp(acceleration*facing*.12f,-4,4)+(grounded?0:Mathf.Clamp(hero.VerticalSpeed*.35f,-7,7));
            frame.localRotation=Quaternion.Lerp(frame.localRotation,Quaternion.Euler(0,0,pitch),1-Mathf.Exp(-9*visualDelta));
            float brace=cinematic?0:Mathf.Clamp(Mathf.Clamp(-acceleration*facing*.16f,-4,4)-skid*2+(grounded?0:Mathf.Clamp(-hero.VerticalSpeed*.12f,-1.8f,1.8f)),-6,6);
            riderLean=Mathf.Lerp(riderLean,brace,1-Mathf.Exp(-12*visualDelta));
            rider.transform.localRotation=Quaternion.Euler(0,0,riderLean);
            rider.transform.localPosition=new Vector2(-.05f+riderLean*.004f,-.42f-compression*.18f);
            wheelAngle=Mathf.Repeat(wheelAngle-signedSpeed*facing/.37f*Mathf.Rad2Deg*visualDelta*(1+skid*.3f),360);
            rear.transform.localRotation=front.transform.localRotation=Quaternion.Euler(0,0,wheelAngle);
            bool damaged=hero.bikeHP<=28;
            chassis.sprite=damaged?damagedBody:normalBody;
            chassis.transform.localScale=new Vector3(damaged?1.06f:1,1,1);
            chassis.transform.localPosition=new Vector2(0,damaged?-.207f:-.18f);
            Color tint=Time.time<flashUntil?new Color(1,.6f,.35f):Color.white;
            chassis.color=tint;
            rear.color=front.color=Color.white;
            rider.color=Color.white;
            bool skidding=running&&grounded&&skid>.15f&&speed>2;
            foreach(var trace in skidTrails)trace.emitting=skidding;
            if(skidding&&Time.time>=nextSkidAt)
            {
                nextSkidAt=Time.time+.09f;
                Vector2 tire=rear.transform.position;tire.y=transform.position.y-.88f;
                game.Burst(tire,new Color(.45f,.5f,.52f),AncientVisuals.HighQuality?4:2);
                if(speed>8)game.Burst(tire,AncientGame.Orange,1);
                game.Sound(7);
            }
            bool effectsVisible=AncientVisuals.HighQuality&&running;
            exhaust.emitting=exhaustCore.emitting=effectsVisible&&speed>3;
            headlight.enabled=effectsVisible;
            if(headlight.enabled)
            {
                Vector2 origin=frame.TransformPoint(new Vector2(1.1f,.12f));
                Vector2 aim=Vector2.right*facing;RaycastHit2D hit=Physics2D.Raycast(origin,aim,3,1<<8);
                headlight.SetPosition(0,origin);headlight.SetPosition(1,hit?hit.point:origin+aim*3);
            }
            if(damaged&&effectsVisible&&Time.unscaledTime>=sparkAt)
            {sparkAt=Time.unscaledTime+.18f;game.Burst(frame.TransformPoint(new Vector2(.2f,-.15f)),AncientGame.Orange,3);}
        }
        void OnDestroy()
        {
            if(headlight)Destroy(headlight.gameObject);
            if(engine){engine.Stop();engine.clip=null;}
            if(skidMaterial)Destroy(skidMaterial);if(effects)Destroy(effects);if(engineClip)Destroy(engineClip);
        }
    }
    public sealed class AncientBikeDebris : MonoBehaviour
    {
        SpriteRenderer art;Vector2 velocity;float spin,age;
        public void Begin(Vector2 speed,float angular){art=GetComponent<SpriteRenderer>();velocity=speed;spin=angular;}
        void Update()
        {
            age+=Time.deltaTime;velocity+=Vector2.down*19*Time.deltaTime;
            transform.position+=(Vector3)(velocity*Time.deltaTime);transform.Rotate(0,0,spin*Time.deltaTime);
            art.color=new Color(.6f,.85f,1,Mathf.Clamp01(1-age/.65f));if(age>=.65f)Destroy(gameObject);
        }
    }
}
