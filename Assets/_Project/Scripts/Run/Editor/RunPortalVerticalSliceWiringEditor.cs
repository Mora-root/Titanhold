using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Titanhold.Run.Editor
{
    public static class RunPortalVerticalSliceWiringEditor
    {
        private const string ScenePath = "Assets/_Project/Scenes/SampleScene.unity";
        private const string RuntimeObjectName = "RunFlowRuntime";
        private const string PortalFolderPath = "Assets/_Project/Prefabs/Run";
        private const string PortalPrefabPath = PortalFolderPath + "/RunPortal.prefab";
        private const string CrystalMaterialPath = "Assets/_Project/Materials/CrystalShard.mat";
        private const string RimMaterialPath = "Assets/_Project/Materials/Glow.mat";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        private const int InteractableLayer = 10;
        private const string InteractableLayerName = "Interactable";

        [MenuItem("Tools/Titanhold/Install Run Portal Vertical Slice Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                Scene scene = RequireCleanSampleScene();
                ConfigureInteractableLayer();
                RunPortalInteractable portalPrefab = CreateOrValidatePortalPrefab();
                ConfigureScene(scene, portalPrefab);

                AssetDatabase.SaveAssets();
                Debug.Log("Run Portal vertical-slice wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Run Portal vertical-slice wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Portal Vertical Slice Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidateInteractableLayer();
                RunPortalInteractable portalPrefab = ValidatePortalPrefab();
                ValidatePlayerSelectionMasks();
                ValidateScene(portalPrefab);
                Debug.Log("Run Portal vertical-slice wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Run Portal vertical-slice wiring validation failed: {exception}");
            }
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Run Portal wiring {operation} is available only in Edit Mode.");
            }
        }

        private static Scene RequireCleanSampleScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} before portal wiring.");

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "The active scene has unrelated unsaved changes. Save or revert them first.");
            }

            return scene;
        }

        private static void ConfigureInteractableLayer()
        {
            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
                throw new InvalidOperationException("Could not load ProjectSettings/TagManager.asset.");

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            SerializedProperty layer = layers.GetArrayElementAtIndex(InteractableLayer);
            string currentName = layer.stringValue;

            if (currentName != "Buildings" && currentName != InteractableLayerName)
            {
                throw new InvalidOperationException(
                    $"Layer {InteractableLayer} is unexpectedly named '{currentName}'.");
            }

            layer.stringValue = InteractableLayerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RunPortalInteractable CreateOrValidatePortalPrefab()
        {
            RunPortalInteractable existing =
                AssetDatabase.LoadAssetAtPath<RunPortalInteractable>(PortalPrefabPath);
            if (existing != null)
                return ValidatePortalPrefab();

            EnsurePortalFolder();

            Material surfaceMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(CrystalMaterialPath);
            Material rimMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(RimMaterialPath);
            if (surfaceMaterial == null || rimMaterial == null)
                throw new InvalidOperationException("Portal placeholder materials are missing.");

            GameObject root = new GameObject("RunPortal");
            try
            {
                SetLayerRecursively(root, InteractableLayer);

                CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
                collider.isTrigger = true;
                collider.direction = 1;
                collider.center = new Vector3(0f, 1.45f, 0f);
                collider.radius = 1.05f;
                collider.height = 2.9f;

                root.AddComponent<TargetVisual>();
                RunPortalInteractable interactable = root.AddComponent<RunPortalInteractable>();
                SerializedObject serializedInteractable = new SerializedObject(interactable);
                serializedInteractable.FindProperty("interactionRange").floatValue = 2f;
                serializedInteractable.ApplyModifiedPropertiesWithoutUndo();

                CreateSurface(root.transform, surfaceMaterial);
                CreateRim(root.transform, rimMaterial);
                SetLayerRecursively(root, InteractableLayer);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PortalPrefabPath);
                if (saved == null)
                    throw new InvalidOperationException($"Could not save {PortalPrefabPath}.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(PortalPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            return ValidatePortalPrefab();
        }

        private static void CreateSurface(Transform parent, Material material)
        {
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            surface.name = "PortalSurface";
            surface.transform.SetParent(parent, false);
            surface.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            surface.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            surface.transform.localScale = new Vector3(1f, 0.045f, 1.4f);

            Collider collider = surface.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);

            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void CreateRim(Transform parent, Material material)
        {
            GameObject rim = new GameObject("PortalRim");
            rim.transform.SetParent(parent, false);

            LineRenderer line = rim.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.widthMultiplier = 0.11f;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;

            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(
                    i,
                    new Vector3(
                        Mathf.Cos(angle) * 1.03f,
                        1.45f + Mathf.Sin(angle) * 1.44f,
                        -0.06f));
            }
        }

        private static void ConfigureScene(
            Scene scene,
            RunPortalInteractable portalPrefab)
        {
            GameObject runtimeObject = FindRootObject(scene, RuntimeObjectName);
            if (runtimeObject == null)
                throw new InvalidOperationException("RunFlowRuntime scene object is missing.");

            RunFlowRuntime runtime = runtimeObject.GetComponent<RunFlowRuntime>();
            if (runtime == null)
                throw new InvalidOperationException("RunFlowRuntime component is missing.");

            RunPortalSpawner spawner = runtimeObject.GetComponent<RunPortalSpawner>();
            if (spawner == null)
                spawner = Undo.AddComponent<RunPortalSpawner>(runtimeObject);

            SerializedObject serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("runFlowRuntime").objectReferenceValue = runtime;
            serializedSpawner.FindProperty("portalPrefab").objectReferenceValue = portalPrefab;
            serializedSpawner.FindProperty("spawnDistance").floatValue = 3f;
            serializedSpawner.FindProperty("navMeshSampleDistance").floatValue = 3f;
            serializedSpawner.FindProperty("heightOffset").floatValue = 0.05f;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static void ValidateInteractableLayer()
        {
            if (LayerMask.LayerToName(InteractableLayer) != InteractableLayerName)
            {
                throw new InvalidOperationException(
                    $"Layer {InteractableLayer} must be named {InteractableLayerName}.");
            }
        }

        private static RunPortalInteractable ValidatePortalPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Portal prefab not found: {PortalPrefabPath}");

            RunPortalInteractable interactable = prefab.GetComponent<RunPortalInteractable>();
            CapsuleCollider collider = prefab.GetComponent<CapsuleCollider>();
            TargetVisual visual = prefab.GetComponent<TargetVisual>();
            if (interactable == null || collider == null || visual == null)
                throw new InvalidOperationException("Portal prefab is missing required components.");

            if (!collider.isTrigger)
                throw new InvalidOperationException("Portal collider must remain a trigger.");

            if (prefab.layer != InteractableLayer)
                throw new InvalidOperationException("Portal prefab uses an invalid selection layer.");

            foreach (Transform transform in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (transform.gameObject.layer != InteractableLayer)
                    throw new InvalidOperationException("Portal child uses an invalid selection layer.");
            }

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Portal prefab has no placeholder renderers.");

            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterial == null)
                    throw new InvalidOperationException("Portal renderer has no material.");
            }

            return interactable;
        }

        private static void ValidatePlayerSelectionMasks()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
                throw new InvalidOperationException($"Player prefab not found: {PlayerPrefabPath}");

            int interactableBit = 1 << InteractableLayer;
            PlayerTargeting targeting = playerPrefab.GetComponent<PlayerTargeting>();
            HoverSystem hover = playerPrefab.GetComponent<HoverSystem>();
            if (targeting == null || hover == null)
                throw new InvalidOperationException("Player selection components are missing.");

            SerializedObject serializedTargeting = new SerializedObject(targeting);
            SerializedObject serializedHover = new SerializedObject(hover);
            int selectableMask = serializedTargeting.FindProperty("selectableMask").intValue;
            int hoverMask = serializedHover.FindProperty("hoverMask").intValue;
            if ((selectableMask & interactableBit) == 0 || (hoverMask & interactableBit) == 0)
            {
                throw new InvalidOperationException(
                    "Player selection masks do not include the Interactable layer.");
            }
        }

        private static void ValidateScene(RunPortalInteractable portalPrefab)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} before portal validation.");

            GameObject runtimeObject = FindRootObject(scene, RuntimeObjectName);
            RunPortalSpawner spawner = runtimeObject != null
                ? runtimeObject.GetComponent<RunPortalSpawner>()
                : null;
            if (spawner == null)
                throw new InvalidOperationException("RunPortalSpawner is missing from RunFlowRuntime.");

            SerializedObject serializedSpawner = new SerializedObject(spawner);
            UnityEngine.Object configuredPrefab =
                serializedSpawner.FindProperty("portalPrefab").objectReferenceValue;
            if (configuredPrefab != portalPrefab)
                throw new InvalidOperationException("RunPortalSpawner references an unexpected prefab.");
        }

        private static void EnsurePortalFolder()
        {
            if (AssetDatabase.IsValidFolder(PortalFolderPath))
                return;

            string guid = AssetDatabase.CreateFolder(
                "Assets/_Project/Prefabs",
                "Run");
            if (string.IsNullOrWhiteSpace(guid))
                throw new InvalidOperationException($"Could not create {PortalFolderPath}.");
        }

        private static GameObject FindRootObject(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                    return roots[i];
            }

            return null;
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
