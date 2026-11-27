namespace Plugin.PlayerCore
{
    // 구역별 하부 점검로 위치. 장치의 압력판·바닥 센서와 두 출입구는 원래 높이를 유지한다.
    public static class PatrolSocketPaving
    {
        static readonly int[] left = {
            31, 35, 29, 31, 7, 32, 33, 30, 6, 7,
            35, 31, 7, 36, 33, 32, 35, 7, 8, 32,
            35, 6, 34, 33, 35, 33, 35, 35, 35, 33,
            32, 33, 34, 35, 35, 35, 35, 34, 7, 34,
            32, 34, 6, 35, 35, 6, 34, 33, 33, 8
        };
        public const float StepWidth = 1.65f;
        public static int Left(int id) => left[id - 1];
        public static float Width(int id) => 6.4f + ((id - 1) % 3) * 1.15f;
        public static float Depth(int id) => .95f + ((id - 1) / 10 % 3) * .20f;
    }

    public sealed partial class PatrolRoom
    {
        void BuildSocketPaving(int id)
        {
            float start = PatrolSocketPaving.Left(id);
            float end = start + PatrolSocketPaving.Width(id);
            float depth = PatrolSocketPaving.Depth(id);
            float step = PatrolSocketPaving.StepWidth;
            SocketGround(-2, start, 0);
            SocketGround(start, start + step, -depth * .5f);
            SocketGround(start + step, end - step, -depth);
            SocketGround(end - step, end, -depth * .5f);
            SocketGround(end, 50, 0);
        }

        void SocketGround(float left, float right, float top)
        {
            const float baseY = -2.8f;
            float mid = (left + right) * .5f, width = right - left;
            Box("하부 점검로 바닥", mid, (baseY + top) * .5f, width, top - baseY,
                new UnityEngine.Color(.13f,.18f,.23f), true, 0);
            Box("점검로 디딤면", mid, top - .07f, width, .14f,
                UnityEngine.Color.Lerp(Accent, UnityEngine.Color.black, .38f), false, 1);
        }
    }
}
