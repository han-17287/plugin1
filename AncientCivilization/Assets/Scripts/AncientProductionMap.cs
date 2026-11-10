using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Gameplay colliders stay separate from the shared static scenery meshes.
    public sealed class AncientProductionMap : MonoBehaviour
    {
        struct Sign
        {
            public readonly Vector2 Point;public readonly GUIContent Content;public readonly float Width;
            public Sign(float x,float y,string text,float width=300){Point=new Vector2(x,y);Content=new GUIContent(text);Width=width;}
        }
        struct Shard {public Transform Transform;public SpriteRenderer View;public Vector2 Velocity;}
        struct PressMotion
        {
            public float X,HeadFloor;public Transform Rod,Head;
            public SpriteRenderer RodView,HeadView,Status;
        }
        AncientGame game;GameObject breach;readonly Shard[] shards=new Shard[8];float breachAge=-1;GUIStyle label;
        CapsuleCollider2D officerShape;BoxCollider2D motorcycleShape;
        Transform inspectionCarrier;SpriteRenderer stockWarning;Sprite paint;float ambientAge,stockAge,beltTravel,pressAge;
        readonly SpriteRenderer[] beltTreads=new SpriteRenderer[10];readonly PressMotion[] presses=new PressMotion[3];
        public bool BreachOpen {get;private set;}
        readonly Sign[] signs={
            new Sign(-229,4.2f,"교대 통로\nA/D 이동 · Ctrl 달리기 전환"),
            new Sign(-201,5.4f,"교대 공고\n거주 포인트 재산정 대기",200),
            new Sign(-186,3.6f,"운송 벨트\nSpace: 낮은 장애물 넘기"),
            new Sign(-128,5.8f,"검수 회랑\n좌클릭: 직선 견인 · 가운데 클릭: 스윙"),
            new Sign(-77,7.7f,"작업 고리 지름길 / 아래는 안전한 통로",340),
            new Sign(-26,4.6f,"장비 검수실\n휠 위: 공격 · 짧은 좌클릭: 베기"),
            new Sign(7,7.3f,"장비 검수 배출구 / 훈련용 빈 하우징",330),
            new Sign(38,4.6f,"코어 재고 관리실 ▶"),
            new Sign(60,8.8f,"C-00 예비분 / 별도 계량",260),
            new Sign(80.2f,7.9f,"반출 기록 없음",180),
            new Sign(92,3.6f,"재고 관리 단말\nE: 코어 수량 확인"),
            new Sign(30018,7.8f,"하층 동부 화물 터널 / 자동주행",360),
            new Sign(30580,7.8f,"냉각 배관선 / 수송 차량 우선",360),
            new Sign(31130,7.8f,"화물 분리·긴급 정차 구간",360)
        };
        static readonly Color Back=new Color(.035f,.065f,.085f),Steel=new Color(.18f,.27f,.30f),Ivory=new Color(.65f,.72f,.70f),Teal=new Color(.16f,.62f,.66f),Amber=new Color(.92f,.64f,.24f);
        public void Initialize(AncientGame owner)
        {
            game=owner;paint=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            BuildTutorial();BuildChaseLane();BuildBreach();BuildAmbientMachinery();
        }
        void B(string name,float x,float y,float w,float h,Color color,int order=-5)
        {game.StaticBox(name,new Vector2(x,y),new Vector2(w,h),color,order);}
        void Prop(int index,float x,float y,float scale=1,int order=-4,bool supported=true)
        {
            var sprite=AncientArt.ProductionProp(index);
            if(supported&&y>0)
            {
                // The native crop keeps three transparent pixels below the machine.
                // Rear-wall stands meet that visible foot edge, without adding collision.
                float width=sprite.bounds.size.x*scale+.3f;
                float foot=y+3/AncientProductionPropAtlas.PixelsPerUnit*scale,deckBottom=foot-.16f;
                Color stand=new Color(.13f,.2f,.23f);
                B("Grounded rear machine stand",x,foot-.08f,width,.16f,stand,-6);
                for(int side=-1;side<=1;side+=2)
                {
                    float legX=x+side*(width*.5f-.28f),ground=AncientProductionLayout.GroundHeight(legX);
                    float top=deckBottom+.04f,height=Mathf.Max(.04f,top-ground);
                    B("Grounded rear machine stand leg",legX,top-height*.5f,.2f,height,stand,-6);
                }
            }
            game.WorldBatch.Sprite(sprite,new Vector2(x,y),Vector2.one*scale,new Color(.68f,.75f,.78f,.92f),order);
        }
        void Solid(string name,float x,float y,float w,float h)
        {
            var o=new GameObject(name);o.layer=8;o.transform.SetParent(transform,false);o.transform.position=new Vector2(x,y);o.AddComponent<BoxCollider2D>().size=new Vector2(w,h);
        }
        void Diagonal(string name,float x,float y,float length,float width,float angle,Color color,int order=-4)
        {game.WorldBatch.RotatedSprite(paint,new Vector2(x,y),new Vector2(length,width),color,order,angle);}
        void Arrow(float x,float y,Color color,float scale=1)
        {
            B("Painted route arrow",x-.15f*scale,y,.9f*scale,.08f*scale,color,3);
            Diagonal("Painted route arrow head",x+.38f*scale,y+.13f*scale,.42f*scale,.08f*scale,-38,color,3);
            Diagonal("Painted route arrow head",x+.38f*scale,y-.13f*scale,.42f*scale,.08f*scale,38,color,3);
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
                B("Inspection hook mechanical socket",x,AncientProductionLayout.WireHookY,.78f,.42f,Steel,5);
                foreach(float side in new[]{-1f,1f})B("Inspection hook socket lug",x+side*.36f,AncientProductionLayout.WireHookY,.09f,.6f,Ivory,5);
                game.WorldBatch.Sprite(AncientArt.WireParticle(),point,Vector2.one*.45f,Teal,6);
                B("Inspection hook bright centre",x,AncientProductionLayout.WireHookY,.09f,.09f,Color.white,7);
            }
            // Mid-height hoists can be selected for a useful swing from a service grating.
            // The higher zip sockets alone are too close to the roof for an upward swing kick.
            foreach(float x in AncientProductionLayout.SwingHookX)
            {
                float y=AncientProductionLayout.SwingHookY;game.anchors.Add(new Vector2(x,y));
                B("Swing hoist drop cable",x,(AncientProductionLayout.Ceiling+y)*.5f,.06f,AncientProductionLayout.Ceiling-y,Steel,-3);
                B("Swing hoist lower socket",x,y-.25f,.72f,.12f,Steel,5);
                foreach(float side in new[]{-1f,1f})B("Swing hoist lower lug",x+side*.34f,y,.12f,.5f,Ivory,5);
                game.WorldBatch.Sprite(AncientArt.WireParticle(),new Vector2(x,y),Vector2.one*.42f,Teal,6);
                B("Swing hoist bright centre",x,y,.08f,.08f,Color.white,7);
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
            foreach(float x in new[]{-215f,-195f,-178f,-144f,-124f,-104f,-84f,-64f,-44f,-12f,20f,48f,73f})
                Arrow(x,AncientProductionLayout.GroundHeight(x)+.18f,Teal*.7f,.8f);
            // Bright paint is reserved for physical edges and the useful wire route.
            foreach(var span in AncientProductionLayout.WireShortcut)
            {
                Arrow((span.Left+span.Right)*.5f,span.Top+.27f,Teal,.7f);
                for(int edge=0;edge<3;edge++)B("Catwalk landing edge paint",span.Left+.2f+edge*.2f,span.Top+.04f,.08f,.08f,Ivory,5);
            }
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
            foreach(float x in new[]{-233f,-222f,-211f})Prop(2,x,0,.9f);
            B("Workers notice board",-201,5.4f,5.8f,2.6f,Steel,-5);B("Workers notice sheet",-201,5.4f,5.2f,2.15f,Ivory*.7f,-4);
            for(int line=0;line<4;line++)B("Notice board list",-201.2f,6-line*.4f,3.6f,.08f,Back,-3);
            B("Main supply conduit",-222,9,41,.35f,Steel,-4);B("Main supply conduit light",-222,9.02f,40,.055f,Teal,-3);
            B("Shift bench shadow",-213,.3f,7,.32f,Back,-4);B("Shift bench seat",-213,.6f,7,.18f,Ivory*.7f,-3);
            foreach(float x in new[]{-215.8f,-210.2f})B("Shift bench leg",x,.28f,.16f,.56f,Steel,-3);
            B("Worker thermos",-211,.95f,.22f,.54f,Amber*.65f,-2);
            B("Shift clock mount",-198,7.8f,2.2f,1.25f,Steel,-4);B("Shift clock dark face",-198,7.8f,1.9f,.95f,Back,-3);
            foreach(float x in new[]{-198.55f,-198.05f,-197.55f})B("Shift clock digits",x,7.8f,.18f,.45f,Ivory,-2);
        }
        void BuildConveyorHall()
        {
            Portal(-164,52,Amber*.55f);
            // The visible belt surface meets the real ground at y=0; markings sit
            // just above it so boots do not appear to sink through raised rollers.
            B("Conveyor road bed",-167,-.055f,18,.11f,new Color(.11f,.17f,.19f),2);
            for(float x=-175;x<=-159;x+=1.3f)
            {B("Conveyor steel roller",x,-.07f,.7f,.14f,Steel,3);B("Conveyor fixed roller light",x+.18f,.02f,.22f,.04f,Teal*.7f,4);}
            for(int i=0;i<3;i++)
            {
                float x=-188+i*18;B("Production press casing",x,5.2f,7.2f,7.6f,Steel*.7f,-7);B("Production press opening",x,4.6f,6.8f,6.8f,Back,-6);
                Prop(i==0?0:1,x,1.1f);
                B("Press white upper shell",x,7.8f,6.8f,.65f,Ivory*.5f,-5);
            }
            for(float x=-190;x<-139;x+=5.5f)B("Machine status indicator",x,8.65f,.5f,.15f,Teal,-2);
            // The raised maintenance road belongs to a machine foundation, rather than a floating platform.
            B("Press foundation backing",-143,.38f,14,.76f,Steel,-2);
            for(float x=-149;x<-136;x+=2)B("Press foundation bolt",x,.5f,.12f,.12f,Ivory*.7f,-1);
            foreach(float x in new[]{-193f,-177f,-161f,-145f})
            {
                B("Conveyor safety chain post",x,1.55f,.09f,2.9f,Steel,-5);
                B("Conveyor safety chain cap",x,3.02f,.24f,.16f,Amber,-4);
            }
            Prop(6,-138,3.5f,.65f,-4,false);
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
            // The low lane stays open. Hoist braces and glazing remain scenery behind it.
            foreach(float x in new[]{-124f,-104f,-84f,-64f})
            {
                Diagonal("Inspection hoist brace",x+1.4f,8.3f,3.2f,.12f,38,Steel,-4);
                B("Inspection hook painted approach",x+3.45f,6.1f,.7f,.16f,Teal*.7f,-3);
            }
            B("Wire route entry placard",-126,6.2f,3.1f,1.1f,Back,-3);
            Arrow(-126,6.2f,Teal,1.2f);
            B("Inspection exit skylight",-56,7.9f,5.5f,1.4f,new Color(.08f,.22f,.26f),-6);
        }
        void BuildEquipmentRoom()
        {
            Portal(-9,80,Ivory*.55f);
            for(int index=0;index<4;index++)
            {
                float x=index==0?-42:index==1?-18:index==2?7:28;
                B("Inspection machinery cabinet recess",x,4.3f,8.4f,6.6f,Back,-6);
                Prop(index==0?0:index==1?7:index==2?6:3,x,1.2f);
            }
            foreach(float x in AncientProductionLayout.TrainingEmitterX)
            {
                float y=AncientProductionLayout.TrainingEmitterY;
                B("Training housing launcher backing",x,y,1.4f,1.25f,Steel,-2);
                B("Training housing launcher casing",x,y,1.2f,.8f,Ivory*.65f,-1);
                B("Training housing launcher aperture",x,y,.7f,.4f,Back,0);
                B("Training housing launcher amber lip",x,y+.31f,.75f,.07f,Amber,1);
                B("Training housing launcher stand",x,.55f,.16f,1.1f,Steel,-2);
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
                        if(AncientProductionLayout.StockCorePresent(rack,row,core))
                        {B("Stock core glow",cx,y,.42f,.85f,new Color(.12f,.55f,.65f),-2);B("Stock core white line",cx-.1f,y,.07f,.7f,Ivory,-1);}
                        else
                        {
                            B("Empty core cradle",cx,y,.7f,1.02f,Back,-2);
                            B("Empty core cradle lower contact",cx,y-.43f,.6f,.1f,Ivory*.6f,-1);
                            B("Missing stock amber marker",cx,y+.68f,.46f,.08f,Amber,-1);
                        }
                    }
                }
            }
            B("Stock administration console",92,.8f,1.6f,1.6f,Steel,2);B("Stock console display",92,1.3f,1.2f,.65f,Teal,3);
            B("Stock console print shelf",92,1.78f,1.9f,.1f,Ivory,4);
            Prop(3,98,1.1f,.82f);
            B("Priority reserve caution plate",60,8.8f,12,.7f,Back,-3);B("Priority reserve plate underline",60,8.42f,12,.04f,Amber,-2);
            B("Unlogged cargo trolley bay",84,.34f,5.4f,.14f,Amber*.55f,-2);
            foreach(float x in new[]{78f,80.5f,83f,85.5f,88f,90.5f,93f,95.5f,98f,100.5f})
            {B("Stock freight wheel mark",x,.035f,1.3f,.025f,new Color(.14f,.18f,.17f),4);B("Stock freight wheel mark",x+.1f,.19f,.8f,.025f,new Color(.14f,.18f,.17f),4);}
            B("Cargo wall impact scuff",103.72f,2.3f,.1f,2.8f,Amber*.45f,6);
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
                if(((int)((x-left+12)/20))%3==0)
                {
                    if(section==1)Prop(6,x+11,2.2f,.95f,-5,false);
                    else if(section==2)Prop(3,x+9,1.3f,.88f,-5);
                    B("Tunnel heavy diagonal foot",x+1.6f,1.25f,.9f,2.5f,Steel*.8f,-6);
                    Diagonal("Tunnel heavy diagonal support",x+2.7f,6.3f,5.4f,.32f,-65,Steel*.8f,-6);
                    B("Tunnel service designation plaque",x+5.4f,5.9f,2.8f,1.25f,section==1?Ivory*.3f:Steel,-5);
                    for(int stripe=0;stripe<=section;stripe++)B("Tunnel service designation stripe",x+4.65f+stripe*.55f,5.9f,.22f,.6f,section==1?Teal:Amber,-4);
                    Arrow(x+10,.5f,accent*.55f,1.6f);
                }
            }
        }
        void BuildAmbientMachinery()
        {
            inspectionCarrier=new GameObject("Inspection overhead sample carrier / scenery").transform;
            inspectionCarrier.SetParent(transform,false);inspectionCarrier.position=new Vector2(-119,7.55f);
            CarrierPart("Sample carrier trolley",new Vector2(0,.96f),new Vector2(1.5f,.38f),Steel,-3);
            CarrierPart("Sample carrier hanging chain",new Vector2(0,.36f),new Vector2(.08f,.9f),Ivory*.7f,-3);
            CarrierPart("Sample carrier empty housing",Vector2.zero,new Vector2(1.65f,.85f),Steel,-3);
            CarrierPart("Sample carrier inspection panel",new Vector2(0,.03f),new Vector2(1.22f,.55f),Amber*.6f,-2);
            CarrierPart("Sample carrier retaining band",new Vector2(.38f,.03f),new Vector2(.14f,.74f),Ivory*.65f,-1);
            var warning=game.Box("Missing inventory status lamp",new Vector2(80.2f,7.02f),new Vector2(.6f,.15f),Amber,1);
            warning.transform.SetParent(transform,true);stockWarning=warning.GetComponent<SpriteRenderer>();
            for(int i=0;i<beltTreads.Length;i++)
                beltTreads[i]=AmbientPart("Reused conveyor moving tread "+i,new Vector2(AncientProductionLayout.ConveyorLeft,.025f),new Vector2(.38f,.04f),Amber*.85f,4);
            for(int i=0;i<presses.Length;i++)
            {
                float x=-188+i*18;
                B("Press fixed upper cylinder",x,7.64f,.48f,.45f,Steel,-2);
                B("Press safety indicator housing",x+2.7f,7.8f,.46f,.25f,Back,-2);
                var rod=AmbientPart("Reused press piston rod "+i,new Vector2(x,7.25f),new Vector2(.16f,.55f),Ivory*.7f,-3);
                var head=AmbientPart("Reused press ram head "+i,new Vector2(x,6.9f),new Vector2(1.1f,.22f),Steel,-3);
                presses[i]=new PressMotion{X=x,HeadFloor=1.1f+AncientArt.ProductionProp(i==0?0:1).bounds.size.y+.11f,
                    Rod=rod.transform,Head=head.transform,RodView=rod,HeadView=head,
                    Status=AmbientPart("Reused press safety indicator "+i,new Vector2(x+2.7f,7.8f),new Vector2(.24f,.11f),Teal,-2)};
            }
        }
        SpriteRenderer AmbientPart(string name,Vector2 point,Vector2 size,Color color,int order)
        {
            var part=game.Box(name,point,size,color,order);part.transform.SetParent(transform,true);
            var view=part.GetComponent<SpriteRenderer>();view.enabled=false;return view;
        }
        static bool InAmbientView(Vector2 point,Vector2 size,Vector2 camera,Vector2 halfView)
        {return Mathf.Abs(point.x-camera.x)<halfView.x+size.x*.5f&&Mathf.Abs(point.y-camera.y)<halfView.y+size.y*.5f;}
        static void ShowAmbient(SpriteRenderer view,bool visible)
        {if(view.enabled!=visible)view.enabled=visible;}
        void TickIndustry(float activeDelta)
        {
            bool lower=game.HasSession&&!game.MainMenu&&game.Story.CurrentFloor==0&&
                !(game.Production&&game.Production.InChaseLane)&&game.View;
            Vector2 camera=game.View?(Vector2)game.View.transform.position:Vector2.zero;
            Vector2 halfView=game.View?new Vector2(game.View.orthographicSize*game.View.aspect,game.View.orthographicSize):Vector2.zero;
            float beltWidth=AncientProductionLayout.ConveyorRight-AncientProductionLayout.ConveyorLeft-.5f;
            if(lower&&activeDelta>0)
            {
                // Hero turns ConveyorDrift(.45) into .45*3.5 metres per game second.
                beltTravel=Mathf.Repeat(beltTravel+activeDelta*1.575f,beltWidth);
                pressAge=Mathf.Repeat(pressAge+activeDelta,3.4f);
            }
            for(int i=0;i<beltTreads.Length;i++)
            {
                float x=AncientProductionLayout.ConveyorLeft+.25f+Mathf.Repeat(beltTravel+i*beltWidth/beltTreads.Length,beltWidth);
                Vector2 point=new Vector2(x,.025f);
                bool visible=lower&&InAmbientView(point,new Vector2(.38f,.04f),camera,halfView);
                ShowAmbient(beltTreads[i],visible);if(visible)beltTreads[i].transform.position=point;
            }
            for(int i=0;i<presses.Length;i++)
            {
                var press=presses[i];
                bool visible=lower&&InAmbientView(new Vector2(press.X,7.3f),new Vector2(6.2f,1.7f),camera,halfView);
                if(!visible){ShowAmbient(press.RodView,false);ShowAmbient(press.HeadView,false);ShowAmbient(press.Status,false);continue;}
                float cycle=Mathf.Repeat(pressAge+i*.9f,3.4f)/3.4f;
                float down=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.36f,cycle));
                float up=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.8f,cycle));
                float compression=down*(1-up),headY=press.HeadFloor+.4f*(1-compression);
                float rodBottom=headY+.06f,rodHeight=Mathf.Max(.08f,7.52f-rodBottom);
                Vector2 rodPoint=new Vector2(press.X,rodBottom+rodHeight*.5f),headPoint=new Vector2(press.X,headY);
                press.Rod.position=rodPoint;press.Rod.localScale=new Vector3(.16f,rodHeight,1);press.Head.position=headPoint;
                press.Status.color=Color.Lerp(Teal,Amber,compression*.85f);
                ShowAmbient(press.RodView,InAmbientView(rodPoint,new Vector2(.16f,rodHeight),camera,halfView));
                ShowAmbient(press.HeadView,InAmbientView(headPoint,new Vector2(1.1f,.22f),camera,halfView));
                ShowAmbient(press.Status,InAmbientView(new Vector2(press.X+2.7f,7.8f),new Vector2(.24f,.11f),camera,halfView));
            }
        }
        void CarrierPart(string name,Vector2 offset,Vector2 size,Color color,int order)
        {
            var part=game.Box(name,Vector2.zero,size,color,order);
            part.transform.SetParent(inspectionCarrier,false);part.transform.localPosition=offset;
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
            // Completed theft and patrol saves must retain the physical exit. Restoring a
            // wall here would strand legacy checkpoints on the wrong side of the stock hall.
            if(AncientGame.Saved("questStage")>=5)RestoreBreachOpen();
        }
        public void RestoreBreachOpen()
        {
            BreachOpen=true;breachAge=-1;if(breach)breach.SetActive(false);
            foreach(var shard in shards)if(shard.Transform)shard.Transform.gameObject.SetActive(false);
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
            if(!game||!game.hero||!game.Story)return;
            bool externalPause=game.MainMenu||game.skills||game.Story.DialogueOpen||game.Story.LiftBusy||
                (game.Journal&&game.Journal.Open)||!Application.isFocused;
            float activeDelta=externalPause||game.Paused?0:Time.deltaTime;
            TickIndustry(activeDelta);
            float heroX=game.hero.transform.position.x;
            bool visibleInspection=!game.MainMenu&&!game.hero.bike&&heroX>-145&&heroX<-35&&game.Story.CurrentFloor==0;
            if(inspectionCarrier.gameObject.activeSelf!=visibleInspection)inspectionCarrier.gameObject.SetActive(visibleInspection);
            if(stockWarning)stockWarning.enabled=!game.MainMenu&&heroX>54&&heroX<107&&game.Story.CurrentFloor==0;
            if(externalPause)return;
            if(visibleInspection)
            {
                ambientAge+=activeDelta;
                inspectionCarrier.localPosition=new Vector2(-119+Mathf.PingPong(ambientAge*1.25f,52),7.55f);
            }
            if(stockWarning&&stockWarning.enabled)
            {
                stockAge=Mathf.Repeat(stockAge+activeDelta,Mathf.PI*2/2.2f);
                stockWarning.color=Color.Lerp(Amber*.55f,Amber,.5f+.5f*Mathf.Sin(stockAge*2.2f));
            }
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
            if(!officerShape)officerShape=game.hero.GetComponent<CapsuleCollider2D>();
            if(!motorcycleShape)motorcycleShape=game.hero.GetComponent<BoxCollider2D>();
            Collider2D shape=game.hero.bike?(Collider2D)motorcycleShape:officerShape;
            Rect officerRect=new Rect();
            if(shape)
            {
                Bounds bounds=shape.bounds;Vector3 low=game.View.WorldToScreenPoint(bounds.min),high=game.View.WorldToScreenPoint(bounds.max);
                officerRect=Rect.MinMaxRect(low.x-10,Screen.height-high.y-10,high.x+10,Screen.height-low.y+10);
            }
            float scale=Mathf.Clamp(Screen.height/720f,.85f,1.6f);label.fontSize=Mathf.RoundToInt(14*scale);
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;GUI.matrix=Matrix4x4.identity;GUI.depth=4;
            foreach(var sign in signs)
            {
                if(sign.Point.x==AncientProductionLayout.StockX&&game.Story.Stage!=3)continue;
                if(Mathf.Abs(sign.Point.x-heroX)>24)continue;Vector3 screen=game.View.WorldToScreenPoint(sign.Point);
                if(screen.z<=0||screen.x<-100||screen.x>Screen.width+100||screen.y<0||screen.y>Screen.height)continue;
                float width=Mathf.Min(sign.Width*scale,Screen.width-24),height=label.CalcHeight(sign.Content,width)+8*scale;
                Rect panel=new Rect(screen.x-width*.5f,Screen.height-screen.y-height*.5f,width,height);
                // A wall hint can be read on approach. During a jump or ceiling zip,
                // let the body pass in front instead of painting a dark panel over it.
                if(shape&&panel.Overlaps(officerRect))continue;
                GUI.color=new Color(.018f,.035f,.045f,.82f);GUI.DrawTexture(panel,Texture2D.whiteTexture);
                GUI.color=new Color(.74f,.88f,.86f);GUI.Label(panel,sign.Content,label);
            }
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
        void OnDestroy(){if(paint)Destroy(paint);}
    }
}
