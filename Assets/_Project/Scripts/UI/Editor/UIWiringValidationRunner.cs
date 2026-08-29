using Titanhold.UI.Common;
using Titanhold.UI.Containers;
using Titanhold.UI.Equipment;
using Titanhold.UI.SectionInventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UIWiringValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate UI Wiring")]
    public static void Validate()
    {
        ValidationState state = new();

        ValidateSceneCore(state);
        ValidateInteractionServices(state);
        ValidateDragVisuals(state);
        ValidateMissingScriptsInOpenScenes(state);
        ValidateProjectLegacyScriptAssets(state);
        ValidateProjectPrefabMissingScripts(state);

        Debug.Log($"{nameof(UIWiringValidationRunner)} finished: {state.Errors} error(s), {state.Warnings} warning(s).");
    }

    private static void ValidateSceneCore(ValidationState state)
    {
        GameUIBinder[] binders = FindSceneObjects<GameUIBinder>();
        if (binders.Length == 0)
            Warn(state, "No GameUIBinder found in the open scene. Manual wiring can still work, but binder setup is recommended.");
        else if (binders.Length > 1)
            Warn(state, $"Found {binders.Length} GameUIBinder components. Usually one UI composition root is enough for one local player.");

        RequireAtLeastOne<PlayerInventory>(state, "PlayerInventory");
        RequireAtLeastOne<PlayerEquipmentRuntime>(state, "PlayerEquipmentRuntime");
        RequireAtLeastOne<ItemInteractionService>(state, "ItemInteractionService");
        RequireAtLeastOne<ItemInteractionContext>(state, "ItemInteractionContext");
        RequireAtLeastOne<ItemDragContext>(state, "ItemDragContext");
        RequireAtLeastOne<PlayerInventoryWindow>(state, "PlayerInventoryWindow or generic ItemContainerWindow player inventory UI", false);
        RequireAtLeastOne<ItemContainerWindow>(state, "ItemContainerWindow", false);
        RequireAtLeastOne<CharacterEquipmentPanel>(state, "CharacterEquipmentPanel", false);
        RequireAtLeastOne<ChestWindowController>(state, "ChestWindowController", false);
    }

    private static void ValidateInteractionServices(ValidationState state)
    {
        foreach (ItemInteractionService service in FindSceneObjects<ItemInteractionService>())
        {
            SerializedObject serialized = new(service);
            CheckReference(state, serialized, service, "playerInventory", "PlayerInventory");
            CheckReference(state, serialized, service, "playerEquipmentRuntime", "PlayerEquipmentRuntime");
            CheckReference(state, serialized, service, "interactionContext", "ItemInteractionContext");
            CheckReference(state, serialized, service, "dragContext", "ItemDragContext");

            SerializedProperty autoDiscover = serialized.FindProperty("autoDiscoverEventSourcesInChildren");
            SerializedProperty eventSources = serialized.FindProperty("eventSourceBehaviours");

            bool autoDiscoverEnabled = autoDiscover != null && autoDiscover.boolValue;
            bool hasExplicitEventSources = false;

            if (eventSources != null && eventSources.isArray)
            {
                for (int i = 0; i < eventSources.arraySize; i++)
                {
                    SerializedProperty element = eventSources.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue == null)
                    {
                        Warn(state, $"{service.name}: ItemInteractionService.eventSourceBehaviours has an empty element at index {i}.");
                    }
                    else
                    {
                        hasExplicitEventSources = true;
                    }
                }
            }

            if (!autoDiscoverEnabled && !hasExplicitEventSources)
                Warn(state, $"{service.name}: ItemInteractionService has no auto discovery and no explicit event sources.");
        }
    }

    private static void ValidateDragVisuals(ValidationState state)
    {
        foreach (ItemDragVisual visual in FindSceneObjects<ItemDragVisual>())
        {
            SerializedObject serialized = new(visual);
            Graphic iconGraphic = serialized.FindProperty("iconImage")?.objectReferenceValue as Graphic;
            Graphic amountGraphic = serialized.FindProperty("amountText")?.objectReferenceValue as Graphic;

            if (iconGraphic != null && iconGraphic.raycastTarget)
                Warn(state, $"{visual.name}: ItemDragVisual iconImage should have raycastTarget disabled.");

            if (amountGraphic != null && amountGraphic.raycastTarget)
                Warn(state, $"{visual.name}: ItemDragVisual amountText should have raycastTarget disabled.");
        }
    }

    private static void ValidateMissingScriptsInOpenScenes(ValidationState state)
    {
        foreach (GameObject gameObject in FindSceneObjects<GameObject>())
        {
            if (!gameObject.scene.IsValid())
                continue;

            int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            if (missingCount > 0)
                Error(state, $"Open scene object '{GetHierarchyPath(gameObject)}' has {missingCount} missing script component(s).");
        }
    }

    private static void ValidateProjectLegacyScriptAssets(ValidationState state)
    {
        string[] legacyScriptNames =
        {
            "InventoryDragDropController",
            "InventoryEquipmentInteractionController",
            "CharacterEquipmentInteractionController",
            "PlayerLootInventory",
            "PlayerItemInventory",
            "PlayerEquipmentPanel",
            "LootItemTooltip",
            "EquipmentItemTooltip"
        };

        foreach (string scriptName in legacyScriptNames)
        {
            string[] guids = AssetDatabase.FindAssets($"{scriptName} t:Script", new[] { "Assets/_Project/Scripts" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == scriptName)
                    Warn(state, $"Legacy script asset still exists: {path}");
            }
        }
    }

    private static void ValidateProjectPrefabMissingScripts(ValidationState state)
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" });
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            foreach (Transform transform in prefab.GetComponentsInChildren<Transform>(true))
            {
                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                if (missingCount > 0)
                    Error(state, $"Prefab '{path}' object '{GetHierarchyPath(transform.gameObject)}' has {missingCount} missing script component(s).");
            }
        }
    }

    private static void RequireAtLeastOne<T>(ValidationState state, string label, bool important = true) where T : Object
    {
        if (FindSceneObjects<T>().Length > 0)
            return;

        if (important)
            Warn(state, $"No {label} found in the open scene.");
        else
            Warn(state, $"No {label} found in the open scene. This is OK if that UI is not part of the current test scene.");
    }

    private static void CheckReference(
        ValidationState state,
        SerializedObject serialized,
        Object owner,
        string propertyName,
        string label)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue != null)
            return;

        Warn(state, $"{owner.name}: missing {label} reference.");
    }

    private static T[] FindSceneObjects<T>() where T : Object
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include);
    }

    private static string GetHierarchyPath(GameObject gameObject)
    {
        string path = gameObject.name;
        Transform current = gameObject.transform.parent;

        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
    }

    private static void Warn(ValidationState state, string message)
    {
        state.Warnings++;
        Debug.LogWarning($"{nameof(UIWiringValidationRunner)}: {message}");
    }

    private static void Error(ValidationState state, string message)
    {
        state.Errors++;
        Debug.LogError($"{nameof(UIWiringValidationRunner)}: {message}");
    }

    private sealed class ValidationState
    {
        public int Warnings;
        public int Errors;
    }
}
