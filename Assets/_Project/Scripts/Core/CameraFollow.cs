using UnityEngine;

/// <summary>
/// Smoothly follows a target transform with configurable offset from PlayerConfig.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private PlayerConfig playerConfig;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -10);
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 fixedRotation = new Vector3(45f, 0f, 0f);


    private void Awake()
    {
        if (playerConfig == null)
        {
            Debug.LogError("PlayerConfig is not assigned in CameraFollow on " + gameObject.name);
            enabled = false;
        }
    }

    private void Start()
    {
        if (target == null || playerConfig == null)
        {
            return;
        }

        transform.position = target.position + playerConfig.CameraOffset;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 🔥 следуем за игроком
        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        // 🔥 фиксированный угол (НЕ LookAt)
        transform.rotation = Quaternion.Euler(fixedRotation);

    }
}
