using System;
using System.Collections.Generic;
using Titanhold.Combat.Abilities;
using UnityEngine;

namespace Titanhold.Run
{
    [Serializable]
    public sealed class RunAbilityUnlockMilestoneDefinition
    {
        [SerializeField, Min(2)] private int unlockLevel = 2;
        [SerializeField, Min(1)] private int targetSlotIndex = 1;
        [SerializeField, Min(1)] private int optionCount = 3;
        [SerializeField] private ScriptableObject[] abilityDefinitions =
            Array.Empty<ScriptableObject>();

        public int UnlockLevel => unlockLevel;
        public int TargetSlotIndex => targetSlotIndex;
        public int OptionCount => optionCount;
        public IReadOnlyList<ScriptableObject> AbilityDefinitions =>
            abilityDefinitions ?? Array.Empty<ScriptableObject>();

        public bool TryCreateMilestone(
            out RunAbilityUnlockMilestone milestone,
            out string error)
        {
            milestone = null;
            error = string.Empty;
            ScriptableObject[] definitions =
                abilityDefinitions ?? Array.Empty<ScriptableObject>();
            string[] abilityIds = new string[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                ScriptableObject asset = definitions[i];
                if (asset == null)
                {
                    error =
                        $"Run level {unlockLevel} has no ability asset at index {i}.";
                    return false;
                }

                if (asset is not IAbilityDefinition definition)
                {
                    error =
                        $"Asset '{asset.name}' at run level {unlockLevel} does not implement IAbilityDefinition.";
                    return false;
                }

                abilityIds[i] = definition.AbilityId;
            }

            milestone = new RunAbilityUnlockMilestone(
                unlockLevel,
                targetSlotIndex,
                optionCount,
                abilityIds);
            if (!milestone.TryValidate(out error))
            {
                milestone = null;
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int configuredUnlockLevel,
            int configuredTargetSlotIndex,
            int configuredOptionCount,
            ScriptableObject[] configuredAbilityDefinitions)
        {
            unlockLevel = configuredUnlockLevel;
            targetSlotIndex = configuredTargetSlotIndex;
            optionCount = configuredOptionCount;
            abilityDefinitions = configuredAbilityDefinitions ??
                Array.Empty<ScriptableObject>();
        }
#endif
    }

    [CreateAssetMenu(
        fileName = "AbilityUnlockSchedule",
        menuName = "Titanhold/Run/Ability Unlock Schedule")]
    public sealed class RunAbilityUnlockScheduleDefinition : ScriptableObject
    {
        [SerializeField] private string scheduleId;
        [SerializeField] private string characterArchetypeId;
        [SerializeField]
        private RunAbilityUnlockMilestoneDefinition[] milestones =
            Array.Empty<RunAbilityUnlockMilestoneDefinition>();

        public string ScheduleId => scheduleId ?? string.Empty;
        public string CharacterArchetypeId =>
            characterArchetypeId ?? string.Empty;
        public IReadOnlyList<RunAbilityUnlockMilestoneDefinition> Milestones =>
            milestones ?? Array.Empty<RunAbilityUnlockMilestoneDefinition>();

        public bool TryCreateSchedule(
            out RunAbilityUnlockSchedule schedule,
            out string error)
        {
            schedule = null;
            error = string.Empty;
            RunAbilityUnlockMilestoneDefinition[] source =
                milestones ?? Array.Empty<RunAbilityUnlockMilestoneDefinition>();
            RunAbilityUnlockMilestone[] runtimeMilestones =
                new RunAbilityUnlockMilestone[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                RunAbilityUnlockMilestoneDefinition definition = source[i];
                if (definition == null)
                {
                    error =
                        $"Ability unlock schedule '{name}' has a null milestone at index {i}.";
                    return false;
                }

                if (!definition.TryCreateMilestone(
                        out runtimeMilestones[i],
                        out string milestoneError))
                {
                    error =
                        $"Ability unlock schedule '{name}' is invalid: {milestoneError}";
                    return false;
                }
            }

            return RunAbilityUnlockSchedule.TryCreate(
                ScheduleId,
                CharacterArchetypeId,
                runtimeMilestones,
                out schedule,
                out error);
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string configuredScheduleId,
            string configuredCharacterArchetypeId,
            RunAbilityUnlockMilestoneDefinition[] configuredMilestones)
        {
            scheduleId = configuredScheduleId;
            characterArchetypeId = configuredCharacterArchetypeId;
            milestones = configuredMilestones ??
                Array.Empty<RunAbilityUnlockMilestoneDefinition>();
        }
#endif
    }
}
