using System;
using System.Collections.Generic;

namespace Titanhold.Combat.Flasks
{
    public interface IFlaskRecoveryGateway
    {
        bool IsAlive { get; }

        bool TryGetRecoveryState(
            FlaskRecoveryTarget target,
            out float current,
            out float maximum);

        bool TryRestore(
            FlaskRecoveryTarget target,
            float requestedAmount,
            out float restoredAmount);
    }

    public enum FlaskUseStatus
    {
        Success = 0,
        InvalidSlot = 1,
        InvalidTime = 2,
        UserUnavailable = 3,
        TargetUnavailable = 4,
        AlreadyFull = 5,
        CoolingDown = 6,
        RestoreRejected = 7
    }

    public readonly struct FlaskUseResult
    {
        public FlaskUseResult(
            FlaskUseStatus status,
            int slotIndex,
            string flaskId,
            float restoredAmount)
        {
            Status = status;
            SlotIndex = slotIndex;
            FlaskId = flaskId ?? string.Empty;
            RestoredAmount = restoredAmount;
        }

        public FlaskUseStatus Status { get; }
        public int SlotIndex { get; }
        public string FlaskId { get; }
        public float RestoredAmount { get; }
        public bool Success => Status == FlaskUseStatus.Success;
    }

    public readonly struct FlaskCooldownSnapshot
    {
        public FlaskCooldownSnapshot(
            int slotIndex,
            string flaskId,
            double duration,
            double remaining)
        {
            SlotIndex = slotIndex;
            FlaskId = flaskId ?? string.Empty;
            Duration = Math.Max(0d, duration);
            Remaining = Math.Max(0d, remaining);
        }

        public int SlotIndex { get; }
        public string FlaskId { get; }
        public double Duration { get; }
        public double Remaining { get; }
        public bool IsValid =>
            SlotIndex >= 0 && !string.IsNullOrWhiteSpace(FlaskId);
        public bool IsCoolingDown => IsValid && Remaining > 0d;
        public double NormalizedRemaining =>
            Duration > 0d ? Math.Clamp(Remaining / Duration, 0d, 1d) : 0d;
    }

    public sealed class FlaskUseService
    {
        private const float FullEpsilon = 0.0001f;

        private readonly FlaskDefinitionSnapshot[] slots;
        private readonly double[] cooldownEndsAt;
        private readonly IFlaskRecoveryGateway recovery;

        public FlaskUseService(
            IReadOnlyList<FlaskDefinitionSnapshot> configuredSlots,
            IFlaskRecoveryGateway recovery)
        {
            if (configuredSlots == null)
                throw new ArgumentNullException(nameof(configuredSlots));

            this.recovery = recovery ??
                throw new ArgumentNullException(nameof(recovery));
            slots = new FlaskDefinitionSnapshot[configuredSlots.Count];
            cooldownEndsAt = new double[configuredSlots.Count];
            for (int i = 0; i < configuredSlots.Count; i++)
            {
                if (!configuredSlots[i].IsValid)
                    throw new ArgumentException(
                        $"Flask slot {i} is invalid.",
                        nameof(configuredSlots));

                slots[i] = configuredSlots[i];
            }
        }

        public int SlotCount => slots.Length;

        public FlaskUseResult TryUse(int slotIndex, double now)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length)
                return Failure(FlaskUseStatus.InvalidSlot, slotIndex);
            if (!IsNonNegativeFinite(now))
                return Failure(FlaskUseStatus.InvalidTime, slotIndex);
            if (!recovery.IsAlive)
                return Failure(FlaskUseStatus.UserUnavailable, slotIndex);

            FlaskDefinitionSnapshot definition = slots[slotIndex];
            if (cooldownEndsAt[slotIndex] > now)
                return Failure(FlaskUseStatus.CoolingDown, slotIndex);
            if (!recovery.TryGetRecoveryState(
                    definition.RecoveryTarget,
                    out float current,
                    out float maximum) ||
                !IsPositiveFinite(maximum) ||
                !IsNonNegativeFinite(current))
            {
                return Failure(FlaskUseStatus.TargetUnavailable, slotIndex);
            }

            if (current >= maximum - FullEpsilon)
                return Failure(FlaskUseStatus.AlreadyFull, slotIndex);

            float requested = maximum * definition.RecoveryFraction;
            if (!IsPositiveFinite(requested) ||
                !recovery.TryRestore(
                    definition.RecoveryTarget,
                    requested,
                    out float restored) ||
                restored <= FullEpsilon ||
                float.IsNaN(restored) ||
                float.IsInfinity(restored))
            {
                return Failure(FlaskUseStatus.RestoreRejected, slotIndex);
            }

            cooldownEndsAt[slotIndex] = now + definition.Cooldown;
            return new FlaskUseResult(
                FlaskUseStatus.Success,
                slotIndex,
                definition.FlaskId,
                restored);
        }

        public bool TryGetCooldown(
            int slotIndex,
            double now,
            out FlaskCooldownSnapshot snapshot)
        {
            snapshot = default;
            if (slotIndex < 0 ||
                slotIndex >= slots.Length ||
                !IsNonNegativeFinite(now))
            {
                return false;
            }

            FlaskDefinitionSnapshot definition = slots[slotIndex];
            snapshot = new FlaskCooldownSnapshot(
                slotIndex,
                definition.FlaskId,
                definition.Cooldown,
                Math.Max(0d, cooldownEndsAt[slotIndex] - now));
            return true;
        }

        private FlaskUseResult Failure(
            FlaskUseStatus status,
            int slotIndex)
        {
            string flaskId = slotIndex >= 0 && slotIndex < slots.Length
                ? slots[slotIndex].FlaskId
                : string.Empty;
            return new FlaskUseResult(status, slotIndex, flaskId, 0f);
        }

        private static bool IsNonNegativeFinite(double value) =>
            value >= 0d && !double.IsNaN(value) && !double.IsInfinity(value);

        private static bool IsNonNegativeFinite(float value) =>
            value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool IsPositiveFinite(float value) =>
            value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
