using System;

namespace Titanhold.Run
{
    public sealed class RunFlowConfiguration
    {
        public RunFlowConfiguration(
            float maxThreat,
            int instabilityPointsPerLevel,
            float assaultHealthBonusPerLevel,
            float assaultDamageBonusPerLevel,
            int startingRound = 1)
        {
            if (!IsFinitePositive(maxThreat))
                throw new ArgumentOutOfRangeException(nameof(maxThreat));

            if (instabilityPointsPerLevel <= 0)
                throw new ArgumentOutOfRangeException(nameof(instabilityPointsPerLevel));

            if (!IsFiniteNonNegative(assaultHealthBonusPerLevel))
                throw new ArgumentOutOfRangeException(nameof(assaultHealthBonusPerLevel));

            if (!IsFiniteNonNegative(assaultDamageBonusPerLevel))
                throw new ArgumentOutOfRangeException(nameof(assaultDamageBonusPerLevel));

            if (startingRound <= 0)
                throw new ArgumentOutOfRangeException(nameof(startingRound));

            MaxThreat = maxThreat;
            InstabilityPointsPerLevel = instabilityPointsPerLevel;
            AssaultHealthBonusPerLevel = assaultHealthBonusPerLevel;
            AssaultDamageBonusPerLevel = assaultDamageBonusPerLevel;
            StartingRound = startingRound;
        }

        public float MaxThreat { get; }
        public int InstabilityPointsPerLevel { get; }
        public float AssaultHealthBonusPerLevel { get; }
        public float AssaultDamageBonusPerLevel { get; }
        public int StartingRound { get; }

        public static RunFlowConfiguration CreateVerticalSliceDefaults()
        {
            return new RunFlowConfiguration(
                maxThreat: 100f,
                instabilityPointsPerLevel: 10,
                assaultHealthBonusPerLevel: 0.10f,
                assaultDamageBonusPerLevel: 0.05f);
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
