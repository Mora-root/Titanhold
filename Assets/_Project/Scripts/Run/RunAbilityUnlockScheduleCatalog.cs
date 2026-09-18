using System;
using System.Collections.Generic;
using Titanhold.Combat.Abilities;
using UnityEngine;

namespace Titanhold.Run
{
    [CreateAssetMenu(
        fileName = "AbilityUnlockScheduleCatalog",
        menuName = "Titanhold/Run/Ability Unlock Schedule Catalog")]
    public sealed class RunAbilityUnlockScheduleCatalog :
        ScriptableObject,
        IRunAbilityUnlockScheduleResolver
    {
        [SerializeField] private AbilityDefinitionCatalog abilityCatalog;
        [SerializeField]
        private RunAbilityUnlockScheduleDefinition[] definitions =
            Array.Empty<RunAbilityUnlockScheduleDefinition>();

        private RunAbilityUnlockScheduleRegistry registry;
        private bool indexBuilt;
        private string validationError;

        public AbilityDefinitionCatalog AbilityCatalog => abilityCatalog;
        public IReadOnlyList<RunAbilityUnlockScheduleDefinition> Definitions =>
            definitions ?? Array.Empty<RunAbilityUnlockScheduleDefinition>();

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
            out RunAbilityUnlockSchedule schedule)
        {
            EnsureIndex();
            if (!string.IsNullOrEmpty(validationError) || registry == null)
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
            AbilityDefinitionCatalog configuredAbilityCatalog,
            RunAbilityUnlockScheduleDefinition[] configuredDefinitions)
        {
            abilityCatalog = configuredAbilityCatalog;
            definitions = configuredDefinitions ??
                Array.Empty<RunAbilityUnlockScheduleDefinition>();
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
            if (abilityCatalog == null)
            {
                Invalidate(
                    $"Ability unlock schedule catalog '{name}' has no ability catalog.");
                return;
            }

            if (!abilityCatalog.IsValid)
            {
                Invalidate(
                    $"Ability unlock schedule catalog '{name}' references an invalid ability catalog: {abilityCatalog.ValidationError}");
                return;
            }

            RunAbilityUnlockScheduleDefinition[] source =
                definitions ?? Array.Empty<RunAbilityUnlockScheduleDefinition>();
            RunAbilityUnlockSchedule[] schedules =
                new RunAbilityUnlockSchedule[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                RunAbilityUnlockScheduleDefinition definition = source[i];
                if (definition == null)
                {
                    Invalidate(
                        $"Ability unlock schedule catalog '{name}' contains a null entry at index {i}.");
                    return;
                }

                if (!definition.TryCreateSchedule(
                        out schedules[i],
                        out string definitionError))
                {
                    Invalidate(definitionError);
                    return;
                }
            }

            if (!RunAbilityUnlockScheduleRegistry.TryCreate(
                    schedules,
                    abilityCatalog,
                    RunAbilityLoadoutService.DefaultAbilitySlotCount,
                    out registry,
                    out validationError))
            {
                registry = null;
                validationError =
                    $"Ability unlock schedule catalog '{name}' is invalid: " +
                    validationError;
            }
        }

        private void Invalidate(string error)
        {
            validationError = error;
            registry = null;
        }
    }
}
