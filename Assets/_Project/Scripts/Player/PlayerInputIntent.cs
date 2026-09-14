using UnityEngine;

public readonly struct PlayerInputIntent
{
    public static PlayerInputIntent Empty => new(
        Vector3.zero,
        false,
        false,
        false,
        false,
        false,
        -1);

    public Vector3 TargetPosition { get; }
    public bool HasMoveTarget { get; }
    public bool LeftClicked { get; }
    public bool RightClicked { get; }
    public bool IsDragging { get; }
    public bool IsHolding { get; }
    public int SkillSlotIndex { get; }
    public bool HasSkillCommand => SkillSlotIndex >= 0;

    public PlayerInputIntent(
        Vector3 targetPosition,
        bool hasMoveTarget,
        bool leftClicked,
        bool rightClicked,
        bool isDragging,
        bool isHolding,
        int skillSlotIndex)
    {
        TargetPosition = targetPosition;
        HasMoveTarget = hasMoveTarget;
        LeftClicked = leftClicked;
        RightClicked = rightClicked;
        IsDragging = isDragging;
        IsHolding = isHolding;
        SkillSlotIndex = skillSlotIndex;
    }
}
