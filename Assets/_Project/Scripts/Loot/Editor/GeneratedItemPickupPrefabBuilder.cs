using UnityEditor;
using UnityEngine;

public static class GeneratedItemPickupPrefabBuilder
{
    private const string PrefabPath = "Assets/_Project/Prefabs/Loot/GeneratedItemPickup.prefab";

    [MenuItem("Tools/Titanhold/Create Generated Item Pickup Prefab")]
    public static void CreatePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Debug.LogWarning($"Generated item pickup prefab already exists at {PrefabPath}.");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            return;
        }

        GameObject root = new("GeneratedItemPickup");

        try
        {
            BuildRoot(root);
            GeneratedItemPickupView view = BuildView(root);

            SerializedObject serializedView = new(view);
            serializedView.FindProperty("modelRoot").objectReferenceValue = root.transform.Find("ModelRoot");
            serializedView.FindProperty("fallbackVisual").objectReferenceValue = root.transform.Find("ModelRoot/FallbackVisual").gameObject;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Selection.activeObject = prefab;
            Debug.Log($"Generated item pickup prefab created at {PrefabPath}.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [MenuItem("Tools/Titanhold/Cleanup Generated Item Pickup Prefab Display")]
    public static void CleanupPrefabDisplay()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Generated item pickup prefab was not found at {PrefabPath}.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

        try
        {
            Transform oldWorldLabel = root.transform.Find("WorldLabelCanvas");
            if (oldWorldLabel != null)
                Object.DestroyImmediate(oldWorldLabel.gameObject);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"Generated item pickup prefab display cleanup completed at {PrefabPath}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildRoot(GameObject root)
    {
        root.AddComponent<LootPickup>();

        SphereCollider collider = root.AddComponent<SphereCollider>();
        collider.radius = 0.75f;
        collider.isTrigger = true;

        root.AddComponent<LootDropMotion>();
        root.AddComponent<GeneratedItemPickupView>();
    }

    private static GeneratedItemPickupView BuildView(GameObject root)
    {
        GameObject modelRoot = new("ModelRoot");
        modelRoot.transform.SetParent(root.transform, false);

        GameObject fallbackVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fallbackVisual.name = "FallbackVisual";
        fallbackVisual.transform.SetParent(modelRoot.transform, false);
        fallbackVisual.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

        Collider fallbackCollider = fallbackVisual.GetComponent<Collider>();
        if (fallbackCollider != null)
            Object.DestroyImmediate(fallbackCollider);

        return root.GetComponent<GeneratedItemPickupView>();
    }
}
