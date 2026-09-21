using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Titanhold.Run
{
    public sealed class RunUpgradeUnlockMilestone
    {
        private readonly ReadOnlyCollection<string> candidateUpgradeIds;

        public RunUpgradeUnlockMilestone(
            int unlockLevel,
            int optionCount,
            IReadOnlyList<string> candidateUpgradeIds)
        {
            UnlockLevel = unlockLevel;
            OptionCount = optionCount;

            int count = candidateUpgradeIds?.Count ?? 0;
            string[] copy = new string[count];
            for (int i = 0; i < count; i++)
                copy[i] = candidateUpgradeIds[i]?.Trim() ?? string.Empty;

            this.candidateUpgradeIds = Array.AsReadOnly(copy);
        }

        public int UnlockLevel { get; }
        public int OptionCount { get; }
        public IReadOnlyList<string> CandidateUpgradeIds =>
            candidateUpgradeIds;

        internal bool TryValidate(out string error)
        {
            error = string.Empty;
            if (UnlockLevel < 2)
            {
                error = "An upgrade milestone must unlock after run level one.";
                return false;
            }

            if (OptionCount <= 0)
            {
                error = "An upgrade milestone requires at least one option.";
                return false;
            }

            if (candidateUpgradeIds.Count < OptionCount)
            {
                error =
                    $"Run level {UnlockLevel} has fewer upgrade candidates than options.";
                return false;
            }

            HashSet<string> uniqueIds = new(StringComparer.Ordinal);
            for (int i = 0; i < candidateUpgradeIds.Count; i++)
            {
                string upgradeId = candidateUpgradeIds[i];
                if (upgradeId.Length == 0 || !uniqueIds.Add(upgradeId))
                {
                    error =
                        $"Run level {UnlockLevel} has an invalid or duplicate upgrade id at index {i}.";
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class RunUpgradeUnlockSchedule
    {
        private readonly ReadOnlyCollection<RunUpgradeUnlockMilestone>
            milestones;

        private RunUpgradeUnlockSchedule(
            string scheduleId,
            string characterArchetypeId,
            IReadOnlyList<RunUpgradeUnlockMilestone> milestones)
        {
            ScheduleId = scheduleId;
            CharacterArchetypeId = characterArchetypeId;
            RunUpgradeUnlockMilestone[] copy =
                new RunUpgradeUnlockMilestone[milestones.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = milestones[i];

            Array.Sort(
                copy,
                (left, right) =>
                    left.UnlockLevel.CompareTo(right.UnlockLevel));
            this.milestones = Array.AsReadOnly(copy);
        }

        public string ScheduleId { get; }
        public string CharacterArchetypeId { get; }
        public IReadOnlyList<RunUpgradeUnlockMilestone> Milestones =>
            milestones;

        public static bool TryCreate(
            string scheduleId,
            string characterArchetypeId,
            IReadOnlyList<RunUpgradeUnlockMilestone> milestones,
            out RunUpgradeUnlockSchedule schedule,
            out string error)
        {
            schedule = null;
            error = string.Empty;
            if (!HasStrictId(scheduleId) ||
                !HasStrictId(characterArchetypeId))
            {
                error =
                    "An upgrade unlock schedule requires strict schedule and archetype ids.";
                return false;
            }

            if (milestones == null || milestones.Count == 0)
            {
                error =
                    $"Upgrade unlock schedule '{scheduleId}' requires at least one milestone.";
                return false;
            }

            HashSet<int> levels = new();
            for (int i = 0; i < milestones.Count; i++)
            {
                RunUpgradeUnlockMilestone milestone = milestones[i];
                if (milestone == null)
                {
                    error =
                        $"Upgrade unlock schedule '{scheduleId}' has a null milestone at index {i}.";
                    return false;
                }

                if (!milestone.TryValidate(out string milestoneError))
                {
                    error =
                        $"Upgrade unlock schedule '{scheduleId}' is invalid: {milestoneError}";
                    return false;
                }

                if (!levels.Add(milestone.UnlockLevel))
                {
                    error =
                        $"Upgrade unlock schedule '{scheduleId}' has more than one milestone at run level {milestone.UnlockLevel}.";
                    return false;
                }
            }

            schedule = new RunUpgradeUnlockSchedule(
                scheduleId,
                characterArchetypeId,
                milestones);
            return true;
        }

        private static bool HasStrictId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   string.Equals(
                       value,
                       value.Trim(),
                       StringComparison.Ordinal);
        }
    }

    public sealed class RunUpgradeUnlockParticipantPlan
    {
        public RunUpgradeUnlockParticipantPlan(
            string playerId,
            int participantIndex,
            RunUpgradeUnlockSchedule schedule)
        {
            PlayerId = playerId?.Trim() ?? string.Empty;
            ParticipantIndex = participantIndex;
            Schedule = schedule;
        }

        public string PlayerId { get; }
        public int ParticipantIndex { get; }
        public RunUpgradeUnlockSchedule Schedule { get; }
        public bool IsValid =>
            PlayerId.Length > 0 &&
            ParticipantIndex >= 0 &&
            Schedule != null;
    }

    public interface IRunUpgradeUnlockScheduleResolver
    {
        bool TryResolve(
            string characterArchetypeId,
            out RunUpgradeUnlockSchedule schedule);
    }
}
