using System;

namespace Titanhold.Run
{
    public static class RunExperienceRewardCalculator
    {
        public static bool TryCalculate(
            long baseExperience,
            float multiplier,
            out int experience)
        {
            experience = 0;
            if (baseExperience <= 0 ||
                multiplier <= 0f ||
                float.IsNaN(multiplier) ||
                float.IsInfinity(multiplier))
            {
                return false;
            }

            double scaledExperience = baseExperience * (double)multiplier;
            if (scaledExperience > int.MaxValue)
                return false;

            experience = (int)Math.Round(
                scaledExperience,
                MidpointRounding.AwayFromZero);
            return experience > 0;
        }
    }
}
