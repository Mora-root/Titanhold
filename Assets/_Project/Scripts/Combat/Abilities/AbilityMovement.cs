using System;
using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    public readonly struct AbilityMovementDirective
    {
        public AbilityMovementDirective(
            Vector3 destination,
            float speedMultiplier,
            float arrivalDistance)
        {
            if (!IsFinite(destination))
                throw new ArgumentOutOfRangeException(nameof(destination));
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(
                    speedMultiplier) || speedMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(speedMultiplier));
            }
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(
                    arrivalDistance) || arrivalDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(arrivalDistance));
            }

            Destination = destination;
            SpeedMultiplier = speedMultiplier;
            ArrivalDistance = arrivalDistance;
        }

        public Vector3 Destination { get; }
        public float SpeedMultiplier { get; }
        public float ArrivalDistance { get; }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    // Optional capability implemented only by snapshots that move their actor
    // during the committed pre-release phase.
    public interface IRuntimeAbilityMovement
    {
        bool TryGetMovement(
            AbilityUseContext context,
            out AbilityMovementDirective movement);
    }
}
