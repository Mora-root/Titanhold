using System;

namespace Titanhold.Combat.Effects
{
    // One stack contributes one linear flat or increased modifier. More and
    // override modifiers require different composition rules and are excluded.
    public sealed class TimedStackingStatEffectDefinition
    {
        public TimedStackingStatEffectDefinition(
            string effectId,
            StatType statType,
            StatModifierType modifierType,
            float valuePerStack,
            int maximumStacks,
            double duration)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                throw new ArgumentException(
                    "A timed stat effect requires a stable id.",
                    nameof(effectId));
            if (!Enum.IsDefined(typeof(StatType), statType))
                throw new ArgumentOutOfRangeException(nameof(statType));
            if (modifierType != StatModifierType.Flat &&
                modifierType != StatModifierType.Increased)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(modifierType),
                    "Stacked stat effects support only linear modifiers.");
            }
            if (!IsFinite(valuePerStack) || valuePerStack == 0f)
                throw new ArgumentOutOfRangeException(nameof(valuePerStack));
            if (maximumStacks <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumStacks));
            double maximumValue = (double)valuePerStack * maximumStacks;
            if (!IsFinite(maximumValue) ||
                Math.Abs(maximumValue) > float.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumStacks),
                    "The maximum stacked modifier must remain finite.");
            }
            if (!IsFinite(duration) || duration <= 0d)
                throw new ArgumentOutOfRangeException(nameof(duration));

            EffectId = effectId.Trim();
            StatType = statType;
            ModifierType = modifierType;
            ValuePerStack = valuePerStack;
            MaximumStacks = maximumStacks;
            Duration = duration;
        }

        public string EffectId { get; }
        public StatType StatType { get; }
        public StatModifierType ModifierType { get; }
        public float ValuePerStack { get; }
        public int MaximumStacks { get; }
        public double Duration { get; }

        internal bool HasSameRules(
            TimedStackingStatEffectDefinition other)
        {
            return other != null &&
                   StatType == other.StatType &&
                   ModifierType == other.ModifierType &&
                   ValuePerStack.Equals(other.ValuePerStack) &&
                   MaximumStacks == other.MaximumStacks &&
                   Duration.Equals(other.Duration);
        }

        internal StatModifier CreateModifier(int stackCount)
        {
            if (stackCount <= 0 || stackCount > MaximumStacks)
                throw new ArgumentOutOfRangeException(nameof(stackCount));

            return new StatModifier(
                StatType,
                ModifierType,
                ValuePerStack * stackCount);
        }

        internal static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
