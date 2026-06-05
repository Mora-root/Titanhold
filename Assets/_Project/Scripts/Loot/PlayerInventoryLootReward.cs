using UnityEngine;

public sealed class PlayerInventoryLootReward : MonoBehaviour, ILootReward, IAmountLootReward
{
    [SerializeField] private ItemDefinition item = null;
    [SerializeField] private int amount = 1;
    [SerializeField] private PlayerInventory playerInventory = null;

    public void SetAmount(int amount)
    {
        this.amount = amount;
    }

    public bool Collect(PlayerInventory inventory)
    {
        PlayerInventory resolvedInventory = inventory != null ? inventory : playerInventory;
        return TryCollect(resolvedInventory);
    }

    public bool Collect(GameObject picker)
    {
        PlayerInventory inventory = playerInventory;

        if (inventory == null && picker != null)
            inventory = picker.GetComponent<PlayerInventory>();

        inventory ??= FindAnyObjectByType<PlayerInventory>();

        return TryCollect(inventory);
    }

    private bool TryCollect(PlayerInventory inventory)
    {
        if (inventory == null)
        {
            Debug.LogWarning($"{nameof(PlayerInventoryLootReward)} requires a PlayerInventory reference.", this);
            return false;
        }

        if (item == null)
        {
            Debug.LogWarning($"{nameof(PlayerInventoryLootReward)} requires an ItemDefinition reward.", this);
            return false;
        }

        if (amount <= 0)
        {
            Debug.LogWarning($"{nameof(PlayerInventoryLootReward)} amount must be greater than zero.", this);
            return false;
        }

        AddItemResult result = inventory.TryAdd(item, amount);

        if (result.FullyAdded)
            return true;

        Debug.LogWarning(
            $"{nameof(PlayerInventoryLootReward)} could not fully collect '{item.DisplayName}'. " +
            $"Added: {result.AddedAmount}, Remaining: {result.RemainingAmount}.",
            this);

        return false;
    }
}
