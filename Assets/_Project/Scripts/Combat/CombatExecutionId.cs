using System;

namespace Titanhold.Combat
{
    public readonly struct CombatExecutionId : IEquatable<CombatExecutionId>
    {
        public CombatExecutionId(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public static CombatExecutionId New()
        {
            return new CombatExecutionId(Guid.NewGuid().ToString("N"));
        }

        public bool Equals(CombatExecutionId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CombatExecutionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(CombatExecutionId left, CombatExecutionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CombatExecutionId left, CombatExecutionId right)
        {
            return !left.Equals(right);
        }
    }
}
