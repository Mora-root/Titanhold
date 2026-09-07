using System;
using System.Collections.Generic;

namespace Titanhold.Combat
{
    public interface ICombatResourceGateway
    {
        bool TryGain(
            CombatExecutionId sourceExecutionId,
            string resourceId,
            float amount);
    }

    // A bounded, owner-local combat resource. The state is plain C# so the same
    // rules can later be hosted by a solo client or an authoritative session.
    public sealed class CombatResourceState : ICombatResourceGateway
    {
        private readonly HashSet<CombatExecutionId> appliedGainExecutions =
            new();
        private float current;

        public CombatResourceState(
            string resourceId,
            float maximum,
            float initial = 0f)
        {
            ResourceId = resourceId?.Trim() ?? string.Empty;
            if (ResourceId.Length == 0)
                throw new ArgumentException(
                    "A combat resource requires a stable id.",
                    nameof(resourceId));
            if (!IsFinite(maximum) || maximum <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maximum));
            if (!IsFinite(initial) || initial < 0f || initial > maximum)
                throw new ArgumentOutOfRangeException(nameof(initial));

            Maximum = maximum;
            current = initial;
        }

        public string ResourceId { get; }
        public float Maximum { get; }
        public float Current => current;

        public event Action<float, float> Changed;

        public bool CanSpend(float amount)
        {
            return IsFinite(amount) && amount >= 0f && current >= amount;
        }

        public bool TrySpend(float amount)
        {
            if (!IsFinite(amount) || amount < 0f || !CanSpend(amount))
                return false;
            if (amount == 0f)
                return true;

            current -= amount;
            Changed?.Invoke(current, Maximum);
            return true;
        }

        public bool TryGain(
            CombatExecutionId sourceExecutionId,
            string resourceId,
            float amount)
        {
            if (!sourceExecutionId.IsValid ||
                !CanAcceptGain(resourceId, amount))
            {
                return false;
            }

            if (!appliedGainExecutions.Add(sourceExecutionId))
                return true;

            ApplyGain(amount);
            return true;
        }

        public bool TryGain(string resourceId, float amount)
        {
            if (!CanAcceptGain(resourceId, amount))
                return false;

            ApplyGain(amount);
            return true;
        }

        public bool TrySetCurrent(float value)
        {
            if (!IsFinite(value) || value < 0f || value > Maximum)
                return false;
            if (value == current)
                return true;

            current = value;
            Changed?.Invoke(current, Maximum);
            return true;
        }

        private void ApplyGain(float amount)
        {
            float next = Math.Min(Maximum, current + amount);
            if (next > current)
            {
                current = next;
                Changed?.Invoke(current, Maximum);
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private bool CanAcceptGain(string resourceId, float amount)
        {
            string normalizedId = resourceId?.Trim() ?? string.Empty;
            return string.Equals(
                       ResourceId,
                       normalizedId,
                       StringComparison.Ordinal) &&
                   IsFinite(amount) &&
                   amount > 0f;
        }
    }
}
