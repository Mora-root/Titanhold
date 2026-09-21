using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public sealed class RunUpgradeUnlockScheduleRegistry :
        IRunUpgradeUnlockScheduleResolver
    {
        private readonly Dictionary<string, RunUpgradeUnlockSchedule>
            schedules;

        private RunUpgradeUnlockScheduleRegistry(
            Dictionary<string, RunUpgradeUnlockSchedule> schedules)
        {
            this.schedules = schedules;
        }

        public int Count => schedules.Count;

        public static bool TryCreate(
            IReadOnlyList<RunUpgradeUnlockSchedule> source,
            IRunUpgradeDefinitionResolver upgrades,
            out RunUpgradeUnlockScheduleRegistry registry,
            out string error)
        {
            registry = null;
            error = string.Empty;
            if (source == null || source.Count == 0)
            {
                error = "At least one upgrade unlock schedule is required.";
                return false;
            }

            if (upgrades == null)
            {
                error = "An upgrade definition resolver is required.";
                return false;
            }

            Dictionary<string, RunUpgradeUnlockSchedule> index =
                new(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                RunUpgradeUnlockSchedule schedule = source[i];
                if (schedule == null)
                {
                    error = $"Upgrade unlock schedule {i} is missing.";
                    return false;
                }

                for (int milestoneIndex = 0;
                     milestoneIndex < schedule.Milestones.Count;
                     milestoneIndex++)
                {
                    RunUpgradeUnlockMilestone milestone =
                        schedule.Milestones[milestoneIndex];
                    for (int candidateIndex = 0;
                         candidateIndex <
                            milestone.CandidateUpgradeIds.Count;
                         candidateIndex++)
                    {
                        string upgradeId =
                            milestone.CandidateUpgradeIds[candidateIndex];
                        if (!upgrades.TryResolve(upgradeId, out _))
                        {
                            error =
                                $"Upgrade schedule '{schedule.ScheduleId}' references unresolved upgrade '{upgradeId}'.";
                            return false;
                        }
                    }
                }

                if (!index.TryAdd(
                        schedule.CharacterArchetypeId,
                        schedule))
                {
                    error =
                        $"Character archetype '{schedule.CharacterArchetypeId}' has more than one upgrade schedule.";
                    return false;
                }
            }

            registry = new RunUpgradeUnlockScheduleRegistry(index);
            return true;
        }

        public bool TryResolve(
            string characterArchetypeId,
            out RunUpgradeUnlockSchedule schedule)
        {
            schedule = null;
            string normalizedId = characterArchetypeId?.Trim() ??
                string.Empty;
            return normalizedId.Length > 0 &&
                   schedules.TryGetValue(normalizedId, out schedule);
        }
    }
}
