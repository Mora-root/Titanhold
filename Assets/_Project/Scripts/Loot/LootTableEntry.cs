using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LootTableEntry
{
    [SerializeField] private LootDropKind kind;
    [SerializeField, Range(0f, 1f)] private float dropChance;
    [SerializeField, Min(0)] private int minAmount;
    [SerializeField, Min(0)] private int maxAmount;

    [Header("Item")]
    [SerializeField] private ItemDefinition item;
    [SerializeField] private ItemModifierRollRule[] generatedModifierRules;
    [SerializeField, Min(0)] private int minGeneratedModifiers;
    [SerializeField, Min(0)] private int maxGeneratedModifiers;

    public LootDropKind Kind => kind;
    public float DropChance => Mathf.Clamp01(dropChance);
    public int MinAmount => Mathf.Max(0, minAmount);
    public int MaxAmount => Mathf.Max(MinAmount, maxAmount);
    public ItemDefinition Item => item;
    public IReadOnlyList<ItemModifierRollRule> GeneratedModifierRules => generatedModifierRules ?? Array.Empty<ItemModifierRollRule>();
    public int MinGeneratedModifiers => Mathf.Max(0, Mathf.Min(minGeneratedModifiers, maxGeneratedModifiers));
    public int MaxGeneratedModifiers => Mathf.Max(MinGeneratedModifiers, Mathf.Max(minGeneratedModifiers, maxGeneratedModifiers));

    public bool TryRoll(ICollection<LootDropResult> results, System.Random random = null)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));

        random ??= new System.Random();

        if (random.NextDouble() > DropChance)
            return false;

        int amount = RollAmount(random);
        if (amount <= 0)
            return false;

        switch (kind)
        {
            case LootDropKind.Item:
                return TryRollItem(results, amount, random);

            case LootDropKind.Gold:
                results.Add(LootDropResult.Gold(amount));
                return true;

            default:
                return false;
        }
    }

    private bool TryRollItem(ICollection<LootDropResult> results, int amount, System.Random random)
    {
        if (item == null)
            return false;

        if (item.IsStackable)
        {
            int remaining = amount;
            while (remaining > 0)
            {
                int stackAmount = Mathf.Min(remaining, item.MaxStack);
                results.Add(LootDropResult.Item(ItemDropGenerator.CreateStack(item, stackAmount)));
                remaining -= stackAmount;
            }

            return true;
        }

        for (int i = 0; i < amount; i++)
        {
            ItemStack stack = ItemDropGenerator.CreateStack(
                item,
                1,
                GeneratedModifierRules,
                MinGeneratedModifiers,
                MaxGeneratedModifiers,
                random);

            results.Add(LootDropResult.Item(stack));
        }

        return true;
    }

    private int RollAmount(System.Random random)
    {
        int min = MinAmount;
        int max = MaxAmount;

        if (min == max)
            return min;

        return random.Next(min, max + 1);
    }
}
