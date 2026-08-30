using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Titanhold.Run
{
    [Serializable]
    public sealed class AssaultWaveSpawnGroup
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField, Min(1)] private int enemyCount = 1;
        [SerializeField, Min(0f)] private float delayBeforeGroup;
        [SerializeField, Min(0f)] private float spawnInterval = 0.5f;

        public GameObject EnemyPrefab => enemyPrefab;
        public int EnemyCount => enemyCount;
        public float DelayBeforeGroup => delayBeforeGroup;
        public float SpawnInterval => spawnInterval;
    }

    public readonly struct AssaultWaveSpawnStep
    {
        public AssaultWaveSpawnStep(
            GameObject enemyPrefab,
            int enemyCount,
            float delayBeforeGroup,
            float spawnInterval)
        {
            EnemyPrefab = enemyPrefab;
            EnemyCount = enemyCount;
            DelayBeforeGroup = delayBeforeGroup;
            SpawnInterval = spawnInterval;
        }

        public GameObject EnemyPrefab { get; }
        public int EnemyCount { get; }
        public float DelayBeforeGroup { get; }
        public float SpawnInterval { get; }
    }

    public sealed class AssaultWavePlan
    {
        private readonly ReadOnlyCollection<AssaultWaveSpawnStep> steps;

        internal AssaultWavePlan(
            float initialDelay,
            AssaultWaveSpawnStep[] steps,
            int plannedEnemyCount)
        {
            InitialDelay = initialDelay;
            this.steps = Array.AsReadOnly(steps);
            PlannedEnemyCount = plannedEnemyCount;
        }

        public float InitialDelay { get; }
        public IReadOnlyList<AssaultWaveSpawnStep> Steps => steps;
        public int PlannedEnemyCount { get; }
    }

    [CreateAssetMenu(
        fileName = "AssaultWaveDefinition",
        menuName = "Titanhold/Run/Assault Wave Definition")]
    public sealed class AssaultWaveDefinition : ScriptableObject
    {
        [SerializeField, Min(0f)] private float initialDelay = 1f;
        [SerializeField] private AssaultWaveSpawnGroup[] spawnGroups =
            Array.Empty<AssaultWaveSpawnGroup>();

        public float InitialDelay => initialDelay;

        public bool TryCreatePlan(out AssaultWavePlan plan, out string error)
        {
            plan = null;

            if (!IsFiniteNonNegative(initialDelay))
            {
                error = "Initial delay must be finite and non-negative.";
                return false;
            }

            if (spawnGroups == null || spawnGroups.Length == 0)
            {
                error = "At least one spawn group is required.";
                return false;
            }

            AssaultWaveSpawnStep[] steps =
                new AssaultWaveSpawnStep[spawnGroups.Length];
            long plannedEnemyCount = 0;

            for (int i = 0; i < spawnGroups.Length; i++)
            {
                AssaultWaveSpawnGroup group = spawnGroups[i];
                if (group == null)
                {
                    error = $"Spawn group {i} is missing.";
                    return false;
                }

                if (group.EnemyPrefab == null)
                {
                    error = $"Spawn group {i} has no enemy prefab.";
                    return false;
                }

                if (group.EnemyPrefab.GetComponentInChildren<EnemyDeathNotifier>(true) == null)
                {
                    error = $"Spawn group {i} prefab has no EnemyDeathNotifier.";
                    return false;
                }

                if (group.EnemyCount <= 0)
                {
                    error = $"Spawn group {i} enemy count must be positive.";
                    return false;
                }

                if (!IsFiniteNonNegative(group.DelayBeforeGroup) ||
                    !IsFiniteNonNegative(group.SpawnInterval))
                {
                    error = $"Spawn group {i} timings must be finite and non-negative.";
                    return false;
                }

                plannedEnemyCount += group.EnemyCount;
                if (plannedEnemyCount > int.MaxValue)
                {
                    error = "Planned enemy count exceeds the supported range.";
                    return false;
                }

                steps[i] = new AssaultWaveSpawnStep(
                    group.EnemyPrefab,
                    group.EnemyCount,
                    group.DelayBeforeGroup,
                    group.SpawnInterval);
            }

            plan = new AssaultWavePlan(
                initialDelay,
                steps,
                (int)plannedEnemyCount);
            error = string.Empty;
            return true;
        }

        private void OnValidate()
        {
            if (!IsFiniteNonNegative(initialDelay))
                initialDelay = 0f;
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
