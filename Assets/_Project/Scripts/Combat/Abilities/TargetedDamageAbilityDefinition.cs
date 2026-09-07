using System;
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
        [SerializeField] private string animatorTrigger = "Attack";

        public string AbilityId => abilityId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public string Description => description ?? string.Empty;
        public Sprite Icon => icon;

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

            try
            {
                AbilityExecutionDefinition execution = new(
                    abilityId,
                    resourceCost,
                    cooldown,
                    windUp,
                    recovery);
                snapshot = new TargetedDamageAbilitySnapshot(
                    execution,
                    (float)damage,
                    useRange,
                    releaseRangeMultiplier,
                    obstructionMask.value,
                    animatorTrigger);
                return true;
            }
            catch (ArgumentException)
            {
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
