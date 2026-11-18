using UnityEngine;

namespace Plugin.AncientCivilization
{
    // One cached image at a time, prepared after camera follow and before rendering.
    // District art is behind platforms, targets and enemies and never changes collision geometry.
    public sealed class AncientDistrictBackdrop : System.IDisposable
    {
        readonly AncientGame game;readonly SpriteRenderer art;
        int floor=-1;
        public AncientDistrictBackdrop(AncientGame owner,Transform parent)
        {
            game=owner;for(int i=0;i<5;i++)AncientArt.DistrictBackdrop(i);
            art=AncientArt.Display("Sealed district interior backdrop",AncientArt.DistrictBackdrop(0),Vector2.zero,-24,parent);
            art.color=new Color(.72f,.76f,.82f,1);art.enabled=false;
            Camera.onPreCull+=Prepare;
        }
        void Prepare(Camera view)
        {
            if(!game||view!=game.View||!game.hero||!game.Story)return;
            bool visible=!game.MainMenu;art.enabled=visible;if(!visible)return;
            bool chase=game.Finale&&game.Finale.Active;
            int district=chase?Mathf.Clamp(4-AncientFinaleRules.Zone(game.hero.transform.position.x),0,4):game.Story.CurrentFloor;
            if(floor!=district){floor=district;art.sprite=AncientArt.DistrictBackdrop(floor);}
            float reference=chase?AncientFinaleRules.Start:AncientChapterLayout.FloorStart(floor);
            Vector3 camera=view.transform.position;
            float length=chase?AncientFinaleRules.Finish-AncientFinaleRules.Start:AncientChapterLayout.FloorEnd(floor)-reference;
            float offset=Mathf.Lerp(-3,3,Mathf.Clamp01((game.hero.transform.position.x-reference)/Mathf.Max(1,length)));
            // Overscan also covers the widest supported window and fast wire camera zooms.
            float height=Mathf.Max(20,view.orthographicSize*2+6);
            height=Mathf.Max(height,(view.orthographicSize*view.aspect*2+8)*9/16f);
            Vector2 size=art.sprite.bounds.size;float scale=Mathf.Max(height/size.y,(view.orthographicSize*view.aspect*2+8)/size.x);
            art.transform.localScale=Vector3.one*scale;
            art.transform.position=new Vector3(camera.x-offset,camera.y+.3f,0);
        }
        public void Dispose()
        {Camera.onPreCull-=Prepare;if(art)Object.Destroy(art.gameObject);}
    }
}
