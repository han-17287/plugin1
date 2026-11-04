using System.Collections.Generic;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientCityCampaign : MonoBehaviour
    {
        AncientGame game;int activeFloor=-1;AncientEnemy activeBoss;GUIStyle label;Vector2 safeRecovery;
        readonly List<AncientAirTarget> targets=new List<AncientAirTarget>();
        public int Cleared {get;private set;}
        public bool ArenaActive {get{return activeBoss&&activeBoss.IsAlive;}}
        public void Initialize(AncientGame owner)
        {
            game=owner;Cleared=AncientGame.Saved("cityCleared");
            game.Platform(264,282,0);
            for(int floor=2;floor<=4;floor++)BuildFloor(floor);
        }
        void BuildFloor(int floor)
        {
            float x=AncientCityLayout.Left(floor),right=AncientCityLayout.Right(floor);
            foreach(var ledge in AncientCityLayout.Route(floor))game.Platform(ledge.left,ledge.right,ledge.top);
            game.Platform(x,right,-4.5f);game.Ceiling(x-2,right+2,10);
            // Fast routes have smaller landings but share the same continuous wire ceiling.
            for(int i=0;i<4;i++)
            {
                float a=x+21+i*23;game.Platform(a,a+2.5f,6.2f);
                game.Box("Expert route edge",new Vector2(a+1.25f,6.23f),new Vector2(2.5f,.045f),new Color(1,.8f,.2f),4);
            }
            var tint=floor==2?new Color(.1f,.22f,.3f):floor==3?new Color(.2f,.24f,.21f):new Color(.19f,.12f,.21f);
            for(int i=0;i<12;i++)
            {
                float bx=x+5+i*10;
                game.Box("District wall panel",new Vector2(bx,4.7f),new Vector2(8.5f,9.4f),tint,-7);
                for(int row=0;row<3;row++)
                    game.Box("District window",new Vector2(bx,3.7f+row*1.7f),new Vector2(3.5f,.75f),tint*1.8f,-6);
            }
            foreach(float bx in new[]{x-1,right+1})
            {
                var wall=game.Box("District boundary",new Vector2(bx,4),new Vector2(.4f,12),tint,0);
                wall.layer=8;wall.AddComponent<BoxCollider2D>();
            }
            if(floor<4)game.BuildLiftTerminal(AncientCityLayout.Entry(floor));
            else
            {
                game.Box("Illegal maintenance hatch",new Vector2(x+4,1.5f),new Vector2(2.6f,3),new Color(.12f,.08f,.16f),1);
                game.Box("Sealed chairman office",new Vector2(right-2,2),new Vector2(2,4),new Color(.23f,.13f,.18f),1);
            }
            for(int i=0;i<3;i++)
            {
                float gx=x+32+i*27;
                if(floor==2)
                {
                    var o=new GameObject("Administrative security shutter");o.transform.SetParent(transform,false);
                    o.AddComponent<AncientSecurityShutter>().Initialize(game,new Vector2(gx,6.4f),i);
                }
                else if(floor==3)
                {
                    var o=new GameObject("Residential ventilation updraft");o.transform.SetParent(transform,false);
                    o.AddComponent<AncientCityVent>().Initialize(game,new Vector2(x+18+i*24,1.5f),floor);
                    var target=new GameObject("Upper-city blade buoy").AddComponent<AncientAirTarget>();
                    target.transform.SetParent(transform,false);target.Initialize(game,new Vector2(x+29+i*25,6));targets.Add(target);
                }
                else
                {
                    var o=new GameObject("Top-floor collapsing scaffold");o.transform.SetParent(transform,false);
                    o.AddComponent<AncientCrumbleLedge>().Initialize(game,new Vector2(x+31+i*25,4.7f));
                }
            }
        }
        public void OnEnterFloor()
        {
            foreach(var enemy in game.enemies.ToArray())
                if(enemy&&enemy.DistrictFloor>0){game.enemies.Remove(enemy);Destroy(enemy.gameObject);}
            if(game.boss&&game.boss.DistrictFloor>0)game.boss=null;
            foreach(var shot in game.shots.ToArray())if(shot)shot.Remove();
            activeBoss=null;activeFloor=-1;
        }
        AncientEnemy Spawn(float x,float y,int kind,int floor,bool boss)
        {
            var enemy=new GameObject(boss?"District boss":"District criminal").AddComponent<AncientEnemy>();
            enemy.transform.position=new Vector2(x,y);enemy.Initialize(kind);enemy.ConfigureDistrict(floor,boss);
            game.enemies.Add(enemy);if(boss){game.boss=enemy;activeBoss=enemy;}return enemy;
        }
        void Update()
        {
            if(!game||!game.Story||!game.hero||game.Paused||game.Story.CurrentFloor==0)return;
            int floor=game.Story.CurrentFloor;float x=AncientCityLayout.Left(floor);var p=game.hero.transform.position;
            if(activeFloor!=floor)
            {
                activeFloor=floor;safeRecovery=new Vector2(AncientCityLayout.Entry(floor),1.3f);
                if((Cleared&(1<<floor))==0)
                {
                    if(floor==1){Spawn(195,2.5f,0,floor,false);Spawn(240,2,1,floor,false);}
                    else
                    {
                        var route=AncientCityLayout.Route(floor);
                        Spawn((route[2].left+route[2].right)/2,route[2].top,0,floor,false);
                        Spawn((route[6].left+route[6].right)/2,route[6].top,1,floor,false);
                    }
                }
            }
            if(!activeBoss&&(Cleared&(1<<floor))==0&&p.x>=AncientCityLayout.ArenaLeft(floor)+1&&p.y<4)
            {
                Spawn(AncientCityLayout.ArenaRight(floor)-4,0,floor==4?1:3,floor,true);
                game.Notice(floor==4?"저격수 / 조준 전조를 보고 엄폐·접근하세요.":"구역 보스 / 전조를 피하고 공격 후 빈틈을 노리세요.",3);
            }
            if(floor>=2&&game.hero.IsOnGround)
            {
                var route=AncientCityLayout.Route(floor);
                for(int index=4;index<=7;index+=3)
                {
                    var ledge=route[index];
                    if(p.x>=ledge.left&&p.x<=ledge.right&&Mathf.Abs(p.y-(ledge.top+1.05f))<.3f)
                        safeRecovery=new Vector2((ledge.left+ledge.right)/2,ledge.top+1.3f);
                }
            }
            if(floor>=2&&p.y<-2.5f)
            {
                game.hero.TravelTo(ArenaActive?new Vector2(AncientCityLayout.ArenaLeft(floor)+1,1.3f):safeRecovery);game.SnapCameraToHero();
                game.Notice("안전 회수 / 마지막 안전 발판에서 다시 시작합니다.",2);
            }
        }
        public void BossDefeated(int floor)
        {
            activeBoss=null;game.boss=null;
            if((Cleared&(1<<floor))!=0)return;
            Cleared|=1<<floor;AncientGame.SetSaved("cityCleared",Cleared);
            game.Story.OnDistrictCleared(floor);game.Save();
        }
        public bool TryInteract()
        {
            if(!game.Story||!game.hero)return false;int floor=game.Story.CurrentFloor;float x=game.hero.transform.position.x;
            if(floor==3&&x>=AncientCityLayout.ArenaRight(3)-5)
            {
                if(!AncientCityLayout.CanInfiltrate(Cleared,floor)){game.Notice("샛길은 구역 보안 장치를 제압한 뒤 열립니다.");return true;}
                game.Story.TravelBySidePassage(4);return true;
            }
            if(floor==4&&Mathf.Abs(x-AncientCityLayout.Entry(4))<3)
            {game.Story.TravelBySidePassage(3);return true;}
            return false;
        }
        public bool TryBladeTarget(Vector2 origin,Vector2 direction)
        {
            foreach(var target in targets)
            {
                if(!target||!target.Ready)continue;Vector2 delta=(Vector2)target.transform.position-origin;
                if(delta.sqrMagnitude>3.1f*3.1f||Vector2.Dot(delta.normalized,direction)<-.15f||Physics2D.Linecast(origin,target.transform.position,1<<8))continue;
                target.Strike();return true;
            }
            return false;
        }
        void OnGUI()
        {
            if(!game||!game.Story||game.Paused||game.MainMenu)return;
            int floor=game.Story.CurrentFloor;if(floor==0)return;
            if(label==null)label=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=15,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            string hint=floor==1?"생활구 / 부유체 반동 → 와이어 재연결":floor==2?"행정구 / 붉은 예고 뒤 셔터 폐쇄 · 낮은 경로로 우회":floor==3?"거주구 / 환기 상승류로 금색 지름길 진입":"최상층 / 발판 점멸 후 붕괴 · 안정된 아래 경로는 유지";
            if((Cleared&(1<<floor))!=0)hint+=floor==3?"\n동쪽 샛길에서 E / 최상층 불법 침입":floor==4?"\n저격수와의 대치 종료 / 서쪽 샛길로 귀환 가능":"\n다음 층 권한 해금 / 입구 엘리베이터에서 목적 층 선택";
            GUI.Label(new Rect(Screen.width/2-300,95,600,65),hint,label);
            string marker=AncientCityLayout.Name(floor)+" / E: 엘리베이터";
            Vector2 point=new Vector2(AncientCityLayout.Entry(floor),4.2f);
            if(floor==4)marker="외벽 샛길 / E: 상층 거주구로 귀환";
            var pixel=game.View.WorldToScreenPoint(point);
            GUI.Label(new Rect(pixel.x-180,Screen.height-pixel.y-22,360,70),marker,label);
            if(floor==3)
            {
                pixel=game.View.WorldToScreenPoint(new Vector2(AncientCityLayout.ArenaRight(3)-2,4.5f));
                GUI.Label(new Rect(pixel.x-180,Screen.height-pixel.y-22,360,70),"외벽 정비 샛길 / E: 최상층 침입\n최상층 승진·엘리베이터 승인 불가",label);
            }
        }
    }
    public sealed class AncientSecurityShutter : MonoBehaviour
    {
        AncientGame game;SpriteRenderer art;BoxCollider2D solid;Vector2 centre;float phase;
        public void Initialize(AncientGame owner,Vector2 point,int index)
        {
            game=owner;centre=point;phase=index*1.1f;
            var o=game.Box("Telegraphed shutter",point,new Vector2(.35f,3.2f),AncientGame.Cyan,3);o.transform.SetParent(transform,true);
            art=o.GetComponent<SpriteRenderer>();o.layer=8;solid=o.AddComponent<BoxCollider2D>();solid.enabled=false;
        }
        void Update()
        {
            if(game.Paused)return;bool close=AncientCityLayout.ShutterClosed(Time.time,phase);
            // Never materialize a solid shutter inside the character.
            if(game.hero&&Mathf.Abs(game.hero.transform.position.x-centre.x)<.65f&&Mathf.Abs(game.hero.transform.position.y-centre.y)<2.7f)close=false;
            solid.enabled=close;art.color=close?new Color(.8f,.2f,.1f):AncientCityLayout.ShutterWarning(Time.time,phase)?new Color(1,.65f,.1f):new Color(.1f,.7f,.8f,.2f);
        }
    }
    public sealed class AncientCityVent : MonoBehaviour
    {
        AncientGame game;Vector2 centre;int floor;SpriteRenderer plume;
        public void Initialize(AncientGame owner,Vector2 point,int district)
        {
            game=owner;centre=point;floor=district;
            plume=game.Box("Visible ventilation column",point+Vector2.up*2,new Vector2(1.5f,4.5f),new Color(.4f,.9f,.7f,.18f),2).GetComponent<SpriteRenderer>();
            plume.transform.SetParent(transform,true);
        }
        void Update()
        {
            if(game.Paused)return;
            plume.color=new Color(.4f,.9f,.7f,.12f+.1f*(.5f+.5f*Mathf.Sin(Time.time*5)));
            if(game.Story.CurrentFloor!=floor||game.hero.bike)return;
            Vector2 delta=(Vector2)game.hero.transform.position-centre;
            if(Mathf.Abs(delta.x)<1.1f&&delta.y>-.5f&&delta.y<5)game.hero.VentLift();
        }
    }
    public sealed class AncientCrumbleLedge : MonoBehaviour
    {
        AncientGame game;BoxCollider2D solid;SpriteRenderer art;Vector2 centre;float collapseAt=-1,restoreAt;
        public void Initialize(AncientGame owner,Vector2 point)
        {
            game=owner;centre=point;var o=game.Box("Collapsible illegal-access scaffold",point,new Vector2(3,.3f),new Color(1,.65f,.2f),3);
            o.transform.SetParent(transform,true);o.layer=8;solid=o.AddComponent<BoxCollider2D>();art=o.GetComponent<SpriteRenderer>();
        }
        void Update()
        {
            if(game.Paused)return;
            Vector2 delta=(Vector2)game.hero.transform.position-centre;
            if(!solid.enabled)
            {
                if(Time.time>=restoreAt&&(Mathf.Abs(delta.x)>2||Mathf.Abs(delta.y)>1.3f)){solid.enabled=true;art.enabled=true;collapseAt=-1;}
                return;
            }
            if(collapseAt<0&&game.Story.CurrentFloor==4&&game.hero.IsOnGround&&Mathf.Abs(delta.x)<1.7f&&Mathf.Abs(delta.y-1.2f)<.3f)collapseAt=Time.time+.6f;
            if(collapseAt>=0)
            {
                art.color=Color.Lerp(new Color(1,.65f,.2f),Color.white,.5f+.5f*Mathf.Sin(Time.time*35));
                if(Time.time>=collapseAt){solid.enabled=false;art.enabled=false;restoreAt=Time.time+2.5f;}
            }
        }
    }
}
