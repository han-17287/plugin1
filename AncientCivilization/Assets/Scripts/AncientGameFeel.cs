using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientGameFeel : MonoBehaviour
    {
        struct Number {public Vector2 point,impact;public float age;public string text;public bool punish;}
        readonly Number[] numbers=new Number[20];int numberCursor;AncientGame game;GUIStyle numberStyle,cursorStyle;
        string signal;Color signalColor;float signalUntil;
        readonly GUIContent chargeMeasure=new GUIContent();
        int lastCharge=-1,lastFontSize=-1;bool lastChargeHasEnergy,transientActive;string chargeText="";
        Vector2 chargeSize;
        public void Initialize(AncientGame owner){game=owner;}
        public void Damage(Vector2 point,float amount,bool punish)
        {
            if(amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return;
            int at=numberCursor++%numbers.Length;
            numbers[at]=new Number{point=point+new Vector2((at%3-1)*.18f,.5f),impact=point,age=.7f,text=Mathf.RoundToInt(amount).ToString()+(punish?"!":""),punish=punish};transientActive=true;
        }
        public void Signal(string text,Color color,float duration=1)
        {signal=text;signalColor=color;signalUntil=Time.time+duration;transientActive=true;}
        public void ClearTransient()
        {
            if(transientActive){for(int i=0;i<numbers.Length;i++)numbers[i]=default(Number);signal=null;signalUntil=0;transientActive=false;}
            lastCharge=-1;
        }
        bool ChangedContext
        {get{return game.MainMenu||game.won||(game.Story&&(game.Story.DialogueOpen||game.Story.LiftBusy))||
            (game.Production&&game.Production.Cinematic)||(game.Finale&&game.Finale.Cinematic);}}
        void Update()
        {
            if(ChangedContext){ClearTransient();return;}
            if(game.Paused)return;
            bool live=Time.time<signalUntil;
            for(int i=0;i<numbers.Length;i++)if(numbers[i].age>0)
            {numbers[i].age-=Time.deltaTime;numbers[i].point.y+=Time.deltaTime*.8f;live|=numbers[i].age>0;}
            transientActive=live;
        }
        static void HitConfirm(Vector3 point,bool punish,float alpha,float scale)
        {
            float size=2*scale,gap=(punish?6:5)*scale;Color ink=punish?new Color(1,.85f,.2f,alpha):new Color(1,1,1,alpha);
            for(int corner=0;corner<4;corner++)
            {
                float x=Mathf.Round(point.x+(corner%2==0?-gap:gap)-size*.5f),y=Mathf.Round(Screen.height-point.y+(corner<2?-gap:gap)-size*.5f);
                GUI.color=new Color(.015f,.025f,.04f,alpha*.8f);GUI.DrawTexture(new Rect(x-scale,y-scale,size+2*scale,size+2*scale),Texture2D.whiteTexture);
                GUI.color=ink;GUI.DrawTexture(new Rect(x,y,size,size),Texture2D.whiteTexture);
            }
        }
        void OnGUI()
        {
            if(!game||!game.hero||game.Paused||game.MainMenu||Event.current.type!=EventType.Repaint)return;
            if(numberStyle==null)
            {
                numberStyle=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=18,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
                cursorStyle=new GUIStyle(numberStyle){fontSize=14,fontStyle=FontStyle.Normal,alignment=TextAnchor.MiddleLeft};
            }
            float scale=Mathf.Clamp(Screen.height/720f,.85f,2);int fontSize=Mathf.RoundToInt(14*scale);
            if(lastFontSize!=fontSize){lastFontSize=fontSize;cursorStyle.fontSize=fontSize;numberStyle.fontSize=Mathf.RoundToInt(18*scale);lastCharge=-1;}
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            GUI.matrix=Matrix4x4.identity;GUI.depth=-15;
            for(int i=0;i<numbers.Length;i++)
            {
                var n=numbers[i];if(n.age<=0)continue;var p=game.View.WorldToScreenPoint(n.point);
                if(p.z<=0||p.x<0||p.x>Screen.width||p.y<0||p.y>Screen.height)continue;
                if(n.age>.54f)
                {var impact=game.View.WorldToScreenPoint(n.impact);if(impact.z>0)HitConfirm(impact,n.punish,Mathf.Clamp01((n.age-.54f)/.16f),scale);}
                Color tint=n.punish?new Color(1,.85f,.2f):Color.white;tint.a=Mathf.Clamp01(n.age/.2f);GUI.color=tint;
                GUI.Label(new Rect(p.x-50*scale,Screen.height-p.y-16*scale,100*scale,36*scale),n.text,numberStyle);
            }
            if(Time.time<signalUntil)
            {
                GUI.color=signalColor;
                GUI.Label(new Rect(Screen.width/2-180,Screen.height*.68f,360,32),signal,numberStyle);
            }
            var combat=game.hero.Combat;
            if(combat&&combat.Charging)
            {
                bool hasEnergy=game.hero.CaptureEnergy>0,ready=AncientCombatFeedbackRules.LaserReady(game.hero.CaptureEnergy,combat.ChargeProgress);
                int percent=combat.ChargeProgress>=1?100:Mathf.Min(99,Mathf.RoundToInt(combat.ChargeProgress*100));
                if(percent!=lastCharge||hasEnergy!=lastChargeHasEnergy)
                {
                    lastCharge=percent;lastChargeHasEnergy=hasEnergy;
                    chargeText=!hasEnergy?"입자 없음 · 우클릭 수집":ready?"준비 완료 · 놓아서 발사":"레이저 조준 "+percent+"%";
                    chargeMeasure.text=chargeText;chargeSize=cursorStyle.CalcSize(chargeMeasure);
                }
                Color chargeColor=!hasEnergy?AncientGame.Orange:ready?new Color(1,.85f,.2f):new Color(.55f,.9f,1);
                var placement=AncientCombatFeedbackRules.ChargeLabel(Screen.width,Screen.height,Input.mousePosition.x,Screen.height-Input.mousePosition.y,chargeSize.x,chargeSize.y,scale);
                var box=new Rect(placement.X,placement.Y,placement.Width,placement.Height);
                GUI.color=new Color(.015f,.035f,.05f,.9f);GUI.DrawTexture(box,Texture2D.whiteTexture);
                GUI.color=chargeColor;GUI.Label(new Rect(box.x+8*scale,box.y,Mathf.Max(1,box.width-16*scale),box.height-3*scale),chargeMeasure,cursorStyle);
                GUI.DrawTexture(new Rect(box.x+2*scale,box.yMax-3*scale,Mathf.Max(0,box.width-4*scale)*(hasEnergy?combat.ChargeProgress:0),2*scale),Texture2D.whiteTexture);
            }
            else lastCharge=-1;
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
    }
}
