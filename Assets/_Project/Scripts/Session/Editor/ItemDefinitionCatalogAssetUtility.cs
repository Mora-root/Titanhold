using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Session.Editor
{
    public static class ItemDefinitionCatalogAssetUtility
    {
        public const string ItemDefinitionsFolder =
            "Assets/_Project/ScriptableObjects/Items";
        public const string CatalogAssetPath =
            ItemDefinitionsFolder + "/ItemDefinitionCatalog.asset";

        [MenuItem("Tools/Titanhold/Build Runtime Item Definition Catalog")]
        public static void BuildCatalog()
        {
            try
            {
                List<ItemDefinition> definitions = LoadDefinitions();
                ValidateDefinitions(definitions);

                ItemDefinitionCatalog catalog =
                    AssetDatabase.LoadAssetAtPath<ItemDefinitionCatalog>(
                        CatalogAssetPath);
                if (catalog == null)
                {
                    catalog = ScriptableObject.CreateInstance<
                        ItemDefinitionCatalog>();
                    AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
                }

                SerializedObject serialized = new(catalog);
                SerializedProperty property =
                    serialized.FindProperty("definitions");
                property.arraySize = definitions.Count;
                for (int i = 0; i < definitions.Count; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue =
                        definitions[i];
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                catalog.RebuildIndex();
                if (!catalog.IsValid)
                    throw new InvalidOperationException(catalog.ValidationError);

                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(CatalogAssetPath);
                Debug.Log(
                    $"Runtime Item Definition Catalog built with {definitions.Count} items.",
                    catalog);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Runtime Item Definition Catalog build failed: {exception}");
            }
        }

        [MenuItem(
            "Tools/Titanhold/Validate Runtime Item Definition Catalog Asset")]
        public static void ValidateCatalogAsset()
        {
            try
            {
                ItemDefinitionCatalog catalog =
                    AssetDatabase.LoadAssetAtPath<ItemDefinitionCatalog>(
                        CatalogAssetPath);
                if (catalog == null)
                {
                    throw new InvalidOperationException(
                        $"Catalog asset is missing at '{CatalogAssetPath}'.");
                }

                catalog.RebuildIndex();
                if (!catalog.IsValid)
                    throw new InvalidOperationException(catalog.ValidationError);

                List<ItemDefinition> definitions = LoadDefinitions();
                ValidateDefinitions(definitions);
                if (catalog.Definitions.Count != definitions.Count)
                {
                    throw new InvalidOperationException(
                        "Catalog does not contain every project item definition.");
                }

                for (int i = 0; i < definitions.Count; i++)
                {
                    ItemDefinition expected = definitions[i];
                    if (!catalog.TryResolve(
                            expected.Id,
                            out ItemDefinition resolved) ||
                        !ReferenceEquals(expected, resolved))
                    {
                        throw new InvalidOperationException(
                            $"Catalog does not resolve item '{expected.Id}' exactly.");
                    }
                }

                Debug.Log(
                    $"Runtime Item Definition Catalog asset validation passed ({definitions.Count} items).",
                    catalog);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Runtime Item Definition Catalog asset validation failed: {exception}");
            }
        }

        private static List<ItemDefinition> LoadDefinitions()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:ItemDefinition",
                new[] { ItemDefinitionsFolder });
            List<ItemDefinition> definitions = new(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ItemDefinition definition =
                    AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (definition != null)
                    definitions.Add(definition);
            }

            definitions.Sort((left, right) => string.Compare(
                AssetDatabase.GetAssetPath(left),
                AssetDatabase.GetAssetPath(right),
                StringComparison.Ordinal));
            return definitions;
        }

        private static void ValidateDefinitions(
            IReadOnlyList<ItemDefinition> definitions)
        {
            HashSet<string> ids = new(StringComparer.Ordinal);
            for (int i = 0; i < definitions.Count; i++)
            {
                ItemDefinition definition = definitions[i];
                if (definition == null)
                {
                    throw new InvalidOperationException(
                        $"Null item definition at index {i}.");
                }

                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"Item '{definition.name}' has an empty id.");
                }

                if (!ids.Add(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate item id '{definition.Id}'.");
                }
            }
        }
    }
}
