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
        Vector3 direction =
            target.AimPoint.position - brain.transform.position;
        direction.y = 0f;
        Vector3 forward = brain.transform.forward;
        forward.y = 0f;
        bool isFacing = direction.sqrMagnitude <= 0.0001f ||
                        forward.sqrMagnitude <= 0.0001f ||
                        Vector3.Angle(forward, direction) <= attackAngle;

        brain.Movement.RotateTowards(target.AimPoint.position);

        if (isFacing && brain.CanAttack())
        {
            brain.Attack(target);
        }
    }

    public void Exit() { }
}
