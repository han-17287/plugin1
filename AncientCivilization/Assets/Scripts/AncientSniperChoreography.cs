using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed partial class AncientSniperEncounter
    {
        enum RelayBeat { Run, Sweep, ChainAttack, Feint, Fold, Return, Opening }

        AncientJokerArena arena;
        AncientJokerAttacks attacks;
        Vector2 travelStart, travelEnd;
        RelayBeat relay;
        float beganBeat, endsBeat, nextSwap;
        int realSide, relayCount, previousChain, chainStep, foldSide, foldLane;
        bool inBackground, swapping, airCatch, airBladeHit, cloneLeads;
        Vector2 plannedMirrorAim;
        int supports,mirrorSafe;
        bool foldPossible;
        JokerChain chain;

        public AncientJokerArena Arena => arena;
        public bool CanWireOwner => !finished && phase<6 && !inBackground && (phase!=4 || relay!=RelayBeat.Return || !airCatch && BeatProgress>.08f && BeatProgress<.88f);
        public int LastFullHouseLength => chain.Length;
        public int LastFullHouseSignature => chain.Signature;

        bool StillFighting => game && game.hero && game.Sniper==this && game.boss==owner && owner && owner.IsAlive;
        float BeatProgress => Mathf.InverseLerp(beganBeat,endsBeat,Time.time);
        int SideOf(float x) => x<(left+right)*.5f ? 0 : 1;
        int LaneOf(float x) => Mathf.Clamp(Mathf.FloorToInt(Mathf.InverseLerp(left,right,x)*3),0,2);

        void FixedUpdate()
        {
            if(arena!=null && StillFighting && !game.Paused && phase<6)
                arena.FixedTick(Time.fixedDeltaTime);
        }

        static void ResetActor(Actor a,float delay)
        {
            a.tell.enabled=false;
            a.state=a.shots=a.grenades=a.magazine=0;
            a.grenadeUsed=a.duelUsed=a.lateLock=a.coveringPlate=a.ejected=false;
            a.began=a.flash=0;
            a.until=Time.time+delay;
        }

        // 각 사선의 선행 신호가 중간에 잘리지 않도록 탄창 사이에서 교대한다.
        bool TickSwap()
        {
            if(swapping)
            {
                if(Time.time<nextSwap) return true;
                var copy=actors[1];
                Vector2 oldCopy=copy.enemy ? (Vector2)copy.enemy.transform.position :
                    inBackground ? arena.LandingPoint(1-realSide) : arena.RailPoint(1-realSide);
                Vector2 oldOwner=owner.transform.position;
                realSide=1-realSide;
                owner.transform.position=inBackground ? arena.LandingPoint(realSide) : arena.RailPoint(realSide);
                if(!copy.enemy) SpawnClone(1,oldOwner);
                else copy.enemy.transform.position=oldOwner;
                inBackground=!inBackground;
                attacks.Ring(oldCopy+Vector2.up*.35f);
                arena.ClearReturn();
                owner.FaceToward(game.hero.transform.position.x);
                actors[0].until=Time.time+.8f;
                if(copy.state==0) copy.until=Time.time+.15f;
                swapping=false;
                nextSwap=Time.time+5.4f;
                return false;
            }
            if(Time.time<nextSwap || attacks.Busy || actors[0].state!=0) return false;
            // 분신이 먼저 새 조준을 시작하면 본체가 기다리기만 하는 상황을 피한다.
            if(actors[1].enemy && (actors[1].state==1 || actors[1].state==3))
            {
                actors[0].until=Time.time+.15f;
                return false;
            }
            if(inBackground) arena.MarkReturn(1-realSide);
            swapping=true;
            nextSwap=Time.time+.62f;
            return true;
        }

        void SetBeat(RelayBeat beat,float duration)
        {
            relay=beat; beganBeat=Time.time; endsBeat=Time.time+duration;
        }

        bool HasGunner(int seat)
        {
            var actor=actors[seat+2];
            return actor.enemy && actor.enemy.IsAlive && !actor.ejected;
        }

        string ActivePhaseHint()
        {
            if(attacks.Busy) return attacks.Hint;
            if(phase==4 && relay==RelayBeat.Return || phase==5 && (relay==RelayBeat.Feint || relay==RelayBeat.Return))
                return realSide==0?"왼쪽 셔터 · 모자 그림자":"모자 그림자 · 오른쪽 셔터";
            if(phase==1 && actors[0].state==2) return "다섯 발 소진 · 재장전";
            return phaseHints[phase];
        }

        void StartRailRun()
        {
            inBackground=true; airCatch=airBladeHit=false;
            arena.ClearReturn();
            travelStart=arena.RailPoint(realSide);
            realSide=1-realSide;
            travelEnd=arena.RailPoint(realSide);
            owner.transform.position=travelStart;
            cloneLeads=(relayCount&1)!=0;
            if(phase==5)
            {
                if(!actors[1].enemy) SpawnClone(1,travelEnd);
                else actors[1].enemy.transform.position=travelEnd;
            }
            SetBeat(RelayBeat.Run,phase==4?1.25f:1.0f);
        }

        void TickRelay()
        {
            float t=BeatProgress;
            Vector2 hero=game.hero.transform.position;
            owner.FaceToward(hero.x);
            bool aiming=attacks.Telegraphing && (phase==4 || !cloneLeads || !actors[1].enemy);
            int pose=relay==RelayBeat.Opening?5:aiming?3:relay==RelayBeat.Run?1:0;
            owner.BossVisual(pose,hero.x<owner.transform.position.x?-1:1,
                aiming?attacks.WindupProgress:0,aiming,Recovering);
            if(phase==5 && actors[1].enemy)
            {
                var copy=actors[1].enemy;
                int copyPose=relay==RelayBeat.Feint?1:cloneLeads && attacks.Telegraphing?3:0;
                copy.BossVisual(copyPose,hero.x<copy.transform.position.x?-1:1,0,
                    cloneLeads && attacks.Telegraphing,false);
            }
            switch(relay)
            {
                case RelayBeat.Run:
                    owner.transform.position=Vector2.Lerp(travelStart,travelEnd,Mathf.SmoothStep(0,1,t));
                    if(phase==5 && actors[1].enemy)
                        actors[1].enemy.transform.position=Vector2.Lerp(travelEnd,travelStart,Mathf.SmoothStep(0,1,t));
                    if(t<1) return;
                    if(phase==5) StartChain();
                    else if((relayCount&1)==0) StartReturn(true);
                    else
                    {
                        attacks.Laser((relayCount&2)!=0);
                        SetBeat(RelayBeat.Sweep,attacks.Duration);
                    }
                    break;
                case RelayBeat.Sweep:
                    if(!attacks.Busy) StartReturn(true);
                    break;
                case RelayBeat.ChainAttack:
                    if(!attacks.Busy) FinishLink();
                    break;
                case RelayBeat.Feint:
                    if(actors[1].enemy)
                    {
                        // 가짜는 반대쪽에 떨어졌다 입자화한다. 모자 그림자는 진짜 쪽에만 남긴다.
                        Vector2 a=arena.RailPoint(1-realSide), b=arena.LandingPoint(1-realSide);
                        actors[1].enemy.transform.position=Vector2.Lerp(a,b,Mathf.SmoothStep(0,1,t));
                    }
                    if(t<1) return;
                    if(actors[1].enemy)
                    {
                        Vector2 vanished=actors[1].enemy.transform.position;
                        game.Burst(vanished+Vector2.up,AncientGame.Cyan,12);
                        actors[1].enemy.transform.position=arena.RailPoint(1-realSide);
                    }
                    arena.ClearReturn();
                    FinishLink();
                    break;
                case RelayBeat.Fold:
                    if(t>=1) FinishLink();
                    break;
                case RelayBeat.Return:
                    if(!airCatch)
                    {
                        Vector2 p=Vector2.Lerp(travelStart,travelEnd,Mathf.SmoothStep(0,1,t));
                        p.y+=Mathf.Sin(t*Mathf.PI)*1.2f;
                        owner.transform.position=p;
                        if(phase==4 && t>.08f && t<.88f) arena.SetRing(owner.AimPoint);
                        else if(phase==4 && t>=.88f) arena.CloseRing();
                    }
                    if(t>=1)
                    {
                        owner.transform.position=travelEnd;
                        arena.ClearReturn();
                        inBackground=false;
                        SetBeat(RelayBeat.Opening,phase==4?1.5f:1.25f);
                    }
                    break;
                case RelayBeat.Opening:
                    if(t>=1){relayCount++;StartRailRun();}
                    break;
            }
        }
        void StartReturn(bool heart)
        {
            travelStart=owner.transform.position;
            travelEnd=arena.LandingPoint(realSide);
            inBackground=!heart; airCatch=airBladeHit=false;
            arena.MarkReturn(realSide);
            SetBeat(RelayBeat.Return,heart?1.4f:.58f);
        }

        public void OnWireImpact()
        {
            if(phase!=4 || relay!=RelayBeat.Return || airCatch || BeatProgress<=.08f || BeatProgress>=.91f) return;
            airCatch=true;
            arena.ClearReturn();
            // 고리를 잡는 프레임에서 내려오던 보스를 한 박자 붙잡아 한 번 벨 여유를 준다.
            endsBeat=Time.time+.68f;
            game.Impact(owner.AimPoint,AncientGame.Cyan,false);
        }

        AncientEnemy ChainShooter => cloneLeads && actors[1].enemy ? actors[1].enemy : owner;
        int SafeLanes(JokerMove move,int step)
        {
            int lanes=supports;
            if(move==JokerMove.DoubleRicochet) return lanes & mirrorSafe;
            if(move==JokerMove.Fold) return foldPossible ? lanes & (1<<foldLane) : 0;
            if(move==JokerMove.FakeReturn)
            {
                int decoyLane=realSide==1?0:2;
                return lanes & ~(1<<decoyLane);
            }
            return lanes;
        }
        void SurveyEscapeRoutes()
        {
            supports=arena.SupportedLanes;
            mirrorSafe=attacks.MirrorSafeLanes(ChainShooter.SniperMuzzle,plannedMirrorAim,2);
            foldPossible=arena.CanFold(foldSide,game.hero.transform.position.x);
        }
        void StartChain()
        {
            int start=LaneOf(game.hero.transform.position.x);
            foldSide=1-realSide;
            foldLane=start;
            plannedMirrorAim=game.hero.transform.position;
            ChainShooter.FaceToward(plannedMirrorAim.x);
            SurveyEscapeRoutes();
            if(!AncientSniperRules.TryChain(Random.Range(0,100000),previousChain,start,supports,SafeLanes,out chain))
            {
                // 퇴로가 사라진 경우 한 턴을 돌려주고 다음 연계에서 지형을 다시 검사한다.
                StartReturn(false); return;
            }
            previousChain=chain.Signature;
            chainStep=0;
            PlayLink();
        }
        void PlayLink()
        {
            JokerMove move=chain.At(chainStep);
            int lane=chain.SafeLaneAt(chainStep);
            Vector2 hero=game.hero.transform.position;
            if(chainStep>0) SurveyEscapeRoutes();
            if((SafeLanes(move,chainStep)&(1<<lane))==0)
            {
                StartReturn(false);
                return;
            }
            var shooter=ChainShooter; shooter.FaceToward(hero.x);
            switch(move)
            {
                case JokerMove.DoubleRicochet:
                    attacks.Mirror(shooter.SniperMuzzle,plannedMirrorAim,2,lane);
                    SetBeat(RelayBeat.ChainAttack,attacks.Duration);
                    break;
                case JokerMove.Sweep:
                    attacks.Laser(((relayCount+chainStep)&1)==0,lane);
                    SetBeat(RelayBeat.ChainAttack,attacks.Duration);
                    break;
                case JokerMove.Fold:
                    arena.BeginFold(foldSide,lane);
                    SetBeat(RelayBeat.Fold,1.65f);
                    break;
                case JokerMove.FakeReturn:
                    if(!actors[1].enemy) SpawnClone(1,arena.RailPoint(1-realSide));
                    arena.MarkReturn(realSide);
                    SetBeat(RelayBeat.Feint,.92f);
                    break;
            }
        }
        void FinishLink()
        {
            chainStep++;
            if(chainStep>=chain.Length) StartReturn(false);
            else PlayLink();
        }
    }
}
