using System;

namespace Titanhold.Run
{
    public sealed class RunFlowConfiguration
    {
        public RunFlowConfiguration(
            float maxThreat,
            int instabilityPointsPerLevel,
            float enemyHealthBonusPerRound,
            float enemyDamageBonusPerRound,
            float assaultHealthBonusPerLevel,
            float assaultDamageBonusPerLevel,
            int regularRoundCount = 3,
            int startingRound = 1)
        {
            if (!IsFinitePositive(maxThreat))
                throw new ArgumentOutOfRangeException(nameof(maxThreat));

            if (instabilityPointsPerLevel <= 0)
                throw new ArgumentOutOfRangeException(nameof(instabilityPointsPerLevel));

            if (!IsFiniteNonNegative(assaultHealthBonusPerLevel))
                throw new ArgumentOutOfRangeException(nameof(assaultHealthBonusPerLevel));

            if (!IsFiniteNonNegative(enemyHealthBonusPerRound))
                throw new ArgumentOutOfRangeException(nameof(enemyHealthBonusPerRound));

            if (!IsFiniteNonNegative(enemyDamageBonusPerRound))
                throw new ArgumentOutOfRangeException(nameof(enemyDamageBonusPerRound));

            if (!IsFiniteNonNegative(assaultDamageBonusPerLevel))
                throw new ArgumentOutOfRangeException(nameof(assaultDamageBonusPerLevel));

            if (regularRoundCount <= 0 || regularRoundCount == int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(regularRoundCount));

            if (startingRound <= 0 || startingRound > regularRoundCount + 1)
                throw new ArgumentOutOfRangeException(nameof(startingRound));

            MaxThreat = maxThreat;
            InstabilityPointsPerLevel = instabilityPointsPerLevel;
            EnemyHealthBonusPerRound = enemyHealthBonusPerRound;
            EnemyDamageBonusPerRound = enemyDamageBonusPerRound;
            AssaultHealthBonusPerLevel = assaultHealthBonusPerLevel;
            AssaultDamageBonusPerLevel = assaultDamageBonusPerLevel;
            RegularRoundCount = regularRoundCount;
            StartingRound = startingRound;
        }

        public float MaxThreat { get; }
        public int InstabilityPointsPerLevel { get; }
        public float EnemyHealthBonusPerRound { get; }
        public float EnemyDamageBonusPerRound { get; }
        public float AssaultHealthBonusPerLevel { get; }
        public float AssaultDamageBonusPerLevel { get; }
        public int RegularRoundCount { get; }
        public int FinalRoundNumber => RegularRoundCount + 1;
        public int StartingRound { get; }

        public static RunFlowConfiguration CreateVerticalSliceDefaults()
        {
            return new RunFlowConfiguration(
                maxThreat: 100f,
                instabilityPointsPerLevel: 10,
                enemyHealthBonusPerRound: 0.20f,
                enemyDamageBonusPerRound: 0.10f,
                assaultHealthBonusPerLevel: 0.10f,
                assaultDamageBonusPerLevel: 0.05f,
                regularRoundCount: 3);
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
