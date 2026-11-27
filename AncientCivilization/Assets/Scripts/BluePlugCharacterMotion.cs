using Plugin.PlayerCore;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 왼손은 감개, 오른손은 플러그 쪽 전선을 맡는다. 시각 관절은 물리 상태와 별개다.
    public sealed class BluePlugCharacterMotion
    {
        readonly Vector3 footOrigin;
        readonly SpriteRenderer art, reel;
        readonly Arm left, right;
        readonly BluePlugData poseSettings;
        readonly float scale;
        readonly BluePlugMotionTimeline timeline = new BluePlugMotionTimeline();
        readonly AncientLocomotionMotion gait = new AncientLocomotionMotion();
        BluePlugPose previousPose;
        float angle, angleVelocity, releaseAngle, facing = 1, reelAngle, finaleArcDegrees, finaleRopeAngle;
        Vector2 throwDirection, towBearing, swingTangent, leftOffset, rightOffset, fromLeft, fromRight;
        bool footPivot, swungBeforeRelease, finalePose;
        int frame;

        public BluePlugCharacterMotion(SpriteRenderer target, float visualScale = 1f, BluePlugData settings = null)
        {
            art = target;
            scale = visualScale;
            if (!art) return;
            footOrigin = art.transform.localPosition;
            poseSettings = settings ? settings : Resources.Load<BluePlugData>("Traversal/DefaultBluePlug");
            BluePlugMotionArt.Prepare();
            BluePlugPoseAtlas.Prepare();
            left = new Arm(art, true);
            right = new Arm(art, false);
            reel = Attach(art, "왼손 전선 감개", BluePlugMotionArt.Reel, 3);
        }

        public bool Presenting { get; private set; }
        public Transform LeftGripSocket => left?.Socket;
        public Transform RightGripSocket => right?.Socket;
        public Vector2 LeftGripWorld => Presenting ? (Vector2)LeftGripSocket.position : PassiveGripWorld(true);
        public Vector2 RightGripWorld => Presenting ? (Vector2)RightGripSocket.position : PassiveGripWorld(false);
        public Vector2 EmitterWorld => LeftGripWorld;
        bool Detailed => !finalePose && BluePlugPoseAtlas.Available;
        public void SetFinalePose(bool value) => finalePose = value;

        public bool Apply(BluePlugPhase phase, Vector2 towardPlug, Vector2 velocity, float movementFacing,
            bool grounded, float dt, AncientLocomotionMotion locomotion = null,
            Vector2? wallGripPoint = null, Vector2? aimDirection = null, bool useFinalePose = false)
        {
            if (!art) return false;
            finalePose = useFinalePose;
            var pose = timeline.Step(phase, dt, wallGripPoint.HasValue, grounded, !finalePose);
            bool changed = pose != previousPose;
            if (pose == BluePlugPose.Recovery && changed) swungBeforeRelease = previousPose == BluePlugPose.Swing;
            previousPose = pose;
            if (locomotion == null)
            {
                gait.Step(grounded, velocity.x, velocity.y, movementFacing, dt);
                locomotion = gait;
            }
            UpdateReel(phase, dt);
            if (pose == BluePlugPose.None)
            {
                Presenting = false;
                angle = angleVelocity = 0;
                if (phase == BluePlugPhase.Ready) HideHands();
                else PresentPassive();
                return false;
            }

            float age = timeline.PoseAge;
            float direction = towardPlug.x < -.09f ? -1 : towardPlug.x > .09f ? 1 : movementFacing < 0 ? -1 : 1;
            if (changed)
            {
                fromLeft = Presenting ? leftOffset : new Vector2(.10f, -.31f);
                fromRight = Presenting ? rightOffset : new Vector2(-.03f, -.38f);
            }
            footPivot = pose == BluePlugPose.Windup || pose == BluePlugPose.Throw || pose == BluePlugPose.Reel || pose == BluePlugPose.Landing;
            float target = 0, sx = 1, sy = 1;
            if (!finalePose || !ApplyFinaleTravel(pose, age, towardPlug, velocity, direction, changed,
                grounded, locomotion, ref target, ref sx, ref sy))
            {
                switch (pose)
                {
                    case BluePlugPose.Windup:
                        float aimX = finalePose ? 0 : aimDirection.GetValueOrDefault().x;
                        facing = Mathf.Abs(aimX) > .1f ? Mathf.Sign(aimX) : Mathf.Sign(movementFacing);
                        frame = locomotion.Frame;
                        Vector2 orbit = towardPlug.normalized;
                        target = locomotion.Angle + facing * (2 + orbit.y * 4);
                        sx = locomotion.ScaleX * (1.008f + orbit.x * .012f);
                        sy = locomotion.ScaleY * (.995f + orbit.y * .012f);
                        break;
                    case BluePlugPose.Throw:
                        if (changed)
                        {
                            throwDirection = towardPlug;
                            facing = direction;
                        }
                        frame = locomotion.Frame;
                        float stretch = Mathf.Sin(Mathf.Clamp01(age / BluePlugMotionTimeline.ThrowDuration) * Mathf.PI);
                        target = facing * Mathf.Lerp(-12, -4, stretch) + Mathf.Clamp(throwDirection.normalized.y * facing * 11, -11, 11);
                        sx = 1 + .08f * stretch;
                        sy = 1 - .05f * stretch;
                        break;
                    case BluePlugPose.Tow:
                        // 도착 직전 소켓은 손을 지나칠 수 있다. 몸통은 실제 진행 방향을, 손은 접점을 따른다.
                        if (velocity.sqrMagnitude > 4f) towBearing = velocity.normalized;
                        else if (changed || towBearing.sqrMagnitude < .001f) towBearing = towardPlug.normalized;
                        if (Mathf.Abs(towBearing.x) > .16f) facing = Mathf.Sign(towBearing.x);
                        frame = age < .065f ? 0 : 1;
                        // 가슴이 먼저 기울고 뒤쪽 부츠는 곧이어 펴진다. 몸통 판정은 고정돼 있다.
                        float side = Mathf.Abs(towBearing.x) < .02f ? .02f * facing : towBearing.x;
                        target = Mathf.Clamp(-Mathf.Atan2(side, towBearing.y) * Mathf.Rad2Deg, -155, 155);
                        sx = .97f;
                        sy = 1.08f;
                        break;
                    case BluePlugPose.Swing:
                        // 무릎을 접어 매달렸다가 호의 바닥을 지날 때 진행 방향으로 부츠를 찬다.
                        facing = AncientSwingRules.Facing(velocity.x, direction);
                        if (velocity.sqrMagnitude > .36f) swingTangent = velocity.normalized;
                        else if (changed) swingTangent = Vector2.right * facing;
                        frame = velocity.y < -1.7f ? 2 : velocity.y > 1.7f ? 3 :
                            Mathf.Abs(velocity.x) > 2.5f ? 4 : 5;
                        Vector2 rope = towardPlug.normalized;
                        float hangerTilt = -Mathf.Atan2(rope.x, Mathf.Max(.18f, rope.y)) * Mathf.Rad2Deg;
                        target = Mathf.Clamp(hangerTilt - facing * 7 + Mathf.Clamp(velocity.x * .4f, -5, 5), -48, 48);
                        break;
                    case BluePlugPose.Landing:
                        frame = age < .075f ? 2 : 3;
                        facing = Mathf.Abs(velocity.x) > 1 ? Mathf.Sign(velocity.x) : facing;
                        target = angle = angleVelocity = 0;
                        break;
                    case BluePlugPose.WallBrace: target = 0; break;
                    case BluePlugPose.Reel:
                        facing = movementFacing < 0 ? -1 : 1;
                        frame = locomotion.Frame;
                        target = locomotion.Angle - facing * 3;
                        break;
                    default:
                        if (changed) releaseAngle = angle;
                        if (swungBeforeRelease && Mathf.Abs(velocity.x) > 1) facing = Mathf.Sign(velocity.x);
                        frame = swungBeforeRelease && age > .13f ? 7 : 6;
                        float recovery = Mathf.SmoothStep(0, 1, age / BluePlugMotionTimeline.RecoveryDuration);
                        if (changed && swungBeforeRelease && velocity.sqrMagnitude > .36f) swingTangent = velocity.normalized;
                        float exitAngle = swungBeforeRelease ? Alignment(swingTangent) : releaseAngle;
                        target = Mathf.LerpAngle(exitAngle, locomotion.Angle, recovery);
                        break;
                }
            }

            // 벽에서 다른 콘센트를 조준해도 하체는 접은 채로 있고 자유로운 오른팔만 던진다.
            if (wallGripPoint.HasValue)
            {
                footPivot = false;
                facing = movementFacing < 0 ? -1 : 1;
                target = angle = angleVelocity = 0;
                sx = sy = 1;
            }
            angle = dt > 0 ? Mathf.SmoothDampAngle(angle, target, ref angleVelocity, .047f, 950, dt) : angle;
            // 지상에서는 발바닥 지지선을 기준으로 회전하고 두 부츠가 바닥을 뚫지 않게 한다.
            if (footPivot && grounded) angle = Mathf.Clamp(Mathf.DeltaAngle(0, angle), -6, 6);
            if (Detailed)
                art.sprite = wallGripPoint.HasValue ? BluePlugPoseAtlas.Wall : TravelArt(pose)
                    ? BluePlugPoseAtlas.Travel(frame) : footPivot ? BluePlugPoseAtlas.Ground(frame) : BluePlugPoseAtlas.Air(frame);
            else
                art.sprite = wallGripPoint.HasValue ? BluePlugMotionArt.WallBody : TravelArt(pose)
                    ? BluePlugMotionArt.TravelBody(frame) : footPivot ? BluePlugMotionArt.GroundBody(frame) : BluePlugMotionArt.Body(frame);
            art.flipX = facing < 0;
            art.transform.localRotation = Quaternion.Euler(0, 0, angle);
            art.transform.localScale = new Vector3(scale * sx, scale * sy, 1);

            Vector3 position = footPivot ? footOrigin : new Vector3(footOrigin.x, 0, footOrigin.z);
            if (!finalePose && pose == BluePlugPose.Tow)
            {
                // 골반의 짧은 지연을 시각 위치로만 표현하고 곧 물리 루트로 되돌린다.
                float lag = Mathf.Sin(Mathf.Clamp01(age / .13f) * Mathf.PI) * .14f;
                position -= (Vector3)(towBearing * (lag * scale));
            }
            // 시각 자식의 두 부츠 밑창을 실제 패널 표면에 맞춘다. 물리 루트나 이동 속도에는 쓰지 않는다.
            if (wallGripPoint.HasValue)
            {
                float wallX = art.transform.parent.InverseTransformPoint(wallGripPoint.Value).x;
                position.x = wallX - facing * BluePlugMotionArt.WallSoleEdge * scale;
            }
            if (footPivot && grounded && pose != BluePlugPose.Landing)
            {
                float radians = angle * Mathf.Deg2Rad;
                float sine = Mathf.Sin(radians);
                float boot = facing * BluePlugMotionArt.GroundSole(frame, facing * sine >= 0) * scale * sx;
                position.x += boot * (1 - Mathf.Cos(radians));
                position.y += locomotion.OffsetY - boot * sine;
            }
            art.transform.localPosition = position;
            Articulate(pose, towardPlug, age, wallGripPoint);
            if (phase == BluePlugPhase.Ready) reel.enabled = false;
            Presenting = true;
            return true;
        }

        // 조커 마지막 포탑 구간은 기존 몸통 셀, 손 위치와 보간 시간을 그대로 사용한다.
        bool ApplyFinaleTravel(BluePlugPose pose, float age, Vector2 towardPlug, Vector2 velocity,
            float direction, bool changed, bool grounded, AncientLocomotionMotion locomotion,
            ref float target, ref float sx, ref float sy)
        {
            switch (pose)
            {
                case BluePlugPose.Tow:
                    if (velocity.sqrMagnitude > 4f) towBearing = velocity.normalized;
                    else if (changed || towBearing.sqrMagnitude < .001f) towBearing = towardPlug.normalized;
                    if (Mathf.Abs(towBearing.x) > .16f) facing = Mathf.Sign(towBearing.x);
                    frame = age < .045f ? 0 : age < .10f ? 1 : 2;
                    target = Alignment(towBearing) - facing * 5;
                    sx = .97f;
                    sy = 1.08f;
                    return true;
                case BluePlugPose.Swing:
                    float ropeAngle = Mathf.Atan2(towardPlug.y, towardPlug.x) * Mathf.Rad2Deg;
                    if (changed) finaleArcDegrees = 0;
                    else finaleArcDegrees += Mathf.Abs(Mathf.DeltaAngle(finaleRopeAngle, ropeAngle));
                    finaleRopeAngle = ropeAngle;
                    facing = AncientSwingRules.Facing(velocity.x, direction);
                    frame = AncientSwingRules.AttachedFrame(age, finaleArcDegrees, velocity.x, velocity.y);
                    target = Alignment(towardPlug);
                    return true;
                case BluePlugPose.Recovery:
                    if (changed) releaseAngle = angle;
                    footPivot = grounded || age >= .12f;
                    frame = footPivot ? locomotion.Frame : 6;
                    float recovery = Mathf.SmoothStep(0, 1, age / BluePlugMotionTimeline.RecoveryDuration);
                    target = Mathf.LerpAngle(releaseAngle, locomotion.Angle, recovery);
                    float settle = grounded ? Mathf.Sin(recovery * Mathf.PI) : 0;
                    sx = locomotion.ScaleX * (1 + .039f * settle);
                    sy = locomotion.ScaleY * (1 - .065f * settle);
                    return true;
                default:
                    return false;
            }
        }

        // 연결 중 걸음과 무기 동작에서도 원래 스프라이트에 있는 왼손을 사용한다.
        public void PresentPassive()
        {
            if (!art) return;
            Presenting = false;
            left.Hide();
            right.Hide();
            Vector2 location = BluePlugMotionArt.OrdinaryGrip(art.sprite, true);
            left.Socket.localPosition = Mirror(location, art.flipX);
            right.Socket.localPosition = Mirror(BluePlugMotionArt.OrdinaryGrip(art.sprite, false), art.flipX);
            ShowReel(left.Socket.localPosition);
        }

        public void Clear()
        {
            timeline.Clear();
            gait.Clear();
            Presenting = false;
            previousPose = BluePlugPose.None;
            angle = angleVelocity = reelAngle = finaleArcDegrees = 0;
            swungBeforeRelease = false;
            towBearing = Vector2.zero;
            swingTangent = Vector2.zero;
            if (art) HideHands();
        }

        public void InterruptRecovery()
        {
            if (!finalePose) timeline.InterruptRecovery();
        }
        bool TravelArt(BluePlugPose pose) => !finalePose && (pose == BluePlugPose.Tow || pose == BluePlugPose.Landing);

        void Articulate(BluePlugPose pose, Vector2 towardPlug, float age, Vector2? wallGripPoint)
        {
            Vector2 localRope = LocalDirection(towardPlug);
            Vector2 leftShoulder = Shoulder(pose, wallGripPoint.HasValue, true);
            Vector2 rightShoulder = Shoulder(pose, wallGripPoint.HasValue, false);
            Vector2 leftTarget = leftShoulder + new Vector2(.16f, -.29f);
            Vector2 rightTarget = rightShoulder + new Vector2(.15f, -.32f);
            Vector2 gripAngle = Vector2.zero;
            if (pose == BluePlugPose.Windup)
            {
                leftTarget += localRope * .035f;
                float radius = poseSettings ? poseSettings.windGripRadius : .43f;
                rightTarget = rightShoulder + new Vector2(.09f, .045f) + localRope * radius;
                gripAngle = localRope;
            }
            else if (pose == BluePlugPose.Throw)
            {
                gripAngle = LocalDirection(throwDirection);
                float finish = Mathf.SmoothStep(0, 1, age / BluePlugMotionTimeline.ThrowDuration);
                rightTarget = rightShoulder + gripAngle * (.59f - .19f * finish) + Vector2.down * (.12f * finish);
                leftTarget += gripAngle * .055f;
            }
            else if (finalePose && (pose == BluePlugPose.Tow || pose == BluePlugPose.Swing))
            {
                leftTarget = leftShoulder + localRope * .42f + new Vector2(.12f, -.11f);
                rightTarget = rightShoulder + localRope * .40f + new Vector2(.05f, -.13f);
            }
            else if (pose == BluePlugPose.Tow)
            {
                // 왼손의 릴이 앞장서고 오른팔은 뒤로 접어 몸통을 길게 편다.
                leftTarget = leftShoulder + localRope * .56f;
                rightTarget = rightShoulder - localRope * .19f + Vector2.down * .25f;
            }
            else if (pose == BluePlugPose.Swing)
            {
                // 양손을 머리 위 한 줄에 모아, 한 손을 앞으로 내민 견인과 구별한다.
                leftTarget = leftShoulder + localRope * .52f;
                rightTarget = leftTarget - localRope * .13f;
                gripAngle = localRope;
            }
            else if (pose == BluePlugPose.WallBrace)
            {
                Vector2 contact = Mirror(art.transform.InverseTransformPoint(wallGripPoint.Value), art.flipX);
                rightTarget = contact + new Vector2(-.14f, .20f);
                gripAngle = Vector2.right;
            }
            else if (!finalePose && pose == BluePlugPose.Recovery && swungBeforeRelease && !footPivot)
            {
                rightTarget = rightShoulder + LocalDirection(swingTangent) * .48f;
                leftTarget = leftShoulder + new Vector2(-.18f, -.25f);
            }
            else if (pose == BluePlugPose.Landing)
            {
                // 착지 충격은 열린 팔로 받아내고, 체중이 발에 실리면 다시 몸 가까이 거둔다.
                float spread = Mathf.Clamp01(1 - age / BluePlugMotionTimeline.LandingDuration);
                rightTarget = rightShoulder + new Vector2(.22f + .27f * spread, -.14f);
                leftTarget = leftShoulder + new Vector2(-.16f * spread, -.24f);
            }
            else if (pose == BluePlugPose.Reel)
            {
                float turn = age * 28;
                rightTarget = leftTarget + new Vector2(.17f + Mathf.Cos(turn) * .055f, .08f + Mathf.Sin(turn) * .07f);
                leftTarget += localRope * .045f;
            }
            if (wallGripPoint.HasValue)
                leftTarget = Mirror(art.transform.InverseTransformPoint(wallGripPoint.Value), art.flipX) + new Vector2(-.10f, -.06f);
            // 릴리즈와 벽 접촉은 첫 렌더 프레임부터 손을 맞춘다. 입력 대기 시간은 없다.
            float reach = pose == BluePlugPose.Throw || pose == BluePlugPose.WallBrace || pose == BluePlugPose.Landing
                ? 1 : Mathf.SmoothStep(0, 1, age / .065f);
            if (finalePose) reach = Mathf.SmoothStep(0, 1, age / (pose == BluePlugPose.Throw ? .055f : .075f));
            leftOffset = Vector2.Lerp(fromLeft, leftTarget - leftShoulder, reach);
            if (!finalePose && pose == BluePlugPose.Tow) leftOffset = leftTarget - leftShoulder;
            rightOffset = Vector2.Lerp(fromRight, rightTarget - rightShoulder, reach);
            left.Draw(leftShoulder, leftShoulder + leftOffset, Vector2.zero, false, art.flipX, 1, Detailed);
            bool behind = pose == BluePlugPose.Windup && localRope.x < -.10f;
            right.Draw(rightShoulder, rightShoulder + rightOffset, gripAngle, pose == BluePlugPose.Throw || pose == BluePlugPose.WallBrace,
                art.flipX, behind ? -3 : 2, Detailed);
            ShowReel(left.Socket.localPosition);
            if (pose == BluePlugPose.Recovery) reel.enabled = false;
        }

        Vector2 Shoulder(BluePlugPose pose, bool onWall, bool leftHand)
        {
            if (onWall) return BluePlugMotionArt.WallShoulder(leftHand);
            if (!TravelArt(pose)) return BluePlugMotionArt.Shoulder(frame, leftHand, footPivot);
            if (Detailed && pose == BluePlugPose.Tow)
                return BluePlugMotionArt.Shoulder(frame == 0 ? 0 : 2, leftHand);
            return BluePlugMotionArt.TravelShoulder(frame, leftHand);
        }

        void UpdateReel(BluePlugPhase phase, float dt)
        {
            float payout = poseSettings ? poseSettings.reelPayoutSpeed : 1200;
            float intake = poseSettings ? poseSettings.reelReturnSpeed : 920;
            float rate = phase == BluePlugPhase.Flying ? -payout : phase == BluePlugPhase.Retracting ? intake :
                phase == BluePlugPhase.Winding ? -165 : 0;
            reelAngle = (reelAngle + rate * dt) % 360;
        }

        void ShowReel(Vector2 position)
        {
            Sprite reelArt = Detailed ? BluePlugPoseAtlas.Equipment(BluePlugPoseAtlas.Part.Reel) : BluePlugMotionArt.Reel;
            if (reel.sprite != reelArt) reel.sprite = reelArt;
            reel.transform.localPosition = position;
            reel.transform.localRotation = Quaternion.Euler(0, 0, art.flipX ? -reelAngle : reelAngle);
            reel.transform.localScale = Vector3.one * (poseSettings ? poseSettings.reelScale : .82f);
            reel.flipX = art.flipX;
            reel.color = art.color;
            reel.sortingOrder = art.sortingOrder + 3;
            reel.enabled = true;
        }

        void HideHands()
        {
            left.Hide();
            right.Hide();
            reel.enabled = false;
        }

        float Alignment(Vector2 towardPlug)
        {
            Vector2 leftReel = BluePlugMotionArt.Shoulder(frame, true) + new Vector2(.26f, .08f);
            return AncientSwingRules.AlignAngle(towardPlug.x, towardPlug.y, leftReel.x * facing, leftReel.y);
        }

        Vector2 LocalDirection(Vector2 direction)
        {
            Vector2 local = Mirror(art.transform.InverseTransformVector(direction), art.flipX);
            return local.sqrMagnitude < .0001f ? Vector2.right : local.normalized;
        }

        Vector2 PassiveGripWorld(bool leftHand)
        {
            if (!art) return Vector2.zero;
            Vector2 point = BluePlugMotionArt.OrdinaryGrip(art.sprite, leftHand);
            return art.transform.TransformPoint(Mirror(point, art.flipX));
        }

        static Vector2 Mirror(Vector2 point, bool flipped) => new Vector2(flipped ? -point.x : point.x, point.y);

        static SpriteRenderer Attach(SpriteRenderer body, string name, Sprite sprite, int depth)
        {
            var part = AncientArt.Display(name, sprite, Vector2.zero, body.sortingOrder + depth, body.transform);
            part.sortingLayerID = body.sortingLayerID;
            part.enabled = false;
            return part;
        }

        // 2D 원 교차점으로 팔꿈치를 구해 두 팔 관절의 길이를 유지한다.
        sealed class Arm
        {
            const float UpperLength = .29f, ForearmLength = .30f;
            readonly SpriteRenderer body, upper, forearm, glove;
            readonly bool left;
            int depth;
            bool detailedParts;
            public Transform Socket => glove.transform;

            public Arm(SpriteRenderer body, bool left)
            {
                this.body = body;
                this.left = left;
                depth = left ? 1 : -3;
                string side = left ? "왼" : "오른";
                upper = Attach(body, side + "팔 상박", left ? BluePlugMotionArt.ClothUpper : BluePlugMotionArt.MetalUpper, depth);
                forearm = Attach(body, side + "팔 하박", left ? BluePlugMotionArt.ClothForearm : BluePlugMotionArt.MetalForearm, depth);
                glove = Attach(body, side + "손", left ? BluePlugMotionArt.LeftFist : BluePlugMotionArt.RightFist, depth + 1);
            }

            public void Draw(Vector2 shoulder, Vector2 target, Vector2 gripDirection, bool open, bool flipped, int order,
                bool detailed)
            {
                depth = order;
                if (detailed != detailedParts)
                {
                    detailedParts = detailed;
                    upper.sprite = detailed ? BluePlugPoseAtlas.Equipment(left ? BluePlugPoseAtlas.Part.ClothUpper :
                        BluePlugPoseAtlas.Part.MetalUpper) : left ? BluePlugMotionArt.ClothUpper : BluePlugMotionArt.MetalUpper;
                    forearm.sprite = detailed ? BluePlugPoseAtlas.Equipment(left ? BluePlugPoseAtlas.Part.ClothForearm :
                        BluePlugPoseAtlas.Part.MetalForearm) : left ? BluePlugMotionArt.ClothForearm : BluePlugMotionArt.MetalForearm;
                }
                Vector2 reach = target - shoulder;
                float raw = reach.magnitude;
                Vector2 direction = raw > .001f ? reach / raw : Vector2.right;
                float length = Mathf.Clamp(raw, .095f, UpperLength + ForearmLength - .007f);
                float along = (UpperLength * UpperLength - ForearmLength * ForearmLength + length * length) / (2 * length);
                float outwards = Mathf.Sqrt(Mathf.Max(0, UpperLength * UpperLength - along * along));
                Vector2 axis = new Vector2(-direction.y, direction.x);
                Vector2 elbow = shoulder + direction * along - axis * outwards;
                Vector2 hand = shoulder + direction * length;
                Segment(upper, shoulder, elbow, flipped);
                Segment(forearm, elbow, hand, flipped);
                glove.sprite = detailed ? BluePlugPoseAtlas.Equipment(left ? BluePlugPoseAtlas.Part.LeftFist :
                    open ? BluePlugPoseAtlas.Part.RightOpen : BluePlugPoseAtlas.Part.RightFist) :
                    left ? BluePlugMotionArt.LeftFist : open ? BluePlugMotionArt.RightOpen : BluePlugMotionArt.RightFist;
                glove.transform.localPosition = Mirror(hand, flipped);
                Vector2 palm = Mirror(gripDirection.sqrMagnitude > .001f ? gripDirection : hand - elbow, flipped);
                glove.transform.localRotation = Quaternion.Euler(0, 0, Heading(palm));
                glove.flipY = flipped;
                glove.sortingOrder = body.sortingOrder + depth + 1;
                glove.color = body.color;
                glove.enabled = true;
            }

            void Segment(SpriteRenderer segment, Vector2 start, Vector2 end, bool flipped)
            {
                Vector2 delta = Mirror(end - start, flipped);
                segment.transform.localPosition = Mirror(start, flipped);
                segment.transform.localRotation = Quaternion.Euler(0, 0, Heading(delta));
                float pixels = segment.sprite.rect.width - 2 * segment.sprite.pivot.x;
                segment.transform.localScale = new Vector3(delta.magnitude * segment.sprite.pixelsPerUnit / pixels, 1, 1);
                segment.flipY = flipped;
                segment.sortingOrder = body.sortingOrder + depth;
                segment.color = body.color;
                segment.enabled = true;
            }

            public void Hide() => upper.enabled = forearm.enabled = glove.enabled = false;
        }

        static float Heading(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
    }
}
