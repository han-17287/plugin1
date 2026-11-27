using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 화물은 실제 물리 발판이다. 도착점과 진행 방향을 한 물리 틱에서 결정한다.
    public sealed class AncientOrbitalFreight : MonoBehaviour
    {
        Rigidbody2D body;
        AncientGame game;
        const float Speed = 3.1f;
        float left, right;
        int direction = 1;
        bool brake;
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

        void FixedUpdate()
        {
            if (!game || !body) return;
            if (game.Paused || game.won || brake)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }
            float current = body.position.x;
            float next = Mathf.Clamp(current + direction * Speed * Time.fixedDeltaTime, left, right);
            body.MovePosition(new Vector2(next, body.position.y));
            if (next > left && next < right) return;
            direction = -direction;
        }
    }
}
