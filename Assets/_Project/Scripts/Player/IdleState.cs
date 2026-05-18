
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
        if (brain.CurrentTarget != null)
        {
            brain.ChangeToChase();
            return;
        }

        if (brain.Input.HasPosition)
        {
            brain.ChangeToMove();
        }
    }

    public void Exit() { }
}
