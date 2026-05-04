using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerConfig _playerConfig;
    private CharacterController _characterController;
    private Transform _transform;
    private float _verticalVelocity;
    private float _groundStickVelocity = 2f;
    private float _gravity = 9.81f;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _transform = transform;
        if (_characterController == null)
        {
            Debug.LogError("CharacterController is missing on " + gameObject.name);
            enabled = false;
        }
        if (_playerConfig == null)
        {
            Debug.LogError("PlayerConfig is not assigned in PlayerMovement on " + gameObject.name);
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // Handles player movement using CharacterController
        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");
        Vector3 inputVector = new Vector3(horizontal, 0f, vertical);
        inputVector = inputVector.normalized;
        if (inputVector.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputVector);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, _playerConfig.RotationSpeed * Time.deltaTime);
            float moveDistance = _playerConfig.MoveSpeed * Time.deltaTime;
            Vector3 move = inputVector * moveDistance;
            _characterController.Move(move);
        }
        if (_characterController.isGrounded)
        {
            _verticalVelocity = -_groundStickVelocity;
        }
        else
        {
            _verticalVelocity -= _gravity * Time.deltaTime;
        }
        _characterController.Move(Vector3.up * (_verticalVelocity * Time.deltaTime));

        Vector3 pos = _transform.position;
        pos.x = Mathf.Clamp(pos.x, -_playerConfig.ClampX, _playerConfig.ClampX);
        pos.z = Mathf.Clamp(pos.z, -_playerConfig.ClampZ, _playerConfig.ClampZ);
        _transform.position = pos;
    }
}
