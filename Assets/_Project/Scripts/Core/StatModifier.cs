
public enum StatModifierType
{
    Flat = 0,
    Increased = 1,
    More = 2,
    Override = 3
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
