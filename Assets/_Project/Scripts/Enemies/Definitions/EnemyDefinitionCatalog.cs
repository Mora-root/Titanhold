using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Enemies
{
    [CreateAssetMenu(
        fileName = "EnemyDefinitionCatalog",
        menuName = "Titanhold/Enemies/Enemy Definition Catalog")]
    public sealed class EnemyDefinitionCatalog :
        ScriptableObject,
        IEnemyDefinitionResolver
    {
        [SerializeField] private EnemyDefinition[] definitions =
            Array.Empty<EnemyDefinition>();

        private EnemyDefinitionRegistry registry;
        private Dictionary<string, GameObject> prefabsById;
        private bool indexBuilt;
        private string validationError;

        public IReadOnlyList<EnemyDefinition> Definitions =>
            definitions ?? Array.Empty<EnemyDefinition>();

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
            string enemyId,
            out EnemyArchetype archetype)
        {
            EnsureIndex();
            if (!string.IsNullOrEmpty(validationError) || registry == null)
            {
                archetype = null;
                return false;
            }

            return registry.TryResolve(enemyId, out archetype);
        }

        public bool TryResolvePrefab(
            string enemyId,
            out GameObject prefab)
        {
            EnsureIndex();
            prefab = null;
            if (!string.IsNullOrEmpty(validationError) || prefabsById == null)
                return false;

            string normalizedId = enemyId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   prefabsById.TryGetValue(normalizedId, out prefab) &&
                   prefab != null;
        }

        public void RebuildIndex()
        {
            indexBuilt = false;
            EnsureIndex();
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            EnemyDefinition[] configuredDefinitions)
        {
            definitions = configuredDefinitions ??
                Array.Empty<EnemyDefinition>();
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
            prefabsById = null;
            validationError = string.Empty;

            EnemyDefinition[] source =
                definitions ?? Array.Empty<EnemyDefinition>();
            EnemyArchetype[] archetypes = new EnemyArchetype[source.Length];
            GameObject[] prefabs = new GameObject[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                EnemyDefinition definition = source[i];
                if (definition == null)
                {
                    Invalidate(
                        $"Enemy definition catalog '{name}' contains a null entry at index {i}.");
                    return;
                }

                if (!definition.TryCreateArchetype(
                        out archetypes[i],
                        out string definitionError))
                {
                    Invalidate(definitionError);
                    return;
                }

                prefabs[i] = definition.Prefab;
            }

            if (!EnemyDefinitionRegistry.TryCreate(
                    archetypes,
                    out registry,
                    out string registryError))
            {
                Invalidate(
                    $"Enemy definition catalog '{name}' is invalid: {registryError}");
                return;
            }

            Dictionary<string, GameObject> resolvedPrefabs =
                new(StringComparer.Ordinal);
            for (int i = 0; i < archetypes.Length; i++)
                resolvedPrefabs.Add(archetypes[i].EnemyId, prefabs[i]);

            prefabsById = resolvedPrefabs;
        }

        private void Invalidate(string error)
        {
            validationError = error;
            registry = null;
            prefabsById = null;
        }
    }
}
