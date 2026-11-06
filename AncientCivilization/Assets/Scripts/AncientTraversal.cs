using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Optional factory roof trial. Ordinary city roads no longer double as forced jump courses.
    public sealed class AncientTraversal : MonoBehaviour
    {
        AncientGame game;AncientAirTarget[] targets;GUIStyle label;
        bool lowerRunning,lowerFinished;float lowerElapsed,lowerBest;
        int shownElapsed=-1,shownBest=-2;string timerText="",elapsedText="",bestText="";
        System.Globalization.CultureInfo timerCulture;
        AncientMovingAnchor[] cranes;
        public void Initialize(AncientGame owner)
        {
            game=owner;
            lowerBest=AncientGame.Saved("factoryBestMs")/1000f;
            cranes=new AncientMovingAnchor[3];
            for(int i=0;i<cranes.Length;i++)
            {
                var o=new GameObject("Factory crane wire anchor "+i);o.transform.SetParent(transform,false);
                cranes[i]=o.AddComponent<AncientMovingAnchor>();
                cranes[i].Initialize(game,new Vector2(38+i*22,7.6f),i);
            }
            foreach(var ledge in AncientTraversalLayout.LowerRoute)
                game.StaticBox("Factory roof route",new Vector2((ledge.left+ledge.right)/2,ledge.top+.025f),new Vector2(ledge.right-ledge.left,.05f),AncientGame.Cyan,4);
            foreach(var ledge in AncientTraversalLayout.LowerFastRoute)
                game.StaticBox("Factory crane shortcut",new Vector2((ledge.left+ledge.right)/2,ledge.top+.025f),new Vector2(ledge.right-ledge.left,.05f),new Color(1,.8f,.18f),4);
            targets=new AncientAirTarget[2];
            for(int i=0;i<targets.Length;i++)
            {
                var o=new GameObject("Factory roof rebound buoy "+i);o.transform.SetParent(transform,false);
                targets[i]=o.AddComponent<AncientAirTarget>();targets[i].Initialize(game,new Vector2(52+i*24,6.2f+i*.4f));
            }
        }
        public void ResetForTravel()
        {lowerRunning=lowerFinished=false;lowerElapsed=0;}

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
            if(!game||!game.hero||!game.Story||game.Paused||(game.Campaign&&game.Campaign.Active))return;
            if(!game.Story.InUpperCity)UpdateLower();
        }

        public bool TryMovingAnchor(Vector2 point,out Transform target)
        {
            target=null;
            foreach(var crane in cranes)
                if(((Vector2)crane.transform.position-point).sqrMagnitude<.04f){target=crane.transform;return true;}
            return false;
        }
        void UpdateLower()
        {
            var hero=game.hero;Vector2 p=hero.transform.position;
            // Factory roof route starts only on foot above the road, after initial training.
            if(hero.bike||game.Story.Stage<4)return;
            if(!lowerRunning&&!lowerFinished&&p.x>=23&&p.x<31&&p.y>3.5f)
            {lowerRunning=true;lowerElapsed=0;}
            if(lowerRunning)lowerElapsed+=Time.deltaTime;
            // A fall exits the roof trial; the existing road is a safe return route.
            if(lowerRunning&&p.y<2.1f)
            {lowerRunning=false;lowerElapsed=0;game.Notice("옥상 경로 이탈 / 도로는 안전 경로입니다. 서쪽 입구에서 재도전하세요.",2);}
            if(lowerRunning&&p.x>=AncientTraversalLayout.LowerFinish&&p.y>3)
            {
                lowerRunning=false;lowerFinished=true;
                if(lowerBest<=0||lowerElapsed<lowerBest)
                {lowerBest=lowerElapsed;AncientGame.SetSaved("factoryBestMs",Mathf.RoundToInt(lowerBest*1000));game.Save();}
                game.Notice("공장 옥상 돌파 / "+lowerElapsed.ToString("F2")+"초",4);
            }
            // Conveyor roofs provide a boost but never affect the road or mounted motorcycle.
            if(hero.IsOnGround)
            {
                if(p.x>=31&&p.x<=36&&Mathf.Abs(p.y-4.65f)<.22f)hero.ConveyorDrift(1);
                if(p.x>=73&&p.x<=78&&Mathf.Abs(p.y-4.55f)<.22f)hero.ConveyorDrift(-1);
            }
        }
        public bool TryInteract()
        {
            if(!game||!game.hero||game.Story.CurrentFloor>1)return false;
            if(game.City&&game.City.ArenaActive)return false;
            float x=game.hero.transform.position.x;
            if(!game.Story.InUpperCity)
            {
                if(game.hero.bike||game.Story.Stage<4||!((x>21&&x<26)||(lowerFinished&&x>=94&&x<=98&&game.hero.transform.position.y>3)))return false;
                lowerRunning=lowerFinished=false;lowerElapsed=0;
                game.Notice("옥상 기록 초기화 / 점프와 와이어로 서쪽 옥상에 올라 출발하세요.",2);return true;
            }
            return false;
        }
        void Sign(Vector2 position,string text,float width=360)
        {
            var p=game.View.WorldToScreenPoint(position);
            if(p.z<=0||p.x<-width||p.x>Screen.width+width)return;
            GUI.Label(new Rect(p.x-width/2,Screen.height-p.y-18,width,65),text,label);
        }
        string TrialTimer()
        {
            // Repaint can run far faster than the displayed hundredths. A stopped
            // timer also keeps its text while the player moves around the roof.
            int elapsed=Mathf.RoundToInt(lowerElapsed*100),best=lowerBest>0?Mathf.RoundToInt(lowerBest*100):-1;
            var culture=System.Globalization.CultureInfo.CurrentCulture;bool cultureChanged=!object.ReferenceEquals(timerCulture,culture);
            bool elapsedChanged=elapsed!=shownElapsed,bestChanged=best!=shownBest;
            if(!cultureChanged&&!elapsedChanged&&!bestChanged)return timerText;
            if(cultureChanged||elapsedChanged)elapsedText=(elapsed/100f).ToString("F2",culture);
            if(cultureChanged||bestChanged)bestText=best<0?"—":(best/100f).ToString("F2",culture)+"초";
            shownElapsed=elapsed;shownBest=best;timerCulture=culture;
            timerText="공장 옥상 "+elapsedText+"초\n최고 "+bestText;return timerText;
        }
        void OnGUI()
        {
            if(!game||!game.Story||game.Paused||game.MainMenu||game.Story.CurrentFloor>1||(game.Campaign&&game.Campaign.Active)||Event.current.type!=EventType.Repaint)return;
            if(label==null)label=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=15,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;GUI.matrix=Matrix4x4.identity;GUI.depth=-10;
            if(!game.Story.InUpperCity)
            {
                Sign(new Vector2(24,5.7f),"하층 공장 운송로 / 점프·와이어로 옥상 진입\nE: 기록 초기화 · 도로: 안전 경로");
                Sign(new Vector2(33,6.5f),"운송 벨트 → 가속 후 와이어 연결\n움직이는 크레인에 연결해 다음 옥상으로");
                Sign(new Vector2(75,6.5f),"역방향 벨트 ←\n스윙으로 넘거나 높은 금색 발판으로 우회");
                Sign(new Vector2(96,5.7f),"공장 운송로 끝 / E: 다시 도전");
                if(game.hero.transform.position.y>2.5f)
                    GUI.Label(new Rect(Screen.width-350,90,330,70),TrialTimer(),label);
                GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;return;
            }
            // The living district is a normal street; dedicated field sectors carry the wire trials.
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
    }
    public sealed class AncientMovingAnchor : MonoBehaviour
    {
        AncientGame game;Vector2 centre;int slot,index;LineRenderer rail;SpriteRenderer node;bool shown=true;
        public void Initialize(AncientGame owner,Vector2 point,int number)
        {
            game=owner;centre=point;index=number;slot=game.anchors.Count;game.anchors.Add(point);
            transform.position=point;
            node=AncientArt.Display("Moving crane hook",AncientArt.WireParticle(),Vector2.zero,5,transform);
            node.transform.localScale=Vector3.one*.55f;node.color=new Color(1,.8f,.2f);
            rail=game.Line("Crane overhead rail",new Color(.3f,.45f,.5f),.045f,2);
            rail.SetPosition(0,point+new Vector2(-3,.6f));rail.SetPosition(1,point+new Vector2(3,.6f));
        }
        void Update()
        {
            bool visible=AncientChapterView.NearCamera(game,centre,AncientCityLayout.VisualSector(centre.x));
            if(shown!=visible){shown=visible;node.enabled=rail.enabled=visible;}
            if(!visible||game.Paused)return;
            Vector2 point=centre+new Vector2(AncientRules.CraneOffset(Time.time,index),0);
            transform.position=point;game.anchors[slot]=point;
        }
        void OnDestroy(){if(rail)Destroy(rail.gameObject);}
    }
    public sealed class AncientAirTarget : MonoBehaviour
    {
        AncientGame game;Vector2 centre;SpriteRenderer core,ring;float readyAt,phase;bool shown=true;
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
            bool visible=AncientChapterView.NearCamera(game,centre,AncientCityLayout.VisualSector(centre.x));
            if(shown!=visible){shown=visible;core.enabled=ring.enabled=visible;}
            if(!visible||game.Paused)return;
            transform.position=centre+Vector2.up*(Mathf.Sin(Time.time*1.8f+phase)*.18f);
            ring.transform.Rotate(0,0,55*Time.deltaTime);
            core.color=Ready?new Color(1,.9f,.2f):new Color(.18f,.25f,.3f);
        }
    }
}
