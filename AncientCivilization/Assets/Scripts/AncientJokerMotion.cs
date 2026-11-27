using UnityEngine;

namespace Plugin.AncientCivilization
{
    // 실제 공격 시계에서 읽는 원화 키다. 양수 100번대는 동일 관절로 그린 중간 자세다.
    internal static class AncientJokerMotion
    {
        public const int Inbetween = 100;
        public const int Articulation = 200;
        readonly struct Key
        {
            public readonly float at;
            public readonly int frame;
            public Key(float at, int frame) { this.at = at; this.frame = frame; }
        }

        static int Sample(float time, Key[] keys)
        {
            for (int i = keys.Length - 1; i > 0; --i)
                if (time >= keys[i].at) return keys[i].frame;
            return keys[0].frame;
        }

        // 견착과 재장전은 공격 길이에 맞춘 비율, 격발과 투척 이탈은 실제 발생 뒤의 초 단위다.
        static readonly Key[] shoulder = {
            new(0,100), new(.042f,1), new(.083f,101), new(.133f,2), new(.19f,102), new(.25f,3)
        };
        static readonly Key[] shot = { new(0,3), new(.017f,103), new(.034f,4), new(.073f,104), new(.11f,5) };
        static readonly Key[] releaseStock = { new(0,105), new(.07f,6), new(.13f,100), new(.18f,0) };
        static readonly Key[] magazine = {
            new(0,5), new(.075f,121), new(.135f,7), new(.215f,106), new(.30f,8),
            new(.36f,211), new(.44f,212), new(.49f,107), new(.56f,9), new(.68f,108), new(.73f,213), new(.80f,10), new(.89f,124), new(.98f,0)
        };
        static readonly Key[] grenadeDraw = {
            new(0,210), new(.085f,11), new(.18f,122), new(.32f,12), new(.55f,123), new(.68f,13),
            new(.875f,206), new(.94f,109), new(.977f,207)
        };
        static readonly Key[] grenadeRelease = { new(0,14), new(.042f,110), new(.081f,15), new(.133f,111) };
        // 실제 포물선이 생성된 시각마다 다시 시작한다. 0.22초 안에 회수, 뒤로 젖히기, 다음 이탈까지 잇는다.
        static readonly Key[] volley = {
            new(0,14),new(.024f,110),new(.045f,15),new(.071f,111),new(.092f,208),new(.115f,11),
            new(.137f,209),new(.156f,13),new(.178f,206),new(.195f,109),new(.211f,207)
        };
        static readonly Key[] beamCharge = {
            new(0,114), new(.16f,16), new(.38f,214), new(.59f,17), new(.76f,215), new(.94f,17)
        };
        static readonly Key[] beamRecoil = { new(0,115), new(.043f,18), new(.15f,116), new(.29f,19),
            new(.41f,216), new(.54f,19), new(.71f,217), new(.85f,19), new(1.03f,216), new(1.18f,19), new(1.36f,217) };
        static readonly Key[] idle = { new(0,0), new(.32f,218), new(.57f,219), new(.79f,218), new(.92f,0) };
        static readonly Key[] returnFlight = { new(0,24), new(.74f,112), new(.91f,25) };
        static readonly Key[] land = { new(0,25), new(.082f,113), new(.19f,0) };
        static readonly int[] stride = { 20,117,21,118,22,119,23,120 };

        public static int Aim(float progress, Vector2 direction, ref int elevation)
        {
            int frame = Sample(progress, shoulder);
            float span = Mathf.Max(.01f, Mathf.Abs(direction.x));
            // 조준선은 실시간이다. 바로 경계에 걸친 작은 손떨림으로 상·하체 원화가 왕복하지 않게 한다.
            if (direction.y > span * .29f) elevation = 1;
            else if (direction.y < -span * .29f) elevation = -1;
            else if (Mathf.Abs(direction.y) < span * .21f || elevation * direction.y < 0) elevation = 0;
            if (frame != 3) return frame;
            // 렌즈가 잠기는 마지막 순간에는 호흡도 멎는다. 어느 각도에서나 총구 위치는 같다.
            if (progress >= .35f && progress < .72f)
            {
                int first = elevation > 0 ? 202 : elevation < 0 ? 204 : 200;
                return progress >= .45f && progress < .61f ? first + 1 : first;
            }
            return elevation > 0 ? 26 : elevation < 0 ? 27 : 3;
        }
        public static int Shot(float age, int elevation = 0)
        {
            int frame = Sample(age, shot);
            if (elevation == 0) return frame;
            if (frame == 3) return elevation > 0 ? 26 : 27;
            int first = elevation > 0 ? 220 : 223;
            if (frame == 103 || frame == 104) return first;
            return frame == 4 ? first + 1 : first + 2;
        }
        public static int Idle(float seconds) => Sample((seconds % 2.4f) / 2.4f, idle);
        public static int LowerStock(float age) => Sample(age, releaseStock);
        public static int Reload(float progress) => Sample(progress, magazine);
        public static int DrawGrenade(float progress) => Sample(progress, grenadeDraw);
        public static int ReleaseGrenade(float age) => Sample(age, grenadeRelease);
        public static int RepeatGrenade(float age) => Sample(age, volley);
        public static int Sweep(bool firing, float time) => Sample(time, firing ? beamRecoil : beamCharge);
        public static int Return(float progress) => Sample(progress, returnFlight);
        public static int Land(float age) => Sample(age, land);
        public static int Run(float distance) => stride[Mathf.FloorToInt(Mathf.Abs(distance) * stride.Length / 5.2f) % stride.Length];

        // 원화 총구의 논리 픽셀 변위. 릴레이 빔은 별도 중계기의 실제 발사점에 고정한다.
        public static Vector2 MuzzleKick(int pose)
        {
            switch (pose)
            {
                case 103: case 220: case 223: return new Vector2(-3, 1);
                case 4: case 221: case 224: return new Vector2(-6, 2);
                case 104: return new Vector2(-4, 1);
                case 5: case 222: case 225: return new Vector2(-2, 0);
                default: return Vector2.zero;
            }
        }
    }
}
