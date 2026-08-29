using System;
using System.Collections.Generic;

public sealed class StatBlock
{
    private sealed class SourcedStatModifier
    {
        public SourcedStatModifier(StatModifier modifier, StatModifierSource source)
        {
            Modifier = modifier;
            Source = source;
        }

        public StatModifier Modifier { get; }
        public StatModifierSource Source { get; }
    }

    private readonly Dictionary<StatType, float> baseValues = new();
    private readonly List<StatModifier> modifiers = new();
    private readonly List<SourcedStatModifier> sourcedModifiers = new();
    private readonly Dictionary<StatType, float> cachedValues = new();
    private readonly HashSet<StatType> dirtyStats = new();

    public event Action<StatType> StatChanged;

    public float GetValue(StatType type)
    {
        if (!dirtyStats.Contains(type) && cachedValues.TryGetValue(type, out float cachedValue))
            return cachedValue;

        float calculatedValue = CalculateValue(type);
        cachedValues[type] = calculatedValue;
        dirtyStats.Remove(type);
        return calculatedValue;
    }

    public void SetBaseValue(StatType type, float value)
    {
        if (baseValues.TryGetValue(type, out float currentValue) && FloatEquals(currentValue, value))
            return;

        baseValues[type] = value;
        MarkChanged(type);
    }

    public void AddModifier(StatModifier modifier)
    {
        if (modifier == null)
            return;

        modifiers.Add(modifier);
        MarkChanged(modifier.Type);
    }

    public void AddModifier(StatModifier modifier, StatModifierSource source)
    {
        if (modifier == null)
            return;

        if (!source.IsValid)
        {
            AddModifier(modifier);
            return;
        }

        sourcedModifiers.Add(new SourcedStatModifier(modifier, source));
        MarkChanged(modifier.Type);
    }

    public void AddModifiers(IEnumerable<StatModifierData> modifiersToAdd, StatModifierSource source)
    {
        if (modifiersToAdd == null)
            return;

        foreach (StatModifierData modifierData in modifiersToAdd)
        {
            AddModifier(modifierData.ToRuntimeModifier(), source);
        }
    }

    public void RemoveModifier(StatModifier modifier)
    {
        if (modifier == null)
            return;

        if (modifiers.Remove(modifier))
        {
            MarkChanged(modifier.Type);
            return;
        }

        for (int i = 0; i < sourcedModifiers.Count; i++)
        {
            if (sourcedModifiers[i].Modifier != modifier)
                continue;

            sourcedModifiers.RemoveAt(i);
            MarkChanged(modifier.Type);
            return;
        }
    }

    public void RemoveModifiersFromSource(StatModifierSource source)
    {
        if (!source.IsValid)
            return;

        HashSet<StatType> changedStats = new();

        for (int i = sourcedModifiers.Count - 1; i >= 0; i--)
        {
            if (sourcedModifiers[i].Source != source)
                continue;

            changedStats.Add(sourcedModifiers[i].Modifier.Type);
            sourcedModifiers.RemoveAt(i);
        }

        foreach (StatType type in changedStats)
        {
            MarkChanged(type);
        }
    }

    public void ClearModifiers()
    {
        HashSet<StatType> changedStats = new();

        foreach (StatModifier modifier in modifiers)
        {
            changedStats.Add(modifier.Type);
        }

        foreach (SourcedStatModifier sourcedModifier in sourcedModifiers)
        {
            changedStats.Add(sourcedModifier.Modifier.Type);
        }

        modifiers.Clear();
        sourcedModifiers.Clear();

        foreach (StatType type in changedStats)
        {
            MarkChanged(type);
        }
    }

    private float CalculateValue(StatType type)
    {
        float baseValue = baseValues.TryGetValue(type, out float configuredBaseValue)
            ? configuredBaseValue
            : 0f;

        float flatBonus = 0f;
        float increasedPercent = 0f;
        float moreMultiplier = 1f;
        bool hasOverride = false;
        float overrideValue = 0f;

        foreach (StatModifier modifier in modifiers)
        {
            if (modifier.Type != type)
                continue;

            AccumulateModifier(
                modifier,
                ref flatBonus,
                ref increasedPercent,
                ref moreMultiplier,
                ref hasOverride,
                ref overrideValue);
        }

        foreach (SourcedStatModifier sourcedModifier in sourcedModifiers)
        {
            StatModifier modifier = sourcedModifier.Modifier;
            if (modifier.Type != type)
                continue;

            AccumulateModifier(
                modifier,
                ref flatBonus,
                ref increasedPercent,
                ref moreMultiplier,
                ref hasOverride,
                ref overrideValue);
        }

        float value = (hasOverride ? overrideValue : baseValue) + flatBonus;
        value *= 1f + increasedPercent / 100f;
        value *= moreMultiplier;
        return value;
    }

    private void MarkChanged(StatType type)
    {
        dirtyStats.Add(type);
        StatChanged?.Invoke(type);
    }

    private static void AccumulateModifier(
        StatModifier modifier,
        ref float flatBonus,
        ref float increasedPercent,
        ref float moreMultiplier,
        ref bool hasOverride,
        ref float overrideValue)
    {
        switch (modifier.ModifierType)
        {
            case StatModifierType.Flat:
                flatBonus += modifier.Value;
                break;
            case StatModifierType.Increased:
                increasedPercent += modifier.Value;
                break;
            case StatModifierType.More:
                moreMultiplier *= 1f + modifier.Value / 100f;
                break;
            case StatModifierType.Override:
                hasOverride = true;
                overrideValue = modifier.Value;
                break;
        }
    }

    private static bool FloatEquals(float a, float b)
    {
        return Math.Abs(a - b) <= 0.0001f;
    }
}
