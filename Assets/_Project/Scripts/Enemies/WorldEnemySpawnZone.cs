using System.Collections;
using System.Collections.Generic;
using Titanhold.Enemies;
using Titanhold.Run;
using UnityEngine;
using UnityEngine.AI;

public sealed class WorldEnemySpawnZone : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int maxAlive = 5;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private float respawnDelay = 10f;
    [SerializeField] private float navMeshSampleDistance = 2f;
    [SerializeField] private int maxSpawnAttempts = 10;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private RunFlowRuntime runFlowRuntime;

    private readonly HashSet<EnemyDeathNotifier> aliveEnemies = new HashSet<EnemyDeathNotifier>();
    private readonly HashSet<RespawnOperation> respawnOperations = new();
    private readonly EnemyScalingApplicator scalingApplicator = new EnemyScalingApplicator();
    private int appliedRound;

    public RunFlowRuntime RunFlowRuntime => runFlowRuntime;

    private void OnEnable()
    {
        if (runFlowRuntime == null)
            return;

        runFlowRuntime.StateChanged += HandleRunFlowStateChanged;
        appliedRound = runFlowRuntime.State.RoundNumber;
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            FillToMaxAlive();
        }
    }

    private void OnDisable()
    {
        if (runFlowRuntime != null)
            runFlowRuntime.StateChanged -= HandleRunFlowStateChanged;

        foreach (EnemyDeathNotifier notifier in aliveEnemies)
        {
            if (notifier != null)
            {
                notifier.Died -= HandleEnemyDied;
            }
        }

        aliveEnemies.Clear();

        foreach (RespawnOperation operation in respawnOperations)
        {
            if (operation.Coroutine != null)
            {
                StopCoroutine(operation.Coroutine);
            }
        }

        respawnOperations.Clear();
    }

    private void FillToMaxAlive()
    {
        while (aliveEnemies.Count < maxAlive)
        {
            if (!SpawnEnemy())
                break;
        }
    }

    private bool SpawnEnemy()
    {
        if (enemyPrefab == null)
            return false;

        if (aliveEnemies.Count >= maxAlive)
            return false;

        if (!TryGetSpawnPosition(out Vector3 position))
            return false;

        GameObject createdEnemy = Instantiate(enemyPrefab, position, transform.rotation);
        EnemyDeathNotifier notifier = createdEnemy.GetComponentInChildren<EnemyDeathNotifier>();

        if (notifier == null)
        {
            Destroy(createdEnemy);
            return false;
        }

        if (!TryInitializeDefinition(createdEnemy))
        {
            Destroy(createdEnemy);
            return false;
        }

        if (!TryApplyCurrentRoundScaling(createdEnemy, restoreFullHealth: true))
        {
            Destroy(createdEnemy);
            return false;
        }

        ExplorationAggroTargetProvider targetProvider =
            createdEnemy.GetComponentInChildren<
                ExplorationAggroTargetProvider>(true);
        ExplorationTargetRegistry targetRegistry =
            runFlowRuntime != null
                ? runFlowRuntime.GetComponent<ExplorationTargetRegistry>()
                : null;
        if (targetProvider != null && targetRegistry != null)
            targetProvider.Bind(targetRegistry);

        aliveEnemies.Add(notifier);
        notifier.Died += HandleEnemyDied;
        return true;
    }

    private bool TryInitializeDefinition(GameObject enemyObject)
    {
        EnemyDefinitionInitializationResult result =
            EnemyDefinitionInstanceInitializer.TryInitialize(
                enemyObject,
                runFlowRuntime != null
                    ? runFlowRuntime.EnemyDefinitions
                    : null);
        if (result.Success)
            return true;

        Debug.LogError(
            $"Could not initialize exploration enemy '{enemyObject.name}' " +
            $"from definition '{result.EnemyId}': {result.Error} " +
            $"({result.ApplicationError}).",
            this);
        return false;
    }

    private void HandleRunFlowStateChanged(RunFlowState state)
    {
        if (state.Phase != RunPhase.Exploration || state.RoundNumber == appliedRound)
            return;

        appliedRound = state.RoundNumber;
        foreach (EnemyDeathNotifier notifier in aliveEnemies)
        {
            if (notifier != null)
            {
                TryApplyCurrentRoundScaling(
                    notifier.transform.root.gameObject,
                    restoreFullHealth: true);
            }
        }
    }

    private bool TryApplyCurrentRoundScaling(
        GameObject enemyObject,
        bool restoreFullHealth)
    {
        if (runFlowRuntime == null)
            return true;

        Health health = enemyObject.GetComponentInChildren<Health>(true);
        EnemyCombat combat = enemyObject.GetComponentInChildren<EnemyCombat>(true);
        EnemyScalingResult result = scalingApplicator.TryApply(
            health,
            combat,
            runFlowRuntime.State.RoundScaling,
            restoreFullHealth);
        if (result.Success)
            return true;

        Debug.LogError(
            $"Could not apply round scaling to exploration enemy '{enemyObject.name}': {result.Error}.",
            this);
        return false;
    }

    private bool TryGetSpawnPosition(out Vector3 position)
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }
        }

        position = transform.position;
        return false;
    }

    private void HandleEnemyDied(EnemyDeathNotifier notifier)
    {
        if (!aliveEnemies.Remove(notifier))
            return;

        notifier.Died -= HandleEnemyDied;
        StartRespawnTimer();
    }

    private void StartRespawnTimer()
    {
        RespawnOperation operation = new();
        respawnOperations.Add(operation);
        operation.Coroutine = StartCoroutine(RespawnAfterDelay(operation));
    }

    private IEnumerator RespawnAfterDelay(RespawnOperation operation)
    {
        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        try
        {
            SpawnEnemy();
        }
        finally
        {
            respawnOperations.Remove(operation);
        }
    }

    private sealed class RespawnOperation
    {
        public Coroutine Coroutine { get; set; }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
