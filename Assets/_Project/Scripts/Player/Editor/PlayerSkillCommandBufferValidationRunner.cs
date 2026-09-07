using System;
using UnityEditor;
using UnityEngine;

public static class PlayerSkillCommandBufferValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate Player Skill Command Buffer")]
    public static void Validate()
    {
        try
        {
            PlayerSkillCommandBuffer buffer = new();
            Assert(!buffer.HasPendingCommand,
                "New skill command buffer is not empty.");
            Assert(!default(PlayerSkillCommand).IsValid,
                "Default skill command is valid.");
            Assert(!buffer.TryBuffer(new PlayerSkillCommand(-1)),
                "Invalid skill command was buffered.");

            Assert(buffer.TryBuffer(new PlayerSkillCommand(0)),
                "First skill command was rejected.");
            ITargetable selectedTarget = new ValidationTarget();
            Assert(buffer.TryBuffer(new PlayerSkillCommand(2, selectedTarget)),
                "Replacement skill command was rejected.");
            Assert(buffer.HasPendingCommand &&
                   buffer.PendingCommand.SlotIndex == 2 &&
                   ReferenceEquals(
                       buffer.PendingCommand.SelectedTarget,
                       selectedTarget),
                "Last skill command or its selected target was not preserved.");

            Assert(buffer.TryTake(out PlayerSkillCommand command) &&
                   command.SlotIndex == 2 &&
                   ReferenceEquals(command.SelectedTarget, selectedTarget),
                "Buffered skill command or its selected target was not consumed.");
            Assert(!buffer.HasPendingCommand &&
                   !buffer.TryTake(out _),
                "Consumed skill command remained in the buffer.");

            buffer.TryBuffer(new PlayerSkillCommand(1));
            buffer.Clear();
            Assert(!buffer.HasPendingCommand,
                "Skill command buffer did not clear.");

            Debug.Log("Player skill command buffer validation passed.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Player skill command buffer validation failed: {exception}");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class ValidationTarget : ITargetable
    {
        public Transform AimPoint => null;
        public bool IsTargetable => true;
    }
}
