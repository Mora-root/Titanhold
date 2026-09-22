using System;
using Titanhold.Combat.Effects;
using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    [CreateAssetMenu(
        menuName = "Titanhold/Abilities/Self Stat Effect Ability")]
    public sealed class SelfStatEffectAbilityDefinition :
        ScriptableObject,
        IRuntimeAbilityDefinition,
        IAbilityPresentationDefinition
    {
        [SerializeField] private string abilityId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0f)] private float resourceCost = 20f;
        [SerializeField, Min(0f)] private float cooldown = 20f;
        [SerializeField, Min(0f)] private float windUp;
        [SerializeField, Min(0f)] private float recovery = 0.2f;
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
            return new AbilityCommitEvaluation(
                context.HasSource
                    ? AbilityCommitStatus.Ready
                    : AbilityCommitStatus.MissingSource);
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
            out SelfStatEffectAbilitySnapshot snapshot)
        {
            snapshot = null;
            if (selfEffect == null ||
                !selfEffect.TryCreateDefinition(
                    out TimedStackingStatEffectDefinition effect) ||
                effect == null ||
                !TryCreateExecutionDefinition(
                    out AbilityExecutionDefinition execution))
            {
                return false;
            }

            try
            {
                snapshot = new SelfStatEffectAbilitySnapshot(
                    execution,
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
                    out SelfStatEffectAbilitySnapshot effectSnapshot))
            {
                return false;
            }

            snapshot = effectSnapshot;
            return true;
        }
    }
}
