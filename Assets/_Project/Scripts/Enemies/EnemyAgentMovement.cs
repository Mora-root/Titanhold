using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Moves an enemy along a WaypointPath using a NavMeshAgent.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAgentMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyConfig enemyConfig;
    private WaypointPath path;
    private int currentWaypointIndex;

    private void Update()
    {
        if (agent == null || path == null)
        {
            return;
        }

        float switchDistance = agent.speed * 0.3f;

        if (!agent.pathPending && agent.remainingDistance <= switchDistance)
        {
            currentWaypointIndex++;
            MoveToNextWaypoint();
        }
    }

    private void MoveToNextWaypoint()
    {
        if (currentWaypointIndex >= path.Length)
        {
            Destroy(gameObject);
            return;
        }
        Transform target = path.GetWaypoint(currentWaypointIndex);
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
    }

    public void Initialize(EnemyConfig config, WaypointPath path)
    {
        enemyConfig = config;
        this.path = path;
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.speed = enemyConfig.MoveSpeed;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.stoppingDistance = 0.001f ;
        }

        currentWaypointIndex = 0;
        MoveToNextWaypoint();
    }
}
