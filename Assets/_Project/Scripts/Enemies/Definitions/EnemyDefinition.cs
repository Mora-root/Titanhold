using System;
using UnityEngine;

namespace Titanhold.Enemies
{
    [CreateAssetMenu(
        fileName = "EnemyDefinition",
        menuName = "Titanhold/Enemies/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string enemyId;
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0.01f)] private float maximumHealth = 100f;
        [SerializeField, Min(0f)] private float armor;
        [SerializeField, Min(0f)] private float baseDamage = 10f;
        [SerializeField, Min(0.01f)] private float attacksPerSecond = 1f;
        [SerializeField, Min(0.01f)] private float attackRange = 1f;
        [SerializeField, Min(0f)] private float movementSpeed = 3f;
        [SerializeField, Min(0f)] private float rotationSpeed = 10f;
        [SerializeField, Min(0f)] private float detectionRange = 10f;

        public string EnemyId => enemyId?.Trim() ?? string.Empty;
        public GameObject Prefab => prefab;

        public bool IsValid => TryCreateArchetype(out _, out _);

        public bool TryCreateArchetype(
            out EnemyArchetype archetype,
            out string error)
        {
            archetype = null;
            if (!HasStrictId(enemyId))
            {
                error = $"Enemy definition '{name}' has an invalid stable id.";
                return false;
            }

            if (prefab == null)
            {
                error = $"Enemy definition '{enemyId}' has no prefab.";
                return false;
            }

            EnemyBaseStats baseStats = new(
                maximumHealth,
                armor,
                baseDamage,
                attacksPerSecond,
                attackRange,
                movementSpeed,
                rotationSpeed,
                detectionRange);
            if (!baseStats.TryValidate(out string statsError))
            {
                error = $"Enemy definition '{enemyId}' is invalid: {statsError}";
                return false;
            }

            archetype = new EnemyArchetype(enemyId, baseStats);
            error = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string configuredEnemyId,
            GameObject configuredPrefab,
            EnemyBaseStats configuredBaseStats)
        {
            enemyId = configuredEnemyId;
            prefab = configuredPrefab;
            maximumHealth = configuredBaseStats.MaximumHealth;
            armor = configuredBaseStats.Armor;
            baseDamage = configuredBaseStats.BaseDamage;
            attacksPerSecond = configuredBaseStats.AttacksPerSecond;
            attackRange = configuredBaseStats.AttackRange;
            movementSpeed = configuredBaseStats.MovementSpeed;
            rotationSpeed = configuredBaseStats.RotationSpeed;
            detectionRange = configuredBaseStats.DetectionRange;
        }
#endif

        private static bool HasStrictId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   string.Equals(value, value.Trim(), StringComparison.Ordinal);
        }
    }
}
