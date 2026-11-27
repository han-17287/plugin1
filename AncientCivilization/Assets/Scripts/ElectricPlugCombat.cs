using System;
using System.Collections.Generic;
using Plugin.AncientCivilization;
using UnityEngine;

namespace Plugin.PlayerCore
{
    public interface IShockable
    {
        void TakeElectricHit(float damage,float stagger,float horizontalPush);
    }

    public interface IElectricReactive
    {
        void React(ElectricPlugPulse pulse,float damage);
    }

    // 본편의 적은 충돌체가 없는 종류도 있다. 실제 피격 중심과 전기 설비를 같은 쓸기 판정으로 다룬다.
    public sealed class ElectricPlugCombat : MonoBehaviour
    {
        readonly struct Target
        {
            public readonly AncientEnemy enemy;
            public readonly ElectricPlugTarget fixture;
            public Target(AncientEnemy value) { enemy=value;fixture=null; }
            public Target(ElectricPlugTarget value) { fixture=value;enemy=null; }
            public Component Key => enemy ? (Component)enemy : fixture;
            public bool Alive => enemy ? enemy.IsAlive && enemy.isActiveAndEnabled && enemy.CanWireTarget :
                fixture && fixture.Targetable;
            public Vector2 Point => enemy ? enemy.AimPoint : fixture.Point;
            public float Radius => enemy ? enemy.IsBoss ? .83f : .48f : fixture.Radius;
            public bool Conductive => enemy || fixture && fixture.Conductive;
        }

        readonly List<Target> targets=new List<Target>(32);
        readonly ElectricPlugClock<Component> clock=new ElectricPlugClock<Component>();
        readonly Component[] visited=new Component[5];
        BluePlugData data;
        AncientGame world;
        Action<Component,float> burn;
        ElectricPlugFeedback feedback;

        public static ElectricPlugCombat Attach(GameObject actor, BluePlugData settings)
        {
            if (!actor || !settings) return null;
            if (!actor.TryGetComponent<ElectricPlugCombat>(out var result))
                result=actor.AddComponent<ElectricPlugCombat>();
            result.data=settings;
            result.world=AncientGame.Instance;
            result.burn=result.Burn;
            if (result.feedback==null) result.feedback=new ElectricPlugFeedback(actor.transform);
            return result;
        }

        ElectricPlugTarget observed;
        public string DebugStatus => observed ? observed.name+"  HP "+Mathf.CeilToInt(observed.Health)+
            "  감전 "+clock.ShockRemaining(observed).ToString("0.0")+"초" : "";
        public void ResetCombat()
        {
            clock.Clear();
            targets.Clear();
            Array.Clear(visited,0,visited.Length);
            feedback?.Clear();
            observed=null;
        }

        void OnDisable() => ResetCombat();

        public void Tick(float dt)
        {
            if (dt<=0 || !data) return;
            clock.Tick(dt,burn);
            feedback?.Step(dt);
            var fixtures=ElectricPlugTarget.Registered;
            for (int i=fixtures.Count-1;i>=0;--i)
            {
                if (i>=fixtures.Count) continue;
                var target=fixtures[i];
                if (target && target.gameObject.scene==gameObject.scene) target.Advance(this,dt);
            }
        }

        void Snapshot()
        {
            targets.Clear();
            if (world && world.gameObject.scene==gameObject.scene)
                for (int i=0;i<world.enemies.Count;i++)
                {
                    var e=world.enemies[i];
                    if (e && e.IsAlive && e.isActiveAndEnabled && e.CanWireTarget) targets.Add(new Target(e));
                }
            var registry=ElectricPlugTarget.Registered;
            for (int i=0;i<registry.Count;i++)
            {
                var f=registry[i];
                if (f && f.Targetable && f.gameObject.scene==gameObject.scene) targets.Add(new Target(f));
            }
        }

        bool Visible(Vector2 start,Vector2 end)
        {
            if ((end-start).sqrMagnitude<.0004f) return true;
            // 지면에 꽂힌 칼과 수면에서도 표면 바깥쪽으로 펼쳐지는 충격은 유지한다.
            Vector2 direction=(end-start).normalized;
            var hit=Physics2D.Linecast(start,end,data.solids);
            if(!hit)return true;
            if(hit.fraction>.0001f || (end-start).sqrMagnitude<.0025f)return false;
            Vector2 surface=start+direction*.045f;
            return !Physics2D.OverlapPoint(surface,data.solids) && !Physics2D.Linecast(surface,end,data.solids);
        }

        public bool TryAim(Vector2 hand,Vector2 pointer,float range,float assist,out Vector2 aim)
        {
            aim=pointer;
            if (!data) return false;
            Snapshot();
            bool found=false;
            float best=assist*assist;
            for(int i=0;i<targets.Count;i++)
            {
                var t=targets[i];
                Vector2 point=t.Point;
                float offset=(point-pointer).sqrMagnitude;
                if(offset>best || (point-hand).sqrMagnitude>range*range || !Visible(hand,point)) continue;
                aim=point;
                best=offset;
                found=true;
            }
            if (found) feedback?.Aim(aim);
            return found;
        }

        public void Spin(Vector2 hand,Vector2 tip)
        {
            if (!data) return;
            Snapshot();
            for(int i=targets.Count-1;i>=0 && i<targets.Count;i--)
            {
                var t=targets[i];
                if (!t.Alive) continue;
                float r=t.Radius+data.spinContactRadius;
                if ((tip-t.Point).sqrMagnitude>r*r || !Visible(hand,t.Point) || !clock.Spin(t.Key,data.spinHitInterval)) continue;
                Hit(t,ElectricPlugPulse.Spin,data.spinDamage,0,data.spinPush,hand);
            }
        }

        public bool ThrowHit(Vector2 from,Vector2 to,out Vector2 point)
        {
            point=to;
            if (!data) return false;
            Snapshot();
            Vector2 d=to-from;
            float best=float.PositiveInfinity;
            int selected=-1;
            for(int i=0;i<targets.Count;i++)
            {
                var target=targets[i];
                Vector2 offset=target.Point-from;
                if (!ElectricPlugRules.SegmentHit(d.x,d.y,offset.x,offset.y,data.tipRadius+target.Radius,out float fraction) ||
                    fraction>=best || !Visible(from,target.Point)) continue;
                best=fraction;
                selected=i;
            }
            AncientProjectile projectile=null;
            if(world && world.hero && world.gameObject.scene==gameObject.scene)
            {
                for(int i=0;i<world.shots.Count;i++)
                {
                    var shot=world.shots[i];
                    if(!shot || !shot.Breakable || !shot.isActiveAndEnabled)continue;
                    Vector2 center=shot.transform.position, offset=center-from;
                    if(!ElectricPlugRules.SegmentHit(d.x,d.y,offset.x,offset.y,data.tipRadius+.34f,out float hit) ||
                        hit>=best || !Visible(from,center))continue;
                    best=hit;
                    projectile=shot;
                }
            }
            if(projectile)
            {
                point=from+d*best;
                Shatter(projectile);
                return true;
            }
            if (selected<0) return false;
            point=from+d*best;
            var first=targets[selected];
            Vector2 source=first.Point;
            Component firstKey=first.Key;
            bool conducts=first.Conductive;
            Hit(first,ElectricPlugPulse.Throw,data.throwDamage,data.throwStagger,0,from);
            if(first.Alive && (first.enemy || first.fixture.CanShock))
                clock.ShockTarget(firstKey,data.shockSeconds,data.shockDamagePerSecond,data.shockTick);
            if (conducts) Chain(source,firstKey);
            return true;
        }

        void Chain(Vector2 from,Component first)
        {
            // 폭탄의 폭발과 도트 피해는 이 함수를 부르지 않는다. 같은 회로 안에서는 한 몸을 두 번 거치지 않는다.
            visited[0]=first;
            int max=Mathf.Clamp(data.chainHops,0,visited.Length-1);
            for(int hop=1;hop<=max;hop++)
            {
                float radius=data.chainRange;
                if (visited[hop-1] is ElectricPlugTarget wet && wet.Kind==ElectricFixtureKind.Water)
                    radius*=1.8f;
                int nearest=-1;float best=radius*radius;
                for(int i=0;i<targets.Count;i++)
                {
                    var t=targets[i];
                    if(!t.Alive || !t.Conductive || Array.IndexOf(visited,t.Key,0,hop)>=0) continue;
                    float d=(from-t.Point).sqrMagnitude;
                    if(d>best || !Visible(from,t.Point)) continue;
                    nearest=i;best=d;
                }
                if(nearest<0) break;
                var next=targets[nearest];
                if(!clock.Chain(next.Key,data.chainCooldown)) break;
                Vector2 end=next.Point;
                visited[hop]=next.Key;
                feedback?.Arc(from,end);
                Hit(next,ElectricPlugPulse.Chain,data.chainDamage,0,0,from);
                from=end;
            }
        }

        public void Dock(BluePlugSocket socket)
        {
            if (socket && socket.TryGetComponent<ElectricPlugTarget>(out var contact) &&
                contact.Kind==ElectricFixtureKind.WornOutlet)
                contact.React(ElectricPlugPulse.Throw,0);
        }

        public void Arrive(Vector2 point)
        {
            if (!data) return;
            feedback?.Ring(point,data.dischargeRadius);
            Burst(point,data.dischargeRadius,data.dischargeDamage,ElectricPlugPulse.Arrival,data.dischargeStagger);
        }

        public Vector2 LastBladeContact { get; private set; }

        public bool Melee(Vector2 point,float radius,float damage)
        {
            if(!data)return false;
            Vector2 hand=(Vector2)transform.position+Vector2.up*data.handHeight;
            var wall=Physics2D.Linecast(hand,point,data.solids);
            if(wall)point=wall.point-(point-hand).normalized*.035f;
            return Burst(point,radius,damage,ElectricPlugPulse.Blade,0);
        }

        public void DamageBeam(Vector2 start,Vector2 end,float radius,float damage)
        {
            if (!data || damage<=0) return;
            Snapshot();
            Vector2 delta=end-start;
            for(int i=targets.Count-1;i>=0 && i<targets.Count;i--)
            {
                var t=targets[i];if(!t.Alive) continue;
                Vector2 p=t.Point-start;
                if(ElectricPlugRules.SegmentHit(delta.x,delta.y,p.x,p.y,radius+t.Radius,out _) && Visible(start,t.Point))
                    Hit(t,ElectricPlugPulse.Beam,damage,0,0,start);
            }
        }

        public void Area(Vector2 point,float radius,float damage,SummonedBladeHit kind)
        {
            var pulse=kind==SummonedBladeHit.Field ? ElectricPlugPulse.Burn :
                kind==SummonedBladeHit.Overload ? ElectricPlugPulse.Blast : ElectricPlugPulse.Impact;
            Burst(point,radius,damage,pulse,0);
            if (pulse!=ElectricPlugPulse.Burn) BreakShots(point,radius);
        }

        internal void FixtureBurst(Vector2 point,float radius,float damage)
        {
            feedback?.Ring(point,radius);
            Burst(point,radius,damage,ElectricPlugPulse.Blast,.18f);
            BreakShots(point,radius);
        }

        bool Burst(Vector2 point,float radius,float damage,ElectricPlugPulse pulse,float stagger)
        {
            if (!data || damage<=0 || radius<=0) return false;
            bool contact=false;
            Snapshot();
            for(int i=targets.Count-1;i>=0 && i<targets.Count;i--)
            {
                var t=targets[i];
                if (!t.Alive) continue;
                float reach=t.Radius+radius;
                if ((point-t.Point).sqrMagnitude>reach*reach || !Visible(point,t.Point))continue;
                if (!contact && pulse==ElectricPlugPulse.Blade)
                    LastBladeContact = t.Point + Vector2.ClampMagnitude(point - t.Point, t.Radius);
                Hit(t,pulse,damage,stagger,0,point);
                contact=true;
            }
            return contact;
        }

        void BreakShots(Vector2 point,float radius)
        {
            if (!world || !world.hero || world.gameObject.scene!=gameObject.scene) return;
            for(int i=world.shots.Count-1;i>=0;i--)
            {
                if(i>=world.shots.Count) continue;
                var shot=world.shots[i];if(!shot || !shot.Breakable)continue;
                Vector2 pos=shot.transform.position;
                if((pos-point).sqrMagnitude>radius*radius || !Visible(point,pos))continue;
                Shatter(shot);
            }
            if(world.Living) world.Living.BreakConductiveCargo(point,radius);
        }

        void Shatter(AncientProjectile shot)
        {
            Vector2 point=shot.transform.position;
            if(world.hero.Combat)world.hero.Combat.EmitParticles(point,Mathf.Clamp(shot.damage,8,25));
            shot.Remove();
            feedback?.Spark(point,.65f);
            if(world.Story)world.Story.OnTraining(5);
        }

        void Burn(Component target,float damage)
        {
            if(!target) return;
            if(target is AncientEnemy e && e.IsAlive && e.CanWireTarget && e.isActiveAndEnabled)
                Hit(new Target(e),ElectricPlugPulse.Burn,damage,0,0,e.AimPoint);
            else if(target is ElectricPlugTarget f && f.Targetable)
                Hit(new Target(f),ElectricPlugPulse.Burn,damage,0,0,f.Point);
        }

        void Hit(Target target,ElectricPlugPulse pulse,float damage,float stagger,float push,Vector2 from)
        {
            Vector2 point=target.Point;
            if(target.enemy)
                target.enemy.TakeElectricHit(damage,stagger,Mathf.Sign(point.x-from.x)*push);
            else
                target.fixture.React(pulse,damage);
            if(target.fixture && target.fixture.CanShock)
                observed=target.fixture;
            if(pulse==ElectricPlugPulse.Throw || pulse==ElectricPlugPulse.Chain || pulse==ElectricPlugPulse.Spin)
                feedback?.Spark(point,pulse==ElectricPlugPulse.Throw ? .65f : .32f);
        }

        internal void CableContact(Vector2 source)
        {
            if(world && world.gameObject.scene==gameObject.scene && world.hero)
                world.hero.Hurt(6);
            else if(TryGetComponent<PlayerStateMachine>(out var p) && p.State!=PlayerStateId.Hurt && p.State!=PlayerStateId.Dead)
                p.ReceiveHurt(new Vector2(transform.position.x<source.x?-3:3,3),.16f);
        }
    }
}
