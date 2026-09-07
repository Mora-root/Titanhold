using System;

namespace Titanhold.Combat.Abilities
{
    // Offensive values and targeting rules are fixed at commit. Target state,
    // position, line of sight, and defenses are evaluated again on release.
    public sealed class TargetedDamageAbilitySnapshot :
        IRuntimeAbilitySnapshot
    {
        public TargetedDamageAbilitySnapshot(
            AbilityExecutionDefinition execution,
            float damage,
            float useRange,
            float releaseRangeMultiplier,
            int obstructionMask,
            float maximumUseAngle,
            string animatorTrigger)
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
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(
                    releaseRangeMultiplier) ||
                releaseRangeMultiplier < 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(releaseRangeMultiplier));
            }
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(maximumUseAngle) ||
                maximumUseAngle <= 0f || maximumUseAngle > 180f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumUseAngle));
            }

            double releaseRange =
                (double)useRange * releaseRangeMultiplier;
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(releaseRange) ||
                releaseRange > float.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(releaseRangeMultiplier));
            }
            if (string.IsNullOrWhiteSpace(animatorTrigger))
            {
                throw new ArgumentException(
                    "An animation trigger is required.",
                    nameof(animatorTrigger));
            }

            Damage = damage;
            UseRange = useRange;
            ReleaseRange = (float)releaseRange;
            ObstructionMask = obstructionMask;
            MaximumUseAngle = maximumUseAngle;
            AnimatorTrigger = animatorTrigger;
        }

        public AbilityExecutionDefinition Execution { get; }
        public float Damage { get; }
        public float UseRange { get; }
        public float ReleaseRange { get; }
        public int ObstructionMask { get; }
        public float MaximumUseAngle { get; }
        public string AnimatorTrigger { get; }

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
            AbilityExecutionSnapshot execution)
        {
            if (execution == null)
                throw new ArgumentNullException(nameof(execution));
            if (!TargetedAbilityRules.Evaluate(
                    context,
                    ReleaseRange,
                    ObstructionMask,
                    false,
                    MaximumUseAngle).IsReady)
            {
                return CombatExecutionReport.Empty(execution.ExecutionId);
            }

            IDamageable target = context.SelectedTarget.AimPoint
                .GetComponentInParent<IDamageable>();
            if (target == null)
                return CombatExecutionReport.Empty(execution.ExecutionId);

            DamageRequest request = new(
                execution.ExecutionId,
                execution.Actor,
                Damage,
                DamageCause.Ability,
                execution.Definition.AbilityId);
            DamageResult result = target.ApplyDamageRequest(request);
            return CombatExecutionReport.Single(
                execution.ExecutionId,
                new DamageTargetResolution(target, result));
        }
    }
}
