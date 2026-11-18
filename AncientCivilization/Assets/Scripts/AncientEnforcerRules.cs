using System;

namespace Plugin.AncientCivilization
{
    public enum AncientEnforcerMove { Drop, Bombard, Gun, Waves, Blocks, Echoes, Planes, Missiles, Charge }

    // 체력 경계와 공격 순서는 컷신이나 연출 시간에 의존하지 않는다.
    public static class AncientEnforcerRules
    {
        public const float MaxHealth = 840f;
        public const float SecondPhase = .68f;
        public const float ThirdPhase = .34f;
        public const float CounterChance = .4f;
        public const float ChargeSeconds = 15f;
        public const int Drops = 5, BombardVolleys = 5, MissilesPerVolley = 5, PlaneCount = 5;
        public const int GunBursts = 8, EnhancedPellets = 5;

        static readonly AncientEnforcerMove[][] Moves = {
            new[] { AncientEnforcerMove.Drop, AncientEnforcerMove.Bombard, AncientEnforcerMove.Gun, AncientEnforcerMove.Waves },
            new[] { AncientEnforcerMove.Blocks, AncientEnforcerMove.Echoes, AncientEnforcerMove.Gun, AncientEnforcerMove.Planes },
            new[] { AncientEnforcerMove.Echoes, AncientEnforcerMove.Blocks, AncientEnforcerMove.Missiles, AncientEnforcerMove.Charge }
        };

        public static int Phase(float healthRatio)
        {
            if (healthRatio <= ThirdPhase + .00001f) return 3;
            return healthRatio <= SecondPhase + .00001f ? 2 : 1;
        }

        public static float LimitDamage(float damage, float health, float maxHealth, int phase)
        {
            float gate = phase == 1 ? SecondPhase : phase == 2 ? ThirdPhase : 0;
            return Math.Max(0, Math.Min(damage, health - maxHealth * gate));
        }

        public static AncientEnforcerMove Move(int phase, int index) => Moves[phase - 1][index % Moves[phase - 1].Length];
        public static bool Counter(float roll) => roll >= 0 && roll < CounterChance;
    }
}
