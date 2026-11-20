using Plugin.PlayerCore;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientPatrolGate : MonoBehaviour
    {
        AncientGame game;
        readonly Vector2[] gates=new Vector2[6];
        GUIStyle label;
        int near=-1;

        public void Initialize(AncientGame owner)
        {
            game=owner;
            float tutorial=AncientProductionLayout.TutorialCheckpointX(0)+6;
            gates[0]=new Vector2(tutorial,AncientProductionLayout.GroundHeight(tutorial));
            for(int f=0;f<5;f++)gates[f+1]=new Vector2(AncientCityLayout.Entry(f)+8,0);
            for(int i=0;i<gates.Length;i++)
            {
                Vector2 feet=gates[i];
                game.Box("도시 순찰 안내소",feet+Vector2.up*1.0f,new Vector2(1.05f,1.9f),new Color(.1f,.17f,.22f),3).transform.SetParent(transform,true);
                game.Box("순찰소 안내등",feet+Vector2.up*1.54f,new Vector2(.75f,.17f),AncientGame.Cyan,4).transform.SetParent(transform,true);
            }
        }
        void Update()
        {
            near=-1;
            if(!game || game.Paused || !game.GameplayInputReady || !game.hero || game.ArenaActive ||
                game.Production && game.Production.Active || game.Finale && game.Finale.Active)return;
            for(int i=0;i<gates.Length;i++)
            {
                if(((Vector2)game.hero.transform.position-gates[i]-Vector2.up).sqrMagnitude>8f)continue;
                near=i;break;
            }
            if(near>=0 && Input.GetKeyDown(KeyCode.P))PatrolEntry.Request(1+10*Mathf.Max(0,near-1));
        }
        void OnGUI()
        {
            if(near<0 || !game || game.Paused || Event.current.type!=EventType.Repaint)return;
            Vector3 point=game.View.WorldToScreenPoint(gates[near]+Vector2.up*2.6f);
            if(point.z<=0)return;
            if(label==null)label=new GUIStyle(GUI.skin.box){font=AncientKoreanUI.Font,fontSize=17,alignment=TextAnchor.MiddleCenter};
            GUI.Box(new Rect(point.x-90,Screen.height-point.y-26,180,28),"P  도시 자유 순찰",label);
        }
    }
}
