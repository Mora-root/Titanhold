using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Configs/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    public float MoveSpeed = 6f;
    public float RotationSpeed = 10f;
    public Vector3 CameraOffset = new Vector3(0, 12, -8);
    public Vector3 CameraLookAtOffset = new Vector3(0, 1.5f, 0);
    public float ClampX = 9.5f;
    public float ClampZ = 9.5f;
    public float AttackDuration = .5f;
    public float AttackRange = 1.5f;
    public float Acceleration = 500;
    public float Damage = 25f;
    public float AttackCooldown = 1f;
}
