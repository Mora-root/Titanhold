using System.Collections;
using System.Collections.Generic;
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

    private readonly HashSet<EnemyDeathNotifier> aliveEnemies = new HashSet<EnemyDeathNotifier>();
    private readonly List<Coroutine> respawnCoroutines = new List<Coroutine>();

    private void Start()
    {
        if (spawnOnStart)
        {
            FillToMaxAlive();
        }
    }

    private void OnDisable()
    {
        foreach (EnemyDeathNotifier notifier in aliveEnemies)
        {
            if (notifier != null)
            {
                notifier.Died -= HandleEnemyDied;
            }
        }

        aliveEnemies.Clear();

        foreach (Coroutine coroutine in respawnCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }

        respawnCoroutines.Clear();
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

        aliveEnemies.Add(notifier);
        notifier.Died += HandleEnemyDied;
        return true;
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
        Coroutine coroutine = StartCoroutine(RespawnAfterDelay());
        respawnCoroutines.Add(coroutine);
    }

    private IEnumerator RespawnAfterDelay()
    {
        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        SpawnEnemy();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
