using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    [CreateAssetMenu(
        fileName = "AbilityDefinitionCatalog",
        menuName = "Titanhold/Abilities/Ability Definition Catalog")]
    public sealed class AbilityDefinitionCatalog :
        ScriptableObject,
        IAbilityDefinitionResolver
    {
        [SerializeField] private ScriptableObject[] definitions =
            Array.Empty<ScriptableObject>();

        private AbilityDefinitionRegistry registry;
        private bool indexBuilt;
        private string validationError;

        public IReadOnlyList<ScriptableObject> Definitions =>
            definitions ?? Array.Empty<ScriptableObject>();

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
            string abilityId,
            out IAbilityDefinition definition)
        {
            EnsureIndex();
            if (!string.IsNullOrEmpty(validationError) || registry == null)
            {
                definition = null;
                return false;
            }

            return registry.TryResolve(abilityId, out definition);
        }

        public void RebuildIndex()
        {
            indexBuilt = false;
            EnsureIndex();
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(ScriptableObject[] configuredDefinitions)
        {
            definitions = configuredDefinitions ?? Array.Empty<ScriptableObject>();
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
            ScriptableObject[] source =
                definitions ?? Array.Empty<ScriptableObject>();
            IAbilityDefinition[] candidates =
                new IAbilityDefinition[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                ScriptableObject asset = source[i];
                if (asset == null)
                {
                    Invalidate(
                        $"Ability definition catalog '{name}' contains a null entry at index {i}.");
                    return;
                }

                if (asset is not IAbilityDefinition definition)
                {
                    Invalidate(
                        $"Asset '{asset.name}' in ability definition catalog '{name}' " +
                        "does not implement IAbilityDefinition.");
                    return;
                }

                candidates[i] = definition;
            }

            if (!AbilityDefinitionRegistry.TryCreate(
                    candidates,
                    out registry,
                    out validationError))
            {
                registry = null;
                validationError =
                    $"Ability definition catalog '{name}' is invalid: {validationError}";
            }
        }

        private void Invalidate(string error)
        {
            validationError = error;
            registry = null;
        }
    }
}
