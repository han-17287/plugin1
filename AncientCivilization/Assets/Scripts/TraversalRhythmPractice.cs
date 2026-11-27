using UnityEngine;
using UnityEngine.SceneManagement;
namespace Plugin.PlayerCore
{
    [DefaultExecutionOrder(-200)]
    public sealed class TraversalRhythmPractice : MonoBehaviour
    {
        public PlayerMovementData movementData;
        PlayerStateMachine player;PlayerInputReader input;PlayerBluePlug plug;TraversalRoom room;Camera view;
        PracticeMusic music;
        PlayerSummonLoadout loadout;
        GameObject roomRoot;int floorIndex,roomIndex=1,tier=1,retries;bool cleared,settings;float elapsed,noticeUntil;GUIStyle style;string notice;
        void Awake()
        {
            Time.timeScale=1;Time.fixedDeltaTime=1f/60;Application.targetFrameRate=144;
            movementData=Resources.Load<PlayerMovementData>("PlayerCore/DefaultMovement");
            view=Camera.main;if(!view)view=new GameObject("District Camera",typeof(Camera)).GetComponent<Camera>();view.tag="MainCamera";view.orthographic=true;view.orthographicSize=6.5f;view.backgroundColor=new Color(.025f,.05f,.08f);
            music = new PracticeMusic(transform, view);
            var hero=new GameObject("백 경위 · 구역 이동");hero.SetActive(false);hero.layer=9;hero.transform.position=new Vector2(3,1.1f);
            hero.AddComponent<Rigidbody2D>();var shape=hero.AddComponent<CapsuleCollider2D>();shape.size=new Vector2(.72f,1.8f);
            hero.AddComponent<PlayerModeController>();input=hero.AddComponent<PlayerInputReader>();input.data=movementData;player=hero.AddComponent<PlayerStateMachine>();player.data=movementData;
            plug=hero.AddComponent<PlayerBluePlug>();hero.AddComponent<DistrictPlayerPresentation>();
            hero.SetActive(true);loadout=PlayerSummonLoadout.Practice(plug,view,ShowNotice);LoadRoom(1,1);
        }
        void LoadRoom(int index,int level,int floor=-1)
        {
            music.EnterRoom();
            input.Resume();plug.ResetLink();if(roomRoot){roomRoot.SetActive(false);Destroy(roomRoot);}if(floor>=0)floorIndex=floor;roomIndex=index;tier=level;cleared=false;retries=0;elapsed=0;noticeUntil=0;
            var prefab=Resources.Load<GameObject>("Traversal/"+(floorIndex==0?(index==0?"SafeFlowRoom":"GauntletRoom"):FloorTraversalLayout.Resources[floorIndex]+(index==0?"FlowRoom":"ChallengeRoom")));roomRoot=Instantiate(prefab,transform);
            room=roomRoot.GetComponent<TraversalRoom>();room.BuildDistrict(Resources.Load<TraversalDifficultyData>("Traversal/"+new[]{"Easy","Normal","Hard"}[level]),input);
            room.InstallElectricBench();
            plug.ConfigureRoom(room.BluePlugSockets(),view,()=>BluePlugSkill.Tow|BluePlugSkill.Swing,ShowNotice);
            player.Respawn(room.Checkpoint);view.transform.position=CameraTarget(room.Checkpoint);Physics2D.SyncTransforms();settings=false;
        }
        void ShowNotice(string text){notice=text;noticeUntil=Time.unscaledTime+2;}
        Vector3 CameraTarget(Vector2 p)
        {
            float half=view.orthographicSize,halfWidth=half*view.aspect;
            // 구덩이 아래 설비 윗부분도 보여 주어 낙하 위험을 미리 읽게 한다.
            return new Vector3(Mathf.Clamp(p.x+2.5f,halfWidth,Mathf.Max(halfWidth,room.Width-halfWidth)),Mathf.Clamp(p.y+2.4f,half-2.6f,room.District.ceiling-half+.5f),-10);
        }
        public void Retry() { Retry(true); }
        void Retry(bool fell)
        {if(!room||input.Paused)return;if(fell)music.Retry();retries++;cleared=false;plug.ResetLink();room.ResetForRetry();room.ResetElectricBench();player.Respawn(room.Checkpoint);view.transform.position=CameraTarget(room.Checkpoint);Physics2D.SyncTransforms();}
        void LateUpdate()
        {
            if(!room||!player)return;
            music.Tick(floorIndex, roomIndex != 0, input.Paused || settings);
            if(input.Ready&&!input.Paused&&Application.isFocused&&!cleared)elapsed+=Time.deltaTime;
            Vector2 p=player.transform.position;
            if (!input.Paused && (p-room.Checkpoint).sqrMagnitude<3) loadout.RestockPractice();
            if(p.y<-5||p.x<-2||p.x>room.Width+2){Retry();return;}
            if(!input.Paused&&room.UpdateCheckpoint(p,player.Grounded))
            {
                ShowNotice("재시작 지점 저장 · "+room.District.stops[room.CheckpointIndex].name);
                music.Checkpoint();
            }
            if(p.x>room.Width-4&&p.y<3&&player.Grounded)cleared=true;
            if(!input.Paused)view.transform.position=Vector3.Lerp(view.transform.position,CameraTarget(p),1-Mathf.Exp(-8*Time.unscaledDeltaTime));
        }
        void OnApplicationFocus(bool focused) { if (!focused) music?.Suspend(); }
        void OnDisable() { music?.Suspend(); }
        void OnDestroy() { music?.Dispose(); }
        void OnGUI()
        {
            if(!player||!room)return;
            if(style==null)style=new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Fonts/NeoDunggeunmo"),fontSize=15,wordWrap=true};
            float scale=Mathf.Max(.1f,Mathf.Min(Screen.width/1280f,Screen.height/720f));Matrix4x4 previous=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale;
            try
            {
                GUI.Box(new Rect(12,12,530,66),"");
                GUI.Label(new Rect(26,20,510,52),room.District.title+"\n"+room.District.stops[room.CheckpointIndex].name+"  /  Q 칼 소환 · 입자 "+loadout.Particles+" · C 견인 · V 스윙 · 재시도 "+retries,style);
                if(GUI.Button(new Rect(w-144,18,126,32),settings?"설정 닫기":"지역 / 도움말")){settings=!settings;if(settings)plug.ResetLink();input.SetInterfaceOpen(settings);}
                if(Time.unscaledTime<noticeUntil)GUI.Label(new Rect(26,90,430,28),notice,style);
                if(settings||input.Paused)
                {
                    GUI.Box(new Rect(w/2-300,140,600,390),"");
                    GUI.Label(new Rect(w/2-282,153,560,65),"지역 이동 맵\n좌클릭 홀드/해제 투척 · Q 칼 소환 · 손잡이에 플러그 명중하면 장착\nA/D 이동 · Space 점프 · C 견인 · V 스윙/이탈 · G 회수",style);
                    for(int i=0;i<5;i++){int selected=i;if(GUI.Button(new Rect(w/2-278+i*112,232,106,32),FloorTraversalLayout.Names[i]))LoadRoom(roomIndex,tier,selected);}
                    for(int i=0;i<3;i++){int selected=i;if(GUI.Button(new Rect(w/2-278+i*186,278,178,32),new[]{"쉬운 장치","보통 장치","빠른 장치"}[i]))LoadRoom(roomIndex,selected);}
                    if(GUI.Button(new Rect(w/2-278,324,270,32),roomIndex==0?"위험 설비 활성화":"안전 바닥으로 연습"))LoadRoom(roomIndex==0?1:0,tier);
                    if(GUI.Button(new Rect(w/2+8,324,270,32),"현재 지점 재시도")){settings=false;input.Resume();Retry(false);}
                    GUI.Label(new Rect(w/2-278,370,556,90),FloorTraversalLayout.Hints[floorIndex]+"\n장착 중 좌클릭 연속 베기 · F 충전/발사 · R 과부하 · E 분리 · 체크포인트 우클릭 보급\n이동 확인: "+elapsed.ToString("F1")+"초 / 상태 "+player.State+" / 속도 "+player.Velocity.ToString("F1")+"\n"+plug.Electricity.DebugStatus,style);
                    if(GUI.Button(new Rect(w/2-278,478,556,32),"메인 화면으로"))SceneManager.LoadScene("Main");
                }
                if(cleared)
                {
                    GUI.Box(new Rect(w/2-205,560,410,82),"");GUI.Label(new Rect(w/2-186,568,370,28),"연결문 도착 · "+room.District.title,style);
                    if(GUI.Button(new Rect(w/2-186,602,370,28),floorIndex<4?"다음 층 맵 보기":"생산구 맵으로"))LoadRoom(roomIndex,tier,(floorIndex+1)%5);
                }
            }
            finally{GUI.matrix=previous;}
        }
    }
}
