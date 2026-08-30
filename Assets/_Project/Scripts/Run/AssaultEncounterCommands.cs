using System;
using Titanhold.Combat;

namespace Titanhold.Run
{
    public readonly struct AssaultEncounterId : IEquatable<AssaultEncounterId>
    {
        public AssaultEncounterId(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(AssaultEncounterId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is AssaultEncounterId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;
        }

        public static bool operator ==(AssaultEncounterId left, AssaultEncounterId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AssaultEncounterId left, AssaultEncounterId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct BeginAssaultEncounterCommand
    {
        public BeginAssaultEncounterCommand(
            AssaultEncounterId encounterId,
            int expectedRound,
            int plannedEnemyCount)
        {
            EncounterId = encounterId;
            ExpectedRound = expectedRound;
            PlannedEnemyCount = plannedEnemyCount;
        }

        public AssaultEncounterId EncounterId { get; }
        public int ExpectedRound { get; }
        public int PlannedEnemyCount { get; }
    }

    public readonly struct AssaultEnemyCommand
    {
        public AssaultEnemyCommand(
            AssaultEncounterId encounterId,
            CombatActorReference enemy)
        {
            EncounterId = encounterId;
            Enemy = enemy;
        }

        public AssaultEncounterId EncounterId { get; }
        public CombatActorReference Enemy { get; }
    }
}
