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

    public StatModifier ToRuntimeModifier()
    {
        return new StatModifier(type, modifierType, value);
    }
}
