using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 일반 경비 둘의 그림만 갱신한다. 원점은 보행 경비의 발바닥과 드론의 기존 부유 기준점이다.
    internal sealed class AncientFieldFoeMotion
    {
        const int CellWidth = 184, CellHeight = 128, FramesPerActor = 24;
        const float BodyPixelsPerUnit = 64;
        const float HitDuration = .16f, StrikeDuration = .16f, SettleDuration = .17f;
        static readonly Sprite[] cached = new Sprite[FramesPerActor];
        readonly SpriteRenderer art;
        readonly bool drone;
        readonly AncientGuardSkin guard;
        readonly Vector3 rest;
        float previousX, speed, stride, hitAt = -100, hitDirection, strokeAt = -100;
        bool sampled;

        public AncientFieldFoeMotion(SpriteRenderer body, bool isDrone)
        {
            art = body;
            drone = isDrone;
            rest = body.transform.localPosition;
            if (drone) body.sprite = DroneFrame(0);
            else guard = new AncientGuardSkin(body);
        }

        public void Strike() => strokeAt = Time.time;

        public void Hit(float away)
        {
            hitAt = Time.time;
            SetHitDirection(away);
        }

        public void SetHitDirection(float away)
        {
            if (Mathf.Abs(away) > .001f) hitDirection = Mathf.Sign(away);
            else hitDirection = art.flipX ? 1 : -1;
        }

        public void Reset()
        {
            sampled = false;
            speed = stride = 0;
            strokeAt = hitAt = -100;
            if (!art) return;
            if (drone) art.sprite = cached[0];
            else guard.Rest();
            art.transform.localPosition = rest;
            art.transform.localRotation = Quaternion.identity;
            art.transform.localScale = Vector3.one;
        }

        public void Present(float localX, float facing, bool aiming, float windup, bool stunned)
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            float dx = sampled ? localX - previousX : 0;
            previousX = localX;
            sampled = true;
            // 막힌 발걸음과 순간 이동에는 보폭을 더하지 않는다. 트럭 자체의 속도는 제외한다.
            float velocity = Mathf.Abs(dx) < .55f ? dx / dt : 0;
            speed = Mathf.MoveTowards(speed, velocity, dt * 36f);
            float age = Time.time - strokeAt;
            float hitAge = Time.time - hitAt;
            float kick = hitAge >= 0 && hitAge < HitDuration ? 1 - hitAge / HitDuration : 0;
            if (!drone)
            {
                stride = Mathf.Repeat(stride + Mathf.Abs(velocity) * dt / 1.12f, 1);
                float gait = (aiming || stunned || age < StrikeDuration + SettleDuration) ? 0 : Mathf.Clamp01(Mathf.Abs(speed) / 2.4f);
                if (gait < .015f && !aiming && !stunned && kick == 0 && age >= StrikeDuration + SettleDuration)
                    guard.Rest();
                else guard.Show(AncientGuardSkin.Pose.Sample(gait, stride, aiming, windup, age, kick, stunned));
                art.transform.localPosition = rest + new Vector3(kick * hitDirection * .13f, 0, 0);
                art.transform.localRotation = Quaternion.Euler(0, 0, -hitDirection * kick * 7);
                return;
            }
            float lean = 0, offset = 0, dip = 0;
            int frame;
            if (stunned)
            {
                frame = 21 + Mathf.Clamp((int)(hitAge / .07f), 0, 2);
                dip = -.025f;
            }
            else if (aiming)
            {
                // 고정한 방향을 그대로 보며 팔을 올리거나 네 추진기를 접는다.
                frame = 10 + Mathf.Clamp((int)(windup * 4), 0, 3);
                lean = facing * 7f * windup;
            }
            else if (age >= 0 && age < StrikeDuration + SettleDuration)
            {
                if (age < StrikeDuration)
                {
                    float progress = age / StrikeDuration;
                    frame = 14 + Mathf.Clamp((int)(progress * 4), 0, 3);
                    offset = -.13f * facing * (1 - progress);
                    lean = facing * 10 * (1 - progress);
                }
                else frame = 18 + Mathf.Clamp((int)((age - StrikeDuration) * 3 / SettleDuration), 0, 2);
            }
            else if (kick > 0)
                frame = 21 + Mathf.Clamp((int)((1 - kick) * 3), 0, 2);
            else if (Mathf.Abs(speed) > .14f)
            {
                stride = Mathf.Repeat(stride + Mathf.Abs(velocity) * dt / 1.55f, 1);
                frame = 2 + Mathf.Min(7, (int)(stride * 8));
                lean = Mathf.Clamp(-speed * 5.2f, -16, 16);
            }
            else frame = Mathf.Sin(Time.time * 4.6f) > .2f ? 1 : 0;

            art.sprite = DroneFrame(frame);
            art.transform.localPosition = rest + new Vector3(offset + hitDirection * kick * .13f, dip, 0);
            art.transform.localRotation = Quaternion.Euler(0, 0, lean - hitDirection * kick * 9);
        }

        public void Dispose() => guard?.Dispose();

        static Sprite DroneFrame(int frame)
        {
            if (cached[frame]) return cached[frame];
            return cached[frame] = AncientArt.Slice("FieldGuardMotion", "field-drone-" + frame,
                new Rect(frame % 8 * CellWidth, frame / 8 * CellHeight, CellWidth, CellHeight), BodyPixelsPerUnit,
                new Vector2(.5f, 8f / CellHeight), false);
        }
    }
}
