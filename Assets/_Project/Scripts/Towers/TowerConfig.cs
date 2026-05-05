using UnityEngine;

[CreateAssetMenu(fileName = "TowerConfig", menuName = "Configs/TowerConfig")]
public class TowerConfig : ScriptableObject
{
    public float Range = 50f;
    public float FireRate = 1f;
    public float RotationSpeed = 5f;
    public float AimTolerance = 15f;
    public float TargetRefreshInterval = 0.15f;
    public LayerMask TargetMask = ~0;
    public GameObject projectilePrefab;
    public ProjectileConfig projectileConfig;

}
