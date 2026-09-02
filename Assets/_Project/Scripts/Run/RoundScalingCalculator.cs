using System;

namespace Titanhold.Run
{
    public sealed class RoundScalingCalculator
    {
        private readonly float healthBonusPerRound;
        private readonly float damageBonusPerRound;

        public RoundScalingCalculator(
            float healthBonusPerRound,
            float damageBonusPerRound)
        {
            if (!IsFiniteNonNegative(healthBonusPerRound))
                throw new ArgumentOutOfRangeException(nameof(healthBonusPerRound));

            if (!IsFiniteNonNegative(damageBonusPerRound))
                throw new ArgumentOutOfRangeException(nameof(damageBonusPerRound));

            this.healthBonusPerRound = healthBonusPerRound;
            this.damageBonusPerRound = damageBonusPerRound;
        }

        public EnemyScalingSnapshot CreateSnapshot(int roundNumber)
        {
            if (roundNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(roundNumber));

            int completedRounds = roundNumber - 1;
            return new EnemyScalingSnapshot(
                roundNumber,
                CalculateMultiplier(completedRounds, healthBonusPerRound),
                CalculateMultiplier(completedRounds, damageBonusPerRound));
        }

        private static float CalculateMultiplier(int count, float bonus)
        {
            double multiplier = 1d + (double)count * bonus;
            return multiplier >= float.MaxValue ? float.MaxValue : (float)multiplier;
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
