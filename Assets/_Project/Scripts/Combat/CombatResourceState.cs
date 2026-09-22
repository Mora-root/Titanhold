using System;
using System.Collections.Generic;

namespace Titanhold.Combat
{
    public readonly struct CombatResourceSnapshot
    {
        internal CombatResourceSnapshot(
            string resourceId,
            float maximum,
            float current)
        {
            ResourceId = resourceId;
            Maximum = maximum;
            Current = current;
        }

        public string ResourceId { get; }
        public float Maximum { get; }
        public float Current { get; }
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ResourceId) &&
            Maximum > 0f &&
            !float.IsNaN(Maximum) &&
            !float.IsInfinity(Maximum) &&
            Current >= 0f &&
            Current <= Maximum &&
            !float.IsNaN(Current) &&
            !float.IsInfinity(Current);
    }

    public interface ICombatResourceGateway
    {
        IDisposable DeferNotifications();

        bool CanSpend(string resourceId, float amount);

        bool TrySpend(string resourceId, float amount);

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
        private int notificationDeferralDepth;
        private bool notificationPending;

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
        public CombatResourceSnapshot Snapshot =>
            new(ResourceId, Maximum, current);

        public event Action<float, float> Changed;

        public IDisposable DeferNotifications()
        {
            notificationDeferralDepth++;
            return new NotificationScope(this);
        }

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
            NotifyChanged();
            return true;
        }

        public bool CanSpend(string resourceId, float amount)
        {
            return MatchesResource(resourceId) && CanSpend(amount);
        }

        public bool TrySpend(string resourceId, float amount)
        {
            return MatchesResource(resourceId) && TrySpend(amount);
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
            NotifyChanged();
            return true;
        }

        private void ApplyGain(float amount)
        {
            float next = Math.Min(Maximum, current + amount);
            if (next > current)
            {
                current = next;
                NotifyChanged();
            }
        }

        private void NotifyChanged()
        {
            if (notificationDeferralDepth > 0)
            {
                notificationPending = true;
                return;
            }

            Changed?.Invoke(current, Maximum);
        }

        private sealed class NotificationScope : IDisposable
        {
            private CombatResourceState owner;

            public NotificationScope(CombatResourceState owner)
            {
                this.owner = owner;
            }

            public void Dispose()
            {
                CombatResourceState resource = owner;
                owner = null;
                if (resource == null)
                    return;

                resource.notificationDeferralDepth--;
                if (resource.notificationDeferralDepth == 0 &&
                    resource.notificationPending)
                {
                    resource.notificationPending = false;
                    resource.NotifyChanged();
                }
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private bool CanAcceptGain(string resourceId, float amount)
        {
            return MatchesResource(resourceId) &&
                   IsFinite(amount) &&
                   amount > 0f;
        }

        private bool MatchesResource(string resourceId)
        {
            string normalizedId = resourceId?.Trim() ?? string.Empty;
            return string.Equals(
                ResourceId,
                normalizedId,
                StringComparison.Ordinal);
        }
    }
}
