using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [SerializeField] private CharacterStatsConfig config;

    private readonly List<StatModifier> modifiers = new();

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

    public void RemoveModifier(StatModifier modifier)
    {
        if (modifier == null) return;

        if (modifiers.Remove(modifier))
        {
            OnStatChanged?.Invoke(modifier.Type);
        }
    }
}
