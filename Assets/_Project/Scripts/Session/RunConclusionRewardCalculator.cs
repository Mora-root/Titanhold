using System;
using System.Collections.Generic;

namespace Titanhold.Session
{
    public sealed class RunConclusionRewardConfiguration
    {
        public RunConclusionRewardConfiguration(
            int characterExperiencePerCompletedRound,
            int crystalsPerCompletedRound,
            int victoryCharacterExperienceBonus,
            int victoryCrystalBonus)
        {
            if (characterExperiencePerCompletedRound < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(characterExperiencePerCompletedRound));
            }

            if (crystalsPerCompletedRound < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(crystalsPerCompletedRound));
            }

            if (victoryCharacterExperienceBonus < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(victoryCharacterExperienceBonus));
            }

            if (victoryCrystalBonus < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(victoryCrystalBonus));
            }

            CharacterExperiencePerCompletedRound =
                characterExperiencePerCompletedRound;
            CrystalsPerCompletedRound = crystalsPerCompletedRound;
            VictoryCharacterExperienceBonus =
                victoryCharacterExperienceBonus;
            VictoryCrystalBonus = victoryCrystalBonus;
        }

        public int CharacterExperiencePerCompletedRound { get; }
        public int CrystalsPerCompletedRound { get; }
        public int VictoryCharacterExperienceBonus { get; }
        public int VictoryCrystalBonus { get; }
    }

    public enum RunConclusionRewardError
    {
        None,
        InvalidRunResult,
        InvalidDifficulty,
        InvalidDifficultyMultiplier,
        RewardOverflow
    }

    public readonly struct RunConclusionRewardResult
    {
        private RunConclusionRewardResult(
            bool success,
            RunConclusionRewardError error,
            int characterExperience,
            int crystals)
        {
            Success = success;
            Error = error;
            CharacterExperience = characterExperience;
            Crystals = crystals;
        }

        public bool Success { get; }
        public RunConclusionRewardError Error { get; }
        public int CharacterExperience { get; }
        public int Crystals { get; }

        public static RunConclusionRewardResult Succeeded(
            int characterExperience,
            int crystals)
        {
            return new RunConclusionRewardResult(
                true,
                RunConclusionRewardError.None,
                characterExperience,
                crystals);
        }

        public static RunConclusionRewardResult Failed(
            RunConclusionRewardError error)
        {
            return new RunConclusionRewardResult(false, error, 0, 0);
        }
    }

    public sealed class RunConclusionRewardCalculator
    {
        private readonly RunConclusionRewardConfiguration configuration;

        public RunConclusionRewardCalculator(
            RunConclusionRewardConfiguration configuration)
        {
            this.configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
        }

        public RunConclusionRewardResult Calculate(
            RunResultSummary summary,
            int difficultyMultiplierPercent)
        {
            if (summary == null || !summary.IsValid)
            {
                return RunConclusionRewardResult.Failed(
                    RunConclusionRewardError.InvalidRunResult);
            }

            if (difficultyMultiplierPercent <= 0)
            {
                return RunConclusionRewardResult.Failed(
                    RunConclusionRewardError.InvalidDifficultyMultiplier);
            }

            long characterExperience =
                (long)configuration.CharacterExperiencePerCompletedRound *
                summary.CompletedRoundCount;
            long crystals =
                (long)configuration.CrystalsPerCompletedRound *
                summary.CompletedRoundCount;

            if (summary.Outcome == RunOutcome.Victory)
            {
                characterExperience +=
                    configuration.VictoryCharacterExperienceBonus;
                crystals += configuration.VictoryCrystalBonus;
            }

            characterExperience = ApplyMultiplier(
                characterExperience,
                difficultyMultiplierPercent);
            crystals = ApplyMultiplier(
                crystals,
                difficultyMultiplierPercent);

            if (characterExperience > int.MaxValue ||
                crystals > int.MaxValue)
            {
                return RunConclusionRewardResult.Failed(
                    RunConclusionRewardError.RewardOverflow);
            }

            return RunConclusionRewardResult.Succeeded(
                (int)characterExperience,
                (int)crystals);
        }

        private static long ApplyMultiplier(
            long value,
            int multiplierPercent)
        {
            if (value == 0L)
                return 0L;

            if (value > long.MaxValue / multiplierPercent)
                return long.MaxValue;

            return value * multiplierPercent / 100L;
        }
    }

    public sealed class RunConclusionRewardPolicy
    {
        private readonly RunConclusionRewardCalculator calculator;
        private readonly Dictionary<string, int> difficultyMultipliers;

        public RunConclusionRewardPolicy(
            RunConclusionRewardConfiguration configuration,
            IReadOnlyDictionary<string, int> configuredDifficultyMultipliers)
        {
            calculator = new RunConclusionRewardCalculator(configuration);
            if (configuredDifficultyMultipliers == null ||
                configuredDifficultyMultipliers.Count == 0)
            {
                throw new ArgumentException(
                    "At least one difficulty multiplier is required.",
                    nameof(configuredDifficultyMultipliers));
            }

            difficultyMultipliers = new Dictionary<string, int>(
                StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> entry in
                     configuredDifficultyMultipliers)
            {
                string difficultyId = entry.Key?.Trim() ?? string.Empty;
                if (difficultyId.Length == 0 || entry.Value <= 0 ||
                    !difficultyMultipliers.TryAdd(difficultyId, entry.Value))
                {
                    throw new ArgumentException(
                        "Difficulty reward multipliers are invalid.",
                        nameof(configuredDifficultyMultipliers));
                }
            }
        }

        public RunConclusionRewardResult Calculate(
            RunResultSummary summary,
            string difficultyId)
        {
            string normalizedId = difficultyId?.Trim() ?? string.Empty;
            if (!difficultyMultipliers.TryGetValue(
                    normalizedId,
                    out int multiplierPercent))
            {
                return RunConclusionRewardResult.Failed(
                    RunConclusionRewardError.InvalidDifficulty);
            }

            return calculator.Calculate(summary, multiplierPercent);
        }
    }
}
