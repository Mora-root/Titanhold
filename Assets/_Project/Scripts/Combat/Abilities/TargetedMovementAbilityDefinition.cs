using System;
using Titanhold.Combat.Effects;
using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    [CreateAssetMenu(
        menuName = "Titanhold/Abilities/Targeted Movement Ability")]
    public sealed class TargetedMovementAbilityDefinition :
        ScriptableObject,
        IRuntimeAbilityDefinition,
        IAbilityPresentationDefinition
    {
        [SerializeField] private string abilityId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0f)] private float resourceCost = 20f;
        [SerializeField, Min(0f)] private float cooldown = 10f;
        [SerializeField, Min(0f)] private float windUp = 0.4f;
        [SerializeField, Min(0f)] private float recovery = 0.2f;
        [SerializeField, Min(0.01f)] private float useRange = 8f;
        [SerializeField] private LayerMask obstructionMask;
        [SerializeField, Range(1f, 180f)]
        private float maximumUseAngle = 45f;
        [SerializeField, Min(0.01f)]
        private float movementSpeedMultiplier = 4f;
        [SerializeField, Min(0f)] private float arrivalDistance = 1.25f;
        [SerializeField] private string animatorTrigger;
        [SerializeField]
        private TimedStackingStatEffectAuthoring selfEffect = new();

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
            out TargetedMovementAbilitySnapshot snapshot)
        {
            snapshot = null;
            TimedStackingStatEffectDefinition effect = null;
            if (selfEffect != null &&
                !selfEffect.TryCreateDefinition(out effect))
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
                snapshot = new TargetedMovementAbilitySnapshot(
                    execution,
                    useRange,
                    obstructionMask.value,
                    maximumUseAngle,
                    movementSpeedMultiplier,
                    arrivalDistance,
                    animatorTrigger,
                    effect);
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
                    out TargetedMovementAbilitySnapshot movementSnapshot))
            {
                return false;
            }

            snapshot = movementSnapshot;
            return true;
        }
    }
}
