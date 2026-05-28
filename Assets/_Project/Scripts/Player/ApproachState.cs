using UnityEngine;

public class ApproachState : IState
{
    private PlayerBrain brain;

    public ApproachState(PlayerBrain brain)
    {
        this.brain = brain;
    }

    public void Enter() { }

    public void Tick()
    {
        var selection = brain.ActionTarget;

        if (selection == null || !selection.IsSelectable)
        {
            brain.ClearActionSelection();
            brain.ChangeToIdle();
            return;
        }

        if (selection is ITargetable targetable && targetable.IsTargetable)
        {
            HandleTargetable(targetable);
            return;
        }

        if (selection is IInteractable interactable && interactable.IsInteractable)
        {
            HandleInteractable(interactable);
            return;
        }

        if (selection is ILootable lootable && lootable.IsLootable)
        {
            HandleLootable(lootable);
            return;
        }

        brain.ClearActionSelection();
        brain.ChangeToIdle();
    }

    private void HandleTargetable(ITargetable target)
    {
        float distance = Vector3.Distance(
            brain.transform.position,
            target.AimPoint.position
        );

        if (distance <= brain.Combat.AttackRange)
        {
            brain.ChangeToAttack();
            return;
        }

        brain.MoveTo(target.AimPoint.position);
    }

    private void HandleInteractable(IInteractable interactable)
    {
        float distance = Vector3.Distance(
            brain.transform.position,
            interactable.InteractionPoint.position
        );

        if (distance <= interactable.InteractionRange)
        {
            brain.ChangeToInteract();
            return;
        }

        brain.MoveTo(interactable.InteractionPoint.position);
    }

    private void HandleLootable(ILootable lootable)
    {
        float distance = Vector3.Distance(
            brain.transform.position,
            lootable.LootPoint.position
        );

        if (distance <= lootable.PickupRange)
        {
            brain.ChangeToLoot();
            return;
        }

        brain.MoveTo(lootable.LootPoint.position);
    }

    public void Exit()
    {
        brain.Stop();
    }
}
