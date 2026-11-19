using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientOrbitalArena
    {
        const float DropSeconds = .68f, DropY = .48f, FreightY = 4.85f;
        const float ExplosionMinimum = 10f;
        static readonly Vector2 Unavailable = new Vector2(-100000, -100000);
        static readonly Color Dark = new Color(.12f,.17f,.22f);
        static readonly Color Steel = new Color(.38f,.48f,.56f);
        static readonly Color Gold = new Color(1,.76f,.23f);
        static readonly Color Blue = new Color(.38f,.89f,1);
        static readonly Color Locked = new Color(.96f,.33f,.20f);
        enum BladeState { Held, Flying, Stuck }

        readonly AncientGame game;
        readonly GameObject scenery, shutter, blade;
        readonly GameObject[] crates = new GameObject[2];
        readonly BoxCollider2D[] crateColliders = new BoxCollider2D[2];
        readonly SpriteRenderer[] crateGrips = new SpriteRenderer[2];
        int dropIndex;
        float otherPlatformTime;
        GameObject drop => crates[dropIndex];
        BoxCollider2D landingBox => crateColliders[dropIndex];
        SpriteRenderer temporaryGrip => crateGrips[dropIndex];
        readonly Transform freight;
        readonly Rigidbody2D freightBody;
        readonly AncientOrbitalFreight rail;
        readonly BoxCollider2D shutterBox;
        readonly SpriteRenderer[] anchorLamps = new SpriteRenderer[2];
        readonly SpriteRenderer leftGrip, rightGrip, supplyLamp, lockLamp;
        readonly Vector2 leftAnchor, rightAnchor;
        readonly int slots;
        readonly float left, right;
        float lockTime, dropTime, platformTime, shutterTime, supplyTime, bladeAge, recallCooldown, latchFlash, wireRecoveryTime;
        int lockedSide, bladeSide;
        bool dropping, landed, usedDropHook, dropHurts, hitByCrate, disposed;
        Vector2 fallTarget, fallingAt, bladePosition;
        BladeState bladeState;

        public float CentreX => (left + right) * .5f;
        public Vector2 CargoPoint => freight.position;
        public int RailDirection => rail.Direction;
        public Vector2 FixedAnchorPoint(int side) => side<0 ? leftAnchor : rightAnchor;

        public bool IsAnchorReachable(int side, Vector2 heroPosition)
        {
            if (side<0 ? !LeftAnchorAvailable : !RightAnchorAvailable) return false;
            Vector2 start=heroPosition+Vector2.up*.35f;
            Vector2 end=FixedAnchorPoint(side);
            float distanceSquared=(end-start).sqrMagnitude;
            if (distanceSquared<1 || distanceSquared>AncientZipRules.Range*AncientZipRules.Range) return false;
            var hit=Physics2D.Linecast(start,end,1<<8);
            return !hit || (hit.point-end).sqrMagnitude<.15f*.15f;
        }
        public Vector2 DropPoint => fallTarget;
        public Vector2 LatchPoint { get; }
        public Vector2 DispenserPoint { get; }
        public bool LeftAnchorAvailable => !disposed && (lockTime <= 0 || lockedSide != -1);
        public bool RightAnchorAvailable => !disposed && (lockTime <= 0 || lockedSide != 1);
        public bool BladeAway => bladeState != BladeState.Held;
        public bool BladeStuck => bladeState == BladeState.Stuck;

        public AncientOrbitalArena(AncientGame owner, Transform parent, float left, float right)
        {
            game = owner;
            this.left = left;
            this.right = right;
            // 경기장은 움직이는 보스의 자식이 되지 않는다.
            scenery = new GameObject("상층 화물선 · 궤도 봉쇄기");
            scenery.transform.SetParent(parent, true);
            leftAnchor = new Vector2(left + 2.1f, 6.65f);
            rightAnchor = new Vector2(right - 2.1f, 6.65f);
            LatchPoint = new Vector2(left + (right-left)*.31f, .38f);
            DispenserPoint = new Vector2(right - 3.15f, .89f);
            fallTarget = new Vector2(CentreX, DropY);
            if (game.OrbitalAnchorSlot < 0)
            {
                game.OrbitalAnchorSlot = game.anchors.Count;
                for (int i=0;i<5;i++) game.anchors.Add(Unavailable);
            }
            slots = game.OrbitalAnchorSlot;
            Trim("화물 주행 레일", new Vector2(CentreX, 7.65f), new Vector2(right-left-1,.14f), Steel, 1);
            for (int i=0;i<2;i++)
            {
                Vector2 anchor = i == 0 ? leftAnchor : rightAnchor;
                Trim("케이블 행거", anchor + Vector2.up*.5f, new Vector2(.10f,.9f), Steel, 5);
                Trim("연결점 하우징", anchor, new Vector2(.54f,.44f), Dark, 6);
                anchorLamps[i] = Trim("와이어 체결 표시", anchor, new Vector2(.35f,.22f), Blue, 7);
                Trim("체결구 중앙", anchor, new Vector2(.13f,.08f), Dark, 8);
            }
            freightBody = Platform("왕복 화물", new Vector2(CentreX,FreightY), new Vector2(3.1f,.68f), out _);
            freight = freightBody.transform;
            rail = freightBody.gameObject.AddComponent<AncientOrbitalFreight>();
            rail.Initialize(game,freightBody,left + 3.2f,right - 3.2f);
            leftGrip = Part(freight, "화물 왼쪽 잠금쇠", new Vector2(-1.15f,.08f),new Vector2(.14f,.48f), Gold, 8);
            rightGrip = Part(freight,"화물 오른쪽 잠금쇠",new Vector2(1.15f,.08f),new Vector2(.14f,.48f), Gold, 8);
            Part(freight,"화물 이음판",Vector2.zero,new Vector2(1.65f,.31f),Steel,7);
            Part(freight,"와이어 걸쇠",new Vector2(0,-.47f),new Vector2(.25f,.22f),Blue,8);
            Part(freight,"운반 도르래",new Vector2(0,2.79f),new Vector2(.68f,.28f),Dark,6);
            Part(freight,"화물 현수줄",new Vector2(0,1.52f),new Vector2(.06f,2.4f),Steel,3);
            for (int i=0; i<crates.Length; i++)
            {
                var crate=Platform("낙하형 회수 발판",fallTarget,new Vector2(2.1f,.90f),out crateColliders[i]).gameObject;
                crates[i]=crate;
                Part(crate.transform,"착지 하중 표시",new Vector2(0,.18f),new Vector2(1.62f,.22f),Gold,8);
                crateGrips[i]=Part(crate.transform,"일회용 걸쇠",new Vector2(0,.70f),new Vector2(.32f,.28f),Blue,8);
                crate.SetActive(false);
            }
            shutter=Platform("상하 분기 셔터",new Vector2(CentreX,3.9f),new Vector2(.78f,2.5f),out shutterBox).gameObject;
            for (int i=-1;i<=1;i++)
                Part(shutter.transform,"셔터 경고 띠",new Vector2(0,i*.72f),new Vector2(.61f,.16f),Gold,8);
            shutter.SetActive(false);
            Trim("낙하 유도 잠금 베이스",LatchPoint,new Vector2(1.55f,.25f),Dark,6);
            lockLamp=Trim("화물 잠금 핀",LatchPoint+Vector2.up*.13f,new Vector2(.9f,.20f),Gold,7);
            Trim("입자 공급기 함체",DispenserPoint,new Vector2(.85f,1.55f),Dark,6);
            Trim("공급기 커넥터",DispenserPoint+Vector2.up*.56f,new Vector2(.61f,.12f),Steel,7);
            supplyLamp=Trim("충전 카트리지",DispenserPoint,new Vector2(.42f,.72f),Blue,7);
            blade=new GameObject("투척된 샤프심 전구 블레이드");
            blade.transform.SetParent(scenery.transform,false);
            Part(blade.transform,"소켓",new Vector2(-.40f,0),new Vector2(.35f,.17f),Steel,19);
            Part(blade.transform,"전구",new Vector2(-.22f,0),new Vector2(.22f,.23f),Gold,20);
            Part(blade.transform,"필라멘트 날",new Vector2(.18f,0),new Vector2(.68f,.10f),new Color(1,1,.64f),20);
            blade.SetActive(false);
            SyncAnchors();
        }

        Rigidbody2D Platform(string name,Vector2 point,Vector2 size,out BoxCollider2D solid)
        {
            var platform=new GameObject(name);
            platform.transform.SetParent(scenery.transform,false);
            platform.transform.position=point;
            platform.layer=8;
            var body=platform.AddComponent<Rigidbody2D>();
            body.bodyType=RigidbodyType2D.Kinematic;
            body.interpolation=RigidbodyInterpolation2D.Interpolate;
            solid=platform.AddComponent<BoxCollider2D>();
            solid.size=size;
            Part(platform.transform,"외장판",Vector2.zero,size,Dark,6);
            Part(platform.transform,"상단 모서리",new Vector2(0,size.y*.5f-.06f),new Vector2(size.x,.12f),Steel,7);
            return body;
        }

        SpriteRenderer Trim(string name,Vector2 position,Vector2 size,Color color,int order)
        {return Part(scenery.transform,name,position,size,color,order);}

        SpriteRenderer Part(Transform owner,string name,Vector2 offset,Vector2 size,Color color,int order)
        {
            var shape=game.Box(name,Vector2.zero,size,color,order);
            shape.transform.SetParent(owner,false);
            shape.transform.localPosition=offset;
            return shape.GetComponent<SpriteRenderer>();
        }

        Vector2 CargoGrip => CargoPoint + Vector2.down*.47f;
        Vector2 BladeSocket => CargoPoint + new Vector2(bladeSide*1.65f,.05f);
        Vector2 DropGrip => (Vector2)drop.transform.position+Vector2.up*.70f;

        void SyncAnchors()
        {
            game.anchors[slots] = LeftAnchorAvailable ? leftAnchor : Unavailable;
            game.anchors[slots+1] = RightAnchorAvailable ? rightAnchor : Unavailable;
            game.anchors[slots+2] = CargoGrip;
            game.anchors[slots+3] = (dropping || landed) && !usedDropHook ? DropGrip : Unavailable;
            game.anchors[slots+4] = BladeStuck ? BladeSocket : Unavailable;
            anchorLamps[0].color = LeftAnchorAvailable ? Blue : Locked;
            anchorLamps[1].color = RightAnchorAvailable ? Blue : Locked;
        }

        public void PrepareMove(OrbitalMove move,int side,Vector2 target)
        {
            if (disposed) return;
            if (move==OrbitalMove.ReverseRail) rail.BrakeForReversal();
            if (move==OrbitalMove.DropCargo || move==OrbitalMove.CargoRush)
                fallTarget=new Vector2(Mathf.Clamp(target.x,left+1.25f,right-1.25f),DropY);
            if (move==OrbitalMove.SupplyLeak) {supplyTime=0; supplyLamp.color=Blue;}
        }

        public void BeginMove(OrbitalMove move,int side,Vector2 target)
        {
            if (disposed) return;
            switch (move)
            {
                case OrbitalMove.AnchorLock:
                    if (lockTime>0 && lockedSide!=side) break;
                    lockedSide=side<0?-1:1;
                    lockTime=4.5f;
                    if (game.hero) game.hero.ReleaseWireAt(FixedAnchorPoint(lockedSide));
                    break;
                case OrbitalMove.BladeFerry:
                    rail.FerryAcross(side);
                    break;
                case OrbitalMove.DropCargo:
                case OrbitalMove.CargoRush:
                    // 선행 낙하가 만든 발판에 서 있거나 와이어가 꽂혀 있어도 그 화물을 옮기지 않는다.
                    if (landed || dropping)
                    {
                        otherPlatformTime=landed ? platformTime : .25f;
                        temporaryGrip.color=Steel;
                        dropIndex=1-dropIndex;
                    }
                    landed=false;
                    fallingAt=fallTarget;
                    dropTime=0; dropping=true; usedDropHook=false; dropHurts=move==OrbitalMove.DropCargo;
                    hitByCrate=false;
                    drop.transform.position=new Vector2(fallingAt.x,8.4f);
                    drop.SetActive(true); landingBox.enabled=false; temporaryGrip.color=Blue;
                    break;
                case OrbitalMove.SupplyLeak: BreakSupply(); break;
                case OrbitalMove.ReverseRail: rail.SetDirection(side); break;
                case OrbitalMove.SplitShutter:
                    shutterTime=2.4f;
                    shutter.SetActive(true);
                    shutterBox.enabled=false;
                    break;
            }
            SyncAnchors();
        }

        void BreakSupply()
        {
            if (supplyTime>0) return;
            supplyTime=7f;
            supplyLamp.color=Dark;
            game.Burst(DispenserPoint,Blue,12);
            if(game.hero.Combat)game.hero.Combat.EmitParticles(DispenserPoint,22f);
        }

        public bool CutSupply(Vector2 origin,Vector2 direction)
        {
            Vector2 offset=DispenserPoint-origin;
            if (disposed || supplyTime>0 || offset.sqrMagnitude>2.7f*2.7f || Vector2.Dot(offset.normalized,direction)<.35f) return false;
            BreakSupply();
            return true;
        }

        public bool LockHit(Vector2 impact)
        {
            if (disposed || Mathf.Abs(impact.x-LatchPoint.x)>1.65f || Mathf.Abs(impact.y-LatchPoint.y)>2.2f) return false;
            latchFlash=1f;
            game.Burst(LatchPoint,Gold,18);
            return true;
        }

        public void OpenCounter()
        {
            if (disposed) return;
            lockTime=0; shutterTime=0;
            rail.ReleaseBrake();
            shutter.SetActive(false);
            dropHurts=false;
            SyncAnchors();
        }

        public void Tick(float dt)
        {
            if (disposed || !game.hero || dt<=0) return;
            lockTime=Mathf.Max(0,lockTime-dt);
            recallCooldown=Mathf.Max(0,recallCooldown-dt);
            wireRecoveryTime=Mathf.Max(0,wireRecoveryTime-dt);
            supplyTime=Mathf.Max(0,supplyTime-dt);
            latchFlash=Mathf.Max(0,latchFlash-dt);
            supplyLamp.color=supplyTime>0 ? Dark : Blue;
            lockLamp.color=latchFlash>0 ? Color.white : Gold;
            leftGrip.color=rail.Direction<0?Gold:Steel; rightGrip.color=rail.Direction>0?Gold:Steel;
            Vector2 hero=game.hero.transform.position;
            if (shutterTime>0)
            {
                shutterTime-=dt;
                bool clear=Mathf.Abs(hero.x-CentreX)>1.2f || hero.y<1.62f || hero.y>6.1f;
                if (shutterTime<=0) shutter.SetActive(false);
                else if (clear) shutterBox.enabled=true;
            }
            if (otherPlatformTime>0)
            {
                otherPlatformTime-=dt;
                if (otherPlatformTime<=0) crates[1-dropIndex].SetActive(false);
                else if (Mathf.Abs(hero.x-crates[1-dropIndex].transform.position.x)>1.5f || hero.y>1.85f)
                    crateColliders[1-dropIndex].enabled=true;
            }
            if (dropping)
            {
                float previous=drop.transform.position.y;
                dropTime+=dt;
                float progress=Mathf.Clamp01(dropTime/DropSeconds);
                float y=Mathf.Lerp(8.4f,DropY,progress*progress);
                drop.transform.position=new Vector2(fallingAt.x,y);
                if (dropHurts && !hitByCrate && Mathf.Abs(hero.x-fallingAt.x)<1.3f && hero.y>=y-1.3f && hero.y<=previous+1.25f)
                {
                    hitByCrate=true;
                    game.hero.Hurt(14f);
                    if(disposed)return;
                }
                if (progress>=1)
                {
                    dropping=false; landed=true; platformTime=4f;
                    game.Burst(fallingAt,Steel,8);
                }
            }
            if (landed)
            {
                platformTime-=dt;
                if (platformTime<=0)
                {
                    landed=false; drop.SetActive(false);
                }
                else if (Mathf.Abs(hero.x-drop.transform.position.x)>1.5f || hero.y>1.85f)
                    landingBox.enabled=true;
            }
            if (BladeAway) TickBlade(dt,hero);
            if (disposed) return;
            SyncAnchors();
        }

        public bool TryAttach(Vector2 selected,out Transform support)
        {
            support=null;
            if (disposed) return false;
            if (BladeStuck && (selected-BladeSocket).sqrMagnitude<.18f*.18f)
            {support=freight;wireRecoveryTime=3.5f;return true;}
            if ((selected-CargoGrip).sqrMagnitude<.18f*.18f)
            {support=freight;return true;}
            if ((dropping||landed) && !usedDropHook && (selected-DropGrip).sqrMagnitude<.18f*.18f)
            {
                usedDropHook=true;
                temporaryGrip.color=Steel;
                game.anchors[slots+3]=Unavailable;
                support=drop.transform;
                return true;
            }
            return false;
        }

        public bool ThrowBlade()
        {
            if (disposed || !game.hero || !game.hero.Combat) return false;
            if (BladeAway || game.hero.Combat.Firing || game.hero.Combat.BladeAnimating) return true;
            bladePosition=game.hero.WireOrigin;
            bladeSide=bladePosition.x<CargoPoint.x?-1:1;
            var wall=Physics2D.Linecast(bladePosition,BladeSocket,1<<8);
            if (wall && wall.rigidbody!=freightBody)
            {
                game.Notice("화물이 보이는 곳에서 E / 짐칸에 블레이드 투척",1.4f);
                return true;
            }
            game.hero.Combat.CancelAttacks();
            bladeAge=0;bladeState=BladeState.Flying;
            blade.transform.position=bladePosition;
            blade.SetActive(true);
            game.Sound(3);
            return true;
        }

        void TickBlade(float dt,Vector2 hero)
        {
            if (bladeState==BladeState.Flying)
            {
                bladeAge+=dt;
                Vector2 goal=BladeSocket;
                Vector2 heading=goal-bladePosition;
                if (heading.sqrMagnitude>.001f)
                    blade.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(heading.y,heading.x)*Mathf.Rad2Deg);
                Vector2 next=Vector2.MoveTowards(bladePosition,goal,dt*28f);
                var hit=Physics2D.Linecast(bladePosition,next,1<<8);
                if ((hit && hit.rigidbody!=freightBody) || bladeAge>2.4f)
                {RecoverBlade();return;}
                bladePosition=next;
                blade.transform.position=next;
                if ((next-goal).sqrMagnitude<.01f)
                {
                    bladeState=BladeState.Stuck;
                    blade.transform.SetParent(freight,true);
                    blade.transform.localPosition=new Vector2(bladeSide*1.65f,.05f);
                    game.Burst(goal,Gold,6);
                }
                return;
            }
            Vector2 socket=BladeSocket;
            if (Input.GetKeyDown(KeyCode.R) && game.GameplayInputReady)
            {
                if (recallCooldown<=0 && game.hero.CaptureEnergy>=ExplosionMinimum)
                {
                    float energy=game.hero.SpendCapturedEnergy();
                    RecoverBlade(); recallCooldown=6;
                    float reach=2f+energy*.045f;
                    game.Impact(socket,Gold,true);
                    game.Burst(socket,Blue,16);
                    for (int i=game.enemies.Count-1;i>=0;i--)
                    {
                        var foe=game.enemies[i];
                        if (!foe || !foe.IsAlive || (foe.AimPoint-socket).sqrMagnitude>reach*reach) continue;
                        foe.Hit(10f+.4f*energy,true);
                        if (disposed) return;
                    }
                    return;
                }
                game.Notice(recallCooldown>0?"폭발 회수 재충전 중":"폭발 회수에 입자 10 이상 필요",1.2f);
            }
            float reachDirect=wireRecoveryTime>0?1.85f:1.2f;
            if ((hero+Vector2.up*.26f-socket).sqrMagnitude<=reachDirect*reachDirect) RecoverBlade();
        }

        void RecoverBlade()
        {
            bladeState=BladeState.Held;
            wireRecoveryTime=0;
            blade.transform.SetParent(scenery.transform,true);
            blade.SetActive(false);
            game.anchors[slots+4]=Unavailable;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed=true;
            if (game)
            {
                for(int i=0;i<5;i++)game.anchors[slots+i]=Unavailable;
                if(game.hero && game.hero.Wired)game.hero.ReleaseWire();
            }
            if(scenery) {scenery.SetActive(false);Object.Destroy(scenery);}
        }
    }
}
