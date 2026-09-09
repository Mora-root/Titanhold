using System;
using Titanhold.Run;
using Titanhold.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Enemies.Editor
{
    public static class ExplorationTargetingWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string RuntimeObjectName = "RunFlowRuntime";

        private static readonly string[] ExplorationPrefabPaths =
        {
            "Assets/_Project/Prefabs/Enemy/Skelet.prefab",
            "Assets/_Project/Prefabs/Enemy/Skelet_Warrior.prefab"
        };

        [MenuItem("Tools/Titanhold/Install Exploration Targeting Wiring")]
        public static void Install()
        {
            try
            {
                Scene scene = RequireActiveScene(requireClean: true);
                GameObject runtimeObject = RequireRuntimeObject(scene);
                RunSceneSessionEntryPoint entryPoint =
                    UnityEngine.Object.FindAnyObjectByType<
                        RunSceneSessionEntryPoint>(FindObjectsInactive.Include);
                if (entryPoint == null || entryPoint.gameObject.scene != scene)
                {
                    throw new InvalidOperationException(
                        "Run scene session entry point is missing.");
                }

                ExplorationTargetRegistry registry =
                    GetOrAddComponent<ExplorationTargetRegistry>(runtimeObject);
                ExplorationTargetRosterController controller =
                    GetOrAddComponent<ExplorationTargetRosterController>(
                        runtimeObject);
                controller.ConfigureForEditor(entryPoint, registry);
                EditorUtility.SetDirty(registry);
                EditorUtility.SetDirty(controller);

                for (int i = 0; i < ExplorationPrefabPaths.Length; i++)
                    ConfigurePrefab(ExplorationPrefabPaths[i]);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Could not save {ScenePath}.");
                }

                AssetDatabase.SaveAssets();
                ValidateInternal(scene);
                Debug.Log("Exploration targeting wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Exploration targeting wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Exploration Targeting Wiring")]
        public static void Validate()
        {
            try
            {
                Scene scene = RequireActiveScene(requireClean: false);
                ValidateInternal(scene);
                Debug.Log("Exploration targeting wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Exploration targeting wiring validation failed: {exception}");
            }
        }

        private static void ConfigurePrefab(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                EnemyBrain brain = root.GetComponentInChildren<EnemyBrain>(true);
                EnemySensor sensor = root.GetComponentInChildren<EnemySensor>(true);
                if (brain == null || sensor == null ||
                    brain.gameObject != sensor.gameObject)
                {
                    throw new InvalidOperationException(
                        $"Exploration prefab '{path}' has incompatible enemy wiring.");
                }

                ExplorationAggroTargetProvider provider =
                    brain.GetComponent<ExplorationAggroTargetProvider>();
                if (provider == null)
                {
                    provider = brain.gameObject.AddComponent<
                        ExplorationAggroTargetProvider>();
                }

                SerializedObject serializedProvider = new(provider);
                serializedProvider.FindProperty("localAggroSensor")
                    .objectReferenceValue = sensor;
                serializedProvider.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject serializedBrain = new(brain);
                serializedBrain.FindProperty("targetProviderBehaviour")
                    .objectReferenceValue = provider;
                serializedBrain.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateInternal(Scene scene)
        {
            GameObject runtimeObject = RequireRuntimeObject(scene);
            ExplorationTargetRegistry registry =
                runtimeObject.GetComponent<ExplorationTargetRegistry>();
            ExplorationTargetRosterController controller =
                runtimeObject.GetComponent<ExplorationTargetRosterController>();
            if (registry == null || controller == null ||
                controller.TargetRegistry != registry ||
                controller.SessionEntryPoint == null ||
                controller.SessionEntryPoint.gameObject.scene != scene)
            {
                throw new InvalidOperationException(
                    "Exploration target roster scene wiring is incomplete.");
            }

            for (int i = 0; i < ExplorationPrefabPaths.Length; i++)
                ValidatePrefab(ExplorationPrefabPaths[i]);
        }

        private static void ValidatePrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Exploration prefab '{path}' is missing.");
            }

            EnemyBrain brain = prefab.GetComponentInChildren<EnemyBrain>(true);
            EnemySensor sensor = prefab.GetComponentInChildren<EnemySensor>(true);
            ExplorationAggroTargetProvider provider =
                prefab.GetComponentInChildren<ExplorationAggroTargetProvider>(
                    true);
            if (brain == null || sensor == null || provider == null ||
                brain.gameObject != provider.gameObject)
            {
                throw new InvalidOperationException(
                    $"Exploration prefab '{path}' has incomplete targeting components.");
            }

            SerializedObject serializedBrain = new(brain);
            SerializedObject serializedProvider = new(provider);
            if (serializedBrain.FindProperty("targetProviderBehaviour")
                    .objectReferenceValue != provider ||
                serializedProvider.FindProperty("localAggroSensor")
                    .objectReferenceValue != sensor)
            {
                throw new InvalidOperationException(
                    $"Exploration prefab '{path}' has stale targeting references.");
            }
        }

        private static Scene RequireActiveScene(bool requireClean)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} first.");

            if (requireClean && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "The active scene has unrelated unsaved changes.");
            }

            return scene;
        }

        private static GameObject RequireRuntimeObject(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == RuntimeObjectName &&
                    roots[i].GetComponent<RunFlowRuntime>() != null)
                {
                    return roots[i];
                }
            }

            throw new InvalidOperationException(
                "RunFlowRuntime scene object is missing.");
        }

        private static T GetOrAddComponent<T>(GameObject owner)
            where T : Component
        {
            T component = owner.GetComponent<T>();
            return component != null ? component : owner.AddComponent<T>();
        }
    }
}
