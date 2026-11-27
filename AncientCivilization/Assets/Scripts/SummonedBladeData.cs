using UnityEngine;

namespace Plugin.PlayerCore
{
    [CreateAssetMenu(menuName = "PLUGIN/전투/소환형 전구 블레이드")]
    public sealed class SummonedBladeData : ScriptableObject
    {
        [Header("소환과 설치")]
        [Min(0)] public int summonCost = 20;
        [Min(.05f)] public float telegraphMin = 1.2f, telegraphMax = 2f;
        [Min(.1f)] public float minDistance = 4, maxDistance = 12, previousClearance = 3;
        [Min(.1f)] public float deckMinDistance = 1.2f;
        [Min(.1f)] public float plugRange = 16, groundMargin = .65f, handleHeight = 1.48f;
        [Min(.1f)] public float fallHeight = 5, fallSpeed = 13;
        [Min(0)] public float installedLifetime = 20, fieldLifetime = 15, cooldown = 12, overloadExtraCooldown = 2;
        [Range(4,64)] public int positionAttempts = 24;
        public LayerMask groundMask = 1 << 8, dangerMask = 1 << 10;

        [Header("피해와 보상")]
        [Min(0)] public float impactRadius = 1.2f, impactDamage = 80, fieldRadius = 2, fieldPerSecond = 8;
        [Min(0)] public float zipBase = 100, zipPerTile = 12, zipCap = 220;
        [Min(0)] public int comboFirst = 35, comboSecond = 35, comboThird = 55;
        [Min(0)] public int perfectCharge = 8;
        [Min(0)] public float perfectInvulnerability = .5f;
        [Min(0)] public float overloadPerParticle = 6, overloadCap = 300, overloadRadius = 4;
    }
}
