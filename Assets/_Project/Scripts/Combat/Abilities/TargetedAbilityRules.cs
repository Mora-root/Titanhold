using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    public static class TargetedAbilityRules
    {
        public static AbilityCommitEvaluation Evaluate(
            AbilityUseContext context,
            float maximumDistance,
            int obstructionMask,
            bool checkFacing,
            float maximumFacingAngle)
        {
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(maximumDistance) ||
                maximumDistance <= 0f ||
                (checkFacing &&
                 (!AbilityExecutionDefinition.IsNonNegativeFinite(maximumFacingAngle) ||
                  maximumFacingAngle <= 0f || maximumFacingAngle > 180f)))
            {
                return Result(AbilityCommitStatus.InvalidDefinition);
            }
            if (!context.HasSource)
                return Result(AbilityCommitStatus.MissingSource);
            if (!context.HasSelectedTarget)
                return Result(AbilityCommitStatus.MissingTarget);
            if (!context.HasUsableTarget)
                return Result(AbilityCommitStatus.InvalidTarget);
            if (IsSelfTarget(context))
                return Result(AbilityCommitStatus.SelfTarget);

            Vector3 targetPosition =
                context.SelectedTarget.AimPoint.position;
            if (Vector3.Distance(
                    context.Source.position,
                    targetPosition) > maximumDistance)
            {
                return Result(AbilityCommitStatus.OutOfRange);
            }

            if (obstructionMask != 0 &&
                Physics.Linecast(
                       context.Source.position,
                       targetPosition,
                       obstructionMask,
                       QueryTriggerInteraction.Ignore))
            {
                return Result(AbilityCommitStatus.Obstructed);
            }

            if (checkFacing && !IsFacing(
                    context.Source,
                    targetPosition,
                    maximumFacingAngle))
            {
                return Result(AbilityCommitStatus.NeedsFacing);
            }

            return Result(AbilityCommitStatus.Ready);
        }

        private static bool IsSelfTarget(AbilityUseContext context)
        {
            return context.SelectedTarget is Component component &&
                   component.transform.root == context.Source.root;
        }

        private static bool IsFacing(
            Transform source,
            Vector3 targetPosition,
            float maximumFacingAngle)
        {
            Vector3 direction = targetPosition - source.position;
            direction.y = 0f;
            Vector3 forward = source.forward;
            forward.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f ||
                forward.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            return Vector3.Angle(forward, direction) <= maximumFacingAngle;
        }

        private static AbilityCommitEvaluation Result(
            AbilityCommitStatus status)
        {
            return new AbilityCommitEvaluation(status);
        }
    }
}
