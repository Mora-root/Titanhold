
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
        if (brain.CurrentTarget != null)
        {
            brain.ChangeToChase();
            return;
        }

        if (!brain.Input.HasPosition)
        {
            brain.ChangeToIdle();
            return;
        }

        brain.MoveTo(brain.Input.TargetPosition);
    }

    public void Exit()
    {
        brain.Stop();
    }
}
