using System;
using Titanhold.Combat;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RoundEnemyScalingValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Round Enemy Scaling")]
        public static void Validate()
        {
            GameObject enemy = null;

            try
            {
                RoundScalingCalculator calculator = new RoundScalingCalculator(
                    healthBonusPerRound: 0.20f,
                    damageBonusPerRound: 0.10f);
                EnemyScalingSnapshot roundOne = calculator.CreateSnapshot(1);
                EnemyScalingSnapshot roundTwo = calculator.CreateSnapshot(2);
                EnemyScalingSnapshot roundThree = calculator.CreateSnapshot(3);

                AssertSnapshot(roundOne, 1, 1f, 1f);
                AssertSnapshot(roundTwo, 2, 1.20f, 1.10f);
                AssertSnapshot(roundThree, 3, 1.40f, 1.20f);

                enemy = new GameObject("RoundEnemyScaling_Validation");
                Health health = enemy.AddComponent<Health>();
                EnemyCombat combat = enemy.AddComponent<EnemyCombat>();
                ConfigureBaseValues(health, combat, 100f, 20f);

                EnemyScalingApplicator applicator = new EnemyScalingApplicator();
                EnemyScalingResult first = applicator.TryApply(
                    health,
                    combat,
                    roundTwo,
                    restoreFullHealth: true);
                Assert(first.Success, $"Round-two scaling failed: {first.Error}.");
                AssertApproximately(health.MaxHealth, 120f, "Round-two maximum health");
                AssertApproximately(health.CurrentHealth, 120f, "Round-two current health");
                AssertApproximately(combat.Damage, 22f, "Round-two damage");

                DamageResult damage = health.ApplyDamage(
                    DamageRequest.CreateUnattributed(50f));
                Assert(damage.WasApplied && health.CurrentHealth < health.MaxHealth,
                    "Validation enemy did not take setup damage.");

                EnemyScalingResult replacement = applicator.TryApply(
                    health,
                    combat,
                    roundThree,
                    restoreFullHealth: true);
                Assert(replacement.Success,
                    $"Round-three replacement failed: {replacement.Error}.");
                AssertApproximately(health.MaxHealth, 140f,
                    "Round-three maximum health compounded");
                AssertApproximately(health.CurrentHealth, 140f,
                    "Living enemy was not restored to its new maximum health");
                AssertApproximately(combat.Damage, 24f,
                    "Round-three damage compounded");

                Debug.Log("Round enemy scaling validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Round enemy scaling validation failed: {exception}");
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

        private static void AssertSnapshot(
            EnemyScalingSnapshot snapshot,
            int round,
            float healthMultiplier,
            float damageMultiplier)
        {
            Assert(snapshot.RoundNumber == round, $"Round {round} identity mismatch.");
            AssertApproximately(snapshot.HealthMultiplier, healthMultiplier,
                $"Round {round} health multiplier");
            AssertApproximately(snapshot.DamageMultiplier, damageMultiplier,
                $"Round {round} damage multiplier");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertApproximately(float actual, float expected, string label)
        {
            if (Math.Abs(actual - expected) <= 0.0001f)
                return;

            throw new InvalidOperationException(
                $"{label} failed. Expected {expected}, got {actual}.");
        }
    }
}
