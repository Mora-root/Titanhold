using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project/Loot/Loot Table")]
public sealed class LootTable : ScriptableObject
{
    [SerializeField] private LootTableEntry[] entries;

    public IReadOnlyList<LootTableEntry> Entries => entries ?? System.Array.Empty<LootTableEntry>();

    public List<LootDropResult> Roll(System.Random random = null)
    {
        List<LootDropResult> results = new();
        RollInto(results, random);
        return results;
    }

    public void RollInto(ICollection<LootDropResult> results, System.Random random = null)
    {
        if (results == null)
            throw new System.ArgumentNullException(nameof(results));

        random ??= new System.Random();

        IReadOnlyList<LootTableEntry> sourceEntries = Entries;
        for (int i = 0; i < sourceEntries.Count; i++)
            sourceEntries[i].TryRoll(results, random);
    }
}
