using System.Collections.Generic;
using UnityEngine;
namespace Plugin.AncientCivilization
{
    public sealed class AncientLivingDistrict : MonoBehaviour
    {
        AncientGame game;readonly List<AncientWireObject> objects=new List<AncientWireObject>();readonly List<AncientWireObject> rockets=new List<AncientWireObject>();
        readonly List<AncientEnemy> guards=new List<AncientEnemy>();readonly List<AncientLivingSpotlight> lights=new List<AncientLivingSpotlight>();
        readonly MissileCue[] missiles=new MissileCue[24];AncientWireObject held;bool wave,announced;int wavesSpawned;float waveAt;
        SpriteRenderer leader;GUIStyle hint;LineRenderer targetRing;AncientLivingAftermath aftermath;
        sealed class MissileCue {public LineRenderer line;public Vector2 start,end;public float until;public bool active;}
        public AncientLivingBoss Boss {get;set;}
        public bool WaveActive {get{return wave;}}
        public bool InputLocked {get{return held&&held.Orbiting||Boss&&Boss.InputLocked;}}
        public bool GrabbingPlayer {get{return Boss&&Boss.InputLocked;}}
        public bool Sucking {get{return Boss&&Boss.Sucking;}}
        public bool ThrowTaught {get{return AncientGame.Saved("livingThrowTaught")!=0;}}
        public void Initialize(AncientGame owner)
        {
            game=owner;
            for(int id=4;id<=7;id++)for(int sector=0;sector<3;sector++)for(int block=0;block<AncientFieldExpansion.Blocks(id);block++)
            {
                float left=AncientChapterLayout.CaseLeft(id)+AncientFieldExpansion.SectorStart(id,sector)+block*AncientFieldExpansion.SectorSpan;
                var segment=AncientFieldExpansion.BlockSegment(id,sector,block,3);var second=AncientFieldExpansion.BlockSegment(id,sector,block,6);
                bool jumping=AncientFieldExpansion.IsJumpSector(id,sector);float baseX=AncientChapterLayout.CaseLeft(id),cargoX=AncientFieldExpansion.TallStreetBay(id,sector,block)?58:52;
                AddObject(new Vector2(jumping?baseX+(segment.left+segment.right)*.5f:left+cargoX,(jumping?segment.top:AncientFieldExpansion.StreetHeight(id,sector,block,cargoX))+1),sector%2==1);
                AddObject(new Vector2(jumping?baseX+(second.left+second.right)*.5f:left+148,(jumping?second.top:AncientFieldExpansion.StreetHeight(id,sector,block,148))+1),false);
                if(id>=6)
                {
                    var support=AncientFieldExpansion.BlockSegment(id,sector,block,4);float coverX=jumping?baseX+(support.left+support.right)*.5f:left+103;
                    var spot=new GameObject("Gang searchlight").AddComponent<AncientLivingSpotlight>();spot.Initialize(game,this,new Vector2(coverX+5,8.7f),(sector+id)%2==0);lights.Add(spot);
                    var cover=game.Box("Searchlight cover column",new Vector2(coverX,support.top+1.8f),new Vector2(1.5f,3.6f),new Color(.17f,.23f,.29f),2);cover.layer=8;cover.AddComponent<BoxCollider2D>();
                }
                // Forward environmental clues are visible on the continuous floor, not separate quest-room markers.
                for(int j=0;j<4;j++)game.StaticBox("Core crate drag trail",new Vector2(left+24+j*36,(jumping?0:AncientFieldExpansion.StreetHeight(id,sector,block,24+j*36))+.035f),new Vector2(2,.07f),new Color(.25f,.62f,.76f),3);
            }
            for(int i=0;i<5;i++)AddObject(new Vector2(AncientCityLayout.ArenaLeft(1)+3+i*6,1),i==0||i==4);
            for(int i=0;i<12;i++)
            {var rocket=new GameObject("Reusable counterable rocket fist").AddComponent<AncientWireObject>();rocket.Initialize(game,this,Vector2.zero,false,true);objects.Add(rocket);rockets.Add(rocket);}
            for(int i=0;i<missiles.Length;i++)
            {var line=game.Line("Missile ignition warning",new Color(1,.5f,.15f,.65f),.035f,16);line.transform.SetParent(transform,true);line.enabled=false;missiles[i]=new MissileCue{line=line};}
            targetRing=game.Line("Throwable target bracket",AncientGame.Cyan,.045f,21);targetRing.loop=true;targetRing.positionCount=4;targetRing.enabled=false;
            leader=AncientArt.Display("Gang leader above hangar",AncientArt.Enemy(5),new Vector2(AncientCityLayout.ArenaRight(1)-6,6.4f),9,transform);leader.enabled=false;
            game.StaticBox("Gang leader gantry",new Vector2(AncientCityLayout.ArenaRight(1)-6,6.2f),new Vector2(5,.25f),new Color(.3f,.38f,.42f),3);
            if((AncientGame.Saved("cityCleared")&2)!=0)BeginAftermath(null,new Vector2(AncientCityLayout.ArenaRight(1)-6,0),true);
        }
        public void BeginAftermath(Transform rig,Vector2 point,bool restored=false)
        {
            if(aftermath)return;
            aftermath=new GameObject("Living district arrest scene").AddComponent<AncientLivingAftermath>();aftermath.transform.SetParent(transform,false);
            aftermath.Initialize(game,rig,leader,point,restored);leader.enabled=!restored;
        }
        public bool TryInteract(){return aftermath&&aftermath.TryInteract();}
        public void AddRouteObject(Vector2 point,bool car){AddObject(point,car);}
        void AddObject(Vector2 point,bool car)
        {var o=new GameObject(car?"Wire throwable car":"Wire throwable crate").AddComponent<AncientWireObject>();o.Initialize(game,this,point,car);objects.Add(o);}
        AncientWireObject Pick(Vector2 cursor)
        {
            AncientWireObject selected=null;float best=2.2f*2.2f;
            foreach(var o in objects)
            {
                if(!o||!o.Selectable||(o.Point-(Vector2)game.hero.transform.position).sqrMagnitude>23*23)continue;
                float d=(o.Point-cursor).sqrMagnitude;if(d>=best||Physics2D.Linecast(game.hero.WireOrigin,o.Point,1<<8))continue;best=d;selected=o;
            }
            return selected;
        }
        public bool TryWireTarget(out Vector2 point)
        {
            point=Vector2.zero;if(!game||!game.Story||game.Story.CurrentFloor!=1||InputLocked)return false;
            var target=Pick(game.View.ScreenToWorldPoint(Input.mousePosition));if(!target)return false;point=target.Point;return true;
        }
        public bool TryThrow(Vector2 cursor)
        {
            if(game.Story.CurrentFloor!=1||held)return false;var target=Pick(cursor);if(!target)return false;
            held=target;target.Grab();return true;
        }
        public void ReleaseObject(AncientWireObject obj){if(held==obj)held=null;}
        public void RecordThrow(){if(!ThrowTaught){AncientGame.SetSaved("livingThrowTaught",1);game.Save();game.Story.RadioBark("사령관","물체를 적에게 던지고, 부서진 입자는 우클릭으로 수집해라.");}}
        public bool TryBlade(Vector2 origin,Vector2 direction)
        {
            if(game.Story.CurrentFloor!=1)return false;
            foreach(var o in objects)
            {if(!o||!o.Selectable||rockets.Contains(o))continue;Vector2 delta=o.Point-origin;if(delta.sqrMagnitude<9&&Vector2.Dot(delta.normalized,direction)>-.15f&&!Physics2D.Linecast(origin,o.Point,1<<8)){o.BreakByBlade();return true;}}
            return false;
        }
        public void LaunchRocket(AncientLivingBoss boss,Vector2 start,bool pair)
        {foreach(var r in rockets)if(r.Available){r.LaunchRocket(boss,start,pair);return;}}
        public void ClearRockets()
        {foreach(var r in rockets)if(r)r.ResetObject();foreach(var m in missiles)if(m!=null){m.active=false;m.line.enabled=false;}}
        public void QueueMissile(Vector2 start,Vector2 target,float delay)
        {foreach(var m in missiles)if(!m.active){m.active=true;m.start=start;m.end=target;m.until=Time.time+delay;m.line.enabled=true;m.line.SetPosition(0,start);m.line.SetPosition(1,target);return;}}
        public bool PrepareBoss()
        {
            if(AncientGame.Saved("livingGuardsCleared")!=0)return true;
            if(!wave)
            {
                wave=true;wavesSpawned=0;waveAt=Time.time+.8f;leader.enabled=true;
                game.CombatCheckpoint(1,new Vector2(AncientCityLayout.ArenaLeft(1)+2,1.3f));
                game.Story.Conversation(null,3,new[]{"두목","백 경위"},new[]{"경찰 하나야. 여기까지 오게 둔 놈들이 직접 막아.","훔친 코어를 내려놓고 투항해라."});
            }
            return false;
        }
        public bool TryActor(string speaker,out Transform actor,out float height)
        {
            actor=null;height=2;
            if(speaker!="두목"||!game||game.Story.CurrentFloor!=1)return false;
            if(Boss){actor=Boss.transform;height=5.8f;return true;}
            if(leader&&leader.enabled){actor=leader.transform;return true;}return false;
        }
        public void ResetForTravel()
        {
            if(Boss)Boss.StopBattle();Boss=null;held=null;wave=false;wavesSpawned=0;leader.enabled=false;
            foreach(var o in objects)if(o)o.ResetObject();foreach(var guard in guards)if(guard){game.enemies.Remove(guard);Destroy(guard.gameObject);}guards.Clear();
            foreach(var light in lights)if(light)light.ResetAlarm();ClearRockets();
        }
        public void SpawnGuard(Vector2 point,bool drone)
        {
            if(drone)
            {
                var support=Physics2D.Raycast(new Vector2(point.x,9.6f),Vector2.down,13,1<<8);
                if(support)point.y=Mathf.Min(8.2f,Mathf.Max(point.y,support.point.y+.65f));
            }
            var e=new GameObject(drone?"Gang guard robot":"Gang hangar guard").AddComponent<AncientEnemy>();e.Initialize(drone?4:0);e.ConfigureDistrict(1,false);e.transform.position=point;game.enemies.Add(e);guards.Add(e);
        }
        public int NearbyGuards(float x)
        {int n=0;foreach(var guard in game.enemies)if(guard&&guard.IsAlive&&!guard.IsBoss&&guard.DistrictFloor==1&&Mathf.Abs(guard.transform.position.x-x)<35)n++;return n;}
        void Update()
        {
            if(!game||!game.hero||!game.Story||game.Paused)return;
            if(game.Story.CurrentFloor!=1){targetRing.enabled=false;return;}
            if(!announced&&game.Campaign.CurrentRoom>=5)
            {announced=true;game.Story.RadioBark("사령관","파란 고리가 보이는 상자를 와이어로 잡아라. 한 바퀴 돌리는 동안 마우스로 던질 방향을 정할 수 있다.");}
            if(wave)
            {
                if(NearbyGuards((AncientCityLayout.ArenaLeft(1)+AncientCityLayout.ArenaRight(1))*.5f)==0&&Time.time>=waveAt)
                {
                    if(wavesSpawned<2)
                    {SpawnGuard(new Vector2(AncientCityLayout.ArenaLeft(1)+12,0),false);SpawnGuard(new Vector2(AncientCityLayout.ArenaRight(1)-7,3),true);wavesSpawned++;waveAt=Time.time+1;}
                    else {wave=false;leader.enabled=false;AncientGame.SetSaved("livingGuardsCleared",1);game.CombatCheckpoint(1,new Vector2(AncientCityLayout.ArenaLeft(1)+2,1.3f));game.Save();}
                }
            }
            foreach(var m in missiles)if(m.active&&Time.time>=m.until)
            {m.active=false;m.line.enabled=false;if(game.shots.Count<60){var shot=game.Projectiles.Spawn(m.start,(m.end-m.start).normalized*11,18,false,0,0);if(shot)shot.SetLivingProjectile(true);}}
            var selected=!game.hero.AttackMode&&!InputLocked?Pick(game.View.ScreenToWorldPoint(Input.mousePosition)):null;
            targetRing.enabled=selected;
            if(selected)
            {Vector2 c=selected.Point;for(int i=0;i<4;i++){float a=(i+.5f)*Mathf.PI*.5f;targetRing.SetPosition(i,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*1.2f);}}
        }
        void OnGUI()
        {
            if(!game||!game.Story||game.Story.CurrentFloor!=1||game.MainMenu||game.Paused||Event.current.type!=EventType.Repaint)return;
            if(hint==null)hint=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=14,alignment=TextAnchor.MiddleCenter};
            if(Boss)GUI.Label(new Rect(Screen.width/2-280,158,560,26),"거대 로봇 두목 / "+Boss.Phase+"페이즈"+(Boss.Barrier>0?" / 배리어 "+Boss.Barrier+" / 70":""),hint);
            else if(wave)GUI.Label(new Rect(Screen.width/2-240,158,480,26),"본거지 경비 제압 / "+wavesSpawned+" / 2",hint);
            else if(targetRing.enabled)GUI.Label(new Rect(Screen.width/2-220,Screen.height-108,440,28),"좌클릭: 와이어로 한 바퀴 돌려 투척 / 회전 중 조준",hint);
        }
    }
    public sealed class AncientLivingSpotlight : MonoBehaviour
    {
        AncientGame game;AncientLivingDistrict district;Vector2 origin,locked;bool robot;LineRenderer cone,beam;float scan,alarm,cooldown;SpriteRenderer lamp;
        public void Initialize(AncientGame g,AncientLivingDistrict d,Vector2 point,bool robotGuard)
        {
            game=g;district=d;origin=point;robot=robotGuard;transform.position=point;scan=point.x*.037f;
            lamp=AncientArt.Display("Searchlight lamp",AncientLivingArt.Part(6),point,5);lamp.transform.SetParent(transform,true);
            cone=game.Line("Searchlight cone",new Color(1,.8f,.3f,.3f),.05f,4);cone.transform.SetParent(transform,true);cone.positionCount=3;
            beam=game.Line("Searchlight security laser",AncientGame.Orange,.8f,14);beam.transform.SetParent(transform,true);beam.enabled=false;
        }
        public void ResetAlarm(){alarm=cooldown=0;beam.enabled=false;}
        void Update()
        {
            if(!game||!game.hero||!game.Story)return;
            bool near=game.Story.CurrentFloor==1&&Mathf.Abs(game.hero.transform.position.x-origin.x)<30;lamp.enabled=cone.enabled=near;
            if(!near){alarm=0;beam.enabled=false;return;}if(game.Paused)return;
            float dt=Time.deltaTime;scan+=dt;cooldown=Mathf.Max(0,cooldown-dt);
            float a=-Mathf.PI*.5f+Mathf.Sin(scan*.65f)*.65f;Vector2 direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
            for(int i=0;i<3;i++)
            {float t=a+(i==0?-.22f:.22f);Vector2 end=origin+new Vector2(Mathf.Cos(t),Mathf.Sin(t))*12;var wall=Physics2D.Linecast(origin,end,1<<8);cone.SetPosition(i,i==1?origin:wall?wall.point:end);}
            if(alarm>0)
            {
                alarm+=dt;beam.enabled=!robot;beam.startWidth=beam.endWidth=alarm<1.1f?.04f:.8f;beam.SetPosition(0,origin);beam.SetPosition(1,locked);
                if(alarm>=1.1f&&robot)
                {if(district.NearbyGuards(origin.x)<2)district.SpawnGuard(new Vector2(origin.x+3,3),true);alarm=0;cooldown=8;}
                else if(alarm>=1.1f)
                {
                    Vector2 p=game.hero.transform.position;var wall=Physics2D.Linecast(origin,locked,1<<8);Vector2 end=wall?wall.point:locked;
                    beam.SetPosition(1,end);if(AncientLivingRules.BeamHit(origin.x,origin.y,end.x,end.y,p.x,p.y,.85f)&&!Physics2D.Linecast(origin,p,1<<8))game.hero.Hurt(20);
                    if(alarm>=1.5f){alarm=0;cooldown=5;beam.enabled=false;}
                }
                return;
            }
            Vector2 delta=(Vector2)game.hero.transform.position-origin;
            if(cooldown<=0&&AncientLivingRules.InSpotlight(delta.x,delta.y,direction.x,direction.y,12,Mathf.Cos(.22f))&&!Physics2D.Linecast(origin,game.hero.transform.position,1<<8))
            {alarm=.001f;locked=game.hero.transform.position;game.Notice(robot?"발각 / 경비 로봇 접근":"발각 / 보안 레이저 조준",1.2f);game.Sound(3);}
        }
    }
}
