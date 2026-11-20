using UnityEngine;

namespace Plugin.AncientCivilization
{
    public static class AncientJokerDuelArt
    {
        // ♣ 단검 결투. 소총은 네 자세 모두 등에 메고, 기존 저격수와 같은 픽셀 밀도를 사용한다.
        static readonly Rect[] bounds = {
            new Rect(22, 1, 47, 60), new Rect(111, 2, 67, 59),
            new Rect(22, 66, 47, 60), new Rect(115, 67, 62, 59)
        };
        static readonly float[] pelvisX = { 44, 141, 44, 144 };
        static readonly Sprite[] frames = new Sprite[4];

        public static Sprite Frame(int pose)
        {
            pose = Mathf.Clamp(pose, 0, frames.Length - 1);
            if (frames[pose]) return frames[pose];
            Rect crop = bounds[pose];
            // 각 칸마다 발바닥 아래 투명 픽셀 한 줄을 둔다.
            Vector2 pivot = new Vector2((pelvisX[pose] - crop.x) / crop.width, 1f / crop.height);
            frames[pose] = AncientArt.Slice("JokerClubDuel", "joker-club-duel-" + pose,
                crop, AncientBossAtlas.Ppu[4] / 8f, pivot, false);
            return frames[pose];
        }
    }
}
