using UnityEngine;
using UnityEngine.AI;
using Titanhold.Combat.Abilities;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 10f;

    private NavMeshAgent agent;
    private PlayerAnimator animator;
    private CharacterStats stats;
    private float fallbackMoveSpeed;
    private float fallbackAcceleration;
    private bool fallbackAutoBraking;
    private float abilityMoveSpeedMultiplier = 1f;
    private bool hasAbilityMoveSpeedOverride;

    public bool IsStopped => agent == null || agent.isStopped;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<PlayerAnimator>();
        stats = GetComponent<CharacterStats>();
        fallbackMoveSpeed = agent != null ? agent.speed : 0f;
        fallbackAcceleration = agent != null ? agent.acceleration : 0f;
        fallbackAutoBraking = agent != null && agent.autoBraking;

        agent.updateRotation = false;
        ApplyMoveSpeed();
    }

    private void OnEnable()
    {
        if (stats != null)
            stats.OnStatChanged += HandleStatChanged;
    }

    private void OnDisable()
    {
        if (stats != null)
            stats.OnStatChanged -= HandleStatChanged;
    }

    public void Tick()
    {
        float currentSpeed = agent.velocity.magnitude;
        animator.SetSpeed(currentSpeed);
        animator.SetLocomotionPlaybackSpeed(GetLocomotionPlaybackSpeed(currentSpeed));

        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            RotateTowards(transform.position + agent.velocity);
        }
    }

    public void MoveTo(Vector3 pos)
    {
        agent.isStopped = false;
        agent.SetDestination(pos);
    }

    public void Stop()
    {
        agent.velocity = Vector3.zero;
        agent.ResetPath();
        agent.isStopped = true;
    }

    public bool MoveForAbility(AbilityMovementDirective movement)
    {
        if (agent == null || !agent.isOnNavMesh)
            return false;

        if (!hasAbilityMoveSpeedOverride ||
            !Mathf.Approximately(
                abilityMoveSpeedMultiplier,
                movement.SpeedMultiplier))
        {
            hasAbilityMoveSpeedOverride = true;
            abilityMoveSpeedMultiplier = movement.SpeedMultiplier;
            agent.autoBraking = false;
            ApplyMoveSpeed();
        }

        Vector3 offset = movement.Destination - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude <=
            movement.ArrivalDistance * movement.ArrivalDistance)
        {
            Stop();
            return true;
        }

        Vector3 destination = movement.Destination -
                              offset.normalized *
                              movement.ArrivalDistance;
        MoveTo(destination);
        return true;
    }

    public void EndAbilityMovement()
    {
        if (!hasAbilityMoveSpeedOverride)
            return;

        hasAbilityMoveSpeedOverride = false;
        abilityMoveSpeedMultiplier = 1f;
        if (agent != null)
            agent.autoBraking = fallbackAutoBraking;
        ApplyMoveSpeed();
    }

    public void RotateTowards(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        dir.y = 0;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rot,
            Time.deltaTime * rotationSpeed
        );
    }

    public bool HasReachedDestination()
    {
        if (agent.pathPending) return false;

        if (agent.remainingDistance > agent.stoppingDistance)
            return false;

        if (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f)
            return false;

        return true;
    }

    private void HandleStatChanged(StatType type)
    {
        if (type == StatType.MoveSpeed)
            ApplyMoveSpeed();
    }

    private void ApplyMoveSpeed()
    {
        if (agent == null)
            return;

        float moveSpeed = stats != null ? stats.GetValue(StatType.MoveSpeed) : 0f;
        float resolvedSpeed = moveSpeed > 0f
            ? moveSpeed
            : fallbackMoveSpeed;
        agent.speed = resolvedSpeed *
                      (hasAbilityMoveSpeedOverride
                          ? abilityMoveSpeedMultiplier
                          : 1f);
        agent.acceleration = hasAbilityMoveSpeedOverride
            ? Mathf.Max(fallbackAcceleration, agent.speed * 10f)
            : fallbackAcceleration;
    }

    private float GetLocomotionPlaybackSpeed(float currentSpeed)
    {
        if (currentSpeed <= 0.05f)
            return 1f;

        float baseSpeed = fallbackMoveSpeed > 0f ? fallbackMoveSpeed : agent.speed;
        if (baseSpeed <= 0f)
            return 1f;

        return Mathf.Max(0.01f, currentSpeed / baseSpeed);
    }
}
