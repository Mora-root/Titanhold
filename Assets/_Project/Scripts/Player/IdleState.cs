
public class IdleState : IState
{
    private PlayerBrain brain;

    public IdleState(PlayerBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        brain.Movement.Stop();
    }

    public void Tick()
    {
        // враг → chase
        if (brain.CurrentTarget != null)
        {
            brain.StateMachine.ChangeState(brain.ChaseState);
            return;
        }

        // позиция → move
        if (brain.Input.HasPosition)
        {
            brain.StateMachine.ChangeState(brain.MoveState);
        }
    }

    public void Exit() { }
}
