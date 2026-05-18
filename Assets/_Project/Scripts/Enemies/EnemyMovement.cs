using GLTFast.Schema;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour, IMovable
{
    private NavMeshAgent agent;
    private EnemyAnimator animator;

    public bool IsMoving => agent.velocity.sqrMagnitude > 0.01f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<EnemyAnimator>();
        agent.updateRotation = false;
    }
    private void UpdateRotation()
    {
        Vector3 velocity = agent.velocity;

        // если двигаемся → смотрим по направлению движения
        if (velocity.sqrMagnitude > 0.01f)
        {
            RotateTowards(transform.position + velocity);
        }
    }

    public void Tick()
    {
        UpdateRotation();
        animator.SetSpeed(agent.velocity.magnitude);
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
            Time.deltaTime * 10f // скорость поворота
        );
    }
}
