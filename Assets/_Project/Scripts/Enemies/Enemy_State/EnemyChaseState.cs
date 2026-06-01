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
        var target = brain.GetTarget();

        if (target == null)
        {
            brain.Wander.SetCenter(brain.transform.position);
            brain.ChangeToIdle();
            return;
        }

        float dist = Vector3.Distance(
            brain.transform.position,
            target.AimPoint.position
        );

        if (dist <= brain.Combat.AttackRange)
        {
            brain.ChangeToAttack();
            return;
        }

        brain.MoveTo(target.AimPoint.position);
    }

    public void Enter() { }
    public void Exit() { }
}
