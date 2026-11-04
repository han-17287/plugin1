using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Upper-city maintenance course: a forgiving route below a faster chain of high ledges.
    public sealed class AncientTraversal : MonoBehaviour
    {
        AncientGame game;AncientAirTarget[] targets;GUIStyle label;
        bool running,finished;float elapsed,best;int falls;Vector2 recovery=new Vector2(136,1.3f);
        public void Initialize(AncientGame owner)
        {
            game=owner;best=AncientGame.Saved("courseBestMs")/1000f;
            targets=new AncientAirTarget[AncientTraversalLayout.TargetX.Length];
            for(int i=0;i<targets.Length;i++)
            {
                var o=new GameObject("Blade rebound buoy "+i);o.transform.SetParent(transform,false);
                targets[i]=o.AddComponent<AncientAirTarget>();
                targets[i].Initialize(game,new Vector2(AncientTraversalLayout.TargetX[i],AncientTraversalLayout.TargetY[i]));
            }
            Mark(139,"Upper route start",AncientGame.Cyan);
            Mark(252,"Upper route finish",new Color(1,.85f,.2f));
            foreach(var ledge in AncientTraversalLayout.FastRoute)
                game.Box("Fast route amber edge",new Vector2((ledge.left+ledge.right)/2,ledge.top+.025f),new Vector2(ledge.right-ledge.left,.05f),new Color(1,.8f,.18f),4);
            foreach(var ledge in AncientTraversalLayout.MainRoute)
                game.Box("Main route cyan edge",new Vector2((ledge.left+ledge.right)/2,ledge.top+.025f),new Vector2(ledge.right-ledge.left,.05f),AncientGame.Cyan,4);
        }
        void Mark(float x,string name,Color color)
        {
            game.Box(name,new Vector2(x,2),new Vector2(.08f,4),color,2);
            game.Box(name+" lamp",new Vector2(x,4.05f),new Vector2(.5f,.15f),color,3);
        }
        public bool TryBladeTarget(Vector2 origin,Vector2 direction)
        {
            // One physical target per slash; a single swing never stacks multiple rebounds.
            AncientAirTarget nearest=null;float bestDistance=3.1f*3.1f;
            foreach(var target in targets)
            {
                if(!target||!target.Ready)continue;
                Vector2 delta=(Vector2)target.transform.position-origin;
                if(delta.sqrMagnitude>=bestDistance||Vector2.Dot(delta.normalized,direction)<-.15f)continue;
                if(Physics2D.Linecast(origin,target.transform.position,1<<8))continue;
                nearest=target;bestDistance=delta.sqrMagnitude;
            }
            if(!nearest)return false;nearest.Strike();return true;
        }
        void Update()
        {
            if(!game||!game.hero||!game.Story||!game.Story.InUpperCity||game.Paused)return;
            var hero=game.hero;Vector2 p=hero.transform.position;
            if(running)elapsed+=Time.deltaTime;
            if(!running&&!finished&&p.x>=AncientTraversalLayout.Start&&p.x<AncientTraversalLayout.Finish)
            {running=true;elapsed=0;falls=0;}
            if(running&&p.y>0)
            {
                if(p.x>=214)recovery=new Vector2(217,4.3f);
                else if(p.x>=172)recovery=new Vector2(174.5f,4.1f);
            }
            if(p.x>=139&&p.x<=264&&p.y<-2.5f)
            {
                falls++;hero.TravelTo(recovery);game.SnapCameraToHero();
                game.Notice("안전 회수 / 마지막 정비 발판에서 다시 시작합니다.",1.5f);
            }
            if(running&&p.x>=AncientTraversalLayout.Finish)
            {
                running=false;finished=true;
                // Falls are allowed, but only a clean run can replace a personal best.
                if(falls==0&&(best<=0||elapsed<best))
                {best=elapsed;AncientGame.SetSaved("courseBestMs",Mathf.RoundToInt(best*1000));game.Save();}
                game.Notice("정비 통로 돌파 / "+elapsed.ToString("F2")+"초 · 안전 회수 "+falls+"회",5);
            }
        }
        public bool TryInteract()
        {
            if(!game||!game.hero||!game.Story.InUpperCity)return false;
            float x=game.hero.transform.position.x;
            if(!((x>134.5f&&x<138.5f)||(finished&&x>=250)))return false;
            running=finished=false;elapsed=0;falls=0;recovery=new Vector2(136,1.3f);
            game.hero.TravelTo(recovery);game.BlockGameplayInput();game.SnapCameraToHero();return true;
        }
        void Sign(Vector2 position,string text,float width=360)
        {
            var p=game.View.WorldToScreenPoint(position);
            if(p.z<=0||p.x<-width||p.x>Screen.width+width)return;
            GUI.Label(new Rect(p.x-width/2,Screen.height-p.y-18,width,65),text,label);
        }
        void OnGUI()
        {
            if(!game||!game.Story||!game.Story.InUpperCity||game.Paused||game.MainMenu)return;
            if(label==null)label=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=15,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;GUI.matrix=Matrix4x4.identity;GUI.depth=-10;
            Sign(new Vector2(136,4.5f),"상층 정비 통로\n청록: 기본 경로 / 금색: 숙련 지름길");
            Sign(new Vector2(145,5),"좌클릭 견인 → 가운데 클릭 스윙\n다른 앵커로 연결해 속도를 유지하세요");
            Sign(new Vector2(158,7.7f),"공중 부유체: 공격 모드 좌클릭으로 반동\n휠 아래로 와이어 전환 → 다음 앵커");
            Sign(new Vector2(196,4.2f),"금색 상단 경로는 착지 없이 연결 가능\n실패하면 마지막 안전 발판으로 회수");
            Sign(new Vector2(255,4.6f),"통로 끝 / E: 다시 도전\n엘리베이터는 서쪽 입구에 있습니다");
            string status=finished?"완주 ":running?"진행 ":"정비 통로 ";
            GUI.Label(new Rect(Screen.width-350,90,330,70),status+elapsed.ToString("F2")+"초 / 회수 "+falls+"\n최고 "+(best>0?best.ToString("F2")+"초":"—"),label);
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
    }
    public sealed class AncientAirTarget : MonoBehaviour
    {
        AncientGame game;Vector2 centre;SpriteRenderer core,ring;float readyAt,phase;
        public bool Ready {get{return Time.time>=readyAt;}}
        public void Initialize(AncientGame owner,Vector2 point)
        {
            game=owner;centre=point;phase=point.x;transform.position=point;
            ring=AncientArt.Display("Ancient rebound shell",AncientArt.WireParticle(),Vector2.zero,11,transform);
            ring.transform.localScale=Vector3.one*.8f;ring.color=new Color(.1f,.55f,.8f);
            core=AncientArt.Display("Blade reactive amber core",AncientArt.WireParticle(),Vector2.zero,12,transform);
            core.transform.localScale=Vector3.one*.36f;core.color=new Color(1,.85f,.16f);
        }
        public void Strike()
        {
            if(!Ready)return;readyAt=Time.time+.85f;
            game.Burst(transform.position,new Color(1,.9f,.2f),8);game.Sound(2);
        }
        void Update()
        {
            if(game.Paused)return;
            transform.position=centre+Vector2.up*(Mathf.Sin(Time.time*1.8f+phase)*.18f);
            ring.transform.Rotate(0,0,55*Time.deltaTime);
            core.color=Ready?new Color(1,.9f,.2f):new Color(.18f,.25f,.3f);
        }
    }
}
