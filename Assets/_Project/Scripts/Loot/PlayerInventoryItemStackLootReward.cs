using UnityEngine;

public sealed class PlayerInventoryItemStackLootReward : MonoBehaviour, ILootReward
{
    [SerializeField] private PlayerInventory playerInventory;

    private ItemStack stack;

    public ItemStack Stack => stack;

    public void SetStack(ItemStack stack)
    {
        this.stack = stack;
    }

    public bool Collect(GameObject picker)
    {
        PlayerInventory inventory = ResolveInventory(picker);
        return TryCollect(inventory);
    }

    private PlayerInventory ResolveInventory(GameObject picker)
    {
        if (playerInventory != null)
            return playerInventory;

        if (picker == null)
            return null;

        PlayerInventory inventory = picker.GetComponent<PlayerInventory>();
        if (inventory != null)
            return inventory;

        return picker.GetComponentInParent<PlayerInventory>();
    }

    private bool TryCollect(PlayerInventory inventory)
    {
        if (inventory == null)
        {
            Debug.LogWarning($"{nameof(PlayerInventoryItemStackLootReward)} requires a PlayerInventory owner.", this);
            return false;
        }

        if (!IsValidStack(stack))
        {
            Debug.LogWarning($"{nameof(PlayerInventoryItemStackLootReward)} has no valid generated ItemStack.", this);
            return false;
        }

        if (!CanFullyAdd(inventory, stack))
        {
            Debug.LogWarning(
                $"{nameof(PlayerInventoryItemStackLootReward)} could not fully collect '{stack.Definition.DisplayName}'. Inventory has no safe capacity.",
                this);
            return false;
        }

        AddItemResult result = inventory.TryAdd(stack);
        if (result.FullyAdded)
            return true;

        Debug.LogWarning(
            $"{nameof(PlayerInventoryItemStackLootReward)} add failed after capacity check for '{stack.Definition.DisplayName}'. " +
            $"Added: {result.AddedAmount}, Remaining: {result.RemainingAmount}.",
            this);

        return false;
    }

    private static bool CanFullyAdd(PlayerInventory inventory, ItemStack stack)
    {
        if (inventory == null || !IsValidStack(stack))
            return false;

        ItemDefinition definition = stack.Definition;
        ItemContainerSection section = inventory.GetSection(definition.Category);
        if (section == null)
            return false;

        return definition.IsStackable
            ? CanFullyAddStackable(section, stack)
            : section.CountFreeSlots() >= stack.Amount;
    }

    private static bool CanFullyAddStackable(ItemContainerSection section, ItemStack stack)
    {
        int capacity = 0;

        foreach (ItemSlot slot in section.Slots)
        {
            if (slot == null || slot.IsEmpty)
            {
                capacity += stack.Definition.MaxStack;
                continue;
            }

            if (slot.CanStackWith(stack))
                capacity += slot.Stack.FreeAmount;

            if (capacity >= stack.Amount)
                return true;
        }

        return capacity >= stack.Amount;
    }

    private static bool IsValidStack(ItemStack stack)
    {
        return stack != null && stack.Definition != null && stack.Amount > 0;
    }
}
