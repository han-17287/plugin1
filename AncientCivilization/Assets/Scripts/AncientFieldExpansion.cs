using System;

namespace Plugin.AncientCivilization
{
    // Three authored traversal sections replace each short investigation interior.
    // Coordinates are local to the full field room; existing city/boss coordinates stay unchanged.
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
            new[]{"암전 접수홀","끊긴 진료선","비상 전력실"},
            new[]{"정지 배달로","운송 대기실","진료소 인계로"},
            new[]{"위조 심사 골목","중개 케이블실","암거래 보관층"},
            new[]{"배급 누락창구","장부 수정실","경비장 문서고"},
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
            int section=Sector(sector),row=(Math.Max(0,Math.Min(19,mission))+section*7)%20;
            var ledge=AncientFieldLayout.Segment(row,index);
            int slot=Math.Max(0,Math.Min(8,index));float offset=SectorStart(section);
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
            x=SectorStart(section)+EncounterOffsets[Math.Max(0,Math.Min(3,kind)),section*2+slot];y=0;
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
        {
            if(danger||!Finite(witness)||!Finite(player)||!Finite(playerSpeed)||!Finite(deltaTime)||deltaTime<=0||player<=witness+1.2f)return witness;
            float distance=player-witness,target=Math.Min(Width-16,player-1.2f);
            int checkpoint=MaximumCheckpoint(tasks,defeated);
            target=Math.Min(target,checkpoint>=SectorCount-1?Width-16:SectorStart(checkpoint+1)-1.4f);
            bool far=distance>=12;
            if(far)
            {
                int section=SectorAt(witness);if(!SectorClear(defeated,section))return witness;
                for(int next=section+1;next<SectorCount;next++)if(!SectorClear(defeated,next))
                {target=Math.Min(target,SectorStart(next)-1.4f);break;}
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
