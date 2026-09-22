using System;
using Titanhold.Combat.Effects;

namespace Titanhold.Combat.Abilities
{
    public sealed class SelfStatEffectAbilitySnapshot :
        IRuntimeAbilitySnapshot
    {
        public SelfStatEffectAbilitySnapshot(
            AbilityExecutionDefinition execution,
            string animatorTrigger,
            TimedStackingStatEffectDefinition effect)
        {
            Execution = execution ??
                throw new ArgumentNullException(nameof(execution));
            Effect = effect ?? throw new ArgumentNullException(nameof(effect));
            AnimatorTrigger = animatorTrigger?.Trim() ?? string.Empty;
        }

        public AbilityExecutionDefinition Execution { get; }
        public string AnimatorTrigger { get; }
        public TimedStackingStatEffectDefinition Effect { get; }
        public PostAbilityActionPolicy PostActionPolicy =>
            PostAbilityActionPolicy.None;

        public bool CanCommit(AbilityUseContext context)
        {
            return context.HasSource;
        }

        public CombatExecutionReport Release(
            AbilityUseContext context,
            AbilityExecutionSnapshot execution,
            double releasedAt)
        {
            if (execution == null)
                throw new ArgumentNullException(nameof(execution));

            TimedSelfStatAbilityEffect.TryApply(
                context,
                Effect,
                execution.Actor,
                releasedAt);
            return CombatExecutionReport.Empty(execution.ExecutionId);
        }
    }
}
