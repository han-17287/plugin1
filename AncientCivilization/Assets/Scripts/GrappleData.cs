using UnityEngine;
namespace Plugin.PlayerCore
{
    [CreateAssetMenu(menuName="PLUGIN/Player/Grapple Data")]
    public sealed class GrappleData : ScriptableObject
    {
        [Header("직선 돌진")]
        [Min(.1f)] public float range=14,speed=72;
        [Min(0)] public float launchDelay=.02f,arrivalTolerance=.06f,skin=.03f;
        [Range(0,1)] public float retainedVelocity=.22f;
        [Min(0)] public float arrivalAttackWindow=.3f,assistRadius=1.5f,circleRadius=.34f;
        public bool allowDashCancel=false,allowModeCancel=false;
        public LayerMask solids=1<<8,hazards=1<<10;
        [Header("모양")]
        [Min(.01f)] public float coreWidth=.045f,helixRadius=.07f;
    }
}
