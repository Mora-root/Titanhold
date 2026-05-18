using UnityEngine;
using UnityEngine.AI;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 10f;

    private NavMeshAgent agent;
    private PlayerAnimator animator;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<PlayerAnimator>();

        agent.updateRotation = false;
    }

    public void Tick()
    {
        animator.SetSpeed(agent.velocity.magnitude);

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
        agent.isStopped = true;
        agent.ResetPath();
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
}
