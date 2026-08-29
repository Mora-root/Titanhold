using System;

namespace Titanhold.Combat
{
    public enum CombatActorKind
    {
        Unknown,
        Player,
        Enemy,
        Environment
    }

    public readonly struct CombatActorReference : IEquatable<CombatActorReference>
    {
        public CombatActorReference(string actorId, CombatActorKind kind)
        {
            ActorId = actorId ?? string.Empty;
            Kind = kind;
        }

        public string ActorId { get; }
        public CombatActorKind Kind { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(ActorId) && Kind != CombatActorKind.Unknown;
        public bool IsPlayer => Kind == CombatActorKind.Player;
        public bool IsEnemy => Kind == CombatActorKind.Enemy;

        public static CombatActorReference Unknown => default;

        public bool Equals(CombatActorReference other)
        {
            return Kind == other.Kind && string.Equals(ActorId, other.ActorId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CombatActorReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ActorId != null ? StringComparer.Ordinal.GetHashCode(ActorId) : 0) * 397) ^ (int)Kind;
            }
        }

        public static bool operator ==(CombatActorReference left, CombatActorReference right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CombatActorReference left, CombatActorReference right)
        {
            return !left.Equals(right);
        }
    }
}
