using System;

namespace Plugin.AncientCivilization
{
    // 진입할 때 이미 누른 점프 키와 포커스를 잃은 동안의 입력은 건너뛰기에 쓰지 않는다.
    internal sealed class AncientCinematicSkip
    {
        bool released;
        float heldSeconds;

        public float Progress(float duration) => duration > 0 ? Math.Min(1, heldSeconds / duration) : 0;

        public void Suspend()
        {
            released = false;
            heldSeconds = 0;
        }

        public bool Tick(bool held, float seconds, float duration)
        {
            if (!held)
            {
                heldSeconds = 0;
                released = true;
                return false;
            }
            if (!released || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds) || duration <= 0)
                return false;
            // 긴 렌더 정체 한 번으로 확인 입력이 끝나지 않게 한다.
            heldSeconds += Math.Min(seconds, .1f);
            return heldSeconds >= duration;
        }
    }
}
