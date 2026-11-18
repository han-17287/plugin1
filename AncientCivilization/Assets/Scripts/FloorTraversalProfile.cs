using UnityEngine;
namespace Plugin.PlayerCore
{
    [CreateAssetMenu(menuName="PLUGIN/Traversal/Floor Profile")]
    public sealed class FloorTraversalProfile : ScriptableObject
    {
        public TraversalFloor floor;
        public Color structureColor=new Color(.15f,.3f,.38f),hazardColor=new Color(.85f,.25f,.2f);
        [Header("생활부 탐조등")]
        [Min(.1f)] public float scanPeriod=4,scanWidth=1.4f,scanHold=.7f;
        [Header("행정구 셔터")]
        [Min(.1f)] public float gatePeriod=3,gateOpen=1.4f,gateWarning=.35f;
        [Header("거주구 환기류")]
        [Min(0)] public float ventLift=12;
        [Header("최상층 시간 앵커")]
        [Min(.1f)] public float socketPeriod=3.6f,socketOpen=2.4f;
    }
}
