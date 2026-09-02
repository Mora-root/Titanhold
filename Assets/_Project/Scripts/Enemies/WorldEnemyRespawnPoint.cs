using System.Collections;
using Titanhold.Run;
using UnityEngine;

public sealed class WorldEnemyRespawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float respawnDelay = 10f;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private RunFlowRuntime runFlowRuntime;

    private EnemyDeathNotifier currentEnemy;
    private Coroutine respawnCoroutine;
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
            SpawnEnemy();
        }
    }

    private void OnDisable()
    {
        if (runFlowRuntime != null)
            runFlowRuntime.StateChanged -= HandleRunFlowStateChanged;

        if (currentEnemy != null)
        {
            currentEnemy.Died -= HandleEnemyDied;
        }

        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
        }

        respawnCoroutine = null;
    }

    public void SpawnEnemy()
    {
        if (enemyPrefab == null)
            return;

        if (currentEnemy != null)
            return;

        Transform targetSpawnPoint = spawnPoint != null ? spawnPoint : transform;
        GameObject createdEnemy = Instantiate(enemyPrefab, targetSpawnPoint.position, targetSpawnPoint.rotation);
        EnemyDeathNotifier notifier = createdEnemy.GetComponentInChildren<EnemyDeathNotifier>();

        if (notifier == null)
        {
            Destroy(createdEnemy);
            return;
        }

        if (!TryApplyCurrentRoundScaling(createdEnemy, restoreFullHealth: true))
        {
            Destroy(createdEnemy);
            return;
        }

        currentEnemy = notifier;
        currentEnemy.Died += HandleEnemyDied;
    }

    private void HandleRunFlowStateChanged(RunFlowState state)
    {
        if (state.Phase != RunPhase.Exploration || state.RoundNumber == appliedRound)
            return;

        appliedRound = state.RoundNumber;
        if (currentEnemy != null)
        {
            TryApplyCurrentRoundScaling(
                currentEnemy.transform.root.gameObject,
                restoreFullHealth: true);
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

    private void HandleEnemyDied(EnemyDeathNotifier notifier)
    {
        if (notifier != currentEnemy)
            return;

        notifier.Died -= HandleEnemyDied;
        currentEnemy = null;

        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
        }

        respawnCoroutine = StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        respawnCoroutine = null;
        SpawnEnemy();
    }
}
