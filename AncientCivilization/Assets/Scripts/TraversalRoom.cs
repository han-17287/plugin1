using System.Collections.Generic;
using UnityEngine;
namespace Plugin.PlayerCore
{
    // 같은 Room Prefab의 데이터를 바꾸어 형상을 다시 짓는다. Transform 스케일로 난도를 바꾸지 않는다.
    public sealed partial class TraversalRoom : MonoBehaviour
    {
        public bool precision;public FloorTraversalProfile floorProfile;
        readonly List<TraversalFloorMechanism> mechanisms=new List<TraversalFloorMechanism>();
        public TraversalFloor Floor=>floorProfile?floorProfile.floor:TraversalFloor.Production;
        public TraversalDifficultyData difficulty;
        public PlayerInputReader clockOwner;
        readonly List<GrappleAnchor> anchors=new List<GrappleAnchor>();
        Sprite sprite;Texture2D texture;GrappleAnchorData movingData;bool built;
        public float Width {get;private set;}
        public Vector2 Checkpoint= new Vector2(3,1.1f);
        public IReadOnlyList<GrappleAnchor> Anchors=>anchors;
        public void Build(TraversalDifficultyData profile,PlayerInputReader reader)
        {
            if(built)return;built=true;if(!floorProfile)floorProfile=Resources.Load<FloorTraversalProfile>("Traversal/Production");difficulty=profile;clockOwner=reader;
            texture=new Texture2D(1,1){filterMode=FilterMode.Point};texture.SetPixel(0,0,Color.white);texture.Apply();sprite=Sprite.Create(texture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            int count=profile.linkedAnchors,gap=profile.gapTiles;float exitX=TraversalLayout.ExitX(count,gap);Width=TraversalLayout.Width(count,gap);
            movingData=Instantiate(Resources.Load<GrappleAnchorData>("Traversal/DefaultAnchor"));movingData.color=new Color(.4f,1,.65f);movingData.movingAmplitude=profile.movingAmplitude;movingData.movingPeriod=profile.movingPeriod;if(Floor==TraversalFloor.Residence)movingData.movingAxis=Vector2.up;
            if(!precision)Solid("안전한 연습 바닥",0,Width,0);
            else
            {
                Solid("난관 직전 체크포인트 바닥",0,10,0);Solid("출구 착지 바닥",exitX-2,Width,0);
                var danger=Box("연습 가시",new Vector2((10+exitX-2)*.5f,.45f),new Vector2(exitX-12,.9f),floorProfile?floorProfile.hazardColor:new Color(.85f,.25f,.2f),10);
                danger.AddComponent<BoxCollider2D>().isTrigger=true;danger.AddComponent<TraversalPracticeHazard>();
                // 도형과 실제 판정이 같은 위험 범위를 갖는다.
                for(float x=10;x<exitX-2;x+=1)Box("가시 밝은 윗선",new Vector2(x+.3f,.94f),new Vector2(.6f,.08f),new Color(1,.65f,.3f),0);
            }
            // 위는 전부 닫힌 천장이다. 천장 견인이라는 두 번째 해결법을 유지한다.
            var roof=Box("와이어 가능한 닫힌 천장",new Vector2(Width*.5f,14.5f),new Vector2(Width,1),new Color(.1f,.24f,.3f),8);roof.AddComponent<BoxCollider2D>();
            Box("왼쪽 외벽",new Vector2(-.5f,7),new Vector2(1,14),new Color(.12f,.25f,.3f),8).AddComponent<BoxCollider2D>();
            Spawn("FixedAnchor",new Vector2(6,4));
            for(int i=1;i<=count;i++){float x,y;FloorTraversalLayout.Anchor(Floor,i,count,gap,out x,out y);Spawn(i%2==0?"MovingAnchor":"OneUseAnchor",new Vector2(x,y));}
            Spawn("FixedAnchor",new Vector2(exitX,4));BuildFloorMechanisms(count,gap);
            var checkpoint=Box("체크포인트 표식",Checkpoint-Vector2.up*.9f,new Vector2(1.8f,.12f),new Color(.3f,1,.6f),0);
            checkpoint.GetComponent<SpriteRenderer>().sortingOrder=4;
            Box("출구 표식",new Vector2(Width-3,.15f),new Vector2(2,.15f),new Color(1,.8f,.2f),0);
        }
        void Spawn(string prefab,Vector2 point)
        {
            var template=Resources.Load<GameObject>("Traversal/"+prefab);var instance=Instantiate(template,transform.TransformPoint(point),Quaternion.identity,transform);
            var anchor=instance.GetComponent<GrappleAnchor>();anchor.clockOwner=clockOwner;
            if(anchor is MovingGrappleAnchor)anchor.data=movingData;anchor.ResetForRetry();anchors.Add(anchor);
        }
        GameObject Box(string title,Vector2 point,Vector2 size,Color color,int layer)
        {var go=new GameObject(title);go.transform.SetParent(transform,false);go.transform.localPosition=point;go.transform.localScale=new Vector3(size.x,size.y,1);go.layer=layer;var art=go.AddComponent<SpriteRenderer>();art.sprite=sprite;art.color=color;return go;}
        void Solid(string title,float left,float right,float top)
        {var go=Box(title,new Vector2((left+right)*.5f,top-2),new Vector2(right-left,4),floorProfile?floorProfile.structureColor:new Color(.15f,.3f,.38f),8);go.AddComponent<BoxCollider2D>();}
        void BuildFloorMechanisms(int count,int gap)
        {
            if(!floorProfile||Floor==TraversalFloor.Production)return;
            if(Floor==TraversalFloor.Living)
            {
                for(int i=1;i<=count;i+=2)
                {var go=Box("생활부 탐조등",new Vector2(6+i*gap,4),new Vector2(floorProfile.scanWidth,8),new Color(1,.9f,.3f,.25f),0);Attach(go);}
            }
            if(Floor==TraversalFloor.Administration)
            {
                float x=FloorTraversalLayout.GateX(count,gap);
                Solid("셔터 앞 대기 발판",FloorTraversalLayout.WaitLeft(x),FloorTraversalLayout.WaitRight(x),2);
                var go=Box("행정구 보안 셔터",new Vector2(x,4),new Vector2(.75f,8),new Color(.3f,1,.7f,.15f),8);go.AddComponent<BoxCollider2D>();Attach(go);
            }
            if(Floor==TraversalFloor.Residence)
            {
                float x=6+2*gap;Solid("환기구 보호 발판",x-1.5f,x+1.5f,1.6f);
                var go=Box("거주구 상승 환기류",new Vector2(x,6.3f),new Vector2(3,9.4f),new Color(.3f,.8f,1,.25f),0);Attach(go);
            }
            if(Floor==TraversalFloor.Top)
                for(int i=2;i<=count;i+=2){var item=anchors[i];var timer=item.gameObject.AddComponent<TraversalFloorMechanism>();timer.socket=item;timer.phase=i*.2f;timer.Initialize(floorProfile,clockOwner,precision);mechanisms.Add(timer);}
        }
        void Attach(GameObject go)
        {var item=go.AddComponent<TraversalFloorMechanism>();item.Initialize(floorProfile,clockOwner,precision);mechanisms.Add(item);}
        public void ResetForRetry(){foreach(var anchor in anchors)if(anchor)anchor.ResetForRetry();foreach(var item in mechanisms)if(item)item.ResetForRetry();}
        void OnDestroy(){if(sprite)Destroy(sprite);if(texture)Destroy(texture);if(movingData)Destroy(movingData);}
    }
}
