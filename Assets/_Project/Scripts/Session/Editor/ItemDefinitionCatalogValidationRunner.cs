using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Session.Editor
{
    public static class ItemDefinitionCatalogValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Item Definition Catalog")]
        public static void Validate()
        {
            ItemDefinitionCatalog validCatalog = null;
            ItemDefinitionCatalog invalidCatalog = null;
            ItemDefinition potion = null;
            ItemDefinition sword = null;
            ItemDefinition duplicatePotion = null;

            try
            {
                potion = CreateDefinition("item:potion");
                sword = CreateDefinition("item:sword");
                duplicatePotion = CreateDefinition("item:potion");

                validCatalog = CreateCatalog(potion, sword);
                Assert(validCatalog.IsValid, validCatalog.ValidationError);
                Assert(validCatalog.Definitions.Count == 2,
                    "Catalog does not expose all configured definitions.");
                Assert(validCatalog.TryResolve("item:potion", out ItemDefinition resolved) &&
                       ReferenceEquals(resolved, potion),
                    "Catalog did not resolve an exact item id.");
                Assert(!validCatalog.TryResolve("ITEM:POTION", out _),
                    "Catalog item ids must use ordinal case-sensitive matching.");
                Assert(!validCatalog.TryResolve("item:missing", out _),
                    "Catalog resolved an unknown item id.");

                invalidCatalog = CreateCatalog(potion, duplicatePotion);
                Assert(!invalidCatalog.IsValid &&
                       invalidCatalog.ValidationError.Contains("duplicate item id"),
                    "Catalog accepted duplicate item ids.");
                Assert(!invalidCatalog.TryResolve("item:potion", out _),
                    "Invalid catalog exposed a partially built index.");

                SetDefinitions(validCatalog, potion, null);
                validCatalog.RebuildIndex();
                Assert(!validCatalog.IsValid &&
                       validCatalog.ValidationError.Contains("null entry"),
                    "Catalog accepted a null definition entry.");

                Debug.Log("Item Definition Catalog validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Item Definition Catalog validation failed: {exception}");
            }
            finally
            {
                Destroy(validCatalog);
                Destroy(invalidCatalog);
                Destroy(potion);
                Destroy(sword);
                Destroy(duplicatePotion);
            }
        }

        private static ItemDefinitionCatalog CreateCatalog(
            params ItemDefinition[] definitions)
        {
            ItemDefinitionCatalog catalog =
                ScriptableObject.CreateInstance<ItemDefinitionCatalog>();
            SetDefinitions(catalog, definitions);
            catalog.RebuildIndex();
            return catalog;
        }

        private static void SetDefinitions(
            ItemDefinitionCatalog catalog,
            params ItemDefinition[] definitions)
        {
            SerializedObject serialized = new(catalog);
            SerializedProperty property = serialized.FindProperty("definitions");
            property.arraySize = definitions.Length;
            for (int i = 0; i < definitions.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ItemDefinition CreateDefinition(string id)
        {
            ItemDefinition definition =
                ScriptableObject.CreateInstance<ItemDefinition>();
            definition.name = id;
            SerializedObject serialized = new(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void Destroy(UnityEngine.Object instance)
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
