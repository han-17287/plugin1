using System;
using System.Collections.Generic;
using Plugin.AncientCivilization;

class FieldExpansionChecks
{
    static int checks;
    static void Check(bool condition,string message)
    {if(!condition)throw new InvalidOperationException(message);checks++;}
    static bool Near(float left,float right){return Math.Abs(left-right)<.0001f;}
    static void Main()
    {
        Check(AncientFieldExpansion.SectorCount==3&&AncientFieldExpansion.RouteCount==27,"Each expanded field contains three nine-ledge sections");
        Check(AncientFieldExpansion.Width>=600&&AncientFieldExpansion.Width==AncientFieldExpansion.SectorCount*AncientFieldExpansion.SectorSpan,"Fields are longer than the former432-unit interiors and cover all three complete sectors");
        Check(AncientFieldExpansion.Stride>AncientFieldExpansion.Width,"Full field boundaries leave a gap before the next field");
        Check(AncientFieldExpansion.Names.Length==20,"All twenty existing cases retain section names");
        var names=new HashSet<string>();var jumpSignatures=new HashSet<string>();int jumpSectors=0;var floorsWithJump=new HashSet<int>();
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
                float offset=AncientFieldExpansion.SectorStart(sector);
                var entrance=AncientFieldExpansion.EntrancePlatform(sector);
                var terminal=AncientFieldExpansion.TerminalPlatform(sector);
                var road=AncientFieldExpansion.GroundRoad(sector);
                Check(Near(road.left,offset)&&Near(road.right,offset+AncientFieldExpansion.SectorSpan)&&Near(road.top,0),"Ordinary sector road geometry covers its entire span without a hidden jump gap");
                Check(Near(entrance.left,offset)&&Near(entrance.right,offset+14)&&Near(entrance.top,0),"Each section has a broad safe entrance at ground height");
                Check(terminal.left>offset+AncientFieldExpansion.SectorSpan-32&&Near(terminal.right,offset+AncientFieldExpansion.SectorSpan)&&Near(terminal.top,0),"Each section ends on a broad ground landing");
                if(sector<2)
                {
                    var nextEntrance=AncientFieldExpansion.EntrancePlatform(sector+1);
                    Check(Near(terminal.right,nextEntrance.left),"Required section transitions leave no gap between terminal and entrance platforms");
                }
                Check(AncientFieldExpansion.EntranceOf(sector)>entrance.left&&AncientFieldExpansion.EntranceOf(sector)<entrance.right,"Checkpoint rests within the safe entrance platform");
                Check(AncientFieldExpansion.ExitOf(sector)>terminal.left&&AncientFieldExpansion.ExitOf(sector)<terminal.right,"Section exit rests within the safe terminal platform");
                bool hasRaisedLanding=false;string jumpSignature="";
                for(int i=0;i<9;i++)
                {
                    var ledge=AncientFieldExpansion.Segment(mission,sector,i);
                    Check(ledge.left>=entrance.right&&ledge.right<=terminal.left,"Every field encounter and objective landing stays between safe ends");
                    Check(ledge.right-ledge.left>=7,"Landing widths remain readable and do not shrink into needle platforms");
                    Check(ledge.top>=0&&ledge.top+2.2f<9,"Required landings preserve full character clearance beneath the solid ceiling");
                    Check(jump||Near(ledge.top,0),"Ordinary investigation, power and escort targets stay on the fast ground route");
                    hasRaisedLanding|=ledge.top>1;
                    if(jump)jumpSignature+=(ledge.left-offset).ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+":"+ledge.top.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+";";
                    if(i>0)
                    {
                        var previous=AncientFieldExpansion.Segment(mission,sector,i-1);
                        Check(ledge.left>previous.right,"Encounter landing order never reverses or overlaps");
                        if(jump)
                        {
                            Check(ledge.left-previous.right<=8,"Dedicated wire traversal retains visible reachable gaps");
                            Check(Math.Abs(ledge.top-previous.top)<=2.2f,"Dedicated jumps preserve ordinary height changes instead of requiring a frame-perfect launch");
                        }
                    }
                }
                Check(!jump||hasRaisedLanding,"Each dedicated wire course actually contains raised traversal geometry");
                if(jump)Check(jumpSignatures.Add(jumpSignature),"Each floor has a distinct authored wire course shape");
                var first=AncientFieldExpansion.Segment(mission,sector,0);
                var last=AncientFieldExpansion.Segment(mission,sector,8);
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
            Check(missionJumps<=1,"Most of each investigation remains continuous fast travel");
        }
        Check(jumpSectors==5&&floorsWithJump.Count==5,"Every city floor retains one dedicated course while the other fifty-five sectors stay grounded");
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
        Check(AncientFieldExpansion.Stride-AncientFieldExpansion.Width==32,"Expanded fields retain a full thirty-two-unit inter-room void");
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
        Console.WriteLine("PASS: "+checks+" expanded-field layout checks");
    }
}
