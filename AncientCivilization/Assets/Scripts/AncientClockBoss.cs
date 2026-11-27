using UnityEngine;
using Cue = Plugin.AncientCivilization.AncientBossCue;
namespace Plugin.AncientCivilization
{
    // First phase only. No health threshold silently introduces an unrequested second phase.
    public sealed class AncientClockBoss : MonoBehaviour
    {
        enum State { Rest,Vector,Flash,TeleportTell,AreaWindup,AreaActive,RainTell,Rain }
        AncientEnemy owner;AncientGame game;State state;float began,until,nextRain,hour,minute,areaStarted;
        int vectorIndex,areaIndex,rainIndex,cursor,lastTick=-1;
        AncientClockDial face;Vector2 centre,destination;float left,right;
        AncientClockAreaDamage areaDamage;
        bool stopped;
        AncientClockSpear[] spears = System.Array.Empty<AncientClockSpear>();
        AncientClockRainWarning rainWarning;
        float rainLeft, rainRight;
        int rainLanes;
        public bool Telegraphing {get{return state==State.Vector||state==State.AreaWindup||state==State.RainTell||state==State.TeleportTell;}}
        public bool Recovering {get{return state==State.Rest;}}
        public bool Physical {get{return state==State.AreaWindup||state==State.AreaActive;}}
        public float WindupProgress {get{return Telegraphing?Mathf.Clamp01((Time.time-began)/Mathf.Max(.01f,until-began)):0;}}
        public float RecoveryRemaining {get{return Recovering?Mathf.Clamp01((until-Time.time)/1.6f):0;}}
        public void Initialize(AncientEnemy enemy,AncientGame world)
        {
            owner=enemy;game=world;left=AncientCityLayout.BossArenaLeft(4,true)+1;right=AncientCityLayout.BossArenaRight(4,true)-1;
            rainLeft = AncientCityLayout.BossArenaLeft(4, true);
            rainRight = AncientCityLayout.BossArenaRight(4, true);
            rainLanes = AncientClockRules.RainLaneCount(rainRight - rainLeft);
            face = new GameObject("비서실장 · 기계식 회중시계").AddComponent<AncientClockDial>();
            face.Initialize(game, rainLanes);
            rainWarning = new AncientClockRainWarning(game, rainLanes, rainLeft, rainRight);
            spears = new AncientClockSpear[AncientClockRules.SpearCapacity(rainLanes)];
            for(int i=0;i<spears.Length;i++)
            {var item=new GameObject("Pooled clock spear");item.transform.SetParent(transform.parent,false);spears[i]=item.AddComponent<AncientClockSpear>();spears[i].Initialize(game, face);}
            Hide();Enter(State.Rest,1.6f);
        }
        void Enter(State value,float duration)
        {
            state=value;began=Time.time;until=began+duration;
            // 예고가 공격으로 넘어가는 순간에만 태엽이 풀리는 소리를 낸다.
            if (value == State.Flash || value == State.AreaActive || value == State.Rain)
                game.Sound(AncientEquipmentCue.ClockStrike);
        }
        void Hide() { if (face) face.Hide(); }
        static Vector2 Direction(float a){return new Vector2(Mathf.Cos(a),Mathf.Sin(a));}
        bool Spawn(Vector2 p,Vector2 v,int kind)
        {
            for(int i=0;i<spears.Length;i++)
            {int at=(cursor+i)%spears.Length;if(!spears[at].Available)continue;spears[at].Play(p,v,kind);cursor=(at+1)%spears.Length;return true;}
            return false;
        }
        public bool TryBlade(Vector2 origin,Vector2 direction)
        {
            bool hit=false;foreach(var spear in spears)
            {
                if(!spear||spear.Available)continue;Vector2 point=spear.BreakPoint,delta=point-origin;
                if(delta.sqrMagnitude>3.1f*3.1f||Vector2.Dot(delta.normalized,direction)<-.15f||Physics2D.Linecast(origin,point,1<<8))continue;
                spear.Break();hit=true;
            }
            return hit;
        }
        public void StopBattle(){stopped=true;Cue.Clear(game, Cue.Style.Clock);rainWarning?.Hide();if(face)face.Strikes.Clear();Hide();foreach(var spear in spears)if(spear)spear.Disarm();}
        void BeginVector(){Hide();lastTick=-1;Enter(State.Vector,AncientClockRules.SpinSeconds);}
        void BeginTeleport()
        {
            Hide();Vector2 hero=game.hero.transform.position;
            destination=areaIndex==2?new Vector2((left+right)*.5f,5.1f):new Vector2(Mathf.Clamp(hero.x+(areaIndex==0?2.3f:-2.3f),left+1,right-1),0);
            // 출발지의 톱니가 잠기고 도착 표식은 기존 안전 폭을 그대로 보여 준다.
            Cue.Play(game, Cue.Style.Clock, Cue.Beat.Tell, owner.AimPoint, Vector2.up, .72f);
            face.Destination(destination, 0, areaIndex == 2);Enter(State.TeleportTell,.45f);
        }
        public void Tick()
        {
            if (stopped) return;
            face.Strikes.Tick(Time.deltaTime);
            TickAttack();
            if (stopped || !owner.IsAlive) return;
            // 순간이동·발사로 이번 프레임에 바뀐 상태를 읽어 얼굴과 손짓을 동시에 보여 준다.
            owner.FaceToward(game.hero.transform.position.x);
            float progress = Mathf.InverseLerp(began, until, Time.time);
            SecretaryGesture gesture;
            switch (state)
            {
                case State.Vector:
                    gesture = SecretaryGesture.ReadWatch;
                    // 3초 내내 팔을 천천히 넘기지 않고 먼저 시계를 들고, 매 초 눈금을 확인한다.
                    float elapsed = Time.time - began;
                    progress = elapsed < .65f ? .83f * elapsed / .65f :
                        .83f + .17f * (1f - Mathf.Abs(Mathf.Sin(elapsed * Mathf.PI)));
                    break;
                case State.Flash: gesture = SecretaryGesture.Dispatch; break;
                case State.TeleportTell: gesture = SecretaryGesture.Fold; break;
                case State.AreaWindup: gesture = SecretaryGesture.Brace; break;
                case State.AreaActive: gesture = SecretaryGesture.Sweep; break;
                case State.RainTell: gesture = SecretaryGesture.RaiseWatch; break;
                case State.Rain:
                    gesture = SecretaryGesture.ConductRain;
                    // 마지막 일제 투하 시각을 기준으로 손목을 내려 프레임 지연에도 박자를 맞춘다.
                    progress = Mathf.InverseLerp(nextRain - AncientClockRules.RainBeat, nextRain, Time.time);
                    break;
                default: gesture = SecretaryGesture.Rest; break;
            }
            owner.PresentClockGesture(AncientSecretaryGestures.Sample(gesture, progress));
        }
        void TickAttack()
        {
            owner.BossContact();
            // 접촉 피해로 재시작한 프레임에는 끝난 전투의 표시나 투사체가 되살아나면 안 된다.
            if (stopped) return;
            Vector2 target=game.hero.transform.position;
            if(state==State.Rest)
            {if(Time.time>=until){vectorIndex=0;BeginVector();}return;}
            if(state==State.Vector)
            {
                centre=owner.AimPoint;Vector2 delta=target-centre;
                int tick=Mathf.CeilToInt(until-Time.time);
                if(tick>0 && tick!=lastTick){lastTick=tick;game.Sound(AncientEquipmentCue.ClockTick);}
                AncientClockRules.Angles(Mathf.Atan2(delta.y,delta.x),WindupProgress,out hour,out minute);
                face.Wind(centre, WindupProgress, hour, minute);
                if(Time.time>=until)
                {
                    Vector2 resultant=Direction(hour)*AncientClockRules.HourForce+Direction(minute)*AncientClockRules.MinuteForce;
                    Spawn(centre,resultant.normalized*15,0);
                    if(AncientClockRules.ThirdVectorShot(vectorIndex)){Spawn(centre,Direction(hour)*15,0);Spawn(centre,Direction(minute)*15,1);}
                    Enter(State.Flash,.6f);face.Strike(centre, 0);
                    face.Strikes.Release(centre, false);
                    if (AncientClockRules.ThirdVectorShot(vectorIndex))
                        Cue.Play(game, Cue.Style.Clock, Cue.Beat.Strike, centre, Vector2.up, .94f);
                }
                return;
            }
            if(state==State.Flash)
            {
                face.Strike(centre, Time.time - began);
                if(Time.time>=until){if(++vectorIndex<3)BeginVector();else{areaIndex=0;BeginTeleport();}}
                return;
            }
            if(state==State.TeleportTell)
            {
                face.Destination(destination, WindupProgress, areaIndex == 2);
                if(Time.time>=until)
                {
                    owner.transform.position=destination;centre=owner.AimPoint;
                    Cue.Play(game, Cue.Style.Clock, Cue.Beat.Strike, centre, Vector2.up, .83f);
                    face.Hide();areaStarted=Time.time;Enter(State.AreaWindup,areaIndex==2?1.1f:.7f);
                }
                return;
            }
            if(state==State.AreaWindup||state==State.AreaActive)
            {
                centre=owner.AimPoint;
                // The hands keep their angle when the warning becomes damaging.
                float elapsed=Time.time-areaStarted,radius=areaIndex==2?AncientClockRules.BroadRadius(right-left+2):3;
                float angle=elapsed*(areaIndex==2?3:5);
                face.Area(centre, radius, angle, elapsed, WindupProgress,
                    state == State.AreaActive, areaIndex == 2, AncientClockRules.UnderSafeHalfWidth);
                if(state==State.AreaWindup)
                {
                    if(Time.time>=until)
                    {
                        areaDamage.Begin();
                        Enter(State.AreaActive,areaIndex==2?1.3f:.3f);
                        face.Strikes.Release(centre, false);
                    }
                }
                else
                {
                    Vector2 relative=target-centre;
                    bool hit=areaIndex==2?AncientClockRules.BroadHit(relative.x,relative.y,radius):relative.sqrMagnitude<radius*radius;
                    if(hit&&areaDamage.CanHit)
                    {
                        float hpBefore=game.hero.hp,bikeBefore=game.hero.bikeHP;
                        game.hero.Hurt(areaIndex==2?28:22);
                        if (stopped) return;
                        areaDamage.RecordHealth(hpBefore,game.hero.hp,bikeBefore,game.hero.bikeHP);
                    }
                    if(Time.time>=until)
                    {
                        Hide();if(++areaIndex<3)BeginTeleport();
                        else
                        {
                            owner.transform.position = new Vector2((left + right) * .5f, 0);
                            rainIndex = 0;
                            Enter(State.RainTell, AncientClockRules.RainFirstTell);
                            rainWarning.Show(rainIndex);
                        }
                    }
                }
                return;
            }
            if(state==State.RainTell)
            {
                rainWarning.Focus(WindupProgress);
                if (Time.time >= until)
                {
                    Enter(State.Rain, (AncientClockRules.RainWaves - 1) * AncientClockRules.RainBeat + AncientClockRules.RainAfterRelease);
                    ReleaseRain();
                }
                return;
            }
            if(state==State.Rain)
            {
                if (rainIndex < AncientClockRules.RainWaves)
                {
                    rainWarning.Focus(1f - (nextRain - Time.time) / AncientClockRules.RainBeat);
                    if (Time.time >= nextRain) ReleaseRain();
                }
                else if (Time.time >= until) { Hide(); Enter(State.Rest, 1.8f); }
            }
        }
        void ReleaseRain()
        {
            for (int lane = 0; lane < rainLanes; lane++)
            {
                int hand = AncientClockRules.RainHand(rainIndex, lane);
                float x = AncientClockRules.RainX(rainLeft, rainRight, rainLanes, rainIndex, lane);
                var origin = new Vector2(x, AncientClockRules.RainHeight);
                if (Spawn(origin, Vector2.down * AncientClockRules.RainSpeed(hand), hand))
                    face.Strikes.Release(origin, true);
            }
            // 긴 프레임에서도 예고되지 않은 열을 몰아서 떨어뜨리지 않는다.
            nextRain = Time.time + AncientClockRules.RainBeat;
            if (++rainIndex < AncientClockRules.RainWaves) rainWarning.Show(rainIndex);
            else
            {
                rainWarning.Hide();
                until = Time.time + AncientClockRules.RainAfterRelease;
            }
        }
        void OnDestroy()
        {
            if (!stopped) Cue.Clear(game, Cue.Style.Clock);
            rainWarning?.Dispose();
            if(face){face.Strikes.Clear(); Destroy(face.gameObject);}
            foreach(var spear in spears)if(spear)Destroy(spear.gameObject);
        }
    }
    public sealed class AncientClockSpear : MonoBehaviour
    {
        AncientGame game;AncientClockDial clockArt;SpriteRenderer shape;Vector2 velocity;float age;bool live,stuck;int kind;
        public bool Available {get{return !live;}}
        public Vector2 BreakPoint {get{return (Vector2)transform.position+(stuck?Vector2.up*.32f:Vector2.zero);}}
        public void Initialize(AncientGame world, AncientClockDial art)
        {
            game = world;
            clockArt = art;
            shape = gameObject.AddComponent<SpriteRenderer>();
            shape.sortingOrder = 17;
            shape.enabled = enabled = false;
        }
        public void Play(Vector2 position,Vector2 speed,int hand)
        {
            live=true;stuck=false;age=0;velocity=speed;kind=hand;transform.position=position;
            transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(speed.y,speed.x)*Mathf.Rad2Deg);
            float length = kind == 0 ? 1.25f : kind == 1 ? 1.6f : 1.85f;
            shape.sprite = clockArt.Projectile(kind);
            // 핀의 실제 장축 길이는 유지하고, 얇은 초침도 기본 줌에서 보이게 단면만 보강한다.
            transform.localScale = new Vector3(length, kind == 2 ? 4.1f : 3.45f, 1);
            shape.enabled = enabled = true;
        }
        public void Break(){if(!live)return;game.hero.Combat.EmitParticles(BreakPoint,kind==2?10:14);game.Burst(BreakPoint,AncientGame.Cyan,6);Remove();}
        public void Disarm(){Remove();}
        void Remove(){live=false;shape.enabled=enabled=false;}
        void Update()
        {
            if(!live||game.Paused)return;if(game.won){Remove();return;}age+=Time.deltaTime;
            if(age>9){Remove();return;}if(stuck)return;
            Vector2 previous=transform.position,next=previous+velocity*Time.deltaTime;
            Vector2 segment=next-previous,target=game.hero.transform.position,offset=target-previous;float entry,length=segment.magnitude;
            Vector2 travel=length>0?segment/length:offset.normalized;
            var terrain=Physics2D.Linecast(previous,next+travel*.65f,1<<8);
            float surfaceFraction=terrain?(length>0?terrain.distance/length:terrain.distance>0?float.MaxValue:0):0;
            Vector2 fromSurface=terrain?target-terrain.point:Vector2.zero;
            if(AncientProjectileRules.CircleEntry(segment.x,segment.y,offset.x,offset.y,AncientClockRules.SpearHitRadius,out entry)&&
                (!terrain||AncientProjectileRules.BeforeSurface(entry,surfaceFraction,fromSurface.x,fromSurface.y,terrain.normal.x,terrain.normal.y)))
            {game.hero.Hurt(18);Remove();return;}
            if(terrain&&terrain.distance<=length)
            {
                if(terrain.normal.y>.5f)
                {
                    stuck=true;transform.position=terrain.point+Vector2.up*.5f;transform.rotation=Quaternion.Euler(0,0,-90);age=0;
                    clockArt.Strikes.Land(terrain.point, terrain.normal, kind);
                    if (kind == 0)
                        Cue.Play(game, Cue.Style.Clock, Cue.Beat.Contact, terrain.point, terrain.normal, .79f);
                    return;
                }
                Remove();return;
            }
            transform.position=next;
        }
    }
}
