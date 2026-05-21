using UnityEngine;
using UnityEngine.AI;

public class EnemyWanderState : IState
{
    private EnemyBrain brain;

    private Vector3 currentPoint;

    public EnemyWanderState(EnemyBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        PickNewPoint();
    }

    public void Tick()
    {
        var target = brain.Sensor.GetTarget();

        if (target != null)
        {
            brain.ChangeToChase();
            return;
        }

        if (HasReached())
        {
            brain.ChangeToIdle();
        }
    }

    public void Exit() { }

    private void PickNewPoint()
    {
        // Search a new point and go to it
        currentPoint = brain.Wander.GetNextPoint();
        brain.MoveTo(currentPoint);
    }

    private bool HasReached()
    {
        // Checks if the enemy has reached the current wandering point
        var agent = brain.GetComponent<NavMeshAgent>();

        if (agent.pathPending) return false;

        if (agent.remainingDistance > agent.stoppingDistance)
            return false;

        if (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f)
            return false;

        return true;
    }
}