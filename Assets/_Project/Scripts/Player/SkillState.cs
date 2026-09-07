using UnityEngine;

public class SkillState : IState
{
    private PlayerBrain brain;

    public SkillState(PlayerBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        brain.Stop();
    }

    public void Tick()
    {
        brain.Stop();

        ITargetable target = brain.Skills?.CurrentTarget;
        if (target != null &&
            (!(target is Object unityObject) || unityObject != null) &&
            target.IsTargetable && target.AimPoint != null)
        {
            brain.Movement.RotateTowards(target.AimPoint.position);
        }

        if (brain.Skills?.IsUsingSkill != true)
        {
            brain.ChangeToIdle();
        }
    }

    public void Exit() { }
}
