using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Enemies.Editor
{
    public static class EnemyDefinitionInitializationValidationRunner
    {
        [MenuItem(
            "Tools/Titanhold/Validate Enemy Definition Initialization")]
        public static void Validate()
        {
            GameObject prefab = new("EnemyDefinitionInitializationPrefab");
            EnemyDefinition definition = null;

            try
            {
                EnemyBaseStats baseStats = new(
                    maximumHealth: 90f,
                    armor: 15f,
                    baseDamage: 14f,
                    attacksPerSecond: 1.4f,
                    attackRange: 1.8f,
                    movementSpeed: 3.75f,
                    rotationSpeed: 9f,
                    detectionRange: 12f);
                definition =
                    ScriptableObject.CreateInstance<EnemyDefinition>();
                definition.ConfigureForEditor(
                    "enemy:initialization-validation",
                    prefab,
                    baseStats);
                Assert(definition.TryCreateArchetype(
                           out EnemyArchetype archetype,
                           out string error) &&
                       string.IsNullOrEmpty(error),
                    "Could not create a valid initialization archetype.");

                SingleArchetypeResolver resolver = new(archetype);
                ValidateSuccessfulServiceInitialization(
                    resolver,
                    archetype);
                ValidateInvalidIdentifiers(resolver);
                ValidateMissingDependencies(resolver, archetype);
                ValidateUnknownDefinition(archetype);
                ValidateApplicationFailure(resolver, archetype);
                ValidateUnityBinding(prefab, resolver, archetype, baseStats);

                Debug.Log(
                    "Enemy definition initialization validation passed (6 scenarios).");
            }
            finally
            {
                if (definition != null)
                    UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        private static void ValidateSuccessfulServiceInitialization(
            IEnemyDefinitionResolver resolver,
            EnemyArchetype archetype)
        {
            EnemyDefinitionInitializationService service = new();
            RecordingReceiver receiver = new(
                EnemyBaseStatsApplicationResult.Succeeded(
                    archetype.EnemyId));

            EnemyDefinitionInitializationResult result =
                service.TryInitialize(
                    archetype.EnemyId,
                    resolver,
                    receiver);

            Assert(result.Success &&
                   result.Error ==
                       EnemyDefinitionInitializationError.None &&
                   result.EnemyId == archetype.EnemyId &&
                   receiver.ApplyCount == 1 &&
                   ReferenceEquals(receiver.LastArchetype, archetype),
                "A valid initialization request was not applied exactly once.");
        }

        private static void ValidateInvalidIdentifiers(
            IEnemyDefinitionResolver resolver)
        {
            EnemyDefinitionInitializationService service = new();
            RecordingReceiver receiver = new(
                EnemyBaseStatsApplicationResult.Succeeded("unused"));

            EnemyDefinitionInitializationResult blank =
                service.TryInitialize("", resolver, receiver);
            EnemyDefinitionInitializationResult padded =
                service.TryInitialize(" enemy:padded ", resolver, receiver);

            Assert(!blank.Success &&
                   blank.Error ==
                       EnemyDefinitionInitializationError.InvalidEnemyId &&
                   !padded.Success &&
                   padded.Error ==
                       EnemyDefinitionInitializationError.InvalidEnemyId &&
                   receiver.ApplyCount == 0,
                "Invalid stable ids reached the receiver.");
        }

        private static void ValidateMissingDependencies(
            IEnemyDefinitionResolver resolver,
            EnemyArchetype archetype)
        {
            EnemyDefinitionInitializationService service = new();
            RecordingReceiver receiver = new(
                EnemyBaseStatsApplicationResult.Succeeded(
                    archetype.EnemyId));

            EnemyDefinitionInitializationResult missingResolver =
                service.TryInitialize(
                    archetype.EnemyId,
                    null,
                    receiver);
            EnemyDefinitionInitializationResult missingReceiver =
                service.TryInitialize(
                    archetype.EnemyId,
                    resolver,
                    null);

            Assert(!missingResolver.Success &&
                   missingResolver.Error ==
                       EnemyDefinitionInitializationError.MissingResolver &&
                   !missingReceiver.Success &&
                   missingReceiver.Error ==
                       EnemyDefinitionInitializationError.MissingReceiver &&
                   receiver.ApplyCount == 0,
                "A request with missing dependencies mutated runtime state.");
        }

        private static void ValidateUnknownDefinition(
            EnemyArchetype archetype)
        {
            EnemyDefinitionInitializationService service = new();
            RecordingReceiver receiver = new(
                EnemyBaseStatsApplicationResult.Succeeded(
                    archetype.EnemyId));

            EnemyDefinitionInitializationResult result =
                service.TryInitialize(
                    "enemy:unknown",
                    new RejectingResolver(),
                    receiver);

            Assert(!result.Success &&
                   result.Error ==
                       EnemyDefinitionInitializationError.DefinitionNotFound &&
                   receiver.ApplyCount == 0,
                "An unresolved definition reached the receiver.");
        }

        private static void ValidateApplicationFailure(
            IEnemyDefinitionResolver resolver,
            EnemyArchetype archetype)
        {
            EnemyDefinitionInitializationService service = new();
            RecordingReceiver receiver = new(
                EnemyBaseStatsApplicationResult.Failed(
                    EnemyBaseStatsApplicationError.GatewayNotReady,
                    archetype.EnemyId));

            EnemyDefinitionInitializationResult result =
                service.TryInitialize(
                    archetype.EnemyId,
                    resolver,
                    receiver);

            Assert(!result.Success &&
                   result.Error ==
                       EnemyDefinitionInitializationError.BaseStatsApplicationFailed &&
                   result.ApplicationError ==
                       EnemyBaseStatsApplicationError.GatewayNotReady &&
                   receiver.ApplyCount == 1,
                "The receiver failure was not preserved by initialization.");
        }

        private static void ValidateUnityBinding(
            GameObject gameObject,
            IEnemyDefinitionResolver resolver,
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
            EnemyDefinitionBinding binding =
                gameObject.AddComponent<EnemyDefinitionBinding>();

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
            binding.ConfigureForEditor(archetype.EnemyId, receiver);

            EnemyDefinitionInitializationResult result =
                binding.TryInitialize(resolver);

            Assert(result.Success &&
                   binding.HasInitialized &&
                   binding.EnemyId == archetype.EnemyId &&
                   receiver.HasAppliedDefinition &&
                   receiver.AppliedEnemyId == archetype.EnemyId &&
                   Approximately(health.MaxHealth, expected.MaximumHealth) &&
                   Approximately(health.CurrentHealth, expected.MaximumHealth) &&
                   Approximately(combat.Damage, expected.BaseDamage),
                "The Unity binding did not initialize its local receiver.");
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

        private sealed class SingleArchetypeResolver :
            IEnemyDefinitionResolver
        {
            private readonly EnemyArchetype archetype;

            public SingleArchetypeResolver(EnemyArchetype archetype)
            {
                this.archetype = archetype;
            }

            public bool TryResolve(
                string enemyId,
                out EnemyArchetype resolvedArchetype)
            {
                bool matches = string.Equals(
                    enemyId,
                    archetype.EnemyId,
                    StringComparison.Ordinal);
                resolvedArchetype = matches ? archetype : null;
                return matches;
            }
        }

        private sealed class RejectingResolver : IEnemyDefinitionResolver
        {
            public bool TryResolve(
                string enemyId,
                out EnemyArchetype archetype)
            {
                archetype = null;
                return false;
            }
        }

        private sealed class RecordingReceiver : IEnemyArchetypeReceiver
        {
            private readonly EnemyBaseStatsApplicationResult result;

            public RecordingReceiver(
                EnemyBaseStatsApplicationResult result)
            {
                this.result = result;
            }

            public int ApplyCount { get; private set; }
            public EnemyArchetype LastArchetype { get; private set; }

            public EnemyBaseStatsApplicationResult TryApply(
                EnemyArchetype archetype)
            {
                ApplyCount++;
                LastArchetype = archetype;
                return result;
            }
        }
    }
}
