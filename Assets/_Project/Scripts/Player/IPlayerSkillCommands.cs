using System;
using Titanhold.Combat;
using Titanhold.Combat.Abilities;
using UnityEngine;

// The brain and reward adapters use the same, explicitly selected executor.
public interface IPlayerSkillCommands
{
    bool IsUsingSkill { get; }
    CombatActorReference ActorReference { get; }
    ITargetable CurrentTarget { get; }
    event Action<CombatExecutionReport> ExecutionResolved;
    PlayerSkillUseEvaluation EvaluateSkillSlot(
        int slotIndex,
        ITargetable selectedTarget);
    bool TryUseSkillSlot(int slotIndex);
    bool TryUseSkillSlot(int slotIndex, ITargetable selectedTarget);
    void CancelCurrentSkill();
}

public enum PlayerSkillUseStatus
{
    Invalid,
    Ready,
    RequiresReposition
}

public readonly struct PlayerSkillUseEvaluation
{
    public PlayerSkillUseEvaluation(
        PlayerSkillUseStatus status,
        AbilityCommitStatus commitStatus)
    {
        Status = status;
        CommitStatus = commitStatus;
    }

    public PlayerSkillUseStatus Status { get; }
    public AbilityCommitStatus CommitStatus { get; }
    public bool IsReady => Status == PlayerSkillUseStatus.Ready;
    public bool RequiresReposition =>
        Status == PlayerSkillUseStatus.RequiresReposition;

    public static PlayerSkillUseEvaluation Invalid =>
        new(PlayerSkillUseStatus.Invalid, AbilityCommitStatus.InvalidDefinition);
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
