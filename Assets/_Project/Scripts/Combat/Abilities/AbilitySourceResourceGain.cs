using System;
using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    public readonly struct AbilitySourceResourceGain
    {
        public AbilitySourceResourceGain(string resourceId, float amount)
        {
            ResourceId = resourceId?.Trim() ?? string.Empty;
            if (ResourceId.Length == 0)
                throw new ArgumentException(
                    "A generated combat resource requires a stable id.",
                    nameof(resourceId));
            if (!IsPositiveFinite(amount))
                throw new ArgumentOutOfRangeException(nameof(amount));

            Amount = amount;
        }

        public string ResourceId { get; }
        public float Amount { get; }
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ResourceId) &&
            IsPositiveFinite(Amount);

        internal static bool IsPositiveFinite(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value) &&
                   value > 0f;
        }
    }

    [Serializable]
    public sealed class AbilitySourceResourceGainAuthoring
    {
        [SerializeField] private bool enabled;
        [SerializeField] private string resourceId = "resource:rage";
        [SerializeField, Min(0f)] private float amount;

        public bool TryCreate(out AbilitySourceResourceGain gain)
        {
            gain = default;
            if (!enabled)
                return true;

            try
            {
                gain = new AbilitySourceResourceGain(resourceId, amount);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    public interface IRuntimeAbilitySourceResourceGain
    {
        AbilitySourceResourceGain SourceResourceGain { get; }
    }

    public static class AbilitySourceResourceGainResolver
    {
        // One ability execution grants at most one authored amount, independent
        // of the number of targets it hits. This prevents area abilities from
        // multiplying a generator reward by target count.
        public static bool TryApply(
            IRuntimeAbilitySnapshot ability,
            CombatExecutionId expectedExecutionId,
            CombatExecutionReport report,
            ICombatResourceGateway gateway)
        {
            if (ability is not IRuntimeAbilitySourceResourceGain provider ||
                !provider.SourceResourceGain.IsValid ||
                !expectedExecutionId.IsValid ||
                report == null ||
                report.ExecutionId != expectedExecutionId ||
                gateway == null ||
                !HasSuccessfulDamage(report))
            {
                return false;
            }

            AbilitySourceResourceGain gain = provider.SourceResourceGain;
            return gateway.TryGain(
                expectedExecutionId,
                gain.ResourceId,
                gain.Amount);
        }

        private static bool HasSuccessfulDamage(CombatExecutionReport report)
        {
            for (int i = 0; i < report.ResolutionCount; i++)
            {
                if (report[i].Result.WasApplied)
                    return true;
            }

            return false;
        }
    }
}
