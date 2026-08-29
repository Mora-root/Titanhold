using System;

[Serializable]
public readonly struct StatModifierSource : IEquatable<StatModifierSource>
{
    public static readonly StatModifierSource None = new(StatModifierSourceKind.None, string.Empty);

    public StatModifierSource(StatModifierSourceKind kind, string sourceId)
    {
        Kind = kind;
        SourceId = sourceId ?? string.Empty;
    }

    public StatModifierSourceKind Kind { get; }
    public string SourceId { get; }
    public bool IsValid => Kind != StatModifierSourceKind.None && !string.IsNullOrWhiteSpace(SourceId);

    public static StatModifierSource ForEquipmentSlot(EquipmentSlotId slotId)
    {
        return new StatModifierSource(StatModifierSourceKind.EquipmentSlot, slotId.ToString());
    }

    public static StatModifierSource ForBuff(string buffId)
    {
        return new StatModifierSource(StatModifierSourceKind.Buff, buffId);
    }

    public static StatModifierSource ForActivity(string activityId)
    {
        return new StatModifierSource(StatModifierSourceKind.Activity, activityId);
    }

    public static StatModifierSource ForSystem(string systemId)
    {
        return new StatModifierSource(StatModifierSourceKind.System, systemId);
    }

    public bool Equals(StatModifierSource other)
    {
        return Kind == other.Kind &&
               string.Equals(SourceId, other.SourceId, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is StatModifierSource other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Kind, SourceId);
    }

    public override string ToString()
    {
        return IsValid ? $"{Kind}:{SourceId}" : "None";
    }

    public static bool operator ==(StatModifierSource left, StatModifierSource right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StatModifierSource left, StatModifierSource right)
    {
        return !left.Equals(right);
    }
}
