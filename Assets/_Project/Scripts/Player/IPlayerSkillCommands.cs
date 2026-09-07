using System;
using Titanhold.Combat;
using Titanhold.Combat.Abilities;
using UnityEngine;

// The brain and reward adapters use the same, explicitly selected executor.
public interface IPlayerSkillCommands
{
    bool IsUsingSkill { get; }
    CombatActorReference ActorReference { get; }
    event Action<CombatExecutionReport> ExecutionResolved;
    bool TryUseSkillSlot(int slotIndex);
    bool TryUseSkillSlot(int slotIndex, ITargetable selectedTarget);
    void CancelCurrentSkill();
}

public interface IPlayerAbilitySlotBinding
{
    bool HasAbilitySlotBinding { get; }
    bool TryBindAbilitySlots(
        IAbilitySlotSource slots,
        IAbilityDefinitionResolver definitions);
    bool TryClearAbilitySlotBinding();
}

public static class PlayerSkillCommands
{
    public static IPlayerSkillCommands Resolve(GameObject participant)
    {
        if (participant == null)
            return null;

        PlayerBrain brain = participant.GetComponent<PlayerBrain>();
        return brain != null ? brain.Skills : participant.GetComponent<PlayerSkillExecutor>();
    }
}
