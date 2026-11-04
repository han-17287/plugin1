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
                Vector2 point=floor==0?new Vector2(i==0?35:79,7.2f):floor==1?new Vector2(i==0?167:212,7.05f):new Vector2(AncientCityLayout.Left(floor)+22.25f+i*46,7.05f);
                var o=new GameObject("Optional high-route supply "+id);o.transform.SetParent(transform,false);
                o.AddComponent<AncientRouteCache>().Initialize(game,this,id,floor,point);
            }
        }
        public bool Claim(int id)
        {
            if(id<0||id>=AncientProgressRules.CacheCount||AncientProgressRules.HasCache(CacheMask,id))return false;
            CacheMask=AncientProgressRules.ClaimCache(CacheMask,id);AncientGame.SetSaved("routeCaches",CacheMask);
            game.credits+=4;game.hero.hp=Mathf.Min(game.hero.MaxHP,game.hero.hp+4);game.Save();game.Sound(5);
            game.Feel.Signal("탐색 보급품 · TECH +4",new Color(1,.85f,.2f),1.4f);return true;
        }
    }
    public sealed class AncientRouteCache : MonoBehaviour
    {
        AncientGame game;AncientExploration owner;int id,floor;Vector2 centre;Transform visual;bool visible;
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
            {if(owner.Claim(id))game.Impact(transform.position,new Color(1,.85f,.25f),false);Destroy(gameObject);}
        }
    }
}
