using System;
using System.Collections.Generic;

namespace Titanhold.Combat
{
    public sealed class CombatExecutionReport
    {
        private readonly DamageTargetResolution[] resolutions;

        public CombatExecutionReport(
            CombatExecutionId executionId,
            IReadOnlyList<DamageTargetResolution> resolutions)
        {
            if (!executionId.IsValid)
                throw new ArgumentException("A combat execution report requires a valid id.", nameof(executionId));

            ExecutionId = executionId;
            int count = resolutions?.Count ?? 0;
            this.resolutions = new DamageTargetResolution[count];

            for (int i = 0; i < count; i++)
                this.resolutions[i] = resolutions[i];
        }

        public CombatExecutionId ExecutionId { get; }
        public int ResolutionCount => resolutions.Length;

        public DamageTargetResolution this[int index] => resolutions[index];

        public static CombatExecutionReport Empty(CombatExecutionId executionId)
        {
            return new CombatExecutionReport(executionId, Array.Empty<DamageTargetResolution>());
        }

        public static CombatExecutionReport Single(
            CombatExecutionId executionId,
            DamageTargetResolution resolution)
        {
            return new CombatExecutionReport(executionId, new[] { resolution });
        }
    }
}
