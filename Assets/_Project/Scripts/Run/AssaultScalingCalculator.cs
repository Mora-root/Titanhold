using System;

namespace Titanhold.Run
{
    public sealed class AssaultScalingCalculator
    {
        private readonly float healthBonusPerLevel;
        private readonly float damageBonusPerLevel;

        public AssaultScalingCalculator(float healthBonusPerLevel, float damageBonusPerLevel)
        {
            if (!IsFiniteNonNegative(healthBonusPerLevel))
                throw new ArgumentOutOfRangeException(nameof(healthBonusPerLevel));

            if (!IsFiniteNonNegative(damageBonusPerLevel))
                throw new ArgumentOutOfRangeException(nameof(damageBonusPerLevel));

            this.healthBonusPerLevel = healthBonusPerLevel;
            this.damageBonusPerLevel = damageBonusPerLevel;
        }

        public AssaultScalingSnapshot CreateSnapshot(RiftInstabilityState instability)
        {
            if (instability == null)
                throw new ArgumentNullException(nameof(instability));

            int level = instability.Level;
            return new AssaultScalingSnapshot(
                instability.Points,
                level,
                CalculateMultiplier(level, healthBonusPerLevel),
                CalculateMultiplier(level, damageBonusPerLevel));
        }

        private static float CalculateMultiplier(int level, float bonusPerLevel)
        {
            double multiplier = 1d + (double)level * bonusPerLevel;
            return multiplier >= float.MaxValue ? float.MaxValue : (float)multiplier;
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
