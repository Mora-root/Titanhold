using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project/Loot/Item Loot Table")]
public sealed class ItemLootTable : ScriptableObject
{
    [SerializeField] private ItemLootTableEntry[] entries;

    public IReadOnlyList<ItemLootTableEntry> Entries => entries ?? System.Array.Empty<ItemLootTableEntry>();

    public List<ItemStack> Roll(System.Random random = null)
    {
        List<ItemStack> results = new();
        RollInto(results, random);
        return results;
    }

    public void RollInto(ICollection<ItemStack> results, System.Random random = null)
    {
        if (results == null)
            throw new System.ArgumentNullException(nameof(results));

        random ??= new System.Random();

        IReadOnlyList<ItemLootTableEntry> sourceEntries = Entries;
        for (int i = 0; i < sourceEntries.Count; i++)
            sourceEntries[i].TryRoll(results, random);
    }
}
