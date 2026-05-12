using UnityEngine;

/// <summary>
/// Handles player movement using CharacterController
/// <summary>
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerConfig playerConfig;
    private CharacterController characterController;
    private Animator animator;
    private float verticalVelocity;
    private float groundStickVelocity = 2f;
    private float gravity = 9.81f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController is missing on " + gameObject.name);
            enabled = false;
        }
        if (playerConfig == null)
        {
            Debug.LogError("PlayerConfig is not assigned in PlayerMovement on " + gameObject.name);
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        HandleMovement();
        // Test
        if (Input.GetKeyDown(KeyCode.Mouse0) && animator != null)
            animator.SetTrigger("Attack");
    }

    private void HandleMovement()
    {
        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");
        Vector3 inputVector = new Vector3(horizontal, 0f, vertical);
        float inputMagnitude = inputVector.magnitude;
        inputVector = inputVector.normalized;
        if (animator != null)
        {
            animator.SetFloat("Speed", inputMagnitude);
        }
        if (inputVector.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputVector);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, playerConfig.RotationSpeed * Time.deltaTime);
            float moveDistance = playerConfig.MoveSpeed * Time.deltaTime;
            Vector3 move = inputVector * moveDistance;
            characterController.Move(move);
        }
        if (characterController.isGrounded)
        {
            verticalVelocity = -groundStickVelocity;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        characterController.Move(Vector3.up * (verticalVelocity * Time.deltaTime));

        //Vector3 pos = transform.position;
        //pos.x = Mathf.Clamp(pos.x, -playerConfig.ClampX, playerConfig.ClampX);
        //pos.z = Mathf.Clamp(pos.z, -playerConfig.ClampZ, playerConfig.ClampZ);
        //transform.position = pos;
    }
}
