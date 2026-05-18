using UnityEngine;

public class ChaseState : IState
{
    private PlayerBrain brain;

    public ChaseState(PlayerBrain brain)
    {
        this.brain = brain;
    }
    public void Enter()
    {
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

        if (dist <= brain.Combat.AttackRange)
        {
            brain.ChangeToAttack();
            return;
        }

        brain.MoveTo(target.AimPoint.position);
    }

    public void Exit()
    {
        brain.Stop();
    }
}
