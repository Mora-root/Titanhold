using UnityEngine;

public class EnemyAttackState : IState
{
    private EnemyBrain brain;

    public EnemyAttackState(EnemyBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        brain.Movement.Stop();
    }

    public void Tick()
    {
        if (!brain.Sensor.HasTarget)
        {
            brain.StateMachine.ChangeState(brain.Idle);
            Debug.Log(brain.StateMachine.CurrentState + " from Attack");
            return;
        }

        ITargetable target = brain.Sensor.CurrentTarget;

        float distance = Vector3.Distance(
            brain.transform.position,
            target.AimPoint.position
        );

        if (distance > brain.Combat.EnemyAttackRange + 0.5f)
        {
            brain.StateMachine.ChangeState(brain.Chase);
            Debug.Log(brain.StateMachine.CurrentState + " from Attack");
            return;
        }

        if (brain.Animator.IsAttacking)
        {
            brain.Movement.Stop();
            return;
        }

        if (brain.Combat.CanAttack())
        {
            brain.Combat.Attack(target);
        }
    }

    public void Exit() { }
}
