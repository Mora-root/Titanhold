using System;

namespace Titanhold.Enemies
{
    public readonly struct EnemyBaseStats
    {
        public EnemyBaseStats(
            float maximumHealth,
            float armor,
            float baseDamage,
            float attacksPerSecond,
            float attackRange,
            float movementSpeed,
            float rotationSpeed,
            float detectionRange)
        {
            MaximumHealth = maximumHealth;
            Armor = armor;
            BaseDamage = baseDamage;
            AttacksPerSecond = attacksPerSecond;
            AttackRange = attackRange;
            MovementSpeed = movementSpeed;
            RotationSpeed = rotationSpeed;
            DetectionRange = detectionRange;
        }

        public float MaximumHealth { get; }
        public float Armor { get; }
        public float BaseDamage { get; }
        public float AttacksPerSecond { get; }
        public float AttackCooldown => 1f / AttacksPerSecond;
        public float AttackRange { get; }
        public float MovementSpeed { get; }
        public float RotationSpeed { get; }
        public float DetectionRange { get; }

        public bool TryValidate(out string error)
        {
            if (!IsFinitePositive(MaximumHealth))
            {
                error = "Maximum health must be finite and positive.";
                return false;
            }

            if (!IsFiniteNonNegative(Armor))
            {
                error = "Armor must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(BaseDamage))
            {
                error = "Base damage must be finite and non-negative.";
                return false;
            }

            if (!IsFinitePositive(AttacksPerSecond))
            {
                error = "Attacks per second must be finite and positive.";
                return false;
            }

            if (!IsFinitePositive(AttackCooldown))
            {
                error =
                    "Attacks per second produces an invalid attack cooldown.";
                return false;
            }

            if (!IsFinitePositive(AttackRange))
            {
                error = "Attack range must be finite and positive.";
                return false;
            }

            if (!IsFiniteNonNegative(MovementSpeed))
            {
                error = "Movement speed must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(RotationSpeed))
            {
                error = "Rotation speed must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(DetectionRange))
            {
                error = "Detection range must be finite and non-negative.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && IsFinite(value);
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return value >= 0f && IsFinite(value);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public sealed class EnemyArchetype
    {
        internal EnemyArchetype(
            string enemyId,
            EnemyBaseStats baseStats)
        {
            EnemyId = enemyId;
            BaseStats = baseStats;
        }

        public string EnemyId { get; }
        public EnemyBaseStats BaseStats { get; }
    }

    public interface IEnemyDefinitionResolver
    {
        bool TryResolve(string enemyId, out EnemyArchetype archetype);
    }
}
