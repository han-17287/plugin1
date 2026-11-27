using UnityEngine;

namespace Plugin.PlayerCore
{
    [CreateAssetMenu(menuName = "PLUGIN/Traversal/층별 플러그 설비")]
    public sealed class PlugDistrictSettings : ScriptableObject
    {
        [Header("전력 유지 시간 (초)")]
        [Min(.25f)] public float shutterSeconds = 5;
        [Min(.25f)] public float sequenceSeconds = 6;
        [Min(.25f)] public float relaySeconds = 5;
        [Min(.25f)] public float releaseHold = 4;
        [Header("설비 동작")]
        [Min(.05f)] public float canopyInterval = .28f;
        [Min(.25f)] public float liftSpeed = 2.6f;
        [Min(.05f)] public float unfoldSeconds = .30f;
    }
}
