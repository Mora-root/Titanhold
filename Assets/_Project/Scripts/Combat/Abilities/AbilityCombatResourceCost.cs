using System;
using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    public readonly struct AbilityCombatResourceCost
    {
        public AbilityCombatResourceCost(string resourceId, float amount)
        {
            ResourceId = resourceId?.Trim() ?? string.Empty;
            if (ResourceId.Length == 0)
            {
                throw new ArgumentException(
                    "A combat-resource cost requires a stable id.",
                    nameof(resourceId));
            }

            if (!IsPositiveFinite(amount))
                throw new ArgumentOutOfRangeException(nameof(amount));

            Amount = amount;
        }

        public string ResourceId { get; }
        public float Amount { get; }
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ResourceId) || Amount != 0f;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ResourceId) &&
            IsPositiveFinite(Amount);

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }
    }

    [Serializable]
    public sealed class AbilityCombatResourceCostAuthoring
    {
        [SerializeField] private bool enabled;
        [SerializeField] private string resourceId = "resource:rage";
        [SerializeField, Min(0f)] private float amount;

        public bool TryCreate(out AbilityCombatResourceCost cost)
        {
            cost = default;
            if (!enabled)
                return true;

            try
            {
                cost = new AbilityCombatResourceCost(resourceId, amount);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
