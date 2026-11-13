using UnityEngine;
namespace Plugin.AncientCivilization
{
    // The defeated machine stays in the scene while its pilot gives testimony.
    // This is scenery only: no enemy, collision, damage or second reward remains.
    public sealed class AncientLivingAftermath : MonoBehaviour
    {
        AncientGame game;Transform wreck;SpriteRenderer pilot;SpriteRenderer[] parts;bool[] enabledParts;Vector2 origin;float age;
        public void Initialize(AncientGame owner,Transform machine,SpriteRenderer witness,Vector2 point,bool restored)
        {
            game=owner;pilot=witness;origin=point;age=restored?1:0;
            wreck=machine;
            if(!wreck)
            {
                wreck=new GameObject("Recovered gang machine wreck").transform;
                Part(0,new Vector2(0,3.3f));Part(1,new Vector2(-2,2.4f));Part(1,new Vector2(2,2.4f));
                Part(2,new Vector2(-2.3f,1.2f));Part(2,new Vector2(2.3f,1.2f));Part(3,new Vector2(-1.2f,.8f));Part(3,new Vector2(1.2f,.8f));
            }
            wreck.SetParent(transform,true);wreck.position=origin;wreck.localScale=Vector3.one;
            parts=wreck.GetComponentsInChildren<SpriteRenderer>();
            enabledParts=new bool[parts.Length];
            for(int i=0;i<parts.Length;i++){enabledParts[i]=parts[i].enabled;parts[i].color=new Color(.52f,.62f,.68f);}
            float player=game.hero?game.hero.transform.position.x:point.x-7;
            float side=point.x>=player?1:-1;
            pilot.transform.position=new Vector2(Mathf.Clamp(player+side*4,AncientCityLayout.ArenaLeft(1)+2,AncientCityLayout.ArenaRight(1)-2),0);
            pilot.color=new Color(.76f,.8f,.84f);pilot.enabled=!restored;
            Pose();
        }
        void Part(int id,Vector2 point){AncientArt.Display("Disabled mech part",AncientLivingArt.Part(id),point,7,wreck);}
        void Pose()
        {
            float t=Mathf.SmoothStep(0,1,age);
            wreck.position=origin+Vector2.down*(.35f*t);wreck.rotation=Quaternion.Euler(0,0,-9*t);
        }
        void LateUpdate()
        {
            if(!game||!game.Story||!wreck)return;
            if(game.HasSession&&!game.MainMenu&&Application.isFocused&&!(game.Journal&&game.Journal.Open))
            {age=Mathf.Min(1,age+Time.unscaledDeltaTime);Pose();}
            bool visible=game.HasSession&&!game.MainMenu&&AncientChapterView.NearCamera(game,origin+Vector2.up*3,1);
            for(int i=0;i<parts.Length;i++)if(parts[i])parts[i].enabled=visible&&enabledParts[i];
            pilot.enabled=game.HasSession&&!game.MainMenu&&AncientChapterView.NearCamera(game,pilot.transform.position,1);
            if(pilot.enabled&&game.hero)pilot.flipX=game.hero.transform.position.x<pilot.transform.position.x;
        }
        public bool TryInteract()
        {
            if(!pilot||!pilot.enabled||game.Paused||!game.GameplayInputReady||!game.hero.IsOnGround||game.hero.bike)return false;
            if(Vector2.Distance(game.hero.transform.position,(Vector2)pilot.transform.position+Vector2.up)>3.2f)return false;
            game.Story.Conversation(null,3,new[]{"두목","백 경위","두목"},new[]{
                "장부에 적힌 건 내가 받은 물량 전부야. 지우기 전 기록도 가져갔잖아.",
                "네가 훔친 물량과 위에서 바뀐 수령 기록은 따로 조사한다. 여기서 인계반을 기다려.",
                "수령처를 고친 건 행정구야. 그 승인 번호를 따라가 봐."});return true;
        }
    }
}
