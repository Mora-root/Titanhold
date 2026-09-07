using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    public static class TargetedAbilityRules
    {
        public static bool CanReach(
            AbilityUseContext context,
            float maximumDistance,
            int obstructionMask)
        {
            if (!context.HasSource ||
                !context.HasUsableTarget ||
                !AbilityExecutionDefinition.IsNonNegativeFinite(maximumDistance) ||
                maximumDistance <= 0f ||
                IsSelfTarget(context))
            {
                return false;
            }

            Vector3 targetPosition =
                context.SelectedTarget.AimPoint.position;
            if (Vector3.Distance(
                    context.Source.position,
                    targetPosition) > maximumDistance)
            {
                return false;
            }

            return obstructionMask == 0 ||
                   !Physics.Linecast(
                       context.Source.position,
                       targetPosition,
                       obstructionMask,
                       QueryTriggerInteraction.Ignore);
        }

        private static bool IsSelfTarget(AbilityUseContext context)
        {
            return context.SelectedTarget is Component component &&
                   component.transform.root == context.Source.root;
        }
    }
}
