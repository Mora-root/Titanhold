using System;
using System.Collections.Generic;

public static class ItemDropGenerator
{
    public static ItemStack CreateStack(
        ItemDefinition definition,
        int amount = 1,
        IReadOnlyList<ItemModifierRollRule> modifierRules = null,
        int minGeneratedModifiers = 0,
        int maxGeneratedModifiers = 0,
        System.Random random = null)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");

        if (definition.IsStackable)
        {
            if (amount > definition.MaxStack)
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    $"A generated ItemStack amount cannot exceed MaxStack ({definition.MaxStack}). Use ItemContainer.TryAdd for splitting.");

            return ItemStack.CreateStackable(definition, amount);
        }

        if (amount != 1)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Non-stackable generated ItemStacks must be created one item at a time.");

        List<StatModifierData> generatedModifiers = RollGeneratedModifiers(
            modifierRules,
            minGeneratedModifiers,
            maxGeneratedModifiers,
            random);

        ItemInstance instance = new(definition, generatedModifiers);
        return ItemStack.CreateNonStackable(instance);
    }

    public static List<StatModifierData> RollGeneratedModifiers(
        IReadOnlyList<ItemModifierRollRule> modifierRules,
        int minGeneratedModifiers,
        int maxGeneratedModifiers,
        System.Random random = null)
    {
        List<StatModifierData> generatedModifiers = new();

        if (modifierRules == null || modifierRules.Count == 0)
            return generatedModifiers;

        random ??= new System.Random();

        int lower = Math.Max(0, Math.Min(minGeneratedModifiers, maxGeneratedModifiers));
        int upper = Math.Max(0, Math.Max(minGeneratedModifiers, maxGeneratedModifiers));
        lower = Math.Min(lower, modifierRules.Count);
        upper = Math.Min(upper, modifierRules.Count);

        if (upper <= 0)
            return generatedModifiers;

        int count = lower == upper
            ? lower
            : random.Next(lower, upper + 1);

        if (count <= 0)
            return generatedModifiers;

        List<int> availableIndexes = new(modifierRules.Count);
        for (int i = 0; i < modifierRules.Count; i++)
            availableIndexes.Add(i);

        for (int i = 0; i < count; i++)
        {
            int pick = random.Next(availableIndexes.Count);
            int ruleIndex = availableIndexes[pick];
            availableIndexes.RemoveAt(pick);
            generatedModifiers.Add(modifierRules[ruleIndex].Roll(random));
        }

        return generatedModifiers;
    }
}
