using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    private sealed class SourcedStatModifier
    {
        public SourcedStatModifier(StatModifier modifier, object source)
        {
            Modifier = modifier;
            Source = source;
        }

        public StatModifier Modifier { get; }
        public object Source { get; }
    }

    [SerializeField] private CharacterStatsConfig config;

    private readonly List<StatModifier> modifiers = new();
    private readonly List<SourcedStatModifier> sourcedModifiers = new();

    public event Action<StatType> OnStatChanged;

    public float GetValue(StatType type)
    {
        float baseValue = config != null ? config.GetBaseValue(type) : 0f;

        float flatBonus = 0f;
        float percentBonus = 0f;

        foreach (var modifier in modifiers)
        {
            if (modifier.Type != type)
                continue;

            if (modifier.ModifierType == StatModifierType.Flat)
                flatBonus += modifier.Value;
            else if (modifier.ModifierType == StatModifierType.Percent)
                percentBonus += modifier.Value;
        }

        foreach (var sourcedModifier in sourcedModifiers)
        {
            StatModifier modifier = sourcedModifier.Modifier;

            if (modifier.Type != type)
                continue;

            if (modifier.ModifierType == StatModifierType.Flat)
                flatBonus += modifier.Value;
            else if (modifier.ModifierType == StatModifierType.Percent)
                percentBonus += modifier.Value;
        }

        float value = baseValue + flatBonus;
        value *= 1f + percentBonus / 100f;

        return value;
    }

    public void AddModifier(StatModifier modifier)
    {
        if (modifier == null) return;

        modifiers.Add(modifier);
        OnStatChanged?.Invoke(modifier.Type);
    }

    public void AddModifier(StatModifier modifier, object source)
    {
        if (modifier == null) return;

        if (source == null)
        {
            AddModifier(modifier);
            return;
        }

        sourcedModifiers.Add(new SourcedStatModifier(modifier, source));
        OnStatChanged?.Invoke(modifier.Type);
    }

    public void AddModifiers(IEnumerable<StatModifierData> modifiers, object source)
    {
        if (modifiers == null) return;

        foreach (StatModifierData modifierData in modifiers)
        {
            AddModifier(modifierData.ToRuntimeModifier(), source);
        }
    }

    public void RemoveModifier(StatModifier modifier)
    {
        if (modifier == null) return;

        if (modifiers.Remove(modifier))
        {
            OnStatChanged?.Invoke(modifier.Type);
            return;
        }

        for (int i = 0; i < sourcedModifiers.Count; i++)
        {
            if (sourcedModifiers[i].Modifier != modifier)
                continue;

            sourcedModifiers.RemoveAt(i);
            OnStatChanged?.Invoke(modifier.Type);
            return;
        }
    }

    public void RemoveModifiersFromSource(object source)
    {
        if (source == null) return;

        HashSet<StatType> changedStats = new();

        for (int i = sourcedModifiers.Count - 1; i >= 0; i--)
        {
            if (!Equals(sourcedModifiers[i].Source, source))
                continue;

            changedStats.Add(sourcedModifiers[i].Modifier.Type);
            sourcedModifiers.RemoveAt(i);
        }

        foreach (StatType type in changedStats)
        {
            OnStatChanged?.Invoke(type);
        }
    }
}
