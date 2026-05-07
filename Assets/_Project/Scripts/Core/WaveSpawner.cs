using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [SerializeField] private WaveConfig[] waves;
    [SerializeField] private WaypointPath defaultPath;
    [SerializeField] private Transform spawnPoint;

    [Header("UI (optional)")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI enemyCountText;

    private int currentWaveIndex = -1;
    private readonly List<Enemy> aliveEnemies = new List<Enemy>();
    private bool allWavesComplete;

    private void Start()
    {
        if (waves.Length == 0)
        {
            Debug.LogError("No waves assigned to WaveSpawner!");
            enabled = false;
            return;
        }
        StartNextWave();
        UpdateUI(null);
    }

    private void StartNextWave()
    {
        currentWaveIndex++;
        if (currentWaveIndex >= waves.Length)
        {
            AllWavesCompleted();
            return;
        }
        WaveConfig wave = waves[currentWaveIndex];
        StartCoroutine(SpawnWave(wave));

    }

    private IEnumerator SpawnWave(WaveConfig wave)
    {
        yield return new WaitForSeconds(wave.StartDelay);

        UpdateUI(wave);

        for (int i = 0; i < wave.EnemyCount; i++)
        {
            SpawnEnemy(wave.EnemyConfig);
            yield return new WaitForSeconds(wave.SpawnInterval);
        }
    }

    private void SpawnEnemy(EnemyConfig config)
    {
        if (config.EnemyPrefab == null)
        {
            Debug.LogError("EnemyConfig has no EnemyPrefab assigned!");
            return;
        }

        GameObject enemyObj = Instantiate(config.EnemyPrefab, spawnPoint.position, spawnPoint.rotation);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        EnemyAgentMovement movement = enemyObj.GetComponent<EnemyAgentMovement>();

        if (enemy == null || movement == null)
        {
            Debug.LogError("Enemy prefab is missing Enemy or EnemyAgentMovement component!");
            Destroy(enemyObj);
            return;
        }
        movement.Initialize(config, defaultPath);
        enemy.OnDied += OnEnemyDied;
        aliveEnemies.Add(enemy);
        UpdateUI(null);
    }

    private void OnEnemyDied(Enemy enemy)
    {
        enemy.OnDied -= OnEnemyDied;
        aliveEnemies.Remove(enemy);

        UpdateUI(null);

        if (aliveEnemies.Count == 0 && !allWavesComplete)
        {
            StartNextWave();
        }
    }

    private void AllWavesCompleted()
    {
        allWavesComplete = true;
        if (waveText != null)
            waveText.text = "All Waves Defeated!";
        Debug.Log("Victory! All waves completed.");
    }

    private void UpdateUI(WaveConfig currentWave = null)
    {
        if (waveText != null && currentWave != null)
            waveText.text = $"Wave {currentWaveIndex + 1}";
        if (enemyCountText != null)
            enemyCountText.text = $"Enemies {aliveEnemies.Count}";
    }

    public int CurrentWave => currentWaveIndex + 1;
    public int TotalWaves => waves.Length;
}
