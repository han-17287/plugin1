using UnityEngine;
namespace Plugin.PlayerCore
{
    [CreateAssetMenu(menuName="PLUGIN/Traversal/Difficulty")]
    public sealed class TraversalDifficultyData : ScriptableObject
    {
        [Range(3,5)] public int linkedAnchors=4;
        [Range(5,8)] public int gapTiles=7;
        [Min(0)] public float movingAmplitude=1;
        [Min(.5f)] public float movingPeriod=3.5f;
        public bool optional;
    }
}
