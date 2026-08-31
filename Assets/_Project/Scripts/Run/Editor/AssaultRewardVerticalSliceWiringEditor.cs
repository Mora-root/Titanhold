using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Run.Editor
{
    public static class AssaultRewardVerticalSliceWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string RuntimeObjectName = "RunFlowRuntime";
        private const string ArenaObjectName = "AssaultArena_Prototype";
        private const string RewardSpawnPointName = "RewardChestSpawnPoint";
        private const string RewardPrefabPath =
            "Assets/_Project/Prefabs/Run/AssaultRewardChest.prefab";
        private const string RewardTablePath =
            "Assets/_Project/ScriptableObjects/Run/AssaultReward_Prototype.asset";
        private const string CrateModelPath =
            "Assets/ModularCastle_AssetPack/Props/Decoration/Crate.fbx";
        private const string ItemPickupPrefabPath =
            "Assets/_Project/Prefabs/Loot/GeneratedItemPickup.prefab";
        private const string GoldPickupPrefabPath =
            "Assets/_Project/Prefabs/Loot/GoldPickup.prefab";
        private const string PrototypeItemPath =
            "Assets/_Project/ScriptableObjects/Items/Sword_Test.asset";
        private const int InteractableLayer = 10;
        private const int GroundLayer = 9;

        [MenuItem("Tools/Titanhold/Install Assault Reward Vertical Slice Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                Scene scene = RequireCleanSampleScene();
                LootTable rewardTable = CreateOrUpdateRewardTable();
                AssaultRewardChestInteractable chestPrefab =
                    CreateOrUpdateRewardChestPrefab();
                ConfigureScene(scene, rewardTable, chestPrefab);
                AssetDatabase.SaveAssets();
                ValidateInternal();
                Debug.Log("Assault Reward vertical-slice wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Assault Reward vertical-slice wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Assault Reward Vertical Slice Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidateInternal();
                Debug.Log("Assault Reward vertical-slice wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Assault Reward vertical-slice wiring validation failed: {exception}");
            }
        }

        private static LootTable CreateOrUpdateRewardTable()
        {
            LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(
                RewardTablePath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<LootTable>();
                AssetDatabase.CreateAsset(table, RewardTablePath);
            }

            ItemDefinition prototypeItem =
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(PrototypeItemPath);
            if (prototypeItem == null)
            {
                throw new InvalidOperationException(
                    $"Prototype reward item is missing: {PrototypeItemPath}");
            }

            SerializedObject serializedTable = new SerializedObject(table);
            SerializedProperty entries = serializedTable.FindProperty("entries");
            entries.arraySize = 2;
            ConfigureEntry(
                entries.GetArrayElementAtIndex(0),
                LootDropKind.Gold,
                1f,
                10,
                20,
                null);
            ConfigureEntry(
                entries.GetArrayElementAtIndex(1),
                LootDropKind.Item,
                0.25f,
                1,
                1,
                prototypeItem);
            serializedTable.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            return table;
        }

        private static AssaultRewardChestInteractable
            CreateOrUpdateRewardChestPrefab()
        {
            GameObject itemPickup = AssetDatabase.LoadAssetAtPath<GameObject>(
                ItemPickupPrefabPath);
            GameObject goldPickup = AssetDatabase.LoadAssetAtPath<GameObject>(
                GoldPickupPrefabPath);
            GameObject crateModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                CrateModelPath);
            if (itemPickup == null || goldPickup == null || crateModel == null)
            {
                throw new InvalidOperationException(
                    "Assault reward chest source assets are incomplete.");
            }

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(
                RewardPrefabPath);
            GameObject contents = existing != null
                ? PrefabUtility.LoadPrefabContents(RewardPrefabPath)
                : new GameObject("AssaultRewardChest");
            try
            {
                contents.name = "AssaultRewardChest";
                SetLayerRecursively(contents, InteractableLayer);

                BoxCollider collider =
                    GetOrAddComponent<BoxCollider>(contents);
                collider.isTrigger = true;
                collider.center = new Vector3(0f, 0.325f, 0f);
                collider.size = new Vector3(0.9f, 0.65f, 0.9f);

                GetOrAddComponent<TargetVisual>(contents);
                WorldLootDropEmitter emitter =
                    GetOrAddComponent<WorldLootDropEmitter>(contents);
                AssaultRewardChestInteractable interactable =
                    GetOrAddComponent<AssaultRewardChestInteractable>(contents);

                Transform interactionPoint = contents.transform.Find(
                    "InteractionPoint");
                if (interactionPoint == null)
                {
                    GameObject point = new GameObject("InteractionPoint");
                    point.transform.SetParent(contents.transform, false);
                    interactionPoint = point.transform;
                }
                interactionPoint.localPosition = new Vector3(0f, 0.4f, 0f);
                interactionPoint.localRotation = Quaternion.identity;

                Transform visual = contents.transform.Find("CrateVisual");
                if (visual == null)
                {
                    GameObject visualObject = PrefabUtility.InstantiatePrefab(
                        crateModel,
                        contents.scene) as GameObject;
                    if (visualObject == null)
                    {
                        throw new InvalidOperationException(
                            "Could not instantiate the reward crate model.");
                    }

                    visualObject.name = "CrateVisual";
                    visualObject.transform.SetParent(contents.transform, false);
                    visual = visualObject.transform;
                }

                if (PrefabUtility.IsPartOfPrefabInstance(visual.gameObject))
                {
                    PrefabUtility.UnpackPrefabInstance(
                        visual.gameObject,
                        PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                }

                visual.localPosition = new Vector3(0f, 0.4f, 0f);
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one * 60f;
                SetLayerRecursively(visual.gameObject, InteractableLayer);

                SerializedObject serializedEmitter =
                    new SerializedObject(emitter);
                serializedEmitter.FindProperty("itemPickupPrefab")
                    .objectReferenceValue = itemPickup;
                serializedEmitter.FindProperty("goldPickupPrefab")
                    .objectReferenceValue = goldPickup;
                serializedEmitter.FindProperty("dropPoint")
                    .objectReferenceValue = interactionPoint;
                serializedEmitter.FindProperty("dropRadius").floatValue = 1.5f;
                serializedEmitter.FindProperty("dropSpawnHeight").floatValue =
                    1.2f;
                serializedEmitter.FindProperty("groundMask").intValue =
                    1 << GroundLayer;
                serializedEmitter.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject serializedInteractable =
                    new SerializedObject(interactable);
                serializedInteractable.FindProperty("interactionPoint")
                    .objectReferenceValue = interactionPoint;
                serializedInteractable.FindProperty("interactionRange")
                    .floatValue = 2f;
                serializedInteractable.FindProperty("dropEmitter")
                    .objectReferenceValue = emitter;
                serializedInteractable.FindProperty("openedLifetime")
                    .floatValue = 0.35f;
                serializedInteractable.ApplyModifiedPropertiesWithoutUndo();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    contents,
                    RewardPrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        $"Could not save {RewardPrefabPath}.");
                }
            }
            finally
            {
                if (existing != null)
                    PrefabUtility.UnloadPrefabContents(contents);
                else
                    UnityEngine.Object.DestroyImmediate(contents);
            }

            AssetDatabase.ImportAsset(
                RewardPrefabPath,
                ImportAssetOptions.ForceSynchronousImport);
            return ValidateRewardChestPrefab();
        }

        private static void ConfigureScene(
            Scene scene,
            LootTable rewardTable,
            AssaultRewardChestInteractable chestPrefab)
        {
            GameObject runtimeObject = FindRootObject(scene, RuntimeObjectName);
            GameObject arenaRoot = FindRootObject(scene, ArenaObjectName);
            if (runtimeObject == null || arenaRoot == null)
            {
                throw new InvalidOperationException(
                    "RunFlowRuntime or Assault arena root is missing.");
            }

            RunFlowRuntime runtime = runtimeObject.GetComponent<RunFlowRuntime>();
            AssaultEnemyRegistry enemyRegistry =
                runtimeObject.GetComponent<AssaultEnemyRegistry>();
            AssaultTargetRegistry targetRegistry =
                runtimeObject.GetComponent<AssaultTargetRegistry>();
            if (runtime == null || enemyRegistry == null || targetRegistry == null)
            {
                throw new InvalidOperationException(
                    "Assault reward dependencies are missing from RunFlowRuntime.");
            }

            Transform spawnPoint = arenaRoot.transform.Find(
                RewardSpawnPointName);
            if (spawnPoint == null)
            {
                GameObject point = new GameObject(RewardSpawnPointName);
                Undo.RegisterCreatedObjectUndo(
                    point,
                    "Create Assault Reward Spawn Point");
                point.transform.SetParent(arenaRoot.transform, false);
                point.transform.localPosition = new Vector3(3f, 0f, -7f);
                point.transform.localRotation = Quaternion.Euler(0f, -30f, 0f);
                spawnPoint = point.transform;
            }

            AssaultRewardChestSpawner spawner =
                GetOrAddComponent<AssaultRewardChestSpawner>(runtimeObject);
            SerializedObject serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("runFlowRuntime")
                .objectReferenceValue = runtime;
            serializedSpawner.FindProperty("enemyRegistry")
                .objectReferenceValue = enemyRegistry;
            serializedSpawner.FindProperty("targetRegistry")
                .objectReferenceValue = targetRegistry;
            serializedSpawner.FindProperty("rewardTable")
                .objectReferenceValue = rewardTable;
            serializedSpawner.FindProperty("chestPrefab")
                .objectReferenceValue = chestPrefab;
            serializedSpawner.FindProperty("spawnPoint")
                .objectReferenceValue = spawnPoint;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static void ValidateInternal()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} before validation.");

            LootTable rewardTable = ValidateRewardTable();
            AssaultRewardChestInteractable chestPrefab =
                ValidateRewardChestPrefab();
            Scene scene = SceneManager.GetActiveScene();
            GameObject runtimeObject = FindRootObject(scene, RuntimeObjectName);
            GameObject arenaRoot = FindRootObject(scene, ArenaObjectName);
            if (runtimeObject == null || arenaRoot == null)
                throw new InvalidOperationException("Reward scene roots are missing.");

            Transform spawnPoint = arenaRoot.transform.Find(
                RewardSpawnPointName);
            AssaultRewardChestSpawner spawner =
                runtimeObject.GetComponent<AssaultRewardChestSpawner>();
            if (spawnPoint == null || spawner == null)
                throw new InvalidOperationException("Reward scene wiring is incomplete.");

            SerializedObject serializedSpawner = new SerializedObject(spawner);
            if (serializedSpawner.FindProperty("runFlowRuntime")
                    .objectReferenceValue != runtimeObject.GetComponent<RunFlowRuntime>() ||
                serializedSpawner.FindProperty("enemyRegistry")
                    .objectReferenceValue != runtimeObject.GetComponent<AssaultEnemyRegistry>() ||
                serializedSpawner.FindProperty("targetRegistry")
                    .objectReferenceValue != runtimeObject.GetComponent<AssaultTargetRegistry>() ||
                serializedSpawner.FindProperty("rewardTable")
                    .objectReferenceValue != rewardTable ||
                serializedSpawner.FindProperty("chestPrefab")
                    .objectReferenceValue != chestPrefab ||
                serializedSpawner.FindProperty("spawnPoint")
                    .objectReferenceValue != spawnPoint)
            {
                throw new InvalidOperationException(
                    "AssaultRewardChestSpawner scene references are incomplete.");
            }
        }

        private static LootTable ValidateRewardTable()
        {
            LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(
                RewardTablePath);
            if (table == null)
                throw new InvalidOperationException("Prototype reward table is missing.");

            SerializedObject serializedTable = new SerializedObject(table);
            SerializedProperty entries = serializedTable.FindProperty("entries");
            if (entries.arraySize != 2)
                throw new InvalidOperationException("Unexpected reward table size.");

            SerializedProperty gold = entries.GetArrayElementAtIndex(0);
            SerializedProperty item = entries.GetArrayElementAtIndex(1);
            ItemDefinition prototypeItem =
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(PrototypeItemPath);
            if (gold.FindPropertyRelative("kind").enumValueIndex !=
                    (int)LootDropKind.Gold ||
                Math.Abs(gold.FindPropertyRelative("dropChance").floatValue - 1f) >
                    0.0001f ||
                gold.FindPropertyRelative("minAmount").intValue != 10 ||
                gold.FindPropertyRelative("maxAmount").intValue != 20 ||
                item.FindPropertyRelative("kind").enumValueIndex !=
                    (int)LootDropKind.Item ||
                Math.Abs(item.FindPropertyRelative("dropChance").floatValue - 0.25f) >
                    0.0001f ||
                item.FindPropertyRelative("item").objectReferenceValue !=
                    prototypeItem)
            {
                throw new InvalidOperationException(
                    "Prototype reward table contents are unexpected.");
            }

            return table;
        }

        private static AssaultRewardChestInteractable
            ValidateRewardChestPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                RewardPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Assault reward chest prefab is missing.");

            if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Variant)
            {
                throw new InvalidOperationException(
                    "Assault reward chest must remain an independent prefab.");
            }

            AssaultRewardChestInteractable interactable =
                prefab.GetComponent<AssaultRewardChestInteractable>();
            WorldLootDropEmitter emitter =
                prefab.GetComponent<WorldLootDropEmitter>();
            BoxCollider collider = prefab.GetComponent<BoxCollider>();
            TargetVisual visual = prefab.GetComponent<TargetVisual>();
            if (interactable == null || emitter == null || collider == null ||
                visual == null || !collider.isTrigger)
            {
                throw new InvalidOperationException(
                    "Assault reward chest prefab components are invalid.");
            }

            if (prefab.GetComponent<ChestInteractable>() != null)
            {
                throw new InvalidOperationException(
                    "Assault reward chest must not open the inventory chest UI.");
            }

            Transform crateVisual = prefab.transform.Find("CrateVisual");
            Renderer[] crateRenderers = crateVisual != null
                ? crateVisual.GetComponentsInChildren<Renderer>(true)
                : Array.Empty<Renderer>();
            if (crateVisual == null || crateRenderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Assault reward chest crate visual is missing.");
            }

            Bounds visualBounds = crateRenderers[0].bounds;
            for (int i = 1; i < crateRenderers.Length; i++)
                visualBounds.Encapsulate(crateRenderers[i].bounds);
            if (visualBounds.size.x < 0.5f ||
                visualBounds.size.y < 0.5f ||
                visualBounds.size.z < 0.5f ||
                visualBounds.max.y < 0.5f)
            {
                throw new InvalidOperationException(
                    $"Assault reward chest visual bounds are too small or below the floor: center={visualBounds.center}, size={visualBounds.size}.");
            }

            Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].gameObject.layer != InteractableLayer)
                {
                    throw new InvalidOperationException(
                        "Assault reward chest hierarchy must use the Interactable layer.");
                }
            }

            SerializedObject serializedEmitter = new SerializedObject(emitter);
            if (serializedEmitter.FindProperty("itemPickupPrefab")
                    .objectReferenceValue == null ||
                serializedEmitter.FindProperty("goldPickupPrefab")
                    .objectReferenceValue == null)
            {
                throw new InvalidOperationException(
                    "Assault reward chest drop prefabs are missing.");
            }

            return interactable;
        }

        private static void ConfigureEntry(
            SerializedProperty entry,
            LootDropKind kind,
            float chance,
            int minAmount,
            int maxAmount,
            ItemDefinition item)
        {
            entry.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            entry.FindPropertyRelative("dropChance").floatValue = chance;
            entry.FindPropertyRelative("minAmount").intValue = minAmount;
            entry.FindPropertyRelative("maxAmount").intValue = maxAmount;
            entry.FindPropertyRelative("item").objectReferenceValue = item;
            entry.FindPropertyRelative("generatedModifierRules").arraySize = 0;
            entry.FindPropertyRelative("minGeneratedModifiers").intValue = 0;
            entry.FindPropertyRelative("maxGeneratedModifiers").intValue = 0;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(gameObject);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
                transforms[i].gameObject.layer = layer;
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Assault Reward wiring {operation} is available only in Edit Mode.");
            }
        }

        private static Scene RequireCleanSampleScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} before reward wiring.");

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "The active scene has unrelated unsaved changes. Save or revert them first.");
            }

            return scene;
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
    }
}
