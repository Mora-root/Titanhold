using System;
using Titanhold.Combat.Effects;
using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    [CreateAssetMenu(
        menuName = "Titanhold/Abilities/Targeted Damage Ability")]
    public sealed class TargetedDamageAbilityDefinition :
        ScriptableObject,
        IRuntimeAbilityDefinition,
        IAbilityPresentationDefinition
    {
        [SerializeField] private string abilityId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0f)] private float resourceCost = 20f;
        [SerializeField, Min(0f)] private float cooldown = 3f;
        [SerializeField, Min(0f)] private float windUp = 0.23333333f;
        [SerializeField, Min(0f)] private float recovery = 0.30000003f;
        [SerializeField, Min(0f)] private float damageMultiplier = 1.5f;
        [SerializeField, Min(0.01f)] private float useRange = 2f;
        [SerializeField, Min(1f)] private float releaseRangeMultiplier = 1.5f;
        [SerializeField] private LayerMask obstructionMask;
        [SerializeField, Range(1f, 180f)] private float maximumUseAngle = 45f;
        [SerializeField] private string animatorTrigger = "Attack";
        [SerializeField]
        private TimedStackingStatEffectAuthoring onHitEffect = new();
        [SerializeField]
        private AbilitySourceResourceGainAuthoring sourceResourceGain = new();

        public string AbilityId => abilityId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public string Description => description ?? string.Empty;
        public Sprite Icon => icon;

        public AbilityCommitEvaluation EvaluateUse(
            AbilityUseContext context)
        {
            return TargetedAbilityRules.Evaluate(
                context,
                useRange,
                obstructionMask.value,
                true,
                maximumUseAngle);
        }

        public bool TryCreateExecutionDefinition(
            out AbilityExecutionDefinition execution)
        {
            try
            {
                execution = new AbilityExecutionDefinition(
                    abilityId,
                    resourceCost,
                    cooldown,
                    windUp,
                    recovery);
                return true;
            }
            catch (ArgumentException)
            {
                execution = null;
                return false;
            }
        }

        public bool TryCreateSnapshot(
            float baseDamage,
            out TargetedDamageAbilitySnapshot snapshot)
        {
            snapshot = null;
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(baseDamage) ||
                !AbilityExecutionDefinition.IsNonNegativeFinite(
                    damageMultiplier))
            {
                return false;
            }

            double damage = (double)baseDamage * damageMultiplier;
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(damage) ||
                damage > float.MaxValue)
            {
                return false;
            }

            TimedStackingStatEffectDefinition effect = null;
            if (onHitEffect != null &&
                !onHitEffect.TryCreateDefinition(out effect))
            {
                return false;
            }

            AbilitySourceResourceGain resourceGain = default;
            if (sourceResourceGain != null &&
                !sourceResourceGain.TryCreate(out resourceGain))
            {
                return false;
            }

            if (!TryCreateExecutionDefinition(
                    out AbilityExecutionDefinition execution))
            {
                return false;
            }

            try
            {
                snapshot = new TargetedDamageAbilitySnapshot(
                    execution,
                    (float)damage,
                    useRange,
                    releaseRangeMultiplier,
                    obstructionMask.value,
                    maximumUseAngle,
                    animatorTrigger,
                    effect,
                    resourceGain);
                return true;
            }
            catch (ArgumentException)
            {
                snapshot = null;
                return false;
            }
        }

        public bool TryCreateRuntimeSnapshot(
            AbilityActorSnapshot actor,
            out IRuntimeAbilitySnapshot snapshot)
        {
            snapshot = null;
            if (!actor.IsValid ||
                !TryCreateSnapshot(
                    actor.GlobalDamage,
                    out TargetedDamageAbilitySnapshot targetedSnapshot))
            {
                return false;
            }

            snapshot = targetedSnapshot;
            return true;
        }
    }
}
