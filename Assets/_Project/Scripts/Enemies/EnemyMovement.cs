using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyAnimator animator;

    public bool IsMoving => agent.velocity.sqrMagnitude > 0.01f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<EnemyAnimator>();
    }

    public void Tick()
    {
        animator.SetSpeed(agent.velocity.magnitude);
    }

    public void MoveTo(Vector3 pos)
    {
        agent.isStopped = false;
        agent.SetDestination(pos);
    }

    public void Stop()
    {
        agent.isStopped = true;
        agent.ResetPath();
    }
    public bool HasReachedDestination()
    {
        if (agent.pathPending) return false;

        if (agent.remainingDistance > agent.stoppingDistance)
            return false;

        if (agent.hasPath && agent.velocity.sqrMagnitude != 0f)
            return false;

        return true;
    }
}
