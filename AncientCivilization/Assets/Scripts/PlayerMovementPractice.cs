using UnityEngine;
using UnityEngine.Tilemaps;
namespace Plugin.PlayerCore
{
    // 연습 씬에서만 실행되며 기존 스토리 저장을 읽거나 변경하지 않는다.
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerMovementPractice : MonoBehaviour
    {
        public PlayerMovementData movementData;
        PlayerStateMachine player;PlayerInputReader input;Camera view;Sprite square;Texture2D texture;Tile tile;GUIStyle style;
        PracticeMusic music;
        PlayerSummonLoadout loadout;
        Vector2 spawn=new Vector2(-17,2);string actionNote="시작점에서 우클릭으로 보급한 뒤 Q로 칼을 소환하세요.";bool ownedData;
        void Awake()
        {
            if(!movementData)movementData=Resources.Load<PlayerMovementData>("PlayerCore/DefaultMovement");
            if(!movementData){movementData=ScriptableObject.CreateInstance<PlayerMovementData>();ownedData=true;}
            texture=new Texture2D(1,1){filterMode=FilterMode.Point};texture.SetPixel(0,0,Color.white);texture.Apply();square=Sprite.Create(texture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            view=Camera.main;if(!view)view=new GameObject("Practice Camera",typeof(Camera)).GetComponent<Camera>();
            view.orthographic=true;view.orthographicSize=8;view.backgroundColor=new Color(.025f,.05f,.08f);view.transform.position=new Vector3(0,4,-10);view.tag="MainCamera";
            music = new PracticeMusic(transform, view);
            BuildTileFloor();Solid("Wall jump shaft left",new Vector2(17,4),new Vector2(1,8));Solid("Wall jump shaft right",new Vector2(22,5.5f),new Vector2(1,11));
            Solid("Low jump step",new Vector2(-10,.4f),new Vector2(2,.8f));Solid("High jump step",new Vector2(-5,1),new Vector2(2,2));
            Solid("Coyote landing",new Vector2(3,1),new Vector2(4,2));Solid("Buffer landing",new Vector2(10,2.5f),new Vector2(3,1));
            var platform=Solid("S + Space one-way",new Vector2(-1,4),new Vector2(6,.25f));
            var effect=platform.AddComponent<PlatformEffector2D>();effect.useOneWay=true;effect.useOneWayGrouping=true;effect.surfaceArc=160;platform.GetComponent<BoxCollider2D>().usedByEffector=true;
            var root=new GameObject("P1 Player");root.SetActive(false);root.transform.position=spawn;root.layer=9;
            var body=root.AddComponent<Rigidbody2D>();body.mass=1;
            var capsule=root.AddComponent<CapsuleCollider2D>();capsule.size=new Vector2(.72f,1.8f);capsule.direction=CapsuleDirection2D.Vertical;
            root.AddComponent<PlayerModeController>();input=root.AddComponent<PlayerInputReader>();input.data=movementData;
            player=root.AddComponent<PlayerStateMachine>();player.data=movementData;
            var plug=root.AddComponent<PlayerBluePlug>();root.AddComponent<DistrictPlayerPresentation>();
            root.SetActive(true);
            var ports = new BluePlugSocket[3];
            for (int i=0;i<ports.Length;i++)
            {
                var panel=new GameObject("정비 훈련 콘센트 "+(i+1));
                panel.transform.SetParent(transform,false);
                panel.transform.localPosition=new Vector2(-13+9*i,4+i%2);
                ports[i]=panel.AddComponent<BluePlugSocket>();
            }
            plug.ConfigureRoom(ports,view,()=>BluePlugSkill.Tow|BluePlugSkill.Swing,text=>actionNote=text);
            loadout=PlayerSummonLoadout.Practice(plug,view,text=>actionNote=text);
            var debug=root.AddComponent<PlayerDebugOverlay>();debug.target=player;debug.input=input;
            var bulb=new GameObject("Mode bulb indicator");bulb.transform.SetParent(root.transform,false);bulb.transform.localPosition=new Vector3(.38f,.35f,0);bulb.transform.localScale=Vector3.one*.18f;
            debug.bulb=bulb.AddComponent<SpriteRenderer>();debug.bulb.sprite=square;debug.bulb.sortingOrder=2;
        }
        GameObject Solid(string name,Vector2 point,Vector2 size)
        {var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.position=point;go.layer=8;go.AddComponent<BoxCollider2D>().size=size;var art=go.AddComponent<SpriteRenderer>();art.sprite=square;art.size=Vector2.one;go.transform.localScale=new Vector3(1,1,1);var mesh=new GameObject("Surface");mesh.transform.SetParent(go.transform,false);mesh.transform.localScale=new Vector3(size.x,size.y,1);art.enabled=false;var image=mesh.AddComponent<SpriteRenderer>();image.sprite=square;image.color=new Color(.15f,.32f,.39f);return go;}
        void BuildTileFloor()
        {
            var grid=new GameObject("Seam test grid",typeof(Grid));grid.transform.SetParent(transform,false);
            var go=new GameObject("Composite tile floor",typeof(Tilemap),typeof(TilemapRenderer));go.layer=8;go.transform.SetParent(grid.transform,false);
            var body=go.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Static;
            var composite=go.AddComponent<CompositeCollider2D>();composite.geometryType=CompositeCollider2D.GeometryType.Polygons;
            var tiles=go.GetComponent<Tilemap>();var collider=go.AddComponent<TilemapCollider2D>();collider.extrusionFactor=.01f;
#if UNITY_6000_0_OR_NEWER
            collider.compositeOperation=Collider2D.CompositeOperation.Merge;
#endif
            tile=ScriptableObject.CreateInstance<Tile>();tile.sprite=square;tile.colliderType=Tile.ColliderType.Grid;tile.color=new Color(.1f,.23f,.29f);
            for(int x=-24;x<=30;x++)if(x<6||x>8)for(int y=-3;y<0;y++)tiles.SetTile(new Vector3Int(x,y,0),tile);
            collider.ProcessTilemapChanges();
        }
        void LateUpdate()
        {
            if(!player)return;
            music.Tick(0, false, input.Paused);
            if (!input.Paused && ((Vector2)player.transform.position-spawn).sqrMagnitude<4) loadout.RestockPractice();
            if(!input.Paused&&player.transform.position.y<-7)
            {
                music.Retry();
                player.Respawn(spawn);
            }
            Vector3 target=new Vector3(player.transform.position.x+player.Facing*2,Mathf.Max(3,player.transform.position.y+1.5f),-10);
            view.transform.position=Vector3.Lerp(view.transform.position,target,1-Mathf.Exp(-6*Time.unscaledDeltaTime));
        }
        void OnGUI()
        {
            if(style==null)style=new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Fonts/NeoDunggeunmo"),fontSize=16,wordWrap=true};
            GUI.Label(new Rect(18,155,Mathf.Min(Screen.width-36,900),110),"P1 이동 연습 / A·D 이동 · Space 점프 · Shift 대시 · S+Space 하강\nQ 칼 소환 · 손잡이에 투척해 장착 · E 분리 · F 레이저 · R 과부하 · Esc 일시정지\n"+actionNote,style);
            if(GUI.Button(new Rect(18,270,180,32),"시작점으로 재배치"))player.Respawn(spawn);
        }
        void OnApplicationFocus(bool focused) { if (!focused) music?.Suspend(); }
        void OnDisable() { music?.Suspend(); }
        void OnDestroy(){music?.Dispose();if(square)Destroy(square);if(texture)Destroy(texture);if(tile)Destroy(tile);if(ownedData&&movementData)Destroy(movementData);}
    }
}
