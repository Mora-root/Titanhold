using System.Collections;
using UnityEngine;

public sealed class WorldEnemyRespawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float respawnDelay = 10f;
    [SerializeField] private bool spawnOnStart = true;

    private EnemyDeathNotifier currentEnemy;
    private Coroutine respawnCoroutine;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnEnemy();
        }
    }

    private void OnDisable()
    {
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

        currentEnemy = notifier;
        currentEnemy.Died += HandleEnemyDied;
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
