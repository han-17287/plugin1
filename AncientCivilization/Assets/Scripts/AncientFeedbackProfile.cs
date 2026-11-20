using UnityEngine;
using UnityEngine.Audio;

namespace Plugin.AncientCivilization
{
    // 장비의 판정이나 입력 창과 독립적으로 청각·화면 피드백을 조절한다.
    public enum AncientEquipmentCue { Forge, MetalFall, Lens, Sweep, Counter, Overload, ShortCircuit, Contact, Shatter, Heartbeat, UI, JokerCharge }

    [CreateAssetMenu(menuName = "PLUGIN/전투 피드백")]
    public sealed class AncientFeedbackProfile : ScriptableObject
    {
        [Header("화면")]
        [Range(0, .12f)] public float contactSlowSeconds = .022f;
        [Range(0, .12f)] public float heavySlowSeconds = .045f;
        [Range(.05f, 1)] public float contactTimeScale = .12f;
        [Range(0, 2)] public float shakeScale = 1;
        [Range(0, .08f)] public float impactZoom = .014f;
        [Range(.05f, .4f)] public float zoomRelease = .16f;
        [Range(0, 1)] public float damageFlash = .65f;
        [Range(0, 1)] public float reducedShake = .15f;
        [Range(0, 1)] public float reducedFlash = .12f;
        [Range(0, .5f)] public float lowHealthRatio = .25f;
        [Header("음향")]
        [Range(0, 1)] public float effectsVolume = .22f;
        [Range(0, 1)] public float warningVolume = .33f;
        [Range(0, 1)] public float uiVolume = .16f;
        [Range(0, 1)] public float scoreVolume = .42f;
        [Range(0, 1)] public float dialogueScoreVolume = .19f;
        [Range(.2f, 3)] public float heartbeatInterval = 1.05f;
        [Range(0, .15f)] public float pitchVariation = .035f;
        [Range(0, 1)] public float duckMusicTo = .43f;
        [Range(0, 1)] public float duckSeconds = .26f;
        [Range(.02f, .3f)] public float pickupInterval = .065f;
        [Range(.1f, 1)] public float pickupChainTimeout = .48f;
        [Header("필드 믹서")]
        public AudioMixerGroup effects, music, ui;
    }
}
