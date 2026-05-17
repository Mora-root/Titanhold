using UnityEngine;

/// <summary>
/// Moves the enemy towards a target point while respecting ground and gravity.
/// </summary>
public class EnemyMovementOld : MonoBehaviour
{
    [SerializeField] private EnemyConfig enemyConfig;
    [SerializeField] private Transform target;
    private CharacterController characterController;

    private float verticalVelocity;
    private float groundStickVelocity = 2f;
    private float gravity = 9.81f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            Debug.LogError("CharacterController is missing on " + gameObject.name);
            enabled = false;
        }
        if (enemyConfig == null)
        {
            Debug.LogError("EnemyConfig is not assigned in EnemyMovement on " + gameObject.name);
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
        if (target == null) return;

        Vector3 direction = (target.position - base.transform.position).normalized;
        direction.y = 0f;
        direction = direction.normalized;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(base.transform.rotation, targetRot, 5f * Time.deltaTime);
        }

        Vector3 move = direction * (enemyConfig.MoveSpeed * Time.deltaTime);

        if (characterController.isGrounded)
        {
            verticalVelocity -= groundStickVelocity;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        move.y = verticalVelocity * Time.deltaTime;
        characterController.Move(move);
    }
}
