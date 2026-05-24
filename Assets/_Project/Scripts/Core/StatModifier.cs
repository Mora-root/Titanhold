
public enum StatModifierType
{
    Flat,
    Percent
}

public class StatModifier
{
    public StatType Type { get; }
    public StatModifierType ModifierType { get; }
    public float Value { get; }

    public StatModifier(StatType type, StatModifierType modifierType, float value)
    {
        Type = type;
        ModifierType = modifierType;
        Value = value;
    }
}
