using UnityEngine;
using Plugin.AncientCivilization;
namespace Plugin.PlayerCore
{
    public sealed partial class TraversalRoom
    {
        public DistrictRouteLayout District {get;private set;}
        public int CheckpointIndex {get;private set;}
        readonly System.Collections.Generic.List<SpriteRenderer> checkpointLights=new System.Collections.Generic.List<SpriteRenderer>();
        static readonly Color Ink=new Color(.035f,.065f,.085f),Steel=new Color(.15f,.23f,.27f),Edge=new Color(.36f,.48f,.49f);
        public void BuildDistrict(TraversalDifficultyData profile,PlayerInputReader reader)
        {
            if(built)return;built=true;difficulty=profile;clockOwner=reader;
            if(!floorProfile)floorProfile=Resources.Load<FloorTraversalProfile>("Traversal/Production");
            District=DistrictRouteLayout.Create((int)Floor);Width=District.width;
            texture=new Texture2D(1,1){filterMode=FilterMode.Point};texture.SetPixel(0,0,Color.white);texture.Apply();sprite=Sprite.Create(texture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            movingData=Instantiate(Resources.Load<GrappleAnchorData>("Traversal/DefaultAnchor"));movingData.color=new Color(.4f,1,.65f);
            // 난도는 이동 장치의 진폭으로 조절한다. 길 폭과 연결 거리는 유지한다.
            movingData.movingAmplitude=Mathf.Min(1,profile.movingAmplitude);movingData.movingPeriod=profile.movingPeriod;movingData.movingAxis=Floor==TraversalFloor.Residence?Vector2.up:Vector2.right;
            BuildArchitecture();
            foreach(var deck in District.decks)DistrictDeck(deck);
            var recovery=Box(precision?"설비 아래 위험 구역":"정비 배수로 안전 바닥",new Vector2((District.gapLeft+District.gapRight)*.5f,-3),new Vector2(District.gapRight-District.gapLeft,2),precision?new Color(.45f,.14f,.11f):Steel,precision?10:8);
            var catchShape=recovery.AddComponent<BoxCollider2D>();catchShape.isTrigger=precision;if(precision)recovery.AddComponent<TraversalPracticeHazard>();
            if(precision)for(float x=District.gapLeft;x<District.gapRight;x+=1.5f)Trim("위험 설비 표시",x,-1.95f,1,.1f,new Color(1,.48f,.18f),3);
            var roof=Box("연속 천장",new Vector2(Width*.5f,District.ceiling+.5f),new Vector2(Width,1),Ink,8);roof.AddComponent<BoxCollider2D>();
            Trim("천장 구조선",Width*.5f,District.ceiling,Width,.14f,Edge,1);
            Box("폐쇄된 출입 벽",new Vector2(-.5f,District.ceiling*.5f),new Vector2(1,District.ceiling+8),Ink,8).AddComponent<BoxCollider2D>();
            Box("출구 외벽",new Vector2(Width+.5f,District.ceiling*.5f),new Vector2(1,District.ceiling+8),Ink,8).AddComponent<BoxCollider2D>();
            for(int i=0;i<District.hooks.Length;i++)
            {
                var h=District.hooks[i];Spawn(h.kind==2?"MovingAnchor":h.kind==1?"OneUseAnchor":"FixedAnchor",new Vector2(h.x,h.y));
                // 실제 지지물에 연결한다. 이동 장치는 충돌 없는 레일 위를 오간다.
                Trim("콘센트 지지 케이블",h.x,(h.y+District.ceiling)*.5f,.06f,District.ceiling-h.y,Steel,-8);
                Trim("콘센트 고정 브래킷",h.x,h.y+.65f,1.1f,.18f,Edge,-2);
                if(h.kind==2)Trim("이동 장치 레일",h.x,h.y+.95f,3.5f,.18f,Steel,-3);
            }
            BuildDistrictMechanism();
            var recoveryHook=District.Recovery;Spawn("FixedAnchor",new Vector2(recoveryHook.x,recoveryHook.y));
            Trim("배수로 비상 정비 콘센트 받침",recoveryHook.x,recoveryHook.y+.65f,1.1f,.18f,Edge,-2);
            for(int i=0;i<District.stops.Length;i++)
            {
                var stop=District.stops[i];var pad=Trim("재시작 발판 "+stop.name,stop.x,stop.y-.94f,2.4f,.12f,new Color(.25f,.55f,.47f),4);
                checkpointLights.Add(pad.GetComponent<SpriteRenderer>());Sign(stop.name,stop.x-2,stop.y+2.5f);
            }
            var exit=Box("다음 구역 연결문",new Vector2(Width-3,2),new Vector2(2.4f,4),new Color(.045f,.16f,.18f),0);
            Trim("출구 표시등",Width-3,4.1f,2.6f,.18f,new Color(.35f,.95f,.85f),3);Sign("다음 구역 >",Width-6,5.3f);
            exit.GetComponent<SpriteRenderer>().sortingOrder=-1;CheckpointIndex=0;SetCheckpoint(0);
        }
        void DistrictDeck(DistrictDeck deck)
        {
            float depth=deck.bridge?.65f:deck.top+5;
            var slab=Box(deck.name,new Vector2((deck.left+deck.right)*.5f,deck.top-depth*.5f),new Vector2(deck.right-deck.left,depth),Ink,8);slab.AddComponent<BoxCollider2D>();
            Trim(deck.name+" 보행면",(deck.left+deck.right)*.5f,deck.top-.08f,deck.right-deck.left,.16f,Edge,2);
            Trim("강재 가장자리",(deck.left+deck.right)*.5f,deck.top-.3f,deck.right-deck.left,.12f,Steel,2);
            for(float x=deck.left+.6f;x<deck.right;x+=3)
            {
                Trim("판재 연결부",x,deck.top-.4f,.06f,.45f,Edge,2);
                if(deck.bridge)Trim("정비교 지지대",x,(deck.top-5)*.5f,.16f,deck.top+5,Steel,-5);
            }
            if(deck.bridge)for(float x=deck.left+.4f;x<deck.right;x+=2.1f)
            {Trim("뒤쪽 안전 난간",x,deck.top+.65f,.08f,1.3f,Steel,-4);}
            if(deck.bridge)Trim("뒤쪽 난간 손잡이",(deck.left+deck.right)*.5f,deck.top+1.3f,deck.right-deck.left,.07f,Edge,-4);
        }
        void BuildArchitecture()
        {
            var background=AncientArt.DistrictBackdrop((int)Floor);
            for(float x=0;x<Width;x+=24)
            {var image=AncientArt.Display("구역 원경",background,new Vector2(x+12,10),-50,transform);image.transform.localScale=new Vector3(24/background.bounds.size.x,24/background.bounds.size.y,1);image.color=new Color(.36f,.44f,.5f);}
            // 건물은 길 뒤쪽에 놓아, 충돌 지형과 배경의 명암을 확실히 구분한다.
            for(int i=0;i<8;i++)
            {
                float x=7+i*14,height=Floor==TraversalFloor.Living?13+(i%3)*2:Floor==TraversalFloor.Residence?10+(i%2)*4:14+(i%2)*3;
                Trim("건물 후벽",x,height*.5f,12,height,new Color(.065f,.105f,.13f),-25);
                Trim("건물 처마",x,height,12.6f,.3f,Steel,-18);
                Trim("배수관",x-5,height*.5f,.18f,height,Edge,-18);
                for(float y=5;y<height-1;y+=3.5f)for(int c=-1;c<=1;c++)
                {
                    Trim("창틀",x+c*3.2f,y,2.3f,1.9f,Ink,-22);
                    Trim("실내 불빛",x+c*3.2f,y,1.9f,1.5f,(i+c)%3==0?new Color(.35f,.27f,.16f):new Color(.10f,.22f,.26f),-21);
                    Trim("창살",x+c*3.2f,y,.09f,1.5f,Steel,-20);
                }
                if(Floor==TraversalFloor.Living)
                {
                    var house=AncientArt.Display("거주민 생활 설비",AncientLivingWallArt.Get(i%4,i%2),new Vector2(x,0),-14,transform);house.color=new Color(.7f,.76f,.79f);
                }
                if(Floor==TraversalFloor.Production)
                {
                    var machine=AncientArt.Display("하역·전력 설비",AncientArt.ProductionProp(i%AncientProductionPropAtlas.Count),new Vector2(x,0),-12,transform);machine.color=new Color(.58f,.68f,.72f);
                    Trim("운송 레일",x,12,13,.4f,Steel,-12);Trim("크레인 기둥",x+5,6,.4f,12,Steel,-12);
                }
                if(Floor==TraversalFloor.Administration)
                {
                    for(int n=0;n<4;n++){Trim("기록 보관함",x-4+n*2.5f,2.3f,2.1f,4.4f,new Color(.12f,.18f,.21f),-15);Trim("보관함 식별등",x-4+n*2.5f,3.5f,.55f,.1f,new Color(.25f,.65f,.59f),-14);}
                }
                if(Floor==TraversalFloor.Residence)
                {
                    Trim("화단 테두리",x,1.1f,7,1,Steel,-12);
                    for(int n=0;n<5;n++){Trim("온실 식물 줄기",x-2.5f+n*1.2f,2,.12f,2,new Color(.16f,.3f,.2f),-11);Trim("온실 수관",x-2.5f+n*1.2f,3,1.5f,1.4f,new Color(.13f,.25f,.18f),-10);}
                }
                if(Floor==TraversalFloor.Top)
                {
                    Trim("불투명 감시창",x,4,8,3,Ink,-15);Trim("통제 장치 상태등",x,3.3f,4,.12f,new Color(.4f,.15f,.12f),-14);
                }
            }
            Sign(District.title,1,7);Sign(District.description,2,5.5f);
            Sign("상부 정비로 / 위쪽",District.gapLeft-6,4);
            Sign(precision?"주의 / 아래 고압 설비":"안전 배수로 / 복구 콘센트 사용",District.gapLeft-4,2.6f);
        }
        GameObject Trim(string title,float x,float y,float width,float height,Color color,int order)
        {var go=Box(title,new Vector2(x,y),new Vector2(width,height),color,0);go.GetComponent<SpriteRenderer>().sortingOrder=order;return go;}
        void Sign(string text,float x,float y)
        {
            var go=new GameObject(text);go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(x,y,0);
            go.AddComponent<DistrictWorldSign>().text=text;
        }
        void BuildDistrictMechanism()
        {
            float x=District.mechanismX;
            if(Floor==TraversalFloor.Living){var go=Box("감시동 탐조등",new Vector2(x,4),new Vector2(floorProfile.scanWidth,8),new Color(1,.9f,.3f,.25f),0);Attach(go);Trim("탐조등 본체",x,8.5f,1.4f,.6f,Edge,-2);}
            if(Floor==TraversalFloor.Administration){var go=Box("원본 반출 보안 셔터",new Vector2(x,4),new Vector2(.75f,8),new Color(.3f,1,.7f,.15f),8);go.AddComponent<BoxCollider2D>();Attach(go);Trim("셔터 권양기",x,8.7f,2,.6f,Steel,-2);}
            if(Floor==TraversalFloor.Residence){var go=Box("온실 환기 상승류",new Vector2(x,6.3f),new Vector2(3,9.4f),new Color(.3f,.8f,1,.25f),0);Attach(go);}
            if(Floor==TraversalFloor.Top)for(int i=0;i<anchors.Count;i++)if(District.hooks[i].kind==2)
            {var timer=anchors[i].gameObject.AddComponent<TraversalFloorMechanism>();timer.socket=anchors[i];timer.phase=i*.2f;timer.Initialize(floorProfile,clockOwner,precision);mechanisms.Add(timer);}
        }
        public bool UpdateCheckpoint(Vector2 point,bool grounded)
        {
            if(District==null||!grounded)return false;
            for(int i=District.stops.Length-1;i>CheckpointIndex;i--)
            {var stop=District.stops[i];if(Mathf.Abs(point.x-stop.x)<2&&Mathf.Abs(point.y-stop.y)<.5f){SetCheckpoint(i);return true;}}
            return false;
        }
        void SetCheckpoint(int index)
        {CheckpointIndex=index;var s=District.stops[index];Checkpoint=new Vector2(s.x,s.y+.02f);for(int i=0;i<checkpointLights.Count;i++)checkpointLights[i].color=i<=index?new Color(.3f,1,.65f):new Color(.25f,.55f,.47f);}
    }
}
