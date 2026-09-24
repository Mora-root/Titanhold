using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Enemies
{
    [Serializable]
    public sealed class ExplorationEnemyWeightDefinition
    {
        [SerializeField] private string enemyId;
        [SerializeField, Min(1)] private int weight = 1;

        public string EnemyId => enemyId ?? string.Empty;
        public int Weight => weight;

#if UNITY_EDITOR
        public void ConfigureForEditor(string configuredEnemyId, int configuredWeight)
        {
            enemyId = configuredEnemyId;
            weight = configuredWeight;
        }
#endif
    }

    [Serializable]
    public sealed class ExplorationSpawnStageDefinition
    {
        [SerializeField, Min(1)] private int startRound = 1;
        [SerializeField, Min(1)] private int maximumAlive = 5;
        [SerializeField, Min(0f)] private float respawnDelay = 10f;
        [SerializeField] private ExplorationEnemyWeightDefinition[] enemies =
            Array.Empty<ExplorationEnemyWeightDefinition>();

        public int StartRound => startRound;
        public int MaximumAlive => maximumAlive;
        public float RespawnDelay => respawnDelay;
        public IReadOnlyList<ExplorationEnemyWeightDefinition> Enemies =>
            enemies ?? Array.Empty<ExplorationEnemyWeightDefinition>();

        public bool TryCreateSnapshot(
            IEnemyDefinitionResolver enemyResolver,
            out ExplorationSpawnStage stage,
            out string error)
        {
            stage = null;
            error = string.Empty;
            if (enemyResolver == null)
            {
                error = "Enemy definitions are required for spawn balance.";
                return false;
            }

            ExplorationEnemyWeightDefinition[] source =
                enemies ?? Array.Empty<ExplorationEnemyWeightDefinition>();
            ExplorationEnemyWeight[] entries =
                new ExplorationEnemyWeight[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                ExplorationEnemyWeightDefinition definition = source[i];
                if (definition == null)
                {
                    error = $"Spawn stage contains a null enemy at index {i}.";
                    return false;
                }

                if (!enemyResolver.TryResolve(
                        definition.EnemyId,
                        out EnemyArchetype _))
                {
                    error =
                        $"Spawn stage references unknown enemy '{definition.EnemyId}'.";
                    return false;
                }

                entries[i] = new ExplorationEnemyWeight(
                    definition.EnemyId,
                    definition.Weight);
            }

            return ExplorationSpawnStage.TryCreate(
                startRound,
                maximumAlive,
                respawnDelay,
                entries,
                out stage,
                out error);
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int configuredStartRound,
            int configuredMaximumAlive,
            float configuredRespawnDelay,
            ExplorationEnemyWeightDefinition[] configuredEnemies)
        {
            startRound = configuredStartRound;
            maximumAlive = configuredMaximumAlive;
            respawnDelay = configuredRespawnDelay;
            enemies = configuredEnemies ??
                Array.Empty<ExplorationEnemyWeightDefinition>();
        }
#endif
    }

    [Serializable]
    public sealed class ExplorationSpawnProfileDefinition
    {
        [SerializeField] private string profileId;
        [SerializeField] private ExplorationSpawnStageDefinition[] stages =
            Array.Empty<ExplorationSpawnStageDefinition>();

        public string ProfileId => profileId ?? string.Empty;
        public IReadOnlyList<ExplorationSpawnStageDefinition> Stages =>
            stages ?? Array.Empty<ExplorationSpawnStageDefinition>();

        public bool TryCreateSnapshot(
            IEnemyDefinitionResolver enemyResolver,
            out ExplorationSpawnProfile profile,
            out string error)
        {
            profile = null;
            error = string.Empty;
            ExplorationSpawnStageDefinition[] source =
                stages ?? Array.Empty<ExplorationSpawnStageDefinition>();
            ExplorationSpawnStage[] snapshots =
                new ExplorationSpawnStage[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                ExplorationSpawnStageDefinition definition = source[i];
                if (definition == null)
                {
                    error =
                        $"Spawn profile '{ProfileId}' contains a null stage at index {i}.";
                    return false;
                }

                if (!definition.TryCreateSnapshot(
                        enemyResolver,
                        out snapshots[i],
                        out string stageError))
                {
                    error = $"Spawn profile '{ProfileId}' is invalid: {stageError}";
                    return false;
                }
            }

            return ExplorationSpawnProfile.TryCreate(
                ProfileId,
                snapshots,
                out profile,
                out error);
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string configuredProfileId,
            ExplorationSpawnStageDefinition[] configuredStages)
        {
            profileId = configuredProfileId;
            stages = configuredStages ??
                Array.Empty<ExplorationSpawnStageDefinition>();
        }
#endif
    }

    [CreateAssetMenu(
        fileName = "ExplorationSpawnBalance",
        menuName = "Titanhold/Enemies/Exploration Spawn Balance")]
    public sealed class ExplorationSpawnBalanceDefinition : ScriptableObject
    {
        [SerializeField] private ExplorationSpawnProfileDefinition[] profiles =
            Array.Empty<ExplorationSpawnProfileDefinition>();

        public IReadOnlyList<ExplorationSpawnProfileDefinition> Profiles =>
            profiles ?? Array.Empty<ExplorationSpawnProfileDefinition>();

        public bool TryCreateTable(
            IEnemyDefinitionResolver enemyResolver,
            out ExplorationSpawnBalanceTable table,
            out string error)
        {
            table = null;
            error = string.Empty;
            ExplorationSpawnProfileDefinition[] source =
                profiles ?? Array.Empty<ExplorationSpawnProfileDefinition>();
            ExplorationSpawnProfile[] snapshots =
                new ExplorationSpawnProfile[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                ExplorationSpawnProfileDefinition definition = source[i];
                if (definition == null)
                {
                    error =
                        $"Exploration spawn balance '{name}' has a null profile at index {i}.";
                    return false;
                }

                if (!definition.TryCreateSnapshot(
                        enemyResolver,
                        out snapshots[i],
                        out string profileError))
                {
                    error =
                        $"Exploration spawn balance '{name}' is invalid: {profileError}";
                    return false;
                }
            }

            return ExplorationSpawnBalanceTable.TryCreate(
                snapshots,
                out table,
                out error);
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            ExplorationSpawnProfileDefinition[] configuredProfiles)
        {
            profiles = configuredProfiles ??
                Array.Empty<ExplorationSpawnProfileDefinition>();
        }
#endif
    }
}
