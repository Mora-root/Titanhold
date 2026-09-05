using System;
using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    [CreateAssetMenu(menuName = "Titanhold/Abilities/Area Damage Ability")]
    public sealed class AreaDamageAbilityDefinition : ScriptableObject
    {
        [SerializeField] private string abilityId;
        [SerializeField, Min(0f)] private float resourceCost = 20f;
        [SerializeField, Min(0f)] private float cooldown = 3f;
        [SerializeField, Min(0f)] private float windUp = 0.23333333f;
        [SerializeField, Min(0f)] private float recovery = 0.30000003f;
        [SerializeField, Min(0f)] private float damageMultiplier = 1.5f;
        [SerializeField, Min(0f)] private float radius = 2.5f;
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private string animatorTrigger = "Spin";

        public bool TryCreateSnapshot(float baseDamage, out AreaDamageAbilitySnapshot snapshot)
        {
            snapshot = null;
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(baseDamage) ||
                !AbilityExecutionDefinition.IsNonNegativeFinite(damageMultiplier))
                return false;

            try
            {
                AbilityExecutionDefinition execution = new(
                    abilityId, resourceCost, cooldown, windUp, recovery);
                snapshot = new AreaDamageAbilitySnapshot(execution,
                    baseDamage * damageMultiplier, radius, targetMask.value, animatorTrigger);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
