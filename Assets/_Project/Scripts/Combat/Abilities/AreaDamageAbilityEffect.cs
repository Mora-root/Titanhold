using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    public static class AreaDamageAbilityEffect
    {
        // Called only for a successful release. One report covers every target,
        // including targets represented by multiple colliders.
        public static CombatExecutionReport Apply(Transform source,
            AbilityExecutionSnapshot execution, AreaDamageAbilitySnapshot ability)
        {
            Collider[] hits = Physics.OverlapSphere(
                source.position, ability.Radius, ability.TargetMask);
            HashSet<IDamageable> damagedTargets = new();
            List<DamageTargetResolution> resolutions = new();
            DamageRequest request = new(execution.ExecutionId, execution.Actor,
                ability.Damage, DamageCause.Ability, execution.Definition.AbilityId);

            foreach (Collider hit in hits)
            {
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                if (target == null ||
                    (target is Component component && component.transform.root == source.root) ||
                    !damagedTargets.Add(target))
                    continue;

                resolutions.Add(new DamageTargetResolution(target, target.ApplyDamageRequest(request)));
            }

            return new CombatExecutionReport(execution.ExecutionId, resolutions);
        }
    }
}
