using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed partial class AncientEnforcerVisuals
    {
        const float BridgePixelsPerUnit = 195f;
        static readonly Sprite[] bridgeBodies = new Sprite[8];
        static readonly Sprite[] bridgeOptics = new Sprite[8];

        // 네 쌍의 자세 사이에 두 컷씩 있다. 역방향에서는 같은 필름을 거꾸로 읽는다.
        // 끝점과 비연속 자세(예: 퇴장)는 기존 원화를 그대로 사용한다.
        // 서로 마주보는 키포즈는 연결 원화가 있는 옆 자세를 경유한다. 발사 시간은 늘리지 않는다.
        static void SelectLeg(ref int from, ref int to, ref float progress)
        {
            if (from < 0 || from > 3 || to < 0 || to > 3 || Mathf.Abs(to - from) != 2) return;
            int middle = from == 2 || to == 2 ? 1 : 0;
            if (progress < .5f) { to = middle; progress *= 2; }
            else { from = middle; progress = (progress - .5f) * 2; }
        }

        internal static int DisplayedPose(int from, int to, float progress)
        {
            SelectLeg(ref from, ref to, ref progress);
            return progress < .5f ? from : to;
        }

        static int BridgeIndex(int from, int to, float progress)
        {
            SelectLeg(ref from, ref to, ref progress);
            if (from < 0 || from > 3 || to < 0 || to > 3 ||
                !(progress > .15f && progress < .85f)) return -1;
            if ((from + 1) % 4 == to) return from * 2 + (progress < .5f ? 0 : 1);
            if ((to + 1) % 4 == from) return to * 2 + (progress > .5f ? 0 : 1);
            return -1;
        }

        internal static void PrewarmTransitions()
        {
            // 예고 동작이 시작되기 전에 원화와 조명 레이어를 한 번만 메모리에 준비한다.
            for (int from = 0; from < 4; from++)
            {
                int to = (from + 1) % 4;
                TransitionSprite(from, to, .33f);
                TransitionSprite(from, to, .67f);
                TransitionOpticsSprite(from, to, .33f);
                TransitionOpticsSprite(from, to, .67f);
            }
        }

        public static Sprite TransitionSprite(int from, int to, float progress)
        {
            int frame = BridgeIndex(from, to, progress);
            if (frame < 0) return null;
            if (bridgeBodies[frame]) return bridgeBodies[frame];
            Rect cell = AncientEnforcerBridgeAtlas.Cells[frame];
            Vector2 foot = AncientEnforcerBridgeAtlas.Feet[frame];
            Vector2 pivot = new Vector2((foot.x - cell.x) / cell.width, (cell.yMax - foot.y) / cell.height);
            return bridgeBodies[frame] = AncientEnforcerCostume.Articulate(AncientArt.Slice("RecordEnforcerBridge",
                "기록 집행관 전환 " + frame, cell, BridgePixelsPerUnit, pivot, false));
        }

        public static Sprite TransitionOpticsSprite(int from, int to, float progress)
        {
            int frame = BridgeIndex(from, to, progress);
            return frame >= 0 ? BridgeOpticsSprite(frame) : null;
        }

        static Sprite BridgeOpticsSprite(int frame)
        {
            if (bridgeOptics[frame]) return bridgeOptics[frame];
            return bridgeOptics[frame] = AncientEnforcerCostume.Articulate(AncientArt.Slice("RecordEnforcerBridgeOptics",
                "집행관 전환 광학 장비 " + frame, AncientEnforcerBridgeAtlas.OpticCells[frame], BridgePixelsPerUnit, new Vector2(.5f, .5f), false));
        }

        // 재생한 한 컷을 기준으로 조명, 카메라, 서류 읽기 선의 좌표를 함께 고른다.
        static int BridgeFrameOf(Sprite body)
        {
            if (!body) return -1;
            for (int frame = 0; frame < bridgeBodies.Length; frame++)
                if (bridgeBodies[frame] == body) return frame;
            return -1;
        }
    }
}
