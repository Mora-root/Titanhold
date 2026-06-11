using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [SerializeField] private CharacterStatsConfig config;

    private StatBlock statBlock;

    public event Action<StatType> OnStatChanged;

    public StatBlock Block
    {
        get
        {
            EnsureInitialized();
            return statBlock;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        if (statBlock != null)
            statBlock.StatChanged -= HandleStatChanged;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || statBlock == null)
            return;

        ApplyConfigBaseValues();
    }

    public void EnsureInitialized()
    {
        if (statBlock != null)
            return;

        statBlock = new StatBlock();
        statBlock.StatChanged += HandleStatChanged;
        ApplyConfigBaseValues();
    }

    public float GetValue(StatType type)
    {
        EnsureInitialized();
        return statBlock.GetValue(type);
    }

    public void AddModifier(StatModifier modifier)
    {
        EnsureInitialized();
        statBlock.AddModifier(modifier);
    }

    public void AddModifier(StatModifier modifier, StatModifierSource source)
    {
        EnsureInitialized();
        statBlock.AddModifier(modifier, source);
    }

    public void AddModifiers(IEnumerable<StatModifierData> modifiers, StatModifierSource source)
    {
        EnsureInitialized();
        statBlock.AddModifiers(modifiers, source);
    }

    public void RemoveModifier(StatModifier modifier)
    {
        EnsureInitialized();
        statBlock.RemoveModifier(modifier);
    }

    public void RemoveModifiersFromSource(StatModifierSource source)
    {
        EnsureInitialized();
        statBlock.RemoveModifiersFromSource(source);
    }

    private void ApplyConfigBaseValues()
    {
        if (statBlock == null)
            return;

        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            float baseValue = config != null ? config.GetBaseValue(type) : 0f;
            statBlock.SetBaseValue(type, baseValue);
        }
    }

    private void HandleStatChanged(StatType type)
    {
        OnStatChanged?.Invoke(type);
    }
}
