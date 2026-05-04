using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private PlayerConfig _playerConfig;
    [SerializeField] private float _smoothSpeed = 5f;
    private Transform _transform;

    private void Awake()
    {
        _transform = transform;
        if (_playerConfig == null)
        {
            Debug.LogError("PlayerConfig is not assigned in CameraFollow on " + gameObject.name);
            enabled = false;
        }
    }

    private void Start()
    {
        if (_target == null || _playerConfig == null)
        {
            return;
        }

        _transform.position = _target.position + _playerConfig.CameraOffset;
    }

    private void LateUpdate()
    {
        if (_target == null || _playerConfig == null)
        {
            return;
        }
        Vector3 desiredPosition = _target.position + _playerConfig.CameraOffset;

        _transform.position = Vector3.Lerp(_transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);

        Vector3 lookAtPoint = _target.position + _playerConfig.CameraLookAtOffset;

        _transform.LookAt(lookAtPoint);
    }
}
