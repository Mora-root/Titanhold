public readonly struct PlayerSkillCommand
{
    public PlayerSkillCommand(int slotIndex)
    {
        SlotIndex = slotIndex;
        IsValid = slotIndex >= 0;
    }

    public int SlotIndex { get; }
    public bool IsValid { get; }
}

public sealed class PlayerSkillCommandBuffer
{
    private PlayerSkillCommand pendingCommand;
    private bool hasPendingCommand;

    public bool HasPendingCommand => hasPendingCommand;
    public PlayerSkillCommand PendingCommand => pendingCommand;

    public bool TryBuffer(PlayerSkillCommand command)
    {
        if (!command.IsValid)
            return false;

        pendingCommand = command;
        hasPendingCommand = true;
        return true;
    }

    public bool TryTake(out PlayerSkillCommand command)
    {
        if (!hasPendingCommand)
        {
            command = default;
            return false;
        }

        command = pendingCommand;
        Clear();
        return true;
    }

    public void Clear()
    {
        pendingCommand = default;
        hasPendingCommand = false;
    }
}
