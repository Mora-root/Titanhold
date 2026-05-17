using UnityEngine;

public class EnemyWanderState : IState
{
    private EnemyBrain brain;


    public EnemyWanderState(EnemyBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        var point = brain.Wander.GetNextPoint();
        brain.Movement.MoveTo(point);
    }

    public void Tick()
    {
        brain.Sensor.UpdateSensor();

        if (brain.Sensor.HasTarget)
        {
            brain.StateMachine.ChangeState(brain.Chase);
            Debug.Log(brain.StateMachine.CurrentState + " from Wander");
            return;
        }

        if (brain.Movement.HasReachedDestination())
        {
            brain.StateMachine.ChangeState(brain.Idle);
            Debug.Log(brain.StateMachine.CurrentState + " from Wander");
        }

    }

    public void Exit() { }
}