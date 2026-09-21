using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Run
{
    [CreateAssetMenu(
        fileName = "RunUpgradeUnlockScheduleCatalog",
        menuName = "Titanhold/Run/Upgrade Unlock Schedule Catalog")]
    public sealed class RunUpgradeUnlockScheduleCatalog :
        ScriptableObject,
        IRunUpgradeUnlockScheduleResolver
    {
        [SerializeField]
        private RunUpgradeDefinitionCatalog upgradeDefinitions;
        [SerializeField]
        private RunUpgradeUnlockScheduleDefinition[] definitions =
            Array.Empty<RunUpgradeUnlockScheduleDefinition>();

        private RunUpgradeUnlockScheduleRegistry registry;
        private bool indexBuilt;
        private string validationError;

        public RunUpgradeDefinitionCatalog UpgradeDefinitions =>
            upgradeDefinitions;
        public IReadOnlyList<RunUpgradeUnlockScheduleDefinition> Definitions =>
            definitions ??
            Array.Empty<RunUpgradeUnlockScheduleDefinition>();

        public bool IsValid
        {
            get
            {
                EnsureIndex();
                return string.IsNullOrEmpty(validationError);
            }
        }

        public string ValidationError
        {
            get
            {
                EnsureIndex();
                return validationError;
            }
        }

        public bool TryResolve(
            string characterArchetypeId,
            out RunUpgradeUnlockSchedule schedule)
        {
            EnsureIndex();
            if (registry == null || !string.IsNullOrEmpty(validationError))
            {
                schedule = null;
                return false;
            }

            return registry.TryResolve(characterArchetypeId, out schedule);
        }

        public void RebuildIndex()
        {
            indexBuilt = false;
            EnsureIndex();
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunUpgradeDefinitionCatalog configuredUpgrades,
            RunUpgradeUnlockScheduleDefinition[] configuredDefinitions)
        {
            upgradeDefinitions = configuredUpgrades;
            definitions = configuredDefinitions ??
                Array.Empty<RunUpgradeUnlockScheduleDefinition>();
            RebuildIndex();
        }
#endif

        private void OnEnable()
        {
            indexBuilt = false;
        }

        private void OnValidate()
        {
            indexBuilt = false;
            EnsureIndex();
            if (!string.IsNullOrEmpty(validationError))
                Debug.LogWarning(validationError, this);
        }

        private void EnsureIndex()
        {
            if (indexBuilt)
                return;

            indexBuilt = true;
            registry = null;
            validationError = string.Empty;
            if (upgradeDefinitions == null || !upgradeDefinitions.IsValid)
            {
                validationError = upgradeDefinitions == null
                    ? $"Upgrade schedule catalog '{name}' has no upgrade catalog."
                    : $"Upgrade schedule catalog '{name}' references invalid upgrades: {upgradeDefinitions.ValidationError}";
                return;
            }

            RunUpgradeUnlockScheduleDefinition[] source =
                definitions ??
                Array.Empty<RunUpgradeUnlockScheduleDefinition>();
            RunUpgradeUnlockSchedule[] schedules =
                new RunUpgradeUnlockSchedule[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null)
                {
                    validationError =
                        $"Upgrade schedule catalog '{name}' has a null entry at index {i}.";
                    return;
                }

                if (!source[i].TryCreateSchedule(
                        out schedules[i],
                        out string scheduleError))
                {
                    validationError = scheduleError;
                    return;
                }
            }

            if (!RunUpgradeUnlockScheduleRegistry.TryCreate(
                    schedules,
                    upgradeDefinitions,
                    out registry,
                    out validationError))
            {
                registry = null;
                validationError =
                    $"Upgrade schedule catalog '{name}' is invalid: " +
                    validationError;
            }
        }
    }
}
