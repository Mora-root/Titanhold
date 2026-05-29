using UnityEngine;

public sealed class CampDefenseEnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int enemyCount = 3;
    [SerializeField] private CampDefenseEnemyRegistry enemyRegistry;

    public int EnemyCount => enemyCount;
    public bool CanSpawn => enemyPrefab != null && spawnPoints != null && spawnPoints.Length > 0;

    private void Awake()
    {
        enemyRegistry ??= GetComponent<CampDefenseEnemyRegistry>();
    }

    public int SpawnEnemies()
    {
        if (!CanSpawn)
            return 0;

        if (enemyRegistry == null)
            return 0;

        int registeredCount = 0;

        for (int i = 0; i < enemyCount; i++)
        {
            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];
            GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            EnemyDeathNotifier notifier = enemy.GetComponentInChildren<EnemyDeathNotifier>();

            if (notifier == null)
            {
                Destroy(enemy);
                continue;
            }

            if (!enemyRegistry.Register(notifier))
            {
                Destroy(enemy);
                continue;
            }

            registeredCount++;
        }

        return registeredCount;
    }
}
