using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Optional workplace conversations. These workers never block the service road,
    // grant progression, or replace the named residents of the later investigation.
    public sealed class AncientProductionResidents : MonoBehaviour
    {
        sealed class Worker
        {
            public string name,prompt;public int role;public Transform root;public SpriteRenderer art;
            public bool spoken;
        }
        static readonly string[] Names={"교대 작업자","압연기 조작원","재고 관리원"};
        static readonly float[] Places={-211,-145,66};
        static readonly int[] Roles={0,2,1};
        readonly Worker[] workers=new Worker[3];
        AncientGame game;GUIStyle prompt;bool shown;
        public void Initialize(AncientGame owner)
        {
            game=owner;
            for(int i=0;i<workers.Length;i++)
            {
                var worker=new Worker{name=Names[i],role=Roles[i],prompt="E / "+Names[i]+(i==0?"와":"과")+" 대화"};
                worker.root=new GameObject("Production worker / "+worker.name).transform;worker.root.SetParent(transform,false);
                worker.root.position=new Vector2(Places[i],AncientProductionLayout.GroundHeight(Places[i]));
                worker.art=AncientArt.Display(worker.name,AncientArt.ProductionWorker(i),Vector2.zero,6,worker.root);
                worker.root.gameObject.SetActive(false);workers[i]=worker;
            }
        }
        bool Opening
        {get{return game&&game.HasSession&&!game.MainMenu&&game.Story&&game.Story.CurrentFloor==0&&game.Story.Stage<5;}}
        void Update()
        {
            bool visible=Opening;
            if(visible!=shown)
            {shown=visible;foreach(var worker in workers)if(worker!=null)worker.root.gameObject.SetActive(visible);}
            if(!visible||game.Paused||!game.hero)return;
            foreach(var worker in workers)
            {
                if(worker==null)continue;
                float dx=game.hero.transform.position.x-worker.root.position.x;
                if(Mathf.Abs(dx)<7)worker.art.flipX=dx<0;
            }
        }
        int Nearest()
        {
            if(!Opening||!game.hero)return -1;
            int result=-1;float best=2.5f*2.5f;Vector2 origin=(Vector2)game.hero.transform.position+Vector2.up*.2f;
            for(int i=0;i<workers.Length;i++)
            {
                var worker=workers[i];if(worker==null)continue;Vector2 target=(Vector2)worker.root.position+Vector2.up*1.2f;
                float distance=(target-origin).sqrMagnitude;
                if(distance>=best||Physics2D.Linecast(origin,target,1<<8))continue;
                best=distance;result=i;
            }
            return result;
        }
        public bool TryInteract()
        {
            if(!Opening||game.Paused||!game.GameplayInputReady||!game.hero||game.hero.bike)return false;
            int index=Nearest();if(index<0)return false;
            if(game.hero.Wired||!game.hero.IsOnGround){game.Notice("통로에 착지한 뒤 작업자와 대화하세요.",1.5f);return true;}
            var worker=workers[index];bool repeat=worker.spoken;worker.spoken=true;
            string[] speakers,words;
            if(repeat)
            {
                speakers=new[]{worker.name};
                words=new[]{index==0?"재고 단말은 동쪽이에요. 위쪽 작업 발판은 안 타셔도 길이 이어집니다.":index==1?"작업등은 켜 뒀어요. 안전 기록도 남겨 주세요.":"출고 서명란은 그대로 두었어요. 수량은 저 단말에서 확인하시면 됩니다."};
            }
            else if(index==0)
            {
                speakers=new[]{worker.name,"백 경위",worker.name};
                words=new[]{"검수실 가시죠? 교대 통로는 열려 있어요. 위쪽 발판은 점검용이라 안 타셔도 됩니다.","아래 길부터 확인하겠습니다. 출고 작업은 시작했습니까?","아직요. 밤새 기계만 돌았는데 계량표는 아침마다 새로 뽑혀요."};
            }
            else if(index==1)
            {
                speakers=new[]{worker.name,"백 경위",worker.name};
                words=new[]{"위층 납품선은 못 멈춘대요. 전력이 모자라면 아래 전등부터 줄이라는 지시가 왔어요.","작업자 안전등은 켜 두세요. 감산 지시는 기록으로 남기겠습니다.","우린 수량만 찍어요. 어디로 가는지는 승인란 뒤에 가려져 있죠."};
            }
            else
            {
                speakers=new[]{worker.name,"백 경위",worker.name};
                words=new[]{"검수 완료품은 저 단말로 세면 돼요. 봉인된 C-00 예비분은 별도 승인 물량입니다.","봉인 번호와 출고 기록은 그대로 두십시오. 장부와 대조하겠습니다.","네. 오늘 출고 서명란은 비어 있어요. 바퀴 소리는 들렸는데요."};
            }
            game.Story.ClearProductionRadio();game.Story.Conversation(null,worker.role,speakers,words);return true;
        }
        public bool TryDialogueActor(string speaker,out Transform actor,out float headOffset)
        {
            actor=null;headOffset=1.95f;
            if(!Opening)return false;
            foreach(var worker in workers)if(worker!=null&&speaker==worker.name)
            {actor=worker.root;headOffset=Mathf.Clamp(worker.art.bounds.max.y-worker.root.position.y,.9f,2.4f);return true;}
            return false;
        }
        void OnGUI()
        {
            if(!Opening||game.Paused||!game.GameplayInputReady||!game.hero||game.hero.bike||Event.current.type!=EventType.Repaint)return;
            int index=Nearest();if(index<0||!game.View)return;var worker=workers[index];
            Vector3 point=game.View.WorldToScreenPoint(worker.root.position+Vector3.up*2.1f);
            if(point.z<=0||point.x<0||point.x>Screen.width||point.y<0||point.y>Screen.height)return;
            if(prompt==null)prompt=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=14,alignment=TextAnchor.MiddleCenter};
            Matrix4x4 matrix=GUI.matrix;Color color=GUI.color;int depth=GUI.depth;
            try
            {
                GUI.matrix=Matrix4x4.identity;GUI.color=AncientGame.Cyan;GUI.depth=-6;
                GUI.Label(new Rect(point.x-120,Screen.height-point.y-22,240,24),worker.prompt,prompt);
            }
            finally {GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}
        }
    }
}
