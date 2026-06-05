using System;
using UnityEngine;

// Temporary prototype helper for manually testing the new PlayerInventory UI.
public sealed class PlayerInventoryDebugSeeder : MonoBehaviour
{
    [Serializable]
    private sealed class SeedEntry
    {
        [SerializeField] private ItemDefinition item = null;
        [SerializeField] private int amount = 1;

        public ItemDefinition Item => item;
        public int Amount => amount;
    }

    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private SeedEntry[] entries;

    private void Awake()
    {
        playerInventory ??= GetComponent<PlayerInventory>();
        playerInventory ??= FindAnyObjectByType<PlayerInventory>();
    }

    [ContextMenu("Seed Player Inventory")]
    public void Seed()
    {
        if (playerInventory == null)
        {
            Debug.LogWarning($"{nameof(PlayerInventoryDebugSeeder)} requires a PlayerInventory reference.", this);
            return;
        }

        if (entries == null || entries.Length == 0)
        {
            Debug.LogWarning($"{nameof(PlayerInventoryDebugSeeder)} has no seed entries.", this);
            return;
        }

        foreach (SeedEntry entry in entries)
        {
            if (entry == null)
            {
                Debug.LogWarning($"{nameof(PlayerInventoryDebugSeeder)} skipped a null seed entry.", this);
                continue;
            }

            if (entry.Item == null)
            {
                Debug.LogWarning($"{nameof(PlayerInventoryDebugSeeder)} skipped a seed entry with no item.", this);
                continue;
            }

            if (entry.Amount <= 0)
            {
                Debug.LogWarning(
                    $"{nameof(PlayerInventoryDebugSeeder)} skipped '{entry.Item.DisplayName}' because amount must be greater than zero.",
                    this);
                continue;
            }

            AddItemResult result = playerInventory.TryAdd(entry.Item, entry.Amount);

            if (!result.FullyAdded)
            {
                Debug.LogWarning(
                    $"{nameof(PlayerInventoryDebugSeeder)} could not fully seed '{entry.Item.DisplayName}'. " +
                    $"Added: {result.AddedAmount}, Remaining: {result.RemainingAmount}.",
                    this);
            }
        }
    }
}
