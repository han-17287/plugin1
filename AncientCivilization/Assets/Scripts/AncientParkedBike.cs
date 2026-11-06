using UnityEngine;

namespace Plugin.AncientCivilization
{
    // A placed vehicle owns its availability, position and health; mounting never creates a new bike.
    public sealed class AncientParkedBike : MonoBehaviour
    {
        public float Health {get;private set;}
        public bool Available {get{return parked&&Health>0;}}
        public float Facing {get;private set;}
        Vector2 spawn;Transform model;SpriteRenderer chassis;bool parked;GUIStyle prompt;
        public void Initialize(Vector2 position,float facing=1)
        {
            spawn=position;model=new GameObject("Parked patrol motorcycle").transform;model.SetParent(transform,false);
            chassis=AncientArt.Display("Parked chassis",AncientArt.Motorcycle(0),new Vector2(0,-.18f),9,model);
            AncientArt.Display("Parked rear wheel",AncientArt.Motorcycle(1),new Vector2(-1.15f,-.45f),10,model);
            AncientArt.Display("Parked front wheel",AncientArt.Motorcycle(1),new Vector2(1.16f,-.45f),10,model);
            Park(position,facing,80);
        }
        public void Park(Vector2 position,float facing,float health)
        {
            transform.position=position;Facing=facing<0?-1:1;Health=Mathf.Clamp(health,0,80);parked=Health>0;
            model.localScale=new Vector3(Facing,1,1);
            chassis.sprite=AncientArt.Motorcycle(Health<=28?3:0);
            chassis.transform.localScale=new Vector3(Health<=28?1.06f:1,1,1);
            chassis.transform.localPosition=new Vector2(0,Health<=28?-.207f:-.18f);
            model.gameObject.SetActive(parked);
        }
        public void Take(){parked=false;model.gameObject.SetActive(false);}
        public void Wreck(){Health=0;parked=false;model.gameObject.SetActive(false);}
        public void ResetAtSpawn(){Park(spawn,1,80);}
        void OnGUI()
        {
            var g=AncientGame.Instance;
            if(Event.current.type!=EventType.Repaint||!Available||!g||(g.Production&&g.Production.Active)||g.Paused||g.won||!g.hero||g.hero.bike||!g.View)return;
            if(((Vector2)g.hero.transform.position-(Vector2)transform.position).sqrMagnitude>2.8f*2.8f)return;
            Vector3 p=g.View.WorldToScreenPoint(transform.position+Vector3.up*1.25f);if(p.z<=0)return;
            if(prompt==null)prompt=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=15,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            var matrix=GUI.matrix;var color=GUI.color;GUI.matrix=Matrix4x4.identity;GUI.color=AncientGame.Cyan;
            GUI.Label(new Rect(p.x-105,Screen.height-p.y-18,210,36),"E / 탑승   "+Mathf.CeilToInt(Health)+" HP",prompt);
            GUI.color=color;GUI.matrix=matrix;
        }
    }
}
