using UnityEngine;

public class EnemyAttackState : IState
{
    private EnemyBrain brain;

    private float attackAngle = 45f;

    public EnemyAttackState(EnemyBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        brain.Stop();
    }

    public void Tick()
    {
        var target = brain.GetTarget();

        if (target == null)
        {
            brain.ChangeToIdle();
            return;
        }

        if (brain.Combat.IsAttacking)
        {
            brain.Stop();
            if (target != null)
            {
                brain.Movement.RotateTowards(target.AimPoint.position);
            }
            return;
        }

        float dist = Vector3.Distance(
            brain.transform.position,
            target.AimPoint.position
        );

        if (dist > brain.Combat.AttackRange)
        {
            brain.ChangeToChase();
            return;
        }
        float angle = Vector3.Angle(
             brain.transform.forward,
            (target.AimPoint.position - brain.transform.position)
);

        brain.Movement.RotateTowards(target.AimPoint.position);

        if (angle > attackAngle || brain.CanAttack())
        {
            brain.Attack(target);
        }
    }

    public void Exit() { }
}
