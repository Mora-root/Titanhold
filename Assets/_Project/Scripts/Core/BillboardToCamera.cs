using UnityEngine;

public sealed class BillboardToCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool useMainCameraFallback = true;
    [SerializeField] private bool reverseForward;
    [SerializeField] private bool keepUpright = true;

    private void LateUpdate()
    {
        Camera cameraToUse = targetCamera;

        if (cameraToUse == null && useMainCameraFallback)
            cameraToUse = Camera.main;

        if (cameraToUse == null)
            return;

        Vector3 direction = transform.position - cameraToUse.transform.position;
        if (reverseForward)
            direction = -direction;

        if (keepUpright)
            direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    public void SetTargetCamera(Camera camera)
    {
        targetCamera = camera;
    }
}
