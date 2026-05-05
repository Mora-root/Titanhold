using UnityEngine;

[CreateAssetMenu(fileName = "TowerConfig", menuName = "Configs/TowerConfig")]
public class TowerConfig : ScriptableObject
{
    public float Range = 50f;
    public float FireRate = 1f;
    public float RotationSpeed = 5f;
    public GameObject projectilePrefab;
    public ProjectileConfig projectileConfig;
}
