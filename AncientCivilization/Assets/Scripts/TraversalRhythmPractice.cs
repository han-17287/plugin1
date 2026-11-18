using UnityEngine;
namespace Plugin.PlayerCore
{
    [DefaultExecutionOrder(-200)]
    public sealed class TraversalRhythmPractice : MonoBehaviour
    {
        public PlayerMovementData movementData;
        PlayerStateMachine player;PlayerInputReader input;PlayerGrapple grapple;TraversalRoom room;Camera view;
        GameObject roomRoot;int roomIndex,tier=1,retries;bool cleared;float elapsed;GUIStyle style;Sprite sprite;Texture2D texture;
        void Awake()
        {
            movementData=Resources.Load<PlayerMovementData>("PlayerCore/DefaultMovement");
            view=Camera.main;if(!view)view=new GameObject("Rhythm Camera",typeof(Camera)).GetComponent<Camera>();view.tag="MainCamera";view.orthographic=true;view.orthographicSize=8;view.backgroundColor=new Color(.025f,.05f,.08f);
            texture=new Texture2D(1,1);texture.SetPixel(0,0,Color.white);texture.Apply();sprite=Sprite.Create(texture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            var hero=new GameObject("P2 Player");hero.SetActive(false);hero.layer=9;hero.transform.position=new Vector2(3,1.1f);
            hero.AddComponent<Rigidbody2D>();var shape=hero.AddComponent<CapsuleCollider2D>();shape.size=new Vector2(.72f,1.8f);
            hero.AddComponent<PlayerModeController>();input=hero.AddComponent<PlayerInputReader>();input.data=movementData;player=hero.AddComponent<PlayerStateMachine>();player.data=movementData;
            grapple=hero.AddComponent<PlayerGrapple>();grapple.data=Resources.Load<GrappleData>("Traversal/DefaultGrapple");
            var art=new GameObject("임시 플레이어 모양");art.transform.SetParent(hero.transform,false);art.transform.localScale=new Vector3(.72f,1.8f,1);var renderer=art.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=Color.white;
            hero.SetActive(true);var debug=hero.AddComponent<PlayerDebugOverlay>();debug.target=player;debug.input=input;LoadRoom(0,1);
        }
        void LoadRoom(int index,int level)
        {
            grapple.ResetForRetry();if(roomRoot){roomRoot.SetActive(false);Destroy(roomRoot);}roomIndex=index;tier=level;cleared=false;retries=0;elapsed=0;
            var prefab=Resources.Load<GameObject>("Traversal/"+(index==0?"SafeFlowRoom":"GauntletRoom"));roomRoot=Instantiate(prefab,transform);
            room=roomRoot.GetComponent<TraversalRoom>();room.Build(Resources.Load<TraversalDifficultyData>("Traversal/"+new[]{"Easy","Normal","Hard"}[level]),input);
            player.Respawn(room.Checkpoint);view.transform.position=new Vector3(8,5,-10);Physics2D.SyncTransforms();
        }
        public void Retry()
        {if(!room||input.Paused)return;retries++;grapple.ResetForRetry();room.ResetForRetry();player.Respawn(room.Checkpoint);view.transform.position=new Vector3(8,5,-10);}
        void LateUpdate()
        {
            if(!room||!player)return;
            if(input.Ready&&!input.Paused&&Application.isFocused&&!cleared)elapsed+=Time.deltaTime;
            Vector2 p=player.transform.position;
            if(p.y<-4||p.x<-2||p.x>room.Width+2){Retry();return;}
            if(p.x>room.Width-4&&p.y<3&&player.Grounded)cleared=true;
            Vector3 target=new Vector3(Mathf.Clamp(p.x+2.5f,8,Mathf.Max(8,room.Width-8)),Mathf.Max(5,p.y+1),-10);
            view.transform.position=Vector3.Lerp(view.transform.position,target,1-Mathf.Exp(-8*Time.unscaledDeltaTime));
        }
        void OnGUI()
        {
            if(!player||!room)return;
            if(style==null)style=new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Fonts/NeoDunggeunmo"),fontSize=15,wordWrap=true};
            GUI.Label(new Rect(18,155,Mathf.Min(900,Screen.width-36),110),(roomIndex==0?"안전 흐름":"짧은 앵커 난관")+" / "+new[]{"쉬움","보통","어려움·선택"}[tier]+"\n휠 아래: 갈고리 · Q: 모드 전환 · 좌클릭: 앵커/벽/천장 직선 돌진 · Shift: 일반 대시\n실제 연습 시간 "+elapsed.ToString("F1")+"초 / 재시도 "+retries+" / "+(cleared?"통과":"진행 중")+" / 도착 공격 창 "+grapple.ZipStrikeReady,style);
            for(int i=0;i<2;i++){int selected=i;if(GUI.Button(new Rect(18+i*190,275,180,30),i==0?"안전 흐름 방":"짧은 난관 방"))LoadRoom(selected,tier);}
            for(int i=0;i<3;i++){int selected=i;if(GUI.Button(new Rect(18+i*125,312,115,30),new[]{"쉬움","보통","어려움·선택"}[i]))LoadRoom(roomIndex,selected);}
            if(GUI.Button(new Rect(18,350,180,30),"체크포인트 재시도"))Retry();
            GUI.Label(new Rect(18,390,Mathf.Min(900,Screen.width-36),80),"두 해법: 표시 앵커를 이어 타기 / 닫힌 천장을 이용해 우회하기\n이 씬은 두 방의 이동 시험입니다. 무기·입자 보상·보스는 아직 연결하지 않았습니다.",style);
        }
        void OnDestroy(){if(sprite)Destroy(sprite);if(texture)Destroy(texture);}
    }
}
