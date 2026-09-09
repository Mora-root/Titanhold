using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Enemies.Editor
{
    public static class EnemyBaseStatsApplicationValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Enemy Base Stats Application")]
        public static void Validate()
        {
            GameObject prefab = new("EnemyBaseStatsApplicationPrefab");
            EnemyDefinition definition = null;

            try
            {
                EnemyBaseStats expected = new(
                    maximumHealth: 80f,
                    armor: 20f,
                    baseDamage: 12f,
                    attacksPerSecond: 1.25f,
                    attackRange: 1.75f,
                    movementSpeed: 3.5f,
                    rotationSpeed: 8f,
                    detectionRange: 11f);
                definition =
                    ScriptableObject.CreateInstance<EnemyDefinition>();
                definition.ConfigureForEditor(
                    "enemy:application-validation",
                    prefab,
                    expected);
                Assert(definition.TryCreateArchetype(
                           out EnemyArchetype archetype,
                           out string error) &&
                       string.IsNullOrEmpty(error),
                    "Could not create a valid application archetype.");

                ValidateService(archetype, expected);
                ValidateUnityReceiver(prefab, archetype, expected);
                Debug.Log(
                    "Enemy base stats application validation passed (4 scenarios).");
            }
            finally
            {
                if (definition != null)
                    UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        private static void ValidateService(
            EnemyArchetype archetype,
            EnemyBaseStats expected)
        {
            EnemyBaseStatsApplicationService service = new();
            RecordingGateway ready = new(isReady: true);
            EnemyBaseStatsApplicationResult applied =
                service.TryApply(archetype, ready);

            Assert(applied.Success &&
                   applied.Error == EnemyBaseStatsApplicationError.None &&
                   applied.EnemyId == archetype.EnemyId,
                "The application service rejected valid base stats.");
            Assert(ready.BaseStats.Count == 6 &&
                   Approximately(
                       ready.BaseStats[StatType.MaxHealth],
                       expected.MaximumHealth) &&
                   Approximately(
                       ready.BaseStats[StatType.Armor],
                       expected.Armor) &&
                   Approximately(
                       ready.BaseStats[StatType.Damage],
                       expected.BaseDamage) &&
                   Approximately(
                       ready.BaseStats[StatType.AttackSpeed],
                       100f) &&
                   Approximately(
                       ready.BaseStats[StatType.AttackRange],
                       expected.AttackRange) &&
                   Approximately(
                       ready.BaseStats[StatType.MoveSpeed],
                       expected.MovementSpeed),
                "The application service wrote incomplete character stats.");
            Assert(Approximately(
                       ready.BaseAttacksPerSecond,
                       expected.AttacksPerSecond) &&
                   Approximately(
                       ready.RotationSpeed,
                       expected.RotationSpeed) &&
                   Approximately(
                       ready.DetectionRange,
                       expected.DetectionRange) &&
                   ready.RestoreCount == 1,
                "The application service did not configure all runtime adapters.");

            EnemyBaseStatsApplicationResult missingGateway =
                service.TryApply(archetype, null);
            Assert(!missingGateway.Success &&
                   missingGateway.Error ==
                       EnemyBaseStatsApplicationError.MissingGateway,
                "The application service accepted a missing gateway.");

            RecordingGateway unavailable = new(isReady: false);
            EnemyBaseStatsApplicationResult unavailableResult =
                service.TryApply(archetype, unavailable);
            Assert(!unavailableResult.Success &&
                   unavailableResult.Error ==
                       EnemyBaseStatsApplicationError.GatewayNotReady &&
                   unavailable.MutationCount == 0,
                "An unavailable gateway was mutated.");
        }

        private static void ValidateUnityReceiver(
            GameObject gameObject,
            EnemyArchetype archetype,
            EnemyBaseStats expected)
        {
            CharacterStats stats = gameObject.AddComponent<CharacterStats>();
            Health health = gameObject.AddComponent<Health>();
            EnemyCombat combat = gameObject.AddComponent<EnemyCombat>();
            EnemyMovement movement = gameObject.AddComponent<EnemyMovement>();
            EnemySensor sensor = gameObject.AddComponent<EnemySensor>();
            EnemyBaseStatsReceiver receiver =
                gameObject.AddComponent<EnemyBaseStatsReceiver>();

            SerializedObject serializedHealth = new(health);
            serializedHealth.FindProperty("characterStats")
                .objectReferenceValue = stats;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();
            receiver.ConfigureForEditor(
                stats,
                health,
                combat,
                movement,
                sensor);

            EnemyBaseStatsApplicationResult result =
                receiver.TryApply(archetype);
            Assert(result.Success &&
                   receiver.HasAppliedDefinition &&
                   receiver.AppliedEnemyId == archetype.EnemyId,
                "The Unity receiver rejected valid base stats.");
            Assert(Approximately(
                       health.MaxHealth,
                       expected.MaximumHealth) &&
                   Approximately(
                       health.CurrentHealth,
                       expected.MaximumHealth) &&
                   Approximately(combat.Damage, expected.BaseDamage) &&
                   Approximately(combat.AttackRange, expected.AttackRange) &&
                   Approximately(
                       combat.CurrentAttackCooldown,
                       expected.AttackCooldown) &&
                   Approximately(
                       movement.MovementSpeed,
                       expected.MovementSpeed) &&
                   Approximately(
                       movement.RotationSpeed,
                       expected.RotationSpeed) &&
                   Approximately(
                       sensor.DetectionRange,
                       expected.DetectionRange),
                "The Unity receiver exposed stale runtime values.");
        }

        private static bool Approximately(float actual, float expected)
        {
            return Math.Abs(actual - expected) <= 0.0001f;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class RecordingGateway : IEnemyBaseStatsGateway
        {
            public RecordingGateway(bool isReady)
            {
                IsReady = isReady;
            }

            public bool IsReady { get; }
            public Dictionary<StatType, float> BaseStats { get; } = new();
            public float BaseAttacksPerSecond { get; private set; }
            public float RotationSpeed { get; private set; }
            public float DetectionRange { get; private set; }
            public int RestoreCount { get; private set; }
            public int MutationCount { get; private set; }

            public void SetBaseStat(StatType statType, float value)
            {
                BaseStats.Add(statType, value);
                MutationCount++;
            }

            public void ConfigureCombat(float baseAttacksPerSecond)
            {
                BaseAttacksPerSecond = baseAttacksPerSecond;
                MutationCount++;
            }

            public void ConfigureMovement(float rotationSpeed)
            {
                RotationSpeed = rotationSpeed;
                MutationCount++;
            }

            public void ConfigureDetection(float detectionRange)
            {
                DetectionRange = detectionRange;
                MutationCount++;
            }

            public void RestoreFullHealth()
            {
                RestoreCount++;
                MutationCount++;
            }
        }
    }
}
