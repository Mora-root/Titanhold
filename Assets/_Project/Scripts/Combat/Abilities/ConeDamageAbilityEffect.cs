using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    public static class ConeDamageAbilityEffect
    {
        public static CombatExecutionReport Apply(
            Transform source,
            AbilityExecutionSnapshot execution,
            ConeDamageAbilitySnapshot ability,
            double releasedAt)
        {
            Collider[] hits = Physics.OverlapSphere(
                source.position,
                ability.UseRange,
                ability.TargetMask,
                QueryTriggerInteraction.Ignore);
            HashSet<IDamageable> damagedTargets = new();
            List<DamageTargetResolution> resolutions = new();
            DamageRequest request = new(
                execution.ExecutionId,
                execution.Actor,
                ability.Damage,
                DamageCause.Ability,
                execution.Definition.AbilityId);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                IDamageable target =
                    hit.GetComponentInParent<IDamageable>();
                if (target == null || damagedTargets.Contains(target))
                    continue;

                Transform targetTransform = target is Component component
                    ? component.transform
                    : hit.transform;
                if (targetTransform.root == source.root)
                    continue;

                Vector3 targetPosition = ResolveTargetPosition(
                    hit,
                    targetTransform);
                if (!IsInsideCone(source, targetPosition, ability) ||
                    IsObstructed(
                        source.position,
                        targetPosition,
                        ability.ObstructionMask))
                {
                    continue;
                }

                damagedTargets.Add(target);
                DamageResult damageResult =
                    target.ApplyDamageRequest(request);
                resolutions.Add(new DamageTargetResolution(
                    target,
                    damageResult));
                TimedStatAbilityEffect.TryApply(
                    targetTransform,
                    damageResult,
                    ability.OnHitEffect,
                    execution.Actor,
                    releasedAt);
            }

            return new CombatExecutionReport(
                execution.ExecutionId,
                resolutions);
        }

        private static Vector3 ResolveTargetPosition(
            Collider hit,
            Transform targetTransform)
        {
            ITargetable targetable =
                hit.GetComponentInParent<ITargetable>();
            return targetable != null && targetable.AimPoint != null
                ? targetable.AimPoint.position
                : targetTransform.position;
        }

        private static bool IsInsideCone(
            Transform source,
            Vector3 targetPosition,
            ConeDamageAbilitySnapshot ability)
        {
            Vector3 direction = targetPosition - source.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > ability.UseRange * ability.UseRange)
                return false;
            if (direction.sqrMagnitude <= 0.0001f ||
                ability.ConeAngle >= 360f)
            {
                return true;
            }

            Vector3 forward = source.forward;
            forward.y = 0f;
            return forward.sqrMagnitude <= 0.0001f ||
                   Vector3.Angle(forward, direction) <=
                   ability.ConeAngle * 0.5f;
        }

        private static bool IsObstructed(
            Vector3 sourcePosition,
            Vector3 targetPosition,
            int obstructionMask)
        {
            return obstructionMask != 0 && Physics.Linecast(
                sourcePosition,
                targetPosition,
                obstructionMask,
                QueryTriggerInteraction.Ignore);
        }
    }
}
