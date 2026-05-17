using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMovementAI : MonoBehaviour
{
    [SerializeField] private PlayerConfig playerConfig;
    private NavMeshAgent agent;
    private PlayerInputAI input;
    private Animator animator;
    private bool hadAttackTarget;

    public bool IsMovementBlocked { get; set; }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = playerConfig.MoveSpeed;
        agent.acceleration = playerConfig.Acceleration;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        agent.stoppingDistance = 0.1f;
        agent.updateRotation = false;
        input = GetComponent<PlayerInputAI>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {

        //
        Vector3 desiredDirection = Vector3.zero;
        bool rotateToEnemy = input != null && input.HasAttackTarget
                         && (!input.HasTargetPosition || IsMovementBlocked);
        // Приоритет 1: если есть цель атаки и мы не в режиме бега (зажатой ЛКМ)
        if (rotateToEnemy)
        {
            Vector3 toEnemy = input.TargetEnemy.GetTransform().position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > 0.01f)
                desiredDirection = toEnemy.normalized;
        }
        // Приоритет 2: если движемся – смотрим в направлении скорости агента
        else if (agent.velocity.sqrMagnitude > 0.1f)
        {
            desiredDirection = agent.velocity.normalized;
        }

        if (desiredDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, playerConfig.RotationSpeed * Time.deltaTime);
        }

        if (IsMovementBlocked)
        {
            agent.SetDestination(transform.position);
            animator?.SetFloat("Speed", 0f);
            return;
        }
        // Если цель атаки только что пропала (умерла) – сбрасываем движение
        if (hadAttackTarget && !input.HasAttackTarget)
        {
            agent.SetDestination(transform.position);
            input.ClearTargetPosition();
        }
        hadAttackTarget = input.HasAttackTarget;

        bool hasDestination = false;

        // Приоритет 1: есть цель атаки (выбрана кликом) – преследуем её
        if (input != null && input.HasAttackTarget)
        {
            Vector3 targetPos = input.TargetEnemy.GetTransform().position;
            agent.SetDestination(targetPos);
            hasDestination = true;
        }
        // Приоритет 2: есть точка движения (зажатая ЛКМ или клик по земле)
        else if (input != null && input.HasTargetPosition)
        {
            agent.SetDestination(input.TargetPosition);
            hasDestination = true;
        }

        // Если нет ни цели, ни точки – стоим (агент сам не движется)
        if (!hasDestination)
        {
            agent.SetDestination(transform.position);
        }

        float speed = agent.velocity.magnitude;
        animator?.SetFloat("Speed", speed);
    }
}
