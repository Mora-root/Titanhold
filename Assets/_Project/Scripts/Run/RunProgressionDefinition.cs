using System;
using UnityEngine;

namespace Titanhold.Run
{
    [CreateAssetMenu(
        fileName = "RunProgression",
        menuName = "Titanhold/Run/Progression Definition")]
    public sealed class RunProgressionDefinition : ScriptableObject
    {
        public const int MaximumSupportedLevel = 1000;

        [SerializeField, Range(2, MaximumSupportedLevel)]
        private int maximumLevel = 20;
        [SerializeField, Min(1)] private int baseExperienceToNextLevel = 100;
        [SerializeField, Min(0)] private int experienceIncreasePerLevel = 50;

        public int MaximumLevel => maximumLevel;
        public int BaseExperienceToNextLevel => baseExperienceToNextLevel;
        public int ExperienceIncreasePerLevel => experienceIncreasePerLevel;
        public bool IsValid => TryBuildCurve(out _, out _);

        public bool TryBuildCurve(
            out RunExperienceCurve curve,
            out string error)
        {
            curve = null;
            error = string.Empty;
            if (maximumLevel < 2)
            {
                error = "Run progression requires at least two levels.";
                return false;
            }

            if (maximumLevel > MaximumSupportedLevel)
            {
                error =
                    $"Run progression supports at most {MaximumSupportedLevel} levels.";
                return false;
            }

            if (baseExperienceToNextLevel <= 0)
            {
                error = "Base run experience requirement must be positive.";
                return false;
            }

            if (experienceIncreasePerLevel < 0)
            {
                error = "Run experience increase per level cannot be negative.";
                return false;
            }

            int[] requirements = new int[maximumLevel - 1];
            for (int levelIndex = 0;
                 levelIndex < requirements.Length;
                 levelIndex++)
            {
                long requirement =
                    baseExperienceToNextLevel +
                    (long)experienceIncreasePerLevel * levelIndex;
                if (requirement > int.MaxValue)
                {
                    error =
                        $"Run experience requirement overflows at level {levelIndex + 1}.";
                    return false;
                }

                requirements[levelIndex] = (int)requirement;
            }

            curve = new RunExperienceCurve(requirements);
            return true;
        }

        public RunExperienceCurve BuildCurve()
        {
            if (!TryBuildCurve(out RunExperienceCurve curve, out string error))
                throw new InvalidOperationException(error);

            return curve;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int configuredMaximumLevel,
            int configuredBaseExperience,
            int configuredExperienceIncrease)
        {
            maximumLevel = configuredMaximumLevel;
            baseExperienceToNextLevel = configuredBaseExperience;
            experienceIncreasePerLevel = configuredExperienceIncrease;
        }
#endif
    }
}
