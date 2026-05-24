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
        if (brain.ActionSelection != null)
        {
            Debug.Log("Idle sees ActionSelection: " + brain.ActionSelection.GetType().Name);
            Debug.Log("CurrentTarget null: " + (brain.CurrentTarget == null));

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
