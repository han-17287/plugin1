using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientGameFeel : MonoBehaviour
    {
        struct Number {public Vector2 point;public float age;public string text;public bool punish;}
        readonly Number[] numbers=new Number[20];int numberCursor;AncientGame game;GUIStyle numberStyle,cursorStyle;
        string signal;Color signalColor;float signalUntil;
        int lastCharge=-1;string chargeText="";
        public void Initialize(AncientGame owner){game=owner;}
        public void Damage(Vector2 point,float amount,bool punish)
        {
            int at=numberCursor++%numbers.Length;
            numbers[at]=new Number{point=point+new Vector2((at%3-1)*.18f,.5f),age=.7f,text=Mathf.RoundToInt(amount).ToString()+(punish?"!":""),punish=punish};
        }
        public void Signal(string text,Color color,float duration=1)
        {signal=text;signalColor=color;signalUntil=Time.time+duration;}
        void Update()
        {
            if(game.Paused)return;
            for(int i=0;i<numbers.Length;i++)if(numbers[i].age>0)
            {numbers[i].age-=Time.deltaTime;numbers[i].point.y+=Time.deltaTime*.8f;}
        }
        void OnGUI()
        {
            if(!game||!game.hero||game.Paused||game.MainMenu||Event.current.type!=EventType.Repaint)return;
            if(numberStyle==null)
            {
                numberStyle=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=18,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
                cursorStyle=new GUIStyle(numberStyle){fontSize=14,fontStyle=FontStyle.Normal};
            }
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            GUI.matrix=Matrix4x4.identity;GUI.depth=-15;
            for(int i=0;i<numbers.Length;i++)
            {
                var n=numbers[i];if(n.age<=0)continue;var p=game.View.WorldToScreenPoint(n.point);
                if(p.z<=0||p.x<0||p.x>Screen.width||p.y<0||p.y>Screen.height)continue;
                Color tint=n.punish?new Color(1,.85f,.2f):Color.white;tint.a=Mathf.Clamp01(n.age/.2f);GUI.color=tint;
                GUI.Label(new Rect(p.x-40,Screen.height-p.y-16,80,32),n.text,numberStyle);
            }
            if(Time.time<signalUntil)
            {
                GUI.color=signalColor;
                GUI.Label(new Rect(Screen.width/2-180,Screen.height*.68f,360,32),signal,numberStyle);
            }
            var combat=game.hero.Combat;
            if(combat&&combat.Charging)
            {
                int percent=Mathf.RoundToInt(combat.ChargeProgress*100);
                if(percent!=lastCharge){lastCharge=percent;chargeText=percent>=100?"준비 완료 · 놓아서 발사":"레이저 조준 "+percent+"%";}
                GUI.color=percent>=100?new Color(1,.85f,.2f):new Color(.55f,.9f,1);
                float x=Mathf.Clamp(Input.mousePosition.x+16,10,Mathf.Max(10,Screen.width-210));
                float y=Mathf.Clamp(Screen.height-Input.mousePosition.y+18,10,Mathf.Max(10,Screen.height-35));
                GUI.Label(new Rect(x,y,200,28),chargeText,cursorStyle);
            }
            else lastCharge=-1;
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
    }
}
