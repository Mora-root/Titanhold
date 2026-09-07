using Titanhold.Combat.Effects;
using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    public static class TimedStatAbilityEffect
    {
        public static bool TryApply(
            Transform targetTransform,
            DamageResult damageResult,
            TimedStackingStatEffectDefinition definition,
            CombatActorReference source,
            double simulationTime)
        {
            if (targetTransform == null || definition == null ||
                !damageResult.WasApplied || damageResult.Killed)
            {
                return false;
            }

            ITimedStackingStatEffectReceiver receiver =
                targetTransform
                    .GetComponentInParent<ITimedStackingStatEffectReceiver>();
            return receiver != null && receiver.TryApply(
                definition,
                source,
                simulationTime,
                out _);
        }
    }
}
