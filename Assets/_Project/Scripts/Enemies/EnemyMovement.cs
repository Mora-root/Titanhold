using GLTFast.Schema;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour, IMovable
{
    private NavMeshAgent agent;
    private EnemyAnimator animator;
    private float rotationSpeed = 10f;

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

        // if we are moving - we are looking in the direction of movement
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
            Time.deltaTime * rotationSpeed
        );
    }
}
