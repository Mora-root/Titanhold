using UnityEngine;

public class AttackState : IState
{
    private PlayerBrain brain;

    public AttackState(PlayerBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        brain.Stop();
    }

    public void Tick()
    {
        var target = brain.CurrentTarget;

        if (target == null)
        {
            brain.ChangeToIdle();
            return;
        }

        float dist = Vector3.Distance(
            brain.transform.position,
            target.AimPoint.position
        );

        if (brain.Combat.IsAttacking)
        {
            brain.Movement.RotateTowards(target.AimPoint.position);
            return;
        }

        if (dist > brain.Combat.AttackRange)
        {
            brain.ChangeToChase();
            return;
        }

        if (brain.CanAttack())
        {
            brain.TryAttack(target);
        }

        brain.Movement.RotateTowards(target.AimPoint.position);
    }

    public void Exit() { }
}