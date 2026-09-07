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
        public bool HasUsableTarget
        {
            get
            {
                if (SelectedTarget == null ||
                    (SelectedTarget is Object unityObject && unityObject == null))
                {
                    return false;
                }

                return SelectedTarget.IsTargetable &&
                       SelectedTarget.AimPoint != null;
            }
        }
    }

    public interface IRuntimeAbilityDefinition : IAbilityDefinition
    {
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
            AbilityExecutionSnapshot execution);
    }
}
