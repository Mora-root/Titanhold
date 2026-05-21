using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private CameraRigController cameraRig;

    private bool isRotatingCamera;
    private float multiplierRotation = 4f;

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isRotatingCamera = true;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isRotatingCamera = false;
        }

        if (isRotatingCamera)
        {
            float mouseX = Input.GetAxis("Mouse X");
            cameraRig.Rotate(mouseX * multiplierRotation);
        }
    }
}