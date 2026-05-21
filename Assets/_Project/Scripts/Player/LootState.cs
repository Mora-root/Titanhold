using UnityEngine;

public class LootState : IState
{
    private PlayerBrain brain;

    public LootState(PlayerBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        brain.Stop();

        var loot = brain.CurrentLoot;

        if (loot != null && loot.IsLootable)
        {
            loot.Pickup(brain.gameObject);
        }

        brain.ClearActionSelection();
        brain.ChangeToIdle();
    }

    public void Tick() { }

    public void Exit() { }
}
