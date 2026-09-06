using System;
using System.Collections.Generic;
using Titanhold.Combat.Abilities;
using UnityEngine;

namespace Titanhold.Run
{
    [CreateAssetMenu(
        fileName = "StartingAbilityPoolCatalog",
        menuName = "Titanhold/Run/Starting Ability Pool Catalog")]
    public sealed class RunStartingAbilityPoolCatalog :
        ScriptableObject,
        IRunStartingAbilityPoolResolver
    {
        [SerializeField] private AbilityDefinitionCatalog abilityCatalog;
        [SerializeField] private RunStartingAbilityPoolDefinition[] definitions =
            Array.Empty<RunStartingAbilityPoolDefinition>();

        private RunStartingAbilityPoolRegistry registry;
        private bool indexBuilt;
        private string validationError;

        public AbilityDefinitionCatalog AbilityCatalog => abilityCatalog;
        public IReadOnlyList<RunStartingAbilityPoolDefinition> Definitions =>
            definitions ?? Array.Empty<RunStartingAbilityPoolDefinition>();

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
            out RunStartingAbilityPool pool)
        {
            EnsureIndex();
            if (!string.IsNullOrEmpty(validationError) || registry == null)
            {
                pool = null;
                return false;
            }

            return registry.TryResolve(characterArchetypeId, out pool);
        }

        public void RebuildIndex()
        {
            indexBuilt = false;
            EnsureIndex();
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            AbilityDefinitionCatalog configuredAbilityCatalog,
            RunStartingAbilityPoolDefinition[] configuredDefinitions)
        {
            abilityCatalog = configuredAbilityCatalog;
            definitions = configuredDefinitions ??
                Array.Empty<RunStartingAbilityPoolDefinition>();
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
                    $"Starting ability pool catalog '{name}' has no ability catalog.");
                return;
            }

            if (!abilityCatalog.IsValid)
            {
                Invalidate(
                    $"Starting ability pool catalog '{name}' references an invalid " +
                    $"ability catalog: {abilityCatalog.ValidationError}");
                return;
            }

            RunStartingAbilityPoolDefinition[] source =
                definitions ?? Array.Empty<RunStartingAbilityPoolDefinition>();
            RunStartingAbilityPool[] pools =
                new RunStartingAbilityPool[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                RunStartingAbilityPoolDefinition definition = source[i];
                if (definition == null)
                {
                    Invalidate(
                        $"Starting ability pool catalog '{name}' contains a null entry at index {i}.");
                    return;
                }

                if (!definition.TryCreatePool(
                        out pools[i],
                        out string definitionError))
                {
                    Invalidate(definitionError);
                    return;
                }
            }

            if (!RunStartingAbilityPoolRegistry.TryCreate(
                    pools,
                    abilityCatalog,
                    out registry,
                    out validationError))
            {
                registry = null;
                validationError =
                    $"Starting ability pool catalog '{name}' is invalid: " +
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
