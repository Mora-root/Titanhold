using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Run
{
    [CreateAssetMenu(
        fileName = "RunUpgradeUnlockSchedule",
        menuName = "Titanhold/Run/Upgrade Unlock Schedule")]
    public sealed class RunUpgradeUnlockScheduleDefinition : ScriptableObject
    {
        [SerializeField] private string scheduleId;
        [SerializeField] private string characterArchetypeId;
        [SerializeField, Min(1)] private int optionCount = 3;
        [SerializeField] private int[] unlockLevels = Array.Empty<int>();
        [SerializeField] private ScriptableObject[] upgradeDefinitions =
            Array.Empty<ScriptableObject>();

        public string ScheduleId => scheduleId ?? string.Empty;
        public string CharacterArchetypeId =>
            characterArchetypeId ?? string.Empty;
        public int OptionCount => optionCount;
        public IReadOnlyList<int> UnlockLevels =>
            unlockLevels ?? Array.Empty<int>();
        public IReadOnlyList<ScriptableObject> UpgradeDefinitions =>
            upgradeDefinitions ?? Array.Empty<ScriptableObject>();

        public bool TryCreateSchedule(
            out RunUpgradeUnlockSchedule schedule,
            out string error)
        {
            schedule = null;
            error = string.Empty;
            ScriptableObject[] definitions =
                upgradeDefinitions ?? Array.Empty<ScriptableObject>();
            string[] upgradeIds = new string[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] is not IRunUpgradeDefinition definition)
                {
                    error = definitions[i] == null
                        ? $"Upgrade schedule '{name}' has a null upgrade at index {i}."
                        : $"Asset '{definitions[i].name}' does not implement IRunUpgradeDefinition.";
                    return false;
                }

                upgradeIds[i] = definition.UpgradeId;
            }

            int[] levels = unlockLevels ?? Array.Empty<int>();
            RunUpgradeUnlockMilestone[] milestones =
                new RunUpgradeUnlockMilestone[levels.Length];
            for (int i = 0; i < levels.Length; i++)
            {
                milestones[i] = new RunUpgradeUnlockMilestone(
                    levels[i],
                    optionCount,
                    upgradeIds);
            }

            return RunUpgradeUnlockSchedule.TryCreate(
                ScheduleId,
                CharacterArchetypeId,
                milestones,
                out schedule,
                out error);
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string configuredScheduleId,
            string configuredCharacterArchetypeId,
            int configuredOptionCount,
            int[] configuredUnlockLevels,
            ScriptableObject[] configuredUpgradeDefinitions)
        {
            scheduleId = configuredScheduleId;
            characterArchetypeId = configuredCharacterArchetypeId;
            optionCount = configuredOptionCount;
            unlockLevels = configuredUnlockLevels ?? Array.Empty<int>();
            upgradeDefinitions = configuredUpgradeDefinitions ??
                Array.Empty<ScriptableObject>();
        }
#endif
    }
}
