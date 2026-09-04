using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public sealed class RunExperienceCurve
    {
        private readonly int[] experienceRequirements;

        public RunExperienceCurve(
            IReadOnlyList<int> experienceRequirements)
        {
            if (experienceRequirements == null)
            {
                throw new ArgumentNullException(
                    nameof(experienceRequirements));
            }

            this.experienceRequirements =
                new int[experienceRequirements.Count];
            for (int i = 0; i < experienceRequirements.Count; i++)
            {
                int requirement = experienceRequirements[i];
                if (requirement <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(experienceRequirements),
                        $"Run level {i + 1} requires a positive amount of experience.");
                }

                this.experienceRequirements[i] = requirement;
            }
        }

        public int MaximumLevel => experienceRequirements.Length + 1;

        public bool TryGetRequirement(
            int currentLevel,
            out int experienceRequired)
        {
            if (currentLevel < 1 || currentLevel >= MaximumLevel)
            {
                experienceRequired = 0;
                return false;
            }

            experienceRequired =
                experienceRequirements[currentLevel - 1];
            return true;
        }
    }
}
