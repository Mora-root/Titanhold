using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Configs/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    public float MoveSpeed = 3f;
    public float MaxHealth = 100f;
    public float DamageToPlayer = 10f;
    public GameObject EnemyPrefab;
    public float EnemyAttackCooldown = 1f;
    public float EnemyAttackRange = 1f;
}
