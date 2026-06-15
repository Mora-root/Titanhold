using System;
using UnityEngine;

[Serializable]
public struct ItemModifierRollRule
{
    [SerializeField] private StatType type;
    [SerializeField] private StatModifierType modifierType;
    [SerializeField] private float minValue;
    [SerializeField] private float maxValue;
    [SerializeField] private bool wholeNumberValues;

    public ItemModifierRollRule(
        StatType type,
        StatModifierType modifierType,
        float minValue,
        float maxValue,
        bool wholeNumberValues = false)
    {
        this.type = type;
        this.modifierType = modifierType;
        this.minValue = minValue;
        this.maxValue = maxValue;
        this.wholeNumberValues = wholeNumberValues;
    }

    public StatType Type => type;
    public StatModifierType ModifierType => modifierType;
    public float MinValue => minValue;
    public float MaxValue => maxValue;
    public bool WholeNumberValues => wholeNumberValues;

    public StatModifierData Roll(System.Random random)
    {
        random ??= new System.Random();

        float lower = Mathf.Min(minValue, maxValue);
        float upper = Mathf.Max(minValue, maxValue);
        float value = Mathf.Approximately(lower, upper)
            ? lower
            : Mathf.Lerp(lower, upper, (float)random.NextDouble());

        if (wholeNumberValues)
            value = Mathf.Round(value);

        return new StatModifierData(type, modifierType, value);
    }
}
