using System;
using Titanhold.Combat.Effects;

namespace Titanhold.Combat.Abilities
{
    public sealed class TargetedMovementAbilitySnapshot :
        IRuntimeAbilitySnapshot,
        IRuntimeAbilityMovement
    {
        public TargetedMovementAbilitySnapshot(
            AbilityExecutionDefinition execution,
            float useRange,
            int obstructionMask,
            float maximumUseAngle,
            float speedMultiplier,
            float arrivalDistance,
            string animatorTrigger,
            TimedStackingStatEffectDefinition selfEffect)
        {
            Execution = execution ??
                throw new ArgumentNullException(nameof(execution));
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(useRange) ||
                useRange <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(useRange));
            }
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(
                    maximumUseAngle) ||
                maximumUseAngle <= 0f || maximumUseAngle > 180f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumUseAngle));
            }
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(
                    speedMultiplier) || speedMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(speedMultiplier));
            }
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(
                    arrivalDistance) || arrivalDistance < 0f ||
                arrivalDistance >= useRange)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(arrivalDistance));
            }

            UseRange = useRange;
            ObstructionMask = obstructionMask;
            MaximumUseAngle = maximumUseAngle;
            SpeedMultiplier = speedMultiplier;
            ArrivalDistance = arrivalDistance;
            AnimatorTrigger = animatorTrigger?.Trim() ?? string.Empty;
            SelfEffect = selfEffect;
        }

        public AbilityExecutionDefinition Execution { get; }
        public float UseRange { get; }
        public int ObstructionMask { get; }
        public float MaximumUseAngle { get; }
        public float SpeedMultiplier { get; }
        public float ArrivalDistance { get; }
        public string AnimatorTrigger { get; }
        public TimedStackingStatEffectDefinition SelfEffect { get; }
        public PostAbilityActionPolicy PostActionPolicy =>
            PostAbilityActionPolicy.ContinueBasicAttackOnPrimaryTarget;

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

        public bool TryGetMovement(
            AbilityUseContext context,
            out AbilityMovementDirective movement)
        {
            movement = default;
            if (!context.HasSource || !context.HasUsableTarget)
                return false;

            try
            {
                movement = new AbilityMovementDirective(
                    context.SelectedTarget.AimPoint.position,
                    SpeedMultiplier,
                    ArrivalDistance);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
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
                SelfEffect,
                execution.Actor,
                releasedAt);
            return CombatExecutionReport.Empty(execution.ExecutionId);
        }
    }
}
