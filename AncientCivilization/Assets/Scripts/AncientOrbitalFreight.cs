using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 화물은 실제 물리 발판이다. 도착점과 진행 방향을 한 물리 틱에서 결정한다.
    public sealed class AncientOrbitalFreight : MonoBehaviour
    {
        Rigidbody2D body;
        AncientGame game;
        float left, right, speed = 3.1f;
        int direction = 1;
        bool ferry, brake;
        public int Direction => direction;

        public void Initialize(AncientGame owner, Rigidbody2D platform, float minX, float maxX)
        {
            game = owner;
            body = platform;
            left = minX;
            right = maxX;
        }

        public void SetDirection(int guide)
        {
            direction = guide < 0 ? -1 : 1;
            brake = false;
        }

        public void BrakeForReversal() => brake = true;

        public void ReleaseBrake() => brake = false;

        public void FerryAcross(int goalSide)
        {
            direction = goalSide < 0 ? -1 : 1;
            speed = 8f;
            ferry = true;
        }

        public void Cruise()
        {
            speed = 3.1f;
            ferry = false;
        }

        void FixedUpdate()
        {
            if (!game || !body) return;
            if (game.Paused || game.won || brake)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }
            float current = body.position.x;
            float next = Mathf.Clamp(current + direction * speed * Time.fixedDeltaTime, left, right);
            body.MovePosition(new Vector2(next, body.position.y));
            if (next > left && next < right) return;
            direction = -direction;
            if (ferry) Cruise();
        }
    }
}
