using Titanhold.Combat;
using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    // Actor values are captured once when the ability is committed. Persistent
    // effects may use their own later evaluation policy without changing this
    // one-release execution contract.
    public readonly struct AbilityActorSnapshot
    {
        public AbilityActorSnapshot(float globalDamage)
        {
            GlobalDamage = globalDamage;
        }

        public float GlobalDamage { get; }
        public bool IsValid =>
            AbilityExecutionDefinition.IsNonNegativeFinite(GlobalDamage);
    }

    // The caller supplies targeting explicitly. Runtime definitions decide
    // whether a target is required and revalidate it again on release.
    public readonly struct AbilityUseContext
    {
        public AbilityUseContext(Transform source, ITargetable selectedTarget)
        {
            Source = source;
            SelectedTarget = selectedTarget;
        }

        public Transform Source { get; }
        public ITargetable SelectedTarget { get; }
        public bool HasSource => Source != null;
        public bool HasSelectedTarget =>
            SelectedTarget != null &&
            (!(SelectedTarget is Object unityObject) || unityObject != null);
        public bool HasUsableTarget
        {
            get
            {
                if (!HasSelectedTarget)
                    return false;

                return SelectedTarget.IsTargetable &&
                       SelectedTarget.AimPoint != null;
            }
        }
    }

    public enum AbilityCommitStatus
    {
        Ready,
        InvalidDefinition,
        MissingSource,
        MissingTarget,
        InvalidTarget,
        SelfTarget,
        OutOfRange,
        Obstructed,
        NeedsFacing
    }

    public readonly struct AbilityCommitEvaluation
    {
        public AbilityCommitEvaluation(AbilityCommitStatus status)
        {
            Status = status;
        }

        public AbilityCommitStatus Status { get; }
        public bool IsReady => Status == AbilityCommitStatus.Ready;
        public bool CanReposition =>
            Status == AbilityCommitStatus.OutOfRange ||
            Status == AbilityCommitStatus.Obstructed ||
            Status == AbilityCommitStatus.NeedsFacing;
    }

    public interface IRuntimeAbilityDefinition : IAbilityDefinition
    {
        AbilityCommitEvaluation EvaluateUse(AbilityUseContext context);

        bool TryCreateRuntimeSnapshot(
            AbilityActorSnapshot actor,
            out IRuntimeAbilitySnapshot snapshot);
    }

    public interface IRuntimeAbilitySnapshot
    {
        AbilityExecutionDefinition Execution { get; }
        string AnimatorTrigger { get; }

        bool CanCommit(AbilityUseContext context);

        CombatExecutionReport Release(
            AbilityUseContext context,
            AbilityExecutionSnapshot execution,
            double releasedAt);
    }
}
