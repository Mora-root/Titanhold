using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Session
{
    [Serializable]
    public sealed class RunDifficultyRewardDefinition
    {
        [SerializeField] private string difficultyId;
        [SerializeField, Min(1)] private int multiplierPercent = 100;

        public RunDifficultyRewardDefinition(
            string configuredDifficultyId,
            int configuredMultiplierPercent)
        {
            difficultyId = configuredDifficultyId?.Trim() ?? string.Empty;
            multiplierPercent = configuredMultiplierPercent;
        }

        public string DifficultyId => difficultyId;
        public int MultiplierPercent => multiplierPercent;
    }

    [CreateAssetMenu(
        menuName = "Titanhold/Run/Conclusion Reward Definition",
        fileName = "RunConclusionRewards")]
    public sealed class RunConclusionRewardDefinition : ScriptableObject
    {
        [SerializeField, Min(0)]
        private int characterExperiencePerCompletedRound = 100;
        [SerializeField, Min(0)] private int crystalsPerCompletedRound = 5;
        [SerializeField, Min(0)]
        private int victoryCharacterExperienceBonus = 200;
        [SerializeField, Min(0)] private int victoryCrystalBonus = 10;
        [SerializeField]
        private RunDifficultyRewardDefinition[] difficultyRewards =
            Array.Empty<RunDifficultyRewardDefinition>();

        public bool IsValid => TryBuildPolicy(out _, out _);

        public bool TryBuildPolicy(
            out RunConclusionRewardPolicy policy,
            out string error)
        {
            policy = null;
            error = string.Empty;
            if (characterExperiencePerCompletedRound < 0 ||
                crystalsPerCompletedRound < 0 ||
                victoryCharacterExperienceBonus < 0 ||
                victoryCrystalBonus < 0)
            {
                error = "Reward amounts cannot be negative.";
                return false;
            }

            if (difficultyRewards == null || difficultyRewards.Length == 0)
            {
                error = "At least one difficulty reward is required.";
                return false;
            }

            Dictionary<string, int> multipliers =
                new(StringComparer.Ordinal);
            for (int i = 0; i < difficultyRewards.Length; i++)
            {
                RunDifficultyRewardDefinition definition =
                    difficultyRewards[i];
                string difficultyId =
                    definition?.DifficultyId?.Trim() ?? string.Empty;
                if (definition == null ||
                    difficultyId.Length == 0 ||
                    definition.MultiplierPercent <= 0)
                {
                    error = $"Difficulty reward {i} is invalid.";
                    return false;
                }

                if (!multipliers.TryAdd(
                        difficultyId,
                        definition.MultiplierPercent))
                {
                    error = $"Difficulty '{difficultyId}' occurs more than once.";
                    return false;
                }
            }

            RunConclusionRewardConfiguration configuration = new(
                characterExperiencePerCompletedRound,
                crystalsPerCompletedRound,
                victoryCharacterExperienceBonus,
                victoryCrystalBonus);
            policy = new RunConclusionRewardPolicy(
                configuration,
                multipliers);
            return true;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int experiencePerRound,
            int crystalsPerRound,
            int victoryExperienceBonus,
            int configuredVictoryCrystalBonus,
            IReadOnlyList<RunDifficultyRewardDefinition> difficulties)
        {
            characterExperiencePerCompletedRound = experiencePerRound;
            crystalsPerCompletedRound = crystalsPerRound;
            victoryCharacterExperienceBonus = victoryExperienceBonus;
            victoryCrystalBonus = configuredVictoryCrystalBonus;

            int count = difficulties?.Count ?? 0;
            difficultyRewards = new RunDifficultyRewardDefinition[count];
            for (int i = 0; i < count; i++)
                difficultyRewards[i] = difficulties[i];
        }
#endif

        private void OnValidate()
        {
            characterExperiencePerCompletedRound = Mathf.Max(
                0,
                characterExperiencePerCompletedRound);
            crystalsPerCompletedRound = Mathf.Max(
                0,
                crystalsPerCompletedRound);
            victoryCharacterExperienceBonus = Mathf.Max(
                0,
                victoryCharacterExperienceBonus);
            victoryCrystalBonus = Mathf.Max(0, victoryCrystalBonus);
        }
    }
}
