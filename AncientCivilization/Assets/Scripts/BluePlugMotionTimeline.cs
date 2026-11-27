using System;

namespace Plugin.PlayerCore
{
    public enum BluePlugPose { None, Windup, Throw, Tow, Swing, Recovery, Reel, WallBrace, Landing }

    // 투척 직후 바로 꽂히더라도 팔의 마무리 동작이 끊기지 않는다.
    public sealed class BluePlugMotionTimeline
    {
        public const float ThrowDuration = .18f;
        public const float RecoveryDuration = .26f;
        public const float LandingDuration = .15f;
        BluePlugPhase phase;
        float phaseAge, throwAge = ThrowDuration, recoveryAge = RecoveryDuration;
        float landingAge = LandingDuration;
        bool awaitingFloor, skipRelease;
        public float PoseAge { get; private set; }

        public BluePlugPose Step(BluePlugPhase next, float dt, bool restingAtSocket = false,
            bool grounded = false, bool showLanding = true)
        {
            dt = float.IsNaN(dt) || dt < 0 ? 0 : Math.Min(dt, .1f);
            if (phase != next)
            {
                if (next == BluePlugPhase.Flying ||
                    phase == BluePlugPhase.Winding && next == BluePlugPhase.Connected) throwAge = 0;
                if (Moves(phase) && next == BluePlugPhase.Ready && !skipRelease)
                {
                    recoveryAge = 0;
                    awaitingFloor = showLanding;
                }
                skipRelease = false;
                if (next != BluePlugPhase.Ready)
                {
                    awaitingFloor = false;
                    landingAge = LandingDuration;
                }
                if (next == BluePlugPhase.Winding || next == BluePlugPhase.Retracting || Moves(next))
                {
                    throwAge = ThrowDuration;
                    recoveryAge = RecoveryDuration;
                }
                phase = next;
                phaseAge = 0;
            }
            else
            {
                phaseAge += dt;
            }

            BluePlugPose pose = BluePlugPose.None;
            if (restingAtSocket || !showLanding)
            {
                awaitingFloor = false;
                landingAge = LandingDuration;
            }
            if (awaitingFloor && grounded)
            {
                awaitingFloor = false;
                recoveryAge = RecoveryDuration;
                landingAge = 0;
            }
            PoseAge = phaseAge;
            switch (phase)
            {
                case BluePlugPhase.Winding: pose = BluePlugPose.Windup; break;
                case BluePlugPhase.Towing: pose = BluePlugPose.Tow; break;
                case BluePlugPhase.Swinging: pose = BluePlugPose.Swing; break;
                case BluePlugPhase.Retracting: pose = BluePlugPose.Reel; break;
                default:
                    if (grounded && landingAge < LandingDuration)
                    {
                        PoseAge = landingAge;
                        pose = BluePlugPose.Landing;
                    }
                    else if (throwAge < ThrowDuration)
                    {
                        PoseAge = throwAge;
                        pose = BluePlugPose.Throw;
                    }
                    else if (recoveryAge < RecoveryDuration)
                    {
                        PoseAge = recoveryAge;
                        pose = BluePlugPose.Recovery;
                    }
                    break;
            }
            throwAge = Math.Min(ThrowDuration, throwAge + dt);
            recoveryAge = Math.Min(RecoveryDuration, recoveryAge + dt);
            landingAge = Math.Min(LandingDuration, landingAge + dt);
            // 플러그가 꽂힌 것과 플레이어가 벽 손잡이에 닿은 것은 별개다.
            if (restingAtSocket && (phase == BluePlugPhase.Ready || phase == BluePlugPhase.Connected ||
                phase == BluePlugPhase.Flying && pose == BluePlugPose.None))
                return BluePlugPose.WallBrace;
            return pose;
        }

        public void Clear()
        {
            phase = BluePlugPhase.Ready;
            phaseAge = PoseAge = 0;
            throwAge = ThrowDuration;
            recoveryAge = RecoveryDuration;
            landingAge = LandingDuration;
            awaitingFloor = skipRelease = false;
        }

        // 새 점프는 이미 실행됐다. 이전 이동기의 착지·이탈 잔상만 비운다.
        public void InterruptRecovery()
        {
            awaitingFloor = false;
            landingAge = LandingDuration;
            recoveryAge = RecoveryDuration;
            skipRelease = Moves(phase);
        }

        static bool Moves(BluePlugPhase value) => value == BluePlugPhase.Towing || value == BluePlugPhase.Swinging;
    }
}
