using UnityEngine;

public class IdleState : IState
{
    private PlayerBrain brain;

    public IdleState(PlayerBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        brain.Stop();
    }

    public void Tick()
    {
        if (brain.ActionTarget != null)
        {
            brain.ChangeToApproach();
            return;
        }

        if (brain.HasMoveTarget)
        {
            brain.ChangeToMove();
            return;
        }
    }

    public void Exit() { }
}
