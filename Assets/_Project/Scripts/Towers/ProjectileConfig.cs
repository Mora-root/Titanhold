using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileConfig", menuName = "Configs/ProjectileConfig")]
public class ProjectileConfig : ScriptableObject
{
    public float Speed = 15f;
    public float Damage = 25f;
    public float Lifetime = 3f;
    public float HomingStrength = 10f;
}

