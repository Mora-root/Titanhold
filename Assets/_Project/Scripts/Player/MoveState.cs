
public class MoveState : IState
{
    private PlayerBrain brain;

    public MoveState(PlayerBrain brain)
    {
        this.brain = brain;
    }

    public void Enter() { }

    public void Tick()
    {
        if (brain.CurrentTarget != null)
        {
            brain.StateMachine.ChangeState(brain.ChaseState);
            return;
        }

        if (!brain.Input.HasPosition)
        {
            brain.StateMachine.ChangeState(brain.IdleState);
            return;
        }

        brain.Movement.MoveTo(brain.Input.TargetPosition);

        Rotate();
    }

    public void Exit()
    {
        brain.Movement.Stop();
    }

    private void Rotate()
    {
        if (brain.Movement.HasVelocity)
        {
            brain.Movement.Rotate(brain.Movement.Velocity.normalized);
        }
    }
}
