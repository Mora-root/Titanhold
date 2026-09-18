using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Titanhold.Run
{
    public sealed class RunAbilityUnlockMilestone
    {
        private readonly ReadOnlyCollection<string> candidateAbilityIds;

        public RunAbilityUnlockMilestone(
            int unlockLevel,
            int targetSlotIndex,
            int optionCount,
            IReadOnlyList<string> candidateAbilityIds)
        {
            UnlockLevel = unlockLevel;
            TargetSlotIndex = targetSlotIndex;
            OptionCount = optionCount;

            int count = candidateAbilityIds?.Count ?? 0;
            string[] copy = new string[count];
            for (int i = 0; i < count; i++)
                copy[i] = candidateAbilityIds[i]?.Trim() ?? string.Empty;

            this.candidateAbilityIds = Array.AsReadOnly(copy);
        }

        public int UnlockLevel { get; }
        public int TargetSlotIndex { get; }
        public int OptionCount { get; }
        public IReadOnlyList<string> CandidateAbilityIds =>
            candidateAbilityIds;

        internal bool TryValidate(out string error)
        {
            error = string.Empty;
            if (UnlockLevel < 2)
            {
                error = "An ability milestone must unlock after run level one.";
                return false;
            }

            if (TargetSlotIndex < 1)
            {
                error =
                    "An ability milestone cannot replace the starting ability slot.";
                return false;
            }

            if (OptionCount <= 0)
            {
                error = "An ability milestone requires at least one option.";
                return false;
            }

            if (candidateAbilityIds.Count < OptionCount)
            {
                error =
                    $"Run level {UnlockLevel} has fewer candidates than options.";
                return false;
            }

            HashSet<string> uniqueIds = new(StringComparer.Ordinal);
            for (int i = 0; i < candidateAbilityIds.Count; i++)
            {
                string abilityId = candidateAbilityIds[i];
                if (abilityId.Length == 0)
                {
                    error =
                        $"Run level {UnlockLevel} has an empty ability id at index {i}.";
                    return false;
                }

                if (!uniqueIds.Add(abilityId))
                {
                    error =
                        $"Ability id '{abilityId}' occurs more than once at run level {UnlockLevel}.";
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class RunAbilityUnlockSchedule
    {
        private readonly ReadOnlyCollection<RunAbilityUnlockMilestone>
            milestones;

        private RunAbilityUnlockSchedule(
            string scheduleId,
            IReadOnlyList<RunAbilityUnlockMilestone> milestones)
        {
            ScheduleId = scheduleId;
            RunAbilityUnlockMilestone[] copy =
                new RunAbilityUnlockMilestone[milestones.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = milestones[i];

            Array.Sort(
                copy,
                (left, right) =>
                    left.UnlockLevel.CompareTo(right.UnlockLevel));
            this.milestones = Array.AsReadOnly(copy);
        }

        public string ScheduleId { get; }
        public IReadOnlyList<RunAbilityUnlockMilestone> Milestones =>
            milestones;

        public static bool TryCreate(
            string scheduleId,
            IReadOnlyList<RunAbilityUnlockMilestone> milestones,
            out RunAbilityUnlockSchedule schedule,
            out string error)
        {
            schedule = null;
            error = string.Empty;
            if (!HasStrictId(scheduleId))
            {
                error = "An ability unlock schedule requires a strict stable id.";
                return false;
            }

            if (milestones == null || milestones.Count == 0)
            {
                error =
                    $"Ability unlock schedule '{scheduleId}' requires at least one milestone.";
                return false;
            }

            HashSet<int> levels = new();
            HashSet<int> slots = new();
            for (int i = 0; i < milestones.Count; i++)
            {
                RunAbilityUnlockMilestone milestone = milestones[i];
                if (milestone == null)
                {
                    error =
                        $"Ability unlock schedule '{scheduleId}' has a null milestone at index {i}.";
                    return false;
                }

                if (!milestone.TryValidate(out string milestoneError))
                {
                    error =
                        $"Ability unlock schedule '{scheduleId}' is invalid: {milestoneError}";
                    return false;
                }

                if (!levels.Add(milestone.UnlockLevel))
                {
                    error =
                        $"Ability unlock schedule '{scheduleId}' has more than one milestone at run level {milestone.UnlockLevel}.";
                    return false;
                }

                if (!slots.Add(milestone.TargetSlotIndex))
                {
                    error =
                        $"Ability unlock schedule '{scheduleId}' targets slot {milestone.TargetSlotIndex} more than once.";
                    return false;
                }
            }

            schedule = new RunAbilityUnlockSchedule(scheduleId, milestones);
            return true;
        }

        private static bool HasStrictId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   string.Equals(value, value.Trim(), StringComparison.Ordinal);
        }
    }

    public sealed class RunAbilityUnlockParticipantPlan
    {
        public RunAbilityUnlockParticipantPlan(
            string playerId,
            int participantIndex,
            RunAbilityUnlockSchedule schedule)
        {
            PlayerId = playerId?.Trim() ?? string.Empty;
            ParticipantIndex = participantIndex;
            Schedule = schedule;
        }

        public string PlayerId { get; }
        public int ParticipantIndex { get; }
        public RunAbilityUnlockSchedule Schedule { get; }
        public bool IsValid =>
            PlayerId.Length > 0 &&
            ParticipantIndex >= 0 &&
            Schedule != null;
    }
}
