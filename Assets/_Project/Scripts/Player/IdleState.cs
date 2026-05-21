
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
        if (brain.ActionSelection != null)
        {
            brain.ChangeToApproach();
            return;
        }

        if (brain.Input.HasPosition)
        {
            brain.ChangeToMove();
            return;
        }
    }

    public void Exit() { }
}
