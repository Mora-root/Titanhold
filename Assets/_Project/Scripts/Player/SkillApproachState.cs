using Titanhold.Combat.Abilities;
using UnityEngine;

public sealed class SkillApproachState : IState
{
    private readonly PlayerBrain brain;
    private PlayerSkillCommand command;

    public SkillApproachState(PlayerBrain brain)
    {
        this.brain = brain;
    }

    public bool HasCommand => command.IsValid;

    public bool TrySetCommand(PlayerSkillCommand nextCommand)
    {
        if (!nextCommand.IsValid)
            return false;

        command = nextCommand;
        return true;
    }

    public void Enter() { }

    public void Tick()
    {
        if (!command.IsValid || brain.Skills == null)
        {
            brain.ChangeToIdle();
            return;
        }

        PlayerSkillUseEvaluation evaluation =
            brain.Skills.EvaluateSkillSlot(
                command.SlotIndex,
                command.SelectedTarget);
        if (evaluation.IsReady)
        {
            PlayerSkillCommand readyCommand = command;
            if (!brain.TryCommitSkillCommand(readyCommand))
                brain.ChangeToIdle();
            return;
        }

        if (!evaluation.RequiresReposition ||
            command.SelectedTarget == null ||
            command.SelectedTarget.AimPoint == null)
        {
            brain.ChangeToIdle();
            return;
        }

        Vector3 targetPosition =
            command.SelectedTarget.AimPoint.position;
        if (evaluation.CommitStatus == AbilityCommitStatus.NeedsFacing)
        {
            brain.Stop();
            brain.Movement.RotateTowards(targetPosition);
            return;
        }

        brain.MoveTo(targetPosition);
    }

    public void Exit()
    {
        command = default;
        brain.Stop();
    }
}
