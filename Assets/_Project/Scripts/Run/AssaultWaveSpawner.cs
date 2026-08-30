using System;
using System.Collections;
using Titanhold.Combat;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunFlowRuntime))]
    [RequireComponent(typeof(AssaultEnemyRegistry))]
    public sealed class AssaultWaveSpawner : MonoBehaviour
    {
        [SerializeField] private RunFlowRuntime runFlowRuntime;
        [SerializeField] private AssaultEnemyRegistry enemyRegistry;
        [SerializeField] private AssaultWaveDefinition waveDefinition;
        [SerializeField] private Transform[] spawnPoints = Array.Empty<Transform>();

        private Coroutine spawnRoutine;
        private AssaultEncounterId activeEncounterId;
        private int spawnedEnemyCount;
        private bool isSpawning;

        public AssaultEncounterId ActiveEncounterId => activeEncounterId;
        public int SpawnedEnemyCount => spawnedEnemyCount;
        public bool IsSpawning => isSpawning;

        public event Action<AssaultWaveStartResult> WaveStarted;
        public event Action<GameObject, CombatActorReference> EnemySpawned;
        public event Action<AssaultEncounterId> SpawnSequenceCompleted;
        public event Action<AssaultWaveSpawnFailure> SpawnFailed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            if (spawnRoutine != null)
                StopCoroutine(spawnRoutine);

            spawnRoutine = null;
            isSpawning = false;
        }

        public AssaultWaveStartResult TryStartWave()
        {
            if (!isActiveAndEnabled)
            {
                return AssaultWaveStartResult.Failed(
                    AssaultWaveStartError.SpawnerInactive);
            }

            if (IsSpawning)
            {
                return AssaultWaveStartResult.Failed(
                    AssaultWaveStartError.AlreadySpawning);
            }

            ResolveReferences();
            if (runFlowRuntime == null)
            {
                return AssaultWaveStartResult.Failed(
                    AssaultWaveStartError.MissingRuntime);
            }

            if (enemyRegistry == null || !enemyRegistry.isActiveAndEnabled)
            {
                return AssaultWaveStartResult.Failed(
                    AssaultWaveStartError.MissingRegistry);
            }

            if (waveDefinition == null)
            {
                return AssaultWaveStartResult.Failed(
                    AssaultWaveStartError.MissingDefinition);
            }

            if (!waveDefinition.TryCreatePlan(
                    out AssaultWavePlan plan,
                    out string definitionError))
            {
                return AssaultWaveStartResult.Failed(
                    AssaultWaveStartError.InvalidDefinition,
                    definitionError);
            }

            if (!HasValidSpawnPoints())
            {
                return AssaultWaveStartResult.Failed(
                    AssaultWaveStartError.MissingSpawnPoints);
            }

            if (runFlowRuntime.State.Phase != RunPhase.TransitionToAssault)
            {
                return AssaultWaveStartResult.Failed(
                    AssaultWaveStartError.InvalidPhase);
            }

            activeEncounterId = new AssaultEncounterId(
                $"assault:{runFlowRuntime.State.RoundNumber}:{Guid.NewGuid():N}");
            AssaultEncounterResult encounterResult =
                runFlowRuntime.AssaultEncounter.TryBegin(
                    new BeginAssaultEncounterCommand(
                        activeEncounterId,
                        runFlowRuntime.State.RoundNumber,
                        plan.PlannedEnemyCount));
            if (!encounterResult.Success)
            {
                activeEncounterId = default;
                return AssaultWaveStartResult.Failed(
                    AssaultWaveStartError.EncounterRejected,
                    encounterResult: encounterResult);
            }

            spawnedEnemyCount = 0;
            isSpawning = true;
            AssaultWaveStartResult result = AssaultWaveStartResult.Succeeded(
                activeEncounterId,
                plan.PlannedEnemyCount,
                encounterResult);
            WaveStarted?.Invoke(result);
            spawnRoutine = StartCoroutine(SpawnWave(plan));
            if (!isSpawning)
                spawnRoutine = null;

            return result;
        }

        private IEnumerator SpawnWave(AssaultWavePlan plan)
        {
            if (plan.InitialDelay > 0f)
                yield return new WaitForSeconds(plan.InitialDelay);

            for (int groupIndex = 0; groupIndex < plan.Steps.Count; groupIndex++)
            {
                AssaultWaveSpawnStep step = plan.Steps[groupIndex];
                if (step.DelayBeforeGroup > 0f)
                    yield return new WaitForSeconds(step.DelayBeforeGroup);

                for (int enemyIndex = 0; enemyIndex < step.EnemyCount; enemyIndex++)
                {
                    if (!TrySpawnEnemy(step.EnemyPrefab, out AssaultWaveSpawnFailure failure))
                    {
                        isSpawning = false;
                        spawnRoutine = null;
                        SpawnFailed?.Invoke(failure);
                        yield break;
                    }

                    bool hasAnotherEnemyInGroup = enemyIndex + 1 < step.EnemyCount;
                    if (hasAnotherEnemyInGroup && step.SpawnInterval > 0f)
                        yield return new WaitForSeconds(step.SpawnInterval);
                }
            }

            isSpawning = false;
            spawnRoutine = null;
            SpawnSequenceCompleted?.Invoke(activeEncounterId);
        }

        private bool TrySpawnEnemy(
            GameObject enemyPrefab,
            out AssaultWaveSpawnFailure failure)
        {
            int sequenceNumber = spawnedEnemyCount + 1;
            Transform spawnPoint = spawnPoints[spawnedEnemyCount % spawnPoints.Length];
            GameObject enemyObject = Instantiate(
                enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation);
            CombatActorReference enemy = new CombatActorReference(
                $"enemy:{activeEncounterId.Value}:{sequenceNumber}",
                CombatActorKind.Enemy);
            EnemyDeathNotifier notifier =
                enemyObject.GetComponentInChildren<EnemyDeathNotifier>(true);

            if (notifier == null)
            {
                failure = new AssaultWaveSpawnFailure(
                    sequenceNumber,
                    AssaultWaveSpawnFailureReason.MissingDeathNotifier,
                    enemyObject,
                    enemy,
                    default);
                Destroy(enemyObject);
                return false;
            }

            if (!notifier.isActiveAndEnabled)
            {
                failure = new AssaultWaveSpawnFailure(
                    sequenceNumber,
                    AssaultWaveSpawnFailureReason.InactiveDeathNotifier,
                    enemyObject,
                    enemy,
                    default);
                Destroy(enemyObject);
                return false;
            }

            AssaultEnemyRegistryResult registration = enemyRegistry.TryRegister(
                notifier,
                activeEncounterId,
                enemy);
            if (!registration.Success)
            {
                failure = new AssaultWaveSpawnFailure(
                    sequenceNumber,
                    AssaultWaveSpawnFailureReason.RegistryRejected,
                    enemyObject,
                    enemy,
                    registration);
                Destroy(enemyObject);
                return false;
            }

            spawnedEnemyCount++;
            EnemySpawned?.Invoke(enemyObject, enemy);
            failure = default;
            return true;
        }

        private bool HasValidSpawnPoints()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return false;

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] == null)
                    return false;
            }

            return true;
        }

        private void ResolveReferences()
        {
            runFlowRuntime ??= GetComponent<RunFlowRuntime>();
            enemyRegistry ??= GetComponent<AssaultEnemyRegistry>();
        }
    }
}
