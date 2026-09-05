using System;

namespace Titanhold.Combat.Abilities
{
    // Immutable, actor-independent values resolved from authored data and modifiers.
    // This first lifecycle supports one release followed by recovery.
    public sealed class AbilityExecutionDefinition
    {
        public AbilityExecutionDefinition(
            string abilityId,
            float resourceCost,
            double cooldown,
            double windUp,
            double recovery)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
                throw new ArgumentException("An ability requires a stable definition id.", nameof(abilityId));

            RequireNonNegativeFinite(resourceCost, nameof(resourceCost));
            RequireNonNegativeFinite(cooldown, nameof(cooldown));
            RequireNonNegativeFinite(windUp, nameof(windUp));
            RequireNonNegativeFinite(recovery, nameof(recovery));
            RequireNonNegativeFinite(windUp + recovery, nameof(recovery));

            AbilityId = abilityId.Trim();
            ResourceCost = resourceCost;
            Cooldown = cooldown;
            WindUp = windUp;
            Recovery = recovery;
        }

        public string AbilityId { get; }
        public float ResourceCost { get; }
        public double Cooldown { get; }
        public double WindUp { get; }
        public double Recovery { get; }

        internal static bool IsNonNegativeFinite(double value)
        {
            return value >= 0d && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static void RequireNonNegativeFinite(double value, string parameter)
        {
            if (!IsNonNegativeFinite(value))
                throw new ArgumentOutOfRangeException(parameter);
        }
    }
}
