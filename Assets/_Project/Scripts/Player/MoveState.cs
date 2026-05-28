
public class MoveState : IState
{
    private PlayerBrain brain;

    public MoveState(PlayerBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {

    }

    public void Tick()
    {
        if (brain.ActionSelection != null)
        {
            brain.ChangeToApproach();
            return;
        }

        if (!brain.HasMoveTarget)
        {
            brain.ChangeToIdle();
            return;
        }

        brain.MoveTo(brain.MoveTargetPosition);

        if (brain.Movement.HasReachedDestination())
        {
            brain.Input.ClearAll();
            brain.ChangeToIdle();
        }
    }

    public void Exit()
    {
        brain.Stop();
    }
}
