using UnityEngine;

public sealed class LootItemReward : MonoBehaviour, ILootReward, IAmountLootReward
{
    [SerializeField] private LootItemDefinition item;
    [SerializeField] private int amount = 1;
    [SerializeField] private PlayerLootInventory playerLootInventory;

    public void SetAmount(int amount)
    {
        this.amount = amount;
    }

    public bool Collect(GameObject picker)
    {
        if (item == null)
            return false;

        if (amount <= 0)
            return false;

        PlayerLootInventory inventory = playerLootInventory;

        if (inventory == null && picker != null)
            inventory = picker.GetComponent<PlayerLootInventory>();

        inventory ??= FindAnyObjectByType<PlayerLootInventory>();

        if (inventory == null)
            return false;

        inventory.Add(item, amount);
        return true;
    }
}
