using System;
using Titanhold.Combat.Effects;

namespace Titanhold.Combat.Abilities
{
    public sealed class ConeDamageAbilitySnapshot : IRuntimeAbilitySnapshot
    {
        public ConeDamageAbilitySnapshot(
            AbilityExecutionDefinition execution,
            float damage,
            float useRange,
            float coneAngle,
            int targetMask,
            int obstructionMask,
            float maximumUseAngle,
            string animatorTrigger,
            TimedStackingStatEffectDefinition onHitEffect = null)
        {
            Execution = execution ??
                throw new ArgumentNullException(nameof(execution));
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(damage))
                throw new ArgumentOutOfRangeException(nameof(damage));
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(useRange) ||
                useRange <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(useRange));
            }
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(coneAngle) ||
                coneAngle <= 0f || coneAngle > 360f)
            {
                throw new ArgumentOutOfRangeException(nameof(coneAngle));
            }
            if (targetMask == 0)
                throw new ArgumentOutOfRangeException(nameof(targetMask));
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(
                    maximumUseAngle) ||
                maximumUseAngle <= 0f || maximumUseAngle > 180f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumUseAngle));
            }
            if (string.IsNullOrWhiteSpace(animatorTrigger))
            {
                throw new ArgumentException(
                    "An animation trigger is required.",
                    nameof(animatorTrigger));
            }

            Damage = damage;
            UseRange = useRange;
            ConeAngle = coneAngle;
            TargetMask = targetMask;
            ObstructionMask = obstructionMask;
            MaximumUseAngle = maximumUseAngle;
            AnimatorTrigger = animatorTrigger;
            OnHitEffect = onHitEffect;
        }

        public AbilityExecutionDefinition Execution { get; }
        public float Damage { get; }
        public float UseRange { get; }
        public float ConeAngle { get; }
        public int TargetMask { get; }
        public int ObstructionMask { get; }
        public float MaximumUseAngle { get; }
        public string AnimatorTrigger { get; }
        public TimedStackingStatEffectDefinition OnHitEffect { get; }

        public bool CanCommit(AbilityUseContext context)
        {
            return EvaluateCommit(context).IsReady;
        }

        public AbilityCommitEvaluation EvaluateCommit(
            AbilityUseContext context)
        {
            return TargetedAbilityRules.Evaluate(
                context,
                UseRange,
                ObstructionMask,
                true,
                MaximumUseAngle);
        }

        public CombatExecutionReport Release(
            AbilityUseContext context,
            AbilityExecutionSnapshot execution,
            double releasedAt)
        {
            if (execution == null)
                throw new ArgumentNullException(nameof(execution));
            if (!context.HasSource)
                return CombatExecutionReport.Empty(execution.ExecutionId);

            return ConeDamageAbilityEffect.Apply(
                context.Source,
                execution,
                this,
                releasedAt);
        }
    }
}
