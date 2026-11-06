using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Gameplay colliders stay separate from the shared static scenery meshes.
    public sealed class AncientProductionMap : MonoBehaviour
    {
        struct Sign
        {
            public readonly Vector2 Point;public readonly string Text;public readonly float Width;
            public Sign(float x,float y,string text,float width=300){Point=new Vector2(x,y);Text=text;Width=width;}
        }
        struct Shard {public Transform Transform;public SpriteRenderer View;public Vector2 Velocity;}
        AncientGame game;GameObject breach;readonly Shard[] shards=new Shard[8];float breachAge=-1;GUIStyle label;
        public bool BreachOpen {get;private set;}
        readonly Sign[] signs={
            new Sign(-229,4.2f,"교대 통로\nA/D 이동 · Ctrl 달리기 전환"),
            new Sign(-186,3.6f,"운송 벨트\nSpace: 낮은 장애물 넘기"),
            new Sign(-116,4.5f,"검수 회랑\n휠 아래 · 천장·작업 고리 좌클릭"),
            new Sign(-77,7.7f,"선택 지름길 / 실패해도 아래는 안전한 통로",380),
            new Sign(-26,4.6f,"장비 검수실\n휠 위: 공격 · 짧은 좌클릭: 베기"),
            new Sign(38,4.6f,"코어 재고 관리실 →"),
            new Sign(60,8.8f,"C-00 예비분 / 별도 계량",260),
            new Sign(92,3.6f,"재고 관리 단말\nE: 코어 수량 확인"),
            new Sign(30018,7.8f,"하층 동부 화물 터널 / 자동주행",360),
            new Sign(30580,7.8f,"냉각 배관선 / 수송 차량 우선",360),
            new Sign(31130,7.8f,"화물 분리·긴급 정차 구간",360)
        };
        static readonly Color Back=new Color(.035f,.065f,.085f),Steel=new Color(.18f,.27f,.30f),Ivory=new Color(.65f,.72f,.70f),Teal=new Color(.16f,.62f,.66f),Amber=new Color(.92f,.64f,.24f);
        public void Initialize(AncientGame owner)
        {
            game=owner;BuildTutorial();BuildChaseLane();BuildBreach();
        }
        void B(string name,float x,float y,float w,float h,Color color,int order=-5)
        {game.StaticBox(name,new Vector2(x,y),new Vector2(w,h),color,order);}
        void Solid(string name,float x,float y,float w,float h)
        {
            var o=new GameObject(name);o.layer=8;o.transform.SetParent(transform,false);o.transform.position=new Vector2(x,y);o.AddComponent<BoxCollider2D>().size=new Vector2(w,h);
        }
        void Ramp(AncientProductionLayout.Ramp ramp)
        {
            var o=new GameObject("Gentle production access ramp");o.layer=8;o.transform.SetParent(transform,false);o.transform.position=new Vector2(ramp.Left,0);
            o.AddComponent<PolygonCollider2D>().points=new[]{new Vector2(0,-.5f),new Vector2(ramp.Right-ramp.Left,-.5f),new Vector2(ramp.Right-ramp.Left,ramp.High),new Vector2(0,ramp.Low)};
            for(float x=ramp.Left;x<ramp.Right;x+=.5f)
            {float width=Mathf.Min(.5f,ramp.Right-x),top=ramp.Height(x+width*.5f);B("Ramp steel tread",x+width*.5f,top-.06f,width,.12f,Steel,1);}
        }
        void BuildTutorial()
        {
            const float left=AncientProductionLayout.TutorialLeft,right=AncientProductionLayout.TutorialRight;
            game.Platform(left,right,0);game.Ceiling(left-2,128,AncientProductionLayout.Ceiling);
            Solid("Production west retaining wall",left-.3f,5,.6f,10);
            B("Production west retaining wall",left-.3f,5,.6f,10,Steel,2);
            foreach(var ramp in AncientProductionLayout.GentleRamps)Ramp(ramp);
            foreach(var span in AncientProductionLayout.RaisedRoad)game.Platform(span.Left,span.Right,span.Top);
            Solid("Low training cargo",AncientProductionLayout.JumpCrateX,AncientProductionLayout.JumpCrateHeight*.5f,AncientProductionLayout.JumpCrateWidth,AncientProductionLayout.JumpCrateHeight);
            B("Low cargo steel body",-185,.275f,AncientProductionLayout.JumpCrateWidth,.55f,Steel,2);B("Low cargo bright top",-185,.52f,AncientProductionLayout.JumpCrateWidth+.05f,.055f,Amber,3);
            foreach(var span in AncientProductionLayout.WireShortcut)
            {
                Solid("Thin production service catwalk",(span.Left+span.Right)*.5f,span.Top-AncientProductionLayout.CatwalkThickness*.5f,span.Right-span.Left,AncientProductionLayout.CatwalkThickness);
                B("Service catwalk steel grating",(span.Left+span.Right)*.5f,span.Top-.09f,span.Right-span.Left,.18f,Steel,2);
                B("Service catwalk rim",(span.Left+span.Right)*.5f,span.Top+.025f,span.Right-span.Left,.05f,Teal,4);
                B("Catwalk wall bracket",span.Left+.35f,span.Top+1.2f,.12f,2.4f,Steel,-3);
            }
            foreach(float x in AncientProductionLayout.WireHookX)
            {
                Vector2 point=new Vector2(x,AncientProductionLayout.WireHookY);game.anchors.Add(point);
                B("Inspection hook upper cable",x,9.4f,.045f,1.2f,Steel,-2);
                game.WorldBatch.Sprite(AncientArt.WireParticle(),point,Vector2.one*.45f,Teal,6);
                B("Inspection hook bright centre",x,AncientProductionLayout.WireHookY,.09f,.09f,Color.white,7);
            }
            BuildShiftHall();BuildConveyorHall();BuildInspectionHall();BuildEquipmentRoom();BuildStockHall();
            for(float x=left+3;x<right;x+=9)
            {
                float top=AncientProductionLayout.GroundHeight(x);
                B("Production floor direction strip",x,top+.035f,1.1f,.06f,Ivory*.65f,3);
                B("Production overhead lamp",x,9.72f,2.5f,.12f,new Color(.44f,.65f,.66f),3);
            }
            foreach(float x in AncientProductionLayout.SectionStarts)
            {B("Work zone floor threshold",x+.7f,.06f,.14f,.12f,Amber,4);B("Work zone floor threshold",x+1.1f,.06f,.14f,.12f,Amber,4);}
        }
        void Portal(float x,float width,Color trim)
        {
            B("Open work bay recess",x,4.2f,width,8.4f,Back,-8);
            B("Open work bay header",x,8.4f,width+.4f,.32f,trim,-4);
            foreach(float side in new[]{-1f,1f})B("Open work bay upright",x+side*width*.5f,4.2f,.24f,8.4f,Steel,-4);
        }
        void BuildShiftHall()
        {
            Portal(-222,34,Ivory*.55f);
            for(int i=0;i<6;i++)
            {
                float x=-236+i*5.2f;B("Shift locker frame",x,2.2f,3.6f,4.4f,Steel,-6);B("Shift locker door",x,2.2f,3.2f,4.1f,Back,-5);
                B("Shift locker white panel",x,3.1f,2.7f,1.8f,Ivory*.65f,-4);B("Shift locker number lamp",x-.9f,3.7f,.15f,.15f,Teal,-3);
                B("Shift locker handle",x+1,1.6f,.12f,.5f,Ivory,-3);
            }
            B("Workers notice board",-201,5.4f,5.8f,2.6f,Steel,-5);B("Workers notice sheet",-201,5.4f,5.2f,2.15f,Ivory*.7f,-4);
            for(int line=0;line<4;line++)B("Notice board list",-201.2f,6-line*.4f,3.6f,.08f,Back,-3);
            B("Main supply conduit",-222,9,41,.35f,Steel,-4);B("Main supply conduit light",-222,9.02f,40,.055f,Teal,-3);
        }
        void BuildConveyorHall()
        {
            Portal(-164,52,Amber*.55f);
            B("Conveyor road bed",-167,.055f,18,.11f,new Color(.11f,.17f,.19f),2);
            for(float x=-175;x<=-159;x+=1.3f)
            {B("Conveyor steel roller",x,.1f,.7f,.14f,Steel,3);B("Conveyor travel mark",x+.18f,.18f,.22f,.05f,Amber,4);}
            for(int i=0;i<3;i++)
            {
                float x=-188+i*18;B("Production press casing",x,5.2f,6.8f,7.6f,Steel*.8f,-7);B("Production press opening",x,4.6f,5.2f,5.2f,Back,-6);
                B("Press white upper shell",x,7.5f,6.2f,1.6f,Ivory*.6f,-5);B("Press piston",x,5.9f,.55f,2.4f,Steel,-4);
                B("Press inspection table",x,2.6f,4.6f,.4f,Steel,-4);B("Press warning lip",x,2.86f,4.7f,.12f,Amber,-3);
            }
            for(float x=-190;x<-139;x+=5.5f)B("Machine status indicator",x,8.65f,.5f,.15f,Teal,-2);
        }
        void BuildInspectionHall()
        {
            Portal(-92,69,Teal*.7f);
            B("Overhead hoist track",-90,9.2f,72,.3f,Steel,-4);B("Overhead hoist track seam",-90,9.1f,72,.055f,Amber,-3);
            for(float x=-124;x<-53;x+=14)
            {
                B("Inspection wall column",x,4.4f,.65f,8.8f,Steel*.8f,-6);
                B("Inspection glass bay",x+5.9f,4.3f,9.3f,5.4f,new Color(.07f,.17f,.2f),-7);
                B("Inspection glass reflection",x+4.5f,4.3f,.2f,5.1f,new Color(.16f,.29f,.3f),-6);
                B("Inspection floor safety line",x+5.8f,.09f,6.4f,.08f,Teal,3);
            }
        }
        void BuildEquipmentRoom()
        {
            Portal(-9,80,Ivory*.55f);
            foreach(float x in new[]{-42f,-18f,7f,28f})
            {
                B("Inspection machinery cabinet",x,3.4f,8.4f,5.8f,Steel,-6);B("Inspection cabinet white casing",x,5.7f,8,.9f,Ivory*.65f,-5);
                B("Inspection cabinet intake",x,3.6f,6.2f,3.2f,Back,-4);B("Inspection cabinet status",x-2.3f,4.3f,.5f,.2f,Teal,-3);
                B("Inspection cabinet lower panel",x,1.55f,6.7f,.5f,Steel*.65f,-3);
            }
            B("Equipment safety floor strip",-14,.055f,56,.055f,Ivory*.65f,3);
            for(int i=0;i<7;i++)B("Equipment safety floor stripe",-39+i*7,.08f,.18f,.12f,Amber,4);
        }
        void BuildStockHall()
        {
            Portal(74,67,Teal);
            for(int rack=0;rack<4;rack++)
            {
                float x=46+rack*11.4f;B("Core inventory rack frame",x,4.1f,8.8f,7.8f,Steel,-6);B("Core rack dark back",x,4.1f,8.2f,7.1f,Back,-5);
                for(int row=0;row<3;row++)
                {
                    float y=1.6f+row*2.3f;B("Core inventory shelf",x,y-.65f,8.4f,.16f,Ivory*.7f,-4);
                    for(int core=0;core<3;core++)
                    {
                        float cx=x-2.6f+core*2.6f;B("Stock core shield",cx,y,.9f,1.2f,Steel,-3);
                        B("Stock core glow",cx,y,.42f,.85f,new Color(.12f,.55f,.65f),-2);B("Stock core white line",cx-.1f,y,.07f,.7f,Ivory,-1);
                    }
                }
            }
            B("Stock administration console",92,.8f,1.6f,1.6f,Steel,2);B("Stock console display",92,1.3f,1.2f,.65f,Teal,3);
            B("Stock console print shelf",92,1.78f,1.9f,.1f,Ivory,4);
            B("Priority reserve caution plate",60,8.8f,12,.7f,Back,-3);B("Priority reserve plate underline",60,8.42f,12,.04f,Amber,-2);
        }
        void BuildChaseLane()
        {
            float left=AncientProductionLayout.ChaseStart,right=AncientProductionLayout.ChaseEnd+144;
            game.Platform(left-12,right+24,0);game.Ceiling(left-12,right+24,AncientProductionLayout.TunnelCeiling);
            for(float x=left-12;x<right+24;x+=20)
            {
                int section=x<left+520?0:x<left+1040?1:2;Color accent=section==1?Teal:Amber;
                B("Transport tunnel rear wall",x+10,5,20,10,section==1?new Color(.035f,.10f,.13f):Back,-9);
                B("Transport tunnel column",x,5,.65f,10,Steel,-6);B("Transport tunnel ceiling girder",x+10,9.8f,19.4f,.4f,Steel,-5);
                B("Transport tunnel lamp",x+10,9.54f,3.6f,.12f,Ivory*.7f,-3);B("Transport lane direction stripe",x+8,.04f,4.2f,.08f,accent,3);
                B("Transport tunnel gutter",x+10,.15f,20,.1f,Steel,1);
                if(section==1)
                {
                    B("Cooling pipe",x+10,7.4f,20,.6f,Steel,-6);B("Cooling pipe light",x+10,7.4f,20,.07f,Teal,-5);
                    B("Cooling pipe riser",x+3,5.2f,.75f,4.4f,Steel,-5);B("Cooling inspection collar",x+3,6.8f,1.1f,.2f,Ivory*.65f,-4);
                }
                else
                {
                    B("Freight tunnel service recess",x+10,4.8f,12,6.5f,new Color(.025f,.045f,.06f),-7);
                    B("Freight tunnel lower door",x+10,2.6f,5.4f,4.8f,Steel*.5f,-6);
                    B("Freight tunnel upper warning",x+10,6.6f,6.8f,.12f,accent,-4);
                }
                if(section==2){B("Emergency siding frame",x+15,4.2f,.3f,8.4f,Ivory*.45f,-4);B("Emergency siding beacon",x+15,8.65f,.7f,.25f,Amber,-3);}
            }
        }
        void BuildBreach()
        {
            breach=game.Box("Reinforced production wall breach section",new Vector2(AncientProductionLayout.BreachX,5),new Vector2(.45f,10),Steel,5);
            breach.transform.SetParent(transform,true);breach.layer=8;breach.AddComponent<BoxCollider2D>();
            for(int i=0;i<8;i++)
            {
                var o=game.Box("Breach wall fragment",Vector2.zero,new Vector2(.3f+(i%3)*.12f,.45f+(i%2)*.3f),i%2==0?Ivory:Steel,13);
                o.transform.SetParent(transform,true);o.SetActive(false);shards[i]=new Shard{Transform=o.transform,View=o.GetComponent<SpriteRenderer>()};
            }
        }
        public void OpenBreach()
        {
            if(BreachOpen)return;BreachOpen=true;breach.SetActive(false);breachAge=0;
            for(int i=0;i<shards.Length;i++)
            {
                var shard=shards[i];shard.Transform.gameObject.SetActive(true);shard.Transform.position=new Vector2(AncientProductionLayout.BreachX,1.1f+i*.92f);
                shard.Transform.rotation=Quaternion.identity;shard.Velocity=new Vector2(i%2==0?-3.5f-i*.4f:2+i*.5f,1.5f+i*.3f);shard.View.color=i%2==0?Ivory:Steel;shards[i]=shard;
            }
            game.Sound(4);game.Shake(.24f);game.Impact(new Vector2(AncientProductionLayout.BreachX,3.6f),Amber,true);
        }
        public void ResetBreach()
        {BreachOpen=false;breachAge=-1;if(breach)breach.SetActive(true);foreach(var shard in shards)if(shard.Transform)shard.Transform.gameObject.SetActive(false);}
        void Update()
        {
            if(!game||!game.hero||!game.Story||game.MainMenu||game.skills||game.Story.DialogueOpen)return;
            if(breachAge>=0)
            {
                float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);breachAge+=dt;
                for(int i=0;i<shards.Length;i++)
                {
                    var shard=shards[i];if(breachAge>=.9f){shard.Transform.gameObject.SetActive(false);continue;}
                    shard.Velocity+=Vector2.down*18*dt;shard.Transform.position+=(Vector3)(shard.Velocity*dt);shard.Transform.Rotate(0,0,(i%2==0?-1:1)*180*dt);
                    Color color=shard.View.color;color.a=Mathf.Clamp01(1-breachAge/.9f);shard.View.color=color;shards[i]=shard;
                }
                if(breachAge>=.9f)breachAge=-1;
            }
            if(game.Paused||game.hero.bike||game.Story.CurrentFloor!=0)return;
            Vector2 p=game.hero.transform.position;
            if(p.x>=AncientProductionLayout.ConveyorLeft&&p.x<=AncientProductionLayout.ConveyorRight&&Mathf.Abs(p.y-1.1f)<.35f&&game.hero.IsOnGround)
                game.hero.ConveyorDrift(.45f);
        }
        void OnGUI()
        {
            if(!game||!game.hero||!game.Story||!game.View||game.MainMenu||game.Paused||Event.current.type!=EventType.Repaint||game.Story.CurrentFloor!=0)return;
            float heroX=game.hero.transform.position.x;
            if(!AncientProductionLayout.InTutorial(heroX)&&!AncientProductionLayout.InChaseLane(heroX))return;
            if(label==null)label=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=14,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;GUI.matrix=Matrix4x4.identity;GUI.depth=4;GUI.color=new Color(.74f,.88f,.86f);
            foreach(var sign in signs)
            {
                if(sign.Point.x==AncientProductionLayout.StockX&&game.Story.Stage!=3)continue;
                if(Mathf.Abs(sign.Point.x-heroX)>24)continue;Vector3 screen=game.View.WorldToScreenPoint(sign.Point);
                if(screen.z<=0||screen.x<-100||screen.x>Screen.width+100||screen.y<0||screen.y>Screen.height)continue;
                GUI.Label(new Rect(screen.x-sign.Width*.5f,Screen.height-screen.y-28,sign.Width,56),sign.Text,label);
            }
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
    }
}
