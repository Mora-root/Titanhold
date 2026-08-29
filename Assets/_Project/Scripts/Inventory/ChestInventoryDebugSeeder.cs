using System;
using UnityEngine;

// Prototype helper for manually seeding runtime chest contents in scenes.
public sealed class ChestInventoryDebugSeeder : MonoBehaviour
{
    [Serializable]
    private sealed class SeedEntry
    {
        [SerializeField] private ItemDefinition item = null;
        [SerializeField] private int amount = 1;

        public ItemDefinition Item => item;
        public int Amount => amount;
    }

    [SerializeField] private ChestInventory chestInventory;
    [SerializeField] private bool seedOnStart = true;
    [SerializeField] private SeedEntry[] entries;

    private bool seeded;

    private void Awake()
    {
        chestInventory ??= GetComponent<ChestInventory>();
    }

    private void Start()
    {
        if (seedOnStart)
            Seed();
    }

    [ContextMenu("Seed Chest Inventory")]
    public void Seed()
    {
        if (seeded)
            return;

        if (chestInventory == null)
        {
            Debug.LogWarning($"{nameof(ChestInventoryDebugSeeder)} requires a ChestInventory reference.", this);
            return;
        }

        if (entries == null || entries.Length == 0)
        {
            Debug.LogWarning($"{nameof(ChestInventoryDebugSeeder)} has no seed entries.", this);
            return;
        }

        foreach (SeedEntry entry in entries)
        {
            if (entry == null)
            {
                Debug.LogWarning($"{nameof(ChestInventoryDebugSeeder)} skipped a null seed entry.", this);
                continue;
            }

            if (entry.Item == null)
            {
                Debug.LogWarning($"{nameof(ChestInventoryDebugSeeder)} skipped a seed entry with no item.", this);
                continue;
            }

            if (entry.Amount <= 0)
            {
                Debug.LogWarning(
                    $"{nameof(ChestInventoryDebugSeeder)} skipped '{entry.Item.DisplayName}' because amount must be greater than zero.",
                    this);
                continue;
            }

            AddItemResult result = chestInventory.TryAdd(entry.Item, entry.Amount);

            if (!result.FullyAdded)
            {
                Debug.LogWarning(
                    $"{nameof(ChestInventoryDebugSeeder)} could not fully seed '{entry.Item.DisplayName}'. " +
                    $"Added: {result.AddedAmount}, Remaining: {result.RemainingAmount}.",
                    this);
            }
        }

        seeded = true;
    }
}
