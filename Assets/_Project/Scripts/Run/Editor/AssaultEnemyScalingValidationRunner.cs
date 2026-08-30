using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class AssaultEnemyScalingValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Assault Enemy Scaling")]
        public static void Validate()
        {
            GameObject enemy = null;

            try
            {
                enemy = new GameObject("AssaultEnemyScaling_Validation");
                Health health = enemy.AddComponent<Health>();
                EnemyCombat combat = enemy.AddComponent<EnemyCombat>();
                ConfigureBaseValues(health, combat, 100f, 20f);

                AssaultEnemyScalingApplicator applicator = new();
                AssaultScalingSnapshot firstSnapshot =
                    new AssaultScalingSnapshot(20, 2, 1.5f, 1.25f);
                AssaultEnemyScalingResult first = applicator.TryApply(
                    health,
                    combat,
                    firstSnapshot);

                Assert(first.Success, $"Initial scaling failed: {first.Error}.");
                AssertApproximately(health.MaxHealth, 150f,
                    "Scaled maximum health mismatch.");
                AssertApproximately(health.CurrentHealth, 150f,
                    "Scaled enemy did not start at full health.");
                AssertApproximately(combat.Damage, 25f,
                    "Scaled damage mismatch.");

                AssaultScalingSnapshot replacementSnapshot =
                    new AssaultScalingSnapshot(30, 3, 2f, 2f);
                AssaultEnemyScalingResult replacement = applicator.TryApply(
                    health,
                    combat,
                    replacementSnapshot);

                Assert(replacement.Success,
                    $"Replacement scaling failed: {replacement.Error}.");
                AssertApproximately(health.MaxHealth, 200f,
                    "Health snapshot compounded instead of being replaced.");
                AssertApproximately(combat.Damage, 40f,
                    "Damage snapshot compounded instead of being replaced.");

                AssaultEnemyScalingResult invalid = applicator.TryApply(
                    health,
                    combat,
                    new AssaultScalingSnapshot(0, 0, float.NaN, 1f));
                Assert(!invalid.Success &&
                       invalid.Error == AssaultEnemyScalingError.InvalidSnapshot,
                    "Invalid scaling snapshot was accepted.");
                AssertApproximately(health.MaxHealth, 200f,
                    "Rejected snapshot mutated maximum health.");
                AssertApproximately(combat.Damage, 40f,
                    "Rejected snapshot mutated damage.");

                Debug.Log("Assault enemy scaling validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Assault enemy scaling validation failed: {exception}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        private static void ConfigureBaseValues(
            Health health,
            EnemyCombat combat,
            float maxHealth,
            float damage)
        {
            SerializedObject serializedHealth = new SerializedObject(health);
            serializedHealth.FindProperty("maxHealth").floatValue = maxHealth;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedCombat = new SerializedObject(combat);
            serializedCombat.FindProperty("damage").floatValue = damage;
            serializedCombat.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertApproximately(
            float actual,
            float expected,
            string message)
        {
            if (Math.Abs(actual - expected) > 0.0001f)
            {
                throw new InvalidOperationException(
                    $"{message} Expected {expected}, got {actual}.");
            }
        }
    }
}
