#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using UnityEditor;

namespace Plugin.AncientCivilization.EditorChecks
{
    public static class GrappleRoomSmokeChecks
    {
        [MenuItem("PLUGIN/P2/안전 바닥 플러그 콘센트 실물 검사")]
        static void Run() => DistrictRouteSmokeChecks.RunSamples();
    }
}
#endif
