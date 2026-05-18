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
        brain.Stop();
    }

    public void Tick()
    {
        var target = brain.Sensor.GetTarget();

        if (target == null)
        {
            brain.ChangeToIdle();
            Debug.Log("No target found, changing to idle.");
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
            Debug.Log("Target out of range, changing to chase.");
            return;
        }
        float angle = Vector3.Angle(
             brain.transform.forward,
            (target.AimPoint.position - brain.transform.position)
);

        if (angle > 45f)
        {
            brain.Movement.RotateTowards(target.AimPoint.position);
            return;
        }

        if (brain.CanAttack())
        {
            Debug.Log("Can attack");
            brain.Attack(target);
        }
    }

    public void Exit() { }
}
