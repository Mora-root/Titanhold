using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class AssaultWaveDefinitionValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Assault Wave Definition")]
        public static void Validate()
        {
            GameObject enemyTemplate = new GameObject("AssaultWaveDefinition_Enemy");
            AssaultWaveDefinition definition =
                ScriptableObject.CreateInstance<AssaultWaveDefinition>();

            try
            {
                enemyTemplate.AddComponent<EnemyDeathNotifier>();
                ConfigureDefinition(
                    definition,
                    enemyTemplate,
                    initialDelay: 1.5f,
                    enemyCount: 3,
                    delayBeforeGroup: 2f,
                    spawnInterval: 0.25f);

                Assert(definition.TryCreatePlan(
                        out AssaultWavePlan plan,
                        out string error),
                    $"Valid wave definition was rejected: {error}");
                AssertApproximately(plan.InitialDelay, 1.5f, "Initial delay");
                Assert(plan.PlannedEnemyCount == 3 && plan.Steps.Count == 1,
                    "Wave plan counters are inconsistent.");
                Assert(plan.Steps[0].EnemyPrefab == enemyTemplate,
                    "Wave plan lost its enemy prefab.");
                AssertApproximately(plan.Steps[0].DelayBeforeGroup, 2f,
                    "Group delay");
                AssertApproximately(plan.Steps[0].SpawnInterval, 0.25f,
                    "Spawn interval");

                ConfigureDefinition(
                    definition,
                    enemyTemplate,
                    initialDelay: 0f,
                    enemyCount: 7,
                    delayBeforeGroup: 0f,
                    spawnInterval: 0f);
                Assert(plan.PlannedEnemyCount == 3 &&
                       plan.Steps[0].EnemyCount == 3,
                    "Existing wave plan changed with its ScriptableObject.");

                ConfigureDefinition(
                    definition,
                    null,
                    initialDelay: 0f,
                    enemyCount: 1,
                    delayBeforeGroup: 0f,
                    spawnInterval: 0f);
                Assert(!definition.TryCreatePlan(out _, out _),
                    "Wave definition accepted a missing enemy prefab.");

                Debug.Log("Assault Wave Definition validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Assault Wave Definition validation failed: {exception}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(enemyTemplate);
            }
        }

        public static void ConfigureDefinition(
            AssaultWaveDefinition definition,
            GameObject enemyPrefab,
            float initialDelay,
            int enemyCount,
            float delayBeforeGroup,
            float spawnInterval)
        {
            SerializedObject serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("initialDelay").floatValue = initialDelay;
            SerializedProperty groups =
                serializedDefinition.FindProperty("spawnGroups");
            groups.arraySize = 1;
            SerializedProperty group = groups.GetArrayElementAtIndex(0);
            group.FindPropertyRelative("enemyPrefab").objectReferenceValue = enemyPrefab;
            group.FindPropertyRelative("enemyCount").intValue = enemyCount;
            group.FindPropertyRelative("delayBeforeGroup").floatValue = delayBeforeGroup;
            group.FindPropertyRelative("spawnInterval").floatValue = spawnInterval;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
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
