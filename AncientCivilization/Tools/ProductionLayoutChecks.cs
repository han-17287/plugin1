using System;
using Plugin.AncientCivilization;
class ProductionLayoutChecks
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    struct Point
    {
        public double X,Y;
        public Point(double x,double y){X=x;Y=y;}
    }
    // Solver-free planning oracle. This checks the authored route's capsule envelope,
    // viewport reach and landing room; it is not a substitute for Unity's joint solver.
    // Force values describe the current Hero contract. Native smoke checks exercise the
    // real component; a change to that contract also requires reviewing this plan.
    sealed class RoutePlan
    {
        public Point Position;public double Vx,Vy,Age;
        double cameraX,cameraY=4.6,zoom=5.8;readonly double dt,aspect;
        const double Radius=.36,InnerHalf=.69;
        public RoutePlan(double step,double viewAspect){dt=step;aspect=viewAspect;Position=new Point(-129,1.4);cameraX=Position.X+2.5;}
        void Frame(bool wired)
        {
            Age+=dt;
            double lead=2.5+1.5*Math.Min(1,Math.Abs(Vx)/25);
            double follow=1-Math.Exp(-7*dt),size=1-Math.Exp(-3*dt);
            cameraX+=(Position.X+lead-cameraX)*follow;
            cameraY+=(Math.Max(4.6,Position.Y+1)-cameraY)*follow;
            zoom+=((wired?7.5:5.8)-zoom)*size;
        }
        public void Pick(Point anchor,bool swing)
        {
            double dx=anchor.X-Position.X,dy=anchor.Y-(Position.Y+.35);
            Check(dx*dx+dy*dy<=AncientZipRules.Range*AncientZipRules.Range,"Every planned attachment is within the actual hand-to-target wire reach");
            Check(!swing||dx*dx+dy*dy>=2.2*2.2,"Every planned swing satisfies PickAnchor's minimum distance");
            Check(!swing||anchor.Y>Position.Y+1,"Every planned swing satisfies PickAnchor's above-player condition");
            Check(Math.Abs(anchor.X-cameraX)<zoom*aspect-.15&&Math.Abs(anchor.Y-cameraY)<zoom-.15,"The next planned hook can be clicked inside the current camera viewport");
            for(int sample=0;sample<=40;sample++)
            {
                double t=sample/40.0,x=Position.X+dx*t,y=Position.Y+.35+dy*t;
                foreach(var span in AncientProductionLayout.WireShortcut)
                    Check(!(x>span.Left&&x<span.Right&&y>span.Top-AncientProductionLayout.CatwalkThickness&&y<span.Top),"The hand-to-hook sightline does not pass through a service grating");
            }
        }
        public void Clear()
        {
            Check(Position.Y+1.05<AncientProductionLayout.Ceiling+.003,"The route's full-height capsule stays below the solid roof");
            Check(Position.Y-1.05>-.003,"The optional route does not fall through the safe ground road");
            foreach(var span in AncientProductionLayout.WireShortcut)
            {
                double dx=Math.Max(0,Math.Max(span.Left-Position.X,Position.X-span.Right));
                double dy=Math.Max(0,Math.Max(span.Top-AncientProductionLayout.CatwalkThickness-(Position.Y+InnerHalf),(Position.Y-InnerHalf)-span.Top));
                Check(dx*dx+dy*dy>=(Radius-.003)*(Radius-.003),"The planned capsule clears actual service grating sides and undersides");
            }
        }
        public void Zip(Point anchor)
        {
            Pick(anchor,false);
            double dx=Position.X-anchor.X,dy=Position.Y-anchor.Y,length=Math.Sqrt(dx*dx+dy*dy);
            double support=AncientZipRules.Support((float)dx,(float)dy,false);
            Point goal=new Point(anchor.X+dx/length*support,anchor.Y+dy/length*support);
            for(int frame=0;frame<300;frame++)
            {
                dx=goal.X-Position.X;dy=goal.Y-Position.Y;length=Math.Sqrt(dx*dx+dy*dy);
                if(length<.00001){Vx=Vy=0;return;}
                double travel=Math.Min(length,AncientZipRules.Speed*dt);
                Vx=dx/length*travel/dt;Vy=dy/length*travel/dt;
                Position.X+=Vx*dt;Position.Y+=Vy*dt;Clear();Frame(true);
            }
            throw new Exception("Planned straight zip did not terminate");
        }
        public void Land(AncientProductionLayout.Span ledge)
        {
            Vx=Vy=0;
            for(int frame=0;frame<600;frame++)
            {
                // Falling gravity 38.4 from the first frame is conservative: Hero applies
                // ordinary gravity for the first zero-velocity frame, then its 1.2 multiplier.
                Vx=Math.Min(9,Vx+22*dt);Vy-=38.4*dt;
                Point next=new Point(Position.X+Vx*dt,Position.Y+Vy*dt);
                if(Position.Y-1.05>=ledge.Top&&next.Y-1.05<=ledge.Top)
                {
                    double t=(Position.Y-1.05-ledge.Top)/(Position.Y-next.Y);
                    Position.X+=(next.X-Position.X)*t;Position.Y=ledge.Top+1.05;Vy=0;
                    Check(Position.X>ledge.Left+.36&&Position.X<ledge.Right-.36,"Holding right after the real zip endpoint lands the whole capsule inside the next grating");
                    Clear();Frame(false);return;
                }
                Position=next;Clear();Frame(false);
            }
            throw new Exception("The planned wire arrival missed its service grating");
        }
        public void Swing(Point anchor)
        {
            Pick(anchor,true);
            double dx=anchor.X-Position.X,dy=anchor.Y-Position.Y,length=Math.Sqrt(dx*dx+dy*dy),rx=dx/length,ry=dy/length;
            Vx=Math.Max(13,Vx+rx*11+ry*5);Vy=Math.Max(8,Vy+ry*11-rx*5);
            double rope=length,minimum=Math.Min(length,Math.Max(3.2,Math.Min(8,length*.68)));
            double previous=Math.Atan2(-ry,-rx),arc=0;
            for(int frame=0;frame<Math.Ceiling(1.5/dt);frame++)
            {
                double age=frame*dt;dx=anchor.X-Position.X;dy=anchor.Y-Position.Y;length=Math.Sqrt(dx*dx+dy*dy);rx=dx/length;ry=dy/length;
                rope=Math.Max(minimum,rope-8*dt);Vx+=(rx*10+ry*19)*dt;Vy+=(ry*10-rx*19)*dt;
                double speed=Math.Sqrt(Vx*Vx+Vy*Vy);if(speed>30){Vx*=30/speed;Vy*=30/speed;}
                double angle=Math.Atan2(-ry,-rx),change=angle-previous;
                while(change>Math.PI)change-=2*Math.PI;while(change<-Math.PI)change+=2*Math.PI;
                arc=Math.Max(0,arc+change*180/Math.PI);previous=angle;
                if(age>.32&&arc>66&&Vx>4&&Vy>1||age>=1.35||length<2)
                {
                    Check(age>=.18,"The swing exit reaches the next zip after the real wire click cooldown");
                    Vx=Math.Max(13,Vx);Vy=Math.Max(5,Vy);return;
                }
                Vy-=32*dt;Position.X+=Vx*dt;Position.Y+=Vy*dt;
                dx=Position.X-anchor.X;dy=Position.Y-anchor.Y;length=Math.Sqrt(dx*dx+dy*dy);
                if(length>rope)
                {
                    double ox=dx/length,oy=dy/length;Position.X=anchor.X+ox*rope;Position.Y=anchor.Y+oy*rope;
                    double outward=Vx*ox+Vy*oy;if(outward>0){Vx-=outward*ox;Vy-=outward*oy;}
                }
                Clear();Frame(true);
            }
            throw new Exception("The planned swing never found its next zip window");
        }
    }
    static void CheckUpperRoutePlan()
    {
        Check(AncientProductionLayout.SwingHookX.Length+1==AncientProductionLayout.WireShortcut.Length,"There is one visible low swing hoist between successive high zip sockets");
        foreach(double aspect in new[]{4.0/3,16.0/10,16.0/9})foreach(int rate in new[]{30,60,120,240})
        {
            var plan=new RoutePlan(1.0/rate,aspect);
            for(int i=0;i<AncientProductionLayout.WireHookX.Length;i++)
            {
                plan.Zip(new Point(AncientProductionLayout.WireHookX[i],AncientProductionLayout.WireHookY));
                plan.Land(AncientProductionLayout.WireShortcut[i]);
                if(i<AncientProductionLayout.SwingHookX.Length)plan.Swing(new Point(AncientProductionLayout.SwingHookX[i],AncientProductionLayout.SwingHookY));
            }
            double groundRun=(plan.Position.X+129)/9;
            Check(plan.Age<groundRun*.85,"The optional linked route beats direct ground running by at least fifteen percent without a double jump");
        }
    }
    static void CheckRoadCamera()
    {
        foreach(float height in new[]{480f,720,1080,1440,2160,4320})
        foreach(float aspect in new[]{4/3f,16/10f,16/9f,21/9f})
        foreach(float gap in new[]{8f,14,22})
        {
            float zoom=AncientProductionLayout.RoadCameraZoom(gap,aspect),ppu=height/(2*zoom);
            float pixels=Math.Max(24*Math.Max(.8f,Math.Min(1.4f,height/900)),AncientProductionLayout.BoardingPickPixels(height,zoom));
            Check(.475f*ppu<pixels,"The complete visible boarding latch and hover rim remain clickable from480p through8K without depending on the capped general snap radius");
            Check(2*ppu>pixels,"A separate point two metres away cannot claim the boarding-latch click area");
        }
        Check(AncientProductionLayout.BoardingPickPixels(0,7.8f)==0&&AncientProductionLayout.BoardingPickPixels(720,0)==0,"A missing viewport cannot create an unlimited boarding pick radius");
        foreach(float aspect in new[]{4/3f,16/10f,16/9f,21/9f})
        foreach(int rate in new[]{30,60,120,144,240})
        foreach(float physicsOffset in new[]{-.34f,0,.34f})
        {
            double dt=1.0/rate,hero=0,truck=22+physicsOffset;
            // Production entry and retry hard-cut into this framing, including zoom.
            double camera=AncientProductionLayout.RoadCameraLead((float)(truck-hero));
            double zoom=AncientProductionLayout.RoadCameraZoom((float)(truck-hero),aspect);
            for(int frame=0;frame<=rate*20;frame++)
            {
                double time=frame*dt;
                if(frame>0)
                {
                    hero+=17*dt;
                    double gap=22-14*Math.Min(1,Math.Max(0,(time-16)/3));
                    truck=hero+gap+physicsOffset;
                    double focus=hero+AncientProductionLayout.RoadCameraLead((float)(truck-hero));
                    double targetZoom=AncientProductionLayout.RoadCameraZoom((float)(truck-hero),aspect);
                    camera+=(focus-camera)*(1-Math.Exp(-7*dt));
                    zoom+=(targetZoom-zoom)*(1-Math.Exp(-3*dt));
                }
                double left=camera-zoom*aspect,right=camera+zoom*aspect;
                Check(left<=hero-1.6-.8&&right>=hero+1.6+.8,"The whole motorcycle remains visible from the actual cut through exponential follow and the moving approach");
                Check(left<=truck-3.3-.8&&right>=truck+3.3+.8,"The whole moving truck has a readable margin at every frame, including high FPS follow lag and a20ms physics offset");
                if(time<=16)Check(right>=hero+18+.8+.8,"New road hazards enter inside the visible forward road instead of offscreen");
            }
        }
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            float zoom=AncientProductionLayout.RoadCameraZoom(bad,bad);
            Check(zoom>=7.8f&&!float.IsInfinity(zoom)&&!float.IsNaN(zoom),"A transient invalid viewport or gap cannot poison the production camera");
        }
    }
    static void CheckDeckCamera()
    {
        foreach(float aspect in new[]{4/3f,16/10f,16/9f,21/9f})
        foreach(int rate in new[]{30,60,120,144,240})
        {
            double dt=1.0/rate,hero=0,truck=0,camera=0;
            double zoom=AncientProductionLayout.DeckCameraZoom(0,aspect);
            for(int frame=0;frame<=rate*13;frame++)
            {
                double time=frame*dt;
                if(frame>0)
                {
                    // Leave the roof in both directions at the actual30m/s wire cap,
                    // reverse in midair, then return to ordinary moving-deck control.
                    double speed=time<2?30:time<6?-30:time<8?30:7;
                    hero+=speed*dt;truck+=7*dt;
                    double target=(hero+truck)*.5;
                    double wanted=AncientProductionLayout.DeckCameraZoom((float)(hero-truck),aspect);
                    camera+=(target-camera)*(1-Math.Exp(-7*dt));
                    zoom=wanted>zoom?wanted:zoom+(wanted-zoom)*(1-Math.Exp(-3*dt));
                }
                double left=camera-zoom*aspect,right=camera+zoom*aspect;
                Check(left<=hero-1.6-.8&&right>=hero+1.6+.8,"The airborne officer remains visible during a full-speed roof-to-ceiling wire in either direction");
                Check(left<=truck-4.5-.8&&right>=truck+4.5+.8,"The moving deck and captain's extended poses stay in the same frame as the airborne officer");
            }
        }
    }
    static void Main()
    {
        CheckRoadCamera();CheckDeckCamera();
        Check(AncientProductionLayout.TutorialLeft<AncientProductionLayout.TutorialSpawn-6,"The opening spawn has a safe margin from its retaining wall");
        Check(AncientProductionLayout.StockX>AncientProductionLayout.SectionStarts[4]&&AncientProductionLayout.StockX<AncientProductionLayout.BreachX-6,"The stock console stands in the inventory hall before the breach wall");
        Check(AncientProductionLayout.TutorialRight<=AncientChapterLayout.CaseLeft(0),"The production route leaves the established continuous chapter entrance intact");
        Check((AncientProductionLayout.ChaseStart>AncientFinaleRules.Finish+256||AncientProductionLayout.ChaseEnd+256<AncientFinaleRules.Start),"The introductory chase cannot overlap the final chase");
        Check(AncientProductionLayout.ChaseEnd>AncientProductionLayout.ChaseStart+1200,"The auto-drive lane has a long uninterrupted roadway");
        Check(AncientChapterLayout.FloorAt(AncientProductionLayout.ChaseStart)<0,"The chase lane does not accidentally become a city investigation case");
        float shortHop=AncientMovementRules.LaunchSpeed(AncientJumpKind.Ground,false),shortHeight=shortHop*shortHop/(2*32);
        Check(AncientProductionLayout.JumpCrateHeight+.2f<shortHeight,"A short Space tap can clear the first jump cargo with a useful height margin");
        float aboveCargoTime=2*(float)Math.Sqrt(shortHop*shortHop-2*32*AncientProductionLayout.JumpCrateHeight)/32;
        Check(AncientProductionLayout.JumpCrateWidth+.72f+.2f<9*aboveCargoTime,"A short running hop clears the first cargo with body width and timing margin; walking can safely land on top");
        for(float x=AncientProductionLayout.TutorialLeft;x<=AncientProductionLayout.TutorialRight;x+=.1f)
        {
            float top=AncientProductionLayout.GroundHeight(x);
            Check(AncientProductionLayout.InTutorial(x),"Every authored ground sample stays inside the novice route");
            Check(top>=-1.4f&&top<=.8f,"Maintenance wells have a shallow recovery floor instead of a death pit");
            Check(top+2.1f+3.1f<AncientProductionLayout.Ceiling,"The normal road leaves clearance for a full jump and the player's body");
        }
        Check(AncientProductionLayout.TrainingReceiverX<AncientProductionLayout.TrainingGateX-2,"The laser receiver is reachable on the approach side of its physical shutter");
        Check(AncientProductionLayout.TrainingBeamHits(20,3.1f,37,3.1f,.2f),"A real beam crossing the receiver powers the inspection gate");
        Check(!AncientProductionLayout.TrainingBeamHits(20,6,37,6,.2f),"Firing into empty space does not complete the practical laser lesson");
        Check(!AncientProductionLayout.TrainingBeamHits(20,3.1f,32,3.1f,.2f),"A beam clipped before the receiver cannot power it");
        Check(!AncientProductionLayout.TrainingBeamHits(34,3.1f,34,3.1f,.2f),"A zero-length pulse is not a successful target shot");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            Check(!AncientProductionLayout.TrainingBeamHits(invalid,3.1f,37,3.1f,.2f),"Invalid beam coordinates cannot unlock the tutorial door");
        int recoveryWells=0;
        for(int i=0;i<AncientProductionLayout.TutorialRoad.Length;i++)
        {
            var road=AncientProductionLayout.TutorialRoad[i];
            Check(road.Right>road.Left,"Tutorial collision pieces have positive width");
            if(i>0)Check(Near(AncientProductionLayout.TutorialRoad[i-1].Right,road.Left),"The tutorial has no bottomless gaps between road pieces");
            if(road.Top<0)
            {
                recoveryWells++;
                Check(Near(road.Top,-1.4f)&&road.Right-road.Left>=8,"Each training well has a wide shallow landing");
                Check(-road.Top+.2f<14*14/(2*32f),"Holding jump can leave a training well without an air-jump upgrade");
                Check(road.Top+1.05f>-2.5f,"Missing a training jump never triggers the chapter recovery threshold");
            }
        }
        Check(recoveryWells==3,"The opening teaches three successive crossings before the equipment room");
        foreach(var ramp in AncientProductionLayout.GentleRamps)
        {
            Check(ramp.Right>ramp.Left&&Math.Abs((ramp.High-ramp.Low)/(ramp.Right-ramp.Left))<=.101f,"The novice road ramps stay at a gentle one-in-ten slope");
            Check(Near(ramp.Height(ramp.Left),ramp.Low)&&Near(ramp.Height(ramp.Right),ramp.High),"Every ramp meets its declared roadway heights");
            for(int step=0;step<=40;step++)
            {
                float x=ramp.Left+(ramp.Right-ramp.Left)*step/40;
                Check(Near(AncientProductionLayout.GroundHeight(x),ramp.Height(x)),"The route height agrees with each actual ramp profile");
            }
        }
        for(int i=0;i<AncientProductionLayout.WireShortcut.Length;i++)
        {
            var ledge=AncientProductionLayout.WireShortcut[i];float hook=AncientProductionLayout.WireHookX[i];
            Check(ledge.Right-ledge.Left>=6,"A novice wire landing has generous standing room");
            Check(hook<ledge.Left-.405f&&hook>ledge.Left-.7f,"Each marked hook sits before the landing edge so a straight zip does not aim through the platform");
            Check(ledge.Top-AncientProductionLayout.CatwalkThickness>2.1f+.3f,"Service gratings leave the ground route clear for the full-height player");
            Check(ledge.Top>=3.2f&&ledge.Top+2.1f<AncientProductionLayout.WireHookY,"The optional route is distinct without trapping the player against the hook");
            Check(AncientProductionLayout.GroundHeight(hook)>=0,"Missing an optional wire landing drops onto the safe ground route");
            Check(AncientProductionLayout.WireHookY-1.1f<AncientZipRules.Range,"A beginner can select the hook directly from the street");
            if(i>0)
            {
                float dx=hook-AncientProductionLayout.WireHookX[i-1],dy=AncientProductionLayout.WireHookY-(AncientProductionLayout.WireShortcut[i-1].Top+1.1f);
                Check(dx*dx+dy*dy<AncientZipRules.Range*AncientZipRules.Range,"The next shortcut hook remains inside the real wire range");
            }
        }
        Check(AncientProductionLayout.WireHookY+1.05f+.045f<AncientProductionLayout.Ceiling,"A nearly horizontal hook chain leaves capsule clearance under the real roof");
        CheckUpperRoutePlan();
        // Check the full capsule footprint, not just the centre point of a checkpoint.
        for(int stage=0;stage<=4;stage++)
        {
            float x=AncientProductionLayout.TutorialCheckpointX(stage),y=AncientProductionLayout.TutorialCheckpointY(stage);
            Check(AncientProductionLayout.InTutorial(x),"Each tutorial checkpoint belongs to the authored lower production route");
            for(int sample=0;sample<=12;sample++)
            {
                float footprint=x-.36f+sample*.06f;
                Check(y-1.05f>AncientProductionLayout.GroundHeight(footprint)+.08f,"Checkpoint capsules spawn above the entire local floor profile, including slopes");
            }
            Check(y+1.05f<AncientProductionLayout.Ceiling-.1f,"Checkpoint capsules fit below the solid roof");
        }
        Check(Near(AncientProductionLayout.TutorialCheckpointX(2),-159),"The conveyor checkpoint sits before the access ramp instead of intersecting its rise");
        Check(AncientProductionLayout.TrainingEmitterY>1.05f&&AncientProductionLayout.TrainingEmitterY<2.1f,"The practice housing exits at the character's visible blade height");
        Check(AncientProductionLayout.TrainingSupplyDirection(-70)==1,"Backtracking out of the lesson points east toward a real outlet");
        Check(AncientProductionLayout.TrainingSupplyDirection(103.4f)==-1,"Overshooting the terminal to the sealed wall points west rather than waiting for an impossible shot");
        Check(AncientProductionLayout.TrainingSupplyDirection(-58)==0&&AncientProductionLayout.TrainingSupplyDirection(-58.001f)==1,"The first outlet's inclusive west reach changes guidance at its real supply boundary");
        Check(AncientProductionLayout.TrainingSupplyDirection(102)==0&&AncientProductionLayout.TrainingSupplyDirection(102.001f)==-1,"The final outlet's inclusive east reach changes guidance immediately beyond its supply boundary");
        Check(AncientProductionLayout.TrainingSupplyDirection(AncientProductionLayout.StockX)==0,"Standing at the stock terminal keeps ordinary weapon practice guidance");
        foreach(float emitter in AncientProductionLayout.TrainingEmitterX)
            Check(AncientProductionLayout.TrainingSupplyDirection(emitter)==0,"Standing at an outlet leaves the separate step-back cue in charge");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            Check(AncientProductionLayout.TrainingSupplyDirection(invalid)==0,"Invalid positions cannot produce a misleading supply direction");
        for(float x=-48;x<=AncientProductionLayout.StockX;x+=.25f)
        {
            float nearest=float.MaxValue;
            foreach(float emitter in AncientProductionLayout.TrainingEmitterX)nearest=Math.Min(nearest,Math.Abs(x-emitter));
            Check(nearest<=AncientProductionLayout.TrainingEmitterRange,"The entire training-to-stock approach remains within a visible housing emitter's reach");
        }
        int absent=0;
        for(int rack=0;rack<4;rack++)for(int row=0;row<3;row++)for(int slot=0;slot<3;slot++)
            if(!AncientProductionLayout.StockCorePresent(rack,row,slot))absent++;
        Check(absent==120-117,"Representative shelf art leaves exactly the reported three missing core cradles");
        Check(Near(AncientProductionLayout.GroundHeight(AncientProductionLayout.StockX),0),"The inventory interaction point has a flat unobstructed standing height");
        Check(AncientProductionLayout.TunnelCeiling>1.3f+8+1.05f,"The tunnel roof leaves room for the requested high backflip silhouette");
        for(int section=0;section<AncientProductionLayout.SectionStarts.Length;section++)
        {
            Check(AncientProductionLayout.SectionAt(AncientProductionLayout.SectionStarts[section])==section,"Each authored landmark begins at its own readable threshold");
            Check(!string.IsNullOrEmpty(AncientProductionLayout.SectionNames[section]),"Every production stage has a named place");
        }
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            Check(!AncientProductionLayout.InTutorial(bad)&&!AncientProductionLayout.InChaseLane(bad),"Invalid coordinates never enter a playable route");
        Check(!AncientProductionLayout.InTutorial(AncientProductionLayout.TutorialRight+1)&&!AncientProductionLayout.InTutorial(AncientProductionLayout.TutorialLeft-1),"The map region does not silently claim neighboring investigation space");
        Console.WriteLine("PASS: "+checks+" production tutorial, safe landing, wire reach and chase separation checks");
    }
}
