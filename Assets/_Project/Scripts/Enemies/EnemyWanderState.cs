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
        Debug.Log("Entering Wander State, moving to new point.");
    }

    public void Tick()
    {
        var target = brain.Sensor.GetTarget();

        if (target != null)
        {
            brain.ChangeToChase();
            Debug.Log("Target spotted, switching to chase state.");
            return;
        }

        if (HasReached())
        {
            brain.ChangeToIdle();
            Debug.Log("Reached point, switching to idle.");
        }
    }

    public void Exit() { }

    private void PickNewPoint()
    {
        currentPoint = brain.Wander.GetNextPoint();
        brain.MoveTo(currentPoint);
    }

    private bool HasReached()
    {
        var agent = brain.GetComponent<NavMeshAgent>();

        if (agent.pathPending) return false;

        if (agent.remainingDistance > agent.stoppingDistance)
            return false;

        if (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f)
            return false;

        return true;
    }
}