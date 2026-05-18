
public class StateMachine
{
    public IState CurrentState { get; private set; }

    public void ChangeState(IState newState)
    {
        if (newState == null)
        {
            CurrentState?.Exit();
            CurrentState = null;
            return;
        }
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState?.Enter();
    }

    public void Update()
    {
        CurrentState?.Tick();
    }
}
