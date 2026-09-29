using System;
using Titanhold.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Run.Editor
{
    public static class RunChapterBossEncounterWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string RuntimeObjectName = "RunFlowRuntime";
        private const string ArenaObjectName = "AssaultArena_Prototype";
        private const string SpawnPointsObjectName = "EnemySpawnPoints";
        private const string BossSpawnPointName = "SpawnPoint_01";
        private const string BossPrefabPath =
            "Assets/_Project/Prefabs/Enemy/" +
            "Skelet_ChapterBoss_Prototype.prefab";

        [MenuItem(
            "Tools/Titanhold/Install Run Chapter Boss Encounter Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                Scene scene = RequireSampleScene(requireClean: true);
                GameObject runtimeObject =
                    RequireRootObject(scene, RuntimeObjectName);
                RunChapterBossEncounterCoordinator coordinator =
                    runtimeObject.GetComponent<
                        RunChapterBossEncounterCoordinator>();
                if (coordinator == null)
                {
                    coordinator = Undo.AddComponent<
                        RunChapterBossEncounterCoordinator>(runtimeObject);
                }

                Configure(scene, runtimeObject, coordinator);
                EditorUtility.SetDirty(coordinator);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException($"Could not save {ScenePath}.");

                ValidateInternal(scene);
                Debug.Log(
                    "Run Chapter Boss Encounter wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Run Chapter Boss Encounter wiring installation failed: " +
                    exception);
            }
        }

        [MenuItem(
            "Tools/Titanhold/Validate Run Chapter Boss Encounter Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                Scene scene = RequireSampleScene(requireClean: false);
                ValidateInternal(scene);
                Debug.Log(
                    "Run Chapter Boss Encounter wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Run Chapter Boss Encounter wiring validation failed: " +
                    exception);
            }
        }

        private static void Configure(
            Scene scene,
            GameObject runtimeObject,
            RunChapterBossEncounterCoordinator coordinator)
        {
            RunChapterFlowRuntime chapterRuntime =
                RequireComponent<RunChapterFlowRuntime>(runtimeObject);
            RunFlowRuntime legacyRuntime =
                RequireComponent<RunFlowRuntime>(runtimeObject);
            LocalAssaultArenaGateway gateway =
                RequireComponent<LocalAssaultArenaGateway>(runtimeObject);
            AssaultTargetRegistry targetRegistry =
                RequireComponent<AssaultTargetRegistry>(runtimeObject);
            PlayerBrain player = UnityEngine.Object.FindAnyObjectByType<
                PlayerBrain>(FindObjectsInactive.Include);
            if (player == null)
                throw new InvalidOperationException("PlayerBrain is missing.");

            GameObject bossPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            if (bossPrefab == null)
                throw new InvalidOperationException("Boss prefab is missing.");

            EnemyDefinitionCatalog enemyDefinitions =
                legacyRuntime.EnemyDefinitions;
            if (enemyDefinitions == null || !enemyDefinitions.IsValid)
            {
                throw new InvalidOperationException(
                    "Enemy definition catalog is missing or invalid.");
            }

            GameObject arena = RequireRootObject(scene, ArenaObjectName);
            Transform spawnPoints = arena.transform.Find(
                SpawnPointsObjectName);
            Transform bossSpawnPoint = spawnPoints != null
                ? spawnPoints.Find(BossSpawnPointName)
                : null;
            if (bossSpawnPoint == null)
            {
                throw new InvalidOperationException(
                    $"Boss spawn point '{BossSpawnPointName}' is missing.");
            }

            coordinator.ConfigureForEditor(
                chapterRuntime,
                gateway,
                targetRegistry,
                enemyDefinitions,
                bossPrefab,
                bossSpawnPoint,
                player);
        }

        private static void ValidateInternal(Scene scene)
        {
            GameObject runtimeObject =
                RequireRootObject(scene, RuntimeObjectName);
            RunChapterBossEncounterCoordinator coordinator =
                RequireComponent<RunChapterBossEncounterCoordinator>(
                    runtimeObject);
            RequireComponent<AssaultArenaTransitionController>(runtimeObject);
            RequireComponent<AssaultWaveSpawner>(runtimeObject);

            if (!coordinator.IsConfigured)
            {
                throw new InvalidOperationException(
                    "Chapter boss encounter coordinator is incomplete.");
            }

            if (AssetDatabase.GetAssetPath(coordinator.BossPrefab) !=
                BossPrefabPath)
            {
                throw new InvalidOperationException(
                    "Chapter encounter does not use the prototype boss prefab.");
            }

            GameObject arena = RequireRootObject(scene, ArenaObjectName);
            Transform expectedSpawn = arena.transform
                .Find(SpawnPointsObjectName)
                ?.Find(BossSpawnPointName);
            if (expectedSpawn == null ||
                coordinator.BossSpawnPoint != expectedSpawn)
            {
                throw new InvalidOperationException(
                    "Chapter boss encounter uses an unexpected spawn point.");
            }

            if (coordinator.ChapterFlowRuntime !=
                    runtimeObject.GetComponent<RunChapterFlowRuntime>() ||
                coordinator.ArenaGatewaySource !=
                    runtimeObject.GetComponent<LocalAssaultArenaGateway>() ||
                coordinator.TargetRegistry !=
                    runtimeObject.GetComponent<AssaultTargetRegistry>())
            {
                throw new InvalidOperationException(
                    "Chapter boss encounter runtime references are incorrect.");
            }
        }

        private static Scene RequireSampleScene(bool requireClean)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} first.");
            if (requireClean && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "SampleScene has unrelated unsaved changes.");
            }

            return scene;
        }

        private static GameObject RequireRootObject(
            Scene scene,
            string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                    return roots[i];
            }

            throw new InvalidOperationException(
                $"Scene object '{objectName}' is missing.");
        }

        private static T RequireComponent<T>(GameObject owner)
            where T : Component
        {
            T component = owner.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"'{owner.name}' is missing {typeof(T).Name}.");
            }

            return component;
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before {operation}.");
            }
        }
    }
}
