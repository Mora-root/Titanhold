using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public readonly struct RunRoundBalanceSnapshot
    {
        public RunRoundBalanceSnapshot(
            int roundNumber,
            float maxThreat,
            float enemyHealthMultiplier,
            float enemyDamageMultiplier,
            float experienceMultiplier)
        {
            if (roundNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(roundNumber));

            if (!IsFinitePositive(maxThreat))
                throw new ArgumentOutOfRangeException(nameof(maxThreat));

            if (!IsFinitePositive(enemyHealthMultiplier))
                throw new ArgumentOutOfRangeException(
                    nameof(enemyHealthMultiplier));

            if (!IsFinitePositive(enemyDamageMultiplier))
                throw new ArgumentOutOfRangeException(
                    nameof(enemyDamageMultiplier));

            if (!IsFinitePositive(experienceMultiplier))
                throw new ArgumentOutOfRangeException(
                    nameof(experienceMultiplier));

            RoundNumber = roundNumber;
            MaxThreat = maxThreat;
            EnemyScaling = new EnemyScalingSnapshot(
                roundNumber,
                enemyHealthMultiplier,
                enemyDamageMultiplier);
            ExperienceMultiplier = experienceMultiplier;
        }

        public int RoundNumber { get; }
        public float MaxThreat { get; }
        public EnemyScalingSnapshot EnemyScaling { get; }
        public float ExperienceMultiplier { get; }
        public bool IsValid =>
            RoundNumber > 0 &&
            IsFinitePositive(MaxThreat) &&
            IsFinitePositive(EnemyScaling.HealthMultiplier) &&
            IsFinitePositive(EnemyScaling.DamageMultiplier) &&
            IsFinitePositive(ExperienceMultiplier);

        private static bool IsFinitePositive(float value)
        {
            return value > 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }
    }

    public interface IRunRoundBalanceResolver
    {
        bool TryResolve(
            int roundNumber,
            out RunRoundBalanceSnapshot snapshot);
    }

    public sealed class LinearRunRoundBalanceResolver :
        IRunRoundBalanceResolver
    {
        private readonly float maxThreat;
        private readonly float experienceBonusPerRound;
        private readonly RoundScalingCalculator scaling;

        public LinearRunRoundBalanceResolver(
            float maxThreat,
            float enemyHealthBonusPerRound,
            float enemyDamageBonusPerRound,
            float experienceBonusPerRound = 0f)
        {
            if (!IsFinitePositive(maxThreat))
                throw new ArgumentOutOfRangeException(nameof(maxThreat));

            if (!IsFiniteNonNegative(experienceBonusPerRound))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(experienceBonusPerRound));
            }

            this.maxThreat = maxThreat;
            this.experienceBonusPerRound = experienceBonusPerRound;
            scaling = new RoundScalingCalculator(
                enemyHealthBonusPerRound,
                enemyDamageBonusPerRound);
        }

        public bool TryResolve(
            int roundNumber,
            out RunRoundBalanceSnapshot snapshot)
        {
            snapshot = default;
            if (roundNumber <= 0)
                return false;

            EnemyScalingSnapshot enemyScaling =
                scaling.CreateSnapshot(roundNumber);
            snapshot = new RunRoundBalanceSnapshot(
                roundNumber,
                maxThreat,
                enemyScaling.HealthMultiplier,
                enemyScaling.DamageMultiplier,
                CalculateMultiplier(
                    roundNumber - 1,
                    experienceBonusPerRound));
            return true;
        }

        private static float CalculateMultiplier(int count, float bonus)
        {
            double multiplier = 1d + (double)count * bonus;
            return multiplier >= float.MaxValue
                ? float.MaxValue
                : (float)multiplier;
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return value >= 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }
    }

    public sealed class RunRoundBalanceTable : IRunRoundBalanceResolver
    {
        private readonly Dictionary<int, RunRoundBalanceSnapshot> snapshots;

        private RunRoundBalanceTable(
            Dictionary<int, RunRoundBalanceSnapshot> snapshots)
        {
            this.snapshots = snapshots;
        }

        public static bool TryCreate(
            IReadOnlyList<RunRoundBalanceSnapshot> entries,
            out RunRoundBalanceTable table,
            out string error)
        {
            table = null;
            error = string.Empty;
            if (entries == null || entries.Count == 0)
            {
                error = "A round balance table requires at least one entry.";
                return false;
            }

            Dictionary<int, RunRoundBalanceSnapshot> resolved = new();
            for (int i = 0; i < entries.Count; i++)
            {
                RunRoundBalanceSnapshot entry = entries[i];
                if (!entry.IsValid)
                {
                    error =
                        $"Round balance entry {i} is invalid.";
                    return false;
                }

                if (!resolved.TryAdd(entry.RoundNumber, entry))
                {
                    error =
                        $"Round {entry.RoundNumber} occurs more than once in the balance table.";
                    return false;
                }
            }

            table = new RunRoundBalanceTable(resolved);
            return true;
        }

        public bool TryResolve(
            int roundNumber,
            out RunRoundBalanceSnapshot snapshot)
        {
            return snapshots.TryGetValue(roundNumber, out snapshot);
        }
    }
}
