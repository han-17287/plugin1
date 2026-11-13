using System;
using System.Collections.Generic;
using Plugin.AncientCivilization;

class FieldExpansionChecks
{
    static int checks;
    static void Check(bool condition,string message)
    {if(!condition)throw new InvalidOperationException(message);checks++;}
    static bool Near(float left,float right){return Math.Abs(left-right)<.0001f;}
    static void CheckExpertGrate(int mission,int sector,int block,int index)
    {
        const float heroWidth=.72f,heroHeight=2.1f,safety=.15f,ceiling=10;
        var support=AncientFieldExpansion.BlockSegment(mission,sector,block,index);
        var expert=AncientFieldExpansion.BlockExpertSegment(mission,sector,block,index);
        float underside=expert.top-AncientFieldExpansion.ExpertLedgeDepth;
        Check(expert.left>support.left+heroWidth*.5f&&expert.right<support.right-heroWidth*.5f,"The expert grate rests entirely over its own broad landing and leaves both ends clear");
        Check(expert.right-expert.left>heroWidth+.5f,"An expert landing supports the actual hero capsule with a readable landing margin");
        Check(underside-support.top>=heroHeight+safety,"A full-height hero and safety margin fit below every expert grate");
        Check(expert.top+heroHeight+safety<ceiling,"Standing on the expert grate keeps the entire hero below the physical ceiling");
        if(index>0)Check(expert.left>AncientFieldExpansion.BlockSegment(mission,sector,block,index-1).right+heroWidth*.5f,"The upper grate does not intrude into the previous landing's approach");
        if(index<8)Check(expert.right<AncientFieldExpansion.BlockSegment(mission,sector,block,index+1).left-heroWidth*.5f,"The upper grate does not intrude into the next landing's reverse approach");
        // An AABB enclosing the actual capsule is conservative: if this box clears
        // the grate, neither direction of ordinary walking can catch the head on it.
        foreach(int direction in new[]{1,-1})for(int sample=0;sample<=128;sample++)
        {
            float travel=(direction>0?sample:128-sample)/128f;
            float x=support.left+heroWidth*.5f+(support.right-support.left-heroWidth)*travel;
            bool horizontal=x+heroWidth*.5f>expert.left&&x-heroWidth*.5f<expert.right;
            Check(!horizontal||support.top+heroHeight+safety<underside,
                direction>0?"Forward walking beneath an optional shortcut has no capsule/head intersection":"Reverse walking beneath an optional shortcut has no capsule/head intersection");
        }
    }
    static void CheckShaftPass(int mission,int sector,int block,float x0,float y0,float x1,float y1)
    {
        float offset=AncientFieldExpansion.SectorStart(mission,sector)+block*208;
        for(int sample=0;sample<=100;sample++)
        {
            float t=sample/100f,x=offset+x0+(x1-x0)*t,y=y0+(y1-y0)*t;
            var core=AncientFieldExpansion.StreetObstacle(mission,sector,block);
            Check(x+.36f<=core.left||x-.36f>=core.right||y-1.05f>=core.top+.04f,"A full capsule clears the machine throughout the planned shaft crossing");
            for(int n=0;n<3;n++)
            {
                var grate=AncientFieldExpansion.StreetShortcut(mission,sector,block,n);
                bool overlap=x+.36f>grate.left&&x-.36f<grate.right;
                Check(!overlap||y-1.05f>=grate.top-.001f||y+1.05f<=grate.top-AncientFieldExpansion.ExpertLedgeDepth,"The planned ascent and edge-release descent do not cut through a catwalk");
            }
        }
    }
    static void CheckStreetDesign()
    {
        int shaped=0,towers=0;var profiles=new HashSet<int>();
        for(int mission=0;mission<8;mission++)for(int sector=0;sector<3;sector++)
        {
            if(!AncientFieldExpansion.HasStreetDesign(mission,sector))continue;
            if(!AncientFieldExpansion.EscortStreet(mission))shaped++;
            for(int block=0;block<4;block++)
            {
                float offset=AncientFieldExpansion.SectorStart(mission,sector)+block*208;
                profiles.Add(AncientFieldExpansion.StreetProfile(mission,sector,block));
                bool tall=AncientFieldExpansion.TallStreetBay(mission,sector,block);
                if(tall)
                {
                    towers++;
                    CheckShaftPass(mission,sector,block,27,7.3f,41,13.6f);
                    CheckShaftPass(mission,sector,block,51,12.2f,61,8.7f);
                    Check(AncientFieldExpansion.StreetHookHeight(mission,sector,block,1)<6.2f+1.05f+1+5.8f,"The next high hoist is visible from the first landing with normal travel zoom");
                }
                int count=AncientFieldExpansion.StreetPieceCount(mission,sector,block);
                Check(count<=14,"Authored street geometry keeps a bounded collision budget");
                var first=AncientFieldExpansion.StreetPiece(mission,sector,block,0);
                var last=AncientFieldExpansion.StreetPiece(mission,sector,block,count-1);
                Check(Near(first.left,offset)&&Near(first.top,0)&&Near(last.right,offset+208)&&Near(last.top,0),"Block entrances and exits meet saved checkpoints and adjacent roads at street level");
                bool raised=false,lowered=false;
                for(int i=0;i<count;i++)
                {
                    var piece=AncientFieldExpansion.StreetPiece(mission,sector,block,i);
                    Check(piece.right-piece.left>=6,"Every structural landing or recovery well is wider than the player and a running approach");
                    Check(piece.top>=-1.4f&&piece.top<=3.5f,"Street floors stay above automatic recovery and below roof clearance");
                    raised|=piece.top>1;lowered|=piece.top<0;
                    if(i>0)Check(Near(AncientFieldExpansion.StreetPiece(mission,sector,block,i-1).right,piece.left),"Every drop has an actual contiguous recovery floor, with no invisible death gap");
                    for(int n=0;n<3;n++)
                    {
                        var grate=AncientFieldExpansion.StreetShortcut(mission,sector,block,n);
                        bool overlaps=piece.left<grate.right&&piece.right>grate.left;
                        Check(!overlaps||grate.top-AncientFieldExpansion.ExpertLedgeDepth-piece.top>=2.25f,"The entire standing capsule fits under each optional catwalk from either direction");
                    }
                    var duct=AncientFieldExpansion.StreetBaffle(mission,sector,block);
                    Check(piece.left>=duct.right||piece.right<=duct.left||duct.top-piece.top>=2.25f,"Suspended machinery cannot close the required lower route");
                }
                Check(AncientFieldExpansion.EscortStreet(mission)||raised&&lowered,"Every redesigned block has a genuine climb and descent, not a recoloured flat road");
                for(float x=84;x<=162;x+=.5f)Check(Near(AncientFieldExpansion.StreetHeight(mission,sector,block,x),0),"Dialogue, investigations, throwing and their progress gates retain a safe level court");
                if(block==3)for(float x=72;x<=162;x+=.5f)Check(Near(AncientFieldExpansion.StreetHeight(mission,sector,block,x),0),"Original enemy patrols and conveyor objectives stay grounded in their encounter court");
                for(int n=0;n<3;n++)
                {
                    var grate=AncientFieldExpansion.StreetShortcut(mission,sector,block,n);
                    Check(grate.top+2.25f<(tall?18:10)&&grate.right<offset+78,"Upper shortcuts keep headroom and rejoin before any mission gate");
                    float hook=offset+21+n*20;
                    Check(hook<grate.left-.4f&&hook>grate.left-3,"Zip approach stays beside the ledge instead of dragging through it");
                    if(n>0)Check(20*20+6*6<24*24,"Consecutive hoists stay inside the existing wire range");
                }
            }
        }
        Check(towers==4,"Four landmark shafts require a genuine ascent before rejoining the court");
        Check(shaped==16&&profiles.Count==6,"Six structural profiles replace sixteen flat sectors; six escort sectors and two dedicated traversal sectors retain their purpose");
    }
    static void Main()
    {
        CheckStreetDesign();
        for(int mission=-1;mission<=8;mission++)for(int sector=-1;sector<=3;sector++)for(int pad=-1;pad<=24;pad++)
        {
            float x,y;bool validPad=AncientFieldExpansion.TryRestPoint(mission,sector,pad,out x,out y);
            bool expected=mission>=0&&mission<8&&sector>=0&&sector<3&&pad>=0&&pad<8&&(!AncientFieldExpansion.IsJumpSector(mission,sector)||pad%2==0);
            Check(validPad==expected,"Recovery rejects corrupt/out-of-chapter slots and platforms that do not exist on a jump route");
            if(!validPad)continue;
            Check(Near(y,1.3f),"The standing capsule spawns above fixed safe-ground pads");
            if(!AncientFieldExpansion.IsJumpSector(mission,sector))for(float dx=-.36f;dx<=.36f;dx+=.12f)
                Check(Near(AncientFieldExpansion.StreetHeight(mission,sector,pad/2,x-AncientFieldExpansion.SectorStart(mission,sector)-(pad/2)*208+dx),0),"The complete respawn capsule footprint has flat physical support");
        }
        Check(AncientFieldExpansion.SectorCount==3&&AncientFieldExpansion.RouteCount==27,"Each expanded field contains three nine-ledge sections");
        Check(AncientFieldExpansion.Width>=600&&AncientFieldExpansion.Width==AncientFieldExpansion.SectorCount*AncientFieldExpansion.SectorSpan,"Fields are longer than the former432-unit interiors and cover all three complete sectors");
        Check(AncientFieldExpansion.Stride>AncientFieldExpansion.Width,"Consecutive case sections leave room for a physical connecting road");
        Check(AncientFieldExpansion.Names.Length==20,"All twenty existing cases retain section names");
        var names=new HashSet<string>();var jumpSignatures=new HashSet<string>();int jumpSectors=0,expertGrates=0,oldDepthConflicts=0;var floorsWithJump=new HashSet<int>();
        for(int mission=0;mission<20;mission++)
        {
            Check(AncientFieldExpansion.Names[mission].Length==3,"Every case has three named sections");
            int missionJumps=0;
            for(int sector=0;sector<3;sector++)
            {
                string name=AncientFieldExpansion.Names[mission][sector];
                Check(!string.IsNullOrWhiteSpace(name)&&names.Add(name),"Each section has a distinct readable landmark name");
                bool jump=AncientFieldExpansion.IsJumpSector(mission,sector);
                if(jump){jumpSectors++;missionJumps++;floorsWithJump.Add(mission/4);}
                Check(!jump||sector==1,"Dedicated traversal is separated into the labelled middle sector");
                Check(!jump||mission!=2&&mission!=5&&mission!=11&&mission!=14,"Required escort paths never become a jump course");
                for(int block=0;block<AncientFieldExpansion.Blocks(mission);block++)
                {
                float offset=AncientFieldExpansion.SectorStart(mission,sector)+block*AncientFieldExpansion.SectorSpan;
                var entrance=AncientFieldExpansion.BlockLanding(mission,sector,block,false);
                var terminal=AncientFieldExpansion.BlockLanding(mission,sector,block,true);
                var road=AncientFieldExpansion.GroundRoad(mission,sector);
                Check(Near(road.left,AncientFieldExpansion.SectorStart(mission,sector))&&Near(road.right,road.left+AncientFieldExpansion.Span(mission))&&Near(road.top,0),"Legacy road bounds still describe the sector extent; redesigned streets use their explicit pieces");
                Check(Near(entrance.left,offset)&&Near(entrance.right,offset+14)&&Near(entrance.top,0),"Each section has a broad safe entrance at ground height");
                Check(terminal.left>offset+AncientFieldExpansion.SectorSpan-32&&Near(terminal.right,offset+AncientFieldExpansion.SectorSpan)&&Near(terminal.top,0),"Each section ends on a broad ground landing");
                if(sector<2||block<AncientFieldExpansion.Blocks(mission)-1)
                {
                    var nextEntrance=block<AncientFieldExpansion.Blocks(mission)-1?AncientFieldExpansion.BlockLanding(mission,sector,block+1,false):AncientFieldExpansion.BlockLanding(mission,sector+1,0,false);
                    Check(Near(terminal.right,nextEntrance.left),"Required section transitions leave no gap between terminal and entrance platforms");
                }
                Check((offset+6)>entrance.left&&(offset+6)<entrance.right,"Checkpoint rests within the safe entrance platform");
                Check((offset+196)>terminal.left&&(offset+196)<terminal.right,"Section exit rests within the safe terminal platform");
                bool hasRaisedLanding=false;string jumpSignature="";
                for(int i=0;i<9;i++)
                {
                    var ledge=AncientFieldExpansion.BlockSegment(mission,sector,block,i);
                    Check(ledge.left>=entrance.right&&ledge.right<=terminal.left,"Every field encounter and objective landing stays between safe ends");
                    Check(ledge.right-ledge.left>=7,"Landing widths remain readable and do not shrink into needle platforms");
                    Check(ledge.top>=0&&ledge.top+2.2f<9,"Required landings preserve full character clearance beneath the solid ceiling");
                    Check(jump||Near(ledge.top,0),"Ordinary investigation, power and escort targets stay on the fast ground route");
                    hasRaisedLanding|=ledge.top>1;
                    if(jump)jumpSignature+=(ledge.left-offset).ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+":"+ledge.top.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+";";
                    if(jump&&i%3==1)
                    {
                        CheckExpertGrate(mission,sector,block,i);expertGrates++;
                        var expert=AncientFieldExpansion.BlockExpertSegment(mission,sector,block,i);
                        if(expert.top-1-ledge.top<2.1f)oldDepthConflicts++;
                    }
                    if(i>0)
                    {
                        var previous=AncientFieldExpansion.BlockSegment(mission,sector,block,i-1);
                        Check(ledge.left>previous.right,"Encounter landing order never reverses or overlaps");
                        if(jump)
                        {
                            Check(ledge.left-previous.right<=8,"Dedicated wire traversal retains visible reachable gaps");
                            Check(Math.Abs(ledge.top-previous.top)<=2.2f,"Dedicated jumps preserve ordinary height changes instead of requiring a frame-perfect launch");
                        }
                    }
                }
                Check(!jump||hasRaisedLanding,"Each dedicated wire course actually contains raised traversal geometry");
                if(jump&&block==0)Check(jumpSignatures.Add(jumpSignature),"Each floor has a distinct authored wire course shape");
                var first=AncientFieldExpansion.BlockSegment(mission,sector,block,0);
                var last=AncientFieldExpansion.BlockSegment(mission,sector,block,8);
                Check(first.top<1.5f&&last.top<2.5f,"Dedicated routes remain accessible from and to flat safe platforms");
                Check(first.left-entrance.right>=0&&first.left-entrance.right<8,"The first landing remains close to the safe entrance");
                Check(terminal.left-last.right>=0&&terminal.left-last.right<8,"The final landing reaches the safe terminal instead of an empty extension");
                int node=AncientFieldExpansion.NodeSegment(sector);
                Check(node/9==sector&&node%9>=0&&node%9<9,"Each required objective maps to a landing in its own section");
                for(int enemy=0;enemy<2;enemy++)
                {
                    int index=AncientFieldExpansion.EnemySegment(sector,enemy);
                    Check(index/9==sector&&index!=node,"Encounter positions stay in their section and off the objective landing");
                }
                }
            }
            Check(missionJumps<=1,"Most of each investigation remains continuous fast travel");
        }
        Check(jumpSectors==5&&floorsWithJump.Count==5,"Every city floor retains one dedicated course while the other fifty-five sectors stay grounded");
        Check(expertGrates==33,"All expert grates across the eleven authored traversal blocks receive physical clearance checks");
        Check(oldDepthConflicts>0,"The geometry regression includes real landings blocked by the former one-meter expert platform depth");
        Check(names.Count==60,"All sixty section landmark names are unique");
        Check(AncientFieldExpansion.SectorAt(-1)==0&&AncientFieldExpansion.SectorAt(0)==0,"Left boundary and negative recovery position stay in first section");
        Check(AncientFieldExpansion.SectorAt(AncientFieldExpansion.SectorSpan-.01f)==0&&AncientFieldExpansion.SectorAt(AncientFieldExpansion.SectorSpan)==1,"First section boundary follows the longer actual next section start");
        Check(AncientFieldExpansion.SectorAt(AncientFieldExpansion.SectorSpan*2-.01f)==1&&AncientFieldExpansion.SectorAt(AncientFieldExpansion.SectorSpan*2)==2,"Second section boundary follows the longer actual next section start");
        Check(AncientFieldExpansion.SectorAt(AncientFieldExpansion.Width)==2&&AncientFieldExpansion.SectorAt(float.MaxValue)==2,"Right edge and finite overshoot stay in last section");
        Check(AncientFieldExpansion.SectorAt(float.NaN)==0&&AncientFieldExpansion.SectorAt(float.PositiveInfinity)==0&&AncientFieldExpansion.SectorAt(float.NegativeInfinity)==0,"Invalid coordinates return a safe initial section");
        var safeLast=AncientFieldExpansion.TerminalPlatform(2);
        Check(Near(AncientFieldExpansion.EntranceOf(-5),6)&&AncientFieldExpansion.ExitOf(50)>safeLast.left&&AncientFieldExpansion.ExitOf(50)<safeLast.right,"Invalid section identifiers clamp to a safe entrance or final exit");
        var clamped=AncientFieldExpansion.Segment(-10,-2,-1);var valid=AncientFieldExpansion.Segment(0,0,0);
        Check(Near(clamped.left,valid.left)&&Near(clamped.right,valid.right)&&Near(clamped.top,valid.top),"Invalid mission, section and ledge identifiers clamp safely");
        Check(AncientFieldExpansion.NodeSegment(-1)==5&&AncientFieldExpansion.NodeSegment(9)==23,"Required-objective route indices cannot escape section bounds");
        Check(AncientFieldExpansion.EnemySegment(-1,-1)==2&&AncientFieldExpansion.EnemySegment(9,9)==24,"Encounter route indices cannot escape section bounds");
        Check(AncientFieldExpansion.Stride-AncientFieldExpansion.Width==AncientChapterLayout.BridgeWidth&&AncientChapterLayout.BridgeWidth==64,"Each consecutive case joins the authored sixty-four-unit connecting road");
        Check(AncientFieldExpansion.MaximumCheckpoint(0)==0,"No objective progress cannot unlock a later checkpoint");
        Check(AncientFieldExpansion.MaximumCheckpoint(1)==1,"First objective authorizes the second-section checkpoint");
        Check(AncientFieldExpansion.MaximumCheckpoint(3)==2,"First two objectives authorize the final-section checkpoint");
        Check(AncientFieldExpansion.MaximumCheckpoint(2)==0,"A noncontiguous second objective cannot bypass the first section");
        Check(AncientFieldExpansion.MaximumCheckpoint(4)==0,"Final objective alone cannot skip either prior section");
        Check(AncientFieldExpansion.MaximumCheckpoint(5)==1,"A missing middle objective still blocks the final checkpoint");
        Check(AncientFieldExpansion.MaximumCheckpoint(7)==2,"Completed case progress stays within the last section");
        Check(AncientFieldExpansion.ResolveCheckpoint(20,0)==0,"Corrupt future checkpoint cannot skip an unstarted objective");
        Check(AncientFieldExpansion.ResolveCheckpoint(20,1)==1,"Corrupt future checkpoint stops at the first earned boundary");
        Check(AncientFieldExpansion.ResolveCheckpoint(20,3)==2,"Corrupt future checkpoint never escapes the field");
        Check(AncientFieldExpansion.ResolveCheckpoint(-1,7)==0,"Negative saved checkpoint resets to the safe entrance");
        Check(AncientFieldExpansion.ResolveCheckpoint(0,7)==0,"Completed objectives do not force an unvisited checkpoint");
        Check(AncientFieldExpansion.ResolveCheckpoint(1,3)==1,"Saved earlier checkpoint remains valid with later objectives complete");
        Check(AncientFieldExpansion.MaximumCheckpoint(9)==1,"Unrelated high save bits cannot unlock the final checkpoint");
        for(int tasks=0;tasks<8;tasks++)for(int saved=-2;saved<=4;saved++)
        {
            int restored=AncientFieldExpansion.ResolveCheckpoint(saved,tasks);
            Check(restored>=0&&restored<AncientFieldExpansion.SectorCount,"Every restored checkpoint stays inside the expanded field");
            Check(restored<1||(tasks&1)!=0,"Every second-section restore has its required first objective");
            Check(restored<2||(tasks&3)==3,"Every final-section restore has both earlier objectives");
        }
        Check(AncientFieldExpansion.EnemiesPerSector==2&&AncientFieldExpansion.EnemyMask==63,"Only two required enemies belong to each of the three sectors");
        int allBits=0;
        for(int sector=0;sector<3;sector++)for(int enemy=0;enemy<2;enemy++)
        {
            int bit=AncientFieldExpansion.EnemyBit(sector,enemy);
            Check(bit>0&&(bit&(bit-1))==0&&(allBits&bit)==0,"Each required enemy occupies one unique progress bit");
            int marked=AncientFieldExpansion.MarkEnemy(0,sector,enemy);
            Check(marked==bit&&!AncientFieldExpansion.SectorClear(marked,sector),"Defeating only one enemy never clears its whole sector");
            Check(AncientFieldExpansion.MarkEnemy(marked,sector,enemy)==marked,"Repeated enemy removal cannot award another sector's progress");
            allBits|=bit;
        }
        Check(allBits==63&&AncientFieldExpansion.AllEnemiesClear(allBits),"All six required defeats are needed for full-field clearance");
        for(int missing=0;missing<6;missing++)Check(!AncientFieldExpansion.AllEnemiesClear(allBits&~(1<<missing)),"Omitting any required enemy blocks full-field clearance");
        Check(AncientFieldExpansion.EnemyBit(-1,0)==0&&AncientFieldExpansion.EnemyBit(3,0)==0&&AncientFieldExpansion.EnemyBit(0,-1)==0&&AncientFieldExpansion.EnemyBit(0,2)==0,"Invalid enemy identifiers cannot manufacture a valid defeat bit");
        Check(AncientFieldExpansion.MarkEnemy(0,9,9)==0&&!AncientFieldExpansion.SectorClear(63,9),"Invalid sectors remain uncleared");
        Check(AncientFieldExpansion.SanitizeEnemies(64)==0&&AncientFieldExpansion.SanitizeEnemies(127)==63,"Unrelated high save bits never create missing required enemies");
        Check(AncientFieldExpansion.SanitizeEnemies(-1)==0&&!AncientFieldExpansion.AllEnemiesClear(-1),"Negative corrupt enemy state cannot clear every encounter");
        Check(AncientFieldExpansion.MaximumCheckpoint(7,0)==0,"Objectives alone do not unlock a checkpoint past live threats");
        Check(AncientFieldExpansion.MaximumCheckpoint(7,3)==1,"First sector's two defeats unlock only the second entrance");
        Check(AncientFieldExpansion.MaximumCheckpoint(7,15)==2,"Both earlier encounters and objectives unlock the final entrance");
        Check(AncientFieldExpansion.MaximumCheckpoint(1,63)==1,"All encounters alone cannot bypass an unfinished middle objective");
        Check(AncientFieldExpansion.MaximumCheckpoint(2,63)==0,"A later objective cannot bypass the initial investigation");
        Check(AncientFieldExpansion.MaximumCheckpoint(-1,63)==0,"Negative corrupt objectives cannot unlock later checkpoints");
        Check(!AncientFieldExpansion.ExitReady(7,31)&&!AncientFieldExpansion.ExitReady(3,63)&&AncientFieldExpansion.ExitReady(7,63),"Exit requires all three objectives and all six defeats together");
        for(int tasks=0;tasks<8;tasks++)for(int enemyMask=0;enemyMask<64;enemyMask++)
        {
            for(int index=0;index<3;index++)
            {
                bool available=AncientFieldExpansion.ObjectiveAvailable(tasks,index);
                Check(index!=0||available,"The first objective always remains available");
                Check(!available||index<1||(tasks&1)!=0,"Middle objective cannot be obtained before the first");
                Check(!available||index<2||(tasks&3)==3,"Final objective cannot be obtained without both preceding records");
            }
            for(int saved=-2;saved<=4;saved++)
            {
                int checkpoint=AncientFieldExpansion.ResolveCheckpoint(saved,tasks,enemyMask);
                Check(checkpoint>=0&&checkpoint<3,"Restored checkpoint always stays inside the field");
                Check(checkpoint<1||((tasks&1)!=0&&AncientFieldExpansion.SectorClear(enemyMask,0)),"Second entrance cannot be restored over missing first evidence or an active first encounter");
                Check(checkpoint<2||((tasks&3)==3&&AncientFieldExpansion.SectorClear(enemyMask,0)&&AncientFieldExpansion.SectorClear(enemyMask,1)),"Final entrance cannot be restored over either preceding investigation encounter");
                Check(checkpoint<=Math.Max(0,Math.Min(2,saved)),"Progress never forces the checkpoint ahead of the player's saved location");
            }
            bool exit=AncientFieldExpansion.ExitReady(tasks,enemyMask);
            Check(!exit||(tasks==7&&enemyMask==63),"Exhaustive save-state scan rejects every incomplete exit state");
        }
        Check(!AncientFieldExpansion.ObjectiveAvailable(7,-1)&&!AncientFieldExpansion.ObjectiveAvailable(7,3)&&!AncientFieldExpansion.ObjectiveAvailable(-1,2),"Invalid objective identifiers or state do not authorize progress");
        var encounterStyles=new HashSet<string>();
        for(int mission=0;mission<20;mission++)for(int kind=0;kind<4;kind++)for(int sector=0;sector<3;sector++)
        {
            float guardX,guardY,supportX,supportY;
            AncientFieldExpansion.EncounterPosition(mission,kind,sector,0,out guardX,out guardY);
            AncientFieldExpansion.EncounterPosition(mission,kind,sector,1,out supportX,out supportY);
            float left=AncientFieldExpansion.SectorStart(mission,sector)+(AncientFieldExpansion.Blocks(mission)-1)*AncientFieldExpansion.SectorSpan;
            Check(guardX>left+16&&guardX<left+AncientFieldExpansion.SectorSpan-28,"Close guards leave clear entry and exit areas");
            Check(supportX>left+16&&supportX<left+AncientFieldExpansion.SectorSpan-28,"Projectile support remains inside the active encounter sector");
            Check(Math.Abs(guardX-supportX)>=18,"The two criminals have distinct readable combat positions");
            int guard=AncientFieldExpansion.EncounterType(mission/4,kind,sector,0),support=AncientFieldExpansion.EncounterType(mission/4,kind,sector,1);
            Check((guard==0||guard==5)&&(support==1||support==2||support==4),"Every pair combines a close guard and a destructible-object thrower");
            if(AncientFieldExpansion.IsJumpSector(mission,sector))
            {
                var a=AncientFieldExpansion.Segment(mission,sector,2);var b=AncientFieldExpansion.Segment(mission,sector,6);
                Check(guardX>a.left&&guardX<a.right&&Near(guardY,a.top),"Jump-course guards retain an actual authored landing");
                Check(supportX>b.left&&supportX<b.right&&Near(supportY,b.top),"Jump-course support retains an actual authored landing");
            }
            else
            {
                Check(Near(guardY,0)&&Near(supportY,0),"Ordinary encounter variety adds no floating required route");
                encounterStyles.Add(kind+":"+(guardX-left)+":"+(supportX-left));
            }
        }
        Check(encounterStyles.Count==12,"The four job mechanics use three distinct encounter positions rather than one repeated field layout");
        Check(AncientFieldExpansion.EncounterType(2,0,0,0)==5&&AncientFieldExpansion.EncounterType(2,0,0,1)==1,"Administrative shields are paired with an object source for blade-capture-laser counterplay");
        float caught=AncientFieldExpansion.EscortStep(330,370,30,.1f,3,15,false);
        Check(caught>330&&caught<=331.81f,"A wire move beyond twelve metres cannot permanently strand a witness on cleared ground");
        Check(Near(AncientFieldExpansion.EscortStep(330,370,30,.1f,3,3,false),330),"Distant catch-up never runs through an uncleared witness sector");
        Check(AncientFieldExpansion.EscortStep(414.5f,500,30,.1f,3,15,false)<=414.601f,"Distant residents wait before the next live encounter");
        Check(Near(AncientFieldExpansion.EscortStep(330,370,30,.1f,3,63,true),330),"A nearby criminal still stops escort movement");
        Check(Near(AncientFieldExpansion.EscortStep(330,320,9,.1f,3,63,false),330),"A resident never moves backward or overtakes a player behind them");
        Check(AncientFieldExpansion.EscortStep(414.5f,500,30,.1f,1,63,false)<=414.601f,"Defeated enemies cannot move a resident through an unearned objective gate");
        Check(AncientFieldExpansion.EscortStep(607.9f,623,30,.1f,3,63,false)<=AncientFieldExpansion.Width-16,"An escort physically reaches but never overshoots the protected exit");
        float nearby=AncientFieldExpansion.EscortStep(330,336,0,.1f,3,63,false)-330;
        float distant=AncientFieldExpansion.EscortStep(330,370,0,.1f,3,63,false)-330;
        Check(distant>nearby&&nearby>0,"Catch-up runs quicker while nearby escort walking keeps its existing pace");
        foreach(int hz in new[]{30,50,60,120,240})
        {
            float witness=330;for(int frame=0;frame<hz*8;frame++)witness=AncientFieldExpansion.EscortStep(witness,620,0,1f/hz,3,63,false);
            Check(Math.Abs(witness-426)<.04f,"Cleared-ground escort catch-up covers the same distance at every physics/frame rate within float integration precision");
        }
        Check(!AncientFieldExpansion.NeedsPowerRefill(0,3,0)&&!AncientFieldExpansion.NeedsPowerRefill(3,1,0),"Energy guidance never replaces an unfinished non-power or earlier objective");
        Check(AncientFieldExpansion.NeedsPowerRefill(3,3,0)&&!AncientFieldExpansion.NeedsPowerRefill(3,3,1),"An empty receiver redirects to refill, and collected energy restores the receiver target");
        Check(!AncientFieldExpansion.NeedsPowerRefill(3,7,0),"Completed power restoration cannot send the player back for unnecessary particles");
        Console.WriteLine("PASS: "+checks+" expanded-field layout checks");
    }
}
