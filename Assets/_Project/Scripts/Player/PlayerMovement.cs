using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerConfig config;

    private NavMeshAgent agent;
    private Animator animator;

    public Vector3 Velocity => agent.velocity;
    public bool HasVelocity => agent.velocity.sqrMagnitude > 0.1f;
    public bool IsMoving => agent.velocity.sqrMagnitude > 0.05f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        agent.speed = config.MoveSpeed;
        agent.acceleration = config.Acceleration;
        agent.updateRotation = false;
        agent.stoppingDistance = 0;
    }

    private void Update()
    {
        animator?.SetFloat("Speed", agent.velocity.magnitude);
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

    public void Rotate(Vector3 dir)
    {
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            target,
            config.RotationSpeed * Time.deltaTime
        );
    }

}
