using UnityEngine;

public class EnemyChaseState : IState
{
    private EnemyBrain brain;

    public EnemyChaseState(EnemyBrain brain)
    {
        this.brain = brain;
    }

    public void Tick()
    {
        brain.Sensor.UpdateSensor();

        if (!brain.Sensor.HasTarget)
        {
            brain.Wander.SetCurrentCenter(brain.transform.position);
            brain.Wander.ResetCenterTimer();

            brain.StateMachine.ChangeState(brain.Idle);
            Debug.Log(brain.StateMachine.CurrentState + " from Chase");
            return;
        }

        ITargetable target = brain.Sensor.CurrentTarget;

        float distance = Vector3.Distance(
            brain.transform.position,
            target.AimPoint.position
        );

        if (distance <= brain.Combat.EnemyAttackRange)
        {
            brain.StateMachine.ChangeState(brain.Attack);
            Debug.Log(brain.StateMachine.CurrentState + " from Chase");
            return;
        }

        brain.Movement.MoveTo(target.AimPoint.position);
    }

    public void Enter() { }
    public void Exit()  { }
}
