using System.Collections;
using UnityEngine;
using UnityEngine.Windows;

/// <summary>
/// Handles player movement using CharacterController
/// <summary>
public class PlayerMovementWASD : MonoBehaviour
{
    [SerializeField] private PlayerConfig playerConfig;
    private CharacterController characterController;
    private PlayerInputWASD input;
    private Animator animator;
    private float verticalVelocity;
    private float groundStickVelocity = 2f;
    private float gravity = 9.81f;
    private bool movementBlocked;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        input = GetComponent<PlayerInputWASD>();
        if (input == null)
        {
            Debug.LogError("PlayerInput is missing on " + gameObject.name);
        }
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
    }


    private void HandleMovement()
    {
        if (movementBlocked)
        {
            return;
        }
        Vector3 inputVector = input != null ? input.MoveDirection : Vector3.zero;
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

    public void BlockMovement(bool block) => movementBlocked = block;
}
