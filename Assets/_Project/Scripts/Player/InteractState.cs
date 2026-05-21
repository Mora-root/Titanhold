using UnityEngine;

public class InteractState : IState
{
    private PlayerBrain brain;

    public InteractState(PlayerBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        brain.Stop();

        var interactable = brain.CurrentInteractable;

        if (interactable != null && interactable.IsInteractable)
        {
            interactable.Interact(brain.gameObject);
        }

        brain.ClearActionSelection();
        brain.ChangeToIdle();
    }

    public void Tick() { }

    public void Exit() { }
}
