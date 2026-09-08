using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Run
{
    [CreateAssetMenu(
        fileName = "CombatResourceLoadoutCatalog",
        menuName = "Titanhold/Run/Combat Resource Loadout Catalog")]
    public sealed class RunCombatResourceLoadoutCatalog :
        ScriptableObject,
        IRunCombatResourceLoadoutResolver
    {
        [SerializeField] private RunCombatResourceLoadoutDefinition[] definitions =
            Array.Empty<RunCombatResourceLoadoutDefinition>();

        private RunCombatResourceLoadoutRegistry registry;
        private bool indexBuilt;
        private string validationError;

        public IReadOnlyList<RunCombatResourceLoadoutDefinition> Definitions =>
            definitions ?? Array.Empty<RunCombatResourceLoadoutDefinition>();

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
            out RunCombatResourceLoadout loadout)
        {
            EnsureIndex();
            if (!string.IsNullOrEmpty(validationError) || registry == null)
            {
                loadout = null;
                return false;
            }

            return registry.TryResolve(characterArchetypeId, out loadout);
        }

        public void RebuildIndex()
        {
            indexBuilt = false;
            EnsureIndex();
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunCombatResourceLoadoutDefinition[] configuredDefinitions)
        {
            definitions = configuredDefinitions ??
                Array.Empty<RunCombatResourceLoadoutDefinition>();
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
            RunCombatResourceLoadoutDefinition[] source =
                definitions ?? Array.Empty<RunCombatResourceLoadoutDefinition>();
            RunCombatResourceLoadout[] loadouts =
                new RunCombatResourceLoadout[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                RunCombatResourceLoadoutDefinition definition = source[i];
                if (definition == null)
                {
                    Invalidate(
                        $"Combat resource loadout catalog '{name}' contains a null entry at index {i}.");
                    return;
                }

                if (!definition.TryCreateLoadout(
                        out loadouts[i],
                        out string definitionError))
                {
                    Invalidate(definitionError);
                    return;
                }
            }

            if (!RunCombatResourceLoadoutRegistry.TryCreate(
                    loadouts,
                    out registry,
                    out validationError))
            {
                registry = null;
                validationError =
                    $"Combat resource loadout catalog '{name}' is invalid: " +
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
