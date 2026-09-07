using System;

namespace Titanhold.Combat.Abilities
{
    // Offensive values and query settings are frozen at commit; target defenses
    // are evaluated by ApplyDamageRequest when the effect is released.
    public sealed class AreaDamageAbilitySnapshot : IRuntimeAbilitySnapshot
    {
        public AreaDamageAbilitySnapshot(AbilityExecutionDefinition execution,
            float damage, float radius, int targetMask, string animatorTrigger)
        {
            Execution = execution ?? throw new ArgumentNullException(nameof(execution));
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(damage))
                throw new ArgumentOutOfRangeException(nameof(damage));
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(radius) || radius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (targetMask == 0)
                throw new ArgumentOutOfRangeException(nameof(targetMask));
            if (string.IsNullOrWhiteSpace(animatorTrigger))
                throw new ArgumentException("An animation trigger is required.", nameof(animatorTrigger));

            Damage = damage;
            Radius = radius;
            TargetMask = targetMask;
            AnimatorTrigger = animatorTrigger;
        }

        public AbilityExecutionDefinition Execution { get; }
        public float Damage { get; }
        public float Radius { get; }
        public int TargetMask { get; }
        public string AnimatorTrigger { get; }

        public bool CanCommit(AbilityUseContext context)
        {
            return context.HasSource;
        }

        public CombatExecutionReport Release(
            AbilityUseContext context,
            AbilityExecutionSnapshot execution)
        {
            if (execution == null)
                throw new ArgumentNullException(nameof(execution));
            if (!context.HasSource)
                return CombatExecutionReport.Empty(execution.ExecutionId);

            return AreaDamageAbilityEffect.Apply(
                context.Source,
                execution,
                this);
        }
    }
}
