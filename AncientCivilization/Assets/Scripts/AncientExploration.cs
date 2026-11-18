using UnityEngine;
namespace Plugin.AncientCivilization
{
    public sealed class AncientExploration : MonoBehaviour
    {
        AncientGame game;public int CacheMask {get;private set;}
        public void Initialize(AncientGame owner)
        {
            game=owner;CacheMask=AncientGame.Saved("routeCaches");
            for(int floor=0;floor<=4;floor++)for(int i=0;i<2;i++)
            {
                int id=floor*2+i;if(AncientProgressRules.HasCache(CacheMask,id))continue;
                Vector2 point=CachePoint(floor,i);
                var o=new GameObject("Optional high-route supply "+id);o.transform.SetParent(transform,false);
                o.AddComponent<AncientRouteCache>().Initialize(game,this,id,floor,point);
            }
        }
        static Vector2 CachePoint(int floor,int index)
        {
            if(floor==0)return new Vector2(index==0?35:79,7.2f);
            if(index==0)return new Vector2(AncientChapterLayout.CaseRight(floor*4)-36,7.05f);
            int jumpCase=floor==1?6:floor==2?9:floor==3?15:18;
            var landing=AncientFieldExpansion.Segment(jumpCase,1,4);
            return new Vector2(AncientChapterLayout.CaseLeft(jumpCase)+landing.left+2.1f,Mathf.Max(6.2f,landing.top+2.2f)+1);
        }
        public bool Claim(int id,Vector2? source=null)
        {
            if(id<0||id>=AncientProgressRules.CacheCount||AncientProgressRules.HasCache(CacheMask,id))return false;
            CacheMask=AncientProgressRules.ClaimCache(CacheMask,id);AncientGame.SetSaved("routeCaches",CacheMask);
            float before=game.hero.hp;game.hero.hp=AncientProgressRules.RecoverHealth(before,game.hero.MaxHP,18);
            int recovered=Mathf.CeilToInt(game.hero.hp-before);game.credits+=4;game.Save();game.Sound(5);
            if(game.hero.Combat)game.hero.Combat.EmitParticles(source??(Vector2)game.hero.transform.position,24);
            game.Feel.Signal(recovered>0?"탐색 보급 · TECH +4 · 회복 +"+recovered:"탐색 보급 · TECH +4 · 입자 방출",new Color(1,.85f,.2f),1.4f);return true;
        }
    }
    public sealed class AncientRouteCache : MonoBehaviour
    {
        AncientGame game;AncientExploration owner;int id,floor;Vector2 centre;Transform visual;bool visible;GUIStyle hint;
        public void Initialize(AncientGame instance,AncientExploration exploration,int cacheId,int district,Vector2 point)
        {
            game=instance;owner=exploration;id=cacheId;floor=district;centre=point;transform.position=point;
            visual=new GameObject("Supply display").transform;visual.SetParent(transform,false);visual.gameObject.SetActive(false);
            var crystal=AncientArt.Display("Supply crystal",AncientArt.WireParticle(),Vector2.zero,12,visual);
            crystal.transform.localScale=Vector3.one*.44f;crystal.color=new Color(1,.85f,.25f);
            var ring=game.Line("Supply locator diamond",new Color(1,.75f,.2f,.8f),.028f,11);
            ring.transform.SetParent(visual,false);ring.useWorldSpace=false;ring.loop=true;ring.positionCount=4;
            ring.SetPosition(0,new Vector2(0,.42f));ring.SetPosition(1,new Vector2(.42f,0));ring.SetPosition(2,new Vector2(0,-.42f));ring.SetPosition(3,new Vector2(-.42f,0));
        }
        void Update()
        {
            if(!game.Story||!game.hero)return;bool active=game.Story.CurrentFloor==floor;
            if(active!=visible){visible=active;visual.gameObject.SetActive(active);}
            if(!active||game.Paused)return;
            transform.position=centre+Vector2.up*(Mathf.Sin(Time.time*2+id)*.12f);
            if(!game.hero.bike&&((Vector2)game.hero.transform.position-(Vector2)transform.position).sqrMagnitude<.95f*.95f)
            {if(owner.Claim(id,transform.position))game.Impact(transform.position,new Color(1,.85f,.25f),false);Destroy(gameObject);}
        }
        void OnGUI()
        {
            if(!visible||!game||!game.hero||game.Paused||game.MainMenu||Event.current.type!=EventType.Repaint)return;
            if(((Vector2)game.hero.transform.position-centre).sqrMagnitude>144)return;
            Vector3 point=game.View.WorldToScreenPoint(transform.position+Vector3.up*.85f);
            if(point.z<=0||point.x<0||point.x>Screen.width||point.y<0||point.y>Screen.height)return;
            if(hint==null)hint=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=14,alignment=TextAnchor.MiddleCenter};
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            GUI.matrix=Matrix4x4.identity;GUI.depth=-12;GUI.color=new Color(1,.88f,.4f);
            GUI.Label(new Rect(point.x-145,Screen.height-point.y-16,290,32),"탐색 보급 · 체력 +18 · 입자 · TECH +4",hint);
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
    }
}
