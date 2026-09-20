using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Run
{
    public sealed class RunUpgradeDefinitionRegistry :
        IRunUpgradeDefinitionResolver
    {
        private readonly Dictionary<string, IRunUpgradeDefinition>
            definitions;

        private RunUpgradeDefinitionRegistry(
            Dictionary<string, IRunUpgradeDefinition> definitions)
        {
            this.definitions = definitions;
        }

        public int Count => definitions.Count;

        public static bool TryCreate(
            IReadOnlyList<IRunUpgradeDefinition> source,
            out RunUpgradeDefinitionRegistry registry,
            out string error)
        {
            registry = null;
            error = string.Empty;
            if (source == null || source.Count == 0)
            {
                error = "At least one run upgrade definition is required.";
                return false;
            }

            Dictionary<string, IRunUpgradeDefinition> index =
                new(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                IRunUpgradeDefinition definition = source[i];
                if (definition == null)
                {
                    error = $"Run upgrade definition {i} is missing.";
                    return false;
                }

                string upgradeId = definition.UpgradeId?.Trim() ??
                    string.Empty;
                if (upgradeId.Length == 0 ||
                    !string.Equals(
                        upgradeId,
                        definition.UpgradeId,
                        StringComparison.Ordinal))
                {
                    error =
                        $"Run upgrade definition {i} has an invalid stable id.";
                    return false;
                }

                if (!definition.TryValidate(out string definitionError))
                {
                    error = definitionError;
                    return false;
                }

                if (!index.TryAdd(upgradeId, definition))
                {
                    error = $"Run upgrade id '{upgradeId}' occurs more than once.";
                    return false;
                }
            }

            registry = new RunUpgradeDefinitionRegistry(index);
            return true;
        }

        public bool TryResolve(
            string upgradeId,
            out IRunUpgradeDefinition definition)
        {
            definition = null;
            string normalizedId = upgradeId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   definitions.TryGetValue(normalizedId, out definition);
        }
    }

    [CreateAssetMenu(
        fileName = "RunUpgradeDefinitionCatalog",
        menuName = "Titanhold/Run/Upgrade Definition Catalog")]
    public sealed class RunUpgradeDefinitionCatalog :
        ScriptableObject,
        IRunUpgradeDefinitionResolver
    {
        [SerializeField] private ScriptableObject[] definitions =
            Array.Empty<ScriptableObject>();

        private RunUpgradeDefinitionRegistry registry;
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
            string upgradeId,
            out IRunUpgradeDefinition definition)
        {
            EnsureIndex();
            if (registry == null || !string.IsNullOrEmpty(validationError))
            {
                definition = null;
                return false;
            }

            return registry.TryResolve(upgradeId, out definition);
        }

        public void RebuildIndex()
        {
            indexBuilt = false;
            EnsureIndex();
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(ScriptableObject[] configuredDefinitions)
        {
            definitions = configuredDefinitions ??
                Array.Empty<ScriptableObject>();
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
            IRunUpgradeDefinition[] runtimeDefinitions =
                new IRunUpgradeDefinition[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] is not IRunUpgradeDefinition definition)
                {
                    validationError = source[i] == null
                        ? $"Run upgrade catalog '{name}' has a null entry at index {i}."
                        : $"Asset '{source[i].name}' does not implement IRunUpgradeDefinition.";
                    return;
                }

                runtimeDefinitions[i] = definition;
            }

            if (!RunUpgradeDefinitionRegistry.TryCreate(
                    runtimeDefinitions,
                    out registry,
                    out validationError))
            {
                registry = null;
                validationError =
                    $"Run upgrade catalog '{name}' is invalid: " +
                    validationError;
            }
        }
    }
}
