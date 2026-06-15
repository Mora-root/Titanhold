using System;
using UnityEngine;

[Serializable]
public struct StatModifierData
{
    [SerializeField] private StatType type;
    [SerializeField] private StatModifierType modifierType;
    [SerializeField] private float value;

    public StatType Type => type;
    public StatModifierType ModifierType => modifierType;
    public float Value => value;

    public StatModifierData(StatType type, StatModifierType modifierType, float value)
    {
        this.type = type;
        this.modifierType = modifierType;
        this.value = value;
    }

    public StatModifier ToRuntimeModifier()
    {
        return new StatModifier(type, modifierType, value);
    }
}
