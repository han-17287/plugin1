using UnityEngine;
namespace Plugin.AncientCivilization
{
    // Short optional field jobs use different mechanics instead of repeating a switch puzzle.
    public sealed class AncientFieldCases : MonoBehaviour
    {
        AncientGame game;int started;bool relayOpen,recordTaken;GUIStyle label;
        public int CompleteMask {get;private set;}
        readonly Vector2[] terminals={Vector2.zero,new Vector2(154.5f,2.2f),new Vector2(344,1.2f),new Vector2(548.5f,3.2f)};
        readonly SpriteRenderer[] displays=new SpriteRenderer[4];
        SpriteRenderer relay,record;readonly Vector2 relayPoint=new Vector2(185,6),recordPoint=new Vector2(333.5f,6.9f);
        public void Initialize(AncientGame owner)
        {
            game=owner;started=AncientGame.Saved("caseStarted");CompleteMask=AncientGame.Saved("caseComplete");
            relayOpen=AncientGame.Saved("caseRelay")!=0;recordTaken=AncientGame.Saved("caseRecord")!=0;
            for(int floor=1;floor<=3;floor++)
            {
                displays[floor]=AncientArt.Display("Field case terminal",AncientArt.World(2),terminals[floor]+Vector2.up*.65f,5,transform);
                displays[floor].transform.localScale=Vector3.one*.32f;
            }
            relay=AncientArt.Display("Damaged clinic insulation plate",AncientArt.MetalProjectile(1),relayPoint,10,transform);
            relay.transform.localScale=Vector3.one*.6f;
            record=AncientArt.Display("Recoverable sealed manifest",AncientArt.WireParticle(),recordPoint,10,transform);
            record.transform.localScale=Vector3.one*.5f;record.color=new Color(.65f,.95f,1);
            Refresh();
        }
        bool Done(int floor){return floor>=1&&floor<=3&&(CompleteMask&(1<<(floor-1)))!=0;}
        bool Started(int floor){return (started&(1<<(floor-1)))!=0;}
        void Persist()
        {
            AncientGame.SetSaved("caseStarted",started);AncientGame.SetSaved("caseComplete",CompleteMask);
            AncientGame.SetSaved("caseRelay",relayOpen?1:0);AncientGame.SetSaved("caseRecord",recordTaken?1:0);game.Save();
        }
        void Complete(int floor)
        {
            if(Done(floor))return;CompleteMask|=1<<(floor-1);game.credits+=6;Persist();Refresh();
            game.Sound(5);game.Feel.Signal("현장 기록 확보 · TECH +6",new Color(1,.85f,.2f),1.5f);
        }
        void Refresh()
        {
            for(int floor=1;floor<=3;floor++)displays[floor].color=Done(floor)?new Color(.45f,.9f,.65f):new Color(1,.8f,.35f);
            relay.enabled=!relayOpen;record.enabled=!recordTaken;
        }
        public bool TryBladeRelay(Vector2 origin,Vector2 direction)
        {
            if(relayOpen||game.Story.CurrentFloor!=1)return false;Vector2 delta=relayPoint-origin;
            if(delta.sqrMagnitude>3.1f*3.1f||Vector2.Dot(delta.normalized,direction)<-.15f||Physics2D.Linecast(origin,relayPoint,1<<8))return false;
            relayOpen=true;game.Impact(relayPoint,new Color(.5f,1,.8f),true);game.Sound(2);
            if(Started(1))Complete(1);else Persist();Refresh();
            game.Feel.Signal("절연판 제거 · 진료소 비상 전력 복구",new Color(.5f,1,.8f),1.5f);return true;
        }
        public bool TryInteract()
        {
            int floor=game.Story.CurrentFloor;if(floor<1||floor>3||game.hero.bike)return false;
            if(((Vector2)game.hero.transform.position-(terminals[floor]+Vector2.up)).sqrMagnitude>2.2f*2.2f)return false;
            if(Done(floor))
            {game.Story.FieldDialogue("통신관","이 구역의 현장 기록은 이미 보관했다. Tab의 순찰 기록에서 다시 읽을 수 있다.");return true;}
            if(floor==3)
            {
                foreach(var enemy in game.enemies)
                    if(enemy&&enemy.IsAlive&&((Vector2)enemy.transform.position-terminals[floor]).sqrMagnitude<25)
                    {game.Notice("단말 근처 경비를 제압한 뒤 기록을 확인하세요.",2);return true;}
            }
            started|=1<<(floor-1);Persist();
            if(floor==1)
            {
                if(relayOpen){Complete(1);game.Story.FieldDialogue("진료소 기록","예비 전력 복구 확인. 정기 배정 물량은 이미 줄어 있었다. 도난 사건 이전에도 코어가 도착하지 않았다.");}
                else game.Story.FieldDialogue("통신관","진료소 주배전망과 별개인 예비선이 끊겨 있다. 동쪽 공중 배전함의 파손된 절연판을 블레이드로 제거해 봐라. 복구되면 배정 기록을 받을 수 있다.");
            }
            else if(floor==2)
            {
                if(recordTaken){Complete(2);game.Story.FieldDialogue("통신관","회수한 기록을 사본과 대조했다. 수량은 같지만 수령처 항목만 나중에 비워졌다. C-00 승인 뒤의 수정이다.");}
                else game.Story.FieldDialogue("통신관","서쪽 보안 셔터 위 정비 선반에 운송장 원본 캐시가 남아 있다. 회수해 오면 내가 보관한 사본과 대조하겠다.");
            }
            else
            {Complete(3);game.Story.FieldDialogue("보관된 녹음","재배치 명단의 코어 일련번호와 하층 배정 번호가 같다. 이건 상층 주민에게 배급된 물량이 아니다. 최상층 직할 운송으로 묶여 있다.");}
            return true;
        }
        void Update()
        {
            if(!game.Story||game.Paused||!game.hero)return;
            if(!recordTaken&&game.Story.CurrentFloor==2&&((Vector2)game.hero.transform.position-recordPoint).sqrMagnitude<1)
            {
                recordTaken=true;Persist();Refresh();game.Sound(2);
                game.Feel.Signal("운송장 원본 캐시 회수 · 단말에서 대조",new Color(.6f,.95f,1),1.5f);
            }
        }
        string Hint(int floor)
        {
            if(Done(floor))return "현장 기록 보관 완료 / E: 확인";
            if(!Started(floor))return "E: 현장 단말 조사 (선택)";
            if(floor==1)return relayOpen?"E: 배정 기록 확인":"동쪽 공중 절연판을 블레이드로 제거";
            if(floor==2)return recordTaken?"E: 원본과 사본 대조":"서쪽 셔터 위 원본 기록 회수";
            return "단말 주변 경비 제압 → E: 기록 확보";
        }
        void OnGUI()
        {
            if(!game.Story||game.Paused||game.MainMenu||Event.current.type!=EventType.Repaint)return;
            int floor=game.Story.CurrentFloor;if(floor<1||floor>3)return;
            if(label==null)label=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=14,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            Vector3 p=game.View.WorldToScreenPoint(terminals[floor]+Vector2.up*2);
            if(p.z>0&&p.x>-180&&p.x<Screen.width+180)
            {
                var matrix=GUI.matrix;var color=GUI.color;GUI.matrix=Matrix4x4.identity;
                GUI.color=Done(floor)?new Color(.5f,1,.7f):new Color(1,.85f,.3f);
                GUI.Label(new Rect(p.x-180,Screen.height-p.y-20,360,55),Hint(floor),label);
                GUI.matrix=matrix;GUI.color=color;
            }
        }
    }
}
