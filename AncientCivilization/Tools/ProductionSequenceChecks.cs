using System;
using Plugin.AncientCivilization;

class ProductionSequenceChecks
{
    static int checks;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    static float Until(AncientProductionSequence sequence,AncientProductionPhase next,int fps,float limit)
    {
        int frames=0;while(sequence.Phase!=next&&frames++<Math.Ceiling(limit*fps))sequence.Tick(1f/fps);
        Check(sequence.Phase==next,"The intended next scene is reachable without input timing tricks: "+next);
        return frames/(float)fps;
    }
    static AncientProductionSequence AtDeck(int round=0)
    {
        var sequence=new AncientProductionSequence();sequence.RestoreRound(round);
        Until(sequence,AncientProductionPhase.Approach,60,18);
        Check(sequence.BeginBoarding()&&sequence.LandOnDeck(),"A granted wire window permits an explicit attach and physical landing");return sequence;
    }
    static void PauseWithoutChanging(AncientProductionSequence sequence)
    {
        var phase=sequence.Phase;float elapsed=sequence.Elapsed,health=sequence.HealthRatio;
        int round=sequence.Round,mounts=sequence.MountCount;
        foreach(float seconds in new[]{1/144f,.2f,1f,float.MaxValue})sequence.Tick(seconds,true);
        Check(sequence.Phase==phase&&sequence.Elapsed==elapsed&&sequence.HealthRatio==health&&sequence.Round==round&&sequence.MountCount==mounts,
            "Pause preserves the full encounter state during "+phase);
    }
    // Replay the requested two complete rides, variable-length deck fights, a missed
    // wire and a fall. This catches interactions that isolated gate checks cannot.
    static void Replay(float[] steps,int seed)
    {
        var random=new Random(seed);var sequence=new AncientProductionSequence();sequence.Begin();
        int frames=0,deckVisits=0,fallRound=-1,lastMount=1;float acceptedDamage=0;
        bool missedWire=false,deckFall=false;var visited=new bool[10];
        while(!sequence.Completed&&frames<50000)
        {
            float dt=steps[frames%steps.Length];frames++;visited[(int)sequence.Phase]=true;
            if(frames%47==0)PauseWithoutChanging(sequence);
            if(sequence.Phase==AncientProductionPhase.Approach)
            {
                Check(sequence.ApplyDamage(.5f)==0,"A weapon held during a safe approach cannot damage the captain");
                Check(sequence.BeginBoarding(),"Every safe approach admits another deliberate wire attempt");
                if(!missedWire){missedWire=true;Check(sequence.AbortBoarding(),"A missed first wire restores its safe approach");}
            }
            else if(sequence.Phase==AncientProductionPhase.Boarding)
            {PauseWithoutChanging(sequence);Check(sequence.LandOnDeck(),"A later successful wire reaches a real deck fight");deckVisits++;}
            else if(sequence.Phase==AncientProductionPhase.Deck)
            {
                float health=sequence.HealthRatio;
                if(!deckFall&&sequence.Round==0&&health<.91f)
                {deckFall=true;fallRound=sequence.Round;Check(sequence.AbortBoarding(),"A player falling off during a partial fight can reboard");Check(sequence.HealthRatio==health,"A deck fall preserves damage already earned");}
                else
                {
                    float hit=(float)(.005+random.NextDouble()*.047);
                    float preview=sequence.FilterRatioDamage(hit);
                    Check(sequence.HealthRatio==health,"Damage previews never consume health before a hit connects");
                    float accepted=sequence.ApplyDamage(hit);acceptedDamage+=accepted;
                    Check(Near(preview,accepted)&&accepted>=0&&accepted<=hit+.000001f,"Each connected hit accepts exactly its gate-limited preview");
                }
            }
            else Check(sequence.ApplyDamage(.2f)==0,"Protected travel and cinematic frames reject damage without advancing health gates");
            if(sequence.MountCount!=lastMount)
            {
                Check(sequence.MountCount==lastMount+1,"A landing adds exactly one bike mount");
                Check(lastMount==1?sequence.Round==1&&sequence.Phase==AncientProductionPhase.Chase:sequence.Phase==AncientProductionPhase.Finisher,
                    "Bike mounts two and three enter the correct playable ride and finishing scene");lastMount=sequence.MountCount;
            }
            Check(sequence.MountCount>=1&&sequence.MountCount<=3&&sequence.HealthRatio>=.3f,"No valid replay can invent a fourth ride or kill the arrested captain");
            sequence.Tick(dt);
        }
        Check(sequence.Completed&&missedWire&&deckFall&&fallRound==0&&deckVisits==3,"A full encounter remains completable after one missed wire and one deck fall");
        Check(Near(acceptedDamage,.7f)&&Near(sequence.HealthRatio,.3f)&&sequence.MountCount==3,"A full encounter conserves seventy percent damage over exactly three mounts");
        foreach(var phase in new[]{AncientProductionPhase.Breach,AncientProductionPhase.Chase,AncientProductionPhase.Approach,AncientProductionPhase.Boarding,AncientProductionPhase.Deck,AncientProductionPhase.Knockback,AncientProductionPhase.Finisher})
            Check(visited[(int)phase],"The replay includes the authored scene "+phase);
        PauseWithoutChanging(sequence);
    }
    static void Main()
    {
        var sequence=new AncientProductionSequence();
        Check(sequence.Phase==AncientProductionPhase.Dormant&&!sequence.Active&&!sequence.Completed&&sequence.MountCount==0,"A loaded but unstarted encounter does not move a bike or play a cinematic");
        Check(!sequence.BeginBoarding()&&!sequence.LandOnDeck()&&!sequence.AbortBoarding()&&sequence.ApplyDamage(1)==0,"Calls cannot skip inventory and breach into deck combat");
        sequence.Tick(100);Check(sequence.Elapsed==0&&sequence.HealthRatio==1,"Idle time cannot complete an encounter");
        foreach(int fps in new[]{15,30,60,120,144,240})
        {
            sequence.Begin();Check(sequence.Round==0&&sequence.MountCount==1&&sequence.Cinematic&&!sequence.AutoDrive&&!sequence.AttackersEnabled,"The wall breach starts with the first bike ride still pending");
            float time=Until(sequence,AncientProductionPhase.Chase,fps,4);
            Check(time>=3.2f-.0001f&&time<=3.2f+2f/fps,"The breach stays visible for its complete authored duration at "+fps+" FPS");
            Check(sequence.AutoDrive&&sequence.AttackersEnabled&&!sequence.Cinematic,"The first ride drives forward automatically while road attacks are enabled");
            Check(!sequence.BeginBoarding()&&!sequence.LandOnDeck()&&sequence.ApplyDamage(100)==0,"Neither early wire input nor a large laser skips the hazardous chase");
            time=Until(sequence,AncientProductionPhase.Approach,fps,18);
            Check(time>=16-.0001f&&time<=16+2f/fps,"The first road section offers sixteen active seconds of jump practice");
            Check(sequence.AutoDrive&&!sequence.AttackersEnabled&&!sequence.Cinematic,"Approach keeps the bike moving but stops incoming road attacks");
            for(int i=0;i<fps*20;i++)sequence.Tick(1f/fps);
            Check(sequence.Phase==AncientProductionPhase.Approach&&sequence.Round==0&&sequence.MountCount==1,"The first boarding opportunity remains available if the player takes time to aim");
            Check(sequence.BeginBoarding()&&!sequence.AutoDrive&&!sequence.AttackersEnabled,"Only a successful wire handoff ends auto-driving control");
            for(int i=0;i<fps*10;i++)sequence.Tick(1f/fps);
            Check(sequence.Phase==AncientProductionPhase.Boarding&&!sequence.Completed,"Wire travel never becomes a deck landing because a timer elapsed");
            Check(sequence.LandOnDeck()&&!sequence.BeginBoarding(),"A physical truck landing begins the deck fight once");
            Check(Near(sequence.FilterRatioDamage(5),.35f)&&sequence.HealthRatio==1,"Previewing a huge damage burst is side-effect free and capped at the first threshold");
            Check(Near(sequence.ApplyDamage(5),.35f)&&Near(sequence.HealthRatio,.65f)&&sequence.Phase==AncientProductionPhase.Knockback,"The first fight ends at sixty-five percent instead of being one-shot");
            Check(sequence.MountCount==1&&sequence.Cinematic&&sequence.ApplyDamage(5)==0&&!sequence.AbortBoarding(),"Knockback is protected and cannot be mistaken for another finished bike mount");
            time=Until(sequence,AncientProductionPhase.Chase,fps,3);
            Check(time>=2.2f-.0001f&&time<=2.2f+2f/fps&&sequence.Round==1&&sequence.MountCount==2,"The first fall lands on the second bike ride after its visible transition");
            Check(Near(sequence.HealthRatio,.65f)&&sequence.AttackersEnabled&&sequence.AutoDrive,"The second ride preserves the captain's earned damage and restores road hazards");
            time=Until(sequence,AncientProductionPhase.Approach,fps,16);
            Check(time>=14-.0001f&&time<=14+2f/fps,"The second road section returns to a safe wire window after fourteen seconds");
            Check(sequence.BeginBoarding()&&sequence.LandOnDeck(),"The second truck board requires its own attach and landing");
            Check(Near(sequence.ApplyDamage(5),.35f)&&Near(sequence.HealthRatio,.30f),"The second damage gate preserves a living captain for the scripted finale");
            time=Until(sequence,AncientProductionPhase.Finisher,fps,3);
            Check(sequence.MountCount==3&&sequence.Round==1&&sequence.Cinematic&&!sequence.AutoDrive&&!sequence.AttackersEnabled,"The third bike mount is the finale rather than a third chase/fight loop");
            Check(sequence.ApplyDamage(float.MaxValue)==0&&!sequence.BeginBoarding()&&!sequence.LandOnDeck(),"Normal attacks and boarding input cannot interrupt the finishing cutscene");
            time=Until(sequence,AncientProductionPhase.Secured,fps,12);
            Check(time>=10.5f-.0001f&&time<=10.5f+2f/fps,"Cargo separation, backflip and laser get the complete finishing duration");
            Check(sequence.Completed&&!sequence.Active&&!sequence.Cinematic&&Near(sequence.HealthRatio,.30f),"Securing the cores ends the sequence without treating the captain as a killed enemy");
            float elapsed=sequence.Elapsed;sequence.Tick(200);Check(sequence.Completed&&sequence.Elapsed==elapsed,"Completed scenes cannot replay or award another mount from extra elapsed time");
        }
        sequence=AtDeck();sequence.ApplyDamage(.1f);
        Check(sequence.AbortBoarding()&&sequence.Phase==AncientProductionPhase.Approach&&Near(sequence.HealthRatio,.9f)&&sequence.Round==0&&sequence.MountCount==1,"An accidental deck fall gives another safe boarding attempt without health loss or phase credit");
        Check(!sequence.AbortBoarding()&&sequence.BeginBoarding()&&sequence.AbortBoarding()&&sequence.Phase==AncientProductionPhase.Approach,"A failed wire can retry without creating extra knockback/finisher transitions");
        Check(sequence.BeginBoarding()&&sequence.LandOnDeck()&&Near(sequence.ApplyDamage(.25f),.25f),"Resuming a partial deck fight only needs its remaining damage");
        Check(sequence.Phase==AncientProductionPhase.Knockback&&Near(sequence.HealthRatio,.65f),"Partial damage and boarding retries reach the same first gate");
        foreach(int checkpoint in new[]{int.MinValue,-1,0,1,2,int.MaxValue})
        {
            sequence.RestoreRound(checkpoint);int expected=checkpoint<=0?0:1;
            Check(sequence.Round==expected&&sequence.MountCount==expected+1&&sequence.Phase==AncientProductionPhase.Chase&&sequence.Elapsed==0,"Saved retry values resolve to one of the two actual playable road checkpoints");
            Check(Near(sequence.HealthRatio,expected==0?1:.65f)&&!sequence.Completed,"Retry restores the appropriate deck health gate without replaying the wall breach");
        }
        sequence=AtDeck(1);sequence.ApplyDamage(.07f);sequence.RestoreRound(1);
        Check(Near(sequence.HealthRatio,.65f)&&sequence.MountCount==2,"A second-round retry restarts only that fight, with no third-mount credit");
        foreach(float invalid in new[]{0f,-.1f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            sequence=AtDeck();float before=sequence.Elapsed;
            sequence.Tick(invalid);Check(sequence.Elapsed==before&&sequence.Phase==AncientProductionPhase.Deck,"Invalid active-clock increments leave state and elapsed time unchanged");
            Check(sequence.FilterRatioDamage(invalid)==0&&sequence.ApplyDamage(invalid)==0&&sequence.HealthRatio==1,"Invalid or nonpositive damage cannot alter health or manufacture a knockback");
        }
        sequence.Begin();for(int i=0;i<1000;i++)sequence.Tick(.25f,true);
        Check(sequence.Elapsed==0&&sequence.Phase==AncientProductionPhase.Breach,"Menus/dialogue/focus pause can freeze the breach without consuming its timing");
        sequence.Tick(float.MaxValue);Check(Near(sequence.Elapsed,.25f)&&sequence.Phase==AncientProductionPhase.Breach,"A stalled frame consumes only a bounded visual step, not a whole cutscene");
        Until(sequence,AncientProductionPhase.Chase,60,4);sequence.Tick(float.MaxValue);
        Check(sequence.Phase==AncientProductionPhase.Chase&&Near(sequence.Elapsed,.25f),"Clock overshoot is discarded when entering a new playable road phase");
        sequence=AtDeck();sequence.ApplyDamage(1);for(int i=0;i<100;i++)sequence.Tick(.25f,true);
        Check(sequence.Phase==AncientProductionPhase.Knockback&&sequence.Elapsed==0&&sequence.MountCount==1,"Pausing during the ejection cannot secretly count the next bike landing");
        sequence=AtDeck(1);sequence.ApplyDamage(1);Until(sequence,AncientProductionPhase.Finisher,60,3);
        for(int i=0;i<100;i++)sequence.Tick(.25f,true);
        Check(sequence.Phase==AncientProductionPhase.Finisher&&sequence.Elapsed==0&&!sequence.Completed,"A paused finishing shot cannot silently secure cores or unlock the next story scene");
        sequence.Begin();Check(sequence.Round==0&&sequence.MountCount==1&&sequence.HealthRatio==1&&!sequence.Completed,"Starting a fresh production encounter removes old victory and retry state");
        sequence.Reset();Check(sequence.Phase==AncientProductionPhase.Dormant&&sequence.MountCount==0&&!sequence.Active&&sequence.Elapsed==0,"A new game or scene cleanup releases all sequence state");
        foreach(float[] steps in new[]{new[]{1/30f},new[]{1/60f},new[]{1/144f},new[]{1/144f,.3f,1/30f,.008f,.7f}})
            for(int seed=1;seed<=8;seed++)Replay(steps,seed);
        Console.WriteLine("PASS: "+checks+" production chase, boarding, health-gate, third-mount finale and active-clock checks");
    }
}
