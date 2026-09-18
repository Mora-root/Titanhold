using System;
using System.Collections.Generic;
using Titanhold.Combat.Abilities;

namespace Titanhold.Run
{
    public sealed class RunAbilityUnlockScheduleRegistry :
        IRunAbilityUnlockScheduleResolver
    {
        private readonly Dictionary<string, RunAbilityUnlockSchedule>
            schedulesByArchetype;

        private RunAbilityUnlockScheduleRegistry(
            Dictionary<string, RunAbilityUnlockSchedule>
                schedulesByArchetype)
        {
            this.schedulesByArchetype = schedulesByArchetype;
        }

        public int Count => schedulesByArchetype.Count;

        public static bool TryCreate(
            IReadOnlyList<RunAbilityUnlockSchedule> source,
            IAbilityDefinitionResolver abilityDefinitions,
            int abilitySlotCount,
            out RunAbilityUnlockScheduleRegistry registry,
            out string error)
        {
            registry = null;
            error = string.Empty;
            if (source == null || source.Count == 0)
            {
                error = "At least one ability unlock schedule is required.";
                return false;
            }

            if (abilityDefinitions == null)
            {
                error = "An ability definition resolver is required.";
                return false;
            }

            if (abilitySlotCount < 2)
            {
                error =
                    "Ability unlock schedules require a starting slot and at least one unlock slot.";
                return false;
            }

            Dictionary<string, RunAbilityUnlockSchedule> byArchetype =
                new(StringComparer.Ordinal);
            HashSet<string> scheduleIds = new(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                RunAbilityUnlockSchedule schedule = source[i];
                if (!TryValidateSchedule(
                        schedule,
                        i,
                        abilityDefinitions,
                        abilitySlotCount,
                        out error))
                {
                    return false;
                }

                if (!scheduleIds.Add(schedule.ScheduleId))
                {
                    error =
                        $"Ability unlock schedule id '{schedule.ScheduleId}' occurs more than once.";
                    return false;
                }

                if (!byArchetype.TryAdd(
                        schedule.CharacterArchetypeId,
                        schedule))
                {
                    error =
                        $"Character archetype '{schedule.CharacterArchetypeId}' has more than one ability unlock schedule.";
                    return false;
                }
            }

            registry = new RunAbilityUnlockScheduleRegistry(byArchetype);
            return true;
        }

        public bool TryResolve(
            string characterArchetypeId,
            out RunAbilityUnlockSchedule schedule)
        {
            schedule = null;
            string normalizedId =
                characterArchetypeId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   schedulesByArchetype.TryGetValue(
                       normalizedId,
                       out schedule);
        }

        private static bool TryValidateSchedule(
            RunAbilityUnlockSchedule schedule,
            int index,
            IAbilityDefinitionResolver abilityDefinitions,
            int abilitySlotCount,
            out string error)
        {
            error = string.Empty;
            if (schedule == null)
            {
                error = $"Ability unlock schedule {index} is missing.";
                return false;
            }

            for (int milestoneIndex = 0;
                 milestoneIndex < schedule.Milestones.Count;
                 milestoneIndex++)
            {
                RunAbilityUnlockMilestone milestone =
                    schedule.Milestones[milestoneIndex];
                if (milestone.TargetSlotIndex >= abilitySlotCount)
                {
                    error =
                        $"Ability unlock schedule '{schedule.ScheduleId}' targets slot {milestone.TargetSlotIndex}, but the loadout has {abilitySlotCount} slots.";
                    return false;
                }

                for (int abilityIndex = 0;
                     abilityIndex < milestone.CandidateAbilityIds.Count;
                     abilityIndex++)
                {
                    string abilityId =
                        milestone.CandidateAbilityIds[abilityIndex];
                    if (!abilityDefinitions.TryResolve(
                            abilityId,
                            out IAbilityDefinition definition) ||
                        definition == null ||
                        !string.Equals(
                            definition.AbilityId,
                            abilityId,
                            StringComparison.Ordinal))
                    {
                        error =
                            $"Ability id '{abilityId}' in schedule '{schedule.ScheduleId}' is not available in the ability catalog.";
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
