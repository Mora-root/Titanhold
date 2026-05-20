using UnityEngine;

public class CameraRigController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private float followSmooth = 12f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 180f;

    private float yaw;

    private void Start()
    {
        yaw = transform.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        FollowTarget();
        ApplyRotation();
    }

    public void Rotate(float mouseX)
    {
        yaw += mouseX * rotationSpeed * Time.deltaTime;
    }

    private void FollowTarget()
    {
        Vector3 desiredPosition = target.position;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSmooth * Time.deltaTime
        );
    }

    private void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}
