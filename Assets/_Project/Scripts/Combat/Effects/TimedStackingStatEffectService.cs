using System;
using System.Collections.Generic;
using Titanhold.Combat;

namespace Titanhold.Combat.Effects
{
    public interface ITimedStatModifierGateway
    {
        bool TrySetModifier(
            StatModifierSource modifierSource,
            StatModifier modifier);

        void RemoveModifier(StatModifierSource modifierSource);
    }

    public readonly struct TimedStatEffectKey :
        IEquatable<TimedStatEffectKey>
    {
        public TimedStatEffectKey(
            string effectId,
            CombatActorReference source)
        {
            EffectId = effectId ?? string.Empty;
            Source = source;
        }

        public string EffectId { get; }
        public CombatActorReference Source { get; }
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(EffectId) && Source.IsValid;

        public StatModifierSource ModifierSource =>
            StatModifierSource.ForBuff(
                $"{EffectId}|{(int)Source.Kind}|{Source.ActorId}");

        public bool Equals(TimedStatEffectKey other)
        {
            return Source == other.Source &&
                   string.Equals(
                       EffectId,
                       other.EffectId,
                       StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is TimedStatEffectKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(EffectId ?? string.Empty),
                Source);
        }
    }

    public readonly struct TimedStatEffectSnapshot
    {
        internal TimedStatEffectSnapshot(
            TimedStatEffectKey key,
            TimedStackingStatEffectDefinition definition,
            int stackCount,
            double expiresAt)
        {
            Key = key;
            Definition = definition;
            StackCount = stackCount;
            ExpiresAt = expiresAt;
        }

        public TimedStatEffectKey Key { get; }
        public TimedStackingStatEffectDefinition Definition { get; }
        public int StackCount { get; }
        public double ExpiresAt { get; }
    }

    public sealed class TimedStackingStatEffectService
    {
        private sealed class ActiveEffect
        {
            public ActiveEffect(
                TimedStackingStatEffectDefinition definition,
                int stackCount,
                double expiresAt)
            {
                Definition = definition;
                StackCount = stackCount;
                ExpiresAt = expiresAt;
            }

            public TimedStackingStatEffectDefinition Definition { get; }
            public int StackCount { get; set; }
            public double ExpiresAt { get; set; }
        }

        private readonly ITimedStatModifierGateway modifierGateway;
        private readonly Dictionary<TimedStatEffectKey, ActiveEffect> active = new();
        private readonly List<TimedStatEffectKey> expired = new();

        public TimedStackingStatEffectService(
            ITimedStatModifierGateway modifierGateway)
        {
            this.modifierGateway = modifierGateway ??
                throw new ArgumentNullException(nameof(modifierGateway));
        }

        public int ActiveCount => active.Count;

        public bool TryApply(
            TimedStackingStatEffectDefinition definition,
            CombatActorReference source,
            double simulationTime,
            out TimedStatEffectSnapshot snapshot)
        {
            snapshot = default;
            if (definition == null ||
                !source.IsValid ||
                !IsValidTime(simulationTime))
            {
                return false;
            }

            double expiresAt = simulationTime + definition.Duration;
            if (!IsValidTime(expiresAt))
                return false;

            TimedStatEffectKey key = new(definition.EffectId, source);
            int nextStackCount = 1;
            if (active.TryGetValue(key, out ActiveEffect current))
            {
                if (!current.Definition.HasSameRules(definition))
                    return false;

                nextStackCount = Math.Min(
                    current.StackCount + 1,
                    definition.MaximumStacks);
            }

            if ((current == null || current.StackCount != nextStackCount) &&
                !modifierGateway.TrySetModifier(
                    key.ModifierSource,
                    definition.CreateModifier(nextStackCount)))
            {
                return false;
            }

            if (current == null)
            {
                current = new ActiveEffect(
                    definition,
                    nextStackCount,
                    expiresAt);
                active.Add(key, current);
            }
            else
            {
                current.StackCount = nextStackCount;
                current.ExpiresAt = expiresAt;
            }

            snapshot = CreateSnapshot(key, current);
            return true;
        }

        public bool TryGet(
            string effectId,
            CombatActorReference source,
            out TimedStatEffectSnapshot snapshot)
        {
            snapshot = default;
            TimedStatEffectKey key = new(
                effectId?.Trim() ?? string.Empty,
                source);
            if (!key.IsValid ||
                !active.TryGetValue(key, out ActiveEffect effect))
            {
                return false;
            }

            snapshot = CreateSnapshot(key, effect);
            return true;
        }

        public int Tick(double simulationTime)
        {
            if (!IsValidTime(simulationTime) || active.Count == 0)
                return 0;

            expired.Clear();
            foreach (KeyValuePair<TimedStatEffectKey, ActiveEffect> pair in active)
            {
                if (simulationTime >= pair.Value.ExpiresAt)
                    expired.Add(pair.Key);
            }

            for (int i = 0; i < expired.Count; i++)
            {
                TimedStatEffectKey key = expired[i];
                modifierGateway.RemoveModifier(key.ModifierSource);
                active.Remove(key);
            }

            int removedCount = expired.Count;
            expired.Clear();
            return removedCount;
        }

        public void Clear()
        {
            if (active.Count == 0)
                return;

            expired.Clear();
            foreach (TimedStatEffectKey key in active.Keys)
                expired.Add(key);

            for (int i = 0; i < expired.Count; i++)
                modifierGateway.RemoveModifier(expired[i].ModifierSource);

            active.Clear();
            expired.Clear();
        }

        private static TimedStatEffectSnapshot CreateSnapshot(
            TimedStatEffectKey key,
            ActiveEffect effect)
        {
            return new TimedStatEffectSnapshot(
                key,
                effect.Definition,
                effect.StackCount,
                effect.ExpiresAt);
        }

        private static bool IsValidTime(double value)
        {
            return value >= 0d &&
                   TimedStackingStatEffectDefinition.IsFinite(value);
        }
    }
}
