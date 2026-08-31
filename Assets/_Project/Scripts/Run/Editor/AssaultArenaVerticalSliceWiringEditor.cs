using System;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Titanhold.Run.Editor
{
    public static class AssaultArenaVerticalSliceWiringEditor
    {
        private const string ScenePath = "Assets/_Project/Scenes/SampleScene.unity";
        private const string RuntimeObjectName = "RunFlowRuntime";
        private const string ArenaObjectName = "AssaultArena_Prototype";
        private const string ArenaDestinationName = "PlayerDestination";
        private const string SpawnPointsObjectName = "EnemySpawnPoints";
        private const string ReturnPortalSpawnPointName =
            "ReturnPortalSpawnPoint";
        private const string SourcePortalPrefabPath =
            "Assets/_Project/Prefabs/Run/RunPortal.prefab";
        private const string ReturnPortalPrefabPath =
            "Assets/_Project/Prefabs/Run/AssaultReturnPortal.prefab";
        private const string SourceEnemyPrefabPath =
            "Assets/_Project/Prefabs/Enemy/Skelet.prefab";
        private const string AssaultEnemyPrefabPath =
            "Assets/_Project/Prefabs/Enemy/Skelet_Assault.prefab";
        private const string RunDataFolderPath =
            "Assets/_Project/ScriptableObjects/Run";
        private const string WaveDefinitionPath =
            RunDataFolderPath + "/AssaultWave_Prototype.asset";
        private const string ArenaNavMeshDataPath =
            "Assets/_Project/Scenes/SampleScene/NavMesh-AssaultArena_Prototype.asset";
        private const string FloorMaterialPath =
            "Assets/_Project/Materials/1.mat";
        private const string SpawnMaterialPath =
            "Assets/_Project/Materials/Spawn.mat";
        private const int GroundLayer = 9;
        private const int InteractableLayer = 10;

        [MenuItem("Tools/Titanhold/Install Assault Arena Vertical Slice Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                Scene scene = RequireCleanSampleScene();
                GameObject enemyPrefab = CreateOrUpdateAssaultEnemyPrefab();
                AssaultReturnPortalInteractable returnPortalPrefab =
                    CreateOrUpdateReturnPortalPrefab();
                AssaultWaveDefinition waveDefinition =
                    CreateOrUpdateWaveDefinition(enemyPrefab);
                ConfigureScene(scene, waveDefinition, returnPortalPrefab);
                AssetDatabase.SaveAssets();
                ValidateInternal();
                Debug.Log("Assault Arena vertical-slice wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Assault Arena vertical-slice wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Assault Arena Vertical Slice Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidateInternal();
                Debug.Log("Assault Arena vertical-slice wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Assault Arena vertical-slice wiring validation failed: {exception}");
            }
        }

        private static GameObject CreateOrUpdateAssaultEnemyPrefab()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                SourceEnemyPrefabPath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Source enemy prefab is missing: {SourceEnemyPrefabPath}");
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(
                SourceEnemyPrefabPath);
            try
            {
                contents.name = "Skelet_Assault";
                RemoveAllComponents<EnemyLootTableDropper>(contents);
                RemoveAllComponents<EnemyRunContributionSource>(contents);
                RemoveAllComponents<EnemyThreatSource>(contents);

                EnemyBrain brain = contents.GetComponentInChildren<EnemyBrain>(true);
                if (brain == null)
                    throw new InvalidOperationException("Assault enemy brain is missing.");

                AssaultAggroTargetProvider targetProvider =
                    brain.GetComponent<AssaultAggroTargetProvider>();
                if (targetProvider == null)
                {
                    targetProvider =
                        brain.gameObject.AddComponent<AssaultAggroTargetProvider>();
                }

                SerializedObject serializedBrain = new SerializedObject(brain);
                serializedBrain.FindProperty("targetProviderBehaviour")
                    .objectReferenceValue = targetProvider;
                serializedBrain.ApplyModifiedPropertiesWithoutUndo();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    contents,
                    AssaultEnemyPrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        $"Could not save {AssaultEnemyPrefabPath}.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.ImportAsset(
                AssaultEnemyPrefabPath,
                ImportAssetOptions.ForceSynchronousImport);
            return ValidateAssaultEnemyPrefab();
        }

        private static AssaultWaveDefinition CreateOrUpdateWaveDefinition(
            GameObject enemyPrefab)
        {
            EnsureFolder(
                "Assets/_Project/ScriptableObjects",
                "Run",
                RunDataFolderPath);

            AssaultWaveDefinition definition =
                AssetDatabase.LoadAssetAtPath<AssaultWaveDefinition>(
                    WaveDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<AssaultWaveDefinition>();
                AssetDatabase.CreateAsset(definition, WaveDefinitionPath);
            }

            SerializedObject serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("initialDelay").floatValue = 1f;
            SerializedProperty groups =
                serializedDefinition.FindProperty("spawnGroups");
            groups.arraySize = 2;
            ConfigureSpawnGroup(
                groups.GetArrayElementAtIndex(0),
                enemyPrefab,
                enemyCount: 3,
                delayBeforeGroup: 0f,
                spawnInterval: 0.4f);
            ConfigureSpawnGroup(
                groups.GetArrayElementAtIndex(1),
                enemyPrefab,
                enemyCount: 3,
                delayBeforeGroup: 3f,
                spawnInterval: 0.4f);
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static AssaultReturnPortalInteractable
            CreateOrUpdateReturnPortalPrefab()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                SourcePortalPrefabPath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Source portal prefab is missing: {SourcePortalPrefabPath}");
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(
                SourcePortalPrefabPath);
            try
            {
                contents.name = "AssaultReturnPortal";
                RunPortalInteractable entryPortal =
                    contents.GetComponent<RunPortalInteractable>();
                if (entryPortal != null)
                    UnityEngine.Object.DestroyImmediate(entryPortal, true);

                AssaultReturnPortalInteractable returnPortal =
                    contents.GetComponent<AssaultReturnPortalInteractable>();
                if (returnPortal == null)
                {
                    returnPortal =
                        contents.AddComponent<AssaultReturnPortalInteractable>();
                }

                SerializedObject serializedPortal =
                    new SerializedObject(returnPortal);
                serializedPortal.FindProperty("interactionRange").floatValue = 2f;
                serializedPortal.ApplyModifiedPropertiesWithoutUndo();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    contents,
                    ReturnPortalPrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        $"Could not save {ReturnPortalPrefabPath}.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.ImportAsset(
                ReturnPortalPrefabPath,
                ImportAssetOptions.ForceSynchronousImport);
            return ValidateReturnPortalPrefab();
        }

        private static void ConfigureScene(
            Scene scene,
            AssaultWaveDefinition waveDefinition,
            AssaultReturnPortalInteractable returnPortalPrefab)
        {
            GameObject runtimeObject = FindRootObject(scene, RuntimeObjectName);
            if (runtimeObject == null)
                throw new InvalidOperationException("RunFlowRuntime scene object is missing.");

            RunFlowRuntime runtime = runtimeObject.GetComponent<RunFlowRuntime>();
            if (runtime == null)
                throw new InvalidOperationException("RunFlowRuntime component is missing.");

            GameObject arenaRoot = FindRootObject(scene, ArenaObjectName);
            if (arenaRoot == null)
                arenaRoot = CreateArenaRoot();

            Transform destination = arenaRoot.transform.Find(ArenaDestinationName);
            Transform spawnPointsRoot = arenaRoot.transform.Find(
                SpawnPointsObjectName);
            Transform returnPortalSpawnPoint = arenaRoot.transform.Find(
                ReturnPortalSpawnPointName);
            if (returnPortalSpawnPoint == null)
            {
                returnPortalSpawnPoint = CreateAnchor(
                    ReturnPortalSpawnPointName,
                    arenaRoot.transform,
                    new Vector3(0f, 0f, -10f));
                returnPortalSpawnPoint.localRotation = Quaternion.identity;
            }
            if (destination == null || spawnPointsRoot == null ||
                spawnPointsRoot.childCount == 0)
            {
                throw new InvalidOperationException(
                    "Assault arena anchors are incomplete.");
            }

            AssaultEnemyRegistry registry =
                GetOrAddComponent<AssaultEnemyRegistry>(runtimeObject);
            AssaultTargetRegistry targetRegistry =
                GetOrAddComponent<AssaultTargetRegistry>(runtimeObject);
            LocalAssaultArenaGateway gateway =
                GetOrAddComponent<LocalAssaultArenaGateway>(runtimeObject);
            AssaultWaveSpawner waveSpawner =
                GetOrAddComponent<AssaultWaveSpawner>(runtimeObject);
            AssaultArenaTransitionController transitionController =
                GetOrAddComponent<AssaultArenaTransitionController>(runtimeObject);
            AssaultReturnPortalSpawner returnPortalSpawner =
                GetOrAddComponent<AssaultReturnPortalSpawner>(runtimeObject);

            SerializedObject serializedRegistry = new SerializedObject(registry);
            serializedRegistry.FindProperty("runFlowRuntime").objectReferenceValue = runtime;
            serializedRegistry.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedGateway = new SerializedObject(gateway);
            serializedGateway.FindProperty("assaultDestination")
                .objectReferenceValue = destination;
            serializedGateway.FindProperty("navMeshSampleRadius").floatValue = 2f;
            serializedGateway.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedSpawner = new SerializedObject(waveSpawner);
            serializedSpawner.FindProperty("runFlowRuntime").objectReferenceValue = runtime;
            serializedSpawner.FindProperty("enemyRegistry").objectReferenceValue = registry;
            serializedSpawner.FindProperty("targetRegistry").objectReferenceValue =
                targetRegistry;
            serializedSpawner.FindProperty("waveDefinition").objectReferenceValue =
                waveDefinition;
            SerializedProperty spawnPoints =
                serializedSpawner.FindProperty("spawnPoints");
            spawnPoints.arraySize = spawnPointsRoot.childCount;
            for (int i = 0; i < spawnPointsRoot.childCount; i++)
            {
                spawnPoints.GetArrayElementAtIndex(i).objectReferenceValue =
                    spawnPointsRoot.GetChild(i);
            }
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

            PlayerBrain player = UnityEngine.Object.FindAnyObjectByType<PlayerBrain>(
                FindObjectsInactive.Include);
            if (player == null)
                throw new InvalidOperationException("PlayerBrain scene object is missing.");

            SerializedObject serializedTransition =
                new SerializedObject(transitionController);
            serializedTransition.FindProperty("runFlowRuntime").objectReferenceValue = runtime;
            serializedTransition.FindProperty("waveSpawner").objectReferenceValue =
                waveSpawner;
            serializedTransition.FindProperty("targetRegistry").objectReferenceValue =
                targetRegistry;
            serializedTransition.FindProperty("arenaGatewaySource").objectReferenceValue =
                gateway;
            serializedTransition.FindProperty("localPlayer").objectReferenceValue = player;
            serializedTransition.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedReturnPortalSpawner =
                new SerializedObject(returnPortalSpawner);
            serializedReturnPortalSpawner.FindProperty("runFlowRuntime")
                .objectReferenceValue = runtime;
            serializedReturnPortalSpawner.FindProperty("transitionController")
                .objectReferenceValue = transitionController;
            serializedReturnPortalSpawner.FindProperty("targetRegistry")
                .objectReferenceValue = targetRegistry;
            serializedReturnPortalSpawner.FindProperty("portalPrefab")
                .objectReferenceValue = returnPortalPrefab;
            serializedReturnPortalSpawner.FindProperty("spawnPoint")
                .objectReferenceValue = returnPortalSpawnPoint;
            serializedReturnPortalSpawner.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static GameObject CreateArenaRoot()
        {
            Material floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                FloorMaterialPath);
            Material spawnMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                SpawnMaterialPath);
            if (floorMaterial == null || spawnMaterial == null)
            {
                throw new InvalidOperationException(
                    "Assault arena placeholder materials are missing.");
            }

            GameObject root = new GameObject(ArenaObjectName);
            Undo.RegisterCreatedObjectUndo(root, "Create Assault Arena");
            root.transform.position = new Vector3(0f, 0f, 100f);

            CreateCube(
                "Floor",
                root.transform,
                new Vector3(0f, -0.5f, 0f),
                new Vector3(28f, 1f, 28f),
                floorMaterial);
            CreateCube(
                "Wall_North",
                root.transform,
                new Vector3(0f, 1f, 14f),
                new Vector3(29f, 2f, 1f),
                floorMaterial);
            CreateCube(
                "Wall_South",
                root.transform,
                new Vector3(0f, 1f, -14f),
                new Vector3(29f, 2f, 1f),
                floorMaterial);
            CreateCube(
                "Wall_East",
                root.transform,
                new Vector3(14f, 1f, 0f),
                new Vector3(1f, 2f, 27f),
                floorMaterial);
            CreateCube(
                "Wall_West",
                root.transform,
                new Vector3(-14f, 1f, 0f),
                new Vector3(1f, 2f, 27f),
                floorMaterial);

            Transform destination = CreateAnchor(
                ArenaDestinationName,
                root.transform,
                new Vector3(0f, 0f, -7f));
            destination.localRotation = Quaternion.identity;

            Transform returnPortalSpawnPoint = CreateAnchor(
                ReturnPortalSpawnPointName,
                root.transform,
                new Vector3(0f, 0f, -10f));
            returnPortalSpawnPoint.localRotation = Quaternion.identity;

            GameObject spawnPointsRoot = new GameObject(SpawnPointsObjectName);
            spawnPointsRoot.transform.SetParent(root.transform, false);
            CreateSpawnPoint(
                "SpawnPoint_01",
                spawnPointsRoot.transform,
                new Vector3(-8f, 0f, 6f),
                spawnMaterial);
            CreateSpawnPoint(
                "SpawnPoint_02",
                spawnPointsRoot.transform,
                new Vector3(0f, 0f, 9f),
                spawnMaterial);
            CreateSpawnPoint(
                "SpawnPoint_03",
                spawnPointsRoot.transform,
                new Vector3(8f, 0f, 6f),
                spawnMaterial);

            NavMeshSurface surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = Physics.AllLayers;
            surface.BuildNavMesh();
            if (surface.navMeshData == null)
                throw new InvalidOperationException("Assault arena NavMesh bake failed.");

            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(
                    ArenaNavMeshDataPath) != null)
            {
                throw new InvalidOperationException(
                    $"Unexpected existing NavMesh asset: {ArenaNavMeshDataPath}");
            }

            AssetDatabase.CreateAsset(surface.navMeshData, ArenaNavMeshDataPath);
            EditorUtility.SetDirty(surface);
            return root;
        }

        private static GameObject CreateCube(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.layer = GroundLayer;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;
            gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(
                gameObject,
                StaticEditorFlags.BatchingStatic);
            return gameObject;
        }

        private static Transform CreateAnchor(
            string name,
            Transform parent,
            Vector3 localPosition)
        {
            GameObject anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = localPosition;
            return anchor.transform;
        }

        private static void CreateSpawnPoint(
            string name,
            Transform parent,
            Vector3 localPosition,
            Material material)
        {
            Transform spawnPoint = CreateAnchor(name, parent, localPosition);
            spawnPoint.localRotation = Quaternion.Euler(0f, 180f, 0f);

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Marker";
            marker.transform.SetParent(spawnPoint, false);
            marker.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            marker.transform.localScale = new Vector3(1.2f, 0.025f, 1.2f);
            marker.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        private static void ConfigureSpawnGroup(
            SerializedProperty group,
            GameObject enemyPrefab,
            int enemyCount,
            float delayBeforeGroup,
            float spawnInterval)
        {
            group.FindPropertyRelative("enemyPrefab").objectReferenceValue =
                enemyPrefab;
            group.FindPropertyRelative("enemyCount").intValue = enemyCount;
            group.FindPropertyRelative("delayBeforeGroup").floatValue =
                delayBeforeGroup;
            group.FindPropertyRelative("spawnInterval").floatValue =
                spawnInterval;
        }

        private static void ValidateInternal()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} before validation.");

            GameObject enemyPrefab = ValidateAssaultEnemyPrefab();
            AssaultReturnPortalInteractable returnPortalPrefab =
                ValidateReturnPortalPrefab();
            AssaultWaveDefinition definition =
                AssetDatabase.LoadAssetAtPath<AssaultWaveDefinition>(
                    WaveDefinitionPath);
            if (definition == null)
            {
                throw new InvalidOperationException(
                    $"Assault wave definition is missing: {WaveDefinitionPath}");
            }

            if (!definition.TryCreatePlan(
                    out AssaultWavePlan plan,
                    out string error))
            {
                throw new InvalidOperationException(
                    $"Assault wave definition is invalid: {error}");
            }

            if (plan.PlannedEnemyCount != 6 || plan.Steps.Count != 2)
                throw new InvalidOperationException("Unexpected prototype wave contents.");

            for (int i = 0; i < plan.Steps.Count; i++)
            {
                if (plan.Steps[i].EnemyPrefab != enemyPrefab)
                    throw new InvalidOperationException("Wave uses an unexpected enemy prefab.");
            }

            Scene scene = SceneManager.GetActiveScene();
            GameObject arenaRoot = FindRootObject(scene, ArenaObjectName);
            if (arenaRoot == null)
                throw new InvalidOperationException("Assault arena root is missing.");

            Transform returnPortalSpawnPoint = arenaRoot.transform.Find(
                ReturnPortalSpawnPointName);
            if (returnPortalSpawnPoint == null)
            {
                throw new InvalidOperationException(
                    "Assault return portal spawn point is missing.");
            }

            NavMeshSurface surface = arenaRoot.GetComponent<NavMeshSurface>();
            if (surface == null || surface.navMeshData == null ||
                AssetDatabase.GetAssetPath(surface.navMeshData) != ArenaNavMeshDataPath)
            {
                throw new InvalidOperationException("Assault arena NavMesh is not persistent.");
            }

            GameObject runtimeObject = FindRootObject(scene, RuntimeObjectName);
            AssaultEnemyRegistry registry =
                runtimeObject != null
                    ? runtimeObject.GetComponent<AssaultEnemyRegistry>()
                    : null;
            AssaultTargetRegistry targetRegistry =
                runtimeObject != null
                    ? runtimeObject.GetComponent<AssaultTargetRegistry>()
                    : null;
            AssaultWaveSpawner spawner =
                runtimeObject != null
                    ? runtimeObject.GetComponent<AssaultWaveSpawner>()
                    : null;
            LocalAssaultArenaGateway gateway =
                runtimeObject != null
                    ? runtimeObject.GetComponent<LocalAssaultArenaGateway>()
                    : null;
            AssaultArenaTransitionController controller =
                runtimeObject != null
                    ? runtimeObject.GetComponent<AssaultArenaTransitionController>()
                    : null;
            AssaultReturnPortalSpawner returnPortalSpawner =
                runtimeObject != null
                    ? runtimeObject.GetComponent<AssaultReturnPortalSpawner>()
                    : null;
            if (registry == null || targetRegistry == null || spawner == null ||
                gateway == null || controller == null ||
                returnPortalSpawner == null)
            {
                throw new InvalidOperationException(
                    "RunFlowRuntime assault arena components are incomplete.");
            }

            SerializedObject serializedSpawner = new SerializedObject(spawner);
            if (serializedSpawner.FindProperty("waveDefinition").objectReferenceValue !=
                    definition ||
                serializedSpawner.FindProperty("targetRegistry").objectReferenceValue !=
                    targetRegistry ||
                serializedSpawner.FindProperty("spawnPoints").arraySize != 3)
            {
                throw new InvalidOperationException(
                    "AssaultWaveSpawner scene wiring is incomplete.");
            }

            SerializedObject serializedController = new SerializedObject(controller);
            if (serializedController.FindProperty("arenaGatewaySource")
                    .objectReferenceValue != gateway ||
                serializedController.FindProperty("targetRegistry")
                    .objectReferenceValue != targetRegistry ||
                serializedController.FindProperty("localPlayer")
                    .objectReferenceValue == null)
            {
                throw new InvalidOperationException(
                    "Assault transition scene wiring is incomplete.");
            }

            SerializedObject serializedReturnPortalSpawner =
                new SerializedObject(returnPortalSpawner);
            if (serializedReturnPortalSpawner.FindProperty("runFlowRuntime")
                    .objectReferenceValue != runtimeObject.GetComponent<RunFlowRuntime>() ||
                serializedReturnPortalSpawner.FindProperty("transitionController")
                    .objectReferenceValue != controller ||
                serializedReturnPortalSpawner.FindProperty("targetRegistry")
                    .objectReferenceValue != targetRegistry ||
                serializedReturnPortalSpawner.FindProperty("portalPrefab")
                    .objectReferenceValue != returnPortalPrefab ||
                serializedReturnPortalSpawner.FindProperty("spawnPoint")
                    .objectReferenceValue != returnPortalSpawnPoint)
            {
                throw new InvalidOperationException(
                    "Assault return portal scene wiring is incomplete.");
            }
        }

        private static AssaultReturnPortalInteractable
            ValidateReturnPortalPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ReturnPortalPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Assault return portal prefab is missing: {ReturnPortalPrefabPath}");
            }

            if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Variant)
            {
                throw new InvalidOperationException(
                    "Assault return portal must remain independent from the entry prefab.");
            }

            AssaultReturnPortalInteractable interactable =
                prefab.GetComponent<AssaultReturnPortalInteractable>();
            CapsuleCollider collider = prefab.GetComponent<CapsuleCollider>();
            TargetVisual visual = prefab.GetComponent<TargetVisual>();
            if (interactable == null || collider == null || visual == null)
            {
                throw new InvalidOperationException(
                    "Assault return portal is missing required components.");
            }

            if (prefab.GetComponent<RunPortalInteractable>() != null)
            {
                throw new InvalidOperationException(
                    "Assault return portal still contains the entry interaction.");
            }

            if (!collider.isTrigger)
            {
                throw new InvalidOperationException(
                    "Assault return portal collider must remain a trigger.");
            }

            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject.layer != InteractableLayer)
                {
                    throw new InvalidOperationException(
                        "Assault return portal does not use the Interactable layer.");
                }
            }

            return interactable;
        }

        private static GameObject ValidateAssaultEnemyPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                AssaultEnemyPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Assault enemy prefab is missing: {AssaultEnemyPrefabPath}");
            }

            if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Variant)
            {
                throw new InvalidOperationException(
                    "Assault enemy must remain independent from the exploration prefab.");
            }

            if (prefab.GetComponentInChildren<EnemyDeathNotifier>(true) == null ||
                prefab.GetComponentInChildren<EnemyRewardSource>(true) == null)
            {
                throw new InvalidOperationException(
                    "Assault enemy must retain death and experience components.");
            }

            EnemyBrain brain = prefab.GetComponentInChildren<EnemyBrain>(true);
            AssaultAggroTargetProvider targetProvider =
                prefab.GetComponentInChildren<AssaultAggroTargetProvider>(true);
            if (brain == null || targetProvider == null)
            {
                throw new InvalidOperationException(
                    "Assault enemy target provider is missing.");
            }

            SerializedObject serializedBrain = new SerializedObject(brain);
            if (serializedBrain.FindProperty("targetProviderBehaviour")
                    .objectReferenceValue != targetProvider)
            {
                throw new InvalidOperationException(
                    "Assault enemy brain does not use the assault target provider.");
            }

            if (prefab.GetComponentInChildren<EnemyLootTableDropper>(true) != null ||
                prefab.GetComponentInChildren<EnemyRunContributionSource>(true) != null ||
                prefab.GetComponentInChildren<EnemyThreatSource>(true) != null)
            {
                throw new InvalidOperationException(
                    "Assault enemy still contains exploration reward components.");
            }

            return prefab;
        }

        private static void RemoveAllComponents<T>(GameObject root)
            where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = components.Length - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(components[i], true);
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(gameObject);
        }

        private static void EnsureFolder(
            string parentPath,
            string folderName,
            string fullPath)
        {
            if (AssetDatabase.IsValidFolder(fullPath))
                return;

            string guid = AssetDatabase.CreateFolder(parentPath, folderName);
            if (string.IsNullOrWhiteSpace(guid))
                throw new InvalidOperationException($"Could not create {fullPath}.");
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Assault Arena wiring {operation} is available only in Edit Mode.");
            }
        }

        private static Scene RequireCleanSampleScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} before arena wiring.");

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
