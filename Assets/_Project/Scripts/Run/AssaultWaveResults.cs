using Titanhold.Combat;
using UnityEngine;

namespace Titanhold.Run
{
    public enum AssaultWaveStartError
    {
        None,
        SpawnerInactive,
        AlreadySpawning,
        MissingRuntime,
        MissingRegistry,
        MissingDefinition,
        InvalidDefinition,
        MissingSpawnPoints,
        InvalidPhase,
        EncounterRejected
    }

    public readonly struct AssaultWaveStartResult
    {
        private AssaultWaveStartResult(
            bool success,
            AssaultWaveStartError error,
            AssaultEncounterId encounterId,
            int plannedEnemyCount,
            string definitionError,
            AssaultEncounterResult encounterResult)
        {
            Success = success;
            Error = error;
            EncounterId = encounterId;
            PlannedEnemyCount = plannedEnemyCount;
            DefinitionError = definitionError ?? string.Empty;
            EncounterResult = encounterResult;
        }

        public bool Success { get; }
        public AssaultWaveStartError Error { get; }
        public AssaultEncounterId EncounterId { get; }
        public int PlannedEnemyCount { get; }
        public string DefinitionError { get; }
        public AssaultEncounterResult EncounterResult { get; }

        public static AssaultWaveStartResult Succeeded(
            AssaultEncounterId encounterId,
            int plannedEnemyCount,
            AssaultEncounterResult encounterResult)
        {
            return new AssaultWaveStartResult(
                true,
                AssaultWaveStartError.None,
                encounterId,
                plannedEnemyCount,
                string.Empty,
                encounterResult);
        }

        public static AssaultWaveStartResult Failed(
            AssaultWaveStartError error,
            string definitionError = "",
            AssaultEncounterResult encounterResult = default)
        {
            return new AssaultWaveStartResult(
                false,
                error,
                default,
                0,
                definitionError,
                encounterResult);
        }
    }

    public enum AssaultWaveSpawnFailureReason
    {
        MissingDeathNotifier,
        InactiveDeathNotifier,
        RegistryRejected
    }

    public readonly struct AssaultWaveSpawnFailure
    {
        public AssaultWaveSpawnFailure(
            int sequenceNumber,
            AssaultWaveSpawnFailureReason reason,
            GameObject enemyObject,
            CombatActorReference enemy,
            AssaultEnemyRegistryResult registryResult)
        {
            SequenceNumber = sequenceNumber;
            Reason = reason;
            EnemyObject = enemyObject;
            Enemy = enemy;
            RegistryResult = registryResult;
        }

        public int SequenceNumber { get; }
        public AssaultWaveSpawnFailureReason Reason { get; }
        public GameObject EnemyObject { get; }
        public CombatActorReference Enemy { get; }
        public AssaultEnemyRegistryResult RegistryResult { get; }
    }
}
