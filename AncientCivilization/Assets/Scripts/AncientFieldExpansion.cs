using System;

namespace Plugin.AncientCivilization
{
    // Three authored traversal sections replace each short investigation interior.
    // Lower districts contain four traversable blocks per sector. Upper districts retain one.
    public static class AncientFieldExpansion
    {
        public const int SectorCount=3,SectorSpan=208,Width=SectorCount*SectorSpan,Stride=Width+64,RouteCount=27;
        public const int EnemiesPerSector=2,EnemyMask=(1<<(SectorCount*EnemiesPerSector))-1;
        public const float ExpertLedgeDepth=.18f;
        public static readonly string[][] Names={
            new[]{"야간 하역장","분리 계량실","밀봉 출고로"},
            new[]{"압력 배관구","고장 도관실","수동 배전소"},
            new[]{"봉쇄 작업선","야간 잔류실","노동자 귀환로"},
            new[]{"인계 정비동","교체 승인실","사본 송신탑"},
            new[]{"생활부 골목","진료소 앞길","운반함 흔적"},
            new[]{"폐차 적치장","상자 운송로","창고 연결로"},
            new[]{"감시 통로","경비 로봇 구역","본거지 외벽"},
            new[]{"밀매 창고","코어 적재실","로봇 격납고"},
            new[]{"민원 접수로","차량 조회실","사건 연결동"},
            new[]{"감사 차단구","폐기 제어실","봉인 원본고"},
            new[]{"복구 기록실","사본 대조실","일괄 서명동"},
            new[]{"통제 대상창구","조사 직원실","증인 안전회랑"},
            new[]{"조경 조명로","공용 분기실","회수 계측동"},
            new[]{"계약 전시실","명단 보관실","지반 설계회랑"},
            new[]{"주소 검문로","이송 대기원","거주 안전 경계"},
            new[]{"포인트 심사동","직할 승인실","외벽 접근 정원"},
            new[]{"무기명 정비로","외벽 감시대","검문 전송회랑"},
            new[]{"비공개 접수실","삭제 기록층","직할 승인선"},
            new[]{"흔적 정비대","미승인 우회로","회장실 전방교"},
            new[]{"연구 색인고","회수 승인실","출발 관제실"}
        };
        public static int Blocks(int mission){return mission>=0&&mission<8?4:1;}
        public static int Span(int mission){return SectorSpan*Blocks(mission);}
        public static int WidthFor(int mission){return SectorCount*Span(mission);}
        public static float SectorStart(int mission,int sector){return Sector(sector)*Span(mission);}
        public static int SectorAt(int mission,float localX)
        {return SectorAt(localX/Blocks(mission));}
        public static float EntranceOf(int mission,int sector){return SectorStart(mission,sector)+6;}
        public static float ExitOf(int mission,int sector){return SectorStart(mission,sector)+Span(mission)-12;}
        // Only fixed, authored landings can become retry points. Moving cranes,
        // combat roofs and arbitrary saved coordinates are deliberately excluded.
        public static bool TryRestPoint(int mission,int sector,int pad,out float x,out float y)
        {
            x=y=0;
            if(mission<0||mission>=8||sector<0||sector>=3||pad<0||pad>=8||IsJumpSector(mission,sector)&&pad%2==1)return false;
            x=SectorStart(mission,sector)+(pad/2)*SectorSpan+(pad%2==0?6:86);y=1.3f;return true;
        }
        public static AncientTraversalLayout.Ledge GroundRoad(int mission,int sector)
        {float start=SectorStart(mission,sector);return new AncientTraversalLayout.Ledge(start,start+Span(mission),0);}
        public static AncientTraversalLayout.Ledge BlockLanding(int mission,int sector,int block,bool end)
        {float start=SectorStart(mission,sector)+Math.Max(0,Math.Min(Blocks(mission)-1,block))*SectorSpan;return new AncientTraversalLayout.Ledge(start+(end?180:0),start+(end?208:14),0);}
        // Structural street silhouettes, not nine equally spaced floating tiles.
        // Coordinates are local to a block. Broad 80..164 work courts preserve
        // witnesses, throw props, combat and unearned progress gates at street level.
        public static bool HasStreetDesign(int mission,int sector)
        {return mission>=0&&mission<8&&!IsJumpSector(mission,sector);}
        static AncientTraversalLayout.Ledge L(float a,float b,float y)
        {return new AncientTraversalLayout.Ledge(a,b,y);}
        static readonly AncientTraversalLayout.Ledge[][] Streets={
            // Loading dock: climb containers, cross a hoist well, descend to the yard.
            new[]{L(0,16,0),L(16,27,1),L(27,39,2.2f),L(39,51,3.4f),L(51,60,-1.4f),L(60,70,1.6f),L(70,80,.7f),L(80,164,0),L(164,175,1.1f),L(175,186,2.3f),L(186,195,.9f),L(195,208,0)},
            // Pressure conduit: descend under the machinery and climb its service stairs.
            new[]{L(0,18,0),L(18,28,-.7f),L(28,43,-1.4f),L(43,54,-.6f),L(54,64,1.2f),L(64,74,1.7f),L(74,80,.7f),L(80,164,0),L(164,176,-.8f),L(176,188,-1.4f),L(188,198,-.6f),L(198,208,0)},
            // Freight roofs: two broad loading roofs with a lower recovery trench.
            new[]{L(0,16,0),L(16,26,1.2f),L(26,43,2.5f),L(43,51,-1.4f),L(51,65,3.5f),L(65,74,1.7f),L(74,80,.7f),L(80,164,0),L(164,176,1.2f),L(176,190,2.6f),L(190,199,1.2f),L(199,208,0)},
            // Tenement stair: roof approach, sheltered lane and a low street exit.
            new[]{L(0,18,0),L(18,28,.9f),L(28,39,1.8f),L(39,55,3),L(55,66,1.5f),L(66,80,.6f),L(80,164,0),L(164,176,-.7f),L(176,188,-1.4f),L(188,198,-.7f),L(198,208,0)},
            // Scrap yard: successive broad wrecks and two recovery bays.
            new[]{L(0,16,0),L(16,28,1.1f),L(28,41,2.2f),L(41,49,-1.4f),L(49,61,3.4f),L(61,71,1.7f),L(71,80,.7f),L(80,164,0),L(164,176,1.2f),L(176,184,-1.4f),L(184,196,1.5f),L(196,208,0)},
            // Security service lane: descend out of sight, then scale the hangar plinth.
            new[]{L(0,18,0),L(18,28,-.7f),L(28,46,-1.4f),L(46,57,-.6f),L(57,68,.7f),L(68,80,1.5f),L(80,164,0),L(164,174,1.2f),L(174,186,2.5f),L(186,198,1.2f),L(198,208,0)}
        };
        // Explicit sequencing gives each sector a different approach/release rhythm.
        // Escort cases retain a continuous ground lane; their optional upper route
        // still rewards the player, without making a rescued worker cross a pit.
        static readonly int[,] StreetSequence={
            {0,1,0,2},{1,0,2,0},{2,1,0,2},
            {1,2,1,0},{2,1,2,1},{1,0,1,2},
            {0,1,0,1},{1,0,1,0},{0,1,0,1},
            {2,0,1,2},{0,2,1,0},{2,1,2,0},
            {3,5,3,4},{5,3,4,3},{3,4,5,3},
            {4,3,4,5},{3,4,5,4},{4,5,4,3},
            {5,3,5,4},{4,5,3,5},{5,4,5,3},
            {4,5,4,3},{5,4,3,4},{4,3,5,4}};
        public static int StreetProfile(int mission,int sector,int block)
        {return StreetSequence[Math.Max(0,Math.Min(7,mission))*3+Sector(sector),Math.Max(0,Math.Min(3,block))];}
        public static bool EscortStreet(int mission){return mission==2||mission==5;}
        public static int StreetPieceCount(int mission,int sector,int block)
        {return EscortStreet(mission)?1:Streets[StreetProfile(mission,sector,block)].Length;}
        public static AncientTraversalLayout.Ledge StreetPiece(int mission,int sector,int block,int index)
        {
            float offset=SectorStart(mission,sector)+block*SectorSpan;
            var local=EscortStreet(mission)?L(0,208,0):Streets[StreetProfile(mission,sector,block)][index];
            // The last block hosts the original encounter/conveyor and must keep
            // its complete enemy patrol and supply panel grounded.
            if(block==3&&local.right>64&&local.right<=80)local=L(local.left,local.right,0);
            return L(offset+local.left,offset+local.right,local.top);
        }
        public static float StreetHeight(int mission,int sector,int block,float localX)
        {
            float x=SectorStart(mission,sector)+block*SectorSpan+localX,top=-1.4f;
            for(int i=0;i<StreetPieceCount(mission,sector,block);i++)
            {var p=StreetPiece(mission,sector,block,i);if(x>=p.left&&x<=p.right)top=Math.Max(top,p.top);}
            return top;
        }
        // Four landmark rooms break the horizontal rhythm: an actual 18m shaft
        // with a 9m machine core forces an ascent, crossing and controlled descent.
        public static bool TallStreetBay(int mission,int sector,int block)
        {return mission==1&&sector==1&&block==1||mission==3&&sector==0&&block==1||mission==6&&sector==2&&block==0||mission==7&&sector==1&&block==1;}
        public static float StreetHookHeight(int mission,int sector,int block,int index)
        {return TallStreetBay(mission,sector,block)&&index==1?13.6f:8.7f;}
        public static AncientTraversalLayout.Ledge StreetObstacle(int mission,int sector,int block)
        {float offset=SectorStart(mission,sector)+block*SectorSpan;return L(offset+34,offset+50,9);}
        public static AncientTraversalLayout.Ledge StreetShortcut(int mission,int sector,int block,int index)
        {
            float offset=SectorStart(mission,sector)+block*SectorSpan;
            // Upper grates end before the court; none bridge an investigation gate.
            float a=index==0?23:index==1?43:63;
            return L(offset+a,offset+a+7,index==1?(TallStreetBay(mission,sector,block)?11.1f:6.9f):6.2f);
        }
        public static AncientTraversalLayout.Ledge StreetBaffle(int mission,int sector,int block)
        {
            float offset=SectorStart(mission,sector)+block*SectorSpan;
            // Over the second staircase, a suspended duct blocks straight roof-zipping
            // and asks for a controlled drop; the whole capsule fits below it.
            return L(offset+177,offset+183,6.1f);
        }
        static readonly float[,] ProductionHeights={
            {.6f,1.2f,2f,3f,3.4f,2f,1.2f,2f,1f},
            {.8f,1.8f,3f,4.2f,3.4f,2.4f,3.2f,2f,.8f},
            {.6f,1.6f,2.8f,4f,3.3f,2.4f,1.5f,2.4f,1.1f}};
        static readonly float[,] LivingHeights={
            {.6f,1.6f,2.7f,3.8f,3f,2.2f,3.1f,1.8f,.8f},
            {.8f,2f,3.3f,4.4f,3.2f,2f,1.4f,2.5f,1f},
            {.6f,1.4f,2.6f,3.6f,3.1f,2.2f,3.3f,2.1f,.8f}};
        public static AncientTraversalLayout.Ledge BlockSegment(int mission,int sector,int block,int index)
        {
            mission=Math.Max(0,Math.Min(19,mission));block=Math.Max(0,Math.Min(Blocks(mission)-1,block));index=Math.Max(0,Math.Min(8,index));
            var ledge=Segment(mission,sector,index);float shift=(block-(Blocks(mission)-1))*SectorSpan,top=ledge.top;
            if(mission<8&&block<3&&IsJumpSector(mission,sector))top=(mission==3?ProductionHeights:LivingHeights)[block,index]+.15f;
            return new AncientTraversalLayout.Ledge(ledge.left+shift,ledge.right+shift,top);
        }
        public static AncientTraversalLayout.Ledge BlockExpertSegment(int mission,int sector,int block,int index)
        {var support=BlockSegment(mission,sector,block,index);return new AncientTraversalLayout.Ledge(support.left+1,support.left+3.2f,Math.Max(6.2f,support.top+2.45f));}
        static int Sector(int sector){return Math.Max(0,Math.Min(SectorCount-1,sector));}
        public static float SectorStart(int sector){return Sector(sector)*SectorSpan;}
        public static int SectorAt(float localX)
        {
            if(float.IsNaN(localX)||float.IsInfinity(localX))return 0;
            if(localX<=0)return 0;
            if(localX>=Width)return SectorCount-1;
            return Sector((int)(localX/SectorSpan));
        }
        public static float EntranceOf(int sector){return SectorStart(sector)+6;}
        public static float ExitOf(int sector){return SectorStart(sector)+SectorSpan-12;}
        public static bool IsJumpSector(int mission,int sector)
        {return sector==1&&(mission==3||mission==6||mission==9||mission==15||mission==18);}
        public static AncientTraversalLayout.Ledge GroundRoad(int sector)
        {float start=SectorStart(sector);return new AncientTraversalLayout.Ledge(start,start+SectorSpan,0);}
        public static AncientTraversalLayout.Ledge EntrancePlatform(int sector)
        {float start=SectorStart(sector);return new AncientTraversalLayout.Ledge(start,start+14,0);}
        public static AncientTraversalLayout.Ledge TerminalPlatform(int sector)
        {float start=SectorStart(sector);return new AncientTraversalLayout.Ledge(start+SectorSpan-28,start+SectorSpan,0);}
        public static AncientTraversalLayout.Ledge Segment(int mission,int sector,int index)
        {
            mission=Math.Max(0,Math.Min(19,mission));int section=Sector(sector),row=(mission+section*7)%20;
            var ledge=AncientFieldLayout.Segment(row,index);
            int slot=Math.Max(0,Math.Min(8,index));float offset=SectorStart(mission,section)+(Blocks(mission)-1)*SectorSpan;
            float variation=ledge.left-(16+slot*11.1f),left=offset+16+slot*18.4f+variation*.35f;
            float width=15+(ledge.right-ledge.left-8)*.8f;
            return new AncientTraversalLayout.Ledge(left,left+width,IsJumpSector(mission,section)?ledge.top+section*.15f:0);
        }
        // Optional grates must leave room for the complete2.1m hero underneath.
        // Use this top together with ExpertLedgeDepth; the ordinary1m platform
        // depth would turn an upper shortcut into a wall across its base landing.
        public static AncientTraversalLayout.Ledge ExpertSegment(int mission,int sector,int index)
        {
            var support=Segment(mission,sector,index);
            return new AncientTraversalLayout.Ledge(support.left+1,support.left+3.2f,Math.Max(6.2f,support.top+2.45f));
        }
        // Checkpoint 1 needs the first section objective; checkpoint 2 needs both earlier objectives.
        // A saved location can move a retry backward, but cannot grant unearned progress.
        public static int MaximumCheckpoint(int tasks)
        {return (tasks&1)==0?0:(tasks&3)==3?2:1;}
        public static int ResolveCheckpoint(int saved,int tasks)
        {return Math.Min(Sector(saved),MaximumCheckpoint(tasks));}
        // Completed objectives never manufacture a defeated encounter. Each sector owns two bits.
        public static bool ObjectiveAvailable(int tasks,int index)
        {
            if(tasks<0||index<0||index>=SectorCount)return false;
            int previous=(1<<index)-1;return (tasks&previous)==previous;
        }
        public static int SanitizeEnemies(int defeated){return defeated<0?0:defeated&EnemyMask;}
        public static int EnemyBit(int sector,int enemy)
        {return sector<0||sector>=SectorCount||enemy<0||enemy>=EnemiesPerSector?0:1<<(sector*EnemiesPerSector+enemy);}
        public static int MarkEnemy(int defeated,int sector,int enemy)
        {return SanitizeEnemies(defeated)|EnemyBit(sector,enemy);}
        public static bool SectorClear(int defeated,int sector)
        {
            if(sector<0||sector>=SectorCount)return false;
            int mask=EnemyBit(sector,0)|EnemyBit(sector,1);return (SanitizeEnemies(defeated)&mask)==mask;
        }
        public static bool AllEnemiesClear(int defeated){return SanitizeEnemies(defeated)==EnemyMask;}
        public static int MaximumCheckpoint(int tasks,int defeated)
        {
            if(tasks<0||(tasks&1)==0||!SectorClear(defeated,0))return 0;
            return (tasks&3)==3&&SectorClear(defeated,1)?2:1;
        }
        public static int ResolveCheckpoint(int saved,int tasks,int defeated)
        {return Math.Min(Sector(saved),MaximumCheckpoint(tasks,defeated));}
        public static bool ExitReady(int tasks,int defeated)
        {return tasks>=0&&(tasks&7)==7&&AllEnemiesClear(defeated);}
        // Different jobs use the same two-enemy budget for different decisions:
        // records are guarded, seals follow a breach, witnesses need an escape lane,
        // and power panels supply projectiles before the receiver's close guard.
        static readonly float[,] EncounterOffsets={
            {88,112,104,128,128,152},
            {72,96,100,124,126,150},
            {88,132,100,146,118,158},
            {106,84,116,94,130,104}
        };
        public static void EncounterPosition(int mission,int kind,int sector,int enemy,out float x,out float y)
        {
            int section=Sector(sector),slot=enemy<=0?0:1;
            if(IsJumpSector(mission,section))
            {
                var landing=Segment(mission,section,slot==0?2:6);
                x=(landing.left+landing.right)*.5f;y=landing.top;return;
            }
            x=SectorStart(mission,section)+(Blocks(mission)-1)*SectorSpan+EncounterOffsets[Math.Max(0,Math.Min(3,kind)),section*2+slot];y=0;
        }
        public static int EncounterType(int floor,int kind,int sector,int enemy)
        {
            int section=Sector(sector),district=Math.Max(0,Math.Min(4,floor));
            if(enemy<=0)return district==2||district>2&&kind==0&&section==2?5:0;
            if(district==3)return kind==3&&section==1?2:4;
            if(district==4&&section==2)return 4;
            return section==2||district==2&&section==1?2:1;
        }
        public static bool NeedsPowerRefill(int kind,int tasks,float energy)
        {return kind==3&&(tasks&3)==3&&(tasks&4)==0&&!(energy>0);}
        // A fast wire move must not abandon a rescued resident. Distant catch-up
        // only crosses cleared ground sectors, and never crosses an unearned gate.
        public static float EscortStep(float witness,float player,float playerSpeed,float deltaTime,int tasks,int defeated,bool danger)
        {return EscortStep(-1,witness,player,playerSpeed,deltaTime,tasks,defeated,danger);}
        public static float EscortStep(int mission,float witness,float player,float playerSpeed,float deltaTime,int tasks,int defeated,bool danger)
        {
            float width=WidthFor(mission);
            if(danger||!Finite(witness)||!Finite(player)||!Finite(playerSpeed)||!Finite(deltaTime)||deltaTime<=0||player<=witness+1.2f)return witness;
            float distance=player-witness,target=Math.Min(width-16,player-1.2f);
            int checkpoint=MaximumCheckpoint(tasks,defeated);
            target=Math.Min(target,checkpoint>=SectorCount-1?width-16:SectorStart(mission,checkpoint+1)-1.4f);
            bool far=distance>=12;
            if(far)
            {
                int section=SectorAt(mission,witness);if(!SectorClear(defeated,section))return witness;
                for(int next=section+1;next<SectorCount;next++)if(!SectorClear(defeated,next))
                {target=Math.Min(target,SectorStart(mission,next)-1.4f);break;}
            }
            if(target<=witness)return witness;
            float speed=far?Math.Min(18,Math.Max(12,Math.Abs(playerSpeed)+3)):Math.Min(12,Math.Max(4.3f,Math.Abs(playerSpeed)+1));
            return Math.Min(target,witness+speed*Math.Min(.25f,deltaTime));
        }
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
        // Global route indices, normalized by /9 and %9 before calling Segment.
        public static int NodeSegment(int index){return Sector(index)*9+5;}
        public static int EnemySegment(int sector,int enemyIndex){return Sector(sector)*9+(enemyIndex<=0?2:6);}
    }
}
