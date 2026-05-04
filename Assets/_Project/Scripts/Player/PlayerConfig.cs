using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Configs/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    [SerializeField] public float MoveSpeed = 6f;
    [SerializeField] public float RotationSpeed = 10f;
    [SerializeField] public Vector3 CameraOffset = new Vector3(0, 12, -8);
    [SerializeField] public Vector3 CameraLookAtOffset = new Vector3(0, 1.5f, 0);
    [SerializeField] public float ClampX = 9.5f;
    [SerializeField] public float ClampZ = 9.5f;
}
