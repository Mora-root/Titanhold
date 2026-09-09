using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour, IMovable
{
    private NavMeshAgent agent;
    private EnemyAnimator animator;
    private CharacterStats definitionStats;
    private float rotationSpeed = 10f;
    private bool usesDefinitionStats;

    public bool IsMoving => agent.velocity.sqrMagnitude > 0.01f;
    public float MovementSpeed => usesDefinitionStats
        ? Mathf.Max(0f, definitionStats.GetValue(StatType.MoveSpeed))
        : agent != null
            ? agent.speed
            : 0f;
    public float RotationSpeed => rotationSpeed;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<EnemyAnimator>();
        agent.updateRotation = false;
    }
    private void UpdateRotation()
    {
        Vector3 velocity = agent.velocity;

        // if we are moving - we are looking in the direction of movement
        if (velocity.sqrMagnitude > 0.01f)
        {
            RotateTowards(transform.position + velocity);
        }
    }

    public void Tick()
    {
        SyncMovementSpeed();
        UpdateRotation();
        animator.SetSpeed(agent.velocity.magnitude);
    }

    internal void UseDefinitionStats(
        CharacterStats configuredStats,
        float configuredRotationSpeed)
    {
        definitionStats = configuredStats;
        rotationSpeed = configuredRotationSpeed;
        usesDefinitionStats = true;
        SyncMovementSpeed();
    }

    public void MoveTo(Vector3 position)
    {
        agent.isStopped = false;
        agent.SetDestination(position);
    }

    public void Stop()
    {
        agent.isStopped = true;
        agent.ResetPath();
    }
    public void RotateTowards(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    private void SyncMovementSpeed()
    {
        if (!usesDefinitionStats || agent == null)
            return;

        float targetSpeed = MovementSpeed;
        if (!Mathf.Approximately(agent.speed, targetSpeed))
            agent.speed = targetSpeed;
    }
}
