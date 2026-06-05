using UnityEngine;

// Temporary prototype helper for manually testing the new PlayerInventory.
public sealed class PlayerInventoryDebugAdder : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory = null;
    [SerializeField] private ItemDefinition itemToAdd = null;
    [SerializeField] private int amount = 1;

    [ContextMenu("Add Item To Player Inventory")]
    public void Add()
    {
        PlayerInventory inventory = ResolveInventory();

        if (inventory == null)
        {
            Debug.LogWarning($"{nameof(PlayerInventoryDebugAdder)} requires a PlayerInventory reference.", this);
            return;
        }

        if (itemToAdd == null)
        {
            Debug.LogWarning($"{nameof(PlayerInventoryDebugAdder)} requires an ItemDefinition to add.", this);
            return;
        }

        if (amount <= 0)
        {
            Debug.LogWarning($"{nameof(PlayerInventoryDebugAdder)} amount must be greater than zero.", this);
            return;
        }

        AddItemResult result = inventory.TryAdd(itemToAdd, amount);

        if (!result.FullyAdded)
        {
            Debug.LogWarning(
                $"{nameof(PlayerInventoryDebugAdder)} could not fully add '{itemToAdd.DisplayName}'. " +
                $"Added: {result.AddedAmount}, Remaining: {result.RemainingAmount}.",
                this);
        }
    }

    private PlayerInventory ResolveInventory()
    {
        if (playerInventory != null)
            return playerInventory;

        playerInventory = GetComponent<PlayerInventory>();
        playerInventory ??= FindAnyObjectByType<PlayerInventory>();
        return playerInventory;
    }
}
