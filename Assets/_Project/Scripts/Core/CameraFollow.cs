using UnityEngine;

/// <summary>
/// Smoothly follows a target transform with configurable offset from PlayerConfig.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private PlayerConfig playerConfig;
    [SerializeField] private float smoothSpeed = 5f;

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
        if (target == null || playerConfig == null)
        {
            return;
        }
        Vector3 desiredPosition = target.position + playerConfig.CameraOffset;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        Vector3 lookAtPoint = target.position + playerConfig.CameraLookAtOffset;

        transform.LookAt(lookAtPoint);
    }
}
