using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Enemies.Editor
{
    public static class EnemyDefinitionCatalogValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Enemy Definition Catalog Foundation")]
        public static void Validate()
        {
            GameObject firstPrefab = new("EnemyDefinitionValidationFirst");
            GameObject secondPrefab = new("EnemyDefinitionValidationSecond");
            EnemyDefinition first = null;
            EnemyDefinition second = null;
            EnemyDefinition malformed = null;
            EnemyDefinitionCatalog catalog = null;

            try
            {
                EnemyBaseStats firstStats = ValidStats(maximumHealth: 80f);
                first = CreateDefinition(
                    "enemy:skeleton",
                    firstPrefab,
                    firstStats);
                second = CreateDefinition(
                    "enemy:skeleton-warrior",
                    secondPrefab,
                    ValidStats(maximumHealth: 150f, armor: 50f));

                Assert(first.TryCreateArchetype(
                           out EnemyArchetype archetype,
                           out string definitionError) &&
                       string.IsNullOrEmpty(definitionError) &&
                       archetype.EnemyId == "enemy:skeleton" &&
                       Approximately(archetype.BaseStats.MaximumHealth, 80f) &&
                       Approximately(archetype.BaseStats.AttackCooldown, 1f),
                    "A valid enemy definition did not create its immutable archetype.");

                Assert(EnemyDefinitionRegistry.TryCreate(
                           new[]
                           {
                               CreateArchetype(first),
                               CreateArchetype(second)
                           },
                           out EnemyDefinitionRegistry registry,
                           out string registryError) &&
                       string.IsNullOrEmpty(registryError) &&
                       registry.Count == 2 &&
                       registry.TryResolve(" enemy:skeleton ", out EnemyArchetype resolved) &&
                       resolved.EnemyId == archetype.EnemyId &&
                       Approximately(
                           resolved.BaseStats.MaximumHealth,
                           archetype.BaseStats.MaximumHealth),
                    "The registry did not resolve a complete valid definition set.");

                Assert(!EnemyDefinitionRegistry.TryCreate(
                           new[]
                           {
                               CreateArchetype(first),
                               CreateArchetype(first)
                           },
                           out _,
                           out _),
                    "The registry accepted duplicate enemy ids.");

                malformed = CreateDefinition(
                    "enemy:malformed",
                    firstPrefab,
                    ValidStats(maximumHealth: float.NaN));
                Assert(!malformed.TryCreateArchetype(out _, out _),
                    "An enemy definition accepted malformed base stats.");

                catalog = ScriptableObject.CreateInstance<EnemyDefinitionCatalog>();
                catalog.ConfigureForEditor(new[] { first, second });
                Assert(catalog.IsValid &&
                       catalog.TryResolve("enemy:skeleton-warrior", out EnemyArchetype warrior) &&
                       Approximately(warrior.BaseStats.Armor, 50f) &&
                       catalog.TryResolvePrefab("enemy:skeleton", out GameObject prefab) &&
                       prefab == firstPrefab,
                    "The catalog did not expose its complete valid registry.");

                catalog.ConfigureForEditor(new[] { first, null, second });
                Assert(!catalog.IsValid &&
                       !catalog.TryResolve("enemy:skeleton", out _) &&
                       !catalog.TryResolvePrefab("enemy:skeleton", out _),
                    "An invalid catalog exposed a partial definition set.");

                Debug.Log(
                    "Enemy definition catalog foundation validation passed (5 scenarios).");
            }
            finally
            {
                Destroy(catalog);
                Destroy(malformed);
                Destroy(second);
                Destroy(first);
                UnityEngine.Object.DestroyImmediate(secondPrefab);
                UnityEngine.Object.DestroyImmediate(firstPrefab);
            }
        }

        private static EnemyDefinition CreateDefinition(
            string enemyId,
            GameObject prefab,
            EnemyBaseStats stats)
        {
            EnemyDefinition definition =
                ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.ConfigureForEditor(enemyId, prefab, stats);
            return definition;
        }

        private static EnemyArchetype CreateArchetype(
            EnemyDefinition definition)
        {
            if (!definition.TryCreateArchetype(
                    out EnemyArchetype archetype,
                    out string error))
            {
                throw new InvalidOperationException(error);
            }

            return archetype;
        }

        private static EnemyBaseStats ValidStats(
            float maximumHealth = 100f,
            float armor = 20f)
        {
            return new EnemyBaseStats(
                maximumHealth,
                armor,
                baseDamage: 10f,
                attacksPerSecond: 1f,
                attackRange: 1.5f,
                movementSpeed: 3f,
                rotationSpeed: 10f,
                detectionRange: 10f);
        }

        private static bool Approximately(float actual, float expected)
        {
            return Math.Abs(actual - expected) <= 0.0001f;
        }

        private static void Destroy(UnityEngine.Object value)
        {
            if (value != null)
                UnityEngine.Object.DestroyImmediate(value);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
