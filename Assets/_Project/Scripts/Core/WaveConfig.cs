using UnityEngine;

[CreateAssetMenu(fileName = "WaveConfig", menuName = "Configs/WaveConfigs")]
public class WaveConfig : ScriptableObject
{
    public EnemyConfig EnemyConfig;
    public int EnemyCount = 10;
    public float SpawnInterval = 0.5f;
    public float StartDelay = 1f;
}
