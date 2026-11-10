using System;

namespace Plugin.AncientCivilization
{
    public enum AncientProductionPhase { Dormant,Breach,Chase,Approach,Boarding,Deck,Knockback,Finisher,Secured }

    public static class AncientProductionRules
    {
        public const float BreachSeconds=3.2f,FirstChaseSeconds=16,SecondChaseSeconds=14;
        public const float KnockbackSeconds=2.2f,FinisherSeconds=10.5f,MaxTickSeconds=.25f;
        public const float FirstHealthGate=.65f,SecondHealthGate=.30f;
        public static float ChaseSeconds(int round){return round<=0?FirstChaseSeconds:SecondChaseSeconds;}
        public static float HealthGate(int round){return round<=0?FirstHealthGate:SecondHealthGate;}
        public static float Duration(AncientProductionPhase phase,int round)
        {
            return phase==AncientProductionPhase.Breach?BreachSeconds:
                phase==AncientProductionPhase.Chase?ChaseSeconds(round):
                phase==AncientProductionPhase.Knockback?KnockbackSeconds:
                phase==AncientProductionPhase.Finisher?FinisherSeconds:0;
        }
    }

    // The driving and deck fights are controlled by player actions; only their visible
    // transitions use a clock. A stalled frame cannot consume the next playable phase.
    public sealed class AncientProductionSequence
    {
        double elapsed;
        public AncientProductionPhase Phase {get;private set;}
        public int Round {get;private set;}
        public int MountCount {get;private set;}
        public float Elapsed {get{return (float)elapsed;}}
        public float HealthRatio {get;private set;}=1;
        public bool Completed {get{return Phase==AncientProductionPhase.Secured;}}
        public bool Active {get{return Phase!=AncientProductionPhase.Dormant&&!Completed;}}
        public bool AutoDrive {get{return Phase==AncientProductionPhase.Chase||Phase==AncientProductionPhase.Approach;}}
        public bool AttackersEnabled {get{return Phase==AncientProductionPhase.Chase;}}
        public bool Cinematic {get{return Phase==AncientProductionPhase.Breach||Phase==AncientProductionPhase.Knockback||Phase==AncientProductionPhase.Finisher;}}
        public float Progress
        {
            get
            {
                float duration=AncientProductionRules.Duration(Phase,Round);
                return duration>0?(float)Math.Min(1,elapsed/duration):0;
            }
        }
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
        void Enter(AncientProductionPhase phase){Phase=phase;elapsed=0;}
        public void Begin()
        {Round=0;MountCount=1;HealthRatio=1;Enter(AncientProductionPhase.Breach);}
        public void RestoreRound(int round)
        {
            Round=Math.Max(0,Math.Min(1,round));MountCount=Round+1;
            HealthRatio=Round==0?1:AncientProductionRules.FirstHealthGate;Enter(AncientProductionPhase.Chase);
        }
        public void Reset()
        {Round=MountCount=0;HealthRatio=1;Enter(AncientProductionPhase.Dormant);}
        public void Tick(float seconds,bool paused=false)
        {
            if(paused||!Active||!Finite(seconds)||seconds<=0)return;
            float duration=AncientProductionRules.Duration(Phase,Round);
            elapsed=Math.Min(float.MaxValue,elapsed+Math.Min(seconds,AncientProductionRules.MaxTickSeconds));
            if(duration<=0||elapsed<duration)return;
            // Deliberately discard overshoot: approach/boarding/deck cannot be skipped by a hitch.
            switch(Phase)
            {
                case AncientProductionPhase.Breach:Enter(AncientProductionPhase.Chase);break;
                case AncientProductionPhase.Chase:Enter(AncientProductionPhase.Approach);break;
                case AncientProductionPhase.Knockback:
                    if(Round==0){Round=1;MountCount=2;Enter(AncientProductionPhase.Chase);}
                    else{MountCount=3;Enter(AncientProductionPhase.Finisher);}break;
                case AncientProductionPhase.Finisher:Enter(AncientProductionPhase.Secured);break;
            }
        }
        public bool BeginBoarding()
        {
            if(Phase!=AncientProductionPhase.Approach)return false;
            Enter(AncientProductionPhase.Boarding);return true;
        }
        public bool LandOnDeck()
        {
            if(Phase!=AncientProductionPhase.Boarding)return false;
            Enter(AncientProductionPhase.Deck);return true;
        }
        public bool AbortBoarding()
        {
            if(Phase!=AncientProductionPhase.Boarding&&Phase!=AncientProductionPhase.Deck)return false;
            Enter(AncientProductionPhase.Approach);return true;
        }
        public float FilterRatioDamage(float ratioDamage)
        {
            if(Phase!=AncientProductionPhase.Deck||!Finite(ratioDamage)||ratioDamage<=0)return 0;
            return Math.Min(ratioDamage,Math.Max(0,HealthRatio-AncientProductionRules.HealthGate(Round)));
        }
        public float ApplyDamage(float ratioDamage)
        {
            float accepted=FilterRatioDamage(ratioDamage);if(accepted<=0)return 0;
            float before=HealthRatio,gate=AncientProductionRules.HealthGate(Round);
            HealthRatio=Math.Max(gate,before-accepted);
            if(accepted>=before-gate||HealthRatio<=gate){HealthRatio=gate;Enter(AncientProductionPhase.Knockback);}
            return before-HealthRatio;
        }
    }
}
