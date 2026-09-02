using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Session
{
    [CreateAssetMenu(
        fileName = "ItemDefinitionCatalog",
        menuName = "Project/Items/Item Definition Catalog")]
    public sealed class ItemDefinitionCatalog : ScriptableObject,
        IItemDefinitionResolver
    {
        [SerializeField] private ItemDefinition[] definitions =
            Array.Empty<ItemDefinition>();

        private Dictionary<string, ItemDefinition> definitionsById;
        private bool indexBuilt;
        private string validationError;

        public IReadOnlyList<ItemDefinition> Definitions =>
            definitions ?? Array.Empty<ItemDefinition>();

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
            string definitionId,
            out ItemDefinition definition)
        {
            EnsureIndex();
            if (!string.IsNullOrEmpty(validationError) ||
                string.IsNullOrWhiteSpace(definitionId))
            {
                definition = null;
                return false;
            }

            return definitionsById.TryGetValue(definitionId, out definition);
        }

        public void RebuildIndex()
        {
            indexBuilt = false;
            EnsureIndex();
        }

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
            validationError = null;

            ItemDefinition[] source = definitions ?? Array.Empty<ItemDefinition>();
            Dictionary<string, ItemDefinition> candidate =
                new(source.Length, StringComparer.Ordinal);

            for (int i = 0; i < source.Length; i++)
            {
                ItemDefinition definition = source[i];
                if (definition == null)
                {
                    Invalidate($"Item definition catalog '{name}' contains a null entry at index {i}.");
                    return;
                }

                string id = definition.Id;
                if (string.IsNullOrWhiteSpace(id))
                {
                    Invalidate($"Item definition catalog '{name}' contains item '{definition.name}' with an empty id.");
                    return;
                }

                if (!candidate.TryAdd(id, definition))
                {
                    Invalidate($"Item definition catalog '{name}' contains duplicate item id '{id}'.");
                    return;
                }
            }

            definitionsById = candidate;
        }

        private void Invalidate(string error)
        {
            validationError = error;
            definitionsById = new Dictionary<string, ItemDefinition>(
                StringComparer.Ordinal);
        }
    }
}
