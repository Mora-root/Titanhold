using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ItemLootTableEntry
{
    [SerializeField] private ItemDefinition item;
    [SerializeField, Range(0f, 1f)] private float dropChance;
    [SerializeField, Min(1)] private int minAmount;
    [SerializeField, Min(1)] private int maxAmount;
    [SerializeField] private ItemModifierRollRule[] generatedModifierRules;
    [SerializeField, Min(0)] private int minGeneratedModifiers;
    [SerializeField, Min(0)] private int maxGeneratedModifiers;

    public ItemLootTableEntry(
        ItemDefinition item,
        float dropChance,
        int minAmount,
        int maxAmount,
        IReadOnlyList<ItemModifierRollRule> generatedModifierRules = null,
        int minGeneratedModifiers = 0,
        int maxGeneratedModifiers = 0)
    {
        this.item = item;
        this.dropChance = dropChance;
        this.minAmount = minAmount;
        this.maxAmount = maxAmount;
        this.generatedModifierRules = CopyRules(generatedModifierRules);
        this.minGeneratedModifiers = minGeneratedModifiers;
        this.maxGeneratedModifiers = maxGeneratedModifiers;
    }

    public ItemDefinition Item => item;
    public float DropChance => Mathf.Clamp01(dropChance);
    public int MinAmount => Mathf.Max(1, minAmount);
    public int MaxAmount => Mathf.Max(MinAmount, maxAmount);
    public IReadOnlyList<ItemModifierRollRule> GeneratedModifierRules => generatedModifierRules ?? Array.Empty<ItemModifierRollRule>();
    public int MinGeneratedModifiers => Mathf.Max(0, Mathf.Min(minGeneratedModifiers, maxGeneratedModifiers));
    public int MaxGeneratedModifiers => Mathf.Max(MinGeneratedModifiers, Mathf.Max(minGeneratedModifiers, maxGeneratedModifiers));

    public bool TryRoll(ICollection<ItemStack> results, System.Random random = null)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));

        if (item == null)
            return false;

        random ??= new System.Random();

        if (random.NextDouble() > DropChance)
            return false;

        int amount = RollAmount(random);
        if (amount <= 0)
            return false;

        AddGeneratedStacks(results, amount, random);
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

    private void AddGeneratedStacks(ICollection<ItemStack> results, int amount, System.Random random)
    {
        if (item.IsStackable)
        {
            int remaining = amount;
            while (remaining > 0)
            {
                int stackAmount = Mathf.Min(remaining, item.MaxStack);
                results.Add(ItemDropGenerator.CreateStack(item, stackAmount));
                remaining -= stackAmount;
            }

            return;
        }

        for (int i = 0; i < amount; i++)
        {
            results.Add(ItemDropGenerator.CreateStack(
                item,
                1,
                GeneratedModifierRules,
                MinGeneratedModifiers,
                MaxGeneratedModifiers,
                random));
        }
    }

    private static ItemModifierRollRule[] CopyRules(IReadOnlyList<ItemModifierRollRule> rules)
    {
        if (rules == null || rules.Count == 0)
            return Array.Empty<ItemModifierRollRule>();

        ItemModifierRollRule[] copy = new ItemModifierRollRule[rules.Count];
        for (int i = 0; i < rules.Count; i++)
            copy[i] = rules[i];

        return copy;
    }
}
