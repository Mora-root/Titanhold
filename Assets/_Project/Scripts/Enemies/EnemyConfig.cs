using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Configs/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    public float MoveSpeed = 3f;
    public float MaxHealth = 100f;
    public float damegeToPlayer = 10f;
}
